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

---

## Tech Stack

| Layer | Technology |
| --- | --- |
| Backend | .NET 10 Web API (C#) |
| Frontend | Angular 22 |
| Database | PostgreSQL |
| ORM | Entity Framework Core (Code-First) |
| Auth | JWT — Access Token (15 min) + Refresh Token (7 days) |
| Payments | Stripe (sandbox) |
| Email | Gmail SMTP via MailKit |
| Logging | Serilog (console + rolling file) |
| Testing | NUnit + Moq |

---

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [PostgreSQL 15+](https://www.postgresql.org/download/)
- [Node.js 20+](https://nodejs.org/) (for Angular frontend)
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

Copy the example settings and fill in your values:

```bash
cp backend/SpeedClaim.Api/appsettings.json backend/SpeedClaim.Api/appsettings.Development.json
```

Edit `appsettings.Development.json` — see [Configuration](#configuration) for all required keys.

### 4. Run database migrations

```bash
cd backend/SpeedClaim.Api
dotnet ef database update
```

### 5. Start the API

```bash
dotnet run
```

The API starts at `https://localhost:7001` (HTTPS) and `http://localhost:5001` (HTTP).

---

## Configuration

All configuration lives in `appsettings.json` / `appsettings.Development.json`. **Never commit real secrets.**

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
  "SmtpSettings": {
    "Host": "smtp.gmail.com",
    "Port": 587,
    "SenderName": "SpeedClaim",
    "SenderEmail": "your-email@gmail.com",
    "AppPassword": "your-gmail-app-password"
  }
}
```

> For Gmail SMTP, generate an [App Password](https://myaccount.google.com/apppasswords) — do not use your main account password.

---

## Running the API

```bash
cd backend/SpeedClaim.Api
dotnet run
```

For hot reload during development:

```bash
dotnet watch run
```

Logs are written to `backend/SpeedClaim.Api/logs/speedclaim-YYYY-MM-DD.log` (rolling daily, 30-day retention).

---

## Running Tests

```bash
cd backend
dotnet test SpeedClaim.Tests/SpeedClaim.Tests.csproj
```

All tests are NUnit unit tests covering the service layer with Moq-mocked repositories. 252 tests, 0 failures.

To generate an HTML coverage report (requires `dotnet-reportgenerator-globaltool`):

```bash
bash coverage.sh
```

Report is written to `coverage-report/index.html`. Service-layer line coverage is above 96% on all services.

---

## API Documentation

Swagger UI is available at runtime in the Development environment:

```text
http://localhost:5001/swagger
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
