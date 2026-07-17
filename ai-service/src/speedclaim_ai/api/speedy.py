from fastapi import APIRouter, Request

from speedclaim_ai.contracts.speedy import SpeedyRequest, SpeedyResponse
from speedclaim_ai.errors import AppError
from speedclaim_ai.providers.chat.base import (
    ChatProviderError,
    ChatProviderRateLimited,
    ChatProviderTimeout,
    ChatProviderUnavailable,
)
from speedclaim_ai.speedy import SpeedyService

router = APIRouter(prefix="/internal/v1", tags=["speedy"])


@router.post("/speedy", response_model=SpeedyResponse)
async def ask_speedy(request: SpeedyRequest, http_request: Request) -> SpeedyResponse:
    service: SpeedyService | None = getattr(http_request.app.state, "speedy_service_override", None)
    if service is None:
        service = await http_request.app.state.services.get_speedy_service()
    try:
        return await service.answer(request)
    except ChatProviderRateLimited as failure:
        headers = {"Retry-After": str(failure.retry_after_seconds)} if failure.retry_after_seconds else None
        raise AppError(429, "speedy_rate_limited", "Speedy is busy. Please retry shortly.", headers=headers) from failure
    except ChatProviderTimeout as failure:
        raise AppError(503, "speedy_timeout", "Speedy took too long to reply. Please try again.") from failure
    except (ChatProviderUnavailable, ChatProviderError) as failure:
        raise AppError(503, "speedy_unavailable", "Speedy is temporarily unavailable.") from failure
