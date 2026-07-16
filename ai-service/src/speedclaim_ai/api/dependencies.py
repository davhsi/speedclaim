import asyncio

from sqlalchemy.ext.asyncio import AsyncEngine

from speedclaim_ai.config.settings import Settings
from speedclaim_ai.database.session import create_database_engine, create_session_factory
from speedclaim_ai.errors import AppError
from speedclaim_ai.providers.embeddings.local import FastEmbedProvider
from speedclaim_ai.providers.storage.azure_blob import AzureBlobBrochureReader
from speedclaim_ai.providers.storage.base import BrochureReader
from speedclaim_ai.providers.storage.local import LocalBrochureReader
from speedclaim_ai.rag.chunker import HierarchicalChunker
from speedclaim_ai.rag.ingestion_service import BrochureIngestionService
from speedclaim_ai.rag.pdf_parser import PdfParser
from speedclaim_ai.repositories.pgvector_repository import PgVectorRepository


class ServiceContainer:
    def __init__(self, settings: Settings) -> None:
        self._settings = settings
        self._lock = asyncio.Lock()
        self._engine: AsyncEngine | None = None
        self._brochure_reader: BrochureReader | None = None
        self._ingestion_service: BrochureIngestionService | None = None

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
                connection_string = (
                    self._settings.vector_connection_string.get_secret_value()
                )
                self._engine = create_database_engine(connection_string)
                repository = PgVectorRepository(
                    create_session_factory(self._engine),
                    dimension=self._settings.embedding_dimension,
                )
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
                    embedding_provider=FastEmbedProvider(
                        model_name=self._settings.embedding_model,
                        dimension=self._settings.embedding_dimension,
                        cache_dir=self._settings.embedding_cache_dir,
                        threads=self._settings.embedding_threads,
                    ),
                    repository=repository,
                    max_pdf_size_bytes=self._settings.pdf_max_size_bytes,
                )
        return self._ingestion_service

    async def close(self) -> None:
        if isinstance(self._brochure_reader, AzureBlobBrochureReader):
            await self._brochure_reader.close()
        if self._engine is not None:
            await self._engine.dispose()
