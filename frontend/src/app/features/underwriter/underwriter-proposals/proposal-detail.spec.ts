import { vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router, convertToParamMap } from '@angular/router';
import { of, throwError } from 'rxjs';
import { ProposalDetailComponent } from './proposal-detail';
import { UnderwriterService } from '../services/underwriter.service';
import { ProductService } from '../../portal/products/services/product.service';
import { ToastService } from '../../../shared/components/toast/toast.service';
import { ProductDto, ProposalDto } from '../../../core/models/api.models';

describe('ProposalDetailComponent', () => {
  let uwService: {
    getProposalById: ReturnType<typeof vi.fn>;
    reviewProposal: ReturnType<typeof vi.fn>;
    requestDocs: ReturnType<typeof vi.fn>;
    updateNotes: ReturnType<typeof vi.fn>;
  };
  let productService: { getById: ReturnType<typeof vi.fn> };
  let router: { navigate: ReturnType<typeof vi.fn> };
  let toast: { success: ReturnType<typeof vi.fn>; error: ReturnType<typeof vi.fn>; warning: ReturnType<typeof vi.fn> };

  const proposal = (overrides: Partial<ProposalDto> = {}): ProposalDto => ({
    id: 'p1', proposalNumber: 'PRO-1', customerId: 'c1', productId: 'prod1', status: 'Submitted',
    sumAssured: 500000, tenureYears: 10, premiumAmount: 12000, paymentFrequency: 'Annually',
    createdAt: '2026-01-01', ...overrides,
  });

  const product = (overrides: Partial<ProductDto> = {}): ProductDto => ({
    id: 'prod1', productName: 'Family Health Shield', uin: 'UIN1', description: 'x', domain: 'Health',
    minAge: 18, maxAge: 65, minSumAssured: 100000, maxSumAssured: 10000000, minTenureYears: 1,
    maxTenureYears: 30, waitingPeriodDays: 30, allowsFamilyFloater: true, maxFamilyMembers: 5,
    isActive: true, ...overrides,
  } as ProductDto);

  function create(p: ProposalDto | null = proposal(), prod: ProductDto | null = product()) {
    TestBed.resetTestingModule();
    if (p) uwService.getProposalById.mockReturnValue(of(p));
    else uwService.getProposalById.mockReturnValue(throwError(() => ({ status: 404 })));
    if (prod) productService.getById.mockReturnValue(of(prod));
    else productService.getById.mockReturnValue(throwError(() => ({ status: 404 })));

    TestBed.configureTestingModule({
      imports: [ProposalDetailComponent],
      providers: [
        { provide: UnderwriterService, useValue: uwService },
        { provide: ProductService, useValue: productService },
        { provide: Router, useValue: router },
        { provide: ToastService, useValue: toast },
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: convertToParamMap({ id: 'p1' }) } } },
      ],
    });
    const fixture = TestBed.createComponent(ProposalDetailComponent);
    fixture.detectChanges();
    return fixture;
  }

  beforeEach(() => {
    uwService = { getProposalById: vi.fn(), reviewProposal: vi.fn(), requestDocs: vi.fn(), updateNotes: vi.fn() };
    productService = { getById: vi.fn() };
    router = { navigate: vi.fn() };
    toast = { success: vi.fn(), error: vi.fn(), warning: vi.fn() };
  });

  describe('ngOnInit', () => {
    it('loads the proposal by routed id and its product, and seeds notes', () => {
      const fixture = create(proposal({ underwriterNotes: 'existing note' }));
      expect(uwService.getProposalById).toHaveBeenCalledWith('p1');
      expect(productService.getById).toHaveBeenCalledWith('prod1');
      expect(fixture.componentInstance.product()).toEqual(product());
      expect(fixture.componentInstance.notes).toBe('existing note');
    });

    it('clears the product when the product fetch fails', () => {
      const fixture = create(proposal(), null);
      expect(fixture.componentInstance.product()).toBeNull();
    });
  });

  describe('riskFlags', () => {
    it('is empty for a modest, short-tenure proposal', () => {
      const fixture = create(proposal({ sumAssured: 500000, tenureYears: 5 }));
      expect(fixture.componentInstance.riskFlags()).toEqual([]);
    });

    it('flags high value at >= 25 lakh', () => {
      const fixture = create(proposal({ sumAssured: 2_500_000, tenureYears: 5 }));
      expect(fixture.componentInstance.riskFlags().some(f => f.label.includes('High value'))).toBe(true);
    });

    it('flags very high value at >= 1 crore, taking precedence over the high-value flag', () => {
      const fixture = create(proposal({ sumAssured: 10_000_000, tenureYears: 5 }));
      const flags = fixture.componentInstance.riskFlags();
      expect(flags.some(f => f.label.includes('Very high value'))).toBe(true);
      expect(flags.some(f => f.label.includes('High value ·'))).toBe(false);
    });

    it('flags long tenure at >= 20 years', () => {
      const fixture = create(proposal({ sumAssured: 500000, tenureYears: 25 }));
      expect(fixture.componentInstance.riskFlags().some(f => f.label.includes('Long tenure'))).toBe(true);
    });
  });

  describe('canDecide / canRequestDocuments', () => {
    it('can decide Submitted, UnderReview, or DocumentsPending proposals', () => {
      expect(create(proposal({ status: 'Submitted' })).componentInstance.canDecide()).toBe(true);
      expect(create(proposal({ status: 'UnderReview' })).componentInstance.canDecide()).toBe(true);
      expect(create(proposal({ status: 'DocumentsPending' })).componentInstance.canDecide()).toBe(true);
      expect(create(proposal({ status: 'Approved' })).componentInstance.canDecide()).toBe(false);
    });

    it('allows document requests for DocumentsPending too', () => {
      expect(create(proposal({ status: 'DocumentsPending' })).componentInstance.canRequestDocuments()).toBe(true);
      expect(create(proposal({ status: 'Rejected' })).componentInstance.canRequestDocuments()).toBe(false);
    });
  });

  describe('onApprove', () => {
    it('does nothing when the proposal is not pending', () => {
      const fixture = create(proposal({ status: 'Approved' }));
      fixture.componentInstance.onApprove();
      expect(uwService.reviewProposal).not.toHaveBeenCalled();
    });

    it('approves with the entered notes, or a default, and navigates back on success', () => {
      const fixture = create(proposal({ id: 'p1', status: 'Submitted' }));
      fixture.componentInstance.notes = '';
      uwService.reviewProposal.mockReturnValue(of({ message: 'ok' }));

      fixture.componentInstance.onApprove();

      expect(uwService.reviewProposal).toHaveBeenCalledWith('p1', { isApproved: true, notes: 'Approved' });
      expect(toast.success).toHaveBeenCalled();
      expect(router.navigate).toHaveBeenCalledWith(['/underwriter/proposals']);
    });

    it('shows an error toast and clears actionInFlight on failure', () => {
      const fixture = create(proposal({ status: 'Submitted' }));
      uwService.reviewProposal.mockReturnValue(throwError(() => ({ status: 500 })));

      fixture.componentInstance.onApprove();

      expect(toast.error).toHaveBeenCalledWith('Approval failed.');
      expect(fixture.componentInstance.actionInFlight()).toBe(false);
    });
  });

  describe('onReject', () => {
    it('does nothing without a reject reason', () => {
      const fixture = create(proposal({ status: 'Submitted' }));
      fixture.componentInstance.rejectReason = '  ';
      fixture.componentInstance.onReject();
      expect(uwService.reviewProposal).not.toHaveBeenCalled();
    });

    it('rejects with the trimmed reason and navigates back on success', () => {
      const fixture = create(proposal({ id: 'p1', status: 'Submitted' }));
      fixture.componentInstance.rejectReason = '  Incomplete KYC  ';
      uwService.reviewProposal.mockReturnValue(of({ message: 'ok' }));

      fixture.componentInstance.onReject();

      expect(uwService.reviewProposal).toHaveBeenCalledWith('p1', { isApproved: false, notes: 'Incomplete KYC' });
      expect(toast.error).toHaveBeenCalledWith('Proposal rejected.');
      expect(router.navigate).toHaveBeenCalledWith(['/underwriter/proposals']);
    });
  });

  describe('onRequestDocs', () => {
    it('does nothing without request text', () => {
      const fixture = create(proposal({ status: 'Submitted' }));
      fixture.componentInstance.docsRequest = '';
      fixture.componentInstance.onRequestDocs();
      expect(uwService.requestDocs).not.toHaveBeenCalled();
    });

    it('sends the request and refetches the proposal on success', () => {
      const fixture = create(proposal({ id: 'p1', status: 'Submitted' }));
      fixture.componentInstance.docsRequest = 'Please upload address proof';
      uwService.requestDocs.mockReturnValue(of({ message: 'ok' }));
      uwService.getProposalById.mockReturnValue(of(proposal({ id: 'p1', status: 'DocumentsPending' })));

      fixture.componentInstance.onRequestDocs();

      expect(uwService.requestDocs).toHaveBeenCalledWith('p1', 'Please upload address proof');
      expect(toast.success).toHaveBeenCalled();
      expect(fixture.componentInstance.proposal()?.status).toBe('DocumentsPending');
      expect(fixture.componentInstance.actionInFlight()).toBe(false);
    });

    it('shows an error toast on failure', () => {
      const fixture = create(proposal({ status: 'Submitted' }));
      fixture.componentInstance.docsRequest = 'x';
      uwService.requestDocs.mockReturnValue(throwError(() => ({ status: 500 })));

      fixture.componentInstance.onRequestDocs();

      expect(toast.error).toHaveBeenCalledWith('Document request failed.');
    });
  });

  describe('saveNotes', () => {
    it('warns instead of saving blank notes', () => {
      const fixture = create();
      fixture.componentInstance.notes = '   ';
      fixture.componentInstance.saveNotes();
      expect(uwService.updateNotes).not.toHaveBeenCalled();
      expect(toast.warning).toHaveBeenCalled();
    });

    it('saves notes and shows a success toast', () => {
      const fixture = create(proposal({ id: 'p1' }));
      fixture.componentInstance.notes = 'Looks fine';
      uwService.updateNotes.mockReturnValue(of({ message: 'ok' }));

      fixture.componentInstance.saveNotes();

      expect(uwService.updateNotes).toHaveBeenCalledWith('p1', 'Looks fine');
      expect(toast.success).toHaveBeenCalledWith('Notes saved.');
    });
  });

  describe('productName / displayDomain', () => {
    it('prefers the fetched product over the proposal fallback', () => {
      const fixture = create(proposal({ productName: 'Fallback', domain: 'Life' }), product({ productName: 'Real Product', domain: 'Health' }));
      expect(fixture.componentInstance.productName()).toBe('Real Product');
      expect(fixture.componentInstance.displayDomain()).toBe('Health');
    });

    it('falls back to the proposal fields when there is no product', () => {
      const fixture = create(proposal({ productName: 'Fallback', domain: 'Life' }), null);
      expect(fixture.componentInstance.productName()).toBe('Fallback');
      expect(fixture.componentInstance.displayDomain()).toBe('Life');
    });

    it('builds static-file links for uploaded documents', () => {
      const fixture = create();
      expect(fixture.componentInstance.documentHref('uploads/proposals/doc.pdf')).toBe('/uploads/proposals/doc.pdf');
      expect(fixture.componentInstance.documentHref('/uploads/proposals/doc.pdf')).toBe('/uploads/proposals/doc.pdf');
    });
  });

  describe('closeDialog / goBack', () => {
    it('closes the dialog unless an action is in flight', () => {
      const fixture = create();
      fixture.componentInstance.showDialog.set('approve');
      fixture.componentInstance.closeDialog();
      expect(fixture.componentInstance.showDialog()).toBeNull();
    });

    it('navigates back to the proposals list', () => {
      const fixture = create();
      fixture.componentInstance.goBack();
      expect(router.navigate).toHaveBeenCalledWith(['/underwriter/proposals']);
    });
  });

  describe('document preview', () => {
    it('openPreview/closePreview toggle the previewed document', () => {
      const fixture = create();
      fixture.componentInstance.openPreview({ documentName: 'x.pdf', filePath: 'uploads/proposals/x.pdf' } as any);
      expect(fixture.componentInstance.previewDoc()).toEqual({ url: '/uploads/proposals/x.pdf', label: 'x.pdf' });
      fixture.componentInstance.closePreview();
      expect(fixture.componentInstance.previewDoc()).toBeNull();
    });
  });
});
