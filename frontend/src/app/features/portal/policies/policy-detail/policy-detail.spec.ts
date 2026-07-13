import { vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { Subject, of, throwError } from 'rxjs';
import { PolicyDetailComponent } from './policy-detail';
import { PolicyService } from '../services/policy.service';
import { PaymentService } from '../../payments/services/payment.service';
import { ProductService } from '../../products/services/product.service';
import { ToastService } from '../../../../shared/components/toast/toast.service';
import { ApiMessage, PolicyDto, PremiumScheduleDto, ProductDto } from '../../../../core/models/api.models';

describe('PolicyDetailComponent', () => {
  let policyService: {
    getById: ReturnType<typeof vi.fn>;
    getNominees: ReturnType<typeof vi.fn>;
    getEndorsements: ReturnType<typeof vi.fn>;
    getSchedule: ReturnType<typeof vi.fn>;
    getHistory: ReturnType<typeof vi.fn>;
    requestEndorsement: ReturnType<typeof vi.fn>;
    cancelPolicy: ReturnType<typeof vi.fn>;
    downloadCertificate: ReturnType<typeof vi.fn>;
  };
  let paymentService: { getHistory: ReturnType<typeof vi.fn> };
  let productService: { getById: ReturnType<typeof vi.fn> };
  let toast: { success: ReturnType<typeof vi.fn>; error: ReturnType<typeof vi.fn>; warning: ReturnType<typeof vi.fn> };

  const basePolicy: PolicyDto = {
    id: 'pol1', policyNumber: 'POL-1', customerId: 'cust1', productId: 'prod1', productName: 'Health Plus',
    status: 'Active', paymentFrequency: 'Monthly', premiumAmount: 500, coverageAmount: 100000,
    currency: 'INR', startDate: '2026-01-01', endDate: '2027-01-01', domain: 'Health', type: 'Individual',
  } as PolicyDto;

  const product: ProductDto = { id: 'prod1', productName: 'Health Plus', domain: 'Health' } as ProductDto;

  function create(policy: PolicyDto | null = basePolicy, schedules: PremiumScheduleDto[] = []) {
    policyService.getById.mockReturnValue(policy ? of(policy) : throwError(() => ({ status: 404 })));
    productService.getById.mockReturnValue(of(product));
    policyService.getNominees.mockReturnValue(of([]));
    policyService.getEndorsements.mockReturnValue(of([]));
    policyService.getSchedule.mockReturnValue(of(schedules));
    policyService.getHistory.mockReturnValue(of([]));
    paymentService.getHistory.mockReturnValue(of([]));

    TestBed.configureTestingModule({
      imports: [PolicyDetailComponent],
      providers: [
        { provide: PolicyService, useValue: policyService },
        { provide: PaymentService, useValue: paymentService },
        { provide: ProductService, useValue: productService },
        { provide: ToastService, useValue: toast },
        { provide: Router, useValue: { navigate: vi.fn() } },
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: new Map([['id', 'pol1']]) } } },
      ],
    });
    const fixture = TestBed.createComponent(PolicyDetailComponent);
    fixture.detectChanges();
    return fixture;
  }

  beforeEach(() => {
    policyService = {
      getById: vi.fn(), getNominees: vi.fn(), getEndorsements: vi.fn(), getSchedule: vi.fn(),
      getHistory: vi.fn(), requestEndorsement: vi.fn(), cancelPolicy: vi.fn(), downloadCertificate: vi.fn(),
    };
    paymentService = { getHistory: vi.fn() };
    productService = { getById: vi.fn() };
    toast = { success: vi.fn(), error: vi.fn(), warning: vi.fn() };
  });

  describe('ngOnInit', () => {
    it('loads the policy, its product, and related data', () => {
      const fixture = create();
      expect(fixture.componentInstance.policy()).toEqual(basePolicy);
      expect(fixture.componentInstance.product()).toEqual(product);
      expect(fixture.componentInstance.loading()).toBe(false);
    });
  });

  describe('submitEndorsement', () => {
    function fillForm(fixture: ReturnType<typeof create>) {
      fixture.componentInstance.endorsementForm.setValue({ endorsementType: 'AddressChange', description: 'Updated my address recently' });
    }

    it('does not submit an invalid endorsement form', () => {
      const fixture = create();
      fixture.componentInstance.endorsementForm.setValue({ endorsementType: 'AddressChange', description: '' });
      fixture.componentInstance.submitEndorsement();
      expect(policyService.requestEndorsement).not.toHaveBeenCalled();
    });

    it('sets submittingEndorsement true while in flight, blocks a duplicate call, and clears on success', () => {
      const fixture = create();
      fillForm(fixture);
      const subject = new Subject<ApiMessage>();
      policyService.requestEndorsement.mockReturnValue(subject.asObservable());
      policyService.getEndorsements.mockReturnValue(of([]));

      fixture.componentInstance.submitEndorsement();
      expect(fixture.componentInstance.submittingEndorsement()).toBe(true);

      fixture.componentInstance.submitEndorsement();
      expect(policyService.requestEndorsement).toHaveBeenCalledTimes(1);

      subject.next({ message: 'ok' });
      subject.complete();

      expect(fixture.componentInstance.submittingEndorsement()).toBe(false);
      expect(toast.success).toHaveBeenCalledWith('Endorsement request submitted');
      expect(fixture.componentInstance.showEndorsementForm()).toBe(false);
    });

    it('clears submittingEndorsement on error', () => {
      const fixture = create();
      fillForm(fixture);
      const subject = new Subject<ApiMessage>();
      policyService.requestEndorsement.mockReturnValue(subject.asObservable());

      fixture.componentInstance.submitEndorsement();
      subject.error({ status: 500 });

      expect(fixture.componentInstance.submittingEndorsement()).toBe(false);
      expect(toast.error).toHaveBeenCalledWith('Failed to submit endorsement');
    });
  });

  describe('cancelEndorsementForm', () => {
    it('does nothing while a submission is in flight', () => {
      const fixture = create();
      fixture.componentInstance.showEndorsementForm.set(true);
      fixture.componentInstance.submittingEndorsement.set(true);
      fixture.componentInstance.cancelEndorsementForm();
      expect(fixture.componentInstance.showEndorsementForm()).toBe(true);
    });

    it('closes the form when not submitting', () => {
      const fixture = create();
      fixture.componentInstance.showEndorsementForm.set(true);
      fixture.componentInstance.cancelEndorsementForm();
      expect(fixture.componentInstance.showEndorsementForm()).toBe(false);
    });
  });

  describe('confirmCancel', () => {
    it('sets cancelling true while in flight, blocks a duplicate call, and clears on success', () => {
      const fixture = create();
      const subject = new Subject<ApiMessage>();
      policyService.cancelPolicy.mockReturnValue(subject.asObservable());

      fixture.componentInstance.confirmCancel();
      expect(fixture.componentInstance.cancelling()).toBe(true);

      fixture.componentInstance.confirmCancel();
      expect(policyService.cancelPolicy).toHaveBeenCalledTimes(1);

      subject.next({ message: 'ok' });
      subject.complete();

      expect(fixture.componentInstance.cancelling()).toBe(false);
      expect(toast.success).toHaveBeenCalledWith('Policy cancelled');
      expect(fixture.componentInstance.showCancelDialog()).toBe(false);
      expect(fixture.componentInstance.policy()?.status).toBe('Cancelled');
    });

    it('clears cancelling on error and shows an error toast', () => {
      const fixture = create();
      const subject = new Subject<ApiMessage>();
      policyService.cancelPolicy.mockReturnValue(subject.asObservable());

      fixture.componentInstance.confirmCancel();
      subject.error({ status: 500 });

      expect(fixture.componentInstance.cancelling()).toBe(false);
      expect(toast.error).toHaveBeenCalledWith('Cancellation failed');
    });
  });

  describe('schedule payments', () => {
    it('shows Pay now for an upcoming installment and navigates to payment', () => {
      const fixture = create({ ...basePolicy, status: 'Pending' }, [
        { id: 'sch1', policyId: 'pol1', installmentNumber: 1, amountDue: 22000, dueDate: '2026-07-20', status: 'Upcoming' },
      ] as PremiumScheduleDto[]);
      fixture.componentInstance.activeTab.set(3);
      fixture.detectChanges();

      const buttons = Array.from(fixture.nativeElement.querySelectorAll('button')) as HTMLButtonElement[];
      const payButton = buttons.find(button => button.textContent?.trim() === 'Pay now');
      expect(payButton).toBeTruthy();

      payButton!.click();
      expect(TestBed.inject(Router).navigate).toHaveBeenCalledWith(['/pay', 'pol1']);
    });
  });
});
