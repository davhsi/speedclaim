import logging
import re
import time
import uuid
from collections.abc import Awaitable, Callable

from fastapi import Request, Response
from starlette.middleware.base import BaseHTTPMiddleware
from starlette.types import ASGIApp

from speedclaim_ai.config.logging import correlation_id_context
from speedclaim_ai.errors import error_response

_CORRELATION_ID_HEADER = "X-Correlation-ID"
_SAFE_CORRELATION_ID = re.compile(r"^[A-Za-z0-9._-]{1,128}$")
_logger = logging.getLogger(__name__)


class CorrelationIdMiddleware(BaseHTTPMiddleware):
    async def dispatch(
        self,
        request: Request,
        call_next: Callable[[Request], Awaitable[Response]],
    ) -> Response:
        supplied_id = request.headers.get(_CORRELATION_ID_HEADER, "")
        correlation_id = (
            supplied_id if _SAFE_CORRELATION_ID.fullmatch(supplied_id) else str(uuid.uuid4())
        )
        request.state.correlation_id = correlation_id
        context_token = correlation_id_context.set(correlation_id)
        started_at = time.perf_counter()

        try:
            try:
                response = await call_next(request)
            except Exception:
                _logger.exception(
                    "Unhandled request failure",
                    extra={"event": "http.request.failed"},
                )
                response = error_response(
                    request,
                    status_code=500,
                    code="internal_error",
                    message="An unexpected error occurred.",
                )

            response.headers[_CORRELATION_ID_HEADER] = correlation_id
            duration_ms = round((time.perf_counter() - started_at) * 1_000, 2)
            _logger.info(
                "HTTP request completed",
                extra={
                    "event": "http.request.completed",
                    "http": {
                        "method": request.method,
                        "path": request.url.path,
                        "statusCode": response.status_code,
                    },
                    "durationMs": duration_ms,
                },
            )
            return response
        finally:
            correlation_id_context.reset(context_token)


class RequestSizeLimitMiddleware(BaseHTTPMiddleware):
    def __init__(self, app: ASGIApp, max_request_size_bytes: int) -> None:
        super().__init__(app)
        self._max_request_size_bytes = max_request_size_bytes

    async def dispatch(
        self,
        request: Request,
        call_next: Callable[[Request], Awaitable[Response]],
    ) -> Response:
        content_length = request.headers.get("content-length")
        if content_length:
            try:
                declared_size = int(content_length)
            except ValueError:
                return error_response(
                    request,
                    status_code=400,
                    code="bad_request",
                    message="Content-Length must be a non-negative integer.",
                )
            if declared_size < 0:
                return error_response(
                    request,
                    status_code=400,
                    code="bad_request",
                    message="Content-Length must be a non-negative integer.",
                )
            if declared_size > self._max_request_size_bytes:
                return self._too_large(request)

        chunks: list[bytes] = []
        received_size = 0
        async for chunk in request.stream():
            received_size += len(chunk)
            if received_size > self._max_request_size_bytes:
                return self._too_large(request)
            chunks.append(chunk)
        request._body = b"".join(chunks)  # Starlette replays this cached body downstream.
        return await call_next(request)

    @staticmethod
    def _too_large(request: Request) -> Response:
        return error_response(
            request,
            status_code=413,
            code="request_too_large",
            message="The request body is too large.",
        )
