# SpeedClaim — Insurance Claim Processing System

This document outlines the core database schema for the SpeedClaim platform. The design utilizes a strictly typed, explicit schema supporting a multi-domain architecture (Health, Vehicle, Life) along with a rigorous compliance, audit, and role-based access control layer.

## 1. Database Schema

### 1.1. Auth & Identity Domain

```sql
CREATE TABLE users (
    id                  uuid PRIMARY KEY,
    email               varchar(255) NOT NULL UNIQUE,
    password_hash       varchar(255) NOT NULL,
    full_name           varchar(200),
    phone               varchar(20),
    address             text,
    date_of_birth       date,
    profile_picture_url text,
    timezone            varchar(50) DEFAULT 'UTC',
    is_active           boolean DEFAULT true,
    created_at          timestamptz DEFAULT now(),
    deleted_at          timestamptz            -- Soft delete: marks user as removed, retained for FK integrity and IRDAI retention compliance
);

CREATE TABLE roles (
    id              uuid PRIMARY KEY,
    code            varchar(50) NOT NULL UNIQUE,
    description     text,
    hierarchy_level integer NOT NULL DEFAULT 10 CHECK (hierarchy_level > 0)
);

CREATE TABLE user_roles (
    id             uuid PRIMARY KEY,
    user_id        uuid NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    role_id        uuid NOT NULL REFERENCES roles(id) ON DELETE CASCADE,
    domain         varchar(20),        -- Application-level RBAC: e.g. 'HEALTH', 'VEHICLE'
    approval_limit decimal(14,2),      -- Application-level RBAC: e.g. 5000.00 for junior adjuster
    assigned_at    timestamptz DEFAULT now(),
    revoked_at     timestamptz         -- Revocation timestamp; row is never deleted
);

CREATE TABLE refresh_tokens (
    id          uuid PRIMARY KEY,
    user_id     uuid NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    token_hash  varchar(255) NOT NULL UNIQUE,  -- SHA-256 hash only; raw token never persisted
    expires_at  timestamptz NOT NULL,
    is_revoked  boolean DEFAULT false,
    ip_address  inet,
    created_at  timestamptz DEFAULT now()
);
```

### 1.2. Required Compliance Layer

```sql
CREATE TABLE user_consents (
    id               uuid PRIMARY KEY,
    user_id          uuid NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    consent_type     varchar(50) NOT NULL,  -- 'MARKETING', 'DATA_PROCESSING', 'HEALTH_DATA_SHARING'
    is_granted       boolean NOT NULL,
    consent_version  varchar(20) NOT NULL,
    ip_address       inet,
    granted_at       timestamptz DEFAULT now(),
    withdrawn_at     timestamptz
);

CREATE TABLE audit_logs (
    id            uuid PRIMARY KEY,
    actor_id      uuid,               -- No physical FK: preserves immutable trail if user is hard-deleted
    entity_type   varchar(50) NOT NULL,
    entity_id     uuid NOT NULL,
    action        varchar(50) NOT NULL,
    old_values    jsonb,
    new_values    jsonb,
    ip_address    inet,
    user_agent    text,
    created_at    timestamptz DEFAULT now()
);
CREATE INDEX idx_audit_logs_composite ON audit_logs(entity_type, entity_id);
```

### 1.3. Core Products & Agents

```sql
CREATE TABLE agents (
    id                  uuid PRIMARY KEY,
    user_id             uuid NOT NULL UNIQUE REFERENCES users(id) ON DELETE CASCADE,
    license_number      varchar(50) NOT NULL UNIQUE,
    agency_name         varchar(200),
    license_valid_until date NOT NULL,
    commission_rate     decimal(5,4) NOT NULL DEFAULT 0.0000,  -- e.g. 0.0500 = 5%; IRDAI disclosure requirement
    is_active           boolean DEFAULT true
);

CREATE TABLE insurance_products (
    id                  uuid PRIMARY KEY,
    code                varchar(50) NOT NULL UNIQUE,
    name                varchar(200) NOT NULL,
    domain              varchar(20) NOT NULL,  -- 'HEALTH', 'VEHICLE', 'LIFE'
    description         text,
    max_coverage        decimal(14,2),
    is_active           boolean DEFAULT true,
    CONSTRAINT UQ_product_id_domain UNIQUE (id, domain)  -- Required for cross-domain integrity guard
);
```

### 1.4. Policies Domain

```sql
CREATE TABLE policies (
    id                uuid PRIMARY KEY,
    policy_number     varchar(50) NOT NULL,
    user_id           uuid NOT NULL REFERENCES users(id),
    product_id        uuid NOT NULL,
    agent_id          uuid REFERENCES agents(id),
    status            varchar(20) NOT NULL,
    payment_frequency varchar(20) NOT NULL DEFAULT 'MONTHLY',  -- Drives premium schedule generation
    premium_amount    decimal(12,2) NOT NULL,
    coverage_amount   decimal(14,2) NOT NULL,
    currency          varchar(3) DEFAULT 'INR',
    start_date        date NOT NULL,
    end_date          date NOT NULL,
    domain            varchar(20) NOT NULL,
    created_at        timestamptz DEFAULT now(),
    deleted_at        timestamptz,
    CONSTRAINT CK_policies_status CHECK (status IN ('ACTIVE', 'LAPSED', 'CANCELLED', 'EXPIRED', 'CLAIMED')),
    CONSTRAINT CK_policies_payment_frequency CHECK (payment_frequency IN ('MONTHLY', 'QUARTERLY', 'ANNUAL')),
    CONSTRAINT UQ_policy_id_domain UNIQUE (id, domain),  -- Safeguard link for claims
    CONSTRAINT FK_policies_product_domain FOREIGN KEY (product_id, domain) REFERENCES insurance_products(id, domain)
);

CREATE UNIQUE INDEX uq_policy_num ON policies(policy_number) WHERE deleted_at IS NULL;

CREATE TABLE policy_versions (
    id                uuid PRIMARY KEY,
    policy_id         uuid NOT NULL REFERENCES policies(id) ON DELETE CASCADE,
    version_number    integer NOT NULL,
    premium_amount    decimal(12,2) NOT NULL,
    coverage_amount   decimal(14,2) NOT NULL,
    effective_from    timestamptz NOT NULL,
    effective_until   timestamptz,
    created_at        timestamptz DEFAULT now(),
    UNIQUE (policy_id, version_number)
);

CREATE TABLE policy_health_details (
    policy_id         uuid PRIMARY KEY REFERENCES policies(id) ON DELETE CASCADE,
    covers_dental     boolean NOT NULL DEFAULT false,
    deductible        decimal(12,2) NOT NULL DEFAULT 0.00,
    network_type      varchar(20) NOT NULL,
    updated_at        timestamptz DEFAULT now(),
    CONSTRAINT CK_health_network CHECK (network_type IN ('TPA', 'CASHLESS', 'REIMBURSEMENT'))
);

CREATE TABLE policy_vehicle_details (
    policy_id              uuid PRIMARY KEY REFERENCES policies(id) ON DELETE CASCADE,
    vehicle_number         varchar(30) NOT NULL UNIQUE,
    make                   varchar(100) NOT NULL,
    model                  varchar(100) NOT NULL,
    manufacture_year       integer NOT NULL,
    insured_declared_value decimal(14,2) NOT NULL,  -- IDV: determines maximum payout
    is_comprehensive       boolean NOT NULL DEFAULT true,  -- false = third-party only
    updated_at             timestamptz DEFAULT now()
);

CREATE TABLE policy_life_details (
    policy_id            uuid PRIMARY KEY REFERENCES policies(id) ON DELETE CASCADE,
    nominee_name         varchar(200) NOT NULL,
    nominee_relation     varchar(50) NOT NULL,
    nominee_phone        varchar(20),
    has_accidental_rider boolean NOT NULL DEFAULT false,
    updated_at           timestamptz DEFAULT now()
);

CREATE TABLE policy_insured_members (
    id                 uuid PRIMARY KEY,
    policy_id          uuid NOT NULL REFERENCES policies(id) ON DELETE CASCADE,
    full_name          varchar(200) NOT NULL,
    date_of_birth      date NOT NULL,
    relation_to_holder varchar(50) NOT NULL,
    is_primary         boolean DEFAULT false
);

CREATE TABLE payments (
    id                     uuid PRIMARY KEY,
    policy_id              uuid REFERENCES policies(id),   -- NULL for claim-only settlements
    claim_id               uuid,                           -- Set for CLAIM_SETTLEMENT type; FK added after claims table
    payment_type           varchar(20) NOT NULL DEFAULT 'PREMIUM',  -- 'PREMIUM', 'CLAIM_SETTLEMENT', 'REFUND'
    original_payment_id    uuid REFERENCES payments(id),  -- Self-ref for REFUND rows
    gateway_transaction_id varchar(100) NOT NULL UNIQUE,
    payment_gateway        varchar(50) NOT NULL,
    amount                 decimal(12,2) NOT NULL,
    status                 varchar(20) NOT NULL,
    paid_at                timestamptz,
    CONSTRAINT CK_payment_status CHECK (status IN ('SUCCESS', 'FAILED', 'PENDING', 'DISPUTED', 'REFUNDED')),
    CONSTRAINT CK_payment_type CHECK (payment_type IN ('PREMIUM', 'CLAIM_SETTLEMENT', 'REFUND'))
);

CREATE TABLE payment_status_history (
    id             uuid PRIMARY KEY,
    payment_id     uuid NOT NULL REFERENCES payments(id) ON DELETE CASCADE,
    old_status     varchar(20),
    new_status     varchar(20) NOT NULL,
    changed_by     uuid REFERENCES users(id),
    remarks        text,
    changed_at     timestamptz DEFAULT now()
);

CREATE TABLE premium_schedule (
    id                  uuid PRIMARY KEY,
    policy_id           uuid NOT NULL REFERENCES policies(id) ON DELETE CASCADE,
    installment_number  integer NOT NULL,
    amount_due          decimal(12,2) NOT NULL,
    due_date            date NOT NULL,
    status              varchar(20) NOT NULL,
    payment_id          uuid REFERENCES payments(id),
    UNIQUE (policy_id, installment_number),
    CONSTRAINT CK_premium_schedule_status CHECK (status IN ('PENDING', 'PAID', 'OVERDUE'))
);
```

### 1.5. Claims Domain

```sql
CREATE TABLE claims (
    id                     uuid PRIMARY KEY,
    claim_number           varchar(50) NOT NULL UNIQUE,
    policy_id              uuid NOT NULL,
    submitted_by           uuid NOT NULL REFERENCES users(id),
    assigned_adjuster      uuid REFERENCES users(id),
    status                 varchar(30) NOT NULL,
    claimed_amount         decimal(14,2) NOT NULL,
    approved_amount        decimal(14,2),
    rejection_reason       text,
    incident_description   text,                          -- Mandatory on submission
    priority               smallint NOT NULL DEFAULT 3,   -- 1=critical, 2=high, 3=normal; SLA escalation
    domain                 varchar(20) NOT NULL,
    incident_date          timestamptz NOT NULL,
    is_automated_processed boolean DEFAULT false,
    risk_score             decimal(5,2) DEFAULT 0.00,
    created_at             timestamptz DEFAULT now(),
    updated_at             timestamptz,
    CONSTRAINT CK_claims_status CHECK (status IN ('SUBMITTED', 'UNDER_REVIEW', 'ESCALATED', 'APPROVED', 'REJECTED', 'SETTLED', 'CLOSED')),
    CONSTRAINT CK_claims_priority CHECK (priority IN (1, 2, 3)),
    CONSTRAINT UQ_claim_id_domain UNIQUE (id, domain),
    CONSTRAINT FK_claims_policy_domain FOREIGN KEY (policy_id, domain) REFERENCES policies(id, domain)
);

-- Forward FK from payments to claims (payments table created first)
ALTER TABLE payments ADD CONSTRAINT FK_payments_claim FOREIGN KEY (claim_id) REFERENCES claims(id);

CREATE TABLE claim_health_details (
    claim_id          uuid PRIMARY KEY REFERENCES claims(id) ON DELETE CASCADE,
    hospital_name     varchar(200) NOT NULL,
    diagnosis         text NOT NULL,
    treating_doctor   varchar(200) NOT NULL,
    admission_date    date NOT NULL,
    discharge_date    date,
    is_cashless       boolean NOT NULL DEFAULT false,
    insured_member_id uuid REFERENCES policy_insured_members(id),  -- Which member this claim is for
    updated_at        timestamptz DEFAULT now()
);

CREATE TABLE claim_vehicle_details (
    claim_id          uuid PRIMARY KEY REFERENCES claims(id) ON DELETE CASCADE,
    accident_location text NOT NULL,
    fir_number        varchar(50),
    repair_estimate   decimal(14,2) NOT NULL DEFAULT 0.00,
    is_total_loss     boolean NOT NULL DEFAULT false,  -- FIR mandatory when true
    surveyor_name     varchar(200),                    -- Required before moving past UNDER_REVIEW
    updated_at        timestamptz DEFAULT now()
);

CREATE TABLE claim_life_details (
    claim_id                 uuid PRIMARY KEY REFERENCES claims(id) ON DELETE CASCADE,
    cause_of_death           varchar(255) NOT NULL,
    place_of_death           varchar(255) NOT NULL,
    death_certificate_number varchar(100),
    certifying_doctor        varchar(200) NOT NULL,
    claimant_name            varchar(200) NOT NULL,  -- Must match nominee on policy_life_details
    claimant_relation        varchar(50) NOT NULL,
    updated_at               timestamptz DEFAULT now()
);

CREATE TABLE claim_workflow (
    id                 uuid PRIMARY KEY,
    claim_id           uuid NOT NULL REFERENCES claims(id) ON DELETE CASCADE,
    actor_id           uuid NOT NULL REFERENCES users(id),
    from_status        varchar(30),  -- NULL on initial submission
    to_status          varchar(30) NOT NULL,
    remarks            text,
    transitioned_at    timestamptz DEFAULT now()
);

CREATE TABLE document_types (
    id                   uuid PRIMARY KEY,
    code                 varchar(50) NOT NULL,
    domain               varchar(20) NOT NULL,
    name                 varchar(150) NOT NULL,
    is_sensitive_phi_pii boolean DEFAULT false,
    CONSTRAINT UQ_doctype_code_domain UNIQUE (code, domain)
);

CREATE TABLE documents (
    id                  uuid PRIMARY KEY,
    claim_id            uuid,
    policy_id           uuid,
    user_id             uuid NOT NULL REFERENCES users(id),
    domain              varchar(20) NOT NULL,
    document_type_code  varchar(50) NOT NULL,
    file_name           varchar(300) NOT NULL,
    file_path           text NOT NULL,
    verification_status varchar(20) NOT NULL DEFAULT 'PENDING',  -- 'PENDING', 'VERIFIED', 'REJECTED'
    uploaded_at         timestamptz DEFAULT now(),
    deleted_at          timestamptz,  -- Soft delete; physical blob removal handled by background job
    CONSTRAINT CK_doc_verification CHECK (verification_status IN ('PENDING', 'VERIFIED', 'REJECTED')),
    CONSTRAINT FK_documents_claim FOREIGN KEY (claim_id, domain) REFERENCES claims(id, domain) ON DELETE CASCADE,
    CONSTRAINT FK_documents_policy FOREIGN KEY (policy_id, domain) REFERENCES policies(id, domain) ON DELETE CASCADE,
    CONSTRAINT FK_documents_doctype FOREIGN KEY (document_type_code, domain) REFERENCES document_types(code, domain)
);

CREATE TABLE claim_document_checklist (
    id                  uuid PRIMARY KEY,
    claim_id            uuid NOT NULL,
    domain              varchar(20) NOT NULL,
    document_type_code  varchar(50) NOT NULL,
    is_received         boolean NOT NULL DEFAULT false,
    updated_at          timestamptz,
    CONSTRAINT FK_checklist_claim FOREIGN KEY (claim_id, domain) REFERENCES claims(id, domain) ON DELETE CASCADE,
    CONSTRAINT FK_checklist_doctype FOREIGN KEY (document_type_code, domain) REFERENCES document_types(code, domain),
    UNIQUE (claim_id, document_type_code)
);
```

## 2. Role-Based Access Control (RBAC)

RBAC is managed entirely at the application level using a Matrix-Based App Role pattern. Core structural permissions (e.g., `CUSTOMER`, `AGENT`, `ADJUSTER`, `ADMIN`) are managed via the `roles` table.

Scope and seniority brackets are enforced by the API logic using the `domain` and `approval_limit` attributes on the `user_roles` table. This provides total flexibility for infinite brackets without database-level complexity.

## 3. Performance Optimization (Indexing)

```sql
-- Domain + status lookups for dashboards and reporting
CREATE INDEX idx_policies_domain_status ON policies(domain, status);
CREATE INDEX idx_claims_domain_status ON claims(domain, status);

-- Financial lookups
CREATE INDEX idx_payments_policy_id ON payments(policy_id);
CREATE INDEX idx_payments_claim_id ON payments(claim_id);
CREATE INDEX idx_premium_schedule_due ON premium_schedule(due_date, status);

-- Foreign key indexes for JOIN performance and cascading deletes
CREATE INDEX idx_user_roles_user_id ON user_roles(user_id);
CREATE INDEX idx_user_roles_role_id ON user_roles(role_id);
CREATE INDEX idx_refresh_tokens_user_id ON refresh_tokens(user_id);
CREATE INDEX idx_claim_workflow_claim_id ON claim_workflow(claim_id);
CREATE INDEX idx_claim_workflow_actor_id ON claim_workflow(actor_id);
CREATE INDEX idx_policies_user_id ON policies(user_id);
CREATE INDEX idx_claims_policy_id ON claims(policy_id);
CREATE INDEX idx_documents_claim_id ON documents(claim_id);
CREATE INDEX idx_documents_policy_id ON documents(policy_id);
```
