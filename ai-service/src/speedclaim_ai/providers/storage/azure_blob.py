import asyncio
from pathlib import PurePosixPath
from typing import Any

from azure.core.exceptions import AzureError, ResourceNotFoundError
from azure.storage.blob import BlobServiceClient

from speedclaim_ai.rag.errors import IngestionFailure


class AzureBlobBrochureReader:
    def __init__(
        self,
        *,
        connection_string: str,
        container_name: str,
        service_client: Any | None = None,
    ) -> None:
        self._container_name = container_name
        self._service_client = service_client or BlobServiceClient.from_connection_string(
            connection_string
        )

    async def read_bytes(self, blob_path: str, *, max_bytes: int) -> bytes:
        normalized_path = self._validate_blob_path(blob_path)
        return await asyncio.to_thread(
            self._read_bytes,
            normalized_path,
            max_bytes,
        )

    async def close(self) -> None:
        await asyncio.to_thread(self._service_client.close)

    def _read_bytes(self, blob_path: str, max_bytes: int) -> bytes:
        try:
            client = self._service_client.get_blob_client(
                container=self._container_name,
                blob=blob_path,
            )
            properties = client.get_blob_properties()
            if properties.size > max_bytes:
                raise IngestionFailure(
                    code="pdf_oversized",
                    message="The PDF exceeds the configured size limit.",
                    status_code=413,
                )
            downloader = client.download_blob(
                offset=0,
                length=max_bytes + 1,
                max_concurrency=1,
            )
            data = downloader.readall()
        except IngestionFailure:
            raise
        except ResourceNotFoundError:
            raise IngestionFailure(
                code="brochure_not_found",
                message="The brochure file was not found.",
                status_code=404,
            ) from None
        except AzureError as exc:
            raise IngestionFailure(
                code="brochure_storage_unavailable",
                message="Brochure storage is temporarily unavailable.",
                status_code=503,
            ) from exc

        if len(data) > max_bytes:
            raise IngestionFailure(
                code="pdf_oversized",
                message="The PDF exceeds the configured size limit.",
                status_code=413,
            )
        return data

    @staticmethod
    def _validate_blob_path(blob_path: str) -> str:
        candidate = PurePosixPath(blob_path)
        if (
            not blob_path.strip()
            or blob_path != blob_path.strip()
            or candidate.is_absolute()
            or ".." in candidate.parts
            or "\\" in blob_path
        ):
            raise IngestionFailure(
                code="brochure_path_invalid",
                message="The brochure storage path is invalid.",
            )
        return candidate.as_posix()
