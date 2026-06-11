# SpeedClaim — Technical Specification v3.0

> Capstone Project | Domains: Health, Life, Motor | Stack: .NET Web API + Angular + PostgreSQL + Stripe + Azure Blob Storage (local first)
>
> **Changelog v3:** Fixed 5 logical data flow gaps — first premium chicken-and-egg, pre-issuance data collection, auth token storage, document entity_type ENUM, surveyor license tracking.

---

## 1. Technology Stack

| Layer | Technology |
|---|---|
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
|---|---|
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

## 5. Database Schema Overview

39 tables across 16 domains.

| Domain | Tables | Count |
| --- | --- | --- |
| Auth & Users | users, sessions, user_tokens, addresses | 4 |
| Customers | customers, customer_members | 2 |
| KYC | kyc_records | 1 |
| Branches | branches | 1 |
| Agents | agents, agent_commissions | 2 |
| Surveyors | surveyors | 1 |
| Products | insurance_products, premium_rate_tables | 2 |
| Proposals | proposals, proposal_members | 2 |
| Domain Details | health_details, life_details, motor_details | 3 |
| Nominees | nominees | 1 |
| Policies | policies, policy_members, policy_status_history, endorsements | 4 |
| Payments | stripe_customers, premium_schedules, premium_payments | 3 |
| Claims | claims, claim_status_history, health_claim_details, life_claim_details, motor_claim_details | 5 |
| Grievances | grievances | 1 |
| Documents | document_requirements, submitted_documents | 2 |
| Notifications & Email | notifications, email_templates, email_logs | 3 |
| Audit & Config | audit_logs, system_config | 2 |
| **Total** | | **39** |

---

> Document version: 3.0 | Stack: .NET 10 Web API + Angular + PostgreSQL + Stripe
