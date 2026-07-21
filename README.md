# SpeedClaim

An end-to-end insurance platform covering Health, Life, and Motor domains. Customers can buy policies, pay premiums, and file claims. Agents, underwriters, claims officers, finance officers, and admins each have role-specific portals.

> Capstone project — .NET 10 Web API + Angular + PostgreSQL + Stripe

---

## Table of Contents

- [Tech Stack](#tech-stack)
- [Prerequisites](#prerequisites)
- [Getting Started](#getting-started)
- [Configuration](#configuration)
- [Running the API](#running-the-api)
- [Running Tests](#running-tests)
- [API Documentation](#api-documentation)
- [Project Structure](#project-structure)
- [Docs](#docs)
- [KT Git Workflow](#kt-git-workflow)

---

## Tech Stack

| Layer | Technology |
| --- | --- |
| Backend | .NET 10 Web API (C#) |
| Frontend | Angular 21.2 + Tailwind CSS 4 |
| Database | PostgreSQL |
| ORM | Entity Framework Core (Code-First) |
| Auth | JWT — Access Token (15 min) + Refresh Token (7 days) |
| Payments | Stripe (sandbox) |
| Email | Gmail SMTP via MailKit |
| Logging | Serilog (console + rolling file) |
| Testing | NUnit + Moq |
| Private AI support | FastAPI + LangGraph, accessed only through the .NET API |

---

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [PostgreSQL 15+](https://www.postgresql.org/download/)
- [Node.js 20 or 22 LTS](https://nodejs.org/) (for Angular frontend)
- [Stripe CLI](https://stripe.com/docs/stripe-cli) (for local webhook testing)

---

## Getting Started

### 1. Clone the repo

```bash
git clone <repo-url>
cd InsuranceApp
```

### 2. Set up the database

Create a PostgreSQL database:

```sql
CREATE DATABASE speedclaim;
```

### 3. Configure the API

Copy the local-development example and fill in your values:

```bash
cp backend/SpeedClaim.Api/appsettings.Development.example.json \
  backend/SpeedClaim.Api/appsettings.Development.json
```

Edit `appsettings.Development.json` — see [Configuration](#configuration) for all required keys.
This file is git-ignored and must never be committed.

### 4. Run database migrations

```bash
dotnet ef database update --project backend/SpeedClaim.Api
```

### 5. Start the API

```bash
dotnet run --project backend/SpeedClaim.Api
```

The API starts at `http://localhost:5062`. Swagger is available at
`http://localhost:5062/swagger` in Development.

### 6. Install and start the frontend

```bash
cd frontend
npm ci
npm start
```

Open `http://localhost:4200`. The Angular development proxy sends `/api`, `/uploads`, and
`/hubs` requests to the local API at `http://localhost:5062`.

---

## Configuration

Committed non-secret defaults live in `appsettings.json`. Each developer keeps local secrets in
the git-ignored `appsettings.Development.json`. **Never commit credentials, even test credentials.**

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=speedclaim;Username=YOUR_USER;Password=YOUR_PASSWORD"
  },
  "JwtSettings": {
    "Secret": "YOUR_JWT_SECRET_MIN_32_CHARS",
    "Issuer": "SpeedClaimApi",
    "Audience": "SpeedClaimClients",
    "AccessTokenExpirationMinutes": 15,
    "RefreshTokenExpirationDays": 7
  },
  "Stripe": {
    "SecretKey": "sk_test_...",
    "PublishableKey": "pk_test_...",
    "WebhookSecret": "whsec_..."
  },
  "Storage": {
    "Provider": "Local"
  },
  "SecuritySettings": {
    "EncryptionKey": "BASE64_ENCODED_32_BYTE_KEY"
  },
  "SmtpSettings": {
    "Host": "smtp.gmail.com",
    "Port": 587,
    "SenderName": "SpeedClaim",
    "SenderEmail": "your-email@gmail.com",
    "AppPassword": "your-gmail-app-password"
  }
}
```

Generate local-only JWT and encryption values with:

```bash
openssl rand -base64 48
openssl rand -base64 32
```

The second command produces the KYC encryption key. Keep it stable for the lifetime of the local
database because existing encrypted KYC values depend on it.

Ordinary local development does not need `KeyVault` or `AzureBlob` sections. The committed base
configuration leaves `KeyVault:Uri` empty, and `Storage:Provider=Local` writes uploads beneath
`backend/SpeedClaim.Api/wwwroot/uploads/`. Azure credentials are loaded only in the production
deployment when `KeyVault:Uri` is configured.

Stripe `sk_test_...`, `pk_test_...`, and `whsec_...` values may be supplied to an authorized KT
developer for sandbox testing, but send them through an approved secret-sharing channel. Do not
put them in Git, chat transcripts, tickets, screenshots, or the example file. Prefer a dedicated
shared test account or restricted test keys where available, and rotate access when KT ends.

> For Gmail SMTP, generate an [App Password](https://myaccount.google.com/apppasswords) — do not use your main account password.

---

## Running the API

```bash
dotnet run --project backend/SpeedClaim.Api
```

For hot reload during development:

```bash
dotnet watch --project backend/SpeedClaim.Api run
```

Logs are written to `backend/SpeedClaim.Api/logs/speedclaim-YYYY-MM-DD.log` (rolling daily, 30-day retention).

---

## Running Tests

```bash
cd backend
dotnet test SpeedClaim.Tests/SpeedClaim.Tests.csproj
cd ../frontend && npm test -- --watch=false
cd ../ai-service && .venv/bin/python -m pytest
```

Backend tests use NUnit and service-layer dependencies are generally mocked with Moq. Use current
command output rather than a hard-coded test count when reporting status.

To generate an HTML coverage report (requires `dotnet-reportgenerator-globaltool`):

```bash
bash coverage.sh
```

Report is written to `coverage-report/index.html`. Service-layer line coverage is above 96% on all services.

---

## API Documentation

Swagger UI is available at runtime in the Development environment:

```text
http://localhost:5062/swagger
```

All endpoints are documented with roles, request/response schemas, and status codes.

For Stripe payment testing without a frontend, see [docs/stripe_testing_guide.md](docs/stripe_testing_guide.md).

---

## Project Structure

```text
InsuranceApp/
├── backend/
│   ├── SpeedClaim.Api/          # .NET 10 Web API
│   │   ├── Controllers/         # HTTP layer — routing & status codes
│   │   ├── Services/            # Business logic
│   │   ├── Interfaces/          # DI abstractions
│   │   ├── Repositories/        # EF Core data access
│   │   ├── Dtos/                # Request & response models
│   │   ├── Models/              # Domain entities & enums
│   │   ├── Exceptions/          # Custom exception types
│   │   ├── Migrations/          # EF Core migrations
│   │   └── Program.cs           # App entry point & DI config
│   └── SpeedClaim.Tests/        # NUnit test project
│       └── Services/            # Service layer unit tests
├── ai-service/                  # Private FastAPI / Speedy AI service
├── frontend/                    # Angular customer and staff portal
└── docs/
    ├── spec.md                        # Technical specification
    ├── roles_and_responsibilities.md  # Per-role capability matrix
    ├── coding_standards.md            # Coding conventions & patterns
    ├── user-flows.md                  # User stories & Mermaid flow diagrams
    ├── api-page-map.md                # API coverage analysis
    └── stripe_testing_guide.md        # Local Stripe webhook testing
```

---

## Docs

| Document | Purpose |
| --- | --- |
| [requirement_understanding.md](docs/requirement_understanding.md) | Full SRS — business objectives, FR-XX functional requirements, NFR-XX non-functional requirements, assumptions, out of scope |
| [spec.md](docs/spec.md) | Technical specification — stack, auth, DB schema |
| [roles_and_responsibilities.md](docs/roles_and_responsibilities.md) | What each role can and cannot do |
| [coding_standards.md](docs/coding_standards.md) | Naming conventions, patterns, Git workflow |
| [user-flows.md](docs/user-flows.md) | Step-by-step user journeys with Mermaid diagrams |
| [api-page-map.md](docs/api-page-map.md) | Page-by-page API endpoint coverage |
| [stripe_testing_guide.md](docs/stripe_testing_guide.md) | How to test Stripe payments locally via CLI |
| [mcp-architecture.md](docs/mcp-architecture.md) | Disabled-by-default external MCP design and exposure policy |

---

## KT Git Workflow

Grant the KT developer repository Write access, protect `main`, and require pull requests. GitHub
permissions are repository-level; branch protection controls how important branches are changed.

For each assignment, the KT developer can create and push a task branch themselves, for example
`feature/kt-policy-change`, then open a pull request into `main`. The repository owner reviews and
merges it. A pre-created long-lived KT branch is only necessary when an instructor or organizational
policy explicitly requires one; even then, use smaller task branches where possible.

Recommended `main` rules are one required approval, passing build/test checks, resolved review
conversations, and no force pushes or deletion. Pull requests targeting `main` trigger an Azure
Static Web Apps preview; merging to `main` triggers the production frontend deployment.
