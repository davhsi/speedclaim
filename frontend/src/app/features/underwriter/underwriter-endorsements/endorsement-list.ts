import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge';
import { UnderwriterService } from '../services/underwriter.service';
import { ToastService } from '../../../shared/components/toast/toast.service';
import { EndorsementDto } from '../../../core/models/api.models';
import { FormsModule } from '@angular/forms';
import { PaginationComponent } from '../../../shared/components/pagination/pagination';
import { SkeletonLoaderComponent } from '../../../shared/components/skeleton-loader/skeleton-loader';

@Component({
  selector: 'app-uw-endorsement-list',
  standalone: true,
  imports: [StatusBadgeComponent, FormsModule, PaginationComponent, SkeletonLoaderComponent],
  templateUrl: './endorsement-list.html',
})
export class EndorsementListComponent implements OnInit {
  private readonly uwService = inject(UnderwriterService);
  private readonly toast = inject(ToastService);

  endorsements = signal<EndorsementDto[]>([]);
  pendingCount = signal(0);
  loading = signal(true);
  rejectingEndorsement = signal<EndorsementDto | null>(null);
  rejectReason = '';
  submittingIds = signal<Set<string>>(new Set());
  currentPage = signal(1);
  readonly pageSize = 10;

  totalPages = computed(() => Math.max(1, Math.ceil(this.endorsements().length / this.pageSize)));
  pagedEndorsements = computed(() => {
    const start = (this.currentPage() - 1) * this.pageSize;
    return this.endorsements().slice(start, start + this.pageSize);
  });

  onPageChange(page: number): void { this.currentPage.set(page); }

  ngOnInit(): void {
    this.loadData();
  }

  private loadData(): void {
    this.uwService.getPendingEndorsements(1, 50).subscribe({
      next: (res) => {
        this.endorsements.set(res.data);
        this.pendingCount.set(res.data.filter(e => this.isReviewable(e)).length);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  isReviewable(e: EndorsementDto): boolean {
    return e.status === 'Requested';
  }

  // Mirrors PolicyService.ApplyEndorsementChangeAsync — these two types are structurally
  // applied to the policy/user on approval; the free-text types are authorization-only.
  isAutoApplied(e: EndorsementDto): boolean {
    return e.endorsementType === 'SumAssuredChange' || e.endorsementType === 'ContactUpdate';
  }

  formatType(type: string): string {
    const map: Record<string, string> = {
      NomineeChange: 'Nominee change',
      AddressChange: 'Address change',
      VehicleCorrection: 'Vehicle correction',
      ContactUpdate: 'Contact update',
      SumAssuredChange: 'Sum assured change',
    };
    return map[type] ?? type;
  }

  isSubmitting(e: EndorsementDto): boolean {
    return this.submittingIds().has(e.id.toString());
  }

  private markSubmitting(id: string, active: boolean): void {
    this.submittingIds.update(ids => {
      const next = new Set(ids);
      if (active) next.add(id); else next.delete(id);
      return next;
    });
  }

  onApprove(e: EndorsementDto): void {
    if (this.isSubmitting(e)) return;
    const id = e.id.toString();
    this.markSubmitting(id, true);
    this.uwService.reviewEndorsement(id, { isApproved: true, reason: 'Approved' }).subscribe({
      next: () => {
        this.markSubmitting(id, false);
        this.toast.success('Endorsement approved.');
        this.loadData();
      },
      error: () => { this.markSubmitting(id, false); },
    });
  }

  openReject(e: EndorsementDto): void {
    this.rejectingEndorsement.set(e);
    this.rejectReason = '';
  }

  closeReject(): void {
    const e = this.rejectingEndorsement();
    if (e && this.isSubmitting(e)) return;
    this.rejectingEndorsement.set(null);
  }

  confirmReject(): void {
    const e = this.rejectingEndorsement();
    if (!e || this.isSubmitting(e) || !this.rejectReason.trim()) return;
    const id = e.id.toString();
    this.markSubmitting(id, true);
    this.uwService.reviewEndorsement(id, { isApproved: false, reason: this.rejectReason }).subscribe({
      next: () => {
        this.markSubmitting(id, false);
        this.toast.error('Endorsement rejected.');
        this.rejectingEndorsement.set(null);
        this.loadData();
      },
      error: () => { this.markSubmitting(id, false); },
    });
  }
}
