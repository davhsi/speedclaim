# Compliance Requirements

## Overview
The system operates under three regulatory frameworks simultaneously: HIPAA (Protected
Health Information handling), GDPR (EU data privacy and right-to-erasure), and IRDAI
2024 regulations (Indian insurance intermediary and policyholder protection rules).
Compliance is enforced at the schema level — not just application logic — through
dedicated tables, soft deletes, immutable logs, and encrypted PII fields.

## Data Privacy & Consents
- Consent is captured per user per type via the USER_CONSENTS table. Three consent
  types are tracked: MARKETING, DATA_PROCESSING, and HEALTH_DATA_SHARING.
- Every consent row records the privacy policy version accepted (consent_version e.g.
  "v2.3"), the IP address of the consent origin, and the granted_at timestamp —
  providing legally defensible proof of consent under GDPR Art.6 & Art.7.
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
  the actor's user ID, IP address, and an immutable created_at timestamp.
  No UPDATE or DELETE operations are ever permitted against AUDIT_LOGS in production.
- Every read of a PHI field — date_of_birth on USERS, all fields in
  CLAIM_HEALTH_DETAILS, POLICY_INSURED_MEMBERS PHI columns — is logged to
  PHI_ACCESS_LOGS with accessor_id, subject_id, entity_type, entity_id, IP address,
  user_agent, and accessed_at. This satisfies HIPAA §164.312(b) and §164.528
  (accounting of disclosures).
- Claim workflow transitions are independently logged in CLAIM_WORKFLOW with
  from_status (nullable on initial submission), to_status, actor_id, remarks, and
  an immutable transitioned_at timestamp — providing a full state machine history
  for every claim independent of AUDIT_LOGS.
- NOTIFICATIONS table serves as compliance proof of mandatory policyholder
  communications required under IRDAI Policyholders' Interests Reg. 2024 — every
  sent, failed, or bounced message is retained with its payload and error details.
- EF Core SaveChangesAsync is overridden to throw an exception if any modification
  is attempted on AuditLog or PhiAccessLog entities.

## Data Retention & Deletion
- Hard deletes are prohibited on all critical tables. Soft deletes via deleted_at
  timestamps are used on USERS, POLICIES, CLAIMS, and DOCUMENTS — preserving FK
  integrity across the entire relational graph.
- GDPR Art.17 right-to-erasure is handled via anonymized_at on the USERS table.
  When a deletion request is processed, PII fields (full_name, phone, address,
  date_of_birth) are scrubbed/nulled and anonymized_at is set. The user row itself
  is retained so that all foreign key references in POLICIES, CLAIMS, AUDIT_LOGS,
  and PAYMENTS remain intact.
- Policy and claim records must be retained per GDPR Art.17(3)(b) — legal obligation
  overrides erasure requests for active insurance contracts.
- IRDAI mandates minimum data retention periods for policy and claim records;
  deleted_at flags mark records as inactive without removing them from the database.
- A scheduled background job (pg_cron or Quartz.NET) processes anonymization for
  users where anonymized_at IS NOT NULL but PII fields have not yet been scrubbed,
  ensuring eventual consistency on erasure requests.
- Document soft deletes via deleted_at on DOCUMENTS — physical blob deletion from
  Azure Blob Storage is handled separately by the same background job after the
  retention period has elapsed.