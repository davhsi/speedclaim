import { Component, inject, signal, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge';
import { MoneyPipe } from '../../../shared/pipes/money.pipe';
import { DateFormatPipe } from '../../../shared/pipes/date-format.pipe';
import { UnderwriterService } from '../services/underwriter.service';
import { ProposalDto } from '../../../core/models/api.models';

@Component({
  selector: 'app-uw-proposal-list',
  standalone: true,
  imports: [StatusBadgeComponent, MoneyPipe, DateFormatPipe],
  templateUrl: './proposal-list.html',
})
export class ProposalListComponent implements OnInit {
  private uwService = inject(UnderwriterService);
  private router = inject(Router);

  proposals = signal<ProposalDto[]>([]);
  pendingCount = signal(0);

  ngOnInit(): void {
    this.uwService.getAllProposals().subscribe({
      next: (data) => {
        this.proposals.set(data);
        this.pendingCount.set(data.filter(p => p.status === 'Submitted' || p.status === 'UnderReview').length);
      },
    });
  }

  openProposal(id: number): void {
    this.router.navigate(['/underwriter/proposals', id]);
  }
}
