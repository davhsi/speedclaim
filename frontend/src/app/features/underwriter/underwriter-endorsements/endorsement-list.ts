import { Component, inject, signal, OnInit } from '@angular/core';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge';
import { UnderwriterService } from '../services/underwriter.service';
import { ToastService } from '../../../shared/components/toast/toast.service';
import { EndorsementDto } from '../../../core/models/api.models';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-uw-endorsement-list',
  standalone: true,
  imports: [StatusBadgeComponent, FormsModule],
  templateUrl: './endorsement-list.html',
})
export class EndorsementListComponent implements OnInit {
  private uwService = inject(UnderwriterService);
  private toast = inject(ToastService);

  endorsements = signal<EndorsementDto[]>([]);
  pendingCount = signal(0);
  rejectingEndorsement = signal<EndorsementDto | null>(null);
  rejectReason = '';

  ngOnInit(): void {
    this.loadData();
  }

  private loadData(): void {
    this.uwService.getPendingEndorsements(1, 50).subscribe({
      next: (res) => {
        this.endorsements.set(res.data);
        this.pendingCount.set(res.data.filter(e => e.status === 'Pending').length);
      },
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
