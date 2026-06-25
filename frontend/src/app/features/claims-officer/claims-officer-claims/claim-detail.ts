import { Component, inject, signal, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge';
import { TimelineComponent, TimelineItem } from '../../../shared/components/timeline/timeline';
import { MoneyPipe } from '../../../shared/pipes/money.pipe';
import { DateFormatPipe } from '../../../shared/pipes/date-format.pipe';
import { ClaimsOfficerService } from '../services/claims-officer.service';
import { ClaimDto, ClaimStatusHistoryDto } from '../../../core/models/api.models';
import { AuthService } from '../../../core/services/auth.service';
import { ToastService } from '../../../shared/components/toast/toast.service';

type ModalType = 'approve' | 'reject' | 'settle' | 'assignSurveyor' | 'requestDocs' | 'preAuth' | null;
type ToastType = 'success' | 'error' | 'warning' | 'info';

@Component({
  selector: 'app-claim-detail',
  standalone: true,
  imports: [FormsModule, StatusBadgeComponent, TimelineComponent, MoneyPipe, DateFormatPipe],
  templateUrl: './claim-detail.html',
})
export class ClaimDetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private claimsService = inject(ClaimsOfficerService);
  private authService = inject(AuthService);
  private toast = inject(ToastService);

  claim = signal<ClaimDto | null>(null);
  timelineItems = signal<TimelineItem[]>([]);
  modalType = signal<ModalType>(null);

  modalAmount = '';
  modalNotes = '';
  modalReason = '';
  modalDocs = '';
  modalSurveyorId = '';

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) this.loadClaim(id);
  }

  private loadClaim(id: string): void {
    this.claimsService.getClaimById(id).subscribe({
      next: (c) => this.claim.set(c),
    });
    this.claimsService.getClaimHistory(id).subscribe({
      next: (history) => {
        this.timelineItems.set(history.map(h => ({
          status: h.status,
          date: h.changedAt,
          remarks: h.remarks ?? undefined,
          changedBy: h.changedBy ?? undefined,
        })));
      },
    });
  }

  goBack(): void {
    this.router.navigate(['/claims-officer/claims']);
  }

  isAssignedToSelf(): boolean {
    const c = this.claim();
    const user = this.authService.currentUser();
    return !!(c?.assignedOfficerId && user && c.assignedOfficerId === user.id);
  }

  showDecisionCard(): boolean {
    return this.canApprove() || this.canReject() || this.canSettle() || this.isTerminal();
  }

  canApprove(): boolean {
    const s = this.claim()?.status;
    return s === 'UnderReview' || s === 'PreAuthApproved';
  }

  canReject(): boolean {
    const s = this.claim()?.status;
    return s === 'UnderReview' || s === 'Intimated' || s === 'DocumentsPending';
  }

  canSettle(): boolean {
    return this.claim()?.status === 'Approved';
  }

  isTerminal(): boolean {
    const s = this.claim()?.status;
    return s === 'Settled' || s === 'Rejected' || s === 'Withdrawn';
  }

  canAssignSurveyor(): boolean {
    const c = this.claim();
    return !c?.surveyorId && !this.isTerminal();
  }

  canRequestDocs(): boolean {
    return !this.isTerminal();
  }

  canApprovePreAuth(): boolean {
    const c = this.claim();
    return !!(c?.isCashless && c.status === 'PreAuthRequested');
  }

  onAssignSelf(): void {
    const c = this.claim();
    if (!c) return;
    this.claimsService.assignToSelf(c.id).subscribe({
      next: () => {
        this.showToast('Claim assigned to you', 'success');
        this.loadClaim(c.id);
      },
      error: () => this.showToast('Failed to assign claim', 'error'),
    });
  }

  openModal(type: ModalType): void {
    this.modalAmount = '';
    this.modalNotes = '';
    this.modalReason = '';
    this.modalDocs = '';
    this.modalSurveyorId = '';
    this.modalType.set(type);
  }

  closeModal(): void {
    this.modalType.set(null);
  }

  modalTitle(): string {
    const map: Record<string, string> = {
      approve: 'Approve claim', reject: 'Reject claim', settle: 'Mark as settled',
      assignSurveyor: 'Assign surveyor', requestDocs: 'Request documents', preAuth: 'Approve pre-authorisation',
    };
    return map[this.modalType() ?? ''] ?? '';
  }

  modalConfirmLabel(): string {
    const map: Record<string, string> = {
      approve: 'Approve', reject: 'Reject', settle: 'Confirm settlement',
      assignSurveyor: 'Assign', requestDocs: 'Send request', preAuth: 'Approve pre-auth',
    };
    return map[this.modalType() ?? ''] ?? 'Confirm';
  }

  modalConfirmBg(): string {
    const type = this.modalType();
    if (type === 'reject') return 'bg-danger';
    if (type === 'approve' || type === 'settle') return 'bg-success';
    return 'bg-primary';
  }

  onModalConfirm(): void {
    const c = this.claim();
    if (!c) return;

    switch (this.modalType()) {
      case 'approve':
        this.claimsService.approveReject(c.id, {
          isApproved: true,
          approvedAmount: Number(this.modalAmount) || undefined,
          reason: this.modalNotes,
        }).subscribe({
          next: () => { this.showToast('Claim approved successfully', 'success'); this.closeModal(); this.loadClaim(c.id); },
          error: () => this.showToast('Failed to approve claim', 'error'),
        });
        break;
      case 'reject':
        this.claimsService.approveReject(c.id, {
          isApproved: false,
          reason: this.modalReason,
        }).subscribe({
          next: () => { this.showToast('Claim rejected', 'success'); this.closeModal(); this.loadClaim(c.id); },
          error: () => this.showToast('Failed to reject claim', 'error'),
        });
        break;
      case 'settle':
        this.claimsService.settleClaim(c.id).subscribe({
          next: () => { this.showToast('Claim marked as settled', 'success'); this.closeModal(); this.loadClaim(c.id); },
          error: () => this.showToast('Failed to settle claim', 'error'),
        });
        break;
      case 'assignSurveyor':
        this.claimsService.assignSurveyor(c.id, {
          surveyorId: this.modalSurveyorId,
          notes: this.modalNotes || undefined,
        }).subscribe({
          next: () => { this.showToast('Surveyor assigned', 'success'); this.closeModal(); this.loadClaim(c.id); },
          error: () => this.showToast('Failed to assign surveyor', 'error'),
        });
        break;
      case 'requestDocs':
        this.claimsService.requestDocs(c.id, this.modalDocs).subscribe({
          next: () => { this.showToast('Document request sent', 'success'); this.closeModal(); },
          error: () => this.showToast('Failed to send request', 'error'),
        });
        break;
      case 'preAuth':
        this.claimsService.approvePreAuth(c.id).subscribe({
          next: () => { this.showToast('Pre-authorisation approved', 'success'); this.closeModal(); this.loadClaim(c.id); },
          error: () => this.showToast('Failed to approve pre-auth', 'error'),
        });
        break;
    }
  }

  private showToast(message: string, type: ToastType): void {
    this.toast[type](message);
  }

  getTypePillClass(type: string): string {
    const map: Record<string, string> = {
      Motor: 'bg-info-bg text-info', Health: 'bg-success-bg text-success',
      Life: 'bg-warning-bg text-warning', Accident: 'bg-info-bg text-info',
      Theft: 'bg-danger-bg text-danger', NaturalDamage: 'bg-warning-bg text-warning',
    };
    return map[type] ?? 'bg-surface text-muted';
  }
}
