# KT: Document Checklist Tracker & Claim Status Timeline

Handoff notes for the requirement:
> "Document checklist tracker showing which claim documents are submitted/pending.
> Claim status timeline view (Filed → Under Review → Approved/Rejected) for the customer."

This covers what already exists, what's actually missing, and a punch list to hand to
whoever picks up the enhancement.

---

## 1. Claim status timeline — already fully implemented

No work needed here. Included for completeness so it isn't accidentally rebuilt.

**Backend**
- Every claim status change goes through one central method,
  `ClaimService.UpdateClaimStatusInternalAsync` (`backend/SpeedClaim.Api/Services/ClaimService.cs`),
  which writes a `ClaimStatusHistory` row (`Models/ClaimStatusHistory.cs`) — `OldStatus`,
  `NewStatus`, `ChangedById`, `Notes`, `ChangedAt` — for every transition (Intimated →
  UnderReview → DocumentsPending/PreAuthRequested → Approved/Rejected → Settled/Withdrawn).
- `GET /api/v1/claims/{id}/history` (`ClaimsController.cs:74`) →
  `ClaimService.GetClaimHistoryAsync` (`ClaimService.cs:355`) reads all rows for the claim,
  orders by `ChangedAt` ascending, maps to `ClaimStatusHistoryDto`.

**Frontend**
- `claim-detail.ts:45` calls `claimService.getHistory(id)`, maps each row to a generic
  `TimelineItem { status, date, remarks }`.
- Rendered by the shared `<app-timeline>` component
  (`shared/components/timeline/timeline.ts` + `.html`) — vertical dot/line layout, one entry
  per transition, using `<app-status-badge>` for the color-coded pill. Shown in
  `claim-detail.html:129` only if `timeline().length > 0`.

**Nuance to flag:** it's a chronological *event log* of transitions that actually happened,
not a fixed-step "stepper." The real state machine branches (a claim can pass through
`DocumentsPending`/`PreAuthRequested` more than once before `Approved`/`Rejected`), so it
doesn't map cleanly onto a literal 3–4 step progress bar. If the ask is "make it look like a
stepper," that's a restyle of this same data — not new backend work.

---

## 2. Document checklist tracker — mostly missing

There is **no** working cross-reference anywhere between "required documents for this
product/claim" and "what's actually been submitted." One place looks like a checklist but
is fake static UI. The admin side that defines requirements exists on the backend but has an
incomplete frontend.

### What exists

- `DocumentRequirement` (`Models/DocumentRequirement.cs`) — catalog of required docs per
  `ProductId` / `Domain` / `EntityType`, with `IsMandatory`.
- `SubmittedDocument` — actual uploaded-file rows, keyed by `EntityType` / `EntityId`.
- Admin CRUD already works end-to-end on the backend:
  - `GET /api/v1/products/{id}/documents` → `ProductService.GetDocumentRequirementsAsync`
  - `PATCH /api/v1/products/{id}/documents` → `ProductService.ConfigureDocumentRequirementsAsync`
    (full replace-list: wipes existing rows for the product, re-inserts from the request)
- Customer proposal-submit (`portal/proposals/proposal-submit/proposal-submit.ts:100`)
  correctly fetches real `DocumentRequirementDto[]` from that endpoint — but only uses it to
  gate/inform KYC-approval status before submission, not to render a submitted/pending list.

### What's broken or missing

**a) Admin "Document requirements" modal has no way to add a requirement**
(`admin-products.html:236-251`, `admin-products.ts`):
- `openEditDocsModal()` only GETs existing rows into `productDocs`.
- `toggleDocRequired(index)` only flips `isMandatory` on an **existing** entry.
- `saveDocs()` PATCHes whatever's currently in `productDocs()`.
- The template only loops over `productDocs()`; the `@empty` block just shows "No document
  requirements configured" — no "+ Add requirement" button, no input fields for
  `DocumentKey`/`Label`/`Description`/`Domain`/`EntityType`, no delete button.
- Confirmed live: opening the modal for a product with zero rows ("Dummy Test Health") is a
  dead end — nothing can be added from the UI even though the backend fully supports it.

**b) Claims portal shows uploaded docs only — no requirement cross-reference**
(`portal/claims/claim-detail/claim-detail.html:92-126`, `.ts`):
- The "Documents" section just lists whatever `SubmittedDocument` rows exist for the claim
  (via `GetClaimByIdAsync`, `ClaimService.cs:342`) — no "pending" state, no reference to
  `DocumentRequirement` at all.
- When status is `DocumentsPending`, a generic free-form file-upload box appears
  (`claim-detail.html:115`) — not tied to any specific missing requirement item.
- `documentKeyFor()` (`claim-detail.ts:106`) derives an upload key by slugifying the
  filename — a workaround for not having a fixed list of required keys to pick from.

**c) Agent's "Document checklist" step is hardcoded fake UI**
(`agent/agent-proposals/proposal-submit.html:182-209`, `.ts:57-65`):
- `kycDocs` / `proposalDocs` are plain string-literal arrays (`{ label, hint }`), not fetched
  from `DocumentRequirementDto` at all.
- The icon next to every item is always the same "incomplete" circle — it never reflects
  real upload state.
- Stale copy: still says "Aadhaar card (front & back)" — KYC has been single-upload only
  since the `RemoveKycBackDocuments` migration.

---

## 3. Decided design (2026-07-08) — supersedes the punch list below

Discussed with the KT person directly; he will implement this himself, targeting roughly a
week out (~2026-07-15). This is the actual design to build — the "add a requirement" punch
list in §4 was the naive fix and is now superseded by this catalog-based approach:

1. **Predefined per-domain document catalog.** Instead of an admin free-typing
   `DocumentKey`/`Label`/`Description` per product (the current `DocumentRequirement` CRUD
   shape), there will be a fixed catalog of known document types per domain, e.g.:
   - Health: hospital bills, MRI report, room rent bill, etc.
   - Life: death certificate, etc.
   - Motor: FIR copy, accident image, vehicle image, etc.
2. **Product creation/edit (admin)** — in the "Add product" / "Edit product" modal
   (`admin-products.html`), once a `Domain` is chosen the admin picks (multi-select) which
   catalog documents apply to *this* product, rather than typing them freeform. This replaces
   the current broken "Document requirements" modal's approach entirely (see §4a) — that
   modal's toggle-only UI becomes moot once selection happens from a catalog at
   product-creation time instead of via a separate post-hoc editor.
3. **Customer portal (proposal creation)** — still an open question, not yet decided,
   whether the required-docs list should also surface there. Don't build anything for this
   surface yet.
4. **Claims officer portal** — the claim's document checklist will be shown here, and the
   claims officer will be able to accept/reject **individual documents** with a remark (not
   just approve/reject the whole claim). This needs new state on `SubmittedDocument` that
   doesn't exist yet (something like a per-document `ReviewStatus` + `RejectionRemark`) — a
   new migration, not just a query. Not started.

### Open questions to resolve with him before he starts

Not blockers, but worth a 5-minute conversation so the design doesn't need rework mid-build:

- **Scope catalog entries by `EntityType`, not just `Domain`.** `Models/Enums/EntityType.cs`
  already has `Kyc, Proposal, Claim, Endorsement, Policy`, and `DocumentRequirement` already
  carries both `Domain` and `EntityType` per row — this isn't new plumbing, just a decision to
  actually use both dimensions. The examples discussed (hospital bills, MRI report, FIR copy,
  accident images) are all claim-stage docs. If a domain's catalog isn't also split by
  Proposal-vs-Claim, the admin's multi-select for a product will mix proposal-underwriting
  docs with claim-settlement docs in one list. Decide: one catalog list per domain covering
  both stages (with entries tagged by stage), or does admin pick separately per stage?
- **Decide how a per-document rejection affects claim status.** If a claims officer rejects
  one document with a remark, does that auto-transition the claim to `DocumentsPending`
  (reusing the existing `RequestAdditionalDocumentsAsync` path in `ClaimService.cs`), or is it
  purely informational, with the officer separately triggering the status change? Without an
  explicit rule, a claim could sit in `UnderReview` indefinitely with rejected documents and no
  signal to the customer to re-upload. Whatever's decided should also write an `AuditLog` entry
  (every meaningful state change in this codebase does — see CLAUDE.md §6), not just flip a
  column silently.

**Existing precedent worth pointing him to:** the catalog concept isn't new to this schema —
`SpeedClaimDbContextModelSnapshot`/seed data already has this exact pattern for KYC:
```csharp
new DocumentRequirement { DocumentKey = "AADHAAR", Domain = "ALL", Label = "Aadhaar Card",
  EntityType = EntityType.Kyc, ... }   // no ProductId — a global/catalog-style entry
```
(`SpeedClaimDbContext.cs:486-487`). `ProductId` on `DocumentRequirement` is already nullable
(`Guid? ProductId`) specifically to support domain-wide entries not tied to one product. The
planned design is a natural generalization of this, not a new concept fighting the schema.

**Data model implication:** this introduces a new concept — a master/seed "document catalog"
table (per domain) that products select *from* — separate from the existing per-product
`DocumentRequirement` rows, which would presumably become the "selected subset" for a given
product rather than admin-authored free text. The KT person owns designing this table; don't
pre-build it.

---

## 4. Groundwork already ruled out — do not build these in the meantime

Because the design in §3 changes the shape of the admin-side feature, the following items
from the original punch list are **superseded** and should NOT be built before the KT person
starts, to avoid conflicting/duplicate work he'd have to unwind:

- ~~Admin "Document requirements" modal — add freeform "Add requirement" form~~ — moot; product
  creation will select from a catalog instead of freeform entry (§3.2).
- ~~Claims-officer per-document accept/reject~~ — depends on the new `SubmittedDocument`
  review-state schema he's designing; don't add ad hoc fields for this now.

**Still safe / unrelated to touch**, since they don't depend on the catalog redesign:

- Claim status timeline (§1) — untouched, unrelated.
- Fixing the stale "Aadhaar card (front & back)" copy in the agent's proposal-submit hardcoded
  arrays (`proposal-submit.ts:57-65`) is a pure copy fix, not a checklist rebuild — fine to fix
  independently if it bothers you, but the arrays themselves will likely be replaced wholesale
  by the KT person's work anyway, so low priority.

**Optional / adjacent, not part of this requirement:**

- Quote generation (`ProposalService.CalculatePremiumAsync`) has no guardrail ensuring a
  product's `PremiumRateTable` bands fully cover its own `MinAge/MaxAge` ×
  `MinSumAssured/MaxSumAssured` eligibility range. A gap in seeded rate data can make a
  validly-eligible quote request throw `NotFoundException("No applicable rate found...")`.
  Unrelated to documents — flag separately if it should be tracked.

---

## 5. Original punch list (superseded, kept for history)

1. ~~Admin document requirements modal — add an "Add requirement" form~~ → replaced by §3.2's
   catalog-selection approach.
2. Claims portal real checklist (fetch requirements, diff against `SubmittedDocument`, render
   submitted/pending) — still directionally correct, but now depends on the catalog model from
   §3 and the per-document review state from §3.4. Not started.
3. ~~Agent proposal-submit checklist rewire~~ → same catalog dependency as #2; not started.
4. Claim status timeline — no action needed (§1).
