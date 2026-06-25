import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  ApiMessage, PolicyDto, ProposalDto, SubmitProposalRequest,
  GenerateQuoteRequest, GenerateQuoteResponse,
  ProductDto,
} from '../../../core/models/api.models';

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
}

export interface RenewalReminderDto {
  policyId: string;
  policyNumber: string;
  customerId: string;
  customerName: string;
  dueDate: string;
  amountDue: number;
  daysUntilDue: number;
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
  fullName: string;
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
  private http = inject(HttpClient);

  getDashboard(): Observable<AgentDashboardDto> {
    return this.http.get<AgentDashboardDto>('/api/v1/agents/dashboard');
  }

  getCustomers(): Observable<AgentCustomerDto[]> {
    return this.http.get<AgentCustomerDto[]>('/api/v1/agents/customers');
  }

  getProfile(): Observable<AgentProfileDto> {
    return this.http.get<AgentProfileDto>('/api/v1/agents/profile');
  }

  updateProfile(req: { salutation: string; firstName: string; lastName: string; phone: string }): Observable<ApiMessage> {
    return this.http.put<ApiMessage>('/api/v1/agents/profile', req);
  }

  getRenewals(): Observable<RenewalReminderDto[]> {
    return this.http.get<RenewalReminderDto[]>('/api/v1/agents/renewals');
  }

  getAssignedPolicies(): Observable<PolicyDto[]> {
    return this.http.get<PolicyDto[]>('/api/v1/policies/assigned');
  }

  getMyProposals(): Observable<ProposalDto[]> {
    return this.http.get<ProposalDto[]>('/api/v1/proposals/my');
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
    return this.http.get<AgentCommissionDto[]>('/api/v1/payments/commissions/pending');
  }
}
