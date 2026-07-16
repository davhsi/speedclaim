from uuid import uuid4

from fastapi.testclient import TestClient
from sqlalchemy.exc import OperationalError

from conftest import TEST_API_KEY
from speedclaim_ai.config.settings import POLICY_QA_PROMPT_VERSION
from speedclaim_ai.rag.errors import PolicyQaFailure
from speedclaim_ai.rag.models import AnswerCitation, PolicyQaResult


class FakePolicyQaService:
    def __init__(self, outcome) -> None:
        self.outcome = outcome
        self.commands = []

    async def answer(self, command):
        self.commands.append(command)
        if isinstance(self.outcome, Exception):
            raise self.outcome
        return self.outcome


def _payload() -> dict:
    return {
        "requestId": str(uuid4()),
        "brochureId": str(uuid4()),
        "productId": str(uuid4()),
        "brochureVersion": "1.0",
        "question": "What is the initial waiting period?",
    }


def test_policy_qa_endpoint_maps_grounded_contract(client: TestClient) -> None:
    result = PolicyQaResult(
        request_id=uuid4(),
        answer="The initial waiting period is thirty days. [1]",
        evidence_status="Grounded",
        brochure_version="1.0",
        citations=(
            AnswerCitation(
                index=1,
                page_number=6,
                section_title="Waiting periods",
                clause_reference="5.1",
                excerpt="A thirty-day initial waiting period applies.",
            ),
        ),
        prompt_version=POLICY_QA_PROMPT_VERSION,
        provider="AnthropicGateway",
        model="claude-sonnet-4-6",
    )
    service = FakePolicyQaService(result)
    client.app.state.policy_qa_service_override = service

    response = client.post(
        "/internal/v1/policy-qa",
        headers={"X-Internal-Api-Key": TEST_API_KEY},
        json=_payload(),
    )

    assert response.status_code == 200
    assert response.json() == {
        "requestId": str(result.request_id),
        "answer": result.answer,
        "evidenceStatus": "Grounded",
        "brochureVersion": "1.0",
        "citations": [
            {
                "index": 1,
                "pageNumber": 6,
                "sectionTitle": "Waiting periods",
                "clauseReference": "5.1",
                "excerpt": "A thirty-day initial waiting period applies.",
            }
        ],
        "promptVersion": POLICY_QA_PROMPT_VERSION,
        "provider": "AnthropicGateway",
        "model": "claude-sonnet-4-6",
    }
    assert service.commands[0].question == "What is the initial waiting period?"
    assert service.commands[0].brochure_version == "1.0"


def test_policy_qa_endpoint_requires_internal_auth(client: TestClient) -> None:
    response = client.post("/internal/v1/policy-qa", json=_payload())

    assert response.status_code == 401
    assert response.json()["error"]["code"] == "unauthorized"


def test_policy_qa_rate_limit_preserves_retry_after(client: TestClient) -> None:
    client.app.state.policy_qa_service_override = FakePolicyQaService(
        PolicyQaFailure(
            code="chat_provider_rate_limited",
            message="The answer provider is rate limited. Please retry later.",
            status_code=429,
            retry_after_seconds=9,
        )
    )

    response = client.post(
        "/internal/v1/policy-qa",
        headers={"X-Internal-Api-Key": TEST_API_KEY},
        json=_payload(),
    )

    assert response.status_code == 429
    assert response.headers["Retry-After"] == "9"
    assert response.json()["error"]["code"] == "chat_provider_rate_limited"


def test_policy_qa_without_external_configuration_fails_only_on_endpoint(
    client: TestClient,
) -> None:
    response = client.post(
        "/internal/v1/policy-qa",
        headers={"X-Internal-Api-Key": TEST_API_KEY},
        json=_payload(),
    )

    assert response.status_code == 503
    assert response.json()["error"]["code"] == "vector_database_not_configured"


def test_policy_qa_contract_rejects_invalid_request(client: TestClient) -> None:
    payload = _payload()
    payload["brochureVersion"] = " padded "

    response = client.post(
        "/internal/v1/policy-qa",
        headers={"X-Internal-Api-Key": TEST_API_KEY},
        json=payload,
    )

    assert response.status_code == 422
    assert response.json()["error"]["code"] == "invalid_request"


def test_policy_qa_database_failure_is_sanitized_as_feature_unavailable(
    client: TestClient,
) -> None:
    client.app.state.policy_qa_service_override = FakePolicyQaService(
        OperationalError("select secret", {}, Exception("database details"))
    )

    response = client.post(
        "/internal/v1/policy-qa",
        headers={"X-Internal-Api-Key": TEST_API_KEY},
        json=_payload(),
    )

    assert response.status_code == 503
    assert response.json()["error"]["code"] == "vector_database_unavailable"
    assert "secret" not in response.text
    assert "database details" not in response.text
