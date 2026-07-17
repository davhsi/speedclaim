from datetime import date
from uuid import uuid4

import pytest

from speedclaim_ai.contracts.speedy import SpeedyAccountSnapshot, SpeedyRequest
from speedclaim_ai.providers.chat.base import ChatCompletion
from speedclaim_ai.speedy import SpeedyService


class FakeChatProvider:
    provider_name = "Fake"
    model_name = "fake-model"

    async def complete(self, request):
        assert "ACCOUNT_DATA" in request.user_prompt
        assert "What policies" in request.user_prompt
        return ChatCompletion(
            content='{"answer":"You have one active policy."}',
            provider="Fake",
            model="fake-model",
        )

    async def close(self):
        return None


pytestmark = pytest.mark.anyio


def anyio_backend() -> str:
    return "asyncio"


async def test_speedy_uses_only_the_server_supplied_account_snapshot():
    request = SpeedyRequest(
        requestId=uuid4(),
        question="What policies do I have?",
        account=SpeedyAccountSnapshot(firstName="Asha", policies=[], upcomingPremiums=[], claims=[]),
    )

    response = await SpeedyService(FakeChatProvider()).answer(request)

    assert response.request_id == request.request_id
    assert response.answer == "You have one active policy."
    assert response.provider == "Fake"
