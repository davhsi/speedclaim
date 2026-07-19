from contextlib import asynccontextmanager

from fastapi import FastAPI

from speedclaim_ai import __version__
from speedclaim_ai.api.dependencies import ServiceContainer
from speedclaim_ai.api.health import router as health_router
from speedclaim_ai.api.ingestion import router as ingestion_router
from speedclaim_ai.api.policy_qa import router as policy_qa_router
from speedclaim_ai.api.speedy import router as speedy_router
from speedclaim_ai.api.workspace import router as workspace_router
from speedclaim_ai.config.logging import configure_logging
from speedclaim_ai.config.settings import Settings, get_settings
from speedclaim_ai.errors import register_exception_handlers
from speedclaim_ai.middleware import CorrelationIdMiddleware, RequestSizeLimitMiddleware
from speedclaim_ai.security.internal_auth import InternalApiKeyMiddleware


@asynccontextmanager
async def lifespan(app: FastAPI):
    yield
    await app.state.services.close()


def build_app(settings: Settings) -> FastAPI:
    configure_logging(settings.log_level)
    app = FastAPI(
        title="SpeedClaim AI Service",
        description="Private policy-brochure AI service",
        version=__version__,
        lifespan=lifespan,
    )
    app.state.settings = settings
    app.state.services = ServiceContainer(settings)

    register_exception_handlers(app)
    app.include_router(health_router)
    app.include_router(ingestion_router)
    app.include_router(policy_qa_router)
    app.include_router(speedy_router)
    app.include_router(workspace_router)

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
