from typing import Literal
from uuid import UUID

from pydantic import BaseModel, ConfigDict, Field, field_validator


class PolicyQaRequest(BaseModel):
    model_config = ConfigDict(populate_by_name=True)

    request_id: UUID = Field(alias="requestId")
    brochure_id: UUID = Field(alias="brochureId")
    product_id: UUID = Field(alias="productId")
    brochure_version: str = Field(alias="brochureVersion", min_length=1, max_length=128)
    question: str = Field(min_length=1, max_length=4_000)

    @field_validator("brochure_version")
    @classmethod
    def validate_brochure_version(cls, value: str) -> str:
        if value != value.strip():
            raise ValueError("brochure version must not contain surrounding whitespace")
        return value


class PolicyQaCitation(BaseModel):
    model_config = ConfigDict(populate_by_name=True)

    index: int = Field(ge=1)
    page_number: int = Field(alias="pageNumber", ge=1)
    section_title: str | None = Field(alias="sectionTitle")
    clause_reference: str | None = Field(alias="clauseReference")
    excerpt: str = Field(min_length=1, max_length=480)


class PolicyQaResponse(BaseModel):
    model_config = ConfigDict(populate_by_name=True)

    request_id: UUID = Field(alias="requestId")
    answer: str
    evidence_status: Literal["Grounded", "InsufficientEvidence", "Rejected"] = Field(
        alias="evidenceStatus"
    )
    brochure_version: str = Field(alias="brochureVersion")
    citations: list[PolicyQaCitation]
    prompt_version: str = Field(alias="promptVersion")
    provider: str | None
    chat_model: str | None = Field(alias="model")
