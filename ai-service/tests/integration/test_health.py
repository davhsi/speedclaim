import uuid

from fastapi.testclient import TestClient


def test_liveness_contract_is_external_dependency_free(client: TestClient) -> None:
    response = client.get("/health/live")

    assert response.status_code == 200
    assert response.json() == {
        "status": "alive",
        "service": "speedclaim-ai",
        "version": "0.4.0",
    }
    assert response.headers["cache-control"] == "no-store"
    uuid.UUID(response.headers["x-correlation-id"])


def test_readiness_checks_only_r1_configuration(client: TestClient) -> None:
    response = client.get("/health/ready")

    assert response.status_code == 200
    assert response.json() == {
        "status": "ready",
        "service": "speedclaim-ai",
        "checks": {"configuration": "ready"},
    }


def test_safe_correlation_id_is_propagated(client: TestClient) -> None:
    response = client.get(
        "/health/live",
        headers={"X-Correlation-ID": "dotnet-request_123"},
    )

    assert response.headers["x-correlation-id"] == "dotnet-request_123"


def test_unsafe_correlation_id_is_replaced(client: TestClient) -> None:
    response = client.get(
        "/health/live",
        headers={"X-Correlation-ID": "unsafe\nvalue"},
    )

    generated_id = response.headers["x-correlation-id"]
    assert generated_id != "unsafe\nvalue"
    uuid.UUID(generated_id)
