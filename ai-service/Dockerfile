FROM ghcr.io/astral-sh/uv:0.11.16 AS uv

FROM python:3.14.6-slim

ENV PYTHONDONTWRITEBYTECODE=1 \
    PYTHONUNBUFFERED=1 \
    UV_COMPILE_BYTECODE=1 \
    UV_LINK_MODE=copy

WORKDIR /app

RUN groupadd --system speedclaim \
    && useradd --system --gid speedclaim --create-home --home-dir /home/speedclaim speedclaim \
    && mkdir --parents /home/speedclaim/.cache/models \
    && chown --recursive speedclaim:speedclaim /home/speedclaim \
    && chmod 700 /home/speedclaim/.cache /home/speedclaim/.cache/models

COPY --from=uv /uv /uvx /bin/

COPY pyproject.toml uv.lock README.md alembic.ini ./

RUN uv sync --frozen --no-dev --no-install-project --no-build

COPY src ./src
COPY migrations ./migrations

ENV HOME=/home/speedclaim \
    XDG_CACHE_HOME=/home/speedclaim/.cache \
    HF_HOME=/home/speedclaim/.cache/huggingface \
    AI__EmbeddingCacheDir=/home/speedclaim/.cache/models \
    PYTHONPATH=/app/src

USER speedclaim

EXPOSE 8000

HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD ["python", "-c", "import urllib.request; urllib.request.urlopen('http://127.0.0.1:8000/health/ready', timeout=2).read()"]

CMD ["/app/.venv/bin/python", "-m", "uvicorn", "speedclaim_ai.main:create_app", "--factory", "--host", "0.0.0.0", "--port", "8000", "--no-access-log"]
