import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { ProfileService } from './services/profile.service';
import { AuthService } from '../../../core/services/auth.service';
import { UserDto, FamilyMemberDto, KycRecordDto, AddressDto } from '../../../core/models/api.models';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge';
import { FileUploadComponent } from '../../../shared/components/file-upload/file-upload';
import { ConfirmDialogComponent } from '../../../shared/components/confirm-dialog/confirm-dialog';
import { ToastService } from '../../../shared/components/toast/toast.service';
import { DateFormatPipe } from '../../../shared/pipes/date-format.pipe';
import { postalCodeValidator, phoneValidator } from '../../../shared/validators/input.validators';

const AADHAAR_PATTERN = /^\d{12}$/;
const PAN_PATTERN = /^[A-Z]{5}[0-9]{4}[A-Z]$/;

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [FormsModule, ReactiveFormsModule, StatusBadgeComponent, FileUploadComponent, ConfirmDialogComponent, DateFormatPipe],
  templateUrl: './profile.html',
})
export class ProfileComponent implements OnInit {
  private fb = inject(FormBuilder);
  private profileService = inject(ProfileService);
  private authService = inject(AuthService);
  private toast = inject(ToastService);

  profile = signal<UserDto | null>(null);
  familyMembers = signal<FamilyMemberDto[]>([]);
  kyc = signal<KycRecordDto | null>(null);
  activeTab = signal(0);
  showAddressForm = signal(false);
  showMemberForm = signal(false);
  deleteConfirm = signal<{ type: string; id: string } | null>(null);
  tabs = ['Personal Info', 'Family Members', 'KYC'];

  aadhaarNum = signal('');
  panNum = signal('');
  aadhaarFile: File | null = null;
  panFile: File | null = null;

  aadhaarError = computed(() => {
    const v = this.aadhaarNum().trim();
    if (!v) return '';
    return AADHAAR_PATTERN.test(v) ? '' : 'Aadhaar must be exactly 12 digits.';
  });
  panError = computed(() => {
    const v = this.panNum().trim().toUpperCase();
    if (!v) return '';
    return PAN_PATTERN.test(v) ? '' : 'PAN must be in the format ABCDE1234F.';
  });
  aadhaarValid = computed(() => AADHAAR_PATTERN.test(this.aadhaarNum().trim()));
  panValid = computed(() => PAN_PATTERN.test(this.panNum().trim().toUpperCase()));

  profileForm = this.fb.group({
    firstName: ['', Validators.required],
    lastName: ['', Validators.required],
    email: [{ value: '', disabled: true }],
    phone: ['', [Validators.required, phoneValidator()]],
    maritalStatus: ['Single'],
  });

  addressForm = this.fb.group({
    line1: ['', Validators.required],
    line2: [''],
    city: ['', Validators.required],
    state: ['', Validators.required],
    postalCode: ['', [Validators.required, postalCodeValidator()]],
    country: ['India'],
    type: ['Permanent', Validators.required],
  });

  memberForm = this.fb.group({
    name: ['', Validators.required],
    dateOfBirth: ['', Validators.required],
    relationship: ['Spouse', Validators.required],
    gender: ['Male', Validators.required],
    salutationTitle: ['Mr'],
  });

  userInitials(): string {
    const u = this.authService.currentUser();
    if (!u) return '?';
    return (u.firstName.charAt(0) + u.lastName.charAt(0)).toUpperCase();
  }

  ngOnInit(): void {
    this.profileService.getProfile().subscribe(p => {
      this.profile.set(p);
      this.profileForm.patchValue(p);
    });
    this.profileService.getFamilyMembers().subscribe(m => this.familyMembers.set(m));
    this.profileService.getKyc().subscribe({ next: k => this.kyc.set(k), error: () => {} });
  }

  saveProfile(): void {
    if (this.profileForm.invalid) {
      this.profileForm.markAllAsTouched();
      this.toast.warning('Please correct the highlighted fields before saving.');
      return;
    }
    this.profileService.updateProfile(this.profileForm.getRawValue() as any).subscribe({
      next: () => this.toast.success('Profile updated'),
      error: () => this.toast.error('Update failed'),
    });
  }

  saveAddress(): void {
    if (this.addressForm.invalid) {
      this.addressForm.markAllAsTouched();
      this.toast.warning('Please correct the highlighted fields before saving.');
      return;
    }
    this.profileService.addAddress(this.addressForm.getRawValue() as any).subscribe({
      next: () => {
        this.toast.success('Address added');
        this.showAddressForm.set(false);
        this.profileService.getProfile().subscribe(p => this.profile.set(p));
      },
      error: () => this.toast.error('Failed to add address'),
    });
  }

  deleteAddr(addr: AddressDto): void { this.deleteConfirm.set({ type: 'address', id: addr.id }); }
  deleteMember(id: string): void { this.deleteConfirm.set({ type: 'member', id }); }

  confirmDelete(): void {
    const d = this.deleteConfirm();
    if (!d) return;
    if (d.type === 'address') {
      this.profileService.deleteAddress(d.id).subscribe({
        next: () => { this.toast.success('Address deleted'); this.profileService.getProfile().subscribe(p => this.profile.set(p)); },
        error: () => this.toast.error('Delete failed'),
      });
    } else {
      this.profileService.deleteFamilyMember(d.id).subscribe({
        next: () => { this.toast.success('Member removed'); this.familyMembers.update(m => m.filter(x => x.id !== d.id)); },
        error: () => this.toast.error('Delete failed'),
      });
    }
    this.deleteConfirm.set(null);
  }

  saveMember(): void {
    if (this.memberForm.invalid) {
      this.memberForm.markAllAsTouched();
      this.toast.warning('Please correct the highlighted fields before saving.');
      return;
    }
    this.profileService.addFamilyMember(this.memberForm.getRawValue() as any).subscribe({
      next: member => {
        this.familyMembers.update(m => [...m, member]);
        this.toast.success('Family member added');
        this.showMemberForm.set(false);
        this.memberForm.reset({ relationship: 'Spouse', gender: 'Male', salutationTitle: 'Mr' });
      },
      error: () => this.toast.error('Failed to add member'),
    });
  }

  uploadAadhaar(): void {
    if (!this.aadhaarFile || !this.aadhaarValid()) return;
    this.profileService.uploadAadhaar(this.aadhaarFile, this.aadhaarNum().trim()).subscribe({
      next: k => { this.kyc.set(k); this.toast.success('Aadhaar uploaded'); },
      error: () => this.toast.error('Upload failed'),
    });
  }

  uploadPan(): void {
    if (!this.panFile || !this.panValid()) return;
    this.profileService.uploadPan(this.panFile, this.panNum().trim().toUpperCase()).subscribe({
      next: k => { this.kyc.set(k); this.toast.success('PAN uploaded'); },
      error: () => this.toast.error('Upload failed'),
    });
  }
}
