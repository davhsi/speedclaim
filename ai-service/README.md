# SpeedClaim AI service

Phases R1–R2 provide the private FastAPI foundation, local embedding adapter, and isolated
pgvector persistence for policy-brochure RAG. There is still no PDF ingestion, retrieval/answer
service, Groq, Redis, Azure, .NET, Angular, or grievance-AI integration.

## Prerequisites

- Python 3.14 (developed and containerized against Python 3.14.6)
- Docker, only for the container workflow

Direct dependency declarations are pinned in `pyproject.toml`. Fully resolved production and
test manifests are `requirements.lock` and `requirements-test.lock`.

## Local setup and startup

```bash
cd ai-service
python3 -m venv .venv
source .venv/bin/activate
python -m pip install --requirement requirements-test.lock
python -m pip install --no-deps --editable .
export AI__InternalApiKey="$(openssl rand -hex 32)"
python -m uvicorn speedclaim_ai.main:create_app --factory --host 127.0.0.1 --port 8000 --no-access-log
```

The internal key remains the only required secret to start the service. PostgreSQL is needed
only for migrations or repository use; the embedding model is loaded lazily on its first call.
Groq, Redis, Blob Storage, and Azure configuration are deliberately not loaded.

```bash
curl http://127.0.0.1:8000/health/live
curl http://127.0.0.1:8000/health/ready
```

Health endpoints are unauthenticated for container/orchestrator probes. All paths below
`/internal/` require `X-Internal-Api-Key`; that namespace is reserved for later RAG phases.

## Configuration

| Environment variable | Required | Default | Validation |
| --- | --- | --- | --- |
| `AI__InternalApiKey` | yes | none | 32–512 characters, no surrounding whitespace |
| `AI__Environment` | no | `Development` | `Local`, `Development`, `Test`, `Staging`, or `Production` |
| `AI__LogLevel` | no | `INFO` | standard Python log level |
| `AI__MaxRequestSizeBytes` | no | `1048576` | 1 KiB–10 MiB |
| `AI__ServiceName` | no | `speedclaim-ai` | non-empty, at most 64 characters |
| `AI__EmbeddingProvider` | no | `Local` | Phase R2 supports only `Local` |
| `AI__EmbeddingModel` | no | `BAAI/bge-small-en-v1.5` | fixed Phase R2 model |
| `AI__EmbeddingDimension` | no | `384` | must match the model/schema |
| `AI__EmbeddingCacheDir` | no | `/tmp/speedclaim-ai-models` | local model cache path |
| `AI__EmbeddingThreads` | no | `2` | 1–32 CPU threads |
| `AI__VectorConnectionString` | repository only | none | PostgreSQL/Psycopg URL; stored as a secret |

Incoming `X-Correlation-ID` values are reused only when they contain 1–128 safe token
characters; otherwise the service generates a UUID. Logs are JSON, omit request bodies and
query strings, and recursively redact secret/content fields.

## Tests

```bash
cd ai-service
python -m pytest
```

The default suite uses a fake embedding model and skips the database integration test unless
an isolated test database is explicitly supplied. It requires no external service or live model
call.

## Embedding choice

Phase R2 uses FastEmbed/ONNX Runtime with `BAAI/bge-small-en-v1.5`: a 384-dimensional,
MIT-licensed English retrieval model. Document chunks use the model's passage mode and questions
use its query mode. Provider/model/dimension are stored with each indexed document.

The provider is CPU-only and lazy. Its first real embedding call may download model artifacts
into `AI__EmbeddingCacheDir`; ordinary service startup and tests do not download or load a model.

## Local pgvector workflow

Start a disposable database (port `5433` avoids the backend's usual PostgreSQL port):

```bash
docker run --rm --name speedclaim-ai-db \
  -e POSTGRES_USER=speedclaim_ai \
  -e POSTGRES_PASSWORD=speedclaim_ai \
  -e POSTGRES_DB=speedclaim_ai \
  -p 5433:5432 \
  pgvector/pgvector:pg17-bookworm
```

In another shell, with the virtual environment active:

```bash
cd ai-service
export AI__VectorConnectionString='postgresql://speedclaim_ai:speedclaim_ai@127.0.0.1:5433/speedclaim_ai'
python -m alembic upgrade head

# This variable is deliberately separate and authorizes destructive test-schema reset.
export AI_TEST_VECTOR_CONNECTION_STRING="$AI__VectorConnectionString"
python -m pytest -m database
```

The initial repository performs exact cosine search after filtering to the immutable brochure
ID. An HNSW index is intentionally deferred until corpus size and query plans are measured.

## Container

From the repository root:

```bash
docker build -f ai-service/Dockerfile -t speedclaim-ai:r2 ai-service
docker run --rm -p 8000:8000 \
  -e AI__InternalApiKey="$(openssl rand -hex 32)" \
  speedclaim-ai:r2
```
