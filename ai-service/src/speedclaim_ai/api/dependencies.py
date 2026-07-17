import asyncio

from sqlalchemy.ext.asyncio import AsyncEngine

from speedclaim_ai.config.settings import Settings
from speedclaim_ai.database.session import create_database_engine, create_session_factory
from speedclaim_ai.errors import AppError
from speedclaim_ai.providers.chat.anthropic_gateway import (
    AnthropicGatewayChatProvider,
)
from speedclaim_ai.providers.chat.base import ChatProvider
from speedclaim_ai.providers.embeddings.local import FastEmbedProvider
from speedclaim_ai.providers.storage.azure_blob import AzureBlobBrochureReader
from speedclaim_ai.providers.storage.base import BrochureReader
from speedclaim_ai.providers.storage.local import LocalBrochureReader
from speedclaim_ai.rag.chunker import HierarchicalChunker
from speedclaim_ai.rag.answer_service import PolicyQaService
from speedclaim_ai.rag.ingestion_service import BrochureIngestionService
from speedclaim_ai.rag.pdf_parser import PdfParser
from speedclaim_ai.rag.retrieval_service import RetrievalService
from speedclaim_ai.repositories.pgvector_repository import PgVectorRepository
from speedclaim_ai.speedy import SpeedyService


class ServiceContainer:
    def __init__(self, settings: Settings) -> None:
        self._settings = settings
        self._lock = asyncio.Lock()
        self._engine: AsyncEngine | None = None
        self._repository: PgVectorRepository | None = None
        self._embedding_provider: FastEmbedProvider | None = None
        self._brochure_reader: BrochureReader | None = None
        self._ingestion_service: BrochureIngestionService | None = None
        self._chat_provider: ChatProvider | None = None
        self._policy_qa_service: PolicyQaService | None = None
        self._speedy_service: SpeedyService | None = None

    async def get_ingestion_service(self) -> BrochureIngestionService:
        if self._ingestion_service is not None:
            return self._ingestion_service
        if self._settings.vector_connection_string is None:
            raise AppError(
                status_code=503,
                code="vector_database_not_configured",
                message="The AI vector database is not configured.",
            )

        async with self._lock:
            if self._ingestion_service is None:
                self._ensure_vector_dependencies()
                assert self._repository is not None
                assert self._embedding_provider is not None
                if self._settings.storage_provider == "Local":
                    self._brochure_reader = LocalBrochureReader(
                        self._settings.local_brochure_root
                    )
                else:
                    assert self._settings.azure_blob_connection_string is not None
                    self._brochure_reader = AzureBlobBrochureReader(
                        connection_string=(
                            self._settings.azure_blob_connection_string.get_secret_value()
                        ),
                        container_name=self._settings.azure_blob_container_name,
                    )
                self._ingestion_service = BrochureIngestionService(
                    brochure_reader=self._brochure_reader,
                    parser=PdfParser(
                        max_size_bytes=self._settings.pdf_max_size_bytes,
                        max_pages=self._settings.pdf_max_pages,
                        minimum_text_characters=(
                            self._settings.pdf_min_text_characters
                        ),
                    ),
                    chunker=HierarchicalChunker(
                        parent_max_characters=(
                            self._settings.parent_chunk_max_characters
                        ),
                        child_max_characters=(
                            self._settings.child_chunk_max_characters
                        ),
                        child_overlap_characters=(
                            self._settings.child_chunk_overlap_characters
                        ),
                    ),
                    embedding_provider=self._embedding_provider,
                    repository=self._repository,
                    max_pdf_size_bytes=self._settings.pdf_max_size_bytes,
                )
        return self._ingestion_service

    async def get_policy_qa_service(self) -> PolicyQaService:
        if self._policy_qa_service is not None:
            return self._policy_qa_service
        if self._settings.vector_connection_string is None:
            raise AppError(
                status_code=503,
                code="vector_database_not_configured",
                message="The AI vector database is not configured.",
            )
        if self._settings.anthropic_base_url is None or self._settings.anthropic_auth_token is None:
            raise AppError(
                status_code=503,
                code="chat_provider_not_configured",
                message="The policy answer provider is not configured.",
            )

        async with self._lock:
            if self._policy_qa_service is None:
                self._ensure_vector_dependencies()
                assert self._repository is not None
                assert self._embedding_provider is not None
                self._ensure_chat_provider()
                assert self._chat_provider is not None
                retrieval = RetrievalService(
                    embedding_provider=self._embedding_provider,
                    repository=self._repository,
                    question_max_characters=(
                        self._settings.policy_qa_question_max_characters
                    ),
                    child_limit=self._settings.retrieval_child_limit,
                    minimum_similarity=self._settings.retrieval_min_similarity,
                    max_parent_chunks=self._settings.retrieval_max_parent_chunks,
                    max_context_characters=(
                        self._settings.retrieval_max_context_characters
                    ),
                )
                self._policy_qa_service = PolicyQaService(
                    repository=self._repository,
                    retrieval_service=retrieval,
                    chat_provider=self._chat_provider,
                    prompt_version=self._settings.policy_qa_prompt_version,
                )
        return self._policy_qa_service

    async def get_speedy_service(self) -> SpeedyService:
        if self._speedy_service is not None:
            return self._speedy_service
        if (
            self._settings.anthropic_base_url is None
            or self._settings.anthropic_auth_token is None
        ):
            raise AppError(
                status_code=503,
                code="chat_provider_not_configured",
                message="The Speedy answer provider is not configured.",
            )
        async with self._lock:
            if self._speedy_service is None:
                self._ensure_chat_provider()
                assert self._chat_provider is not None
                self._speedy_service = SpeedyService(self._chat_provider)
        return self._speedy_service

    def _ensure_chat_provider(self) -> None:
        if self._chat_provider is not None:
            return
        assert self._settings.anthropic_base_url is not None
        assert self._settings.anthropic_auth_token is not None
        self._chat_provider = AnthropicGatewayChatProvider(
            auth_token=self._settings.anthropic_auth_token.get_secret_value(),
            model=self._settings.anthropic_chat_model,
            base_url=self._settings.anthropic_base_url,
            output_mode=self._settings.anthropic_output_mode,
            timeout_seconds=self._settings.chat_timeout_seconds,
            max_attempts=self._settings.chat_max_attempts,
            max_output_tokens=self._settings.chat_max_output_tokens,
        )

    def _ensure_vector_dependencies(self) -> None:
        if self._repository is not None and self._embedding_provider is not None:
            return
        assert self._settings.vector_connection_string is not None
        connection_string = self._settings.vector_connection_string.get_secret_value()
        self._engine = create_database_engine(connection_string)
        self._repository = PgVectorRepository(
            create_session_factory(self._engine),
            dimension=self._settings.embedding_dimension,
        )
        self._embedding_provider = FastEmbedProvider(
            model_name=self._settings.embedding_model,
            dimension=self._settings.embedding_dimension,
            cache_dir=self._settings.embedding_cache_dir,
            threads=self._settings.embedding_threads,
        )

    async def close(self) -> None:
        if self._chat_provider is not None:
            await self._chat_provider.close()
        if isinstance(self._brochure_reader, AzureBlobBrochureReader):
            await self._brochure_reader.close()
        if self._engine is not None:
            await self._engine.dispose()
