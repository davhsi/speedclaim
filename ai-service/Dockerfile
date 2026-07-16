FROM python:3.14.6-slim

ENV PYTHONDONTWRITEBYTECODE=1 \
    PYTHONUNBUFFERED=1 \
    PIP_DISABLE_PIP_VERSION_CHECK=1

WORKDIR /app

RUN groupadd --system speedclaim \
    && useradd --system --gid speedclaim --create-home --home-dir /home/speedclaim speedclaim \
    && mkdir --parents /home/speedclaim/.cache /tmp/speedclaim-ai-models \
    && chown --recursive speedclaim:speedclaim /home/speedclaim /tmp/speedclaim-ai-models

COPY requirements.lock ./

RUN python -m pip install --no-cache-dir --requirement requirements.lock

COPY pyproject.toml README.md alembic.ini ./
COPY src ./src
COPY migrations ./migrations

RUN python -m pip install --no-cache-dir --no-deps .

ENV HOME=/home/speedclaim \
    XDG_CACHE_HOME=/home/speedclaim/.cache \
    HF_HOME=/home/speedclaim/.cache/huggingface \
    AI__EmbeddingCacheDir=/tmp/speedclaim-ai-models

USER speedclaim

EXPOSE 8000

HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD ["python", "-c", "import urllib.request; urllib.request.urlopen('http://127.0.0.1:8000/health/ready', timeout=2).read()"]

CMD ["python", "-m", "uvicorn", "speedclaim_ai.main:create_app", "--factory", "--host", "0.0.0.0", "--port", "8000", "--no-access-log"]
