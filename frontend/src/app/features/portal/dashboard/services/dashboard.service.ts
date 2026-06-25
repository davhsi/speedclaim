import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { PolicyDto, ClaimDto, PremiumScheduleDto } from '../../../../core/models/api.models';

@Injectable({ providedIn: 'root' })
export class DashboardService {
  private http = inject(HttpClient);

  getPolicies(): Observable<PolicyDto[]> {
    return this.http.get<PolicyDto[]>('/api/v1/policies/my');
  }

  getClaims(): Observable<ClaimDto[]> {
    return this.http.get<ClaimDto[]>('/api/v1/claims/my');
  }

  getSchedule(policyId: string): Observable<PremiumScheduleDto[]> {
    return this.http.get<PremiumScheduleDto[]>(`/api/v1/payments/schedule/${policyId}`);
  }
}
