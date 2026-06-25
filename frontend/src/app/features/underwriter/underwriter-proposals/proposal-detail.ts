import { Component, inject, signal, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge';
import { ConfirmDialogComponent } from '../../../shared/components/confirm-dialog/confirm-dialog';
import { MoneyPipe } from '../../../shared/pipes/money.pipe';
import { DateFormatPipe } from '../../../shared/pipes/date-format.pipe';
import { UnderwriterService } from '../services/underwriter.service';
import { ToastService } from '../../../shared/components/toast/toast.service';
import { ProductDto, ProposalDto } from '../../../core/models/api.models';
import { FormsModule } from '@angular/forms';
import { ProductService } from '../../portal/products/services/product.service';

@Component({
  selector: 'app-uw-proposal-detail',
  standalone: true,
  imports: [StatusBadgeComponent, ConfirmDialogComponent, MoneyPipe, DateFormatPipe, FormsModule],
  templateUrl: './proposal-detail.html',
})
export class ProposalDetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private uwService = inject(UnderwriterService);
  private productService = inject(ProductService);
  private toast = inject(ToastService);

  proposal = signal<ProposalDto | null>(null);
  product = signal<ProductDto | null>(null);
  showDialog = signal<'approve' | 'reject' | 'docs' | null>(null);
  notes = '';
  rejectReason = '';
  docsRequest = '';

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id')!;
    this.uwService.getProposalById(id).subscribe({
      next: (p) => {
        this.proposal.set(p);
        this.notes = p.underwriterNotes ?? '';
        this.productService.getById(p.productId).subscribe({
          next: product => this.product.set(product),
          error: () => this.product.set(null),
        });
      },
    });
  }

  isPending(): boolean {
    const s = this.proposal()?.status;
    return s === 'Submitted' || s === 'UnderReview' || s === 'DocumentsPending';
  }

  onApprove(): void {
    const id = this.proposal()!.id.toString();
    this.uwService.reviewProposal(id, { isApproved: true, notes: this.notes || 'Approved' }).subscribe({
      next: () => {
        this.toast.success('Proposal approved. Policy has been created.');
        this.showDialog.set(null);
        this.router.navigate(['/underwriter/proposals']);
      },
    });
  }

  onReject(): void {
    if (!this.rejectReason.trim()) return;
    const id = this.proposal()!.id.toString();
    this.uwService.reviewProposal(id, { isApproved: false, notes: this.rejectReason }).subscribe({
      next: () => {
        this.toast.error('Proposal rejected.');
        this.showDialog.set(null);
        this.router.navigate(['/underwriter/proposals']);
      },
    });
  }

  onRequestDocs(): void {
    if (!this.docsRequest.trim()) return;
    const id = this.proposal()!.id.toString();
    this.uwService.requestDocs(id, this.docsRequest).subscribe({
      next: () => {
        this.toast.success('Document request sent to the customer.');
        this.showDialog.set(null);
      },
    });
  }

  saveNotes(): void {
    const id = this.proposal()!.id.toString();
    this.uwService.updateNotes(id, this.notes).subscribe({
      next: () => this.toast.success('Notes saved.'),
    });
  }

  goBack(): void {
    this.router.navigate(['/underwriter/proposals']);
  }

  productName(): string {
    return this.product()?.productName ?? this.proposal()?.productName ?? 'Insurance product';
  }

  displayDomain(): string {
    return this.product()?.domain ?? this.proposal()?.domain ?? 'Unknown';
  }
}
