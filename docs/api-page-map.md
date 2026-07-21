# SpeedClaim — API Page Map & Endpoint Coverage

> Historical API-coverage snapshot. Use controllers and generated OpenAPI/Swagger for the current endpoint contract; do not use this document's endpoint count as a live inventory.
>
> **Base URL:** all routes are versioned — `{{baseUrl}}/api/v1/...` (from `BaseApiController` → `api/v{version:apiVersion}/[controller]`).
> The Angular portal, including the Speedy workspace, evolves independently of this static map.

---

## AUTH PAGES

### Login Page
| Action | Endpoint | Status |
|---|---|---|
| Login | `POST /api/v1/auth/login` | ✅ |
| Refresh access token (silent) | `POST /api/v1/auth/refresh` | ✅ |
| Logout (revoke session) | `POST /api/v1/auth/logout` | ✅ |

### Register Page (Customer self-register)
| Action | Endpoint | Status |
|---|---|---|
| Register new customer | `POST /api/v1/auth/register` | ✅ |

### Email Verification Page
| Action | Endpoint | Status |
|---|---|---|
| Verify email | `POST /api/v1/auth/verify-email` | ✅ |

### Forgot Password / Reset Password Pages
| Action | Endpoint | Status |
|---|---|---|
| Request reset link | `POST /api/v1/auth/forgot-password` | ✅ |
| Submit new password | `POST /api/v1/auth/reset-password` | ✅ |

---

## CUSTOMER PORTAL

### Customer Dashboard
| Action | Endpoint | Status |
|---|---|---|
| Get my policies | `GET /api/v1/policies/my` | ✅ |
| Get my claims | `GET /api/v1/claims/my` | ✅ |
| Get my premium schedules | `GET /api/v1/payments/schedule/{policyId}` | ✅ |
| Get my notifications | `GET /api/v1/users/notifications` | ✅ |

### Browse Insurance Products Page
| Action | Endpoint | Status |
|---|---|---|
| List all active products | `GET /api/v1/products` | ✅ |
| Get single product details | `GET /api/v1/products/{id}` | ✅ |
| Get document requirements for a product | `GET /api/v1/products/{id}/documents` | ✅ |

### Get a Quote Page
| Action | Endpoint | Status |
|---|---|---|
| Generate quote | `POST /api/v1/proposals/quote` | ✅ |

### Submit Proposal / Apply Page
| Action | Endpoint | Status |
|---|---|---|
| Submit proposal | `POST /api/v1/proposals` | ✅ |
| Upload supporting documents | `PUT /api/v1/proposals/{id}/documents/{documentKey}` | ✅ |

### My Proposals Page
| Action | Endpoint | Status |
|---|---|---|
| List my proposals | `GET /api/v1/proposals/my` | ✅ |
| Get single proposal details | `GET /api/v1/proposals/{id}` | ✅ |

### My Policies Page
| Action | Endpoint | Status |
|---|---|---|
| List my policies | `GET /api/v1/policies/my` | ✅ |
| Get single policy details | `GET /api/v1/policies/{id}` | ✅ |

### Policy Detail Page
| Action | Endpoint | Status |
|---|---|---|
| Download policy certificate (plain-text `.txt`) | `GET /api/v1/policies/{id}/download` | ✅ |
| Get policy status history | `GET /api/v1/policies/{id}/history` | ✅ |
| Get endorsements for policy | `GET /api/v1/policies/{id}/endorsements` | ✅ |
| Request endorsement | `POST /api/v1/policies/{id}/endorsements` | ✅ |
| List nominees for a policy | `GET /api/v1/policies/{id}/nominees` | ✅ |
| Update nominee details | `PUT /api/v1/policies/nominees/{nomineeId}` | ✅ |
| Cancel policy | `PUT /api/v1/policies/{id}/cancel` | ✅ |

### Pay Premium Page
| Action | Endpoint | Status |
|---|---|---|
| Get premium schedule | `GET /api/v1/payments/schedule/{policyId}` | ✅ |
| Create Stripe PaymentIntent | `POST /api/v1/payments/pay/{scheduleId}` | ✅ |
| List saved payment methods | `GET /api/v1/payments/methods` | ✅ |

### Payment History Page
| Action | Endpoint | Status |
|---|---|---|
| Get my payment history | `GET /api/v1/payments/history` | ✅ |
| Download receipt | `GET /api/v1/payments/{paymentId}/receipt` | ✅ |

### Submit a Claim Page
| Action | Endpoint | Status |
|---|---|---|
| Intimate claim | `POST /api/v1/claims/intimate` | ✅ |
| Upload claim documents | `PUT /api/v1/claims/{id}/documents/{documentKey}` | ✅ |

### My Claims Page
| Action | Endpoint | Status |
|---|---|---|
| List my claims (filter by `status`, `type`) | `GET /api/v1/claims/my` | ✅ |
| Get single claim details | `GET /api/v1/claims/{id}` | ✅ |
| Get claim status history | `GET /api/v1/claims/{id}/history` | ✅ |

### My Profile Page
| Action | Endpoint | Status |
|---|---|---|
| View profile | `GET /api/v1/users/profile` | ✅ |
| Update profile | `PUT /api/v1/users/profile` | ✅ |
| Add address | `POST /api/v1/users/addresses` | ✅ |
| Update address | `PUT /api/v1/users/addresses/{addressId}` | ✅ |
| Delete address | `DELETE /api/v1/users/addresses/{addressId}` | ✅ |

### Family Members Page
| Action | Endpoint | Status |
|---|---|---|
| Add family member | `POST /api/v1/users/family` | ✅ |
| Update family member | `PUT /api/v1/users/family/{memberId}` | ✅ |
| List family members | `GET /api/v1/users/family` | ✅ |
| Delete family member | `DELETE /api/v1/users/family/{memberId}` | ✅ |

### KYC Documents Page
| Action | Endpoint | Status |
|---|---|---|
| View my KYC status | `GET /api/v1/users/kyc` | ✅ |
| Upload Aadhaar (multipart; also allowed for Agent) | `POST /api/v1/users/kyc/aadhaar` | ✅ |
| Upload PAN (multipart; also allowed for Agent) | `POST /api/v1/users/kyc/pan` | ✅ |

### Notifications Page
| Action | Endpoint | Status |
|---|---|---|
| List my notifications | `GET /api/v1/users/notifications` | ✅ |
| Mark notification as read | `PATCH /api/v1/users/notifications/{id}/read` | ✅ |
| Mark all as read | `PATCH /api/v1/users/notifications/read-all` | ✅ |

### Grievances / Support Page
| Action | Endpoint | Status |
|---|---|---|
| Raise grievance | `POST /api/v1/grievances` | ✅ |
| List my grievances | `GET /api/v1/grievances/my` | ✅ |
| Get single grievance status | `GET /api/v1/grievances/{id}` | ✅ |

---

## AGENT PORTAL

### Agent Dashboard
| Action | Endpoint | Status |
|---|---|---|
| Get dashboard summary | `GET /api/v1/agents/dashboard` | ✅ |

### My Customers Page
| Action | Endpoint | Status |
|---|---|---|
| List assigned customers | `GET /api/v1/agents/customers` | ✅ |

### Submit Proposal for Customer
| Action | Endpoint | Status |
|---|---|---|
| Get quote | `POST /api/v1/proposals/quote` | ✅ |
| Submit proposal (as agent) | `POST /api/v1/proposals` | ✅ |
| View my proposals | `GET /api/v1/proposals/my` | ✅ |
| Get single proposal details | `GET /api/v1/proposals/{id}` | ✅ |
| Upload proposal documents | `PUT /api/v1/proposals/{id}/documents/{documentKey}` | ✅ |

### Customer Policies Page
| Action | Endpoint | Status |
|---|---|---|
| List policies for assigned customers | `GET /api/v1/policies/assigned` | ✅ |

### Renewal Reminders Page
| Action | Endpoint | Status |
|---|---|---|
| Get upcoming renewals | `GET /api/v1/agents/renewals` | ✅ |

### Agent Profile Page
| Action | Endpoint | Status |
|---|---|---|
| Get agent profile | `GET /api/v1/agents/profile` | ✅ |
| Update own agent profile | `PUT /api/v1/agents/profile` | ✅ |

---

## UNDERWRITER PORTAL

### Proposal Review Queue
| Action | Endpoint | Status |
|---|---|---|
| List all proposals | `GET /api/v1/proposals/all` | ✅ |
| Get single proposal details | `GET /api/v1/proposals/{id}` | ✅ |
| Approve or reject proposal | `POST /api/v1/proposals/{id}/review` | ✅ |
| Request additional documents | `POST /api/v1/proposals/{id}/request-docs` | ✅ |
| Add notes | `PUT /api/v1/proposals/{id}/notes` | ✅ |

### KYC Review Queue
| Action | Endpoint | Status |
|---|---|---|
| List pending KYC | `GET /api/v1/users/kyc/pending` | ✅ |
| Approve/reject KYC | `PUT /api/v1/users/{customerId}/kyc/review` | ✅ |

### Endorsement Review Queue
| Action | Endpoint | Status |
|---|---|---|
| List pending endorsements | `GET /api/v1/policies/endorsements/pending` | ✅ |
| Approve/reject endorsement | `PUT /api/v1/policies/endorsements/{endorsementId}/review` | ✅ |

### All Policies View
| Action | Endpoint | Status |
|---|---|---|
| List all policies | `GET /api/v1/policies/all` | ✅ |
| Get single policy details | `GET /api/v1/policies/{id}` | ✅ |
| Get policy status history | `GET /api/v1/policies/{id}/history` | ✅ |

---

## CLAIMS OFFICER PORTAL

### Claims Queue
| Action | Endpoint | Status |
|---|---|---|
| List all claims (filter by status/type, paged) | `GET /api/v1/claims/all` | ✅ |
| Get single claim with full details | `GET /api/v1/claims/{id}` | ✅ |
| Assign claim to self | `PUT /api/v1/claims/{id}/assign` | ✅ |
| Update claim status | `PUT /api/v1/claims/{id}/status` | ✅ |
| Approve/reject claim | `PUT /api/v1/claims/{id}/approve` | ✅ |
| Mark as settled | `PUT /api/v1/claims/{id}/settle` | ✅ |
| Assign surveyor | `PUT /api/v1/claims/{id}/assign-surveyor` | ✅ |
| Request additional documents | `POST /api/v1/claims/{id}/request-docs` | ✅ |
| Approve cashless pre-auth | `PUT /api/v1/claims/{id}/approve-preauth` | ✅ |
| Submit survey report (also Surveyor) | `POST /api/v1/claims/{id}/survey-report` | ✅ |

### Grievance Management
| Action | Endpoint | Status |
|---|---|---|
| List all grievances | `GET /api/v1/grievances/all` | ✅ |
| Get grievance by ID | `GET /api/v1/grievances/{id}` | ✅ |
| Assign grievance | `PUT /api/v1/grievances/{id}/assign` | ✅ |
| Update grievance status | `PUT /api/v1/grievances/{id}/status` | ✅ |

---

## FINANCE OFFICER PORTAL

### Payment Records
| Action | Endpoint | Status |
|---|---|---|
| List all payments | `GET /api/v1/payments/all-records` | ✅ |
| Manually reconcile payment | `PUT /api/v1/payments/{paymentId}/reconcile` | ✅ |
| Process refund | `POST /api/v1/payments/{paymentId}/refund` | ✅ |

### Claim Payout
| Action | Endpoint | Status |
|---|---|---|
| Process claim payout (Stripe) | `POST /api/v1/payments/payout/claim/{claimId}` | ✅ |
| Mark claim financially settled | `PUT /api/v1/payments/claims/{claimId}/settle` | ✅ |

### Commission Management
| Action | Endpoint | Status |
|---|---|---|
| List pending commissions | `GET /api/v1/payments/commissions/pending` | ✅ |
| Approve and pay commission | `POST /api/v1/payments/commissions/{id}/approve` | ✅ |

### Reports
| Action | Endpoint | Status |
|---|---|---|
| Overdue policies report | `GET /api/v1/payments/reports/overdue` | ✅ |
| Payment collection summary (`?period=`) | `GET /api/v1/payments/reports/summary` | ✅ |
| Export payments to Excel (`.xlsx`) | `GET /api/v1/payments/reports/export` | ✅ |

---

## SURVEYOR PORTAL

### My Assigned Claims
| Action | Endpoint | Status |
|---|---|---|
| List assigned motor claims | `GET /api/v1/claims/surveyor/assigned` | ✅ |
| Submit survey report | `POST /api/v1/claims/{id}/survey-report` | ✅ |

---

## ADMIN PORTAL

### User Management
| Action | Endpoint | Status |
|---|---|---|
| List all users (paged) | `GET /api/v1/users/all` | ✅ |
| Change user role | `PUT /api/v1/users/{userId}/role` | ✅ |
| Activate/deactivate user | `PUT /api/v1/users/{userId}/status` | ✅ |
| Reset user password | `POST /api/v1/auth/admin/reset-password/{userId}` | ✅ |
| View all sessions | `GET /api/v1/users/sessions` | ✅ |

### Agent Management
| Action | Endpoint | Status |
|---|---|---|
| Register new agent | `POST /api/v1/auth/admin/register-agent` | ✅ |
| List branches | `GET /api/v1/agents/branches` | ✅ |
| Create branch | `POST /api/v1/agents/branches` | ✅ |
| Assign agent to branch | `PUT /api/v1/agents/{agentId}/branch/{branchId}` | ✅ |
| Update agent license | `PUT /api/v1/agents/{agentId}/license` | ✅ |
| Activate/deactivate agent | `PUT /api/v1/agents/{agentId}/status` | ✅ |

### Product Catalog Management
| Action | Endpoint | Status |
|---|---|---|
| List products | `GET /api/v1/products` | ✅ |
| Create product | `POST /api/v1/products` | ✅ |
| Update premium rate table | `PUT /api/v1/products/{id}/rates` | ✅ |
| Get document requirements for a product | `GET /api/v1/products/{id}/documents` | ✅ |
| Configure document requirements for a product | `PUT /api/v1/products/{id}/documents` | ✅ |
| Toggle product active/inactive | `PUT /api/v1/products/{id}/status` | ✅ |

### System Config
| Action | Endpoint | Status |
|---|---|---|
| View system configs | `GET /api/v1/system/configs` | ✅ |
| Update system config key | `PUT /api/v1/system/configs` | ✅ |
| View audit logs | `GET /api/v1/system/audit-logs` | ✅ |
| View notification/email logs | `GET /api/v1/system/notifications-logs` | ✅ |
| Manage email templates | `PUT /api/v1/system/email-templates` | ✅ |

---

## INTEGRATION / NON-UI ENDPOINTS

These are not driven by any frontend page — they are called by external systems.

| Action | Endpoint | Caller | Status |
|---|---|---|---|
| Stripe webhook receiver (handles `payment_intent.succeeded`) | `POST /api/v1/payments/webhook` | Stripe (anonymous, signature-validated) | ✅ |

---

## Remaining Known Gaps

No outstanding gaps. All 110 endpoints are implemented and exercised via the Postman "Full Demo" collection. All routes are versioned under `/api/v1`.
