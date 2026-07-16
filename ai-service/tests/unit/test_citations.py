import json
from uuid import uuid4

import pytest

from speedclaim_ai.rag.citations import InvalidGroundedAnswer, validate_generated_answer
from speedclaim_ai.rag.retrieval_service import RetrievedEvidence


def _evidence(citation_id: str = "C1") -> RetrievedEvidence:
    content = (
        "5.1 Initial waiting period\n"
        "A thirty-day initial waiting period applies from the policy start date."
    )
    return RetrievedEvidence(
        citation_id=citation_id,
        child_chunk_id=uuid4(),
        parent_chunk_id=uuid4(),
        page_number=6,
        section_title="Waiting periods",
        clause_reference="5.1",
        content=content,
        matched_content=content,
        score=0.91,
    )


def test_validated_answer_uses_application_owned_markers_and_metadata() -> None:
    evidence = (_evidence(),)
    content = json.dumps(
        {
            "answerType": "Grounded",
            "claims": [
                {
                    "text": "The brochure applies an initial waiting period of thirty days.",
                    "supports": [
                        {
                            "citationId": "C1",
                            "quote": "A thirty-day initial waiting period applies",
                        }
                    ],
                }
            ],
        }
    )

    answer = validate_generated_answer(content, evidence)

    assert answer.answer == (
        "The brochure applies an initial waiting period of thirty days. [1]"
    )
    assert answer.citations[0].index == 1
    assert answer.citations[0].page_number == 6
    assert answer.citations[0].section_title == "Waiting periods"
    assert answer.citations[0].clause_reference == "5.1"
    assert "thirty-day initial waiting period" in answer.citations[0].excerpt


@pytest.mark.parametrize(
    "payload",
    [
        {
            "answerType": "Grounded",
            "claims": [
                {
                    "text": "Unsupported citation.",
                    "supports": [{"citationId": "C99", "quote": "waiting period"}],
                }
            ],
        },
        {
            "answerType": "Grounded",
            "claims": [
                {
                    "text": "Invented support.",
                    "supports": [
                        {"citationId": "C1", "quote": "This quote is not in the brochure"}
                    ],
                }
            ],
        },
        {
            "answerType": "Grounded",
            "claims": [
                {
                    "text": "Model-owned marker [1]",
                    "supports": [
                        {"citationId": "C1", "quote": "thirty-day initial waiting period"}
                    ],
                }
            ],
        },
        {"answerType": "Grounded", "claims": []},
    ],
)
def test_unknown_unsupported_or_malformed_claims_are_rejected(payload: dict) -> None:
    with pytest.raises(InvalidGroundedAnswer):
        validate_generated_answer(json.dumps(payload), (_evidence(),))


def test_unsupported_request_uses_fixed_application_refusal() -> None:
    answer = validate_generated_answer(
        '{"answerType":"UnsupportedRequest","claims":[]}',
        (_evidence(),),
    )

    assert answer.answer_type == "UnsupportedRequest"
    assert "can’t approve claims" in answer.answer
    assert answer.citations == ()


def test_model_can_report_insufficient_evidence_without_citations() -> None:
    answer = validate_generated_answer(
        '{"answerType":"InsufficientEvidence","claims":[]}',
        (_evidence(),),
    )

    assert answer.answer_type == "InsufficientEvidence"
    assert "couldn’t find enough evidence" in answer.answer
    assert answer.citations == ()
