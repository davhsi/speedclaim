from datetime import datetime
from uuid import UUID, uuid4

from pgvector.sqlalchemy import Vector
from sqlalchemy import CheckConstraint, DateTime, ForeignKey, Index, Integer, String, Text, func
from sqlalchemy.dialects.postgresql import UUID as PostgresUUID
from sqlalchemy.orm import Mapped, mapped_column

from speedclaim_ai.config.settings import EMBEDDING_DIMENSION
from speedclaim_ai.database.base import Base


class RagDocument(Base):
    __tablename__ = "rag_documents"
    __table_args__ = (
        CheckConstraint("page_count > 0", name="ck_rag_documents_page_count_positive"),
        CheckConstraint(
            "embedding_dimension > 0",
            name="ck_rag_documents_embedding_dimension_positive",
        ),
    )

    id: Mapped[UUID] = mapped_column(
        PostgresUUID(as_uuid=True), primary_key=True, default=uuid4
    )
    brochure_id: Mapped[UUID] = mapped_column(
        PostgresUUID(as_uuid=True), nullable=False, unique=True
    )
    product_id: Mapped[UUID] = mapped_column(PostgresUUID(as_uuid=True), nullable=False)
    brochure_version: Mapped[str] = mapped_column(String(128), nullable=False)
    content_hash: Mapped[str] = mapped_column(String(64), nullable=False)
    page_count: Mapped[int] = mapped_column(Integer, nullable=False)
    embedding_provider: Mapped[str] = mapped_column(String(64), nullable=False)
    embedding_model: Mapped[str] = mapped_column(String(255), nullable=False)
    embedding_dimension: Mapped[int] = mapped_column(Integer, nullable=False)
    indexed_at: Mapped[datetime] = mapped_column(
        DateTime(timezone=True), nullable=False, server_default=func.now()
    )


class RagChunk(Base):
    __tablename__ = "rag_chunks"
    __table_args__ = (
        CheckConstraint("page_number > 0", name="ck_rag_chunks_page_number_positive"),
        CheckConstraint("chunk_index >= 0", name="ck_rag_chunks_chunk_index_nonnegative"),
        CheckConstraint("token_count > 0", name="ck_rag_chunks_token_count_positive"),
        Index(
            "ix_rag_chunks_document_page_chunk",
            "document_id",
            "page_number",
            "chunk_index",
        ),
        Index("uq_rag_chunks_document_chunk_index", "document_id", "chunk_index", unique=True),
    )

    id: Mapped[UUID] = mapped_column(
        PostgresUUID(as_uuid=True), primary_key=True, default=uuid4
    )
    document_id: Mapped[UUID] = mapped_column(
        PostgresUUID(as_uuid=True),
        ForeignKey("rag_documents.id", ondelete="CASCADE"),
        nullable=False,
    )
    parent_chunk_id: Mapped[UUID | None] = mapped_column(
        PostgresUUID(as_uuid=True),
        ForeignKey("rag_chunks.id", ondelete="SET NULL"),
        nullable=True,
    )
    page_number: Mapped[int] = mapped_column(Integer, nullable=False)
    section_title: Mapped[str | None] = mapped_column(String(512), nullable=True)
    clause_reference: Mapped[str | None] = mapped_column(String(128), nullable=True)
    chunk_index: Mapped[int] = mapped_column(Integer, nullable=False)
    content: Mapped[str] = mapped_column(Text, nullable=False)
    content_hash: Mapped[str] = mapped_column(String(64), nullable=False)
    token_count: Mapped[int] = mapped_column(Integer, nullable=False)
    embedding: Mapped[list[float] | None] = mapped_column(
        Vector(EMBEDDING_DIMENSION), nullable=True
    )


class RagIngestionRun(Base):
    __tablename__ = "rag_ingestion_runs"
    __table_args__ = (
        CheckConstraint(
            "status IN ('Processing', 'Succeeded', 'Failed')",
            name="ck_rag_ingestion_runs_status",
        ),
        CheckConstraint(
            "page_count IS NULL OR page_count > 0",
            name="ck_rag_ingestion_runs_page_count_positive",
        ),
        CheckConstraint(
            "chunk_count IS NULL OR chunk_count >= 0",
            name="ck_rag_ingestion_runs_chunk_count_nonnegative",
        ),
        Index("ix_rag_ingestion_runs_brochure_started", "brochure_id", "started_at"),
    )

    id: Mapped[UUID] = mapped_column(
        PostgresUUID(as_uuid=True), primary_key=True, default=uuid4
    )
    brochure_id: Mapped[UUID] = mapped_column(PostgresUUID(as_uuid=True), nullable=False)
    status: Mapped[str] = mapped_column(String(32), nullable=False)
    started_at: Mapped[datetime] = mapped_column(
        DateTime(timezone=True), nullable=False, server_default=func.now()
    )
    completed_at: Mapped[datetime | None] = mapped_column(DateTime(timezone=True), nullable=True)
    page_count: Mapped[int | None] = mapped_column(Integer, nullable=True)
    chunk_count: Mapped[int | None] = mapped_column(Integer, nullable=True)
    error_code: Mapped[str | None] = mapped_column(String(128), nullable=True)
    error_message_redacted: Mapped[str | None] = mapped_column(String(1024), nullable=True)
