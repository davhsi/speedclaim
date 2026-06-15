-- =============================================================
-- SpeedClaim — Database Seed Script
-- All passwords: Password@123
-- Run: psql -h localhost -U postgres -d speedclaim -f seed.sql
-- =============================================================
-- UUID key (all chars are 0-9 or a-f — valid hex)
--  Staff users   : 10000000-0000-0000-0000-00000000000N
--  Customer users: 20000000-0000-0000-0000-00000000000N
--  Branch        : 30000000-0000-0000-0000-000000000001
--  Agent record  : 40000000-0000-0000-0000-000000000001
--  Surveyor rec  : 50000000-0000-0000-0000-000000000001
--  Customer recs : 60000000-0000-0000-0000-00000000000N
--  Products      : 70000000-0000-0000-0000-00000000000N
--  Proposals     : 80000000-0000-0000-0000-00000000000N
--  Policies      : 90000000-0000-0000-0000-00000000000N
--  Claims        : a0000000-0000-0000-0000-00000000000N
--  Schedules     : b0000000-0000-0000-0000-00000000000N
--  Payments      : c0000000-0000-0000-0000-00000000000N
-- =============================================================

BEGIN;

-- ─────────────────────────────────────────────────────────────
-- 1. BRANCH
-- ─────────────────────────────────────────────────────────────
INSERT INTO branches (id, name, city, state, address, phone, email, is_active, created_at)
VALUES (
  '30000000-0000-0000-0000-000000000001',
  'Mumbai Central Branch', 'Mumbai', 'Maharashtra',
  '101 Nariman Point, Mumbai - 400021',
  '9876543210', 'mumbai@speedclaim.com',
  true, NOW()
);


-- ─────────────────────────────────────────────────────────────
-- 2. USERS
-- role: 0=Customer 1=Agent 2=Underwriter 3=ClaimsOfficer
--       4=FinanceOfficer 5=Surveyor 6=Admin
-- salutation: 0=Mr 1=Mrs 2=Ms 3=Dr
-- BCrypt of "Password@123" (work=11):
--   $2a$11$X1dqZiAKL4PTVGlilJIHGewazOD6YqRdDZNfBDRNY/XJmOYFyGSSy
-- ─────────────────────────────────────────────────────────────
INSERT INTO users (id, salutation, first_name, last_name, email, phone, password_hash,
                   role, is_email_verified, is_active, failed_login_attempts, is_deleted, created_at)
VALUES
  -- Admin
  ('10000000-0000-0000-0000-000000000001', 3, 'Vikram', 'Nair',
   'davish2204@gmail.com', '9000000001',
   '$2a$11$X1dqZiAKL4PTVGlilJIHGewazOD6YqRdDZNfBDRNY/XJmOYFyGSSy',
   6, true, true, 0, false, NOW()),

  -- Underwriter (internal — no real email needed)
  ('10000000-0000-0000-0000-000000000002', 0, 'Anand', 'Krishnamurthy',
   'underwriter@speedclaim.com', '9000000002',
   '$2a$11$X1dqZiAKL4PTVGlilJIHGewazOD6YqRdDZNfBDRNY/XJmOYFyGSSy',
   2, true, true, 0, false, NOW()),

  -- Claims Officer (internal)
  ('10000000-0000-0000-0000-000000000003', 2, 'Meera', 'Iyer',
   'claimsofficer@speedclaim.com', '9000000003',
   '$2a$11$X1dqZiAKL4PTVGlilJIHGewazOD6YqRdDZNfBDRNY/XJmOYFyGSSy',
   3, true, true, 0, false, NOW()),

  -- Finance Officer (internal)
  ('10000000-0000-0000-0000-000000000004', 0, 'Suresh', 'Pillai',
   'financeofficer@speedclaim.com', '9000000004',
   '$2a$11$X1dqZiAKL4PTVGlilJIHGewazOD6YqRdDZNfBDRNY/XJmOYFyGSSy',
   4, true, true, 0, false, NOW()),

  -- Agent (gets proposal/commission notifications)
  ('10000000-0000-0000-0000-000000000005', 0, 'Rajesh', 'Verma',
   'davishthedelicious@gmail.com', '9000000005',
   '$2a$11$X1dqZiAKL4PTVGlilJIHGewazOD6YqRdDZNfBDRNY/XJmOYFyGSSy',
   1, true, true, 0, false, NOW()),

  -- Surveyor (internal)
  ('10000000-0000-0000-0000-000000000006', 0, 'Deepak', 'Rao',
   'surveyor@speedclaim.com', '9000000006',
   '$2a$11$X1dqZiAKL4PTVGlilJIHGewazOD6YqRdDZNfBDRNY/XJmOYFyGSSy',
   5, true, true, 0, false, NOW()),

  -- Customer 1 — Rahul (gets claim/policy emails)
  ('20000000-0000-0000-0000-000000000001', 0, 'Rahul', 'Sharma',
   'davish.cs22@bitsathy.ac.in', '9876500001',
   '$2a$11$X1dqZiAKL4PTVGlilJIHGewazOD6YqRdDZNfBDRNY/XJmOYFyGSSy',
   0, true, true, 0, false, NOW()),

  -- Customer 2 — Priya (gets claim/policy emails)
  ('20000000-0000-0000-0000-000000000002', 2, 'Priya', 'Patel',
   'davishoffl@gmail.com', '9876500002',
   '$2a$11$X1dqZiAKL4PTVGlilJIHGewazOD6YqRdDZNfBDRNY/XJmOYFyGSSy',
   0, true, true, 0, false, NOW()),

  -- Customer 3 — Arjun (proposal under review; you will register davish.std manually instead)
  ('20000000-0000-0000-0000-000000000003', 0, 'Arjun', 'Mehta',
   'arjun.seed@speedclaim.internal', '9876500003',
   '$2a$11$X1dqZiAKL4PTVGlilJIHGewazOD6YqRdDZNfBDRNY/XJmOYFyGSSy',
   0, true, true, 0, false, NOW());


-- ─────────────────────────────────────────────────────────────
-- 3. ADDRESSES  (address_type: 0=Permanent)
-- ─────────────────────────────────────────────────────────────
INSERT INTO addresses (id, user_id, address_type, address_line1, city, state,
                       pincode, country, is_same_as_permanent, created_at)
VALUES
  (gen_random_uuid(), '20000000-0000-0000-0000-000000000001', 0,
   '42 MG Road', 'Mumbai', 'Maharashtra', '400001', 'India', false, NOW()),
  (gen_random_uuid(), '20000000-0000-0000-0000-000000000002', 0,
   '15 Nehru Street', 'Ahmedabad', 'Gujarat', '380001', 'India', false, NOW()),
  (gen_random_uuid(), '20000000-0000-0000-0000-000000000003', 0,
   '8 Park Avenue', 'Delhi', 'Delhi', '110001', 'India', false, NOW());


-- ─────────────────────────────────────────────────────────────
-- 4. AGENT & SURVEYOR
-- agent_type: 0=Internal   surveyor_type: 0=Internal
-- specialization: 1=FourWheeler
-- ─────────────────────────────────────────────────────────────
INSERT INTO agents (id, user_id, branch_id, agent_code, agent_type,
                    license_number, license_expiry, commission_rate, is_active, created_at)
VALUES (
  '40000000-0000-0000-0000-000000000001',
  '10000000-0000-0000-0000-000000000005',
  '30000000-0000-0000-0000-000000000001',
  'AGT-2024-001', 0, 'LIC-MH-2024-8821', '2027-12-31', 5.00, true, NOW()
);

INSERT INTO surveyors (id, user_id, surveyor_type, license_number, license_expiry,
                       specialization, is_active, created_at)
VALUES (
  '50000000-0000-0000-0000-000000000001',
  '10000000-0000-0000-0000-000000000006',
  0, 'SRV-MH-2024-441', '2027-06-30', 1, true, NOW()
);


-- ─────────────────────────────────────────────────────────────
-- 5. CUSTOMERS
-- gender: 0=Male 1=Female   marital_status: 0=Single 1=Married
-- ─────────────────────────────────────────────────────────────
INSERT INTO customers (id, user_id, date_of_birth, gender, occupation,
                       annual_income, marital_status, created_at)
VALUES
  ('60000000-0000-0000-0000-000000000001',
   '20000000-0000-0000-0000-000000000001',
   '1990-05-14', 0, 'Software Engineer', 1200000, 1, NOW()),

  ('60000000-0000-0000-0000-000000000002',
   '20000000-0000-0000-0000-000000000002',
   '1988-11-22', 1, 'Doctor', 1800000, 1, NOW()),

  ('60000000-0000-0000-0000-000000000003',
   '20000000-0000-0000-0000-000000000003',
   '1995-03-08', 0, 'Business Analyst', 900000, 0, NOW());


-- ─────────────────────────────────────────────────────────────
-- 6. KYC RECORDS
-- kyc_status: 0=Pending  2=Approved   id_type: 0=Aadhaar  1=Pan
-- Note: IdNumber stored as plain text in seed (no encryption key at seed time)
-- ─────────────────────────────────────────────────────────────
INSERT INTO kyc_records (id, user_id, kyc_status, id_type, id_number,
                         reviewed_by_id, reviewed_at, created_at)
VALUES
  (gen_random_uuid(), '20000000-0000-0000-0000-000000000001',
   2, 0, '123456789012',
   '10000000-0000-0000-0000-000000000001', NOW(), NOW()),

  (gen_random_uuid(), '20000000-0000-0000-0000-000000000002',
   2, 1, 'BVQPK1234F',
   '10000000-0000-0000-0000-000000000001', NOW(), NOW()),

  (gen_random_uuid(), '20000000-0000-0000-0000-000000000003',
   0, 0, '987654321098',
   NULL, NULL, NOW());


-- ─────────────────────────────────────────────────────────────
-- 7. INSURANCE PRODUCTS
-- ─────────────────────────────────────────────────────────────
INSERT INTO insurance_products (id, product_name, domain, uin, description,
  min_age, max_age, min_sum_assured, max_sum_assured,
  min_tenure_years, max_tenure_years, waiting_period_days,
  allows_family_floater, max_family_members, is_active, created_by_id, created_at)
VALUES
  ('70000000-0000-0000-0000-000000000001',
   'SpeedShield Health Plan', 'Health', 'IRDA/HLT/SC/2024/001',
   'Comprehensive health insurance covering hospitalisation, day-care, and OPD expenses.',
   18, 65, 200000, 10000000, 1, 30, 30,
   true, 6, true, '10000000-0000-0000-0000-000000000001', NOW()),

  ('70000000-0000-0000-0000-000000000002',
   'SpeedDrive Motor Insurance', 'Motor', 'IRDA/MTR/SC/2024/002',
   'Comprehensive motor insurance with zero depreciation and 24x7 roadside assistance.',
   18, 70, 100000, 5000000, 1, 5, 0,
   false, 1, true, '10000000-0000-0000-0000-000000000001', NOW()),

  ('70000000-0000-0000-0000-000000000003',
   'SpeedLife Term Plan', 'Life', 'IRDA/LIF/SC/2024/003',
   'Pure term life insurance with high sum assured at affordable premiums.',
   18, 60, 1000000, 50000000, 10, 40, 0,
   false, 1, true, '10000000-0000-0000-0000-000000000001', NOW());


-- ─────────────────────────────────────────────────────────────
-- 8. PROPOSALS
-- status: 2=UnderReview  4=Approved   policy_type: 0=Individual
-- ─────────────────────────────────────────────────────────────
INSERT INTO proposals (id, proposal_number, customer_id, agent_id, product_id,
  policy_type, sum_assured, tenure_years, premium_amount, payment_frequency,
  status, underwriter_id, underwriter_notes, submitted_at, reviewed_at,
  is_deleted, created_at)
VALUES
  ('80000000-0000-0000-0000-000000000001',
   'PRO-2024-0001',
   '60000000-0000-0000-0000-000000000001',
   '40000000-0000-0000-0000-000000000001',
   '70000000-0000-0000-0000-000000000001',
   0, 500000, 5, 18500, 'Annually',
   4, '10000000-0000-0000-0000-000000000002',
   'Approved. Customer in good health.',
   NOW() - INTERVAL '60 days', NOW() - INTERVAL '55 days',
   false, NOW() - INTERVAL '62 days'),

  ('80000000-0000-0000-0000-000000000002',
   'PRO-2024-0002',
   '60000000-0000-0000-0000-000000000001',
   '40000000-0000-0000-0000-000000000001',
   '70000000-0000-0000-0000-000000000002',
   0, 800000, 1, 12000, 'Annually',
   4, '10000000-0000-0000-0000-000000000002',
   'Vehicle verified. IDV accepted.',
   NOW() - INTERVAL '30 days', NOW() - INTERVAL '28 days',
   false, NOW() - INTERVAL '32 days'),

  ('80000000-0000-0000-0000-000000000003',
   'PRO-2024-0003',
   '60000000-0000-0000-0000-000000000002',
   '40000000-0000-0000-0000-000000000001',
   '70000000-0000-0000-0000-000000000003',
   0, 5000000, 20, 22000, 'Annually',
   4, '10000000-0000-0000-0000-000000000002',
   'Medical reports satisfactory.',
   NOW() - INTERVAL '45 days', NOW() - INTERVAL '40 days',
   false, NOW() - INTERVAL '47 days'),

  ('80000000-0000-0000-0000-000000000004',
   'PRO-2024-0004',
   '60000000-0000-0000-0000-000000000003',
   '40000000-0000-0000-0000-000000000001',
   '70000000-0000-0000-0000-000000000001',
   0, 300000, 3, 11000, 'Annually',
   2, NULL, NULL,
   NOW() - INTERVAL '5 days', NULL,
   false, NOW() - INTERVAL '6 days');


-- ─────────────────────────────────────────────────────────────
-- 9. PROPOSAL DETAILS
-- ─────────────────────────────────────────────────────────────
INSERT INTO health_details (id, proposal_id, policy_id, pre_existing_conditions,
  network_hospital_coverage, tpa_name, room_rent_limit,
  maternity_covered, copay_percentage, created_at)
VALUES
  (gen_random_uuid(), '80000000-0000-0000-0000-000000000001', NULL,
   'None', 'PAN India 5000+ hospitals', 'Medi Assist', 5000, false, 10, NOW()),
  (gen_random_uuid(), '80000000-0000-0000-0000-000000000004', NULL,
   'Mild hypertension', 'PAN India 3000+ hospitals', 'Medi Assist', 3000, false, 20, NOW());

INSERT INTO motor_details (id, proposal_id, policy_id, vehicle_number, vehicle_make,
  vehicle_model, manufacture_year, vehicle_type, idv, engine_number,
  chassis_number, cover_type, created_at)
VALUES
  (gen_random_uuid(), '80000000-0000-0000-0000-000000000002', NULL,
   'MH01AB1234', 'Maruti Suzuki', 'Swift Dzire', 2021,
   'Sedan', 800000, 'K12N1234567', 'MA3FJEB1S00123456', 'Comprehensive', NOW());

INSERT INTO life_details (id, proposal_id, policy_id, policy_subtype, maturity_benefit,
  death_benefit, surrender_value_applicable, loan_eligible, created_at)
VALUES
  (gen_random_uuid(), '80000000-0000-0000-0000-000000000003', NULL,
   'Term', 0, 5000000, false, false, NOW());


-- ─────────────────────────────────────────────────────────────
-- 10. NOMINEES
-- ─────────────────────────────────────────────────────────────
INSERT INTO nominees (id, proposal_id, policy_id, full_name, relationship,
                      date_of_birth, share_percentage, is_minor, created_at)
VALUES
  (gen_random_uuid(), '80000000-0000-0000-0000-000000000001', NULL,
   'Sunita Sharma', 'Spouse', '1992-08-10', 100, false, NOW()),
  (gen_random_uuid(), '80000000-0000-0000-0000-000000000002', NULL,
   'Sunita Sharma', 'Spouse', '1992-08-10', 100, false, NOW()),
  (gen_random_uuid(), '80000000-0000-0000-0000-000000000003', NULL,
   'Ramesh Patel', 'Spouse', '1985-03-25', 100, false, NOW());


-- ─────────────────────────────────────────────────────────────
-- 11. POLICIES  (status: 1=Active  policy_type: 0=Individual)
-- ─────────────────────────────────────────────────────────────
INSERT INTO policies (id, policy_number, proposal_id, customer_id, product_id, agent_id,
  policy_type, sum_assured, premium_amount, payment_frequency,
  start_date, end_date, status, issued_at, is_deleted, created_at)
VALUES
  ('90000000-0000-0000-0000-000000000001',
   'POL-2024-0001',
   '80000000-0000-0000-0000-000000000001',
   '60000000-0000-0000-0000-000000000001',
   '70000000-0000-0000-0000-000000000001',
   '40000000-0000-0000-0000-000000000001',
   0, 500000, 18500, 'Annually',
   NOW() - INTERVAL '55 days', NOW() + INTERVAL '310 days',
   1, NOW() - INTERVAL '55 days', false, NOW() - INTERVAL '55 days'),

  ('90000000-0000-0000-0000-000000000002',
   'POL-2024-0002',
   '80000000-0000-0000-0000-000000000002',
   '60000000-0000-0000-0000-000000000001',
   '70000000-0000-0000-0000-000000000002',
   '40000000-0000-0000-0000-000000000001',
   0, 800000, 12000, 'Annually',
   NOW() - INTERVAL '28 days', NOW() + INTERVAL '337 days',
   1, NOW() - INTERVAL '28 days', false, NOW() - INTERVAL '28 days'),

  ('90000000-0000-0000-0000-000000000003',
   'POL-2024-0003',
   '80000000-0000-0000-0000-000000000003',
   '60000000-0000-0000-0000-000000000002',
   '70000000-0000-0000-0000-000000000003',
   '40000000-0000-0000-0000-000000000001',
   0, 5000000, 22000, 'Annually',
   NOW() - INTERVAL '40 days', NOW() + INTERVAL '7260 days',
   1, NOW() - INTERVAL '40 days', false, NOW() - INTERVAL '40 days');


-- ─────────────────────────────────────────────────────────────
-- 12. POLICY STATUS HISTORY
-- ─────────────────────────────────────────────────────────────
INSERT INTO policy_status_histories (id, policy_id, old_status, new_status,
  changed_by_id, reason, changed_at)
VALUES
  (gen_random_uuid(), '90000000-0000-0000-0000-000000000001',
   0, 1, '10000000-0000-0000-0000-000000000002',
   'Proposal approved and policy issued.', NOW() - INTERVAL '55 days'),
  (gen_random_uuid(), '90000000-0000-0000-0000-000000000002',
   0, 1, '10000000-0000-0000-0000-000000000002',
   'Proposal approved and policy issued.', NOW() - INTERVAL '28 days'),
  (gen_random_uuid(), '90000000-0000-0000-0000-000000000003',
   0, 1, '10000000-0000-0000-0000-000000000002',
   'Proposal approved and policy issued.', NOW() - INTERVAL '40 days');


-- ─────────────────────────────────────────────────────────────
-- 13. PREMIUM SCHEDULES & PAYMENTS
-- schedule_status: 0=Upcoming  2=Paid
-- payment_status:  0=Pending   1=Paid
-- payment_type:    0=FirstPremium
-- payment_method:  0=Card
-- ─────────────────────────────────────────────────────────────
INSERT INTO premium_schedules (id, policy_id, installment_number, amount,
  due_date, status, created_at)
VALUES
  ('b0000000-0000-0000-0000-000000000001',
   '90000000-0000-0000-0000-000000000001', 1, 18500,
   NOW() - INTERVAL '55 days', 2, NOW() - INTERVAL '55 days'),
  ('b0000000-0000-0000-0000-000000000002',
   '90000000-0000-0000-0000-000000000001', 2, 18500,
   NOW() + INTERVAL '310 days', 0, NOW() - INTERVAL '55 days'),

  ('b0000000-0000-0000-0000-000000000003',
   '90000000-0000-0000-0000-000000000002', 1, 12000,
   NOW() - INTERVAL '28 days', 2, NOW() - INTERVAL '28 days'),
  ('b0000000-0000-0000-0000-000000000004',
   '90000000-0000-0000-0000-000000000002', 2, 12000,
   NOW() + INTERVAL '337 days', 0, NOW() - INTERVAL '28 days'),

  ('b0000000-0000-0000-0000-000000000005',
   '90000000-0000-0000-0000-000000000003', 1, 22000,
   NOW() - INTERVAL '40 days', 2, NOW() - INTERVAL '40 days'),
  ('b0000000-0000-0000-0000-000000000006',
   '90000000-0000-0000-0000-000000000003', 2, 22000,
   NOW() + INTERVAL '325 days', 0, NOW() - INTERVAL '40 days');

INSERT INTO premium_payments (id, policy_id, schedule_id, customer_id, amount, currency,
  payment_type, status, stripe_payment_intent_id, stripe_charge_id,
  payment_method, paid_at, receipt_url, created_at)
VALUES
  ('c0000000-0000-0000-0000-000000000001',
   '90000000-0000-0000-0000-000000000001',
   'b0000000-0000-0000-0000-000000000001',
   '60000000-0000-0000-0000-000000000001',
   18500, 'INR', 0, 1,
   'pi_seed_health_001', 'ch_seed_health_001', 0,
   NOW() - INTERVAL '55 days',
   'https://receipts.stripe.com/seed_health_001',
   NOW() - INTERVAL '55 days'),

  ('c0000000-0000-0000-0000-000000000002',
   '90000000-0000-0000-0000-000000000002',
   'b0000000-0000-0000-0000-000000000003',
   '60000000-0000-0000-0000-000000000001',
   12000, 'INR', 0, 1,
   'pi_seed_motor_001', 'ch_seed_motor_001', 0,
   NOW() - INTERVAL '28 days',
   'https://receipts.stripe.com/seed_motor_001',
   NOW() - INTERVAL '28 days'),

  ('c0000000-0000-0000-0000-000000000003',
   '90000000-0000-0000-0000-000000000003',
   'b0000000-0000-0000-0000-000000000005',
   '60000000-0000-0000-0000-000000000002',
   22000, 'INR', 0, 1,
   'pi_seed_life_001', 'ch_seed_life_001', 0,
   NOW() - INTERVAL '40 days',
   'https://receipts.stripe.com/seed_life_001',
   NOW() - INTERVAL '40 days');

UPDATE premium_schedules SET payment_id = 'c0000000-0000-0000-0000-000000000001'
  WHERE id = 'b0000000-0000-0000-0000-000000000001';
UPDATE premium_schedules SET payment_id = 'c0000000-0000-0000-0000-000000000002'
  WHERE id = 'b0000000-0000-0000-0000-000000000003';
UPDATE premium_schedules SET payment_id = 'c0000000-0000-0000-0000-000000000003'
  WHERE id = 'b0000000-0000-0000-0000-000000000005';


-- ─────────────────────────────────────────────────────────────
-- 14. CLAIMS
-- claim_type: 0=Death  2=Health  3=Accident
-- claim_status: 0=Intimated  4=UnderReview  7=Settled
-- ─────────────────────────────────────────────────────────────
INSERT INTO claims (id, claim_number, policy_id, customer_id,
  claim_type, claim_amount_requested, claim_amount_approved, is_cashless,
  status, intimation_date, incident_date, incident_description,
  assigned_officer_id, surveyor_id, settlement_date, is_deleted, created_at)
VALUES
  ('a0000000-0000-0000-0000-000000000001',
   'CLM-2024-0001',
   '90000000-0000-0000-0000-000000000001',
   '60000000-0000-0000-0000-000000000001',
   2, 45000, 42000, true,
   7,
   NOW() - INTERVAL '40 days', NOW() - INTERVAL '42 days',
   'Hospitalised for 3 days due to viral fever and dehydration.',
   '10000000-0000-0000-0000-000000000003', NULL,
   NOW() - INTERVAL '30 days', false, NOW() - INTERVAL '40 days'),

  ('a0000000-0000-0000-0000-000000000002',
   'CLM-2024-0002',
   '90000000-0000-0000-0000-000000000002',
   '60000000-0000-0000-0000-000000000001',
   3, 95000, NULL, false,
   4,
   NOW() - INTERVAL '10 days', NOW() - INTERVAL '12 days',
   'Vehicle rear-ended at a traffic signal. Significant damage to boot and rear bumper.',
   '10000000-0000-0000-0000-000000000003',
   '50000000-0000-0000-0000-000000000001',
   NULL, false, NOW() - INTERVAL '10 days'),

  ('a0000000-0000-0000-0000-000000000003',
   'CLM-2024-0003',
   '90000000-0000-0000-0000-000000000003',
   '60000000-0000-0000-0000-000000000002',
   0, 5000000, NULL, false,
   0,
   NOW() - INTERVAL '2 days', NOW() - INTERVAL '3 days',
   'Policyholder diagnosed with critical illness. Death benefit claim filed by nominee.',
   NULL, NULL,
   NULL, false, NOW() - INTERVAL '2 days');


-- ─────────────────────────────────────────────────────────────
-- 15. CLAIM STATUS HISTORIES
-- ─────────────────────────────────────────────────────────────
INSERT INTO claim_status_histories (id, claim_id, old_status, new_status,
  changed_by_id, notes, changed_at)
VALUES
  (gen_random_uuid(), 'a0000000-0000-0000-0000-000000000001',
   0, 4, '10000000-0000-0000-0000-000000000003',
   'Documents verified. Moving to review.', NOW() - INTERVAL '38 days'),
  (gen_random_uuid(), 'a0000000-0000-0000-0000-000000000001',
   4, 5, '10000000-0000-0000-0000-000000000003',
   'Claim approved for Rs 42,000.', NOW() - INTERVAL '35 days'),
  (gen_random_uuid(), 'a0000000-0000-0000-0000-000000000001',
   5, 7, '10000000-0000-0000-0000-000000000004',
   'Payment processed.', NOW() - INTERVAL '30 days'),

  (gen_random_uuid(), 'a0000000-0000-0000-0000-000000000002',
   0, 4, '10000000-0000-0000-0000-000000000003',
   'Surveyor assigned. Inspection scheduled.', NOW() - INTERVAL '8 days'),

  (gen_random_uuid(), 'a0000000-0000-0000-0000-000000000003',
   0, 0, '20000000-0000-0000-0000-000000000002',
   'Claim intimated.', NOW() - INTERVAL '2 days');


-- ─────────────────────────────────────────────────────────────
-- 16. AGENT COMMISSIONS  (status is a string: PENDING/APPROVED/PAID)
-- ─────────────────────────────────────────────────────────────
INSERT INTO agent_commissions (id, agent_id, policy_id, premium_payment_id,
  commission_rate, commission_amount, status, created_at)
VALUES
  (gen_random_uuid(),
   '40000000-0000-0000-0000-000000000001',
   '90000000-0000-0000-0000-000000000001',
   'c0000000-0000-0000-0000-000000000001',
   5.00, 925, 'PAID', NOW() - INTERVAL '55 days'),

  (gen_random_uuid(),
   '40000000-0000-0000-0000-000000000001',
   '90000000-0000-0000-0000-000000000002',
   'c0000000-0000-0000-0000-000000000002',
   5.00, 600, 'PENDING', NOW() - INTERVAL '28 days'),

  (gen_random_uuid(),
   '40000000-0000-0000-0000-000000000001',
   '90000000-0000-0000-0000-000000000003',
   'c0000000-0000-0000-0000-000000000003',
   5.00, 1100, 'APPROVED', NOW() - INTERVAL '40 days');


-- ─────────────────────────────────────────────────────────────
-- 17. USER CONSENTS (DPDP Act — 2 per customer)
--     consent_type: 'DataProcessing' | 'KycDataCollection'
-- ─────────────────────────────────────────────────────────────
INSERT INTO user_consents (id, user_id, consent_type, is_granted, is_revoked,
  consent_version, ip_address, consented_at)
VALUES
  (gen_random_uuid(), '20000000-0000-0000-0000-000000000001',
   'DataProcessing',    true, false, '1.0', '127.0.0.1', NOW()),
  (gen_random_uuid(), '20000000-0000-0000-0000-000000000001',
   'KycDataCollection', true, false, '1.0', '127.0.0.1', NOW()),
  (gen_random_uuid(), '20000000-0000-0000-0000-000000000002',
   'DataProcessing',    true, false, '1.0', '127.0.0.1', NOW()),
  (gen_random_uuid(), '20000000-0000-0000-0000-000000000002',
   'KycDataCollection', true, false, '1.0', '127.0.0.1', NOW()),
  (gen_random_uuid(), '20000000-0000-0000-0000-000000000003',
   'DataProcessing',    true, false, '1.0', '127.0.0.1', NOW()),
  (gen_random_uuid(), '20000000-0000-0000-0000-000000000003',
   'KycDataCollection', true, false, '1.0', '127.0.0.1', NOW());


-- ─────────────────────────────────────────────────────────────
-- 18. SYSTEM CONFIG  (no created_at column in this model)
-- ─────────────────────────────────────────────────────────────
INSERT INTO system_configs (id, config_key, config_value, description)
VALUES
  (gen_random_uuid(), 'max_claim_amount',      '10000000', 'Maximum single claim settlement amount in INR'),
  (gen_random_uuid(), 'claim_sla_days',        '30',       'SLA days for claim resolution'),
  (gen_random_uuid(), 'kyc_review_sla_days',   '7',        'SLA days for KYC review'),
  (gen_random_uuid(), 'lockout_duration_mins', '15',       'Account lockout duration after failed login attempts');


-- ─────────────────────────────────────────────────────────────
-- 19. EMAIL TEMPLATES
-- ─────────────────────────────────────────────────────────────
INSERT INTO email_templates (id, template_key, subject, body_html, is_active, created_at)
VALUES
  (gen_random_uuid(), 'CLAIM_APPROVED',
   'Your Claim Has Been Approved — SpeedClaim',
   '<p>Dear {{Name}},</p><p>Your claim <strong>{{ClaimNumber}}</strong> has been approved for <strong>₹{{Amount}}</strong>. Settlement will be processed within 3 working days.</p>',
   true, NOW()),

  (gen_random_uuid(), 'CLAIM_REJECTED',
   'Update on Your Claim — SpeedClaim',
   '<p>Dear {{Name}},</p><p>We regret that your claim <strong>{{ClaimNumber}}</strong> has been rejected. Reason: {{Reason}}.</p>',
   true, NOW()),

  (gen_random_uuid(), 'POLICY_ISSUED',
   'Your Policy Is Now Active — SpeedClaim',
   '<p>Dear {{Name}},</p><p>Congratulations! Your policy <strong>{{PolicyNumber}}</strong> is now active. Sum assured: <strong>₹{{SumAssured}}</strong>.</p>',
   true, NOW()),

  (gen_random_uuid(), 'PROPOSAL_APPROVED',
   'Your Proposal Has Been Approved — SpeedClaim',
   '<p>Dear {{Name}},</p><p>Your proposal <strong>{{ProposalNumber}}</strong> has been approved. Please make your first premium payment to activate your policy.</p>',
   true, NOW());


COMMIT;

-- =============================================================
-- SEEDED ACCOUNTS SUMMARY
-- =============================================================
-- Role            Email                           Password
-- Admin           davish2204@gmail.com               Password@123  ← SMTP sender account
-- Underwriter     underwriter@speedclaim.com         Password@123  (internal, no real inbox needed)
-- Claims Officer  claimsofficer@speedclaim.com       Password@123  (internal)
-- Finance Officer financeofficer@speedclaim.com      Password@123  (internal)
-- Agent           davishthedelicious@gmail.com       Password@123  (gets proposal/commission emails)
-- Surveyor        surveyor@speedclaim.com            Password@123  (internal)
-- Customer 1      davish.cs22@bitsathy.ac.in         Password@123  (Health + Motor policies, 1 settled + 1 open claim)
-- Customer 2      davishoffl@gmail.com               Password@123  (Life policy, 1 new claim)
-- Customer 3      arjun.seed@speedclaim.internal     Password@123  (Proposal under review — demo data only)
--
-- davish.std@gmail.com → register manually via POST /api/v1/auth/register/customer
--                         to demo the email verification flow live
