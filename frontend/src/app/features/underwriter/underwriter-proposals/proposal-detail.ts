import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge';
import { ConfirmDialogComponent } from '../../../shared/components/confirm-dialog/confirm-dialog';
import { MoneyPipe } from '../../../shared/pipes/money.pipe';
import { DateFormatPipe } from '../../../shared/pipes/date-format.pipe';
import { UnderwriterService } from '../services/underwriter.service';
import { ToastService } from '../../../shared/components/toast/toast.service';
import { ProductDto, ProposalDto, SubmittedDocumentDto } from '../../../core/models/api.models';
import { FormsModule } from '@angular/forms';
import { ProductService } from '../../portal/products/services/product.service';
import { DocumentPreviewComponent, PreviewDoc } from '../../../shared/components/document-preview/document-preview';

@Component({
  selector: 'app-uw-proposal-detail',
  standalone: true,
  imports: [StatusBadgeComponent, ConfirmDialogComponent, MoneyPipe, DateFormatPipe, FormsModule, DocumentPreviewComponent],
  templateUrl: './proposal-detail.html',
})
export class ProposalDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly uwService = inject(UnderwriterService);
  private readonly productService = inject(ProductService);
  private readonly toast = inject(ToastService);

  proposal = signal<ProposalDto | null>(null);
  product = signal<ProductDto | null>(null);

  riskFlags = computed(() => {
    const p = this.proposal();
    if (!p) return [];
    const flags: { label: string; color: string; bg: string; border: string }[] = [];
    const sa = p.sumAssured ?? 0;
    if (sa >= 10_000_000) {
      flags.push({ label: `Very high value · ${this.formatCr(sa)}`, color: '#D14343', bg: '#FBE9E9', border: '#F5B4B4' });
    } else if (sa >= 2_500_000) {
      flags.push({ label: `High value · ${this.formatL(sa)}`, color: '#D9920A', bg: '#FEF6E6', border: '#FAD88A' });
    }
    if ((p.tenureYears ?? 0) >= 20) {
      flags.push({ label: `Long tenure · ${p.tenureYears} years`, color: '#0F6E8C', bg: '#E6F4F8', border: '#B3D9E6' });
    }
    return flags;
  });

  private formatL(amount: number): string { return (amount / 100_000).toFixed(0) + 'L'; }
  private formatCr(amount: number): string { return (amount / 10_000_000).toFixed(1) + 'Cr'; }
  showDialog = signal<'approve' | 'reject' | 'docs' | null>(null);
  actionInFlight = signal(false);
  previewDoc = signal<PreviewDoc | null>(null);
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

  canDecide(): boolean {
    const s = this.proposal()?.status;
    return s === 'Submitted' || s === 'UnderReview' || s === 'DocumentsPending';
  }

  canRequestDocuments(): boolean {
    const s = this.proposal()?.status;
    return s === 'Submitted' || s === 'UnderReview' || s === 'DocumentsPending';
  }

  onApprove(): void {
    if (this.actionInFlight() || !this.canDecide()) return;
    const id = this.proposal()!.id.toString();
    this.actionInFlight.set(true);
    this.uwService.reviewProposal(id, { isApproved: true, notes: this.notes || 'Approved' }).subscribe({
      next: () => {
        this.toast.success('Proposal approved. Policy has been created.');
        this.showDialog.set(null);
        this.router.navigate(['/underwriter/proposals']);
      },
      error: () => {
        this.actionInFlight.set(false);
        this.toast.error('Approval failed.');
      },
    });
  }

  onReject(): void {
    if (this.actionInFlight() || !this.canDecide() || !this.rejectReason.trim()) return;
    const id = this.proposal()!.id.toString();
    this.actionInFlight.set(true);
    this.uwService.reviewProposal(id, { isApproved: false, notes: this.rejectReason.trim() }).subscribe({
      next: () => {
        this.toast.error('Proposal rejected.');
        this.showDialog.set(null);
        this.router.navigate(['/underwriter/proposals']);
      },
      error: () => {
        this.actionInFlight.set(false);
        this.toast.error('Rejection failed.');
      },
    });
  }

  onRequestDocs(): void {
    if (this.actionInFlight() || !this.canRequestDocuments() || !this.docsRequest.trim()) return;
    const id = this.proposal()!.id.toString();
    this.actionInFlight.set(true);
    this.uwService.requestDocs(id, this.docsRequest.trim()).subscribe({
      next: () => {
        this.toast.success('Document request sent to the customer.');
        this.showDialog.set(null);
        this.uwService.getProposalById(id).subscribe({ next: p => this.proposal.set(p) });
        this.actionInFlight.set(false);
      },
      error: () => {
        this.actionInFlight.set(false);
        this.toast.error('Document request failed.');
      },
    });
  }

  saveNotes(): void {
    if (this.actionInFlight()) return;
    if (!this.notes.trim()) {
      this.toast.warning('Please enter a note before saving.');
      return;
    }
    const id = this.proposal()!.id.toString();
    this.actionInFlight.set(true);
    this.uwService.updateNotes(id, this.notes).subscribe({
      next: () => {
        this.actionInFlight.set(false);
        this.toast.success('Notes saved.');
      },
      error: () => {
        this.actionInFlight.set(false);
        this.toast.error('Failed to save notes.');
      },
    });
  }

  closeDialog(): void {
    if (this.actionInFlight()) return;
    this.showDialog.set(null);
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

  documentHref(filePath: string): string {
    if (!filePath) return '#';
    return filePath.startsWith('/') ? filePath : `/${filePath}`;
  }

  openPreview(doc: SubmittedDocumentDto): void {
    this.previewDoc.set({ url: this.documentHref(doc.filePath), label: doc.documentName });
  }
  closePreview(): void { this.previewDoc.set(null); }
}
