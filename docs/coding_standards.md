# SpeedClaim Coding Standards & Preferences

## 1. General Preferences

* **Architecture Pattern:** Strictly follow the **Layered Architecture** approach. The code must be clean, modular, and separated into distinct folders/projects:
* `Controllers`: Handle HTTP requests, routing, and status codes.
* `Services`: Core business logic, orchestration, and handling mapping between Entities and Dtos.
* `Interfaces`: Abstract definitions for repositories and services to support Dependency Injection.
* `Repositories`: Data access layer interactions (EF Core queries, calling stored procedures/functions).
* `Context`: EF Core database context configuration.
* `Dtos`: Data Transfer Objects for incoming requests and outgoing API responses.
* `Exceptions`: Custom domain-specific exception classes.


* **Asynchronous Programming:** Every I/O-bound operation, database call, and external API request must be fully **Async** (`async`/`await`). Synchronous blocking calls (e.g., `.Result` or `.Wait()`) are strictly forbidden.
* **Coding Conventions:** Follow standard .NET PascalCase naming for classes and methods, camelCase for local variables and parameters, and prefix private fields with an underscore (e.g., `_paymentService`). Use highly descriptive variable names that express intent.
* **Error & Log Management:** * Implement a global **Exception Middleware** to catch unhandled errors, log them securely, and return a clean, unified JSON error response to the client.
* Use **Serilog** for structured logging across all layers (Information, Warning, Error logs with contextual metadata).



## 2. Backend Stack & Framework Rules

* **Core Stack:** **.NET 10** for the backend API and **Angular** for the frontend user interface.
* **Authentication & Authorization:** Implement **JWT (JSON Web Tokens)** for secure, stateless authentication and role-based authorization. Password hashing and verification must utilize **BCrypt**.
* **Storage & File Management (Current Phase):**
* **File Storage:** Currently using the **Local File System** via file paths. Code should handle file read/write operations locally for now, keeping the design modular enough to swap to Azure Blob Storage later.
* **Configuration & Secrets:** Keep all secrets, database strings, email credentials, and file paths locally in `appsettings.json` or `.env`. *Do not use Azure Key Vault for now.*


* **Payment Processing:** Utilizes the **Stripe testing sandbox** for workflows. Keep the Publishable Key and Secret Key securely isolated within the configuration files.
* **Notification Engine:** Email notifications will be delivered using Gmail SMTP via Google Account **App Passwords** stored securely in the app configuration.

## 3. Database & Naming Conventions

* **Database Provider:** **PostgreSQL**.
* **EF Core Approach:** Strictly **Code-First**.
* **Model Configuration Rule:** **Zero DataAnnotations** allowed on Domain Models or Dtos. All schema definitions, table configurations, relationships, and validation constraints must be configured explicitly via **Fluent API** inside the `DbContext` (using `IEntityTypeConfiguration<T>`).
* **Constraint Naming Standards:** Every constraint generated in the database must be explicitly and systematically named. Follow this naming convention:
* **Primary Keys:** `PK_TableName`
* **Foreign Keys:** `FK_TableName_TargetTableName_ColumnName`
* **Unique Constraints:** `UQ_TableName_ColumnName`
* **Check Constraints:** `CK_TableName_ConstraintDescription`


* **Transactional & Complex Operations:** For highly critical, transactional, or performance-intensive database operations, logic must be written inside **PostgreSQL Stored Procedures or Functions** and called explicitly from the repository layer.

## 4. API & Testing Standards

* **API Documentation:** Integration of **Swagger** is mandatory for all environments during development to auto-generate, visualize, and interactively test backend API endpoints.
* **Unit Testing Framework:** **NUnit** will be used exclusively for testing backend logic.
* **Testing Coverage Goal:** The immediate milestone is a complete, working backend with comprehensive unit tests written for the Service layer (mocking repositories) and critical business workflows to ensure structural integrity prior to frontend integration.

## 5. Version Control & Git Workflow (Production Standard)

* **Branching Strategy (Feature Branch Workflow):** * The `main` or `develop` branch must always represent a stable, deployable state.
* Direct commits to the stable branch are strictly prohibited. Every new feature, bug fix, or test suite must be developed in an isolated branch created off the stable branch.
* **Branch Naming Convention:** Use a clean, predictable prefix system:
* `feature/feature-name` (e.g., `feature/jwt-auth`, `feature/stripe-integration`)
* `bugfix/issue-name` (e.g., `bugfix/local-file-path-error`)
* `test/test-target` (e.g., `test/payment-service-nunit`)


* **Merging:** Features must be integrated back into the stable branch strictly via Pull Requests (PRs). Before a merge can happen, all code must compile cleanly and pass all NUnit tests.


* **Commit Message Standard (Conventional Commits):** All commits must follow structured, descriptive prefixes to keep the Git log professional and highly readable:
* `feat: ...` — For adding a new feature or capability. (e.g., `feat: implement user registration with bcrypt hashing`)
* `fix: ...` — For fixing a bug. (e.g., `fix: resolve fluent api foreign key constraint error`)
* `test: ...` — For adding or updating unit tests. (e.g., `test: add nunit cases for authentication service`)
* `refactor: ...` — For code changes that neither fix a bug nor add a feature. (e.g., `refactor: extract token generation logic to helper service`)
* `chore: ...` — For updating dependencies, configurations, or build tools. (e.g., `chore: configure serilog file logging in appsettings`)