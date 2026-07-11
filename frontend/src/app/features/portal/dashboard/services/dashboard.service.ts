import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, shareReplay } from 'rxjs';
import { PolicyDto, ClaimDto, PremiumScheduleDto } from '../../../../core/models/api.models';

@Injectable({ providedIn: 'root' })
export class DashboardService {
  private readonly http = inject(HttpClient);

  // Populated by prefetchDashboard() right after login, before the dashboard route
  // even loads. Each entry is consumed (and cleared) by its first real getter call,
  // so a prefetch only ever short-circuits the *next* dashboard load, never later
  // visits in the same session.
  private prefetchedPolicies$?: Observable<PolicyDto[]>;
  private prefetchedClaims$?: Observable<ClaimDto[]>;
  private prefetchedSchedules = new Map<string, Observable<PremiumScheduleDto[]>>();

  /**
   * Optimistically starts fetching the customer dashboard's data as soon as we know
   * login succeeded (see CLAUDE.md-adjacent discussion: safe post-auth analogue of the
   * "assume success, start fetching" pattern — unlike a pre-auth version, this can't leak
   * data or double-count anything since it only runs once a real JWT exists).
   */
  prefetchDashboard(): void {
    this.prefetchedPolicies$ = this.http.get<PolicyDto[]>('/api/v1/policies/my').pipe(shareReplay(1));
    this.prefetchedClaims$ = this.http.get<ClaimDto[]>('/api/v1/claims/my').pipe(shareReplay(1));

    this.prefetchedPolicies$.subscribe({
      next: policies => {
        for (const policy of policies.filter(p => p.status === 'Active')) {
          const schedule$ = this.http
            .get<PremiumScheduleDto[]>(`/api/v1/payments/schedule/${policy.id}`)
            .pipe(shareReplay(1));
          schedule$.subscribe({ error: () => {} });
          this.prefetchedSchedules.set(policy.id, schedule$);
        }
      },
      error: () => {},
    });
    this.prefetchedClaims$.subscribe({ error: () => {} });
  }

  getPolicies(): Observable<PolicyDto[]> {
    const prefetched = this.prefetchedPolicies$;
    this.prefetchedPolicies$ = undefined;
    return prefetched ?? this.http.get<PolicyDto[]>('/api/v1/policies/my');
  }

  getClaims(): Observable<ClaimDto[]> {
    const prefetched = this.prefetchedClaims$;
    this.prefetchedClaims$ = undefined;
    return prefetched ?? this.http.get<ClaimDto[]>('/api/v1/claims/my');
  }

  getSchedule(policyId: string): Observable<PremiumScheduleDto[]> {
    const prefetched = this.prefetchedSchedules.get(policyId);
    if (prefetched) {
      this.prefetchedSchedules.delete(policyId);
      return prefetched;
    }
    return this.http.get<PremiumScheduleDto[]>(`/api/v1/payments/schedule/${policyId}`);
  }
}
