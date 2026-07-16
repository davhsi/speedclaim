import asyncio
import hashlib
import logging
from collections.abc import Sequence
from uuid import UUID

from speedclaim_ai.providers.embeddings.base import EmbeddingProvider
from speedclaim_ai.providers.storage.base import BrochureReader
from speedclaim_ai.rag.chunker import HierarchicalChunker
from speedclaim_ai.rag.errors import IngestionFailure
from speedclaim_ai.rag.models import IngestionCommand, IngestionResult, PreparedChunk
from speedclaim_ai.rag.pdf_parser import PdfParser
from speedclaim_ai.repositories.vector_repository import (
    ChunkInput,
    DocumentInput,
    DocumentRecord,
    ImmutableDocumentConflict,
    VectorRepository,
)

_logger = logging.getLogger(__name__)


class BrochureIngestionService:
    def __init__(
        self,
        *,
        brochure_reader: BrochureReader,
        parser: PdfParser,
        chunker: HierarchicalChunker,
        embedding_provider: EmbeddingProvider,
        repository: VectorRepository,
        max_pdf_size_bytes: int,
    ) -> None:
        self._brochure_reader = brochure_reader
        self._parser = parser
        self._chunker = chunker
        self._embedding_provider = embedding_provider
        self._repository = repository
        self._max_pdf_size_bytes = max_pdf_size_bytes

    async def ingest(self, command: IngestionCommand) -> IngestionResult:
        run_id = await self._repository.start_ingestion_run(command.brochure_id)
        try:
            existing = await self._repository.get_document_by_brochure_id(command.brochure_id)
            if existing is not None:
                if (
                    existing.content_hash != command.content_hash
                    or existing.product_id != command.product_id
                    or existing.brochure_version != command.brochure_version
                ):
                    raise IngestionFailure(
                        code="brochure_immutable_conflict",
                        message="The brochure ID is already indexed with different immutable metadata.",
                        status_code=409,
                    )
                await self._repository.complete_ingestion_run(
                    run_id,
                    page_count=existing.page_count,
                    chunk_count=existing.child_chunk_count,
                )
                return self._existing_result(command, existing)

            data = await self._brochure_reader.read_bytes(
                command.blob_path,
                max_bytes=self._max_pdf_size_bytes,
            )
            actual_hash = hashlib.sha256(data).hexdigest()
            if actual_hash != command.content_hash:
                raise IngestionFailure(
                    code="brochure_hash_mismatch",
                    message="The brochure content hash does not match the stored file.",
                    status_code=409,
                )

            parsed = await asyncio.to_thread(self._parser.parse, data)
            prepared = await asyncio.to_thread(self._chunker.create_chunks, parsed)
            child_chunks = [chunk for chunk in prepared if not chunk.is_parent]
            if not child_chunks:
                raise IngestionFailure(
                    code="pdf_empty",
                    message="The PDF does not contain indexable text chunks.",
                )

            embedding_texts = [self._embedding_text(chunk) for chunk in child_chunks]
            embeddings = await asyncio.to_thread(
                self._embedding_provider.embed_documents,
                embedding_texts,
            )
            chunks = self._with_embeddings(prepared, embeddings)
            stored = await self._repository.store_document(
                DocumentInput(
                    brochure_id=command.brochure_id,
                    product_id=command.product_id,
                    brochure_version=command.brochure_version,
                    content_hash=actual_hash,
                    page_count=parsed.page_count,
                    embedding_provider=self._embedding_provider.provider_name,
                    embedding_model=self._embedding_provider.model_name,
                    embedding_dimension=self._embedding_provider.dimension,
                ),
                chunks,
            )

            parent_count = sum(chunk.is_parent for chunk in prepared)
            child_count = len(child_chunks)
            status = "Succeeded" if stored.created else "NoOp"
            if not stored.created:
                raced = await self._repository.get_document_by_brochure_id(command.brochure_id)
                if raced is not None:
                    parent_count = raced.parent_chunk_count
                    child_count = raced.child_chunk_count
            await self._repository.complete_ingestion_run(
                run_id,
                page_count=parsed.page_count,
                chunk_count=child_count,
            )
            _logger.info(
                "Brochure ingestion completed",
                extra={
                    "event": "brochure.ingestion.completed",
                    "requestId": str(command.request_id),
                    "brochureId": str(command.brochure_id),
                    "productId": str(command.product_id),
                    "brochureVersion": command.brochure_version,
                    "pageCount": parsed.page_count,
                    "parentChunkCount": parent_count,
                    "childChunkCount": child_count,
                    "result": status,
                },
            )
            return IngestionResult(
                request_id=command.request_id,
                document_id=stored.document_id,
                status=status,
                page_count=parsed.page_count,
                parent_chunk_count=parent_count,
                child_chunk_count=child_count,
                embedding_provider=self._embedding_provider.provider_name,
                embedding_model=self._embedding_provider.model_name,
                embedding_dimension=self._embedding_provider.dimension,
            )
        except ImmutableDocumentConflict as exc:
            failure = IngestionFailure(
                code="brochure_immutable_conflict",
                message="The brochure ID is already indexed with different immutable content.",
                status_code=409,
            )
            await self._record_failure(run_id, failure)
            raise failure from exc
        except IngestionFailure as failure:
            await self._record_failure(run_id, failure)
            raise
        except Exception as exc:
            failure = IngestionFailure(
                code="ingestion_failed",
                message="The brochure could not be ingested.",
                status_code=500,
            )
            await self._record_failure(run_id, failure)
            _logger.exception(
                "Brochure ingestion failed",
                extra={
                    "event": "brochure.ingestion.failed",
                    "requestId": str(command.request_id),
                    "brochureId": str(command.brochure_id),
                    "errorCode": failure.code,
                },
            )
            raise failure from exc

    async def _record_failure(self, run_id: UUID, failure: IngestionFailure) -> None:
        try:
            await self._repository.fail_ingestion_run(
                run_id,
                error_code=failure.code,
                error_message_redacted=failure.message,
            )
        except Exception:
            _logger.exception(
                "Unable to persist brochure ingestion failure state",
                extra={
                    "event": "brochure.ingestion.failure_state_failed",
                    "ingestionRunId": str(run_id),
                    "errorCode": failure.code,
                },
            )

    def _existing_result(
        self,
        command: IngestionCommand,
        existing: DocumentRecord,
    ) -> IngestionResult:
        return IngestionResult(
            request_id=command.request_id,
            document_id=existing.document_id,
            status="NoOp",
            page_count=existing.page_count,
            parent_chunk_count=existing.parent_chunk_count,
            child_chunk_count=existing.child_chunk_count,
            embedding_provider=existing.embedding_provider,
            embedding_model=existing.embedding_model,
            embedding_dimension=existing.embedding_dimension,
        )

    @staticmethod
    def _embedding_text(chunk: PreparedChunk) -> str:
        metadata = [value for value in (chunk.section_title, chunk.clause_reference) if value]
        return "\n".join([*metadata, chunk.content])

    @staticmethod
    def _with_embeddings(
        prepared: Sequence[PreparedChunk], embeddings: Sequence[Sequence[float]]
    ) -> list[ChunkInput]:
        iterator = iter(embeddings)
        chunks: list[ChunkInput] = []
        for chunk in prepared:
            embedding = None if chunk.is_parent else next(iterator, None)
            if not chunk.is_parent and embedding is None:
                raise RuntimeError("embedding provider returned too few vectors")
            chunks.append(
                ChunkInput(
                    id=chunk.id,
                    parent_chunk_id=chunk.parent_chunk_id,
                    page_number=chunk.page_number,
                    section_title=chunk.section_title,
                    clause_reference=chunk.clause_reference,
                    chunk_index=chunk.chunk_index,
                    content=chunk.content,
                    content_hash=chunk.content_hash,
                    token_count=chunk.token_count,
                    embedding=embedding,
                )
            )
        if next(iterator, None) is not None:
            raise RuntimeError("embedding provider returned too many vectors")
        return chunks
