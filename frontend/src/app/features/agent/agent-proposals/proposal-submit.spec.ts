import { vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { Router, ActivatedRoute } from '@angular/router';
import { of, throwError, Subject } from 'rxjs';
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
    searchCustomers: ReturnType<typeof vi.fn>;
    getCustomerKyc: ReturnType<typeof vi.fn>;
    getProductDocuments: ReturnType<typeof vi.fn>;
    uploadProposalDocument: ReturnType<typeof vi.fn>;
  };
  let router: { navigate: ReturnType<typeof vi.fn> };
  let toast: { warning: ReturnType<typeof vi.fn>; error: ReturnType<typeof vi.fn> };

  const customers: AgentCustomerDto[] = [{ id: 'c1', customerId: 'cust1', fullName: 'Jane Doe', kycApproved: true } as AgentCustomerDto];
  const healthProduct = {
    id: 'prod-health', domain: 'Health', minSumAssured: 100000, maxSumAssured: 2000000,
    minTenureYears: 1, maxTenureYears: 5,
  } as ProductDto;
  const motorProduct = {
    id: 'prod-motor', domain: 'Motor', minSumAssured: 100000, maxSumAssured: 2000000,
    minTenureYears: 1, maxTenureYears: 3,
  } as ProductDto;
  const products: ProductDto[] = [healthProduct, motorProduct];

  function create() {
    agentService = {
      getCustomers: vi.fn(() => of(customers)),
      getProducts: vi.fn(() => of(products)),
      generateQuote: vi.fn(),
      submitProposal: vi.fn(),
      searchCustomers: vi.fn(() => of([])),
      getCustomerKyc: vi.fn(() => of(null)),
      getProductDocuments: vi.fn(() => of([])),
      uploadProposalDocument: vi.fn(() => of({ message: 'ok' })),
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

  describe('hasActiveProduct', () => {
    it('is true for a domain with an active product', () => {
      const fixture = create();
      expect(fixture.componentInstance.hasActiveProduct('Health')).toBe(true);
      expect(fixture.componentInstance.hasActiveProduct('Motor')).toBe(true);
    });

    it('is false for a domain with no active product', () => {
      const fixture = create();
      expect(fixture.componentInstance.hasActiveProduct('Life')).toBe(false);
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

    it('warns and blocks up front when the selected type has no active product, instead of failing later at calculateQuote', () => {
      // Regression test: the type-selector buttons used to render Health/Motor/Life
      // unconditionally regardless of whether an active product existed for that domain —
      // the agent could fill in the whole quote step before ever finding out. Life has no
      // product in this test's fixture (only Health and Motor do).
      const fixture = create();
      fixture.componentInstance.selectedType = 'Life';

      fixture.componentInstance.nextStep();

      expect(toast.warning).toHaveBeenCalledWith(expect.stringContaining('No active Life product'));
      expect(fixture.componentInstance.currentStep).toBe(0);
    });

    it('warns and blocks when the selected customer\'s KYC is not approved', () => {
      const fixture = create();
      const c = fixture.componentInstance;
      c.selectedCustomerKycApproved = false;
      c.selectedType = 'Health';

      c.nextStep();

      expect(toast.warning).toHaveBeenCalledWith('This customer\'s KYC must be approved before you can submit a proposal for them.');
      expect(c.currentStep).toBe(0);
    });

    it('applies the selected customer\'s KYC status/rejection reason when picked from "My customers"', () => {
      const fixture = create();
      const c = fixture.componentInstance;
      const rejected = { id: 'c2', customerId: 'cust2', fullName: 'Arjun Nair', kycApproved: false, kycStatus: 'Rejected', kycRejectionReason: 'Blurry photo' } as AgentCustomerDto;
      c.customers.set([...customers, rejected]);

      c.onMyCustomerSelected('cust2');

      expect(c.selectedCustomerKycApproved).toBe(false);
      expect(c.selectedCustomerKycStatus).toBe('Rejected');
      expect(c.selectedCustomerKycRejectionReason).toBe('Blurry photo');
    });

    it('applies the selected customer\'s KYC status when picked from search results', () => {
      const fixture = create();
      const c = fixture.componentInstance;
      const approved = { id: 'c3', customerId: 'cust3', fullName: 'Priya Sharma', kycApproved: true, kycStatus: 'Approved' } as AgentCustomerDto;

      c.selectCustomerFromSearch(approved);

      expect(c.selectedCustomerKycApproved).toBe(true);
      expect(c.selectedCustomerKycStatus).toBe('Approved');
      expect(c.selectedCustomerKycRejectionReason).toBeNull();
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
    it('shows an error toast and does not call generateQuote when no product matches the selected type', () => {
      const fixture = create();
      fixture.componentInstance.selectedType = 'Life';
      fixture.componentInstance.calculateQuote();
      expect(agentService.generateQuote).not.toHaveBeenCalled();
      expect(toast.error).toHaveBeenCalledWith(expect.stringContaining('No active Life product'));
    });

    it('does not send an age for Motor quotes (Motor is not age-rated)', () => {
      const fixture = create();
      fixture.componentInstance.selectedType = 'Motor';
      fixture.componentInstance.motorForm.idv = '9,00,000';
      agentService.generateQuote.mockReturnValue(of({ premiumAmount: 18000 }));

      fixture.componentInstance.calculateQuote();

      expect(agentService.generateQuote).toHaveBeenCalledWith({
        productId: 'prod-motor', age: undefined, sumAssured: 900000, tenureYears: 1,
      });
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

    it('sets calculatingQuote while in flight, blocks a duplicate call, and clears on success', () => {
      const fixture = create();
      const c = fixture.componentInstance;
      c.selectedType = 'Health';
      const subject = new Subject<any>();
      agentService.generateQuote.mockReturnValue(subject);

      c.calculateQuote();

      expect(c.calculatingQuote()).toBe(true);
      c.calculateQuote();
      expect(agentService.generateQuote).toHaveBeenCalledTimes(1);

      subject.next({ premiumAmount: 4000 });
      subject.complete();

      expect(c.calculatingQuote()).toBe(false);
      expect(c.quoteResult()).toEqual({ premiumAmount: 4000 });
    });

    it('clears calculatingQuote on error', () => {
      const fixture = create();
      const c = fixture.componentInstance;
      c.selectedType = 'Health';
      const subject = new Subject<any>();
      agentService.generateQuote.mockReturnValue(subject);

      c.calculateQuote();
      expect(c.calculatingQuote()).toBe(true);

      subject.error({ status: 500 });

      expect(c.calculatingQuote()).toBe(false);
    });
  });

  describe('nextStep (step 3: confirm + submit)', () => {
    function setupReadyToSubmit(fixture: ReturnType<typeof create>) {
      fixture.componentInstance.selectedType = 'Health';
      fixture.componentInstance.currentStep = 3;
      fixture.componentInstance.selectedCustomerId = 'cust1';
      fixture.componentInstance.quoteResult.set({ sumAssured: 500000, tenureYears: 1, premiumAmount: 4000 });
      fixture.componentInstance.docRequirementsLoaded.set(true);
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

    it('sets submitting while in flight, blocks a duplicate submit, and clears on success', () => {
      const fixture = create();
      const c = fixture.componentInstance;
      setupReadyToSubmit(fixture);
      c.confirmReady = true;
      const subject = new Subject<any>();
      agentService.submitProposal.mockReturnValue(subject);

      c.nextStep();

      expect(c.submitting()).toBe(true);
      c.nextStep();
      expect(agentService.submitProposal).toHaveBeenCalledTimes(1);

      subject.next({ id: 'p-new' });
      subject.complete();

      expect(c.submitting()).toBe(false);
      expect(router.navigate).toHaveBeenCalledWith(['/agent/proposals']);
    });

    it('clears submitting on error', () => {
      const fixture = create();
      const c = fixture.componentInstance;
      setupReadyToSubmit(fixture);
      c.confirmReady = true;
      const subject = new Subject<any>();
      agentService.submitProposal.mockReturnValue(subject);

      c.nextStep();
      expect(c.submitting()).toBe(true);

      subject.error({ status: 500 });

      expect(c.submitting()).toBe(false);
    });

    it('waits for document uploads to finish before clearing submitting and navigating', () => {
      const fixture = create();
      const c = fixture.componentInstance;
      setupReadyToSubmit(fixture);
      c.confirmReady = true;
      c.uploadedFiles.set('doc1', new File(['x'], 'a.pdf'));
      agentService.submitProposal.mockReturnValue(of({ id: 'p-new' }));
      const uploadSubject = new Subject<any>();
      agentService.uploadProposalDocument.mockReturnValue(uploadSubject);

      c.nextStep();

      expect(c.submitting()).toBe(true);
      expect(router.navigate).not.toHaveBeenCalled();

      uploadSubject.next({ message: 'ok' });
      uploadSubject.complete();

      expect(c.submitting()).toBe(false);
      expect(router.navigate).toHaveBeenCalledWith(['/agent/proposals']);
    });
  });

  describe('customer search', () => {
    beforeEach(() => {
      vi.useFakeTimers();
    });

    afterEach(() => {
      vi.useRealTimers();
    });

    it('does not search below 2 characters', () => {
      const fixture = create();
      fixture.componentInstance.onCustomerSearchInput('a');
      vi.advanceTimersByTime(500);
      expect(agentService.searchCustomers).not.toHaveBeenCalled();
      expect(fixture.componentInstance.customerSearchResults()).toEqual([]);
    });

    it('debounces and searches after 2+ characters', () => {
      const results = [{ id: 'c2', customerId: 'cust2', fullName: 'Arjun Nair', email: 'arjun@example.com' } as AgentCustomerDto];
      const fixture = create();
      agentService.searchCustomers.mockReturnValue(of(results));

      fixture.componentInstance.onCustomerSearchInput('arjun');
      expect(agentService.searchCustomers).not.toHaveBeenCalled();
      vi.advanceTimersByTime(300);

      expect(agentService.searchCustomers).toHaveBeenCalledWith('arjun');
      expect(fixture.componentInstance.customerSearchResults()).toEqual(results);
    });

    it('selecting a search result sets the customer and clears the search', () => {
      const fixture = create();
      const c = fixture.componentInstance;
      const picked = { id: 'c2', customerId: 'cust2', fullName: 'Arjun Nair', email: 'arjun@example.com' } as AgentCustomerDto;

      c.selectCustomerFromSearch(picked);

      expect(c.selectedCustomerId).toBe('cust2');
      expect(c.selectedCustomerName).toBe('Arjun Nair');
      expect(c.proposerForm.fullName).toBe('Arjun Nair');
      expect(c.customerSearchResults()).toEqual([]);
      expect(c.customerSearchQuery()).toBe('');
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

  describe('canDeactivate', () => {
    it('allows navigation while still on step 0 (customer/type selection)', () => {
      const fixture = create();
      fixture.componentInstance.selectedType = 'Health';
      expect(fixture.componentInstance.canDeactivate()).toBe(true);
    });

    it('prompts for confirmation once past step 0', () => {
      const fixture = create();
      fixture.componentInstance.currentStep = 1;

      const result = fixture.componentInstance.canDeactivate();

      expect(fixture.componentInstance.showLeaveConfirm()).toBe(true);
      expect(result).not.toBe(true);
    });

    it('resolves true on confirmLeave and false on cancelLeave', async () => {
      const fixture = create();
      fixture.componentInstance.currentStep = 1;

      const result$ = fixture.componentInstance.canDeactivate();
      const resultPromise = new Promise(resolve => (result$ as any).subscribe(resolve));
      fixture.componentInstance.confirmLeave();

      expect(await resultPromise).toBe(true);
      expect(fixture.componentInstance.showLeaveConfirm()).toBe(false);
    });

    it('allows navigation without prompting right after a successful submit', () => {
      const fixture = create();
      const c = fixture.componentInstance;
      c.selectedType = 'Health';
      c.currentStep = 3;
      c.selectedCustomerId = 'cust1';
      c.quoteResult.set({ sumAssured: 500000, tenureYears: 1, premiumAmount: 4000 });
      c.docRequirementsLoaded.set(true);
      c.confirmReady = true;
      agentService.submitProposal.mockReturnValue(of({ id: 'p-new' }));

      c.nextStep();

      expect(c.canDeactivate()).toBe(true);
    });
  });
});
