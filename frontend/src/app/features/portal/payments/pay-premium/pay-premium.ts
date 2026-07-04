import { Component, inject, signal, OnInit, effect } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
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
  private router = inject(Router);
  private paymentService = inject(PaymentService);
  private toast = inject(ToastService);

  schedule = signal<PremiumScheduleDto[]>([]);
  loading = signal(true);
  paying = signal(false);
  payingId = signal<string | null>(null);
  nextDue = signal<PremiumScheduleDto | null>(null);

  // Checkout state
  clientSecret = signal<string | null>(null);
  checkoutReady = signal(false);
  checkoutError = signal<string | null>(null);
  confirming = signal(false);

  // Post-charge state — the modal stays open through all of this so the customer
  // always sees a definitive outcome instead of the dialog vanishing mid-flow.
  paymentSuccess = signal(false);
  reconciling = signal(false);
  reconciliationTimedOut = signal(false);
  paidAmount = signal<number | null>(null);
  paidInstallmentNumber = signal<number | null>(null);

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
    this.paidAmount.set(item.amountDue);
    this.paidInstallmentNumber.set(item.installmentNumber);
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
      error: err => {
        this.paying.set(false);
        if (err.status === 409) {
          // Server already reconciled this as paid (e.g. a prior confirmation succeeded on
          // Stripe's side but the frontend never saw it) — the error interceptor's toast
          // already explains why, so just refresh to reflect the new status.
          const policyId = this.route.snapshot.paramMap.get('policyId') ?? '';
          this.paymentService.getSchedule(policyId).subscribe({
            next: data => { this.schedule.set(data); this.nextDue.set(data.find(s => this.isPayable(s)) ?? null); },
          });
          return;
        }
        this.toast.error('Could not initiate payment. Please try again.');
      },
    });
  }

  private mountStripeElements(): void {
    if (!this.stripeElements) return;
    const paymentElement = this.stripeElements.create('payment');
    // Mounts into a dedicated, always-present div (see template) — never touch its
    // innerHTML here. Clearing it previously wiped out the Angular-rendered loading
    // skeleton via raw DOM manipulation, leaving a blank modal until Stripe painted.
    paymentElement.mount('#stripe-payment-element-mount');
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
      // Stripe has charged the card — the modal now shows a success view and stays
      // open through backend confirmation instead of closing immediately.
      const paidScheduleId = this.payingId();
      this.confirming.set(false);
      this.paymentSuccess.set(true);
      await this.waitForReconciliation(paidScheduleId);
    }
  }

  // The card charge succeeds on Stripe's side immediately, but our DB only flips the
  // installment to Paid once the async webhook lands — that can take several seconds
  // in production (a local dev webhook forwarder is near-instant, real delivery isn't).
  private async waitForReconciliation(scheduleId: string | null): Promise<void> {
    const policyId = this.route.snapshot.paramMap.get('policyId') ?? '';
    this.reconciling.set(true);
    this.reconciliationTimedOut.set(false);
    try {
      for (let attempt = 0; attempt < 10; attempt++) {
        const data = await firstValueFrom(this.paymentService.getSchedule(policyId));
        this.schedule.set(data);
        this.nextDue.set(data.find(s => this.isPayable(s)) ?? null);
        if (data.find(s => s.id === scheduleId)?.status === 'Paid') {
          return;
        }
        await new Promise(resolve => setTimeout(resolve, 1500));
      }
      this.reconciliationTimedOut.set(true);
    } finally {
      this.reconciling.set(false);
    }
  }

  // Called from the success view once the customer is done — resets all checkout state
  // and closes the modal. Reconciliation has either already finished or is still best-effort
  // syncing in the background (the email + eventual status update will land regardless).
  closeSuccessModal(): void {
    this.clientSecret.set(null);
    this.stripeInstance = null;
    this.stripeElements = null;
    this.checkoutReady.set(false);
    this.checkoutError.set(null);
    this.paymentSuccess.set(false);
    this.reconciling.set(false);
    this.reconciliationTimedOut.set(false);
    this.paidAmount.set(null);
    this.paidInstallmentNumber.set(null);
    this.payingId.set(null);
  }

  viewReceipt(): void {
    this.closeSuccessModal();
    this.router.navigate(['/payments']);
  }

  cancelPayment(): void {
    if (this.paymentSuccess()) return; // never let the backdrop/X close a charged payment silently
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
