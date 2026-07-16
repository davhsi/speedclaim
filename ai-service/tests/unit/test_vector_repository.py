from dataclasses import replace
from uuid import uuid4

import pytest

from speedclaim_ai.config.settings import DEFAULT_EMBEDDING_MODEL, EMBEDDING_DIMENSION
from speedclaim_ai.database.session import normalize_connection_string
from speedclaim_ai.repositories.vector_repository import (
    ChunkInput,
    DocumentInput,
    validate_document_batch,
    validate_embedding,
)

HASH = "a" * 64


def _document() -> DocumentInput:
    return DocumentInput(
        brochure_id=uuid4(),
        product_id=uuid4(),
        brochure_version="1",
        content_hash=HASH,
        page_count=1,
        embedding_provider="FastEmbed",
        embedding_model=DEFAULT_EMBEDDING_MODEL,
    )


def _chunk() -> ChunkInput:
    return ChunkInput(
        page_number=1,
        chunk_index=0,
        content="A waiting period applies.",
        content_hash=HASH,
        token_count=5,
        embedding=[1.0] * EMBEDDING_DIMENSION,
    )


def test_batch_validation_assigns_stable_ids_for_storage() -> None:
    identified = validate_document_batch(_document(), [_chunk()])

    assert len(identified) == 1
    assert identified[0][1].version == 4


@pytest.mark.parametrize(
    "chunk",
    [
        replace(_chunk(), content_hash="not-a-sha"),
        replace(_chunk(), page_number=0),
        replace(_chunk(), token_count=0),
        replace(_chunk(), embedding=None),
    ],
)
def test_batch_validation_rejects_invalid_chunks(chunk: ChunkInput) -> None:
    with pytest.raises(ValueError):
        validate_document_batch(_document(), [chunk])


def test_embedding_validation_rejects_wrong_dimension() -> None:
    with pytest.raises(ValueError, match="dimension"):
        validate_embedding([1.0])


def test_connection_string_normalization_selects_async_psycopg() -> None:
    assert normalize_connection_string("postgresql://localhost/db") == (
        "postgresql+psycopg://localhost/db"
    )
    assert normalize_connection_string("postgresql+psycopg://localhost/db") == (
        "postgresql+psycopg://localhost/db"
    )
