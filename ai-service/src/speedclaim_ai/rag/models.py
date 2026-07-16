from dataclasses import dataclass
from typing import Literal
from uuid import UUID


@dataclass(frozen=True, slots=True)
class ParsedPage:
    page_number: int
    lines: tuple[str, ...]

    @property
    def text(self) -> str:
        return "\n".join(self.lines)


@dataclass(frozen=True, slots=True)
class ParsedPdf:
    pages: tuple[ParsedPage, ...]
    removed_boilerplate: tuple[str, ...]

    @property
    def page_count(self) -> int:
        return len(self.pages)


@dataclass(frozen=True, slots=True)
class PreparedChunk:
    id: UUID
    parent_chunk_id: UUID | None
    page_number: int
    section_title: str | None
    clause_reference: str | None
    chunk_index: int
    content: str
    content_hash: str
    token_count: int
    is_parent: bool


@dataclass(frozen=True, slots=True)
class IngestionCommand:
    request_id: UUID
    brochure_id: UUID
    product_id: UUID
    brochure_version: str
    blob_path: str
    content_hash: str


@dataclass(frozen=True, slots=True)
class IngestionResult:
    request_id: UUID
    document_id: UUID
    status: Literal["Succeeded", "NoOp"]
    page_count: int
    parent_chunk_count: int
    child_chunk_count: int
    embedding_provider: str
    embedding_model: str
    embedding_dimension: int
