import { Component, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AgentService } from '../services/agent.service';
import { ProposalDto } from '../../../core/models/api.models';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge';
import { MoneyPipe } from '../../../shared/pipes/money.pipe';
import { SkeletonLoaderComponent } from '../../../shared/components/skeleton-loader/skeleton-loader';

@Component({
  selector: 'app-agent-proposal-list',
  standalone: true,
  imports: [RouterLink, StatusBadgeComponent, MoneyPipe, SkeletonLoaderComponent],
  templateUrl: './proposal-list.html',
})
export class AgentProposalListComponent implements OnInit {
  private agentService = inject(AgentService);

  loading = signal(true);
  proposals = signal<ProposalDto[]>([]);

  ngOnInit(): void {
    this.agentService.getMyProposals().subscribe({
      next: proposals => {
        this.proposals.set(proposals);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  formatDate(dateStr: string): string {
    return new Date(dateStr).toLocaleDateString('en-IN', { day: '2-digit', month: 'short', year: 'numeric' });
  }
}
