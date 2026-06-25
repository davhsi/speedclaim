import { Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { ProductService } from '../services/product.service';
import { ProductDto, DocumentRequirementDto } from '../../../../core/models/api.models';
import { MoneyPipe } from '../../../../shared/pipes/money.pipe';
import { SkeletonLoaderComponent } from '../../../../shared/components/skeleton-loader/skeleton-loader';

@Component({
  selector: 'app-product-detail',
  standalone: true,
  imports: [RouterLink, MoneyPipe, SkeletonLoaderComponent],
  templateUrl: './product-detail.html',
})
export class ProductDetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private productService = inject(ProductService);

  loading = signal(true);
  product = signal<ProductDto | null>(null);
  documents = signal<DocumentRequirementDto[]>([]);

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id') ?? '';
    forkJoin({
      product: this.productService.getById(id),
      docs: this.productService.getDocumentRequirements(id),
    }).subscribe({
      next: ({ product, docs }) => {
        this.product.set(product);
        this.documents.set(docs);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  domainBg(domain: string): string {
    switch (domain) {
      case 'Health': return 'bg-success-bg';
      case 'Motor': return 'bg-info-bg';
      case 'Life': return 'bg-[#F3EEFF]';
      default: return 'bg-surface-alt';
    }
  }

  domainFg(domain: string): string {
    switch (domain) {
      case 'Health': return 'text-success';
      case 'Motor': return 'text-info';
      case 'Life': return 'text-[#7C3AED]';
      default: return 'text-muted';
    }
  }

  domainIcon(domain: string): string {
    const map: Record<string, string> = {
      Health: '<svg width="28" height="28" viewBox="0 0 24 24" fill="none" stroke="#1F9D6B" stroke-width="1.75"><path d="M22 12h-4l-3 9L9 3l-3 9H2"/></svg>',
      Motor: '<svg width="28" height="28" viewBox="0 0 24 24" fill="none" stroke="#2D7FF9" stroke-width="1.75"><circle cx="7" cy="17" r="2"/><circle cx="17" cy="17" r="2"/><path d="M5 17H3v-6l2-5h9l4 5h3v6h-2"/></svg>',
      Life: '<svg width="28" height="28" viewBox="0 0 24 24" fill="none" stroke="#7C3AED" stroke-width="1.75"><path d="M20.84 4.61a5.5 5.5 0 00-7.78 0L12 5.67l-1.06-1.06a5.5 5.5 0 00-7.78 7.78l1.06 1.06L12 21.23l7.78-7.78 1.06-1.06a5.5 5.5 0 000-7.78z"/></svg>',
    };
    return map[domain] ?? '<svg width="28" height="28" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75"><circle cx="12" cy="12" r="10"/></svg>';
  }
}
