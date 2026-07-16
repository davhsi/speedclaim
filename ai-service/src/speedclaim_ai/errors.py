from dataclasses import dataclass
from typing import Any

from fastapi import FastAPI, Request
from fastapi.exceptions import RequestValidationError
from fastapi.responses import JSONResponse
from starlette.exceptions import HTTPException as StarletteHttpException


@dataclass(slots=True)
class AppError(Exception):
    status_code: int
    code: str
    message: str
    headers: dict[str, str] | None = None


def get_correlation_id(request: Request) -> str:
    return getattr(request.state, "correlation_id", "unavailable")


def error_response(
    request: Request,
    *,
    status_code: int,
    code: str,
    message: str,
    headers: dict[str, str] | None = None,
) -> JSONResponse:
    correlation_id = get_correlation_id(request)
    response_headers = {
        "Cache-Control": "no-store",
        "X-Correlation-ID": correlation_id,
        **(headers or {}),
    }
    return JSONResponse(
        status_code=status_code,
        headers=response_headers,
        content={
            "error": {
                "code": code,
                "message": message,
                "requestId": correlation_id,
            }
        },
    )


def _http_error_code(status_code: int) -> str:
    return {
        400: "bad_request",
        401: "unauthorized",
        403: "forbidden",
        404: "not_found",
        405: "method_not_allowed",
        409: "conflict",
        413: "request_too_large",
        422: "invalid_request",
        429: "rate_limited",
        503: "service_unavailable",
    }.get(status_code, "http_error")


def register_exception_handlers(app: FastAPI) -> None:
    @app.exception_handler(AppError)
    async def handle_app_error(request: Request, exc: AppError) -> JSONResponse:
        return error_response(
            request,
            status_code=exc.status_code,
            code=exc.code,
            message=exc.message,
            headers=exc.headers,
        )

    @app.exception_handler(RequestValidationError)
    async def handle_validation_error(
        request: Request,
        _exc: RequestValidationError,
    ) -> JSONResponse:
        return error_response(
            request,
            status_code=422,
            code="invalid_request",
            message="The request is invalid.",
        )

    @app.exception_handler(StarletteHttpException)
    async def handle_http_error(
        request: Request,
        exc: StarletteHttpException,
    ) -> JSONResponse:
        detail: Any = exc.detail
        message = detail if isinstance(detail, str) else "The request could not be completed."
        return error_response(
            request,
            status_code=exc.status_code,
            code=_http_error_code(exc.status_code),
            message=message,
            headers=exc.headers,
        )

    @app.exception_handler(Exception)
    async def handle_unexpected_error(request: Request, _exc: Exception) -> JSONResponse:
        return error_response(
            request,
            status_code=500,
            code="internal_error",
            message="An unexpected error occurred.",
        )
