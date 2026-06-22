import { Component, inject, OnInit, signal } from '@angular/core';
import { AgentService, AgentCommissionDto, AgentDashboardDto } from '../services/agent.service';
import { MoneyPipe } from '../../../shared/pipes/money.pipe';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge';
import { SkeletonLoaderComponent } from '../../../shared/components/skeleton-loader/skeleton-loader';
import { forkJoin } from 'rxjs';

@Component({
  selector: 'app-agent-commission-list',
  standalone: true,
  imports: [MoneyPipe, StatusBadgeComponent, SkeletonLoaderComponent],
  templateUrl: './commission-list.html',
})
export class AgentCommissionListComponent implements OnInit {
  private agentService = inject(AgentService);

  loading = signal(true);
  commissions = signal<AgentCommissionDto[]>([]);
  dashboard = signal<AgentDashboardDto | null>(null);
  pendingTotal = 0;

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
