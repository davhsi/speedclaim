import asyncio
import hashlib
import json
import os
from pathlib import Path
from uuid import uuid4

import pytest
from alembic import command
from alembic.config import Config

from speedclaim_ai.config.settings import Settings
from speedclaim_ai.database.session import create_database_engine, create_session_factory
from speedclaim_ai.providers.chat.anthropic_gateway import AnthropicGatewayChatProvider
from speedclaim_ai.providers.chat.base import ChatProvider
from speedclaim_ai.providers.embeddings.local import FastEmbedProvider
from speedclaim_ai.providers.storage.local import LocalBrochureReader
from speedclaim_ai.rag.answer_service import PolicyQaService
from speedclaim_ai.rag.chunker import HierarchicalChunker
from speedclaim_ai.rag.ingestion_service import BrochureIngestionService
from speedclaim_ai.rag.errors import PolicyQaFailure
from speedclaim_ai.rag.models import IngestionCommand, PolicyQaCommand
from speedclaim_ai.rag.pdf_parser import PdfParser
from speedclaim_ai.rag.retrieval_service import RetrievalService
from speedclaim_ai.repositories.pgvector_repository import PgVectorRepository

pytestmark = [pytest.mark.anyio, pytest.mark.database, pytest.mark.live]

ROOT = Path(__file__).resolve().parents[3]
FIXTURE_PATH = ROOT / "output/pdf/speedclaim-arogya-shield-plus-synthetic-brochure-v1.pdf"
QUESTIONS = (
    "What is the initial waiting period?",
    "How soon after buying the policy can I claim for an illness?",
    "Is there a waiting period when the policy begins?",
    "Does the initial waiting period apply to accidents?",
    "When does regular illness coverage start?",
)


async def answer_with_rate_limit_retry(
    policy_qa: PolicyQaService,
    command_model: PolicyQaCommand,
):
    for attempt in range(1, 4):
        try:
            return await policy_qa.answer(command_model)
        except PolicyQaFailure as failure:
            if failure.code != "chat_provider_rate_limited" or attempt == 3:
                raise
            retry_after = failure.retry_after_seconds or 10
            delay = max(1, min(retry_after + 1, 45))
            print(
                json.dumps(
                    {
                        "event": "rateLimitRetry",
                        "attempt": attempt,
                        "retryAfterSeconds": failure.retry_after_seconds,
                        "delaySeconds": delay,
                    }
                )
            )
            await asyncio.sleep(delay)
    raise AssertionError("unreachable")


@pytest.fixture
def anyio_backend() -> str:
    return "asyncio"


@pytest.fixture(scope="module")
def live_settings() -> Settings:
    if os.getenv("AI_RUN_LIVE_POLICY_QA") != "1":
        pytest.skip("set AI_RUN_LIVE_POLICY_QA=1 to run the live policy-Q&A evaluation")
    connection_string = os.getenv("AI_TEST_VECTOR_CONNECTION_STRING")
    if not connection_string:
        pytest.skip("AI_TEST_VECTOR_CONNECTION_STRING is not configured")

    os.environ["AI__VectorConnectionString"] = connection_string
    settings = Settings()
    if settings.anthropic_base_url is None or settings.anthropic_auth_token is None:
        pytest.skip("ignored Anthropic gateway credentials are required for the live evaluation")
    command.upgrade(Config("alembic.ini"), "head")
    return settings


def _create_chat_provider(settings: Settings) -> ChatProvider:
    assert settings.anthropic_base_url is not None
    assert settings.anthropic_auth_token is not None
    return AnthropicGatewayChatProvider(
        auth_token=settings.anthropic_auth_token.get_secret_value(),
        model=settings.anthropic_chat_model,
        base_url=settings.anthropic_base_url,
        output_mode=settings.anthropic_output_mode,
        timeout_seconds=settings.chat_timeout_seconds,
        max_attempts=1,
        max_output_tokens=settings.chat_max_output_tokens,
    )


async def test_natural_waiting_period_questions_remain_grounded(
    live_settings: Settings,
) -> None:
    assert live_settings.vector_connection_string is not None
    engine = create_database_engine(
        live_settings.vector_connection_string.get_secret_value()
    )
    repository = PgVectorRepository(
        create_session_factory(engine),
        dimension=live_settings.embedding_dimension,
    )
    embedding = FastEmbedProvider(
        model_name=live_settings.embedding_model,
        dimension=live_settings.embedding_dimension,
        cache_dir=live_settings.embedding_cache_dir,
        threads=live_settings.embedding_threads,
    )
    chat = _create_chat_provider(live_settings)
    retrieval = RetrievalService(
        embedding_provider=embedding,
        repository=repository,
        question_max_characters=live_settings.policy_qa_question_max_characters,
        child_limit=live_settings.retrieval_child_limit,
        minimum_similarity=live_settings.retrieval_min_similarity,
        max_parent_chunks=live_settings.retrieval_max_parent_chunks,
        max_context_characters=live_settings.retrieval_max_context_characters,
    )
    policy_qa = PolicyQaService(
        repository=repository,
        retrieval_service=retrieval,
        chat_provider=chat,
        prompt_version=live_settings.policy_qa_prompt_version,
    )
    ingestion = BrochureIngestionService(
        brochure_reader=LocalBrochureReader(ROOT),
        parser=PdfParser(
            max_size_bytes=live_settings.pdf_max_size_bytes,
            max_pages=live_settings.pdf_max_pages,
            minimum_text_characters=live_settings.pdf_min_text_characters,
        ),
        chunker=HierarchicalChunker(
            parent_max_characters=live_settings.parent_chunk_max_characters,
            child_max_characters=live_settings.child_chunk_max_characters,
            child_overlap_characters=live_settings.child_chunk_overlap_characters,
        ),
        embedding_provider=embedding,
        repository=repository,
        max_pdf_size_bytes=live_settings.pdf_max_size_bytes,
    )
    brochure_id = uuid4()
    product_id = uuid4()
    pdf_bytes = FIXTURE_PATH.read_bytes()

    try:
        ingested = await ingestion.ingest(
            IngestionCommand(
                request_id=uuid4(),
                brochure_id=brochure_id,
                product_id=product_id,
                brochure_version="1.0",
                blob_path=str(FIXTURE_PATH.relative_to(ROOT)),
                content_hash=hashlib.sha256(pdf_bytes).hexdigest(),
            )
        )
        assert ingested.page_count == 13
        assert ingested.parent_chunk_count == 13
        assert ingested.child_chunk_count == 95

        configured_limit = int(os.getenv("AI_LIVE_POLICY_QA_QUESTION_LIMIT", len(QUESTIONS)))
        if not 1 <= configured_limit <= len(QUESTIONS):
            raise AssertionError("live question limit must be between 1 and 5")
        questions = QUESTIONS[:configured_limit]
        provider_call_limit = 4 * len(questions)
        evaluation_rows = []
        for question in questions:
            query_embedding = await asyncio.to_thread(embedding.embed_query, question)
            ranked = await repository.search(
                brochure_id,
                query_embedding,
                limit=live_settings.retrieval_child_limit,
            )
            ranking = [
                {
                    "rank": rank,
                    "score": round(match.score, 4),
                    "page": match.page_number,
                    "section": match.section_title,
                    "clause": match.clause_reference,
                    "chunkIndex": match.chunk_index,
                }
                for rank, match in enumerate(ranked, start=1)
            ]
            print(
                json.dumps(
                    {"question": question, "retrievalRanking": ranking},
                    ensure_ascii=False,
                )
            )
            try:
                answer = await answer_with_rate_limit_retry(
                    policy_qa,
                    PolicyQaCommand(
                        request_id=uuid4(),
                        brochure_id=brochure_id,
                        product_id=product_id,
                        brochure_version="1.0",
                        question=question,
                    )
                )
            except PolicyQaFailure:
                if isinstance(chat, AnthropicGatewayChatProvider):
                    print(
                        json.dumps(
                            {
                                "validationFailureCategory": chat.last_validation_failure,
                                "messagesApiCalls": chat.request_count,
                                "inputTokens": chat.input_tokens_used,
                                "outputTokens": chat.output_tokens_used,
                            }
                        )
                    )
                raise

            assert answer.evidence_status == "Grounded"
            assert answer.citations
            assert any(citation.page_number == 6 for citation in answer.citations)
            assert all(
                f"[{citation.index}]" in answer.answer
                for citation in answer.citations
            )
            if "accident" in question.casefold():
                assert "accident" in answer.answer.casefold()
                assert any(
                    phrase in answer.answer.casefold()
                    for phrase in ("does not apply", "doesn't apply", "not apply", "exempt")
                )
            else:
                assert "30" in answer.answer or "thirty" in answer.answer.casefold()

            evaluation_rows.append(
                {
                    "question": question,
                    "citations": [
                        {
                            "index": citation.index,
                            "page": citation.page_number,
                            "section": citation.section_title,
                            "clause": citation.clause_reference,
                        }
                        for citation in answer.citations
                    ],
                    "retrievalRanking": ranking,
                }
            )

        if isinstance(chat, AnthropicGatewayChatProvider):
            assert chat.request_count <= provider_call_limit
            usage = {
                "messagesApiCalls": chat.request_count,
                "callLimit": provider_call_limit,
                "inputTokens": chat.input_tokens_used,
                "outputTokens": chat.output_tokens_used,
            }
        else:
            usage = {"messagesApiCalls": None, "callLimit": None}
        print(
            json.dumps(
                {
                    "provider": chat.provider_name,
                    "model": chat.model_name,
                    "questionCount": len(questions),
                    "usage": usage,
                    "results": evaluation_rows,
                },
                ensure_ascii=False,
                indent=2,
            )
        )
    finally:
        await repository.delete_by_brochure_id(brochure_id)
        await chat.close()
        await engine.dispose()
