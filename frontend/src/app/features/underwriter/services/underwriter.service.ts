import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  ProposalDto, PolicyDto, PolicyStatusHistoryDto, EndorsementDto,
  PagedResponse, ApiMessage,
} from '../../../core/models/api.models';

export interface UnderwriterKycDto {
  id: string;
  userId: string;
  kycStatus: string;
  aadhaarUploaded: boolean;
  aadhaarNumber?: string;
  aadhaarFrontPath?: string;
  aadhaarBackPath?: string;
  panUploaded: boolean;
  panNumber?: string;
  panFrontPath?: string;
  panBackPath?: string;
  rejectionReason?: string;
  createdAt: string;
}

export interface ReviewProposalRequest {
  isApproved: boolean;
  notes: string;
}

export interface ApproveRejectEndorsementRequest {
  isApproved: boolean;
  reason: string;
}

@Injectable({ providedIn: 'root' })
export class UnderwriterService {
  private http = inject(HttpClient);

  // ── Proposals ──
  getAllProposals(): Observable<ProposalDto[]> {
    return this.http.get<ProposalDto[]>('/api/v1/proposals/all');
  }

  getProposalById(id: string): Observable<ProposalDto> {
    return this.http.get<ProposalDto>(`/api/v1/proposals/${id}`);
  }

  reviewProposal(id: string, request: ReviewProposalRequest): Observable<ApiMessage> {
    return this.http.post<ApiMessage>(`/api/v1/proposals/${id}/review`, request);
  }

  requestDocs(id: string, details: string): Observable<ApiMessage> {
    return this.http.post<ApiMessage>(`/api/v1/proposals/${id}/request-docs`, { details });
  }

  updateNotes(id: string, notes: string): Observable<ApiMessage> {
    return this.http.patch<ApiMessage>(`/api/v1/proposals/${id}/notes`, { notes });
  }

  // ── KYC ──
  getPendingKyc(page = 1, pageSize = 10): Observable<PagedResponse<UnderwriterKycDto>> {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return this.http.get<PagedResponse<UnderwriterKycDto>>('/api/v1/users/kyc/pending', { params });
  }

  getKycByUserId(customerId: string): Observable<UnderwriterKycDto> {
    return this.http.get<UnderwriterKycDto>(`/api/v1/users/${customerId}/kyc`);
  }

  reviewKyc(customerId: string, isApproved: boolean, reason: string): Observable<UnderwriterKycDto> {
    const params = new HttpParams().set('isApproved', isApproved).set('reason', reason);
    return this.http.put<UnderwriterKycDto>(`/api/v1/users/${customerId}/kyc/review`, {}, { params });
  }

  // ── Endorsements ──
  getPendingEndorsements(page = 1, pageSize = 20): Observable<PagedResponse<EndorsementDto>> {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return this.http.get<PagedResponse<EndorsementDto>>('/api/v1/policies/endorsements/pending', { params });
  }

  reviewEndorsement(endorsementId: string, request: ApproveRejectEndorsementRequest): Observable<ApiMessage> {
    return this.http.put<ApiMessage>(`/api/v1/policies/endorsements/${endorsementId}/review`, request);
  }

  // ── Policies ──
  getAllPolicies(page = 1, pageSize = 20): Observable<PagedResponse<PolicyDto>> {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return this.http.get<PagedResponse<PolicyDto>>('/api/v1/policies/all', { params });
  }

  getPolicyById(id: string): Observable<PolicyDto> {
    return this.http.get<PolicyDto>(`/api/v1/policies/${id}`);
  }

  getPolicyHistory(id: string): Observable<PolicyStatusHistoryDto[]> {
    return this.http.get<PolicyStatusHistoryDto[]>(`/api/v1/policies/${id}/history`);
  }

  // ── Profile ──
  updateProfile(data: { firstName: string; lastName: string; phone: string }): Observable<ApiMessage> {
    return this.http.patch<ApiMessage>('/api/v1/users/profile', data);
  }

  requestPasswordReset(email: string): Observable<ApiMessage> {
    return this.http.post<ApiMessage>('/api/v1/auth/forgot-password', { email });
  }
}
