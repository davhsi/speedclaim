import { vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { ProposalDetailComponent } from './proposal-detail';
import { ProposalService } from '../services/proposal.service';
import { ProductService } from '../../products/services/product.service';
import { ToastService } from '../../../../shared/components/toast/toast.service';
import { ProductDto, ProposalDto } from '../../../../core/models/api.models';

describe('ProposalDetailComponent', () => {
  let proposalService: { getById: ReturnType<typeof vi.fn>; withdraw: ReturnType<typeof vi.fn>; uploadDocument: ReturnType<typeof vi.fn> };
  let productService: { getById: ReturnType<typeof vi.fn> };
  let toast: { success: ReturnType<typeof vi.fn>; error: ReturnType<typeof vi.fn> };

  const baseProposal: ProposalDto = {
    id: 'pr1', proposalNumber: 'PR-1', customerId: 'u1', productId: 'prod1', productName: 'Health Plus',
    domain: 'Health', status: 'Submitted', sumAssured: 100000, tenureYears: 10, premiumAmount: 500, paymentFrequency: 'Monthly',
  } as ProposalDto;

  const product: ProductDto = { id: 'prod1', productName: 'Health Plus', domain: 'Health' } as ProductDto;

  function create(proposal: ProposalDto | null = baseProposal, productResult: ProductDto | null = product) {
    proposalService.getById.mockReturnValue(proposal ? of(proposal) : throwError(() => ({ status: 404 })));
    productService.getById.mockReturnValue(productResult ? of(productResult) : throwError(() => ({ status: 404 })));

    TestBed.configureTestingModule({
      imports: [ProposalDetailComponent],
      providers: [
        { provide: ProposalService, useValue: proposalService },
        { provide: ProductService, useValue: productService },
        { provide: ToastService, useValue: toast },
        { provide: Router, useValue: { navigate: vi.fn() } },
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: new Map([['id', 'pr1']]) } } },
      ],
    });
    const fixture = TestBed.createComponent(ProposalDetailComponent);
    fixture.detectChanges();
    return fixture;
  }

  beforeEach(() => {
    proposalService = { getById: vi.fn(), withdraw: vi.fn(), uploadDocument: vi.fn() };
    productService = { getById: vi.fn() };
    toast = { success: vi.fn(), error: vi.fn() };
  });

  describe('ngOnInit', () => {
    it('loads the proposal and its product', () => {
      const fixture = create();
      expect(fixture.componentInstance.proposal()).toEqual(baseProposal);
      expect(fixture.componentInstance.product()).toEqual(product);
      expect(fixture.componentInstance.loading()).toBe(false);
    });

    it('sets product to null (without failing the page) when the product fetch fails', () => {
      const fixture = create(baseProposal, null);
      expect(fixture.componentInstance.proposal()).toEqual(baseProposal);
      expect(fixture.componentInstance.product()).toBeNull();
      expect(fixture.componentInstance.loading()).toBe(false);
    });

    it('stops loading when the proposal fetch fails', () => {
      const fixture = create(null);
      expect(fixture.componentInstance.proposal()).toBeNull();
      expect(fixture.componentInstance.loading()).toBe(false);
    });
  });

  describe('productName / displayDomain', () => {
    it('prefers the fetched product name, falling back to the proposal name', () => {
      const fixture = create(baseProposal, null);
      expect(fixture.componentInstance.productName()).toBe('Health Plus');
    });

    it('falls back to a default label when neither is available', () => {
      const fixture = create({ ...baseProposal, productName: undefined }, null);
      expect(fixture.componentInstance.productName()).toBe('Insurance proposal');
    });
  });

  describe('domainBgClass', () => {
    it('maps a known domain to its background class', () => {
      const fixture = create();
      expect(fixture.componentInstance.domainBgClass()).toBe('bg-success-bg');
    });
  });

  describe('canWithdraw', () => {
    it.each(['Submitted', 'DocumentsPending', 'UnderReview'])('is true while status is %s', (status) => {
      const fixture = create({ ...baseProposal, status: status as ProposalDto['status'] });
      expect(fixture.componentInstance.canWithdraw()).toBe(true);
    });

    it.each(['Approved', 'Rejected', 'Withdrawn', 'Draft'])('is false while status is %s', (status) => {
      const fixture = create({ ...baseProposal, status: status as ProposalDto['status'] });
      expect(fixture.componentInstance.canWithdraw()).toBe(false);
    });
  });

  describe('confirmWithdraw', () => {
    it('withdraws the proposal and updates local status on success', () => {
      const fixture = create();
      proposalService.withdraw.mockReturnValue(of({ message: 'ok' }));
      fixture.componentInstance.showWithdrawDialog.set(true);

      fixture.componentInstance.confirmWithdraw();

      expect(proposalService.withdraw).toHaveBeenCalledWith('pr1');
      expect(toast.success).toHaveBeenCalledWith('Proposal withdrawn');
      expect(fixture.componentInstance.proposal()?.status).toBe('Withdrawn');
      expect(fixture.componentInstance.showWithdrawDialog()).toBe(false);
    });

    it('shows an error toast and closes the dialog on failure', () => {
      const fixture = create();
      proposalService.withdraw.mockReturnValue(throwError(() => ({ status: 500 })));
      fixture.componentInstance.confirmWithdraw();
      expect(toast.error).toHaveBeenCalledWith('Failed to withdraw proposal');
      expect(fixture.componentInstance.showWithdrawDialog()).toBe(false);
    });

    it('does nothing without a loaded proposal', () => {
      const fixture = create(null);
      fixture.componentInstance.confirmWithdraw();
      expect(proposalService.withdraw).not.toHaveBeenCalled();
    });
  });

  describe('onDocUpload', () => {
    it('uploads using the filename (without extension) as the document key', () => {
      const fixture = create();
      proposalService.uploadDocument.mockReturnValue(of({ message: 'ok' }));
      const file = new File(['x'], 'income-proof.pdf');

      fixture.componentInstance.onDocUpload(file);

      expect(proposalService.uploadDocument).toHaveBeenCalledWith('pr1', 'income-proof', file);
      expect(toast.success).toHaveBeenCalledWith('Document uploaded');
    });

    it('shows an error toast when the upload fails', () => {
      const fixture = create();
      proposalService.uploadDocument.mockReturnValue(throwError(() => ({ status: 500 })));
      fixture.componentInstance.onDocUpload(new File(['x'], 'doc.pdf'));
      expect(toast.error).toHaveBeenCalledWith('Upload failed');
    });

    it('does nothing without a loaded proposal', () => {
      const fixture = create(null);
      fixture.componentInstance.onDocUpload(new File(['x'], 'doc.pdf'));
      expect(proposalService.uploadDocument).not.toHaveBeenCalled();
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
