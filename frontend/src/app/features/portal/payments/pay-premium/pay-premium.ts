import { Component, inject, signal, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { PaymentService } from '../services/payment.service';
import { PremiumScheduleDto } from '../../../../core/models/api.models';
import { StatusBadgeComponent } from '../../../../shared/components/status-badge/status-badge';
import { MoneyPipe } from '../../../../shared/pipes/money.pipe';
import { DateFormatPipe } from '../../../../shared/pipes/date-format.pipe';
import { ToastService } from '../../../../shared/components/toast/toast.service';

@Component({
  selector: 'app-pay-premium',
  standalone: true,
  imports: [StatusBadgeComponent, MoneyPipe, DateFormatPipe],
  templateUrl: './pay-premium.html',
})
export class PayPremiumComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private paymentService = inject(PaymentService);
  private toast = inject(ToastService);

  schedule = signal<PremiumScheduleDto[]>([]);
  loading = signal(true);
  paying = signal(false);
  payingId = signal<number | null>(null);
  paymentSuccess = signal(false);

  nextDue = signal<PremiumScheduleDto | null>(null);

  ngOnInit(): void {
    const policyId = Number(this.route.snapshot.paramMap.get('policyId'));
    this.paymentService.getSchedule(policyId).subscribe({
      next: data => {
        this.schedule.set(data);
        this.nextDue.set(data.find(s => s.status === 'Due' || s.status === 'Overdue') ?? null);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  pay(item: PremiumScheduleDto): void {
    this.paying.set(true);
    this.payingId.set(item.id);
    this.paymentService.createPaymentIntent(item.id, { policyId: item.policyId }).subscribe({
      next: res => {
        this.paying.set(false);
        this.paymentSuccess.set(true);
        this.toast.success('Payment intent created. Stripe checkout would open here.');
      },
      error: () => {
        this.paying.set(false);
        this.toast.error('Payment failed. Please try again.');
      },
    });
  }
}
