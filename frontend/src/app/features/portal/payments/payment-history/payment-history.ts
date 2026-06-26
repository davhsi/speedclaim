import { Component, inject, signal, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { PaymentService } from '../services/payment.service';
import { PaymentRecordDto } from '../../../../core/models/api.models';
import { StatusBadgeComponent } from '../../../../shared/components/status-badge/status-badge';
import { EmptyStateComponent } from '../../../../shared/components/empty-state/empty-state';
import { MoneyPipe } from '../../../../shared/pipes/money.pipe';
import { DateFormatPipe } from '../../../../shared/pipes/date-format.pipe';

@Component({
  selector: 'app-payment-history',
  standalone: true,
  imports: [StatusBadgeComponent, EmptyStateComponent, MoneyPipe, DateFormatPipe],
  templateUrl: './payment-history.html',
})
export class PaymentHistoryComponent implements OnInit {
  private paymentService = inject(PaymentService);
  payments = signal<PaymentRecordDto[]>([]);
  loading = signal(true);

  ngOnInit(): void {
    this.paymentService.getHistory().subscribe({
      next: data => { this.payments.set(data); this.loading.set(false); },
      error: () => this.loading.set(false),
    });
  }

  formatPaymentType(type: string): string {
    const map: Record<string, string> = {
      FirstPremium: 'First Premium',
      Renewal: 'Renewal Premium',
      Reinstatement: 'Reinstatement',
      ClaimPayout: 'Claim Payout',
    };
    return map[type] ?? type;
  }
}
