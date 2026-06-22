export type UserRole = 'Customer' | 'Agent' | 'Underwriter' | 'ClaimsOfficer' | 'FinanceOfficer' | 'Surveyor' | 'Admin';

export type PolicyStatus = 'Pending' | 'Active' | 'Lapsed' | 'Cancelled' | 'Expired' | 'Claimed';

export type ClaimStatus =
  | 'Intimated'
  | 'DocumentsPending'
  | 'PreAuthRequested'
  | 'PreAuthApproved'
  | 'UnderReview'
  | 'Approved'
  | 'PayoutProcessed'
  | 'Rejected'
  | 'Settled'
  | 'Withdrawn';

export type ProposalStatus = 'Draft' | 'Submitted' | 'UnderReview' | 'DocumentsPending' | 'Approved' | 'Rejected';

export type ClaimType = 'Death' | 'Maturity' | 'Health' | 'Accident' | 'Theft' | 'NaturalDamage';

export type InsuranceDomain = 'Health' | 'Life' | 'Motor';

export type PaymentFrequency = 'Monthly' | 'Quarterly' | 'HalfYearly' | 'Annually';

export type PaymentStatus = 'Due' | 'Paid' | 'Overdue' | 'Failed' | 'Refunded';

export type Gender = 'Male' | 'Female' | 'NonBinary' | 'Other';

export type MaritalStatus = 'Single' | 'Married' | 'Divorced' | 'Widowed';

export type Salutation = 'Mr' | 'Mrs' | 'Ms' | 'Dr';

export type Relationship = 'Spouse' | 'Son' | 'Daughter' | 'Father' | 'Mother' | 'Sibling' | 'Other';

export type EndorsementType = 'NomineeChange' | 'AddressChange' | 'VehicleCorrection' | 'ContactUpdate' | 'SumAssuredChange' | 'Other';

export type EndorsementStatus = 'Pending' | 'Approved' | 'Rejected';

export type AddressType = 'Permanent' | 'Current';

export type KycStatus = 'Pending' | 'Approved' | 'Rejected';

export type GrievanceCategory = 'ClaimDelay' | 'PolicyServicing' | 'PremiumIssue' | 'MisSelling' | 'AgentMisconduct' | 'Other';

export type GrievanceStatus = 'Open' | 'InProgress' | 'Resolved' | 'Escalated' | 'Closed';

export type NotificationType = 'Info' | 'Warning' | 'Success' | 'Error';
