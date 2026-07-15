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
    # R1 has no external dependencies. Later phases extend this with bounded dependency checks.
    response.headers["Cache-Control"] = "no-store"
    return ReadinessResponse(
        status="ready",
        service=request.app.state.settings.service_name,
        checks={"configuration": "ready"},
    )
