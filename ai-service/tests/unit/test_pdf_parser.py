import io
from pathlib import Path

import pytest
from PIL import Image
from pypdf import PdfWriter

from speedclaim_ai.rag.errors import IngestionFailure
from speedclaim_ai.rag.pdf_parser import PdfParser

FIXTURE = (
    Path(__file__).resolve().parents[3]
    / "output/pdf/speedclaim-arogya-shield-plus-synthetic-brochure-v1.pdf"
)


def _parser(**overrides) -> PdfParser:
    values = {
        "max_size_bytes": 10_485_760,
        "max_pages": 300,
        "minimum_text_characters": 100,
        **overrides,
    }
    return PdfParser(**values)


def _blank_pdf(*, encrypted: bool = False, pages: int = 1) -> bytes:
    writer = PdfWriter()
    for _ in range(pages):
        writer.add_blank_page(width=100, height=100)
    if encrypted:
        writer.encrypt("test-password")
    output = io.BytesIO()
    writer.write(output)
    return output.getvalue()


def _image_only_pdf() -> bytes:
    image = Image.new("RGB", (100, 100), color="white")
    output = io.BytesIO()
    image.save(output, format="PDF")
    return output.getvalue()


def test_synthetic_brochure_is_extracted_page_by_page_without_boilerplate() -> None:
    parsed = _parser().parse(FIXTURE.read_bytes())

    assert parsed.page_count == 13
    assert "page #" in parsed.removed_boilerplate
    assert "speedclaim arogya shield plus" in parsed.removed_boilerplate
    assert "SECTION 5" in parsed.pages[5].lines
    assert "Waiting periods" in parsed.pages[5].lines
    assert all(
        "Fictional product. Not valid insurance coverage" not in page.text
        for page in parsed.pages
    )
    assert all(not line.startswith("Page ") for page in parsed.pages for line in page.lines)


@pytest.mark.parametrize("data", [b"not-a-pdf", b"%PDF-1.7\ntruncated"])
def test_corrupt_pdf_is_rejected(data: bytes) -> None:
    with pytest.raises(IngestionFailure, match="valid PDF|corrupt") as failure:
        _parser().parse(data)

    assert failure.value.code == "pdf_corrupt"


def test_encrypted_pdf_is_rejected_without_attempting_decryption() -> None:
    with pytest.raises(IngestionFailure) as failure:
        _parser().parse(_blank_pdf(encrypted=True))

    assert failure.value.code == "pdf_encrypted"


def test_blank_pdf_is_rejected() -> None:
    with pytest.raises(IngestionFailure) as failure:
        _parser().parse(_blank_pdf())

    assert failure.value.code == "pdf_empty"


def test_image_only_pdf_is_rejected_without_ocr() -> None:
    with pytest.raises(IngestionFailure) as failure:
        _parser().parse(_image_only_pdf())

    assert failure.value.code == "pdf_image_only"


def test_oversized_pdf_is_rejected_before_parsing() -> None:
    data = FIXTURE.read_bytes()

    with pytest.raises(IngestionFailure) as failure:
        _parser(max_size_bytes=len(data) - 1).parse(data)

    assert failure.value.code == "pdf_oversized"
    assert failure.value.status_code == 413


def test_pdf_page_limit_is_enforced_before_extraction() -> None:
    with pytest.raises(IngestionFailure) as failure:
        _parser(max_pages=1).parse(_blank_pdf(pages=2))

    assert failure.value.code == "pdf_too_many_pages"
    assert failure.value.status_code == 413
