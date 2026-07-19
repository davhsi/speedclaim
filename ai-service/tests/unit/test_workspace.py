from uuid import uuid4

import pytest

from speedclaim_ai.contracts.speedy import SpeedyAccountSnapshot, SpeedyCatalogSnapshot
from speedclaim_ai.contracts.workspace import WorkspaceRequest
from speedclaim_ai.providers.chat.base import ChatCompletion
from speedclaim_ai.workspace import WorkspaceService


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
