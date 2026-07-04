import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge';
import { MoneyPipe } from '../../../shared/pipes/money.pipe';
import { ToastService } from '../../../shared/components/toast/toast.service';
import { FinanceOfficerService } from '../services/finance-officer.service';
import { ClaimDto } from '../../../core/models/api.models';
import { PaginationComponent } from '../../../shared/components/pagination/pagination';
import { SkeletonLoaderComponent } from '../../../shared/components/skeleton-loader/skeleton-loader';

@Component({
  selector: 'app-finance-officer-payouts',
  standalone: true,
  imports: [FormsModule, StatusBadgeComponent, MoneyPipe, PaginationComponent, SkeletonLoaderComponent],
  templateUrl: './finance-officer-payouts.html',
})
export class FinanceOfficerPayoutsComponent implements OnInit {
  private readonly financeService = inject(FinanceOfficerService);
  private readonly toast = inject(ToastService);
  private readonly moneyPipe = new MoneyPipe();

  allClaims = signal<ClaimDto[]>([]);
  loading = signal(true);
  statusFilter = signal('All');
  processing = signal(new Set<string>());
  currentPage = signal(1);
  readonly pageSize = 10;

  filteredClaims = computed(() => {
    const status = this.statusFilter();
    if (status === 'All') return this.allClaims();
    return this.allClaims().filter(c => c.status === status);
  });

  totalPages = computed(() => Math.max(1, Math.ceil(this.filteredClaims().length / this.pageSize)));

  pagedClaims = computed(() => {
    const start = (this.currentPage() - 1) * this.pageSize;
    return this.filteredClaims().slice(start, start + this.pageSize);
  });

  ngOnInit(): void {
    this.loadClaims();
  }

  private loadClaims(): void {
    const statuses: string[] = ['Approved', 'Settled'];
    let remaining = statuses.length;
    for (const status of statuses) {
      this.financeService.getClaimsForPayout(status as any).subscribe({
        next: (res) => {
          this.allClaims.update(list => {
            const existing = new Set(list.map(c => c.id));
            const newClaims = res.data.filter(c => !existing.has(c.id));
            return [...list, ...newClaims];
          });
          remaining -= 1;
          if (remaining === 0) this.loading.set(false);
        },
        error: () => {
          remaining -= 1;
          if (remaining === 0) this.loading.set(false);
        },
      });
    }
  }

  onFilterChange(): void { this.currentPage.set(1); }
  onPageChange(page: number): void { this.currentPage.set(page); }

  typeBadge(type: string): string {
    const map: Record<string, string> = {
      Health: 'bg-success-bg text-success',
      Motor: 'bg-info-bg text-info',
      Life: 'bg-surface-alt text-muted',
      Death: 'bg-surface-alt text-muted',
      Accident: 'bg-info-bg text-info',
      Theft: 'bg-danger-bg text-danger',
      NaturalDamage: 'bg-warning-bg text-warning',
    };
    return map[type] ?? 'bg-surface-alt text-muted';
  }

  claimsSummary(): string {
    const count = this.filteredClaims().length;
    const noun = count === 1 ? 'claim' : 'claims';
    return `${count} ${noun} — process Stripe payouts or mark approved claims settled manually.`;
  }

  processPayout(claim: ClaimDto): void {
    if (this.processing().has(claim.id)) return;
    this.processing.update(s => new Set(s).add(claim.id));
    this.financeService.processClaimPayout(claim.id).subscribe({
      next: () => {
        this.allClaims.update(list => list.map(c => c.id === claim.id ? { ...c, status: 'Settled' as any } : c));
        this.toast.success(`Stripe payout of ${this.moneyPipe.transform(claim.claimAmountApproved ?? claim.claimAmountRequested)} initiated and claim settled`);
        this.processing.update(s => { const n = new Set(s); n.delete(claim.id); return n; });
      },
      error: () => {
        this.toast.error('Failed to process payout');
        this.processing.update(s => { const n = new Set(s); n.delete(claim.id); return n; });
      },
    });
  }

  markSettled(claim: ClaimDto): void {
    if (this.processing().has(claim.id)) return;
    this.processing.update(s => new Set(s).add(claim.id));
    this.financeService.markClaimSettled(claim.id).subscribe({
      next: () => {
        this.allClaims.update(list => list.map(c => c.id === claim.id ? { ...c, status: 'Settled' as any } : c));
        this.toast.success(`Claim ${claim.claimNumber} marked as financially settled`);
        this.processing.update(s => { const n = new Set(s); n.delete(claim.id); return n; });
      },
      error: () => {
        this.toast.error('Failed to mark claim as settled');
        this.processing.update(s => { const n = new Set(s); n.delete(claim.id); return n; });
      },
    });
  }
}
