import asyncio
import json
import re
from collections.abc import Awaitable, Callable
from typing import Any
from urllib.parse import urlsplit

import httpx

from speedclaim_ai.providers.chat.base import (
    ChatCompletion,
    ChatProviderError,
    ChatProviderRateLimited,
    ChatProviderRefusal,
    ChatProviderResponseError,
    ChatProviderTimeout,
    ChatProviderTruncated,
    ChatProviderUnavailable,
    ChatRequest,
)

ANTHROPIC_VERSION = "2023-06-01"
NATIVE_SCHEMA_MODE = "NativeSchema"
VALIDATED_JSON_MODE = "ValidatedJson"
_OUTPUT_MODES = frozenset({NATIVE_SCHEMA_MODE, VALIDATED_JSON_MODE})
_CANONICAL_JSON_FENCE = re.compile(
    r"\A```json\n(?P<content>.*)\n```\Z",
    flags=re.DOTALL,
)
_UNSUPPORTED_SCHEMA_CONSTRAINTS = frozenset(
    {"minLength", "maxLength", "minItems", "maxItems"}
)
Sleep = Callable[[float], Awaitable[None]]


class AnthropicGatewayChatProvider:
    """Anthropic Messages adapter for an explicitly selected corporate gateway."""

    def __init__(
        self,
        *,
        auth_token: str,
        model: str,
        base_url: str,
        output_mode: str = VALIDATED_JSON_MODE,
        timeout_seconds: float = 15.0,
        max_attempts: int = 2,
        max_output_tokens: int = 700,
        retry_delay_seconds: float = 0.25,
        client: httpx.AsyncClient | None = None,
        sleep: Sleep = asyncio.sleep,
    ) -> None:
        if not auth_token or auth_token != auth_token.strip():
            raise ValueError("Anthropic gateway token must not be blank or padded")
        if not re.fullmatch(r"[a-z0-9][a-z0-9._:-]{0,254}", model):
            raise ValueError("Anthropic gateway model must be lowercase")
        normalized_base_url = base_url.strip().rstrip("/")
        parsed_base_url = urlsplit(normalized_base_url)
        if (
            parsed_base_url.scheme != "https"
            or not parsed_base_url.netloc
            or parsed_base_url.username is not None
            or parsed_base_url.password is not None
            or parsed_base_url.query
            or parsed_base_url.fragment
        ):
            raise ValueError("Anthropic gateway base URL must use HTTPS")
        if not 1 <= max_attempts <= 3:
            raise ValueError("Anthropic gateway attempts must be between 1 and 3")
        if retry_delay_seconds < 0:
            raise ValueError("Anthropic gateway retry delay must not be negative")
        if output_mode not in _OUTPUT_MODES:
            raise ValueError(
                "Anthropic output mode must be NativeSchema or ValidatedJson"
            )

        self._auth_token = auth_token
        self._model = model.strip()
        self._base_url = normalized_base_url
        self._output_mode = output_mode
        self._max_attempts = max_attempts
        self._max_output_tokens = max_output_tokens
        self._retry_delay_seconds = retry_delay_seconds
        self._sleep = sleep
        self._owns_client = client is None
        self._client = client or httpx.AsyncClient(
            timeout=httpx.Timeout(timeout_seconds),
            follow_redirects=False,
        )
        self._request_count = 0
        self._input_tokens_used = 0
        self._output_tokens_used = 0
        self._last_validation_failure: str | None = None

    @property
    def provider_name(self) -> str:
        return "AnthropicGateway"

    @property
    def model_name(self) -> str:
        return self._model

    @property
    def output_mode(self) -> str:
        return self._output_mode

    @property
    def request_count(self) -> int:
        return self._request_count

    @property
    def input_tokens_used(self) -> int:
        return self._input_tokens_used

    @property
    def output_tokens_used(self) -> int:
        return self._output_tokens_used

    @property
    def last_validation_failure(self) -> str | None:
        return self._last_validation_failure

    async def complete(self, request: ChatRequest) -> ChatCompletion:
        if self._output_mode == NATIVE_SCHEMA_MODE:
            return await self._send(request, system_prompt=request.system_prompt)

        if request.response_validator is None:
            raise ChatProviderResponseError(
                "chat provider response validation is not configured"
            )

        format_prompt = self._validated_json_system_prompt(request)
        for format_attempt in range(2):
            system_prompt = format_prompt
            if format_attempt == 1:
                system_prompt = (
                    f"{format_prompt}\n\n"
                    "FORMAT CORRECTION: Return a fresh response as exactly one raw JSON "
                    "object that matches the supplied schema. Do not include Markdown, code "
                    "fences, commentary, or any text before or after the object."
                )
            completion = await self._send(request, system_prompt=system_prompt)
            normalized_content, failure = self._validated_json_content(
                completion.content,
                request,
            )
            self._last_validation_failure = failure
            if normalized_content is not None:
                return ChatCompletion(
                    content=normalized_content,
                    provider=completion.provider,
                    model=completion.model,
                    input_tokens=completion.input_tokens,
                    output_tokens=completion.output_tokens,
                )

        raise ChatProviderResponseError("chat provider returned invalid JSON")

    async def _send(
        self,
        request: ChatRequest,
        *,
        system_prompt: str,
    ) -> ChatCompletion:
        payload = {
            "model": self._model,
            "max_tokens": self._max_output_tokens,
            "temperature": 0,
            "system": system_prompt,
            "messages": [{"role": "user", "content": request.user_prompt}],
        }
        if self._output_mode == NATIVE_SCHEMA_MODE:
            payload["output_config"] = {
                "format": {
                    "type": "json_schema",
                    "schema": self._compatible_schema(request.response_schema),
                }
            }
        headers = {
            "Authorization": f"Bearer {self._auth_token}",
            "anthropic-version": ANTHROPIC_VERSION,
            "Content-Type": "application/json",
        }

        for attempt in range(1, self._max_attempts + 1):
            try:
                self._request_count += 1
                response = await self._client.post(
                    f"{self._base_url}/v1/messages",
                    headers=headers,
                    json=payload,
                )
            except httpx.TimeoutException as exc:
                if attempt == self._max_attempts:
                    raise ChatProviderTimeout("chat provider timed out") from exc
                await self._backoff(attempt)
                continue
            except httpx.RequestError as exc:
                if attempt == self._max_attempts:
                    raise ChatProviderUnavailable("chat provider is unavailable") from exc
                await self._backoff(attempt)
                continue

            if response.status_code == 429:
                raise ChatProviderRateLimited(
                    self._parse_retry_after(response.headers.get("retry-after"))
                )
            if response.status_code >= 500:
                if attempt == self._max_attempts:
                    raise ChatProviderUnavailable("chat provider is unavailable")
                await self._backoff(attempt)
                continue
            if response.status_code >= 400:
                raise ChatProviderError("chat provider rejected the request")

            return self._parse_completion(response)

        raise ChatProviderUnavailable("chat provider is unavailable")

    @staticmethod
    def _validated_json_content(
        content: str,
        request: ChatRequest,
    ) -> tuple[str | None, str | None]:
        candidate = content
        if content.startswith("```"):
            fenced = _CANONICAL_JSON_FENCE.fullmatch(content)
            if fenced is None:
                return None, "invalid_markdown_envelope"
            candidate = fenced.group("content")

        try:
            parsed = json.loads(candidate)
        except (TypeError, ValueError):
            return None, AnthropicGatewayChatProvider._json_syntax_category(candidate)
        if not isinstance(parsed, dict):
            return None, "top_level_shape"
        try:
            assert request.response_validator is not None
            request.response_validator(candidate)
        except (TypeError, ValueError):
            return None, "answer_contract"
        return candidate, None

    @staticmethod
    def _json_syntax_category(content: str) -> str:
        stripped = content.strip()
        if stripped.startswith("```"):
            return "markdown_wrapper"
        if not stripped.startswith("{"):
            return "non_json_prefix"
        try:
            _, end = json.JSONDecoder().raw_decode(stripped)
        except (TypeError, ValueError):
            return "invalid_json_syntax"
        if stripped[end:].strip():
            return "trailing_text"
        return "invalid_json_syntax"

    @staticmethod
    def _validated_json_system_prompt(request: ChatRequest) -> str:
        schema = json.dumps(
            request.response_schema,
            ensure_ascii=False,
            separators=(",", ":"),
            sort_keys=True,
        )
        return (
            f"{request.system_prompt}\n\n"
            "OUTPUT FORMAT (mandatory): Return exactly one raw JSON object and nothing "
            "else. Do not use Markdown or code fences. Do not add prose before or after the "
            f"object. The complete response must validate against this JSON schema: {schema}"
        )

    async def close(self) -> None:
        if self._owns_client:
            await self._client.aclose()

    async def _backoff(self, attempt: int) -> None:
        await self._sleep(self._retry_delay_seconds * (2 ** (attempt - 1)))

    def _parse_completion(self, response: httpx.Response) -> ChatCompletion:
        try:
            payload: Any = response.json()
            usage = payload.get("usage") or {}
            input_tokens = self._safe_token_count(usage.get("input_tokens"))
            output_tokens = self._safe_token_count(usage.get("output_tokens"))
            if input_tokens is not None:
                self._input_tokens_used += input_tokens
            if output_tokens is not None:
                self._output_tokens_used += output_tokens

            stop_reason = payload["stop_reason"]
            if stop_reason == "refusal":
                raise ChatProviderRefusal("chat provider refused the request")
            if stop_reason == "max_tokens":
                raise ChatProviderTruncated("chat provider response was truncated")
            if stop_reason != "end_turn":
                raise ValueError("unexpected stop reason")

            blocks = payload["content"]
            if not isinstance(blocks, list) or len(blocks) != 1:
                raise ValueError("unexpected content blocks")
            block = blocks[0]
            if not isinstance(block, dict) or block.get("type") != "text":
                raise ValueError("missing text content")
            content = block.get("text")
            if not isinstance(content, str) or not content.strip():
                raise ValueError("missing completion content")

        except (AttributeError, KeyError, IndexError, TypeError, ValueError) as exc:
            raise ChatProviderResponseError(
                "chat provider returned an invalid response"
            ) from exc

        return ChatCompletion(
            content=content,
            provider=self.provider_name,
            model=self.model_name,
            input_tokens=input_tokens,
            output_tokens=output_tokens,
        )

    @staticmethod
    def _safe_token_count(value: Any) -> int | None:
        return value if isinstance(value, int) and value >= 0 else None

    @classmethod
    def _compatible_schema(cls, value: Any) -> Any:
        """Remove grammar-unsupported bounds; application validation retains them."""
        if isinstance(value, dict):
            return {
                key: cls._compatible_schema(item)
                for key, item in value.items()
                if key not in _UNSUPPORTED_SCHEMA_CONSTRAINTS
            }
        if isinstance(value, list):
            return [cls._compatible_schema(item) for item in value]
        return value

    @staticmethod
    def _parse_retry_after(value: str | None) -> int | None:
        if value is None:
            return None
        try:
            seconds = int(float(value))
        except ValueError:
            return None
        return max(0, min(seconds, 300))
