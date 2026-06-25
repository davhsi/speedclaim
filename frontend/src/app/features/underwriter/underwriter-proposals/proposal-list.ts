import { Component, inject, signal, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge';
import { MoneyPipe } from '../../../shared/pipes/money.pipe';
import { DateFormatPipe } from '../../../shared/pipes/date-format.pipe';
import { UnderwriterService } from '../services/underwriter.service';
import { ProductDto, ProposalDto } from '../../../core/models/api.models';
import { ProductService } from '../../portal/products/services/product.service';

@Component({
  selector: 'app-uw-proposal-list',
  standalone: true,
  imports: [StatusBadgeComponent, MoneyPipe, DateFormatPipe],
  templateUrl: './proposal-list.html',
})
export class ProposalListComponent implements OnInit {
  private uwService = inject(UnderwriterService);
  private productService = inject(ProductService);
  private router = inject(Router);

  proposals = signal<ProposalDto[]>([]);
  products = signal<ProductDto[]>([]);
  pendingCount = signal(0);

  ngOnInit(): void {
    this.productService.getAll().subscribe(products => this.products.set(products));
    this.uwService.getAllProposals().subscribe({
      next: (data) => {
        this.proposals.set(data);
        this.pendingCount.set(data.filter(p => p.status === 'Submitted' || p.status === 'UnderReview').length);
      },
    });
  }

  openProposal(id: string): void {
    this.router.navigate(['/underwriter/proposals', id]);
  }

  productName(proposal: ProposalDto): string {
    return this.products().find(p => p.id === proposal.productId)?.productName
      ?? proposal.productName
      ?? 'Insurance product';
  }
}
