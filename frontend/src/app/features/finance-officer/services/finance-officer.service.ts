import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  FinancePaymentRecordDto, AgentCommissionDto, ClaimDto,
  PaymentSummaryDto, OverduePolicyDto, ApiMessage, PagedResponse,
} from '../../../core/models/api.models';
import { ClaimStatus } from '../../../core/models/enums';

@Injectable({ providedIn: 'root' })
export class FinanceOfficerService {
  private http = inject(HttpClient);

  getAllPaymentRecords(): Observable<FinancePaymentRecordDto[]> {
    return this.http.get<FinancePaymentRecordDto[]>('/api/v1/payments/all-records');
  }

  reconcilePayment(paymentId: string): Observable<ApiMessage> {
    return this.http.put<ApiMessage>(`/api/v1/payments/${paymentId}/reconcile`, {});
  }

  refundPayment(paymentId: string): Observable<ApiMessage> {
    return this.http.post<ApiMessage>(`/api/v1/payments/${paymentId}/refund`, {});
  }

  getClaimsForPayout(status?: ClaimStatus): Observable<PagedResponse<ClaimDto>> {
    let params = new HttpParams().set('page', 1).set('pageSize', 100);
    if (status) params = params.set('status', status);
    return this.http.get<PagedResponse<ClaimDto>>('/api/v1/claims/all', { params });
  }

  processClaimPayout(claimId: string): Observable<ApiMessage> {
    return this.http.post<ApiMessage>(`/api/v1/payments/payout/claim/${claimId}`, {});
  }

  markClaimSettled(claimId: string): Observable<ApiMessage> {
    return this.http.put<ApiMessage>(`/api/v1/payments/claims/${claimId}/settle`, {});
  }

  getPendingCommissions(): Observable<AgentCommissionDto[]> {
    return this.http.get<AgentCommissionDto[]>('/api/v1/payments/commissions/pending');
  }

  approveCommission(id: number): Observable<ApiMessage> {
    return this.http.post<ApiMessage>(`/api/v1/payments/commissions/${id}/approve`, {});
  }

  getOverduePolicies(): Observable<OverduePolicyDto[]> {
    return this.http.get<OverduePolicyDto[]>('/api/v1/payments/reports/overdue');
  }

  getCollectionSummary(period: string): Observable<PaymentSummaryDto> {
    const params = new HttpParams().set('period', period);
    return this.http.get<PaymentSummaryDto>('/api/v1/payments/reports/summary', { params });
  }

  exportPaymentReport(from?: string, to?: string): Observable<Blob> {
    let params = new HttpParams();
    if (from) params = params.set('from', from);
    if (to) params = params.set('to', to);
    return this.http.get('/api/v1/payments/reports/export', { params, responseType: 'blob' });
  }

  updateProfile(payload: { name: string; email: string; phone: string }): Observable<ApiMessage> {
    return this.http.put<ApiMessage>('/api/v1/users/profile', payload);
  }
}
