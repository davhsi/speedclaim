import io
import math
import re
import unicodedata
from collections import Counter

from pypdf import PdfReader

from speedclaim_ai.rag.errors import IngestionFailure
from speedclaim_ai.rag.models import ParsedPage, ParsedPdf

_CONTROL_CHARACTERS = re.compile(r"[\x00-\x08\x0b\x0c\x0e-\x1f\x7f]")
_HORIZONTAL_WHITESPACE = re.compile(r"[ \t]+")
_PAGE_NUMBER = re.compile(r"^page\s+\d+(?:\s+of\s+\d+)?$", re.IGNORECASE)


class PdfParser:
    def __init__(
        self,
        *,
        max_size_bytes: int,
        max_pages: int,
        minimum_text_characters: int,
    ) -> None:
        self._max_size_bytes = max_size_bytes
        self._max_pages = max_pages
        self._minimum_text_characters = minimum_text_characters

    def parse(self, data: bytes) -> ParsedPdf:
        self._validate_container(data)
        try:
            reader = PdfReader(io.BytesIO(data), strict=True)
            if reader.is_encrypted:
                raise IngestionFailure(
                    code="pdf_encrypted",
                    message="Encrypted PDFs are not supported.",
                )
            if not reader.pages:
                raise IngestionFailure(code="pdf_empty", message="The PDF has no pages.")
            if len(reader.pages) > self._max_pages:
                raise IngestionFailure(
                    code="pdf_too_many_pages",
                    message="The PDF exceeds the configured page limit.",
                    status_code=413,
                )

            raw_pages = [
                self._extract_page_lines(page.extract_text()) for page in reader.pages
            ]
            text_character_count = sum(
                1 for lines in raw_pages for line in lines for value in line if value.isalnum()
            )
            if text_character_count < self._minimum_text_characters:
                has_images = any(self._page_has_images(page) for page in reader.pages)
                if has_images:
                    raise IngestionFailure(
                        code="pdf_image_only",
                        message="Image-only PDFs are not supported.",
                    )
                raise IngestionFailure(
                    code="pdf_empty",
                    message="The PDF does not contain enough extractable text.",
                )
        except IngestionFailure:
            raise
        except Exception as exc:
            raise IngestionFailure(
                code="pdf_corrupt",
                message="The PDF is corrupt or unreadable.",
            ) from exc

        repeated = self._find_repeated_boilerplate(raw_pages)
        pages = tuple(
            ParsedPage(
                page_number=index,
                lines=tuple(
                    line
                    for line in lines
                    if self._boilerplate_key(line) not in repeated
                ),
            )
            for index, lines in enumerate(raw_pages, start=1)
        )
        if not any(page.lines for page in pages):
            raise IngestionFailure(
                code="pdf_empty",
                message="The PDF does not contain usable text.",
            )
        return ParsedPdf(pages=pages, removed_boilerplate=tuple(sorted(repeated)))

    def _validate_container(self, data: bytes) -> None:
        if len(data) > self._max_size_bytes:
            raise IngestionFailure(
                code="pdf_oversized",
                message="The PDF exceeds the configured size limit.",
                status_code=413,
            )
        if not data.startswith(b"%PDF-"):
            raise IngestionFailure(
                code="pdf_corrupt",
                message="The file is not a valid PDF.",
            )

    @staticmethod
    def _extract_page_lines(text: str | None) -> list[str]:
        normalized = unicodedata.normalize("NFC", text or "")
        normalized = _CONTROL_CHARACTERS.sub("", normalized)
        return [
            cleaned
            for line in normalized.splitlines()
            if (cleaned := _HORIZONTAL_WHITESPACE.sub(" ", line).strip())
        ]

    @classmethod
    def _find_repeated_boilerplate(cls, pages: list[list[str]]) -> set[str]:
        if len(pages) < 2:
            return set()
        counts: Counter[str] = Counter()
        for lines in pages:
            counts.update(
                {
                    key
                    for line in lines
                    if len(line) <= 200 and (key := cls._boilerplate_key(line))
                }
            )
        threshold = max(2, math.ceil(len(pages) * 0.6))
        return {key for key, count in counts.items() if count >= threshold}

    @staticmethod
    def _boilerplate_key(line: str) -> str:
        normalized = _HORIZONTAL_WHITESPACE.sub(" ", line).strip().casefold()
        return "page #" if _PAGE_NUMBER.fullmatch(normalized) else normalized

    @staticmethod
    def _page_has_images(page: object) -> bool:
        try:
            return bool(list(page.images))  # type: ignore[attr-defined]
        except Exception:
            return False
