# Policy Requirements

## Overview
A policy is the central contract entity of the system, instantiated from an
INSURANCE_PRODUCTS template and linked to a policyholder, an optional agent, and
exactly one domain-specific detail table. The policy lifecycle is governed by payment
status, expiry dates, and claim activity. Premium schedules are generated at issuance
and drive all billing, lapse, and grace period logic.

## Policy Issuance & Lifecycle
- A policy is created by an Admin or a licensed Agent (agent_id nullable for direct
  purchases). Creation requires a valid INSURANCE_PRODUCTS reference, a policyholder
  user, coverage amount within product bounds, and a selected payment frequency.
- A human-readable policy_number is auto-generated on issuance in the format
  POL-YYYY-NNNNN (e.g. POL-2026-00001) and is unique across the system.
- Policies move through the following states:
    - **ACTIVE:** Policy is in force. Premiums are current.
    - **LAPSED:** A scheduled premium installment passed its due_date without payment
      and the grace period has elapsed. Triggered automatically by the billing cron job.
    - **CANCELLED:** Manually cancelled by Admin or policyholder before expiry.
      Cancellation reason should be recorded via AUDIT_LOGS.
    - **EXPIRED:** Policy reached its end_date without renewal. Set automatically.
    - **CLAIMED:** A life policy where the death benefit has been fully settled.
      Terminal state — no further premiums or claims are accepted.
- Soft delete via deleted_at is available for administrative removal. IRDAI retention
  rules prohibit hard deletes; the record must remain queryable for regulatory audits.
- Policy updates (coverage changes, agent reassignment) are captured in AUDIT_LOGS
  with old_values and new_values JSONB snapshots before any mutation is committed.
- A LAPSED policy can be reinstated to ACTIVE by an Admin if outstanding premiums
  are cleared within a defined reinstatement window — handled via a status update
  with full audit trail.

## Members & Coverage
- Every policy has at least one entry in POLICY_INSURED_MEMBERS. Exactly one member
  must have is_primary = true per policy, enforced at the application layer.
- Health policies support multiple insured members (family floater plans) — spouse,
  children, and parents linked via relation_to_holder. All member fields (full_name,
  date_of_birth, gender) are PHI and encrypted at rest.
- Vehicle policies are 1:1 with a single vehicle. The vehicle_number (registration
  plate) carries a unique constraint — no two active policies can cover the same
  vehicle. IDV (insured_declared_value) is mandatory and determines maximum payout.
  is_comprehensive distinguishes comprehensive from third-party only cover.
- Life policies are 1:1 and require a nominee (nominee_name, nominee_relation,
  nominee_phone) before issuance. The nominee is the legal recipient of the death
  benefit and must match claim settlement details at payout time. Optional accidental
  rider tracked via has_accidental_rider boolean.
- Domain detail rows (POLICY_HEALTH_DETAILS, POLICY_VEHICLE_DETAILS,
  POLICY_LIFE_DETAILS) are created atomically with the POLICIES row in a single
  transaction — a policy cannot exist without its domain extension.

## Premiums & Payments
- Premium amount is set at issuance on the POLICIES row. The payment_frequency
  (MONTHLY | QUARTERLY | ANNUAL) determines how the total premium is split across
  the policy tenure.
- On policy issuance, the full PREMIUM_SCHEDULE is generated upfront — one row per
  installment with installment_number, amount_due, due_date, and status PENDING.
  This schedule drives all billing automation.
- Payment flow: policyholder initiates payment → a PAYMENTS row is created with
  status PENDING and a gateway_transaction_id → gateway confirms →
  PAYMENTS.status updated to SUCCESS and paid_at populated →
  corresponding PREMIUM_SCHEDULE row updated to PAID with payment_id FK set.
- PREMIUM_SCHEDULE rows where payment_id IS NULL and due_date has passed are the
  trigger for the overdue cron job, which transitions status to OVERDUE and
  dispatches premium overdue notifications.
- A grace period is observed before transitioning a policy to LAPSED — the exact
  grace window is configurable at the application layer. During grace period the
  policy remains ACTIVE but notifications escalate.
- If a claim is approved during a grace period, it is settled normally. The overdue
  premium remains outstanding on the PREMIUM_SCHEDULE and the lapse cron job continues
  to run independently. The overdue premium is not deducted from the claim settlement.
- Refunds are modelled as a new PAYMENTS row with payment_type = REFUND and
  original_payment_id pointing to the original transaction, satisfying the
  self-referencing idempotency requirement.
- PAYMENTS supports both premium intake (policy_id set, claim_id null) and claim
  settlements (claim_id set, policy_id nullable) in the same table, distinguished
  by payment_type (PREMIUM | CLAIM_SETTLEMENT | REFUND).
- All monetary amounts use decimal(12,2) with ISO 4217 currency code (default INR).
  No floating point types are used anywhere in financial calculations.
