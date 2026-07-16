import asyncio
import re
from dataclasses import dataclass
from difflib import SequenceMatcher
from typing import Literal
from uuid import UUID

from speedclaim_ai.providers.embeddings.base import EmbeddingProvider
from speedclaim_ai.repositories.vector_repository import (
    ChunkMatch,
    StoredChunk,
    VectorRepository,
)
from speedclaim_ai.security.input_validation import normalize_question

_WHITESPACE = re.compile(r"\s+")


@dataclass(frozen=True, slots=True)
class RetrievedEvidence:
    citation_id: str
    child_chunk_id: UUID
    parent_chunk_id: UUID
    page_number: int
    section_title: str | None
    clause_reference: str | None
    content: str
    matched_content: str
    score: float


@dataclass(frozen=True, slots=True)
class RetrievalResult:
    normalized_question: str
    evidence_status: Literal["Sufficient", "InsufficientEvidence"]
    evidence: tuple[RetrievedEvidence, ...]
    top_score: float | None


class RetrievalService:
    def __init__(
        self,
        *,
        embedding_provider: EmbeddingProvider,
        repository: VectorRepository,
        question_max_characters: int,
        child_limit: int,
        minimum_similarity: float,
        max_parent_chunks: int,
        max_context_characters: int,
    ) -> None:
        self._embedding_provider = embedding_provider
        self._repository = repository
        self._question_max_characters = question_max_characters
        self._child_limit = child_limit
        self._minimum_similarity = minimum_similarity
        self._max_parent_chunks = max_parent_chunks
        self._max_context_characters = max_context_characters

    async def retrieve(self, brochure_id: UUID, question: str) -> RetrievalResult:
        normalized_question = normalize_question(
            question,
            max_characters=self._question_max_characters,
        )
        query_embedding = await asyncio.to_thread(
            self._embedding_provider.embed_query,
            normalized_question,
        )
        matches = await self._repository.search(
            brochure_id,
            query_embedding,
            limit=self._child_limit,
        )
        self._validate_matches(brochure_id, matches)
        top_score = matches[0].score if matches else None

        relevant = [
            match for match in matches if match.score >= self._minimum_similarity
        ]
        relevant = self._deduplicate_children(relevant)
        selected = self._select_distinct_parents(relevant)
        if not selected:
            return RetrievalResult(
                normalized_question=normalized_question,
                evidence_status="InsufficientEvidence",
                evidence=(),
                top_score=top_score,
            )

        parent_ids = [
            match.parent_chunk_id or match.chunk_id for match in selected
        ]
        parents = await self._repository.get_chunks_by_ids(brochure_id, parent_ids)
        self._validate_parents(brochure_id, parents)
        parent_by_id = {parent.chunk_id: parent for parent in parents}

        evidence: list[RetrievedEvidence] = []
        seen_parent_text: list[str] = []
        total_characters = 0
        for match in selected:
            parent_id = match.parent_chunk_id or match.chunk_id
            parent = parent_by_id.get(parent_id)
            if parent is None:
                continue
            normalized_parent = self._normalize_for_dedup(parent.content)
            if self._is_near_duplicate(normalized_parent, seen_parent_text):
                continue
            if total_characters + len(parent.content) > self._max_context_characters:
                continue
            seen_parent_text.append(normalized_parent)
            total_characters += len(parent.content)
            evidence.append(
                RetrievedEvidence(
                    citation_id=f"C{len(evidence) + 1}",
                    child_chunk_id=match.chunk_id,
                    parent_chunk_id=parent.chunk_id,
                    page_number=match.page_number,
                    section_title=match.section_title or parent.section_title,
                    clause_reference=match.clause_reference or parent.clause_reference,
                    content=parent.content,
                    matched_content=match.content,
                    score=match.score,
                )
            )

        return RetrievalResult(
            normalized_question=normalized_question,
            evidence_status=("Sufficient" if evidence else "InsufficientEvidence"),
            evidence=tuple(evidence),
            top_score=top_score,
        )

    @staticmethod
    def _validate_matches(brochure_id: UUID, matches: list[ChunkMatch]) -> None:
        if any(match.brochure_id != brochure_id for match in matches):
            raise RuntimeError("vector repository returned a cross-brochure search result")

    @staticmethod
    def _validate_parents(brochure_id: UUID, parents: list[StoredChunk]) -> None:
        if any(parent.brochure_id != brochure_id for parent in parents):
            raise RuntimeError("vector repository returned cross-brochure parent context")

    def _select_distinct_parents(self, matches: list[ChunkMatch]) -> list[ChunkMatch]:
        selected: list[ChunkMatch] = []
        parent_ids: set[UUID] = set()
        for match in matches:
            parent_id = match.parent_chunk_id or match.chunk_id
            if parent_id in parent_ids:
                continue
            parent_ids.add(parent_id)
            selected.append(match)
            if len(selected) == self._max_parent_chunks:
                break
        return selected

    @classmethod
    def _deduplicate_children(cls, matches: list[ChunkMatch]) -> list[ChunkMatch]:
        selected: list[ChunkMatch] = []
        normalized_contents: list[str] = []
        for match in matches:
            normalized = cls._normalize_for_dedup(match.content)
            if cls._is_near_duplicate(normalized, normalized_contents):
                continue
            selected.append(match)
            normalized_contents.append(normalized)
        return selected

    @staticmethod
    def _normalize_for_dedup(content: str) -> str:
        return _WHITESPACE.sub(" ", content).strip().casefold()

    @staticmethod
    def _is_near_duplicate(candidate: str, existing: list[str]) -> bool:
        return any(
            candidate == value
            or SequenceMatcher(None, candidate, value, autojunk=False).ratio() >= 0.94
            for value in existing
        )
