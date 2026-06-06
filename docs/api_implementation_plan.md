# API Implementation Plan & Versioning Strategy

This plan outlines the iterative development strategy for the SpeedClaim backend. Your proposed approach—building Version 1 (Core), testing it thoroughly (NUnit), committing to version control, and then proceeding to V2 and beyond—is excellent. It adheres to Agile methodologies and minimizes integration risks.

## Phase 1: API Version 1 (MVP - Completed)
This phase established the structural integrity of the application, including core business logic, claims processing, and payment integration.
- **Project Setup:** .NET 10 Web API, Layered Architecture (Controllers, Services, Repositories, DTOs).
- **Database Setup:** EF Core with PostgreSQL, Code-First migrations, Table-Per-Hierarchy (TPH) for policies, Fluent API constraints.
- **Cross-Cutting Concerns:** Serilog for logging, Global Exception Middleware, Swagger UI with server-side pagination support.
- **Auth & Identity:** JWT Auth (Bcrypt), Strict KYC Registration (Aadhaar/PAN masking, Enum mappings), Token Refresh.
- **Validation:** Pre-controller input validation using FluentValidation.
- **Document Management:** Local file system upload for User Profile Pictures, KYC Documents, and Claim Proofs.
- **RBAC Foundation:** Role table, User_Roles junction, basic API authorization.
- **Catalog & Policies:** Insurance Products CRUD, issuing domain-specific policies (Health, Vehicle, Life).
- **Payments:** Stripe sandbox integration for Premium Payments and Webhooks.
- **Claims Workflow:** Claim submission, Adjuster assignment, Under Review, Approved, Settled states.
- **Testing:** NUnit tests for Auth, Policy, Claims, and Payment services with mocked repositories.

## Phase 2: API Version 2 (Automation, Cloud & Compliance)
This phase will optimize the platform for scale, robustness, and strict IRDAI compliance.
- **Cloud Storage Migration:** Transition from Local File System to Azure Blob Storage for user profile pictures and claim documents.
- **Secrets Management:** Move all configuration secrets to Azure Key Vault.
- **Advanced Claim Features:** Server-Side Claim Drafts, Two-Way Adjuster/Customer Messaging, and Document Rejection Context.
- **Payments Automation:** Stripe Connect integration for automated claim settlement payouts.
- **Background Jobs:** Overdue premium cron, SLA escalation for claims, Agent License Expiry enforcement.
- **Notifications & Real-time:** SignalR WebSockets for live UI updates, and granular notification preferences (Email/SMS).
- **Compliance:** Audit Logs for all DB changes, User Consents.
- **Advanced RBAC:** Approval limits enforcement for Adjusters.
- **Testing:** Unit tests for Background jobs, SignalR hubs, and Audit trails.

## Verification Plan

### Automated Tests
- NUnit test suites will be run at the end of every phase using `dotnet test`.
- Test coverage will strictly target the `Services` layer (business logic), utilizing mocked repositories to ensure tests are fast and isolated.

### Manual Verification
- Swagger UI will be used to manually test and demonstrate endpoints locally.
- JWT tokens will be inspected to ensure roles and claims are accurately attached.
