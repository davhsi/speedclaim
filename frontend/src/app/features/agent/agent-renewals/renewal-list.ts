import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { Router } from '@angular/router';
import { AgentService, RenewalReminderDto } from '../services/agent.service';
import { MoneyPipe } from '../../../shared/pipes/money.pipe';
import { SkeletonLoaderComponent } from '../../../shared/components/skeleton-loader/skeleton-loader';
import { PaginationComponent } from '../../../shared/components/pagination/pagination';
import { ToastService } from '../../../shared/components/toast/toast.service';

@Component({
  selector: 'app-agent-renewal-list',
  standalone: true,
  imports: [MoneyPipe, SkeletonLoaderComponent, PaginationComponent],
  templateUrl: './renewal-list.html',
})
export class AgentRenewalListComponent implements OnInit {
  private readonly agentService = inject(AgentService);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);

  loading = signal(true);
  renewals = signal<RenewalReminderDto[]>([]);
  filterDays = signal(30);
  sendingReminderId = signal<string | null>(null);
  sentReminderIds = signal<Set<string>>(new Set());
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
        this.sentReminderIds.set(new Set(renewals.filter(r => r.reminderSentRecently).map(r => r.policyId)));
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  formatDate(dateStr: string): string {
    return new Date(dateStr).toLocaleDateString('en-IN', { day: '2-digit', month: 'short', year: 'numeric' });
  }

  sendReminder(renewal: RenewalReminderDto): void {
    if (this.sendingReminderId() || this.sentReminderIds().has(renewal.policyId)) return;

    this.sendingReminderId.set(renewal.policyId);
    this.agentService.sendRenewalReminder(renewal.policyId).subscribe({
      next: () => {
        this.toast.success('Premium reminder sent to the customer.');
        this.sentReminderIds.update(ids => new Set(ids).add(renewal.policyId));
        this.sendingReminderId.set(null);
      },
      error: err => {
        if (err?.status === 409) {
          this.toast.warning('A reminder was already sent for this policy in the last 24 hours.');
          this.sentReminderIds.update(ids => new Set(ids).add(renewal.policyId));
        } else {
          this.toast.error('Could not send premium reminder.');
        }
        this.sendingReminderId.set(null);
      },
    });
  }

  viewDetails(renewal: RenewalReminderDto): void {
    this.router.navigate(['/agent/policies'], { queryParams: { policyId: renewal.policyId } });
  }
}
