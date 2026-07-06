import { vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { Router, ActivatedRoute } from '@angular/router';
import { of, throwError } from 'rxjs';
import { AgentProposalSubmitComponent } from './proposal-submit';
import { AgentService, AgentCustomerDto } from '../services/agent.service';
import { ToastService } from '../../../shared/components/toast/toast.service';
import { ProductDto } from '../../../core/models/api.models';

describe('AgentProposalSubmitComponent', () => {
  let agentService: {
    getCustomers: ReturnType<typeof vi.fn>;
    getProducts: ReturnType<typeof vi.fn>;
    generateQuote: ReturnType<typeof vi.fn>;
    submitProposal: ReturnType<typeof vi.fn>;
  };
  let router: { navigate: ReturnType<typeof vi.fn> };
  let toast: { warning: ReturnType<typeof vi.fn>; error: ReturnType<typeof vi.fn> };

  const customers: AgentCustomerDto[] = [{ id: 'c1', customerId: 'cust1', fullName: 'Jane Doe' } as AgentCustomerDto];
  const healthProduct = {
    id: 'prod-health', domain: 'Health', minSumAssured: 100000, maxSumAssured: 2000000,
    minTenureYears: 1, maxTenureYears: 5,
  } as ProductDto;
  const products: ProductDto[] = [healthProduct];

  function create() {
    agentService = {
      getCustomers: vi.fn(() => of(customers)),
      getProducts: vi.fn(() => of(products)),
      generateQuote: vi.fn(),
      submitProposal: vi.fn(),
    };
    router = { navigate: vi.fn() };
    toast = { warning: vi.fn(), error: vi.fn() };

    TestBed.configureTestingModule({
      imports: [AgentProposalSubmitComponent],
      providers: [
        { provide: AgentService, useValue: agentService },
        { provide: Router, useValue: router },
        { provide: ToastService, useValue: toast },
        { provide: ActivatedRoute, useValue: {} },
      ],
    });
    const fixture = TestBed.createComponent(AgentProposalSubmitComponent);
    fixture.detectChanges();
    return fixture;
  }

  describe('ngOnInit', () => {
    it('loads customers and products, and preselects the first customer', () => {
      const fixture = create();
      expect(fixture.componentInstance.customers()).toEqual(customers);
      expect(fixture.componentInstance.products()).toEqual(products);
      expect(fixture.componentInstance.selectedCustomerId).toBe('cust1');
      expect(fixture.componentInstance.proposerForm.fullName).toBe('Jane Doe');
    });
  });

  describe('nextStep (step 0: customer + type)', () => {
    it('warns and blocks when no customer is selected', () => {
      const fixture = create();
      fixture.componentInstance.selectedCustomerId = null;
      fixture.componentInstance.selectedType = 'Health';
      fixture.componentInstance.nextStep();
      expect(toast.warning).toHaveBeenCalledWith('Please select a customer before continuing.');
      expect(fixture.componentInstance.currentStep).toBe(0);
    });

    it('warns and blocks when no product type is selected', () => {
      const fixture = create();
      fixture.componentInstance.selectedType = null;
      fixture.componentInstance.nextStep();
      expect(toast.warning).toHaveBeenCalledWith('Please select a product type before continuing.');
      expect(fixture.componentInstance.currentStep).toBe(0);
    });

    it('advances when both are selected', () => {
      const fixture = create();
      fixture.componentInstance.selectedType = 'Health';
      fixture.componentInstance.nextStep();
      expect(fixture.componentInstance.currentStep).toBe(1);
    });
  });

  describe('nextStep (step 1: quote)', () => {
    it('warns and blocks when no quote has been calculated', () => {
      const fixture = create();
      fixture.componentInstance.selectedType = 'Health';
      fixture.componentInstance.currentStep = 1;
      fixture.componentInstance.nextStep();
      expect(toast.warning).toHaveBeenCalledWith('Please calculate the premium before continuing.');
      expect(fixture.componentInstance.currentStep).toBe(1);
    });

    it('advances once a quote exists', () => {
      const fixture = create();
      fixture.componentInstance.currentStep = 1;
      fixture.componentInstance.quoteResult.set({ premiumAmount: 500 });
      fixture.componentInstance.nextStep();
      expect(fixture.componentInstance.currentStep).toBe(2);
    });
  });

  describe('prevStep', () => {
    it('decrements the step, but not below 0', () => {
      const fixture = create();
      fixture.componentInstance.currentStep = 1;
      fixture.componentInstance.prevStep();
      expect(fixture.componentInstance.currentStep).toBe(0);
      fixture.componentInstance.prevStep();
      expect(fixture.componentInstance.currentStep).toBe(0);
    });
  });

  describe('calculateQuote', () => {
    it('does nothing when no product matches the selected type', () => {
      const fixture = create();
      fixture.componentInstance.selectedType = 'Motor';
      fixture.componentInstance.calculateQuote();
      expect(agentService.generateQuote).not.toHaveBeenCalled();
    });

    it('computes age/sumAssured/tenure for Health and calls generateQuote', () => {
      const fixture = create();
      fixture.componentInstance.selectedType = 'Health';
      fixture.componentInstance.healthForm.eldestAge = 45;
      fixture.componentInstance.healthForm.sumAssured = '₹5,00,000';
      agentService.generateQuote.mockReturnValue(of({ premiumAmount: 4000 }));

      fixture.componentInstance.calculateQuote();

      expect(agentService.generateQuote).toHaveBeenCalledWith({
        productId: 'prod-health', age: 45, sumAssured: 500000, tenureYears: 1,
      });
      expect(fixture.componentInstance.quoteResult()).toEqual({ premiumAmount: 4000 });
    });

    it('clamps sumAssured to the product limits', () => {
      const fixture = create();
      fixture.componentInstance.selectedType = 'Health';
      fixture.componentInstance.healthForm.sumAssured = '₹50,00,00,000'; // way above max
      agentService.generateQuote.mockReturnValue(of({ premiumAmount: 1 }));

      fixture.componentInstance.calculateQuote();

      expect(agentService.generateQuote).toHaveBeenCalledWith(expect.objectContaining({ sumAssured: healthProduct.maxSumAssured }));
    });

    it('shows an error toast when the quote call fails', () => {
      const fixture = create();
      fixture.componentInstance.selectedType = 'Health';
      agentService.generateQuote.mockReturnValue(throwError(() => ({ status: 500 })));

      fixture.componentInstance.calculateQuote();

      expect(toast.error).toHaveBeenCalledWith('Failed to calculate quote. Please check the values and try again.');
    });
  });

  describe('nextStep (step 3: confirm + submit)', () => {
    function setupReadyToSubmit(fixture: ReturnType<typeof create>) {
      fixture.componentInstance.selectedType = 'Health';
      fixture.componentInstance.currentStep = 3;
      fixture.componentInstance.selectedCustomerId = 'cust1';
      fixture.componentInstance.quoteResult.set({ sumAssured: 500000, tenureYears: 1, premiumAmount: 4000 });
    }

    it('warns and blocks submission when the confirmation checkbox is unchecked', () => {
      const fixture = create();
      setupReadyToSubmit(fixture);
      fixture.componentInstance.confirmReady = false;

      fixture.componentInstance.nextStep();

      expect(toast.warning).toHaveBeenCalledWith('Please confirm that all documents are accurate before submitting.');
      expect(agentService.submitProposal).not.toHaveBeenCalled();
    });

    it('submits the mapped proposal payload and navigates on success', () => {
      const fixture = create();
      setupReadyToSubmit(fixture);
      fixture.componentInstance.confirmReady = true;
      agentService.submitProposal.mockReturnValue(of({ id: 'p-new' }));

      fixture.componentInstance.nextStep();

      expect(agentService.submitProposal).toHaveBeenCalledWith(expect.objectContaining({
        productId: 'prod-health', customerId: 'cust1', sumAssured: 500000, tenureYears: 1, premiumAmount: 4000,
        paymentFrequency: 'Annually', customerMemberIds: [], nominees: [],
      }));
      expect(router.navigate).toHaveBeenCalledWith(['/agent/proposals']);
    });

    it('shows an error toast when submission fails', () => {
      const fixture = create();
      setupReadyToSubmit(fixture);
      fixture.componentInstance.confirmReady = true;
      agentService.submitProposal.mockReturnValue(throwError(() => ({ status: 500 })));

      fixture.componentInstance.nextStep();

      expect(toast.error).toHaveBeenCalledWith('Failed to submit proposal. Please try again.');
    });
  });

  describe('typeIcon', () => {
    it('returns a distinct icon per product type', () => {
      const fixture = create();
      const c = fixture.componentInstance;
      const icons = new Set([c.typeIcon('Health'), c.typeIcon('Motor'), c.typeIcon('Life')]);
      expect(icons.size).toBe(3);
    });
  });
});
