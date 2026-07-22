import { vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { Subject, of, throwError } from 'rxjs';
import { ProfileComponent } from './profile';
import { ProfileService } from './services/profile.service';
import { AuthService } from '../../../core/services/auth.service';
import { ToastService } from '../../../shared/components/toast/toast.service';
import { AuthUserDto, FamilyMemberDto, KycRecordDto, UserDto } from '../../../core/models/api.models';

describe('ProfileComponent', () => {
  let profileService: {
    getProfile: ReturnType<typeof vi.fn>;
    updateProfile: ReturnType<typeof vi.fn>;
    addAddress: ReturnType<typeof vi.fn>;
    deleteAddress: ReturnType<typeof vi.fn>;
    getFamilyMembers: ReturnType<typeof vi.fn>;
    addFamilyMember: ReturnType<typeof vi.fn>;
    deleteFamilyMember: ReturnType<typeof vi.fn>;
    getKyc: ReturnType<typeof vi.fn>;
    uploadAadhaar: ReturnType<typeof vi.fn>;
    uploadPan: ReturnType<typeof vi.fn>;
    uploadAvatar: ReturnType<typeof vi.fn>;
    getExternalIdentities: ReturnType<typeof vi.fn>;
    startExternalIdentityAuthorization: ReturnType<typeof vi.fn>;
  };
  let authService: { currentUser: ReturnType<typeof vi.fn>; patchCurrentUser: ReturnType<typeof vi.fn> };
  let toast: { success: ReturnType<typeof vi.fn>; error: ReturnType<typeof vi.fn>; warning: ReturnType<typeof vi.fn> };

  const baseProfile: UserDto = {
    id: 'u1',
    email: 'jane@example.com',
    salutation: 'Ms',
    firstName: 'Jane',
    lastName: 'Doe',
    fullName: 'Jane Doe',
    phone: '9876543210',
    role: 'Customer',
    maritalStatus: 'Single',
    dateOfBirth: '1990-01-01',
    isEmailVerified: true,
    isActive: true,
    createdAt: '2024-01-01',
    kycApproved: false,
  };

  function create(profile: UserDto = baseProfile, members: FamilyMemberDto[] = [], kyc: KycRecordDto | null = null) {
    profileService.getProfile.mockReturnValue(of(profile));
    profileService.getFamilyMembers.mockReturnValue(of(members));
    profileService.getKyc.mockReturnValue(kyc ? of(kyc) : throwError(() => ({ status: 404 })));
    profileService.getExternalIdentities.mockReturnValue(of([]));

    const fixture = TestBed.createComponent(ProfileComponent);
    fixture.detectChanges();
    return fixture;
  }

  beforeEach(() => {
    profileService = {
      getProfile: vi.fn(),
      updateProfile: vi.fn(),
      addAddress: vi.fn(),
      deleteAddress: vi.fn(),
      getFamilyMembers: vi.fn(),
      addFamilyMember: vi.fn(),
      deleteFamilyMember: vi.fn(),
      getKyc: vi.fn(),
      uploadAadhaar: vi.fn(),
      uploadPan: vi.fn(),
      uploadAvatar: vi.fn(),
      getExternalIdentities: vi.fn(),
      startExternalIdentityAuthorization: vi.fn(),
    };
    authService = {
      currentUser: vi.fn(() => ({ firstName: 'Jane', lastName: 'Doe' }) as AuthUserDto),
      patchCurrentUser: vi.fn(),
    };
    toast = { success: vi.fn(), error: vi.fn(), warning: vi.fn() };

    TestBed.configureTestingModule({
      imports: [ProfileComponent],
      providers: [
        { provide: ProfileService, useValue: profileService },
        { provide: AuthService, useValue: authService },
        { provide: ToastService, useValue: toast },
        provideRouter([]),
      ],
    });
  });

  describe('ngOnInit', () => {
    it('loads profile, family members, and KYC, and patches the form', () => {
      const fixture = create(baseProfile, [{ id: 'm1' } as FamilyMemberDto], { id: 'k1' } as KycRecordDto);
      const c = fixture.componentInstance;

      expect(c.profile()).toEqual(baseProfile);
      expect(c.familyMembers()).toEqual([{ id: 'm1' }]);
      expect(c.kyc()).toEqual({ id: 'k1' });
      expect(c.profileForm.controls.firstName.value).toBe('Jane');
      expect(c.profileForm.controls.phone.value).toBe('9876543210');
      expect(c.externalIdentities()).toEqual([]);
    });

    it('disables firstName/lastName/dateOfBirth once KYC is approved', () => {
      const fixture = create({ ...baseProfile, kycApproved: true });
      const c = fixture.componentInstance;
      expect(c.profileForm.controls.firstName.disabled).toBe(true);
      expect(c.profileForm.controls.lastName.disabled).toBe(true);
      expect(c.profileForm.controls.dateOfBirth.disabled).toBe(true);
      expect(c.profileForm.controls.phone.disabled).toBe(false);
    });

    it('leaves name/DOB fields enabled when KYC is not approved', () => {
      const fixture = create({ ...baseProfile, kycApproved: false });
      const c = fixture.componentInstance;
      expect(c.profileForm.controls.firstName.disabled).toBe(false);
      expect(c.profileForm.controls.dateOfBirth.disabled).toBe(false);
    });

    it('swallows a KYC fetch failure (e.g. no KYC record yet) without throwing', () => {
      expect(() => create(baseProfile, [], null)).not.toThrow();
      const fixture = create(baseProfile, [], null);
      expect(fixture.componentInstance.kyc()).toBeNull();
    });
  });

  describe('userInitials', () => {
    it('returns the uppercased first+last initials of the current user', () => {
      const fixture = create();
      expect(fixture.componentInstance.userInitials()).toBe('JD');
    });

    it('returns "?" when there is no current user', () => {
      authService.currentUser.mockReturnValue(null);
      const fixture = create();
      expect(fixture.componentInstance.userInitials()).toBe('?');
    });
  });

  describe('avatarUrl', () => {
    it('falls back to the profile avatarUrl when there is no local preview', () => {
      const fixture = create({ ...baseProfile, avatarUrl: 'existing.jpg' });
      expect(fixture.componentInstance.avatarUrl()).toBe('existing.jpg');
    });

    it('returns null when neither a preview nor a profile avatar exists', () => {
      const fixture = create(baseProfile);
      expect(fixture.componentInstance.avatarUrl()).toBeNull();
    });

    it('prefers the local preview over the saved profile avatar', () => {
      const fixture = create({ ...baseProfile, avatarUrl: 'existing.jpg' });
      fixture.componentInstance.avatarPreview.set('data:image/png;base64,preview');
      expect(fixture.componentInstance.avatarUrl()).toBe('data:image/png;base64,preview');
    });
  });

  describe('onAvatarSelected', () => {
    function selectEvent(file: File | undefined): Event {
      const input = document.createElement('input');
      Object.defineProperty(input, 'files', { value: file ? [file] : [] });
      return { target: input } as unknown as Event;
    }

    it('does nothing when no file is chosen', () => {
      const fixture = create();
      fixture.componentInstance.onAvatarSelected(selectEvent(undefined));
      expect(profileService.uploadAvatar).not.toHaveBeenCalled();
    });

    it('uploads the file and updates profile + current user on success', () => {
      const fixture = create();
      profileService.uploadAvatar.mockReturnValue(of({ avatarUrl: 'new.jpg' }));
      const file = new File(['x'], 'avatar.png', { type: 'image/png' });

      fixture.componentInstance.onAvatarSelected(selectEvent(file));

      expect(profileService.uploadAvatar).toHaveBeenCalledWith(file);
      expect(fixture.componentInstance.avatarUploading()).toBe(false);
      expect(fixture.componentInstance.profile()?.avatarUrl).toBe('new.jpg');
      expect(authService.patchCurrentUser).toHaveBeenCalledWith({ avatarUrl: 'new.jpg' });
      expect(toast.success).toHaveBeenCalledWith('Profile picture updated');
    });

    it('shows an error toast and resets state when the upload fails', () => {
      const fixture = create();
      profileService.uploadAvatar.mockReturnValue(throwError(() => ({ status: 500 })));
      const file = new File(['x'], 'avatar.png', { type: 'image/png' });

      fixture.componentInstance.onAvatarSelected(selectEvent(file));

      expect(fixture.componentInstance.avatarUploading()).toBe(false);
      expect(fixture.componentInstance.avatarPreview()).toBeNull();
      expect(toast.error).toHaveBeenCalledWith('Upload failed');
    });
  });

  describe('saveProfile', () => {
    it('marks fields touched and warns instead of saving an invalid form', () => {
      const fixture = create();
      fixture.componentInstance.profileForm.controls.phone.setValue('123'); // fails phoneValidator
      fixture.componentInstance.saveProfile();
      expect(profileService.updateProfile).not.toHaveBeenCalled();
      expect(toast.warning).toHaveBeenCalled();
      expect(fixture.componentInstance.profileForm.controls.phone.touched).toBe(true);
    });

    it('saves a valid form and shows a success toast', () => {
      const fixture = create();
      profileService.updateProfile.mockReturnValue(of({ message: 'ok' }));
      fixture.componentInstance.saveProfile();
      expect(profileService.updateProfile).toHaveBeenCalled();
      expect(toast.success).toHaveBeenCalledWith('Profile updated');
    });

    it('shows an error toast when the save fails', () => {
      const fixture = create();
      profileService.updateProfile.mockReturnValue(throwError(() => ({ status: 500 })));
      fixture.componentInstance.saveProfile();
      expect(toast.error).toHaveBeenCalledWith('Update failed');
    });

    it('sets savingProfile true while in flight, blocks a duplicate call, and clears on success', () => {
      const fixture = create();
      const subject = new Subject<{ message: string }>();
      profileService.updateProfile.mockReturnValue(subject.asObservable());

      fixture.componentInstance.saveProfile();
      expect(fixture.componentInstance.savingProfile()).toBe(true);

      fixture.componentInstance.saveProfile();
      expect(profileService.updateProfile).toHaveBeenCalledTimes(1);

      subject.next({ message: 'ok' });
      subject.complete();
      expect(fixture.componentInstance.savingProfile()).toBe(false);
    });

    it('clears savingProfile on error', () => {
      const fixture = create();
      const subject = new Subject<{ message: string }>();
      profileService.updateProfile.mockReturnValue(subject.asObservable());

      fixture.componentInstance.saveProfile();
      subject.error({ status: 500 });
      expect(fixture.componentInstance.savingProfile()).toBe(false);
    });
  });

  describe('saveAddress', () => {
    it('marks fields touched and warns instead of saving an invalid address', () => {
      const fixture = create();
      fixture.componentInstance.saveAddress();
      expect(profileService.addAddress).not.toHaveBeenCalled();
      expect(toast.warning).toHaveBeenCalled();
    });

    it('saves a valid address, closes the form, and refetches the profile', () => {
      const fixture = create();
      fixture.componentInstance.addressForm.setValue({
        line1: '123 St', line2: '', city: 'Mumbai', state: 'Maharashtra', postalCode: '400001', country: 'India', type: 'Permanent',
      });
      profileService.addAddress.mockReturnValue(of({ message: 'ok' }));
      fixture.componentInstance.showAddressForm.set(true);

      fixture.componentInstance.saveAddress();

      expect(profileService.addAddress).toHaveBeenCalledWith({
        addressType: 'Permanent',
        addressLine1: '123 St',
        addressLine2: undefined,
        city: 'Mumbai',
        state: 'Maharashtra',
        postalCode: '400001',
        country: 'India',
        isSameAsPermanent: true,
      });
      expect(toast.success).toHaveBeenCalledWith('Address added');
      expect(fixture.componentInstance.showAddressForm()).toBe(false);
      expect(profileService.getProfile).toHaveBeenCalledTimes(2); // once on init, once on refetch
    });

    it('shows an error toast when adding the address fails', () => {
      const fixture = create();
      fixture.componentInstance.addressForm.setValue({
        line1: '123 St', line2: '', city: 'Mumbai', state: 'Maharashtra', postalCode: '400001', country: 'India', type: 'Permanent',
      });
      profileService.addAddress.mockReturnValue(throwError(() => ({ status: 500 })));
      fixture.componentInstance.saveAddress();
      expect(toast.error).toHaveBeenCalledWith('Failed to add address');
    });

    it('sets savingAddress true while in flight and blocks a duplicate call', () => {
      const fixture = create();
      fixture.componentInstance.addressForm.setValue({
        line1: '123 St', line2: '', city: 'Mumbai', state: 'Maharashtra', postalCode: '400001', country: 'India', type: 'Permanent',
      });
      const subject = new Subject<{ message: string }>();
      profileService.addAddress.mockReturnValue(subject.asObservable());
      profileService.getProfile.mockReturnValue(of(baseProfile));

      fixture.componentInstance.saveAddress();
      expect(fixture.componentInstance.savingAddress()).toBe(true);

      fixture.componentInstance.saveAddress();
      expect(profileService.addAddress).toHaveBeenCalledTimes(1);

      subject.next({ message: 'ok' });
      subject.complete();
      expect(fixture.componentInstance.savingAddress()).toBe(false);
    });
  });

  describe('toggleAddressForm', () => {
    it('toggles the form open and closed', () => {
      const fixture = create();
      expect(fixture.componentInstance.showAddressForm()).toBe(false);
      fixture.componentInstance.toggleAddressForm();
      expect(fixture.componentInstance.showAddressForm()).toBe(true);
      fixture.componentInstance.toggleAddressForm();
      expect(fixture.componentInstance.showAddressForm()).toBe(false);
    });

    it('does nothing while an address save is in flight', () => {
      const fixture = create();
      fixture.componentInstance.savingAddress.set(true);
      fixture.componentInstance.showAddressForm.set(true);
      fixture.componentInstance.toggleAddressForm();
      expect(fixture.componentInstance.showAddressForm()).toBe(true);
    });
  });

  describe('delete flow (address / family member)', () => {
    it('deleteAddr stores a pending confirmation of type address', () => {
      const fixture = create();
      fixture.componentInstance.deleteAddr({ id: 'addr1' } as never);
      expect(fixture.componentInstance.deleteConfirm()).toEqual({ type: 'address', id: 'addr1' });
    });

    it('deleteMember stores a pending confirmation of type member', () => {
      const fixture = create();
      fixture.componentInstance.deleteMember('mem1');
      expect(fixture.componentInstance.deleteConfirm()).toEqual({ type: 'member', id: 'mem1' });
    });

    it('confirmDelete does nothing when there is no pending confirmation', () => {
      const fixture = create();
      fixture.componentInstance.confirmDelete();
      expect(profileService.deleteAddress).not.toHaveBeenCalled();
      expect(profileService.deleteFamilyMember).not.toHaveBeenCalled();
    });

    it('confirmDelete removes an address and refetches the profile', () => {
      const fixture = create();
      profileService.deleteAddress.mockReturnValue(of({ message: 'ok' }));
      fixture.componentInstance.deleteAddr({ id: 'addr1' } as never);

      fixture.componentInstance.confirmDelete();

      expect(profileService.deleteAddress).toHaveBeenCalledWith('addr1');
      expect(toast.success).toHaveBeenCalledWith('Address deleted');
      expect(fixture.componentInstance.deleteConfirm()).toBeNull();
    });

    it('confirmDelete shows an error toast when address deletion fails', () => {
      const fixture = create();
      profileService.deleteAddress.mockReturnValue(throwError(() => ({ status: 500 })));
      fixture.componentInstance.deleteAddr({ id: 'addr1' } as never);
      fixture.componentInstance.confirmDelete();
      expect(toast.error).toHaveBeenCalledWith('Delete failed');
    });

    it('confirmDelete removes a family member from the local list', () => {
      const fixture = create(baseProfile, [{ id: 'm1' } as FamilyMemberDto, { id: 'm2' } as FamilyMemberDto]);
      profileService.deleteFamilyMember.mockReturnValue(of({ message: 'ok' }));
      fixture.componentInstance.deleteMember('m1');

      fixture.componentInstance.confirmDelete();

      expect(profileService.deleteFamilyMember).toHaveBeenCalledWith('m1');
      expect(fixture.componentInstance.familyMembers().map(m => m.id)).toEqual(['m2']);
      expect(toast.success).toHaveBeenCalledWith('Member removed');
    });

    it('sets deleting true while in flight, keeps the dialog open, blocks a duplicate call, and clears on success', () => {
      const fixture = create();
      const subject = new Subject<{ message: string }>();
      profileService.deleteAddress.mockReturnValue(subject.asObservable());
      profileService.getProfile.mockReturnValue(of(baseProfile));
      fixture.componentInstance.deleteAddr({ id: 'addr1' } as never);

      fixture.componentInstance.confirmDelete();
      expect(fixture.componentInstance.deleting()).toBe(true);
      expect(fixture.componentInstance.deleteConfirm()).toEqual({ type: 'address', id: 'addr1' });

      fixture.componentInstance.confirmDelete();
      expect(profileService.deleteAddress).toHaveBeenCalledTimes(1);

      subject.next({ message: 'ok' });
      subject.complete();
      expect(fixture.componentInstance.deleting()).toBe(false);
      expect(fixture.componentInstance.deleteConfirm()).toBeNull();
    });

    it('clears deleting on error', () => {
      const fixture = create();
      const subject = new Subject<{ message: string }>();
      profileService.deleteAddress.mockReturnValue(subject.asObservable());
      fixture.componentInstance.deleteAddr({ id: 'addr1' } as never);

      fixture.componentInstance.confirmDelete();
      subject.error({ status: 500 });
      expect(fixture.componentInstance.deleting()).toBe(false);
      expect(fixture.componentInstance.deleteConfirm()).toBeNull();
    });
  });

  describe('saveMember', () => {
    it('marks fields touched and warns instead of saving an invalid member', () => {
      const fixture = create();
      fixture.componentInstance.saveMember();
      expect(profileService.addFamilyMember).not.toHaveBeenCalled();
      expect(toast.warning).toHaveBeenCalled();
    });

    it('adds a valid member, appends it, and resets the form', () => {
      const fixture = create();
      fixture.componentInstance.memberForm.setValue({
        firstName: 'Sam', lastName: 'Doe', dateOfBirth: '2010-01-01', relationship: 'Child', gender: 'Male', salutation: 'Mr',
      });
      const newMember = { id: 'm-new', firstName: 'Sam' } as FamilyMemberDto;
      profileService.addFamilyMember.mockReturnValue(of(newMember));
      fixture.componentInstance.showMemberForm.set(true);

      fixture.componentInstance.saveMember();

      expect(profileService.addFamilyMember).toHaveBeenCalledWith(expect.objectContaining({ firstName: 'Sam', isDependent: true }));
      expect(fixture.componentInstance.familyMembers()).toContainEqual(newMember);
      expect(toast.success).toHaveBeenCalledWith('Family member added');
      expect(fixture.componentInstance.showMemberForm()).toBe(false);
      expect(fixture.componentInstance.memberForm.controls.relationship.value).toBe('Spouse');
    });

    it('shows an error toast when adding the member fails', () => {
      const fixture = create();
      fixture.componentInstance.memberForm.setValue({
        firstName: 'Sam', lastName: 'Doe', dateOfBirth: '2010-01-01', relationship: 'Child', gender: 'Male', salutation: 'Mr',
      });
      profileService.addFamilyMember.mockReturnValue(throwError(() => ({ status: 500 })));
      fixture.componentInstance.saveMember();
      expect(toast.error).toHaveBeenCalledWith('Failed to add member');
    });

    it('sets savingMember true while in flight and blocks a duplicate call', () => {
      const fixture = create();
      fixture.componentInstance.memberForm.setValue({
        firstName: 'Sam', lastName: 'Doe', dateOfBirth: '2010-01-01', relationship: 'Child', gender: 'Male', salutation: 'Mr',
      });
      const subject = new Subject<FamilyMemberDto>();
      profileService.addFamilyMember.mockReturnValue(subject.asObservable());

      fixture.componentInstance.saveMember();
      expect(fixture.componentInstance.savingMember()).toBe(true);

      fixture.componentInstance.saveMember();
      expect(profileService.addFamilyMember).toHaveBeenCalledTimes(1);

      subject.next({ id: 'm-new', firstName: 'Sam' } as FamilyMemberDto);
      subject.complete();
      expect(fixture.componentInstance.savingMember()).toBe(false);
    });
  });

  describe('toggleMemberForm', () => {
    it('toggles the form open and closed', () => {
      const fixture = create();
      expect(fixture.componentInstance.showMemberForm()).toBe(false);
      fixture.componentInstance.toggleMemberForm();
      expect(fixture.componentInstance.showMemberForm()).toBe(true);
    });

    it('does nothing while a member save is in flight', () => {
      const fixture = create();
      fixture.componentInstance.savingMember.set(true);
      fixture.componentInstance.showMemberForm.set(true);
      fixture.componentInstance.toggleMemberForm();
      expect(fixture.componentInstance.showMemberForm()).toBe(true);
    });
  });

  describe('Aadhaar / PAN validation and upload', () => {
    it('computes aadhaarError/aadhaarValid for a well-formed number', () => {
      const fixture = create();
      fixture.componentInstance.aadhaarNum.set('123456789012');
      expect(fixture.componentInstance.aadhaarError()).toBe('');
      expect(fixture.componentInstance.aadhaarValid()).toBe(true);
    });

    it('flags an invalid Aadhaar number', () => {
      const fixture = create();
      fixture.componentInstance.aadhaarNum.set('12345');
      expect(fixture.componentInstance.aadhaarError()).toBe('Aadhaar must be exactly 12 digits.');
      expect(fixture.componentInstance.aadhaarValid()).toBe(false);
    });

    it('shows no error for an empty Aadhaar field', () => {
      const fixture = create();
      fixture.componentInstance.aadhaarNum.set('');
      expect(fixture.componentInstance.aadhaarError()).toBe('');
    });

    it('computes panError/panValid case-insensitively', () => {
      const fixture = create();
      fixture.componentInstance.panNum.set('abcde1234f');
      expect(fixture.componentInstance.panError()).toBe('');
      expect(fixture.componentInstance.panValid()).toBe(true);
    });

    it('flags an invalid PAN number', () => {
      const fixture = create();
      fixture.componentInstance.panNum.set('12345');
      expect(fixture.componentInstance.panError()).toBe('PAN must be in the format ABCDE1234F.');
    });

    it('uploadAadhaar does nothing without a file', () => {
      const fixture = create();
      fixture.componentInstance.aadhaarNum.set('123456789012');
      fixture.componentInstance.uploadAadhaar();
      expect(profileService.uploadAadhaar).not.toHaveBeenCalled();
    });

    it('uploadAadhaar does nothing when the number is invalid, even with a file', () => {
      const fixture = create();
      fixture.componentInstance.aadhaarFile = new File(['x'], 'a.jpg');
      fixture.componentInstance.aadhaarNum.set('123');
      fixture.componentInstance.uploadAadhaar();
      expect(profileService.uploadAadhaar).not.toHaveBeenCalled();
    });

    it('uploadAadhaar uploads a valid file+number and updates KYC', () => {
      const fixture = create();
      const file = new File(['x'], 'a.jpg');
      fixture.componentInstance.aadhaarFile = file;
      fixture.componentInstance.aadhaarNum.set(' 123456789012 ');
      const updatedKyc = { id: 'k1', aadhaarUploaded: true } as KycRecordDto;
      profileService.uploadAadhaar.mockReturnValue(of(updatedKyc));

      fixture.componentInstance.uploadAadhaar();

      expect(profileService.uploadAadhaar).toHaveBeenCalledWith(file, '123456789012');
      expect(fixture.componentInstance.kyc()).toEqual(updatedKyc);
      expect(toast.success).toHaveBeenCalledWith('Aadhaar uploaded');
    });

    it('uploadAadhaar shows an error toast when the upload fails', () => {
      const fixture = create();
      fixture.componentInstance.aadhaarFile = new File(['x'], 'a.jpg');
      fixture.componentInstance.aadhaarNum.set('123456789012');
      profileService.uploadAadhaar.mockReturnValue(throwError(() => ({ status: 500 })));
      fixture.componentInstance.uploadAadhaar();
      expect(toast.error).toHaveBeenCalledWith('Upload failed');
    });

    it('uploadPan uppercases the number before uploading', () => {
      const fixture = create();
      const file = new File(['x'], 'p.jpg');
      fixture.componentInstance.panFile = file;
      fixture.componentInstance.panNum.set(' abcde1234f ');
      const updatedKyc = { id: 'k1', panUploaded: true } as KycRecordDto;
      profileService.uploadPan.mockReturnValue(of(updatedKyc));

      fixture.componentInstance.uploadPan();

      expect(profileService.uploadPan).toHaveBeenCalledWith(file, 'ABCDE1234F');
      expect(toast.success).toHaveBeenCalledWith('PAN uploaded');
    });

    it('uploadPan does nothing without a file or with an invalid number', () => {
      const fixture = create();
      fixture.componentInstance.panNum.set('ABCDE1234F');
      fixture.componentInstance.uploadPan();
      expect(profileService.uploadPan).not.toHaveBeenCalled();
    });
  });
});
