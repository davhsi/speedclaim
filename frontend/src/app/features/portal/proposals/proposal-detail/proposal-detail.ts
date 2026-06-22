import { Component, inject, signal, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { ProposalService } from '../services/proposal.service';
import { ProposalDto } from '../../../../core/models/api.models';
import { StatusBadgeComponent } from '../../../../shared/components/status-badge/status-badge';
import { MoneyPipe } from '../../../../shared/pipes/money.pipe';
import { DateFormatPipe } from '../../../../shared/pipes/date-format.pipe';
import { FileUploadComponent } from '../../../../shared/components/file-upload/file-upload';
import { ToastService } from '../../../../shared/components/toast/toast.service';

@Component({
  selector: 'app-proposal-detail',
  standalone: true,
  imports: [StatusBadgeComponent, MoneyPipe, DateFormatPipe, FileUploadComponent],
  templateUrl: './proposal-detail.html',
})
export class ProposalDetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private proposalService = inject(ProposalService);
  private toast = inject(ToastService);
  router = inject(Router);

  proposal = signal<ProposalDto | null>(null);
  loading = signal(true);

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    this.proposalService.getById(id).subscribe({
      next: data => { this.proposal.set(data); this.loading.set(false); },
      error: () => this.loading.set(false),
    });
  }

  domainBgClass(): string {
    const d = this.proposal()?.domain;
    const map: Record<string, string> = { Health: 'bg-success-bg', Motor: 'bg-info-bg', Life: 'bg-[#F3EEFF]' };
    return map[d ?? ''] ?? 'bg-surface-alt';
  }

  domainIcon(): string {
    const d = this.proposal()?.domain;
    const map: Record<string, string> = {
      Health: '<svg width="26" height="26" viewBox="0 0 24 24" fill="none" stroke="#1F9D6B" stroke-width="1.75"><path d="M22 12h-4l-3 9L9 3l-3 9H2"/></svg>',
      Motor: '<svg width="26" height="26" viewBox="0 0 24 24" fill="none" stroke="#2D7FF9" stroke-width="1.75"><circle cx="7" cy="17" r="2"/><circle cx="17" cy="17" r="2"/><path d="M5 17H3v-6l2-5h9l4 5h3v6h-2"/></svg>',
      Life: '<svg width="26" height="26" viewBox="0 0 24 24" fill="none" stroke="#7C3AED" stroke-width="1.75"><path d="M20.84 4.61a5.5 5.5 0 00-7.78 0L12 5.67l-1.06-1.06a5.5 5.5 0 00-7.78 7.78l1.06 1.06L12 21.23l7.78-7.78 1.06-1.06a5.5 5.5 0 000-7.78z"/></svg>',
    };
    return map[d ?? ''] ?? '';
  }

  onDocUpload(file: File): void {
    const p = this.proposal();
    if (!p) return;
    this.proposalService.uploadDocument(p.id, file.name.split('.')[0], file).subscribe({
      next: () => this.toast.success('Document uploaded'),
      error: () => this.toast.error('Upload failed'),
    });
  }
}
