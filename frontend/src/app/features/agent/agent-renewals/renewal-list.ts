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
  private agentService = inject(AgentService);
  private router = inject(Router);
  private toast = inject(ToastService);

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

  sendReminder(renewal: RenewalReminderDto): void {
    if (!renewal.customerEmail) {
      this.toast.warning('Customer email is not available for this renewal.');
      return;
    }

    const subject = encodeURIComponent(`Renewal reminder for ${renewal.policyNumber}`);
    const body = encodeURIComponent(
      `Dear ${renewal.customerName},\n\nYour SpeedClaim policy ${renewal.policyNumber} is due for renewal on ${this.formatDate(renewal.dueDate)}. The premium due is INR ${renewal.amountDue}.\n\nPlease log in to SpeedClaim to complete the renewal.\n\nRegards,\nSpeedClaim`
    );
    window.location.href = `mailto:${renewal.customerEmail}?subject=${subject}&body=${body}`;
  }

  viewDetails(renewal: RenewalReminderDto): void {
    this.router.navigate(['/agent/policies'], { queryParams: { policyId: renewal.policyId } });
  }
}
