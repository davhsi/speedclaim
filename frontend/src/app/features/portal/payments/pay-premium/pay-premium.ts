import { Component, inject, signal, OnInit, effect } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { PaymentService } from '../services/payment.service';
import { PremiumScheduleDto } from '../../../../core/models/api.models';
import { StatusBadgeComponent } from '../../../../shared/components/status-badge/status-badge';
import { MoneyPipe } from '../../../../shared/pipes/money.pipe';
import { DateFormatPipe } from '../../../../shared/pipes/date-format.pipe';
import { ToastService } from '../../../../shared/components/toast/toast.service';

declare const Stripe: any;

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
  payingId = signal<string | null>(null);
  paymentSuccess = signal(false);
  nextDue = signal<PremiumScheduleDto | null>(null);

  // Checkout state
  clientSecret = signal<string | null>(null);
  checkoutReady = signal(false);
  checkoutError = signal<string | null>(null);
  confirming = signal(false);

  private stripeInstance: any = null;
  private stripeElements: any = null;

  constructor() {
    effect(() => {
      const secret = this.clientSecret();
      if (secret) {
        // DOM update happens after signal change; defer mount to next tick
        setTimeout(() => this.mountStripeElements(), 0);
      }
    });
  }

  ngOnInit(): void {
    const policyId = this.route.snapshot.paramMap.get('policyId') ?? '';
    this.paymentService.getSchedule(policyId).subscribe({
      next: data => {
        this.schedule.set(data);
        this.nextDue.set(data.find(s => this.isPayable(s)) ?? null);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  pay(item: PremiumScheduleDto): void {
    if (this.paying() || !this.isPayable(item)) return;
    this.paying.set(true);
    this.payingId.set(item.id);
    this.checkoutError.set(null);
    this.paymentService.createPaymentIntent(item.id, { policyId: item.policyId }).subscribe({
      next: res => {
        this.paying.set(false);
        if (!res.publishableKey || !res.clientSecret) {
          this.toast.error('Payment configuration error. Please contact support.');
          return;
        }
        if (typeof Stripe === 'undefined') {
          // Stripe.js is loaded async in index.html and may not be ready yet
          this.toast.error('Payment library is still loading. Please try again in a moment.');
          return;
        }
        this.stripeInstance = Stripe(res.publishableKey);
        this.stripeElements = this.stripeInstance.elements({
          clientSecret: res.clientSecret,
          appearance: { theme: 'stripe', variables: { colorPrimary: '#0F6E8C', borderRadius: '8px' } },
        });
        this.clientSecret.set(res.clientSecret);
      },
      error: () => {
        this.paying.set(false);
        this.toast.error('Could not initiate payment. Please try again.');
      },
    });
  }

  private mountStripeElements(): void {
    if (!this.stripeElements) return;
    const paymentElement = this.stripeElements.create('payment');
    const container = document.getElementById('stripe-payment-element');
    if (!container) return;
    container.innerHTML = '';
    paymentElement.mount('#stripe-payment-element');
    paymentElement.on('ready', () => this.checkoutReady.set(true));
  }

  async confirmPayment(): Promise<void> {
    if (!this.stripeInstance || !this.stripeElements || this.confirming()) return;
    this.confirming.set(true);
    this.checkoutError.set(null);
    const { error } = await this.stripeInstance.confirmPayment({
      elements: this.stripeElements,
      confirmParams: { return_url: window.location.origin + '/payments' },
      redirect: 'if_required',
    });
    if (error) {
      this.checkoutError.set(error.message ?? 'Payment failed. Please try again.');
      this.confirming.set(false);
    } else {
      this.clientSecret.set(null);
      this.paymentSuccess.set(true);
      // Refresh schedule so paid installment shows updated status
      const policyId = this.route.snapshot.paramMap.get('policyId') ?? '';
      this.paymentService.getSchedule(policyId).subscribe({
        next: data => { this.schedule.set(data); this.nextDue.set(data.find(s => this.isPayable(s)) ?? null); },
      });
    }
  }

  cancelPayment(): void {
    this.clientSecret.set(null);
    this.stripeInstance = null;
    this.stripeElements = null;
    this.checkoutReady.set(false);
    this.checkoutError.set(null);
    this.paying.set(false);
    this.payingId.set(null);
  }

  isPayable(item: PremiumScheduleDto): boolean {
    return item.status === 'Upcoming' || item.status === 'Due' || item.status === 'Overdue';
  }
}
