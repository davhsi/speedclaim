from fastapi.testclient import TestClient

from conftest import TEST_API_KEY


def test_internal_namespace_rejects_missing_key(client: TestClient) -> None:
    response = client.get("/internal/v1/future-endpoint")

    assert response.status_code == 401
    assert response.headers["www-authenticate"] == "ApiKey"
    assert response.json()["error"]["code"] == "unauthorized"
    assert response.json()["error"]["requestId"] == response.headers["x-correlation-id"]


def test_internal_namespace_rejects_incorrect_key(client: TestClient) -> None:
    response = client.get(
        "/internal/v1/future-endpoint",
        headers={"X-Internal-Api-Key": f"{TEST_API_KEY}-wrong"},
    )

    assert response.status_code == 401
    assert response.json()["error"]["message"] == (
        "A valid internal service credential is required."
    )


def test_internal_namespace_accepts_valid_key_before_route_resolution(
    client: TestClient,
) -> None:
    response = client.get(
        "/internal/v1/future-endpoint",
        headers={"X-Internal-Api-Key": TEST_API_KEY},
    )

    assert response.status_code == 404
    assert response.json()["error"]["code"] == "not_found"


def test_health_endpoints_do_not_require_internal_key(client: TestClient) -> None:
    response = client.get(
        "/health/ready",
        headers={"X-Internal-Api-Key": "incorrect"},
    )

    assert response.status_code == 200
