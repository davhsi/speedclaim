#!/usr/bin/env bash
# ============================================================================
# SpeedClaim — Comprehensive Validation Scenario Test Runner
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
PASSWORD="Password@123"

# Known IDs from environment
CUSTOMER_USER_ID="019ed3b7-7b51-704a-b36a-9a2f6e3a3fd8"
POLICY_ID="530ed510-3aaa-4897-9b74-eaddc37b971a"
CLAIM_ID="54a4764d-ce58-48d7-95ba-0d076ec56e66"
PROPOSAL_ID="019ed3b8-990e-773a-8a2d-a777a30a5601"
PRODUCT_ID="70000000-0000-0000-0000-000000000001"
SCHEDULE_ID="ee055aea-bee5-4562-9d8b-c9c9ce3c3cf1"
PAYMENT_ID="41667489-478d-4ace-ba5a-39d802b4162a"
GRIEVANCE_ID="61be6f1f-1cbe-4b22-9b28-faa057f52481"
MOTOR_CLAIM_ID="b6b93dcc-91d7-4368-b9e8-d10a746a81cd"
CANCELLABLE_POLICY_ID="c3bf257a-ce23-4050-9a9e-5b1aa4792c29"
FAKE_GUID="00000000-0000-0000-0000-000000000000"
TARGET_USER_ID="019ed3b8-84db-7b7d-9fce-e2e6aca7cf97"

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
  if [ "$actual_status" != "$expected_status" ]; then
    passed="false"
    FAIL=$((FAIL + 1))
  else
    PASS=$((PASS + 1))
  fi

  SCENARIOS=$(
    SCENARIOS_JSON="$SCENARIOS" \
    RESPONSE_BODY="$response_body" \
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

entry = {
    "id": int(os.environ["ENTRY_ID"]),
    "scenario": os.environ["ENTRY_SCENARIO"],
    "category": os.environ["ENTRY_CATEGORY"],
    "method": os.environ["ENTRY_METHOD"],
    "url": os.environ["ENTRY_URL"],
    "expectedStatus": int(os.environ["ENTRY_EXPECTED"]),
    "actualStatus": int(os.environ["ENTRY_ACTUAL"]),
    "passed": os.environ["ENTRY_PASSED"] == "true",
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
  local extra_args="${8:-}"

  local curl_args=(-s -w "\n%{http_code}" -X "$method" "$url")
  [ -n "$token" ] && curl_args+=(-H "Authorization: Bearer $token")

  if [ -n "$body" ]; then
    curl_args+=(-H "Content-Type: application/json" -d "$body")
  fi

  local raw
  raw=$(curl "${curl_args[@]}" 2>/dev/null || echo -e "\n000")
  local status_code="${raw##*$'\n'}"
  local response_body="${raw%$'\n'*}"

  record "$scenario" "$category" "$expected_status" "$status_code" "$response_body" "$method" "$url"
  printf "  [%s] #%-3d %-4s %-60s expected=%s got=%s\n" \
    "$([ "$status_code" = "$expected_status" ] && echo "PASS" || echo "FAIL")" \
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
  [ -n "$token" ] && curl_args+=(-H "Authorization: Bearer $token")
  curl_args+=("${form_args[@]}")

  local raw
  raw=$(curl "${curl_args[@]}" 2>/dev/null || echo -e "\n000")
  local status_code="${raw##*$'\n'}"
  local response_body="${raw%$'\n'*}"

  record "$scenario" "$category" "$expected_status" "$status_code" "$response_body" "$method" "$url"
  printf "  [%s] #%-3d %-4s %-60s expected=%s got=%s\n" \
    "$([ "$status_code" = "$expected_status" ] && echo "PASS" || echo "FAIL")" \
    "$TOTAL" "$method" "$scenario" "$expected_status" "$status_code"
}

# ── Login to get tokens ─────────────────────────────────────────────────────

echo "🔐 Logging in as all roles..."
ADMIN_TOKEN=$(login "$ADMIN_EMAIL")
CUSTOMER_TOKEN=$(login "$CUSTOMER_EMAIL")
AGENT_TOKEN=$(login "$AGENT_EMAIL")
UNDERWRITER_TOKEN=$(login "$UNDERWRITER_EMAIL")
CLAIMS_TOKEN=$(login "$CLAIMS_OFFICER_EMAIL")
FINANCE_TOKEN=$(login "$FINANCE_EMAIL")

if [ -z "$ADMIN_TOKEN" ]; then
  echo "❌ Failed to get admin token. Is the API running at $BASE_URL?"
  exit 1
fi
echo "✅ All tokens acquired."
echo ""
echo "⏳ Waiting 60s for auth rate-limit window to reset (10 req/60s)..."
sleep 62

# ============================================================================
# 1. AUTH — REGISTRATION VALIDATION
# ============================================================================
echo "━━━ 1. AUTH — Registration Validation ━━━"

fire "Register: empty body" \
  "Auth/Register" 400 POST "$API/auth/register" "" "{}"

fire "Register: invalid email format" \
  "Auth/Register" 400 POST "$API/auth/register" "" \
  '{"email":"not-an-email","password":"Password@123","firstName":"Test","lastName":"User","salutation":"Mr","phone":"9876543210","dateOfBirth":"1995-01-01","aadhaarNumber":"123456789012","panNumber":"ABCDE1234F","gender":"Male","maritalStatus":"Single","permanentAddress":{"line1":"123 St","city":"Delhi","state":"Delhi","postalCode":"110001","country":"India"},"isSameAsPermanent":true}'

fire "Register: weak password (no uppercase)" \
  "Auth/Register" 400 POST "$API/auth/register" "" \
  '{"email":"test@test.com","password":"password@123","firstName":"Test","lastName":"User","salutation":"Mr","phone":"9876543210","dateOfBirth":"1995-01-01","aadhaarNumber":"123456789012","panNumber":"ABCDE1234F","gender":"Male","maritalStatus":"Single","permanentAddress":{"line1":"123 St","city":"Delhi","state":"Delhi","postalCode":"110001","country":"India"},"isSameAsPermanent":true}'

fire "Register: weak password (too short)" \
  "Auth/Register" 400 POST "$API/auth/register" "" \
  '{"email":"test@test.com","password":"Pa@1","firstName":"Test","lastName":"User","salutation":"Mr","phone":"9876543210","dateOfBirth":"1995-01-01","aadhaarNumber":"123456789012","panNumber":"ABCDE1234F","gender":"Male","maritalStatus":"Single","permanentAddress":{"line1":"123 St","city":"Delhi","state":"Delhi","postalCode":"110001","country":"India"},"isSameAsPermanent":true}'

fire "Register: weak password (no special char)" \
  "Auth/Register" 400 POST "$API/auth/register" "" \
  '{"email":"test@test.com","password":"Password123","firstName":"Test","lastName":"User","salutation":"Mr","phone":"9876543210","dateOfBirth":"1995-01-01","aadhaarNumber":"123456789012","panNumber":"ABCDE1234F","gender":"Male","maritalStatus":"Single","permanentAddress":{"line1":"123 St","city":"Delhi","state":"Delhi","postalCode":"110001","country":"India"},"isSameAsPermanent":true}'

fire "Register: underage (< 18 years)" \
  "Auth/Register" 400 POST "$API/auth/register" "" \
  '{"email":"test@test.com","password":"Password@123","firstName":"Test","lastName":"User","salutation":"Mr","phone":"9876543210","dateOfBirth":"2015-01-01","aadhaarNumber":"123456789012","panNumber":"ABCDE1234F","gender":"Male","maritalStatus":"Single","permanentAddress":{"line1":"123 St","city":"Delhi","state":"Delhi","postalCode":"110001","country":"India"},"isSameAsPermanent":true}'

fire "Register: invalid Aadhaar (not 12 digits)" \
  "Auth/Register" 400 POST "$API/auth/register" "" \
  '{"email":"test@test.com","password":"Password@123","firstName":"Test","lastName":"User","salutation":"Mr","phone":"9876543210","dateOfBirth":"1995-01-01","aadhaarNumber":"12345","panNumber":"ABCDE1234F","gender":"Male","maritalStatus":"Single","permanentAddress":{"line1":"123 St","city":"Delhi","state":"Delhi","postalCode":"110001","country":"India"},"isSameAsPermanent":true}'

fire "Register: invalid PAN format" \
  "Auth/Register" 400 POST "$API/auth/register" "" \
  '{"email":"test@test.com","password":"Password@123","firstName":"Test","lastName":"User","salutation":"Mr","phone":"9876543210","dateOfBirth":"1995-01-01","aadhaarNumber":"123456789012","panNumber":"invalid","gender":"Male","maritalStatus":"Single","permanentAddress":{"line1":"123 St","city":"Delhi","state":"Delhi","postalCode":"110001","country":"India"},"isSameAsPermanent":true}'

fire "Register: invalid salutation" \
  "Auth/Register" 400 POST "$API/auth/register" "" \
  '{"email":"test@test.com","password":"Password@123","firstName":"Test","lastName":"User","salutation":"King","phone":"9876543210","dateOfBirth":"1995-01-01","aadhaarNumber":"123456789012","panNumber":"ABCDE1234F","gender":"Male","maritalStatus":"Single","permanentAddress":{"line1":"123 St","city":"Delhi","state":"Delhi","postalCode":"110001","country":"India"},"isSameAsPermanent":true}'

fire "Register: salutation-gender mismatch (Mrs with Male)" \
  "Auth/Register" 400 POST "$API/auth/register" "" \
  '{"email":"test@test.com","password":"Password@123","firstName":"Test","lastName":"User","salutation":"Mrs","phone":"9876543210","dateOfBirth":"1995-01-01","aadhaarNumber":"123456789012","panNumber":"ABCDE1234F","gender":"Male","maritalStatus":"Single","permanentAddress":{"line1":"123 St","city":"Delhi","state":"Delhi","postalCode":"110001","country":"India"},"isSameAsPermanent":true}'

# Rate-limit test: after 6 logins + 10 register requests = 16 auth calls, we're past the 10/min limit
fire "Register: rate limit triggered (429) — proves auth rate limiting works" \
  "Auth/RateLimit" 429 POST "$API/auth/register" "" \
  '{"email":"ratelimit@test.com","password":"Password@123","firstName":"Rate","lastName":"Limit","salutation":"Mr","phone":"9876543210","dateOfBirth":"1995-01-01","aadhaarNumber":"123456789012","panNumber":"ABCDE1234F","gender":"Male","maritalStatus":"Single","permanentAddress":{"line1":"123 St","city":"Delhi","state":"Delhi","postalCode":"110001","country":"India"},"isSameAsPermanent":true}'

echo ""
echo "⏳ Waiting 62s for auth rate-limit window to reset (batch 2)..."
sleep 62

# Registration batch 2 (after rate-limit window resets)
fire "Register: Mrs requires Married status" \
  "Auth/Register" 400 POST "$API/auth/register" "" \
  '{"email":"test@test.com","password":"Password@123","firstName":"Test","lastName":"User","salutation":"Mrs","phone":"9876543210","dateOfBirth":"1995-01-01","aadhaarNumber":"123456789012","panNumber":"ABCDE1234F","gender":"Female","maritalStatus":"Single","permanentAddress":{"line1":"123 St","city":"Delhi","state":"Delhi","postalCode":"110001","country":"India"},"isSameAsPermanent":true}'

fire "Register: missing current address when isSameAsPermanent=false" \
  "Auth/Register" 400 POST "$API/auth/register" "" \
  '{"email":"test@test.com","password":"Password@123","firstName":"Test","lastName":"User","salutation":"Mr","phone":"9876543210","dateOfBirth":"1995-01-01","aadhaarNumber":"123456789012","panNumber":"ABCDE1234F","gender":"Male","maritalStatus":"Single","permanentAddress":{"line1":"123 St","city":"Delhi","state":"Delhi","postalCode":"110001","country":"India"},"isSameAsPermanent":false}'

fire "Register: invalid postal code (not 6 digits)" \
  "Auth/Register" 400 POST "$API/auth/register" "" \
  '{"email":"test@test.com","password":"Password@123","firstName":"Test","lastName":"User","salutation":"Mr","phone":"9876543210","dateOfBirth":"1995-01-01","aadhaarNumber":"123456789012","panNumber":"ABCDE1234F","gender":"Male","maritalStatus":"Single","permanentAddress":{"line1":"123 St","city":"Delhi","state":"Delhi","postalCode":"123","country":"India"},"isSameAsPermanent":true}'

fire "Register: duplicate email (already exists)" \
  "Auth/Register" 409 POST "$API/auth/register" "" \
  "{\"email\":\"$CUSTOMER_EMAIL\",\"password\":\"Password@123\",\"firstName\":\"Test\",\"lastName\":\"User\",\"salutation\":\"Mr\",\"phone\":\"9999999999\",\"dateOfBirth\":\"1995-01-01\",\"aadhaarNumber\":\"123456789012\",\"panNumber\":\"ABCDE1234F\",\"gender\":\"Male\",\"maritalStatus\":\"Single\",\"permanentAddress\":{\"line1\":\"123 St\",\"city\":\"Delhi\",\"state\":\"Delhi\",\"postalCode\":\"110001\",\"country\":\"India\"},\"isSameAsPermanent\":true}"

fire "Register: invalid gender enum" \
  "Auth/Register" 400 POST "$API/auth/register" "" \
  '{"email":"test@test.com","password":"Password@123","firstName":"Test","lastName":"User","salutation":"Mr","phone":"9876543210","dateOfBirth":"1995-01-01","aadhaarNumber":"123456789012","panNumber":"ABCDE1234F","gender":"Alien","maritalStatus":"Single","permanentAddress":{"line1":"123 St","city":"Delhi","state":"Delhi","postalCode":"110001","country":"India"},"isSameAsPermanent":true}'

echo ""
echo "⏳ Waiting 62s for auth rate-limit window to reset..."
sleep 62

# ============================================================================
# 2. AUTH — LOGIN VALIDATION
# ============================================================================
echo "━━━ 2. AUTH — Login Validation ━━━"

fire "Login: empty body" \
  "Auth/Login" 400 POST "$API/auth/login" "" "{}"

fire "Login: invalid email format" \
  "Auth/Login" 400 POST "$API/auth/login" "" \
  '{"email":"not-an-email","password":"Password@123"}'

fire "Login: missing password" \
  "Auth/Login" 400 POST "$API/auth/login" "" \
  '{"email":"test@test.com","password":""}'

fire "Login: wrong password (invalid credentials)" \
  "Auth/Login" 400 POST "$API/auth/login" "" \
  "{\"email\":\"$CUSTOMER_EMAIL\",\"password\":\"WrongPassword@123\"}"

fire "Login: non-existent email" \
  "Auth/Login" 400 POST "$API/auth/login" "" \
  '{"email":"nonexistent@speedclaim.com","password":"Password@123"}'

echo ""
echo "⏳ Waiting 62s for auth rate-limit window to reset..."
sleep 62

# ============================================================================
# 3. AUTH — TOKEN VALIDATION
# ============================================================================
echo "━━━ 3. AUTH — Token Validation ━━━"

fire "Verify Email: empty token" \
  "Auth/Token" 400 POST "$API/auth/verify-email" "" \
  '{"token":""}'

fire "Verify Email: invalid/expired token" \
  "Auth/Token" 400 POST "$API/auth/verify-email" "" \
  '{"token":"invalid-fake-token-12345"}'

fire "Refresh Token: empty" \
  "Auth/Token" 400 POST "$API/auth/refresh" "" \
  '{"refreshToken":""}'

fire "Refresh Token: invalid format" \
  "Auth/Token" 400 POST "$API/auth/refresh" "" \
  '{"refreshToken":"not-a-valid-refresh-token"}'

fire "Forgot Password: empty email" \
  "Auth/Token" 400 POST "$API/auth/forgot-password" "" \
  '{"email":""}'

fire "Forgot Password: invalid email format" \
  "Auth/Token" 400 POST "$API/auth/forgot-password" "" \
  '{"email":"not-an-email"}'

fire "Reset Password: empty token" \
  "Auth/Token" 400 POST "$API/auth/reset-password" "" \
  '{"token":"","newPassword":"Password@123"}'

fire "Reset Password: weak new password" \
  "Auth/Token" 400 POST "$API/auth/reset-password" "" \
  '{"token":"some-token","newPassword":"weak"}'

fire "Reset Password: invalid token" \
  "Auth/Token" 400 POST "$API/auth/reset-password" "" \
  '{"token":"invalid-token-value","newPassword":"StrongPass@123"}'

echo ""

# ============================================================================
# 4. AUTH — AUTHORIZATION (401/403)
# ============================================================================
echo "━━━ 4. AUTH — Authorization (401/403) ━━━"

fire "No token: access protected endpoint" \
  "Auth/AuthZ" 401 GET "$API/users/profile" ""

fire "Invalid JWT: garbled token" \
  "Auth/AuthZ" 401 GET "$API/users/profile" "Bearer invalid.jwt.token"

fire "Customer accesses Admin endpoint (role mismatch)" \
  "Auth/AuthZ" 403 GET "$API/users/all" "$CUSTOMER_TOKEN"

fire "Customer accesses Agent endpoint" \
  "Auth/AuthZ" 403 GET "$API/agents/customers" "$CUSTOMER_TOKEN"

fire "Agent accesses Admin-only system configs" \
  "Auth/AuthZ" 403 GET "$API/system/configs" "$AGENT_TOKEN"

fire "Customer accesses Underwriter proposals" \
  "Auth/AuthZ" 403 GET "$API/proposals/all" "$CUSTOMER_TOKEN"

fire "Customer accesses ClaimsOfficer claims list" \
  "Auth/AuthZ" 403 GET "$API/claims/all" "$CUSTOMER_TOKEN"

fire "Customer accesses Finance endpoints" \
  "Auth/AuthZ" 403 GET "$API/payments/all-records" "$CUSTOMER_TOKEN"

fire "Agent accesses admin reset password" \
  "Auth/AuthZ" 403 POST "$API/auth/admin/reset-password/$TARGET_USER_ID" "$AGENT_TOKEN" \
  '{"newPassword":"NewPass@123"}'

fire "Customer accesses admin register-agent" \
  "Auth/AuthZ" 403 POST "$API/auth/admin/register-agent" "$CUSTOMER_TOKEN" \
  '{"email":"test@test.com","password":"Password@123","salutation":"Mr","firstName":"Test","lastName":"Agent","phone":"9876543210","licenseNumber":"LIC-001","agencyName":"Test","aadhaarNumber":"123456789012","panNumber":"ABCDE1234F","maritalStatus":"Single","permanentAddress":{"line1":"St","city":"City","state":"State","postalCode":"110001","country":"India"},"isSameAsPermanent":true}'

echo ""

# ============================================================================
# 5. AUTH — ADMIN OPERATIONS
# ============================================================================
echo "━━━ 5. AUTH — Admin Operations ━━━"

fire "Admin Reset Password: user not found" \
  "Auth/Admin" 404 POST "$API/auth/admin/reset-password/$FAKE_GUID" "$ADMIN_TOKEN" \
  '{"newPassword":"NewPass@123"}'

fire "Admin Reset Password: weak password" \
  "Auth/Admin" 400 POST "$API/auth/admin/reset-password/$TARGET_USER_ID" "$ADMIN_TOKEN" \
  '{"newPassword":"weak"}'

fire "Register Agent: duplicate email" \
  "Auth/Admin" 409 POST "$API/auth/admin/register-agent" "$ADMIN_TOKEN" \
  "{\"email\":\"$AGENT_EMAIL\",\"password\":\"Password@123\",\"salutation\":\"Mr\",\"firstName\":\"Dup\",\"lastName\":\"Agent\",\"phone\":\"9999999998\",\"licenseNumber\":\"LIC-DUP-001\",\"agencyName\":\"Dup Agency\",\"aadhaarNumber\":\"123456789012\",\"panNumber\":\"ABCDE1234F\",\"maritalStatus\":\"Single\",\"permanentAddress\":{\"line1\":\"St\",\"city\":\"City\",\"state\":\"State\",\"postalCode\":\"110001\",\"country\":\"India\"},\"isSameAsPermanent\":true}"

fire "Register Agent: empty body" \
  "Auth/Admin" 400 POST "$API/auth/admin/register-agent" "$ADMIN_TOKEN" "{}"

fire "Register Agent: invalid phone (not 10 digits)" \
  "Auth/Admin" 400 POST "$API/auth/admin/register-agent" "$ADMIN_TOKEN" \
  '{"email":"newagent@test.com","password":"Password@123","salutation":"Mr","firstName":"New","lastName":"Agent","phone":"123","licenseNumber":"LIC-NEW-001","agencyName":"New Agency","aadhaarNumber":"123456789012","panNumber":"ABCDE1234F","maritalStatus":"Single","permanentAddress":{"line1":"St","city":"City","state":"State","postalCode":"110001","country":"India"},"isSameAsPermanent":true}'

echo ""

# ============================================================================
# 6. USER — PROFILE & KYC VALIDATION
# ============================================================================
echo "━━━ 6. USER — Profile & KYC ━━━"

fire "Get Profile: wrong role (Admin is not Customer)" \
  "User/Profile" 403 GET "$API/users/profile" "$ADMIN_TOKEN"

fire "KYC Approve: user not found" \
  "User/KYC" 404 PUT "$API/users/$FAKE_GUID/kyc/review?isApproved=true&reason=ok" "$ADMIN_TOKEN"

fire "Change Role: invalid role" \
  "User/Admin" 400 PUT "$API/users/$CUSTOMER_USER_ID/role" "$ADMIN_TOKEN" \
  '"SuperAdmin"'

fire "Change Role: user not found" \
  "User/Admin" 404 PUT "$API/users/$FAKE_GUID/role" "$ADMIN_TOKEN" \
  '"Customer"'

fire "Activate User: user not found" \
  "User/Admin" 404 PUT "$API/users/$FAKE_GUID/status?isActive=true" "$ADMIN_TOKEN"

echo ""

# ============================================================================
# 7. USER — FAMILY MEMBERS VALIDATION
# ============================================================================
echo "━━━ 7. USER — Family Members ━━━"

fire "Add Family Member: empty body" \
  "User/Family" 400 POST "$API/users/family" "$CUSTOMER_TOKEN" "{}"

fire "Add Family Member: invalid gender enum" \
  "User/Family" 400 POST "$API/users/family" "$CUSTOMER_TOKEN" \
  '{"firstName":"Test","lastName":"Member","salutation":"Mr","gender":"Alien","relationship":"Spouse","dateOfBirth":"1995-01-01"}'

fire "Add Family Member: future DOB" \
  "User/Family" 400 POST "$API/users/family" "$CUSTOMER_TOKEN" \
  '{"firstName":"Test","lastName":"Member","salutation":"Mr","gender":"Male","relationship":"Spouse","dateOfBirth":"2030-01-01"}'

fire "Update Family Member: not found" \
  "User/Family" 404 PUT "$API/users/family/$FAKE_GUID" "$CUSTOMER_TOKEN" \
  '{"firstName":"Test","lastName":"Member","salutation":"Mr","gender":"Male","relationship":"Spouse","dateOfBirth":"1995-01-01"}'

fire "Delete Family Member: not found" \
  "User/Family" 404 DELETE "$API/users/family/$FAKE_GUID" "$CUSTOMER_TOKEN"

echo ""

# ============================================================================
# 8. USER — ADDRESS VALIDATION
# ============================================================================
echo "━━━ 8. USER — Address ━━━"

fire "Add Address: empty body" \
  "User/Address" 400 POST "$API/users/addresses" "$CUSTOMER_TOKEN" "{}"

fire "Add Address: invalid postal code" \
  "User/Address" 400 POST "$API/users/addresses" "$CUSTOMER_TOKEN" \
  '{"addressType":"Permanent","addressLine1":"123 St","city":"Delhi","state":"Delhi","postalCode":"123","country":"India"}'

fire "Add Address: missing required fields" \
  "User/Address" 400 POST "$API/users/addresses" "$CUSTOMER_TOKEN" \
  '{"addressType":"Permanent","addressLine1":"","city":"","state":"","postalCode":"","country":""}'

fire "Update Address: not found" \
  "User/Address" 404 PUT "$API/users/addresses/$FAKE_GUID" "$CUSTOMER_TOKEN" \
  '{"addressType":"Permanent","addressLine1":"123 St","city":"Delhi","state":"Delhi","postalCode":"110001","country":"India"}'

fire "Delete Address: not found" \
  "User/Address" 404 DELETE "$API/users/addresses/$FAKE_GUID" "$CUSTOMER_TOKEN"

echo ""

# ============================================================================
# 9. KYC — DOCUMENT UPLOAD VALIDATION
# ============================================================================
echo "━━━ 9. KYC — Document Upload ━━━"

fire_form "Aadhaar Upload: invalid number (not 12 digits)" \
  "KYC/Upload" 400 POST "$API/users/kyc/aadhaar" "$CUSTOMER_TOKEN" \
  -F "aadhaarNumber=12345" \
  -F "frontDocument=@/dev/null;filename=empty.pdf;type=application/pdf"

fire_form "PAN Upload: invalid format" \
  "KYC/Upload" 400 POST "$API/users/kyc/pan" "$CUSTOMER_TOKEN" \
  -F "panNumber=INVALID" \
  -F "frontDocument=@/dev/null;filename=empty.pdf;type=application/pdf"

echo ""

# ============================================================================
# 10. PRODUCT — VALIDATION
# ============================================================================
echo "━━━ 10. PRODUCT — Validation ━━━"

fire "Get Product: not found" \
  "Product" 404 GET "$API/products/$FAKE_GUID" "$ADMIN_TOKEN"

fire "Create Product: empty body" \
  "Product" 400 POST "$API/products" "$ADMIN_TOKEN" "{}"

fire "Create Product: invalid domain" \
  "Product" 400 POST "$API/products" "$ADMIN_TOKEN" \
  '{"productName":"Test","domain":"InvalidDomain","uin":"UIN-001","description":"A test product","minAge":18,"maxAge":65,"minSumAssured":100000,"maxSumAssured":5000000,"minTenureYears":1,"maxTenureYears":30,"waitingPeriodDays":30,"allowsFamilyFloater":false}'

fire "Create Product: maxAge < minAge" \
  "Product" 400 POST "$API/products" "$ADMIN_TOKEN" \
  '{"productName":"Test","domain":"Health","uin":"UIN-001","description":"A test product","minAge":65,"maxAge":18,"minSumAssured":100000,"maxSumAssured":5000000,"minTenureYears":1,"maxTenureYears":30,"waitingPeriodDays":30,"allowsFamilyFloater":false}'

fire "Create Product: maxSumAssured < minSumAssured" \
  "Product" 400 POST "$API/products" "$ADMIN_TOKEN" \
  '{"productName":"Test","domain":"Health","uin":"UIN-001","description":"A test product","minAge":18,"maxAge":65,"minSumAssured":5000000,"maxSumAssured":100000,"minTenureYears":1,"maxTenureYears":30,"waitingPeriodDays":30,"allowsFamilyFloater":false}'

fire "Create Product: family floater but maxFamilyMembers < 2" \
  "Product" 400 POST "$API/products" "$ADMIN_TOKEN" \
  '{"productName":"Test","domain":"Health","uin":"UIN-002","description":"A test product","minAge":18,"maxAge":65,"minSumAssured":100000,"maxSumAssured":5000000,"minTenureYears":1,"maxTenureYears":30,"waitingPeriodDays":30,"allowsFamilyFloater":true,"maxFamilyMembers":1}'

fire "Update Rates: product not found" \
  "Product" 404 PUT "$API/products/$FAKE_GUID/rates" "$ADMIN_TOKEN" \
  '{"rates":[]}'

fire "Toggle Status: product not found" \
  "Product" 404 PUT "$API/products/$FAKE_GUID/status" "$ADMIN_TOKEN" "true"

fire "Customer creating product (forbidden)" \
  "Product" 403 POST "$API/products" "$CUSTOMER_TOKEN" \
  '{"productName":"Hack","domain":"Health","uin":"UIN-H4CK","description":"Hacked product","minAge":18,"maxAge":65,"minSumAssured":100000,"maxSumAssured":5000000,"minTenureYears":1,"maxTenureYears":30,"waitingPeriodDays":30,"allowsFamilyFloater":false}'

echo ""

# ============================================================================
# 11. PROPOSAL — VALIDATION
# ============================================================================
echo "━━━ 11. PROPOSAL — Validation ━━━"

fire "Generate Quote: empty body" \
  "Proposal/Quote" 400 POST "$API/proposals/quote" "$CUSTOMER_TOKEN" "{}"

fire "Generate Quote: age out of range" \
  "Proposal/Quote" 400 POST "$API/proposals/quote" "$CUSTOMER_TOKEN" \
  "{\"productId\":\"$PRODUCT_ID\",\"age\":150,\"sumAssured\":500000,\"tenureYears\":10}"

fire "Generate Quote: zero sum assured" \
  "Proposal/Quote" 400 POST "$API/proposals/quote" "$CUSTOMER_TOKEN" \
  "{\"productId\":\"$PRODUCT_ID\",\"age\":30,\"sumAssured\":0,\"tenureYears\":10}"

fire "Generate Quote: product not found" \
  "Proposal/Quote" 404 POST "$API/proposals/quote" "$CUSTOMER_TOKEN" \
  "{\"productId\":\"$FAKE_GUID\",\"age\":30,\"sumAssured\":500000,\"tenureYears\":10}"

fire "Submit Proposal: empty body" \
  "Proposal/Submit" 400 POST "$API/proposals" "$CUSTOMER_TOKEN" "{}"

fire "Submit Proposal: invalid customer ID format" \
  "Proposal/Submit" 400 POST "$API/proposals" "$CUSTOMER_TOKEN" \
  '{"customerId":"not-a-guid","productId":"also-bad","sumAssured":500000,"tenureYears":10,"premiumAmount":5000,"paymentFrequency":"Monthly"}'

fire "Submit Proposal: invalid payment frequency" \
  "Proposal/Submit" 400 POST "$API/proposals" "$CUSTOMER_TOKEN" \
  "{\"customerId\":\"$CUSTOMER_USER_ID\",\"productId\":\"$PRODUCT_ID\",\"sumAssured\":500000,\"tenureYears\":10,\"premiumAmount\":5000,\"paymentFrequency\":\"Weekly\"}"

fire "Submit Proposal: nominee shares don't add to 100%" \
  "Proposal/Submit" 400 POST "$API/proposals" "$CUSTOMER_TOKEN" \
  "{\"customerId\":\"$CUSTOMER_USER_ID\",\"productId\":\"$PRODUCT_ID\",\"sumAssured\":500000,\"tenureYears\":10,\"premiumAmount\":5000,\"paymentFrequency\":\"Monthly\",\"nominees\":[{\"fullName\":\"Nom1\",\"relationship\":\"Spouse\",\"dateOfBirth\":\"1996-01-01\",\"sharePercentage\":30,\"isMinor\":false},{\"fullName\":\"Nom2\",\"relationship\":\"Child\",\"dateOfBirth\":\"2010-01-01\",\"sharePercentage\":30,\"isMinor\":true,\"appointeeName\":\"Guardian\"}]}"

fire "Submit Proposal: minor nominee without appointee" \
  "Proposal/Submit" 400 POST "$API/proposals" "$CUSTOMER_TOKEN" \
  "{\"customerId\":\"$CUSTOMER_USER_ID\",\"productId\":\"$PRODUCT_ID\",\"sumAssured\":500000,\"tenureYears\":10,\"premiumAmount\":5000,\"paymentFrequency\":\"Monthly\",\"nominees\":[{\"fullName\":\"ChildNom\",\"relationship\":\"Child\",\"dateOfBirth\":\"2015-01-01\",\"sharePercentage\":100,\"isMinor\":true}]}"

fire "Get Proposal: not found" \
  "Proposal" 404 GET "$API/proposals/$FAKE_GUID" "$CUSTOMER_TOKEN"

fire "Customer accessing other's proposal (forbidden)" \
  "Proposal" 403 GET "$API/proposals/$PROPOSAL_ID" "$FINANCE_TOKEN"

echo ""

echo "⏳ Waiting 62s for global rate-limit window to reset (100 req/60s)..."
sleep 62

# ============================================================================
# 12. POLICY — VALIDATION
# ============================================================================
echo "━━━ 12. POLICY — Validation ━━━"

fire "Get Policy: not found" \
  "Policy" 404 GET "$API/policies/$FAKE_GUID" "$CUSTOMER_TOKEN"

fire "Cancel Policy: not found" \
  "Policy" 404 PUT "$API/policies/$FAKE_GUID/cancel" "$CUSTOMER_TOKEN"

fire "Request Endorsement: empty body" \
  "Policy/Endorsement" 400 POST "$API/policies/$POLICY_ID/endorsements" "$CUSTOMER_TOKEN" "{}"

fire "Request Endorsement: description too short" \
  "Policy/Endorsement" 400 POST "$API/policies/$POLICY_ID/endorsements" "$CUSTOMER_TOKEN" \
  '{"endorsementType":"AddressChange","description":"Short"}'

fire "Request Endorsement: policy not found" \
  "Policy/Endorsement" 404 POST "$API/policies/$FAKE_GUID/endorsements" "$CUSTOMER_TOKEN" \
  '{"endorsementType":"AddressChange","description":"Please update my address to the new residential address in Mumbai"}'

fire "Get Policy Endorsements: access denied (not owner)" \
  "Policy/Endorsement" 403 GET "$API/policies/$FAKE_GUID/endorsements" "$CUSTOMER_TOKEN"

fire "Get Nominees: access denied (not owner)" \
  "Policy/Nominee" 403 GET "$API/policies/$FAKE_GUID/nominees" "$CUSTOMER_TOKEN"

fire "Update Nominee: not found" \
  "Policy/Nominee" 404 PUT "$API/policies/nominees/$FAKE_GUID" "$CUSTOMER_TOKEN" \
  '{"fullName":"Test","relationship":"Spouse","dateOfBirth":"1996-01-01","sharePercentage":100,"isMinor":false}'

fire "Update Nominee: share out of range" \
  "Policy/Nominee" 400 PUT "$API/policies/nominees/$FAKE_GUID" "$CUSTOMER_TOKEN" \
  '{"fullName":"Test","relationship":"Spouse","dateOfBirth":"1996-01-01","sharePercentage":150,"isMinor":false}'

fire "Update Nominee: minor without appointee" \
  "Policy/Nominee" 400 PUT "$API/policies/nominees/$FAKE_GUID" "$CUSTOMER_TOKEN" \
  '{"fullName":"ChildNom","relationship":"Child","dateOfBirth":"2015-01-01","sharePercentage":100,"isMinor":true}'

fire "Download Policy: not found" \
  "Policy" 404 GET "$API/policies/$FAKE_GUID/download" "$CUSTOMER_TOKEN"

fire "Approve Endorsement: not found" \
  "Policy/Endorsement" 404 PUT "$API/policies/endorsements/aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee/review" "$UNDERWRITER_TOKEN" \
  '{"isApproved":true,"reason":"Approved"}'

echo ""

# ============================================================================
# 13. CLAIMS — VALIDATION
# ============================================================================
echo "━━━ 13. CLAIMS — Validation ━━━"

fire "Intimate Claim: empty body" \
  "Claim/Intimate" 400 POST "$API/claims/intimate" "$CUSTOMER_TOKEN" "{}"

fire "Intimate Claim: zero amount" \
  "Claim/Intimate" 400 POST "$API/claims/intimate" "$CUSTOMER_TOKEN" \
  "{\"policyId\":\"$POLICY_ID\",\"claimType\":\"Health\",\"claimAmountRequested\":0,\"incidentDate\":\"2025-01-01T10:00:00Z\",\"incidentDescription\":\"Hospitalized for surgery at Apollo Hospital\"}"

fire "Intimate Claim: future incident date" \
  "Claim/Intimate" 400 POST "$API/claims/intimate" "$CUSTOMER_TOKEN" \
  "{\"policyId\":\"$POLICY_ID\",\"claimType\":\"Health\",\"claimAmountRequested\":50000,\"incidentDate\":\"2030-01-01T10:00:00Z\",\"incidentDescription\":\"Hospitalized for surgery at Apollo Hospital\"}"

fire "Intimate Claim: description too short" \
  "Claim/Intimate" 400 POST "$API/claims/intimate" "$CUSTOMER_TOKEN" \
  "{\"policyId\":\"$POLICY_ID\",\"claimType\":\"Health\",\"claimAmountRequested\":50000,\"incidentDate\":\"2025-01-01T10:00:00Z\",\"incidentDescription\":\"Short\"}"

fire "Intimate Claim: policy not found (non-existent GUID)" \
  "Claim/Intimate" 404 POST "$API/claims/intimate" "$CUSTOMER_TOKEN" \
  "{\"policyId\":\"aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee\",\"claimType\":2,\"claimAmountRequested\":50000,\"incidentDate\":\"2025-01-01T10:00:00Z\",\"incidentDescription\":\"Hospitalized for surgery at Apollo Hospital\"}"

fire "Intimate Claim: invalid claim type" \
  "Claim/Intimate" 400 POST "$API/claims/intimate" "$CUSTOMER_TOKEN" \
  "{\"policyId\":\"$POLICY_ID\",\"claimType\":\"InvalidType\",\"claimAmountRequested\":50000,\"incidentDate\":\"2025-01-01T10:00:00Z\",\"incidentDescription\":\"Hospitalized for surgery at Apollo Hospital\"}"

fire "Get Claim: not found" \
  "Claim" 404 GET "$API/claims/$FAKE_GUID" "$CUSTOMER_TOKEN"

fire "Get Claim History: not found" \
  "Claim" 404 GET "$API/claims/$FAKE_GUID/history" "$CUSTOMER_TOKEN"

fire "Update Claim Status: invalid status value" \
  "Claim/Status" 400 PUT "$API/claims/$CLAIM_ID/status" "$CLAIMS_TOKEN" \
  '{"status":"InvalidStatus","remarks":"Testing invalid status transition"}'

fire "Update Claim Status: empty remarks" \
  "Claim/Status" 400 PUT "$API/claims/$CLAIM_ID/status" "$CLAIMS_TOKEN" \
  '{"status":"UnderReview","remarks":""}'

fire "Update Claim Status: claim not found" \
  "Claim/Status" 404 PUT "$API/claims/$FAKE_GUID/status" "$CLAIMS_TOKEN" \
  '{"status":"UnderReview","remarks":"Reviewing the claim documentation"}'

fire "Approve Claim: reject without reason" \
  "Claim/Approve" 400 PUT "$API/claims/$CLAIM_ID/approve" "$CLAIMS_TOKEN" \
  '{"isApproved":false,"reason":""}'

fire "Approve Claim: reject reason too short" \
  "Claim/Approve" 400 PUT "$API/claims/$CLAIM_ID/approve" "$CLAIMS_TOKEN" \
  '{"isApproved":false,"reason":"No"}'

fire "Approve Claim: approve without amount" \
  "Claim/Approve" 400 PUT "$API/claims/$CLAIM_ID/approve" "$CLAIMS_TOKEN" \
  '{"isApproved":true}'

fire "Approve Claim: claim not found" \
  "Claim/Approve" 404 PUT "$API/claims/aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee/approve" "$CLAIMS_TOKEN" \
  '{"isApproved":true,"approvedAmount":50000,"reason":"Approved after review"}'

fire "Assign Surveyor: empty notes" \
  "Claim/Surveyor" 400 PUT "$API/claims/$MOTOR_CLAIM_ID/assign-surveyor" "$CLAIMS_TOKEN" \
  '{"surveyorId":"5aabbc9f-0525-4a86-a45c-26cd3d72ff38","notes":""}'

fire "Assign Surveyor: claim not found" \
  "Claim/Surveyor" 404 PUT "$API/claims/$FAKE_GUID/assign-surveyor" "$CLAIMS_TOKEN" \
  '{"surveyorId":"5aabbc9f-0525-4a86-a45c-26cd3d72ff38","notes":"Please inspect the vehicle damage"}'

fire "Settle Claim: not found" \
  "Claim/Settle" 404 PUT "$API/claims/$FAKE_GUID/settle" "$CLAIMS_TOKEN"

echo ""

# ============================================================================
# 14. GRIEVANCE — VALIDATION
# ============================================================================
echo "━━━ 14. GRIEVANCE — Validation ━━━"

fire "Raise Grievance: empty body" \
  "Grievance" 400 POST "$API/grievances" "$CUSTOMER_TOKEN" "{}"

fire "Raise Grievance: description too short" \
  "Grievance" 400 POST "$API/grievances" "$CUSTOMER_TOKEN" \
  '{"category":"ClaimDelay","description":"Short"}'

fire "Raise Grievance: invalid category" \
  "Grievance" 400 POST "$API/grievances" "$CUSTOMER_TOKEN" \
  '{"category":"InvalidCategory","description":"This is a detailed grievance description for testing"}'

fire "Get Grievance: not found" \
  "Grievance" 404 GET "$API/grievances/$FAKE_GUID" "$CLAIMS_TOKEN"

fire "Update Grievance Status: not found" \
  "Grievance" 404 PUT "$API/grievances/$FAKE_GUID/status" "$CLAIMS_TOKEN" \
  '{"status":"InProgress"}'

fire "Update Grievance Status: resolve without notes" \
  "Grievance" 400 PUT "$API/grievances/$GRIEVANCE_ID/status" "$CLAIMS_TOKEN" \
  '{"status":"Resolved","resolutionNotes":""}'

fire "Assign Grievance: not found" \
  "Grievance" 404 PUT "$API/grievances/$FAKE_GUID/assign" "$CLAIMS_TOKEN" \
  '{"assignedToId":"019ed3b7-3e38-7abf-9ee6-275fb145387a"}'

echo ""

# ============================================================================
# 15. PAYMENTS — VALIDATION
# ============================================================================
echo "━━━ 15. PAYMENTS — Validation ━━━"

fire "Pay Premium: schedule not found" \
  "Payment" 404 POST "$API/payments/pay/$FAKE_GUID" "$CUSTOMER_TOKEN" \
  '{"paymentMethodId":"pm_card_visa"}'

fire "Get Schedule: invalid policy ID" \
  "Payment" 400 GET "$API/payments/schedule/not-a-guid" "$CUSTOMER_TOKEN"

fire "Download Receipt: not found" \
  "Payment" 404 GET "$API/payments/$FAKE_GUID/receipt" "$CUSTOMER_TOKEN"

fire "Reconcile Payment: not found" \
  "Payment" 404 PUT "$API/payments/$FAKE_GUID/reconcile" "$FINANCE_TOKEN"

fire "Refund Payment: not found" \
  "Payment" 404 POST "$API/payments/$FAKE_GUID/refund" "$FINANCE_TOKEN"

fire "Process Payout: claim not found" \
  "Payment" 404 POST "$API/payments/payout/claim/$FAKE_GUID" "$FINANCE_TOKEN"

fire "Mark Claim Settled: not found" \
  "Payment" 404 PUT "$API/payments/claims/$FAKE_GUID/settle" "$FINANCE_TOKEN"

fire "Approve Commission: not found" \
  "Payment" 404 POST "$API/payments/commissions/$FAKE_GUID/approve" "$FINANCE_TOKEN"

fire "Customer accessing finance reports (forbidden)" \
  "Payment" 403 GET "$API/payments/reports/overdue" "$CUSTOMER_TOKEN"

fire "Customer accessing reconcile (forbidden)" \
  "Payment" 403 PUT "$API/payments/$PAYMENT_ID/reconcile" "$CUSTOMER_TOKEN"

echo ""

# ============================================================================
# 16. AGENT — VALIDATION
# ============================================================================
echo "━━━ 16. AGENT — Validation ━━━"

fire "Agent Profile Update: empty body" \
  "Agent" 400 PUT "$API/agents/profile" "$AGENT_TOKEN" "{}"

fire "Agent Profile Update: invalid phone" \
  "Agent" 400 PUT "$API/agents/profile" "$AGENT_TOKEN" \
  '{"firstName":"Test","lastName":"Agent","salutation":"Mr","phone":"123"}'

fire "Create Branch: empty body" \
  "Agent/Branch" 400 POST "$API/agents/branches" "$ADMIN_TOKEN" "{}"

fire "Create Branch: invalid email" \
  "Agent/Branch" 400 POST "$API/agents/branches" "$ADMIN_TOKEN" \
  '{"name":"Test Branch","city":"Delhi","state":"Delhi","address":"123 St","phone":"9876543210","email":"not-an-email"}'

fire "Assign Agent to Branch: agent not found" \
  "Agent/Branch" 404 PUT "$API/agents/$FAKE_GUID/branch/$FAKE_GUID" "$ADMIN_TOKEN"

fire "Update Agent License: agent not found" \
  "Agent/License" 404 PUT "$API/agents/$FAKE_GUID/license" "$ADMIN_TOKEN" \
  '{"licenseNumber":"LIC-001","licenseExpiry":"2027-01-01"}'

fire "Update Agent License: expiry in the past" \
  "Agent/License" 400 PUT "$API/agents/019ed3b7-78f2-7cbc-a7d2-db3f3f45321b/license" "$ADMIN_TOKEN" \
  '{"licenseNumber":"LIC-001","licenseExpiry":"2020-01-01"}'

fire "Activate Agent: not found" \
  "Agent" 404 PUT "$API/agents/$FAKE_GUID/status?isActive=true" "$ADMIN_TOKEN"

echo ""

# ============================================================================
# 17. SYSTEM — VALIDATION
# ============================================================================
echo "━━━ 17. SYSTEM — Validation ━━━"

fire "Update Config: empty body" \
  "System" 400 PUT "$API/system/configs" "$ADMIN_TOKEN" "{}"

fire "Update Config: key too long (> 100 chars)" \
  "System" 400 PUT "$API/system/configs" "$ADMIN_TOKEN" \
  "{\"configKey\":\"$(python3 -c "print('A'*101)")\",\"configValue\":\"test\"}"

fire "Email Template: empty body" \
  "System" 400 PUT "$API/system/email-templates" "$ADMIN_TOKEN" "{}"

fire "Email Template: missing subject" \
  "System" 400 PUT "$API/system/email-templates" "$ADMIN_TOKEN" \
  '{"templateKey":"test_template","subject":"","bodyHtml":"<p>Test</p>"}'

echo ""

# ============================================================================
# 18. NOTIFICATION — VALIDATION
# ============================================================================
echo "━━━ 18. NOTIFICATION — Validation ━━━"

fire "Mark Notification Read: not found" \
  "Notification" 404 PATCH "$API/users/notifications/$FAKE_GUID/read" "$CUSTOMER_TOKEN"

echo ""

# ============================================================================
# 19. STRIPE WEBHOOK — VALIDATION
# ============================================================================
echo "━━━ 19. STRIPE WEBHOOK ━━━"

fire "Stripe Webhook: invalid signature" \
  "Webhook" 400 POST "$API/payments/webhook" "" \
  '{"type":"payment_intent.succeeded"}'

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
