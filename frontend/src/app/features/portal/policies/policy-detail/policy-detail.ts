import { Component, inject, signal, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { PolicyService } from '../services/policy.service';
import { PolicyDto, PolicyStatusHistoryDto, EndorsementDto, PolicyNomineeDto } from '../../../../core/models/api.models';
import { StatusBadgeComponent } from '../../../../shared/components/status-badge/status-badge';
import { TimelineComponent, TimelineItem } from '../../../../shared/components/timeline/timeline';
import { ConfirmDialogComponent } from '../../../../shared/components/confirm-dialog/confirm-dialog';
import { MoneyPipe } from '../../../../shared/pipes/money.pipe';
import { DateFormatPipe } from '../../../../shared/pipes/date-format.pipe';
import { ToastService } from '../../../../shared/components/toast/toast.service';

@Component({
  selector: 'app-policy-detail',
  standalone: true,
  imports: [StatusBadgeComponent, TimelineComponent, ConfirmDialogComponent, MoneyPipe, DateFormatPipe, ReactiveFormsModule],
  templateUrl: './policy-detail.html',
})
export class PolicyDetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private policyService = inject(PolicyService);
  private fb = inject(FormBuilder);
  private toast = inject(ToastService);
  router = inject(Router);

  policy = signal<PolicyDto | null>(null);
  nominees = signal<PolicyNomineeDto[]>([]);
  endorsements = signal<EndorsementDto[]>([]);
  historyItems = signal<TimelineItem[]>([]);
  loading = signal(true);
  activeTab = signal(0);
  showCancelDialog = signal(false);
  showEndorsementForm = signal(false);
  tabs = ['Overview', 'Nominees', 'Endorsements', 'History'];

  endorsementForm = this.fb.group({
    type: ['NomineeChange', Validators.required],
    description: ['', [Validators.required, Validators.minLength(10)]],
  });

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    this.policyService.getById(id).subscribe({
      next: p => { this.policy.set(p); this.loading.set(false); },
      error: () => this.loading.set(false),
    });
    this.policyService.getNominees(id).subscribe(n => this.nominees.set(n));
    this.policyService.getEndorsements(id).subscribe(e => this.endorsements.set(e));
    this.policyService.getHistory(id).subscribe(h =>
      this.historyItems.set(h.map(i => ({ status: i.status, date: i.changedAt, remarks: i.remarks, changedBy: i.changedBy }))),
    );
  }

  domainBgClass(): string {
    const d = this.policy()?.domain;
    const map: Record<string, string> = { Health: 'bg-success-bg', Motor: 'bg-info-bg', Life: 'bg-[#F3EEFF]' };
    return map[d ?? ''] ?? 'bg-surface-alt';
  }

  domainIcon(): string {
    const d = this.policy()?.domain;
    const map: Record<string, string> = {
      Health: '<svg width="26" height="26" viewBox="0 0 24 24" fill="none" stroke="#1F9D6B" stroke-width="1.75"><path d="M22 12h-4l-3 9L9 3l-3 9H2"/></svg>',
      Motor: '<svg width="26" height="26" viewBox="0 0 24 24" fill="none" stroke="#2D7FF9" stroke-width="1.75"><circle cx="7" cy="17" r="2"/><circle cx="17" cy="17" r="2"/><path d="M5 17H3v-6l2-5h9l4 5h3v6h-2"/></svg>',
      Life: '<svg width="26" height="26" viewBox="0 0 24 24" fill="none" stroke="#7C3AED" stroke-width="1.75"><path d="M20.84 4.61a5.5 5.5 0 00-7.78 0L12 5.67l-1.06-1.06a5.5 5.5 0 00-7.78 7.78l1.06 1.06L12 21.23l7.78-7.78 1.06-1.06a5.5 5.5 0 000-7.78z"/></svg>',
    };
    return map[d ?? ''] ?? '';
  }

  downloadCert(): void {
    this.policyService.downloadCertificate(this.policy()!.id).subscribe(blob => {
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `${this.policy()!.policyNumber}-certificate.txt`;
      a.click();
      URL.revokeObjectURL(url);
    });
  }

  submitEndorsement(): void {
    if (this.endorsementForm.invalid) return;
    this.policyService.requestEndorsement(this.policy()!.id, this.endorsementForm.getRawValue() as any).subscribe({
      next: () => {
        this.toast.success('Endorsement request submitted');
        this.showEndorsementForm.set(false);
        this.endorsementForm.reset({ type: 'NomineeChange' });
        this.policyService.getEndorsements(this.policy()!.id).subscribe(e => this.endorsements.set(e));
      },
      error: () => this.toast.error('Failed to submit endorsement'),
    });
  }

  confirmCancel(): void {
    this.policyService.cancelPolicy(this.policy()!.id).subscribe({
      next: () => {
        this.toast.success('Policy cancelled');
        this.showCancelDialog.set(false);
        this.policy.update(p => p ? { ...p, status: 'Cancelled' as any } : p);
      },
      error: () => this.toast.error('Cancellation failed'),
    });
  }
}
