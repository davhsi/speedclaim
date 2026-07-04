import { Component, inject, signal, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { ClaimService } from '../services/claim.service';
import { ClaimDto } from '../../../../core/models/api.models';
import { StatusBadgeComponent } from '../../../../shared/components/status-badge/status-badge';
import { TimelineComponent, TimelineItem } from '../../../../shared/components/timeline/timeline';
import { FileUploadComponent } from '../../../../shared/components/file-upload/file-upload';
import { ConfirmDialogComponent } from '../../../../shared/components/confirm-dialog/confirm-dialog';
import { MoneyPipe } from '../../../../shared/pipes/money.pipe';
import { DateFormatPipe } from '../../../../shared/pipes/date-format.pipe';
import { SafeHtmlPipe } from '../../../../shared/pipes/safe-html.pipe';
import { ToastService } from '../../../../shared/components/toast/toast.service';

@Component({
  selector: 'app-claim-detail',
  standalone: true,
  imports: [StatusBadgeComponent, TimelineComponent, FileUploadComponent, ConfirmDialogComponent, MoneyPipe, DateFormatPipe, SafeHtmlPipe],
  templateUrl: './claim-detail.html',
})
export class ClaimDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly claimService = inject(ClaimService);
  private readonly toast = inject(ToastService);
  router = inject(Router);

  claim = signal<ClaimDto | null>(null);
  timeline = signal<TimelineItem[]>([]);
  loading = signal(true);
  uploading = signal(false);
  showWithdrawDialog = signal(false);

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id')!;
    this.loadClaim(id);
  }

  private loadClaim(id: string): void {
    this.loading.set(true);
    this.claimService.getById(id).subscribe({
      next: c => { this.claim.set(c); this.loading.set(false); },
      error: () => this.loading.set(false),
    });
    this.claimService.getHistory(id).subscribe(h =>
      this.timeline.set(h.map(i => ({ status: i.newStatus, date: i.changedAt, remarks: i.notes }))),
    );
  }

  domainBgClass(): string {
    const d = this.claim()?.claimType?.toUpperCase();
    const map: Record<string, string> = { HEALTH: 'bg-success-bg', MOTOR: 'bg-info-bg', LIFE: 'bg-[#F3EEFF]', ACCIDENT: 'bg-warning-bg' };
    return map[d ?? ''] ?? 'bg-surface-alt';
  }

  domainIcon(): string {
    const d = this.claim()?.claimType?.toUpperCase();
    const map: Record<string, string> = {
      HEALTH: '<svg width="26" height="26" viewBox="0 0 24 24" fill="none" stroke="#1F9D6B" stroke-width="1.75"><path d="M22 12h-4l-3 9L9 3l-3 9H2"/></svg>',
      MOTOR: '<svg width="26" height="26" viewBox="0 0 24 24" fill="none" stroke="#2D7FF9" stroke-width="1.75"><circle cx="7" cy="17" r="2"/><circle cx="17" cy="17" r="2"/><path d="M5 17H3v-6l2-5h9l4 5h3v6h-2"/></svg>',
      LIFE: '<svg width="26" height="26" viewBox="0 0 24 24" fill="none" stroke="#7C3AED" stroke-width="1.75"><path d="M20.84 4.61a5.5 5.5 0 00-7.78 0L12 5.67l-1.06-1.06a5.5 5.5 0 00-7.78 7.78l1.06 1.06L12 21.23l7.78-7.78 1.06-1.06a5.5 5.5 0 000-7.78z"/></svg>',
      ACCIDENT: '<svg width="26" height="26" viewBox="0 0 24 24" fill="none" stroke="#D9920A" stroke-width="1.75"><path d="M10.29 3.86L1.82 18a2 2 0 001.71 3h16.94a2 2 0 001.71-3L13.71 3.86a2 2 0 00-3.42 0z"/><line x1="12" y1="9" x2="12" y2="13"/><line x1="12" y1="17" x2="12.01" y2="17"/></svg>',
    };
    return map[d ?? ''] ?? '<svg width="26" height="26" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75"><circle cx="12" cy="12" r="10"/></svg>';
  }

  uploadDoc(file: File): void {
    const c = this.claim();
    if (!c || this.uploading()) return;

    this.uploading.set(true);
    this.claimService.uploadDocument(c.id, this.documentKeyFor(file), file).subscribe({
      next: () => {
        this.toast.success('Document uploaded');
        this.uploading.set(false);
        this.loadClaim(c.id);
      },
      error: () => {
        this.uploading.set(false);
        this.toast.error('Upload failed');
      },
    });
  }

  confirmWithdraw(): void {
    const c = this.claim();
    if (!c) return;
    this.claimService.withdraw(c.id).subscribe({
      next: () => {
        this.toast.success('Claim withdrawn successfully');
        this.showWithdrawDialog.set(false);
        this.claim.update(cl => cl ? { ...cl, status: 'Withdrawn' as typeof cl.status } : cl);
      },
      error: () => {
        this.showWithdrawDialog.set(false);
        this.toast.error('Failed to withdraw claim');
      },
    });
  }

  private documentKeyFor(file: File): string {
    const baseName = file.name.replace(/\.[^/.]+$/, '');
    const key = baseName.replace(/[^a-zA-Z0-9_-]+/g, '_').replace(/^_+|_+$/g, '');
    return (key || 'SUPPORTING_DOCUMENT').slice(0, 100).toUpperCase();
  }
}
