import { Component, inject, signal, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge';
import { MoneyPipe } from '../../../shared/pipes/money.pipe';
import { DateFormatPipe } from '../../../shared/pipes/date-format.pipe';
import { FinanceOfficerService } from '../services/finance-officer.service';
import { FinancePaymentRecordDto } from '../../../core/models/api.models';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-finance-officer-dashboard',
  standalone: true,
  imports: [StatusBadgeComponent, MoneyPipe, DateFormatPipe],
  templateUrl: './finance-officer-dashboard.html',
})
export class FinanceOfficerDashboardComponent implements OnInit {
  private financeService = inject(FinanceOfficerService);
  private authService = inject(AuthService);
  private router = inject(Router);

  recentPayments = signal<FinancePaymentRecordDto[]>([]);
  totalPaid = signal('₹ 0.00');
  payoutCount = signal(0);
  commissionCount = signal(0);
  pendingCommTotal = signal('₹ 0.00');
  overdueCount = signal(0);

  private moneyPipe = new MoneyPipe();

  ngOnInit(): void {
    this.loadData();
  }

  private loadData(): void {
    this.financeService.getAllPaymentRecords().subscribe({
      next: (records) => {
        this.recentPayments.set(records.slice(0, 5));
        const paid = records
          .filter(r => r.status === 'Paid' || r.status === 'Reconciled')
          .reduce((sum, r) => sum + r.amount, 0);
        this.totalPaid.set(this.moneyPipe.transform(paid));
      },
    });

    this.financeService.getClaimsForPayout('Approved').subscribe({
      next: (res) => this.payoutCount.set(res.totalRecords),
      error: () => {},
    });

    this.financeService.getPendingCommissions().subscribe({
      next: (comms) => {
        const pending = comms.filter(c => c.status === 'Pending');
        this.commissionCount.set(pending.length);
        const total = pending.reduce((sum, c) => sum + c.commissionAmount, 0);
        this.pendingCommTotal.set(this.moneyPipe.transform(total));
      },
    });

    this.financeService.getOverduePolicies().subscribe({
      next: (policies) => this.overdueCount.set(policies.length),
      error: () => {},
    });
  }

  firstName(): string {
    return this.authService.currentUser()?.firstName ?? '';
  }

  navigateTo(path: string): void {
    this.router.navigate([path]);
  }
}
