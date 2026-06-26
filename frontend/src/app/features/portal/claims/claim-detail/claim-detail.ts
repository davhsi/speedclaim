import { Component, inject, signal, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { ClaimService } from '../services/claim.service';
import { ClaimDto } from '../../../../core/models/api.models';
import { StatusBadgeComponent } from '../../../../shared/components/status-badge/status-badge';
import { TimelineComponent, TimelineItem } from '../../../../shared/components/timeline/timeline';
import { FileUploadComponent } from '../../../../shared/components/file-upload/file-upload';
import { MoneyPipe } from '../../../../shared/pipes/money.pipe';
import { DateFormatPipe } from '../../../../shared/pipes/date-format.pipe';
import { SafeHtmlPipe } from '../../../../shared/pipes/safe-html.pipe';
import { ToastService } from '../../../../shared/components/toast/toast.service';

@Component({
  selector: 'app-claim-detail',
  standalone: true,
  imports: [StatusBadgeComponent, TimelineComponent, FileUploadComponent, MoneyPipe, DateFormatPipe, SafeHtmlPipe],
  templateUrl: './claim-detail.html',
})
export class ClaimDetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private claimService = inject(ClaimService);
  private toast = inject(ToastService);
  router = inject(Router);

  claim = signal<ClaimDto | null>(null);
  timeline = signal<TimelineItem[]>([]);
  loading = signal(true);

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id')!;
    this.claimService.getById(id).subscribe({
      next: c => { this.claim.set(c); this.loading.set(false); },
      error: () => this.loading.set(false),
    });
    this.claimService.getHistory(id).subscribe(h =>
      this.timeline.set(h.map(i => ({ status: i.status, date: i.changedAt, remarks: i.remarks, changedBy: i.changedBy }))),
    );
  }

  domainBgClass(): string {
    const d = this.claim()?.claimType;
    const map: Record<string, string> = { Health: 'bg-success-bg', Motor: 'bg-info-bg', Life: 'bg-[#F3EEFF]', Accident: 'bg-warning-bg' };
    return map[d ?? ''] ?? 'bg-surface-alt';
  }

  domainIcon(): string {
    const d = this.claim()?.claimType;
    const map: Record<string, string> = {
      Health: '<svg width="26" height="26" viewBox="0 0 24 24" fill="none" stroke="#1F9D6B" stroke-width="1.75"><path d="M22 12h-4l-3 9L9 3l-3 9H2"/></svg>',
      Motor: '<svg width="26" height="26" viewBox="0 0 24 24" fill="none" stroke="#2D7FF9" stroke-width="1.75"><circle cx="7" cy="17" r="2"/><circle cx="17" cy="17" r="2"/><path d="M5 17H3v-6l2-5h9l4 5h3v6h-2"/></svg>',
      Life: '<svg width="26" height="26" viewBox="0 0 24 24" fill="none" stroke="#7C3AED" stroke-width="1.75"><path d="M20.84 4.61a5.5 5.5 0 00-7.78 0L12 5.67l-1.06-1.06a5.5 5.5 0 00-7.78 7.78l1.06 1.06L12 21.23l7.78-7.78 1.06-1.06a5.5 5.5 0 000-7.78z"/></svg>',
      Accident: '<svg width="26" height="26" viewBox="0 0 24 24" fill="none" stroke="#D9920A" stroke-width="1.75"><path d="M10.29 3.86L1.82 18a2 2 0 001.71 3h16.94a2 2 0 001.71-3L13.71 3.86a2 2 0 00-3.42 0z"/><line x1="12" y1="9" x2="12" y2="13"/><line x1="12" y1="17" x2="12.01" y2="17"/></svg>',
    };
    return map[d ?? ''] ?? '<svg width="26" height="26" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75"><circle cx="12" cy="12" r="10"/></svg>';
  }

  uploadDoc(file: File): void {
    const c = this.claim();
    if (!c) return;
    this.claimService.uploadDocument(c.id, file.name.split('.')[0], file).subscribe({
      next: () => this.toast.success('Document uploaded'),
      error: () => this.toast.error('Upload failed'),
    });
  }
}
