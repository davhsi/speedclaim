import hashlib
import os
from dataclasses import replace
from pathlib import Path
from uuid import uuid4

import pytest
from alembic import command
from alembic.config import Config
from sqlalchemy import select

from speedclaim_ai.config.settings import DEFAULT_EMBEDDING_MODEL, EMBEDDING_DIMENSION
from speedclaim_ai.database.models import RagChunk, RagDocument, RagIngestionRun
from speedclaim_ai.database.session import create_database_engine, create_session_factory
from speedclaim_ai.providers.storage.local import LocalBrochureReader
from speedclaim_ai.rag.chunker import HierarchicalChunker
from speedclaim_ai.rag.errors import IngestionFailure
from speedclaim_ai.rag.ingestion_service import BrochureIngestionService
from speedclaim_ai.rag.models import IngestionCommand
from speedclaim_ai.rag.pdf_parser import PdfParser
from speedclaim_ai.repositories.pgvector_repository import PgVectorRepository
from speedclaim_ai.repositories.vector_repository import (
    ChunkInput,
    DocumentInput,
    ImmutableDocumentConflict,
)

pytestmark = [pytest.mark.database, pytest.mark.anyio]


@pytest.fixture
def anyio_backend() -> str:
    return "asyncio"


@pytest.fixture(scope="module", autouse=True)
def migrated_database():
    connection_string = os.getenv("AI_TEST_VECTOR_CONNECTION_STRING")
    if not connection_string:
        pytest.skip("AI_TEST_VECTOR_CONNECTION_STRING is not configured")

    os.environ["AI__VectorConnectionString"] = connection_string
    alembic = Config("alembic.ini")
    command.downgrade(alembic, "base")
    command.upgrade(alembic, "head")
    yield connection_string
    command.downgrade(alembic, "base")


def _sha(value: str) -> str:
    return hashlib.sha256(value.encode()).hexdigest()


def _vector(index: int) -> list[float]:
    vector = [0.0] * EMBEDDING_DIMENSION
    vector[index] = 1.0
    return vector


def _document(brochure_id, product_id, content: str) -> DocumentInput:
    return DocumentInput(
        brochure_id=brochure_id,
        product_id=product_id,
        brochure_version="1",
        content_hash=_sha(content),
        page_count=2,
        embedding_provider="FastEmbed",
        embedding_model=DEFAULT_EMBEDDING_MODEL,
    )


def _chunk(content: str, index: int, embedding_index: int) -> ChunkInput:
    return ChunkInput(
        page_number=index + 1,
        chunk_index=index,
        content=content,
        content_hash=_sha(content),
        token_count=len(content.split()),
        embedding=_vector(embedding_index),
    )


class DeterministicEmbeddingProvider:
    provider_name = "DeterministicTest"
    model_name = DEFAULT_EMBEDDING_MODEL
    dimension = EMBEDDING_DIMENSION

    def embed_documents(self, texts) -> list[list[float]]:
        return [_vector(0) for _ in texts]

    def embed_query(self, _text: str) -> list[float]:
        return _vector(0)


async def test_transactional_idempotent_filtered_search_and_delete(
    migrated_database: str,
) -> None:
    connection_string = migrated_database
    engine = create_database_engine(connection_string)
    repository = PgVectorRepository(create_session_factory(engine))
    brochure_a = uuid4()
    brochure_b = uuid4()
    product_id = uuid4()
    document_a = _document(brochure_a, product_id, "brochure-a")
    document_b = _document(brochure_b, product_id, "brochure-b")

    try:
        stored_a = await repository.store_document(
            document_a,
            [
                _chunk("Waiting period is thirty days.", 0, 0),
                _chunk("Dental treatment is excluded.", 1, 1),
            ],
        )
        stored_b = await repository.store_document(
            document_b,
            [_chunk("Another brochure has similar waiting period text.", 0, 0)],
        )

        idempotent = await repository.store_document(
            document_a,
            [_chunk("This input is ignored for an identical content hash.", 0, 2)],
        )
        assert stored_a.created is True
        assert stored_b.created is True
        assert idempotent == replace(stored_a, created=False)

        with pytest.raises(ImmutableDocumentConflict):
            await repository.store_document(
                replace(document_a, content_hash=_sha("changed-content")),
                [_chunk("Changed content.", 0, 0)],
            )

        matches = await repository.search(brochure_a, _vector(0), limit=5)
        assert [match.content for match in matches] == [
            "Waiting period is thirty days.",
            "Dental treatment is excluded.",
        ]
        assert all(match.brochure_id == brochure_a for match in matches)
        assert matches[0].score == pytest.approx(1.0)

        assert await repository.delete_by_brochure_id(brochure_a) is True
        assert await repository.delete_by_brochure_id(brochure_a) is False
        assert await repository.search(brochure_a, _vector(0), limit=5) == []
        assert len(await repository.search(brochure_b, _vector(0), limit=5)) == 1
    finally:
        await engine.dispose()


async def test_synthetic_pdf_ingestion_preserves_hierarchical_metadata(
    migrated_database: str,
) -> None:
    root = Path(__file__).resolve().parents[3]
    blob_path = "output/pdf/speedclaim-arogya-shield-plus-synthetic-brochure-v1.pdf"
    data = (root / blob_path).read_bytes()
    engine = create_database_engine(migrated_database)
    session_factory = create_session_factory(engine)
    repository = PgVectorRepository(session_factory)
    brochure_id = uuid4()
    command_model = IngestionCommand(
        request_id=uuid4(),
        brochure_id=brochure_id,
        product_id=uuid4(),
        brochure_version="1.0",
        blob_path=blob_path,
        content_hash=hashlib.sha256(data).hexdigest(),
    )
    service = BrochureIngestionService(
        brochure_reader=LocalBrochureReader(root),
        parser=PdfParser(
            max_size_bytes=10_485_760,
            max_pages=300,
            minimum_text_characters=100,
        ),
        chunker=HierarchicalChunker(
            parent_max_characters=6_000,
            child_max_characters=1_200,
            child_overlap_characters=150,
        ),
        embedding_provider=DeterministicEmbeddingProvider(),
        repository=repository,
        max_pdf_size_bytes=10_485_760,
    )

    try:
        with pytest.raises(IngestionFailure) as failed_attempt:
            await service.ingest(replace(command_model, content_hash="a" * 64))
        result = await service.ingest(command_model)
        no_op = await service.ingest(replace(command_model, request_id=uuid4()))
        record = await repository.get_document_by_brochure_id(brochure_id)

        assert result.status == "Succeeded"
        assert failed_attempt.value.code == "brochure_hash_mismatch"
        assert no_op.status == "NoOp"
        assert record is not None
        assert record.brochure_version == "1.0"
        assert record.page_count == 13
        assert record.parent_chunk_count == 13
        assert record.child_chunk_count == 95

        async with session_factory() as session:
            waiting_period = await session.scalar(
                select(RagChunk)
                .join(RagDocument, RagDocument.id == RagChunk.document_id)
                .where(
                    RagDocument.brochure_id == brochure_id,
                    RagChunk.clause_reference == "5.1",
                )
            )
            runs = list(
                await session.scalars(
                    select(RagIngestionRun)
                    .where(RagIngestionRun.brochure_id == brochure_id)
                    .order_by(RagIngestionRun.started_at)
                )
            )

        assert waiting_period is not None
        assert waiting_period.page_number == 6
        assert waiting_period.section_title == "Waiting periods"
        assert waiting_period.parent_chunk_id is not None
        assert [run.status for run in runs] == ["Failed", "Succeeded", "Succeeded"]
        assert runs[0].error_code == "brochure_hash_mismatch"
        assert runs[0].error_message_redacted == (
            "The brochure content hash does not match the stored file."
        )
    finally:
        await repository.delete_by_brochure_id(brochure_id)
        await engine.dispose()
