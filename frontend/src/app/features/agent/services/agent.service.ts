import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpContext, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  ApiMessage, PolicyDto, ProposalDto, SubmitProposalRequest,
  GenerateQuoteRequest, GenerateQuoteResponse,
  ProductDto, AgentAddCustomerRequest, RegistrationResponse, KycRecordDto,
  DocumentRequirementDto,
} from '../../../core/models/api.models';
import { SKIP_ERROR_TOAST } from '../../../core/interceptors/error.interceptor';

export interface AgentDashboardDto {
  totalCustomers: number;
  totalPolicies: number;
  totalCommission: number;
  pendingClaims: number;
}

export interface AgentCustomerDto {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  fullName: string;
  phone: string;
  role: string;
  isActive: boolean;
  createdAt: string;
  customerId?: string;
  kycApproved?: boolean;
  kycStatus?: string;
  kycRejectionReason?: string;
  dateOfBirth?: string | null;
  occupation?: string | null;
  annualIncome?: number | null;
}

export interface RenewalReminderDto {
  policyId: string;
  policyNumber: string;
  customerId: string;
  customerName: string;
  customerEmail: string;
  customerPhone: string;
  dueDate: string;
  amountDue: number;
  daysUntilDue: number;
  reminderSentRecently: boolean;
}

export interface AgentCommissionDto {
  id: string;
  agentId: string;
  policyId: string;
  commissionAmount: number;
  status: string;
  paidAt?: string;
  createdAt: string;
}

export interface AgentProfileDto {
  agentId: string;
  userId: string;
  email: string;
  salutation: string;
  firstName: string;
  lastName: string;
  fullName: string;
  phone: string;
  agentCode: string;
  agentType: string;
  licenseNumber: string;
  licenseExpiry: string;
  commissionRate: number;
  isActive: boolean;
  branchName?: string;
  branchCity?: string;
}

@Injectable({ providedIn: 'root' })
export class AgentService {
  private readonly http = inject(HttpClient);

  getDashboard(): Observable<AgentDashboardDto> {
    return this.http.get<AgentDashboardDto>('/api/v1/agents/dashboard');
  }

  getCustomers(): Observable<AgentCustomerDto[]> {
    return this.http.get<AgentCustomerDto[]>('/api/v1/agents/customers');
  }

  searchCustomers(query: string): Observable<AgentCustomerDto[]> {
    return this.http.get<AgentCustomerDto[]>('/api/v1/agents/customers/search', { params: new HttpParams().set('q', query) });
  }

  getCustomerKyc(customerId: string): Observable<KycRecordDto | null> {
    return this.http.get<KycRecordDto | null>(`/api/v1/agents/customers/${customerId}/kyc`, {
      context: new HttpContext().set(SKIP_ERROR_TOAST, true),
    });
  }

  getProductDocuments(productId: string): Observable<DocumentRequirementDto[]> {
    return this.http.get<DocumentRequirementDto[]>(`/api/v1/products/${productId}/documents`);
  }

  uploadProposalDocument(proposalId: string, documentKey: string, file: File): Observable<ApiMessage> {
    const fd = new FormData();
    fd.append('file', file);
    return this.http.put<ApiMessage>(`/api/v1/proposals/${proposalId}/documents/${documentKey}`, fd);
  }

  addCustomer(req: AgentAddCustomerRequest): Observable<RegistrationResponse> {
    return this.http.post<RegistrationResponse>('/api/v1/auth/agent/add-customer', req);
  }

  getProfile(): Observable<AgentProfileDto> {
    return this.http.get<AgentProfileDto>('/api/v1/agents/profile');
  }

  updateProfile(req: { salutation: string; firstName: string; lastName: string; phone: string }): Observable<ApiMessage> {
    return this.http.patch<ApiMessage>('/api/v1/agents/profile', req);
  }

  getRenewals(): Observable<RenewalReminderDto[]> {
    return this.http.get<RenewalReminderDto[]>('/api/v1/agents/renewals');
  }

  sendRenewalReminder(policyId: string): Observable<ApiMessage> {
    return this.http.post<ApiMessage>(`/api/v1/agents/renewals/${policyId}/reminder`, {}, {
      context: new HttpContext().set(SKIP_ERROR_TOAST, true),
    });
  }

  getAssignedPolicies(): Observable<PolicyDto[]> {
    return this.http.get<PolicyDto[]>('/api/v1/policies/assigned');
  }

  remindCustomerToPay(policyId: string): Observable<ApiMessage> {
    return this.http.post<ApiMessage>(`/api/v1/policies/${policyId}/payment-reminder`, {}, {
      context: new HttpContext().set(SKIP_ERROR_TOAST, true),
    });
  }

  getMyProposals(): Observable<ProposalDto[]> {
    return this.http.get<ProposalDto[]>('/api/v1/proposals/my');
  }

  getProposalById(id: string): Observable<ProposalDto> {
    return this.http.get<ProposalDto>(`/api/v1/proposals/${id}`);
  }

  submitProposal(req: SubmitProposalRequest): Observable<ProposalDto> {
    return this.http.post<ProposalDto>('/api/v1/proposals', req);
  }

  getProducts(): Observable<ProductDto[]> {
    return this.http.get<ProductDto[]>('/api/v1/products');
  }

  generateQuote(req: GenerateQuoteRequest): Observable<GenerateQuoteResponse> {
    return this.http.post<GenerateQuoteResponse>('/api/v1/proposals/quote', req);
  }

  getCommissions(): Observable<AgentCommissionDto[]> {
    return this.http.get<AgentCommissionDto[]>('/api/v1/agents/commissions');
  }

  withdrawProposal(id: string): Observable<ApiMessage> {
    return this.http.put<ApiMessage>(`/api/v1/proposals/${id}/withdraw`, {});
  }
}
