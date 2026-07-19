import { Component, inject, signal, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge';
import { ConfirmDialogComponent } from '../../../shared/components/confirm-dialog/confirm-dialog';
import { DateFormatPipe } from '../../../shared/pipes/date-format.pipe';
import { UnderwriterService, UnderwriterKycDto, KycIdentityRevealDto } from '../services/underwriter.service';
import { ToastService } from '../../../shared/components/toast/toast.service';
import { FormsModule } from '@angular/forms';
import { DocumentPreviewComponent, PreviewDoc } from '../../../shared/components/document-preview/document-preview';
import { resolveBackendUrl } from '../../../core/config/backend-url.config';

@Component({
  selector: 'app-uw-kyc-detail',
  standalone: true,
  imports: [StatusBadgeComponent, ConfirmDialogComponent, DateFormatPipe, FormsModule, DocumentPreviewComponent],
  templateUrl: './kyc-detail.html',
})
export class KycDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly uwService = inject(UnderwriterService);
  private readonly toast = inject(ToastService);

  kyc = signal<UnderwriterKycDto | null>(null);
  loading = signal(true);
  notFound = signal(false);
  revealed = signal(false);
  revealedIdentity = signal<KycIdentityRevealDto | null>(null);
  revealing = signal(false);
  showDialog = signal<'approve' | 'reject' | null>(null);
  actionInFlight = signal(false);
  previewDoc = signal<PreviewDoc | null>(null);
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

  // The list/detail KYC endpoints only ever return the last-4-masked number (see
  // UserService.MapToKycDto). Revealing the full number is a separate, audited, on-demand
  // call — fetched once per visit and cached, not re-requested on every toggle.
  toggleReveal(): void {
    if (this.revealed()) {
      this.revealed.set(false);
      return;
    }
    if (this.revealedIdentity()) {
      this.revealed.set(true);
      return;
    }
    const userId = this.kyc()?.userId;
    if (!userId || this.revealing()) return;
    this.revealing.set(true);
    this.uwService.revealKycIdentity(userId).subscribe({
      next: identity => {
        this.revealedIdentity.set(identity);
        this.revealed.set(true);
        this.revealing.set(false);
      },
      error: () => {
        this.toast.error('Failed to reveal identity details.');
        this.revealing.set(false);
      },
    });
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

  openPreview(path: string, label: string): void {
    const relativeUploadPath = path.startsWith('/') ? path : `/${path}`;
    this.previewDoc.set({ url: resolveBackendUrl(relativeUploadPath), label });
  }
  closePreview(): void { this.previewDoc.set(null); }
}
