import { Component, inject, signal, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge';
import { TimelineComponent, TimelineItem } from '../../../shared/components/timeline/timeline';
import { MoneyPipe } from '../../../shared/pipes/money.pipe';
import { DateFormatPipe } from '../../../shared/pipes/date-format.pipe';
import { ClaimsOfficerService, SurveyorDto } from '../services/claims-officer.service';
import { ClaimDto, ClaimStatusHistoryDto, SubmittedDocumentDto } from '../../../core/models/api.models';
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
  private sanitizer = inject(DomSanitizer);

  claim = signal<ClaimDto | null>(null);
  timelineItems = signal<TimelineItem[]>([]);
  surveyors = signal<SurveyorDto[]>([]);
  modalType = signal<ModalType>(null);
  actionInFlight = signal(false);
  previewDoc = signal<SubmittedDocumentDto | null>(null);

  modalAmount = '';
  modalNotes = '';
  modalReason = '';
  modalDocs = '';
  modalSurveyorId = '';

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) this.loadClaim(id);
    this.claimsService.getSurveyors().subscribe({
      next: surveyors => this.surveyors.set(surveyors),
    });
  }

  private loadClaim(id: string): void {
    this.claimsService.getClaimById(id).subscribe({
      next: (c) => this.claim.set(c),
    });
    this.claimsService.getClaimHistory(id).subscribe({
      next: (history) => {
        this.timelineItems.set(history.map(h => ({
          status: h.newStatus,
          date: h.changedAt,
          remarks: h.notes ?? undefined,
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
    return this.isAssignedToSelf() && (s === 'UnderReview' || s === 'PreAuthApproved');
  }

  canReject(): boolean {
    const s = this.claim()?.status;
    return this.isAssignedToSelf() && (s === 'UnderReview' || s === 'Intimated' || s === 'DocumentsPending' || s === 'PreAuthApproved');
  }

  canSettle(): boolean {
    return this.isAssignedToSelf() && this.claim()?.status === 'Approved';
  }

  isTerminal(): boolean {
    const s = this.claim()?.status;
    return s === 'Settled' || s === 'Rejected' || s === 'Withdrawn';
  }

  isActionLocked(): boolean {
    const s = this.claim()?.status;
    return s === 'Approved' || s === 'Settled' || s === 'Rejected' || s === 'Withdrawn';
  }

  canAssignSelf(): boolean {
    const c = this.claim();
    return !!c && !c.assignedOfficerId && !this.isActionLocked();
  }

  canAssignSurveyor(): boolean {
    const c = this.claim();
    return !!c && this.isAssignedToSelf() && ['Accident', 'Theft', 'NaturalDamage'].includes(c.claimType) && !c.surveyorId && (c.status === 'Intimated' || c.status === 'UnderReview');
  }

  canRequestDocs(): boolean {
    const s = this.claim()?.status;
    return this.isAssignedToSelf() && (s === 'Intimated' || s === 'UnderReview' || s === 'PreAuthApproved');
  }

  canApprovePreAuth(): boolean {
    const c = this.claim();
    return !!(c?.isCashless && this.isAssignedToSelf() && c.status === 'PreAuthRequested');
  }

  onAssignSelf(): void {
    const c = this.claim();
    if (!c || this.actionInFlight() || !this.canAssignSelf()) return;
    this.actionInFlight.set(true);
    this.claimsService.assignToSelf(c.id).subscribe({
      next: () => {
        this.showToast('Claim assigned to you', 'success');
        this.loadClaim(c.id);
        this.actionInFlight.set(false);
      },
      error: () => {
        this.actionInFlight.set(false);
        this.showToast('Failed to assign claim', 'error');
      },
    });
  }

  openModal(type: ModalType): void {
    if (this.actionInFlight()) return;
    this.modalAmount = '';
    this.modalNotes = '';
    this.modalReason = '';
    this.modalDocs = '';
    this.modalSurveyorId = '';
    this.modalType.set(type);
  }

  closeModal(): void {
    if (this.actionInFlight()) return;
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

  modalConfirmDisabled(): boolean {
    if (this.actionInFlight()) return true;

    switch (this.modalType()) {
      case 'approve':
        return !this.hasValidApprovedAmount(this.modalAmount);
      case 'reject':
        return this.modalReason.trim().length === 0;
      case 'assignSurveyor':
        return this.modalSurveyorId.trim().length === 0;
      case 'requestDocs':
        return this.modalDocs.trim().length === 0;
      default:
        return false;
    }
  }

  onModalConfirm(): void {
    const c = this.claim();
    if (!c || this.modalConfirmDisabled()) return;
    this.actionInFlight.set(true);

    switch (this.modalType()) {
      case 'approve':
        this.claimsService.approveReject(c.id, {
          isApproved: true,
          approvedAmount: Number(this.modalAmount) || undefined,
          reason: this.modalNotes,
        }).subscribe({
          next: () => { this.finishAction('Claim approved successfully', 'success'); this.closeModal(); this.loadClaim(c.id); },
          error: () => this.finishAction('Failed to approve claim', 'error'),
        });
        break;
      case 'reject':
        this.claimsService.approveReject(c.id, {
          isApproved: false,
          reason: this.modalReason,
        }).subscribe({
          next: () => { this.finishAction('Claim rejected', 'success'); this.closeModal(); this.loadClaim(c.id); },
          error: () => this.finishAction('Failed to reject claim', 'error'),
        });
        break;
      case 'settle':
        this.claimsService.settleClaim(c.id).subscribe({
          next: () => { this.finishAction('Claim marked as settled', 'success'); this.closeModal(); this.loadClaim(c.id); },
          error: () => this.finishAction('Failed to settle claim', 'error'),
        });
        break;
      case 'assignSurveyor':
        this.claimsService.assignSurveyor(c.id, {
          surveyorId: this.modalSurveyorId,
          notes: this.modalNotes || undefined,
        }).subscribe({
          next: () => { this.finishAction('Surveyor assigned', 'success'); this.closeModal(); this.loadClaim(c.id); },
          error: () => this.finishAction('Failed to assign surveyor', 'error'),
        });
        break;
      case 'requestDocs':
        this.claimsService.requestDocs(c.id, this.modalDocs.trim()).subscribe({
          next: () => { this.finishAction('Document request sent', 'success'); this.closeModal(); this.loadClaim(c.id); },
          error: () => this.finishAction('Failed to send request', 'error'),
        });
        break;
      case 'preAuth':
        this.claimsService.approvePreAuth(c.id).subscribe({
          next: () => { this.finishAction('Pre-authorisation approved', 'success'); this.closeModal(); this.loadClaim(c.id); },
          error: () => this.finishAction('Failed to approve pre-auth', 'error'),
        });
        break;
    }
  }

  private finishAction(message: string, type: ToastType): void {
    this.actionInFlight.set(false);
    this.showToast(message, type);
  }

  private hasValidApprovedAmount(value: string): boolean {
    const claim = this.claim();
    const amount = Number(value);
    return Number.isFinite(amount) && amount > 0 && (!claim || amount <= claim.claimAmountRequested);
  }

  private showToast(message: string, type: ToastType): void {
    this.toast[type](message);
  }

  openPreview(doc: SubmittedDocumentDto): void { this.previewDoc.set(doc); }
  closePreview(): void { this.previewDoc.set(null); }

  isImage(doc: SubmittedDocumentDto): boolean {
    const ext = doc.documentName?.split('.').pop()?.toLowerCase() ?? '';
    return ['jpg', 'jpeg', 'png', 'gif', 'webp', 'avif'].includes(ext);
  }

  isPdf(doc: SubmittedDocumentDto): boolean {
    return doc.documentName?.toLowerCase().endsWith('.pdf') ?? false;
  }

  docRawUrl(doc: SubmittedDocumentDto): string {
    return '/' + doc.filePath;
  }

  safePreviewUrl(doc: SubmittedDocumentDto): SafeResourceUrl {
    // filePath is server-generated (LocalStorageService writes uploads/<folder>/<guid>.<ext>
    // with an allowlisted extension) — never a raw user-supplied path or URL.
    return this.sanitizer.bypassSecurityTrustResourceUrl('/' + doc.filePath);
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
