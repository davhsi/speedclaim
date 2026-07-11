import { vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { of, throwError, Subject } from 'rxjs';
import { EndorsementListComponent } from './endorsement-list';
import { UnderwriterService } from '../services/underwriter.service';
import { ToastService } from '../../../shared/components/toast/toast.service';
import { EndorsementDto } from '../../../core/models/api.models';

describe('EndorsementListComponent', () => {
  let uwService: { getPendingEndorsements: ReturnType<typeof vi.fn>; reviewEndorsement: ReturnType<typeof vi.fn> };
  let toast: { success: ReturnType<typeof vi.fn>; error: ReturnType<typeof vi.fn> };

  function endorsement(overrides: Partial<EndorsementDto> = {}): EndorsementDto {
    return {
      id: 'e1', policyId: 'p1', endorsementType: 'AddressChange', status: 'Requested',
      description: 'desc', requestedById: 'u1', createdAt: '2026-01-01', ...overrides,
    } as EndorsementDto;
  }

  function create(list: EndorsementDto[] = [endorsement()]) {
    uwService.getPendingEndorsements.mockReturnValue(of({ data: list, total: list.length, page: 1, pageSize: 50 } as never));
    const fixture = TestBed.createComponent(EndorsementListComponent);
    fixture.detectChanges();
    return fixture;
  }

  beforeEach(() => {
    uwService = { getPendingEndorsements: vi.fn(), reviewEndorsement: vi.fn() };
    toast = { success: vi.fn(), error: vi.fn() };

    TestBed.configureTestingModule({
      imports: [EndorsementListComponent],
      providers: [
        { provide: UnderwriterService, useValue: uwService },
        { provide: ToastService, useValue: toast },
      ],
    });
  });

  describe('ngOnInit / loadData', () => {
    it('loads endorsements and computes pendingCount from reviewable (Requested) ones', () => {
      const fixture = create([endorsement({ id: 'e1', status: 'Requested' }), endorsement({ id: 'e2', status: 'Approved' })]);
      const c = fixture.componentInstance;
      expect(c.endorsements()).toHaveLength(2);
      expect(c.pendingCount()).toBe(1);
      expect(c.loading()).toBe(false);
    });

    it('stops loading even if the request fails', () => {
      uwService.getPendingEndorsements.mockReturnValue(throwError(() => ({ status: 500 })));
      const fixture = TestBed.createComponent(EndorsementListComponent);
      fixture.detectChanges();
      expect(fixture.componentInstance.loading()).toBe(false);
    });
  });

  describe('isReviewable', () => {
    it('is true only for Requested status', () => {
      const fixture = create();
      expect(fixture.componentInstance.isReviewable(endorsement({ status: 'Requested' }))).toBe(true);
      expect(fixture.componentInstance.isReviewable(endorsement({ status: 'Approved' }))).toBe(false);
    });
  });

  describe('formatType', () => {
    it('maps known endorsement types to friendly labels', () => {
      const fixture = create();
      expect(fixture.componentInstance.formatType('NomineeChange')).toBe('Nominee change');
      expect(fixture.componentInstance.formatType('SumAssuredChange')).toBe('Sum assured change');
    });

    it('falls back to the raw type for an unmapped value', () => {
      const fixture = create();
      expect(fixture.componentInstance.formatType('SomethingElse')).toBe('SomethingElse');
    });
  });

  describe('pagination', () => {
    it('computes totalPages from the endorsement count and page size (10)', () => {
      const list = Array.from({ length: 25 }, (_, i) => endorsement({ id: `e${i}` }));
      const fixture = create(list);
      expect(fixture.componentInstance.totalPages()).toBe(3);
      expect(fixture.componentInstance.pagedEndorsements()).toHaveLength(10);
    });

    it('onPageChange moves to the requested page slice', () => {
      const list = Array.from({ length: 25 }, (_, i) => endorsement({ id: `e${i}` }));
      const fixture = create(list);
      fixture.componentInstance.onPageChange(3);
      expect(fixture.componentInstance.currentPage()).toBe(3);
      expect(fixture.componentInstance.pagedEndorsements()).toHaveLength(5);
    });
  });

  describe('onApprove', () => {
    it('approves, shows a success toast, and reloads the list', () => {
      const fixture = create([endorsement({ id: 'e1' })]);
      uwService.reviewEndorsement.mockReturnValue(of({ message: 'ok' }));

      fixture.componentInstance.onApprove(endorsement({ id: 'e1' }));

      expect(uwService.reviewEndorsement).toHaveBeenCalledWith('e1', { isApproved: true, reason: 'Approved' });
      expect(toast.success).toHaveBeenCalledWith('Endorsement approved.');
      expect(uwService.getPendingEndorsements).toHaveBeenCalledTimes(2); // init + reload
    });

    it('marks only the target endorsement as submitting while the request is in flight, and blocks a duplicate click', () => {
      const fixture = create([endorsement({ id: 'e1' }), endorsement({ id: 'e2' })]);
      const c = fixture.componentInstance;
      const subject = new Subject<{ message: string }>();
      uwService.reviewEndorsement.mockReturnValue(subject);

      const e1 = endorsement({ id: 'e1' });
      c.onApprove(e1);

      expect(c.isSubmitting(e1)).toBe(true);
      expect(c.isSubmitting(endorsement({ id: 'e2' }))).toBe(false);

      c.onApprove(e1);
      expect(uwService.reviewEndorsement).toHaveBeenCalledTimes(1);

      subject.next({ message: 'ok' });
      subject.complete();

      expect(c.isSubmitting(e1)).toBe(false);
    });

    it('clears the submitting state on error without a success toast', () => {
      const fixture = create([endorsement({ id: 'e1' })]);
      const c = fixture.componentInstance;
      const subject = new Subject<{ message: string }>();
      uwService.reviewEndorsement.mockReturnValue(subject);

      const e1 = endorsement({ id: 'e1' });
      c.onApprove(e1);
      expect(c.isSubmitting(e1)).toBe(true);

      subject.error({ status: 500 });

      expect(c.isSubmitting(e1)).toBe(false);
      expect(toast.success).not.toHaveBeenCalled();
    });
  });

  describe('reject flow', () => {
    it('openReject stores the target endorsement and clears the reason', () => {
      const fixture = create();
      const e = endorsement({ id: 'e2' });
      fixture.componentInstance.rejectReason = 'stale';

      fixture.componentInstance.openReject(e);

      expect(fixture.componentInstance.rejectingEndorsement()).toEqual(e);
      expect(fixture.componentInstance.rejectReason).toBe('');
    });

    it('confirmReject does nothing when the reason is blank', () => {
      const fixture = create();
      fixture.componentInstance.openReject(endorsement({ id: 'e2' }));
      fixture.componentInstance.rejectReason = '   ';

      fixture.componentInstance.confirmReject();

      expect(uwService.reviewEndorsement).not.toHaveBeenCalled();
    });

    it('confirmReject submits the reason, shows an error-style toast, clears state, and reloads', () => {
      const fixture = create([endorsement({ id: 'e2' })]);
      fixture.componentInstance.openReject(endorsement({ id: 'e2' }));
      fixture.componentInstance.rejectReason = 'Insufficient documents';
      uwService.reviewEndorsement.mockReturnValue(of({ message: 'ok' }));

      fixture.componentInstance.confirmReject();

      expect(uwService.reviewEndorsement).toHaveBeenCalledWith('e2', { isApproved: false, reason: 'Insufficient documents' });
      expect(toast.error).toHaveBeenCalledWith('Endorsement rejected.');
      expect(fixture.componentInstance.rejectingEndorsement()).toBeNull();
    });

    it('sets isSubmitting while in flight, blocks a duplicate confirm and closeReject, and clears on success', () => {
      const fixture = create([endorsement({ id: 'e2' })]);
      const c = fixture.componentInstance;
      const e2 = endorsement({ id: 'e2' });
      c.openReject(e2);
      c.rejectReason = 'Insufficient documents';
      const subject = new Subject<{ message: string }>();
      uwService.reviewEndorsement.mockReturnValue(subject);

      c.confirmReject();
      expect(c.isSubmitting(e2)).toBe(true);

      c.confirmReject();
      expect(uwService.reviewEndorsement).toHaveBeenCalledTimes(1);

      c.closeReject();
      expect(c.rejectingEndorsement()).not.toBeNull();

      subject.next({ message: 'ok' });
      subject.complete();

      expect(c.isSubmitting(e2)).toBe(false);
      expect(c.rejectingEndorsement()).toBeNull();
    });

    it('clears isSubmitting on error and keeps the dialog open for retry', () => {
      const fixture = create([endorsement({ id: 'e2' })]);
      const c = fixture.componentInstance;
      const e2 = endorsement({ id: 'e2' });
      c.openReject(e2);
      c.rejectReason = 'Insufficient documents';
      const subject = new Subject<{ message: string }>();
      uwService.reviewEndorsement.mockReturnValue(subject);

      c.confirmReject();
      subject.error({ status: 500 });

      expect(c.isSubmitting(e2)).toBe(false);
      expect(c.rejectingEndorsement()).not.toBeNull();
      expect(toast.error).not.toHaveBeenCalled();
    });
  });
});
