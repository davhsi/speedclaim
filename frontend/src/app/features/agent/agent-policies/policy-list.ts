import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { AgentService } from '../services/agent.service';
import { PolicyDto } from '../../../core/models/api.models';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge';
import { MoneyPipe } from '../../../shared/pipes/money.pipe';
import { SkeletonLoaderComponent } from '../../../shared/components/skeleton-loader/skeleton-loader';
import { PaginationComponent } from '../../../shared/components/pagination/pagination';
import { ToastService } from '../../../shared/components/toast/toast.service';

@Component({
  selector: 'app-agent-policy-list',
  standalone: true,
  imports: [StatusBadgeComponent, MoneyPipe, SkeletonLoaderComponent, PaginationComponent],
  templateUrl: './policy-list.html',
})
export class AgentPolicyListComponent implements OnInit {
  private readonly agentService = inject(AgentService);
  private readonly route = inject(ActivatedRoute);
  private readonly toast = inject(ToastService);

  loading = signal(true);
  policies = signal<PolicyDto[]>([]);
  highlightedPolicyId = signal<string | null>(null);
  remindingPolicyId = signal<string | null>(null);
  remindedPolicyIds = signal<Set<string>>(new Set());
  currentPage = signal(1);
  readonly pageSize = 10;

  totalPages = computed(() => Math.max(1, Math.ceil(this.policies().length / this.pageSize)));
  pagedPolicies = computed(() => {
    const start = (this.currentPage() - 1) * this.pageSize;
    return this.policies().slice(start, start + this.pageSize);
  });

  onPageChange(page: number): void { this.currentPage.set(page); }

  ngOnInit(): void {
    this.highlightedPolicyId.set(this.route.snapshot.queryParamMap.get('policyId'));
    this.agentService.getAssignedPolicies().subscribe({
      next: policies => {
        this.policies.set(policies);
        this.focusHighlightedPolicy(policies);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  formatDate(dateStr: string): string {
    return new Date(dateStr).toLocaleDateString('en-IN', { day: '2-digit', month: 'short', year: 'numeric' });
  }

  isHighlighted(policy: PolicyDto): boolean {
    return this.highlightedPolicyId() === policy.id;
  }

  canRequestPayment(policy: PolicyDto): boolean {
    return policy.status === 'Pending';
  }

  notifyCustomer(policy: PolicyDto): void {
    if (!this.canRequestPayment(policy) || this.remindingPolicyId() || this.remindedPolicyIds().has(policy.id)) return;

    this.remindingPolicyId.set(policy.id);
    this.agentService.remindCustomerToPay(policy.id).subscribe({
      next: () => {
        this.toast.success('Payment reminder sent to the customer.');
        this.remindedPolicyIds.update(ids => new Set(ids).add(policy.id));
        this.remindingPolicyId.set(null);
      },
      error: err => {
        if (err?.status === 409) {
          this.toast.warning('A payment reminder was already sent for this policy in the last 24 hours.');
          this.remindedPolicyIds.update(ids => new Set(ids).add(policy.id));
        } else {
          this.toast.error('Could not send payment reminder.');
        }
        this.remindingPolicyId.set(null);
      },
    });
  }

  async copyPaymentInstructions(policy: PolicyDto): Promise<void> {
    const message = this.paymentInstructions(policy);
    try {
      await globalThis.navigator.clipboard.writeText(message);
      this.toast.success('Payment instructions copied.');
    } catch {
      this.toast.error('Could not copy payment instructions.');
    }
  }

  paymentInstructions(policy: PolicyDto): string {
    const payUrl = `${globalThis.location.origin}/pay/${policy.id}`;
    return `Your SpeedClaim policy ${policy.policyNumber} is approved and pending first premium payment. Please log in and pay ${this.formatAmount(policy.premiumAmount)} here: ${payUrl}`;
  }

  private formatAmount(amount: number): string {
    return new Intl.NumberFormat('en-IN', { style: 'currency', currency: 'INR', maximumFractionDigits: 2 }).format(amount);
  }

  private focusHighlightedPolicy(policies: PolicyDto[]): void {
    const policyId = this.highlightedPolicyId();
    if (!policyId) return;

    const index = policies.findIndex(policy => policy.id === policyId);
    if (index >= 0) {
      this.currentPage.set(Math.floor(index / this.pageSize) + 1);
    }
  }
}
