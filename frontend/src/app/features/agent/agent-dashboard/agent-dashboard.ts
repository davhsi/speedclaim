import { Component, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { AgentService, AgentDashboardDto, RenewalReminderDto } from '../services/agent.service';
import { MoneyPipe } from '../../../shared/pipes/money.pipe';
import { SkeletonLoaderComponent } from '../../../shared/components/skeleton-loader/skeleton-loader';

interface ActivityItem {
  initial: string;
  bg: string;
  iconFg: string;
  title: string;
  subtitle: string;
  time: string;
}

@Component({
  selector: 'app-agent-dashboard',
  standalone: true,
  imports: [RouterLink, MoneyPipe, SkeletonLoaderComponent],
  templateUrl: './agent-dashboard.html',
})
export class AgentDashboardComponent implements OnInit {
  private agentService = inject(AgentService);

  loading = signal(true);
  dashboard = signal<AgentDashboardDto | null>(null);
  renewals = signal<RenewalReminderDto[]>([]);
  pendingProposals = 0;
  newCustomersThisMonth = 3;

  recentActivity: ActivityItem[] = [];

  ngOnInit(): void {
    forkJoin({
      dashboard: this.agentService.getDashboard(),
      renewals: this.agentService.getRenewals(),
      proposals: this.agentService.getMyProposals(),
    }).subscribe({
      next: ({ dashboard, renewals, proposals }) => {
        this.dashboard.set(dashboard);
        this.renewals.set(renewals);
        this.pendingProposals = proposals.filter(p =>
          p.status === 'Submitted' || p.status === 'UnderReview',
        ).length;

        this.recentActivity = this.buildActivity(renewals, proposals);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  private buildActivity(renewals: RenewalReminderDto[], proposals: any[]): ActivityItem[] {
    const items: ActivityItem[] = [];

    for (const p of proposals.slice(0, 2)) {
      items.push({
        initial: p.productName?.substring(0, 2)?.toUpperCase() ?? 'PR',
        bg: 'var(--color-info-bg)',
        iconFg: 'var(--color-info)',
        title: `Proposal ${p.proposalNumber}`,
        subtitle: `${p.productName} · ${p.status}`,
        time: this.timeAgo(p.createdAt),
      });
    }

    for (const r of renewals.slice(0, 2)) {
      items.push({
        initial: r.customerName?.substring(0, 2)?.toUpperCase() ?? 'RN',
        bg: 'var(--color-warning-bg)',
        iconFg: 'var(--color-warning)',
        title: `Renewal due: ${r.customerName}`,
        subtitle: `${r.policyNumber} · ${r.daysUntilDue} days left`,
        time: new Date(r.dueDate).toLocaleDateString('en-IN', { day: '2-digit', month: 'short' }),
      });
    }

    return items;
  }

  private timeAgo(dateStr: string): string {
    const diff = Date.now() - new Date(dateStr).getTime();
    const mins = Math.floor(diff / 60000);
    if (mins < 60) return `${mins}m ago`;
    const hrs = Math.floor(mins / 60);
    if (hrs < 24) return `${hrs}h ago`;
    const days = Math.floor(hrs / 24);
    return `${days}d ago`;
  }
}
