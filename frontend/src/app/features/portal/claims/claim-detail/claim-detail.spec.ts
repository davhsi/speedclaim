import { vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { of, throwError, Subject } from 'rxjs';
import { ClaimDetailComponent } from './claim-detail';
import { ClaimService } from '../services/claim.service';
import { ToastService } from '../../../../shared/components/toast/toast.service';
import { ClaimDto, ClaimStatusHistoryDto } from '../../../../core/models/api.models';

describe('ClaimDetailComponent', () => {
  let claimService: { getById: ReturnType<typeof vi.fn>; getHistory: ReturnType<typeof vi.fn>; uploadDocument: ReturnType<typeof vi.fn>; withdraw: ReturnType<typeof vi.fn> };
  let toast: { success: ReturnType<typeof vi.fn>; error: ReturnType<typeof vi.fn> };

  const baseClaim: ClaimDto = {
    id: 'c1', claimNumber: 'CLM-1', policyId: 'p1', customerId: 'u1',
    claimType: 'Health', claimAmountRequested: 5000, isCashless: false, status: 'Intimated',
  } as ClaimDto;

  function create(claim: ClaimDto | null = baseClaim, history: ClaimStatusHistoryDto[] = []) {
    claimService.getById.mockReturnValue(claim ? of(claim) : throwError(() => ({ status: 404 })));
    claimService.getHistory.mockReturnValue(of(history));

    TestBed.configureTestingModule({
      imports: [ClaimDetailComponent],
      providers: [
        { provide: ClaimService, useValue: claimService },
        { provide: ToastService, useValue: toast },
        { provide: Router, useValue: { navigate: vi.fn() } },
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: new Map([['id', 'c1']]) } } },
      ],
    });
    const fixture = TestBed.createComponent(ClaimDetailComponent);
    fixture.detectChanges();
    return fixture;
  }

  beforeEach(() => {
    claimService = { getById: vi.fn(), getHistory: vi.fn(), uploadDocument: vi.fn(), withdraw: vi.fn() };
    toast = { success: vi.fn(), error: vi.fn() };
  });

  describe('ngOnInit', () => {
    it('loads the claim and maps history into timeline items', () => {
      const history: ClaimStatusHistoryDto[] = [{ id: 'h1', claimId: 'c1', oldStatus: 'Intimated', newStatus: 'UnderReview', changedAt: '2026-01-01', notes: 'reviewing' } as ClaimStatusHistoryDto];
      const fixture = create(baseClaim, history);
      expect(fixture.componentInstance.claim()).toEqual(baseClaim);
      expect(fixture.componentInstance.timeline()).toEqual([{ status: 'UnderReview', date: '2026-01-01', remarks: 'reviewing' }]);
      expect(fixture.componentInstance.loading()).toBe(false);
    });

    it('stops loading without a claim when the fetch fails', () => {
      const fixture = create(null);
      expect(fixture.componentInstance.claim()).toBeNull();
      expect(fixture.componentInstance.loading()).toBe(false);
    });
  });

  describe('domainBgClass / domainIcon', () => {
    it('maps a known claim type to its background class', () => {
      const fixture = create({ ...baseClaim, claimType: 'Health' });
      expect(fixture.componentInstance.domainBgClass()).toBe('bg-success-bg');
    });

    it('falls back to a default background class for an unmapped type', () => {
      const fixture = create({ ...baseClaim, claimType: 'Theft' });
      expect(fixture.componentInstance.domainBgClass()).toBe('bg-surface-alt');
    });
  });

  describe('uploadDoc', () => {
    it('uploads the file with a sanitized, uppercased document key derived from the filename', () => {
      const fixture = create();
      claimService.uploadDocument.mockReturnValue(of({ message: 'ok' }));
      const file = new File(['x'], 'my report.pdf');

      fixture.componentInstance.uploadDoc(file);

      expect(claimService.uploadDocument).toHaveBeenCalledWith('c1', 'MY_REPORT', file);
      expect(toast.success).toHaveBeenCalledWith('Document uploaded');
      expect(fixture.componentInstance.uploading()).toBe(false);
    });

    it('falls back to SUPPORTING_DOCUMENT when the filename sanitizes to nothing', () => {
      const fixture = create();
      claimService.uploadDocument.mockReturnValue(of({ message: 'ok' }));
      fixture.componentInstance.uploadDoc(new File(['x'], '....pdf'));
      expect(claimService.uploadDocument).toHaveBeenCalledWith('c1', 'SUPPORTING_DOCUMENT', expect.any(File));
    });

    it('does nothing when there is no loaded claim', () => {
      const fixture = create(null);
      fixture.componentInstance.uploadDoc(new File(['x'], 'a.pdf'));
      expect(claimService.uploadDocument).not.toHaveBeenCalled();
    });

    it('shows an error toast when the upload fails', () => {
      const fixture = create();
      claimService.uploadDocument.mockReturnValue(throwError(() => ({ status: 500 })));
      fixture.componentInstance.uploadDoc(new File(['x'], 'a.pdf'));
      expect(toast.error).toHaveBeenCalledWith('Upload failed');
      expect(fixture.componentInstance.uploading()).toBe(false);
    });
  });

  describe('confirmWithdraw', () => {
    it('withdraws the claim and updates local status on success', () => {
      const fixture = create();
      claimService.withdraw.mockReturnValue(of({ message: 'ok' }));
      fixture.componentInstance.showWithdrawDialog.set(true);

      fixture.componentInstance.confirmWithdraw();

      expect(claimService.withdraw).toHaveBeenCalledWith('c1');
      expect(toast.success).toHaveBeenCalledWith('Claim withdrawn successfully');
      expect(fixture.componentInstance.claim()?.status).toBe('Withdrawn');
      expect(fixture.componentInstance.showWithdrawDialog()).toBe(false);
    });

    it('shows an error toast and closes the dialog when withdrawal fails', () => {
      const fixture = create();
      claimService.withdraw.mockReturnValue(throwError(() => ({ status: 500 })));
      fixture.componentInstance.confirmWithdraw();
      expect(toast.error).toHaveBeenCalledWith('Failed to withdraw claim');
      expect(fixture.componentInstance.showWithdrawDialog()).toBe(false);
    });

    it('does nothing when there is no loaded claim', () => {
      const fixture = create(null);
      fixture.componentInstance.confirmWithdraw();
      expect(claimService.withdraw).not.toHaveBeenCalled();
    });

    it('sets withdrawing while in flight, blocks a duplicate call, and clears on success', () => {
      const fixture = create();
      const subject = new Subject<{ message: string }>();
      claimService.withdraw.mockReturnValue(subject);
      fixture.componentInstance.showWithdrawDialog.set(true);

      fixture.componentInstance.confirmWithdraw();
      expect(fixture.componentInstance.withdrawing()).toBe(true);

      fixture.componentInstance.confirmWithdraw();
      expect(claimService.withdraw).toHaveBeenCalledTimes(1);

      subject.next({ message: 'ok' });
      subject.complete();

      expect(fixture.componentInstance.withdrawing()).toBe(false);
      expect(fixture.componentInstance.showWithdrawDialog()).toBe(false);
      expect(fixture.componentInstance.claim()?.status).toBe('Withdrawn');
    });

    it('clears withdrawing on error too', () => {
      const fixture = create();
      const subject = new Subject<{ message: string }>();
      claimService.withdraw.mockReturnValue(subject);

      fixture.componentInstance.confirmWithdraw();
      expect(fixture.componentInstance.withdrawing()).toBe(true);

      subject.error({ status: 500 });

      expect(fixture.componentInstance.withdrawing()).toBe(false);
      expect(toast.error).toHaveBeenCalledWith('Failed to withdraw claim');
    });
  });

  describe('document preview', () => {
    it('openPreview/closePreview toggle the previewed document', () => {
      const fixture = create();
      fixture.componentInstance.openPreview({ documentName: 'x.pdf', filePath: 'uploads/claims/x.pdf' } as any);
      expect(fixture.componentInstance.previewDoc()).toEqual({ url: '/uploads/claims/x.pdf', label: 'x.pdf' });
      fixture.componentInstance.closePreview();
      expect(fixture.componentInstance.previewDoc()).toBeNull();
    });
  });
});
