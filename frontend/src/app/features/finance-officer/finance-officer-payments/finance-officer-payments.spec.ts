import { vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { FinanceOfficerPaymentsComponent } from './finance-officer-payments';
import { FinanceOfficerService } from '../services/finance-officer.service';
import { ToastService } from '../../../shared/components/toast/toast.service';
import { FinancePaymentRecordDto } from '../../../core/models/api.models';

describe('FinanceOfficerPaymentsComponent', () => {
  let financeService: { getAllPaymentRecords: ReturnType<typeof vi.fn>; reconcilePayment: ReturnType<typeof vi.fn>; refundPayment: ReturnType<typeof vi.fn> };
  let toast: { success: ReturnType<typeof vi.fn>; error: ReturnType<typeof vi.fn> };

  const record = (overrides: Partial<FinancePaymentRecordDto> = {}): FinancePaymentRecordDto => ({
    id: 'p1', customerId: 'c1', customerName: 'Jane Doe', amount: 5000, currency: 'inr',
    paymentType: 'Premium', status: 'Pending', createdAt: '2026-01-01', policyNumber: 'POL-1', ...overrides,
  });

  function create(records: FinancePaymentRecordDto[] = [record()]) {
    financeService.getAllPaymentRecords.mockReturnValue(of(records));
    const fixture = TestBed.createComponent(FinanceOfficerPaymentsComponent);
    fixture.detectChanges();
    return fixture;
  }

  beforeEach(() => {
    financeService = { getAllPaymentRecords: vi.fn(), reconcilePayment: vi.fn(), refundPayment: vi.fn() };
    toast = { success: vi.fn(), error: vi.fn() };
    TestBed.configureTestingModule({
      imports: [FinanceOfficerPaymentsComponent],
      providers: [
        { provide: FinanceOfficerService, useValue: financeService },
        { provide: ToastService, useValue: toast },
      ],
    });
  });

  describe('ngOnInit', () => {
    it('loads records and stops loading on success', () => {
      const fixture = create([record()]);
      expect(fixture.componentInstance.allRecords()).toHaveLength(1);
      expect(fixture.componentInstance.loading()).toBe(false);
    });

    it('stops loading even when the fetch fails', () => {
      financeService.getAllPaymentRecords.mockReturnValue(throwError(() => ({ status: 500 })));
      const fixture = TestBed.createComponent(FinanceOfficerPaymentsComponent);
      fixture.detectChanges();
      expect(fixture.componentInstance.loading()).toBe(false);
    });
  });

  describe('filteredRecords / pagination', () => {
    it('filters by status', () => {
      const fixture = create([record({ id: 'p1', status: 'Pending' }), record({ id: 'p2', status: 'Paid' })]);
      fixture.componentInstance.statusFilter = 'Paid';
      fixture.componentInstance.onFilterChange();
      expect(fixture.componentInstance.filteredRecords().map(r => r.id)).toEqual(['p2']);
    });

    it('filters by search across id/policyNumber/customerName', () => {
      const fixture = create([record({ id: 'p1', customerName: 'Jane' }), record({ id: 'p2', customerName: 'Bob' })]);
      fixture.componentInstance.searchQuery = 'bob';
      fixture.componentInstance.onFilterChange();
      expect(fixture.componentInstance.filteredRecords().map(r => r.id)).toEqual(['p2']);
    });

    it('resets to page 1 on filter change', () => {
      const fixture = create();
      fixture.componentInstance.currentPage.set(3);
      fixture.componentInstance.onFilterChange();
      expect(fixture.componentInstance.currentPage()).toBe(1);
    });

    it('paginates records by pageSize (8)', () => {
      const many = Array.from({ length: 9 }, (_, i) => record({ id: `p${i}` }));
      const fixture = create(many);
      expect(fixture.componentInstance.totalPages()).toBe(2);
      expect(fixture.componentInstance.pagedRecords()).toHaveLength(8);
      fixture.componentInstance.onPageChange(2);
      expect(fixture.componentInstance.pagedRecords()).toHaveLength(1);
    });
  });

  describe('openReconcile / openRefund', () => {
    it('sets up the dialog for reconcile', () => {
      const fixture = create();
      fixture.componentInstance.openReconcile(record());
      expect(fixture.componentInstance.dialogAction()).toBe('reconcile');
      expect(fixture.componentInstance.dialogVariant()).toBe('default');
      expect(fixture.componentInstance.dialogTarget()).toEqual(record());
    });

    it('sets up the dialog for refund with danger variant', () => {
      const fixture = create();
      fixture.componentInstance.openRefund(record());
      expect(fixture.componentInstance.dialogAction()).toBe('refund');
      expect(fixture.componentInstance.dialogVariant()).toBe('danger');
    });
  });

  describe('onConfirm', () => {
    it('does nothing when there is no dialog target', () => {
      const fixture = create();
      fixture.componentInstance.onConfirm();
      expect(financeService.reconcilePayment).not.toHaveBeenCalled();
      expect(financeService.refundPayment).not.toHaveBeenCalled();
    });

    it('reconciles the payment, updates local state, and shows a success toast', () => {
      const fixture = create([record({ id: 'p1', status: 'Pending' })]);
      fixture.componentInstance.openReconcile(fixture.componentInstance.allRecords()[0]);
      financeService.reconcilePayment.mockReturnValue(of({ message: 'ok' }));

      fixture.componentInstance.onConfirm();

      expect(financeService.reconcilePayment).toHaveBeenCalledWith('p1');
      expect(fixture.componentInstance.allRecords()[0].status).toBe('Paid');
      expect(toast.success).toHaveBeenCalled();
      expect(fixture.componentInstance.dialogTarget()).toBeNull();
      expect(fixture.componentInstance.actionInFlight()).toBe(false);
    });

    it('shows an error toast and clears actionInFlight when reconcile fails', () => {
      const fixture = create([record({ id: 'p1' })]);
      fixture.componentInstance.openReconcile(fixture.componentInstance.allRecords()[0]);
      financeService.reconcilePayment.mockReturnValue(throwError(() => ({ status: 500 })));

      fixture.componentInstance.onConfirm();

      expect(toast.error).toHaveBeenCalledWith('Failed to reconcile payment');
      expect(fixture.componentInstance.actionInFlight()).toBe(false);
    });

    it('refunds the payment and updates local state on success', () => {
      const fixture = create([record({ id: 'p1', status: 'Paid' })]);
      fixture.componentInstance.openRefund(fixture.componentInstance.allRecords()[0]);
      financeService.refundPayment.mockReturnValue(of({ message: 'ok' }));

      fixture.componentInstance.onConfirm();

      expect(financeService.refundPayment).toHaveBeenCalledWith('p1');
      expect(fixture.componentInstance.allRecords()[0].status).toBe('Refunded');
      expect(toast.success).toHaveBeenCalled();
    });

    it('shows an error toast when refund fails', () => {
      const fixture = create([record({ id: 'p1' })]);
      fixture.componentInstance.openRefund(fixture.componentInstance.allRecords()[0]);
      financeService.refundPayment.mockReturnValue(throwError(() => ({ status: 500 })));

      fixture.componentInstance.onConfirm();

      expect(toast.error).toHaveBeenCalledWith('Failed to process refund');
    });

    it('does not start a second action while one is in flight', () => {
      const fixture = create([record({ id: 'p1' })]);
      fixture.componentInstance.openReconcile(fixture.componentInstance.allRecords()[0]);
      fixture.componentInstance.actionInFlight.set(true);

      fixture.componentInstance.onConfirm();

      expect(financeService.reconcilePayment).not.toHaveBeenCalled();
    });
  });
});
