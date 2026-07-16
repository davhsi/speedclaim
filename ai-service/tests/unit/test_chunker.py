import hashlib
from pathlib import Path

from speedclaim_ai.rag.chunker import HierarchicalChunker
from speedclaim_ai.rag.pdf_parser import PdfParser

FIXTURE = (
    Path(__file__).resolve().parents[3]
    / "output/pdf/speedclaim-arogya-shield-plus-synthetic-brochure-v1.pdf"
)


def test_synthetic_brochure_produces_page_aware_parent_child_chunks() -> None:
    parsed = PdfParser(
        max_size_bytes=10_485_760,
        max_pages=300,
        minimum_text_characters=100,
    ).parse(FIXTURE.read_bytes())
    chunks = HierarchicalChunker(
        parent_max_characters=6_000,
        child_max_characters=1_200,
        child_overlap_characters=150,
    ).create_chunks(parsed)

    parents = [chunk for chunk in chunks if chunk.is_parent]
    children = [chunk for chunk in chunks if not chunk.is_parent]
    parent_by_id = {chunk.id: chunk for chunk in parents}

    assert len(parents) == 13
    assert len(children) == 95
    assert [chunk.chunk_index for chunk in chunks] == list(range(len(chunks)))
    assert all(child.parent_chunk_id in parent_by_id for child in children)
    assert all(
        parent_by_id[child.parent_chunk_id].page_number == child.page_number
        for child in children
    )
    assert all(len(child.content) <= 1_200 for child in children)
    assert all(len(parent.content) <= 6_000 for parent in parents)
    assert all(
        child.clause_reference is None for child in children if child.page_number == 1
    )
    assert all(
        chunk.content_hash == hashlib.sha256(chunk.content.encode()).hexdigest()
        for chunk in chunks
    )

    waiting_period = next(chunk for chunk in children if chunk.clause_reference == "5.1")
    assert waiting_period.page_number == 6
    assert waiting_period.section_title == "Waiting periods"
    assert "initial waiting period" in waiting_period.content.lower()
    assert all("SPEEDCLAIM AROGYA SHIELD PLUS" not in chunk.content for chunk in chunks)
