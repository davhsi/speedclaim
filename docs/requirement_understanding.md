# SpeedClaim — Requirements Understanding

> **Document type:** Software Requirements Specification (SRS)
> **Version:** 1.0
> **Project:** SpeedClaim — Insurance Platform (Capstone)
> **Stack:** .NET 10 Web API · Angular · PostgreSQL · Stripe

---

## Table of Contents

1. [Introduction](#1-introduction)
2. [Business Context](#2-business-context)
3. [Stakeholders](#3-stakeholders)
4. [Functional Requirements](#4-functional-requirements)
5. [Non-Functional Requirements](#5-non-functional-requirements)
6. [Assumptions](#6-assumptions)
7. [Out of Scope](#7-out-of-scope)

---

## 1. Introduction

### 1.1 Purpose

This document captures the functional and non-functional requirements for SpeedClaim — an end-to-end insurance platform covering Health, Life, and Motor domains. It serves as both an academic deliverable for capstone evaluation and an internal developer reference.

### 1.2 Scope

SpeedClaim provides:
- A customer-facing portal to browse products, buy policies, pay premiums, and file claims
- Role-specific staff portals for agents, underwriters, claims officers, finance officers, and admins
- A .NET 10 REST API backend consumed by an Angular frontend

### 1.3 Definitions

| Term | Meaning |
| --- | --- |
| Customer | End user who purchases insurance policies |
| Agent | Staff member who sells policies on behalf of customers |
| Underwriter | Staff member who assesses risk and approves or rejects proposals |
| Claims Officer | Staff member who processes and adjudicates insurance claims |
| Finance Officer | Staff member who manages payments, payouts, and commissions |
| Surveyor | External party assigned to inspect motor claims |
| Admin | Super user with full system access |
| Proposal | Application submitted by a customer or agent to purchase a policy |
| Policy | Active insurance contract issued after proposal approval |
| KYC | Know Your Customer — identity verification process |
| Endorsement | A customer-requested change to an existing policy |
| Grievance | A formal complaint raised against a policy or claim |
| Premium Schedule | Installment plan generated after policy issuance |
| JWT | JSON Web Token — used for stateless authentication |

---

## 2. Business Context

### 2.1 Problem Statement

Traditional insurance workflows involve heavy paperwork, slow turnaround, and poor visibility for customers. Customers cannot track their claim or proposal status in real time, agents rely on manual processes to onboard customers, and staff work in siloed systems with no unified view.

### 2.2 Business Objectives

| ID | Objective |
| --- | --- |
| BO-01 | Enable customers to self-serve — purchase policies, pay premiums, file claims — entirely online |
| BO-02 | Reduce proposal approval time by providing underwriters a structured digital review queue |
| BO-03 | Provide agents with a performance dashboard and tools to submit proposals on behalf of customers |
| BO-04 | Give claims officers end-to-end claim management — from intimation to settlement |
| BO-05 | Ensure all financial transactions (premiums, payouts, commissions) are tracked and reconcilable |
| BO-06 | Enforce strict data ownership — no role can access data outside their permission boundary |

---

## 3. Stakeholders

| Stakeholder | Role in System | Primary Need |
| --- | --- | --- |
| Customer | Buys and manages policies | Self-service, transparency, ease of use |
| Agent | Sells policies, tracks performance | Tools to submit proposals and monitor commissions |
| Underwriter | Reviews proposals and KYC | Structured queue, ability to request documents |
| Claims Officer | Processes claims | Full claim lifecycle management |
| Finance Officer | Handles money movement | Payment tracking, payout processing, reports |
| Surveyor | Inspects motor claims | View assigned claims, submit survey report |
| Admin | Manages entire platform | User management, product config, audit access |

---

## 4. Functional Requirements

### 4.1 Authentication & Session Management

| ID | Requirement |
| --- | --- |
| FR-01 | The system shall allow a new user to register with name, email, password, and phone number |
| FR-02 | The system shall send a verification email upon registration; accounts remain inactive until verified |
| FR-03 | The system shall authenticate users with email and password, returning a JWT access token (15 min) and a hashed refresh token (7 days) |
| FR-04 | The system shall reject login attempts for unverified or deactivated accounts with an appropriate error |
| FR-05 | The system shall support token refresh — issuing new token pairs while revoking the used refresh token |
| FR-06 | The system shall allow users to log out, revoking all active sessions |
| FR-07 | The system shall support password reset via a time-limited email token |
| FR-08 | The system shall rate-limit authentication endpoints to 10 requests per 60 seconds per IP address |
| FR-09 | Admins shall be able to register agent accounts and reset any user's password |

### 4.2 User Profile & KYC

| ID | Requirement |
| --- | --- |
| FR-10 | Customers shall be able to view and update their personal profile (name, phone, salutation) |
| FR-11 | Customers shall be able to add, update, and delete addresses |
| FR-12 | Customers shall be able to add, update, list, and delete family members |
| FR-13 | Customers shall be able to upload KYC documents; the submission status shall be visible to the customer |
| FR-14 | Underwriters shall be able to view the pending KYC queue and approve or reject submissions with a reason |
| FR-15 | Agents shall be able to update their own profile (name, phone, salutation) |

### 4.3 Insurance Products

| ID | Requirement |
| --- | --- |
| FR-16 | The system shall support three insurance domains: Health, Life, and Motor |
| FR-17 | Admins shall be able to create insurance products with configurable parameters (age range, sum assured range, tenure, family floater settings) |
| FR-18 | Admins shall be able to define a premium rate table per product (age bands × sum assured bands → annual premium) |
| FR-19 | Admins shall be able to configure document requirements scoped to a specific product |
| FR-20 | Admins shall be able to activate or deactivate a product; inactive products shall not appear in public listings |
| FR-21 | Any user (including unauthenticated) shall be able to browse active products and retrieve document requirements for a product |

### 4.4 Proposals

| ID | Requirement |
| --- | --- |
| FR-22 | The system shall generate a premium quote given product, age, gender, sum assured, and tenure — before a proposal is submitted |
| FR-23 | Customers and agents shall be able to submit a proposal with domain-specific details (health, life, or motor), nominees, and supporting documents |
| FR-24 | Underwriters shall be able to view all submitted proposals, approve or reject them with notes, or request additional documents |
| FR-25 | On approval, the system shall automatically create a policy and generate a premium schedule |
| FR-26 | On approval of an agent-submitted proposal, the system shall automatically create a commission record for that agent |
| FR-27 | Customers and agents shall be able to track the status and history of their own proposals |

### 4.5 Policies

| ID | Requirement |
| --- | --- |
| FR-28 | Customers shall be able to view all their own policies and download an e-policy certificate |
| FR-29 | Customers shall be able to request a policy endorsement (e.g., nominee or address change); underwriters shall review and action it |
| FR-30 | Customers shall be able to cancel an active or pending policy |
| FR-31 | Customers shall be able to view and update nominee details on a policy |
| FR-32 | The system shall maintain a full status history for every policy |
| FR-33 | Underwriters and admins shall be able to view all policies across all customers |

### 4.6 Premium Payments

| ID | Requirement |
| --- | --- |
| FR-34 | The system shall generate an installment premium schedule upon policy issuance |
| FR-35 | Customers shall be able to pay premium installments via Stripe; the system shall create a PaymentIntent and return the client secret |
| FR-36 | The system shall reject payment attempts on already-paid schedules with HTTP 409 |
| FR-37 | Customers shall be able to view their full payment history and download receipts for completed payments |
| FR-38 | Finance officers shall be able to view all payment records, manually reconcile discrepancies, and process refunds |
| FR-39 | The system shall generate overdue policy reports and payment summary reports; data shall be exportable to Excel |

### 4.7 Claims

| ID | Requirement |
| --- | --- |
| FR-40 | Customers shall be able to intimate a claim against an active policy, specifying claim type, amount, incident date, and description |
| FR-41 | Customers shall be able to upload supporting documents for a claim |
| FR-42 | Claims officers shall be able to view all claims, assign claims to themselves, update claim status, and request additional documents |
| FR-43 | Claims officers shall be able to assign a surveyor to motor claims; surveyors shall submit a survey report |
| FR-44 | Claims officers shall be able to approve cashless pre-authorisation for health claims |
| FR-45 | Claims officers shall be able to approve or reject a claim with a stated reason and approved amount |
| FR-46 | Finance officers shall be able to process approved claim payouts via Stripe and mark claims as financially settled |
| FR-47 | The system shall maintain a full status history for every claim |

### 4.8 Grievances

| ID | Requirement |
| --- | --- |
| FR-48 | Customers shall be able to raise a grievance linked to a policy or claim, with a category and description |
| FR-49 | Customers shall be able to view the status and resolution of their own grievances |
| FR-50 | Claims officers shall be able to view all grievances, assign them, update their status, and resolve or escalate them |

### 4.9 Agent Management

| ID | Requirement |
| --- | --- |
| FR-51 | Admins shall be able to create branch offices and assign agents to branches |
| FR-52 | Admins shall be able to set and update an agent's license details and activation status |
| FR-53 | Agents shall be able to view their assigned customers and the policies linked to those customers |
| FR-54 | Agents shall be able to view a renewal reminder list of policies expiring within a configured period |
| FR-55 | Finance officers shall be able to review and approve pending agent commission records |

### 4.10 Notifications & Communication

| ID | Requirement |
| --- | --- |
| FR-56 | The system shall send email notifications for key events: registration, email verification, password reset, proposal decisions, and policy issuance |
| FR-57 | The system shall create in-app notifications for users on key events (proposal approved/rejected, claim status change, commission credited) |
| FR-58 | Users shall be able to list their notifications, mark individual ones as read, and mark all as read |

### 4.11 System Administration

| ID | Requirement |
| --- | --- |
| FR-59 | Admins shall be able to view and update system configuration values (grace period, free-look days, etc.) |
| FR-60 | Admins shall be able to manage email templates |
| FR-61 | The system shall maintain an audit log of significant actions; admins shall be able to search and view audit logs |
| FR-62 | Admins shall be able to view all notification and email logs |

---

## 5. Non-Functional Requirements

### 5.1 Security

| ID | Requirement |
| --- | --- |
| NFR-01 | All passwords shall be hashed using BCrypt before storage; plaintext passwords shall never be persisted |
| NFR-02 | JWT access tokens shall expire after 15 minutes; refresh tokens shall expire after 7 days and be stored as BCrypt hashes |
| NFR-03 | All API endpoints shall enforce role-based access control; a user may not access resources belonging to another user |
| NFR-04 | Authentication endpoints shall be rate-limited to prevent brute-force attacks (FR-08) |
| NFR-05 | Secrets (JWT key, Stripe keys, SMTP credentials) shall never be committed to version control |
| NFR-06 | All financial operations shall validate ownership before processing — a customer may only pay their own schedules |

### 5.2 Performance

| ID | Requirement |
| --- | --- |
| NFR-07 | All bulk list endpoints shall return paginated responses; default page size is 20 records |
| NFR-08 | All database operations shall be performed asynchronously; synchronous blocking calls (`.Result`, `.Wait()`) are prohibited |
| NFR-09 | The global API rate limiter shall enforce a ceiling of 100 requests per 60 seconds per IP on all non-auth endpoints |

### 5.3 Reliability & Observability

| ID | Requirement |
| --- | --- |
| NFR-10 | The system shall use a global exception middleware to catch unhandled errors, log them, and return a consistent JSON error response |
| NFR-11 | Structured logs shall be written to the console and to rolling daily log files (30-day retention) using Serilog |
| NFR-12 | All email send operations shall log success or failure to the `email_logs` table regardless of SMTP outcome |

### 5.4 Maintainability

| ID | Requirement |
| --- | --- |
| NFR-13 | The codebase shall follow a strict layered architecture: Controllers → Services → Repositories → Database |
| NFR-14 | All service layer business logic shall be covered by unit tests using NUnit and Moq-mocked repositories |
| NFR-15 | Database schema shall be managed exclusively via EF Core Code-First migrations; no manual SQL schema changes |
| NFR-16 | All database constraints shall follow the named convention standard defined in `coding_standards.md` |
| NFR-17 | The API shall be fully documented via Swagger, including roles, request/response schemas, and status codes |

### 5.5 Scalability & Extensibility

| ID | Requirement |
| --- | --- |
| NFR-18 | File storage shall be abstracted behind `IStorageService` to allow swapping local storage for Azure Blob Storage without service-layer changes |
| NFR-19 | The API shall support versioning (`/api/v{n}/`) to allow future breaking changes without disrupting existing clients |

---

## 6. Assumptions

| ID | Assumption |
| --- | --- |
| A-01 | The platform operates in a single currency (INR); no multi-currency support is required |
| A-02 | Stripe operates in sandbox/test mode for the duration of this project; real money is never transferred |
| A-03 | Email delivery uses Gmail SMTP with App Passwords; a production deployment would use a transactional email provider (e.g., SendGrid) |
| A-04 | File storage uses the local filesystem for development; a production deployment would migrate to Azure Blob Storage |
| A-05 | KYC approval is a manual process performed by an underwriter; no automated identity verification service is integrated |
| A-06 | Premium amounts are calculated from a static rate table configured per product; no actuarial engine is in scope |
| A-07 | A single admin account is pre-seeded; self-registration of admin accounts is not supported |
| A-08 | Stripe webhooks are tested locally using the Stripe CLI tunnel; see `stripe_testing_guide.md` |

---

## 7. Out of Scope

| Item | Reason |
| --- | --- |
| Mobile application | Web-only for this capstone phase |
| Real-time push notifications (SignalR/WebSockets) | In-app notification polling is sufficient for the demo scope |
| Multi-currency support | Single-market (INR) product |
| Automated KYC via third-party identity API | Manual underwriter review is the defined process |
| Actuarial premium engine | Static rate tables per product are sufficient |
| Multi-language / i18n | English only |
| CI/CD pipeline | Out of capstone scope; deployment is manual |
| Reinsurance or co-insurance workflows | Not part of the defined product domains |
