import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Observable, Subject } from 'rxjs';
import { ProfileService } from '../profile/services/profile.service';
import { KycRecordDto } from '../../../core/models/api.models';
import { FileUploadComponent } from '../../../shared/components/file-upload/file-upload';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge';
import { ConfirmDialogComponent } from '../../../shared/components/confirm-dialog/confirm-dialog';
import { ToastService } from '../../../shared/components/toast/toast.service';
import { CanComponentDeactivate } from '../../../core/guards/unsaved-changes.guard';

const AADHAAR_PATTERN = /^\d{12}$/;
const PAN_PATTERN = /^[A-Z]{5}\d{4}[A-Z]$/;

@Component({
  selector: 'app-kyc',
  standalone: true,
  imports: [FormsModule, FileUploadComponent, StatusBadgeComponent, ConfirmDialogComponent],
  templateUrl: './kyc.html',
})
export class KycComponent implements OnInit, CanComponentDeactivate {
  private readonly profileService = inject(ProfileService);
  private readonly toast = inject(ToastService);

  kyc = signal<KycRecordDto | null>(null);
  loading = signal(true);
  private uploadTarget = signal<'aadhaar' | 'pan' | null>(null);
  submitting = computed(() => this.uploadTarget() !== null);
  uploadingAadhaar = computed(() => this.uploadTarget() === 'aadhaar');
  uploadingPan = computed(() => this.uploadTarget() === 'pan');

  aadhaarNum = signal('');
  panNum = signal('');
  aadhaarFile = signal<File | null>(null);
  panFile = signal<File | null>(null);

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

  // Once submitted, KYC is locked while an Underwriter reviews it — only a Rejected
  // record (or no record at all) can be (re)uploaded.
  canEditKyc = computed(() => {
    const k = this.kyc();
    return !k || k.kycStatus === 'Rejected';
  });

  hasUnsavedInput = computed(
    () => !!this.aadhaarNum().trim() || !!this.panNum().trim() || !!this.aadhaarFile() || !!this.panFile(),
  );

  showLeaveConfirm = signal(false);
  private leaveSubject: Subject<boolean> | null = null;

  canDeactivate(): boolean | Observable<boolean> {
    if (!this.hasUnsavedInput()) return true;

    this.showLeaveConfirm.set(true);
    this.leaveSubject = new Subject<boolean>();
    return this.leaveSubject.asObservable();
  }

  confirmLeave(): void {
    this.showLeaveConfirm.set(false);
    this.leaveSubject?.next(true);
    this.leaveSubject?.complete();
    this.leaveSubject = null;
  }

  cancelLeave(): void {
    this.showLeaveConfirm.set(false);
    this.leaveSubject?.next(false);
    this.leaveSubject?.complete();
    this.leaveSubject = null;
  }

  ngOnInit(): void {
    this.profileService.getKyc().subscribe({
      next: k => { this.kyc.set(k); this.loading.set(false); },
      error: () => this.loading.set(false),
    });
  }

  uploadAadhaar(): void {
    if (this.submitting() || !this.canEditKyc() || !this.aadhaarFile() || !this.aadhaarValid()) return;
    this.uploadTarget.set('aadhaar');
    this.profileService.uploadAadhaar(this.aadhaarFile()!, this.aadhaarNum().trim()).subscribe({
      next: k => {
        this.kyc.set(k);
        this.toast.success('Aadhaar uploaded successfully');
        this.uploadTarget.set(null);
        this.aadhaarNum.set('');
        this.aadhaarFile.set(null);
      },
      error: () => { this.toast.error('Upload failed'); this.uploadTarget.set(null); },
    });
  }

  uploadPan(): void {
    if (this.submitting() || !this.canEditKyc() || !this.panFile() || !this.panValid()) return;
    this.uploadTarget.set('pan');
    this.profileService.uploadPan(this.panFile()!, this.panNum().trim().toUpperCase()).subscribe({
      next: k => {
        this.kyc.set(k);
        this.toast.success('PAN uploaded successfully');
        this.uploadTarget.set(null);
        this.panNum.set('');
        this.panFile.set(null);
      },
      error: () => { this.toast.error('Upload failed'); this.uploadTarget.set(null); },
    });
  }
}
