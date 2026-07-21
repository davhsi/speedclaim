import math
import os
from collections.abc import Callable, Iterable, Sequence
from pathlib import Path
from typing import Any

from speedclaim_ai.config.settings import DEFAULT_EMBEDDING_MODEL, EMBEDDING_DIMENSION

ModelFactory = Callable[[], Any]


class FastEmbedProvider:
    """Lazy CPU embedding provider; model files are loaded only on first embedding call."""

    def __init__(
        self,
        *,
        model_name: str = DEFAULT_EMBEDDING_MODEL,
        dimension: int = EMBEDDING_DIMENSION,
        cache_dir: Path = Path("/home/speedclaim/.cache/models"),
        threads: int = 2,
        model_factory: ModelFactory | None = None,
    ) -> None:
        self._model_name = model_name
        self._dimension = dimension
        self._cache_dir = cache_dir
        self._threads = threads
        self._model_factory = model_factory or self._build_model
        self._model: Any | None = None

    @property
    def provider_name(self) -> str:
        return "FastEmbed"

    @property
    def model_name(self) -> str:
        return self._model_name

    @property
    def dimension(self) -> int:
        return self._dimension

    def embed_documents(self, texts: Sequence[str]) -> list[list[float]]:
        normalized = self._validate_texts(texts)
        return self._validate_vectors(self._get_model().passage_embed(normalized), len(normalized))

    def embed_query(self, text: str) -> list[float]:
        normalized = self._validate_texts([text])
        vectors = self._validate_vectors(self._get_model().query_embed(normalized), 1)
        return vectors[0]

    def _get_model(self) -> Any:
        if self._model is None:
            self._model = self._model_factory()
        return self._model

    def _build_model(self) -> Any:
        from fastembed import TextEmbedding

        self._ensure_private_cache_dir()
        return TextEmbedding(
            model_name=self._model_name,
            cache_dir=str(self._cache_dir),
            threads=self._threads,
            cuda=False,
        )

    def _ensure_private_cache_dir(self) -> None:
        """Create a model cache owned by the service user, never a shared temp directory."""
        self._cache_dir.mkdir(parents=True, exist_ok=True, mode=0o700)
        if self._cache_dir.is_symlink() or not self._cache_dir.is_dir():
            raise RuntimeError("embedding cache path must be a real directory")
        os.chmod(self._cache_dir, 0o700)

    @staticmethod
    def _validate_texts(texts: Sequence[str]) -> list[str]:
        normalized = [text.strip() for text in texts]
        if not normalized or any(not text for text in normalized):
            raise ValueError("embedding text must not be empty")
        return normalized

    def _validate_vectors(
        self, vectors: Iterable[Any], expected_count: int
    ) -> list[list[float]]:
        normalized = [[float(value) for value in vector] for vector in vectors]
        if len(normalized) != expected_count:
            raise RuntimeError("embedding provider returned an unexpected vector count")
        for vector in normalized:
            if len(vector) != self._dimension:
                raise RuntimeError("embedding provider returned an unexpected vector dimension")
            if not all(math.isfinite(value) for value in vector):
                raise RuntimeError("embedding provider returned a non-finite value")
            if not any(value != 0 for value in vector):
                raise RuntimeError("embedding provider returned a zero vector")
        return normalized
