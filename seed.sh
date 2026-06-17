#!/usr/bin/env bash
# ============================================================
# SpeedClaim Demo Seed Script
# Usage: bash seed.sh
# Prereqs: API running on :5062, curl, jq, psql, python3
# ============================================================
set -euo pipefail

BASE="http://localhost:5062/api/v1"
export PGPASSWORD=root
PGARGS="-h localhost -U postgres -d speedclaimTest"
DOCS="/Users/davishe/Documents/dummy-docs"
ENV_FILE="postman/SpeedClaim.postman_environment.json"
PRODUCT_ID="70000000-0000-0000-0000-000000000001"

GREEN='\033[0;32m'; YELLOW='\033[1;33m'; RED='\033[0;31m'; NC='\033[0m'
log()  { echo -e "${GREEN}[+]${NC} $1"; }
info() { echo -e "${YELLOW}[~]${NC} $1"; }
die()  { echo -e "${RED}[!] ERROR:${NC} $1" >&2; exit 1; }

# ─── helpers ────────────────────────────────────────────────
J()  { jq -r "$1" <<< "$2"; }

# Pass SQL via stdin so special chars ($, quotes) are not re-evaluated by shell
db() { echo "$1" | psql $PGARGS -t -A; }

login() {
  local email="$1" pass="$2"
  curl -sf -X POST "$BASE/auth/login" \
    -H "Content-Type: application/json" \
    -d "{\"email\":\"$email\",\"password\":\"$pass\"}"
}

token_of() { J '.accessToken' "$1"; }
uid_of()   { J '.user.id'     "$1"; }

set_verified() {
  db "UPDATE users SET is_email_verified = true WHERE email = '$1';"
}

get_uid() {
  local id
  id=$(db "SELECT id FROM users WHERE email = '$1'")
  [ -z "$id" ] && die "User '$1' not found in DB — registration may have failed before committing"
  echo "$id"
}

change_role() {
  local uid="$1" tok="$2" role="$3"
  curl -sf -X PUT "$BASE/users/$uid/role" \
    -H "Content-Type: application/json" \
    -H "Authorization: Bearer $tok" \
    -d "\"$role\"" > /dev/null
}

register_staff() {
  # args: firstName email phone dob gender salutation
  local fn="$1" email="$2" phone="$3" dob="$4" gender="$5" sal="$6"
  local http_code
  http_code=$(curl -s -o /dev/null -w "%{http_code}" -X POST "$BASE/auth/register" \
    -H "Content-Type: application/json" \
    -d "{
      \"email\": \"$email\",
      \"password\": \"Password@123\",
      \"salutation\": \"$sal\",
      \"firstName\": \"$fn\",
      \"lastName\": \"Seed\",
      \"phone\": \"$phone\",
      \"permanentAddress\": {
        \"line1\": \"123 Corporate Park\",
        \"line2\": null,
        \"city\": \"Mumbai\",
        \"state\": \"Maharashtra\",
        \"postalCode\": \"400001\",
        \"country\": \"India\"
      },
      \"currentAddress\": null,
      \"isSameAsPermanent\": true,
      \"dateOfBirth\": \"$dob\",
      \"aadhaarNumber\": \"111100000000\",
      \"panNumber\": \"STFFA0000A\",
      \"gender\": \"$gender\",
      \"maritalStatus\": \"Married\"
    }")
  [ "$http_code" = "200" ] || [ "$http_code" = "500" ] \
    || die "Staff registration for $email returned HTTP $http_code (expected 200 or 500 for SMTP-only failure)"
}

# ============================================================
# 0. Sanity checks
# ============================================================
info "Checking API health..."
curl -sf "$BASE/products" > /dev/null || die "API is not reachable at $BASE — start the server first"

# Wipe any previous seed data so the script is safely re-runnable
info "Clearing previous seed data (if any)..."
psql $PGARGS > /dev/null << 'CLEANSQL'
\set ON_ERROR_STOP on

-- Delete all non-admin seed data.
-- Admin is hardcoded ID 11111111-...; everyone else was created by this script.
-- Delete in reverse-dependency order so no FK RESTRICT blocks us.

-- Grievances reference customers, policies, and claims
DELETE FROM grievances
  WHERE customer_id IN (
    SELECT id FROM customers
    WHERE user_id != '11111111-1111-1111-1111-111111111111'
  );

-- Agent commissions reference policies + premium_payments (both RESTRICT)
DELETE FROM agent_commissions
  WHERE policy_id IN (
    SELECT id FROM policies
    WHERE customer_id IN (
      SELECT id FROM customers WHERE user_id != '11111111-1111-1111-1111-111111111111'
    )
  );

-- Submitted documents: polymorphic entity_id, no DB-level FK
DELETE FROM submitted_documents
  WHERE entity_id IN (
    SELECT id FROM claims
    WHERE customer_id IN (
      SELECT id FROM customers WHERE user_id != '11111111-1111-1111-1111-111111111111'
    )
  );
DELETE FROM submitted_documents
  WHERE entity_id IN (
    SELECT id FROM proposals
    WHERE customer_id IN (
      SELECT id FROM customers WHERE user_id != '11111111-1111-1111-1111-111111111111'
    )
  );

-- Claims: RESTRICT from policies; cascades claim_status_history + detail tables
DELETE FROM claims
  WHERE customer_id IN (
    SELECT id FROM customers WHERE user_id != '11111111-1111-1111-1111-111111111111'
  );

-- Policies: RESTRICT from proposals; cascades policy_members, policy_status_history, endorsements
DELETE FROM policies
  WHERE customer_id IN (
    SELECT id FROM customers WHERE user_id != '11111111-1111-1111-1111-111111111111'
  );

-- Proposals: RESTRICT from customers; cascades proposal_members, nominees,
-- health/life/motor details, premium_schedules, premium_payments
DELETE FROM proposals
  WHERE customer_id IN (
    SELECT id FROM customers WHERE user_id != '11111111-1111-1111-1111-111111111111'
  );

-- audit_logs has nullable FK to users — delete explicitly before deleting users
DELETE FROM audit_logs
  WHERE user_id != '11111111-1111-1111-1111-111111111111' AND user_id IS NOT NULL;

-- user_consents not configured for cascade — delete explicitly
DELETE FROM user_consents
  WHERE user_id != '11111111-1111-1111-1111-111111111111';

-- Delete all non-admin users; cascades: customers, agents, surveyors,
-- sessions, user_tokens, addresses, kyc_records, stripe_customers
DELETE FROM users WHERE id != '11111111-1111-1111-1111-111111111111';

-- All branches are seed branches (admin has no branch)
DELETE FROM branches;
CLEANSQL

# Ensure admin password is Password@123 (hash may differ on fresh migrations)
psql $PGARGS > /dev/null << 'ADMINSQL'
UPDATE users
  SET email                  = 'davish2204@gmail.com',
      password_hash          = '$2a$11$noTUuePjq5y/ldIskdG1JOVh7IxShG0RPMr3OEK8Mc6eXPKa3WTWK',
      failed_login_attempts  = 0,
      locked_until           = NULL
WHERE id = '11111111-1111-1111-1111-111111111111';
ADMINSQL
[ -f "$DOCS/aadhar-dummy.pdf" ]   || die "Missing: $DOCS/aadhar-dummy.pdf"
[ -f "$DOCS/pan-dummy.pdf" ]      || die "Missing: $DOCS/pan-dummy.pdf"
[ -f "$DOCS/dummy-document.pdf" ] || die "Missing: $DOCS/dummy-document.pdf"
[ -f "$ENV_FILE" ]                || die "Missing Postman env file: $ENV_FILE"
log "Preflight OK"

# Auth rate limiter: 10 req/60 s (fixed server-aligned window).
# Sleeping here ensures any leftover capacity from a previous run has expired
# so the script can always start auth calls in a clean window.
info "Waiting 62 s for auth rate-limit window to clear (safe for re-runs)..."
sleep 62

# ============================================================
# 1. Admin login
# ============================================================
log "Logging in as admin..."
ADMIN_RESP=$(login "davish2204@gmail.com" "Password@123")
ADMIN_TOKEN=$(token_of "$ADMIN_RESP")
ADMIN_USER_ID=$(uid_of "$ADMIN_RESP")
[ -z "$ADMIN_TOKEN" ] && die "Admin login failed — check credentials or DB seed"
log "Admin ID: $ADMIN_USER_ID"

# ============================================================
# 2. Staff users — register → verify → role
# ============================================================
info "Creating Underwriter (Anand)..."
register_staff "Anand"  "underwriter@speedclaim.com"    "9100000001" "1985-03-15" "Male"   "Mr"
set_verified "underwriter@speedclaim.com"
UW_USER_ID=$(get_uid "underwriter@speedclaim.com")
change_role "$UW_USER_ID" "$ADMIN_TOKEN" "Underwriter"
UW_RESP=$(login "underwriter@speedclaim.com" "Password@123")
UW_TOKEN=$(token_of "$UW_RESP")
log "Underwriter ID: $UW_USER_ID"

info "Creating ClaimsOfficer (Meera)..."
register_staff "Meera"  "claimsofficer@speedclaim.com"  "9100000002" "1988-07-22" "Female" "Ms"
set_verified "claimsofficer@speedclaim.com"
CO_USER_ID=$(get_uid "claimsofficer@speedclaim.com")
change_role "$CO_USER_ID" "$ADMIN_TOKEN" "ClaimsOfficer"
log "Claims Officer ID: $CO_USER_ID"

info "Creating FinanceOfficer (Suresh)..."
register_staff "Suresh" "financeofficer@speedclaim.com" "9100000003" "1980-11-10" "Male"   "Mr"
set_verified "financeofficer@speedclaim.com"
FO_USER_ID=$(get_uid "financeofficer@speedclaim.com")
change_role "$FO_USER_ID" "$ADMIN_TOKEN" "FinanceOfficer"
FO_RESP=$(login "financeofficer@speedclaim.com" "Password@123")
FO_TOKEN=$(token_of "$FO_RESP")
log "Finance Officer ID: $FO_USER_ID"

info "Creating Surveyor (Deepak)..."
register_staff "Deepak" "surveyor@speedclaim.com"       "9100000004" "1983-09-25" "Male"   "Mr"
set_verified "surveyor@speedclaim.com"
SV_USER_ID=$(get_uid "surveyor@speedclaim.com")
change_role "$SV_USER_ID" "$ADMIN_TOKEN" "Surveyor"
log "Surveyor user ID: $SV_USER_ID"

# ============================================================
# 3. Agent (admin endpoint — pre-verified, no email step)
# ============================================================
info "Creating Agent (Rajesh)..."
curl -sf -X POST "$BASE/auth/admin/register-agent" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -d '{
    "email": "davishthedelicious@gmail.com",
    "password": "Password@123",
    "salutation": "Mr",
    "firstName": "Rajesh",
    "lastName": "Kumar",
    "phone": "9100000005",
    "permanentAddress": {
      "line1": "456 Agent Lane",
      "line2": null,
      "city": "Pune",
      "state": "Maharashtra",
      "postalCode": "411001",
      "country": "India"
    },
    "currentAddress": null,
    "isSameAsPermanent": true,
    "licenseNumber": "LIC-AGENT-2024",
    "agencyName": "SpeedClaim Direct",
    "aadhaarNumber": "111100000005",
    "panNumber": "AGNET1234A",
    "maritalStatus": "Married"
  }' > /dev/null
AG_USER_ID=$(get_uid "davishthedelicious@gmail.com")
AG_RECORD_ID=$(db "SELECT id FROM agents WHERE user_id = '$AG_USER_ID'")
[ -z "$AG_RECORD_ID" ] && die "Agent record not found — registration likely failed"
log "Agent user ID: $AG_USER_ID | record ID: $AG_RECORD_ID"

# ============================================================
# 4. Branch + assign agent
# ============================================================
info "Creating branch..."
BRANCH_RESP=$(curl -sf -X POST "$BASE/agents/branches" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -d '{
    "name":    "Mumbai Central Branch",
    "city":    "Mumbai",
    "state":   "Maharashtra",
    "address": "Level 4, Bandra Kurla Complex, Mumbai",
    "phone":   "2261234567",
    "email":   "mumbai@speedclaim.in"
  }')
BRANCH_ID=$(J '.id' "$BRANCH_RESP")
[ -z "$BRANCH_ID" ] && die "Branch creation failed"
log "Branch ID: $BRANCH_ID"

info "Assigning agent to branch..."
curl -sf -X PUT "$BASE/agents/$AG_USER_ID/branch/$BRANCH_ID" \
  -H "Authorization: Bearer $ADMIN_TOKEN" > /dev/null

# ============================================================
# 5. Surveyor record (no API endpoint — insert directly)
# ============================================================
info "Creating surveyor DB record..."
SV_RECORD_ID=$(uuidgen | tr '[:upper:]' '[:lower:]')
db "INSERT INTO surveyors (id, user_id, surveyor_type, license_number, license_expiry, specialization, is_active, created_at)
    VALUES ('$SV_RECORD_ID', '$SV_USER_ID', 'External', 'SUR-LIC-2024', '2027-12-31', 'All', true, NOW())
    ON CONFLICT (user_id) DO UPDATE SET license_number = EXCLUDED.license_number;" > /dev/null
SV_RECORD_ID=$(db "SELECT id FROM surveyors WHERE user_id = '$SV_USER_ID'")
log "Surveyor record ID: $SV_RECORD_ID"

# ============================================================
# 6. Customers
# ============================================================
info "Registering Customer 1 (Rahul / davish.cs22)..."
C1_HTTP=$(curl -s -o /dev/null -w "%{http_code}" -X POST "$BASE/auth/register" \
  -H "Content-Type: application/json" \
  -d '{
    "email": "davish.cs22@bitsathy.ac.in",
    "password": "Password@123",
    "salutation": "Mr",
    "firstName": "Rahul",
    "lastName": "Sharma",
    "phone": "9200000001",
    "permanentAddress": {
      "line1": "12 Lotus Colony",
      "line2": "Andheri West",
      "city": "Mumbai",
      "state": "Maharashtra",
      "postalCode": "400053",
      "country": "India"
    },
    "currentAddress": null,
    "isSameAsPermanent": true,
    "dateOfBirth": "2000-01-15",
    "aadhaarNumber": "777788889999",
    "panNumber": "RAHUL1234A",
    "gender": "Male",
    "maritalStatus": "Single"
  }')
[ "$C1_HTTP" = "200" ] || [ "$C1_HTTP" = "500" ] || die "Customer 1 registration returned HTTP $C1_HTTP (expected 200 or 500 for SMTP-only failure)"
set_verified "davish.cs22@bitsathy.ac.in"
C1_RESP=$(login "davish.cs22@bitsathy.ac.in" "Password@123")
C1_TOKEN=$(token_of "$C1_RESP")
C1_USER_ID=$(uid_of "$C1_RESP")
C1_RECORD_ID=$(db "SELECT id FROM customers WHERE user_id = '$C1_USER_ID'")
[ -z "$C1_RECORD_ID" ] && die "Customer 1 record not found"
log "Customer 1 user ID: $C1_USER_ID | record ID: $C1_RECORD_ID"

# Auth rate limiter: 10 req/60 s. Wait for the fixed window to reset before
# starting Customer 2 (which needs 2 more auth calls).
info "Waiting 62 s for auth rate-limit window to reset..."
sleep 62

info "Registering Customer 2 (Priya Patel)..."
C2_HTTP=$(curl -s -o /dev/null -w "%{http_code}" -X POST "$BASE/auth/register" \
  -H "Content-Type: application/json" \
  -d '{
    "email": "priya.patel@example.com",
    "password": "Password@123",
    "salutation": "Ms",
    "firstName": "Priya",
    "lastName": "Patel",
    "phone": "9200000002",
    "permanentAddress": {
      "line1": "88 Green Park",
      "line2": null,
      "city": "Bengaluru",
      "state": "Karnataka",
      "postalCode": "560001",
      "country": "India"
    },
    "currentAddress": null,
    "isSameAsPermanent": true,
    "dateOfBirth": "1998-05-20",
    "aadhaarNumber": "111100000007",
    "panNumber": "PRIYA0001A",
    "gender": "Female",
    "maritalStatus": "Single"
  }')
[ "$C2_HTTP" = "200" ] || [ "$C2_HTTP" = "500" ] || die "Customer 2 registration returned HTTP $C2_HTTP (expected 200 or 500 for SMTP-only failure)"
set_verified "priya.patel@example.com"
C2_RESP=$(login "priya.patel@example.com" "Password@123")
C2_TOKEN=$(token_of "$C2_RESP")
C2_USER_ID=$(uid_of "$C2_RESP")
C2_RECORD_ID=$(db "SELECT id FROM customers WHERE user_id = '$C2_USER_ID'")
[ -z "$C2_RECORD_ID" ] && die "Customer 2 record not found"
log "Customer 2 user ID: $C2_USER_ID | record ID: $C2_RECORD_ID"

# ============================================================
# 7. KYC — Customer 1
# ============================================================
info "Uploading Aadhaar for Customer 1..."
curl -sf -X POST "$BASE/users/kyc/aadhaar" \
  -H "Authorization: Bearer $C1_TOKEN" \
  -F "aadhaarNumber=777788889999" \
  -F "frontDocument=@$DOCS/aadhar-dummy.pdf;type=application/pdf" > /dev/null

info "Uploading PAN for Customer 1..."
curl -sf -X POST "$BASE/users/kyc/pan" \
  -H "Authorization: Bearer $C1_TOKEN" \
  -F "panNumber=RAHUL1234A" \
  -F "frontDocument=@$DOCS/pan-dummy.pdf;type=application/pdf" > /dev/null

# ============================================================
# 8. KYC — Customer 2
# ============================================================
info "Uploading Aadhaar for Customer 2..."
curl -sf -X POST "$BASE/users/kyc/aadhaar" \
  -H "Authorization: Bearer $C2_TOKEN" \
  -F "aadhaarNumber=888899990001" \
  -F "frontDocument=@$DOCS/aadhar-dummy.pdf;type=application/pdf" > /dev/null

info "Uploading PAN for Customer 2..."
curl -sf -X POST "$BASE/users/kyc/pan" \
  -H "Authorization: Bearer $C2_TOKEN" \
  -F "panNumber=PRIYA1234A" \
  -F "frontDocument=@$DOCS/pan-dummy.pdf;type=application/pdf" > /dev/null

# ============================================================
# 9. Admin: approve KYC for both customers
# ============================================================
info "Approving KYC for Customer 1..."
curl -sf -X PUT "$BASE/users/$C1_USER_ID/kyc/review?isApproved=true&reason=Documents+verified+successfully" \
  -H "Authorization: Bearer $ADMIN_TOKEN" > /dev/null

info "Approving KYC for Customer 2..."
curl -sf -X PUT "$BASE/users/$C2_USER_ID/kyc/review?isApproved=true&reason=Documents+verified+successfully" \
  -H "Authorization: Bearer $ADMIN_TOKEN" > /dev/null

log "KYC approved for both customers"

# ============================================================
# 10. Submit proposals
# ============================================================
info "Submitting Proposal 1 (Customer 1)..."
PROP1_RESP=$(curl -sf -X POST "$BASE/proposals" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $C1_TOKEN" \
  -d "{
    \"customerId\":       \"$C1_RECORD_ID\",
    \"productId\":        \"$PRODUCT_ID\",
    \"sumAssured\":       200000,
    \"tenureYears\":      2,
    \"premiumAmount\":    8000,
    \"paymentFrequency\": \"Monthly\",
    \"healthDetail\": {
      \"preExistingConditions\":   \"None\",
      \"networkHospitalCoverage\": \"5000+ hospitals across India\",
      \"tpaName\":                 \"HealthAssist TPA\",
      \"roomRentLimit\":           5000,
      \"maternityCovered\":        false,
      \"copayPercentage\":         10
    },
    \"lifeDetail\":  null,
    \"motorDetail\": null,
    \"customerMemberIds\": [],
    \"nominees\": [{
      \"fullName\":         \"Ravi Sharma\",
      \"relationship\":     \"Father\",
      \"dateOfBirth\":      \"1965-06-01\",
      \"sharePercentage\":  100,
      \"isMinor\":          false,
      \"appointeeName\":    null
    }]
  }")
PROPOSAL_ID=$(J '.id' "$PROP1_RESP")
[ -z "$PROPOSAL_ID" ] && die "Proposal 1 submission failed: $PROP1_RESP"
log "Proposal 1 ID: $PROPOSAL_ID"

info "Submitting Proposal 2 (Customer 2 — stays under review)..."
PROP2_RESP=$(curl -sf -X POST "$BASE/proposals" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $C2_TOKEN" \
  -d "{
    \"customerId\":       \"$C2_RECORD_ID\",
    \"productId\":        \"$PRODUCT_ID\",
    \"sumAssured\":       300000,
    \"tenureYears\":      3,
    \"premiumAmount\":    8000,
    \"paymentFrequency\": \"Annually\",
    \"healthDetail\": {
      \"preExistingConditions\":   \"Type 2 Diabetes (controlled)\",
      \"networkHospitalCoverage\": \"5000+ hospitals across India\",
      \"tpaName\":                 \"HealthAssist TPA\",
      \"roomRentLimit\":           6000,
      \"maternityCovered\":        true,
      \"copayPercentage\":         15
    },
    \"lifeDetail\":  null,
    \"motorDetail\": null,
    \"customerMemberIds\": [],
    \"nominees\": [{
      \"fullName\":         \"Kiran Patel\",
      \"relationship\":     \"Mother\",
      \"dateOfBirth\":      \"1963-09-15\",
      \"sharePercentage\":  100,
      \"isMinor\":          false,
      \"appointeeName\":    null
    }]
  }")
PROPOSAL_ID_UNDER_REVIEW=$(J '.id' "$PROP2_RESP")
[ -z "$PROPOSAL_ID_UNDER_REVIEW" ] && die "Proposal 2 submission failed: $PROP2_RESP"
log "Proposal 2 ID (under review): $PROPOSAL_ID_UNDER_REVIEW"

# ============================================================
# 11. Underwriter: approve Proposal 1
# ============================================================
info "Approving Proposal 1..."
curl -sf -X POST "$BASE/proposals/$PROPOSAL_ID/review" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $UW_TOKEN" \
  -d '{"isApproved": true, "notes": "All documents verified. Risk profile acceptable. Policy approved for issuance."}' > /dev/null
log "Proposal 1 approved — policy created in Pending status"

# ============================================================
# 12. Get policy ID and schedule ID
# ============================================================
info "Fetching policies for Customer 1..."
POLICIES_RESP=$(curl -sf -X GET "$BASE/policies/my" \
  -H "Authorization: Bearer $C1_TOKEN")
POLICY_ID=$(J '.[0].id' "$POLICIES_RESP")
[ -z "$POLICY_ID" ] && die "Policy not found for Customer 1 after proposal approval"
log "Policy ID: $POLICY_ID"

info "Fetching premium schedule..."
SCHEDULE_RESP=$(curl -sf -X GET "$BASE/payments/schedule/$POLICY_ID" \
  -H "Authorization: Bearer $C1_TOKEN")
SCHEDULE_ID=$(J '.[0].id' "$SCHEDULE_RESP")
[ -z "$SCHEDULE_ID" ] && die "Premium schedule not found for policy $POLICY_ID"
log "Schedule ID: $SCHEDULE_ID"

# ============================================================
# 13. Payment — insert record directly, then reconcile via API
#     (avoids Stripe dependency; reconcile endpoint still exercised)
# ============================================================
info "Creating premium payment record in DB..."
PAYMENT_ID=$(uuidgen | tr '[:upper:]' '[:lower:]')
db "INSERT INTO premium_payments
      (id, proposal_id, policy_id, schedule_id, customer_id,
       amount, currency, payment_type, status,
       stripe_payment_intent_id, stripe_charge_id, payment_method,
       paid_at, receipt_url, created_at)
    SELECT
      '$PAYMENT_ID',
      pr.id,
      po.id,
      '$SCHEDULE_ID',
      '$C1_RECORD_ID',
      8000, 'INR', 'FirstPremium', 'Pending',
      'pi_seed_demo_001', '', 'Card',
      NULL, '', NOW()
    FROM policies po
    JOIN proposals pr ON po.proposal_id = pr.id
    WHERE po.id = '$POLICY_ID';" > /dev/null
log "Payment ID: $PAYMENT_ID"

info "Reconciling payment (activates policy)..."
curl -sf -X PUT "$BASE/payments/$PAYMENT_ID/reconcile" \
  -H "Authorization: Bearer $FO_TOKEN" > /dev/null
log "Policy activated"

# ============================================================
# 13b. Proposal 3 → Pending policy for Cancel Policy demo
# ============================================================
info "Submitting Proposal 3 (cancellable policy for Postman demo)..."
PROP3_RESP=$(curl -sf -X POST "$BASE/proposals" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $C1_TOKEN" \
  -d "{
    \"customerId\":       \"$C1_RECORD_ID\",
    \"productId\":        \"$PRODUCT_ID\",
    \"sumAssured\":       100000,
    \"tenureYears\":      1,
    \"premiumAmount\":    5000,
    \"paymentFrequency\": \"Annually\",
    \"healthDetail\": {
      \"preExistingConditions\":   \"None\",
      \"networkHospitalCoverage\": \"Pan India\",
      \"tpaName\":                 \"HealthAssist TPA\",
      \"roomRentLimit\":           3000,
      \"maternityCovered\":        false,
      \"copayPercentage\":         0
    },
    \"lifeDetail\":  null,
    \"motorDetail\": null,
    \"customerMemberIds\": [],
    \"nominees\": [{
      \"fullName\":         \"Ravi Sharma\",
      \"relationship\":     \"Father\",
      \"dateOfBirth\":      \"1965-06-01\",
      \"sharePercentage\":  100,
      \"isMinor\":          false,
      \"appointeeName\":    null
    }]
  }")
PROP3_ID=$(J '.id' "$PROP3_RESP")
[ -z "$PROP3_ID" ] && die "Proposal 3 submission failed: $PROP3_RESP"
log "Proposal 3 ID: $PROP3_ID"

info "Approving Proposal 3 (creates Pending policy for cancel demo)..."
curl -sf -X POST "$BASE/proposals/$PROP3_ID/review" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $UW_TOKEN" \
  -d '{"isApproved": true, "notes": "Approved for cancellation demo."}' > /dev/null

# Find the Pending policy (the one that is NOT Policy 1)
CANCEL_POLICY_ID=$(curl -sf -X GET "$BASE/policies/my" \
  -H "Authorization: Bearer $C1_TOKEN" | \
  jq -r "[.[] | select(.id != \"$POLICY_ID\")] | .[0].id // empty")
[ -z "$CANCEL_POLICY_ID" ] && die "Cancellable policy not found after Proposal 3 approval"
log "Cancellable Policy ID: $CANCEL_POLICY_ID"

# ============================================================
# 14. Intimate claim
# ============================================================
info "Intimating Health claim for Customer 1..."
INCIDENT_DATE=$(date -u -v-10d +%Y-%m-%dT%H:%M:%SZ 2>/dev/null \
  || date -u --date='10 days ago' +%Y-%m-%dT%H:%M:%SZ)

CLAIM_RESP=$(curl -sf -X POST "$BASE/claims/intimate" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $C1_TOKEN" \
  -d "{
    \"policyId\":               \"$POLICY_ID\",
    \"claimantMemberId\":       null,
    \"claimType\":              \"Health\",
    \"claimAmountRequested\":   50000,
    \"isCashless\":             false,
    \"incidentDate\":           \"$INCIDENT_DATE\",
    \"incidentDescription\":    \"Emergency hospitalisation for appendicitis. Requesting reimbursement for surgery and post-op care.\"
  }")
CLAIM_ID=$(J '.id' "$CLAIM_RESP")
[ -z "$CLAIM_ID" ] && die "Claim intimation failed: $CLAIM_RESP"
log "Claim ID: $CLAIM_ID"

# ============================================================
# 14b. Intimate Accident claim (surveyor demo in Postman folder 7)
# ============================================================
info "Intimating Accident claim for Customer 1 (surveyor demo)..."
MOTOR_CLAIM_RESP=$(curl -sf -X POST "$BASE/claims/intimate" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $C1_TOKEN" \
  -d "{
    \"policyId\":             \"$POLICY_ID\",
    \"claimantMemberId\":     null,
    \"claimType\":            \"Accident\",
    \"claimAmountRequested\": 85000,
    \"isCashless\":           false,
    \"incidentDate\":         \"$INCIDENT_DATE\",
    \"incidentDescription\":  \"Vehicle rear-ended by truck on highway. Bumper and boot panel damaged. Requesting repair reimbursement.\"
  }")
MOTOR_CLAIM_ID=$(J '.id' "$MOTOR_CLAIM_RESP")
[ -z "$MOTOR_CLAIM_ID" ] && die "Accident claim intimation failed: $MOTOR_CLAIM_RESP"
log "Motor/Accident Claim ID: $MOTOR_CLAIM_ID"

# ============================================================
# 15. Raise grievance
# ============================================================
info "Raising grievance for Customer 1..."
GRIEVANCE_RESP=$(curl -sf -X POST "$BASE/grievances" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $C1_TOKEN" \
  -d "{
    \"policyId\":    \"$POLICY_ID\",
    \"claimId\":     \"$CLAIM_ID\",
    \"category\":    \"ClaimDelay\",
    \"description\": \"Claim submitted over a week ago with no status update. Requesting expedited review of my hospitalisation claim.\"
  }")
GRIEVANCE_ID=$(J '.id' "$GRIEVANCE_RESP")
[ -z "$GRIEVANCE_ID" ] && die "Grievance creation failed: $GRIEVANCE_RESP"
log "Grievance ID: $GRIEVANCE_ID"

# ============================================================
# 16. Update Postman environment file
# ============================================================
info "Updating Postman environment..."
python3 - <<PYEOF
import json

with open("$ENV_FILE") as f:
    env = json.load(f)

updates = {
    "adminUserId":           "$ADMIN_USER_ID",
    "underwriterUserId":     "$UW_USER_ID",
    "claimsOfficerUserId":   "$CO_USER_ID",
    "financeOfficerUserId":  "$FO_USER_ID",
    "agentUserId":           "$AG_USER_ID",
    "surveyorUserId":        "$SV_USER_ID",
    "customerUserId":        "$C1_USER_ID",
    "customer2UserId":       "$C2_USER_ID",
    "targetUserId":          "$C2_USER_ID",
    "branchId":              "$BRANCH_ID",
    "agentId":               "$AG_RECORD_ID",
    "surveyorId":            "$SV_RECORD_ID",
    "customerRecordId":      "$C1_RECORD_ID",
    "customer2RecordId":     "$C2_RECORD_ID",
    "productId":             "$PRODUCT_ID",
    "proposalId":            "$PROPOSAL_ID",
    "proposalIdUnderReview": "$PROPOSAL_ID_UNDER_REVIEW",
    "policyId":              "$POLICY_ID",
    "claimId":               "$CLAIM_ID",
    "scheduleId":            "$SCHEDULE_ID",
    "paymentId":             "$PAYMENT_ID",
    "grievanceId":           "$GRIEVANCE_ID",
    "cancellablePolicyId":   "$CANCEL_POLICY_ID",
    "motorClaimId":          "$MOTOR_CLAIM_ID",
}

for v in env["values"]:
    if v["key"] in updates:
        v["value"] = updates[v["key"]]

with open("$ENV_FILE", "w") as f:
    json.dump(env, f, indent=2)

print("Postman environment updated.")
for k, val in updates.items():
    print(f"  {k:28s} = {val}")
PYEOF

# ============================================================
# Summary
# ============================================================
echo ""
log "Seed complete!"
echo ""
echo "  ┌─────────────────────────────────────────────────────────────┐"
echo "  │  Account credentials (all passwords: Password@123)          │"
echo "  ├─────────────────┬───────────────────────────────────────────┤"
echo "  │ Admin           │ davish2204@gmail.com                       │"
echo "  │ Underwriter     │ underwriter@speedclaim.com                │"
echo "  │ Claims Officer  │ claimsofficer@speedclaim.com              │"
echo "  │ Finance Officer │ financeofficer@speedclaim.com             │"
echo "  │ Surveyor        │ surveyor@speedclaim.com                   │"
echo "  │ Agent           │ davishthedelicious@gmail.com              │"
echo "  │ Customer 1      │ davish.cs22@bitsathy.ac.in                │"
echo "  │ Customer 2      │ priya.patel@example.com                   │"
echo "  └─────────────────┴───────────────────────────────────────────┘"
echo ""
echo "  Postman env updated: $ENV_FILE"
echo "  Load it in Postman to use real IDs for all requests."
