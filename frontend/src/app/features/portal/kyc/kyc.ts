import { Component, inject, signal, OnInit } from '@angular/core';
import { ProfileService } from '../profile/services/profile.service';
import { KycRecordDto } from '../../../core/models/api.models';
import { FileUploadComponent } from '../../../shared/components/file-upload/file-upload';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge';
import { ToastService } from '../../../shared/components/toast/toast.service';

@Component({
  selector: 'app-kyc',
  standalone: true,
  imports: [FileUploadComponent, StatusBadgeComponent],
  templateUrl: './kyc.html',
})
export class KycComponent implements OnInit {
  private profileService = inject(ProfileService);
  private toast = inject(ToastService);

  kyc = signal<KycRecordDto | null>(null);
  loading = signal(true);
  submitting = signal(false);

  aadhaarNum = '';
  panNum = '';
  aadhaarFile: File | null = null;
  panFile: File | null = null;

  ngOnInit(): void {
    this.profileService.getKyc().subscribe({
      next: k => { this.kyc.set(k); this.loading.set(false); },
      error: () => this.loading.set(false),
    });
  }

  uploadAadhaar(): void {
    if (!this.aadhaarFile || !this.aadhaarNum) return;
    this.submitting.set(true);
    this.profileService.uploadAadhaar(this.aadhaarFile, this.aadhaarNum).subscribe({
      next: k => { this.kyc.set(k); this.toast.success('Aadhaar uploaded successfully'); this.submitting.set(false); },
      error: () => { this.toast.error('Upload failed'); this.submitting.set(false); },
    });
  }

  uploadPan(): void {
    if (!this.panFile || !this.panNum) return;
    this.submitting.set(true);
    this.profileService.uploadPan(this.panFile, this.panNum).subscribe({
      next: k => { this.kyc.set(k); this.toast.success('PAN uploaded successfully'); this.submitting.set(false); },
      error: () => { this.toast.error('Upload failed'); this.submitting.set(false); },
    });
  }
}
