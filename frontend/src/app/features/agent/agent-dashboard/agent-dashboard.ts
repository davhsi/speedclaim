import { Component, inject, OnInit, signal } from '@angular/core';
import { Router } from '@angular/router';
import { forkJoin } from 'rxjs';
import { AgentService, AgentDashboardDto, RenewalReminderDto } from '../services/agent.service';
import { MoneyPipe } from '../../../shared/pipes/money.pipe';
import { SkeletonLoaderComponent } from '../../../shared/components/skeleton-loader/skeleton-loader';
import { SafeHtmlPipe } from '../../../shared/pipes/safe-html.pipe';

interface ActivityItem {
  icon: string;
  bg: string;
  title: string;
  subtitle: string;
  time: string;
}

const PROPOSAL_ICON = `<svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="var(--color-info)" stroke-width="1.75" stroke-linecap="round" stroke-linejoin="round"><path d="M14 2H6a2 2 0 00-2 2v16a2 2 0 002 2h12a2 2 0 002-2V8z"/><polyline points="14 2 14 8 20 8"/><line x1="16" y1="13" x2="8" y2="13"/><line x1="16" y1="17" x2="8" y2="17"/></svg>`;

const RENEWAL_ICON = `<svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="var(--color-warning)" stroke-width="1.75" stroke-linecap="round" stroke-linejoin="round"><rect x="3" y="4" width="18" height="18" rx="2"/><line x1="16" y1="2" x2="16" y2="6"/><line x1="8" y1="2" x2="8" y2="6"/><line x1="3" y1="10" x2="21" y2="10"/></svg>`;

@Component({
  selector: 'app-agent-dashboard',
  standalone: true,
  imports: [MoneyPipe, SkeletonLoaderComponent, SafeHtmlPipe],
  templateUrl: './agent-dashboard.html',
})
export class AgentDashboardComponent implements OnInit {
  private agentService = inject(AgentService);
  private router = inject(Router);

  loading = signal(true);
  dashboard = signal<AgentDashboardDto | null>(null);
  renewals = signal<RenewalReminderDto[]>([]);
  pendingProposals = 0;
  newCustomersThisMonth = 0;

  recentActivity: ActivityItem[] = [];

  ngOnInit(): void {
    forkJoin({
      dashboard: this.agentService.getDashboard(),
      renewals: this.agentService.getRenewals(),
      proposals: this.agentService.getMyProposals(),
      customers: this.agentService.getCustomers(),
    }).subscribe({
      next: ({ dashboard, renewals, proposals, customers }) => {
        this.dashboard.set(dashboard);
        this.renewals.set(renewals);
        this.pendingProposals = proposals.filter(p =>
          p.status === 'Submitted' || p.status === 'UnderReview',
        ).length;

        const now = new Date();
        this.newCustomersThisMonth = customers.filter(c => {
          const d = new Date(c.createdAt);
          return d.getFullYear() === now.getFullYear() && d.getMonth() === now.getMonth();
        }).length;

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
        icon: PROPOSAL_ICON,
        bg: 'var(--color-info-bg)',
        title: `Proposal ${p.proposalNumber}`,
        subtitle: `${p.productName} · ${p.status}`,
        time: this.timeAgo(p.createdAt),
      });
    }

    for (const r of renewals.slice(0, 2)) {
      items.push({
        icon: RENEWAL_ICON,
        bg: 'var(--color-warning-bg)',
        title: `Renewal due: ${r.customerName}`,
        subtitle: `${r.policyNumber} · ${r.daysUntilDue} days left`,
        time: new Date(r.dueDate).toLocaleDateString('en-IN', { day: '2-digit', month: 'short' }),
      });
    }

    return items;
  }

  navigateTo(path: string, queryParams?: Record<string, string>): void {
    this.router.navigate([path], queryParams ? { queryParams } : {});
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
