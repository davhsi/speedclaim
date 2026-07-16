import pytest
from pydantic import SecretStr

from speedclaim_ai.api.dependencies import ServiceContainer
from speedclaim_ai.config.settings import Settings
from speedclaim_ai.errors import AppError
from speedclaim_ai.providers.chat.anthropic_gateway import (
    AnthropicGatewayChatProvider,
)
from speedclaim_ai.providers.chat.groq import GroqChatProvider

pytestmark = pytest.mark.anyio

TEST_INTERNAL_KEY = "provider-selection-test-key-0123456789abcdef"
TEST_VECTOR_URL = "postgresql://test:test@localhost/speedclaim_ai"


@pytest.fixture
def anyio_backend() -> str:
    return "asyncio"


def _settings(**values) -> Settings:
    return Settings(
        internal_api_key=SecretStr(TEST_INTERNAL_KEY),
        vector_connection_string=SecretStr(TEST_VECTOR_URL),
        environment="Test",
        _env_file=None,
        **values,
    )


def _without_vector_initialization(container: ServiceContainer) -> None:
    container._repository = object()  # type: ignore[assignment]
    container._embedding_provider = object()  # type: ignore[assignment]


async def test_anthropic_gateway_selection_constructs_only_gateway_provider() -> None:
    container = ServiceContainer(
        _settings(
            chat_provider="AnthropicGateway",
            anthropic_base_url="https://gateway.example.test/anthropic",
            anthropic_auth_token=SecretStr("test-corporate-token"),
            groq_api_key=SecretStr("unused-groq-token"),
        )
    )
    _without_vector_initialization(container)

    await container.get_policy_qa_service()

    assert isinstance(container._chat_provider, AnthropicGatewayChatProvider)
    assert container._chat_provider.output_mode == "ValidatedJson"
    assert not isinstance(container._chat_provider, GroqChatProvider)
    await container.close()


async def test_groq_selection_constructs_only_groq_provider() -> None:
    container = ServiceContainer(
        _settings(
            chat_provider="Groq",
            groq_api_key=SecretStr("test-groq-token"),
            anthropic_base_url="https://gateway.example.test",
            anthropic_auth_token=SecretStr("unused-corporate-token"),
        )
    )
    _without_vector_initialization(container)

    await container.get_policy_qa_service()

    assert isinstance(container._chat_provider, GroqChatProvider)
    assert not isinstance(container._chat_provider, AnthropicGatewayChatProvider)
    await container.close()


@pytest.mark.parametrize(
    "settings",
    [
        _settings(
            chat_provider="AnthropicGateway",
            groq_api_key=SecretStr("must-not-fallback-to-groq"),
        ),
        _settings(
            chat_provider="Groq",
            anthropic_base_url="https://gateway.example.test",
            anthropic_auth_token=SecretStr("must-not-fallback-to-gateway"),
        ),
    ],
)
async def test_selected_provider_never_falls_back_to_other_credentials(
    settings: Settings,
) -> None:
    container = ServiceContainer(settings)

    with pytest.raises(AppError) as failure:
        await container.get_policy_qa_service()

    assert failure.value.code == "chat_provider_not_configured"
    assert failure.value.status_code == 503
