import { vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router, convertToParamMap } from '@angular/router';
import { of, throwError } from 'rxjs';
import { AgentProposalDetailComponent } from './proposal-detail';
import { AgentService } from '../services/agent.service';
import { ProductService } from '../../portal/products/services/product.service';
import { ToastService } from '../../../shared/components/toast/toast.service';
import { ProductDto, ProposalDto } from '../../../core/models/api.models';

describe('AgentProposalDetailComponent', () => {
  let agentService: { getProposalById: ReturnType<typeof vi.fn>; withdrawProposal: ReturnType<typeof vi.fn> };
  let productService: { getById: ReturnType<typeof vi.fn> };
  let router: { navigate: ReturnType<typeof vi.fn> };
  let toast: { success: ReturnType<typeof vi.fn>; error: ReturnType<typeof vi.fn> };

  const proposal: ProposalDto = {
    id: 'p1', proposalNumber: 'PN-1', customerId: 'c1', productId: 'prod1',
    status: 'Submitted', sumAssured: 100000, tenureYears: 10, premiumAmount: 5000,
    paymentFrequency: 'Annually', createdAt: '2026-01-01',
  } as ProposalDto;
  const product = { id: 'prod1', productName: 'Health Shield', domain: 'Health' } as ProductDto;

  function create(id: string | null = 'p1') {
    agentService = { getProposalById: vi.fn(() => of(proposal)), withdrawProposal: vi.fn() };
    productService = { getById: vi.fn(() => of(product)) };
    router = { navigate: vi.fn() };
    toast = { success: vi.fn(), error: vi.fn() };

    TestBed.configureTestingModule({
      imports: [AgentProposalDetailComponent],
      providers: [
        { provide: AgentService, useValue: agentService },
        { provide: ProductService, useValue: productService },
        { provide: Router, useValue: router },
        { provide: ToastService, useValue: toast },
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: convertToParamMap(id ? { id } : {}) } } },
      ],
    });
    const fixture = TestBed.createComponent(AgentProposalDetailComponent);
    fixture.detectChanges();
    return fixture;
  }

  describe('ngOnInit', () => {
    it('goes back immediately when there is no id in the route', () => {
      const fixture = create(null);
      expect(router.navigate).toHaveBeenCalledWith(['/agent/proposals']);
      expect(agentService.getProposalById).not.toHaveBeenCalled();
    });

    it('loads the proposal and its product', () => {
      const fixture = create('p1');
      expect(agentService.getProposalById).toHaveBeenCalledWith('p1');
      expect(fixture.componentInstance.proposal()).toEqual(proposal);
      expect(fixture.componentInstance.product()).toEqual(product);
      expect(fixture.componentInstance.loading()).toBe(false);
    });

    it('sets product to null when the product fetch fails', () => {
      productService = { getById: vi.fn(() => throwError(() => ({ status: 404 }))) };
      agentService = { getProposalById: vi.fn(() => of(proposal)), withdrawProposal: vi.fn() };
      TestBed.configureTestingModule({
        imports: [AgentProposalDetailComponent],
        providers: [
          { provide: AgentService, useValue: agentService },
          { provide: ProductService, useValue: productService },
          { provide: Router, useValue: { navigate: vi.fn() } },
          { provide: ToastService, useValue: { success: vi.fn(), error: vi.fn() } },
          { provide: ActivatedRoute, useValue: { snapshot: { paramMap: convertToParamMap({ id: 'p1' }) } } },
        ],
      });
      const fixture = TestBed.createComponent(AgentProposalDetailComponent);
      fixture.detectChanges();
      expect(fixture.componentInstance.product()).toBeNull();
    });

    it('shows an error toast, stops loading, and goes back when the proposal fetch fails', () => {
      agentService = { getProposalById: vi.fn(() => throwError(() => ({ status: 404 }))), withdrawProposal: vi.fn() };
      router = { navigate: vi.fn() };
      toast = { success: vi.fn(), error: vi.fn() };
      TestBed.configureTestingModule({
        imports: [AgentProposalDetailComponent],
        providers: [
          { provide: AgentService, useValue: agentService },
          { provide: ProductService, useValue: { getById: vi.fn() } },
          { provide: Router, useValue: router },
          { provide: ToastService, useValue: toast },
          { provide: ActivatedRoute, useValue: { snapshot: { paramMap: convertToParamMap({ id: 'p1' }) } } },
        ],
      });
      const fixture = TestBed.createComponent(AgentProposalDetailComponent);
      fixture.detectChanges();

      expect(toast.error).toHaveBeenCalledWith('Could not load proposal details.');
      expect(fixture.componentInstance.loading()).toBe(false);
      expect(router.navigate).toHaveBeenCalledWith(['/agent/proposals']);
    });
  });

  describe('canWithdraw', () => {
    it.each(['Submitted', 'UnderReview', 'DocumentsPending'])('is true when status is %s', (status) => {
      const fixture = create();
      fixture.componentInstance.proposal.set({ ...proposal, status } as ProposalDto);
      expect(fixture.componentInstance.canWithdraw()).toBe(true);
    });

    it.each(['Approved', 'Rejected', 'Withdrawn'])('is false when status is %s', (status) => {
      const fixture = create();
      fixture.componentInstance.proposal.set({ ...proposal, status } as ProposalDto);
      expect(fixture.componentInstance.canWithdraw()).toBe(false);
    });
  });

  describe('confirmWithdraw', () => {
    it('withdraws, shows a success toast, closes the dialog, and navigates away', () => {
      const fixture = create();
      agentService.withdrawProposal.mockReturnValue(of({ message: 'ok' }));
      fixture.componentInstance.showWithdrawDialog.set(true);

      fixture.componentInstance.confirmWithdraw();

      expect(agentService.withdrawProposal).toHaveBeenCalledWith('p1');
      expect(toast.success).toHaveBeenCalledWith('Proposal withdrawn successfully.');
      expect(fixture.componentInstance.showWithdrawDialog()).toBe(false);
      expect(router.navigate).toHaveBeenCalledWith(['/agent/proposals']);
    });

    it('shows an error toast and resets state on failure', () => {
      const fixture = create();
      agentService.withdrawProposal.mockReturnValue(throwError(() => ({ status: 500 })));
      fixture.componentInstance.showWithdrawDialog.set(true);

      fixture.componentInstance.confirmWithdraw();

      expect(toast.error).toHaveBeenCalledWith('Failed to withdraw proposal.');
      expect(fixture.componentInstance.actionInFlight()).toBe(false);
      expect(fixture.componentInstance.showWithdrawDialog()).toBe(false);
    });

    it('does nothing when an action is already in flight', () => {
      const fixture = create();
      fixture.componentInstance.actionInFlight.set(true);
      fixture.componentInstance.confirmWithdraw();
      expect(agentService.withdrawProposal).not.toHaveBeenCalled();
    });
  });

  describe('detailEntries', () => {
    it('formats the proposal fields for display', () => {
      const fixture = create();
      const entries = fixture.componentInstance.detailEntries(proposal);
      expect(entries).toContainEqual({ label: 'Proposal number', value: 'PN-1' });
      expect(entries).toContainEqual({ label: 'Tenure', value: '10 years' });
      expect(entries.find(e => e.label === 'Product')?.value).toBe('Health Shield');
    });
  });
});
