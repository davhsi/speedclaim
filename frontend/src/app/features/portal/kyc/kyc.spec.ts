import { vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { of, throwError, Subject } from 'rxjs';
import { KycComponent } from './kyc';
import { ProfileService } from '../profile/services/profile.service';
import { ToastService } from '../../../shared/components/toast/toast.service';
import { KycRecordDto } from '../../../core/models/api.models';

describe('KycComponent', () => {
  let profileService: { getKyc: ReturnType<typeof vi.fn>; uploadAadhaar: ReturnType<typeof vi.fn>; uploadPan: ReturnType<typeof vi.fn> };
  let toast: { success: ReturnType<typeof vi.fn>; error: ReturnType<typeof vi.fn> };

  function create(kyc: KycRecordDto | null = null) {
    profileService.getKyc.mockReturnValue(kyc ? of(kyc) : throwError(() => ({ status: 404 })));
    TestBed.configureTestingModule({
      imports: [KycComponent],
      providers: [
        { provide: ProfileService, useValue: profileService },
        { provide: ToastService, useValue: toast },
      ],
    });
    const fixture = TestBed.createComponent(KycComponent);
    fixture.detectChanges();
    return fixture;
  }

  beforeEach(() => {
    profileService = { getKyc: vi.fn(), uploadAadhaar: vi.fn(), uploadPan: vi.fn() };
    toast = { success: vi.fn(), error: vi.fn() };
  });

  describe('ngOnInit', () => {
    it('loads the KYC record', () => {
      const kyc = { id: 'k1', kycStatus: 'Pending' } as KycRecordDto;
      const fixture = create(kyc);
      expect(fixture.componentInstance.kyc()).toEqual(kyc);
      expect(fixture.componentInstance.loading()).toBe(false);
    });

    it('stops loading without a record when the fetch fails (e.g. no KYC yet)', () => {
      const fixture = create(null);
      expect(fixture.componentInstance.kyc()).toBeNull();
      expect(fixture.componentInstance.loading()).toBe(false);
    });
  });

  describe('Aadhaar validation', () => {
    it('accepts exactly 12 digits', () => {
      const fixture = create();
      fixture.componentInstance.aadhaarNum.set('123456789012');
      expect(fixture.componentInstance.aadhaarError()).toBe('');
      expect(fixture.componentInstance.aadhaarValid()).toBe(true);
    });

    it('flags a malformed number', () => {
      const fixture = create();
      fixture.componentInstance.aadhaarNum.set('123');
      expect(fixture.componentInstance.aadhaarError()).toBe('Aadhaar must be exactly 12 digits.');
      expect(fixture.componentInstance.aadhaarValid()).toBe(false);
    });

    it('shows no error while the field is empty', () => {
      const fixture = create();
      expect(fixture.componentInstance.aadhaarError()).toBe('');
    });
  });

  describe('PAN validation', () => {
    it('accepts a well-formed PAN case-insensitively', () => {
      const fixture = create();
      fixture.componentInstance.panNum.set('abcde1234f');
      expect(fixture.componentInstance.panError()).toBe('');
      expect(fixture.componentInstance.panValid()).toBe(true);
    });

    it('flags a malformed PAN', () => {
      const fixture = create();
      fixture.componentInstance.panNum.set('123');
      expect(fixture.componentInstance.panError()).toBe('PAN must be in the format ABCDE1234F.');
    });
  });

  describe('canEditKyc', () => {
    it('is true when there is no KYC record yet', () => {
      const fixture = create(null);
      expect(fixture.componentInstance.canEditKyc()).toBe(true);
    });

    it('is false while Pending review', () => {
      const fixture = create({ id: 'k1', kycStatus: 'Pending' } as KycRecordDto);
      expect(fixture.componentInstance.canEditKyc()).toBe(false);
    });

    it('is false once Approved', () => {
      const fixture = create({ id: 'k1', kycStatus: 'Approved' } as KycRecordDto);
      expect(fixture.componentInstance.canEditKyc()).toBe(false);
    });

    it('is true once Rejected', () => {
      const fixture = create({ id: 'k1', kycStatus: 'Rejected' } as KycRecordDto);
      expect(fixture.componentInstance.canEditKyc()).toBe(true);
    });

    it('blocks uploadAadhaar/uploadPan while Pending, even with valid input', () => {
      const fixture = create({ id: 'k1', kycStatus: 'Pending' } as KycRecordDto);
      const c = fixture.componentInstance;
      c.aadhaarFile.set(new File(['x'], 'a.jpg'));
      c.aadhaarNum.set('123456789012');
      c.panFile.set(new File(['x'], 'p.jpg'));
      c.panNum.set('ABCDE1234F');

      c.uploadAadhaar();
      c.uploadPan();

      expect(profileService.uploadAadhaar).not.toHaveBeenCalled();
      expect(profileService.uploadPan).not.toHaveBeenCalled();
    });
  });

  describe('uploadAadhaar', () => {
    it('does nothing without a file', () => {
      const fixture = create();
      fixture.componentInstance.aadhaarNum.set('123456789012');
      fixture.componentInstance.uploadAadhaar();
      expect(profileService.uploadAadhaar).not.toHaveBeenCalled();
    });

    it('does nothing when the number is invalid, even with a file', () => {
      const fixture = create();
      fixture.componentInstance.aadhaarFile.set(new File(['x'], 'a.jpg'));
      fixture.componentInstance.aadhaarNum.set('123');
      fixture.componentInstance.uploadAadhaar();
      expect(profileService.uploadAadhaar).not.toHaveBeenCalled();
    });

    it('uploads a valid file+number, updates the KYC record, and resets submitting', () => {
      const fixture = create();
      const file = new File(['x'], 'a.jpg');
      fixture.componentInstance.aadhaarFile.set(file);
      fixture.componentInstance.aadhaarNum.set(' 123456789012 ');
      const updated = { id: 'k1', aadhaarUploaded: true } as KycRecordDto;
      profileService.uploadAadhaar.mockReturnValue(of(updated));

      fixture.componentInstance.uploadAadhaar();

      expect(profileService.uploadAadhaar).toHaveBeenCalledWith(file, '123456789012');
      expect(fixture.componentInstance.kyc()).toEqual(updated);
      expect(toast.success).toHaveBeenCalledWith('Aadhaar uploaded successfully');
      expect(fixture.componentInstance.submitting()).toBe(false);
    });

    it('shows an error toast and resets submitting when the upload fails', () => {
      const fixture = create();
      fixture.componentInstance.aadhaarFile.set(new File(['x'], 'a.jpg'));
      fixture.componentInstance.aadhaarNum.set('123456789012');
      profileService.uploadAadhaar.mockReturnValue(throwError(() => ({ status: 500 })));

      fixture.componentInstance.uploadAadhaar();

      expect(toast.error).toHaveBeenCalledWith('Upload failed');
      expect(fixture.componentInstance.submitting()).toBe(false);
    });

    it('sets submitting/uploadingAadhaar while in flight, blocks a duplicate call, and clears on success', () => {
      const fixture = create();
      const c = fixture.componentInstance;
      c.aadhaarFile.set(new File(['x'], 'a.jpg'));
      c.aadhaarNum.set('123456789012');
      const subject = new Subject<any>();
      profileService.uploadAadhaar.mockReturnValue(subject);

      c.uploadAadhaar();
      expect(c.submitting()).toBe(true);
      expect(c.uploadingAadhaar()).toBe(true);
      expect(c.uploadingPan()).toBe(false);

      c.uploadAadhaar();
      expect(profileService.uploadAadhaar).toHaveBeenCalledTimes(1);

      subject.next({ id: 'k1', aadhaarUploaded: true });
      subject.complete();

      expect(c.submitting()).toBe(false);
      expect(c.uploadingAadhaar()).toBe(false);
    });

    it('blocks uploadPan while an Aadhaar upload is already in flight', () => {
      const fixture = create();
      const c = fixture.componentInstance;
      c.aadhaarFile.set(new File(['x'], 'a.jpg'));
      c.aadhaarNum.set('123456789012');
      c.panFile.set(new File(['x'], 'p.jpg'));
      c.panNum.set('ABCDE1234F');
      const subject = new Subject<any>();
      profileService.uploadAadhaar.mockReturnValue(subject);

      c.uploadAadhaar();
      c.uploadPan();

      expect(profileService.uploadPan).not.toHaveBeenCalled();
    });
  });

  describe('uploadPan', () => {
    it('uppercases the number before uploading', () => {
      const fixture = create();
      const file = new File(['x'], 'p.jpg');
      fixture.componentInstance.panFile.set(file);
      fixture.componentInstance.panNum.set(' abcde1234f ');
      const updated = { id: 'k1', panUploaded: true } as KycRecordDto;
      profileService.uploadPan.mockReturnValue(of(updated));

      fixture.componentInstance.uploadPan();

      expect(profileService.uploadPan).toHaveBeenCalledWith(file, 'ABCDE1234F');
      expect(toast.success).toHaveBeenCalledWith('PAN uploaded successfully');
    });

    it('does nothing without a file or with an invalid number', () => {
      const fixture = create();
      fixture.componentInstance.panNum.set('ABCDE1234F');
      fixture.componentInstance.uploadPan();
      expect(profileService.uploadPan).not.toHaveBeenCalled();
    });

    it('sets submitting/uploadingPan while in flight, blocks a duplicate call, and clears on error', () => {
      const fixture = create();
      const c = fixture.componentInstance;
      c.panFile.set(new File(['x'], 'p.jpg'));
      c.panNum.set('ABCDE1234F');
      const subject = new Subject<any>();
      profileService.uploadPan.mockReturnValue(subject);

      c.uploadPan();
      expect(c.submitting()).toBe(true);
      expect(c.uploadingPan()).toBe(true);

      c.uploadPan();
      expect(profileService.uploadPan).toHaveBeenCalledTimes(1);

      subject.error({ status: 500 });

      expect(c.submitting()).toBe(false);
      expect(c.uploadingPan()).toBe(false);
      expect(toast.error).toHaveBeenCalledWith('Upload failed');
    });
  });

  describe('canDeactivate', () => {
    it('allows navigation when nothing has been entered', () => {
      const fixture = create();
      expect(fixture.componentInstance.canDeactivate()).toBe(true);
    });

    it('prompts for confirmation when an id number has been typed but not uploaded', () => {
      const fixture = create();
      fixture.componentInstance.aadhaarNum.set('123456789012');

      const result = fixture.componentInstance.canDeactivate();

      expect(fixture.componentInstance.showLeaveConfirm()).toBe(true);
      expect(result).not.toBe(true);
    });

    it('resolves true on confirmLeave and false on cancelLeave', async () => {
      const fixture = create();
      fixture.componentInstance.panFile.set(new File(['x'], 'p.jpg'));

      const result$ = fixture.componentInstance.canDeactivate();
      const resultPromise = new Promise(resolve => (result$ as any).subscribe(resolve));
      fixture.componentInstance.confirmLeave();

      expect(await resultPromise).toBe(true);
      expect(fixture.componentInstance.showLeaveConfirm()).toBe(false);
    });

    it('allows navigation without prompting right after a successful Aadhaar upload', () => {
      const fixture = create();
      const c = fixture.componentInstance;
      c.aadhaarFile.set(new File(['x'], 'a.jpg'));
      c.aadhaarNum.set('123456789012');
      profileService.uploadAadhaar.mockReturnValue(of({ id: 'k1', aadhaarUploaded: true } as KycRecordDto));

      c.uploadAadhaar();

      expect(c.canDeactivate()).toBe(true);
    });
  });
});
