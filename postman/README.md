# SpeedClaim — Postman Demo Runbook

This collection (`SpeedClaim.postman_collection.json`) walks every API endpoint as a
single coherent user story across 11 folders. Run the folders **top to bottom** — each
folder logs in as the role it needs and reuses the auth/state left by the previous one.

---

## 1. One-time setup (fresh database)

```bash
# From repo root. Connection string (speedclaimDB) is in appsettings.Development.json.

# a) Apply EF migrations (creates 41 tables + seeds admin/products/rate-tables)
dotnet ef database update --project backend/SpeedClaim.Api

# b) Seed demo data (users, policies, proposals, claims, notifications, …)
psql -h localhost -U postgres -d speedclaimDB -f seed.sql

# c) Run the API
dotnet run --project backend/SpeedClaim.Api      # http://localhost:5062
```

> The seed is written to layer cleanly on top of the migration seed
> (`insurance_products` uses `ON CONFLICT (id) DO NOTHING`), so the order above is safe.

## 2. Import into Postman

1. Import `postman/SpeedClaim.postman_collection.json`
2. Import `postman/SpeedClaim.postman_environment.json` and select it (top-right).
3. `baseUrl` defaults to `http://localhost:5062`.

Then just press **Send** down each folder, in order.

---

## 3. Seeded accounts (all password `Password@123`)

| Role | Email |
|---|---|
| Admin | davish2204@gmail.com |
| Underwriter | underwriter@speedclaim.com |
| Claims Officer | claimsofficer@speedclaim.com |
| Finance Officer | financeofficer@speedclaim.com |
| Agent (Rajesh) | davishthedelicious@gmail.com |
| Surveyor | surveyor@speedclaim.com |
| Customer 1 (Rahul) | davish.cs22@bitsathy.ac.in |
| Customer 2 (Priya) | davishoffl@gmail.com |

---

## 4. Steps that need a manual action

Almost every request runs unattended. These few need a human, by design:

| Folder / Request | What to do |
|---|---|
| **1 · Verify Email** | First run **Register New Customer** (sends a real verification email to `davish2204+democust@gmail.com` → arrives in `davish2204@gmail.com`). Copy the token from the email into the request body and Send. |
| **1 · Reset Password** | First run **Forgot Password** (emails a reset token to `davish.cs22@bitsathy.ac.in`). Copy the token into the request body and Send. |
| **8 · Pay Premium** / **Process Claim Payout** | Live Stripe **test-mode** calls. Optional but recommended: run `stripe listen --forward-to localhost:5062/api/v1/payments/webhook` so the webhook marks the payment **Paid**. The endpoints return 200 regardless. |
| **8 · Stripe Webhook** | Driven by the Stripe CLI, *not* manual clicks. Clicking it returns **400** on purpose (it rejects the unsigned payload — signature verification). The request's test asserts this 400 as the correct response. |

## 5. File uploads

Four requests upload PDFs from `/Users/davishe/Documents/dummy-docs/`
(`aadhar-dummy.pdf`, `income proof.pdf`, `dummy-document.pdf`). The paths are
pre-wired; if you move the files, re-select them in the request's form-data **file**
field.

---

## 6. Re-running

The story is designed for **one clean pass on a freshly seeded DB**. A few requests
create unique rows (register, create product, create branch) or consume one-time state
(approve the single pending commission, cancel a policy). To run again cleanly, reseed:

```bash
psql -h localhost -U postgres -d speedclaimDB \
  -c 'DROP SCHEMA public CASCADE; CREATE SCHEMA public;'
dotnet ef database update --project backend/SpeedClaim.Api
psql -h localhost -U postgres -d speedclaimDB -f seed.sql
```
