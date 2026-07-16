from collections.abc import Sequence
from uuid import UUID, uuid4

from sqlalchemy import delete, func, select, text, update
from sqlalchemy.ext.asyncio import AsyncSession, async_sessionmaker

from speedclaim_ai.config.settings import EMBEDDING_DIMENSION
from speedclaim_ai.database.models import RagChunk, RagDocument, RagIngestionRun
from speedclaim_ai.repositories.vector_repository import (
    ChunkInput,
    ChunkMatch,
    DocumentInput,
    DocumentRecord,
    ImmutableDocumentConflict,
    StoredChunk,
    StoreResult,
    validate_document_batch,
    validate_embedding,
)


class PgVectorRepository:
    def __init__(
        self,
        session_factory: async_sessionmaker[AsyncSession],
        *,
        dimension: int = EMBEDDING_DIMENSION,
    ) -> None:
        self._session_factory = session_factory
        self._dimension = dimension

    async def get_document_by_brochure_id(
        self, brochure_id: UUID
    ) -> DocumentRecord | None:
        async with self._session_factory() as session:
            document = await session.scalar(
                select(RagDocument).where(RagDocument.brochure_id == brochure_id)
            )
            if document is None:
                return None
            parent_count = await session.scalar(
                select(func.count(RagChunk.id)).where(
                    RagChunk.document_id == document.id,
                    RagChunk.embedding.is_(None),
                )
            )
            child_count = await session.scalar(
                select(func.count(RagChunk.id)).where(
                    RagChunk.document_id == document.id,
                    RagChunk.embedding.is_not(None),
                )
            )
        return DocumentRecord(
            document_id=document.id,
            brochure_id=document.brochure_id,
            product_id=document.product_id,
            brochure_version=document.brochure_version,
            content_hash=document.content_hash,
            page_count=document.page_count,
            parent_chunk_count=int(parent_count or 0),
            child_chunk_count=int(child_count or 0),
            embedding_provider=document.embedding_provider,
            embedding_model=document.embedding_model,
            embedding_dimension=document.embedding_dimension,
        )

    async def store_document(
        self, document: DocumentInput, chunks: Sequence[ChunkInput]
    ) -> StoreResult:
        identified_chunks = validate_document_batch(document, chunks)
        if document.embedding_dimension != self._dimension:
            raise ValueError("document embedding dimension does not match repository dimension")

        async with self._session_factory() as session, session.begin():
            # Serialize same-brochure writers even before the unique row exists.
            await session.execute(
                text("SELECT pg_advisory_xact_lock(hashtextextended(:brochure_id, 0))"),
                {"brochure_id": str(document.brochure_id)},
            )
            existing = await session.scalar(
                select(RagDocument)
                .where(RagDocument.brochure_id == document.brochure_id)
                .with_for_update()
            )
            if existing is not None:
                if existing.content_hash == document.content_hash:
                    return StoreResult(document_id=existing.id, created=False)
                raise ImmutableDocumentConflict(
                    "brochure ID is already indexed with different immutable content"
                )

            stored_document = RagDocument(
                brochure_id=document.brochure_id,
                product_id=document.product_id,
                brochure_version=document.brochure_version.strip(),
                content_hash=document.content_hash,
                page_count=document.page_count,
                embedding_provider=document.embedding_provider.strip(),
                embedding_model=document.embedding_model.strip(),
                embedding_dimension=document.embedding_dimension,
            )
            session.add(stored_document)
            await session.flush()

            # Parents must exist before child foreign keys are inserted.
            ordered_chunks = sorted(
                identified_chunks, key=lambda item: item[0].parent_chunk_id is not None
            )
            for chunk, chunk_id in ordered_chunks:
                session.add(self._to_model(stored_document.id, chunk, chunk_id))
                if chunk.parent_chunk_id is None:
                    await session.flush()

            return StoreResult(document_id=stored_document.id, created=True)

    async def search(
        self, brochure_id: UUID, query_embedding: Sequence[float], *, limit: int
    ) -> list[ChunkMatch]:
        if not 1 <= limit <= 100:
            raise ValueError("search limit must be between 1 and 100")
        query = validate_embedding(query_embedding, dimension=self._dimension)
        distance = RagChunk.embedding.cosine_distance(query).label("distance")
        statement = (
            select(RagChunk, RagDocument.brochure_id, distance)
            .join(RagDocument, RagDocument.id == RagChunk.document_id)
            .where(
                RagDocument.brochure_id == brochure_id,
                RagChunk.embedding.is_not(None),
            )
            .order_by(distance, RagChunk.chunk_index)
            .limit(limit)
        )

        async with self._session_factory() as session:
            rows = (await session.execute(statement)).all()
        return [
            ChunkMatch(
                chunk_id=chunk.id,
                document_id=chunk.document_id,
                brochure_id=stored_brochure_id,
                parent_chunk_id=chunk.parent_chunk_id,
                page_number=chunk.page_number,
                section_title=chunk.section_title,
                clause_reference=chunk.clause_reference,
                chunk_index=chunk.chunk_index,
                content=chunk.content,
                score=max(-1.0, min(1.0, 1.0 - float(raw_distance))),
            )
            for chunk, stored_brochure_id, raw_distance in rows
        ]

    async def get_chunks_by_ids(
        self, brochure_id: UUID, chunk_ids: Sequence[UUID]
    ) -> list[StoredChunk]:
        requested_ids = list(dict.fromkeys(chunk_ids))
        if not requested_ids:
            return []
        if len(requested_ids) > 100:
            raise ValueError("at most 100 chunks can be loaded at once")

        statement = (
            select(RagChunk, RagDocument.brochure_id)
            .join(RagDocument, RagDocument.id == RagChunk.document_id)
            .where(
                RagDocument.brochure_id == brochure_id,
                RagChunk.id.in_(requested_ids),
            )
        )
        async with self._session_factory() as session:
            rows = (await session.execute(statement)).all()

        by_id = {
            chunk.id: StoredChunk(
                chunk_id=chunk.id,
                document_id=chunk.document_id,
                brochure_id=stored_brochure_id,
                parent_chunk_id=chunk.parent_chunk_id,
                page_number=chunk.page_number,
                section_title=chunk.section_title,
                clause_reference=chunk.clause_reference,
                chunk_index=chunk.chunk_index,
                content=chunk.content,
            )
            for chunk, stored_brochure_id in rows
        }
        return [by_id[chunk_id] for chunk_id in requested_ids if chunk_id in by_id]

    async def delete_by_brochure_id(self, brochure_id: UUID) -> bool:
        async with self._session_factory() as session, session.begin():
            result = await session.execute(
                delete(RagDocument).where(RagDocument.brochure_id == brochure_id)
            )
            return bool(result.rowcount)

    async def start_ingestion_run(self, brochure_id: UUID) -> UUID:
        run_id = uuid4()
        async with self._session_factory() as session, session.begin():
            session.add(
                RagIngestionRun(
                    id=run_id,
                    brochure_id=brochure_id,
                    status="Processing",
                )
            )
        return run_id

    async def complete_ingestion_run(
        self, run_id: UUID, *, page_count: int, chunk_count: int
    ) -> None:
        async with self._session_factory() as session, session.begin():
            result = await session.execute(
                update(RagIngestionRun)
                .where(
                    RagIngestionRun.id == run_id,
                    RagIngestionRun.status == "Processing",
                )
                .values(
                    status="Succeeded",
                    completed_at=func.now(),
                    page_count=page_count,
                    chunk_count=chunk_count,
                    error_code=None,
                    error_message_redacted=None,
                )
            )
            if result.rowcount != 1:
                raise RuntimeError("ingestion run is missing or no longer processing")

    async def fail_ingestion_run(
        self, run_id: UUID, *, error_code: str, error_message_redacted: str
    ) -> None:
        async with self._session_factory() as session, session.begin():
            result = await session.execute(
                update(RagIngestionRun)
                .where(
                    RagIngestionRun.id == run_id,
                    RagIngestionRun.status == "Processing",
                )
                .values(
                    status="Failed",
                    completed_at=func.now(),
                    error_code=error_code[:128],
                    error_message_redacted=error_message_redacted[:1024],
                )
            )
            if result.rowcount != 1:
                raise RuntimeError("ingestion run is missing or no longer processing")

    @staticmethod
    def _to_model(document_id: UUID, chunk: ChunkInput, chunk_id: UUID) -> RagChunk:
        return RagChunk(
            id=chunk_id,
            document_id=document_id,
            parent_chunk_id=chunk.parent_chunk_id,
            page_number=chunk.page_number,
            section_title=chunk.section_title.strip() if chunk.section_title else None,
            clause_reference=(
                chunk.clause_reference.strip() if chunk.clause_reference else None
            ),
            chunk_index=chunk.chunk_index,
            content=chunk.content.strip(),
            content_hash=chunk.content_hash,
            token_count=chunk.token_count,
            embedding=(
                validate_embedding(chunk.embedding) if chunk.embedding is not None else None
            ),
        )
