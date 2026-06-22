import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { AgentService, RenewalReminderDto } from '../services/agent.service';
import { MoneyPipe } from '../../../shared/pipes/money.pipe';
import { SkeletonLoaderComponent } from '../../../shared/components/skeleton-loader/skeleton-loader';

@Component({
  selector: 'app-agent-renewal-list',
  standalone: true,
  imports: [MoneyPipe, SkeletonLoaderComponent],
  templateUrl: './renewal-list.html',
})
export class AgentRenewalListComponent implements OnInit {
  private agentService = inject(AgentService);

  loading = signal(true);
  renewals = signal<RenewalReminderDto[]>([]);
  filterDays = signal(30);

  filteredRenewals = computed(() => {
    return this.renewals().filter(r => r.daysUntilDue <= this.filterDays());
  });

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
