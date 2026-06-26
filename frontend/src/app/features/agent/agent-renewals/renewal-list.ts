import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { AgentService, RenewalReminderDto } from '../services/agent.service';
import { MoneyPipe } from '../../../shared/pipes/money.pipe';
import { SkeletonLoaderComponent } from '../../../shared/components/skeleton-loader/skeleton-loader';
import { PaginationComponent } from '../../../shared/components/pagination/pagination';

@Component({
  selector: 'app-agent-renewal-list',
  standalone: true,
  imports: [MoneyPipe, SkeletonLoaderComponent, PaginationComponent],
  templateUrl: './renewal-list.html',
})
export class AgentRenewalListComponent implements OnInit {
  private agentService = inject(AgentService);

  loading = signal(true);
  renewals = signal<RenewalReminderDto[]>([]);
  filterDays = signal(30);
  currentPage = signal(1);
  readonly pageSize = 10;

  filteredRenewals = computed(() => {
    return this.renewals().filter(r => r.daysUntilDue <= this.filterDays());
  });

  totalPages = computed(() => Math.max(1, Math.ceil(this.filteredRenewals().length / this.pageSize)));

  pagedRenewals = computed(() => {
    const start = (this.currentPage() - 1) * this.pageSize;
    return this.filteredRenewals().slice(start, start + this.pageSize);
  });

  onFilterChange(): void { this.currentPage.set(1); }
  onPageChange(page: number): void { this.currentPage.set(page); }

  ngOnInit(): void {
    this.agentService.getRenewals().subscribe({
      next: renewals => {
        this.renewals.set(renewals);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  formatDate(dateStr: string): string {
    return new Date(dateStr).toLocaleDateString('en-IN', { day: '2-digit', month: 'short', year: 'numeric' });
  }
}
