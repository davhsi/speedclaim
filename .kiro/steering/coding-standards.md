---
inclusion: always
---

# SpeedClaim Coding Standards & Preferences

## Architecture

Follow a strict **Layered Architecture**. Code is organized into these layers/folders:

| Layer | Responsibility |
|---|---|
| `Controllers` | HTTP request handling, routing, status codes |
| `Services` | Business logic, orchestration, Entity↔DTO mapping |
| `Interfaces` | Abstract contracts for repositories and services (DI) |
| `Repositories` | Data access via EF Core queries or stored procedures |
| `Context` | EF Core `DbContext` and `IEntityTypeConfiguration<T>` configs |
| `Dtos` | Request/response data transfer objects |
| `Exceptions` | Custom domain-specific exception classes |

## Tech Stack

- **Backend:** .NET 10 Web API
- **Frontend:** Angular
- **Database:** PostgreSQL (EF Core Code-First)
- **Auth:** JWT for stateless authentication + role-based authorization; BCrypt for password hashing
- **Payments:** Stripe testing sandbox (keys in config only)
- **Email:** Gmail SMTP via Google App Passwords (credentials in config only)
- **File Storage:** Local file system for now — keep storage logic behind an abstraction to allow future swap to Azure Blob Storage
- **Secrets/Config:** `appsettings.json` or `.env` — no Azure Key Vault

## Coding Conventions

- **Naming:** PascalCase for classes and methods; camelCase for local variables and parameters; `_camelCase` prefix for private fields (e.g., `_paymentService`)
- **Async:** All I/O-bound operations, DB calls, and external API requests must use `async`/`await`. Never use `.Result` or `.Wait()`
- **Error handling:** Global Exception Middleware catches unhandled errors, logs them, and returns a unified JSON error response
- **Logging:** Serilog for structured logging across all layers (Info, Warning, Error with contextual metadata)

## Database Rules

- **EF Core:** Code-First only. **No DataAnnotations** on domain models or DTOs — all configuration via Fluent API (`IEntityTypeConfiguration<T>`)
- **Constraint naming:**
  - Primary Keys: `PK_TableName`
  - Foreign Keys: `FK_TableName_TargetTableName_ColumnName`
  - Unique Constraints: `UQ_TableName_ColumnName`
  - Check Constraints: `CK_TableName_ConstraintDescription`
- **Complex/transactional operations:** Implement as PostgreSQL stored procedures or functions, called from the repository layer

## API & Testing

- **Swagger:** Required in all development environments
- **Unit tests:** NUnit exclusively, targeting the Service layer with mocked repositories
- **Coverage goal:** Full Service layer coverage + critical business workflows before frontend integration

## Git Workflow

- `main`/`develop` must always be stable — no direct commits
- Branch naming: `feature/name`, `bugfix/name`, `test/name`
- Merge via Pull Request only; all code must compile and pass NUnit tests before merging
- Commit messages follow Conventional Commits:
  - `feat:` — new feature
  - `fix:` — bug fix
  - `test:` — adding/updating tests
  - `refactor:` — code restructure without behavior change
  - `chore:` — config, dependencies, build tooling
