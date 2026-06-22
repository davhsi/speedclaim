import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge';
import { ConfirmDialogComponent } from '../../../shared/components/confirm-dialog/confirm-dialog';
import { MoneyPipe } from '../../../shared/pipes/money.pipe';
import { DateFormatPipe } from '../../../shared/pipes/date-format.pipe';
import { ToastService } from '../../../shared/components/toast/toast.service';
import { FinanceOfficerService } from '../services/finance-officer.service';
import { AgentCommissionDto } from '../../../core/models/api.models';

@Component({
  selector: 'app-finance-officer-commissions',
  standalone: true,
  imports: [StatusBadgeComponent, ConfirmDialogComponent, MoneyPipe, DateFormatPipe],
  templateUrl: './finance-officer-commissions.html',
})
export class FinanceOfficerCommissionsComponent implements OnInit {
  private financeService = inject(FinanceOfficerService);
  private toast = inject(ToastService);
  private moneyPipe = new MoneyPipe();

  allCommissions = signal<AgentCommissionDto[]>([]);
  page = signal(0);
  pageSize = 8;

  dialogTarget = signal<AgentCommissionDto | null>(null);
  dialogMessage = signal('');

  pendingCount = computed(() => this.allCommissions().filter(c => c.status === 'Pending').length);
  pendingTotal = computed(() => {
    const total = this.allCommissions().filter(c => c.status === 'Pending').reduce((sum, c) => sum + c.commissionAmount, 0);
    return this.moneyPipe.transform(total);
  });

  totalPages = computed(() => Math.max(1, Math.ceil(this.allCommissions().length / this.pageSize)));

  pagedCommissions = computed(() => {
    const start = this.page() * this.pageSize;
    return this.allCommissions().slice(start, start + this.pageSize);
  });

  pageLabel = computed(() => {
    const all = this.allCommissions();
    if (all.length === 0) return 'No records';
    const start = this.page() * this.pageSize + 1;
    const end = Math.min(start + this.pageSize - 1, all.length);
    return `Showing ${start}–${end} of ${all.length}`;
  });

  ngOnInit(): void {
    this.financeService.getPendingCommissions().subscribe({
      next: (comms) => this.allCommissions.set(comms),
    });
  }

  prevPage(): void { this.page.update(p => Math.max(0, p - 1)); }
  nextPage(): void { this.page.update(p => Math.min(this.totalPages() - 1, p + 1)); }

  typeBadge(domain: string): string {
    const map: Record<string, string> = {
      Health: 'bg-success-bg text-success',
      Motor: 'bg-info-bg text-info',
      Life: 'bg-surface-alt text-muted',
    };
    return map[domain] ?? 'bg-surface-alt text-muted';
  }

  openApprove(c: AgentCommissionDto): void {
    this.dialogMessage.set(`Approve and pay commission of ${this.moneyPipe.transform(c.commissionAmount)} to ${c.agentName} (AGT-${c.agentId}) for policy ${c.policyNumber}.`);
    this.dialogTarget.set(c);
  }

  onConfirmApprove(): void {
    const target = this.dialogTarget();
    if (!target) return;

    this.financeService.approveCommission(target.id).subscribe({
      next: () => {
        this.allCommissions.update(list => list.map(c => c.id === target.id ? { ...c, status: 'Paid' } : c));
        this.toast.success(`Commission of ${this.moneyPipe.transform(target.commissionAmount)} approved and paid to ${target.agentName}`);
      },
      error: () => this.toast.error('Failed to approve commission'),
    });

    this.dialogTarget.set(null);
  }
}
