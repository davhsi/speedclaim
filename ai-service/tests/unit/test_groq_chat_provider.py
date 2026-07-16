import json

import httpx
import pytest

from speedclaim_ai.config.settings import DEFAULT_CHAT_MODEL
from speedclaim_ai.providers.chat.base import (
    ChatProviderRateLimited,
    ChatProviderResponseError,
    ChatProviderTimeout,
    ChatProviderUnavailable,
    ChatRequest,
)
from speedclaim_ai.providers.chat.groq import GroqChatProvider

pytestmark = pytest.mark.anyio


@pytest.fixture
def anyio_backend() -> str:
    return "asyncio"


def _request() -> ChatRequest:
    return ChatRequest(
        system_prompt="System safety rules",
        user_prompt='{"QUESTION_DATA":"waiting period"}',
        response_schema_name="speedclaim_policy_qa",
        response_schema={
            "type": "object",
            "properties": {"answerType": {"type": "string"}},
            "required": ["answerType"],
            "additionalProperties": False,
        },
    )


def _success(content: str = '{"answerType":"Grounded"}') -> httpx.Response:
    return httpx.Response(
        200,
        json={
            "choices": [{"message": {"content": content}}],
            "usage": {"prompt_tokens": 20, "completion_tokens": 8},
        },
    )


async def _no_sleep(_seconds: float) -> None:
    pass


async def test_groq_adapter_uses_strict_schema_without_tools() -> None:
    captured = {}

    def handler(request: httpx.Request) -> httpx.Response:
        captured["url"] = str(request.url)
        captured["authorization"] = request.headers["Authorization"]
        captured["payload"] = json.loads(request.content)
        return _success()

    async with httpx.AsyncClient(transport=httpx.MockTransport(handler)) as client:
        provider = GroqChatProvider(
            api_key="test-groq-key",
            model=DEFAULT_CHAT_MODEL,
            client=client,
            retry_delay_seconds=0,
        )
        completion = await provider.complete(_request())

    assert captured["url"] == "https://api.groq.com/openai/v1/chat/completions"
    assert captured["authorization"] == "Bearer test-groq-key"
    assert captured["payload"]["model"] == DEFAULT_CHAT_MODEL
    assert captured["payload"]["temperature"] == 0
    assert captured["payload"]["response_format"]["type"] == "json_schema"
    assert captured["payload"]["response_format"]["json_schema"]["strict"] is True
    assert "tools" not in captured["payload"]
    assert completion.provider == "Groq"
    assert completion.input_tokens == 20
    assert completion.output_tokens == 8


async def test_groq_adapter_retries_server_error_then_succeeds() -> None:
    calls = 0

    def handler(_request: httpx.Request) -> httpx.Response:
        nonlocal calls
        calls += 1
        return httpx.Response(503) if calls == 1 else _success()

    async with httpx.AsyncClient(transport=httpx.MockTransport(handler)) as client:
        provider = GroqChatProvider(
            api_key="test-groq-key",
            model=DEFAULT_CHAT_MODEL,
            max_attempts=2,
            client=client,
            retry_delay_seconds=0,
            sleep=_no_sleep,
        )
        await provider.complete(_request())

    assert calls == 2


async def test_groq_adapter_retries_timeout_then_fails_safely() -> None:
    calls = 0

    def handler(request: httpx.Request) -> httpx.Response:
        nonlocal calls
        calls += 1
        raise httpx.ReadTimeout("secret upstream details", request=request)

    async with httpx.AsyncClient(transport=httpx.MockTransport(handler)) as client:
        provider = GroqChatProvider(
            api_key="test-groq-key",
            model=DEFAULT_CHAT_MODEL,
            max_attempts=2,
            client=client,
            retry_delay_seconds=0,
            sleep=_no_sleep,
        )
        with pytest.raises(ChatProviderTimeout, match="timed out"):
            await provider.complete(_request())

    assert calls == 2


async def test_groq_adapter_maps_rate_limit_and_retry_after_without_retrying() -> None:
    calls = 0

    def handler(_request: httpx.Request) -> httpx.Response:
        nonlocal calls
        calls += 1
        return httpx.Response(429, headers={"retry-after": "12.8"})

    async with httpx.AsyncClient(transport=httpx.MockTransport(handler)) as client:
        provider = GroqChatProvider(
            api_key="test-groq-key",
            model=DEFAULT_CHAT_MODEL,
            client=client,
        )
        with pytest.raises(ChatProviderRateLimited) as failure:
            await provider.complete(_request())

    assert failure.value.retry_after_seconds == 12
    assert calls == 1


async def test_groq_adapter_rejects_malformed_success_response() -> None:
    async with httpx.AsyncClient(
        transport=httpx.MockTransport(lambda _request: httpx.Response(200, json={}))
    ) as client:
        provider = GroqChatProvider(
            api_key="test-groq-key",
            model=DEFAULT_CHAT_MODEL,
            client=client,
        )
        with pytest.raises(ChatProviderResponseError):
            await provider.complete(_request())


async def test_groq_adapter_maps_exhausted_server_errors_to_unavailable() -> None:
    async with httpx.AsyncClient(
        transport=httpx.MockTransport(lambda _request: httpx.Response(500))
    ) as client:
        provider = GroqChatProvider(
            api_key="test-groq-key",
            model=DEFAULT_CHAT_MODEL,
            max_attempts=1,
            client=client,
        )
        with pytest.raises(ChatProviderUnavailable):
            await provider.complete(_request())
