# API Implementation Plan & Versioning Strategy

This plan outlines the iterative development strategy for the SpeedClaim backend. Your proposed approach—building Version 1 (Core), testing it thoroughly (NUnit), committing to version control, and then proceeding to V2 and beyond—is excellent. It adheres to Agile methodologies and minimizes integration risks.

## Phase 1: API Version 1 (Core Foundation)
This phase establishes the structural integrity of the application.
- **Project Setup:** .NET 10 Web API, Layered Architecture (Controllers, Services, Repositories, DTOs).
- **Database Setup:** EF Core with PostgreSQL, Code-First migrations, Fluent API constraints (no DataAnnotations). Includes Agent Expiry Date field.
- **Cross-Cutting Concerns:** Serilog for logging, Global Exception Middleware, Swagger UI.
- **Auth & Identity:** JWT Auth (Bcrypt), User Registration, Login, Token Refresh.
- **Document Management (Basic):** Local file system upload for User Profile Pictures.
- **RBAC Foundation:** Role table, User_Roles junction, basic API authorization.
- **Catalog:** Insurance Products CRUD.
- **Policies (Basic):** Issuing a basic policy (Health, Vehicle, Life) without full payment workflow.
- **Testing:** NUnit tests for Auth and Policy services.
- **Version Control:** Commit and push to `feature/v1-core` and merge to stable.

## Phase 2: API Version 2 (Payments & Claims)
This phase introduces financial transactions and workflows.
- **Payments:** Stripe sandbox integration for Premium Payments.
- **Premium Schedules:** Auto-generating schedules on policy issuance.
- **Claims Submission:** Creating claims against active policies.
- **Claims Workflow:** Adjuster assignment, Under Review, Approved, Rejected states.
- **Document Management (Advanced):** File system upload/download for claim proofs and checklists.
- **Testing:** NUnit tests for Claims logic and Payment processing.
- **Version Control:** Commit and push to `feature/v2-payments-claims` and merge to stable.

## Phase 3: API Version 3 (Automation & Compliance)
This phase covers background processes and strict IRDAI compliance.
- **Background Jobs:** Overdue premium cron, SLA escalation for claims, Agent License Expiry enforcement.
- **Notifications:** Gmail SMTP integration for Emails (representing SMS as well).
- **Compliance:** Audit Logs for all DB changes, User Consents.
- **Advanced RBAC:** Approval limits enforcement for Adjusters.
- **Testing:** Unit tests for Background jobs and Audit trails.
- **Version Control:** Commit and push to `feature/v3-automation` and merge to stable.

## Verification Plan

### Automated Tests
- NUnit test suites will be run at the end of every phase using `dotnet test`.
- Test coverage will strictly target the `Services` layer (business logic), utilizing mocked repositories to ensure tests are fast and isolated.

### Manual Verification
- Swagger UI will be used to manually test and demonstrate endpoints locally.
- JWT tokens will be inspected to ensure roles and claims are accurately attached.
