import hashlib
import re
from dataclasses import dataclass
from uuid import UUID, uuid4

from speedclaim_ai.rag.models import ParsedPage, ParsedPdf, PreparedChunk

_SECTION = re.compile(r"^SECTION\s+(\d+)[A-Z]?$", re.IGNORECASE)
_CLAUSE_WITH_TITLE = re.compile(r"^(\d+(?:\.\d+)+)\s+(.+)$")
_CLAUSE_ONLY = re.compile(r"^(\d+(?:\.\d+)+)$")
_TOKEN = re.compile(r"\w+|[^\w\s]", re.UNICODE)


@dataclass(frozen=True, slots=True)
class _SourceBlock:
    section_title: str | None
    clause_reference: str | None
    content: str


class HierarchicalChunker:
    def __init__(
        self,
        *,
        parent_max_characters: int,
        child_max_characters: int,
        child_overlap_characters: int,
    ) -> None:
        if child_overlap_characters >= child_max_characters:
            raise ValueError("child overlap must be smaller than child size")
        if parent_max_characters < child_max_characters:
            raise ValueError("parent chunk size must not be smaller than child chunk size")
        self._parent_max_characters = parent_max_characters
        self._child_max_characters = child_max_characters
        self._child_overlap_characters = child_overlap_characters

    def create_chunks(self, parsed: ParsedPdf) -> list[PreparedChunk]:
        chunks: list[PreparedChunk] = []
        next_index = 0

        for page in parsed.pages:
            children = self._page_children(page)
            for group in self._group_for_parents(children):
                parent_id = uuid4()
                section_title = group[0].section_title
                parent_content = "\n\n".join(child.content for child in group).strip()
                chunks.append(
                    self._prepared_chunk(
                        id=parent_id,
                        parent_chunk_id=None,
                        page_number=page.page_number,
                        section_title=section_title,
                        clause_reference=None,
                        chunk_index=next_index,
                        content=parent_content,
                        is_parent=True,
                    )
                )
                next_index += 1

                for child in group:
                    chunks.append(
                        self._prepared_chunk(
                            id=uuid4(),
                            parent_chunk_id=parent_id,
                            page_number=page.page_number,
                            section_title=child.section_title,
                            clause_reference=child.clause_reference,
                            chunk_index=next_index,
                            content=child.content,
                            is_parent=False,
                        )
                    )
                    next_index += 1

        return chunks

    def _page_children(self, page: ParsedPage) -> list[_SourceBlock]:
        section_title, section_number, body_lines = self._section_and_body(page)
        logical_blocks = self._logical_blocks(
            section_title,
            section_number,
            body_lines,
        )
        children: list[_SourceBlock] = []
        for block in logical_blocks:
            children.extend(
                _SourceBlock(
                    section_title=block.section_title,
                    clause_reference=block.clause_reference,
                    content=part,
                )
                for part in self._split_text(block.content)
            )
        return children

    @staticmethod
    def _section_and_body(
        page: ParsedPage,
    ) -> tuple[str | None, str | None, list[str]]:
        lines = list(page.lines)
        for index, line in enumerate(lines[:-1]):
            if section_match := _SECTION.fullmatch(line):
                return lines[index + 1][:512], section_match.group(1), lines[index + 2 :]
        return ("Document overview" if page.page_number == 1 else None), None, lines

    @staticmethod
    def _logical_blocks(
        section_title: str | None,
        section_number: str | None,
        lines: list[str],
    ) -> list[_SourceBlock]:
        blocks: list[_SourceBlock] = []
        current_lines: list[str] = []
        current_clause: str | None = None
        index = 0

        def flush() -> None:
            nonlocal current_lines
            content = "\n".join(current_lines).strip()
            if content:
                blocks.append(
                    _SourceBlock(
                        section_title=section_title,
                        clause_reference=current_clause,
                        content=content,
                    )
                )
            current_lines = []

        while index < len(lines):
            line = lines[index]
            combined = _CLAUSE_WITH_TITLE.fullmatch(line)
            standalone = _CLAUSE_ONLY.fullmatch(line)
            if combined and combined.group(1).split(".", 1)[0] != section_number:
                combined = None
            if standalone and standalone.group(1).split(".", 1)[0] != section_number:
                standalone = None
            if combined:
                flush()
                current_clause = combined.group(1)
                current_lines = [line]
            elif standalone:
                flush()
                current_clause = standalone.group(1)
                if index + 1 < len(lines):
                    index += 1
                    current_lines = [f"{current_clause} {lines[index]}"]
                else:
                    current_lines = [current_clause]
            else:
                current_lines.append(line)
            index += 1
        flush()

        if not blocks and section_title:
            blocks.append(
                _SourceBlock(
                    section_title=section_title,
                    clause_reference=None,
                    content=section_title,
                )
            )
        return blocks

    def _split_text(self, text: str) -> list[str]:
        if len(text) <= self._child_max_characters:
            return [text]

        chunks: list[str] = []
        start = 0
        while start < len(text):
            maximum_end = min(start + self._child_max_characters, len(text))
            end = maximum_end
            if maximum_end < len(text):
                candidates = (
                    text.rfind("\n", start + self._child_max_characters // 2, maximum_end),
                    text.rfind(". ", start + self._child_max_characters // 2, maximum_end),
                    text.rfind(" ", start + self._child_max_characters // 2, maximum_end),
                )
                boundary = max(candidates)
                if boundary > start:
                    end = boundary + (1 if text[boundary : boundary + 2] == ". " else 0)

            part = text[start:end].strip()
            if part:
                chunks.append(part)
            if end >= len(text):
                break

            next_start = max(end - self._child_overlap_characters, start + 1)
            while next_start < end and not text[next_start].isspace():
                next_start += 1
            start = next_start if next_start < end else end
        return chunks

    def _group_for_parents(self, children: list[_SourceBlock]) -> list[list[_SourceBlock]]:
        groups: list[list[_SourceBlock]] = []
        current: list[_SourceBlock] = []
        current_size = 0
        for child in children:
            separator_size = 2 if current else 0
            proposed = current_size + separator_size + len(child.content)
            if current and proposed > self._parent_max_characters:
                groups.append(current)
                current = []
                current_size = 0
            current.append(child)
            current_size += (2 if current_size else 0) + len(child.content)
        if current:
            groups.append(current)
        return groups

    @staticmethod
    def _prepared_chunk(
        *,
        id: UUID,
        parent_chunk_id: UUID | None,
        page_number: int,
        section_title: str | None,
        clause_reference: str | None,
        chunk_index: int,
        content: str,
        is_parent: bool,
    ) -> PreparedChunk:
        return PreparedChunk(
            id=id,
            parent_chunk_id=parent_chunk_id,
            page_number=page_number,
            section_title=section_title,
            clause_reference=clause_reference,
            chunk_index=chunk_index,
            content=content,
            content_hash=hashlib.sha256(content.encode("utf-8")).hexdigest(),
            token_count=max(1, len(_TOKEN.findall(content))),
            is_parent=is_parent,
        )
