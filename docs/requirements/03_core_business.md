# Core Business Requirements

## Overview
The core business layer consists of three entities: a unified insurance product catalog
that drives domain-specific policy structures, a regulated agent management system
compliant with IRDAI intermediary rules, and a templated notification system that
satisfies mandatory policyholder communication requirements. These entities underpin
every downstream policy and claim operation.

## Insurance Products
- Three insurance domains are supported: HEALTH, VEHICLE, and LIFE. The domain field
  on INSURANCE_PRODUCTS drives which detail table (POLICY_HEALTH_DETAILS,
  POLICY_VEHICLE_DETAILS, or POLICY_LIFE_DETAILS) is created when a policy is
  instantiated from that product.
- Products are defined as templates in the INSURANCE_PRODUCTS catalog with a unique
  product code (e.g. HEALTH_INDIVIDUAL_V1), coverage bounds (min_premium,
  max_coverage), and tenure limits (min_tenure_months, max_tenure_months).
- Health products support configurable coverage flags: dental, vision, maternity,
  room rent limit per day, deductible amount, and network type
  (TPA | CASHLESS | REIMBURSEMENT).
- Vehicle products support two-wheelers, cars, and commercial vehicles. Key attributes
  are Insured Declared Value (IDV), comprehensive vs. third-party coverage, and
  registration plate as a unique constraint.
- Life products support TERM, WHOLE_LIFE, ENDOWMENT, and ULIP policy types with
  nominee details and optional accidental rider.
- Products can be deactivated (is_active = false) without deletion, preserving
  historical metrics for all policies already issued against that product.
- Payment frequency options per policy are MONTHLY, QUARTERLY, and ANNUAL —
  enforced via CHECK constraint on POLICIES.

## Agents & Agencies
- Agents are onboarded as a two-step process: a USERS row is created first, then an
  AGENTS row is linked 1:1 via user_id with their IRDAI-issued license number
  (unique constraint), affiliated agency name, and license validity date.
- The system enforces license expiry automatically — agents with
  license_valid_until < today are blocked from policy-creation endpoints regardless
  of their active role assignment in USER_ROLES.
- Commission rates are stored per agent as a decimal percentage. IRDAI regulatory
  disclosure requirements mandate this is recorded and auditable.
- Commission calculation is derived from `AGENTS.commission_rate × PAYMENTS.amount` where
  payment_type = PREMIUM and status = SUCCESS. Actual disbursement is a batch process
  outside the core system scope for the capstone (planned future extension via a
  COMPLIANCE_MONEY_LEDGER table).
- Agents can be deactivated (is_active = false) without deleting their record,
  preserving the full transactional and audit trail for all policies they issued.
- Policies sold directly by policyholders without an agent set agent_id to NULL on
  the POLICIES row — direct purchase is a supported flow.
- Future architecture includes BROKER_QUALIFIED_PERSONS for corporate broker entities
  where individual certified professionals (BQPs) must be mapped to transactions,
  with NIA certification numbers and principal officer flags tracked separately.

## Notifications & Communications
- All notifications are template-driven via NOTIFICATION_TEMPLATES. Templates are
  keyed by a unique code (e.g. CLAIM_APPROVED_EMAIL) and support three channels:
  EMAIL, SMS, and PUSH. Email templates include a subject line. All templates use
  Handlebars-style variable substitution.
- Every dispatched notification is recorded in the NOTIFICATIONS table with status
  (PENDING | SENT | FAILED | BOUNCED), the rendered payload as JSONB, and
  error_message populated on failure — enabling automatic multi-channel fallback
  and retry logic.
- The following events must trigger notifications per IRDAI Policyholders'
  Interests Reg. 2024 mandatory communication requirements:
    - Policy issued → policyholder EMAIL + SMS
    - Premium due reminder (driven by PREMIUM_SCHEDULE.due_date) → policyholder SMS
    - Premium payment received → policyholder EMAIL
    - Premium overdue / policy lapse warning → policyholder EMAIL + SMS
    - Claim submitted confirmation → policyholder EMAIL + SMS
    - Claim status change (UNDER_REVIEW, APPROVED, REJECTED, SETTLED) →
      policyholder EMAIL + SMS
    - Claim rejection with mandatory reason → policyholder EMAIL
    - Document verified or rejected → policyholder EMAIL
    - Agent license expiry warning (30 days prior) → agent EMAIL
- sent_at is populated only after gateway delivery confirmation — NULL indicates
  the message has not been confirmed delivered yet.
- NOTIFICATIONS rows are never deleted; they serve as compliance proof of mandatory
  communications.