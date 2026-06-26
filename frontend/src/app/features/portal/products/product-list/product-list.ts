import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { RouterLink } from '@angular/router';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { ProductService } from '../services/product.service';
import { ProductDto } from '../../../../core/models/api.models';
import { InsuranceDomain } from '../../../../core/models/enums';
import { MoneyPipe } from '../../../../shared/pipes/money.pipe';
import { SkeletonLoaderComponent } from '../../../../shared/components/skeleton-loader/skeleton-loader';
import { EmptyStateComponent } from '../../../../shared/components/empty-state/empty-state';

@Component({
  selector: 'app-product-list',
  standalone: true,
  imports: [RouterLink, MoneyPipe, SkeletonLoaderComponent, EmptyStateComponent],
  templateUrl: './product-list.html',
})
export class ProductListComponent implements OnInit {
  private productService = inject(ProductService);
  private sanitizer = inject(DomSanitizer);

  loading = signal(true);
  products = signal<ProductDto[]>([]);
  selectedDomain = signal<string>('All');

  domainTabs = [
    { label: 'All', value: 'All' },
    { label: 'Health', value: 'Health' },
    { label: 'Motor', value: 'Motor' },
    { label: 'Life', value: 'Life' },
  ];

  filteredProducts = computed(() => {
    const domain = this.selectedDomain();
    const all = this.products();
    return domain === 'All' ? all : all.filter(p => p.domain === domain);
  });

  ngOnInit(): void {
    this.productService.getAll().subscribe({
      next: products => { this.products.set(products); this.loading.set(false); },
      error: () => this.loading.set(false),
    });
  }

  domainBg(domain: InsuranceDomain): string {
    const map: Record<string, string> = { HEALTH: 'bg-success-bg', MOTOR: 'bg-info-bg', LIFE: 'bg-[#F3EEFF]' };
    return map[domain.toUpperCase()] ?? 'bg-surface-alt';
  }

  domainFg(domain: InsuranceDomain): string {
    const map: Record<string, string> = { HEALTH: 'text-success', MOTOR: 'text-info', LIFE: 'text-[#7C3AED]' };
    return map[domain.toUpperCase()] ?? 'text-muted';
  }

  domainIcon(domain: string): SafeHtml {
    const map: Record<string, string> = {
      HEALTH: '<svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="#1F9D6B" stroke-width="1.75"><path d="M22 12h-4l-3 9L9 3l-3 9H2"/></svg>',
      MOTOR: '<svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="#2D7FF9" stroke-width="1.75"><circle cx="7" cy="17" r="2"/><circle cx="17" cy="17" r="2"/><path d="M5 17H3v-6l2-5h9l4 5h3v6h-2"/></svg>',
      LIFE: '<svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="#7C3AED" stroke-width="1.75"><path d="M20.84 4.61a5.5 5.5 0 00-7.78 0L12 5.67l-1.06-1.06a5.5 5.5 0 00-7.78 7.78l1.06 1.06L12 21.23l7.78-7.78 1.06-1.06a5.5 5.5 0 000-7.78z"/></svg>',
    };
    const svg = map[domain.toUpperCase()] ?? '<svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75"><circle cx="12" cy="12" r="10"/></svg>';
    return this.sanitizer.bypassSecurityTrustHtml(svg);
  }
}
