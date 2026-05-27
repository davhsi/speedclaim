# Claims Requirements

## Overview
A claim is filed against an active policy and processed through a structured workflow
managed by assigned adjusters. Every state transition is immutably recorded in
CLAIM_WORKFLOW. Domain-specific detail tables (CLAIM_HEALTH_DETAILS,
CLAIM_VEHICLE_DETAILS, CLAIM_LIFE_DETAILS) extend the core CLAIMS row with
type-specific evidence. Documents are attached via the DOCUMENTS table. Settlement
is executed via the PAYMENTS table on approval.

## Claim Submission
- Claims can be submitted by a POLICYHOLDER or an AGENT on behalf of a policyholder.
  submitted_by on the CLAIMS row records which user filed the claim.
- The policy referenced must be in ACTIVE status at the time of submission.
  LAPSED, CANCELLED, EXPIRED, or CLAIMED policies are rejected at the API layer.
- A human-readable claim_number is auto-generated on submission in the format
  CLM-YYYY-NNNNN (e.g. CLM-2026-00001), unique across the system.
- incident_date and incident_description are mandatory on submission.
  incident_date must not be after the submission date and must fall within the
  policy's start_date and end_date range.
- claimed_amount is provided by the submitter and cannot exceed the policy's
  coverage_amount.
- On submission a domain-specific detail row is created atomically with the CLAIMS
  row in a single transaction:
    - **Health:** CLAIM_HEALTH_DETAILS requires hospital_name, diagnosis,
      treating_doctor, admission_date, and is_cashless flag. insured_member_id
      must reference a valid member on the parent policy.
    - **Vehicle:** CLAIM_VEHICLE_DETAILS requires accident_location and
      repair_estimate. fir_number is mandatory if is_total_loss is true.
      surveyor_name is required before the claim can move past UNDER_REVIEW.
    - **Life:** CLAIM_LIFE_DETAILS requires cause_of_death, certifying_doctor,
      claimant_name, and claimant_relation. claimant_name must match the nominee
      recorded on POLICY_LIFE_DETAILS before settlement can be triggered.
- Required documents must be uploaded to DOCUMENTS with the claim_id set before
  the claim can advance from SUBMITTED to UNDER_REVIEW:
    - Health: discharge summary, treating doctor certificate, bills
    - Vehicle: FIR copy (if applicable), repair estimate, surveyor report
    - Life: death certificate, claimant identity proof, nominee relationship proof
- All document uploads trigger a DOCUMENTS.verification_status of PENDING.
  The assigned adjuster is responsible for verifying each document while the claim is
  in SUBMITTED state before advancing it to UNDER_REVIEW.

## Claim Processing Workflow
- Claims move through the following states. Every transition writes an immutable
  row to CLAIM_WORKFLOW with from_status (NULL on first entry), to_status,
  actor_id, remarks, and transitioned_at:
    - **SUBMITTED:** Initial state on filing. assigned_adjuster is NULL until
      an Admin assigns the claim. Document verification is the responsibility of the assigned adjuster.
    - **UNDER_REVIEW:** Adjuster has been assigned and has begun assessment.
      This state is reachable only when all required documents are verified AND
      assigned_adjuster IS NOT NULL.
    - **APPROVED:** Adjuster has validated the claim. approved_amount is set
      (may differ from claimed_amount). A PAYMENTS row with
      payment_type = CLAIM_SETTLEMENT and status PENDING is created atomically.
    - **REJECTED:** Adjuster has denied the claim. rejection_reason is mandatory
      — the API must reject any REJECTED transition where rejection_reason is
      null or empty. Policyholder is notified via EMAIL with the reason.
    - **SETTLED:** Payment gateway has confirmed successful disbursement.
      PAYMENTS.status = SUCCESS and paid_at is populated. For life policies
      this triggers a POLICIES.status transition to CLAIMED.
    - **CLOSED:** Administrative terminal state for claims that are withdrawn,
      duplicate, or administratively resolved without settlement.
- Backward transitions are not permitted. The workflow is strictly linear:
  SUBMITTED → UNDER_REVIEW → APPROVED → SETTLED, with REJECTED and CLOSED
  available as terminal exits from UNDER_REVIEW or APPROVED respectively.
- SLA monitoring: IRDAI mandates 30-day claim settlement targets. The SLA clock
  starts at CLAIMS.created_at. A background job monitors claims approaching the
  30-day window and escalates priority (priority field: 1=critical, 2=high,
  3=normal) and notifies the assigned adjuster.
- priority can be manually overridden by Admin at any point in the workflow.

## Adjuster & Approval Rules
- Only users with the ADJUSTER role can transition claims from SUBMITTED to
  UNDER_REVIEW, and from UNDER_REVIEW to APPROVED or REJECTED.
- For the capstone, all adjusters have unlimited financial authority. Future extension:
  add an `approval_limit` column to a future `ADJUSTER_PROFILES` table.
- Only ADMIN users can assign or reassign the assigned_adjuster on a CLAIMS row.
- Adjusters can only action claims assigned to them. An adjuster cannot approve
  or reject a claim where assigned_adjuster != their own user ID, enforced at
  the API layer.
- approved_amount must be greater than zero and cannot exceed the lesser of
  claimed_amount and the policy's coverage_amount. The API rejects any approval
  where this constraint is violated.
- Rejection requires a non-empty rejection_reason. This is a mandatory textual
  declaration for adverse adjudications per IRDAI Policyholders' Interests
  Reg. 2024 — the field cannot be bypassed.
- All approval and rejection actions are written to both CLAIM_WORKFLOW
  (for claim-specific history) and AUDIT_LOGS (for system-wide compliance log)
  in the same transaction.
- PHI fields accessed during claim review (diagnosis, cause_of_death,
  hospital_name, treating_doctor) must log a PHI_ACCESS_LOGS entry per read,
  recording the adjuster's user ID as accessor_id.
- ADMIN users can force-close any claim (transition to CLOSED) at any workflow
  stage with a mandatory remarks entry in CLAIM_WORKFLOW.