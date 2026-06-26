import { Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { AgentService, AgentCustomerDto } from '../services/agent.service';
import { PolicyDto, ProposalDto } from '../../../core/models/api.models';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge';
import { MoneyPipe } from '../../../shared/pipes/money.pipe';
import { SkeletonLoaderComponent } from '../../../shared/components/skeleton-loader/skeleton-loader';

@Component({
  selector: 'app-agent-customer-detail',
  standalone: true,
  imports: [RouterLink, StatusBadgeComponent, MoneyPipe, SkeletonLoaderComponent],
  templateUrl: './customer-detail.html',
})
export class AgentCustomerDetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private agentService = inject(AgentService);

  loading = signal(true);
  customer = signal<AgentCustomerDto | null>(null);
  customerPolicies = signal<PolicyDto[]>([]);
  customerProposals = signal<ProposalDto[]>([]);
  activeTab: 'policies' | 'proposals' = 'policies';

  ngOnInit(): void {
    const customerId = this.route.snapshot.paramMap.get('id') ?? '';

    forkJoin({
      customers: this.agentService.getCustomers(),
      policies: this.agentService.getAssignedPolicies(),
      proposals: this.agentService.getMyProposals(),
    }).subscribe({
      next: ({ customers, policies, proposals }) => {
        const c = customers.find(c => c.id === customerId) ?? null;
        this.customer.set(c);
        const entityId = c?.customerId ?? customerId;
        this.customerPolicies.set(policies.filter(p => p.userId === entityId));
        this.customerProposals.set(proposals.filter(p => p.customerId === entityId));
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  initials(c: AgentCustomerDto): string {
    return (c.firstName.charAt(0) + c.lastName.charAt(0)).toUpperCase();
  }

  formatDate(dateStr: string): string {
    return new Date(dateStr).toLocaleDateString('en-IN', { month: 'short', year: 'numeric' });
  }
}
