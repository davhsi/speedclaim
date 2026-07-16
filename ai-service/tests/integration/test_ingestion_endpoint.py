from uuid import uuid4

from fastapi.testclient import TestClient

from speedclaim_ai.config.settings import DEFAULT_EMBEDDING_MODEL, EMBEDDING_DIMENSION
from speedclaim_ai.rag.errors import IngestionFailure
from speedclaim_ai.rag.models import IngestionResult
from conftest import TEST_API_KEY


class FakeIngestionService:
    def __init__(self, outcome) -> None:
        self.outcome = outcome
        self.commands = []

    async def ingest(self, command):
        self.commands.append(command)
        if isinstance(self.outcome, Exception):
            raise self.outcome
        return self.outcome


def _payload() -> dict:
    return {
        "requestId": str(uuid4()),
        "brochureId": str(uuid4()),
        "productId": str(uuid4()),
        "version": "1",
        "blobPath": "products/brochure-v1.pdf",
        "contentHash": "a" * 64,
    }


def test_ingestion_endpoint_maps_internal_contract(client: TestClient) -> None:
    payload = _payload()
    document_id = uuid4()
    service = FakeIngestionService(
        IngestionResult(
            request_id=uuid4(),
            document_id=document_id,
            status="Succeeded",
            page_count=13,
            parent_chunk_count=13,
            child_chunk_count=95,
            embedding_provider="FastEmbed",
            embedding_model=DEFAULT_EMBEDDING_MODEL,
            embedding_dimension=EMBEDDING_DIMENSION,
        )
    )
    client.app.state.ingestion_service_override = service

    response = client.post(
        "/internal/v1/brochures/ingest",
        headers={"X-Internal-Api-Key": TEST_API_KEY},
        json=payload,
    )

    assert response.status_code == 200
    assert response.json() == {
        "requestId": str(service.outcome.request_id),
        "brochureId": payload["brochureId"],
        "documentId": str(document_id),
        "status": "Succeeded",
        "pageCount": 13,
        "parentChunkCount": 13,
        "childChunkCount": 95,
        "embeddingProvider": "FastEmbed",
        "embeddingModel": DEFAULT_EMBEDDING_MODEL,
        "embeddingDimension": EMBEDDING_DIMENSION,
    }
    assert len(service.commands) == 1
    command = service.commands[0]
    assert command.brochure_version == "1"
    assert command.blob_path == "products/brochure-v1.pdf"


def test_ingestion_failure_uses_global_error_contract(client: TestClient) -> None:
    service = FakeIngestionService(
        IngestionFailure(
            code="pdf_encrypted",
            message="Encrypted PDFs are not supported.",
            status_code=422,
        )
    )
    client.app.state.ingestion_service_override = service

    response = client.post(
        "/internal/v1/brochures/ingest",
        headers={"X-Internal-Api-Key": TEST_API_KEY},
        json=_payload(),
    )

    assert response.status_code == 422
    assert response.json()["error"]["code"] == "pdf_encrypted"
    assert response.json()["error"]["message"] == "Encrypted PDFs are not supported."


def test_ingestion_contract_rejects_invalid_hash(client: TestClient) -> None:
    payload = _payload()
    payload["contentHash"] = "not-a-sha"

    response = client.post(
        "/internal/v1/brochures/ingest",
        headers={"X-Internal-Api-Key": TEST_API_KEY},
        json=payload,
    )

    assert response.status_code == 422
    assert response.json()["error"]["code"] == "invalid_request"


def test_ingestion_requires_database_configuration_only_when_called(
    client: TestClient,
) -> None:
    response = client.post(
        "/internal/v1/brochures/ingest",
        headers={"X-Internal-Api-Key": TEST_API_KEY},
        json=_payload(),
    )

    assert response.status_code == 503
    assert response.json()["error"]["code"] == "vector_database_not_configured"
