from collections.abc import Iterator

import pytest
from fastapi import FastAPI
from fastapi.testclient import TestClient
from pydantic import SecretStr

from speedclaim_ai.config.settings import Settings
from speedclaim_ai.main import build_app

TEST_API_KEY = "speedclaim-r1-test-key-0123456789abcdef"


@pytest.fixture
def settings() -> Settings:
    return Settings(
        internal_api_key=SecretStr(TEST_API_KEY),
        environment="Test",
        log_level="CRITICAL",
        max_request_size_bytes=1_024,
        _env_file=None,
    )


@pytest.fixture
def app(settings: Settings) -> FastAPI:
    return build_app(settings)


@pytest.fixture
def client(app: FastAPI) -> Iterator[TestClient]:
    with TestClient(app, raise_server_exceptions=False) as test_client:
        yield test_client
