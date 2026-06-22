import { Component, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { DashboardService } from './services/dashboard.service';
import { NotificationService } from '../../../core/services/notification.service';
import { AuthService } from '../../../core/services/auth.service';
import { PolicyDto, ClaimDto, PremiumScheduleDto } from '../../../core/models/api.models';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge';
import { SkeletonLoaderComponent } from '../../../shared/components/skeleton-loader/skeleton-loader';
import { MoneyPipe } from '../../../shared/pipes/money.pipe';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [RouterLink, StatusBadgeComponent, SkeletonLoaderComponent, MoneyPipe],
  templateUrl: './dashboard.html',
})
export class DashboardComponent implements OnInit {
  private dashService = inject(DashboardService);
  private authService = inject(AuthService);
  notifService = inject(NotificationService);

  loading = signal(true);
  policies = signal<PolicyDto[]>([]);
  claims = signal<ClaimDto[]>([]);
  nextDue = signal<PremiumScheduleDto | null>(null);

  activePoliciesCount = signal(0);
  openClaimsCount = signal(0);

  firstName(): string {
    return this.authService.currentUser()?.firstName ?? 'there';
  }

  ngOnInit(): void {
    forkJoin({
      policies: this.dashService.getPolicies(),
      claims: this.dashService.getClaims(),
    }).subscribe({
      next: ({ policies, claims }) => {
        this.policies.set(policies);
        this.claims.set(claims);
        this.activePoliciesCount.set(policies.filter(p => p.status === 'Active').length);
        this.openClaimsCount.set(claims.filter(c => !['Settled', 'Rejected', 'Withdrawn'].includes(c.status)).length);

        const activePolicy = policies.find(p => p.status === 'Active');
        if (activePolicy) {
          this.dashService.getSchedule(activePolicy.id).subscribe(schedule => {
            const due = schedule.find(s => s.status === 'Due' || s.status === 'Overdue');
            this.nextDue.set(due ?? null);
          });
        }
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  nextPremiumDisplay(): string {
    const due = this.nextDue();
    if (!due) return 'None';
    return `₹${due.amountDue.toLocaleString('en-IN')}`;
  }

  nextPremiumDate(): string {
    const due = this.nextDue();
    if (!due) return 'No upcoming payments';
    return `Due: ${new Date(due.dueDate).toLocaleDateString('en-IN', { day: '2-digit', month: 'short', year: 'numeric' })}`;
  }

  domainBgClass(domain: string): string {
    const map: Record<string, string> = {
      Health: 'bg-success-bg',
      Motor: 'bg-info-bg',
      Life: 'bg-[#F3EEFF]',
    };
    return map[domain] ?? 'bg-surface-alt';
  }

  domainIcon(domain: string): string {
    const map: Record<string, string> = {
      Health: '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="#1F9D6B" stroke-width="1.75"><path d="M22 12h-4l-3 9L9 3l-3 9H2"/></svg>',
      Motor: '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="#2D7FF9" stroke-width="1.75"><circle cx="7" cy="17" r="2"/><circle cx="17" cy="17" r="2"/><path d="M5 17H3v-6l2-5h9l4 5h3v6h-2"/></svg>',
      Life: '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="#7C3AED" stroke-width="1.75"><path d="M20.84 4.61a5.5 5.5 0 00-7.78 0L12 5.67l-1.06-1.06a5.5 5.5 0 00-7.78 7.78l1.06 1.06L12 21.23l7.78-7.78 1.06-1.06a5.5 5.5 0 000-7.78z"/></svg>',
    };
    return map[domain] ?? '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75"><circle cx="12" cy="12" r="10"/></svg>';
  }
}
