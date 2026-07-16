import logging
import secrets
from collections.abc import Awaitable, Callable

from fastapi import Request, Response
from starlette.middleware.base import BaseHTTPMiddleware
from starlette.types import ASGIApp

from speedclaim_ai.errors import error_response

INTERNAL_API_KEY_HEADER = "X-Internal-Api-Key"
_logger = logging.getLogger(__name__)


def is_internal_path(path: str) -> bool:
    return path == "/internal" or path.startswith("/internal/")


class InternalApiKeyMiddleware(BaseHTTPMiddleware):
    def __init__(self, app: ASGIApp, api_key: str) -> None:
        super().__init__(app)
        self._api_key = api_key

    async def dispatch(
        self,
        request: Request,
        call_next: Callable[[Request], Awaitable[Response]],
    ) -> Response:
        if not is_internal_path(request.url.path):
            return await call_next(request)

        supplied_key = request.headers.get(INTERNAL_API_KEY_HEADER, "")
        if not supplied_key or not secrets.compare_digest(supplied_key, self._api_key):
            _logger.warning(
                "Internal authentication rejected",
                extra={"event": "internal_auth.rejected"},
            )
            return error_response(
                request,
                status_code=401,
                code="unauthorized",
                message="A valid internal service credential is required.",
                headers={"WWW-Authenticate": "ApiKey"},
            )

        return await call_next(request)
