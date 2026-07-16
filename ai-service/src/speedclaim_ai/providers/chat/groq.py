import asyncio
from collections.abc import Awaitable, Callable
from typing import Any

import httpx

from speedclaim_ai.providers.chat.base import (
    ChatCompletion,
    ChatProviderError,
    ChatProviderRateLimited,
    ChatProviderResponseError,
    ChatProviderTimeout,
    ChatProviderUnavailable,
    ChatRequest,
)

Sleep = Callable[[float], Awaitable[None]]


class GroqChatProvider:
    """Minimal Groq chat-completions adapter with bounded transport retries."""

    def __init__(
        self,
        *,
        api_key: str,
        model: str,
        base_url: str = "https://api.groq.com/openai/v1",
        timeout_seconds: float = 15.0,
        max_attempts: int = 2,
        max_output_tokens: int = 700,
        retry_delay_seconds: float = 0.25,
        client: httpx.AsyncClient | None = None,
        sleep: Sleep = asyncio.sleep,
    ) -> None:
        if not api_key or api_key != api_key.strip():
            raise ValueError("Groq API key must not be blank or padded")
        if not model.strip():
            raise ValueError("Groq model must not be blank")
        if not 1 <= max_attempts <= 3:
            raise ValueError("Groq attempts must be between 1 and 3")
        if retry_delay_seconds < 0:
            raise ValueError("Groq retry delay must not be negative")

        self._api_key = api_key
        self._model = model.strip()
        self._base_url = base_url.rstrip("/")
        self._max_attempts = max_attempts
        self._max_output_tokens = max_output_tokens
        self._retry_delay_seconds = retry_delay_seconds
        self._sleep = sleep
        self._owns_client = client is None
        self._client = client or httpx.AsyncClient(
            timeout=httpx.Timeout(timeout_seconds),
            follow_redirects=False,
        )

    @property
    def provider_name(self) -> str:
        return "Groq"

    @property
    def model_name(self) -> str:
        return self._model

    async def complete(self, request: ChatRequest) -> ChatCompletion:
        payload = {
            "model": self._model,
            "messages": [
                {"role": "system", "content": request.system_prompt},
                {"role": "user", "content": request.user_prompt},
            ],
            "temperature": 0,
            "max_completion_tokens": self._max_output_tokens,
            "response_format": {
                "type": "json_schema",
                "json_schema": {
                    "name": request.response_schema_name,
                    "strict": True,
                    "schema": request.response_schema,
                },
            },
        }
        headers = {
            "Authorization": f"Bearer {self._api_key}",
            "Content-Type": "application/json",
        }

        for attempt in range(1, self._max_attempts + 1):
            try:
                response = await self._client.post(
                    f"{self._base_url}/chat/completions",
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
            if response.status_code == 498 or response.status_code >= 500:
                if attempt == self._max_attempts:
                    raise ChatProviderUnavailable("chat provider is unavailable")
                await self._backoff(attempt)
                continue
            if response.status_code >= 400:
                raise ChatProviderError("chat provider rejected the request")

            return self._parse_completion(response)

        raise ChatProviderUnavailable("chat provider is unavailable")

    async def close(self) -> None:
        if self._owns_client:
            await self._client.aclose()

    async def _backoff(self, attempt: int) -> None:
        await self._sleep(self._retry_delay_seconds * (2 ** (attempt - 1)))

    def _parse_completion(self, response: httpx.Response) -> ChatCompletion:
        try:
            payload: Any = response.json()
            content = payload["choices"][0]["message"]["content"]
            if not isinstance(content, str) or not content.strip():
                raise ValueError("missing completion content")
            usage = payload.get("usage") or {}
            input_tokens = usage.get("prompt_tokens")
            output_tokens = usage.get("completion_tokens")
            if input_tokens is not None and not isinstance(input_tokens, int):
                input_tokens = None
            if output_tokens is not None and not isinstance(output_tokens, int):
                output_tokens = None
        except (KeyError, IndexError, TypeError, ValueError) as exc:
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
    def _parse_retry_after(value: str | None) -> int | None:
        if value is None:
            return None
        try:
            seconds = int(float(value))
        except ValueError:
            return None
        return max(0, min(seconds, 300))
