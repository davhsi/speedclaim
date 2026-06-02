# Compliance Requirements

## Overview
The system operates under two regulatory frameworks: HIPAA (Protected Health Information
handling) and IRDAI 2024 regulations (Indian insurance intermediary and policyholder
protection rules). Compliance is enforced at the schema level — not just application
logic — through dedicated tables, soft deletes, immutable logs, and structured consent
tracking.

## Data Privacy & Consents
- Consent is captured per user per type via the USER_CONSENTS table. Three consent
  types are tracked: MARKETING, DATA_PROCESSING, and HEALTH_DATA_SHARING.
- Every consent row records the privacy policy version accepted (consent_version e.g.
  "v2.3"), the IP address of the consent origin, and the granted_at timestamp —
  providing an auditable proof of consent.
- Users can withdraw consent at any time. Withdrawal sets withdrawn_at timestamp on
  the relevant row; the original grant record is never deleted, preserving the audit
  trail.
- Health data processing requires explicit HEALTH_DATA_SHARING consent before any
  PHI fields in CLAIM_HEALTH_DETAILS or POLICY_INSURED_MEMBERS are accessible.
- IRDAI mandates explicit policyholder communication consent — tracked via the
  DATA_PROCESSING consent type before any notification is dispatched.

## Audit Logging
- Every state-changing action (CREATE, UPDATE, DELETE, APPROVE, REJECT) on any
  critical entity is written to AUDIT_LOGS with before/after JSONB snapshots,
  the actor's user ID, IP address, and an immutable created_at timestamp. Financial
  status changes are additionally logged in PAYMENT_STATUS_HISTORY.
  No UPDATE or DELETE operations are ever permitted against AUDIT_LOGS in production.
- Claim workflow transitions are independently logged in CLAIM_WORKFLOW with
  from_status (nullable on initial submission), to_status, actor_id, remarks, and
  an immutable transitioned_at timestamp — providing a full state machine history
  for every claim independent of AUDIT_LOGS.
- EF Core SaveChangesAsync is overridden to throw an exception if any modification
  is attempted on AuditLog entities.

## Data Retention & Deletion
- Hard deletes are prohibited on all critical tables. Soft deletes via deleted_at
  timestamps are used on USERS, POLICIES, and DOCUMENTS — preserving FK integrity
  across the entire relational graph and enabling partial indexes (e.g. for policy numbers).
- When a user requests deletion, deleted_at is set and is_active is set to false.
  All user data is retained in full to preserve FK integrity across POLICIES, CLAIMS,
  AUDIT_LOGS, and PAYMENTS, and to satisfy IRDAI minimum retention requirements.
- IRDAI mandates minimum data retention periods for policy and claim records;
  deleted_at flags mark records as inactive without removing them from the database.
- Document soft deletes via deleted_at on DOCUMENTS — physical blob deletion from
  storage is handled separately by a background job after the retention period elapses.