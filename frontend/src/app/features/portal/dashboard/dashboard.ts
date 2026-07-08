import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { RouterLink } from '@angular/router';
import { catchError, forkJoin, of } from 'rxjs';
import { DashboardService } from './services/dashboard.service';
import { NotificationService } from '../../../core/services/notification.service';
import { AuthService } from '../../../core/services/auth.service';
import { ProfileService } from '../profile/services/profile.service';
import { PolicyDto, ClaimDto, PremiumScheduleDto, ProductDto, KycRecordDto } from '../../../core/models/api.models';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge';
import { SkeletonLoaderComponent } from '../../../shared/components/skeleton-loader/skeleton-loader';
import { MoneyPipe } from '../../../shared/pipes/money.pipe';
import { ProductService } from '../products/services/product.service';
import { SafeHtmlPipe } from '../../../shared/pipes/safe-html.pipe';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [RouterLink, StatusBadgeComponent, SkeletonLoaderComponent, MoneyPipe, SafeHtmlPipe],
  templateUrl: './dashboard.html',
})
export class DashboardComponent implements OnInit {
  private readonly dashService = inject(DashboardService);
  private readonly authService = inject(AuthService);
  private readonly productService = inject(ProductService);
  private readonly profileService = inject(ProfileService);
  notifService = inject(NotificationService);

  loading = signal(true);
  policies = signal<PolicyDto[]>([]);
  products = signal<ProductDto[]>([]);
  claims = signal<ClaimDto[]>([]);
  nextDue = signal<PremiumScheduleDto | null>(null);
  kyc = signal<KycRecordDto | null>(null);

  activePoliciesCount = signal(0);
  openClaimsCount = signal(0);

  kycPending = computed(() => !this.kyc() || this.kyc()?.kycStatus !== 'Approved');
  isNewUser = computed(() => !this.loading() && this.policies().length === 0);
  journeyStep = computed(() => {
    if (this.activePoliciesCount() > 0) return 3;
    if (this.kyc()?.kycStatus === 'Approved') return 2;
    return 1;
  });

  actionClaims = computed(() =>
    this.claims().filter(c => c.status === 'DocumentsPending')
  );
  isPremiumOverdue = computed(() => this.nextDue()?.status === 'Overdue');
  isPremiumDueSoon = computed(() => {
    const d = this.nextDue();
    if (!d || d.status === 'Overdue') return false;
    const daysLeft = Math.ceil((new Date(d.dueDate).getTime() - Date.now()) / 86400000);
    return daysLeft <= 7;
  });

  firstName(): string {
    return this.authService.currentUser()?.firstName ?? 'there';
  }

  ngOnInit(): void {
    this.profileService.getKyc().subscribe({ next: k => this.kyc.set(k), error: () => {} });
    this.productService.getAll().subscribe(products => this.products.set(products));
    forkJoin({
      policies: this.dashService.getPolicies(),
      claims: this.dashService.getClaims(),
    }).subscribe({
      next: ({ policies, claims }) => {
        this.policies.set(policies);
        this.claims.set(claims);
        this.activePoliciesCount.set(policies.filter(p => p.status === 'Active').length);
        this.openClaimsCount.set(claims.filter(c => !['Settled', 'Rejected', 'Withdrawn'].includes(c.status)).length);

        this.loadNextPremium(policies.filter(p => p.status === 'Active'));
      },
      error: () => this.loading.set(false),
    });
  }

  private loadNextPremium(activePolicies: PolicyDto[]): void {
    if (activePolicies.length === 0) {
      this.nextDue.set(null);
      this.loading.set(false);
      return;
    }

    forkJoin(activePolicies.map(policy =>
      this.dashService.getSchedule(policy.id).pipe(catchError(() => of([] as PremiumScheduleDto[]))),
    )).subscribe({
      next: schedulesByPolicy => {
        this.nextDue.set(this.findNextPremium(schedulesByPolicy.flat()));
        this.loading.set(false);
      },
      error: () => {
        this.nextDue.set(null);
        this.loading.set(false);
      },
    });
  }

  private findNextPremium(schedules: PremiumScheduleDto[]): PremiumScheduleDto | null {
    return schedules
      .filter(s => s.status === 'Overdue' || s.status === 'Due' || s.status === 'Upcoming')
      .sort((a, b) => new Date(a.dueDate).getTime() - new Date(b.dueDate).getTime())[0] ?? null;
  }

  nextPremiumDisplay(): string {
    const due = this.nextDue();
    if (!due) return 'None';
    return `₹${due.amountDue.toLocaleString('en-IN')}`;
  }

  nextPremiumDate(): string {
    const due = this.nextDue();
    if (!due) return 'No upcoming payments';
    const dateStr = new Date(due.dueDate).toLocaleDateString('en-IN', { day: '2-digit', month: 'short', year: 'numeric' });
    if (due.status === 'Overdue') return `Overdue since ${dateStr} — policy at risk`;
    const daysLeft = Math.ceil((new Date(due.dueDate).getTime() - Date.now()) / 86400000);
    if (daysLeft <= 7) return `Due in ${daysLeft} day${daysLeft === 1 ? '' : 's'} — pay before ${dateStr}`;
    return `Due: ${dateStr}`;
  }

  productName(policy: PolicyDto): string {
    return this.products().find(p => p.id === policy.productId)?.productName
      ?? policy.productName
      ?? 'Insurance product';
  }

  displayDomain(policyOrDomain: PolicyDto | string): string {
    if (typeof policyOrDomain === 'string') return policyOrDomain;
    return this.products().find(p => p.id === policyOrDomain.productId)?.domain
      ?? policyOrDomain.domain
      ?? 'Unknown';
  }

  domainBgClass(policyOrDomain: PolicyDto | string): string {
    const map: Record<string, string> = {
      HEALTH: 'bg-success-bg',
      MOTOR: 'bg-info-bg',
      LIFE: 'bg-[#F3EEFF]',
    };
    return map[this.displayDomain(policyOrDomain).toUpperCase()] ?? 'bg-surface-alt';
  }

  domainIcon(policyOrDomain: PolicyDto | string): string {
    const map: Record<string, string> = {
      HEALTH: '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="#1F9D6B" stroke-width="1.75"><path d="M22 12h-4l-3 9L9 3l-3 9H2"/></svg>',
      MOTOR: '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="#2D7FF9" stroke-width="1.75"><circle cx="7" cy="17" r="2"/><circle cx="17" cy="17" r="2"/><path d="M5 17H3v-6l2-5h9l4 5h3v6h-2"/></svg>',
      LIFE: '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="#7C3AED" stroke-width="1.75"><path d="M20.84 4.61a5.5 5.5 0 00-7.78 0L12 5.67l-1.06-1.06a5.5 5.5 0 00-7.78 7.78l1.06 1.06L12 21.23l7.78-7.78 1.06-1.06a5.5 5.5 0 000-7.78z"/></svg>',
    };
    return map[this.displayDomain(policyOrDomain).toUpperCase()] ?? '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75"><circle cx="12" cy="12" r="10"/></svg>';
  }
}
