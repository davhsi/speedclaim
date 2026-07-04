#!/usr/bin/env bash
# ============================================================================
# SpeedClaim — Comprehensive Validation Scenario Test Runner (312 scenarios)
# ============================================================================
# Fires every known error path in the API and records responses.
# Output: scripts/validation-report.json
#
# Prerequisites:
#   1. API running at BASE_URL (default http://localhost:5062)
#   2. DB seeded with seed.sh data
#
# Usage:
#   chmod +x scripts/validation-scenarios.sh
#   ./scripts/validation-scenarios.sh
# ============================================================================

set -euo pipefail

BASE_URL="${BASE_URL:-http://localhost:5062}"
API="$BASE_URL/api/v1"
REPORT="scripts/validation-report.json"

# Credentials (from seed data)
ADMIN_EMAIL="davish2204@gmail.com"
CUSTOMER_EMAIL="davish.cs22@bitsathy.ac.in"
AGENT_EMAIL="davishthedelicious@gmail.com"
UNDERWRITER_EMAIL="underwriter@speedclaim.com"
CLAIMS_OFFICER_EMAIL="claimsofficer@speedclaim.com"
FINANCE_EMAIL="financeofficer@speedclaim.com"
SURVEYOR_EMAIL="surveyor@speedclaim.com"
PASSWORD="Password@123"

# Read IDs from Postman environment (written by seed.sh)
ENV_FILE="postman/SpeedClaim.postman_environment.json"
[[ -f "$ENV_FILE" ]] || { echo "ERROR: $ENV_FILE not found. Run seed.sh first." >&2; exit 1; }
penv() {
  local key="$1"
  python3 -c "import json,sys; env=json.load(open('$ENV_FILE')); print(next((v['value'] for v in env['values'] if v['key']=='$key'),''))"
}

CUSTOMER_USER_ID="$(penv customerUserId)"
POLICY_ID="$(penv policyId)"
CLAIM_ID="$(penv claimId)"
PROPOSAL_ID="$(penv proposalId)"
PRODUCT_ID="$(penv productId)"
SCHEDULE_ID="$(penv scheduleId)"
PAYMENT_ID="$(penv paymentId)"
GRIEVANCE_ID="$(penv grievanceId)"
MOTOR_CLAIM_ID="$(penv motorClaimId)"
CANCELLABLE_POLICY_ID="$(penv cancellablePolicyId)"
FAKE_GUID="00000000-0000-0000-0000-000000000000"
NONEXISTENT="aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"
TARGET_USER_ID="$(penv targetUserId)"
AGENT_USER_ID="$(penv agentUserId)"

# Category name constants (avoid duplicated string literals across scenarios)
CAT_AUTH_REGISTER="Auth/Register"
CAT_AUTH_LOGIN="Auth/Login"
CAT_AUTH_TOKEN="Auth/Token"
CAT_AUTH_AUTHZ="Auth/AuthZ"
CAT_AUTH_ADMIN="Auth/Admin"
CAT_USER_FAMILY="User/Family"
CAT_USER_ADDRESS="User/Address"
CAT_KYC_UPLOAD="KYC/Upload"
CAT_PRODUCT="Product"
CAT_PROPOSAL_QUOTE="Proposal/Quote"
CAT_PROPOSAL_SUBMIT="Proposal/Submit"
CAT_POLICY_NOMINEE="Policy/Nominee"
CAT_POLICY_ENDORSEMENT="Policy/Endorsement"
CAT_POLICY="Policy"
CAT_CLAIM_INTIMATE="Claim/Intimate"
CAT_CLAIM_STATUS="Claim/Status"
CAT_CLAIM_APPROVE="Claim/Approve"
CAT_GRIEVANCE="Grievance"
CAT_PAYMENT="Payment"
CAT_AGENT_BRANCH="Agent/Branch"
CAT_SYSTEM="System"
CAT_BIZLOGIC="BizLogic"
CAT_CROSSROLE="CrossRole"
CAT_FIELDEDGE="FieldEdge"
CAT_IDEMPOTENCY="Idempotency"

# Repeated form-field literal for empty-file upload scenarios
EMPTY_PDF_FORM_FIELD="frontDocument=@/dev/null;filename=empty.pdf;type=application/pdf"

# Repeated rate-limit wait banner
WAIT_MSG_GLOBAL_RATE_LIMIT="⏳ Waiting 62s for global rate-limit window to reset (100 req/60s)..."

# Tiny valid PDF for scenarios that must pass validation and reach the service layer
DUMMY_PDF=$(mktemp /tmp/speedclaim-dummy-XXXXXX.pdf)
printf '%%PDF-1.0\n1 0 obj<</Type/Catalog/Pages 2 0 R>>endobj\n%%%%EOF\n' > "$DUMMY_PDF"
trap 'rm -f "$DUMMY_PDF"' EXIT

# Long strings for boundary tests
LONG_101=$(python3 -c "print('A'*101)")
LONG_201=$(python3 -c "print('A'*201)")
LONG_501=$(python3 -c "print('A'*501)")
LONG_1001=$(python3 -c "print('A'*1001)")
LONG_2001=$(python3 -c "print('A'*2001)")

# ── Helpers ──────────────────────────────────────────────────────────────────

SCENARIOS="[]"
PASS=0
FAIL=0
TOTAL=0

login() {
  local email="$1"
  curl -s -X POST "$API/auth/login" \
    -H "Content-Type: application/json" \
    -d "{\"email\":\"$email\",\"password\":\"$PASSWORD\"}" \
    | python3 -c "import sys,json; print(json.load(sys.stdin).get('accessToken',''))" 2>/dev/null
}

record() {
  local scenario="$1"
  local category="$2"
  local expected_status="$3"
  local actual_status="$4"
  local response_body="$5"
  local method="$6"
  local url="$7"

  TOTAL=$((TOTAL + 1))
  local passed="true"
  if [[ "$actual_status" != "$expected_status" ]]; then
    passed="false"
    FAIL=$((FAIL + 1))
  else
    PASS=$((PASS + 1))
  fi

  SCENARIOS=$(
    SCENARIOS_JSON="$SCENARIOS" \
    RESPONSE_BODY="$response_body" \
    REQUEST_BODY="${body:-}" \
    ENTRY_ID="$TOTAL" \
    ENTRY_SCENARIO="$scenario" \
    ENTRY_CATEGORY="$category" \
    ENTRY_METHOD="$method" \
    ENTRY_URL="$url" \
    ENTRY_EXPECTED="$expected_status" \
    ENTRY_ACTUAL="$actual_status" \
    ENTRY_PASSED="$passed" \
    python3 << 'PYEOF'
import json, os

scenarios = json.loads(os.environ.get("SCENARIOS_JSON") or "[]")
response_text = os.environ.get("RESPONSE_BODY", "{}")
try:
    response_obj = json.loads(response_text) if response_text.strip() else {}
except:
    response_obj = {"raw": response_text}

request_text = os.environ.get("REQUEST_BODY", "{}")
try:
    request_obj = json.loads(request_text) if request_text.strip() else {}
except:
    request_obj = {"raw": request_text}

entry = {
    "id": int(os.environ["ENTRY_ID"]),
    "scenario": os.environ["ENTRY_SCENARIO"],
    "category": os.environ["ENTRY_CATEGORY"],
    "method": os.environ["ENTRY_METHOD"],
    "url": os.environ["ENTRY_URL"],
    "expectedStatus": int(os.environ["ENTRY_EXPECTED"]),
    "actualStatus": int(os.environ["ENTRY_ACTUAL"]),
    "passed": os.environ["ENTRY_PASSED"] == "true",
    "request": request_obj,
    "response": response_obj
}
scenarios.append(entry)
print(json.dumps(scenarios))
PYEOF
  )
}

fire() {
  local scenario="$1"
  local category="$2"
  local expected_status="$3"
  local method="$4"
  local url="$5"
  local token="${6:-}"
  local body="${7:-}"

  local curl_args=(-s -w "\n%{http_code}" -X "$method" "$url")
  [[ -n "$token" ]] && curl_args+=(-H "Authorization: Bearer $token")

  if [[ -n "$body" ]]; then
    curl_args+=(-H "Content-Type: application/json" -d "$body")
  fi

  local raw
  raw=$(curl "${curl_args[@]}" 2>/dev/null || echo -e "\n000")
  local status_code="${raw##*$'\n'}"
  local response_body="${raw%$'\n'*}"

  record "$scenario" "$category" "$expected_status" "$status_code" "$response_body" "$method" "$url"
  printf "  [%s] #%-3d %-4s %-60s expected=%s got=%s\n" \
    "$([[ "$status_code" = "$expected_status" ]] && echo "PASS" || echo "FAIL")" \
    "$TOTAL" "$method" "$scenario" "$expected_status" "$status_code"
}

fire_with_header() {
  local scenario="$1"
  local category="$2"
  local expected_status="$3"
  local method="$4"
  local url="$5"
  local token="${6:-}"
  local extra_header="${7:-}"
  local body="${8:-}"

  local curl_args=(-s -w "\n%{http_code}" -X "$method" "$url")
  [[ -n "$token" ]] && curl_args+=(-H "Authorization: Bearer $token")
  [[ -n "$extra_header" ]] && curl_args+=(-H "$extra_header")

  if [[ -n "$body" ]]; then
    curl_args+=(-H "Content-Type: application/json" -d "$body")
  fi

  local raw
  raw=$(curl "${curl_args[@]}" 2>/dev/null || echo -e "\n000")
  local status_code="${raw##*$'\n'}"
  local response_body="${raw%$'\n'*}"

  record "$scenario" "$category" "$expected_status" "$status_code" "$response_body" "$method" "$url"
  printf "  [%s] #%-3d %-4s %-60s expected=%s got=%s\n" \
    "$([[ "$status_code" = "$expected_status" ]] && echo "PASS" || echo "FAIL")" \
    "$TOTAL" "$method" "$scenario" "$expected_status" "$status_code"
}

fire_form() {
  local scenario="$1"
  local category="$2"
  local expected_status="$3"
  local method="$4"
  local url="$5"
  local token="${6:-}"
  shift 6
  local form_args=("$@")

  local curl_args=(-s -w "\n%{http_code}" -X "$method" "$url")
  [[ -n "$token" ]] && curl_args+=(-H "Authorization: Bearer $token")
  curl_args+=("${form_args[@]}")

  local raw
  raw=$(curl "${curl_args[@]}" 2>/dev/null || echo -e "\n000")
  local status_code="${raw##*$'\n'}"
  local response_body="${raw%$'\n'*}"

  record "$scenario" "$category" "$expected_status" "$status_code" "$response_body" "$method" "$url"
  printf "  [%s] #%-3d %-4s %-60s expected=%s got=%s\n" \
    "$([[ "$status_code" = "$expected_status" ]] && echo "PASS" || echo "FAIL")" \
    "$TOTAL" "$method" "$scenario" "$expected_status" "$status_code"
}

# Valid registration base template (used by many tests)
REG_BASE='{"email":"test@test.com","password":"Password@123","firstName":"Test","lastName":"User","salutation":"Mr","phone":"9876543210","dateOfBirth":"1995-01-01","aadhaarNumber":"123456789012","panNumber":"ABCDE1234F","gender":"Male","maritalStatus":"Single","permanentAddress":{"line1":"123 St","city":"Delhi","state":"Delhi","postalCode":"110001","country":"India"},"isSameAsPermanent":true}'

# ── Login to get tokens ─────────────────────────────────────────────────────

echo "🔐 Logging in as all roles..."
ADMIN_TOKEN=$(login "$ADMIN_EMAIL")
CUSTOMER_TOKEN=$(login "$CUSTOMER_EMAIL")
AGENT_TOKEN=$(login "$AGENT_EMAIL")
UNDERWRITER_TOKEN=$(login "$UNDERWRITER_EMAIL")
CLAIMS_TOKEN=$(login "$CLAIMS_OFFICER_EMAIL")
FINANCE_TOKEN=$(login "$FINANCE_EMAIL")
SURVEYOR_TOKEN=$(login "$SURVEYOR_EMAIL")
CUSTOMER2_EMAIL="priya.patel@example.com"
CUSTOMER2_TOKEN=$(login "$CUSTOMER2_EMAIL")

if [[ -z "$ADMIN_TOKEN" ]]; then
  echo "❌ Failed to get admin token. Is the API running at $BASE_URL?"
  exit 1
fi
echo "✅ All tokens acquired."
echo ""
echo "⏳ Waiting 62s for auth rate-limit window to reset (10 req/60s)..."
sleep 62

# ============================================================================
# 1. AUTH — REGISTRATION VALIDATION (Batch 1: 10 scenarios)
# ============================================================================
echo "━━━ 1a. AUTH — Registration Validation (Batch 1) ━━━"

fire "Register: empty body" \
  "$CAT_AUTH_REGISTER" 400 POST "$API/auth/register" "" "{}"

fire "Register: invalid email format" \
  "$CAT_AUTH_REGISTER" 400 POST "$API/auth/register" "" \
  '{"email":"not-an-email","password":"Password@123","firstName":"Test","lastName":"User","salutation":"Mr","phone":"9876543210","dateOfBirth":"1995-01-01","aadhaarNumber":"123456789012","panNumber":"ABCDE1234F","gender":"Male","maritalStatus":"Single","permanentAddress":{"line1":"123 St","city":"Delhi","state":"Delhi","postalCode":"110001","country":"India"},"isSameAsPermanent":true}'

fire "Register: weak password (no uppercase)" \
  "$CAT_AUTH_REGISTER" 400 POST "$API/auth/register" "" \
  '{"email":"test@test.com","password":"password@123","firstName":"Test","lastName":"User","salutation":"Mr","phone":"9876543210","dateOfBirth":"1995-01-01","aadhaarNumber":"123456789012","panNumber":"ABCDE1234F","gender":"Male","maritalStatus":"Single","permanentAddress":{"line1":"123 St","city":"Delhi","state":"Delhi","postalCode":"110001","country":"India"},"isSameAsPermanent":true}'

fire "Register: weak password (too short)" \
  "$CAT_AUTH_REGISTER" 400 POST "$API/auth/register" "" \
  '{"email":"test@test.com","password":"Pa@1","firstName":"Test","lastName":"User","salutation":"Mr","phone":"9876543210","dateOfBirth":"1995-01-01","aadhaarNumber":"123456789012","panNumber":"ABCDE1234F","gender":"Male","maritalStatus":"Single","permanentAddress":{"line1":"123 St","city":"Delhi","state":"Delhi","postalCode":"110001","country":"India"},"isSameAsPermanent":true}'

fire "Register: weak password (no special char)" \
  "$CAT_AUTH_REGISTER" 400 POST "$API/auth/register" "" \
  '{"email":"test@test.com","password":"Password123","firstName":"Test","lastName":"User","salutation":"Mr","phone":"9876543210","dateOfBirth":"1995-01-01","aadhaarNumber":"123456789012","panNumber":"ABCDE1234F","gender":"Male","maritalStatus":"Single","permanentAddress":{"line1":"123 St","city":"Delhi","state":"Delhi","postalCode":"110001","country":"India"},"isSameAsPermanent":true}'

fire "Register: weak password (no digit)" \
  "$CAT_AUTH_REGISTER" 400 POST "$API/auth/register" "" \
  '{"email":"test@test.com","password":"Password@abc","firstName":"Test","lastName":"User","salutation":"Mr","phone":"9876543210","dateOfBirth":"1995-01-01","aadhaarNumber":"123456789012","panNumber":"ABCDE1234F","gender":"Male","maritalStatus":"Single","permanentAddress":{"line1":"123 St","city":"Delhi","state":"Delhi","postalCode":"110001","country":"India"},"isSameAsPermanent":true}'

fire "Register: weak password (no lowercase)" \
  "$CAT_AUTH_REGISTER" 400 POST "$API/auth/register" "" \
  '{"email":"test@test.com","password":"PASSWORD@123","firstName":"Test","lastName":"User","salutation":"Mr","phone":"9876543210","dateOfBirth":"1995-01-01","aadhaarNumber":"123456789012","panNumber":"ABCDE1234F","gender":"Male","maritalStatus":"Single","permanentAddress":{"line1":"123 St","city":"Delhi","state":"Delhi","postalCode":"110001","country":"India"},"isSameAsPermanent":true}'

fire "Register: underage (< 18 years)" \
  "$CAT_AUTH_REGISTER" 400 POST "$API/auth/register" "" \
  '{"email":"test@test.com","password":"Password@123","firstName":"Test","lastName":"User","salutation":"Mr","phone":"9876543210","dateOfBirth":"2015-01-01","aadhaarNumber":"123456789012","panNumber":"ABCDE1234F","gender":"Male","maritalStatus":"Single","permanentAddress":{"line1":"123 St","city":"Delhi","state":"Delhi","postalCode":"110001","country":"India"},"isSameAsPermanent":true}'

fire "Register: invalid Aadhaar (not 12 digits)" \
  "$CAT_AUTH_REGISTER" 400 POST "$API/auth/register" "" \
  '{"email":"test@test.com","password":"Password@123","firstName":"Test","lastName":"User","salutation":"Mr","phone":"9876543210","dateOfBirth":"1995-01-01","aadhaarNumber":"12345","panNumber":"ABCDE1234F","gender":"Male","maritalStatus":"Single","permanentAddress":{"line1":"123 St","city":"Delhi","state":"Delhi","postalCode":"110001","country":"India"},"isSameAsPermanent":true}'

fire "Register: invalid PAN format" \
  "$CAT_AUTH_REGISTER" 400 POST "$API/auth/register" "" \
  '{"email":"test@test.com","password":"Password@123","firstName":"Test","lastName":"User","salutation":"Mr","phone":"9876543210","dateOfBirth":"1995-01-01","aadhaarNumber":"123456789012","panNumber":"invalid","gender":"Male","maritalStatus":"Single","permanentAddress":{"line1":"123 St","city":"Delhi","state":"Delhi","postalCode":"110001","country":"India"},"isSameAsPermanent":true}'

# Rate-limit test
fire "Register: rate limit triggered (429)" \
  "Auth/RateLimit" 429 POST "$API/auth/register" "" \
  '{"email":"ratelimit@test.com","password":"Password@123","firstName":"Rate","lastName":"Limit","salutation":"Mr","phone":"9876543210","dateOfBirth":"1995-01-01","aadhaarNumber":"123456789012","panNumber":"ABCDE1234F","gender":"Male","maritalStatus":"Single","permanentAddress":{"line1":"123 St","city":"Delhi","state":"Delhi","postalCode":"110001","country":"India"},"isSameAsPermanent":true}'

echo ""
echo "⏳ Waiting 62s for auth rate-limit window to reset (batch 2)..."
sleep 62

# ============================================================================
# 1b. AUTH — REGISTRATION VALIDATION (Batch 2: 10 scenarios)
# ============================================================================
echo "━━━ 1b. AUTH — Registration Validation (Batch 2) ━━━"

fire "Register: invalid salutation" \
  "$CAT_AUTH_REGISTER" 400 POST "$API/auth/register" "" \
  '{"email":"test@test.com","password":"Password@123","firstName":"Test","lastName":"User","salutation":"King","phone":"9876543210","dateOfBirth":"1995-01-01","aadhaarNumber":"123456789012","panNumber":"ABCDE1234F","gender":"Male","maritalStatus":"Single","permanentAddress":{"line1":"123 St","city":"Delhi","state":"Delhi","postalCode":"110001","country":"India"},"isSameAsPermanent":true}'

fire "Register: salutation-gender mismatch (Mrs with Male)" \
  "$CAT_AUTH_REGISTER" 400 POST "$API/auth/register" "" \
  '{"email":"test@test.com","password":"Password@123","firstName":"Test","lastName":"User","salutation":"Mrs","phone":"9876543210","dateOfBirth":"1995-01-01","aadhaarNumber":"123456789012","panNumber":"ABCDE1234F","gender":"Male","maritalStatus":"Single","permanentAddress":{"line1":"123 St","city":"Delhi","state":"Delhi","postalCode":"110001","country":"India"},"isSameAsPermanent":true}'

fire "Register: Mr salutation with Female gender" \
  "$CAT_AUTH_REGISTER" 400 POST "$API/auth/register" "" \
  '{"email":"test@test.com","password":"Password@123","firstName":"Test","lastName":"User","salutation":"Mr","phone":"9876543210","dateOfBirth":"1995-01-01","aadhaarNumber":"123456789012","panNumber":"ABCDE1234F","gender":"Female","maritalStatus":"Single","permanentAddress":{"line1":"123 St","city":"Delhi","state":"Delhi","postalCode":"110001","country":"India"},"isSameAsPermanent":true}'

fire "Register: Mrs requires Married status" \
  "$CAT_AUTH_REGISTER" 400 POST "$API/auth/register" "" \
  '{"email":"test@test.com","password":"Password@123","firstName":"Test","lastName":"User","salutation":"Mrs","phone":"9876543210","dateOfBirth":"1995-01-01","aadhaarNumber":"123456789012","panNumber":"ABCDE1234F","gender":"Female","maritalStatus":"Single","permanentAddress":{"line1":"123 St","city":"Delhi","state":"Delhi","postalCode":"110001","country":"India"},"isSameAsPermanent":true}'

fire "Register: missing current address when isSameAsPermanent=false" \
  "$CAT_AUTH_REGISTER" 400 POST "$API/auth/register" "" \
  '{"email":"test@test.com","password":"Password@123","firstName":"Test","lastName":"User","salutation":"Mr","phone":"9876543210","dateOfBirth":"1995-01-01","aadhaarNumber":"123456789012","panNumber":"ABCDE1234F","gender":"Male","maritalStatus":"Single","permanentAddress":{"line1":"123 St","city":"Delhi","state":"Delhi","postalCode":"110001","country":"India"},"isSameAsPermanent":false}'

fire "Register: invalid postal code (not 6 digits)" \
  "$CAT_AUTH_REGISTER" 400 POST "$API/auth/register" "" \
  '{"email":"test@test.com","password":"Password@123","firstName":"Test","lastName":"User","salutation":"Mr","phone":"9876543210","dateOfBirth":"1995-01-01","aadhaarNumber":"123456789012","panNumber":"ABCDE1234F","gender":"Male","maritalStatus":"Single","permanentAddress":{"line1":"123 St","city":"Delhi","state":"Delhi","postalCode":"123","country":"India"},"isSameAsPermanent":true}'

fire "Register: duplicate email (already exists)" \
  "$CAT_AUTH_REGISTER" 409 POST "$API/auth/register" "" \
  "{\"email\":\"$CUSTOMER_EMAIL\",\"password\":\"Password@123\",\"firstName\":\"Test\",\"lastName\":\"User\",\"salutation\":\"Mr\",\"phone\":\"9999999999\",\"dateOfBirth\":\"1995-01-01\",\"aadhaarNumber\":\"123456789012\",\"panNumber\":\"ABCDE1234F\",\"gender\":\"Male\",\"maritalStatus\":\"Single\",\"permanentAddress\":{\"line1\":\"123 St\",\"city\":\"Delhi\",\"state\":\"Delhi\",\"postalCode\":\"110001\",\"country\":\"India\"},\"isSameAsPermanent\":true}"

fire "Register: invalid gender enum" \
  "$CAT_AUTH_REGISTER" 400 POST "$API/auth/register" "" \
  '{"email":"test@test.com","password":"Password@123","firstName":"Test","lastName":"User","salutation":"Mr","phone":"9876543210","dateOfBirth":"1995-01-01","aadhaarNumber":"123456789012","panNumber":"ABCDE1234F","gender":"Alien","maritalStatus":"Single","permanentAddress":{"line1":"123 St","city":"Delhi","state":"Delhi","postalCode":"110001","country":"India"},"isSameAsPermanent":true}'

fire "Register: missing firstName" \
  "$CAT_AUTH_REGISTER" 400 POST "$API/auth/register" "" \
  '{"email":"test@test.com","password":"Password@123","firstName":"","lastName":"User","salutation":"Mr","phone":"9876543210","dateOfBirth":"1995-01-01","aadhaarNumber":"123456789012","panNumber":"ABCDE1234F","gender":"Male","maritalStatus":"Single","permanentAddress":{"line1":"123 St","city":"Delhi","state":"Delhi","postalCode":"110001","country":"India"},"isSameAsPermanent":true}'

fire "Register: missing lastName" \
  "$CAT_AUTH_REGISTER" 400 POST "$API/auth/register" "" \
  '{"email":"test@test.com","password":"Password@123","firstName":"Test","lastName":"","salutation":"Mr","phone":"9876543210","dateOfBirth":"1995-01-01","aadhaarNumber":"123456789012","panNumber":"ABCDE1234F","gender":"Male","maritalStatus":"Single","permanentAddress":{"line1":"123 St","city":"Delhi","state":"Delhi","postalCode":"110001","country":"India"},"isSameAsPermanent":true}'

echo ""
echo "⏳ Waiting 62s for auth rate-limit window to reset (batch 3)..."
sleep 62

# ============================================================================
# 1c. AUTH — REGISTRATION VALIDATION (Batch 3)
# ============================================================================
echo "━━━ 1c. AUTH — Registration Validation (Batch 3) ━━━"

fire "Register: firstName too long (>100 chars)" \
  "$CAT_AUTH_REGISTER" 400 POST "$API/auth/register" "" \
  "{\"email\":\"test@test.com\",\"password\":\"Password@123\",\"firstName\":\"$LONG_101\",\"lastName\":\"User\",\"salutation\":\"Mr\",\"phone\":\"9876543210\",\"dateOfBirth\":\"1995-01-01\",\"aadhaarNumber\":\"123456789012\",\"panNumber\":\"ABCDE1234F\",\"gender\":\"Male\",\"maritalStatus\":\"Single\",\"permanentAddress\":{\"line1\":\"123 St\",\"city\":\"Delhi\",\"state\":\"Delhi\",\"postalCode\":\"110001\",\"country\":\"India\"},\"isSameAsPermanent\":true}"

fire "Register: lastName too long (>100 chars)" \
  "$CAT_AUTH_REGISTER" 400 POST "$API/auth/register" "" \
  "{\"email\":\"test@test.com\",\"password\":\"Password@123\",\"firstName\":\"Test\",\"lastName\":\"$LONG_101\",\"salutation\":\"Mr\",\"phone\":\"9876543210\",\"dateOfBirth\":\"1995-01-01\",\"aadhaarNumber\":\"123456789012\",\"panNumber\":\"ABCDE1234F\",\"gender\":\"Male\",\"maritalStatus\":\"Single\",\"permanentAddress\":{\"line1\":\"123 St\",\"city\":\"Delhi\",\"state\":\"Delhi\",\"postalCode\":\"110001\",\"country\":\"India\"},\"isSameAsPermanent\":true}"

fire "Register: missing phone" \
  "$CAT_AUTH_REGISTER" 400 POST "$API/auth/register" "" \
  '{"email":"test@test.com","password":"Password@123","firstName":"Test","lastName":"User","salutation":"Mr","phone":"","dateOfBirth":"1995-01-01","aadhaarNumber":"123456789012","panNumber":"ABCDE1234F","gender":"Male","maritalStatus":"Single","permanentAddress":{"line1":"123 St","city":"Delhi","state":"Delhi","postalCode":"110001","country":"India"},"isSameAsPermanent":true}'

fire "Register: Aadhaar with alpha characters" \
  "$CAT_AUTH_REGISTER" 400 POST "$API/auth/register" "" \
  '{"email":"test@test.com","password":"Password@123","firstName":"Test","lastName":"User","salutation":"Mr","phone":"9876543210","dateOfBirth":"1995-01-01","aadhaarNumber":"12345ABC9012","panNumber":"ABCDE1234F","gender":"Male","maritalStatus":"Single","permanentAddress":{"line1":"123 St","city":"Delhi","state":"Delhi","postalCode":"110001","country":"India"},"isSameAsPermanent":true}'

fire "Register: PAN lowercase" \
  "$CAT_AUTH_REGISTER" 400 POST "$API/auth/register" "" \
  '{"email":"test@test.com","password":"Password@123","firstName":"Test","lastName":"User","salutation":"Mr","phone":"9876543210","dateOfBirth":"1995-01-01","aadhaarNumber":"123456789012","panNumber":"abcde1234f","gender":"Male","maritalStatus":"Single","permanentAddress":{"line1":"123 St","city":"Delhi","state":"Delhi","postalCode":"110001","country":"India"},"isSameAsPermanent":true}'

fire "Register: invalid marital status enum" \
  "$CAT_AUTH_REGISTER" 400 POST "$API/auth/register" "" \
  '{"email":"test@test.com","password":"Password@123","firstName":"Test","lastName":"User","salutation":"Mr","phone":"9876543210","dateOfBirth":"1995-01-01","aadhaarNumber":"123456789012","panNumber":"ABCDE1234F","gender":"Male","maritalStatus":"Complicated","permanentAddress":{"line1":"123 St","city":"Delhi","state":"Delhi","postalCode":"110001","country":"India"},"isSameAsPermanent":true}'

echo ""
echo "⏳ Waiting 62s for auth rate-limit window to reset..."
sleep 62

# ============================================================================
# 2. AUTH — LOGIN VALIDATION
# ============================================================================
echo "━━━ 2. AUTH — Login Validation ━━━"

fire "Login: empty body" \
  "$CAT_AUTH_LOGIN" 400 POST "$API/auth/login" "" "{}"

fire "Login: invalid email format" \
  "$CAT_AUTH_LOGIN" 400 POST "$API/auth/login" "" \
  '{"email":"not-an-email","password":"Password@123"}'

fire "Login: missing password" \
  "$CAT_AUTH_LOGIN" 400 POST "$API/auth/login" "" \
  '{"email":"test@test.com","password":""}'

fire "Login: wrong password (invalid credentials)" \
  "$CAT_AUTH_LOGIN" 400 POST "$API/auth/login" "" \
  "{\"email\":\"$CUSTOMER_EMAIL\",\"password\":\"WrongPassword@123\"}"

fire "Login: non-existent email" \
  "$CAT_AUTH_LOGIN" 400 POST "$API/auth/login" "" \
  '{"email":"nonexistent@speedclaim.com","password":"Password@123"}'

echo ""
echo "⏳ Waiting 62s for auth rate-limit window to reset..."
sleep 62

# ============================================================================
# 3. AUTH — TOKEN VALIDATION
# ============================================================================
echo "━━━ 3. AUTH — Token Validation ━━━"

fire "Verify Email: empty token" \
  "$CAT_AUTH_TOKEN" 400 POST "$API/auth/verify-email" "" \
  '{"token":""}'

fire "Verify Email: invalid/expired token" \
  "$CAT_AUTH_TOKEN" 400 POST "$API/auth/verify-email" "" \
  '{"token":"invalid-fake-token-12345"}'

fire "Refresh Token: empty" \
  "$CAT_AUTH_TOKEN" 400 POST "$API/auth/refresh" "" \
  '{"refreshToken":""}'

fire "Refresh Token: invalid format (no colon separator)" \
  "$CAT_AUTH_TOKEN" 400 POST "$API/auth/refresh" "" \
  '{"refreshToken":"not-a-valid-refresh-token"}'

fire "Forgot Password: empty email" \
  "$CAT_AUTH_TOKEN" 400 POST "$API/auth/forgot-password" "" \
  '{"email":""}'

fire "Forgot Password: invalid email format" \
  "$CAT_AUTH_TOKEN" 400 POST "$API/auth/forgot-password" "" \
  '{"email":"not-an-email"}'

fire "Reset Password: empty token" \
  "$CAT_AUTH_TOKEN" 400 POST "$API/auth/reset-password" "" \
  '{"token":"","newPassword":"Password@123"}'

fire "Reset Password: weak new password" \
  "$CAT_AUTH_TOKEN" 400 POST "$API/auth/reset-password" "" \
  '{"token":"some-token","newPassword":"weak"}'

fire "Reset Password: invalid token" \
  "$CAT_AUTH_TOKEN" 400 POST "$API/auth/reset-password" "" \
  '{"token":"invalid-token-value","newPassword":"StrongPass@123"}'

echo ""

# ============================================================================
# 4. AUTH — AUTHORIZATION (401/403) — expanded cross-role matrix
# ============================================================================
echo "━━━ 4. AUTH — Authorization (401/403) ━━━"

fire "No token: access protected endpoint" \
  "$CAT_AUTH_AUTHZ" 401 GET "$API/users/profile" ""

fire "Invalid JWT: garbled token" \
  "$CAT_AUTH_AUTHZ" 401 GET "$API/users/profile" "Bearer invalid.jwt.token"

fire "Customer accesses Admin endpoint (role mismatch)" \
  "$CAT_AUTH_AUTHZ" 403 GET "$API/users/all" "$CUSTOMER_TOKEN"

fire "Customer accesses Agent endpoint" \
  "$CAT_AUTH_AUTHZ" 403 GET "$API/agents/customers" "$CUSTOMER_TOKEN"

fire "Agent accesses Admin-only system configs" \
  "$CAT_AUTH_AUTHZ" 403 GET "$API/system/configs" "$AGENT_TOKEN"

fire "Customer accesses Underwriter proposals" \
  "$CAT_AUTH_AUTHZ" 403 GET "$API/proposals/all" "$CUSTOMER_TOKEN"

fire "Customer accesses ClaimsOfficer claims list" \
  "$CAT_AUTH_AUTHZ" 403 GET "$API/claims/all" "$CUSTOMER_TOKEN"

fire "Customer accesses Finance endpoints" \
  "$CAT_AUTH_AUTHZ" 403 GET "$API/payments/all-records" "$CUSTOMER_TOKEN"

fire "Agent accesses admin reset password" \
  "$CAT_AUTH_AUTHZ" 403 POST "$API/auth/admin/reset-password/$TARGET_USER_ID" "$AGENT_TOKEN" \
  '{"newPassword":"NewPass@123"}'

fire "Customer accesses admin register-agent" \
  "$CAT_AUTH_AUTHZ" 403 POST "$API/auth/admin/register-agent" "$CUSTOMER_TOKEN" \
  '{"email":"test@test.com","password":"Password@123","salutation":"Mr","firstName":"Test","lastName":"Agent","phone":"9876543210","licenseNumber":"LIC-001","agencyName":"Test","aadhaarNumber":"123456789012","panNumber":"ABCDE1234F","maritalStatus":"Single","permanentAddress":{"line1":"St","city":"City","state":"State","postalCode":"110001","country":"India"},"isSameAsPermanent":true}'

fire "Surveyor accesses Admin system configs" \
  "$CAT_AUTH_AUTHZ" 403 GET "$API/system/configs" "$SURVEYOR_TOKEN"

fire "FinanceOfficer accesses Underwriter proposals" \
  "$CAT_AUTH_AUTHZ" 403 GET "$API/proposals/all" "$FINANCE_TOKEN"

fire "ClaimsOfficer accesses Admin product create" \
  "$CAT_AUTH_AUTHZ" 403 POST "$API/products" "$CLAIMS_TOKEN" \
  '{"productName":"Hack","domain":"Health","uin":"UIN-H","description":"Test","minAge":18,"maxAge":65,"minSumAssured":100000,"maxSumAssured":5000000,"minTenureYears":1,"maxTenureYears":30,"waitingPeriodDays":30,"allowsFamilyFloater":false}'

fire "Underwriter accesses Agent customers" \
  "$CAT_AUTH_AUTHZ" 403 GET "$API/agents/customers" "$UNDERWRITER_TOKEN"

fire "Surveyor accesses Customer profile" \
  "$CAT_AUTH_AUTHZ" 403 GET "$API/users/profile" "$SURVEYOR_TOKEN"

fire "Agent accesses ClaimsOfficer claim assign" \
  "$CAT_AUTH_AUTHZ" 403 PUT "$API/claims/$CLAIM_ID/assign" "$AGENT_TOKEN"

fire "Customer accesses Surveyor assigned claims" \
  "$CAT_AUTH_AUTHZ" 403 GET "$API/claims/surveyor/assigned" "$CUSTOMER_TOKEN"

fire "FinanceOfficer accesses Admin system configs" \
  "$CAT_AUTH_AUTHZ" 403 GET "$API/system/configs" "$FINANCE_TOKEN"

echo ""

# ============================================================================
# 5. AUTH — ADMIN OPERATIONS
# ============================================================================
echo "━━━ 5. AUTH — Admin Operations ━━━"

fire "Admin Reset Password: user not found" \
  "$CAT_AUTH_ADMIN" 404 POST "$API/auth/admin/reset-password/$NONEXISTENT" "$ADMIN_TOKEN" \
  '{"newPassword":"NewPass@123"}'

fire "Admin Reset Password: weak password" \
  "$CAT_AUTH_ADMIN" 400 POST "$API/auth/admin/reset-password/$TARGET_USER_ID" "$ADMIN_TOKEN" \
  '{"newPassword":"weak"}'

fire "Register Agent: duplicate email" \
  "$CAT_AUTH_ADMIN" 409 POST "$API/auth/admin/register-agent" "$ADMIN_TOKEN" \
  "{\"email\":\"$AGENT_EMAIL\",\"password\":\"Password@123\",\"salutation\":\"Mr\",\"firstName\":\"Dup\",\"lastName\":\"Agent\",\"phone\":\"9999999998\",\"licenseNumber\":\"LIC-DUP-001\",\"agencyName\":\"Dup Agency\",\"aadhaarNumber\":\"123456789012\",\"panNumber\":\"ABCDE1234F\",\"maritalStatus\":\"Single\",\"permanentAddress\":{\"line1\":\"St\",\"city\":\"City\",\"state\":\"State\",\"postalCode\":\"110001\",\"country\":\"India\"},\"isSameAsPermanent\":true}"

fire "Register Agent: empty body" \
  "$CAT_AUTH_ADMIN" 400 POST "$API/auth/admin/register-agent" "$ADMIN_TOKEN" "{}"

fire "Register Agent: invalid phone (not 10 digits)" \
  "$CAT_AUTH_ADMIN" 400 POST "$API/auth/admin/register-agent" "$ADMIN_TOKEN" \
  '{"email":"newagent@test.com","password":"Password@123","salutation":"Mr","firstName":"New","lastName":"Agent","phone":"123","licenseNumber":"LIC-NEW-001","agencyName":"New Agency","aadhaarNumber":"123456789012","panNumber":"ABCDE1234F","maritalStatus":"Single","permanentAddress":{"line1":"St","city":"City","state":"State","postalCode":"110001","country":"India"},"isSameAsPermanent":true}'

fire "Register Agent: missing Aadhaar" \
  "$CAT_AUTH_ADMIN" 400 POST "$API/auth/admin/register-agent" "$ADMIN_TOKEN" \
  '{"email":"newagent2@test.com","password":"Password@123","salutation":"Mr","firstName":"New","lastName":"Agent","phone":"9876543210","licenseNumber":"LIC-002","agencyName":"Agency","aadhaarNumber":"","panNumber":"ABCDE1234F","maritalStatus":"Single","permanentAddress":{"line1":"St","city":"City","state":"State","postalCode":"110001","country":"India"},"isSameAsPermanent":true}'

fire "Register Agent: invalid Aadhaar format" \
  "$CAT_AUTH_ADMIN" 400 POST "$API/auth/admin/register-agent" "$ADMIN_TOKEN" \
  '{"email":"newagent3@test.com","password":"Password@123","salutation":"Mr","firstName":"New","lastName":"Agent","phone":"9876543210","licenseNumber":"LIC-003","agencyName":"Agency","aadhaarNumber":"1234ABCD9012","panNumber":"ABCDE1234F","maritalStatus":"Single","permanentAddress":{"line1":"St","city":"City","state":"State","postalCode":"110001","country":"India"},"isSameAsPermanent":true}'

fire "Register Agent: missing license number" \
  "$CAT_AUTH_ADMIN" 400 POST "$API/auth/admin/register-agent" "$ADMIN_TOKEN" \
  '{"email":"newagent4@test.com","password":"Password@123","salutation":"Mr","firstName":"New","lastName":"Agent","phone":"9876543210","licenseNumber":"","agencyName":"Agency","aadhaarNumber":"123456789012","panNumber":"ABCDE1234F","maritalStatus":"Single","permanentAddress":{"line1":"St","city":"City","state":"State","postalCode":"110001","country":"India"},"isSameAsPermanent":true}'

fire "Register Agent: missing agency name" \
  "$CAT_AUTH_ADMIN" 400 POST "$API/auth/admin/register-agent" "$ADMIN_TOKEN" \
  '{"email":"newagent5@test.com","password":"Password@123","salutation":"Mr","firstName":"New","lastName":"Agent","phone":"9876543210","licenseNumber":"LIC-005","agencyName":"","aadhaarNumber":"123456789012","panNumber":"ABCDE1234F","maritalStatus":"Single","permanentAddress":{"line1":"St","city":"City","state":"State","postalCode":"110001","country":"India"},"isSameAsPermanent":true}'

fire "Register Agent: invalid postal code" \
  "$CAT_AUTH_ADMIN" 400 POST "$API/auth/admin/register-agent" "$ADMIN_TOKEN" \
  '{"email":"newagent6@test.com","password":"Password@123","salutation":"Mr","firstName":"New","lastName":"Agent","phone":"9876543210","licenseNumber":"LIC-006","agencyName":"Agency","aadhaarNumber":"123456789012","panNumber":"ABCDE1234F","maritalStatus":"Single","permanentAddress":{"line1":"St","city":"City","state":"State","postalCode":"ABC","country":"India"},"isSameAsPermanent":true}'

echo ""

# ============================================================================
# 6. USER — PROFILE & KYC VALIDATION
# ============================================================================
echo "━━━ 6. USER — Profile & KYC ━━━"

fire "Get Profile: wrong role (Admin is not Customer)" \
  "User/Profile" 403 GET "$API/users/profile" "$ADMIN_TOKEN"

fire "KYC Approve: user not found" \
  "User/KYC" 404 PUT "$API/users/$NONEXISTENT/kyc/review?isApproved=true&reason=ok" "$ADMIN_TOKEN"

fire "Change Role: invalid role" \
  "User/Admin" 400 PUT "$API/users/$CUSTOMER_USER_ID/role" "$ADMIN_TOKEN" \
  '"SuperAdmin"'

fire "Change Role: user not found" \
  "User/Admin" 404 PUT "$API/users/$NONEXISTENT/role" "$ADMIN_TOKEN" \
  '"Customer"'

fire "Activate User: user not found" \
  "User/Admin" 404 PUT "$API/users/$NONEXISTENT/status?isActive=true" "$ADMIN_TOKEN"

echo ""

# ============================================================================
# 7. USER — FAMILY MEMBERS VALIDATION (expanded)
# ============================================================================
echo "━━━ 7. USER — Family Members ━━━"

fire "Add Family Member: empty body" \
  "$CAT_USER_FAMILY" 400 POST "$API/users/family" "$CUSTOMER_TOKEN" "{}"

fire "Add Family Member: invalid gender enum" \
  "$CAT_USER_FAMILY" 400 POST "$API/users/family" "$CUSTOMER_TOKEN" \
  '{"firstName":"Test","lastName":"Member","salutation":"Mr","gender":"Alien","relationship":"Spouse","dateOfBirth":"1995-01-01"}'

fire "Add Family Member: future DOB" \
  "$CAT_USER_FAMILY" 400 POST "$API/users/family" "$CUSTOMER_TOKEN" \
  '{"firstName":"Test","lastName":"Member","salutation":"Mr","gender":"Male","relationship":"Spouse","dateOfBirth":"2030-01-01"}'

fire "Add Family Member: missing firstName only" \
  "$CAT_USER_FAMILY" 400 POST "$API/users/family" "$CUSTOMER_TOKEN" \
  '{"firstName":"","lastName":"Member","salutation":"Mr","gender":"Male","relationship":"Spouse","dateOfBirth":"1995-01-01"}'

fire "Add Family Member: missing lastName only" \
  "$CAT_USER_FAMILY" 400 POST "$API/users/family" "$CUSTOMER_TOKEN" \
  '{"firstName":"Test","lastName":"","salutation":"Mr","gender":"Male","relationship":"Spouse","dateOfBirth":"1995-01-01"}'

fire "Add Family Member: invalid relationship enum" \
  "$CAT_USER_FAMILY" 400 POST "$API/users/family" "$CUSTOMER_TOKEN" \
  '{"firstName":"Test","lastName":"Member","salutation":"Mr","gender":"Male","relationship":"Cousin","dateOfBirth":"1995-01-01"}'

fire "Add Family Member: invalid salutation enum" \
  "$CAT_USER_FAMILY" 400 POST "$API/users/family" "$CUSTOMER_TOKEN" \
  '{"firstName":"Test","lastName":"Member","salutation":"King","gender":"Male","relationship":"Spouse","dateOfBirth":"1995-01-01"}'

fire "Add Family Member: firstName too long" \
  "$CAT_USER_FAMILY" 400 POST "$API/users/family" "$CUSTOMER_TOKEN" \
  "{\"firstName\":\"$LONG_101\",\"lastName\":\"Member\",\"salutation\":\"Mr\",\"gender\":\"Male\",\"relationship\":\"Spouse\",\"dateOfBirth\":\"1995-01-01\"}"

fire "Update Family Member: not found" \
  "$CAT_USER_FAMILY" 404 PUT "$API/users/family/$NONEXISTENT" "$CUSTOMER_TOKEN" \
  '{"firstName":"Test","lastName":"Member","salutation":"Mr","gender":"Male","relationship":"Spouse","dateOfBirth":"1995-01-01"}'

fire "Delete Family Member: not found" \
  "$CAT_USER_FAMILY" 404 DELETE "$API/users/family/$NONEXISTENT" "$CUSTOMER_TOKEN"

echo ""

# ============================================================================
# 8. USER — ADDRESS VALIDATION (expanded)
# ============================================================================
echo "━━━ 8. USER — Address ━━━"

fire "Add Address: empty body" \
  "$CAT_USER_ADDRESS" 400 POST "$API/users/addresses" "$CUSTOMER_TOKEN" "{}"

fire "Add Address: invalid postal code" \
  "$CAT_USER_ADDRESS" 400 POST "$API/users/addresses" "$CUSTOMER_TOKEN" \
  '{"addressType":"Permanent","addressLine1":"123 St","city":"Delhi","state":"Delhi","postalCode":"123","country":"India"}'

fire "Add Address: missing required fields" \
  "$CAT_USER_ADDRESS" 400 POST "$API/users/addresses" "$CUSTOMER_TOKEN" \
  '{"addressType":"Permanent","addressLine1":"","city":"","state":"","postalCode":"","country":""}'

fire "Add Address: missing addressLine1 only" \
  "$CAT_USER_ADDRESS" 400 POST "$API/users/addresses" "$CUSTOMER_TOKEN" \
  '{"addressType":"Permanent","addressLine1":"","city":"Delhi","state":"Delhi","postalCode":"110001","country":"India"}'

fire "Add Address: missing city only" \
  "$CAT_USER_ADDRESS" 400 POST "$API/users/addresses" "$CUSTOMER_TOKEN" \
  '{"addressType":"Permanent","addressLine1":"123 St","city":"","state":"Delhi","postalCode":"110001","country":"India"}'

fire "Add Address: missing state only" \
  "$CAT_USER_ADDRESS" 400 POST "$API/users/addresses" "$CUSTOMER_TOKEN" \
  '{"addressType":"Permanent","addressLine1":"123 St","city":"Delhi","state":"","postalCode":"110001","country":"India"}'

fire "Add Address: missing country only" \
  "$CAT_USER_ADDRESS" 400 POST "$API/users/addresses" "$CUSTOMER_TOKEN" \
  '{"addressType":"Permanent","addressLine1":"123 St","city":"Delhi","state":"Delhi","postalCode":"110001","country":""}'

fire "Add Address: addressLine1 too long (>200 chars)" \
  "$CAT_USER_ADDRESS" 400 POST "$API/users/addresses" "$CUSTOMER_TOKEN" \
  "{\"addressType\":\"Permanent\",\"addressLine1\":\"$LONG_201\",\"city\":\"Delhi\",\"state\":\"Delhi\",\"postalCode\":\"110001\",\"country\":\"India\"}"

fire "Update Address: not found" \
  "$CAT_USER_ADDRESS" 404 PUT "$API/users/addresses/$NONEXISTENT" "$CUSTOMER_TOKEN" \
  '{"addressType":"Permanent","addressLine1":"123 St","city":"Delhi","state":"Delhi","postalCode":"110001","country":"India"}'

fire "Delete Address: not found" \
  "$CAT_USER_ADDRESS" 404 DELETE "$API/users/addresses/$NONEXISTENT" "$CUSTOMER_TOKEN"

echo ""

# ============================================================================
# 9. KYC — DOCUMENT UPLOAD VALIDATION
# ============================================================================
echo "━━━ 9. KYC — Document Upload ━━━"

fire_form "Aadhaar Upload: invalid number (not 12 digits)" \
  "$CAT_KYC_UPLOAD" 400 POST "$API/users/kyc/aadhaar" "$CUSTOMER_TOKEN" \
  -F "aadhaarNumber=12345" \
  -F "$EMPTY_PDF_FORM_FIELD"

fire_form "PAN Upload: invalid format" \
  "$CAT_KYC_UPLOAD" 400 POST "$API/users/kyc/pan" "$CUSTOMER_TOKEN" \
  -F "panNumber=INVALID" \
  -F "$EMPTY_PDF_FORM_FIELD"

fire_form "Aadhaar Upload: number with alpha chars" \
  "$CAT_KYC_UPLOAD" 400 POST "$API/users/kyc/aadhaar" "$CUSTOMER_TOKEN" \
  -F "aadhaarNumber=12345ABC9012" \
  -F "$EMPTY_PDF_FORM_FIELD"

fire_form "PAN Upload: all digits (no alpha)" \
  "$CAT_KYC_UPLOAD" 400 POST "$API/users/kyc/pan" "$CUSTOMER_TOKEN" \
  -F "panNumber=1234567890" \
  -F "$EMPTY_PDF_FORM_FIELD"

fire_form "Aadhaar Upload: duplicate (already registered to another user)" \
  "$CAT_KYC_UPLOAD" 409 POST "$API/users/kyc/aadhaar" "$CUSTOMER2_TOKEN" \
  -F "aadhaarNumber=777788889999" \
  -F "frontDocument=@$DUMMY_PDF;type=application/pdf"

fire_form "PAN Upload: duplicate (already registered to another user)" \
  "$CAT_KYC_UPLOAD" 409 POST "$API/users/kyc/pan" "$CUSTOMER2_TOKEN" \
  -F "panNumber=RAHUL1234A" \
  -F "frontDocument=@$DUMMY_PDF;type=application/pdf"

echo ""

# ============================================================================
# 10. PRODUCT — VALIDATION (expanded)
# ============================================================================
echo "━━━ 10. PRODUCT — Validation ━━━"

fire "Get Product: not found" \
  "$CAT_PRODUCT" 404 GET "$API/products/$NONEXISTENT" "$ADMIN_TOKEN"

fire "Create Product: empty body" \
  "$CAT_PRODUCT" 400 POST "$API/products" "$ADMIN_TOKEN" "{}"

fire "Create Product: invalid domain" \
  "$CAT_PRODUCT" 400 POST "$API/products" "$ADMIN_TOKEN" \
  '{"productName":"Test","domain":"InvalidDomain","uin":"UIN-001","description":"A test product","minAge":18,"maxAge":65,"minSumAssured":100000,"maxSumAssured":5000000,"minTenureYears":1,"maxTenureYears":30,"waitingPeriodDays":30,"allowsFamilyFloater":false}'

fire "Create Product: maxAge < minAge" \
  "$CAT_PRODUCT" 400 POST "$API/products" "$ADMIN_TOKEN" \
  '{"productName":"Test","domain":"Health","uin":"UIN-001","description":"A test product","minAge":65,"maxAge":18,"minSumAssured":100000,"maxSumAssured":5000000,"minTenureYears":1,"maxTenureYears":30,"waitingPeriodDays":30,"allowsFamilyFloater":false}'

fire "Create Product: maxSumAssured < minSumAssured" \
  "$CAT_PRODUCT" 400 POST "$API/products" "$ADMIN_TOKEN" \
  '{"productName":"Test","domain":"Health","uin":"UIN-001","description":"A test product","minAge":18,"maxAge":65,"minSumAssured":5000000,"maxSumAssured":100000,"minTenureYears":1,"maxTenureYears":30,"waitingPeriodDays":30,"allowsFamilyFloater":false}'

fire "Create Product: family floater but maxFamilyMembers < 2" \
  "$CAT_PRODUCT" 400 POST "$API/products" "$ADMIN_TOKEN" \
  '{"productName":"Test","domain":"Health","uin":"UIN-002","description":"A test product","minAge":18,"maxAge":65,"minSumAssured":100000,"maxSumAssured":5000000,"minTenureYears":1,"maxTenureYears":30,"waitingPeriodDays":30,"allowsFamilyFloater":true,"maxFamilyMembers":1}'

fire "Create Product: missing product name" \
  "$CAT_PRODUCT" 400 POST "$API/products" "$ADMIN_TOKEN" \
  '{"productName":"","domain":"Health","uin":"UIN-003","description":"A test product","minAge":18,"maxAge":65,"minSumAssured":100000,"maxSumAssured":5000000,"minTenureYears":1,"maxTenureYears":30,"waitingPeriodDays":30,"allowsFamilyFloater":false}'

fire "Create Product: missing UIN" \
  "$CAT_PRODUCT" 400 POST "$API/products" "$ADMIN_TOKEN" \
  '{"productName":"TestProd","domain":"Health","uin":"","description":"A test product","minAge":18,"maxAge":65,"minSumAssured":100000,"maxSumAssured":5000000,"minTenureYears":1,"maxTenureYears":30,"waitingPeriodDays":30,"allowsFamilyFloater":false}'

fire "Create Product: missing description" \
  "$CAT_PRODUCT" 400 POST "$API/products" "$ADMIN_TOKEN" \
  '{"productName":"TestProd","domain":"Health","uin":"UIN-004","description":"","minAge":18,"maxAge":65,"minSumAssured":100000,"maxSumAssured":5000000,"minTenureYears":1,"maxTenureYears":30,"waitingPeriodDays":30,"allowsFamilyFloater":false}'

fire "Create Product: min age 0" \
  "$CAT_PRODUCT" 400 POST "$API/products" "$ADMIN_TOKEN" \
  '{"productName":"TestProd","domain":"Health","uin":"UIN-005","description":"Desc","minAge":0,"maxAge":65,"minSumAssured":100000,"maxSumAssured":5000000,"minTenureYears":1,"maxTenureYears":30,"waitingPeriodDays":30,"allowsFamilyFloater":false}'

fire "Create Product: negative min sum assured" \
  "$CAT_PRODUCT" 400 POST "$API/products" "$ADMIN_TOKEN" \
  '{"productName":"TestProd","domain":"Health","uin":"UIN-006","description":"Desc","minAge":18,"maxAge":65,"minSumAssured":-1,"maxSumAssured":5000000,"minTenureYears":1,"maxTenureYears":30,"waitingPeriodDays":30,"allowsFamilyFloater":false}'

fire "Create Product: negative waiting period" \
  "$CAT_PRODUCT" 400 POST "$API/products" "$ADMIN_TOKEN" \
  '{"productName":"TestProd","domain":"Health","uin":"UIN-007","description":"Desc","minAge":18,"maxAge":65,"minSumAssured":100000,"maxSumAssured":5000000,"minTenureYears":1,"maxTenureYears":30,"waitingPeriodDays":-1,"allowsFamilyFloater":false}'

fire "Create Product: max age > 100" \
  "$CAT_PRODUCT" 400 POST "$API/products" "$ADMIN_TOKEN" \
  '{"productName":"TestProd","domain":"Health","uin":"UIN-008","description":"Desc","minAge":18,"maxAge":150,"minSumAssured":100000,"maxSumAssured":5000000,"minTenureYears":1,"maxTenureYears":30,"waitingPeriodDays":30,"allowsFamilyFloater":false}'

fire "Update Rates: product not found" \
  "$CAT_PRODUCT" 404 PUT "$API/products/$NONEXISTENT/rates" "$ADMIN_TOKEN" \
  '{"rates":[]}'

fire "Toggle Status: product not found" \
  "$CAT_PRODUCT" 404 PUT "$API/products/$NONEXISTENT/status" "$ADMIN_TOKEN" "true"

fire "Customer creating product (forbidden)" \
  "$CAT_PRODUCT" 403 POST "$API/products" "$CUSTOMER_TOKEN" \
  '{"productName":"Hack","domain":"Health","uin":"UIN-H4CK","description":"Hacked product","minAge":18,"maxAge":65,"minSumAssured":100000,"maxSumAssured":5000000,"minTenureYears":1,"maxTenureYears":30,"waitingPeriodDays":30,"allowsFamilyFloater":false}'

echo ""

echo "$WAIT_MSG_GLOBAL_RATE_LIMIT"
sleep 62

# ============================================================================
# 11. PROPOSAL — VALIDATION (expanded)
# ============================================================================
echo "━━━ 11. PROPOSAL — Validation ━━━"

fire "Generate Quote: empty body" \
  "$CAT_PROPOSAL_QUOTE" 400 POST "$API/proposals/quote" "$CUSTOMER_TOKEN" "{}"

fire "Generate Quote: age out of range" \
  "$CAT_PROPOSAL_QUOTE" 400 POST "$API/proposals/quote" "$CUSTOMER_TOKEN" \
  "{\"productId\":\"$PRODUCT_ID\",\"age\":150,\"sumAssured\":500000,\"tenureYears\":10}"

fire "Generate Quote: zero sum assured" \
  "$CAT_PROPOSAL_QUOTE" 400 POST "$API/proposals/quote" "$CUSTOMER_TOKEN" \
  "{\"productId\":\"$PRODUCT_ID\",\"age\":30,\"sumAssured\":0,\"tenureYears\":10}"

fire "Generate Quote: product not found" \
  "$CAT_PROPOSAL_QUOTE" 404 POST "$API/proposals/quote" "$CUSTOMER_TOKEN" \
  "{\"productId\":\"$NONEXISTENT\",\"age\":30,\"sumAssured\":500000,\"tenureYears\":10}"

fire "Generate Quote: zero tenure" \
  "$CAT_PROPOSAL_QUOTE" 400 POST "$API/proposals/quote" "$CUSTOMER_TOKEN" \
  "{\"productId\":\"$PRODUCT_ID\",\"age\":30,\"sumAssured\":500000,\"tenureYears\":0}"

fire "Submit Proposal: empty body" \
  "$CAT_PROPOSAL_SUBMIT" 400 POST "$API/proposals" "$CUSTOMER_TOKEN" "{}"

fire "Submit Proposal: invalid customer ID format" \
  "$CAT_PROPOSAL_SUBMIT" 400 POST "$API/proposals" "$CUSTOMER_TOKEN" \
  '{"customerId":"not-a-guid","productId":"also-bad","sumAssured":500000,"tenureYears":10,"premiumAmount":5000,"paymentFrequency":"Monthly"}'

fire "Submit Proposal: invalid payment frequency" \
  "$CAT_PROPOSAL_SUBMIT" 400 POST "$API/proposals" "$CUSTOMER_TOKEN" \
  "{\"customerId\":\"$CUSTOMER_USER_ID\",\"productId\":\"$PRODUCT_ID\",\"sumAssured\":500000,\"tenureYears\":10,\"premiumAmount\":5000,\"paymentFrequency\":\"Weekly\"}"

fire "Submit Proposal: nominee shares don't add to 100%" \
  "$CAT_PROPOSAL_SUBMIT" 400 POST "$API/proposals" "$CUSTOMER_TOKEN" \
  "{\"customerId\":\"$CUSTOMER_USER_ID\",\"productId\":\"$PRODUCT_ID\",\"sumAssured\":500000,\"tenureYears\":10,\"premiumAmount\":5000,\"paymentFrequency\":\"Monthly\",\"nominees\":[{\"fullName\":\"Nom1\",\"relationship\":\"Spouse\",\"dateOfBirth\":\"1996-01-01\",\"sharePercentage\":30,\"isMinor\":false},{\"fullName\":\"Nom2\",\"relationship\":\"Child\",\"dateOfBirth\":\"2010-01-01\",\"sharePercentage\":30,\"isMinor\":true,\"appointeeName\":\"Guardian\"}]}"

fire "Submit Proposal: minor nominee without appointee" \
  "$CAT_PROPOSAL_SUBMIT" 400 POST "$API/proposals" "$CUSTOMER_TOKEN" \
  "{\"customerId\":\"$CUSTOMER_USER_ID\",\"productId\":\"$PRODUCT_ID\",\"sumAssured\":500000,\"tenureYears\":10,\"premiumAmount\":5000,\"paymentFrequency\":\"Monthly\",\"nominees\":[{\"fullName\":\"ChildNom\",\"relationship\":\"Child\",\"dateOfBirth\":\"2015-01-01\",\"sharePercentage\":100,\"isMinor\":true}]}"

fire "Submit Proposal: missing product ID" \
  "$CAT_PROPOSAL_SUBMIT" 400 POST "$API/proposals" "$CUSTOMER_TOKEN" \
  "{\"customerId\":\"$CUSTOMER_USER_ID\",\"productId\":\"\",\"sumAssured\":500000,\"tenureYears\":10,\"premiumAmount\":5000,\"paymentFrequency\":\"Monthly\"}"

fire "Submit Proposal: zero tenure" \
  "$CAT_PROPOSAL_SUBMIT" 400 POST "$API/proposals" "$CUSTOMER_TOKEN" \
  "{\"customerId\":\"$CUSTOMER_USER_ID\",\"productId\":\"$PRODUCT_ID\",\"sumAssured\":500000,\"tenureYears\":0,\"premiumAmount\":5000,\"paymentFrequency\":\"Monthly\"}"

fire "Submit Proposal: nominee future DOB" \
  "$CAT_PROPOSAL_SUBMIT" 400 POST "$API/proposals" "$CUSTOMER_TOKEN" \
  "{\"customerId\":\"$CUSTOMER_USER_ID\",\"productId\":\"$PRODUCT_ID\",\"sumAssured\":500000,\"tenureYears\":10,\"premiumAmount\":5000,\"paymentFrequency\":\"Monthly\",\"nominees\":[{\"fullName\":\"Future\",\"relationship\":\"Spouse\",\"dateOfBirth\":\"2030-01-01\",\"sharePercentage\":100,\"isMinor\":false}]}"

fire "Submit Proposal: nominee missing full name" \
  "$CAT_PROPOSAL_SUBMIT" 400 POST "$API/proposals" "$CUSTOMER_TOKEN" \
  "{\"customerId\":\"$CUSTOMER_USER_ID\",\"productId\":\"$PRODUCT_ID\",\"sumAssured\":500000,\"tenureYears\":10,\"premiumAmount\":5000,\"paymentFrequency\":\"Monthly\",\"nominees\":[{\"fullName\":\"\",\"relationship\":\"Spouse\",\"dateOfBirth\":\"1996-01-01\",\"sharePercentage\":100,\"isMinor\":false}]}"

fire "Submit Proposal: nominee share > 100%" \
  "$CAT_PROPOSAL_SUBMIT" 400 POST "$API/proposals" "$CUSTOMER_TOKEN" \
  "{\"customerId\":\"$CUSTOMER_USER_ID\",\"productId\":\"$PRODUCT_ID\",\"sumAssured\":500000,\"tenureYears\":10,\"premiumAmount\":5000,\"paymentFrequency\":\"Monthly\",\"nominees\":[{\"fullName\":\"Over\",\"relationship\":\"Spouse\",\"dateOfBirth\":\"1996-01-01\",\"sharePercentage\":150,\"isMinor\":false}]}"

fire "Submit Proposal: motor detail missing vehicle number" \
  "$CAT_PROPOSAL_SUBMIT" 400 POST "$API/proposals" "$CUSTOMER_TOKEN" \
  "{\"customerId\":\"$CUSTOMER_USER_ID\",\"productId\":\"$PRODUCT_ID\",\"sumAssured\":500000,\"tenureYears\":10,\"premiumAmount\":5000,\"paymentFrequency\":\"Monthly\",\"motorDetail\":{\"vehicleNumber\":\"\",\"vehicleMake\":\"Toyota\",\"vehicleModel\":\"Camry\",\"manufactureYear\":2020,\"engineNumber\":\"ENG123\",\"chassisNumber\":\"CHS123\",\"idv\":500000}}"

fire "Submit Proposal: motor detail IDV zero" \
  "$CAT_PROPOSAL_SUBMIT" 400 POST "$API/proposals" "$CUSTOMER_TOKEN" \
  "{\"customerId\":\"$CUSTOMER_USER_ID\",\"productId\":\"$PRODUCT_ID\",\"sumAssured\":500000,\"tenureYears\":10,\"premiumAmount\":5000,\"paymentFrequency\":\"Monthly\",\"motorDetail\":{\"vehicleNumber\":\"KA01AB1234\",\"vehicleMake\":\"Toyota\",\"vehicleModel\":\"Camry\",\"manufactureYear\":2020,\"engineNumber\":\"ENG123\",\"chassisNumber\":\"CHS123\",\"idv\":0}}"

fire "Submit Proposal: motor detail future manufacture year" \
  "$CAT_PROPOSAL_SUBMIT" 400 POST "$API/proposals" "$CUSTOMER_TOKEN" \
  "{\"customerId\":\"$CUSTOMER_USER_ID\",\"productId\":\"$PRODUCT_ID\",\"sumAssured\":500000,\"tenureYears\":10,\"premiumAmount\":5000,\"paymentFrequency\":\"Monthly\",\"motorDetail\":{\"vehicleNumber\":\"KA01AB1234\",\"vehicleMake\":\"Toyota\",\"vehicleModel\":\"Camry\",\"manufactureYear\":2035,\"engineNumber\":\"ENG123\",\"chassisNumber\":\"CHS123\",\"idv\":500000}}"

fire "Submit Proposal: motor detail missing engine number" \
  "$CAT_PROPOSAL_SUBMIT" 400 POST "$API/proposals" "$CUSTOMER_TOKEN" \
  "{\"customerId\":\"$CUSTOMER_USER_ID\",\"productId\":\"$PRODUCT_ID\",\"sumAssured\":500000,\"tenureYears\":10,\"premiumAmount\":5000,\"paymentFrequency\":\"Monthly\",\"motorDetail\":{\"vehicleNumber\":\"KA01AB1234\",\"vehicleMake\":\"Toyota\",\"vehicleModel\":\"Camry\",\"manufactureYear\":2020,\"engineNumber\":\"\",\"chassisNumber\":\"CHS123\",\"idv\":500000}}"

fire "Submit Proposal: motor detail missing chassis number" \
  "$CAT_PROPOSAL_SUBMIT" 400 POST "$API/proposals" "$CUSTOMER_TOKEN" \
  "{\"customerId\":\"$CUSTOMER_USER_ID\",\"productId\":\"$PRODUCT_ID\",\"sumAssured\":500000,\"tenureYears\":10,\"premiumAmount\":5000,\"paymentFrequency\":\"Monthly\",\"motorDetail\":{\"vehicleNumber\":\"KA01AB1234\",\"vehicleMake\":\"Toyota\",\"vehicleModel\":\"Camry\",\"manufactureYear\":2020,\"engineNumber\":\"ENG123\",\"chassisNumber\":\"\",\"idv\":500000}}"

fire "Get Proposal: not found" \
  "Proposal" 404 GET "$API/proposals/$NONEXISTENT" "$CUSTOMER_TOKEN"

fire "Customer accessing other's proposal (forbidden)" \
  "Proposal" 403 GET "$API/proposals/$PROPOSAL_ID" "$FINANCE_TOKEN"

echo ""

# ============================================================================
# 12. POLICY — VALIDATION (expanded)
# ============================================================================
echo "━━━ 12. POLICY — Validation ━━━"

fire "Get Policy: not found" \
  "$CAT_POLICY" 404 GET "$API/policies/$NONEXISTENT" "$CUSTOMER_TOKEN"

fire "Cancel Policy: not found" \
  "$CAT_POLICY" 404 PUT "$API/policies/$NONEXISTENT/cancel" "$CUSTOMER_TOKEN"

fire "Request Endorsement: empty body" \
  "$CAT_POLICY_ENDORSEMENT" 400 POST "$API/policies/$POLICY_ID/endorsements" "$CUSTOMER_TOKEN" "{}"

fire "Request Endorsement: description too short" \
  "$CAT_POLICY_ENDORSEMENT" 400 POST "$API/policies/$POLICY_ID/endorsements" "$CUSTOMER_TOKEN" \
  '{"endorsementType":"AddressChange","description":"Short"}'

fire "Request Endorsement: policy not found" \
  "$CAT_POLICY_ENDORSEMENT" 404 POST "$API/policies/$NONEXISTENT/endorsements" "$CUSTOMER_TOKEN" \
  '{"endorsementType":"AddressChange","description":"Please update my address to the new residential address in Mumbai"}'

fire "Request Endorsement: invalid type enum" \
  "$CAT_POLICY_ENDORSEMENT" 400 POST "$API/policies/$POLICY_ID/endorsements" "$CUSTOMER_TOKEN" \
  '{"endorsementType":"InvalidType","description":"Please update my address to the new residential address in Mumbai"}'

fire "Request Endorsement: description too long (>1000 chars)" \
  "$CAT_POLICY_ENDORSEMENT" 400 POST "$API/policies/$POLICY_ID/endorsements" "$CUSTOMER_TOKEN" \
  "{\"endorsementType\":\"AddressChange\",\"description\":\"$LONG_1001\"}"

fire "Approve Endorsement: reject without reason" \
  "$CAT_POLICY_ENDORSEMENT" 400 PUT "$API/policies/endorsements/$NONEXISTENT/review" "$UNDERWRITER_TOKEN" \
  '{"isApproved":false,"reason":""}'

fire "Get Policy Endorsements: access denied (not owner)" \
  "$CAT_POLICY_ENDORSEMENT" 403 GET "$API/policies/$FAKE_GUID/endorsements" "$CUSTOMER_TOKEN"

fire "Get Nominees: access denied (not owner)" \
  "$CAT_POLICY_NOMINEE" 403 GET "$API/policies/$FAKE_GUID/nominees" "$CUSTOMER_TOKEN"

fire "Update Nominee: not found" \
  "$CAT_POLICY_NOMINEE" 404 PUT "$API/policies/nominees/$NONEXISTENT" "$CUSTOMER_TOKEN" \
  '{"fullName":"Test","relationship":"Spouse","dateOfBirth":"1996-01-01","sharePercentage":100,"isMinor":false}'

fire "Update Nominee: share out of range" \
  "$CAT_POLICY_NOMINEE" 400 PUT "$API/policies/nominees/$NONEXISTENT" "$CUSTOMER_TOKEN" \
  '{"fullName":"Test","relationship":"Spouse","dateOfBirth":"1996-01-01","sharePercentage":150,"isMinor":false}'

fire "Update Nominee: minor without appointee" \
  "$CAT_POLICY_NOMINEE" 400 PUT "$API/policies/nominees/$NONEXISTENT" "$CUSTOMER_TOKEN" \
  '{"fullName":"ChildNom","relationship":"Child","dateOfBirth":"2015-01-01","sharePercentage":100,"isMinor":true}'

fire "Update Nominee: future DOB" \
  "$CAT_POLICY_NOMINEE" 400 PUT "$API/policies/nominees/$NONEXISTENT" "$CUSTOMER_TOKEN" \
  '{"fullName":"Test","relationship":"Spouse","dateOfBirth":"2030-01-01","sharePercentage":100,"isMinor":false}'

fire "Update Nominee: missing full name" \
  "$CAT_POLICY_NOMINEE" 400 PUT "$API/policies/nominees/$NONEXISTENT" "$CUSTOMER_TOKEN" \
  '{"fullName":"","relationship":"Spouse","dateOfBirth":"1996-01-01","sharePercentage":100,"isMinor":false}'

fire "Update Nominee: missing relationship" \
  "$CAT_POLICY_NOMINEE" 400 PUT "$API/policies/nominees/$NONEXISTENT" "$CUSTOMER_TOKEN" \
  '{"fullName":"Test","relationship":"","dateOfBirth":"1996-01-01","sharePercentage":100,"isMinor":false}'

fire "Download Policy: not found" \
  "$CAT_POLICY" 404 GET "$API/policies/$NONEXISTENT/download" "$CUSTOMER_TOKEN"

fire "Approve Endorsement: not found" \
  "$CAT_POLICY_ENDORSEMENT" 404 PUT "$API/policies/endorsements/$NONEXISTENT/review" "$UNDERWRITER_TOKEN" \
  '{"isApproved":true,"reason":"Approved"}'

fire "Policy History: access denied (not owner)" \
  "$CAT_POLICY" 403 GET "$API/policies/$NONEXISTENT/history" "$CUSTOMER_TOKEN"

echo ""

echo "$WAIT_MSG_GLOBAL_RATE_LIMIT"
sleep 62

# ============================================================================
# 13. CLAIMS — VALIDATION (expanded)
# ============================================================================
echo "━━━ 13. CLAIMS — Validation ━━━"

fire "Intimate Claim: empty body" \
  "$CAT_CLAIM_INTIMATE" 400 POST "$API/claims/intimate" "$CUSTOMER_TOKEN" "{}"

fire "Intimate Claim: zero amount" \
  "$CAT_CLAIM_INTIMATE" 400 POST "$API/claims/intimate" "$CUSTOMER_TOKEN" \
  "{\"policyId\":\"$POLICY_ID\",\"claimType\":2,\"claimAmountRequested\":0,\"incidentDate\":\"2025-01-01T10:00:00Z\",\"incidentDescription\":\"Hospitalized for surgery at Apollo Hospital\"}"

fire "Intimate Claim: future incident date" \
  "$CAT_CLAIM_INTIMATE" 400 POST "$API/claims/intimate" "$CUSTOMER_TOKEN" \
  "{\"policyId\":\"$POLICY_ID\",\"claimType\":2,\"claimAmountRequested\":50000,\"incidentDate\":\"2030-01-01T10:00:00Z\",\"incidentDescription\":\"Hospitalized for surgery at Apollo Hospital\"}"

fire "Intimate Claim: description too short" \
  "$CAT_CLAIM_INTIMATE" 400 POST "$API/claims/intimate" "$CUSTOMER_TOKEN" \
  "{\"policyId\":\"$POLICY_ID\",\"claimType\":2,\"claimAmountRequested\":50000,\"incidentDate\":\"2025-01-01T10:00:00Z\",\"incidentDescription\":\"Short\"}"

fire "Intimate Claim: description exactly 9 chars (under min 10)" \
  "$CAT_CLAIM_INTIMATE" 400 POST "$API/claims/intimate" "$CUSTOMER_TOKEN" \
  "{\"policyId\":\"$POLICY_ID\",\"claimType\":2,\"claimAmountRequested\":50000,\"incidentDate\":\"2025-01-01T10:00:00Z\",\"incidentDescription\":\"NineChars\"}"

fire "Intimate Claim: description >2000 chars" \
  "$CAT_CLAIM_INTIMATE" 400 POST "$API/claims/intimate" "$CUSTOMER_TOKEN" \
  "{\"policyId\":\"$POLICY_ID\",\"claimType\":2,\"claimAmountRequested\":50000,\"incidentDate\":\"2025-01-01T10:00:00Z\",\"incidentDescription\":\"$LONG_2001\"}"

fire "Intimate Claim: policy not found (non-existent GUID)" \
  "$CAT_CLAIM_INTIMATE" 404 POST "$API/claims/intimate" "$CUSTOMER_TOKEN" \
  "{\"policyId\":\"$NONEXISTENT\",\"claimType\":2,\"claimAmountRequested\":50000,\"incidentDate\":\"2025-01-01T10:00:00Z\",\"incidentDescription\":\"Hospitalized for surgery at Apollo Hospital\"}"

fire "Intimate Claim: invalid claim type" \
  "$CAT_CLAIM_INTIMATE" 400 POST "$API/claims/intimate" "$CUSTOMER_TOKEN" \
  "{\"policyId\":\"$POLICY_ID\",\"claimType\":\"InvalidType\",\"claimAmountRequested\":50000,\"incidentDate\":\"2025-01-01T10:00:00Z\",\"incidentDescription\":\"Hospitalized for surgery at Apollo Hospital\"}"

fire "Get Claim: not found" \
  "Claim" 404 GET "$API/claims/$NONEXISTENT" "$CUSTOMER_TOKEN"

fire "Get Claim History: not found" \
  "Claim" 404 GET "$API/claims/$NONEXISTENT/history" "$CUSTOMER_TOKEN"

fire "Update Claim Status: invalid status value" \
  "$CAT_CLAIM_STATUS" 400 PUT "$API/claims/$CLAIM_ID/status" "$CLAIMS_TOKEN" \
  '{"status":"InvalidStatus","remarks":"Testing invalid status transition"}'

fire "Update Claim Status: empty remarks" \
  "$CAT_CLAIM_STATUS" 400 PUT "$API/claims/$CLAIM_ID/status" "$CLAIMS_TOKEN" \
  '{"status":"UnderReview","remarks":""}'

fire "Update Claim Status: claim not found" \
  "$CAT_CLAIM_STATUS" 404 PUT "$API/claims/$NONEXISTENT/status" "$CLAIMS_TOKEN" \
  '{"status":"UnderReview","remarks":"Reviewing the claim documentation"}'

fire "Update Claim Status: remarks too long (>1000 chars)" \
  "$CAT_CLAIM_STATUS" 400 PUT "$API/claims/$CLAIM_ID/status" "$CLAIMS_TOKEN" \
  "{\"status\":\"UnderReview\",\"remarks\":\"$LONG_1001\"}"

fire "Approve Claim: reject without reason" \
  "$CAT_CLAIM_APPROVE" 400 PUT "$API/claims/$CLAIM_ID/approve" "$CLAIMS_TOKEN" \
  '{"isApproved":false,"reason":"","approvedAmount":null}'

fire "Approve Claim: reject reason too short" \
  "$CAT_CLAIM_APPROVE" 400 PUT "$API/claims/$CLAIM_ID/approve" "$CLAIMS_TOKEN" \
  '{"isApproved":false,"reason":"No","approvedAmount":null}'

fire "Approve Claim: reject reason too long (>1000 chars)" \
  "$CAT_CLAIM_APPROVE" 400 PUT "$API/claims/$CLAIM_ID/approve" "$CLAIMS_TOKEN" \
  "{\"isApproved\":false,\"reason\":\"$LONG_1001\",\"approvedAmount\":null}"

fire "Approve Claim: approve without amount" \
  "$CAT_CLAIM_APPROVE" 400 PUT "$API/claims/$CLAIM_ID/approve" "$CLAIMS_TOKEN" \
  '{"isApproved":true,"reason":"Approved","approvedAmount":null}'

fire "Approve Claim: approve with zero amount" \
  "$CAT_CLAIM_APPROVE" 400 PUT "$API/claims/$CLAIM_ID/approve" "$CLAIMS_TOKEN" \
  '{"isApproved":true,"reason":"Approved","approvedAmount":0}'

fire "Approve Claim: approve with negative amount" \
  "$CAT_CLAIM_APPROVE" 400 PUT "$API/claims/$CLAIM_ID/approve" "$CLAIMS_TOKEN" \
  '{"isApproved":true,"reason":"Approved","approvedAmount":-5000}'

fire "Approve Claim: claim not found" \
  "$CAT_CLAIM_APPROVE" 404 PUT "$API/claims/$NONEXISTENT/approve" "$CLAIMS_TOKEN" \
  '{"isApproved":true,"approvedAmount":50000,"reason":"Approved after review"}'

fire "Assign Surveyor: empty notes" \
  "Claim/Surveyor" 400 PUT "$API/claims/$MOTOR_CLAIM_ID/assign-surveyor" "$CLAIMS_TOKEN" \
  '{"surveyorId":"5aabbc9f-0525-4a86-a45c-26cd3d72ff38","notes":""}'

fire "Assign Surveyor: notes too long (>500 chars)" \
  "Claim/Surveyor" 400 PUT "$API/claims/$MOTOR_CLAIM_ID/assign-surveyor" "$CLAIMS_TOKEN" \
  "{\"surveyorId\":\"5aabbc9f-0525-4a86-a45c-26cd3d72ff38\",\"notes\":\"$LONG_501\"}"

fire "Assign Surveyor: claim not found" \
  "Claim/Surveyor" 404 PUT "$API/claims/$NONEXISTENT/assign-surveyor" "$CLAIMS_TOKEN" \
  '{"surveyorId":"5aabbc9f-0525-4a86-a45c-26cd3d72ff38","notes":"Please inspect the vehicle damage"}'

fire "Settle Claim: not found" \
  "Claim/Settle" 404 PUT "$API/claims/$NONEXISTENT/settle" "$CLAIMS_TOKEN"

fire "Assign Claim: not found" \
  "Claim" 404 PUT "$API/claims/$NONEXISTENT/assign" "$CLAIMS_TOKEN"

fire "Request Docs: claim not found" \
  "Claim" 404 POST "$API/claims/$NONEXISTENT/request-docs" "$CLAIMS_TOKEN" \
  '"Please submit hospital bills"'

fire "Approve Pre-Auth: claim not found" \
  "Claim" 404 PUT "$API/claims/$NONEXISTENT/approve-preauth" "$CLAIMS_TOKEN"

echo ""

# ============================================================================
# 14. GRIEVANCE — VALIDATION (expanded)
# ============================================================================
echo "━━━ 14. GRIEVANCE — Validation ━━━"

fire "Raise Grievance: empty body" \
  "$CAT_GRIEVANCE" 400 POST "$API/grievances" "$CUSTOMER_TOKEN" "{}"

fire "Raise Grievance: description too short" \
  "$CAT_GRIEVANCE" 400 POST "$API/grievances" "$CUSTOMER_TOKEN" \
  '{"category":"ClaimDelay","description":"Short"}'

fire "Raise Grievance: description exactly 9 chars" \
  "$CAT_GRIEVANCE" 400 POST "$API/grievances" "$CUSTOMER_TOKEN" \
  '{"category":"ClaimDelay","description":"NineChars"}'

fire "Raise Grievance: description too long (>2000 chars)" \
  "$CAT_GRIEVANCE" 400 POST "$API/grievances" "$CUSTOMER_TOKEN" \
  "{\"category\":\"ClaimDelay\",\"description\":\"$LONG_2001\"}"

fire "Raise Grievance: invalid category" \
  "$CAT_GRIEVANCE" 400 POST "$API/grievances" "$CUSTOMER_TOKEN" \
  '{"category":"InvalidCategory","description":"This is a detailed grievance description for testing"}'

fire "Get Grievance: not found" \
  "$CAT_GRIEVANCE" 404 GET "$API/grievances/$NONEXISTENT" "$CLAIMS_TOKEN"

fire "Update Grievance Status: not found" \
  "$CAT_GRIEVANCE" 404 PUT "$API/grievances/$NONEXISTENT/status" "$CLAIMS_TOKEN" \
  '{"status":"InProgress"}'

fire "Update Grievance Status: resolve without notes" \
  "$CAT_GRIEVANCE" 400 PUT "$API/grievances/$GRIEVANCE_ID/status" "$CLAIMS_TOKEN" \
  '{"status":"Resolved","resolutionNotes":""}'

fire "Update Grievance Status: close without notes" \
  "$CAT_GRIEVANCE" 400 PUT "$API/grievances/$GRIEVANCE_ID/status" "$CLAIMS_TOKEN" \
  '{"status":"Closed","resolutionNotes":""}'

fire "Update Grievance Status: invalid status enum" \
  "$CAT_GRIEVANCE" 400 PUT "$API/grievances/$GRIEVANCE_ID/status" "$CLAIMS_TOKEN" \
  '{"status":"InvalidStatus"}'

fire "Assign Grievance: not found" \
  "$CAT_GRIEVANCE" 404 PUT "$API/grievances/$NONEXISTENT/assign" "$CLAIMS_TOKEN" \
  '{"assignedToId":"019ed3b7-3e38-7abf-9ee6-275fb145387a"}'

echo ""

echo "$WAIT_MSG_GLOBAL_RATE_LIMIT"
sleep 62

# ============================================================================
# 15. PAYMENTS — VALIDATION (expanded)
# ============================================================================
echo "━━━ 15. PAYMENTS — Validation ━━━"

fire "Pay Premium: schedule not found" \
  "$CAT_PAYMENT" 404 POST "$API/payments/pay/$NONEXISTENT" "$CUSTOMER_TOKEN" \
  '{"paymentMethodId":"pm_card_visa"}'

fire "Get Schedule: invalid policy ID" \
  "$CAT_PAYMENT" 400 GET "$API/payments/schedule/not-a-guid" "$CUSTOMER_TOKEN"

fire "Download Receipt: not found" \
  "$CAT_PAYMENT" 404 GET "$API/payments/$NONEXISTENT/receipt" "$CUSTOMER_TOKEN"

fire "Reconcile Payment: not found" \
  "$CAT_PAYMENT" 404 PUT "$API/payments/$NONEXISTENT/reconcile" "$FINANCE_TOKEN"

fire "Refund Payment: not found" \
  "$CAT_PAYMENT" 404 POST "$API/payments/$NONEXISTENT/refund" "$FINANCE_TOKEN"

fire "Process Payout: claim not found" \
  "$CAT_PAYMENT" 404 POST "$API/payments/payout/claim/$NONEXISTENT" "$FINANCE_TOKEN"

fire "Mark Claim Settled: not found" \
  "$CAT_PAYMENT" 404 PUT "$API/payments/claims/$NONEXISTENT/settle" "$FINANCE_TOKEN"

fire "Approve Commission: not found" \
  "$CAT_PAYMENT" 404 POST "$API/payments/commissions/$NONEXISTENT/approve" "$FINANCE_TOKEN"

fire "Customer accessing finance reports (forbidden)" \
  "$CAT_PAYMENT" 403 GET "$API/payments/reports/overdue" "$CUSTOMER_TOKEN"

fire "Customer accessing reconcile (forbidden)" \
  "$CAT_PAYMENT" 403 PUT "$API/payments/$PAYMENT_ID/reconcile" "$CUSTOMER_TOKEN"

fire "Get Schedule: invalid ID format (not-a-guid)" \
  "$CAT_PAYMENT" 400 GET "$API/payments/schedule/not-a-guid-at-all" "$CUSTOMER_TOKEN"

fire "Download Receipt: invalid payment ID format" \
  "$CAT_PAYMENT" 400 GET "$API/payments/not-a-guid/receipt" "$CUSTOMER_TOKEN"

fire "Process Payout: invalid claim ID" \
  "$CAT_PAYMENT" 400 POST "$API/payments/payout/claim/not-a-guid" "$FINANCE_TOKEN"

fire "Approve Commission: invalid ID" \
  "$CAT_PAYMENT" 400 POST "$API/payments/commissions/not-a-guid/approve" "$FINANCE_TOKEN"

fire "Agent accessing finance reports (forbidden)" \
  "$CAT_PAYMENT" 403 GET "$API/payments/reports/overdue" "$AGENT_TOKEN"

fire "Underwriter accessing reconcile (forbidden)" \
  "$CAT_PAYMENT" 403 PUT "$API/payments/$PAYMENT_ID/reconcile" "$UNDERWRITER_TOKEN"

echo ""

# ============================================================================
# 16. AGENT — VALIDATION (expanded)
# ============================================================================
echo "━━━ 16. AGENT — Validation ━━━"

fire "Agent Profile Update: empty body" \
  "Agent" 400 PUT "$API/agents/profile" "$AGENT_TOKEN" "{}"

fire "Agent Profile Update: invalid phone" \
  "Agent" 400 PUT "$API/agents/profile" "$AGENT_TOKEN" \
  '{"firstName":"Test","lastName":"Agent","salutation":"Mr","phone":"123"}'

fire "Agent Profile Update: missing firstName" \
  "Agent" 400 PUT "$API/agents/profile" "$AGENT_TOKEN" \
  '{"firstName":"","lastName":"Agent","salutation":"Mr","phone":"9876543210"}'

fire "Agent Profile Update: missing lastName" \
  "Agent" 400 PUT "$API/agents/profile" "$AGENT_TOKEN" \
  '{"firstName":"Test","lastName":"","salutation":"Mr","phone":"9876543210"}'

fire "Agent Profile Update: invalid salutation" \
  "Agent" 400 PUT "$API/agents/profile" "$AGENT_TOKEN" \
  '{"firstName":"Test","lastName":"Agent","salutation":"King","phone":"9876543210"}'

fire "Create Branch: empty body" \
  "$CAT_AGENT_BRANCH" 400 POST "$API/agents/branches" "$ADMIN_TOKEN" "{}"

fire "Create Branch: invalid email" \
  "$CAT_AGENT_BRANCH" 400 POST "$API/agents/branches" "$ADMIN_TOKEN" \
  '{"name":"Test Branch","city":"Delhi","state":"Delhi","address":"123 St","phone":"9876543210","email":"not-an-email"}'

fire "Create Branch: missing city" \
  "$CAT_AGENT_BRANCH" 400 POST "$API/agents/branches" "$ADMIN_TOKEN" \
  '{"name":"Test Branch","city":"","state":"Delhi","address":"123 St","phone":"9876543210","email":"branch@test.com"}'

fire "Create Branch: phone not 10 digits" \
  "$CAT_AGENT_BRANCH" 400 POST "$API/agents/branches" "$ADMIN_TOKEN" \
  '{"name":"Test Branch","city":"Delhi","state":"Delhi","address":"123 St","phone":"12345","email":"branch@test.com"}'

fire "Create Branch: missing name" \
  "$CAT_AGENT_BRANCH" 400 POST "$API/agents/branches" "$ADMIN_TOKEN" \
  '{"name":"","city":"Delhi","state":"Delhi","address":"123 St","phone":"9876543210","email":"branch@test.com"}'

fire "Assign Agent to Branch: agent not found" \
  "$CAT_AGENT_BRANCH" 404 PUT "$API/agents/$NONEXISTENT/branch/$NONEXISTENT" "$ADMIN_TOKEN"

fire "Update Agent License: agent not found" \
  "Agent/License" 404 PUT "$API/agents/$NONEXISTENT/license" "$ADMIN_TOKEN" \
  '{"licenseNumber":"LIC-001","licenseExpiry":"2027-01-01"}'

fire "Update Agent License: expiry in the past" \
  "Agent/License" 400 PUT "$API/agents/$AGENT_USER_ID/license" "$ADMIN_TOKEN" \
  '{"licenseNumber":"LIC-001","licenseExpiry":"2020-01-01"}'

fire "Update Agent License: missing license number" \
  "Agent/License" 400 PUT "$API/agents/$AGENT_USER_ID/license" "$ADMIN_TOKEN" \
  '{"licenseNumber":"","licenseExpiry":"2027-01-01"}'

fire "Activate Agent: not found" \
  "Agent" 404 PUT "$API/agents/$NONEXISTENT/status?isActive=true" "$ADMIN_TOKEN"

echo ""

# ============================================================================
# 17. SYSTEM — VALIDATION (expanded)
# ============================================================================
echo "━━━ 17. SYSTEM — Validation ━━━"

fire "Update Config: empty body" \
  "$CAT_SYSTEM" 400 PUT "$API/system/configs" "$ADMIN_TOKEN" "{}"

fire "Update Config: key too long (> 100 chars)" \
  "$CAT_SYSTEM" 400 PUT "$API/system/configs" "$ADMIN_TOKEN" \
  "{\"configKey\":\"$LONG_101\",\"configValue\":\"test\"}"

fire "Update Config: missing key" \
  "$CAT_SYSTEM" 400 PUT "$API/system/configs" "$ADMIN_TOKEN" \
  '{"configKey":"","configValue":"test"}'

fire "Update Config: missing value" \
  "$CAT_SYSTEM" 400 PUT "$API/system/configs" "$ADMIN_TOKEN" \
  '{"configKey":"test_key","configValue":""}'

fire "Email Template: empty body" \
  "$CAT_SYSTEM" 400 PUT "$API/system/email-templates" "$ADMIN_TOKEN" "{}"

fire "Email Template: missing subject" \
  "$CAT_SYSTEM" 400 PUT "$API/system/email-templates" "$ADMIN_TOKEN" \
  '{"templateKey":"test_template","subject":"","bodyHtml":"<p>Test</p>"}'

fire "Email Template: missing bodyHtml" \
  "$CAT_SYSTEM" 400 PUT "$API/system/email-templates" "$ADMIN_TOKEN" \
  '{"templateKey":"test_template","subject":"Test Subject","bodyHtml":""}'

fire "Email Template: key too long (>100 chars)" \
  "$CAT_SYSTEM" 400 PUT "$API/system/email-templates" "$ADMIN_TOKEN" \
  "{\"templateKey\":\"$LONG_101\",\"subject\":\"Test\",\"bodyHtml\":\"<p>Test</p>\"}"

echo ""

# ============================================================================
# 18. NOTIFICATION — VALIDATION
# ============================================================================
echo "━━━ 18. NOTIFICATION — Validation ━━━"

fire "Mark Notification Read: not found" \
  "Notification" 404 PATCH "$API/users/notifications/$NONEXISTENT/read" "$CUSTOMER_TOKEN"

echo ""

# ============================================================================
# 19. STRIPE WEBHOOK — VALIDATION
# ============================================================================
echo "━━━ 19. STRIPE WEBHOOK ━━━"

fire "Stripe Webhook: invalid signature" \
  "Webhook" 400 POST "$API/payments/webhook" "" \
  '{"type":"payment_intent.succeeded"}'

echo ""

# ============================================================================
# 20. BUSINESS LOGIC ERRORS (cross-cutting state violations)
# ============================================================================
echo "━━━ 20. BUSINESS LOGIC — State Violations ━━━"

fire "Settle unapproved claim (must be approved first)" \
  "$CAT_BIZLOGIC" 422 PUT "$API/claims/$CLAIM_ID/settle" "$CLAIMS_TOKEN"

fire "Pre-auth on non-cashless claim" \
  "$CAT_BIZLOGIC" 422 PUT "$API/claims/$CLAIM_ID/approve-preauth" "$CLAIMS_TOKEN"

fire "Process payout on unapproved claim" \
  "$CAT_BIZLOGIC" 422 POST "$API/payments/payout/claim/$CLAIM_ID" "$FINANCE_TOKEN"

fire "Pay already-paid schedule (conflict)" \
  "$CAT_BIZLOGIC" 409 POST "$API/payments/pay/$SCHEDULE_ID" "$CUSTOMER_TOKEN" \
  '{"paymentMethodId":"pm_card_visa"}'

fire "Assign surveyor to health claim (not motor)" \
  "$CAT_BIZLOGIC" 422 PUT "$API/claims/$CLAIM_ID/assign-surveyor" "$CLAIMS_TOKEN" \
  '{"surveyorId":"5aabbc9f-0525-4a86-a45c-26cd3d72ff38","notes":"Inspect the damage at the hospital site"}'

fire "Claim on inactive/cancelled policy" \
  "$CAT_BIZLOGIC" 422 POST "$API/claims/intimate" "$CUSTOMER_TOKEN" \
  "{\"policyId\":\"$CANCELLABLE_POLICY_ID\",\"claimType\":2,\"claimAmountRequested\":50000,\"incidentDate\":\"2025-06-01T10:00:00Z\",\"incidentDescription\":\"Testing claim on cancelled policy scenario\"}"

fire "Get Policy Endorsements: access denied (underwriter, not customer)" \
  "$CAT_BIZLOGIC" 403 GET "$API/policies/$NONEXISTENT/endorsements" "$UNDERWRITER_TOKEN"

fire "Get Policy: not found (GUID is all zeros)" \
  "$CAT_BIZLOGIC" 404 GET "$API/policies/$FAKE_GUID" "$CUSTOMER_TOKEN"

fire "Download Policy: not found (GUID is all zeros)" \
  "$CAT_BIZLOGIC" 404 GET "$API/policies/$FAKE_GUID/download" "$CUSTOMER_TOKEN"

echo ""

echo "$WAIT_MSG_GLOBAL_RATE_LIMIT"
sleep 62

# ============================================================================
# 21. ADDITIONAL CROSS-ROLE FORBIDDEN TESTS
# ============================================================================
echo "━━━ 21. ADDITIONAL — Cross-Role Forbidden ━━━"

fire "Underwriter accessing Agent dashboard" \
  "$CAT_CROSSROLE" 403 GET "$API/agents/dashboard" "$UNDERWRITER_TOKEN"

fire "Surveyor accessing Customer family members" \
  "$CAT_CROSSROLE" 403 GET "$API/users/family" "$SURVEYOR_TOKEN"

fire "FinanceOfficer accessing Customer profile" \
  "$CAT_CROSSROLE" 403 GET "$API/users/profile" "$FINANCE_TOKEN"

fire "ClaimsOfficer accessing Agent profile" \
  "$CAT_CROSSROLE" 403 GET "$API/agents/profile" "$CLAIMS_TOKEN"

fire "Surveyor accessing Admin user list" \
  "$CAT_CROSSROLE" 403 GET "$API/users/all" "$SURVEYOR_TOKEN"

fire "Agent accessing Admin audit logs" \
  "$CAT_CROSSROLE" 403 GET "$API/system/audit-logs" "$AGENT_TOKEN"

fire "Customer accessing Underwriter KYC pending" \
  "$CAT_CROSSROLE" 403 GET "$API/users/kyc/pending" "$CUSTOMER_TOKEN"

fire "Agent accessing Finance payment records" \
  "$CAT_CROSSROLE" 403 GET "$API/payments/all-records" "$AGENT_TOKEN"

fire "Surveyor accessing Finance reports" \
  "$CAT_CROSSROLE" 403 GET "$API/payments/reports/summary?period=monthly" "$SURVEYOR_TOKEN"

fire "Customer accessing Agent renewals" \
  "$CAT_CROSSROLE" 403 GET "$API/agents/renewals" "$CUSTOMER_TOKEN"

fire "FinanceOfficer accessing Admin email templates" \
  "$CAT_CROSSROLE" 403 PUT "$API/system/email-templates" "$FINANCE_TOKEN" \
  '{"templateKey":"hack","subject":"Hack","bodyHtml":"<p>hack</p>"}'

fire "ClaimsOfficer accessing Admin user status" \
  "$CAT_CROSSROLE" 403 PUT "$API/users/$CUSTOMER_USER_ID/status?isActive=false" "$CLAIMS_TOKEN"

fire "Surveyor accessing Admin branches" \
  "$CAT_CROSSROLE" 403 GET "$API/agents/branches" "$SURVEYOR_TOKEN"

fire "Agent accessing Underwriter proposal review" \
  "$CAT_CROSSROLE" 403 POST "$API/proposals/$PROPOSAL_ID/review" "$AGENT_TOKEN" \
  '{"isApproved":true,"notes":"Approved"}'

fire "Customer accessing ClaimsOfficer claim status update" \
  "$CAT_CROSSROLE" 403 PUT "$API/claims/$CLAIM_ID/status" "$CUSTOMER_TOKEN" \
  '{"status":"UnderReview","remarks":"Customer trying to update"}'

fire "Customer accessing Admin user role change" \
  "$CAT_CROSSROLE" 403 PUT "$API/users/$CUSTOMER_USER_ID/role" "$CUSTOMER_TOKEN" \
  '"Admin"'

fire "FinanceOfficer accessing Agent branch assign" \
  "$CAT_CROSSROLE" 403 PUT "$API/agents/$AGENT_USER_ID/branch/$NONEXISTENT" "$FINANCE_TOKEN"

fire "Surveyor accessing Admin product create" \
  "$CAT_CROSSROLE" 403 POST "$API/products" "$SURVEYOR_TOKEN" \
  '{"productName":"SurvProd","domain":"Health","uin":"UIN-SURV","description":"Surveyor attempt","minAge":18,"maxAge":65,"minSumAssured":100000,"maxSumAssured":5000000,"minTenureYears":1,"maxTenureYears":30,"waitingPeriodDays":30,"allowsFamilyFloater":false}'

echo ""

# ============================================================================
# 22. ADDITIONAL FIELD-LEVEL VALIDATION
# ============================================================================
echo "━━━ 22. ADDITIONAL — Field-Level Edge Cases ━━━"

fire "Grievance: resolve notes too long (>2000 chars)" \
  "$CAT_FIELDEDGE" 400 PUT "$API/grievances/$GRIEVANCE_ID/status" "$CLAIMS_TOKEN" \
  "{\"status\":\"Resolved\",\"resolutionNotes\":\"$LONG_2001\"}"

fire "Product: description too long (>2000 chars)" \
  "$CAT_FIELDEDGE" 400 POST "$API/products" "$ADMIN_TOKEN" \
  "{\"productName\":\"Test\",\"domain\":\"Health\",\"uin\":\"UIN-LONG\",\"description\":\"$LONG_2001\",\"minAge\":18,\"maxAge\":65,\"minSumAssured\":100000,\"maxSumAssured\":5000000,\"minTenureYears\":1,\"maxTenureYears\":30,\"waitingPeriodDays\":30,\"allowsFamilyFloater\":false}"

fire "Product: UIN too long (>50 chars)" \
  "$CAT_FIELDEDGE" 400 POST "$API/products" "$ADMIN_TOKEN" \
  '{"productName":"Test","domain":"Health","uin":"AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA","description":"Desc","minAge":18,"maxAge":65,"minSumAssured":100000,"maxSumAssured":5000000,"minTenureYears":1,"maxTenureYears":30,"waitingPeriodDays":30,"allowsFamilyFloater":false}'

fire "Branch: address too long (>300 chars)" \
  "$CAT_FIELDEDGE" 400 POST "$API/agents/branches" "$ADMIN_TOKEN" \
  "{\"name\":\"Test\",\"city\":\"Delhi\",\"state\":\"Delhi\",\"address\":\"$(python3 -c "print('A'*301)")\",\"phone\":\"9876543210\",\"email\":\"b@t.com\"}"

fire "Branch: state too long (>100 chars)" \
  "$CAT_FIELDEDGE" 400 POST "$API/agents/branches" "$ADMIN_TOKEN" \
  "{\"name\":\"Test\",\"city\":\"Delhi\",\"state\":\"$LONG_101\",\"address\":\"123 St\",\"phone\":\"9876543210\",\"email\":\"b@t.com\"}"

fire "Branch: city too long (>100 chars)" \
  "$CAT_FIELDEDGE" 400 POST "$API/agents/branches" "$ADMIN_TOKEN" \
  "{\"name\":\"Test\",\"city\":\"$LONG_101\",\"state\":\"Delhi\",\"address\":\"123 St\",\"phone\":\"9876543210\",\"email\":\"b@t.com\"}"

fire "Branch: name too long (>200 chars)" \
  "$CAT_FIELDEDGE" 400 POST "$API/agents/branches" "$ADMIN_TOKEN" \
  "{\"name\":\"$LONG_201\",\"city\":\"Delhi\",\"state\":\"Delhi\",\"address\":\"123 St\",\"phone\":\"9876543210\",\"email\":\"b@t.com\"}"

fire "Config: value too long (>2000 chars)" \
  "$CAT_FIELDEDGE" 400 PUT "$API/system/configs" "$ADMIN_TOKEN" \
  "{\"configKey\":\"test_key\",\"configValue\":\"$LONG_2001\"}"

fire "Email Template: subject too long (>200 chars)" \
  "$CAT_FIELDEDGE" 400 PUT "$API/system/email-templates" "$ADMIN_TOKEN" \
  "{\"templateKey\":\"test\",\"subject\":\"$LONG_201\",\"bodyHtml\":\"<p>Test</p>\"}"

fire "Agent Profile: firstName too long (>100 chars)" \
  "$CAT_FIELDEDGE" 400 PUT "$API/agents/profile" "$AGENT_TOKEN" \
  "{\"firstName\":\"$LONG_101\",\"lastName\":\"Agent\",\"salutation\":\"Mr\",\"phone\":\"9876543210\"}"

fire "Agent Profile: lastName too long (>100 chars)" \
  "$CAT_FIELDEDGE" 400 PUT "$API/agents/profile" "$AGENT_TOKEN" \
  "{\"firstName\":\"Test\",\"lastName\":\"$LONG_101\",\"salutation\":\"Mr\",\"phone\":\"9876543210\"}"

fire "Address: city too long (>100 chars)" \
  "$CAT_FIELDEDGE" 400 POST "$API/users/addresses" "$CUSTOMER_TOKEN" \
  "{\"addressType\":\"Permanent\",\"addressLine1\":\"123 St\",\"city\":\"$LONG_101\",\"state\":\"Delhi\",\"postalCode\":\"110001\",\"country\":\"India\"}"

fire "Address: state too long (>100 chars)" \
  "$CAT_FIELDEDGE" 400 POST "$API/users/addresses" "$CUSTOMER_TOKEN" \
  "{\"addressType\":\"Permanent\",\"addressLine1\":\"123 St\",\"city\":\"Delhi\",\"state\":\"$LONG_101\",\"postalCode\":\"110001\",\"country\":\"India\"}"

fire "Address: country too long (>100 chars)" \
  "$CAT_FIELDEDGE" 400 POST "$API/users/addresses" "$CUSTOMER_TOKEN" \
  "{\"addressType\":\"Permanent\",\"addressLine1\":\"123 St\",\"city\":\"Delhi\",\"state\":\"Delhi\",\"postalCode\":\"110001\",\"country\":\"$LONG_101\"}"

fire "Family Member: lastName too long (>100 chars)" \
  "$CAT_FIELDEDGE" 400 POST "$API/users/family" "$CUSTOMER_TOKEN" \
  "{\"firstName\":\"Test\",\"lastName\":\"$LONG_101\",\"salutation\":\"Mr\",\"gender\":\"Male\",\"relationship\":\"Spouse\",\"dateOfBirth\":\"1995-01-01\"}"

fire "Nominee: fullName too long (>100 chars)" \
  "$CAT_FIELDEDGE" 400 PUT "$API/policies/nominees/$NONEXISTENT" "$CUSTOMER_TOKEN" \
  "{\"fullName\":\"$LONG_101\",\"relationship\":\"Spouse\",\"dateOfBirth\":\"1996-01-01\",\"sharePercentage\":100,\"isMinor\":false}"

fire "Motor detail: vehicle number too long (>20 chars)" \
  "$CAT_FIELDEDGE" 400 POST "$API/proposals" "$CUSTOMER_TOKEN" \
  "{\"customerId\":\"$CUSTOMER_USER_ID\",\"productId\":\"$PRODUCT_ID\",\"sumAssured\":500000,\"tenureYears\":10,\"premiumAmount\":5000,\"paymentFrequency\":\"Monthly\",\"motorDetail\":{\"vehicleNumber\":\"AAAAAAAAAAAAAAAAAAAAA\",\"vehicleMake\":\"Toyota\",\"vehicleModel\":\"Camry\",\"manufactureYear\":2020,\"engineNumber\":\"ENG123\",\"chassisNumber\":\"CHS123\",\"idv\":500000}}"

fire "Motor detail: missing vehicleMake" \
  "$CAT_FIELDEDGE" 400 POST "$API/proposals" "$CUSTOMER_TOKEN" \
  "{\"customerId\":\"$CUSTOMER_USER_ID\",\"productId\":\"$PRODUCT_ID\",\"sumAssured\":500000,\"tenureYears\":10,\"premiumAmount\":5000,\"paymentFrequency\":\"Monthly\",\"motorDetail\":{\"vehicleNumber\":\"KA01AB1234\",\"vehicleMake\":\"\",\"vehicleModel\":\"Camry\",\"manufactureYear\":2020,\"engineNumber\":\"ENG123\",\"chassisNumber\":\"CHS123\",\"idv\":500000}}"

fire "Motor detail: missing vehicleModel" \
  "$CAT_FIELDEDGE" 400 POST "$API/proposals" "$CUSTOMER_TOKEN" \
  "{\"customerId\":\"$CUSTOMER_USER_ID\",\"productId\":\"$PRODUCT_ID\",\"sumAssured\":500000,\"tenureYears\":10,\"premiumAmount\":5000,\"paymentFrequency\":\"Monthly\",\"motorDetail\":{\"vehicleNumber\":\"KA01AB1234\",\"vehicleMake\":\"Toyota\",\"vehicleModel\":\"\",\"manufactureYear\":2020,\"engineNumber\":\"ENG123\",\"chassisNumber\":\"CHS123\",\"idv\":500000}}"

fire "Submit Proposal: negative premium amount" \
  "$CAT_FIELDEDGE" 400 POST "$API/proposals" "$CUSTOMER_TOKEN" \
  "{\"customerId\":\"$CUSTOMER_USER_ID\",\"productId\":\"$PRODUCT_ID\",\"sumAssured\":500000,\"tenureYears\":10,\"premiumAmount\":-100,\"paymentFrequency\":\"Monthly\"}"

fire "Submit Proposal: zero sum assured" \
  "$CAT_FIELDEDGE" 400 POST "$API/proposals" "$CUSTOMER_TOKEN" \
  "{\"customerId\":\"$CUSTOMER_USER_ID\",\"productId\":\"$PRODUCT_ID\",\"sumAssured\":0,\"tenureYears\":10,\"premiumAmount\":5000,\"paymentFrequency\":\"Monthly\"}"

fire "Agent License: license number too long (>50 chars)" \
  "$CAT_FIELDEDGE" 400 PUT "$API/agents/$AGENT_USER_ID/license" "$ADMIN_TOKEN" \
  '{"licenseNumber":"AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA","licenseExpiry":"2027-01-01"}'

fire "Intimate Claim: negative amount" \
  "$CAT_FIELDEDGE" 400 POST "$API/claims/intimate" "$CUSTOMER_TOKEN" \
  "{\"policyId\":\"$POLICY_ID\",\"claimType\":2,\"claimAmountRequested\":-1000,\"incidentDate\":\"2025-01-01T10:00:00Z\",\"incidentDescription\":\"Testing negative claim amount scenario\"}"

fire "Endorsement: description min boundary (9 chars)" \
  "$CAT_FIELDEDGE" 400 POST "$API/policies/$POLICY_ID/endorsements" "$CUSTOMER_TOKEN" \
  '{"endorsementType":"AddressChange","description":"NineChars"}'

fire "Grievance: description min boundary (9 chars)" \
  "$CAT_FIELDEDGE" 400 POST "$API/grievances" "$CUSTOMER_TOKEN" \
  '{"category":"ClaimDelay","description":"NineChars"}'

fire "Nominee: share percentage zero" \
  "$CAT_FIELDEDGE" 400 PUT "$API/policies/nominees/$NONEXISTENT" "$CUSTOMER_TOKEN" \
  '{"fullName":"Test","relationship":"Spouse","dateOfBirth":"1996-01-01","sharePercentage":0,"isMinor":false}'

fire "Register Agent: salutation not in allowed list" \
  "$CAT_FIELDEDGE" 400 POST "$API/auth/admin/register-agent" "$ADMIN_TOKEN" \
  '{"email":"edge@test.com","password":"Password@123","salutation":"Emperor","firstName":"New","lastName":"Agent","phone":"9876543210","licenseNumber":"LIC-E01","agencyName":"Agency","aadhaarNumber":"123456789012","panNumber":"ABCDE1234F","maritalStatus":"Single","permanentAddress":{"line1":"St","city":"City","state":"State","postalCode":"110001","country":"India"},"isSameAsPermanent":true}'

fire "Motor detail: engine number too long (>50 chars)" \
  "$CAT_FIELDEDGE" 400 POST "$API/proposals" "$CUSTOMER_TOKEN" \
  "{\"customerId\":\"$CUSTOMER_USER_ID\",\"productId\":\"$PRODUCT_ID\",\"sumAssured\":500000,\"tenureYears\":10,\"premiumAmount\":5000,\"paymentFrequency\":\"Monthly\",\"motorDetail\":{\"vehicleNumber\":\"KA01AB1234\",\"vehicleMake\":\"Toyota\",\"vehicleModel\":\"Camry\",\"manufactureYear\":2020,\"engineNumber\":\"$(python3 -c "print('E'*51)")\",\"chassisNumber\":\"CHS123\",\"idv\":500000}}"

fire "Motor detail: chassis number too long (>50 chars)" \
  "$CAT_FIELDEDGE" 400 POST "$API/proposals" "$CUSTOMER_TOKEN" \
  "{\"customerId\":\"$CUSTOMER_USER_ID\",\"productId\":\"$PRODUCT_ID\",\"sumAssured\":500000,\"tenureYears\":10,\"premiumAmount\":5000,\"paymentFrequency\":\"Monthly\",\"motorDetail\":{\"vehicleNumber\":\"KA01AB1234\",\"vehicleMake\":\"Toyota\",\"vehicleModel\":\"Camry\",\"manufactureYear\":2020,\"engineNumber\":\"ENG123\",\"chassisNumber\":\"$(python3 -c "print('C'*51)")\",\"idv\":500000}}"

fire "Motor detail: vehicleMake too long (>100 chars)" \
  "$CAT_FIELDEDGE" 400 POST "$API/proposals" "$CUSTOMER_TOKEN" \
  "{\"customerId\":\"$CUSTOMER_USER_ID\",\"productId\":\"$PRODUCT_ID\",\"sumAssured\":500000,\"tenureYears\":10,\"premiumAmount\":5000,\"paymentFrequency\":\"Monthly\",\"motorDetail\":{\"vehicleNumber\":\"KA01AB1234\",\"vehicleMake\":\"$LONG_101\",\"vehicleModel\":\"Camry\",\"manufactureYear\":2020,\"engineNumber\":\"ENG123\",\"chassisNumber\":\"CHS123\",\"idv\":500000}}"

fire "Motor detail: vehicleModel too long (>100 chars)" \
  "$CAT_FIELDEDGE" 400 POST "$API/proposals" "$CUSTOMER_TOKEN" \
  "{\"customerId\":\"$CUSTOMER_USER_ID\",\"productId\":\"$PRODUCT_ID\",\"sumAssured\":500000,\"tenureYears\":10,\"premiumAmount\":5000,\"paymentFrequency\":\"Monthly\",\"motorDetail\":{\"vehicleNumber\":\"KA01AB1234\",\"vehicleMake\":\"Toyota\",\"vehicleModel\":\"$LONG_101\",\"manufactureYear\":2020,\"engineNumber\":\"ENG123\",\"chassisNumber\":\"CHS123\",\"idv\":500000}}"

fire "Motor detail: negative IDV" \
  "$CAT_FIELDEDGE" 400 POST "$API/proposals" "$CUSTOMER_TOKEN" \
  "{\"customerId\":\"$CUSTOMER_USER_ID\",\"productId\":\"$PRODUCT_ID\",\"sumAssured\":500000,\"tenureYears\":10,\"premiumAmount\":5000,\"paymentFrequency\":\"Monthly\",\"motorDetail\":{\"vehicleNumber\":\"KA01AB1234\",\"vehicleMake\":\"Toyota\",\"vehicleModel\":\"Camry\",\"manufactureYear\":2020,\"engineNumber\":\"ENG123\",\"chassisNumber\":\"CHS123\",\"idv\":-5000}}"

fire "Proposal: nominee share percentage negative" \
  "$CAT_FIELDEDGE" 400 POST "$API/proposals" "$CUSTOMER_TOKEN" \
  "{\"customerId\":\"$CUSTOMER_USER_ID\",\"productId\":\"$PRODUCT_ID\",\"sumAssured\":500000,\"tenureYears\":10,\"premiumAmount\":5000,\"paymentFrequency\":\"Monthly\",\"nominees\":[{\"fullName\":\"Neg\",\"relationship\":\"Spouse\",\"dateOfBirth\":\"1996-01-01\",\"sharePercentage\":-10,\"isMinor\":false}]}"

fire "Register Agent: agency name too long (>200 chars)" \
  "$CAT_FIELDEDGE" 400 POST "$API/auth/admin/register-agent" "$ADMIN_TOKEN" \
  "{\"email\":\"longagency@test.com\",\"password\":\"Password@123\",\"salutation\":\"Mr\",\"firstName\":\"New\",\"lastName\":\"Agent\",\"phone\":\"9876543210\",\"licenseNumber\":\"LIC-LNG\",\"agencyName\":\"$LONG_201\",\"aadhaarNumber\":\"123456789012\",\"panNumber\":\"ABCDE1234F\",\"maritalStatus\":\"Single\",\"permanentAddress\":{\"line1\":\"St\",\"city\":\"City\",\"state\":\"State\",\"postalCode\":\"110001\",\"country\":\"India\"},\"isSameAsPermanent\":true}"

fire "Submit Proposal: zero premium amount" \
  "$CAT_FIELDEDGE" 400 POST "$API/proposals" "$CUSTOMER_TOKEN" \
  "{\"customerId\":\"$CUSTOMER_USER_ID\",\"productId\":\"$PRODUCT_ID\",\"sumAssured\":500000,\"tenureYears\":10,\"premiumAmount\":0,\"paymentFrequency\":\"Monthly\"}"

fire "Product: min tenure 0" \
  "$CAT_FIELDEDGE" 400 POST "$API/products" "$ADMIN_TOKEN" \
  '{"productName":"TestProd","domain":"Health","uin":"UIN-MT0","description":"Desc","minAge":18,"maxAge":65,"minSumAssured":100000,"maxSumAssured":5000000,"minTenureYears":0,"maxTenureYears":30,"waitingPeriodDays":30,"allowsFamilyFloater":false}'

fire "Product: product name too long (>200 chars)" \
  "$CAT_FIELDEDGE" 400 POST "$API/products" "$ADMIN_TOKEN" \
  "{\"productName\":\"$LONG_201\",\"domain\":\"Health\",\"uin\":\"UIN-PN\",\"description\":\"Desc\",\"minAge\":18,\"maxAge\":65,\"minSumAssured\":100000,\"maxSumAssured\":5000000,\"minTenureYears\":1,\"maxTenureYears\":30,\"waitingPeriodDays\":30,\"allowsFamilyFloater\":false}"

fire "Intimate Claim: negative claim amount" \
  "$CAT_FIELDEDGE" 400 POST "$API/claims/intimate" "$CUSTOMER_TOKEN" \
  "{\"policyId\":\"$POLICY_ID\",\"claimType\":2,\"claimAmountRequested\":-5000,\"incidentDate\":\"2025-01-01T10:00:00Z\",\"incidentDescription\":\"Testing negative claim amount for field validation\"}"

fire "Proposal: nominee name too long (>100 chars)" \
  "$CAT_FIELDEDGE" 400 POST "$API/proposals" "$CUSTOMER_TOKEN" \
  "{\"customerId\":\"$CUSTOMER_USER_ID\",\"productId\":\"$PRODUCT_ID\",\"sumAssured\":500000,\"tenureYears\":10,\"premiumAmount\":5000,\"paymentFrequency\":\"Monthly\",\"nominees\":[{\"fullName\":\"$LONG_101\",\"relationship\":\"Spouse\",\"dateOfBirth\":\"1996-01-01\",\"sharePercentage\":100,\"isMinor\":false}]}"

# ── Idempotency ──────────────────────────────────────────────────────────────
echo ""
echo "▸ Idempotency"

fire_with_header "Idempotency: invalid key (not a UUID) on proposal submit" \
  "$CAT_IDEMPOTENCY" 400 POST "$API/proposals" "$CUSTOMER_TOKEN" \
  "Idempotency-Key: not-a-valid-uuid" \
  "{\"customerId\":\"$CUSTOMER_USER_ID\",\"productId\":\"$PRODUCT_ID\",\"sumAssured\":200000,\"tenureYears\":2,\"premiumAmount\":8000,\"paymentFrequency\":\"Monthly\"}"

fire_with_header "Idempotency: invalid key (not a UUID) on claim intimate" \
  "$CAT_IDEMPOTENCY" 400 POST "$API/claims/intimate" "$CUSTOMER_TOKEN" \
  "Idempotency-Key: abc-123" \
  "{\"policyId\":\"$POLICY_ID\",\"claimType\":\"Health\",\"claimAmountRequested\":10000,\"incidentDate\":\"2025-01-01T10:00:00Z\",\"incidentDescription\":\"Test\"}"

fire_with_header "Idempotency: invalid key (not a UUID) on grievance" \
  "$CAT_IDEMPOTENCY" 400 POST "$API/grievances" "$CUSTOMER_TOKEN" \
  "Idempotency-Key: bad-key" \
  "{\"policyId\":\"$POLICY_ID\",\"category\":\"ClaimDelay\",\"description\":\"Test grievance\"}"

fire_with_header "Idempotency: empty key — passes through to normal validation" \
  "$CAT_IDEMPOTENCY" 400 POST "$API/proposals" "$CUSTOMER_TOKEN" \
  "Idempotency-Key: " \
  "{}"

fire "Idempotency: no header on proposal submit — passes through normally" \
  "$CAT_IDEMPOTENCY" 400 POST "$API/proposals" "$CUSTOMER_TOKEN" \
  "{}"

fire_with_header "Idempotency: valid key on proposal submit — validation still runs" \
  "$CAT_IDEMPOTENCY" 400 POST "$API/proposals" "$CUSTOMER_TOKEN" \
  "Idempotency-Key: $(uuidgen | tr '[:upper:]' '[:lower:]')" \
  "{}"

echo ""

# ── Generate Report ─────────────────────────────────────────────────────────

SCENARIOS_JSON="$SCENARIOS" \
TOTAL_COUNT="$TOTAL" \
PASS_COUNT="$PASS" \
FAIL_COUNT="$FAIL" \
python3 << 'PYEOF' > "$REPORT"
import json, os

scenarios = json.loads(os.environ.get("SCENARIOS_JSON") or "[]")
total = int(os.environ["TOTAL_COUNT"])
passed = int(os.environ["PASS_COUNT"])
failed = int(os.environ["FAIL_COUNT"])

report = {
    "summary": {
        "total": total,
        "passed": passed,
        "failed": failed,
        "passRate": f"{(passed/total*100):.1f}%" if total > 0 else "0%"
    },
    "categories": {},
    "scenarios": scenarios
}
for s in scenarios:
    cat = s["category"]
    if cat not in report["categories"]:
        report["categories"][cat] = {"total": 0, "passed": 0, "failed": 0}
    report["categories"][cat]["total"] += 1
    if s["passed"]:
        report["categories"][cat]["passed"] += 1
    else:
        report["categories"][cat]["failed"] += 1
print(json.dumps(report, indent=2))
PYEOF

echo ""
echo "═══════════════════════════════════════════════════════════════════"
echo "  VALIDATION SCENARIO REPORT"
echo "═══════════════════════════════════════════════════════════════════"
echo "  Total:  $TOTAL"
echo "  Passed: $PASS"
echo "  Failed: $FAIL"
echo "  Rate:   $(python3 -c "print(f'{$PASS/$TOTAL*100:.1f}%')" 2>/dev/null || echo "N/A")"
echo ""
echo "  Report saved to: $REPORT"
echo "═══════════════════════════════════════════════════════════════════"
