# SpeedClaim — Frontend Design Brief

> A comprehensive design brief for **Claude Design** (claude.ai/design). It covers the product, all seven role-based portals, every page, the design system direction, key flows, and the API/role constraints the UI must respect. The backend is a live .NET 10 Web API; this brief drives the Angular frontend in `frontend/`.

---

## 0. How to use this brief with Claude Design (quick start)

Claude Design works best when you (a) give it a design-system foundation, (b) answer its upfront UX interview, then (c) move wireframes → high-fidelity → Claude Code handoff. Suggested path for this project:

1. **Set up the design system first.** Point Claude Design at this repo (codebase reference) and/or paste §3–§4 below. If you have no brand assets, let it generate a system from §3's direction. The system becomes CSS tokens + a component library reused across every screen.
2. **Create the project and paste this brief.** Start with §1–§2 and §9 so it understands the product, the seven users, and the hard constraints. It will ask clarifying UX questions — §10 pre-answers the common ones, so paste that too.
3. **Generate wireframes per portal.** Don't ask for all 110 screens at once. Go portal-by-portal (start with Auth + Customer, §7.1–§7.2). Request 2–3 design directions and pick one.
4. **Promote to high-fidelity.** Reference the chosen wireframes + attach the design system; ask for polished, responsive screens. Re-use the status-badge and table patterns (§5) everywhere for consistency.
5. **Iterate with inline comments.** Comment directly on elements ("make this status badge use the semantic color from the system") rather than re-prompting from scratch — it's cheaper on usage and more precise.
6. **Export the bundle to Claude Code** to scaffold the actual Angular components wired to the `/api/v1` endpoints listed per page.

**Tips:** keep the design system lightweight to start (saves usage credits); use a simpler model for small tweaks; engage with its UX questions — they measurably improve output.

---

## 1. Product overview

**SpeedClaim** is a full-stack insurance claims-management platform. It lets customers buy and manage insurance policies (Health, Motor, Life), pay premiums, and file & track claims — while internal staff underwrite proposals, process claims, handle finance/payouts, survey motor claims, and administer the system.

- **Domain:** Indian insurance (₹ INR currency, Aadhaar/PAN KYC, IRDAI-style roles, DPDP consent).
- **Backend:** .NET 10 Web API, PostgreSQL, JWT auth, Stripe payments, email notifications. All routes versioned under `/api/v1`.
- **Frontend (to design):** Angular SPA, role-aware, talking to that API.
- **Tone:** trustworthy, calm, professional. This is money and health — clarity and confidence beat flashiness.

---

## 2. Users & personas (7 roles)

Auth is JWT with role claims; the UI must render a different portal per role. Roles: `Customer`, `Agent`, `Underwriter`, `ClaimsOfficer`, `FinanceOfficer`, `Surveyor`, `Admin`.

| Persona | Role | Goal | Mindset | Primary screens |
|---|---|---|---|---|
| **Priya** — policyholder | Customer | Buy cover, pay premiums, file & track claims | Anxious, non-expert, on mobile | Dashboard, products, quote, proposal, policies, pay, claims, grievances |
| **Arjun** — sales agent | Agent | Sell to assigned customers, hit renewals | Targets-driven, efficiency | Dashboard, my customers, submit proposal, renewals, commissions (view) |
| **Meera** — underwriter | Underwriter | Approve/reject proposals & KYC, manage risk | Detail-oriented, risk-averse | Proposal queue, KYC queue, endorsement queue, all policies |
| **Karthik** — claims officer | ClaimsOfficer | Move claims through to settlement fairly & fast | Investigative, process-driven | Claims queue, claim detail, assign surveyor, approvals, grievances |
| **Divya** — finance officer | FinanceOfficer | Reconcile payments, pay claims & commissions, report | Precise, audit-conscious | Payment records, payouts, commissions, reports/exports |
| **Rahul** — surveyor | Surveyor | Inspect motor claims, file survey reports | Field worker, mobile-first | Assigned claims, survey report upload |
| **Admin** — system admin | Admin | Manage users, agents, products, system config | Power user, oversight | User/agent mgmt, product catalog, system config, audit logs |

**Design implication:** Customer & Surveyor portals are **mobile-first and reassuring**; Underwriter/Claims/Finance/Admin are **data-dense desktop workspaces** (tables, queues, filters, detail panes).

---

## 3. Design principles & brand direction

1. **Trust through clarity.** Plain language, obvious status, no jargon walls. Every policy/claim/payment shows its state at a glance.
2. **Status-first.** The single most important UI primitive is the **status badge** (see §4). Policies, claims, proposals, payments, endorsements, grievances all have lifecycle states.
3. **Guided journeys.** Multi-step flows (quote → proposal → policy → pay; intimate → documents → track) use steppers/progress so users always know where they are and what's next.
4. **Role-appropriate density.** Customers get cards & generous spacing; staff get tables, bulk actions, and keyboard-friendly queues.
5. **Honest empty/loading/error states.** Insurance has lots of "nothing here yet" (no claims, KYC pending). Design these deliberately.

**Brand direction:** confident and institutional but warm. Primary a trustworthy blue/teal; a single warm accent for primary CTAs. Rounded-but-not-playful corners, soft elevation, strong typographic hierarchy. (Let Claude Design finalize tokens from your codebase/brand; §4 is a concrete starting point.)

---

## 4. Design system / tokens (starting point)

**Color**
- Primary: deep teal-blue `#0F6E8C` (brand, nav, primary buttons)
- Primary-dark / hover: `#0A5469`
- Accent (CTA): `#F2784B` (warm coral — "Get a quote", "Pay now")
- Neutrals: ink `#1A2230`, body `#3C4654`, muted `#6B7685`, line `#E2E6EB`, surface `#F7F9FA`, white `#FFFFFF`
- **Semantic status palette** (used by all badges):
  - Success/Active/Approved/Paid/Settled → green `#1F9D6B`
  - Pending/Intimated/Requested/UnderReview → amber `#D9920A`
  - Danger/Rejected/Failed/Overdue/Cancelled → red `#D14343`
  - Info/Assigned/InProgress → blue `#2D7FF9`
  - Neutral/Draft/Closed → grey `#6B7685`

**Typography:** Inter (or system sans). Scale: Display 32/40, H1 24/32, H2 20/28, H3 16/24, Body 14/22, Caption 12/16. Tabular numerals for amounts.

**Spacing:** 4px base scale (4/8/12/16/24/32/48). Card radius 12px, control radius 8px, badge radius full.

**Core components to define once and reuse:**
- Status badge (driven by semantic palette + a status→color map)
- Data table (sortable, paged, filter bar, row actions, empty state)
- Stat/summary card (dashboards)
- Stepper / progress (multi-step flows)
- Money display (₹, tabular, 2 decimals)
- File upload (drag-drop, multipart, file-type/size hints) — used by KYC, proposal docs, claim docs, survey report
- Detail layout (header + status + key facts + tabbed sub-sections + timeline)
- Timeline/history (status histories for policy & claim)
- Confirmation dialog (cancel policy, approve/reject, refund — destructive/irreversible)
- Toast/notification + a notifications dropdown (unread count)
- Role-aware app shell (sidebar nav that changes per role)

---

## 5. Global patterns

- **App shell:** left sidebar (role-specific items) + top bar (logo, search where relevant, notifications bell with unread count, profile/role menu, logout). Customer/Surveyor collapse to a mobile bottom-nav / hamburger.
- **Auth gating:** unauthenticated → auth pages only. Access token in memory, silent refresh via `/auth/refresh`; on 401 attempt refresh then bounce to login. Hide nav items the role can't access (don't just rely on API 403).
- **Status badge map:** every lifecycle field renders through the §4 semantic palette. Keep one shared mapping.
- **States:** design **loading** (skeletons for tables/cards), **empty** (friendly illustration + primary action), and **error** (the API returns RFC-9110 problem-details JSON with `errors` — surface field errors inline on forms, general errors as toasts).
- **Money & dates:** ₹ with tabular numerals; dates as `dd MMM yyyy`; date-only fields (DOB, policy start/end) have no time component.
- **Responsive:** Customer & Surveyor mobile-first; staff portals desktop-first but must not break on tablet.
- **Accessibility:** WCAG AA contrast, focus rings, keyboard nav for tables/queues, labels on every field.

---

## 6. Information architecture (route map)

```
/auth        login · register · verify-email · forgot-password · reset-password
/app (authenticated shell, nav varies by role)
  Customer       /dashboard /products /products/:id /quote /proposals /proposals/:id
                 /policies /policies/:id /pay/:policyId /payments /claims
                 /claims/new /claims/:id /profile /family /kyc /notifications /grievances
  Agent          /agent/dashboard /agent/customers /agent/proposals /agent/proposals/new
                 /agent/policies /agent/renewals /agent/profile
  Underwriter    /uw/proposals /uw/proposals/:id /uw/kyc /uw/endorsements /uw/policies
  ClaimsOfficer  /claims/queue /claims/:id /co/grievances
  FinanceOfficer /fin/payments /fin/payouts /fin/commissions /fin/reports
  Surveyor       /surveyor/claims /surveyor/claims/:id
  Admin          /admin/users /admin/agents /admin/products /admin/system
```

---

## 7. Page-by-page specs

Each page lists: **purpose**, **key components/data**, and **actions → endpoint**. Endpoints are the live `/api/v1` routes (see `docs/api-page-map.md` for the full table). Design loading/empty/error states for every list and detail.

### 7.1 Auth
- **Login** — email/password card; "forgot password" link; link to register. → `POST /auth/login`, silent `POST /auth/refresh`. Show lockout message after 5 failed attempts (account locks 15 min).
- **Register (customer)** — multi-field form (name, email, phone, password) + **DPDP consent checkboxes** (data processing + KYC). → `POST /auth/register`. On success → verify-email notice.
- **Verify email** — token-entry / confirmation state. → `POST /auth/verify-email`.
- **Forgot / Reset password** — request link → confirmation; reset form with new password. → `POST /auth/forgot-password`, `POST /auth/reset-password`.
- **Logout** — in profile menu. → `POST /auth/logout`.

### 7.2 Customer portal
- **Dashboard** — summary cards (active policies, open claims, next premium due, unread notifications); quick actions (Get a quote, File a claim). → `GET /policies/my`, `GET /claims/my`, `GET /payments/schedule/{policyId}`, `GET /users/notifications`.
- **Browse products** — grid of product cards (Health/Motor/Life) with domain filter; card → detail. → `GET /products`.
- **Product detail** — features, sum-assured range, document requirements, "Get a quote" CTA. → `GET /products/{id}`, `GET /products/{id}/documents`.
- **Get a quote** — dynamic form (varies by product domain: motor/health/life fields); returns premium. → `POST /proposals/quote`. Show computed premium prominently, then "Apply".
- **Submit proposal** — form prefilled from quote; add proposal members (for family floater); document upload step. → `POST /proposals`, then `PUT /proposals/{id}/documents/{documentKey}` (multipart).
- **My proposals** — list with status badge (Submitted/UnderReview/Approved/Rejected/DocsRequested); → detail. → `GET /proposals/my`, `GET /proposals/{id}`.
- **My policies** — card/list with status (Active/Pending/Cancelled); → detail. → `GET /policies/my`, `GET /policies/{id}`.
- **Policy detail** — header (policy no., status, type, sum assured, premium, validity), tabs: Overview, Nominees, Endorsements, History. Actions: download certificate (**plain-text `.txt`**), request endorsement, manage nominees, cancel policy (confirm dialog). → `GET /policies/{id}/download`, `GET /policies/{id}/history`, `GET/POST /policies/{id}/endorsements`, `GET /policies/{id}/nominees`, `PUT /policies/nominees/{nomineeId}`, `PUT /policies/{id}/cancel`.
- **Pay premium** — show schedule + amount due; "Pay now" launches **Stripe** (PaymentIntent → confirm on frontend with Stripe.js using `clientSecret`; policy activates after `payment_intent.succeeded` webhook). → `GET /payments/schedule/{policyId}`, `POST /payments/pay/{scheduleId}`, `GET /payments/methods`. Design states: pending → processing → paid.
- **Payment history** — table (date, type, amount, status, receipt link). → `GET /payments/history`, `GET /payments/{paymentId}/receipt`.
- **File a claim** — guided: select policy → claim details (incident date/description, amount, cashless toggle) → upload documents. → `POST /claims/intimate`, `PUT /claims/{id}/documents/{documentKey}`.
- **My claims** — list with status (Intimated/Assigned/UnderReview/Approved/Rejected/Settled), filters; → detail with **status timeline**. → `GET /claims/my`, `GET /claims/{id}`, `GET /claims/{id}/history`.
- **Profile** — view/edit personal info; addresses CRUD. → `GET/PUT /users/profile`, `POST/PUT/DELETE /users/addresses[/{id}]`.
- **Family members** — list/add/edit/delete dependents (for floater & member-level claims). → `GET/POST /users/family`, `PUT/DELETE /users/family/{memberId}`.
- **KYC** — show status (Pending/Approved/Rejected) + masked ID; upload Aadhaar/PAN (front/back, multipart). → `GET /users/kyc`, `POST /users/kyc`.
- **Notifications** — list with read/unread; mark one / all read. → `GET /users/notifications`, `PATCH /users/notifications/{id}/read`, `PATCH /users/notifications/read-all`.
- **Grievances** — raise + list + detail/status. → `POST /grievances`, `GET /grievances/my`, `GET /grievances/{id}`.

### 7.3 Agent portal
- **Dashboard** — KPI cards (assigned customers, proposals in flight, upcoming renewals, commission summary). → `GET /agents/dashboard`.
- **My customers** — table of assigned customers. → `GET /agents/customers`.
- **Submit proposal (for customer)** — same quote→proposal→docs flow as customer, on behalf of a customer. → `POST /proposals/quote`, `POST /proposals`, `GET /proposals/my`, `GET /proposals/{id}`, `PUT /proposals/{id}/documents/{documentKey}`. (Agents may also upload KYC: `POST /users/kyc`.)
- **Customer policies** — policies for assigned customers. → `GET /policies/assigned`.
- **Renewals** — upcoming renewals list with reminder actions. → `GET /agents/renewals`.
- **Agent profile** — view/edit own profile. → `GET/PUT /agents/profile`.

### 7.4 Underwriter portal
- **Proposal review queue** — table (applicant, product, sum assured, status, submitted date); → detail with members, documents, risk facts. Actions: approve/reject, request docs, add notes. → `GET /proposals/all`, `GET /proposals/{id}`, `POST /proposals/{id}/review`, `POST /proposals/{id}/request-docs`, `PUT /proposals/{id}/notes`.
- **KYC review queue** — pending KYC list (paged); view decrypted ID (admin/UW), approve/reject with reason. → `GET /users/kyc/pending`, `PUT /users/{customerId}/kyc/review`.
- **Endorsement review queue** — pending endorsements; approve/reject. → `GET /policies/endorsements/pending`, `PUT /policies/endorsements/{endorsementId}/review`.
- **All policies** — searchable/paged policy list + detail + history. → `GET /policies/all`, `GET /policies/{id}`, `GET /policies/{id}/history`.

### 7.5 Claims officer portal
- **Claims queue** — table (claim no., type, requested amt, status, cashless flag, intimation date) with status/type filters + paging. → `GET /claims/all`.
- **Claim detail** — header + status timeline; sub-panels for type-specific details (motor/health/life), documents, surveyor report. Action rail: assign to self, update status, approve/reject (with approved amount or reason), assign surveyor, request docs, approve cashless pre-auth, mark settled. → `GET /claims/{id}`, `GET /claims/{id}/history`, `PUT /claims/{id}/assign`, `PUT /claims/{id}/status`, `PUT /claims/{id}/approve`, `PUT /claims/{id}/settle`, `PUT /claims/{id}/assign-surveyor`, `POST /claims/{id}/request-docs`, `PUT /claims/{id}/approve-preauth`, `POST /claims/{id}/survey-report`.
- **Grievances** — all grievances; assign + update status. → `GET /grievances/all`, `GET /grievances/{id}`, `PUT /grievances/{id}/assign`, `PUT /grievances/{id}/status`.

### 7.6 Finance officer portal
- **Payment records** — all payments table; manual reconcile (fallback to webhook); refund (confirm dialog). → `GET /payments/all-records`, `PUT /payments/{paymentId}/reconcile`, `POST /payments/{paymentId}/refund`.
- **Claim payouts** — process Stripe payout for approved claims; mark financially settled. → `POST /payments/payout/claim/{claimId}`, `PUT /payments/claims/{claimId}/settle`.
- **Commissions** — pending commissions; approve & pay. → `GET /payments/commissions/pending`, `POST /payments/commissions/{id}/approve`.
- **Reports** — overdue policies; collection summary (period selector); **export to Excel** (binary `.xlsx` download). → `GET /payments/reports/overdue`, `GET /payments/reports/summary`, `GET /payments/reports/export`.

### 7.7 Surveyor portal (mobile-first)
- **Assigned claims** — list of motor claims to inspect. → `GET /claims/surveyor/assigned`.
- **Survey report** — claim context + report form with photo/document upload (multipart). → `POST /claims/{id}/survey-report`.

### 7.8 Admin portal
- **User management** — all users (paged); change role, activate/deactivate, reset password, view all active sessions. → `GET /users/all`, `PUT /users/{userId}/role`, `PUT /users/{userId}/status`, `POST /auth/admin/reset-password/{userId}`, `GET /users/sessions`.
- **Agent management** — register agent; branches list/create; assign agent↔branch; update license; activate/deactivate. → `POST /auth/admin/register-agent`, `GET/POST /agents/branches`, `PUT /agents/{agentId}/branch/{branchId}`, `PUT /agents/{agentId}/license`, `PUT /agents/{agentId}/status`.
- **Product catalog** — list/create products; update premium rate table; configure document requirements; toggle active. → `GET/POST /products`, `PUT /products/{id}/rates`, `GET/PUT /products/{id}/documents`, `PUT /products/{id}/status`.
- **System** — system configs (key/value); audit logs; notification/email logs; manage email templates. → `GET/PUT /system/configs`, `GET /system/audit-logs`, `GET /system/notifications-logs`, `PUT /system/email-templates`.

---

## 8. Key end-to-end flows (with conditional logic)

1. **Buy a policy (Customer/Agent):** browse → quote (`/proposals/quote`) → submit proposal + upload docs → *underwriter reviews* → if **Approved**, a policy is created in **Pending** → customer pays first premium via Stripe → on `payment_intent.succeeded` the policy flips to **Active** and an activation email fires. Design the "Pending payment" → "Active" transition clearly.
2. **File & settle a claim:** intimate (`/claims/intimate`, status **Intimated**) → upload docs → officer **assigns** to self → optionally **assign surveyor** (motor) → survey report → **approve** (set approved amount) or **reject** (reason) → finance **payout** → **settled**. Show the timeline advancing.
3. **Cashless pre-auth (health):** intimate with `isCashless=true` → officer **approve-preauth** before treatment → later full settlement.
4. **Endorsement:** customer requests change on policy → status **Requested** → underwriter approves/rejects.
5. **KYC gate:** customer uploads Aadhaar/PAN → **Pending** → underwriter approves/rejects. Surface KYC status prominently; some actions may be gated on Approved KYC.

**Conditional UI:** forms differ by product **domain** (Motor vs Health vs Life have different detail fields & claim detail types); family-floater policies expose member selection on claims; cashless toggle changes the claim flow.

---

## 9. Technical & API constraints (must respect)

- **Base path:** every endpoint is `{{baseUrl}}/api/v1/...`. JWT Bearer auth; role claim drives which portal/nav renders.
- **Errors:** API returns RFC-9110 problem-details (`type/title/status/errors/traceId`). Map `errors` to inline field errors; everything else → toast. (We saw real 400s on KYC when a file part was empty.)
- **File uploads:** multipart/form-data for KYC (`frontDocument`/`backDocument`), proposal docs, claim docs, survey reports. Design drag-drop with file-type/size hints.
- **Payments:** Stripe. Frontend creates a PaymentIntent (`/payments/pay/{scheduleId}`), confirms with Stripe.js using the returned `clientSecret`; the **webhook** (server-side) reconciles → don't mark "paid" in UI optimistically, poll/refresh history after confirm. Receipt URL now captured post-reconcile.
- **Binary downloads:** policy certificate is **plain-text `.txt`**; payment report export is **`.xlsx`** — both are file responses (trigger browser download, not inline render).
- **Currency/dates:** ₹ INR; `DateOnly` fields (DOB, policy start/end) have no time.
- **Rate limits:** auth endpoints 10 req/60s/IP, others 100/60s — design graceful "too many requests" handling on login.
- **Security headers / CORS:** API allows the Angular dev origin (`http://localhost:4200`).

---

## 10. Pre-answered UX interview questions

Claude Design will ask these — here are the answers so you can paste them up front:

- **Target users?** Seven roles (§2); customer & surveyor mobile-first, staff desktop-dense.
- **Key features?** Policy purchase, premium payments (Stripe), claims lifecycle, underwriting, KYC, endorsements, finance/payouts, grievances, admin & catalog management.
- **Onboarding?** Customer self-registers + DPDP consent + email verification + KYC upload. Staff are provisioned by Admin.
- **Business model / paywall?** No app paywall; revenue is insurance premiums via Stripe. No free/premium tiers in the UI.
- **API constraints?** As in §9 — versioned `/api/v1`, JWT roles, multipart uploads, Stripe webhook reconciliation, problem-details errors.
- **Conditional logic?** Product-domain-specific forms (Motor/Health/Life), cashless vs reimbursement claim paths, family-floater member selection, role-gated navigation, status-driven action availability.
- **Brand?** Trustworthy institutional + warm; teal-blue primary, coral CTA accent (§4) — but derive final tokens from the codebase if present.
- **Most important UI primitive?** The status badge + lifecycle timelines (policies, claims, proposals, payments).
