<USER_REQUEST>
i went through ur gaps and arrived with another plan

# Insurance App — Technical Specification v3.0
> Capstone Project | Domains: Health, Life, Motor | Stack: .NET Web API + Angular + PostgreSQL + Stripe + Azure Blob Storage (local first)
> **Changelog v3:** Fixed 5 logical data flow gaps — first premium chicken-and-egg, pre-issuance data collection, auth token storage, document entity_type ENUM, surveyor license tracking.

---

## 1. Technology Stack

| Layer | Technology |
|---|---|
| Backend | .NET Web API (C#) |
| Frontend | Angular 22 |
| Database | PostgreSQL |
| Authentication | JWT — Access Token (15 min) + Refresh Token (7 days) |
| Payment | Stripe (US sandbox) |
| Email | SendGrid / SMTP via .NET MailKit |
| File Storage | Local storage now → Azure Blob Storage later |
| ORM | Entity Framework Core |

---

## 2. Roles

| Role | Description |
|---|---|
| `customer` | Registers, manages family members, completes KYC, buys policies, makes payments, raises claims and grievances |
| `agent` | Onboards customers, creates proposals, views own dashboard — commissions, policies sold, customer list |
| `underwriter` | Reviews proposals, assesses risk, approves or rejects policies, requests additional documents |
| `claims_officer` | Processes claims, verifies documents, coordinates with surveyor, approves cashless pre-auth |
| `finance_officer` | Reconciles Stripe payments, manages refunds and claim payouts, monitors premium schedules |
| `surveyor` | Assigned to motor claims — inspects vehicle, submits surveyor report |
| `admin` | Full system access — manages users, roles, products, branches, document requirements, system config |

---

## 3. JWT Authentication

### Token Strategy

- **Access Token** — short-lived (15 minutes), sent in `Authorization: Bearer <token>` header on every request
- **Refresh Token** — long-lived (7 days), stored hashed in `sessions` table, used only to issue new access tokens
- **OTP / Reset tokens** — short-lived (10–60 minutes), stored in `use
<truncated 27307 bytes>
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

*Document version: 3.0 | Stack: .NET Web API + Angular + PostgreSQL + Stripe + Azure Blob*
</USER_REQUEST>
<ADDITIONAL_METADATA>
The current local time is: 2026-06-09T17:08:24+05:30.

The user's current state is as follows:
Active Document: /Users/davishe/workspace/InsuranceApp/backend/SpeedClaim.Api/Controllers/ClaimsController.cs (LANGUAGE_CSHARP)
Cursor is on line: 1
Other open documents:
- /Users/davishe/workspace/InsuranceApp/backend/SpeedClaim.Tests/Services/EmailServiceTests.cs (LANGUAGE_CSHARP)
- /Users/davishe/workspace/InsuranceApp/backend/SpeedClaim.Api/Controllers/ComplianceController.cs (LANGUAGE_CSHARP)
- /Users/davishe/workspace/InsuranceApp/backend/SpeedClaim.Api/Models/Enums/PolicyType.cs (LANGUAGE_CSHARP)
- /Users/davishe/workspace/InsuranceApp/backend/SpeedClaim.Api/Models/Enums/EntityType.cs (LANGUAGE_CSHARP)
- /Users/davishe/workspace/InsuranceApp/backend/SpeedClaim.Api/Models/AuditLog.cs (LANGUAGE_CSHARP)
Running terminal commands:
- dotnet run (in /Users/davishe/workspace/InsuranceApp/backend/SpeedClaim.Api, running for 1h40m26s)
</ADDITIONAL_METADATA>