from fastapi import APIRouter, Request

from speedclaim_ai.contracts.workspace import WorkspaceRequest, WorkspaceResponse
from speedclaim_ai.errors import AppError
from speedclaim_ai.providers.chat.base import (
    ChatProviderError,
    ChatProviderRateLimited,
    ChatProviderTimeout,
    ChatProviderUnavailable,
)
from speedclaim_ai.workspace import WorkspaceService

router = APIRouter(prefix="/internal/v1", tags=["workspace"])


@router.post("/workspace", response_model=WorkspaceResponse)
async def ask_workspace(request: WorkspaceRequest, http_request: Request) -> WorkspaceResponse:
    service: WorkspaceService | None = getattr(http_request.app.state, "workspace_service_override", None)
    if service is None:
        service = await http_request.app.state.services.get_workspace_service()
    try:
        return await service.answer(request)
    except ChatProviderRateLimited as failure:
        headers = {"Retry-After": str(failure.retry_after_seconds)} if failure.retry_after_seconds else None
        raise AppError(429, "workspace_rate_limited", "Speedy is busy. Please retry shortly.", headers=headers) from failure
    except ChatProviderTimeout as failure:
        raise AppError(503, "workspace_timeout", "Speedy took too long to reply. Please try again.") from failure
    except (ChatProviderUnavailable, ChatProviderError) as failure:
        raise AppError(503, "workspace_unavailable", "Speedy is temporarily unavailable.") from failure
