from typing import Literal

from fastapi import APIRouter, Request, Response
from pydantic import BaseModel

from speedclaim_ai import __version__

router = APIRouter(prefix="/health", tags=["health"])


class LivenessResponse(BaseModel):
    status: Literal["alive"]
    service: str
    version: str


class ReadinessResponse(BaseModel):
    status: Literal["ready"]
    service: str
    checks: dict[str, Literal["ready"]]


@router.get("/live", response_model=LivenessResponse)
async def liveness(request: Request, response: Response) -> LivenessResponse:
    response.headers["Cache-Control"] = "no-store"
    return LivenessResponse(
        status="alive",
        service=request.app.state.settings.service_name,
        version=__version__,
    )


@router.get("/ready", response_model=ReadinessResponse)
async def readiness(request: Request, response: Response) -> ReadinessResponse:
    # Optional RAG dependencies are initialized and checked only when an ingestion call uses them,
    # so the service can start and expose probes before a database or model cache is configured.
    response.headers["Cache-Control"] = "no-store"
    return ReadinessResponse(
        status="ready",
        service=request.app.state.settings.service_name,
        checks={"configuration": "ready"},
    )
