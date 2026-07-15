from functools import lru_cache
from pathlib import Path
from typing import Self

from pydantic import AliasChoices, Field, SecretStr, field_validator, model_validator
from pydantic_settings import BaseSettings, SettingsConfigDict

_ENVIRONMENTS = {"local", "development", "test", "staging", "production"}
_LOG_LEVELS = {"CRITICAL", "ERROR", "WARNING", "INFO", "DEBUG"}
DEFAULT_EMBEDDING_MODEL = "BAAI/bge-small-en-v1.5"
EMBEDDING_DIMENSION = 384


class Settings(BaseSettings):
    model_config = SettingsConfigDict(
        case_sensitive=False,
        env_file=".env",
        env_file_encoding="utf-8",
        extra="ignore",
        populate_by_name=True,
    )

    service_name: str = Field(
        default="speedclaim-ai",
        min_length=1,
        max_length=64,
        validation_alias=AliasChoices("AI__ServiceName", "AI__SERVICE_NAME"),
    )
    environment: str = Field(
        default="Development",
        validation_alias=AliasChoices("AI__Environment", "AI__ENVIRONMENT"),
    )
    log_level: str = Field(
        default="INFO",
        validation_alias=AliasChoices("AI__LogLevel", "AI__LOG_LEVEL"),
    )
    internal_api_key: SecretStr = Field(
        validation_alias=AliasChoices("AI__InternalApiKey", "AI__INTERNAL_API_KEY"),
    )
    max_request_size_bytes: int = Field(
        default=1_048_576,
        ge=1_024,
        le=10_485_760,
        validation_alias=AliasChoices(
            "AI__MaxRequestSizeBytes",
            "AI__MAX_REQUEST_SIZE_BYTES",
        ),
    )
    embedding_provider: str = Field(
        default="Local",
        validation_alias=AliasChoices("AI__EmbeddingProvider", "AI__EMBEDDING_PROVIDER"),
    )
    embedding_model: str = Field(
        default=DEFAULT_EMBEDDING_MODEL,
        validation_alias=AliasChoices("AI__EmbeddingModel", "AI__EMBEDDING_MODEL"),
    )
    embedding_dimension: int = Field(
        default=EMBEDDING_DIMENSION,
        validation_alias=AliasChoices("AI__EmbeddingDimension", "AI__EMBEDDING_DIMENSION"),
    )
    embedding_cache_dir: Path = Field(
        default=Path("/tmp/speedclaim-ai-models"),
        validation_alias=AliasChoices("AI__EmbeddingCacheDir", "AI__EMBEDDING_CACHE_DIR"),
    )
    embedding_threads: int = Field(
        default=2,
        ge=1,
        le=32,
        validation_alias=AliasChoices("AI__EmbeddingThreads", "AI__EMBEDDING_THREADS"),
    )
    vector_connection_string: SecretStr | None = Field(
        default=None,
        validation_alias=AliasChoices(
            "AI__VectorConnectionString",
            "AI__VECTOR_CONNECTION_STRING",
        ),
    )

    @field_validator("service_name")
    @classmethod
    def validate_service_name(cls, value: str) -> str:
        normalized = value.strip()
        if not normalized:
            raise ValueError("service name must not be blank")
        return normalized

    @field_validator("environment")
    @classmethod
    def validate_environment(cls, value: str) -> str:
        normalized = value.strip().lower()
        if normalized not in _ENVIRONMENTS:
            raise ValueError("environment is not supported")
        return normalized.capitalize()

    @field_validator("log_level")
    @classmethod
    def validate_log_level(cls, value: str) -> str:
        normalized = value.strip().upper()
        if normalized not in _LOG_LEVELS:
            raise ValueError("log level is not supported")
        return normalized

    @field_validator("embedding_provider")
    @classmethod
    def validate_embedding_provider(cls, value: str) -> str:
        if value.strip().lower() != "local":
            raise ValueError("only the Local embedding provider is supported in Phase R2")
        return "Local"

    @field_validator("embedding_model")
    @classmethod
    def validate_embedding_model(cls, value: str) -> str:
        normalized = value.strip()
        if normalized != DEFAULT_EMBEDDING_MODEL:
            raise ValueError(f"Phase R2 requires embedding model {DEFAULT_EMBEDDING_MODEL}")
        return normalized

    @field_validator("embedding_dimension")
    @classmethod
    def validate_embedding_dimension(cls, value: int) -> int:
        if value != EMBEDDING_DIMENSION:
            raise ValueError(f"Phase R2 requires {EMBEDDING_DIMENSION}-dimension embeddings")
        return value

    @field_validator("vector_connection_string")
    @classmethod
    def validate_vector_connection_string(cls, value: SecretStr | None) -> SecretStr | None:
        if value is None:
            return None
        connection_string = value.get_secret_value()
        if connection_string != connection_string.strip():
            raise ValueError("vector connection string must not contain surrounding whitespace")
        if not connection_string.startswith(("postgresql://", "postgresql+psycopg://")):
            raise ValueError("vector connection string must use PostgreSQL with Psycopg")
        return value

    @model_validator(mode="after")
    def validate_internal_api_key(self) -> Self:
        value = self.internal_api_key.get_secret_value()
        if value != value.strip():
            raise ValueError("internal API key must not contain surrounding whitespace")
        if not 32 <= len(value) <= 512:
            raise ValueError("internal API key must contain between 32 and 512 characters")
        return self


@lru_cache(maxsize=1)
def get_settings() -> Settings:
    return Settings()
