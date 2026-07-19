from uuid import uuid4

import pytest

from speedclaim_ai.contracts.speedy import SpeedyAccountSnapshot, SpeedyCatalogSnapshot, SpeedyKycSnapshot
from speedclaim_ai.contracts.workspace import WorkspaceRequest
from speedclaim_ai.providers.chat.base import ChatCompletion
from speedclaim_ai.workspace import WorkspaceService, _action_for


class FakeRouterProvider:
    provider_name = "FakeRouter"
    model_name = "fake-haiku"

    async def complete(self, request):
        assert "classify" in request.system_prompt
        return ChatCompletion(
            content='{"intent":"kyc","risk":"regulated"}',
            provider=self.provider_name,
            model=self.model_name,
        )

    async def close(self):
        return None


class FakeAnswerProvider:
    provider_name = "FakeAnswer"
    model_name = "fake-sonnet"

    async def complete(self, request):
        assert "AADHAAR" not in request.user_prompt
        assert "ACCOUNT_DATA" in request.user_prompt
        return ChatCompletion(
            content='{"answer":"Attach Aadhaar and PAN in the labelled slots, then review your submission."}',
            provider=self.provider_name,
            model=self.model_name,
        )

    async def close(self):
        return None


pytestmark = pytest.mark.anyio


def anyio_backend() -> str:
    return "asyncio"


async def test_workspace_runs_a_langgraph_workflow_and_returns_a_guided_kyc_action():
    request = WorkspaceRequest(
        requestId=str(uuid4()),
        question="I need to complete my KYC",
        account=SpeedyAccountSnapshot(
            firstName="Asha",
            isAuthenticated=True,
            policies=[],
            upcomingPremiums=[],
            claims=[],
        ),
        catalog=SpeedyCatalogSnapshot(products=[]),
    )

    response = await WorkspaceService(FakeAnswerProvider(), FakeRouterProvider()).answer(request)

    assert response.intent == "kyc"
    assert response.risk == "regulated"
    assert response.actions[0].kind == "guided_kyc"
    assert response.actions[0].requires_confirmation is True
    assert response.model == "fake-sonnet"


async def test_workspace_does_not_offer_signed_in_actions_to_a_guest():
    request = WorkspaceRequest(
        requestId=str(uuid4()),
        question="I need to complete my KYC",
        account=SpeedyAccountSnapshot(
            firstName="Guest",
            isAuthenticated=False,
            policies=[],
            upcomingPremiums=[],
            claims=[],
        ),
        catalog=SpeedyCatalogSnapshot(products=[]),
    )

    response = await WorkspaceService(FakeAnswerProvider(), FakeRouterProvider()).answer(request)

    assert response.actions == []


async def test_workspace_does_not_offer_resubmission_when_kyc_is_under_review():
    request = WorkspaceRequest(
        requestId=str(uuid4()),
        question="What is my KYC status?",
        account=SpeedyAccountSnapshot(
            firstName="Asha",
            isAuthenticated=True,
            policies=[],
            upcomingPremiums=[],
            claims=[],
            kyc=SpeedyKycSnapshot(status="Pending", aadhaarUploaded=True, panUploaded=True),
        ),
        catalog=SpeedyCatalogSnapshot(products=[]),
    )

    response = await WorkspaceService(FakeAnswerProvider(), FakeRouterProvider()).answer(request)

    assert response.actions == []
    assert "awaiting underwriter review" in response.answer
    assert "do not need to submit them again" in response.answer


def test_workspace_routes_customer_tasks_to_typed_in_workspace_actions():
    account = SpeedyAccountSnapshot(
        firstName="Asha",
        isAuthenticated=True,
        policies=[],
        upcomingPremiums=[],
        claims=[],
    )

    assert _action_for("product_discovery", account).kind == "guided_quote"
    assert _action_for("claim_guidance", account).kind == "guided_claim"
    assert _action_for("claim_status", account).kind == "claim_status"
    assert _action_for("proposal_status", account).kind == "policy_status"
    assert _action_for("grievance_status", account).kind == "grievance_status"
    assert _action_for("policy_help", account).kind == "policy_status"
