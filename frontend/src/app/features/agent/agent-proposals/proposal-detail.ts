import { Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { AgentService } from '../services/agent.service';
import { ProductDto, ProposalDto } from '../../../core/models/api.models';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge';
import { ConfirmDialogComponent } from '../../../shared/components/confirm-dialog/confirm-dialog';
import { MoneyPipe } from '../../../shared/pipes/money.pipe';
import { DateFormatPipe } from '../../../shared/pipes/date-format.pipe';
import { SkeletonLoaderComponent } from '../../../shared/components/skeleton-loader/skeleton-loader';
import { ToastService } from '../../../shared/components/toast/toast.service';
import { ProductService } from '../../portal/products/services/product.service';

@Component({
  selector: 'app-agent-proposal-detail',
  standalone: true,
  imports: [StatusBadgeComponent, ConfirmDialogComponent, MoneyPipe, DateFormatPipe, SkeletonLoaderComponent],
  templateUrl: './proposal-detail.html',
})
export class AgentProposalDetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private agentService = inject(AgentService);
  private productService = inject(ProductService);
  private toast = inject(ToastService);

  proposal = signal<ProposalDto | null>(null);
  product = signal<ProductDto | null>(null);
  loading = signal(true);
  showWithdrawDialog = signal(false);
  actionInFlight = signal(false);

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.goBack();
      return;
    }

    this.agentService.getProposalById(id).subscribe({
      next: proposal => {
        this.proposal.set(proposal);
        this.loading.set(false);
        this.productService.getById(proposal.productId).subscribe({
          next: product => this.product.set(product),
          error: () => this.product.set(null),
        });
      },
      error: () => {
        this.toast.error('Could not load proposal details.');
        this.loading.set(false);
        this.goBack();
      },
    });
  }

  goBack(): void {
    this.router.navigate(['/agent/proposals']);
  }

  canWithdraw(): boolean {
    const s = this.proposal()?.status;
    return s === 'Submitted' || s === 'UnderReview' || s === 'DocumentsPending';
  }

  confirmWithdraw(): void {
    const p = this.proposal();
    if (!p || this.actionInFlight()) return;
    this.actionInFlight.set(true);
    this.agentService.withdrawProposal(p.id).subscribe({
      next: () => {
        this.toast.success('Proposal withdrawn successfully.');
        this.showWithdrawDialog.set(false);
        this.router.navigate(['/agent/proposals']);
      },
      error: () => {
        this.actionInFlight.set(false);
        this.showWithdrawDialog.set(false);
        this.toast.error('Failed to withdraw proposal.');
      },
    });
  }

  detailEntries(proposal: ProposalDto): Array<{ label: string; value: string }> {
    return [
      { label: 'Proposal number', value: proposal.proposalNumber },
      { label: 'Product', value: this.product()?.productName || proposal.productName || 'Insurance product' },
      { label: 'Domain', value: this.product()?.domain || proposal.domain || 'Not specified' },
      { label: 'Tenure', value: `${proposal.tenureYears} years` },
      { label: 'Payment frequency', value: proposal.paymentFrequency },
      { label: 'Submitted', value: new Date(proposal.createdAt).toLocaleDateString('en-IN', { day: '2-digit', month: 'short', year: 'numeric' }) },
    ];
  }
}
