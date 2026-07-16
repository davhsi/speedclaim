from fastapi import APIRouter, Request

from speedclaim_ai.contracts.brochure import (
    BrochureIngestionRequest,
    BrochureIngestionResponse,
)
from speedclaim_ai.errors import AppError
from speedclaim_ai.rag.errors import IngestionFailure
from speedclaim_ai.rag.ingestion_service import BrochureIngestionService
from speedclaim_ai.rag.models import IngestionCommand

router = APIRouter(prefix="/internal/v1/brochures", tags=["brochure-ingestion"])


@router.post("/ingest", response_model=BrochureIngestionResponse)
async def ingest_brochure(
    request: BrochureIngestionRequest,
    http_request: Request,
) -> BrochureIngestionResponse:
    service: BrochureIngestionService | None = getattr(
        http_request.app.state, "ingestion_service_override", None
    )
    if service is None:
        service = await http_request.app.state.services.get_ingestion_service()
    try:
        result = await service.ingest(
            IngestionCommand(
                request_id=request.request_id,
                brochure_id=request.brochure_id,
                product_id=request.product_id,
                brochure_version=request.brochure_version,
                blob_path=request.blob_path,
                content_hash=request.content_hash,
            )
        )
    except IngestionFailure as failure:
        raise AppError(
            status_code=failure.status_code,
            code=failure.code,
            message=failure.message,
        ) from failure

    return BrochureIngestionResponse(
        requestId=result.request_id,
        brochureId=request.brochure_id,
        documentId=result.document_id,
        status=result.status,
        pageCount=result.page_count,
        parentChunkCount=result.parent_chunk_count,
        childChunkCount=result.child_chunk_count,
        embeddingProvider=result.embedding_provider,
        embeddingModel=result.embedding_model,
        embeddingDimension=result.embedding_dimension,
    )
