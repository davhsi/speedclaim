from fastapi import FastAPI
from fastapi.testclient import TestClient

from conftest import TEST_API_KEY
from speedclaim_ai.errors import AppError


def test_request_body_over_limit_is_rejected(client: TestClient) -> None:
    response = client.post(
        "/internal/v1/future-endpoint",
        headers={"X-Internal-Api-Key": TEST_API_KEY},
        content=b"x" * 1_025,
    )

    assert response.status_code == 413
    assert response.json()["error"]["code"] == "request_too_large"
    assert response.json()["error"]["requestId"] == response.headers["x-correlation-id"]


def test_request_body_at_limit_reaches_routing(client: TestClient) -> None:
    response = client.post(
        "/internal/v1/future-endpoint",
        headers={"X-Internal-Api-Key": TEST_API_KEY},
        content=b"x" * 1_024,
    )

    assert response.status_code == 404


def test_application_error_uses_public_error_contract(app: FastAPI) -> None:
    @app.get("/_test/conflict")
    async def conflict() -> None:
        raise AppError(409, "fixture_conflict", "The fixture conflicts.")

    with TestClient(app, raise_server_exceptions=False) as client:
        response = client.get("/_test/conflict")

    assert response.status_code == 409
    assert response.json()["error"]["code"] == "fixture_conflict"
    assert response.json()["error"]["message"] == "The fixture conflicts."


def test_unexpected_error_does_not_leak_exception_details(app: FastAPI) -> None:
    @app.get("/_test/failure")
    async def failure() -> None:
        raise RuntimeError("sensitive provider detail")

    with TestClient(app, raise_server_exceptions=False) as client:
        response = client.get("/_test/failure")

    body = response.json()
    assert response.status_code == 500
    assert body["error"]["code"] == "internal_error"
    assert body["error"]["message"] == "An unexpected error occurred."
    assert "sensitive provider detail" not in response.text
    assert body["error"]["requestId"] == response.headers["x-correlation-id"]


def test_framework_404_uses_public_error_contract(client: TestClient) -> None:
    response = client.get("/missing")

    assert response.status_code == 404
    assert response.json()["error"]["code"] == "not_found"
    assert response.json()["error"]["requestId"] == response.headers["x-correlation-id"]
