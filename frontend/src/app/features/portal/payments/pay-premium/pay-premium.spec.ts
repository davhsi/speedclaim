import { vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { PayPremiumComponent } from './pay-premium';
import { PaymentService } from '../services/payment.service';
import { ToastService } from '../../../../shared/components/toast/toast.service';
import { PremiumScheduleDto } from '../../../../core/models/api.models';

// Stripe.js itself (Elements mounting, confirmPayment) is not unit-tested here — it's a
// third-party SDK loaded async in index.html and never available in this test environment.
// Covered: schedule loading, the isPayable gate, the two pre-Stripe error guards (missing
// publishableKey/clientSecret, Stripe not yet loaded), the 409-already-reconciled refetch,
// and the modal-reset methods (cancelPayment/closeSuccessModal/viewReceipt). Not covered:
// confirmPayment() and waitForReconciliation(), since both require a real stripeInstance/
// stripeElements that only get set once `Stripe(...)` succeeds — mocking the Stripe SDK
// deeply enough to reach them would just be testing mocks talking to mocks.
describe('PayPremiumComponent', () => {
  let paymentService: { getSchedule: ReturnType<typeof vi.fn>; createPaymentIntent: ReturnType<typeof vi.fn> };
  let toast: { success: ReturnType<typeof vi.fn>; error: ReturnType<typeof vi.fn> };
  let router: { navigate: ReturnType<typeof vi.fn> };

  const upcoming: PremiumScheduleDto = { id: 's1', policyId: 'p1', installmentNumber: 1, amountDue: 1000, dueDate: '2026-08-01', status: 'Upcoming' };
  const paid: PremiumScheduleDto = { id: 's0', policyId: 'p1', installmentNumber: 0, amountDue: 500, dueDate: '2026-01-01', status: 'Paid' };

  function create(policyId = 'p1') {
    TestBed.configureTestingModule({
      providers: [
        { provide: PaymentService, useValue: paymentService },
        { provide: ToastService, useValue: toast },
        { provide: Router, useValue: router },
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: { get: (key: string) => (key === 'policyId' ? policyId : null) } } } },
      ],
    });
    const fixture = TestBed.createComponent(PayPremiumComponent);
    fixture.detectChanges();
    return fixture;
  }

  beforeEach(() => {
    paymentService = { getSchedule: vi.fn(() => of([paid, upcoming])), createPaymentIntent: vi.fn() };
    toast = { success: vi.fn(), error: vi.fn() };
    router = { navigate: vi.fn() };
  });

  describe('ngOnInit', () => {
    it('loads the schedule and picks the first payable item as nextDue', () => {
      const fixture = create();
      expect(fixture.componentInstance.schedule()).toEqual([paid, upcoming]);
      expect(fixture.componentInstance.nextDue()).toEqual(upcoming);
      expect(fixture.componentInstance.loading()).toBe(false);
    });

    it('leaves nextDue null when nothing is payable', () => {
      paymentService.getSchedule.mockReturnValue(of([paid]));
      const fixture = create();
      expect(fixture.componentInstance.nextDue()).toBeNull();
    });

    it('stops loading when the schedule fetch fails', () => {
      paymentService.getSchedule.mockReturnValue(throwError(() => ({ status: 500 })));
      const fixture = create();
      expect(fixture.componentInstance.loading()).toBe(false);
    });
  });

  describe('isPayable', () => {
    it.each(['Upcoming', 'Due', 'Overdue'])('is true for status %s', (status) => {
      const fixture = create();
      expect(fixture.componentInstance.isPayable({ ...upcoming, status } as PremiumScheduleDto)).toBe(true);
    });

    it('is false for Paid', () => {
      const fixture = create();
      expect(fixture.componentInstance.isPayable(paid)).toBe(false);
    });
  });

  describe('pay', () => {
    it('does nothing for a non-payable item', () => {
      const fixture = create();
      fixture.componentInstance.pay(paid);
      expect(paymentService.createPaymentIntent).not.toHaveBeenCalled();
    });

    it('does nothing while already paying', () => {
      const fixture = create();
      fixture.componentInstance.paying.set(true);
      fixture.componentInstance.pay(upcoming);
      expect(paymentService.createPaymentIntent).not.toHaveBeenCalled();
    });

    it('shows a configuration error when the response is missing publishableKey/clientSecret', () => {
      const fixture = create();
      paymentService.createPaymentIntent.mockReturnValue(of({ clientSecret: '', publishableKey: '' }));

      fixture.componentInstance.pay(upcoming);

      expect(toast.error).toHaveBeenCalledWith('Payment configuration error. Please contact support.');
      expect(fixture.componentInstance.clientSecret()).toBeNull();
      expect(fixture.componentInstance.paying()).toBe(false);
    });

    it('shows a "still loading" error when Stripe.js has not loaded yet (global Stripe undefined in this test env)', () => {
      const fixture = create();
      paymentService.createPaymentIntent.mockReturnValue(of({ clientSecret: 'secret', publishableKey: 'pk_test' }));

      fixture.componentInstance.pay(upcoming);

      expect(toast.error).toHaveBeenCalledWith('Payment library is still loading. Please try again in a moment.');
      expect(fixture.componentInstance.clientSecret()).toBeNull();
    });

    it('refetches the schedule on a 409 (already reconciled server-side)', () => {
      const fixture = create();
      paymentService.createPaymentIntent.mockReturnValue(throwError(() => ({ status: 409 })));
      paymentService.getSchedule.mockReturnValue(of([paid, upcoming]));

      fixture.componentInstance.pay(upcoming);

      expect(paymentService.getSchedule).toHaveBeenCalledTimes(2); // once on init, once on 409 refetch
      expect(toast.error).not.toHaveBeenCalledWith('Could not initiate payment. Please try again.');
    });

    it('shows a generic error on any other failure', () => {
      const fixture = create();
      paymentService.createPaymentIntent.mockReturnValue(throwError(() => ({ status: 500 })));

      fixture.componentInstance.pay(upcoming);

      expect(toast.error).toHaveBeenCalledWith('Could not initiate payment. Please try again.');
      expect(fixture.componentInstance.paying()).toBe(false);
    });
  });

  describe('cancelPayment', () => {
    it('resets the checkout state', () => {
      const fixture = create();
      fixture.componentInstance.paying.set(true);
      fixture.componentInstance.payingId.set('s1');

      fixture.componentInstance.cancelPayment();

      expect(fixture.componentInstance.paying()).toBe(false);
      expect(fixture.componentInstance.payingId()).toBeNull();
      expect(fixture.componentInstance.clientSecret()).toBeNull();
    });

    it('refuses to close a charge that has already succeeded', () => {
      const fixture = create();
      fixture.componentInstance.paymentSuccess.set(true);
      fixture.componentInstance.payingId.set('s1');

      fixture.componentInstance.cancelPayment();

      expect(fixture.componentInstance.payingId()).toBe('s1');
    });
  });

  describe('closeSuccessModal / viewReceipt', () => {
    it('closeSuccessModal resets all post-charge state', () => {
      const fixture = create();
      fixture.componentInstance.paymentSuccess.set(true);
      fixture.componentInstance.paidAmount.set(1000);
      fixture.componentInstance.clientSecret.set('secret');

      fixture.componentInstance.closeSuccessModal();

      expect(fixture.componentInstance.paymentSuccess()).toBe(false);
      expect(fixture.componentInstance.paidAmount()).toBeNull();
      expect(fixture.componentInstance.clientSecret()).toBeNull();
    });

    it('viewReceipt closes the modal and navigates to /payments', () => {
      const fixture = create();
      fixture.componentInstance.paymentSuccess.set(true);

      fixture.componentInstance.viewReceipt();

      expect(fixture.componentInstance.paymentSuccess()).toBe(false);
      expect(router.navigate).toHaveBeenCalledWith(['/payments']);
    });
  });
});
