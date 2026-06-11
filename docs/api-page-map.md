# SpeedClaim — API Page Map & Endpoint Coverage

> Every page a real user would open in the Angular frontend, what it needs from the API, and whether that endpoint exists.

---

## AUTH PAGES

### Login Page
| Action | Endpoint | Status |
|---|---|---|
| Login | `POST /api/auth/login` | ✅ |
| Refresh access token (silent) | `POST /api/auth/refresh` | ✅ |

### Register Page (Customer self-register)
| Action | Endpoint | Status |
|---|---|---|
| Register new customer | `POST /api/auth/register` | ✅ |

### Email Verification Page
| Action | Endpoint | Status |
|---|---|---|
| Verify email | `POST /api/auth/verify-email` | ✅ |

### Forgot Password / Reset Password Pages
| Action | Endpoint | Status |
|---|---|---|
| Request reset link | `POST /api/auth/forgot-password` | ✅ |
| Submit new password | `POST /api/auth/reset-password` | ✅ |

---

## CUSTOMER PORTAL

### Customer Dashboard
| Action | Endpoint | Status |
|---|---|---|
| Get my policies | `GET /api/policies/my` | ✅ |
| Get my claims | `GET /api/claims/my` | ✅ |
| Get my premium schedules | `GET /api/payments/schedule/{policyId}` | ✅ |
| Get my notifications | `GET /api/users/notifications` | ✅ |

### Browse Insurance Products Page
| Action | Endpoint | Status |
|---|---|---|
| List all active products | `GET /api/products` | ✅ |
| Get single product details | `GET /api/products/{id}` | ✅ |

### Get a Quote Page
| Action | Endpoint | Status |
|---|---|---|
| Generate quote | `POST /api/proposals/quote` | ✅ |

### Submit Proposal / Apply Page
| Action | Endpoint | Status |
|---|---|---|
| Submit proposal | `POST /api/proposals` | ✅ |
| Upload supporting documents | `POST /api/proposals/{id}/upload` | ✅ |

### My Proposals Page
| Action | Endpoint | Status |
|---|---|---|
| List my proposals | `GET /api/proposals/my` | ✅ |
| Get single proposal details | `GET /api/proposals/{id}` | ✅ |

### My Policies Page
| Action | Endpoint | Status |
|---|---|---|
| List my policies | `GET /api/policies/my` | ✅ |
| Get single policy details | `GET /api/policies/{id}` | ✅ |

### Policy Detail Page
| Action | Endpoint | Status |
|---|---|---|
| Download policy certificate | `GET /api/policies/{id}/download` | ✅ |
| Get policy status history | `GET /api/policies/{id}/history` | ✅ |
| Get endorsements for policy | `GET /api/policies/{id}/endorsements` | ✅ |
| Request endorsement | `POST /api/policies/{id}/endorsements` | ✅ |
| Update nominee details | `PUT /api/policies/nominees/{nomineeId}` | ✅ |
| List nominees for a policy | `GET /api/policies/{id}/nominees` | ✅ |
| Cancel policy | `PUT /api/policies/{id}/cancel` | ✅ |

### Pay Premium Page
| Action | Endpoint | Status |
|---|---|---|
| Get premium schedule | `GET /api/payments/schedule/{policyId}` | ✅ |
| Create Stripe PaymentIntent | `POST /api/payments/pay/{scheduleId}` | ✅ |
| List saved payment methods | `GET /api/payments/methods` | ✅ |

### Payment History Page
| Action | Endpoint | Status |
|---|---|---|
| Get my payment history | `GET /api/payments/history` | ✅ |
| Download receipt | `GET /api/payments/{paymentId}/receipt` | ✅ |

### Submit a Claim Page
| Action | Endpoint | Status |
|---|---|---|
| Intimate claim | `POST /api/claims/intimate` | ✅ |
| Upload claim documents | `POST /api/claims/{id}/upload` | ✅ |

### My Claims Page
| Action | Endpoint | Status |
|---|---|---|
| List my claims | `GET /api/claims/my` | ✅ |
| Get single claim details | `GET /api/claims/{id}` | ✅ |
| Get claim status history | `GET /api/claims/{id}/history` | ✅ |

### My Profile Page
| Action | Endpoint | Status |
|---|---|---|
| View profile | `GET /api/users/profile` | ✅ |
| Update profile | `PUT /api/users/profile` | ✅ |
| Add address | `POST /api/users/addresses` | ✅ |
| Update address | `PUT /api/users/addresses/{addressId}` | ✅ |
| Delete address | `DELETE /api/users/addresses/{addressId}` | ✅ |

### Family Members Page
| Action | Endpoint | Status |
|---|---|---|
| Add family member | `POST /api/users/family` | ✅ |
| Update family member | `PUT /api/users/family/{memberId}` | ✅ |
| List family members | `GET /api/users/family` | ✅ |
| Delete family member | `DELETE /api/users/family/{memberId}` | ✅ |

### KYC Documents Page
| Action | Endpoint | Status |
|---|---|---|
| View my KYC status | `GET /api/users/kyc` | ✅ |
| Upload KYC documents | `POST /api/users/kyc` | ✅ |

### Notifications Page
| Action | Endpoint | Status |
|---|---|---|
| List my notifications | `GET /api/users/notifications` | ✅ |
| Mark notification as read | `PATCH /api/users/notifications/{id}/read` | ✅ |
| Mark all as read | `PATCH /api/users/notifications/read-all` | ✅ |

### Grievances / Support Page
| Action | Endpoint | Status |
|---|---|---|
| Raise grievance | `POST /api/grievances` | ✅ |
| List my grievances | `GET /api/grievances/my` | ✅ |
| Get single grievance status | `GET /api/grievances/{id}` | ✅ |

---

## AGENT PORTAL

### Agent Dashboard
| Action | Endpoint | Status |
|---|---|---|
| Get dashboard summary | `GET /api/agents/dashboard` | ✅ |

### My Customers Page
| Action | Endpoint | Status |
|---|---|---|
| List assigned customers | `GET /api/agents/customers` | ✅ |

### Submit Proposal for Customer
| Action | Endpoint | Status |
|---|---|---|
| Get quote | `POST /api/proposals/quote` | ✅ |
| Submit proposal (as agent) | `POST /api/proposals` | ✅ |
| View my proposals | `GET /api/proposals/my` | ✅ |
| Get single proposal details | `GET /api/proposals/{id}` | ✅ |
| Upload proposal documents | `POST /api/proposals/{id}/upload` | ✅ |

### Customer Policies Page
| Action | Endpoint | Status |
|---|---|---|
| List policies for assigned customers | `GET /api/policies/assigned` | ✅ |

### Renewal Reminders Page
| Action | Endpoint | Status |
|---|---|---|
| Get upcoming renewals | `GET /api/agents/renewals` | ✅ |

### Agent Profile Page
| Action | Endpoint | Status |
|---|---|---|
| Get agent profile | `GET /api/agents/profile` | ✅ |
| Update own agent profile | `PUT /api/agents/profile` | ✅ |

---

## UNDERWRITER PORTAL

### Proposal Review Queue
| Action | Endpoint | Status |
|---|---|---|
| List all proposals | `GET /api/proposals/all` | ✅ |
| Get single proposal details | `GET /api/proposals/{id}` | ✅ |
| Approve or reject proposal | `POST /api/proposals/{id}/review` | ✅ |
| Request additional documents | `POST /api/proposals/{id}/request-docs` | ✅ |
| Add notes | `PUT /api/proposals/{id}/notes` | ✅ |

### KYC Review Queue
| Action | Endpoint | Status |
|---|---|---|
| List pending KYC | `GET /api/users/kyc/pending` | ✅ |
| Approve/reject KYC | `PUT /api/users/{customerId}/kyc/review` | ✅ |

### Endorsement Review Queue
| Action | Endpoint | Status |
|---|---|---|
| List pending endorsements | `GET /api/policies/endorsements/pending` | ✅ |
| Approve/reject endorsement | `PUT /api/policies/endorsements/{endorsementId}/review` | ✅ |

### All Policies View
| Action | Endpoint | Status |
|---|---|---|
| List all policies | `GET /api/policies/all` | ✅ |
| Get single policy details | `GET /api/policies/{id}` | ✅ |

---

## CLAIMS OFFICER PORTAL

### Claims Queue
| Action | Endpoint | Status |
|---|---|---|
| List all claims | `GET /api/claims/all` | ✅ |
| Get single claim with full details | `GET /api/claims/{id}` | ✅ |
| Assign claim to self | `PUT /api/claims/{id}/assign` | ✅ |
| Update claim status | `PUT /api/claims/{id}/status` | ✅ |
| Approve/reject claim | `PUT /api/claims/{id}/approve` | ✅ |
| Mark as settled | `PUT /api/claims/{id}/settle` | ✅ |
| Assign surveyor | `PUT /api/claims/{id}/assign-surveyor` | ✅ |
| Request additional documents | `POST /api/claims/{id}/request-docs` | ✅ |
| Approve cashless pre-auth | `PUT /api/claims/{id}/approve-preauth` | ✅ |

### Grievance Management
| Action | Endpoint | Status |
|---|---|---|
| List all grievances | `GET /api/grievances/all` | ✅ |
| Get grievance by ID | `GET /api/grievances/{id}` | ✅ |
| Assign grievance | `PUT /api/grievances/{id}/assign` | ✅ |
| Update grievance status | `PUT /api/grievances/{id}/status` | ✅ |

---

## FINANCE OFFICER PORTAL

### Payment Records
| Action | Endpoint | Status |
|---|---|---|
| List all payments | `GET /api/payments/all-records` | ✅ |
| Manually reconcile payment | `PUT /api/payments/{paymentId}/reconcile` | ✅ |
| Process refund | `POST /api/payments/{paymentId}/refund` | ✅ |

### Claim Payout
| Action | Endpoint | Status |
|---|---|---|
| Process claim payout (Stripe) | `POST /api/payments/payout/claim/{claimId}` | ✅ |
| Mark claim financially settled | `PUT /api/payments/claims/{claimId}/settle` | ✅ |

### Commission Management
| Action | Endpoint | Status |
|---|---|---|
| List pending commissions | `GET /api/payments/commissions/pending` | ✅ |
| Approve and pay commission | `POST /api/payments/commissions/{id}/approve` | ✅ |

### Reports
| Action | Endpoint | Status |
|---|---|---|
| Overdue policies report | `GET /api/payments/reports/overdue` | ✅ |
| Payment collection summary | `GET /api/payments/reports/summary` | ✅ |
| Export payments to Excel | `GET /api/payments/reports/export` | ✅ |

---

## SURVEYOR PORTAL

### My Assigned Claims
| Action | Endpoint | Status |
|---|---|---|
| List assigned motor claims | `GET /api/claims/surveyor/assigned` | ✅ |
| Submit survey report | `POST /api/claims/{id}/survey-report` | ✅ |

---

## ADMIN PORTAL

### User Management
| Action | Endpoint | Status |
|---|---|---|
| List all users | `GET /api/users/all` | ✅ |
| Change user role | `PUT /api/users/{userId}/role` | ✅ |
| Activate/deactivate user | `PUT /api/users/{userId}/status` | ✅ |
| Reset user password | `POST /api/auth/admin/reset-password/{userId}` | ✅ |
| View all sessions | `GET /api/users/sessions` | ✅ |

### Agent Management
| Action | Endpoint | Status |
|---|---|---|
| Register new agent | `POST /api/auth/admin/register-agent` | ✅ |
| List branches | `GET /api/agents/branches` | ✅ |
| Create branch | `POST /api/agents/branches` | ✅ |
| Assign agent to branch | `PUT /api/agents/{agentId}/branch/{branchId}` | ✅ |
| Update agent license | `PUT /api/agents/{agentId}/license` | ✅ |
| Activate/deactivate agent | `PUT /api/agents/{agentId}/status` | ✅ |

### Product Catalog Management
| Action | Endpoint | Status |
|---|---|---|
| List products | `GET /api/products` | ✅ |
| Create product | `POST /api/products` | ✅ |
| Update premium rate table | `PUT /api/products/{id}/rates` | ✅ |
| Get document requirements for a product | `GET /api/products/{id}/documents` | ✅ |
| Configure document requirements for a product | `PUT /api/products/{id}/documents` | ✅ |
| Toggle product active/inactive | `PUT /api/products/{id}/status` | ✅ |

### System Config
| Action | Endpoint | Status |
|---|---|---|
| View system configs | `GET /api/system/configs` | ✅ |
| Update system config key | `PUT /api/system/configs` | ✅ |
| View audit logs | `GET /api/system/audit-logs` | ✅ |
| View notification/email logs | `GET /api/system/notifications-logs` | ✅ |
| Manage email templates | `PUT /api/system/email-templates` | ✅ |

---

## Remaining Known Gaps

No outstanding gaps. All previously identified issues have been resolved.
