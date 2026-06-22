import { Component, inject, signal, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ProfileService } from './services/profile.service';
import { AuthService } from '../../../core/services/auth.service';
import { UserDto, FamilyMemberDto, KycRecordDto, AddressDto } from '../../../core/models/api.models';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge';
import { FileUploadComponent } from '../../../shared/components/file-upload/file-upload';
import { ConfirmDialogComponent } from '../../../shared/components/confirm-dialog/confirm-dialog';
import { ToastService } from '../../../shared/components/toast/toast.service';
import { DateFormatPipe } from '../../../shared/pipes/date-format.pipe';
import { postalCodeValidator, phoneValidator } from '../../../shared/validators/input.validators';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [ReactiveFormsModule, StatusBadgeComponent, FileUploadComponent, ConfirmDialogComponent, DateFormatPipe],
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
  deleteConfirm = signal<{ type: string; id: number } | null>(null);
  tabs = ['Personal Info', 'Family Members', 'KYC'];

  aadhaarNum = '';
  panNum = '';
  aadhaarFile: File | null = null;
  panFile: File | null = null;

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
    this.profileService.updateProfile(this.profileForm.getRawValue() as any).subscribe({
      next: () => this.toast.success('Profile updated'),
      error: () => this.toast.error('Update failed'),
    });
  }

  saveAddress(): void {
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
  deleteMember(id: number): void { this.deleteConfirm.set({ type: 'member', id }); }

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
    if (!this.aadhaarFile) return;
    this.profileService.uploadAadhaar(this.aadhaarFile, this.aadhaarNum).subscribe({
      next: k => { this.kyc.set(k); this.toast.success('Aadhaar uploaded'); },
      error: () => this.toast.error('Upload failed'),
    });
  }

  uploadPan(): void {
    if (!this.panFile) return;
    this.profileService.uploadPan(this.panFile, this.panNum).subscribe({
      next: k => { this.kyc.set(k); this.toast.success('PAN uploaded'); },
      error: () => this.toast.error('Upload failed'),
    });
  }
}
