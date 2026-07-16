import hashlib
from dataclasses import replace
from uuid import uuid4

import pytest

from speedclaim_ai.config.settings import DEFAULT_EMBEDDING_MODEL, EMBEDDING_DIMENSION
from speedclaim_ai.rag.errors import IngestionFailure
from speedclaim_ai.rag.ingestion_service import BrochureIngestionService
from speedclaim_ai.rag.models import (
    IngestionCommand,
    ParsedPage,
    ParsedPdf,
    PreparedChunk,
)
from speedclaim_ai.repositories.vector_repository import (
    DocumentRecord,
    StoreResult,
)

pytestmark = pytest.mark.anyio


@pytest.fixture
def anyio_backend() -> str:
    return "asyncio"


class FakeReader:
    def __init__(self, outcomes) -> None:
        self.outcomes = list(outcomes)
        self.calls = 0

    async def read_bytes(self, _blob_path: str, *, max_bytes: int) -> bytes:
        self.calls += 1
        outcome = self.outcomes.pop(0)
        if isinstance(outcome, Exception):
            raise outcome
        assert len(outcome) <= max_bytes
        return outcome


class FakeParser:
    def parse(self, _data: bytes) -> ParsedPdf:
        return ParsedPdf(
            pages=(ParsedPage(page_number=1, lines=("SECTION 1", "Coverage")),),
            removed_boilerplate=(),
        )


class FakeChunker:
    def create_chunks(self, _parsed: ParsedPdf) -> list[PreparedChunk]:
        parent_id = uuid4()
        content = "1.1 Covered treatment\nEligible hospital treatment is covered."
        return [
            PreparedChunk(
                id=parent_id,
                parent_chunk_id=None,
                page_number=1,
                section_title="Coverage",
                clause_reference=None,
                chunk_index=0,
                content=content,
                content_hash=hashlib.sha256(content.encode()).hexdigest(),
                token_count=8,
                is_parent=True,
            ),
            PreparedChunk(
                id=uuid4(),
                parent_chunk_id=parent_id,
                page_number=1,
                section_title="Coverage",
                clause_reference="1.1",
                chunk_index=1,
                content=content,
                content_hash=hashlib.sha256(content.encode()).hexdigest(),
                token_count=8,
                is_parent=False,
            ),
        ]


class FakeEmbeddingProvider:
    provider_name = "FakeEmbed"
    model_name = DEFAULT_EMBEDDING_MODEL
    dimension = EMBEDDING_DIMENSION

    def __init__(self) -> None:
        self.calls = 0

    def embed_documents(self, texts) -> list[list[float]]:
        self.calls += 1
        return [[1.0] * EMBEDDING_DIMENSION for _ in texts]


class FakeRepository:
    def __init__(self) -> None:
        self.document: DocumentRecord | None = None
        self.store_calls = 0
        self.runs: dict = {}

    async def start_ingestion_run(self, brochure_id):
        run_id = uuid4()
        self.runs[run_id] = {"brochure_id": brochure_id, "status": "Processing"}
        return run_id

    async def get_document_by_brochure_id(self, _brochure_id):
        return self.document

    async def store_document(self, document, chunks):
        self.store_calls += 1
        document_id = uuid4()
        self.document = DocumentRecord(
            document_id=document_id,
            brochure_id=document.brochure_id,
            product_id=document.product_id,
            brochure_version=document.brochure_version,
            content_hash=document.content_hash,
            page_count=document.page_count,
            parent_chunk_count=sum(chunk.embedding is None for chunk in chunks),
            child_chunk_count=sum(chunk.embedding is not None for chunk in chunks),
            embedding_provider=document.embedding_provider,
            embedding_model=document.embedding_model,
            embedding_dimension=document.embedding_dimension,
        )
        return StoreResult(document_id=document_id, created=True)

    async def complete_ingestion_run(self, run_id, *, page_count, chunk_count):
        self.runs[run_id].update(
            status="Succeeded", page_count=page_count, chunk_count=chunk_count
        )

    async def fail_ingestion_run(
        self, run_id, *, error_code, error_message_redacted
    ):
        self.runs[run_id].update(
            status="Failed",
            error_code=error_code,
            error_message_redacted=error_message_redacted,
        )


def _command(data: bytes) -> IngestionCommand:
    return IngestionCommand(
        request_id=uuid4(),
        brochure_id=uuid4(),
        product_id=uuid4(),
        brochure_version="1",
        blob_path="products/brochure.pdf",
        content_hash=hashlib.sha256(data).hexdigest(),
    )


def _service(reader: FakeReader, repository: FakeRepository):
    embedding = FakeEmbeddingProvider()
    return (
        BrochureIngestionService(
            brochure_reader=reader,
            parser=FakeParser(),
            chunker=FakeChunker(),
            embedding_provider=embedding,
            repository=repository,
            max_pdf_size_bytes=1_024,
        ),
        embedding,
    )


async def test_successful_ingestion_is_idempotent_before_read_parse_and_embed() -> None:
    data = b"%PDF-test-content"
    reader = FakeReader([data])
    repository = FakeRepository()
    service, embedding = _service(reader, repository)
    command = _command(data)

    created = await service.ingest(command)
    no_op = await service.ingest(replace(command, request_id=uuid4()))

    assert created.status == "Succeeded"
    assert no_op.status == "NoOp"
    assert no_op.document_id == created.document_id
    assert reader.calls == 1
    assert embedding.calls == 1
    assert repository.store_calls == 1
    assert [run["status"] for run in repository.runs.values()] == [
        "Succeeded",
        "Succeeded",
    ]


async def test_immutable_metadata_conflict_records_failure_without_reading_again() -> None:
    data = b"%PDF-test-content"
    reader = FakeReader([data])
    repository = FakeRepository()
    service, _ = _service(reader, repository)
    command = _command(data)
    await service.ingest(command)

    with pytest.raises(IngestionFailure) as failure:
        await service.ingest(replace(command, content_hash="f" * 64))

    assert failure.value.code == "brochure_immutable_conflict"
    assert reader.calls == 1
    assert list(repository.runs.values())[-1]["status"] == "Failed"


async def test_failed_read_records_failure_and_a_retry_can_succeed() -> None:
    data = b"%PDF-retry-content"
    reader = FakeReader(
        [
            IngestionFailure(
                code="brochure_not_found",
                message="The brochure file was not found.",
                status_code=404,
            ),
            data,
        ]
    )
    repository = FakeRepository()
    service, _ = _service(reader, repository)
    command = _command(data)

    with pytest.raises(IngestionFailure) as first_attempt:
        await service.ingest(command)
    second_attempt = await service.ingest(replace(command, request_id=uuid4()))

    assert first_attempt.value.code == "brochure_not_found"
    assert second_attempt.status == "Succeeded"
    runs = list(repository.runs.values())
    assert runs[0] == {
        "brochure_id": command.brochure_id,
        "status": "Failed",
        "error_code": "brochure_not_found",
        "error_message_redacted": "The brochure file was not found.",
    }
    assert runs[1]["status"] == "Succeeded"


async def test_content_hash_mismatch_records_a_retryable_failure() -> None:
    data = b"%PDF-actual"
    reader = FakeReader([data])
    repository = FakeRepository()
    service, _ = _service(reader, repository)
    command = replace(_command(data), content_hash="a" * 64)

    with pytest.raises(IngestionFailure) as failure:
        await service.ingest(command)

    assert failure.value.code == "brochure_hash_mismatch"
    assert list(repository.runs.values())[-1]["status"] == "Failed"
    assert repository.store_calls == 0
