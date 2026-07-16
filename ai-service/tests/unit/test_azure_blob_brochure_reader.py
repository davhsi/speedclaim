from dataclasses import dataclass

import pytest
from azure.core.exceptions import AzureError, ResourceNotFoundError

from speedclaim_ai.providers.storage.azure_blob import AzureBlobBrochureReader
from speedclaim_ai.rag.errors import IngestionFailure

pytestmark = pytest.mark.anyio


@pytest.fixture
def anyio_backend() -> str:
    return "asyncio"


@dataclass
class FakeProperties:
    size: int


class FakeDownloader:
    def __init__(self, data: bytes) -> None:
        self._data = data

    def readall(self) -> bytes:
        return self._data


class FakeBlobClient:
    def __init__(self, data: bytes = b"%PDF-test", error: Exception | None = None) -> None:
        self.data = data
        self.error = error
        self.download_arguments = None

    def get_blob_properties(self) -> FakeProperties:
        if self.error:
            raise self.error
        return FakeProperties(size=len(self.data))

    def download_blob(self, **kwargs) -> FakeDownloader:
        self.download_arguments = kwargs
        return FakeDownloader(self.data)


class FakeServiceClient:
    def __init__(self, blob_client: FakeBlobClient) -> None:
        self.blob_client = blob_client
        self.requests = []
        self.closed = False

    def get_blob_client(self, *, container: str, blob: str) -> FakeBlobClient:
        self.requests.append((container, blob))
        return self.blob_client

    def close(self) -> None:
        self.closed = True


def _reader(service_client: FakeServiceClient) -> AzureBlobBrochureReader:
    return AzureBlobBrochureReader(
        connection_string="unused-in-test",
        container_name="speedclaim-uploads",
        service_client=service_client,
    )


async def test_reader_downloads_a_bounded_blob_and_closes_client() -> None:
    blob_client = FakeBlobClient()
    service_client = FakeServiceClient(blob_client)
    reader = _reader(service_client)

    data = await reader.read_bytes("product-brochures/v1.pdf", max_bytes=100)
    await reader.close()

    assert data == b"%PDF-test"
    assert service_client.requests == [
        ("speedclaim-uploads", "product-brochures/v1.pdf")
    ]
    assert blob_client.download_arguments == {
        "offset": 0,
        "length": 101,
        "max_concurrency": 1,
    }
    assert service_client.closed is True


async def test_reader_rejects_oversized_blob_before_download() -> None:
    blob_client = FakeBlobClient(data=b"x" * 101)
    reader = _reader(FakeServiceClient(blob_client))

    with pytest.raises(IngestionFailure) as failure:
        await reader.read_bytes("large.pdf", max_bytes=100)

    assert failure.value.code == "pdf_oversized"
    assert blob_client.download_arguments is None


@pytest.mark.parametrize(
    ("error", "expected_code", "expected_status"),
    [
        (ResourceNotFoundError("missing"), "brochure_not_found", 404),
        (AzureError("unavailable"), "brochure_storage_unavailable", 503),
    ],
)
async def test_reader_maps_azure_failures(
    error: Exception, expected_code: str, expected_status: int
) -> None:
    reader = _reader(FakeServiceClient(FakeBlobClient(error=error)))

    with pytest.raises(IngestionFailure) as failure:
        await reader.read_bytes("brochure.pdf", max_bytes=100)

    assert failure.value.code == expected_code
    assert failure.value.status_code == expected_status


@pytest.mark.parametrize("path", ["", " ../secret.pdf", "../secret.pdf", "/root.pdf"])
async def test_reader_rejects_invalid_blob_paths(path: str) -> None:
    reader = _reader(FakeServiceClient(FakeBlobClient()))

    with pytest.raises(IngestionFailure) as failure:
        await reader.read_bytes(path, max_bytes=100)

    assert failure.value.code == "brochure_path_invalid"
