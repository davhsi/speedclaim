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
  private uwService = inject(UnderwriterService);
  private toast = inject(ToastService);

  endorsements = signal<EndorsementDto[]>([]);
  pendingCount = signal(0);
  loading = signal(true);
  rejectingEndorsement = signal<EndorsementDto | null>(null);
  rejectReason = '';
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
        this.pendingCount.set(res.data.filter(e => e.status === 'Pending').length);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
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

  onApprove(e: EndorsementDto): void {
    this.uwService.reviewEndorsement(e.id.toString(), { isApproved: true, reason: 'Approved' }).subscribe({
      next: () => {
        this.toast.success('Endorsement approved.');
        this.loadData();
      },
    });
  }

  openReject(e: EndorsementDto): void {
    this.rejectingEndorsement.set(e);
    this.rejectReason = '';
  }

  confirmReject(): void {
    if (!this.rejectReason.trim()) return;
    const e = this.rejectingEndorsement()!;
    this.uwService.reviewEndorsement(e.id.toString(), { isApproved: false, reason: this.rejectReason }).subscribe({
      next: () => {
        this.toast.error('Endorsement rejected.');
        this.rejectingEndorsement.set(null);
        this.loadData();
      },
    });
  }
}
