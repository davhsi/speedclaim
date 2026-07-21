from collections.abc import Iterable, Sequence
import stat

import pytest

from speedclaim_ai.config.settings import EMBEDDING_DIMENSION
from speedclaim_ai.providers.embeddings.local import FastEmbedProvider


def _vector(value: float = 1.0) -> list[float]:
    return [value] * EMBEDDING_DIMENSION


class FakeModel:
    def __init__(self, vectors: Sequence[Sequence[float]] | None = None) -> None:
        self.vectors = vectors
        self.passage_calls: list[list[str]] = []
        self.query_calls: list[list[str]] = []

    def passage_embed(self, texts: Sequence[str]) -> Iterable[Sequence[float]]:
        self.passage_calls.append(list(texts))
        return iter(self.vectors or [_vector(float(index + 1)) for index in range(len(texts))])

    def query_embed(self, texts: Sequence[str]) -> Iterable[Sequence[float]]:
        self.query_calls.append(list(texts))
        return iter(self.vectors or [_vector()])


def test_provider_is_lazy_and_uses_passage_and_query_modes() -> None:
    model = FakeModel()
    construction_count = 0

    def build_model() -> FakeModel:
        nonlocal construction_count
        construction_count += 1
        return model

    provider = FastEmbedProvider(model_factory=build_model)

    assert construction_count == 0
    assert provider.provider_name == "FastEmbed"
    document_vectors = provider.embed_documents([" first clause ", "second clause"])
    query_vector = provider.embed_query(" waiting period? ")

    assert construction_count == 1
    assert model.passage_calls == [["first clause", "second clause"]]
    assert model.query_calls == [["waiting period?"]]
    assert len(document_vectors) == 2
    assert len(query_vector) == EMBEDDING_DIMENSION


@pytest.mark.parametrize("text", ["", "   "])
def test_provider_rejects_blank_text(text: str) -> None:
    provider = FastEmbedProvider(model_factory=FakeModel)

    with pytest.raises(ValueError, match="must not be empty"):
        provider.embed_query(text)


@pytest.mark.parametrize(
    "vector",
    [
        [1.0],
        [0.0] * EMBEDDING_DIMENSION,
        [float("nan")] * EMBEDDING_DIMENSION,
    ],
)
def test_provider_rejects_invalid_model_output(vector: list[float]) -> None:
    provider = FastEmbedProvider(model_factory=lambda: FakeModel([vector]))

    with pytest.raises(RuntimeError, match="embedding provider"):
        provider.embed_query("valid text")


def test_provider_creates_a_private_model_cache(tmp_path) -> None:
    cache_dir = tmp_path / "models"
    provider = FastEmbedProvider(cache_dir=cache_dir, model_factory=FakeModel)

    provider._ensure_private_cache_dir()

    assert stat.S_IMODE(cache_dir.stat().st_mode) == 0o700
