import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AgentService } from '../services/agent.service';
import { ProposalDto } from '../../../core/models/api.models';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge';
import { MoneyPipe } from '../../../shared/pipes/money.pipe';
import { SkeletonLoaderComponent } from '../../../shared/components/skeleton-loader/skeleton-loader';
import { PaginationComponent } from '../../../shared/components/pagination/pagination';

@Component({
  selector: 'app-agent-proposal-list',
  standalone: true,
  imports: [RouterLink, StatusBadgeComponent, MoneyPipe, SkeletonLoaderComponent, PaginationComponent],
  templateUrl: './proposal-list.html',
})
export class AgentProposalListComponent implements OnInit {
  private agentService = inject(AgentService);

  loading = signal(true);
  proposals = signal<ProposalDto[]>([]);
  currentPage = signal(1);
  readonly pageSize = 10;

  totalPages = computed(() => Math.max(1, Math.ceil(this.proposals().length / this.pageSize)));
  pagedProposals = computed(() => {
    const start = (this.currentPage() - 1) * this.pageSize;
    return this.proposals().slice(start, start + this.pageSize);
  });

  onPageChange(page: number): void { this.currentPage.set(page); }

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
