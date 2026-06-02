# API Versioning & Release Plan

This document outlines the phased implementation strategy for the SpeedClaim platform API, moving from foundational core features (v1) to more advanced integrations (v2 and beyond). 

## Release Strategy

The development cycle will follow a strict, phased approach:
1. **Develop Core Features:** Implement the targeted features for the current API version.
2. **Comprehensive Unit Testing:** Ensure high coverage (specifically mocking repositories) and stability using NUnit.
3. **Commit & Integration:** Branch merges via PR and push to standard Git repository structure.
4. **Iterate to Next Version:** Begin the next API version, introducing non-breaking enhancements or isolated `/api/vX/` routes for breaking changes and advanced capabilities.

---

## API Version 1 (v1) - The Core Foundation

**Goal:** Establish a robust baseline system with functional Auth, Catalog, Policy Issuance, and basic file handling.

### Features
* **Authentication & Authorization**
  * JWT-based login and registration for Users and Agents.
  * Role-based endpoints via `[Authorize]`.
  * Basic password hashing (BCrypt).
* **Document / File Management**
  * Local File System storage for Profile Pictures (upload and retrieval).
* **Catalog Management**
  * Full CRUD for `InsuranceProduct` entities.
* **Policy Management**
  * Issuance of base policies (`Health`, `Vehicle`, `Life`).
  * Basic policy retrieval endpoints.
* **Testing**
  * NUnit service tests covering `AuthService`, `ProductService`, and `PolicyService`.
  * Repository interfaces heavily mocked for deterministic testing.

---

## API Version 2 (v2) - Financial Integration & Claims

**Goal:** Add core business functionalities that rely heavily on third-party services like payments, as well as complex workflows such as the Claims lifecycle.

### Features
* **Payment Processing (Stripe Integration)**
  * Sandbox Stripe endpoints for processing policy premiums.
  * Webhooks for listening to successful/failed payments.
* **Claims Engine**
  * Claim submission endpoint with document attachment logic (extending local file storage).
  * Agent/Admin claim review workflows (Approve, Reject, Request More Info).
* **Automated Notifications**
  * Gmail SMTP integration for sending policy issuance certificates and claim status updates.

---

## API Version 3 (v3) - Advanced Features & Automation

**Goal:** Optimize the platform for scale and background robustness.

### Features
* **Background Jobs (e.g., Hangfire / Quartz / Hosted Services)**
  * Agent License Expiry Enforcement: Automatically flagging policies/agents if licenses expire.
* **Cloud Storage Migration**
  * Transition from Local File System to Azure Blob Storage for user profile pictures and claim documents.
* **Analytics & Reporting**
  * Endpoints aggregating claim processing speeds, agent performance, and premium revenue.

---

## Principles for Moving Forward
1. **No Breaking Changes in v1:** Once v1 is signed off, any breaking change requires an update to the new API segment (e.g., `/api/v2/...`).
2. **Swagger Documentation:** Every active API version will have its own Swagger definition generated explicitly by the API explorer.
3. **Quality Gates:** We will not proceed to v2 feature development until v1 tests pass consistently and the branch is merged gracefully into `main`.
