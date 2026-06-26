import {
  UserRole, PolicyStatus, ClaimStatus, ClaimType, ProposalStatus,
  InsuranceDomain, PaymentFrequency, PaymentStatus, Gender, MaritalStatus,
  Salutation, Relationship, EndorsementType, EndorsementStatus,
  AddressType, KycStatus, GrievanceCategory, GrievanceStatus, NotificationType,
} from './enums';

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterUserRequest {
  salutationTitle: Salutation;
  firstName: string;
  lastName: string;
  email: string;
  phone: string;
  password: string;
  dateOfBirth: string;
  gender: Gender;
  maritalStatus: MaritalStatus;
  aadhaarNumber: string;
  panNumber: string;
  permanentAddress: AddressRequest;
  currentAddress: AddressRequest;
  consentDataProcessing: boolean;
  consentKycCollection: boolean;
}

export interface AddressRequest {
  line1: string;
  line2?: string;
  city: string;
  state: string;
  postalCode: string;
  country: string;
}

export interface ForgotPasswordRequest {
  email: string;
}

export interface ResetPasswordRequest {
  token: string;
  newPassword: string;
}

export interface VerifyEmailRequest {
  token: string;
}

export interface ResendVerificationRequest {
  email: string;
}

export interface RefreshTokenRequest {
  token: string;
}

export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  user: AuthUserDto;
}

export interface RegistrationResponse {
  message: string;
  userId?: string;
}

export interface AuthUserDto {
  id: string;
  email: string;
  salutationTitle: Salutation;
  firstName: string;
  lastName: string;
  fullName: string;
  phone: string;
  role: UserRole;
  maritalStatus: MaritalStatus;
}

export interface UserDto extends AuthUserDto {
  gender: Gender;
  dateOfBirth: string;
  customerId?: string;
  isEmailVerified: boolean;
  isActive: boolean;
  createdAt: string;
  permanentAddress?: AddressDto;
  currentAddress?: AddressDto;
}

export interface AddressDto {
  id: string;
  line1: string;
  line2?: string;
  city: string;
  state: string;
  postalCode: string;
  country: string;
  type: AddressType;
}

export interface SingleAddressRequest {
  line1: string;
  line2?: string;
  city: string;
  state: string;
  postalCode: string;
  country: string;
  type: AddressType;
}

export interface FamilyMemberDto {
  id: string;
  name: string;
  dateOfBirth: string;
  relationship: Relationship;
  gender: Gender;
  salutationTitle: Salutation;
  appointeeName?: string;
  phone?: string;
  email?: string;
}

export interface AddFamilyMemberRequest {
  name: string;
  dateOfBirth: string;
  relationship: Relationship;
  gender: Gender;
  salutationTitle: Salutation;
  appointeeName?: string;
}

export interface UpdateFamilyMemberRequest {
  name?: string;
  dateOfBirth?: string;
  relationship?: Relationship;
  gender?: Gender;
  salutationTitle?: Salutation;
  appointeeName?: string;
}

export interface KycRecordDto {
  id: string;
  userId: string;
  kycStatus: KycStatus;
  aadhaarUploaded: boolean;
  aadhaarNumber?: string | null;
  panUploaded: boolean;
  panNumber?: string | null;
  aadhaarFrontPath?: string;
  aadhaarBackPath?: string;
  panFrontPath?: string;
  panBackPath?: string;
  rejectionReason?: string;
  createdAt: string;
  updatedAt?: string;
}

export interface ProductDto {
  id: string;
  productName: string;
  uin: string;
  description: string;
  domain: InsuranceDomain;
  minAge: number;
  maxAge: number;
  minSumAssured: number;
  maxSumAssured: number;
  minTenureYears: number;
  maxTenureYears: number;
  waitingPeriodDays: number;
  allowsFamilyFloater: boolean;
  maxFamilyMembers: number;
  isActive: boolean;
}

export interface DocumentRequirementDto {
  id: string;
  documentKey: string;
  label: string;
  description: string;
  isMandatory: boolean;
}

export interface GenerateQuoteRequest {
  productId: string;
  age: number;
  sumAssured: number;
  tenureYears: number;
  gender?: Gender;
}

export interface HealthQuoteDetail {
  preExistingConditions?: string;
}

export interface MotorQuoteDetail {
  vehicleMake: string;
  vehicleModel: string;
  manufactureYear: number;
  insuredDeclaredValue: number;
}

export interface LifeQuoteDetail {
  isSmoker: boolean;
}

export interface GenerateQuoteResponse {
  premiumAmount: number;
  paymentFrequency: PaymentFrequency;
  sumAssured: number;
  tenureYears: number;
}

export interface SubmitProposalRequest {
  customerId: string;
  productId: string;
  sumAssured: number;
  tenureYears: number;
  premiumAmount: number;
  paymentFrequency: PaymentFrequency;
  healthDetail?: object;
  motorDetail?: object;
  lifeDetail?: object;
  customerMemberIds?: string[];
  nominees?: NomineeRequest[];
}

export interface ProposalMemberRequest {
  familyMemberId: string;
}

export interface NomineeRequest {
  fullName: string;
  relationship: Relationship;
  sharePercentage: number;
  dateOfBirth: string;
  isMinor: boolean;
  appointeeName?: string;
}

export interface ProposalDto {
  id: string;
  proposalNumber: string;
  customerId: string;
  agentId?: string;
  productId: string;
  productName?: string;
  domain?: InsuranceDomain;
  status: ProposalStatus;
  sumAssured: number;
  tenureYears: number;
  premiumAmount: number;
  paymentFrequency: PaymentFrequency;
  createdAt: string;
  updatedAt?: string;
  healthDetail?: object;
  motorDetail?: object;
  lifeDetail?: object;
  documents?: SubmittedDocumentDto[];
  members?: ProposalMemberDto[];
  nominees?: NomineeDto[];
  underwriterNotes?: string;
}

export interface ProposalMemberDto {
  id: string;
  familyMemberId: string;
  name: string;
  relationship: Relationship;
}

export interface NomineeDto {
  id: number;
  name: string;
  relationship: Relationship;
  sharePercentage: number;
  dateOfBirth: string;
  appointeeName?: string;
}

export interface SubmittedDocumentDto {
  id: number;
  documentKey: string;
  documentName: string;
  filePath: string;
  uploadedAt: string;
}

export interface PolicyDto {
  id: string;
  policyNumber: string;
  userId: string;
  productId: string;
  productName: string;
  agentId?: string;
  status: PolicyStatus;
  paymentFrequency: PaymentFrequency;
  premiumAmount: number;
  coverageAmount: number;
  currency: string;
  startDate: string;
  endDate: string;
  domain: InsuranceDomain;
  type: string;
  healthDetail?: object;
  vehicleDetail?: object;
  lifeDetail?: object;
  createdAt: string;
}

export interface PolicyStatusHistoryDto {
  id: number;
  status: PolicyStatus;
  changedAt: string;
  remarks?: string;
  changedBy?: string;
}

export interface PolicyNomineeDto {
  id: number;
  name: string;
  relationship: Relationship;
  sharePercentage: number;
  dateOfBirth: string;
  appointeeName?: string;
}

export interface UpdateNomineeRequest {
  name?: string;
  relationship?: Relationship;
  sharePercentage?: number;
  dateOfBirth?: string;
  appointeeName?: string;
}

export interface EndorsementDto {
  id: number;
  policyId: number;
  type: EndorsementType;
  status: EndorsementStatus;
  description: string;
  oldValue?: string;
  newValue?: string;
  createdAt: string;
  reviewedAt?: string;
  reviewRemarks?: string;
}

export interface RequestEndorsementRequest {
  type: EndorsementType;
  description: string;
  oldValue?: string;
  newValue?: string;
}

export interface IntimateClaimRequest {
  policyId: number;
  claimType: ClaimType;
  claimAmountRequested: number;
  incidentDate: string;
  incidentDescription: string;
  isCashless: boolean;
  claimantMemberId?: number;
}

export interface ClaimDto {
  id: string;
  claimNumber: string;
  policyId: string;
  policyNumber: string;
  customerId: string;
  claimantMemberId?: string;
  claimType: ClaimType;
  claimAmountRequested: number;
  claimAmountApproved?: number;
  isCashless: boolean;
  status: ClaimStatus;
  intimationDate: string;
  incidentDate: string;
  incidentDescription: string;
  assignedOfficerId?: string;
  surveyorId?: string;
  settlementDate?: string;
  rejectionReason?: string;
  createdAt: string;
  updatedAt?: string;
  documents?: SubmittedDocumentDto[];
}

export interface ClaimStatusHistoryDto {
  id: number;
  status: ClaimStatus;
  changedAt: string;
  remarks?: string;
  changedBy?: string;
}

export interface PremiumScheduleDto {
  id: string;
  policyId: string;
  policyNumber?: string;
  installmentNumber: number;
  amountDue: number;
  dueDate: string;
  status: PaymentStatus;
  paymentId?: string;
}

export interface PaymentRecordDto {
  id: string;
  policyId?: string;
  customerId: string;
  customerName: string;
  policyNumber?: string;
  amount: number;
  currency: string;
  createdAt: string;
  paidAt?: string;
  paymentType: string;
  status: string;
  stripePaymentIntentId?: string;
}

export interface CreatePaymentIntentRequest {
  policyId: string;
}

export interface CreatePaymentIntentResponse {
  clientSecret: string;
  publishableKey: string;
  paymentIntentId: string;
}

export interface SavedCardDto {
  id: string;
  brand: string;
  last4: string;
  expMonth: number;
  expYear: number;
}

export interface GrievanceDto {
  id: string;
  grievanceNumber: string;
  category: GrievanceCategory;
  description: string;
  status: GrievanceStatus;
  relatedPolicyId?: string;
  relatedClaimId?: string;
  assignedToId?: string;
  resolutionNotes?: string;
  createdAt: string;
  updatedAt?: string;
}

export interface RaiseGrievanceRequest {
  category: GrievanceCategory;
  description: string;
  relatedPolicyId?: number;
  relatedClaimId?: number;
}

export interface NotificationDto {
  id: number;
  title: string;
  message: string;
  type: NotificationType;
  isRead: boolean;
  createdAt: string;
}

export interface AgentCommissionDto {
  id: number;
  agentId: number;
  agentName: string;
  policyId: number;
  policyNumber: string;
  domain: InsuranceDomain;
  commissionRate: number;
  commissionAmount: number;
  status: string;
  paidAt?: string;
  createdAt: string;
}

export interface PaymentSummaryDto {
  totalCollected: number;
  premiums: number;
  claimsPaid: number;
  netInflow: number;
}

export interface OverduePolicyDto {
  policyId: number;
  policyNumber: string;
  customerName: string;
  domain: InsuranceDomain;
  amountDue: number;
  daysOverdue: number;
  dueDate: string;
}

export interface FinancePaymentRecordDto {
  id: string;
  paymentNumber?: string;
  policyId?: string;
  policyNumber?: string;
  customerId: string;
  customerName: string;
  amount: number;
  currency: string;
  paymentType: string;
  status: string;
  createdAt: string;
  paidAt?: string;
  stripePaymentIntentId?: string;
}

export interface ProblemDetails {
  type?: string;
  title: string;
  status: number;
  errors?: Record<string, string[]>;
  traceId?: string;
}

export interface PagedResponse<T> {
  data: T[];
  pageNumber: number;
  pageSize: number;
  totalRecords: number;
  totalPages: number;
}

export interface ApiMessage {
  message: string;
}

export interface SessionDto {
  id: string;
  userId: string;
  userEmail: string;
  ipAddress: string;
  userAgent: string;
  expiresAt: string;
  isRevoked: boolean;
  createdAt: string;
}

export interface SystemConfigDto {
  configKey: string;
  configValue: string;
  description?: string;
  updatedAt?: string;
}

export interface AuditLogDto {
  id: string;
  entityType: string;
  entityId: string;
  action: string;
  oldValue?: string;
  newValue?: string;
  userId?: string;
  ipAddress?: string;
  createdAt: string;
}

export interface BranchDto {
  id: string;
  name: string;
  city: string;
  state: string;
  address: string;
  phone: string;
  email: string;
  isActive: boolean;
}

export interface CreateBranchRequest {
  name: string;
  city: string;
  state: string;
  address: string;
  phone: string;
  email: string;
}

export interface UpdateAgentLicenseRequest {
  licenseNumber: string;
  licenseExpiry: string;
}

export interface AgentProfileDto {
  agentId: string;
  userId: string;
  email: string;
  fullName: string;
  agentCode: string;
  agentType: string;
  licenseNumber: string;
  licenseExpiry: string;
  commissionRate: number;
  isActive: boolean;
  branchId?: string;
  branchName?: string;
  branchCity?: string;
}

export interface CreateProductRequest {
  productName: string;
  domain: string;
  uin: string;
  description: string;
  minAge: number;
  maxAge: number;
  minSumAssured: number;
  maxSumAssured: number;
  minTenureYears: number;
  maxTenureYears: number;
  waitingPeriodDays: number;
  allowsFamilyFloater: boolean;
  maxFamilyMembers: number;
}

export interface PremiumRateDto {
  ageMin: number;
  ageMax: number;
  sumAssuredMin: number;
  sumAssuredMax: number;
  annualPremium: number;
}

export interface DocumentRequirementResponseDto {
  id: number;
  productId: number;
  entityType: string;
  domain: string;
  documentKey: string;
  label: string;
  description: string;
  isMandatory: boolean;
  isActive: boolean;
}

export interface DocumentRequirementUpdateDto {
  entityType: string;
  domain: string;
  documentKey: string;
  label: string;
  description: string;
  isMandatory: boolean;
  isActive: boolean;
}

export interface AdminResetPasswordRequest {
  newPassword: string;
}

export interface ManageEmailTemplateRequest {
  templateKey: string;
  subject: string;
  bodyHtml: string;
}

export interface UpdateSystemConfigRequest {
  configKey: string;
  configValue: string;
}

export interface EmailTemplateDto {
  id: number;
  templateKey: string;
  subject: string;
  bodyHtml: string;
  isActive: boolean;
  createdAt: string;
  updatedAt?: string;
}

export interface RegisterAgentRequest {
  email: string;
  password: string;
  salutation: string;
  firstName: string;
  lastName: string;
  phone: string;
  licenseNumber: string;
  licenseExpiry: string;
  agencyName: string;
  aadhaarNumber: string;
  panNumber: string;
  maritalStatus: string;
  permanentAddress: AddressRequest;
  currentAddress: AddressRequest;
  isSameAsPermanent: boolean;
}
