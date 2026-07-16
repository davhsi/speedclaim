from fastapi import APIRouter, Request
from sqlalchemy.exc import SQLAlchemyError

from speedclaim_ai.contracts.policy_qa import (
    PolicyQaCitation,
    PolicyQaRequest,
    PolicyQaResponse,
)
from speedclaim_ai.errors import AppError
from speedclaim_ai.rag.answer_service import PolicyQaService
from speedclaim_ai.rag.errors import PolicyQaFailure
from speedclaim_ai.rag.models import PolicyQaCommand

router = APIRouter(prefix="/internal/v1", tags=["policy-qa"])


@router.post("/policy-qa", response_model=PolicyQaResponse)
async def answer_policy_question(
    request: PolicyQaRequest,
    http_request: Request,
) -> PolicyQaResponse:
    service: PolicyQaService | None = getattr(
        http_request.app.state,
        "policy_qa_service_override",
        None,
    )
    if service is None:
        service = await http_request.app.state.services.get_policy_qa_service()

    try:
        result = await service.answer(
            PolicyQaCommand(
                request_id=request.request_id,
                brochure_id=request.brochure_id,
                product_id=request.product_id,
                brochure_version=request.brochure_version,
                question=request.question,
            )
        )
    except PolicyQaFailure as failure:
        headers = None
        if failure.retry_after_seconds is not None:
            headers = {"Retry-After": str(failure.retry_after_seconds)}
        raise AppError(
            status_code=failure.status_code,
            code=failure.code,
            message=failure.message,
            headers=headers,
        ) from failure
    except SQLAlchemyError as failure:
        raise AppError(
            status_code=503,
            code="vector_database_unavailable",
            message="The AI vector database is temporarily unavailable.",
        ) from failure

    return PolicyQaResponse(
        requestId=result.request_id,
        answer=result.answer,
        evidenceStatus=result.evidence_status,
        brochureVersion=result.brochure_version,
        citations=[
            PolicyQaCitation(
                index=citation.index,
                pageNumber=citation.page_number,
                sectionTitle=citation.section_title,
                clauseReference=citation.clause_reference,
                excerpt=citation.excerpt,
            )
            for citation in result.citations
        ],
        promptVersion=result.prompt_version,
        provider=result.provider,
        model=result.model,
    )
