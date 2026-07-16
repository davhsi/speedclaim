# SpeedClaim AI service

Phases R1–R4 provide the private FastAPI foundation, local embedding adapter, isolated
pgvector persistence, text-PDF ingestion, exact-brochure retrieval, and grounded policy answer
generation. R4 includes a provider-neutral chat interface and a Groq adapter, but no Redis,
persistent conversations, .NET, Angular, pre-purchase Q&A, or grievance-AI integration.

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
only for ingestion or policy Q&A; the embedding model is loaded lazily on its first call. A
Groq key is required only when calling the policy Q&A endpoint. Redis, Blob Storage, Azure, and
Groq are not contacted during ordinary startup or automated tests.

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
| `AI__StorageProvider` | no | `Local` | `Local` or `AzureBlob` |
| `AI__LocalBrochureRoot` | ingestion only | `/data/brochures` | absolute root for relative brochure paths |
| `AI__AzureBlobConnectionString` | with `AzureBlob` | none | Azure connection string stored as a secret |
| `AI__AzureBlobContainerName` | no | `speedclaim-uploads` | valid lowercase Azure container name |
| `AI__PdfMaxSizeBytes` | no | `10485760` | 1 KiB–50 MiB |
| `AI__PdfMaxPages` | no | `300` | 1–1000 pages |
| `AI__PdfMinTextCharacters` | no | `100` | minimum extractable alphanumeric characters |
| `AI__ParentChunkMaxCharacters` | no | `6000` | configurable parent context bound |
| `AI__ChildChunkMaxCharacters` | no | `1200` | configurable searchable child bound |
| `AI__ChildChunkOverlapCharacters` | no | `150` | must be smaller than child size |
| `AI__ChatProvider` | no | `Groq` | Phase R4 supports only `Groq` |
| `AI__GroqApiKey` | Q&A only | none | secret; non-empty with no surrounding whitespace |
| `AI__GroqBaseUrl` | no | `https://api.groq.com/openai/v1` | fixed official HTTPS endpoint |
| `AI__ChatModel` | no | `openai/gpt-oss-120b` | configurable model identifier |
| `AI__ChatTimeoutSeconds` | no | `15` | 1–60 seconds |
| `AI__ChatMaxAttempts` | no | `2` | 1–3 bounded transport attempts |
| `AI__ChatMaxOutputTokens` | no | `700` | 128–2048 tokens |
| `AI__PolicyQaPromptVersion` | no | `insurance-qa-v1` | fixed reviewed R4 prompt version |
| `AI__PolicyQaQuestionMaxCharacters` | no | `1000` | 100–4000 characters |
| `AI__RetrievalChildLimit` | no | `8` | 1–20 child-vector results |
| `AI__RetrievalMinSimilarity` | no | `0.45` | cosine similarity threshold from 0–1 |
| `AI__RetrievalMaxParentChunks` | no | `4` | 1–10 distinct parent contexts |
| `AI__RetrievalMaxContextCharacters` | no | `18000` | 1000–40000 prompt characters |

Incoming `X-Correlation-ID` values are reused only when they contain 1–128 safe token
characters; otherwise the service generates a UUID. Logs are JSON, omit request bodies and
query strings, and recursively redact secret/content fields.

## Tests

```bash
cd ai-service
python -m pytest
```

The default suite uses fake embedding/chat models and skips the database integration test unless
an isolated test database is explicitly supplied. It requires no external service or live model
call; Groq behavior is exercised through provider mocks and HTTP mock transports.

The five natural-language waiting-period regression questions are retained as an opt-in live
evaluation. They print only answer/citation/ranking metadata and never the configured key:

```bash
AI_RUN_LIVE_POLICY_QA=1 \
AI_TEST_VECTOR_CONNECTION_STRING='postgresql://speedclaim_ai:speedclaim_ai@127.0.0.1:5433/speedclaim_ai' \
python -m pytest -m live -s tests/evaluation/test_policy_qa_natural_questions.py
```

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

## Text-PDF ingestion

Phase R3 uses `pypdf` 6.14.2, a BSD-3-Clause pure-Python library with Python 3.14 support.
It extracts each page independently. The ingestion pipeline rejects invalid magic bytes,
corrupt/encrypted PDFs, blank PDFs, image-only PDFs, excessive pages, oversized files, path
traversal, and content-hash mismatches. OCR is intentionally excluded.

Repeated lines found on at least 60% of pages are removed as headers/footers. Section headings
and numbered clause references drive page-bounded parent/child chunks. Parent chunks retain
context without embeddings; child chunks receive local embeddings and remain linked to their
parent. Page, section, clause, content hash, brochure version, provider, model, and dimension
metadata are persisted.

Local filesystem and Azure Blob readers implement the same bounded interface. Azure support uses
the MIT-licensed `azure-storage-blob` 12.30.0 SDK, which explicitly supports Python 3.14. Its
client is created only when `AI__StorageProvider=AzureBlob`; ordinary startup and tests require no
Azure credentials or access.

For .NET R5 uploads with local storage, set `AI__LocalBrochureRoot` to the backend's `wwwroot`
directory because .NET stores keys such as `uploads/product-brochures/...`. Configure the same
32-or-more-character internal key in `.NET AI:InternalApiKey` and Python
`AI__InternalApiKey`. With Azure Blob storage, both services must use the same container while
keeping their connection strings in ignored local configuration or a secret store.

With a database migrated and a brochure mounted below `AI__LocalBrochureRoot`:

```bash
curl -X POST http://127.0.0.1:8000/internal/v1/brochures/ingest \
  -H "Content-Type: application/json" \
  -H "X-Internal-Api-Key: $AI__InternalApiKey" \
  -d '{
    "requestId": "11111111-1111-4111-8111-111111111111",
    "brochureId": "22222222-2222-4222-8222-222222222222",
    "productId": "33333333-3333-4333-8333-333333333333",
    "version": "1.0",
    "blobPath": "products/arogya-shield-plus-v1.pdf",
    "contentHash": "9df46877809920bc061a8daec0ed017845fc2d0fd3ddf7a901269da01df2af57"
  }'
```

Each attempt creates an ingestion-run record. Same brochure ID, product, version, and hash is
a no-op success; changed immutable metadata is rejected. Failed attempts preserve a redacted
failure state and can be retried safely.

## Retrieval and grounded policy answers

`POST /internal/v1/policy-qa` accepts only an immutable brochure ID, its product/version
metadata, and a policy question. Retrieval embeds the normalized question, filters vector
search by the exact brochure ID, applies a configurable similarity threshold, deduplicates
child hits, and expands a bounded set of parent chunks. Weak evidence returns
`InsufficientEvidence` without calling Groq.

The default chat model is `openai/gpt-oss-120b` through Groq. The adapter requests strict JSON
Schema output and enables no tools. The versioned `insurance-qa-v1` prompt treats both the
question and brochure text as untrusted data. Every generated claim must name a retrieved
citation and provide an exact supporting quote. The application checks that quote against the
stored parent chunk, rejects unknown or unsupported citations, and builds citation markers and
excerpts from repository metadata rather than model-supplied page details.

Store a development key only in the ignored `ai-service/.env` file or an environment variable;
never commit it:

```bash
AI__GroqApiKey=gsk_replace_with_local_secret
```

With the vector database migrated, the brochure ingested, and `AI__GroqApiKey` configured:

```bash
curl -X POST http://127.0.0.1:8000/internal/v1/policy-qa \
  -H "Content-Type: application/json" \
  -H "X-Internal-Api-Key: $AI__InternalApiKey" \
  -d '{
    "requestId": "11111111-1111-4111-8111-111111111111",
    "brochureId": "22222222-2222-4222-8222-222222222222",
    "productId": "33333333-3333-4333-8333-333333333333",
    "brochureVersion": "1.0",
    "question": "What is the initial waiting period?"
  }'
```

Provider timeouts and retryable server failures receive bounded retries. Rate limits return a
sanitized `429` and preserve a bounded `Retry-After` value. Malformed or unsupported model
output is validated and retried once, then rejected without an invented fallback answer.

## Container

From the repository root:

```bash
docker build -f ai-service/Dockerfile -t speedclaim-ai:r4 ai-service
docker run --rm -p 8000:8000 \
  -e AI__InternalApiKey="$(openssl rand -hex 32)" \
  -e AI__LocalBrochureRoot=/data/brochures \
  -v /absolute/local/brochures:/data/brochures:ro \
  speedclaim-ai:r4
```
