import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge';
import { ConfirmDialogComponent } from '../../../shared/components/confirm-dialog/confirm-dialog';
import { MoneyPipe } from '../../../shared/pipes/money.pipe';
import { DateFormatPipe } from '../../../shared/pipes/date-format.pipe';
import { ToastService } from '../../../shared/components/toast/toast.service';
import { FinanceOfficerService } from '../services/finance-officer.service';
import { FinancePaymentRecordDto } from '../../../core/models/api.models';
import { PaginationComponent } from '../../../shared/components/pagination/pagination';

@Component({
  selector: 'app-finance-officer-payments',
  standalone: true,
  imports: [FormsModule, StatusBadgeComponent, ConfirmDialogComponent, MoneyPipe, DateFormatPipe, PaginationComponent],
  templateUrl: './finance-officer-payments.html',
})
export class FinanceOfficerPaymentsComponent implements OnInit {
  private readonly financeService = inject(FinanceOfficerService);
  private readonly toast = inject(ToastService);
  private readonly moneyPipe = new MoneyPipe();

  allRecords = signal<FinancePaymentRecordDto[]>([]);
  loading = signal(true);
  searchQuery = '';
  statusFilter = 'All';
  currentPage = signal(1);
  readonly pageSize = 8;

  dialogTarget = signal<FinancePaymentRecordDto | null>(null);
  dialogAction = signal<'reconcile' | 'refund'>('reconcile');
  dialogTitle = signal('');
  dialogMessage = signal('');
  dialogConfirmLabel = signal('');
  dialogVariant = signal<'danger' | 'default'>('default');
  actionInFlight = signal(false);

  filteredRecords = computed(() => {
    const q = this.searchQuery.toLowerCase();
    return this.allRecords().filter(p => {
      const matchStatus = this.statusFilter === 'All' || p.status === this.statusFilter;
      const matchSearch = !q ||
        `PAY-${p.id}`.toLowerCase().includes(q) ||
        (p.policyNumber ?? '').toLowerCase().includes(q) ||
        p.customerName.toLowerCase().includes(q);
      return matchStatus && matchSearch;
    });
  });

  totalPages = computed(() => Math.max(1, Math.ceil(this.filteredRecords().length / this.pageSize)));

  pagedRecords = computed(() => {
    const start = (this.currentPage() - 1) * this.pageSize;
    return this.filteredRecords().slice(start, start + this.pageSize);
  });

  ngOnInit(): void {
    this.financeService.getAllPaymentRecords().subscribe({
      next: (records) => { this.allRecords.set(records); this.loading.set(false); },
      error: () => this.loading.set(false),
    });
  }

  onFilterChange(): void {
    this.currentPage.set(1);
    this.allRecords.update(records => [...records]);
  }
  onPageChange(page: number): void { this.currentPage.set(page); }

  openReconcile(p: FinancePaymentRecordDto): void {
    this.dialogAction.set('reconcile');
    this.dialogTitle.set('Reconcile payment');
    this.dialogMessage.set(`Manually reconcile PAY-${p.id} for ${this.moneyPipe.transform(p.amount)} (${p.customerName}). This marks the payment as reconciled in the system.`);
    this.dialogConfirmLabel.set('Reconcile');
    this.dialogVariant.set('default');
    this.dialogTarget.set(p);
  }

  openRefund(p: FinancePaymentRecordDto): void {
    this.dialogAction.set('refund');
    this.dialogTitle.set('Confirm refund');
    this.dialogMessage.set(`This will initiate a refund of ${this.moneyPipe.transform(p.amount)} to ${p.customerName} (${p.policyNumber}). This action cannot be undone.`);
    this.dialogConfirmLabel.set('Confirm refund');
    this.dialogVariant.set('danger');
    this.dialogTarget.set(p);
  }

  onConfirm(): void {
    const target = this.dialogTarget();
    if (!target || this.actionInFlight()) return;
    this.actionInFlight.set(true);

    if (this.dialogAction() === 'reconcile') {
      this.financeService.reconcilePayment(target.id).subscribe({
        next: () => {
          this.allRecords.update(list => list.map(p => p.id === target.id ? { ...p, status: 'Paid', paidAt: new Date().toISOString() } : p));
          this.toast.success(`Payment PAY-${target.id} reconciled successfully`);
          this.dialogTarget.set(null);
          this.actionInFlight.set(false);
        },
        error: () => {
          this.toast.error('Failed to reconcile payment');
          this.actionInFlight.set(false);
        },
      });
    } else {
      this.financeService.refundPayment(target.id).subscribe({
        next: () => {
          this.allRecords.update(list => list.map(p => p.id === target.id ? { ...p, status: 'Refunded' } : p));
          this.toast.success(`Refund of ${this.moneyPipe.transform(target.amount)} initiated for ${target.customerName}`);
          this.dialogTarget.set(null);
          this.actionInFlight.set(false);
        },
        error: () => {
          this.toast.error('Failed to process refund');
          this.actionInFlight.set(false);
        },
      });
    }
  }
}
