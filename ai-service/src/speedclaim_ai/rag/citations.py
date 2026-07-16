import re
from dataclasses import dataclass
from typing import Literal

from pydantic import BaseModel, ConfigDict, Field, ValidationError, field_validator, model_validator

from speedclaim_ai.rag.models import AnswerCitation
from speedclaim_ai.rag.retrieval_service import RetrievedEvidence

_MODEL_CITATION_MARKER = re.compile(r"\[(?:C)?\d+\]")
_WHITESPACE = re.compile(r"\s+")


class InvalidGroundedAnswer(ValueError):
    pass


class GeneratedSupport(BaseModel):
    model_config = ConfigDict(extra="forbid")

    citation_id: str = Field(alias="citationId", pattern=r"^C[1-9][0-9]*$")
    quote: str = Field(min_length=8, max_length=300)


class GeneratedClaim(BaseModel):
    model_config = ConfigDict(extra="forbid")

    text: str = Field(min_length=1, max_length=800)
    supports: list[GeneratedSupport] = Field(min_length=1, max_length=4)

    @field_validator("text")
    @classmethod
    def reject_model_owned_markers(cls, value: str) -> str:
        normalized = value.strip()
        if _MODEL_CITATION_MARKER.search(normalized):
            raise ValueError("claim text must not contain citation markers")
        return normalized


class GeneratedAnswer(BaseModel):
    model_config = ConfigDict(extra="forbid")

    answer_type: Literal[
        "Grounded", "InsufficientEvidence", "UnsupportedRequest"
    ] = Field(alias="answerType")
    claims: list[GeneratedClaim] = Field(max_length=6)

    @model_validator(mode="after")
    def validate_answer_shape(self):
        if self.answer_type != "Grounded" and self.claims:
            raise ValueError("non-grounded answers must not contain claims")
        if self.answer_type == "Grounded" and not self.claims:
            raise ValueError("grounded answers must contain claims")
        return self


@dataclass(frozen=True, slots=True)
class ValidatedGeneratedAnswer:
    answer_type: Literal["Grounded", "InsufficientEvidence", "UnsupportedRequest"]
    answer: str
    citations: tuple[AnswerCitation, ...]


def validate_generated_answer(
    content: str,
    evidence: tuple[RetrievedEvidence, ...],
) -> ValidatedGeneratedAnswer:
    generated = parse_generated_answer_contract(content)

    if generated.answer_type == "UnsupportedRequest":
        return ValidatedGeneratedAnswer(
            answer_type="UnsupportedRequest",
            answer=(
                "I can explain brochure wording, but I can’t approve claims, make policy "
                "changes, take account actions, or provide legal advice."
            ),
            citations=(),
        )
    if generated.answer_type == "InsufficientEvidence":
        return ValidatedGeneratedAnswer(
            answer_type="InsufficientEvidence",
            answer=(
                "I couldn’t find enough evidence in this brochure to answer that question. "
                "Please check the policy document or contact SpeedClaim support."
            ),
            citations=(),
        )

    evidence_by_id = {item.citation_id: item for item in evidence}
    used_ids: list[str] = []
    rendered_claims: list[str] = []
    for claim in generated.claims:
        claim_ids: list[str] = []
        for support in claim.supports:
            source = evidence_by_id.get(support.citation_id)
            if source is None:
                raise InvalidGroundedAnswer("model cited evidence that was not retrieved")
            if not _contains_exact_quote(source.content, support.quote):
                raise InvalidGroundedAnswer("model support quote is absent from cited evidence")
            if support.citation_id not in claim_ids:
                claim_ids.append(support.citation_id)
            if support.citation_id not in used_ids:
                used_ids.append(support.citation_id)

        markers = "".join(f" [{_citation_index(item_id)}]" for item_id in claim_ids)
        rendered_claims.append(f"{claim.text}{markers}")

    citations = tuple(
        _to_answer_citation(evidence_by_id[item_id]) for item_id in used_ids
    )
    return ValidatedGeneratedAnswer(
        answer_type="Grounded",
        answer="\n\n".join(rendered_claims),
        citations=citations,
    )


def parse_generated_answer_contract(content: str) -> GeneratedAnswer:
    """Validate only the provider-neutral answer shape, not its evidence claims."""
    try:
        return GeneratedAnswer.model_validate_json(content)
    except ValidationError as exc:
        raise InvalidGroundedAnswer("model output does not match the answer schema") from exc


def _contains_exact_quote(content: str, quote: str) -> bool:
    normalized_content = _WHITESPACE.sub(" ", content).strip().casefold()
    normalized_quote = _WHITESPACE.sub(" ", quote).strip().casefold()
    return len(normalized_quote) >= 8 and normalized_quote in normalized_content


def _citation_index(citation_id: str) -> int:
    return int(citation_id[1:])


def _to_answer_citation(evidence: RetrievedEvidence) -> AnswerCitation:
    excerpt = _WHITESPACE.sub(" ", evidence.matched_content).strip()
    if len(excerpt) > 480:
        excerpt = f"{excerpt[:477].rstrip()}..."
    return AnswerCitation(
        index=_citation_index(evidence.citation_id),
        page_number=evidence.page_number,
        section_title=evidence.section_title,
        clause_reference=evidence.clause_reference,
        excerpt=excerpt,
    )
