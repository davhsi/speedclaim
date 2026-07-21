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
  salutation: Salutation;
  firstName: string;
  lastName: string;
  email: string;
  phone: string;
  password: string;
  dateOfBirth: string;
  gender: Gender;
  maritalStatus: MaritalStatus;
  aadhaarNumber?: string;
  panNumber?: string;
  permanentAddress: AddressRequest;
  currentAddress?: AddressRequest | null;
  isSameAsPermanent: boolean;
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
  salutation: Salutation;
  firstName: string;
  lastName: string;
  fullName: string;
  phone: string;
  role: UserRole;
  maritalStatus: MaritalStatus;
  avatarUrl?: string;
}

export interface UserDto extends AuthUserDto {
  dateOfBirth?: string;
  customerId?: string;
  isEmailVerified: boolean;
  isActive: boolean;
  createdAt: string;
  permanentAddress?: AddressDto;
  currentAddress?: AddressDto;
  kycApproved: boolean;
  occupation?: string | null;
  annualIncome?: number | null;
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
  addressType: AddressType;
  addressLine1: string;
  addressLine2?: string;
  city: string;
  state: string;
  postalCode: string;
  country: string;
  isSameAsPermanent: boolean;
}

export interface FamilyMemberDto {
  id: string;
  salutation: Salutation;
  firstName: string;
  lastName: string;
  fullName: string;
  dateOfBirth: string;
  gender: Gender;
  relationship: Relationship;
  isDependent: boolean;
}

export interface AddFamilyMemberRequest {
  salutation: Salutation;
  firstName: string;
  lastName: string;
  dateOfBirth: string;
  gender: Gender;
  relationship: Relationship;
  isDependent: boolean;
}

export interface UpdateFamilyMemberRequest {
  salutation: Salutation;
  firstName: string;
  lastName: string;
  dateOfBirth: string;
  gender: Gender;
  relationship: Relationship;
  isDependent: boolean;
}

export interface KycRecordDto {
  id: string;
  userId: string;
  kycStatus: KycStatus;
  aadhaarUploaded: boolean;
  aadhaarNumber?: string | null;
  panUploaded: boolean;
  panNumber?: string | null;
  aadhaarDocumentPath?: string;
  panDocumentPath?: string;
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
  isAvailableForSale: boolean;
  motorVehicleType?: string | null;
  coverageOptions?: number[];
  sumAssuredIncrement?: number | null;
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
  age?: number;
  sumAssured: number;
  tenureYears: number;
  vehicleMarketValue?: number;
  vehicleManufactureYear?: number;
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

export interface HealthDetailDto {
  preExistingConditions: string;
  networkHospitalCoverage: string;
  tpaName: string;
  roomRentLimit: number;
  maternityCovered: boolean;
  copayPercentage: number;
}

export interface LifeDetailDto {
  policySubtype: string;
  maturityBenefit: number;
  deathBenefit: number;
  surrenderValueApplicable: boolean;
  loanEligible: boolean;
}

export interface MotorDetailDto {
  vehicleNumber: string;
  vehicleMake: string;
  vehicleModel: string;
  manufactureYear: number;
  vehicleType: string;
  idv: number;
  engineNumber: string;
  chassisNumber: string;
  coverType: string;
}

export interface SubmitProposalRequest {
  customerId: string;
  productId: string;
  sumAssured: number;
  tenureYears: number;
  premiumAmount: number;
  paymentFrequency: PaymentFrequency;
  healthDetail?: HealthDetailDto;
  motorDetail?: MotorDetailDto;
  lifeDetail?: LifeDetailDto;
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
  healthDetail?: HealthDetailDto | null;
  motorDetail?: MotorDetailDto | null;
  lifeDetail?: LifeDetailDto | null;
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
  id: string;
  name: string;
  relationship: Relationship;
  sharePercentage: number;
  dateOfBirth: string;
  isMinor?: boolean;
  appointeeName?: string;
}

export interface SubmittedDocumentDto {
  id: string;
  documentKey: string;
  documentName: string;
  filePath: string;
  uploadedAt: string;
}

export interface PolicyDto {
  id: string;
  policyNumber: string;
  customerId: string;
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
  waitingPeriodDays?: number;
  healthDetail?: object;
  vehicleDetail?: object;
  lifeDetail?: object;
}

export interface PolicyStatusHistoryDto {
  id: string;
  policyId: string;
  status: PolicyStatus;
  changedAt: string;
  remarks?: string;
  changedById: string;
}

export interface PolicyNomineeDto {
  id: string;
  fullName: string;
  relationship: Relationship;
  sharePercentage: number;
  dateOfBirth: string;
  isMinor: boolean;
  appointeeName?: string;
}

export interface UpdateNomineeRequest {
  fullName: string;
  relationship: Relationship;
  sharePercentage: number;
  dateOfBirth: string;
  isMinor: boolean;
  appointeeName?: string;
}

export interface EndorsementDto {
  id: string;
  policyId: string;
  endorsementType: EndorsementType;
  status: EndorsementStatus;
  description: string;
  oldValue?: string;
  newValue?: string;
  requestedById: string;
  reviewedById?: string;
  reviewedAt?: string;
  createdAt: string;
}

export interface RequestEndorsementRequest {
  endorsementType: EndorsementType;
  description: string;
  oldValue?: string;
  newValue?: string;
}

export interface IntimateClaimRequest {
  policyId: string;
  claimType: ClaimType;
  claimAmountRequested: number;
  incidentDate: string;
  incidentDescription: string;
  isCashless: boolean;
  claimantMemberId?: string | null;
}

export interface ClaimDto {
  id: string;
  claimNumber: string;
  policyId: string;
  policyNumber?: string;
  customerId: string;
  customerName?: string;
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
  surveyEstimatedCost?: number | null;
  surveyDate?: string | null;
  surveyorRemarks?: string | null;
}

export interface ClaimStatusHistoryDto {
  id: string;
  claimId: string;
  oldStatus: string;
  newStatus: string;
  changedById?: string;
  notes?: string;
  changedAt: string;
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
  customerId: string;
  policyId?: string;
  claimId?: string;
  policyNumber?: string;
  claimNumber?: string;
  category: GrievanceCategory;
  description: string;
  status: GrievanceStatus;
  assignedToId?: string;
  resolutionNotes?: string;
  resolvedAt?: string;
  createdAt: string;
  updatedAt?: string;
  attachmentPath?: string;
}

export interface RaiseGrievanceRequest {
  policyId?: string;
  claimId?: string;
  category: GrievanceCategory;
  description: string;
}

export interface NotificationDto {
  id: string;
  title: string;
  message: string;
  type: NotificationType;
  isRead: boolean;
  createdAt: string;
  redirectUrl?: string | null;
}

export interface AgentCommissionDto {
  id: string;
  agentId: string;
  agentName: string;
  policyId: string;
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
  successfulPayments: number;
  failedPayments: number;
}

export interface OverduePolicyDto {
  policyId?: string;
  policyNumber: string;
  customerName: string;
  domain: InsuranceDomain;
  amountDue: number;
  daysOverdue: number;
  dueDate: string;
}

export interface FinancePaymentRecordDto {
  id: string;
  policyId?: string;
  proposalId?: string;
  policyNumber?: string;
  customerId: string;
  customerName: string;
  amount: number;
  currency: string;
  paymentType: string;
  status: string;
  createdAt: string;
  paidAt?: string;
  receiptUrl?: string;
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
  userName?: string;
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
  motorVehicleType?: string | null;
  coverageOptions?: number[];
  sumAssuredIncrement?: number | null;
}

export interface UpdateProductRequest {
  productName: string;
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
  motorVehicleType?: string | null;
  coverageOptions?: number[];
  sumAssuredIncrement?: number | null;
}

export interface PremiumRateDto {
  ageMin: number;
  ageMax: number;
  sumAssuredMin: number;
  sumAssuredMax: number;
  annualPremium: number;
}

export interface DocumentRequirementResponseDto {
  id: string;
  productId?: string;
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
  id: string;
  templateKey: string;
  subject: string;
  bodyHtml: string;
  isActive: boolean;
  createdAt: string;
  updatedAt?: string;
}

export interface RegisterAgentRequest {
  email: string;
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

export interface AgentAddCustomerRequest {
  email: string;
  salutation: string;
  firstName: string;
  lastName: string;
  phone: string;
  permanentAddress: AddressRequest;
  currentAddress: AddressRequest;
  isSameAsPermanent: boolean;
  dateOfBirth: string;
  gender: Gender;
  maritalStatus: MaritalStatus;
  occupation: string;
  annualIncome: number;
}

export interface ProductBrochureDto {
  id: string; productId: string; version: string; originalFilename: string; blobPath: string; mimeType: string;
  fileSizeKb: number; contentHash: string; status: 'Draft' | 'Processing' | 'Ready' | 'Published' | 'Failed' | 'Archived';
  effectiveFrom: string; effectiveTo?: string; pageCount?: number; parentChunkCount?: number; childChunkCount?: number;
  ingestionErrorCode?: string; createdAt: string; publishedAt?: string;
}

export interface PolicyAssistantAvailability { available: boolean; state: string; brochureVersion?: string | null; effectiveFrom?: string | null; }
export interface PolicyAssistantCitation { index: number; pageNumber: number; sectionTitle?: string | null; clauseReference?: string | null; excerpt: string; }
export interface PolicyAssistantAnswer {
  requestId: string; conversationId: string; messageId: string; answer: string; evidenceStatus: 'Grounded' | 'InsufficientEvidence' | 'Rejected'; brochureVersion: string; citations: PolicyAssistantCitation[];
}
export interface PolicyAssistantMessage { id: string; role: 'User' | 'Assistant'; content: string; evidenceStatus?: string | null; citations: PolicyAssistantCitation[]; createdAt: string; }
export interface PolicyAssistantConversation { id: string; policyId: string; brochureId: string; brochureVersion: string; createdAt: string; updatedAt: string; messages?: PolicyAssistantMessage[] | null; }

export interface SpeedyAssistantResponse { requestId: string; answer: string; provider?: string | null; model?: string | null; }
export interface SpeedyWorkspaceAction {
  kind: 'navigate' | 'guided_kyc' | 'guided_quote' | 'guided_application' | 'guided_claim' | 'claim_status' | 'grievance_status' | 'policy_status' | 'claim_documents' | 'payment' | 'none'; label: string; route?: string | null; detail: string; requiresConfirmation: boolean;
}
export interface SpeedyWorkspaceResponse {
  requestId: string; answer: string; intent: string; risk: 'low' | 'regulated'; actions: SpeedyWorkspaceAction[];
  provider?: string | null; model?: string | null; conversationId?: string | null;
}
export interface SpeedyWorkspaceMessage {
  id: string; role: 'user' | 'assistant'; content: string; intent?: string | null; risk?: 'low' | 'regulated' | null;
  actions: SpeedyWorkspaceAction[]; createdAt: string;
}
export interface SpeedyWorkspaceConversation {
  id: string; title: string; createdAt: string; updatedAt: string; messages?: SpeedyWorkspaceMessage[] | null;
}
