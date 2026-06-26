# SpeedClaim — End-to-End Test Checklist

**Testing date:** 2026-06-26  
**Tester:** Claude Code (automated browser + API)  
**Source of truth:** This file tracks every tested scenario, found issues, and fix status.

---

## Credentials

| Role | Email | Password |
|------|-------|----------|
| Customer | davish.cs22@bitsathy.ac.in | Password@123 |
| Admin | davish.std@gmail.com | Password@123 |
| Underwriter | underwriter.qa@speedclaim.com | Password@123 |
| Claims Officer | claimsofficer.qa@speedclaim.com | Password@123 |
| Finance Officer | financeofficer.qa@speedclaim.com | Password@123 |
| Surveyor | surveyor.qa@speedclaim.com | Password@123 |
| Agent | agent2.qa@speedclaim.com | TempPass@123 |

---

## Phase 1 — Auth Flows

| # | Scenario | Status | Notes |
|---|----------|--------|-------|
| 1.1 | Login with valid customer credentials | ⬜ | |
| 1.2 | Login with wrong password → error message | ⬜ | |
| 1.3 | Login with non-existent email → error | ⬜ | |
| 1.4 | Guest guard: accessing `/policies` without login redirects to `/auth/login` | ⬜ | |
| 1.5 | Auth guard: accessing `/auth/login` while logged in redirects to dashboard | ⬜ | |
| 1.6 | Logout clears session and redirects to login | ⬜ | |
| 1.7 | Forgot password flow (email input screen) | ⬜ | |

---

## Phase 2 — Customer Portal

### 2A — Dashboard
| # | Scenario | Status | Notes |
|---|----------|--------|-------|
| 2A.1 | Dashboard loads with correct greeting and stat cards | ⬜ | |
| 2A.2 | KYC banner shown if KYC not approved | ⬜ | |
| 2A.3 | Getting-started journey card shown for new user | ⬜ | |
| 2A.4 | Policies list and Claims list sections render | ⬜ | |
| 2A.5 | Domain icons render (no sanitization warnings) | ⬜ | |

### 2B — KYC
| # | Scenario | Status | Notes |
|---|----------|--------|-------|
| 2B.1 | KYC page loads, shows current status | ⬜ | |
| 2B.2 | Aadhaar upload UI works | ⬜ | |
| 2B.3 | PAN upload UI works | ⬜ | |

### 2C — Products & Quote
| # | Scenario | Status | Notes |
|---|----------|--------|-------|
| 2C.1 | Product list renders with domain icons | ⬜ | |
| 2C.2 | Product detail page loads | ⬜ | |
| 2C.3 | Get a Quote flow — Health | ⬜ | |
| 2C.4 | Get a Quote flow — Motor | ⬜ | |
| 2C.5 | Get a Quote flow — Life | ⬜ | |

### 2D — Policies
| # | Scenario | Status | Notes |
|---|----------|--------|-------|
| 2D.1 | Policy list renders | ⬜ | |
| 2D.2 | Policy detail page loads | ⬜ | |
| 2D.3 | Download policy document (PDF) | ⬜ | |
| 2D.4 | Premium schedule visible | ⬜ | |

### 2E — Claims
| # | Scenario | Status | Notes |
|---|----------|--------|-------|
| 2E.1 | Claims list renders | ⬜ | |
| 2E.2 | Claim detail page loads | ⬜ | |
| 2E.3 | File new claim flow (if active policy exists) | ⬜ | |

### 2F — Payments
| # | Scenario | Status | Notes |
|---|----------|--------|-------|
| 2F.1 | Payments page loads with upcoming premiums | ⬜ | |
| 2F.2 | Pay premium UI (Stripe test card) | ⬜ | |

### 2G — Misc
| # | Scenario | Status | Notes |
|---|----------|--------|-------|
| 2G.1 | Notifications page | ⬜ | |
| 2G.2 | Grievances page | ⬜ | |
| 2G.3 | Profile page | ⬜ | |
| 2G.4 | Family/members page | ⬜ | |

---

## Phase 3 — Agent Portal

| # | Scenario | Status | Notes |
|---|----------|--------|-------|
| 3.1 | Agent login and dashboard | ⬜ | |
| 3.2 | Customers list | ⬜ | |
| 3.3 | Submit proposal (4-step flow) | ⬜ | |
| 3.4 | Agent proposals list | ⬜ | |
| 3.5 | Policies list | ⬜ | |
| 3.6 | Commissions page | ⬜ | |
| 3.7 | Profile page | ⬜ | |

---

## Phase 4 — Underwriter Portal

| # | Scenario | Status | Notes |
|---|----------|--------|-------|
| 4.1 | Underwriter login and dashboard | ⬜ | |
| 4.2 | Proposals list and detail | ⬜ | |
| 4.3 | Approve/reject proposal | ⬜ | |
| 4.4 | KYC review list | ⬜ | |
| 4.5 | Approve/reject KYC | ⬜ | |
| 4.6 | Endorsements list | ⬜ | |

---

## Phase 5 — Claims Officer Portal

| # | Scenario | Status | Notes |
|---|----------|--------|-------|
| 5.1 | Claims officer login and dashboard | ⬜ | |
| 5.2 | Claims list and filters | ⬜ | |
| 5.3 | Claim detail and status update | ⬜ | |
| 5.4 | Grievances list and detail | ⬜ | |

---

## Phase 6 — Finance Officer Portal

| # | Scenario | Status | Notes |
|---|----------|--------|-------|
| 6.1 | Finance officer login and dashboard | ⬜ | |
| 6.2 | Payments list | ⬜ | |
| 6.3 | Payouts list | ⬜ | |
| 6.4 | Reports page | ⬜ | |

---

## Phase 7 — Surveyor Portal

| # | Scenario | Status | Notes |
|---|----------|--------|-------|
| 7.1 | Surveyor login and dashboard | ⬜ | |
| 7.2 | Assigned claims list | ⬜ | |
| 7.3 | Survey report submission | ⬜ | |

---

## Phase 8 — Admin Portal

| # | Scenario | Status | Notes |
|---|----------|--------|-------|
| 8.1 | Admin login and dashboard | ⬜ | |
| 8.2 | Users management | ⬜ | |
| 8.3 | Agents list | ⬜ | |
| 8.4 | Products management | ⬜ | |
| 8.5 | System config | ⬜ | |

---

## Issues Log

| # | Phase | Severity | Description | Fix Status |
|---|-------|----------|-------------|------------|
| — | — | — | None yet | — |

---

## Legend
- ⬜ Not tested
- ✅ Passed
- ❌ Failed / Bug found
- 🔧 Fix applied
- ⚠️ Minor UX issue noted
