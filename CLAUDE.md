# SpeedClaim — CLAUDE.md

Persistent context for AI sessions. Read this before touching any code.

---

## Project Overview

**SpeedClaim** is a full-stack insurance claims management platform built as an internship capstone project.

- **Backend**: .NET 10 Web API (C#), EF Core, PostgreSQL
- **Frontend**: Angular 21 + Tailwind CSS 4 (in `frontend/`)
- **Payments**: Stripe (backend PaymentIntents + frontend Stripe Elements checkout)
- **Email**: MailKit over SMTP (Gmail App Password); templates stored in DB (see §12)
- **Auth**: JWT Bearer tokens, validated per-request against session + user state (see §13)
- **Tests**: backend NUnit 4 + Moq (NOT xUnit — attributes are `[TestFixture]`/`[Test]`, asserts are `Assert.That`), 453 tests, all passing; frontend Vitest via `ng test`

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
│   ├── spec.md                  # Architecture + API spec
│   └── speedclaim-design-system.md  # Frontend design tokens / visual language
└── CLAUDE.md                    # This file
```

Uploaded files (KYC docs, claim docs, avatars, survey photos) live in
`backend/SpeedClaim.Api/wwwroot/uploads/<folder>/` and are served by the API at
`/uploads/...` (see §15).

---

## Common Commands

```bash
# Run API (from repo root)
dotnet run --project backend/SpeedClaim.Api

# Run Angular frontend — plain `ng`, no npx prefix (CLI is installed globally)
cd frontend && ng serve            # serves at http://localhost:4200
# Use `ng serve` or `ng build` to catch compilation errors (not tsc — it misses template errors)
# One-shot compile check (CI-style): ng build --configuration development

# Run all backend tests
dotnet test backend/SpeedClaim.Tests

# Run frontend tests (Vitest)
cd frontend && ng test --watch=false

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

### 6. Audit log (two mechanisms)

**A. Semantic entries** — every service writes named `AuditLog` rows (`Action` like
`ClaimIntimated`, `PaymentReconciled`, `AgentLicenseUpdated`, `ProfileUpdated`,
`StaffInvited`, `GrievanceRaised`, `UserRoleChanged`, `PasswordResetByAdmin`, …).
Coverage now spans: Auth, User, Claim (via `UpdateClaimStatusInternalAsync` central hub),
Proposal, Policy, Finance, Grievance, Agent, Product, System services.
When writing new ones: capture the OLD value snapshot **before** mutating the entity
(a bug where oldValue == newValue was fixed once already).

**B. Generic change-tracker entries** — `SpeedClaimDbContext.Audit.cs` (`OnBeforeSaveChanges`)
logs entity adds/updates/deletes with actor user id + IP address. Noisy infrastructure
entities are excluded via the `_auditExclusions` HashSet (Session, UserToken, Notification,
EmailLog, status histories, PremiumSchedule/Payment, Customer, ProcessedWebhookEvent).

Admin views logs at `GET /api/v1/system/audit-logs` — paged, with `search`/`from`/`to`
filters, sorted newest-first; actor GUIDs are resolved to display names.

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

### 12. Email templates live in the DB (seeded)

All transactional emails render from the `email_templates` table, NOT hardcoded HTML:
- Seeded via `HasData` in `SpeedClaimDbContext.cs` + migrations. Keys:
  `EmailVerification`, `PasswordReset`, `PolicyActivated`, `KycApproved`, `KycRejected`,
  `ProposalApproved`, `ProposalRejected`, `ClaimApproved`, `ClaimRejected`, `ClaimSettled`,
  `ClaimIntimated`, `PolicyCancelled`, `EndorsementApproved`, `EndorsementRejected`,
  `GrievanceFiled`, `GrievanceResolved`, `PremiumOverdue`
- `EmailService.SendTemplatedEmailAsync(key, variables, to, attachment?)` substitutes
  `{{variable}}` placeholders; `{{year}}` is injected automatically
- **A missing/inactive template throws `InvalidOperationException`** — this means the DB
  isn't migrated, not a user error. Run `dotnet ef database update` after pulling.
- Admin can edit templates in the console (`PATCH /api/v1/system/email-templates`);
  the frontend preview substitutes sample values from `TEMPLATE_DUMMIES` in `admin-system.ts`
- Always `WebUtility.HtmlEncode` user-supplied values before passing as template variables

### 13. JWT validated against live state on every request

`Program.cs` `OnTokenValidated` checks, per request:
1. the session row exists, matches the user, is not revoked, and is not expired
2. the user still exists, is active (`IsActive`), and **the user's current role matches
   the role claim in the token** — deactivating a user or changing their role kills
   their existing tokens immediately

### 14. Claim workflow state machine

`ClaimService.CanTransitionClaimStatus` enforces legal transitions
(Intimated → UnderReview/DocumentsPending; UnderReview → DocumentsPending/PreAuthRequested/
Approved/Rejected; Approved → Settled; Rejected/Settled are terminal). Also:
- A claim already assigned to another officer cannot be re-assigned (`ConflictException`)
- Status updates require the caller to be the assigned officer (`EnsureAssignedOfficer`)

### 15. File storage and the /uploads URL space

`LocalStorageService` writes to `wwwroot/uploads/<folder>/<guid>.<ext>` and returns the
relative path (`uploads/claims/xxx.pdf`). Rules:
- Allowed extensions: .jpg .jpeg .png .webp .pdf; max 10 MB
- Folders in use: `claims`, `kyc`, `proposals/<id>`, `avatars`, `survey`
- Frontend builds URLs as `'/' + filePath` → `/uploads/...`, served by `UseStaticFiles`
- **Dev proxy forwards ONLY `/api` and `/uploads` to the backend. NEVER add bare paths
  like `/claims` or `/kyc` to `proxy.conf.js` — those are Angular SPA routes and proxying
  them breaks page refresh / deep links.** (This regression happened once.)

### 16. Refresh token storage & remember-me

`TokenService` keeps the access token in memory only. The refresh token goes to
`localStorage` when the user checked "remember me", otherwise `sessionStorage`.
`setTokens(access, refresh, persistent?)`: when `persistent` is **omitted** (silent
refresh path in `AuthService.refreshToken()`), the token stays in whichever storage it
currently occupies — do not pass `false` there or remembered logins silently degrade.
Saved login email lives under the `sc_saved_email` localStorage key.

### 17. HTTP verb convention

Field/partial updates use **PATCH** (profile, agent license, nominees, product rates &
documents, proposal notes, system configs, email templates, branches, family members,
addresses). **PUT** is reserved for workflow actions (status changes, assign, review,
cancel, settle, reconcile). Keep frontend service calls in sync — a verb mismatch
returns 405.

### 18. JSON contract: camelCase + string enums

`JsonStringEnumConverter` is registered globally: enums serialize as their names
(`"Approved"`, `"Spouse"`) and bind from strings. DTOs are C# records → camelCase JSON.
**Frontend `core/models/api.models.ts` must mirror backend DTO records exactly** —
a mismatched property name binds as null/default *silently* (this broke the family
members feature once: frontend sent `salutationTitle`/`name`, backend wanted
`salutation`/`firstName`/`lastName`).

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

### AuditLogs mock — required in ALL service test fixtures
Every service now writes audit logs, so every fixture needs:
```csharp
_mockUnitOfWork.Setup(u => u.AuditLogs)
    .Returns(new Mock<IRepository<AuditLog>>().Object);
```
Affects: Claim, Proposal, User, Auth, Agent, Product, Grievance, Finance, System —
i.e. all service test classes. A missing setup shows up as `NullReferenceException`
inside the service at the `AuditLogs.AddAsync` line.

### ProposalServiceTests — detail-enrichment mocks
`ProposalService.GetByIdAsync` reads members/nominees/documents, so the fixture needs
`FindAsync → empty list` setups for `ProposalMembers`, `Nominees`, and
`SubmittedDocuments` (a loose mock returns **null** from `FindAsync`, which NREs).

### Actor IDs must be real GUID strings
Service methods that take `adminId`/`financeOfficerId`/`officerId` strings call
`Guid.Parse` on them for the audit entry — tests must pass `Guid.NewGuid().ToString()`,
never `"admin123"`.

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
| `/underwriter` | Underwriter | Dashboard, proposals (with documents/members/nominees), policies, KYC review, endorsements, profile |
| `/surveyor` | Surveyor | Claims, survey report (localStorage draft autosave), history, profile |
| `/admin` | Admin | Users (invite staff, reset password, bulk deactivate), agents, products, system (configs, email template editor + preview, audit log viewer, notification logs) |

**Customer portal routes are root-level** (`/claims`, `/proposals`, `/kyc`, …) — they can
collide with proxy contexts and backend paths; see §15 before touching `proxy.conf.js`.

### Guards
Route guards in `core/guards/`: `auth`, `guest`, `admin`, `agent`, `claims-officer`, `finance-officer`, `surveyor`, `underwriter`. Each checks JWT role claim.

### Shared Components
Reusable UI in `shared/components/`: `data-table`, `pagination`, `stat-card`, `status-badge`, `timeline`, `toast`, `confirm-dialog`, `empty-state`, `file-upload`, `skeleton-loader`, `app-select` (custom styled select).

Custom pipes: `date-format`, `money`, `time-ago`.

### Interceptors
- `auth.interceptor.ts` — attaches JWT `Authorization` header
- `error.interceptor.ts` — global HTTP error handling with toast notifications

### Stripe checkout (customer pay-premium page)
- Stripe.js is loaded **async** in `index.html`; `pay-premium.ts` guards with
  `typeof Stripe === 'undefined'` before use — keep that guard
- Backend `POST /api/v1/payments/pay/{scheduleId}` returns `clientSecret` +
  `publishableKey` (from `Stripe:PublishableKey` in appsettings — must be set in
  `appsettings.Development.json` or the UI shows "Payment configuration error")
- Flow: create intent → mount Payment Element → `confirmPayment(redirect: 'if_required')`
  → refresh schedule; webhook + reconcile handle the backend side

### Profile rules (backend-enforced, frontend-mirrored)
Once KYC is **Approved**, `firstName`, `lastName`, and `dateOfBirth` are locked:
`UserService.UpdateProfileAsync` ignores them and the profile form disables the controls
(`UserDto.kycApproved` drives this). Phone, salutation, marital status stay editable.

---

## KYC Duplicate Detection

Aadhaar and PAN uploads now enforce uniqueness across users:
- `UploadAadhaarAsync` / `UploadPanAsync` decrypt all existing records and compare plaintext
- Duplicate detected → `ConflictException` (HTTP 409)
- Same user re-uploading their own document is allowed (re-upload flow)

---

## Commit Message Rules

- **NEVER include `Co-Authored-By: Claude` in any commit message.** User's evaluator will see git history.
- Use conventional commits: `feat:`, `fix:`, `chore:`, `docs:`, `test:`, `refactor:`
- Keep subject line concise; explain *why* in the body if needed

---

## Deferred / Not Implemented (known, intentional)

- **P2-B scheduled email reminders** (`PremiumDueReminder`, `PolicyExpiryReminder`):
  need a `BackgroundService` + `PeriodicTimer`; deliberately deferred. All other
  notifications are event-driven and implemented.
- `updateFamilyMember` exists in `profile.service.ts` + backend PATCH endpoint, but no
  UI calls it yet (add-only flow in the UI).
- In-memory `IDistributedCache` for idempotency — swap for Redis only if productionizing.

---

## Evaluation Checklist (frontend eval: Tuesday 2026-07-07; deployment eval on AKS: TBD)

- [ ] Postman workspace with all endpoints preloaded + auth pre-request script
- [ ] DB seeded with realistic data covering all scenarios
- [ ] 100% service layer test coverage (currently 453 tests, all passing)
- [ ] No unhandled exceptions (GlobalExceptionMiddleware covers all routes)
- [ ] Clean git commits with descriptive messages (see Commit Message Rules — no AI attribution)
- [ ] All EF migrations applied (`dotnet ef database update`) — DB-seeded email templates are load-bearing
