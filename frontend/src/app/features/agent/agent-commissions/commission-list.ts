import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { AgentService, AgentCommissionDto, AgentDashboardDto } from '../services/agent.service';
import { MoneyPipe } from '../../../shared/pipes/money.pipe';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge';
import { SkeletonLoaderComponent } from '../../../shared/components/skeleton-loader/skeleton-loader';
import { PaginationComponent } from '../../../shared/components/pagination/pagination';
import { forkJoin } from 'rxjs';

@Component({
  selector: 'app-agent-commission-list',
  standalone: true,
  imports: [MoneyPipe, StatusBadgeComponent, SkeletonLoaderComponent, PaginationComponent],
  templateUrl: './commission-list.html',
})
export class AgentCommissionListComponent implements OnInit {
  private agentService = inject(AgentService);

  loading = signal(true);
  commissions = signal<AgentCommissionDto[]>([]);
  dashboard = signal<AgentDashboardDto | null>(null);
  pendingTotal = 0;
  currentPage = signal(1);
  readonly pageSize = 10;

  totalPages = computed(() => Math.max(1, Math.ceil(this.commissions().length / this.pageSize)));
  pagedCommissions = computed(() => {
    const start = (this.currentPage() - 1) * this.pageSize;
    return this.commissions().slice(start, start + this.pageSize);
  });

  onPageChange(page: number): void { this.currentPage.set(page); }

  ngOnInit(): void {
    forkJoin({
      commissions: this.agentService.getCommissions(),
      dashboard: this.agentService.getDashboard(),
    }).subscribe({
      next: ({ commissions, dashboard }) => {
        this.commissions.set(commissions);
        this.dashboard.set(dashboard);
        this.pendingTotal = commissions
          .filter(c => c.status !== 'Paid')
          .reduce((sum, c) => sum + c.commissionAmount, 0);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  formatDate(dateStr: string): string {
    return new Date(dateStr).toLocaleDateString('en-IN', { day: '2-digit', month: 'short', year: 'numeric' });
  }
}
