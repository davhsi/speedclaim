import { Component, inject, signal, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MoneyPipe } from '../../../shared/pipes/money.pipe';
import { DateFormatPipe } from '../../../shared/pipes/date-format.pipe';
import { ToastService } from '../../../shared/components/toast/toast.service';
import { FinanceOfficerService } from '../services/finance-officer.service';
import { OverduePolicyDto, PaymentSummaryDto } from '../../../core/models/api.models';

@Component({
  selector: 'app-finance-officer-reports',
  standalone: true,
  imports: [FormsModule, MoneyPipe, DateFormatPipe],
  templateUrl: './finance-officer-reports.html',
})
export class FinanceOfficerReportsComponent implements OnInit {
  private financeService = inject(FinanceOfficerService);
  private toast = inject(ToastService);

  overdueList = signal<OverduePolicyDto[]>([]);
  summary = signal<PaymentSummaryDto | null>(null);
  exporting = signal(false);

  summaryPeriod = 'Jun 2026';
  exportFrom = '2026-01-01';
  exportTo = '2026-06-30';

  periods = ['Jun 2026', 'May 2026', 'Apr 2026', 'Mar 2026'];

  ngOnInit(): void {
    this.financeService.getOverduePolicies().subscribe({
      next: (list) => this.overdueList.set(list),
    });
    this.loadSummary();
  }

  loadSummary(): void {
    this.financeService.getCollectionSummary(this.summaryPeriod).subscribe({
      next: (data) => this.summary.set(data),
    });
  }

  typeBadge(domain: string): string {
    const map: Record<string, string> = {
      Health: 'bg-success-bg text-success',
      Motor: 'bg-info-bg text-info',
      Life: 'bg-surface-alt text-muted',
    };
    return map[domain] ?? 'bg-surface-alt text-muted';
  }

  daysColor(days: number): string {
    if (days > 30) return 'text-danger';
    if (days > 20) return 'text-warning';
    return 'text-muted';
  }

  onExport(): void {
    if (this.exporting()) return;
    if (!this.exportFrom || !this.exportTo) {
      this.toast.warning('Please select both a from and to date.');
      return;
    }
    if (this.exportFrom > this.exportTo) {
      this.toast.warning('The from date must be on or before the to date.');
      return;
    }
    this.exporting.set(true);

    this.financeService.exportPaymentReport(this.exportFrom, this.exportTo).subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `PaymentReport_${this.exportFrom}_to_${this.exportTo}.xlsx`;
        a.click();
        window.URL.revokeObjectURL(url);
        this.exporting.set(false);
        this.toast.success(`Excel report downloaded · ${this.exportFrom} to ${this.exportTo}`);
      },
      error: () => {
        this.exporting.set(false);
        this.toast.error('Failed to export report');
      },
    });
  }
}
