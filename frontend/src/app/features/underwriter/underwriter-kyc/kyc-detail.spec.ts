import { vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router, convertToParamMap } from '@angular/router';
import { of, throwError } from 'rxjs';
import { KycDetailComponent } from './kyc-detail';
import { UnderwriterService, UnderwriterKycDto } from '../services/underwriter.service';
import { ToastService } from '../../../shared/components/toast/toast.service';

describe('KycDetailComponent', () => {
  let uwService: { getKycByUserId: ReturnType<typeof vi.fn>; reviewKyc: ReturnType<typeof vi.fn> };
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
    uwService = { getKycByUserId: vi.fn(), reviewKyc: vi.fn() };
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

  describe('maskAadhaar / maskPan', () => {
    it('masks all but the last 4 digits of an Aadhaar number', () => {
      const fixture = create();
      expect(fixture.componentInstance.maskAadhaar('123456789012')).toBe('XXXXXXXX9012');
    });

    it('returns short Aadhaar numbers unmasked', () => {
      const fixture = create();
      expect(fixture.componentInstance.maskAadhaar('1234')).toBe('1234');
    });

    it('masks the middle of a PAN, keeping the first 3 and last char', () => {
      const fixture = create();
      expect(fixture.componentInstance.maskPan('ABCDE1234F')).toBe('ABC******F');
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
});
