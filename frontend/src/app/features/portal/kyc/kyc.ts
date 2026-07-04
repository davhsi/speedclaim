import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ProfileService } from '../profile/services/profile.service';
import { KycRecordDto } from '../../../core/models/api.models';
import { FileUploadComponent } from '../../../shared/components/file-upload/file-upload';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge';
import { ToastService } from '../../../shared/components/toast/toast.service';

const AADHAAR_PATTERN = /^\d{12}$/;
const PAN_PATTERN = /^[A-Z]{5}\d{4}[A-Z]$/;

@Component({
  selector: 'app-kyc',
  standalone: true,
  imports: [FormsModule, FileUploadComponent, StatusBadgeComponent],
  templateUrl: './kyc.html',
})
export class KycComponent implements OnInit {
  private profileService = inject(ProfileService);
  private toast = inject(ToastService);

  kyc = signal<KycRecordDto | null>(null);
  loading = signal(true);
  submitting = signal(false);

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

  ngOnInit(): void {
    this.profileService.getKyc().subscribe({
      next: k => { this.kyc.set(k); this.loading.set(false); },
      error: () => this.loading.set(false),
    });
  }

  uploadAadhaar(): void {
    if (!this.aadhaarFile || !this.aadhaarValid()) return;
    this.submitting.set(true);
    this.profileService.uploadAadhaar(this.aadhaarFile, this.aadhaarNum().trim()).subscribe({
      next: k => { this.kyc.set(k); this.toast.success('Aadhaar uploaded successfully'); this.submitting.set(false); },
      error: () => { this.toast.error('Upload failed'); this.submitting.set(false); },
    });
  }

  uploadPan(): void {
    if (!this.panFile || !this.panValid()) return;
    this.submitting.set(true);
    this.profileService.uploadPan(this.panFile, this.panNum().trim().toUpperCase()).subscribe({
      next: k => { this.kyc.set(k); this.toast.success('PAN uploaded successfully'); this.submitting.set(false); },
      error: () => { this.toast.error('Upload failed'); this.submitting.set(false); },
    });
  }
}
