import { vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { FinanceOfficerPayoutsComponent } from './finance-officer-payouts';
import { FinanceOfficerService } from '../services/finance-officer.service';
import { ToastService } from '../../../shared/components/toast/toast.service';
import { ClaimDto, PagedResponse } from '../../../core/models/api.models';

describe('FinanceOfficerPayoutsComponent', () => {
  let financeService: {
    getClaimsForPayout: ReturnType<typeof vi.fn>;
    processClaimPayout: ReturnType<typeof vi.fn>;
    markClaimSettled: ReturnType<typeof vi.fn>;
  };
  let toast: { success: ReturnType<typeof vi.fn>; error: ReturnType<typeof vi.fn> };

  const claim = (overrides: Partial<ClaimDto> = {}): ClaimDto => ({
    id: 'c1', claimNumber: 'CLM-1', policyId: 'pol1', customerId: 'cust1', claimType: 'Health',
    claimAmountRequested: 10000, isCashless: false, status: 'Approved', intimationDate: '2026-01-01',
    incidentDate: '2026-01-01', incidentDescription: 'x', createdAt: '2026-01-01', ...overrides,
  });

  function paged(data: ClaimDto[]): PagedResponse<ClaimDto> {
    return { data, pageNumber: 1, pageSize: 100, totalRecords: data.length, totalPages: 1 };
  }

  function create(approved: ClaimDto[] = [], settled: ClaimDto[] = []) {
    financeService.getClaimsForPayout.mockImplementation((status: string) =>
      of(paged(status === 'Approved' ? approved : settled)));
    const fixture = TestBed.createComponent(FinanceOfficerPayoutsComponent);
    fixture.detectChanges();
    return fixture;
  }

  beforeEach(() => {
    financeService = { getClaimsForPayout: vi.fn(), processClaimPayout: vi.fn(), markClaimSettled: vi.fn() };
    toast = { success: vi.fn(), error: vi.fn() };
    TestBed.configureTestingModule({
      imports: [FinanceOfficerPayoutsComponent],
      providers: [
        { provide: FinanceOfficerService, useValue: financeService },
        { provide: ToastService, useValue: toast },
      ],
    });
  });

  describe('ngOnInit', () => {
    it('fetches both Approved and Settled claims and merges them without duplicates', () => {
      const fixture = create([claim({ id: 'c1' })], [claim({ id: 'c1' }), claim({ id: 'c2' })]);
      expect(financeService.getClaimsForPayout).toHaveBeenCalledWith('Approved');
      expect(financeService.getClaimsForPayout).toHaveBeenCalledWith('Settled');
      expect(fixture.componentInstance.allClaims().map(c => c.id).sort()).toEqual(['c1', 'c2']);
    });

    it('stops loading once both requests complete, even if one fails', () => {
      financeService.getClaimsForPayout.mockImplementation((status: string) =>
        status === 'Approved' ? of(paged([claim()])) : throwError(() => ({ status: 500 })));
      const fixture = TestBed.createComponent(FinanceOfficerPayoutsComponent);
      fixture.detectChanges();
      expect(fixture.componentInstance.loading()).toBe(false);
    });
  });

  describe('filteredClaims / pagination', () => {
    it('filters by status', () => {
      const fixture = create([claim({ id: 'c1', status: 'Approved' })], [claim({ id: 'c2', status: 'Settled' })]);
      fixture.componentInstance.statusFilter.set('Settled');
      expect(fixture.componentInstance.filteredClaims().map(c => c.id)).toEqual(['c2']);
    });

    it('shows all claims when filter is "All"', () => {
      const fixture = create([claim({ id: 'c1' })], [claim({ id: 'c2' })]);
      fixture.componentInstance.statusFilter.set('All');
      expect(fixture.componentInstance.filteredClaims()).toHaveLength(2);
    });

    it('paginates by pageSize (10)', () => {
      const many = Array.from({ length: 11 }, (_, i) => claim({ id: `c${i}` }));
      const fixture = create(many, []);
      expect(fixture.componentInstance.totalPages()).toBe(2);
      expect(fixture.componentInstance.pagedClaims()).toHaveLength(10);
    });

    it('resets to page 1 on filter change', () => {
      const fixture = create();
      fixture.componentInstance.currentPage.set(3);
      fixture.componentInstance.onFilterChange();
      expect(fixture.componentInstance.currentPage()).toBe(1);
    });
  });

  describe('typeBadge', () => {
    it('maps known claim types to a badge class', () => {
      const fixture = create();
      expect(fixture.componentInstance.typeBadge('Health')).toContain('success');
      expect(fixture.componentInstance.typeBadge('Theft')).toContain('danger');
    });

    it('falls back to a neutral class for an unknown type', () => {
      const fixture = create();
      expect(fixture.componentInstance.typeBadge('Unknown')).toBe('bg-surface-alt text-muted');
    });
  });

  describe('claimsSummary', () => {
    it('pluralizes correctly for 0/1/many claims', () => {
      const fixture = create([claim({ id: 'c1' })]);
      expect(fixture.componentInstance.claimsSummary()).toContain('1 claim —');
    });
  });

  describe('processPayout', () => {
    it('processes the payout, marks the claim Settled, and shows a success toast', () => {
      const fixture = create([claim({ id: 'c1', claimAmountApproved: 8000 })]);
      financeService.processClaimPayout.mockReturnValue(of({ message: 'ok' }));

      fixture.componentInstance.processPayout(fixture.componentInstance.allClaims()[0]);

      expect(financeService.processClaimPayout).toHaveBeenCalledWith('c1');
      expect(fixture.componentInstance.allClaims()[0].status).toBe('Settled');
      expect(toast.success).toHaveBeenCalled();
      expect(fixture.componentInstance.processing().has('c1')).toBe(false);
    });

    it('does not process the same claim twice concurrently', () => {
      const fixture = create([claim({ id: 'c1' })]);
      fixture.componentInstance.processing.set(new Set(['c1']));

      fixture.componentInstance.processPayout(fixture.componentInstance.allClaims()[0]);

      expect(financeService.processClaimPayout).not.toHaveBeenCalled();
    });

    it('shows an error toast and clears processing state on failure', () => {
      const fixture = create([claim({ id: 'c1' })]);
      financeService.processClaimPayout.mockReturnValue(throwError(() => ({ status: 500 })));

      fixture.componentInstance.processPayout(fixture.componentInstance.allClaims()[0]);

      expect(toast.error).toHaveBeenCalledWith('Failed to process payout');
      expect(fixture.componentInstance.processing().has('c1')).toBe(false);
    });
  });

  describe('markSettled', () => {
    it('marks the claim settled and shows a success toast', () => {
      const fixture = create([claim({ id: 'c1', claimNumber: 'CLM-1' })]);
      financeService.markClaimSettled.mockReturnValue(of({ message: 'ok' }));

      fixture.componentInstance.markSettled(fixture.componentInstance.allClaims()[0]);

      expect(financeService.markClaimSettled).toHaveBeenCalledWith('c1');
      expect(fixture.componentInstance.allClaims()[0].status).toBe('Settled');
      expect(toast.success).toHaveBeenCalledWith('Claim CLM-1 marked as financially settled');
    });

    it('shows an error toast on failure', () => {
      const fixture = create([claim({ id: 'c1' })]);
      financeService.markClaimSettled.mockReturnValue(throwError(() => ({ status: 500 })));

      fixture.componentInstance.markSettled(fixture.componentInstance.allClaims()[0]);

      expect(toast.error).toHaveBeenCalledWith('Failed to mark claim as settled');
    });
  });
});
