from fastapi import FastAPI

from speedclaim_ai import __version__
from speedclaim_ai.api.health import router as health_router
from speedclaim_ai.config.logging import configure_logging
from speedclaim_ai.config.settings import Settings, get_settings
from speedclaim_ai.errors import register_exception_handlers
from speedclaim_ai.middleware import CorrelationIdMiddleware, RequestSizeLimitMiddleware
from speedclaim_ai.security.internal_auth import InternalApiKeyMiddleware


def build_app(settings: Settings) -> FastAPI:
    configure_logging(settings.log_level)
    app = FastAPI(
        title="SpeedClaim AI Service",
        description="Private policy-brochure AI service",
        version=__version__,
    )
    app.state.settings = settings

    register_exception_handlers(app)
    app.include_router(health_router)

    # Starlette executes the most recently added middleware first. Correlation IDs therefore
    # wrap every outcome, auth rejects before body buffering, and authorized bodies are capped.
    app.add_middleware(
        RequestSizeLimitMiddleware,
        max_request_size_bytes=settings.max_request_size_bytes,
    )
    app.add_middleware(
        InternalApiKeyMiddleware,
        api_key=settings.internal_api_key.get_secret_value(),
    )
    app.add_middleware(CorrelationIdMiddleware)
    return app


def create_app() -> FastAPI:
    return build_app(get_settings())
