from collections.abc import Callable
from dataclasses import dataclass
from typing import Any, Protocol


class ChatProviderError(Exception):
    """Base class for sanitized chat-provider failures."""


class ChatProviderTimeout(ChatProviderError):
    pass


class ChatProviderUnavailable(ChatProviderError):
    pass


class ChatProviderResponseError(ChatProviderError):
    pass


class ChatProviderRefusal(ChatProviderResponseError):
    pass


class ChatProviderTruncated(ChatProviderResponseError):
    pass


class ChatProviderRateLimited(ChatProviderError):
    def __init__(self, retry_after_seconds: int | None = None) -> None:
        super().__init__("chat provider rate limited the request")
        self.retry_after_seconds = retry_after_seconds


@dataclass(frozen=True, slots=True)
class ChatRequest:
    system_prompt: str
    user_prompt: str
    response_schema_name: str
    response_schema: dict[str, Any]
    response_validator: Callable[[str], object] | None = None


@dataclass(frozen=True, slots=True)
class ChatCompletion:
    content: str
    provider: str
    model: str
    input_tokens: int | None = None
    output_tokens: int | None = None


class ChatProvider(Protocol):
    @property
    def provider_name(self) -> str: ...

    @property
    def model_name(self) -> str: ...

    async def complete(self, request: ChatRequest) -> ChatCompletion: ...

    async def close(self) -> None: ...
