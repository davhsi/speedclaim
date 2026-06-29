import { Component, inject, signal, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge';
import { ConfirmDialogComponent } from '../../../shared/components/confirm-dialog/confirm-dialog';
import { DateFormatPipe } from '../../../shared/pipes/date-format.pipe';
import { UnderwriterService, UnderwriterKycDto } from '../services/underwriter.service';
import { ToastService } from '../../../shared/components/toast/toast.service';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-uw-kyc-detail',
  standalone: true,
  imports: [StatusBadgeComponent, ConfirmDialogComponent, DateFormatPipe, FormsModule],
  templateUrl: './kyc-detail.html',
})
export class KycDetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private uwService = inject(UnderwriterService);
  private toast = inject(ToastService);

  kyc = signal<UnderwriterKycDto | null>(null);
  loading = signal(true);
  notFound = signal(false);
  revealed = signal(false);
  showDialog = signal<'approve' | 'reject' | null>(null);
  actionInFlight = signal(false);
  rejectReason = '';

  ngOnInit(): void {
    const userId = this.route.snapshot.paramMap.get('userId')!;
    this.uwService.getKycByUserId(userId).subscribe({
      next: (record) => {
        this.kyc.set(record);
        this.loading.set(false);
      },
      error: () => {
        this.notFound.set(true);
        this.loading.set(false);
      },
    });
  }

  maskAadhaar(num: string): string {
    if (num.length <= 4) return num;
    return 'X'.repeat(num.length - 4) + num.slice(-4);
  }

  maskPan(num: string): string {
    if (num.length <= 4) return num;
    return num.slice(0, 3) + '*'.repeat(num.length - 4) + num.slice(-1);
  }

  onApprove(): void {
    if (this.actionInFlight() || this.kyc()?.kycStatus !== 'Pending') return;
    const userId = this.kyc()!.userId;
    this.actionInFlight.set(true);
    this.uwService.reviewKyc(userId, true, 'Approved').subscribe({
      next: () => {
        this.toast.success('KYC approved.');
        this.showDialog.set(null);
        this.router.navigate(['/underwriter/kyc']);
      },
      error: () => {
        this.actionInFlight.set(false);
        this.toast.error('KYC approval failed.');
      },
    });
  }

  onReject(): void {
    if (this.actionInFlight() || this.kyc()?.kycStatus !== 'Pending' || !this.rejectReason.trim()) return;
    const userId = this.kyc()!.userId;
    this.actionInFlight.set(true);
    this.uwService.reviewKyc(userId, false, this.rejectReason).subscribe({
      next: () => {
        this.toast.error('KYC rejected.');
        this.showDialog.set(null);
        this.router.navigate(['/underwriter/kyc']);
      },
      error: () => {
        this.actionInFlight.set(false);
        this.toast.error('KYC rejection failed.');
      },
    });
  }

  closeDialog(): void {
    if (this.actionInFlight()) return;
    this.showDialog.set(null);
  }

  goBack(): void {
    this.router.navigate(['/underwriter/kyc']);
  }
}
