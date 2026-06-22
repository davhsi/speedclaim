import { Component, inject, signal, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { ProposalService } from '../services/proposal.service';
import { ProposalDto } from '../../../../core/models/api.models';
import { StatusBadgeComponent } from '../../../../shared/components/status-badge/status-badge';
import { EmptyStateComponent } from '../../../../shared/components/empty-state/empty-state';
import { MoneyPipe } from '../../../../shared/pipes/money.pipe';
import { DateFormatPipe } from '../../../../shared/pipes/date-format.pipe';

@Component({
  selector: 'app-proposal-list',
  standalone: true,
  imports: [StatusBadgeComponent, EmptyStateComponent, MoneyPipe, DateFormatPipe],
  templateUrl: './proposal-list.html',
})
export class ProposalListComponent implements OnInit {
  private proposalService = inject(ProposalService);
  router = inject(Router);

  proposals = signal<ProposalDto[]>([]);
  loading = signal(true);

  ngOnInit(): void {
    this.proposalService.getMyProposals().subscribe({
      next: data => { this.proposals.set(data); this.loading.set(false); },
      error: () => this.loading.set(false),
    });
  }
}
