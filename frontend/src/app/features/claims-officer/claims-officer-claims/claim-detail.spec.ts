import { vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router, convertToParamMap } from '@angular/router';
import { of, throwError } from 'rxjs';
import { ClaimDetailComponent } from './claim-detail';
import { ClaimsOfficerService } from '../services/claims-officer.service';
import { AuthService } from '../../../core/services/auth.service';
import { ToastService } from '../../../shared/components/toast/toast.service';
import { AuthUserDto, ClaimDto, ClaimStatusHistoryDto, SubmittedDocumentDto } from '../../../core/models/api.models';

describe('ClaimDetailComponent', () => {
  let claimsService: {
    getClaimById: ReturnType<typeof vi.fn>; getClaimHistory: ReturnType<typeof vi.fn>; getSurveyors: ReturnType<typeof vi.fn>;
    assignToSelf: ReturnType<typeof vi.fn>; approveReject: ReturnType<typeof vi.fn>; settleClaim: ReturnType<typeof vi.fn>;
    assignSurveyor: ReturnType<typeof vi.fn>; requestDocs: ReturnType<typeof vi.fn>; approvePreAuth: ReturnType<typeof vi.fn>;
  };
  let authService: { currentUser: ReturnType<typeof vi.fn> };
  let toast: { success: ReturnType<typeof vi.fn>; error: ReturnType<typeof vi.fn>; warning: ReturnType<typeof vi.fn>; info: ReturnType<typeof vi.fn> };
  let router: { navigate: ReturnType<typeof vi.fn> };

  const officer = { id: 'officer-1' } as AuthUserDto;

  const baseClaim: ClaimDto = {
    id: 'claim1', claimNumber: 'CL-1', policyId: 'pol1', customerId: 'cust1',
    claimType: 'Accident', claimAmountRequested: 50000, isCashless: false,
    status: 'UnderReview', intimationDate: '2026-01-01', incidentDate: '2026-01-01',
    incidentDescription: 'x', assignedOfficerId: 'officer-1', createdAt: '2026-01-01',
  } as ClaimDto;

  function create(claim: ClaimDto = baseClaim) {
    TestBed.resetTestingModule();
    claimsService = {
      getClaimById: vi.fn(() => of(claim)),
      getClaimHistory: vi.fn(() => of([] as ClaimStatusHistoryDto[])),
      getSurveyors: vi.fn(() => of([])),
      assignToSelf: vi.fn(),
      approveReject: vi.fn(),
      settleClaim: vi.fn(),
      assignSurveyor: vi.fn(),
      requestDocs: vi.fn(),
      approvePreAuth: vi.fn(),
    };
    authService = { currentUser: vi.fn(() => officer) };
    toast = { success: vi.fn(), error: vi.fn(), warning: vi.fn(), info: vi.fn() };
    router = { navigate: vi.fn() };

    TestBed.configureTestingModule({
      imports: [ClaimDetailComponent],
      providers: [
        { provide: ClaimsOfficerService, useValue: claimsService },
        { provide: AuthService, useValue: authService },
        { provide: ToastService, useValue: toast },
        { provide: Router, useValue: router },
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: convertToParamMap({ id: 'claim1' }) } } },
      ],
    });
    const fixture = TestBed.createComponent(ClaimDetailComponent);
    fixture.detectChanges();
    return fixture;
  }

  describe('ngOnInit', () => {
    it('loads the claim, its history (mapped into timeline items), and surveyors', () => {
      const history: ClaimStatusHistoryDto[] = [{ id: 'h1', claimId: 'claim1', oldStatus: 'Intimated', newStatus: 'UnderReview', notes: 'reviewed', changedAt: '2026-01-02' } as ClaimStatusHistoryDto];
      claimsService = {
        getClaimById: vi.fn(() => of(baseClaim)),
        getClaimHistory: vi.fn(() => of(history)),
        getSurveyors: vi.fn(() => of([{ id: 's1' }])),
        assignToSelf: vi.fn(), approveReject: vi.fn(), settleClaim: vi.fn(), assignSurveyor: vi.fn(), requestDocs: vi.fn(), approvePreAuth: vi.fn(),
      };
      authService = { currentUser: vi.fn(() => officer) };
      TestBed.configureTestingModule({
        imports: [ClaimDetailComponent],
        providers: [
          { provide: ClaimsOfficerService, useValue: claimsService },
          { provide: AuthService, useValue: authService },
          { provide: ToastService, useValue: { success: vi.fn(), error: vi.fn(), warning: vi.fn(), info: vi.fn() } },
          { provide: Router, useValue: { navigate: vi.fn() } },
          { provide: ActivatedRoute, useValue: { snapshot: { paramMap: convertToParamMap({ id: 'claim1' }) } } },
        ],
      });
      const fixture = TestBed.createComponent(ClaimDetailComponent);
      fixture.detectChanges();

      expect(fixture.componentInstance.claim()).toEqual(baseClaim);
      expect(fixture.componentInstance.timelineItems()).toEqual([{ status: 'UnderReview', date: '2026-01-02', remarks: 'reviewed' }]);
      expect(fixture.componentInstance.surveyors()).toEqual([{ id: 's1' }]);
    });
  });

  describe('permission booleans (assigned officer, claim status)', () => {
    it('isAssignedToSelf is true only when the claim officer matches the current user', () => {
      const fixture = create();
      expect(fixture.componentInstance.isAssignedToSelf()).toBe(true);
    });

    it('isAssignedToSelf is false for a different officer', () => {
      const fixture = create({ ...baseClaim, assignedOfficerId: 'someone-else' });
      expect(fixture.componentInstance.isAssignedToSelf()).toBe(false);
    });

    it('canApprove is true when assigned and status is UnderReview or PreAuthApproved', () => {
      expect(create({ ...baseClaim, status: 'UnderReview' }).componentInstance.canApprove()).toBe(true);
      expect(create({ ...baseClaim, status: 'PreAuthApproved' }).componentInstance.canApprove()).toBe(true);
      expect(create({ ...baseClaim, status: 'Intimated' }).componentInstance.canApprove()).toBe(false);
    });

    it('canReject covers UnderReview/Intimated/DocumentsPending/PreAuthApproved', () => {
      expect(create({ ...baseClaim, status: 'Intimated' }).componentInstance.canReject()).toBe(true);
      expect(create({ ...baseClaim, status: 'Settled' }).componentInstance.canReject()).toBe(false);
    });

    it('canSettle is true only for Approved status while assigned', () => {
      expect(create({ ...baseClaim, status: 'Approved' }).componentInstance.canSettle()).toBe(true);
      expect(create({ ...baseClaim, status: 'UnderReview' }).componentInstance.canSettle()).toBe(false);
    });

    it('isTerminal covers Settled/Rejected/Withdrawn', () => {
      expect(create({ ...baseClaim, status: 'Settled' }).componentInstance.isTerminal()).toBe(true);
      expect(create({ ...baseClaim, status: 'UnderReview' }).componentInstance.isTerminal()).toBe(false);
    });

    it('canAssignSelf is true only when unassigned and not terminal', () => {
      expect(create({ ...baseClaim, assignedOfficerId: undefined, status: 'Intimated' }).componentInstance.canAssignSelf()).toBe(true);
      expect(create({ ...baseClaim, assignedOfficerId: 'officer-1' }).componentInstance.canAssignSelf()).toBe(false);
      expect(create({ ...baseClaim, assignedOfficerId: undefined, status: 'Settled' }).componentInstance.canAssignSelf()).toBe(false);
    });

    it('canAssignSurveyor requires an eligible claim type, no existing surveyor, assigned-to-self, and an early status', () => {
      const eligible = { ...baseClaim, claimType: 'Accident', surveyorId: undefined, status: 'Intimated' } as ClaimDto;
      expect(create(eligible).componentInstance.canAssignSurveyor()).toBe(true);

      const wrongType = { ...eligible, claimType: 'Health' } as ClaimDto;
      expect(create(wrongType).componentInstance.canAssignSurveyor()).toBe(false);

      const alreadyHasSurveyor = { ...eligible, surveyorId: 's1' };
      expect(create(alreadyHasSurveyor).componentInstance.canAssignSurveyor()).toBe(false);
    });

    it('canRequestDocs covers Intimated/UnderReview/PreAuthApproved while assigned', () => {
      expect(create({ ...baseClaim, status: 'UnderReview' }).componentInstance.canRequestDocs()).toBe(true);
      expect(create({ ...baseClaim, status: 'Settled' }).componentInstance.canRequestDocs()).toBe(false);
    });

    it('canApprovePreAuth requires isCashless, assigned-to-self, and PreAuthRequested status', () => {
      expect(create({ ...baseClaim, isCashless: true, status: 'PreAuthRequested' }).componentInstance.canApprovePreAuth()).toBe(true);
      expect(create({ ...baseClaim, isCashless: false, status: 'PreAuthRequested' }).componentInstance.canApprovePreAuth()).toBe(false);
    });
  });

  describe('onAssignSelf', () => {
    it('assigns the claim to the current officer and reloads it', () => {
      const fixture = create({ ...baseClaim, assignedOfficerId: undefined, status: 'Intimated' });
      claimsService.assignToSelf.mockReturnValue(of({ message: 'ok' }));

      fixture.componentInstance.onAssignSelf();

      expect(claimsService.assignToSelf).toHaveBeenCalledWith('claim1');
      expect(toast.success).toHaveBeenCalledWith('Claim assigned to you');
      expect(fixture.componentInstance.actionInFlight()).toBe(false);
    });

    it('does nothing when the claim cannot be self-assigned', () => {
      const fixture = create({ ...baseClaim, assignedOfficerId: 'officer-1' });
      fixture.componentInstance.onAssignSelf();
      expect(claimsService.assignToSelf).not.toHaveBeenCalled();
    });

    it('shows an error toast on failure', () => {
      const fixture = create({ ...baseClaim, assignedOfficerId: undefined, status: 'Intimated' });
      claimsService.assignToSelf.mockReturnValue(throwError(() => ({ status: 409 })));
      fixture.componentInstance.onAssignSelf();
      expect(toast.error).toHaveBeenCalledWith('Failed to assign claim');
      expect(fixture.componentInstance.actionInFlight()).toBe(false);
    });
  });

  describe('modal state', () => {
    it('openModal resets fields and sets the modal type', () => {
      const fixture = create();
      fixture.componentInstance.modalNotes = 'stale';
      fixture.componentInstance.openModal('approve');
      expect(fixture.componentInstance.modalType()).toBe('approve');
      expect(fixture.componentInstance.modalNotes).toBe('');
    });

    it('closeModal clears the modal type', () => {
      const fixture = create();
      fixture.componentInstance.openModal('reject');
      fixture.componentInstance.closeModal();
      expect(fixture.componentInstance.modalType()).toBeNull();
    });

    it('modalConfirmDisabled requires a valid approved amount not exceeding the requested amount', () => {
      const fixture = create({ ...baseClaim, claimAmountRequested: 1000 });
      fixture.componentInstance.openModal('approve');
      fixture.componentInstance.modalAmount = '2000';
      expect(fixture.componentInstance.modalConfirmDisabled()).toBe(true);
      fixture.componentInstance.modalAmount = '500';
      expect(fixture.componentInstance.modalConfirmDisabled()).toBe(false);
    });

    it('modalConfirmDisabled requires a non-empty reason for reject', () => {
      const fixture = create();
      fixture.componentInstance.openModal('reject');
      expect(fixture.componentInstance.modalConfirmDisabled()).toBe(true);
      fixture.componentInstance.modalReason = 'not covered';
      expect(fixture.componentInstance.modalConfirmDisabled()).toBe(false);
    });
  });

  describe('onModalConfirm', () => {
    it('approves the claim, reloads it, and closes the modal on success', () => {
      const fixture = create();
      claimsService.approveReject.mockReturnValue(of({ message: 'ok' }));
      fixture.componentInstance.openModal('approve');
      fixture.componentInstance.modalAmount = '30000';

      fixture.componentInstance.onModalConfirm();

      expect(claimsService.approveReject).toHaveBeenCalledWith('claim1', { isApproved: true, approvedAmount: 30000, reason: '' });
      expect(toast.success).toHaveBeenCalledWith('Claim approved successfully');
      expect(fixture.componentInstance.modalType()).toBeNull();
    });

    it('shows an error toast but keeps the modal open when approval fails', () => {
      const fixture = create();
      claimsService.approveReject.mockReturnValue(throwError(() => ({ status: 500 })));
      fixture.componentInstance.openModal('approve');
      fixture.componentInstance.modalAmount = '30000';

      fixture.componentInstance.onModalConfirm();

      expect(toast.error).toHaveBeenCalledWith('Failed to approve claim');
      expect(fixture.componentInstance.modalType()).toBe('approve');
      expect(fixture.componentInstance.actionInFlight()).toBe(false);
    });

    it('rejects the claim with the given reason', () => {
      const fixture = create();
      claimsService.approveReject.mockReturnValue(of({ message: 'ok' }));
      fixture.componentInstance.openModal('reject');
      fixture.componentInstance.modalReason = 'incomplete documents';

      fixture.componentInstance.onModalConfirm();

      expect(claimsService.approveReject).toHaveBeenCalledWith('claim1', { isApproved: false, reason: 'incomplete documents' });
      expect(toast.success).toHaveBeenCalledWith('Claim rejected');
    });

    it('settles the claim', () => {
      const fixture = create({ ...baseClaim, status: 'Approved' });
      claimsService.settleClaim.mockReturnValue(of({ message: 'ok' }));
      fixture.componentInstance.openModal('settle');

      fixture.componentInstance.onModalConfirm();

      expect(claimsService.settleClaim).toHaveBeenCalledWith('claim1');
      expect(toast.success).toHaveBeenCalledWith('Claim marked as settled');
    });

    it('assigns a surveyor with notes', () => {
      const fixture = create();
      claimsService.assignSurveyor.mockReturnValue(of({ message: 'ok' }));
      fixture.componentInstance.openModal('assignSurveyor');
      fixture.componentInstance.modalSurveyorId = 's1';
      fixture.componentInstance.modalNotes = 'urgent';

      fixture.componentInstance.onModalConfirm();

      expect(claimsService.assignSurveyor).toHaveBeenCalledWith('claim1', { surveyorId: 's1', notes: 'urgent' });
      expect(toast.success).toHaveBeenCalledWith('Surveyor assigned');
    });

    it('sends a document request', () => {
      const fixture = create();
      claimsService.requestDocs.mockReturnValue(of({ message: 'ok' }));
      fixture.componentInstance.openModal('requestDocs');
      fixture.componentInstance.modalDocs = 'medical bills';

      fixture.componentInstance.onModalConfirm();

      expect(claimsService.requestDocs).toHaveBeenCalledWith('claim1', 'medical bills');
      expect(toast.success).toHaveBeenCalledWith('Document request sent');
    });

    it('approves pre-authorisation', () => {
      const fixture = create({ ...baseClaim, isCashless: true, status: 'PreAuthRequested' });
      claimsService.approvePreAuth.mockReturnValue(of({ message: 'ok' }));
      fixture.componentInstance.openModal('preAuth');

      fixture.componentInstance.onModalConfirm();

      expect(claimsService.approvePreAuth).toHaveBeenCalledWith('claim1');
      expect(toast.success).toHaveBeenCalledWith('Pre-authorisation approved');
    });

    it('does nothing when the confirm action is disabled', () => {
      const fixture = create();
      fixture.componentInstance.openModal('reject'); // no reason set -> disabled
      fixture.componentInstance.onModalConfirm();
      expect(claimsService.approveReject).not.toHaveBeenCalled();
    });
  });

  describe('document preview helpers', () => {
    it('openPreview/closePreview toggle the previewed document', () => {
      const fixture = create();
      const doc = { documentName: 'x.pdf' } as SubmittedDocumentDto;
      fixture.componentInstance.openPreview(doc);
      expect(fixture.componentInstance.previewDoc()).toEqual(doc);
      fixture.componentInstance.closePreview();
      expect(fixture.componentInstance.previewDoc()).toBeNull();
    });

    it('isImage/isPdf detect file type by extension', () => {
      const fixture = create();
      expect(fixture.componentInstance.isImage({ documentName: 'photo.JPG' } as SubmittedDocumentDto)).toBe(true);
      expect(fixture.componentInstance.isImage({ documentName: 'doc.pdf' } as SubmittedDocumentDto)).toBe(false);
      expect(fixture.componentInstance.isPdf({ documentName: 'doc.PDF' } as SubmittedDocumentDto)).toBe(true);
    });

    it('docRawUrl prefixes the file path with a slash', () => {
      const fixture = create();
      expect(fixture.componentInstance.docRawUrl({ filePath: 'uploads/claims/x.pdf' } as SubmittedDocumentDto)).toBe('/uploads/claims/x.pdf');
    });
  });

  describe('getTypePillClass', () => {
    it('maps known claim types to a style class and falls back for unknown types', () => {
      const fixture = create();
      expect(fixture.componentInstance.getTypePillClass('Motor')).toBe('bg-info-bg text-info');
      expect(fixture.componentInstance.getTypePillClass('Unknown')).toBe('bg-surface text-muted');
    });
  });
});
