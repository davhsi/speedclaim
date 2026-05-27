# Auth & Roles Requirements

## Overview
The application uses JWT-based authentication with stateful refresh token management.
Users have a single identity (USERS table) and can hold multiple roles simultaneously
via the USER_ROLES junction table. All role assignments are time-bounded and auditable.
Agents are a regulated sub-entity with their own AGENTS table linked 1:1 to a USERS row.

## User Roles
- **Admin:** Full system access. Manages user accounts, assigns/revokes roles, configures
  insurance products, and oversees compliance reporting.
- **Policyholder:** End customer. Can purchase policies, view their own policy and claim
  history, submit claims, and upload supporting documents.
- **Agent:** IRDAI-licensed intermediary. Can create and manage policies on behalf of
  policyholders. System enforces license expiry — expired agents are locked out of
  policy creation automatically.
- **Adjuster:** Internal claims processor. Reviews submitted claims, transitions claim
  workflow states, approves or rejects with mandatory reasoning, and triggers settlement
  payments.
- **Auditor:** Read-only compliance role. Can access AUDIT_LOGS, PHI_ACCESS_LOGS, and
  claim/policy history for regulatory reporting. Cannot modify any data.

## Authentication Workflows
- **Sign Up:** Email + password registration. Email must be verified before full access
  is granted (IRDAI Digital KYC requirement). Password stored as bcrypt/argon2 hash,
  never plaintext.
- **Login:** Credentials validated → short-lived JWT access token issued + long-lived
  refresh token stored as SHA-256 hash in REFRESH_TOKENS with IP address logged.
- **Token Refresh:** Client presents refresh token → server validates hash + expiry +
  is_revoked flag → new access token issued.
- **Logout:** Sets is_revoked = true on the active REFRESH_TOKENS row, immediately
  invalidating the session.
- **Role Assignment:** Admin assigns roles via USER_ROLES with assigned_at timestamp
  and assigned_by reference. Revocation sets revoked_at; the row is never deleted.

## Security & Access Control
- Passwords must meet minimum complexity requirements; stored exclusively as
  bcrypt/argon2 hashes.
- Refresh tokens stored as SHA-256 hashes only — raw token never persisted.
- JWT access tokens are short-lived (recommended: 15 minutes). Refresh tokens have
  a defined TTL enforced via expires_at.
- RBAC enforced at API layer — every endpoint checks the user's currently active roles
  (USER_ROLES rows where revoked_at IS NULL).
- PHI fields (date_of_birth, health details) are access-logged via PHI_ACCESS_LOGS on
  every read.
- PII fields (full_name, phone, address) are encrypted at rest.
- GDPR right-to-erasure handled via anonymized_at — PII is scrubbed but the user row
  is retained to preserve FK integrity across policies, claims, and audit logs.
- Agents with expired licenses (license_valid_until < today) are blocked from
  policy-creation endpoints regardless of active role assignment.
