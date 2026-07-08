import { vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router, convertToParamMap } from '@angular/router';
import { of, throwError } from 'rxjs';
import { KycDetailComponent } from './kyc-detail';
import { UnderwriterService, UnderwriterKycDto } from '../services/underwriter.service';
import { ToastService } from '../../../shared/components/toast/toast.service';

describe('KycDetailComponent', () => {
  let uwService: { getKycByUserId: ReturnType<typeof vi.fn>; reviewKyc: ReturnType<typeof vi.fn>; revealKycIdentity: ReturnType<typeof vi.fn> };
  let router: { navigate: ReturnType<typeof vi.fn> };
  let toast: { success: ReturnType<typeof vi.fn>; error: ReturnType<typeof vi.fn> };

  const kyc = (overrides: Partial<UnderwriterKycDto> = {}): UnderwriterKycDto => ({
    id: 'k1', userId: 'u1', kycStatus: 'Pending', aadhaarUploaded: true, panUploaded: true,
    createdAt: '2026-01-01', ...overrides,
  });

  function create(record: UnderwriterKycDto | null = kyc(), userId = 'u1') {
    if (record) uwService.getKycByUserId.mockReturnValue(of(record));
    else uwService.getKycByUserId.mockReturnValue(throwError(() => ({ status: 404 })));

    TestBed.configureTestingModule({
      imports: [KycDetailComponent],
      providers: [
        { provide: UnderwriterService, useValue: uwService },
        { provide: Router, useValue: router },
        { provide: ToastService, useValue: toast },
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: convertToParamMap({ userId }) } } },
      ],
    });
    const fixture = TestBed.createComponent(KycDetailComponent);
    fixture.detectChanges();
    return fixture;
  }

  beforeEach(() => {
    uwService = { getKycByUserId: vi.fn(), reviewKyc: vi.fn(), revealKycIdentity: vi.fn() };
    router = { navigate: vi.fn() };
    toast = { success: vi.fn(), error: vi.fn() };
  });

  describe('ngOnInit', () => {
    it('loads the KYC record for the routed userId', () => {
      const fixture = create(kyc(), 'u1');
      expect(uwService.getKycByUserId).toHaveBeenCalledWith('u1');
      expect(fixture.componentInstance.kyc()).toEqual(kyc());
      expect(fixture.componentInstance.loading()).toBe(false);
    });

    it('sets notFound when the fetch fails', () => {
      const fixture = create(null);
      expect(fixture.componentInstance.notFound()).toBe(true);
      expect(fixture.componentInstance.loading()).toBe(false);
    });
  });

  describe('toggleReveal', () => {
    it('fetches and shows the decrypted identity on first reveal', () => {
      const fixture = create();
      const c = fixture.componentInstance;
      uwService.revealKycIdentity.mockReturnValue(of({ aadhaarNumber: '123456789012', panNumber: 'ABCDE1234F' }));

      c.toggleReveal();

      expect(uwService.revealKycIdentity).toHaveBeenCalledWith('u1');
      expect(c.revealed()).toBe(true);
      expect(c.revealedIdentity()).toEqual({ aadhaarNumber: '123456789012', panNumber: 'ABCDE1234F' });
    });

    it('does not re-fetch on a second reveal — reuses the cached value', () => {
      const fixture = create();
      const c = fixture.componentInstance;
      uwService.revealKycIdentity.mockReturnValue(of({ aadhaarNumber: '123456789012', panNumber: 'ABCDE1234F' }));

      c.toggleReveal();
      c.toggleReveal(); // hide
      c.toggleReveal(); // reveal again

      expect(uwService.revealKycIdentity).toHaveBeenCalledTimes(1);
      expect(c.revealed()).toBe(true);
    });

    it('toggles hidden without calling the API', () => {
      const fixture = create();
      const c = fixture.componentInstance;
      uwService.revealKycIdentity.mockReturnValue(of({ aadhaarNumber: '123456789012', panNumber: 'ABCDE1234F' }));
      c.toggleReveal();

      c.toggleReveal();

      expect(c.revealed()).toBe(false);
    });

    it('shows an error toast and resets revealing on failure', () => {
      const fixture = create();
      const c = fixture.componentInstance;
      uwService.revealKycIdentity.mockReturnValue(throwError(() => ({ status: 500 })));

      c.toggleReveal();

      expect(toast.error).toHaveBeenCalledWith('Failed to reveal identity details.');
      expect(c.revealing()).toBe(false);
      expect(c.revealed()).toBe(false);
    });
  });

  describe('onApprove', () => {
    it('does nothing when the KYC is not Pending', () => {
      const fixture = create(kyc({ kycStatus: 'Approved' }));
      fixture.componentInstance.onApprove();
      expect(uwService.reviewKyc).not.toHaveBeenCalled();
    });

    it('approves the KYC and navigates back to the list on success', () => {
      const fixture = create(kyc({ kycStatus: 'Pending', userId: 'u1' }));
      uwService.reviewKyc.mockReturnValue(of(kyc({ kycStatus: 'Approved' })));

      fixture.componentInstance.onApprove();

      expect(uwService.reviewKyc).toHaveBeenCalledWith('u1', true, 'Approved');
      expect(toast.success).toHaveBeenCalledWith('KYC approved.');
      expect(router.navigate).toHaveBeenCalledWith(['/underwriter/kyc']);
    });

    it('shows an error toast and clears actionInFlight on failure', () => {
      const fixture = create(kyc({ kycStatus: 'Pending' }));
      uwService.reviewKyc.mockReturnValue(throwError(() => ({ status: 500 })));

      fixture.componentInstance.onApprove();

      expect(toast.error).toHaveBeenCalledWith('KYC approval failed.');
      expect(fixture.componentInstance.actionInFlight()).toBe(false);
    });

    it('does not act while an action is already in flight', () => {
      const fixture = create(kyc({ kycStatus: 'Pending' }));
      fixture.componentInstance.actionInFlight.set(true);
      fixture.componentInstance.onApprove();
      expect(uwService.reviewKyc).not.toHaveBeenCalled();
    });
  });

  describe('onReject', () => {
    it('does nothing without a reject reason', () => {
      const fixture = create(kyc({ kycStatus: 'Pending' }));
      fixture.componentInstance.rejectReason = '   ';
      fixture.componentInstance.onReject();
      expect(uwService.reviewKyc).not.toHaveBeenCalled();
    });

    it('does nothing when the KYC is not Pending', () => {
      const fixture = create(kyc({ kycStatus: 'Rejected' }));
      fixture.componentInstance.rejectReason = 'bad docs';
      fixture.componentInstance.onReject();
      expect(uwService.reviewKyc).not.toHaveBeenCalled();
    });

    it('rejects the KYC with the given reason and navigates back on success', () => {
      const fixture = create(kyc({ kycStatus: 'Pending', userId: 'u1' }));
      fixture.componentInstance.rejectReason = 'Blurry document';
      uwService.reviewKyc.mockReturnValue(of(kyc({ kycStatus: 'Rejected' })));

      fixture.componentInstance.onReject();

      expect(uwService.reviewKyc).toHaveBeenCalledWith('u1', false, 'Blurry document');
      expect(toast.error).toHaveBeenCalledWith('KYC rejected.');
      expect(router.navigate).toHaveBeenCalledWith(['/underwriter/kyc']);
    });

    it('shows an error toast on failure', () => {
      const fixture = create(kyc({ kycStatus: 'Pending' }));
      fixture.componentInstance.rejectReason = 'bad docs';
      uwService.reviewKyc.mockReturnValue(throwError(() => ({ status: 500 })));

      fixture.componentInstance.onReject();

      expect(toast.error).toHaveBeenCalledWith('KYC rejection failed.');
    });
  });

  describe('closeDialog / goBack', () => {
    it('closes the dialog when no action is in flight', () => {
      const fixture = create();
      fixture.componentInstance.showDialog.set('approve');
      fixture.componentInstance.closeDialog();
      expect(fixture.componentInstance.showDialog()).toBeNull();
    });

    it('does not close the dialog while an action is in flight', () => {
      const fixture = create();
      fixture.componentInstance.showDialog.set('approve');
      fixture.componentInstance.actionInFlight.set(true);
      fixture.componentInstance.closeDialog();
      expect(fixture.componentInstance.showDialog()).toBe('approve');
    });

    it('navigates back to the KYC list', () => {
      const fixture = create();
      fixture.componentInstance.goBack();
      expect(router.navigate).toHaveBeenCalledWith(['/underwriter/kyc']);
    });
  });

  describe('document preview', () => {
    it('openPreview/closePreview toggle the previewed document with a friendly label', () => {
      const fixture = create();
      fixture.componentInstance.openPreview('uploads/kyc/x.jpg', 'Aadhaar');
      expect(fixture.componentInstance.previewDoc()).toEqual({ url: '/uploads/kyc/x.jpg', label: 'Aadhaar' });
      fixture.componentInstance.closePreview();
      expect(fixture.componentInstance.previewDoc()).toBeNull();
    });
  });
});
