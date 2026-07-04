import { Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { ProductService } from '../services/product.service';
import { ProductDto, DocumentRequirementDto } from '../../../../core/models/api.models';
import { MoneyPipe } from '../../../../shared/pipes/money.pipe';
import { SafeHtmlPipe } from '../../../../shared/pipes/safe-html.pipe';
import { SkeletonLoaderComponent } from '../../../../shared/components/skeleton-loader/skeleton-loader';

@Component({
  selector: 'app-product-detail',
  standalone: true,
  imports: [RouterLink, MoneyPipe, SafeHtmlPipe, SkeletonLoaderComponent],
  templateUrl: './product-detail.html',
})
export class ProductDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly productService = inject(ProductService);

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
    const map: Record<string, string> = { HEALTH: 'bg-success-bg', MOTOR: 'bg-info-bg', LIFE: 'bg-[#F3EEFF]' };
    return map[domain.toUpperCase()] ?? 'bg-surface-alt';
  }

  domainFg(domain: string): string {
    const map: Record<string, string> = { HEALTH: 'text-success', MOTOR: 'text-info', LIFE: 'text-[#7C3AED]' };
    return map[domain.toUpperCase()] ?? 'text-muted';
  }

  domainIcon(domain: string): string {
    const map: Record<string, string> = {
      HEALTH: '<svg width="28" height="28" viewBox="0 0 24 24" fill="none" stroke="#1F9D6B" stroke-width="1.75"><path d="M22 12h-4l-3 9L9 3l-3 9H2"/></svg>',
      MOTOR: '<svg width="28" height="28" viewBox="0 0 24 24" fill="none" stroke="#2D7FF9" stroke-width="1.75"><circle cx="7" cy="17" r="2"/><circle cx="17" cy="17" r="2"/><path d="M5 17H3v-6l2-5h9l4 5h3v6h-2"/></svg>',
      LIFE: '<svg width="28" height="28" viewBox="0 0 24 24" fill="none" stroke="#7C3AED" stroke-width="1.75"><path d="M20.84 4.61a5.5 5.5 0 00-7.78 0L12 5.67l-1.06-1.06a5.5 5.5 0 00-7.78 7.78l1.06 1.06L12 21.23l7.78-7.78 1.06-1.06a5.5 5.5 0 000-7.78z"/></svg>',
    };
    return map[domain.toUpperCase()] ?? '<svg width="28" height="28" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75"><circle cx="12" cy="12" r="10"/></svg>';
  }
}
