# SpeedClaim — Technical Specification v7.0

> Capstone Project | Domains: Health, Life, Motor | Stack: .NET Web API + Angular + PostgreSQL + Stripe + Azure Blob Storage (local first)
>
> **Changelog v3:** Fixed 5 logical data flow gaps — first premium chicken-and-egg, pre-issuance data collection, auth token storage, document entity_type ENUM, surveyor license tracking.
>
> **Changelog v4:** Added Section 6 (Security Controls — account lockout, soft delete, DPDP consent, audit log, security headers, CORS), Section 7 (API Filtering & Pagination), updated schema to 41 tables with `user_consents`.
>
> **Changelog v5:** Added Section 9 (Input Validation) covering all 30 FluentValidation validators across every domain. Access token expiry corrected to 15 minutes.
>
> **Changelog v6:** Fixed customer/surveyor identity resolution — the JWT subject is the `User.Id`, but `Claim`/`Policy`/`Proposal.customer_id` and `Claim.surveyor_id` are FKs to `customers.id` / `surveyors.id`. `PolicyService` and `ClaimService` now resolve `User.Id → Customer.Id` / `Surveyor.Id` before filtering (mirroring `UserService`), so customer-scoped (`/policy/my`, `/claims/my`, claim intimation) and surveyor-scoped (`/claims/surveyor/assigned`, survey report) endpoints return the correct records. Also: `audit_logs.old_value`/`new_value` are `jsonb` and are now JSON-serialized before persistence (previously caused `invalid input syntax for type json` 500s). Test suite expanded to 365. **Changelog v7:** Added three-layer idempotency handling — `[Idempotent]` attribute with `IDistributedCache` on 8 state-changing endpoints, Stripe `RequestOptions.IdempotencyKey` on payment/payout creation, and webhook event deduplication via `ProcessedWebhookEvent` table. Swagger auto-documents the `Idempotency-Key` header via `IdempotencyOperationFilter`. Schema now 42 tables. Test suite expanded to 385.

---

## 1. Technology Stack

| Layer | Technology |
| --- | --- |
| Backend | .NET 10 Web API (C#) |
| Frontend | Angular 22 |
| Database | PostgreSQL |
| Authentication | JWT — Access Token (15 min) + Refresh Token (7 days) |
| Payment | Stripe (US sandbox) |
| Email | Gmail SMTP via MailKit |
| File Storage | Local storage now → Azure Blob Storage later |
| ORM | Entity Framework Core (Code-First) |

---

## 2. Roles

| Role | Description |
| --- | --- |
| `Customer` | Registers, manages family members, completes KYC, buys policies, makes payments, raises claims and grievances |
| `Agent` | Onboards customers, creates proposals, views own dashboard — commissions, policies sold, customer list |
| `Underwriter` | Reviews proposals, assesses risk, approves or rejects policies, requests additional documents |
| `ClaimsOfficer` | Processes claims, verifies documents, coordinates with surveyor, approves cashless pre-auth |
| `FinanceOfficer` | Reconciles Stripe payments, manages refunds and claim payouts, monitors premium schedules |
| `Surveyor` | Assigned to motor claims — inspects vehicle, submits surveyor report |
| `Admin` | Full system access — manages users, roles, products, branches, document requirements, system config |

---

## 3. JWT Authentication

### Token Strategy

- **Access Token** — short-lived (15 minutes), sent in `Authorization: Bearer <token>` header on every request
- **Refresh Token** — long-lived (7 days), stored hashed in the `sessions` table, used only to issue new access tokens
- **OTP / Reset tokens** — short-lived (10–60 minutes), stored in the `user_tokens` table, single-use
- **Email verification tokens** — stored in `user_tokens`, expire after 24 hours

### Flow

1. `POST /api/auth/register` → account created (inactive), verification email sent
2. `POST /api/auth/verify-email` → account activated
3. `POST /api/auth/login` → returns `{ accessToken, refreshToken }`
4. `POST /api/auth/refresh` → rotates both tokens; old refresh token revoked
5. `POST /api/auth/logout` → revokes all active sessions for the user

---

## 4. Rate Limiting

| Policy | Applies To | Limit |
| --- | --- | --- |
| `auth` | `/auth/login`, `/auth/register`, `/auth/forgot-password`, `/auth/reset-password` | 10 requests / 60s per IP |
| Global | All other endpoints | 100 requests / 60s per IP |

Returns **HTTP 429** when exceeded.

---

## 5. PII Encryption — Aadhaar & PAN

Aadhaar numbers and PAN numbers are regulated sensitive identifiers under the **Aadhaar Act 2016** and **DPDP Act 2023**. Storing them in plaintext is a compliance violation. SpeedClaim encrypts them at rest using AES-256-CBC before writing to the database.

### Algorithm: AES-256-CBC with Random IV

| Property | Value |
| --- | --- |
| Algorithm | AES (Advanced Encryption Standard) |
| Key size | 256 bits (32 bytes) |
| Mode | CBC — Cipher Block Chaining |
| IV | 16 random bytes generated fresh per encryption |
| Storage format | `Base64( IV + Ciphertext )` — IV prepended to ciphertext, encoded as a single Base64 string |
| Key source | `SecuritySettings:EncryptionKey` in appsettings (never committed to git) |

**Why AES-256-CBC?**

- AES-256 is the global standard mandated by PCI-DSS, HIPAA, and the DPDP Act for PII at rest.
- CBC mode with a random IV ensures the same plaintext never produces the same ciphertext across two encryptions — preventing frequency analysis attacks.
- The IV is not secret; prepending it to the ciphertext is the standard practice so decryption is self-contained.

### Where it applies

| Field | Model | Table |
| --- | --- | --- |
| `IdNumber` (Aadhaar or PAN) | `KycRecord` | `kyc_records` |

### Read behaviour

| Caller | Returns |
| --- | --- |
| Admin (`GET /api/v1/users/kyc/pending`) | Full decrypted value — admin must see it to verify identity |
| Customer (`GET /api/v1/users/kyc/me`) | Masked value — last 4 digits visible, rest replaced with `X` |

### Key management

- Generate with: `openssl rand -base64 32`
- Store in `appsettings.Development.json` (git-ignored) for local development
- In production, inject via environment variable or a secrets manager — never in source code

---

## 6. Security Controls

### 6.1 Account Lockout

Brute-force protection is implemented directly on the `users` table — no external cache required.

| Field | Type | Behaviour |
| --- | --- | --- |
| `failed_login_attempts` | `int` (default 0) | Incremented on every wrong password |
| `locked_until` | `timestamp?` (nullable) | Set to `UtcNow + 15 minutes` after 5 consecutive failures |

- On a correct password: both fields are reset to `0` / `null`.
- On a locked account: login is rejected immediately — password is never checked.
- Lockout status is visible in the audit log (`Action = "LoginFailed"` / `"LoginSuccess"`).

### 6.2 Soft Delete

`User`, `Policy`, `Claim`, and `Proposal` are never hard-deleted. Instead:

| Field | Type |
| --- | --- |
| `is_deleted` | `boolean` (default `false`) |
| `deleted_at` | `timestamptz?` |

EF Core global query filters (`HasQueryFilter`) automatically exclude soft-deleted rows from every query — no per-query `WHERE is_deleted = false` is needed. EF Core emits warnings about required navigations interacting with query filters; these are accepted as an honest architectural observation and intentionally not suppressed.

### 6.3 DPDP Consent (Digital Personal Data Protection Act 2023)

Two consent records are created for every new customer at registration:

| `consent_type` | Purpose |
| --- | --- |
| `DataProcessing` | Processing personal data for policy issuance and claims |
| `KycDataCollection` | Collecting and storing Aadhaar / PAN under the Aadhaar Act 2016 |

Each record stores: `user_id`, `consent_type`, `is_granted`, `consented_at`, `consent_version` ("1.0"), `ip_address`, and revocation fields (`is_revoked`, `revoked_at`).

### 6.4 Audit Log

Every sensitive action writes a record to `audit_logs` with `entity_type`, `entity_id`, `action`, `old_value`, `new_value`, `user_id`, and `created_at`.

| Service | Actions audited |
| --- | --- |
| `AuthService` | Customer registration, agent registration, successful login |
| `UserService` | KYC approved, KYC rejected |
| `ClaimService` | Every claim status transition (single central hub — `UpdateClaimStatusInternalAsync`) |
| `ProposalService` | Proposal approved, proposal rejected |
| `PolicyService` | Endorsement approved, endorsement rejected |

### 6.5 Security Response Headers

Applied to every HTTP response via inline middleware:

| Header | Value |
| --- | --- |
| `X-Content-Type-Options` | `nosniff` |
| `X-Frame-Options` | `DENY` |
| `Referrer-Policy` | `strict-origin-when-cross-origin` |
| `X-XSS-Protection` | `0` (disabled — browsers implement their own XSS filtering) |
| `Content-Security-Policy` | `default-src 'none'; frame-ancestors 'none'` |
| `Permissions-Policy` | `geolocation=(), microphone=(), camera=()` |

### 6.6 CORS

Requests from browser clients are restricted to the Angular dev server origin (`http://localhost:4200` by default, configurable via `AllowedOrigins` in appsettings). CORS is a browser enforcement mechanism — it has no effect on Postman or other non-browser API clients.

---

## 7. API Filtering & Pagination

All list endpoints that return collections support server-side filtering. Filtering and pagination are applied in the database via EF Core LINQ — no in-memory filtering.

### Claims

| Endpoint | Role | Query Params |
| --- | --- | --- |
| `GET /api/v1/claims/my` | Customer | `?status=` `?type=` |
| `GET /api/v1/claims/all` | ClaimsOfficer, Admin | `?page=` `?pageSize=` `?status=` `?type=` |

**`status` values:** `Intimated`, `DocumentsPending`, `PreAuthRequested`, `PreAuthApproved`, `UnderReview`, `Approved`, `Rejected`, `Settled`, `Withdrawn`

**`type` values:** `Death`, `Maturity`, `Health`, `Accident`, `Theft`, `NaturalDamage`

### Policies

| Endpoint | Role | Query Params |
| --- | --- | --- |
| `GET /api/v1/policies/my` | Customer | `?status=` `?type=` |
| `GET /api/v1/policies/all` | Underwriter, Admin | `?page=` `?pageSize=` `?status=` `?type=` |

**`status` values:** `Pending`, `Active`, `Lapsed`, `Cancelled`, `Expired`, `Claimed`

**`type` values:** `Individual`, `FamilyFloater`

All filter params are optional. Invalid or unrecognised values are silently ignored (no filter applied). Pagination defaults: `page=1`, `pageSize=20`.

Paginated responses follow the `PagedResponse<T>` envelope:

```json
{
  "data": [...],
  "pageNumber": 1,
  "pageSize": 20,
  "totalRecords": 84,
  "totalPages": 5
}
```

---

## 8. Database Schema Overview

42 tables across 18 domains.

| Domain | Tables | Count |
| --- | --- | --- |
| Auth & Users | users, sessions, user_tokens, addresses | 4 |
| Consent | user_consents | 1 |
| Customers | customers, customer_members | 2 |
| KYC | kyc_records | 1 |
| Branches | branches | 1 |
| Agents | agents, agent_commissions | 2 |
| Surveyors | surveyors | 1 |
| Products | insurance_products, premium_rate_tables, document_requirements | 3 |
| Proposals | proposals, proposal_members | 2 |
| Domain Details | health_details, life_details, motor_details | 3 |
| Nominees | nominees | 1 |
| Policies | policies, policy_members, policy_status_history, endorsements | 4 |
| Payments | stripe_customers, premium_schedules, premium_payments | 3 |
| Claims | claims, claim_status_history, health_claim_details, life_claim_details, motor_claim_details | 5 |
| Grievances | grievances | 1 |
| Documents | submitted_documents | 1 |
| Notifications & Email | notifications, email_templates, email_logs | 3 |
| Audit & Config | audit_logs, system_config | 2 |
| Idempotency | processed_webhook_events | 1 |
| **Total** | | **42** |

---

## 9. Input Validation

All request bodies are validated using **FluentValidation** with `AddFluentValidationAutoValidation()`. Validation runs automatically before any controller action is reached. Invalid requests return **HTTP 422** with a structured error body listing all violated rules.

30 validators are auto-registered from the assembly. Key rules by domain:

### 9.1 Authentication

| Request | Rules |
| --- | --- |
| `LoginRequest` | Email format, password not empty |
| `ForgotPasswordRequest` | Email format |
| `ResetPasswordRequest` | Token not empty; new password: min 8 chars, upper, lower, digit, special character |
| `VerifyEmailRequest` | Token not empty |
| `RefreshTokenRequest` | Token not empty |
| `RegisterAgentRequest` | Same password rules as customer; Aadhaar 12 digits; PAN `ABCDE1234F`; phone 10 digits; postal code 6 digits; license number and agency name required |

### 9.2 Claims

| Request | Rules |
| --- | --- |
| `IntimateClaimRequest` | Policy ID not empty; amount > 0; incident date not in future; description 10–2000 chars |
| `ApproveRejectClaimRequest` | On approve: `approvedAmount` required and > 0. On reject: `reason` required, min 10 chars |
| `UpdateClaimStatusRequest` | Status must be a valid `ClaimStatus` enum value; remarks required |
| `AssignSurveyorRequest` | Surveyor ID not empty; notes required |

### 9.3 Proposals & Policies

| Request | Rules |
| --- | --- |
| `GenerateQuoteRequest` | Product ID not empty; age 1–100; sum assured > 0; tenure ≥ 1 |
| `SubmitProposalRequest` | Customer ID and Product ID must be valid GUIDs; sum assured, premium, tenure all > 0; payment frequency must be Monthly/Quarterly/HalfYearly/Annually; **nominee shares must sum to exactly 100%** |
| `NomineeDto` (nested) | Full name required; DOB in past; share 0.01–100; if minor, appointee name required |
| `MotorDetailDto` (nested) | Vehicle number, make, model, engine, chassis required; manufacture year 1900–current year; IDV > 0 |
| `RequestEndorsementRequest` | Endorsement type valid enum; description 10–1000 chars |
| `ApproveRejectEndorsementRequest` | On reject: reason required |
| `UpdateNomineeRequest` | Full name required; DOB in past; share 0.01–100; if minor, appointee name required |

### 9.4 Users & KYC

| Request | Rules |
| --- | --- |
| `AddFamilyMemberRequest` | Name required; DOB in past; salutation, gender, relationship valid enums |
| `UpdateFamilyMemberRequest` | Same rules as Add |
| `KycUploadRequest` | ID type valid enum; **Aadhaar: 12 digits; PAN: `ABCDE1234F`; Passport: 1 letter + 7 digits**; front document required; file size ≤ 5 MB; allowed types: PDF, JPG, JPEG, PNG |
| `SingleAddressRequest` | Line 1, city, state, country required; **pincode exactly 6 digits** |

### 9.5 Grievances

| Request | Rules |
| --- | --- |
| `RaiseGrievanceRequest` | Category valid enum; description 10–2000 chars |
| `UpdateGrievanceStatusRequest` | Status valid enum; **resolution notes required when status is Resolved or Closed** |

### 9.6 Catalog (Admin)

| Request | Rules |
| --- | --- |
| `CreateProductRequest` | Name, UIN, description required; domain must be Health/Life/Motor; `minAge < maxAge ≤ 100`; `minSumAssured < maxSumAssured`; `minTenure ≤ maxTenure`; if family floater allowed, max members > 1 |

### 9.7 Agents & System (Admin)

| Request | Rules |
| --- | --- |
| `UpdateAgentProfileRequest` | Name required; phone 10 digits; salutation valid |
| `UpdateAgentLicenseRequest` | License number required; expiry must be a **future date** |
| `CreateBranchRequest` | Name, city, state, address required; phone 10 digits; email format |
| `UpdateSystemConfigRequest` | Key and value both required |
| `ManageEmailTemplateRequest` | Template key, subject, and HTML body all required |

---

## 10. Idempotency

Prevents duplicate side-effects when clients retry requests (network timeouts, double-clicks). Three layers:

### 10.1 `[Idempotent]` Attribute (API-level)

An `IAsyncActionFilter` applied to state-changing endpoints. Clients must send an `Idempotency-Key` header containing a UUID.

**Behaviour:**
- **No header** → request processes normally (opt-in, not mandatory)
- **Invalid header (non-UUID)** → `400 Bad Request`
- **Cache hit (key already processed)** → returns cached response with `X-Idempotent-Replay: true` header
- **Cache miss** → processes request, caches 2xx response (4xx/5xx are not cached so clients can retry)
- **Cache backend** → `IDistributedCache` (in-memory; swappable to Redis)
- **Default TTL** → 60 minutes; payment endpoints use 1440 minutes (24 hours)

**Protected endpoints:**

| Controller | Endpoint | Cache TTL |
| --- | --- | --- |
| PaymentsController | `POST pay/{scheduleId}` | 24h |
| PaymentsController | `POST payout/claim/{claimId}` | 24h |
| PaymentsController | `POST {paymentId}/refund` | 1h |
| PaymentsController | `POST commissions/{id}/approve` | 1h |
| ClaimsController | `POST intimate` | 1h |
| ProposalsController | `POST` (submit) | 1h |
| PolicyController | `POST {id}/endorsements` | 1h |
| GrievancesController | `POST` (raise) | 1h |

### 10.2 Stripe Idempotency Keys

Deterministic `RequestOptions.IdempotencyKey` passed to Stripe SDK calls:
- `pay-premium-{scheduleId}` for `PayPremiumAsync`
- `claim-payout-{claimId}` for `ProcessClaimPayoutAsync`

Same schedule/claim always produces the same Stripe key → retries reuse the same PaymentIntent.

### 10.3 Webhook Event Deduplication

`ProcessedWebhookEvent` table stores Stripe event IDs (`stripe_event_id`, unique index). Duplicate webhook deliveries return `200 OK` without reprocessing.

---

> Document version: 7.0 | Stack: .NET 10 Web API + Angular + PostgreSQL + Stripe
