import re
from typing import Literal
from uuid import UUID

from pydantic import BaseModel, ConfigDict, Field, field_validator

_SHA256 = re.compile(r"^[0-9a-f]{64}$")


class BrochureIngestionRequest(BaseModel):
    model_config = ConfigDict(populate_by_name=True)

    request_id: UUID = Field(alias="requestId")
    brochure_id: UUID = Field(alias="brochureId")
    product_id: UUID = Field(alias="productId")
    brochure_version: str = Field(alias="version", min_length=1, max_length=128)
    blob_path: str = Field(alias="blobPath", min_length=1, max_length=1_024)
    content_hash: str = Field(alias="contentHash")

    @field_validator("brochure_version", "blob_path")
    @classmethod
    def validate_bounded_text(cls, value: str) -> str:
        if value != value.strip():
            raise ValueError("value must not contain surrounding whitespace")
        return value

    @field_validator("content_hash")
    @classmethod
    def validate_content_hash(cls, value: str) -> str:
        if not _SHA256.fullmatch(value):
            raise ValueError("content hash must be a lowercase SHA-256 value")
        return value


class BrochureIngestionResponse(BaseModel):
    model_config = ConfigDict(populate_by_name=True)

    request_id: UUID = Field(alias="requestId")
    brochure_id: UUID = Field(alias="brochureId")
    document_id: UUID = Field(alias="documentId")
    status: Literal["Succeeded", "NoOp"]
    page_count: int = Field(alias="pageCount")
    parent_chunk_count: int = Field(alias="parentChunkCount")
    child_chunk_count: int = Field(alias="childChunkCount")
    embedding_provider: str = Field(alias="embeddingProvider")
    embedding_model: str = Field(alias="embeddingModel")
    embedding_dimension: int = Field(alias="embeddingDimension")
