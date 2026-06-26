import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { AgentService } from '../services/agent.service';
import { PolicyDto } from '../../../core/models/api.models';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge';
import { MoneyPipe } from '../../../shared/pipes/money.pipe';
import { SkeletonLoaderComponent } from '../../../shared/components/skeleton-loader/skeleton-loader';
import { PaginationComponent } from '../../../shared/components/pagination/pagination';

@Component({
  selector: 'app-agent-policy-list',
  standalone: true,
  imports: [StatusBadgeComponent, MoneyPipe, SkeletonLoaderComponent, PaginationComponent],
  templateUrl: './policy-list.html',
})
export class AgentPolicyListComponent implements OnInit {
  private agentService = inject(AgentService);

  loading = signal(true);
  policies = signal<PolicyDto[]>([]);
  currentPage = signal(1);
  readonly pageSize = 10;

  totalPages = computed(() => Math.max(1, Math.ceil(this.policies().length / this.pageSize)));
  pagedPolicies = computed(() => {
    const start = (this.currentPage() - 1) * this.pageSize;
    return this.policies().slice(start, start + this.pageSize);
  });

  onPageChange(page: number): void { this.currentPage.set(page); }

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
