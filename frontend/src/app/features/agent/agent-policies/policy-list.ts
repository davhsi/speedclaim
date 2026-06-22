import { Component, inject, OnInit, signal } from '@angular/core';
import { AgentService } from '../services/agent.service';
import { PolicyDto } from '../../../core/models/api.models';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge';
import { MoneyPipe } from '../../../shared/pipes/money.pipe';
import { SkeletonLoaderComponent } from '../../../shared/components/skeleton-loader/skeleton-loader';

@Component({
  selector: 'app-agent-policy-list',
  standalone: true,
  imports: [StatusBadgeComponent, MoneyPipe, SkeletonLoaderComponent],
  templateUrl: './policy-list.html',
})
export class AgentPolicyListComponent implements OnInit {
  private agentService = inject(AgentService);

  loading = signal(true);
  policies = signal<PolicyDto[]>([]);

  ngOnInit(): void {
    this.agentService.getAssignedPolicies().subscribe({
      next: policies => {
        this.policies.set(policies);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  formatDate(dateStr: string): string {
    return new Date(dateStr).toLocaleDateString('en-IN', { day: '2-digit', month: 'short', year: 'numeric' });
  }
}
