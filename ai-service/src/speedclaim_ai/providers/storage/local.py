import asyncio
from pathlib import Path

from speedclaim_ai.rag.errors import IngestionFailure


class LocalBrochureReader:
    def __init__(self, root: Path) -> None:
        self._root = root.resolve()

    async def read_bytes(self, blob_path: str, *, max_bytes: int) -> bytes:
        return await asyncio.to_thread(self._read_bytes, blob_path, max_bytes)

    def _read_bytes(self, blob_path: str, max_bytes: int) -> bytes:
        candidate = Path(blob_path)
        if (
            not blob_path.strip()
            or candidate.is_absolute()
            or ".." in candidate.parts
            or "\\" in blob_path
        ):
            raise IngestionFailure(
                code="brochure_path_invalid",
                message="The brochure storage path is invalid.",
            )

        try:
            target = (self._root / blob_path).resolve(strict=True)
            target.relative_to(self._root)
        except (FileNotFoundError, RuntimeError):
            raise IngestionFailure(
                code="brochure_not_found",
                message="The brochure file was not found.",
                status_code=404,
            ) from None
        except ValueError:
            raise IngestionFailure(
                code="brochure_path_invalid",
                message="The brochure storage path is invalid.",
            ) from None

        if not target.is_file():
            raise IngestionFailure(
                code="brochure_not_found",
                message="The brochure file was not found.",
                status_code=404,
            )
        if target.stat().st_size > max_bytes:
            raise IngestionFailure(
                code="pdf_oversized",
                message="The PDF exceeds the configured size limit.",
                status_code=413,
            )

        data = target.read_bytes()
        if len(data) > max_bytes:
            raise IngestionFailure(
                code="pdf_oversized",
                message="The PDF exceeds the configured size limit.",
                status_code=413,
            )
        return data
