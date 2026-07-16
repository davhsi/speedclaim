"""Create policy brochure RAG vector schema.

Revision ID: 20260715_0001
Revises:
Create Date: 2026-07-15
"""
from collections.abc import Sequence

from alembic import op
from pgvector.sqlalchemy import Vector
import sqlalchemy as sa
from sqlalchemy.dialects import postgresql

revision: str = "20260715_0001"
down_revision: str | None = None
branch_labels: str | Sequence[str] | None = None
depends_on: str | Sequence[str] | None = None


def upgrade() -> None:
    op.execute("CREATE EXTENSION IF NOT EXISTS vector")

    op.create_table(
        "rag_documents",
        sa.Column("id", postgresql.UUID(as_uuid=True), nullable=False),
        sa.Column("brochure_id", postgresql.UUID(as_uuid=True), nullable=False),
        sa.Column("product_id", postgresql.UUID(as_uuid=True), nullable=False),
        sa.Column("brochure_version", sa.String(length=128), nullable=False),
        sa.Column("content_hash", sa.String(length=64), nullable=False),
        sa.Column("page_count", sa.Integer(), nullable=False),
        sa.Column("embedding_provider", sa.String(length=64), nullable=False),
        sa.Column("embedding_model", sa.String(length=255), nullable=False),
        sa.Column("embedding_dimension", sa.Integer(), nullable=False),
        sa.Column(
            "indexed_at",
            sa.DateTime(timezone=True),
            server_default=sa.text("now()"),
            nullable=False,
        ),
        sa.CheckConstraint("page_count > 0", name="ck_rag_documents_page_count_positive"),
        sa.CheckConstraint(
            "embedding_dimension > 0",
            name="ck_rag_documents_embedding_dimension_positive",
        ),
        sa.PrimaryKeyConstraint("id"),
        sa.UniqueConstraint("brochure_id", name="uq_rag_documents_brochure_id"),
    )

    op.create_table(
        "rag_ingestion_runs",
        sa.Column("id", postgresql.UUID(as_uuid=True), nullable=False),
        sa.Column("brochure_id", postgresql.UUID(as_uuid=True), nullable=False),
        sa.Column("status", sa.String(length=32), nullable=False),
        sa.Column(
            "started_at",
            sa.DateTime(timezone=True),
            server_default=sa.text("now()"),
            nullable=False,
        ),
        sa.Column("completed_at", sa.DateTime(timezone=True), nullable=True),
        sa.Column("page_count", sa.Integer(), nullable=True),
        sa.Column("chunk_count", sa.Integer(), nullable=True),
        sa.Column("error_code", sa.String(length=128), nullable=True),
        sa.Column("error_message_redacted", sa.String(length=1024), nullable=True),
        sa.CheckConstraint(
            "status IN ('Processing', 'Succeeded', 'Failed')",
            name="ck_rag_ingestion_runs_status",
        ),
        sa.CheckConstraint(
            "page_count IS NULL OR page_count > 0",
            name="ck_rag_ingestion_runs_page_count_positive",
        ),
        sa.CheckConstraint(
            "chunk_count IS NULL OR chunk_count >= 0",
            name="ck_rag_ingestion_runs_chunk_count_nonnegative",
        ),
        sa.PrimaryKeyConstraint("id"),
    )
    op.create_index(
        "ix_rag_ingestion_runs_brochure_started",
        "rag_ingestion_runs",
        ["brochure_id", "started_at"],
    )

    op.create_table(
        "rag_chunks",
        sa.Column("id", postgresql.UUID(as_uuid=True), nullable=False),
        sa.Column("document_id", postgresql.UUID(as_uuid=True), nullable=False),
        sa.Column("parent_chunk_id", postgresql.UUID(as_uuid=True), nullable=True),
        sa.Column("page_number", sa.Integer(), nullable=False),
        sa.Column("section_title", sa.String(length=512), nullable=True),
        sa.Column("clause_reference", sa.String(length=128), nullable=True),
        sa.Column("chunk_index", sa.Integer(), nullable=False),
        sa.Column("content", sa.Text(), nullable=False),
        sa.Column("content_hash", sa.String(length=64), nullable=False),
        sa.Column("token_count", sa.Integer(), nullable=False),
        sa.Column("embedding", Vector(dim=384), nullable=True),
        sa.CheckConstraint("page_number > 0", name="ck_rag_chunks_page_number_positive"),
        sa.CheckConstraint("chunk_index >= 0", name="ck_rag_chunks_chunk_index_nonnegative"),
        sa.CheckConstraint("token_count > 0", name="ck_rag_chunks_token_count_positive"),
        sa.ForeignKeyConstraint(
            ["document_id"], ["rag_documents.id"], ondelete="CASCADE"
        ),
        sa.ForeignKeyConstraint(
            ["parent_chunk_id"], ["rag_chunks.id"], ondelete="SET NULL"
        ),
        sa.PrimaryKeyConstraint("id"),
    )
    op.create_index(
        "ix_rag_chunks_document_page_chunk",
        "rag_chunks",
        ["document_id", "page_number", "chunk_index"],
    )
    op.create_index(
        "uq_rag_chunks_document_chunk_index",
        "rag_chunks",
        ["document_id", "chunk_index"],
        unique=True,
    )


def downgrade() -> None:
    op.drop_index("uq_rag_chunks_document_chunk_index", table_name="rag_chunks")
    op.drop_index("ix_rag_chunks_document_page_chunk", table_name="rag_chunks")
    op.drop_table("rag_chunks")
    op.drop_index(
        "ix_rag_ingestion_runs_brochure_started", table_name="rag_ingestion_runs"
    )
    op.drop_table("rag_ingestion_runs")
    op.drop_table("rag_documents")
    # The vector extension can be shared by other schemas, so downgrade intentionally leaves it.
