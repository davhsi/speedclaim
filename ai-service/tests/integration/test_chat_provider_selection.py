import pytest
from pydantic import SecretStr

from speedclaim_ai.api.dependencies import ServiceContainer
from speedclaim_ai.config.settings import Settings
from speedclaim_ai.errors import AppError
from speedclaim_ai.providers.chat.anthropic_gateway import AnthropicGatewayChatProvider

pytestmark = pytest.mark.anyio


@pytest.fixture
def anyio_backend() -> str:
    return "asyncio"


def _settings(**values) -> Settings:
    return Settings(
        internal_api_key=SecretStr("provider-selection-test-key-0123456789abcdef"),
        vector_connection_string=SecretStr("postgresql://test:test@localhost/speedclaim_ai"),
        environment="Test",
        _env_file=None,
        **values,
    )


async def test_anthropic_gateway_is_the_only_chat_provider() -> None:
    container = ServiceContainer(
        _settings(
            anthropic_base_url="https://gateway.example.test/anthropic",
            anthropic_auth_token=SecretStr("test-corporate-token"),
        )
    )
    container._repository = object()  # type: ignore[assignment]
    container._embedding_provider = object()  # type: ignore[assignment]

    await container.get_policy_qa_service()

    assert isinstance(container._chat_provider, AnthropicGatewayChatProvider)
    assert container._chat_provider.model_name == "claude-sonnet-4-6"
    await container.close()


async def test_anthropic_gateway_credentials_are_required() -> None:
    container = ServiceContainer(_settings())

    with pytest.raises(AppError) as failure:
        await container.get_policy_qa_service()

    assert failure.value.code == "chat_provider_not_configured"
