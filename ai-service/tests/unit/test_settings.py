import pytest
from pydantic import SecretStr, ValidationError

from speedclaim_ai.config.settings import (
    DEFAULT_CHAT_MODEL,
    DEFAULT_EMBEDDING_MODEL,
    EMBEDDING_DIMENSION,
    POLICY_QA_PROMPT_VERSION,
    Settings,
)

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
    assert settings.groq_api_key is None
    assert settings.vector_connection_string is None
    assert settings.embedding_provider == "Local"
    assert settings.embedding_model == DEFAULT_EMBEDDING_MODEL
    assert settings.embedding_dimension == EMBEDDING_DIMENSION
    assert settings.storage_provider == "Local"
    assert settings.local_brochure_root.is_absolute()
    assert settings.pdf_max_size_bytes == 10_485_760
    assert settings.chat_provider == "Groq"
    assert settings.chat_model == DEFAULT_CHAT_MODEL
    assert settings.policy_qa_prompt_version == POLICY_QA_PROMPT_VERSION
    assert settings.retrieval_min_similarity == pytest.approx(0.45)


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


@pytest.mark.parametrize(
    ("field", "value"),
    [
        ("storage_provider", "AzureBlob"),
        ("local_brochure_root", "relative/brochures"),
        ("pdf_max_pages", 0),
    ],
)
def test_phase_r3_ingestion_configuration_is_validated(
    field: str, value: object
) -> None:
    with pytest.raises(ValidationError):
        Settings(
            internal_api_key=SecretStr(VALID_KEY),
            _env_file=None,
            **{field: value},
        )


def test_child_chunk_overlap_must_be_smaller_than_chunk_size() -> None:
    with pytest.raises(ValidationError, match="overlap"):
        Settings(
            internal_api_key=SecretStr(VALID_KEY),
            child_chunk_max_characters=500,
            child_chunk_overlap_characters=500,
            _env_file=None,
        )


def test_parent_chunk_must_not_be_smaller_than_child_chunk() -> None:
    with pytest.raises(ValidationError, match="parent chunk"):
        Settings(
            internal_api_key=SecretStr(VALID_KEY),
            parent_chunk_max_characters=500,
            child_chunk_max_characters=600,
            _env_file=None,
        )


def test_azure_blob_storage_requires_and_redacts_connection_string() -> None:
    connection_string = "DefaultEndpointsProtocol=https;AccountName=test;AccountKey=secret"
    settings = Settings(
        internal_api_key=SecretStr(VALID_KEY),
        storage_provider="AzureBlob",
        azure_blob_connection_string=SecretStr(connection_string),
        azure_blob_container_name="speedclaim-uploads",
        _env_file=None,
    )

    assert settings.storage_provider == "AzureBlob"
    assert settings.azure_blob_connection_string is not None
    assert connection_string not in repr(settings)


def test_azure_blob_container_name_is_validated() -> None:
    with pytest.raises(ValidationError, match="container"):
        Settings(
            internal_api_key=SecretStr(VALID_KEY),
            azure_blob_container_name="Invalid_Name",
            _env_file=None,
        )


def test_optional_groq_key_is_validated_and_redacted() -> None:
    api_key = "test-groq-api-key-not-live"
    settings = Settings(
        internal_api_key=SecretStr(VALID_KEY),
        groq_api_key=SecretStr(api_key),
        _env_file=None,
    )

    assert settings.groq_api_key is not None
    assert settings.groq_api_key.get_secret_value() == api_key
    assert api_key not in repr(settings)


@pytest.mark.parametrize(
    ("field", "value"),
    [
        ("chat_provider", "Other"),
        ("groq_api_key", SecretStr(" ")),
        ("groq_base_url", "https://example.com/openai/v1"),
        ("policy_qa_prompt_version", "unreviewed-v2"),
        ("retrieval_min_similarity", 1.1),
        ("retrieval_child_limit", 0),
    ],
)
def test_phase_r4_configuration_is_validated(field: str, value: object) -> None:
    with pytest.raises(ValidationError):
        Settings(
            internal_api_key=SecretStr(VALID_KEY),
            _env_file=None,
            **{field: value},
        )
