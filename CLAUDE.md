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
- **Tests**: backend NUnit + Moq (NOT xUnit — attributes are `[TestFixture]`/`[Test]`, asserts are `Assert.That`); frontend Vitest via `ng test`. Use fresh command output rather than a hard-coded test count.

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
│   └── SpeedClaim.Tests/        # NUnit test project (see Tests line above — NOT xUnit)
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
`KycRecord.AadhaarNumber` and `KycRecord.PanNumber` (separate fields since the `RefactorKycToDualDocument` migration — not a single `IdNumber` field) store Aadhaar/PAN encrypted with AES-256-CBC:
- Random 16-byte IV generated per call → prepended to ciphertext → single Base64 blob stored in DB
- Same plaintext encrypts to different ciphertext every time (prevents frequency analysis)
- Key: 32-byte value in `appsettings.Development.json` under `SecuritySettings:EncryptionKey`
- `IEncryptionService` / `EncryptionService` handles Encrypt / Decrypt / Mask
- Decrypt has a try-catch fallback: returns raw value if decryption fails (backward compat for pre-existing rows)
- All list/detail KYC responses (customer's own view, underwriter/admin review) return the
  value masked (last 4 chars visible, rest X) — nobody gets a decrypted value by default.
  Underwriter/Admin can decrypt on demand via the audited reveal endpoint (see §26).
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
- `PayPremiumAsync` → `RequestOptions { IdempotencyKey = "pay-premium-{scheduleId}-inr" }`
- `ProcessClaimPayoutAsync` → `RequestOptions { IdempotencyKey = "claim-payout-{claimId}-inr" }`
- `ProcessRefundAsync` → `RequestOptions { IdempotencyKey = "refund-{payment.Id}" }`
- Deterministic keys prevent duplicate Stripe charges even without the header middleware
- The `-inr` suffix exists because Stripe idempotency keys are tied to their exact request
  params for 24h — reusing a key after the currency/params changed throws `idempotency_error`.
  If request params change again in the future, version the suffix again rather than reusing.

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
Approved/Rejected; Rejected/Settled are terminal). Also:
- A claim already assigned to another officer cannot be re-assigned (`ConflictException`)
- Status updates require the caller to be the assigned officer (`EnsureAssignedOfficer`)
- **`Approved → Settled` is intentionally NOT a legal transition here** (`CanTransitionClaimStatus`
  returns `false` for `Approved`) and there is no Claims Officer-facing settle endpoint. Settlement
  implies money actually moved, which is a Finance responsibility, not a claims-adjudication one —
  `FinanceService.ProcessClaimPayoutAsync` (real Stripe payout) and `MarkClaimFinanciallySettledAsync`
  (manual settle for out-of-band payment) are the only two paths to `Settled`, both requiring
  `Approved` status and gated to the `FinanceOfficer` role. A prior version had a Claims Officer
  "Mark settled" button/endpoint that bypassed Finance entirely (flipped status with zero payment
  processed, then permanently blocked Finance from ever paying out since their endpoints require
  `Approved`) — this was removed as a segregation-of-duties bug, not a feature.

### 15. File storage and the /uploads URL space

`LocalStorageService` writes to `wwwroot/uploads/<folder>/<guid>.<ext>` and returns the
relative path (`uploads/claims/xxx.pdf`). Rules:
- Allowed extensions: .jpg .jpeg .png .webp .pdf; max 5 MB per file (frontend `app-file-upload`
  usages all pass `[maxSizeMb]="5"` to match)
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

### 19. Payments run in INR on a US-domiciled Stripe test account

Indian Stripe onboarding is halted for this account, so it's a **US test account** paying
in **INR** — Stripe accounts can charge in a currency other than their home country's, so
this works fine for card payments. Practical consequences:
- All `PaymentIntent`/amount math is in paise (`Math.Round(amount * 100)`), `Currency = "inr"`.
- **UPI is not available and cannot be added** — it's a Stripe platform restriction limited
  to India-domiciled accounts, not a code gap. Don't spend time trying to enable it via
  `AutomaticPaymentMethods` or similar; it was verified directly against the Stripe API.
  Card (and Stripe Link) are the only payment methods this account can offer.
- Webhook (`PaymentsController.StripeWebhook`) handles both `payment_intent.succeeded`
  (→ `ReconcilePaymentAsync`) and `payment_intent.payment_failed`
  (→ `MarkPaymentFailedByStripeIntentAsync`, only downgrades a still-`Pending` payment —
  never overwrites an already-`Paid` one).
- `ProcessRefundAsync` creates a **real** Stripe refund (not a local-only status flip):
  requires the payment to be `Paid` (else `UnprocessableException`), rejects an
  already-`Refunded` payment (`ConflictException`), tolerates Stripe's
  `charge_already_refunded` error code as a no-op success, and skips the Stripe call
  entirely if the payment has no `StripePaymentIntentId` (pre-Stripe-integration legacy rows).
- Reconciliation email/notification sends are **post-commit best-effort**
  (`FinanceService.SendEmailBestEffortAsync`): the DB write via `CompleteAsync()` happens
  first and is what the customer's payment status depends on; a failed email is logged and
  swallowed, never rolls back or blocks the financial state change.

### 20. Admin-created accounts never get a plaintext password (agents + staff)

Both `RegisterAgentAsync` (Agent) and `InviteStaffUserAsync` (Underwriter/ClaimsOfficer/
FinanceOfficer/Surveyor) create the `User` row with a random, never-communicated password
hash (`BCrypt.HashPassword(Guid.NewGuid().ToString())`) — there is no admin-typed password
field for either flow. Instead, a `PasswordReset`-type `UserToken` is generated
(`AuthService.CreatePasswordResetTokenAsync`, shared with `ForgotPasswordAsync`) and emailed:
- Staff invites reuse the plain `ForgotPasswordAsync` → `PasswordReset` email template.
- Agent registration sends a dedicated `AgentWelcome` template (via
  `EmailService.SendAgentWelcomeAsync`) with the same reset-token link, styled as an
  account-welcome email rather than a "you requested a reset" one.
- Both templates point at `/auth/reset-password?token=...`, which doubles as the
  first-login flow — the agent/staff member cannot sign in at all until they set a password
  via that link, so there's no separate "force password change on first login" flag needed.
- This was a deliberate fix: agent registration previously took an admin-typed password and
  emailed only the login email, leaving the admin to communicate the password out-of-band
  with no forced first-login change. Don't reintroduce an admin-set-password field for new
  admin-created-account flows — follow this pattern instead.

### 21. Agent management endpoints key off the User ID, not the Agent row's own ID

`AgentProfileDto` carries **two different IDs**: `agentId` (the `Agent` table's own PK) and
`userId` (the linked `User`'s ID). All three admin agent-management endpoints —
`PUT {id}/status`, `PUT {id}/branch/{branchId}`, `PATCH {id}/license` — despite the route
param being named `agentId`, resolve the agent via `Agents.FirstOrDefaultAsync(a => a.UserId
== id)` on the backend (`AgentService`). The frontend (`admin-agents.ts`) must always pass the
**User ID** (`agent.id` from `UserDto`) to these three calls, never `AgentProfileDto.agentId`
— a prior bug had `profile?.agentId ?? agent.id`, which sent the wrong GUID and 404'd
("Agent not found") whenever the agent's profile had loaded, i.e. for essentially every
agent, not just newly-registered ones.

### 22. Two ways a customer becomes "assigned" to an agent

An agent's "My customers" list (`AgentService.GetAssignedCustomersAsync`) is the union of:
1. **Proposal-linked** — any customer with a `Proposal.AgentId` pointing at this agent (set
   when the agent submits a proposal for them via `AgentsController`/`ProposalService`).
2. **Directly onboarded** — `Customer.OnboardingAgentId` set at account-creation time via
   `POST /api/v1/auth/agent/add-customer` (`AuthService.AddCustomerAsync`, agent portal's
   "Add customer" page). This is what lets a brand-new agent acquire their first customer at
   all — without it, "My customers" and the proposal customer-picker were circular (proposal
   requires an existing customer in "My customers", which requires a proposal) and a fresh
   agent could never onboard anyone. `AddCustomerAsync` mirrors `RegisterAgentAsync`'s
   invite-link pattern (see #20): no password is set by the agent, the customer gets a
   `CustomerWelcome` email with a "Set Password & Log In" reset-token link, and
   `IsEmailVerified` is set `true` immediately (the reset link doubles as verification).
   KYC is **not** collected at this step — same as self-registration, the customer submits
   KYC themselves after logging in.
3. Agents can also link to a customer who **already self-registered** (KYC-approved or not)
   via `GET /api/v1/agents/customers/search?q=` (searches all customers, not just this
   agent's) surfaced in the "Submit proposal" page's customer picker — submitting a proposal
   for them is what creates the link in that case, per path 1 above.

DPDP consent (`UserConsent` rows) is recorded as granted at `AddCustomerAsync` time with the
**agent's** IP, not the customer's — a simplification (the agent is assumed to have captured
consent in person before submitting); nothing in the app currently enforces or displays
consent status, so this has no functional effect beyond the audit trail.

### 23. Motor insurance is not age-rated — `Age` is optional end-to-end for it

Health and Life premiums are rated on both age and sum assured (`PremiumRateTable.AgeMin/AgeMax`
+ `SumAssuredMin/Max` → `AnnualPremium`); Motor is rated on sum assured (IDV) alone. This is
enforced in `ProposalService`:
- `IsAgeRatedDomain(domain)` returns `false` only for `"Motor"` (case-insensitive).
- `ValidateProductEligibility` skips the age-range check entirely for Motor; for age-rated
  domains it now explicitly requires `age.HasValue` first (throws "Age is required for this
  product." if not) before checking the range.
- `CalculatePremiumAsync` skips the `AgeMin/AgeMax` filter on rate-table rows for Motor —
  matches on sum-assured band only. It takes the `InsuranceProduct` itself (not just the
  product ID) so it can check `Domain`.
- `GenerateQuoteRequest.Age` is `int?` (was `int`) for this reason — the Motor quote forms
  (agent portal's `proposal-submit.ts` and the customer portal's `quote.ts`/`.html`) don't
  collect an age at all for Motor and send `undefined`/omit it, rather than the old fake
  hardcoded `age = 35` placeholder that used to be silently sent regardless of domain.
- Admin's "Edit rates" modal (`admin-products.html`) hides the Age Min/Max columns when
  editing rates for a Motor product and `addRateBand()` auto-fills a full-range placeholder
  (`0`/`150`) behind the scenes, since those columns are ignored by matching anyway.
- `SubmitProposalAsync` still always computes a real `customerAge` from the customer's KYC'd
  DOB (never a placeholder) — it's just not used for Motor's eligibility/rating anymore.

If a 4th domain is ever added that also isn't age-rated, extend `IsAgeRatedDomain` rather than
special-casing `"Motor"` at each call site.

### 24. Agent-facing customer lists carry real KYC status — used to warn before wasted steps

`UserDto` has three KYC fields (`KycApproved`, `KycStatus`, `KycRejectionReason`) populated
from the customer's actual `KycRecord` — computed once per customer via
`AgentService.GetKycInfoAsync` and threaded through both `GetAssignedCustomersAsync` ("My
customers") and `SearchCustomersAsync` (search-any-customer). This lets the agent portal know
a customer's KYC state *before* the agent burns four wizard steps, instead of discovering it
only when `SubmitProposalAsync`'s hard KYC-approved gate (§14/§22) rejects the submission at
the very end.

`agent/agent-proposals/proposal-submit.ts` uses this: whichever customer is selected (via the
"My customers" dropdown or the customer search) sets `selectedCustomerKycApproved` /
`selectedCustomerKycStatus` / `selectedCustomerKycRejectionReason` from that same `AgentCustomerDto`
— no extra API call needed, the status arrives bundled with the customer object already being
displayed. `nextStep()` blocks leaving Step 1 (Customer) for **any** product domain (not just
Motor — the backend's KYC gate applies to all three) with a toast, and the step also shows a
banner with the specific status: "not submitted yet" / "Pending — awaiting Underwriter review"
/ "Rejected: `{reason}`", the latter two linking to `/agent/customer-kyc?customerId=...`.

`agent/agent-customer-kyc/customer-kyc.ts` (the existing agent-submits-KYC-on-behalf-of-customer
page — uploads Aadhaar/PAN via the same `/api/v1/users/kyc/aadhaar|pan` endpoints a customer
would use themselves) reads that `customerId` query param on init and preselects the customer,
so the link from Submit Proposal lands pre-filled instead of making the agent re-pick from the
dropdown. Approval is still exclusively an Underwriter action via their normal KYC review queue
— nothing about *who approves* KYC changed here, only *how early the agent finds out* it's
blocking them.

### 25. KYC documents are single-upload — no front/back

Aadhaar and PAN each take exactly **one** file upload; there is no "back" document anywhere
in the app (a prior "Aadhaar back (optional)" upload existed only on the agent's
`customer-kyc` page and has been removed — PAN never had a back upload on any page).
- `KycRecord`: `AadhaarDocumentKey` / `PanDocumentKey` (renamed from `...KeyFront`, the
  `...KeyBack` columns are dropped — see migration `RemoveKycBackDocuments`).
- `AadhaarUploadRequest`/`PanUploadRequest`: single `Document` field (renamed from
  `FrontDocument`; `BackDocument` removed). The multipart form field name sent by every
  frontend uploader (customer self-service `profile.service.ts`, agent-on-behalf
  `customer-kyc.ts`) is `document` (was `frontDocument`).
- `KycRecordDto`: `AadhaarDocumentPath` / `PanDocumentPath` (was `...FrontPath` +
  `...BackPath`). The underwriter's KYC review page (`kyc-detail.html`) shows one "View
  document" button per ID type instead of separate Front/Back buttons.
- Existing already-uploaded documents kept their real storage key unchanged (a migration
  column rename, not a file move) — only new uploads write to the new `kyc/{uid}/aadhaar` /
  `kyc/{uid}/pan` key shape (no `/front` suffix).

### 26. KYC numbers are only ever masked in bulk/detail responses — reveal is a separate, audited call

Every `KycRecordDto` (customer's own view, `GetPendingKycAsync`, `GetCustomerKyc`) returns
`AadhaarNumber`/`PanNumber` **already masked** (`EncryptionService.Mask(Decrypt(...))` — last 4
chars visible) — there is no code path that returns a full decrypted number from those. This
used to make the underwriter KYC review page's "Reveal identity" button a no-op: it toggled a
local `revealed` boolean, but both branches displayed the same already-masked string from the
API, so a reviewer could never actually compare the typed number against the uploaded document
photo. Fixed with a dedicated on-demand endpoint:
- `GET /api/v1/users/{customerId}/kyc/reveal` (`UserService.RevealKycIdentityAsync`,
  Underwriter/Admin only) decrypts server-side and returns `KycIdentityRevealDto` (just the two
  numbers, nothing else). Every call writes an audit log (`Action = "KycIdentityRevealed"`,
  actor + target `KycRecord`/`User` IDs) — **never the decrypted value itself** — since this is
  sensitive PII access, not a routine read.
- `kyc-detail.ts`'s `toggleReveal()` calls this once per visit and caches the result
  (`revealedIdentity` signal) — toggling hide/show again doesn't re-hit the API or re-log
  another reveal.
- The old client-side `maskAadhaar`/`maskPan` helpers were removed — they were masking an
  already-masked string a second time, which is why "Reveal identity" looked like it was doing
  something (the output happened to be visually identical either way) when it wasn't.
- CLAUDE.md previously claimed "Admin gets full decrypted value; customer gets masked" (see
  §3) — that was **inaccurate**; decryption was never exposed to any client before this fix.
  Admin now gets the real value through this same reveal endpoint (`Underwriter,Admin` are
  both authorized), not through a separate always-decrypted response.

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
- Dev proxy in `proxy.conf.js` forwards `/api` and `/uploads` → `http://localhost:5062` (see §15 — never add other bare paths)

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
- The Payment Element mounts into its own dedicated, always-present div
  (`#stripe-payment-element-mount`) — **never** clear that div's `innerHTML` manually;
  doing so previously wiped Angular's own loading skeleton out from under itself and left
  a blank modal for several seconds (a real regression, now fixed).
- The checkout modal stays open through the entire post-charge sequence instead of closing
  the instant Stripe confirms — it shows a 2-phase result: success checkmark + amount, then
  "Confirming with SpeedClaim…" while `waitForReconciliation()` polls the schedule (webhook
  lag between Stripe's success and the DB flipping to `Paid` can take several seconds in
  production), then a final "Confirmed — installment marked as Paid" state. Closing is
  disabled once the charge has succeeded but reconciliation hasn't landed yet, so a customer
  can't accidentally lose track of a charged-but-unconfirmed payment.
- Declined cards show Stripe's inline error plus "You haven't been charged" reassurance,
  and the customer can retry in the same modal without losing their place.
- See §19 for the INR/US-account/UPI details and the webhook/refund backend behavior.

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
- In-memory `IDistributedCache` for idempotency — swap for Redis only if productionizing.
- 3D Secure / RBI e-mandate authentication flow for real Indian cards — not yet tested against
  this Stripe integration (deferred by explicit choice: card + webhook UX was prioritized first).

---

## Evaluation Checklist (frontend eval: Tuesday 2026-07-07; deployment eval on AKS: TBD)

- [ ] Postman workspace with all endpoints preloaded + auth pre-request script
- [ ] DB seeded with realistic data covering all scenarios
- [ ] 100% service layer test coverage (use fresh test output when measuring progress)
- [ ] No unhandled exceptions (GlobalExceptionMiddleware covers all routes)
- [ ] Clean git commits with descriptive messages (see Commit Message Rules — no AI attribution)
- [ ] All EF migrations applied (`dotnet ef database update`) — DB-seeded email templates are load-bearing
