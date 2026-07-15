import hashlib
import os
from dataclasses import replace
from uuid import uuid4

import pytest
from alembic import command
from alembic.config import Config

from speedclaim_ai.config.settings import DEFAULT_EMBEDDING_MODEL, EMBEDDING_DIMENSION
from speedclaim_ai.database.session import create_database_engine, create_session_factory
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
