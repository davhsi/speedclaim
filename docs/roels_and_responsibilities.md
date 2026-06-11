# System Roles & Responsibilities

**Total roles:** 7

---

## 1. Customer

**Primary end user** — buys policies, manages family, files claims

### Account & Profile
- Register, verify email, login
- Update personal profile and contact details
- Add / edit permanent and current address
- Add family members (spouse, children, parents)
- Upload and track KYC documents

### Proposals & Policies
- Browse available insurance products
- Generate premium quote before applying
- Submit proposal (individual or family floater)
- View own proposals and their status
- View all own active and past policies
- Download policy document (e-policy)

### Payments
- Pay first premium to activate policy
- View premium schedule for a policy
- Make renewal payments
- View payment history and download receipts

### Policy Servicing
- Request endorsements (address, nominee changes)
- View endorsement status
- Update nominee details

### Claims
- Intimate a new claim
- Upload claim documents
- Track claim status in real time
- View claim history

### Grievances
- Raise a grievance against any policy or claim
- Track grievance status
- View resolution notes

### Cannot Do
- See other customers' data
- Approve or reject their own proposal
- Access agent or staff dashboards
- Change policy premium or sum assured directly

---

## 2. Agent

**Sells policies, onboards customers, tracks own performance**

### Customer Management
- View own assigned customers only
- Create a proposal on behalf of a customer
- Assist customer through KYC completion
- View customer's policy list (own customers only)

### Proposals
- Create and submit proposals for customers
- Track status of proposals they submitted
- Re-submit or update a draft proposal

### Agent Dashboard
- View total policies sold (lifetime and monthly)
- View commission earned — pending, approved, paid
- View renewal reminders for own customers
- View own profile and license details

### Notifications
- Receive alerts on proposal approval or rejection
- Receive commission credit notifications
- Receive renewal due alerts for own customers

### Cannot Do
- Access customers not assigned to them
- Approve proposals themselves
- View other agents' commissions or customers
- Process or approve claims
- Access finance or underwriting dashboards

---

## 3. Underwriter

**Reviews proposals, assesses risk, approves or rejects policies**

### Proposal Review
- View all submitted proposals across all agents and customers
- Review customer KYC and submitted documents
- Approve or reject a proposal with notes
- Request additional documents from customer
- Add underwriting notes to a proposal

### KYC Review
- Review KYC submissions pending approval
- Approve or reject KYC with reason

### Policy View
- View issued policies (read only)
- View policy history and status changes

### Cannot Do
- Create or modify insurance products
- Process payments or commissions
- Approve or reject claims
- Access grievances
- Edit customer personal details

---

## 4. Claims Officer

**End-to-end claim processing — from intimation to settlement**

### Claim Management
- View all intimated claims across all domains
- Assign claims to self or other officers
- Update claim status at each processing step
- Request additional documents from customer
- Approve or reject a claim with reason
- Approve cashless pre-authorization for health claims
- Assign a surveyor to a motor claim

### Surveyor Coordination
- View surveyor assignment and report status
- Upload surveyor report on behalf of surveyor if needed

### Settlement
- Mark claim as settled after finance processes payout
- View full claim history and document trail

### Cannot Do
- Process actual payment/payout (finance does this)
- Modify policy details
- Access grievances directly
- Approve proposals or KYC

---

## 5. Finance Officer

**Manages all money movement — premiums, payouts, refunds, commissions**

### Premium Management
- View all premium payment records
- Reconcile Stripe payments against premium schedules
- Mark payments as paid after webhook confirmation
- Process refunds for cancelled or rejected policies
- View overdue and lapsed policy payment reports

### Claim Payouts
- Process approved claim settlement payouts via Stripe
- Mark claims as financially settled
- View payout history

### Agent Commissions
- Review pending commission records
- Approve and mark commissions as paid
- View commission reports by agent and period

### Reports
- View premium collection summary (daily/monthly)
- View outstanding premium schedules
- Export payment and payout reports

### Cannot Do
- Approve or reject claims
- Approve proposals or KYC
- Modify policy or customer details
- Access grievances

---

## 6. Surveyor

**Inspects vehicles for motor claims, submits survey report**

### Assigned Claims
- View only motor claims assigned to them
- View vehicle and policy details for assigned claims
- View customer contact details for coordination

### Survey Report
- Upload survey report document
- Enter estimated repair cost
- Enter survey date and remarks
- Mark survey as completed

### Cannot Do
- View non-motor claims
- View claims not assigned to them
- Approve or reject claims
- Access any financial data
- View other customers' profiles or policies

---

## 7. Admin

**Full system control — users, products, config, audit**

### User Management
- Create, edit, activate or deactivate any user
- Assign or change user roles
- Reset passwords for any user
- View all user profiles and sessions

### Product Management
- Create new insurance products (health, life, motor)
- Set and update premium rate tables
- Configure document requirements per domain
- Activate or deactivate products

### Branch & Agent Management
- Create and manage branches
- Create agent records and assign to branches
- View and update agent license details

### System Config
- Update system_config values (grace period, free-look days etc.)
- Manage email templates
- View and search audit logs
- View all notifications and email logs

### Oversight
- View all proposals, policies, claims, grievances
- View all payment records and commission records
- Generate any report across the system

### Cannot Do
- Nothing is restricted — full access across all modules