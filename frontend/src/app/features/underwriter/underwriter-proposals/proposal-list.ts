import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge';
import { MoneyPipe } from '../../../shared/pipes/money.pipe';
import { DateFormatPipe } from '../../../shared/pipes/date-format.pipe';
import { UnderwriterService } from '../services/underwriter.service';
import { ProductDto, ProposalDto } from '../../../core/models/api.models';
import { ProductService } from '../../portal/products/services/product.service';
import { PaginationComponent } from '../../../shared/components/pagination/pagination';
import { SkeletonLoaderComponent } from '../../../shared/components/skeleton-loader/skeleton-loader';

@Component({
  selector: 'app-uw-proposal-list',
  standalone: true,
  imports: [StatusBadgeComponent, MoneyPipe, DateFormatPipe, PaginationComponent, SkeletonLoaderComponent],
  templateUrl: './proposal-list.html',
})
export class ProposalListComponent implements OnInit {
  private readonly uwService = inject(UnderwriterService);
  private readonly productService = inject(ProductService);
  private readonly router = inject(Router);

  proposals = signal<ProposalDto[]>([]);
  products = signal<ProductDto[]>([]);
  pendingCount = signal(0);
  loading = signal(true);
  currentPage = signal(1);
  statusFilter = signal('All');
  sortOrder = signal<'latest' | 'oldest'>('latest');
  readonly pageSize = 10;
  readonly statuses = ['All', 'Submitted', 'UnderReview', 'DocumentsPending', 'Approved', 'Rejected', 'Withdrawn'];

  filteredProposals = computed(() => {
    const status = this.statusFilter();
    return [...this.proposals()]
      .filter(p => status === 'All' || p.status === status)
      .sort((a, b) => {
        const diff = new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime();
        return this.sortOrder() === 'latest' ? diff : -diff;
      });
  });

  totalPages = computed(() => Math.max(1, Math.ceil(this.filteredProposals().length / this.pageSize)));
  pagedProposals = computed(() => {
    const start = (this.currentPage() - 1) * this.pageSize;
    return this.filteredProposals().slice(start, start + this.pageSize);
  });

  onPageChange(page: number): void { this.currentPage.set(page); }
  onFilterChange(value: string): void { this.statusFilter.set(value); this.currentPage.set(1); }
  onSortChange(value: string): void { this.sortOrder.set(value === 'oldest' ? 'oldest' : 'latest'); this.currentPage.set(1); }

  ngOnInit(): void {
    this.productService.getAll().subscribe(products => this.products.set(products));
    this.uwService.getAllProposals().subscribe({
      next: (data) => {
        this.proposals.set(data);
        this.pendingCount.set(data.filter(p => p.status === 'Submitted' || p.status === 'UnderReview').length);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
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
