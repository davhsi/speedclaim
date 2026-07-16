from pathlib import Path

import pytest

from speedclaim_ai.providers.storage.local import LocalBrochureReader
from speedclaim_ai.rag.errors import IngestionFailure

pytestmark = pytest.mark.anyio


@pytest.fixture
def anyio_backend() -> str:
    return "asyncio"


async def test_reader_reads_only_files_below_its_configured_root(tmp_path: Path) -> None:
    brochure = tmp_path / "products" / "brochure.pdf"
    brochure.parent.mkdir()
    brochure.write_bytes(b"%PDF-test")
    reader = LocalBrochureReader(tmp_path)

    assert await reader.read_bytes("products/brochure.pdf", max_bytes=100) == b"%PDF-test"


@pytest.mark.parametrize("blob_path", ["../outside.pdf", "/tmp/outside.pdf", "..\\outside.pdf"])
async def test_reader_rejects_path_escape(tmp_path: Path, blob_path: str) -> None:
    reader = LocalBrochureReader(tmp_path)

    with pytest.raises(IngestionFailure) as failure:
        await reader.read_bytes(blob_path, max_bytes=100)

    assert failure.value.code == "brochure_path_invalid"


async def test_reader_reports_missing_and_oversized_files(tmp_path: Path) -> None:
    reader = LocalBrochureReader(tmp_path)
    with pytest.raises(IngestionFailure) as missing:
        await reader.read_bytes("missing.pdf", max_bytes=100)
    assert missing.value.code == "brochure_not_found"

    brochure = tmp_path / "large.pdf"
    brochure.write_bytes(b"x" * 101)
    with pytest.raises(IngestionFailure) as oversized:
        await reader.read_bytes("large.pdf", max_bytes=100)
    assert oversized.value.code == "pdf_oversized"
