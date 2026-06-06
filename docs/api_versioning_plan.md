# API Versioning & Release Plan

This document outlines the phased implementation strategy for the SpeedClaim platform API, moving from foundational core features (v1) to more advanced integrations (v2 and beyond). 

## Release Strategy

The development cycle will follow a strict, phased approach:
1. **Develop Core Features:** Implement the targeted features for the current API version.
2. **Comprehensive Unit Testing:** Ensure high coverage (specifically mocking repositories) and stability using NUnit.
3. **Commit & Integration:** Branch merges via PR and push to standard Git repository structure.
4. **Iterate to Next Version:** Begin the next API version, introducing non-breaking enhancements or isolated `/api/vX/` routes for breaking changes and advanced capabilities.

---

## API Version 1 (v1) - The MVP (Completed)

**Goal:** Establish a robust baseline system with functional Auth, Catalog, Policy Issuance, Payments, Claims, and file handling.

### Features
* **Authentication & Authorization**
  * JWT-based login and registration for Users and Agents.
  * Role-based endpoints via `[Authorize]`.
  * Basic password hashing (BCrypt).
  * Strict KYC Enforcement (FluentValidation for Age/PAN/Aadhaar) and PII masking (Aadhaar Data Vault compliance).
* **Document / File Management**
  * Local File System storage for Profile Pictures, KYC Documents, and Claims Proofs.
* **Catalog & Policy Management**
  * Full CRUD for `InsuranceProduct` entities.
  * Issuance of base policies (`Health`, `Vehicle`, `Life`) using TPH strategy.
  * Pagination for efficient retrieval.
* **Payment Processing & Claims**
  * Sandbox Stripe integration and Webhooks for processing policy premiums.
  * Complete Claims Engine (Submission, Adjuster assignment, Document attachment, Approval, Settlement).
* **Testing**
  * Comprehensive NUnit service tests heavily mocking repository interfaces.

---

## API Version 2 (v2) - Automation, Scale, & Cloud Infrastructure

**Goal:** Add advanced features, real-time updates, and migrate infrastructure to the cloud for production readiness.

### Features
* **Cloud & Infrastructure Upgrades**
  * Transition from Local File System to Azure Blob Storage for user profile pictures and claim documents.
  * Azure Key Vault integration for all secrets and credentials.
* **Advanced Claim Features**
  * Server-Side Claim Drafts.
  * Two-Way Adjuster/Customer Messaging.
  * Document Rejection Context tracking.
* **Payout Automation (Stripe Connect)**
  * Automating outbound claim settlement payouts directly to a user's bank account.
* **Real-time UX & Notifications**
  * SignalR integration for real-time websocket updates (e.g., live claim status updates).
  * Granular Notification Preferences (Email/SMS toggles).
* **Compliance & Background Jobs**
  * `audit_logs` and `user_consents` implementation.
  * Overdue premium crons and SLA escalations.

---

## Principles for Moving Forward
1. **No Breaking Changes in v1:** Once v1 is signed off, any breaking change requires an update to the new API segment (e.g., `/api/v2/...`).
2. **Swagger Documentation:** Every active API version will have its own Swagger definition generated explicitly by the API explorer.
3. **Quality Gates:** We will not proceed to v2 feature development until v1 tests pass consistently and the branch is merged gracefully into `main`.
