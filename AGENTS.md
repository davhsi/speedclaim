# SpeedClaim — AGENTS.md

Persistent context for AI sessions. Read this before touching any code.

---

## Project Overview

**SpeedClaim** is a full-stack insurance claims management platform built as an internship capstone project.

- **Backend**: .NET 10 Web API (C#), EF Core, PostgreSQL
- **Frontend**: Angular 21 + Tailwind CSS 4 (in `frontend/`)
- **Payments**: Stripe
- **Email**: MailKit over SMTP (Gmail App Password)
- **Auth**: JWT Bearer tokens
- **Tests**: xUnit + Moq, 396 tests, all passing

---

## Repository Layout

```
InsuranceApp/
├── backend/
│   ├── SpeedClaim.Api/          # Main Web API project
│   │   ├── Controllers/
│   │   ├── Context/             # SpeedClaimDbContext.cs
│   │   ├── Interfaces/
│   │   ├── Models/
│   │   ├── Services/
│   │   ├── Repositories/
│   │   ├── Migrations/
│   │   ├── DTOs/
│   │   ├── Validators/
│   │   ├── Exceptions/          # GlobalExceptionMiddleware
│   │   ├── Swagger/             # AuthorizeOperationFilter
│   │   ├── appsettings.json     # Non-secret defaults (committed)
│   │   └── appsettings.Development.json  # ALL secrets (git-ignored)
│   └── SpeedClaim.Tests/        # xUnit test project
├── frontend/                    # Angular 21 app
│   ├── src/app/
│   │   ├── core/                # Guards, interceptors, services, models
│   │   ├── features/            # Feature modules (see below)
│   │   └── shared/              # Reusable components, pipes, validators
│   ├── proxy.conf.js            # Dev proxy → backend (localhost:5062)
│   ├── package.json
│   └── angular.json
├── docs/
│   └── spec.md                  # Architecture + API spec
└── AGENTS.md                    # This file
```

---

## Common Commands

```bash
# Run API (from repo root)
dotnet run --project backend/SpeedClaim.Api

# Run Angular frontend
cd frontend && npx ng serve        # serves at http://localhost:4200
# Use `npx ng serve` to catch compilation errors (not ng build or tsc)

# Run all tests
dotnet test backend/SpeedClaim.Tests

# EF Core migrations
dotnet ef migrations add <MigrationName> --project backend/SpeedClaim.Api
dotnet ef database update --project backend/SpeedClaim.Api

# Build
dotnet build backend/SpeedClaim.Api
```

---

## Models (42 tables in DB)

`User`, `Customer`, `Agent`, `Address`, `Branch`, `Session`, `UserToken`, `UserConsent`  
`InsuranceProduct`, `PremiumRateTable`, `DocumentRequirement`  
`Proposal`, `ProposalMember`  
`Policy`, `PolicyMember`, `PolicyStatusHistory`, `PremiumSchedule`, `PremiumPayment`  
`MotorDetail`, `HealthDetail`, `LifeDetail`  
`Endorsement`  
`Claim`, `ClaimStatusHistory`, `MotorClaimDetail`, `HealthClaimDetail`, `LifeClaimDetail`  
`SubmittedDocument`  
`KycRecord`  
`Grievance`  
`Notification`, `EmailLog`, `EmailTemplate`  
`Surveyor`, `AgentCommission`  
`StripeCustomer`  
`AuditLog`  
`SystemConfig`  
`Nominee`, `CustomerMember`  
`ProcessedWebhookEvent`  
`__EFMigrationsHistory` (EF internal)

---

## Critical Architectural Decisions

### 1. snake_case PostgreSQL columns
EF Core is configured with `UseSnakeCaseNamingConvention()`. All DB columns are snake_case; C# properties are PascalCase. Never write raw SQL with PascalCase column names.

### 2. DateOnly for date-only fields
Policy start/end dates, DOB, and similar fields use `DateOnly` (not `DateTime`). Npgsql maps these to `date` columns in Postgres. DTOs and tests must use `DateOnly`, not `DateTime` or `string`.

### 3. AES-256-CBC encryption for KYC
`KycRecord.IdNumber` stores Aadhaar/PAN encrypted with AES-256-CBC:
- Random 16-byte IV generated per call → prepended to ciphertext → single Base64 blob stored in DB
- Same plaintext encrypts to different ciphertext every time (prevents frequency analysis)
- Key: 32-byte value in `appsettings.Development.json` under `SecuritySettings:EncryptionKey`
- `IEncryptionService` / `EncryptionService` handles Encrypt / Decrypt / Mask
- Decrypt has a try-catch fallback: returns raw value if decryption fails (backward compat for pre-existing rows)
- Admin gets full decrypted value; customer gets masked (last 4 chars visible, rest X)
- Registered as `AddSingleton<IEncryptionService, EncryptionService>()` in Program.cs

### 4. Soft delete with EF Core global query filters
`User`, `Policy`, `Claim`, `Proposal` all have `IsDeleted` + `DeletedAt` fields.
Global query filters in `SpeedClaimDbContext.OnModelCreating`:
```csharp
modelBuilder.Entity<User>().HasQueryFilter(u => !u.IsDeleted);
// ... same for Policy, Claim, Proposal
```
EF Core emits warnings about required navigation properties with query filters — **these warnings are accepted and intentionally NOT suppressed**. Suppressing them would hide a real architectural concern.

### 5. Account lockout
5 failed login attempts → `LockedUntil = DateTime.UtcNow.AddMinutes(15)` on the `User` row.
Fields: `FailedLoginAttempts` (int, default 0), `LockedUntil` (DateTime?, nullable).
Resets to 0 / null on successful login.

### 6. Audit log
`AuditLog` table exists and is actively written to. Write points:
- `AuthService`: register customer, register agent, successful login
- `UserService`: KYC approve/reject
- `ClaimService`: all claim status changes (via `UpdateClaimStatusInternalAsync` — single central hub)
- `ProposalService`: proposal approve/reject
- `PolicyService`: endorsement approve/reject

### 7. DPDP consent (UserConsent table)
Two consent records created at customer registration: `DataProcessing` and `KycDataCollection`.
Stores IP address, consent version ("1.0"), timestamp. Can be revoked (`IsRevoked` + `RevokedAt`).

### 8. CORS
Configured for Angular dev server only (`http://localhost:4200` by default, from `AllowedOrigins` in appsettings).
**CORS is browser-only — Postman bypasses it completely.** No impact on API testing via Postman.

### 9. JWT secret
Uses `Encoding.UTF8.GetBytes(secretKey)` — any long string works, no Base64 decoding needed.
Stored in git-ignored `appsettings.Development.json` under `JwtSettings:Secret`.

### 10. Repository + UnitOfWork pattern
All DB access goes through `IUnitOfWork` → `IRepository<T>`. Direct `DbContext` injection is not used in services. `SaveChangesAsync()` is called via `_unitOfWork.SaveChangesAsync()`.

### 11. Idempotency (three layers)

**Layer 1 — `[Idempotent]` attribute** (`Filters/IdempotentAttribute.cs`):
- `IAsyncActionFilter` applied to specific endpoints (payments, claims, proposals, grievances, endorsements)
- Accepts optional `Idempotency-Key` header (GUID) — without it, request processes normally; with an invalid (non-UUID) value, returns `400`
- Uses `IDistributedCache` (in-memory; swap `AddDistributedMemoryCache()` for Redis in production)
- Caches 2xx responses; 4xx/5xx are NOT cached so clients can retry
- Replay responses include `X-Idempotent-Replay: true` header
- Default TTL: 60 min; payment endpoints use 1440 min (24h)
- Swagger auto-documents the header via `IdempotencyOperationFilter`

**Layer 2 — Stripe idempotency keys** (`FinanceService`):
- `PayPremiumAsync` → `RequestOptions { IdempotencyKey = "pay-premium-{scheduleId}" }`
- `ProcessClaimPayoutAsync` → `RequestOptions { IdempotencyKey = "claim-payout-{claimId}" }`
- Deterministic keys prevent duplicate Stripe charges even without the header middleware

**Layer 3 — Webhook event deduplication** (`PaymentsController.StripeWebhook`):
- `ProcessedWebhookEvent` table tracks Stripe event IDs (`stripe_event_id`, unique indexed)
- Duplicate webhook deliveries return `200 OK` immediately without reprocessing

---

## Security Headers (applied to every response)

```
X-Content-Type-Options: nosniff
X-Frame-Options: DENY
Referrer-Policy: strict-origin-when-cross-origin
X-XSS-Protection: 0
Content-Security-Policy: default-src 'none'; frame-ancestors 'none'
Permissions-Policy: geolocation=(), microphone=(), camera=()
```

Set via inline middleware in `Program.cs` after `UseStaticFiles`.

---

## appsettings.Development.json (git-ignored — never commit)

Contains all real secrets:
- `ConnectionStrings:DefaultConnection` — PostgreSQL connection string
- `JwtSettings:Secret` — JWT signing key
- `Stripe:SecretKey` + `Stripe:WebhookSecret` — Stripe API keys
- `SmtpSettings:AppPassword` — Gmail App Password for SMTP
- `SecuritySettings:EncryptionKey` — 32-byte AES key (Base64, from `openssl rand -base64 32`)

The committed `appsettings.json` has empty/placeholder values for all of these.

---

## Test Patterns (important — deviates from typical Moq usage)

### Use real ConfigurationBuilder, not Mock\<IConfiguration\>
`Mock<IConfiguration>` indexer is not reliably intercepted by Moq.
```csharp
// CORRECT
var config = new ConfigurationBuilder()
    .AddInMemoryCollection(new Dictionary<string, string?> {
        ["SmtpSettings:Host"] = "smtp.test.com",
        ["SmtpSettings:Port"] = "587",
        // ...
    }).Build();

// WRONG — don't do this
var mockConfig = new Mock<IConfiguration>();
mockConfig.Setup(c => c["SmtpSettings:Host"]).Returns("smtp.test.com"); // unreliable
```

### IEncryptionService mock setup (pass-through)
```csharp
_mockEncryptionService = new Mock<IEncryptionService>();
_mockEncryptionService.Setup(e => e.Encrypt(It.IsAny<string>())).Returns<string>(s => s);
_mockEncryptionService.Setup(e => e.Decrypt(It.IsAny<string>())).Returns<string>(s => s);
_mockEncryptionService.Setup(e => e.Mask(It.IsAny<string>())).Returns<string>(s => s);
```

### AuditLogs mock — required in all service tests
Any service that writes audit logs needs:
```csharp
_mockUnitOfWork.Setup(u => u.AuditLogs)
    .Returns(new Mock<IRepository<AuditLog>>().Object);
```
Affects: `ClaimServiceTests`, `ProposalServiceTests`, `UserServiceTests`, `AuthServiceTests`.

### UserConsents mock — required in AuthServiceTests
```csharp
_mockUnitOfWork.Setup(u => u.UserConsents)
    .Returns(new Mock<IRepository<UserConsent>>().Object);
```

### IHttpContextAccessor mock — required in AuthServiceTests
```csharp
var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
mockHttpContextAccessor.Setup(h => h.HttpContext).Returns((HttpContext?)null);
```

---

## Key NuGet Packages

| Package | Version | Note |
| --- | --- | --- |
| Microsoft.AspNetCore.Authentication.JwtBearer | 10.0.0-preview.1.25120.3 | Pinned to exact resolved version to fix NU1603 |
| Microsoft.EntityFrameworkCore | 10.0.0-preview.1.25081.1 | |
| Npgsql.EntityFrameworkCore.PostgreSQL | 10.0.0-preview.1 | |
| BCrypt.Net-Next | 4.2.0 | Password hashing |
| MailKit | 4.17.0 | SMTP email |
| Stripe.net | 51.2.0 | Payments |
| Serilog.AspNetCore | 10.0.0 | Structured logging |
| FluentValidation.AspNetCore | 11.3.0 | Request validation |
| Swashbuckle.AspNetCore | 6.6.2 | Swagger/OpenAPI |

---

## Known Warnings (accepted, not suppressed)

**EF Core query filter warnings** on navigation properties:
> "Entity type 'X' has a global query filter defined and is the required end of a relationship with the entity type 'Y'..."

These appear during `dotnet ef database update` and at runtime. They are real architectural observations about soft-delete interacting with required navigations. They are **intentionally left as warnings** — suppressing them would hide a real concern.

---

## Rate Limiting

- Auth endpoints (`/api/v1/auth/*`): 10 req/60s per IP (`[EnableRateLimiting("auth")]`)
- All other endpoints: 100 req/60s per IP (global limiter)

---

## Frontend Architecture (Angular 21 + Tailwind CSS 4)

### Tech Stack
- **Angular 21** with standalone components (no NgModules)
- **Tailwind CSS 4** via `@tailwindcss/postcss`
- **RxJS 7.8** for reactive state
- **Vitest** for unit testing (not Karma/Jasmine)
- Dev proxy in `proxy.conf.js` forwards `/api/*` → `http://localhost:5062`

### Feature Modules (role-based routing)

| Route prefix | Role | Module |
| --- | --- | --- |
| `/auth` | Guest | Login, Register, Forgot/Reset Password, Verify Email |
| `/` (root) | Customer | Portal — dashboard, policies, claims, proposals, payments, KYC, grievances, products, quotes, family, profile |
| `/agent` | Agent | Dashboard, customers, customer KYC, proposals, policies, commissions, renewals, profile |
| `/claims-officer` | ClaimsOfficer | Dashboard, claims (list + detail), grievances (list + detail), profile |
| `/finance-officer` | FinanceOfficer | Dashboard, payments, payouts, commissions, reports, profile |
| `/underwriter` | Underwriter | Dashboard, proposals, policies, KYC review, endorsements, profile |
| `/surveyor` | Surveyor | Claims, survey report, history, profile |
| `/admin` | Admin | Users, agents, products, system config |

### Guards
Route guards in `core/guards/`: `auth`, `guest`, `admin`, `agent`, `claims-officer`, `finance-officer`, `surveyor`, `underwriter`. Each checks JWT role claim.

### Shared Components
Reusable UI in `shared/components/`: `data-table`, `pagination`, `stat-card`, `status-badge`, `timeline`, `toast`, `confirm-dialog`, `empty-state`, `file-upload`, `skeleton-loader`.

Custom pipes: `date-format`, `money`, `time-ago`.

### Interceptors
- `auth.interceptor.ts` — attaches JWT `Authorization` header
- `error.interceptor.ts` — global HTTP error handling with toast notifications

---

## KYC Duplicate Detection

Aadhaar and PAN uploads now enforce uniqueness across users:
- `UploadAadhaarAsync` / `UploadPanAsync` decrypt all existing records and compare plaintext
- Duplicate detected → `ConflictException` (HTTP 409)
- Same user re-uploading their own document is allowed (re-upload flow)

---

## Commit Message Rules

- **NEVER include `Co-Authored-By: Codex` in any commit message.** User's evaluator will see git history.
- Use conventional commits: `feat:`, `fix:`, `chore:`, `docs:`, `test:`, `refactor:`
- Keep subject line concise; explain *why* in the body if needed

---

## Evaluation Checklist (Monday)

- [ ] Postman workspace with all endpoints preloaded + auth pre-request script
- [ ] DB seeded with realistic data covering all scenarios
- [ ] 100% service layer test coverage (currently 396 tests, all passing)
- [ ] No unhandled exceptions (GlobalExceptionMiddleware covers all routes)
- [ ] Clean git commits with descriptive messages
