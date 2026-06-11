# SpeedClaim — User Flows & User Stories

> Step-by-step flows for every actor, including decision branches for key outcomes.  
> Diagrams are rendered as Mermaid flowcharts — viewable in GitHub, VS Code (Markdown Preview), and Notion.

---

## Actors

| Actor | How they join | Portal |
|---|---|---|
| Customer | Self-registers | Customer Portal |
| Agent | Created by Admin | Agent Portal |
| Underwriter | Created by Admin | Underwriter Portal |
| Claims Officer | Created by Admin | Claims Portal |
| Finance Officer | Created by Admin | Finance Portal |
| Admin | Pre-seeded / super user | Admin Portal |

---

## 1. Customer Flows

### 1.1 Registration & Onboarding

**User story:** As a new user I want to create an account and verify my email so I can access the platform.

```mermaid
flowchart TD
    A([Start]) --> B[Fill registration form\nname · email · password · phone]
    B --> C[POST /api/auth/register]
    C --> D{Email already\nregistered?}
    D -->|Yes — 409| E[Show conflict error\nSuggest login instead]
    D -->|No| F[Account created — inactive\nVerification email sent]
    F --> G[Open inbox · click verify link]
    G --> H[POST /api/auth/verify-email]
    H --> I{Token valid?}
    I -->|No — 400| J[Request new link\nvia forgot-password]
    J --> G
    I -->|Yes| K[Account activated]
    K --> L[Login\nPOST /api/auth/login]
    L --> M{Credentials valid?}
    M -->|No — 400| N[Show error · retry]
    M -->|Yes| O[Receive JWT + Refresh Token]
    O --> P[Customer Dashboard]
    P --> Q[Upload KYC documents\nPOST /api/users/kyc]
    Q --> R[KYC status: Pending]
```

---

### 1.2 KYC Verification

**User story:** As a customer I want my identity documents reviewed so I am authorised to purchase policies.

```mermaid
flowchart TD
    A[Customer uploads KYC docs\nPOST /api/users/kyc] --> B[Status: Pending]
    B --> C[Underwriter opens KYC queue\nGET /api/users/kyc/pending]
    C --> D[Reviews submitted documents]
    D --> E{Decision}
    E -->|Approve| F[PUT /api/users/:id/kyc/review\nstatus: Approved]
    E -->|Reject| G[PUT /api/users/:id/kyc/review\nstatus: Rejected · reason provided]
    F --> H[Customer notified ✅\nCan now purchase policies]
    G --> I[Customer notified ❌\nReason shown]
    I --> J[Customer corrects & re-uploads]
    J --> A
```

---

### 1.3 Buying a Policy

**User story:** As a customer I want to get a quote, submit a proposal, and receive a policy after underwriter approval.

```mermaid
flowchart TD
    A[Browse products\nGET /api/products] --> B[Select a product]
    B --> C[Generate quote\nPOST /api/proposals/quote\nage · sum assured · tenure]
    C --> D[Quote shown\npremium amount · cover details]
    D --> E{Accept quote?}
    E -->|No| A
    E -->|Yes| F[Fill proposal form\nhealth / life / motor details\nnominees · document list]
    F --> G[POST /api/proposals]
    G --> H[Upload supporting documents\nPOST /api/proposals/:id/upload]
    H --> I[Status: Submitted]
    I --> J[Underwriter reviews proposal\nPOST /api/proposals/:id/review]
    J --> K{Decision}
    K -->|Request more docs| L[Status: DocumentsPending\nCustomer re-uploads]
    L --> H
    K -->|Reject| M[Status: Rejected\nReason shown to customer]
    K -->|Approve| N[Status: Approved\nPolicy auto-created & issued]
    N --> O[Premium schedule generated]
    O --> P[Customer & Agent notified 🎉\nPolicy visible in My Policies]
```

---

### 1.4 Paying Premiums

**User story:** As a customer I want to pay my premium installments online so my policy stays active.

```mermaid
flowchart TD
    A[View premium schedule\nGET /api/payments/schedule/:policyId] --> B[Select a due installment]
    B --> C[Create Stripe PaymentIntent\nPOST /api/payments/pay/:scheduleId]
    C --> D{Schedule already paid?}
    D -->|Yes — 409| E[Show already-paid message]
    D -->|No| F[Stripe checkout presented to customer]
    F --> G{Payment result}
    G -->|Success| H[Schedule status: Paid\nPayment record created]
    G -->|Failure| I[Show failure · retry]
    I --> F
    H --> J[Download receipt\nGET /api/payments/:paymentId/receipt]
    H --> K[View full payment history\nGET /api/payments/history]
```

---

### 1.5 Filing a Claim

**User story:** As a customer I want to file an insurance claim and track it through to payout.

```mermaid
flowchart TD
    A[Incident occurs] --> B[Intimate claim\nPOST /api/claims/intimate\ntype · amount · date · description]
    B --> C{Policy active\nand belongs to customer?}
    C -->|No — 404| D[Error shown]
    C -->|Yes| E[Claim created\nStatus: Intimated]
    E --> F[Upload claim documents\nPOST /api/claims/:id/upload]
    F --> G[Claims Officer picks up claim\nPUT /api/claims/:id/assign]
    G --> H[Claims Officer reviews]
    H --> I{More documents\nneeded?}
    I -->|Yes| J[Request docs\nPOST /api/claims/:id/request-docs]
    J --> K[Customer re-uploads]
    K --> H
    I -->|No| L{Requires on-site\nsurvey?}
    L -->|Yes| M[Assign Surveyor\nPUT /api/claims/:id/assign-surveyor]
    M --> N[Surveyor submits report\nPOST /api/claims/:id/survey-report]
    N --> O[Claims Officer reviews report]
    L -->|No| O
    O --> P{Cashless\npre-auth needed?}
    P -->|Yes| Q[Approve pre-auth\nPUT /api/claims/:id/approve-preauth]
    Q --> R[Claims Officer final decision]
    P -->|No| R
    R --> S{Decision}
    S -->|Reject — 200| T[Status: Rejected\nCustomer notified]
    S -->|Approve| U[Status: Approved\nApproved amount set]
    U --> V[Finance Officer initiates payout\nPOST /api/payments/payout/claim/:claimId]
    V --> W[Stripe payout processed]
    W --> X[Finance marks claim settled\nPUT /api/payments/claims/:claimId/settle]
    X --> Y[Customer notified 💰\nClaim closed]
```

---

### 1.6 Raising a Grievance

**User story:** As a customer I want to raise a complaint and track its resolution status.

```mermaid
flowchart TD
    A[Customer has a complaint] --> B[Raise grievance\nPOST /api/grievances\ncategory · description\nlinked policy or claim optional]
    B --> C[Status: Open]
    C --> D[Claims Officer views queue\nGET /api/grievances/all]
    D --> E[Assign to self\nPUT /api/grievances/:id/assign]
    E --> F[Status: InProgress\nInvestigate & communicate]
    F --> G[Update status\nPUT /api/grievances/:id/status]
    G --> H{Outcome}
    H -->|Resolved| I[Status: Resolved\nCustomer can view\nGET /api/grievances/:id]
    H -->|Needs escalation| J[Status: Escalated\nSenior officer reviews]
    J --> F
```

---

## 2. Agent Flows

### 2.1 Getting Started

**User story:** As a new agent I want to be onboarded by admin so I can start serving customers.

```mermaid
flowchart TD
    A[Admin creates agent account\nPOST /api/auth/admin/register-agent] --> B[Agent receives login credentials]
    B --> C[Agent logs in\nPOST /api/auth/login]
    C --> D[Admin creates branch if needed\nPOST /api/agents/branches]
    D --> E[Admin assigns agent to branch\nPUT /api/agents/:id/branch/:branchId]
    E --> F[Admin sets license details\nPUT /api/agents/:id/license]
    F --> G[Agent is fully onboarded]
    G --> H[View dashboard & KPIs\nGET /api/agents/dashboard]
    H --> I[Update own profile details\nPUT /api/agents/profile]
```

---

### 2.2 Submitting a Proposal for a Customer

**User story:** As an agent I want to submit insurance proposals on behalf of my customers so they can get covered.

```mermaid
flowchart TD
    A[View assigned customers\nGET /api/agents/customers] --> B[Select customer to assist]
    B --> C[Generate quote\nPOST /api/proposals/quote]
    C --> D[Walk customer through quote details]
    D --> E{Customer agrees?}
    E -->|No| C
    E -->|Yes| F[Submit proposal on behalf of customer\nPOST /api/proposals  isAgent: true]
    F --> G[Upload documents\nPOST /api/proposals/:id/upload]
    G --> H[Status: Submitted\nAwait underwriter review]
    H --> I[Track status\nGET /api/proposals/:id]
    I --> J{Underwriter decision}
    J -->|Request more docs| K[Re-upload\nPOST /api/proposals/:id/upload]
    K --> H
    J -->|Approved| L[Policy issued\nCommission scheduled 💼]
    J -->|Rejected| M[Inform customer\nExplore alternative products]
    M --> A
```

---

### 2.3 Tracking Renewals

**User story:** As an agent I want to see which policies are expiring soon so I can proactively contact customers.

```mermaid
flowchart TD
    A[Check renewal reminders\nGET /api/agents/renewals] --> B[List of policies expiring soon]
    B --> C[Contact customer]
    C --> D{Customer wants\nto renew?}
    D -->|Yes| E[Submit new proposal\nsee flow 2.2]
    D -->|Not yet| F[Schedule follow-up]
    D -->|No| G[Note outcome]
```

---

## 3. Admin & Underwriter Flows

### 3.1 Admin — User & Platform Management

**User story:** As an admin I want full control over users, agents, products, and system configuration.

```mermaid
flowchart TD
    A[Admin logs in] --> B{Select task}
    B --> C[List & search users\nGET /api/users/all]
    B --> D[Change user role\nPUT /api/users/:id/role]
    B --> E[Activate or deactivate user\nPUT /api/users/:id/status]
    B --> F[Reset user password\nPOST /api/auth/admin/reset-password/:id]
    B --> G[Register new agent\nPOST /api/auth/admin/register-agent]
    B --> H[Manage branches\nPOST /api/agents/branches\nPUT /api/agents/:id/branch/:branchId]
    B --> I[Manage products\nPOST /api/products\nPUT /api/products/:id/rates\nPUT /api/products/:id/status]
    B --> J[View audit logs & configs\nGET /api/system/audit-logs\nGET /api/system/configs]
```

---

### 3.2 Underwriter — KYC Review

**User story:** As an underwriter I want to review customer identity documents so only verified customers can buy policies.

```mermaid
flowchart TD
    A[Open KYC queue\nGET /api/users/kyc/pending] --> B[Select a pending record]
    B --> C[Review submitted documents]
    C --> D{Decision}
    D -->|Approve| E[PUT /api/users/:id/kyc/review\nstatus: Approved]
    D -->|Reject| F[PUT /api/users/:id/kyc/review\nstatus: Rejected · reason]
    E --> G[Customer notified ✅\nUnlocked to purchase policies]
    F --> H[Customer notified ❌\nCan correct and resubmit]
    H --> A
```

---

### 3.3 Underwriter — Proposal Review

**User story:** As an underwriter I want to evaluate submitted proposals and approve or reject them based on risk.

```mermaid
flowchart TD
    A[Open proposal queue\nGET /api/proposals/all] --> B[Select a submitted proposal]
    B --> C[Review applicant details\ndocuments · health/life/motor data · nominees]
    C --> D{Need more\ninformation?}
    D -->|Yes| E[Request additional documents\nPOST /api/proposals/:id/request-docs]
    E --> F[Wait for customer resubmission]
    F --> C
    D -->|No| G[Add underwriting notes\nPUT /api/proposals/:id/notes]
    G --> H{Decision}
    H -->|Approve| I[POST /api/proposals/:id/review — approved\nPolicy auto-created]
    H -->|Reject| J[POST /api/proposals/:id/review — rejected\nReason recorded]
    I --> K[Premium schedule generated\nCustomer & Agent notified 🎉]
    J --> L[Customer & Agent notified]
```

---

### 3.4 Underwriter — Endorsement Review

**User story:** As an underwriter I want to review and action policy change requests from customers.

```mermaid
flowchart TD
    A[Open endorsement queue\nGET /api/policies/endorsements/pending] --> B[Select pending endorsement]
    B --> C[Review requested policy changes\nnominee update · sum assured · address]
    C --> D{Decision}
    D -->|Approve| E[PUT /api/policies/endorsements/:id/review\nstatus: Approved]
    D -->|Reject| F[PUT /api/policies/endorsements/:id/review\nstatus: Rejected · reason]
    E --> G[Policy updated\nCustomer notified ✅]
    F --> H[Customer notified with reason]
```

---

## 4. Claims Officer & Finance Officer Flows

### 4.1 Claims Officer — Processing a Claim

**User story:** As a claims officer I want to review, investigate, and adjudicate claims efficiently.

```mermaid
flowchart TD
    A[View claims queue\nGET /api/claims/all] --> B[Pick an unassigned claim]
    B --> C[Assign to self\nPUT /api/claims/:id/assign]
    C --> D[Review claim details & documents]
    D --> E{Documents\nsufficient?}
    E -->|No| F[Request more documents\nPOST /api/claims/:id/request-docs]
    F --> G[Customer re-uploads]
    G --> D
    E -->|Yes| H{Physical damage /\nmotor claim?}
    H -->|Yes| I[Assign surveyor\nPUT /api/claims/:id/assign-surveyor]
    I --> J[Surveyor submits report\nPOST /api/claims/:id/survey-report]
    J --> K[Review survey findings]
    H -->|No| K
    K --> L{Cashless\npre-auth needed?}
    L -->|Yes| M[Approve pre-auth\nPUT /api/claims/:id/approve-preauth]
    M --> N[Final decision]
    L -->|No| N
    N --> O{Decision}
    O -->|Approve| P[PUT /api/claims/:id/approve — approved\nApproved amount recorded]
    O -->|Reject| Q[PUT /api/claims/:id/approve — rejected\nCustomer notified]
    P --> R[Hand off to Finance Officer for payout]
```

---

### 4.2 Finance Officer — Payout & Reconciliation

**User story:** As a finance officer I want to process claim payouts, reconcile payments, and manage agent commissions.

```mermaid
flowchart TD
    A[Approved claim awaiting payout] --> B[Initiate Stripe payout\nPOST /api/payments/payout/claim/:claimId]
    B --> C{Payout\nsuccessful?}
    C -->|Yes| D[Mark claim financially settled\nPUT /api/payments/claims/:claimId/settle]
    C -->|No| E[Investigate · retry]
    E --> B
    D --> F[Claim fully closed ✅]

    G[View all payment records\nGET /api/payments/all-records] --> H{Discrepancy found?}
    H -->|Yes| I[Reconcile manually\nPUT /api/payments/:id/reconcile]
    H -->|No| J[No action needed]

    K[View pending commissions\nGET /api/payments/commissions/pending] --> L[Review agent commission record]
    L --> M[Approve & pay\nPOST /api/payments/commissions/:id/approve]

    N[Generate reports] --> O[Overdue policies\nGET /api/payments/reports/overdue]
    N --> P[Payment summary\nGET /api/payments/reports/summary]
    N --> Q[Export to Excel\nGET /api/payments/reports/export]
```

---

## 5. End-to-End Policy Lifecycle

> A single view of how all actors interact across the full life of a policy.

```mermaid
flowchart LR
    CUST([Customer])
    AGENT([Agent])
    UW([Underwriter])
    CO([Claims Officer])
    FO([Finance Officer])
    ADMIN([Admin])

    ADMIN -->|Creates agent accounts\nManages products & users| AGENT
    ADMIN -->|Configures roles & system| UW & CO & FO

    CUST -->|Registers · submits KYC| UW
    AGENT -->|Submits proposal\non behalf of customer| UW
    CUST -->|Submits proposal directly| UW

    UW -->|Approves proposal\nPolicy issued| CUST
    UW -->|Commission triggered| AGENT

    CUST -->|Pays premium installments| FO
    CUST -->|Files claim| CO
    CO -->|Approves claim| FO
    FO -->|Processes payout| CUST

    CUST -->|Raises grievance| CO
    CO -->|Resolves grievance| CUST
```
