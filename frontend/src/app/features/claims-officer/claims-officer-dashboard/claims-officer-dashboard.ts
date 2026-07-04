import { Component, inject, signal, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { StatCardComponent } from '../../../shared/components/stat-card/stat-card';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge';
import { MoneyPipe } from '../../../shared/pipes/money.pipe';
import { DateFormatPipe } from '../../../shared/pipes/date-format.pipe';
import { ClaimsOfficerService } from '../services/claims-officer.service';
import { ClaimDto } from '../../../core/models/api.models';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-claims-officer-dashboard',
  standalone: true,
  imports: [StatCardComponent, StatusBadgeComponent, MoneyPipe, DateFormatPipe],
  templateUrl: './claims-officer-dashboard.html',
})
export class ClaimsOfficerDashboardComponent implements OnInit {
  private readonly claimsService = inject(ClaimsOfficerService);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  recentClaims = signal<ClaimDto[]>([]);
  newClaimsCount = signal(0);
  activeClaimsCount = signal(0);
  surveyorPendingCount = signal(0);
  openGrievancesCount = signal(0);

  iconClaims = '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75" stroke-linecap="round" stroke-linejoin="round"><path d="M14 2H6a2 2 0 00-2 2v16a2 2 0 002 2h12a2 2 0 002-2V8z"/><polyline points="14 2 14 8 20 8"/><line x1="16" y1="13" x2="8" y2="13"/></svg>';
  iconClock = '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"/><polyline points="12 6 12 12 16 14"/></svg>';
  iconSurveyor = '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75" stroke-linecap="round" stroke-linejoin="round"><path d="M17 21v-2a4 4 0 00-4-4H5a4 4 0 00-4 4v2"/><circle cx="9" cy="7" r="4"/><path d="M23 21v-2a4 4 0 00-3-3.87"/><path d="M16 3.13a4 4 0 010 7.75"/></svg>';
  iconGrievance = '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75" stroke-linecap="round" stroke-linejoin="round"><path d="M21 15a2 2 0 01-2 2H7l-4 4V5a2 2 0 012-2h14a2 2 0 012 2z"/></svg>';

  ngOnInit(): void {
    this.loadData();
  }

  private loadData(): void {
    this.claimsService.getAllClaims(1, 5).subscribe({
      next: (res) => {
        this.recentClaims.set(res.data);
      },
    });

    this.claimsService.getAllClaims(1, 100, 'Intimated').subscribe({
      next: (res) => this.newClaimsCount.set(res.totalRecords),
    });

    this.claimsService.getAllClaims(1, 1, 'UnderReview').subscribe({
      next: (res) => this.activeClaimsCount.set(res.totalRecords),
    });

    this.claimsService.getAllClaims(1, 100, 'UnderReview').subscribe({
      next: (res) => this.surveyorPendingCount.set(res.data.filter(c => !!c.surveyorId).length),
    });

    this.claimsService.getAllGrievances(1, 100).subscribe({
      next: (res) => {
        const openStatuses = new Set(['Open', 'InProgress', 'Escalated']);
        this.openGrievancesCount.set(res.data.filter(g => openStatuses.has(g.status)).length);
      },
    });
  }

  firstName(): string {
    return this.authService.currentUser()?.firstName ?? '';
  }

  greeting(): string {
    const h = new Date().getHours();
    if (h < 12) return 'Good morning';
    if (h < 17) return 'Good afternoon';
    return 'Good evening';
  }

  getTypeBgClass(type: string): string {
    const map: Record<string, string> = { Health: 'bg-success-bg', Death: 'bg-surface-alt', Maturity: 'bg-surface-alt', Accident: 'bg-info-bg', Theft: 'bg-danger-bg', NaturalDamage: 'bg-warning-bg' };
    return map[type] ?? 'bg-surface';
  }

  getTypeFgClass(type: string): string {
    const map: Record<string, string> = { Health: 'text-success', Death: 'text-muted', Maturity: 'text-muted', Accident: 'text-info', Theft: 'text-danger', NaturalDamage: 'text-warning' };
    return map[type] ?? 'text-muted';
  }

  getTypeAbbr(type: string): string {
    const map: Record<string, string> = { Health: 'HLT', Death: 'DTH', Maturity: 'MAT', Accident: 'ACC', Theft: 'THF', NaturalDamage: 'NAT' };
    return map[type] ?? type.substring(0, 3).toUpperCase();
  }

  openClaim(id: string): void {
    this.router.navigate(['/claims-officer/claims', id]);
  }

  navigateTo(path: string, queryParams?: Record<string, string>): void {
    this.router.navigate([path], queryParams ? { queryParams } : {});
  }
}
