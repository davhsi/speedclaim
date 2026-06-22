import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge';
import { MoneyPipe } from '../../../shared/pipes/money.pipe';
import { ToastService } from '../../../shared/components/toast/toast.service';
import { FinanceOfficerService } from '../services/finance-officer.service';
import { ClaimDto } from '../../../core/models/api.models';

@Component({
  selector: 'app-finance-officer-payouts',
  standalone: true,
  imports: [FormsModule, StatusBadgeComponent, MoneyPipe],
  templateUrl: './finance-officer-payouts.html',
})
export class FinanceOfficerPayoutsComponent implements OnInit {
  private financeService = inject(FinanceOfficerService);
  private toast = inject(ToastService);
  private moneyPipe = new MoneyPipe();

  allClaims = signal<ClaimDto[]>([]);
  statusFilter = 'All';
  processing = signal(new Set<number>());

  filteredClaims = computed(() => {
    if (this.statusFilter === 'All') return this.allClaims();
    return this.allClaims().filter(c => c.status === this.statusFilter);
  });

  ngOnInit(): void {
    this.loadClaims();
  }

  private loadClaims(): void {
    const statuses: string[] = ['Approved', 'Settled'];
    for (const status of statuses) {
      this.financeService.getClaimsForPayout(status as any).subscribe({
        next: (res) => {
          this.allClaims.update(list => {
            const existing = new Set(list.map(c => c.id));
            const newClaims = res.data.filter(c => !existing.has(c.id));
            return [...list, ...newClaims];
          });
        },
      });
    }
  }

  onFilterChange(): void {}

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

  processPayout(claim: ClaimDto): void {
    this.processing.update(s => new Set(s).add(claim.id));
    this.financeService.processClaimPayout(claim.id).subscribe({
      next: () => {
        this.allClaims.update(list => list.map(c => c.id === claim.id ? { ...c, status: 'PayoutProcessed' as any } : c));
        this.toast.success(`Stripe payout of ${this.moneyPipe.transform(claim.claimAmountApproved ?? claim.claimAmountRequested)} initiated`);
        this.processing.update(s => { const n = new Set(s); n.delete(claim.id); return n; });
      },
      error: () => {
        this.toast.error('Failed to process payout');
        this.processing.update(s => { const n = new Set(s); n.delete(claim.id); return n; });
      },
    });
  }

  markSettled(claim: ClaimDto): void {
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
