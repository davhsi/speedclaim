import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { RouterLink, ActivatedRoute, Router } from '@angular/router';
import { AgentService } from '../services/agent.service';
import { ProposalDto } from '../../../core/models/api.models';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge';
import { MoneyPipe } from '../../../shared/pipes/money.pipe';
import { DateFormatPipe } from '../../../shared/pipes/date-format.pipe';
import { SkeletonLoaderComponent } from '../../../shared/components/skeleton-loader/skeleton-loader';
import { PaginationComponent } from '../../../shared/components/pagination/pagination';

@Component({
  selector: 'app-agent-proposal-list',
  standalone: true,
  imports: [RouterLink, StatusBadgeComponent, MoneyPipe, DateFormatPipe, SkeletonLoaderComponent, PaginationComponent],
  templateUrl: './proposal-list.html',
})
export class AgentProposalListComponent implements OnInit {
  private agentService = inject(AgentService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  loading = signal(true);
  proposals = signal<ProposalDto[]>([]);
  activeFilter = signal('');
  currentPage = signal(1);
  readonly pageSize = 10;

  readonly PENDING_STATUSES = ['Submitted', 'UnderReview'];

  filteredProposals = computed(() => {
    const filter = this.activeFilter();
    const all = this.proposals();
    if (filter === 'pending') return all.filter(p => this.PENDING_STATUSES.includes(p.status));
    return all;
  });

  totalPages = computed(() => Math.max(1, Math.ceil(this.filteredProposals().length / this.pageSize)));
  pagedProposals = computed(() => {
    const start = (this.currentPage() - 1) * this.pageSize;
    return this.filteredProposals().slice(start, start + this.pageSize);
  });

  onPageChange(page: number): void { this.currentPage.set(page); }

  clearFilter(): void {
    this.activeFilter.set('');
    this.currentPage.set(1);
    this.router.navigate([], { queryParams: {}, replaceUrl: true });
  }

  ngOnInit(): void {
    const params = this.route.snapshot.queryParams;
    if (params['status']) this.activeFilter.set(params['status']);

    this.agentService.getMyProposals().subscribe({
      next: proposals => {
        this.proposals.set(proposals);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }
}
