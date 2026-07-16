import json

import httpx
import pytest

from speedclaim_ai.config.settings import DEFAULT_ANTHROPIC_GATEWAY_MODEL
from speedclaim_ai.providers.chat.anthropic_gateway import (
    ANTHROPIC_VERSION,
    NATIVE_SCHEMA_MODE,
    VALIDATED_JSON_MODE,
    AnthropicGatewayChatProvider,
)
from speedclaim_ai.providers.chat.base import (
    ChatProviderError,
    ChatProviderRateLimited,
    ChatProviderRefusal,
    ChatProviderResponseError,
    ChatProviderTimeout,
    ChatProviderTruncated,
    ChatProviderUnavailable,
    ChatRequest,
)

pytestmark = pytest.mark.anyio


@pytest.fixture
def anyio_backend() -> str:
    return "asyncio"


def _validate_answer_type(content: str) -> object:
    parsed = json.loads(content)
    if not isinstance(parsed, dict) or parsed.get("answerType") != "Grounded":
        raise ValueError("invalid contract")
    return parsed


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
        response_validator=_validate_answer_type,
    )


def _success(
    content: str = '{"answerType":"Grounded"}',
    *,
    stop_reason: str = "end_turn",
) -> httpx.Response:
    return httpx.Response(
        200,
        json={
            "content": [{"type": "text", "text": content}],
            "stop_reason": stop_reason,
            "usage": {"input_tokens": 21, "output_tokens": 9},
        },
    )


async def _no_sleep(_seconds: float) -> None:
    pass


async def test_native_schema_mode_uses_messages_api_bearer_auth_and_output_config() -> None:
    captured = {}

    def handler(request: httpx.Request) -> httpx.Response:
        captured["url"] = str(request.url)
        captured["authorization"] = request.headers["Authorization"]
        captured["anthropic_version"] = request.headers["anthropic-version"]
        captured["payload"] = json.loads(request.content)
        return _success()

    async with httpx.AsyncClient(transport=httpx.MockTransport(handler)) as client:
        provider = AnthropicGatewayChatProvider(
            auth_token="test-corporate-token",
            model=DEFAULT_ANTHROPIC_GATEWAY_MODEL,
            base_url="https://gateway.example.test/anthropic",
            output_mode=NATIVE_SCHEMA_MODE,
            client=client,
        )
        completion = await provider.complete(_request())

    payload = captured["payload"]
    assert captured["url"] == "https://gateway.example.test/anthropic/v1/messages"
    assert captured["authorization"] == "Bearer test-corporate-token"
    assert captured["anthropic_version"] == ANTHROPIC_VERSION
    assert payload["model"] == DEFAULT_ANTHROPIC_GATEWAY_MODEL
    assert payload["temperature"] == 0
    assert payload["system"] == "System safety rules"
    assert payload["messages"] == [
        {"role": "user", "content": '{"QUESTION_DATA":"waiting period"}'}
    ]
    assert payload["output_config"]["format"] == {
        "type": "json_schema",
        "schema": _request().response_schema,
    }
    assert "tools" not in payload
    assert "response_format" not in payload
    assert completion.provider == "AnthropicGateway"
    assert completion.model == DEFAULT_ANTHROPIC_GATEWAY_MODEL
    assert completion.input_tokens == 21
    assert completion.output_tokens == 9


async def test_validated_json_mode_prompts_for_raw_json_without_native_schema() -> None:
    captured = {}

    def handler(request: httpx.Request) -> httpx.Response:
        captured["payload"] = json.loads(request.content)
        return _success()

    async with httpx.AsyncClient(transport=httpx.MockTransport(handler)) as client:
        provider = AnthropicGatewayChatProvider(
            auth_token="test-token",
            model=DEFAULT_ANTHROPIC_GATEWAY_MODEL,
            base_url="https://gateway.example.test",
            output_mode=VALIDATED_JSON_MODE,
            client=client,
        )
        await provider.complete(_request())

    payload = captured["payload"]
    assert "output_config" not in payload
    assert "exactly one raw JSON object" in payload["system"]
    assert "Do not use Markdown or code fences" in payload["system"]
    assert '"additionalProperties":false' in payload["system"]
    assert "tools" not in payload


async def test_gateway_retries_server_error_then_succeeds() -> None:
    calls = 0

    def handler(_request: httpx.Request) -> httpx.Response:
        nonlocal calls
        calls += 1
        return httpx.Response(529) if calls == 1 else _success()

    async with httpx.AsyncClient(transport=httpx.MockTransport(handler)) as client:
        provider = AnthropicGatewayChatProvider(
            auth_token="test-token",
            model=DEFAULT_ANTHROPIC_GATEWAY_MODEL,
            base_url="https://gateway.example.test",
            max_attempts=2,
            retry_delay_seconds=0,
            client=client,
            sleep=_no_sleep,
        )
        await provider.complete(_request())

    assert calls == 2


async def test_gateway_exhausted_timeout_is_sanitized() -> None:
    def handler(request: httpx.Request) -> httpx.Response:
        raise httpx.ReadTimeout("upstream secret detail", request=request)

    async with httpx.AsyncClient(transport=httpx.MockTransport(handler)) as client:
        provider = AnthropicGatewayChatProvider(
            auth_token="test-token",
            model=DEFAULT_ANTHROPIC_GATEWAY_MODEL,
            base_url="https://gateway.example.test",
            max_attempts=1,
            client=client,
        )
        with pytest.raises(ChatProviderTimeout, match="timed out") as failure:
            await provider.complete(_request())

    assert "secret" not in str(failure.value)


async def test_gateway_maps_rate_limit_and_retry_after_without_retry() -> None:
    calls = 0

    def handler(_request: httpx.Request) -> httpx.Response:
        nonlocal calls
        calls += 1
        return httpx.Response(429, headers={"retry-after": "7.9"})

    async with httpx.AsyncClient(transport=httpx.MockTransport(handler)) as client:
        provider = AnthropicGatewayChatProvider(
            auth_token="test-token",
            model=DEFAULT_ANTHROPIC_GATEWAY_MODEL,
            base_url="https://gateway.example.test",
            client=client,
        )
        with pytest.raises(ChatProviderRateLimited) as failure:
            await provider.complete(_request())

    assert failure.value.retry_after_seconds == 7
    assert calls == 1


@pytest.mark.parametrize(
    ("stop_reason", "error_type"),
    [
        ("refusal", ChatProviderRefusal),
        ("max_tokens", ChatProviderTruncated),
    ],
)
async def test_gateway_rejects_refusal_and_token_truncation(
    stop_reason: str,
    error_type: type[Exception],
) -> None:
    calls = 0

    def handler(_request: httpx.Request) -> httpx.Response:
        nonlocal calls
        calls += 1
        return _success("partial or refusal", stop_reason=stop_reason)

    async with httpx.AsyncClient(
        transport=httpx.MockTransport(handler)
    ) as client:
        provider = AnthropicGatewayChatProvider(
            auth_token="test-token",
            model=DEFAULT_ANTHROPIC_GATEWAY_MODEL,
            base_url="https://gateway.example.test",
            client=client,
        )
        with pytest.raises(error_type):
            await provider.complete(_request())

    assert calls == 1


@pytest.mark.parametrize(
    "payload",
    [
        {},
        {"stop_reason": "end_turn", "content": []},
        {
            "stop_reason": "end_turn",
            "content": [{"type": "text", "text": "{}"}, {"type": "text", "text": "{}"}],
        },
        {
            "stop_reason": "tool_use",
            "content": [{"type": "text", "text": "{}"}],
        },
    ],
)
async def test_gateway_rejects_malformed_or_unexpected_content(payload: dict) -> None:
    async with httpx.AsyncClient(
        transport=httpx.MockTransport(
            lambda _request: httpx.Response(200, json=payload)
        )
    ) as client:
        provider = AnthropicGatewayChatProvider(
            auth_token="test-token",
            model=DEFAULT_ANTHROPIC_GATEWAY_MODEL,
            base_url="https://gateway.example.test",
            client=client,
        )
        with pytest.raises(ChatProviderResponseError):
            await provider.complete(_request())


async def test_gateway_maps_4xx_without_exposing_response_body() -> None:
    async with httpx.AsyncClient(
        transport=httpx.MockTransport(
            lambda _request: httpx.Response(400, text="gateway internal detail")
        )
    ) as client:
        provider = AnthropicGatewayChatProvider(
            auth_token="test-token",
            model=DEFAULT_ANTHROPIC_GATEWAY_MODEL,
            base_url="https://gateway.example.test",
            client=client,
        )
        with pytest.raises(ChatProviderError) as failure:
            await provider.complete(_request())

    assert "internal detail" not in str(failure.value)


async def test_gateway_maps_exhausted_5xx_to_unavailable() -> None:
    async with httpx.AsyncClient(
        transport=httpx.MockTransport(lambda _request: httpx.Response(503))
    ) as client:
        provider = AnthropicGatewayChatProvider(
            auth_token="test-token",
            model=DEFAULT_ANTHROPIC_GATEWAY_MODEL,
            base_url="https://gateway.example.test",
            max_attempts=1,
            client=client,
        )
        with pytest.raises(ChatProviderUnavailable):
            await provider.complete(_request())


def test_gateway_requires_lowercase_model_identifier() -> None:
    with pytest.raises(ValueError, match="lowercase"):
        AnthropicGatewayChatProvider(
            auth_token="test-token",
            model="Claude-Haiku-4-5-20251001",
            base_url="https://gateway.example.test",
        )


def test_gateway_requires_https_base_url() -> None:
    with pytest.raises(ValueError, match="HTTPS"):
        AnthropicGatewayChatProvider(
            auth_token="test-token",
            model=DEFAULT_ANTHROPIC_GATEWAY_MODEL,
            base_url="http://gateway.example.test",
        )


async def test_gateway_removes_only_unsupported_grammar_bounds_without_mutating_contract() -> None:
    request = _request()
    request.response_schema["properties"]["answerType"].update(
        {"minLength": 1, "maxLength": 40}
    )
    captured_schema = None

    def handler(http_request: httpx.Request) -> httpx.Response:
        nonlocal captured_schema
        captured_schema = json.loads(http_request.content)["output_config"]["format"][
            "schema"
        ]
        return _success()

    async with httpx.AsyncClient(transport=httpx.MockTransport(handler)) as client:
        provider = AnthropicGatewayChatProvider(
            auth_token="test-token",
            model=DEFAULT_ANTHROPIC_GATEWAY_MODEL,
            base_url="https://gateway.example.test",
            output_mode=NATIVE_SCHEMA_MODE,
            client=client,
        )
        await provider.complete(request)

    assert "minLength" not in captured_schema["properties"]["answerType"]
    assert "maxLength" not in captured_schema["properties"]["answerType"]
    assert request.response_schema["properties"]["answerType"]["minLength"] == 1
    assert request.response_schema["properties"]["answerType"]["maxLength"] == 40


@pytest.mark.parametrize(
    "malformed",
    [
        '```json\n{"answerType":"Grounded"}\n```',
        'Here is the answer: {"answerType":"Grounded"}',
        '{"answerType":"Grounded"} trailing prose',
        '[{"answerType":"Grounded"}]',
        '{"wrong":"shape"}',
    ],
)
async def test_validated_json_retries_adversarial_format_once_then_fails_safely(
    malformed: str,
) -> None:
    calls = []

    def handler(request: httpx.Request) -> httpx.Response:
        calls.append(json.loads(request.content))
        return _success(malformed)

    request = _request()
    request = ChatRequest(
        system_prompt=request.system_prompt,
        user_prompt=request.user_prompt,
        response_schema_name=request.response_schema_name,
        response_schema=request.response_schema,
        response_validator=_validate_answer_type,
    )
    async with httpx.AsyncClient(transport=httpx.MockTransport(handler)) as client:
        provider = AnthropicGatewayChatProvider(
            auth_token="test-token",
            model=DEFAULT_ANTHROPIC_GATEWAY_MODEL,
            base_url="https://gateway.example.test",
            client=client,
        )
        with pytest.raises(ChatProviderResponseError, match="invalid JSON") as failure:
            await provider.complete(request)

    assert len(calls) == 2
    assert "FORMAT CORRECTION" not in calls[0]["system"]
    assert "FORMAT CORRECTION" in calls[1]["system"]
    assert calls[1]["messages"][-1] == {"role": "assistant", "content": "{"}
    assert malformed not in calls[1]["system"]
    assert malformed not in str(failure.value)
    assert provider.last_validation_failure in {
        "markdown_wrapper",
        "non_json_prefix",
        "invalid_json_syntax",
        "trailing_text",
        "top_level_shape",
        "answer_contract",
    }


async def test_validated_json_accepts_fresh_valid_object_on_format_retry() -> None:
    responses = iter(
        [
            _success('```json\n{"answerType":"Grounded"}\n```'),
            _success('"answerType":"Grounded"}'),
        ]
    )

    async with httpx.AsyncClient(
        transport=httpx.MockTransport(lambda _request: next(responses))
    ) as client:
        provider = AnthropicGatewayChatProvider(
            auth_token="test-token",
            model=DEFAULT_ANTHROPIC_GATEWAY_MODEL,
            base_url="https://gateway.example.test",
            client=client,
        )
        completion = await provider.complete(_request())

    assert completion.content == '{"answerType":"Grounded"}'
    assert provider.request_count == 2
    assert provider.input_tokens_used == 42
    assert provider.output_tokens_used == 18
    assert provider.last_validation_failure is None


async def test_validated_json_requires_application_contract_validator() -> None:
    request = _request()
    request = ChatRequest(
        system_prompt=request.system_prompt,
        user_prompt=request.user_prompt,
        response_schema_name=request.response_schema_name,
        response_schema=request.response_schema,
    )
    async with httpx.AsyncClient(
        transport=httpx.MockTransport(lambda _request: _success())
    ) as client:
        provider = AnthropicGatewayChatProvider(
            auth_token="test-token",
            model=DEFAULT_ANTHROPIC_GATEWAY_MODEL,
            base_url="https://gateway.example.test",
            client=client,
        )
        with pytest.raises(ChatProviderResponseError, match="not configured"):
            await provider.complete(request)

    assert provider.request_count == 0


def test_gateway_rejects_unknown_output_mode() -> None:
    with pytest.raises(ValueError, match="output mode"):
        AnthropicGatewayChatProvider(
            auth_token="test-token",
            model=DEFAULT_ANTHROPIC_GATEWAY_MODEL,
            base_url="https://gateway.example.test",
            output_mode="Automatic",
        )
