import math
import re
from dataclasses import dataclass
from typing import Protocol, Sequence
from uuid import UUID, uuid4

from speedclaim_ai.config.settings import EMBEDDING_DIMENSION

_SHA256 = re.compile(r"^[0-9a-f]{64}$")


class ImmutableDocumentConflict(Exception):
    """A brochure ID was reused with different immutable content."""


@dataclass(frozen=True, slots=True)
class DocumentInput:
    brochure_id: UUID
    product_id: UUID
    brochure_version: str
    content_hash: str
    page_count: int
    embedding_provider: str
    embedding_model: str
    embedding_dimension: int = EMBEDDING_DIMENSION


@dataclass(frozen=True, slots=True)
class ChunkInput:
    page_number: int
    chunk_index: int
    content: str
    content_hash: str
    token_count: int
    embedding: Sequence[float] | None
    id: UUID | None = None
    parent_chunk_id: UUID | None = None
    section_title: str | None = None
    clause_reference: str | None = None


@dataclass(frozen=True, slots=True)
class StoreResult:
    document_id: UUID
    created: bool


@dataclass(frozen=True, slots=True)
class ChunkMatch:
    chunk_id: UUID
    document_id: UUID
    brochure_id: UUID
    parent_chunk_id: UUID | None
    page_number: int
    section_title: str | None
    clause_reference: str | None
    chunk_index: int
    content: str
    score: float


class VectorRepository(Protocol):
    async def store_document(
        self, document: DocumentInput, chunks: Sequence[ChunkInput]
    ) -> StoreResult: ...

    async def search(
        self, brochure_id: UUID, query_embedding: Sequence[float], *, limit: int
    ) -> list[ChunkMatch]: ...

    async def delete_by_brochure_id(self, brochure_id: UUID) -> bool: ...


def validate_document_batch(
    document: DocumentInput, chunks: Sequence[ChunkInput]
) -> list[tuple[ChunkInput, UUID]]:
    for name, value in (
        ("brochure version", document.brochure_version),
        ("embedding provider", document.embedding_provider),
        ("embedding model", document.embedding_model),
    ):
        if not value.strip():
            raise ValueError(f"{name} must not be blank")
    if not _SHA256.fullmatch(document.content_hash):
        raise ValueError("document content hash must be a lowercase SHA-256 value")
    if document.page_count <= 0:
        raise ValueError("document page count must be positive")
    if document.embedding_dimension != EMBEDDING_DIMENSION:
        raise ValueError(f"document embeddings must have dimension {EMBEDDING_DIMENSION}")
    if not chunks:
        raise ValueError("at least one chunk is required")

    identified = [(chunk, chunk.id or uuid4()) for chunk in chunks]
    ids = [chunk_id for _, chunk_id in identified]
    indexes = [chunk.chunk_index for chunk, _ in identified]
    if len(set(ids)) != len(ids):
        raise ValueError("chunk IDs must be unique")
    if len(set(indexes)) != len(indexes):
        raise ValueError("chunk indexes must be unique within a document")
    known_ids = set(ids)

    for chunk, chunk_id in identified:
        if chunk.page_number <= 0:
            raise ValueError("chunk page number must be positive")
        if chunk.chunk_index < 0:
            raise ValueError("chunk index must not be negative")
        if not chunk.content.strip():
            raise ValueError("chunk content must not be blank")
        if not _SHA256.fullmatch(chunk.content_hash):
            raise ValueError("chunk content hash must be a lowercase SHA-256 value")
        if chunk.token_count <= 0:
            raise ValueError("chunk token count must be positive")
        if chunk.parent_chunk_id == chunk_id:
            raise ValueError("a chunk cannot be its own parent")
        if chunk.parent_chunk_id is not None and chunk.parent_chunk_id not in known_ids:
            raise ValueError("parent chunk must belong to the same document batch")
        if chunk.embedding is not None:
            validate_embedding(chunk.embedding, dimension=document.embedding_dimension)

    if not any(chunk.embedding is not None for chunk, _ in identified):
        raise ValueError("at least one searchable chunk embedding is required")
    return identified


def validate_embedding(
    embedding: Sequence[float], *, dimension: int = EMBEDDING_DIMENSION
) -> list[float]:
    values = [float(value) for value in embedding]
    if len(values) != dimension:
        raise ValueError(f"embedding must have dimension {dimension}")
    if not all(math.isfinite(value) for value in values):
        raise ValueError("embedding values must be finite")
    if not any(value != 0 for value in values):
        raise ValueError("embedding must not be a zero vector")
    return values
