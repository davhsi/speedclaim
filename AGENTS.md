# SpeedClaim Codex Memory

This file is the Codex-facing project memory. Read it before changing code. `CLAUDE.md`
is still the fuller historical handoff and should remain the first reference for detailed
business workflow notes.

## Project Shape

- SpeedClaim is a full-stack insurance platform.
- Backend: .NET 10 Web API in `backend/SpeedClaim.Api`, EF Core code-first, PostgreSQL,
  repository + unit-of-work services, NUnit tests in `backend/SpeedClaim.Tests`.
- Frontend: Angular 21/22 standalone components in `frontend/`, Tailwind CSS 4, Vitest via
  `ng test`.
- Main domains: auth, users/customers/agents, proposals, policies, claims, grievances,
  finance/payments, KYC, admin system/config/email templates/audit.
- Uploaded files are served from backend `wwwroot/uploads/...`; Angular proxy must only
  forward `/api` and `/uploads`.

## Current Repository State

- As of 2026-07-11, the worktree already contains many modified backend/frontend files and
  several untracked migration/spec files before Codex started. Treat them as prior user/Claude
  work. Do not revert or overwrite them unless explicitly asked.
- As of 2026-07-14, Azure deployment preparation was split into conventional commits:
  - `feat(deploy): prepare backend for Azure deployment`
  - `feat(profile): add staff avatar upload`
- `docs/azure-deployment-plan.md` and `k8s/` are intentionally uncommitted local deployment
  planning/experiment files for now. Do not stage them unless the user explicitly asks.
- Last audit verification on 2026-07-11:
  - `dotnet build backend/SpeedClaim.Api` passed with 0 warnings/errors.
  - `dotnet test backend/SpeedClaim.Tests` passed: 510 tests.
  - `cd frontend && npm run build -- --configuration development` passed.
  - `cd frontend && npm test -- --watch=false` passed: 1140 tests.
- Root `CLAUDE.md` and `README.md` have stale backend test counts; use fresh test output before
  reporting status.
- `README.md` says Angular 22, while `frontend/package.json` currently pins Angular `^21.2.x`.
  Prefer package metadata/source over README when in conflict.
- Local Node observed during frontend build/test is v25.9.0, which Angular/npm warn is odd
  numbered and not production LTS.

## Commands

Run from repo root unless noted:

```bash
dotnet build backend/SpeedClaim.Api
dotnet test backend/SpeedClaim.Tests
cd frontend && ng build --configuration development
cd frontend && ng test --watch=false
```

Frontend notes:

- `ng build` catches Angular template errors; plain `tsc` is not enough.
- `ng serve` uses `frontend/proxy.conf.js` and normally serves on `http://localhost:4200`.

Backend notes:

- `dotnet ef database update --project backend/SpeedClaim.Api` applies migrations.
- Real secrets belong only in `backend/SpeedClaim.Api/appsettings.Development.json`.
- Build the backend container from the repo root:
  `docker build -f backend/SpeedClaim.Api/Dockerfile -t speedclaim-api:local .`

## Azure Deployment State

- Resource group: `rg-davish`; subscription: `Training-2026`.
- Current Azure services created/tested:
  - Azure Blob Storage account `stspeedclaimdavish`, container `speedclaim-uploads`.
  - Azure Key Vault `kv-speedclaim-davish`.
  - Azure Database for PostgreSQL flexible server `speedclaim`
    (`speedclaim.postgres.database.azure.com`).
  - Azure Container Registry `acrspeedclaim` (`acrspeedclaim.azurecr.io`).
  - AKS cluster `aks-speedclaim-davish`.
- Backend image pushed to ACR: `acrspeedclaim.azurecr.io/speedclaim-api:v1`.
- Local Docker backend has already been verified against Azure PostgreSQL and Azure Blob
  Storage with the local Angular frontend.
- The AKS registry attach flow failed because the user has Contributor access but not
  `Microsoft.Authorization/roleAssignments/write`. Use an ACR image pull secret as a temporary
  demo workaround, or ask an Owner/User Access Administrator to grant `AcrPull` to the AKS
  kubelet identity.
- Secret-management approaches under evaluation:
  1. .NET SDK / Azure Key Vault configuration provider, avoiding app Kubernetes Secrets.
  2. Secrets Store CSI driver syncing Key Vault values into Kubernetes Secrets and env vars.
  3. Secrets Store CSI driver direct file mounts without Kubernetes Secret sync.
- Senior-engineer preference for the final demo is Key Vault-backed secrets, ConfigMaps for
  non-sensitive config, and no committed secret manifests.

## Backend Invariants

- EF is configured for PostgreSQL snake_case names in `SpeedClaimDbContext`. Raw SQL must use
  snake_case column/table names.
- Date-only business fields use `DateOnly`, not `DateTime` or strings.
- JSON enums are string names via `JsonStringEnumConverter`; frontend DTO names must mirror
  backend record properties exactly in camelCase.
- Services should use `IUnitOfWork`/repositories, not direct `DbContext` injection.
- Soft-delete query filters exist for `User`, `Policy`, `Claim`, and `Proposal`.
- JWT validation checks the session row and current user state on every request. Role changes
  or deactivation invalidate existing tokens.
- KYC Aadhaar/PAN fields are encrypted and normally returned masked. Full reveal is through the
  audited `GET /api/v1/users/{customerId}/kyc/reveal` path only.
- Claims settlement is a finance responsibility. Do not reintroduce a claims-officer path that
  moves `Approved` claims to `Settled` without finance/payment handling.
- Email templates are DB seeded and rendered by key. Missing templates indicate migrations or
  seed data are out of date, not a normal user error.
- `Storage:Provider` selects storage implementation. `AzureBlob` uses `AzureBlob:ConnectionString`
  and `AzureBlob:ContainerName`; `Local` uses the existing local upload path.
- Admin-created agents/staff/customers should receive reset-token welcome flows, not admin-set
  plaintext passwords.
- Agent management routes that look like `agentId` currently key off linked `UserId`; frontend
  calls should pass the user id for those endpoints.

## Frontend Invariants

- Standalone Angular components and route files are organized by role under
  `frontend/src/app/features`.
- Customer portal routes are root-level (`/claims`, `/policies`, `/kyc`, etc.); do not add
  those bare route names to the dev proxy.
- Access tokens are memory-only. Refresh tokens are in localStorage only for remember-me,
  otherwise sessionStorage. Silent refresh must preserve the existing persistence choice.
- Global API models live in `frontend/src/app/core/models/api.models.ts`; keep them synchronized
  with backend DTOs.
- Stripe checkout depends on backend returning `clientSecret` and `publishableKey`; the UI also
  guards for missing async-loaded `Stripe`.

## Testing Patterns

- Backend tests are NUnit, not xUnit. Use `[TestFixture]`, `[Test]`, and `Assert.That`.
- Service tests commonly need `AuditLogs` mocked on `IUnitOfWork` because services write audit
  rows.
- Prefer real `ConfigurationBuilder().AddInMemoryCollection(...)` over `Mock<IConfiguration>`
  for configuration-dependent tests.
- Test actor ids passed to services must be parseable GUID strings.
- Encryption service mocks usually pass through `Encrypt`, `Decrypt`, and `Mask`.

## Working Rules for Future Codex Sessions

- Read `CLAUDE.md`, this file, and `git status --short` before edits.
- Assume existing modified files are not yours. Preserve user/Claude work and make narrow patches.
- Before changing frontend contracts, inspect the matching backend DTO/service/controller and the
  Angular model/service together.
- Before changing workflow status logic, inspect both service state machine and role-specific UI.
- Verify with the narrowest meaningful backend/frontend tests or builds, and report any command
  that was not run.
