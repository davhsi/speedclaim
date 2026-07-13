import { Component, inject, signal, computed, OnInit, ViewChild, ElementRef } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ProfileService } from './services/profile.service';
import { AuthService } from '../../../core/services/auth.service';
import { UserDto, FamilyMemberDto, KycRecordDto, AddressDto, SingleAddressRequest } from '../../../core/models/api.models';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge';
import { ConfirmDialogComponent } from '../../../shared/components/confirm-dialog/confirm-dialog';
import { ToastService } from '../../../shared/components/toast/toast.service';
import { DateFormatPipe } from '../../../shared/pipes/date-format.pipe';
import { postalCodeValidator, phoneValidator } from '../../../shared/validators/input.validators';
import { AppSelectComponent } from '../../../shared/components/app-select/app-select';

const AADHAAR_PATTERN = /^\d{12}$/;
const PAN_PATTERN = /^[A-Z]{5}\d{4}[A-Z]$/;

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, StatusBadgeComponent, ConfirmDialogComponent, DateFormatPipe, AppSelectComponent],
  templateUrl: './profile.html',
})
export class ProfileComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly profileService = inject(ProfileService);
  private readonly authService = inject(AuthService);
  private readonly toast = inject(ToastService);

  profile = signal<UserDto | null>(null);
  familyMembers = signal<FamilyMemberDto[]>([]);
  kyc = signal<KycRecordDto | null>(null);
  activeTab = signal(0);
  showAddressForm = signal(false);
  showMemberForm = signal(false);
  deleteConfirm = signal<{ type: string; id: string } | null>(null);
  savingProfile = signal(false);
  savingAddress = signal(false);
  savingMember = signal(false);
  deleting = signal(false);
  tabs = ['Personal Info', 'Family Members', 'KYC'];
  maritalStatuses = ['Single', 'Married', 'Divorced', 'Widowed'];
  salutations = ['Mr', 'Mrs', 'Ms', 'Dr', 'Prof'];

  avatarPreview = signal<string | null>(null);
  avatarUploading = signal(false);
  @ViewChild('avatarInput') avatarInput!: ElementRef<HTMLInputElement>;

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

  kycApproved = computed(() => this.profile()?.kycApproved ?? false);
  kycStatus = computed(() => this.kyc()?.kycStatus ?? (this.kycApproved() ? 'Approved' : 'Pending'));
  kycMessage = computed(() => {
    switch (this.kycStatus()) {
      case 'Approved': return 'Identity verification is complete. Your verified profile fields are locked for security.';
      case 'Rejected': return this.kyc()?.rejectionReason || 'Your KYC was rejected. Please review and re-upload documents.';
      default: return this.kyc() ? 'Your documents are under review.' : 'Upload Aadhaar and PAN from the KYC page.';
    }
  });

  profileForm = this.fb.group({
    salutation: ['Mr'],
    firstName: ['', Validators.required],
    lastName: ['', Validators.required],
    email: [{ value: '', disabled: true }],
    phone: ['', [Validators.required, phoneValidator()]],
    maritalStatus: ['Single'],
    dateOfBirth: [''],
    occupation: [''],
    annualIncome: [''],
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
    firstName: ['', Validators.required],
    lastName: ['', Validators.required],
    dateOfBirth: ['', Validators.required],
    relationship: ['Spouse', Validators.required],
    gender: ['Male', Validators.required],
    salutation: ['Mr'],
  });

  userInitials(): string {
    const u = this.authService.currentUser();
    if (!u) return '?';
    return (u.firstName.charAt(0) + u.lastName.charAt(0)).toUpperCase();
  }

  kycDocumentStatus(uploaded?: boolean): string {
    if (this.kycStatus() === 'Approved') return 'Approved';
    return uploaded ? this.kycStatus() : 'NotUploaded';
  }

  avatarUrl(): string | null {
    return this.avatarPreview() ?? this.profile()?.avatarUrl ?? null;
  }

  onAvatarSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;

    const reader = new FileReader();
    reader.onload = () => this.avatarPreview.set(reader.result as string);
    reader.readAsDataURL(file);

    this.avatarUploading.set(true);
    this.profileService.uploadAvatar(file).subscribe({
      next: (res) => {
        this.avatarUploading.set(false);
        this.avatarPreview.set(null);
        this.profile.update(p => p ? { ...p, avatarUrl: res.avatarUrl } : p);
        this.authService.patchCurrentUser({ avatarUrl: res.avatarUrl });
        this.toast.success('Profile picture updated');
      },
      error: () => {
        this.avatarUploading.set(false);
        this.avatarPreview.set(null);
        this.toast.error('Upload failed');
      },
    });
  }

  ngOnInit(): void {
    this.profileService.getProfile().subscribe(p => {
      this.profile.set(p);
      this.profileForm.patchValue({
        ...p,
        dateOfBirth: p.dateOfBirth ?? '',
        occupation: p.occupation ?? '',
        annualIncome: p.annualIncome != null ? String(p.annualIncome) : '',
      });
      if (p.kycApproved) {
        this.profileForm.get('firstName')?.disable();
        this.profileForm.get('lastName')?.disable();
        this.profileForm.get('dateOfBirth')?.disable();
      }
    });
    this.profileService.getFamilyMembers().subscribe(m => this.familyMembers.set(m));
    this.profileService.getKyc().subscribe({ next: k => this.kyc.set(k), error: () => {} });
  }

  saveProfile(): void {
    if (this.savingProfile()) return;
    if (this.profileForm.invalid) {
      this.profileForm.markAllAsTouched();
      this.toast.warning('Please correct the highlighted fields before saving.');
      return;
    }
    const raw = this.profileForm.getRawValue();
    const annualIncome = raw.annualIncome ? Number(raw.annualIncome) : null;
    this.savingProfile.set(true);
    this.profileService.updateProfile({ ...raw, annualIncome } as any).subscribe({
      next: () => { this.savingProfile.set(false); this.toast.success('Profile updated'); },
      error: () => { this.savingProfile.set(false); this.toast.error('Update failed'); },
    });
  }

  saveAddress(): void {
    if (this.savingAddress()) return;
    if (this.addressForm.invalid) {
      this.addressForm.markAllAsTouched();
      this.toast.warning('Please correct the highlighted fields before saving.');
      return;
    }
    this.savingAddress.set(true);
    const raw = this.addressForm.getRawValue();
    const request: SingleAddressRequest = {
      addressType: raw.type as SingleAddressRequest['addressType'],
      addressLine1: raw.line1 ?? '',
      addressLine2: raw.line2 || undefined,
      city: raw.city ?? '',
      state: raw.state ?? '',
      postalCode: raw.postalCode ?? '',
      country: raw.country ?? '',
      isSameAsPermanent: raw.type === 'Permanent',
    };
    this.profileService.addAddress(request).subscribe({
      next: () => {
        this.savingAddress.set(false);
        this.toast.success('Address added');
        this.showAddressForm.set(false);
        this.profileService.getProfile().subscribe(p => this.profile.set(p));
      },
      error: () => { this.savingAddress.set(false); this.toast.error('Failed to add address'); },
    });
  }

  toggleAddressForm(): void {
    if (this.savingAddress()) return;
    this.showAddressForm.update(v => !v);
  }

  deleteAddr(addr: AddressDto): void { this.deleteConfirm.set({ type: 'address', id: addr.id }); }
  deleteMember(id: string): void { this.deleteConfirm.set({ type: 'member', id }); }

  confirmDelete(): void {
    if (this.deleting()) return;
    const d = this.deleteConfirm();
    if (!d) return;
    this.deleting.set(true);
    if (d.type === 'address') {
      this.profileService.deleteAddress(d.id).subscribe({
        next: () => {
          this.deleting.set(false);
          this.deleteConfirm.set(null);
          this.toast.success('Address deleted');
          this.profileService.getProfile().subscribe(p => this.profile.set(p));
        },
        error: () => { this.deleting.set(false); this.deleteConfirm.set(null); this.toast.error('Delete failed'); },
      });
    } else {
      this.profileService.deleteFamilyMember(d.id).subscribe({
        next: () => {
          this.deleting.set(false);
          this.deleteConfirm.set(null);
          this.toast.success('Member removed');
          this.familyMembers.update(m => m.filter(x => x.id !== d.id));
        },
        error: () => { this.deleting.set(false); this.deleteConfirm.set(null); this.toast.error('Delete failed'); },
      });
    }
  }

  saveMember(): void {
    if (this.savingMember()) return;
    if (this.memberForm.invalid) {
      this.memberForm.markAllAsTouched();
      this.toast.warning('Please correct the highlighted fields before saving.');
      return;
    }
    this.savingMember.set(true);
    this.profileService.addFamilyMember({ ...this.memberForm.getRawValue(), isDependent: true } as any).subscribe({
      next: member => {
        this.savingMember.set(false);
        this.familyMembers.update(m => [...m, member]);
        this.toast.success('Family member added');
        this.showMemberForm.set(false);
        this.memberForm.reset({ relationship: 'Spouse', gender: 'Male', salutation: 'Mr' });
      },
      error: () => { this.savingMember.set(false); this.toast.error('Failed to add member'); },
    });
  }

  toggleMemberForm(): void {
    if (this.savingMember()) return;
    this.showMemberForm.update(v => !v);
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
