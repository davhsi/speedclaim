import pytest
from pydantic import SecretStr, ValidationError

from speedclaim_ai.config.settings import DEFAULT_EMBEDDING_MODEL, EMBEDDING_DIMENSION, Settings

VALID_KEY = "settings-test-key-0123456789abcdef"


def test_settings_read_planned_environment_names(monkeypatch: pytest.MonkeyPatch) -> None:
    monkeypatch.setenv("AI__InternalApiKey", VALID_KEY)
    monkeypatch.setenv("AI__Environment", "production")
    monkeypatch.setenv("AI__LogLevel", "warning")
    monkeypatch.setenv("AI__MaxRequestSizeBytes", "2048")

    settings = Settings(_env_file=None)

    assert settings.internal_api_key.get_secret_value() == VALID_KEY
    assert settings.environment == "Production"
    assert settings.log_level == "WARNING"
    assert settings.max_request_size_bytes == 2_048
    assert VALID_KEY not in repr(settings)


def test_internal_api_key_is_required(monkeypatch: pytest.MonkeyPatch) -> None:
    monkeypatch.delenv("AI__InternalApiKey", raising=False)
    monkeypatch.delenv("AI__INTERNAL_API_KEY", raising=False)

    with pytest.raises(ValidationError, match="InternalApiKey"):
        Settings(_env_file=None)


@pytest.mark.parametrize(
    "value",
    [
        "too-short",
        f" {VALID_KEY}",
        f"{VALID_KEY} ",
    ],
)
def test_internal_api_key_validation(value: str) -> None:
    with pytest.raises(ValidationError, match="internal API key"):
        Settings(internal_api_key=SecretStr(value), _env_file=None)


@pytest.mark.parametrize("size", [1_023, 10_485_761])
def test_request_size_limit_has_safe_bounds(size: int) -> None:
    with pytest.raises(ValidationError):
        Settings(
            internal_api_key=SecretStr(VALID_KEY),
            max_request_size_bytes=size,
            _env_file=None,
        )


def test_external_provider_configuration_is_not_required() -> None:
    settings = Settings(
        internal_api_key=SecretStr(VALID_KEY),
        _env_file=None,
    )

    assert settings.environment == "Development"
    assert not hasattr(settings, "chat_api_key")
    assert settings.vector_connection_string is None
    assert settings.embedding_provider == "Local"
    assert settings.embedding_model == DEFAULT_EMBEDDING_MODEL
    assert settings.embedding_dimension == EMBEDDING_DIMENSION


def test_vector_connection_string_is_validated_and_redacted() -> None:
    connection_string = "postgresql://user:password@localhost/speedclaim_ai"

    settings = Settings(
        internal_api_key=SecretStr(VALID_KEY),
        vector_connection_string=SecretStr(connection_string),
        _env_file=None,
    )

    assert settings.vector_connection_string is not None
    assert settings.vector_connection_string.get_secret_value() == connection_string
    assert connection_string not in repr(settings)


@pytest.mark.parametrize(
    ("field", "value"),
    [
        ("embedding_provider", "Cloud"),
        ("embedding_model", "some-other-model"),
        ("embedding_dimension", 768),
        ("vector_connection_string", SecretStr("sqlite:///speedclaim.db")),
    ],
)
def test_phase_r2_provider_configuration_is_fixed(field: str, value: object) -> None:
    with pytest.raises(ValidationError):
        Settings(
            internal_api_key=SecretStr(VALID_KEY),
            _env_file=None,
            **{field: value},
        )
