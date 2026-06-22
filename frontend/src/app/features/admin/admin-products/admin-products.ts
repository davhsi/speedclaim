import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { StatCardComponent } from '../../../shared/components/stat-card/stat-card';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge';
import { AdminService } from '../services/admin.service';
import { ToastService } from '../../../shared/components/toast/toast.service';
import { ProductDto, DocumentRequirementResponseDto, PremiumRateDto } from '../../../core/models/api.models';

@Component({
  selector: 'app-admin-products',
  standalone: true,
  imports: [FormsModule, StatCardComponent, StatusBadgeComponent],
  templateUrl: './admin-products.html',
})
export class AdminProductsComponent implements OnInit {
  private adminService = inject(AdminService);
  private toastService = inject(ToastService);

  products = signal<ProductDto[]>([]);
  loading = signal(true);
  searchQuery = signal('');

  activeModal = signal<'create' | 'editRates' | 'editDocs' | null>(null);
  selectedProduct = signal<ProductDto | null>(null);
  productDocs = signal<DocumentRequirementResponseDto[]>([]);
  rateBands = signal<PremiumRateDto[]>([]);

  createForm = { productName: '', domain: 'Motor', uin: '', description: '', minAge: 18, maxAge: 65, minSumAssured: 100000, maxSumAssured: 5000000, minTenureYears: 1, maxTenureYears: 30, waitingPeriodDays: 30, allowsFamilyFloater: false, maxFamilyMembers: 1 };

  filteredProducts = computed(() => {
    const q = this.searchQuery().toLowerCase();
    return !q ? this.products() : this.products().filter(p => p.name.toLowerCase().includes(q) || p.domain.toLowerCase().includes(q));
  });

  totalProducts = computed(() => this.products().length);
  activeProducts = computed(() => this.products().filter(p => p.isActive).length);
  motorCount = computed(() => this.products().filter(p => p.domain === 'Motor').length);
  inactiveCount = computed(() => this.products().filter(p => !p.isActive).length);

  iconPackage = '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75" stroke-linecap="round" stroke-linejoin="round"><path d="M21 16V8a2 2 0 00-1-1.73l-7-4a2 2 0 00-2 0l-7 4A2 2 0 002 8v8a2 2 0 001 1.73l7 4a2 2 0 002 0l7-4A2 2 0 0021 16z"/><polyline points="3.27 6.96 12 12.01 20.73 6.96"/><line x1="12" y1="22.08" x2="12" y2="12"/></svg>';
  iconCheck = '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75" stroke-linecap="round" stroke-linejoin="round"><path d="M22 11.08V12a10 10 0 11-5.93-9.14"/><polyline points="22 4 12 14.01 9 11.01"/></svg>';
  iconCar = '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75" stroke-linecap="round" stroke-linejoin="round"><path d="M14 16H9m10 0h3v-3.15a1 1 0 00-.84-.99L16 11l-2.7-3.6a1 1 0 00-.8-.4H5.24a1 1 0 00-.9.55l-2.2 4.4a1 1 0 00-.1.45V16h3"/><circle cx="6.5" cy="16.5" r="2.5"/><circle cx="16.5" cy="16.5" r="2.5"/></svg>';
  iconX = '<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"/><line x1="15" y1="9" x2="9" y2="15"/><line x1="9" y1="9" x2="15" y2="15"/></svg>';

  ngOnInit(): void {
    this.loadProducts();
  }

  private loadProducts(): void {
    this.adminService.getProducts().subscribe({
      next: products => { this.products.set(products); this.loading.set(false); },
      error: () => this.loading.set(false),
    });
  }

  formatSum(val: number): string {
    return '₹' + val.toLocaleString('en-IN');
  }

  domainBadge(domain: string): { bg: string; fg: string; bdr: string } {
    const m: Record<string, { bg: string; fg: string; bdr: string }> = {
      Motor: { bg: '#E6F4F8', fg: '#0F6E8C', bdr: '#B3D9E6' },
      Health: { bg: '#E8F7F1', fg: '#1F9D6B', bdr: '#B2E4CE' },
      Life: { bg: '#FEF6E6', fg: '#D9920A', bdr: '#FAD88A' },
    };
    return m[domain] ?? m['Motor'];
  }

  openCreateModal(): void {
    this.createForm = { productName: '', domain: 'Motor', uin: '', description: '', minAge: 18, maxAge: 65, minSumAssured: 100000, maxSumAssured: 5000000, minTenureYears: 1, maxTenureYears: 30, waitingPeriodDays: 30, allowsFamilyFloater: false, maxFamilyMembers: 1 };
    this.activeModal.set('create');
  }

  openEditRatesModal(product: ProductDto): void {
    this.selectedProduct.set(product);
    this.rateBands.set([{ ageMin: 18, ageMax: 30, sumAssuredMin: 100000, sumAssuredMax: 500000, annualPremium: 5000 }]);
    this.activeModal.set('editRates');
  }

  openEditDocsModal(product: ProductDto): void {
    this.selectedProduct.set(product);
    this.productDocs.set([]);
    this.adminService.getProductDocuments(product.id).subscribe({
      next: docs => this.productDocs.set(docs),
      error: () => this.productDocs.set([]),
    });
    this.activeModal.set('editDocs');
  }

  closeModal(): void {
    this.activeModal.set(null);
  }

  createProduct(): void {
    this.adminService.createProduct(this.createForm as any).subscribe({
      next: () => { this.toastService.success('Product created'); this.closeModal(); this.loadProducts(); },
      error: () => this.toastService.error('Failed to create product'),
    });
  }

  toggleProductStatus(product: ProductDto): void {
    const next = !product.isActive;
    this.adminService.toggleProductStatus(product.id, next).subscribe({
      next: () => {
        this.products.update(list => list.map(p => p.id === product.id ? { ...p, isActive: next } : p));
        this.toastService.success(product.name + (next ? ' activated' : ' deactivated'));
      },
    });
  }

  addRateBand(): void {
    this.rateBands.update(bands => [...bands, { ageMin: 0, ageMax: 0, sumAssuredMin: 0, sumAssuredMax: 0, annualPremium: 0 }]);
  }

  removeRateBand(index: number): void {
    this.rateBands.update(bands => bands.filter((_, i) => i !== index));
  }

  updateRateBand(index: number, field: string, event: Event): void {
    const val = +(event.target as HTMLInputElement).value;
    this.rateBands.update(bands => bands.map((b, i) => i === index ? { ...b, [field]: val } : b));
  }

  saveRates(): void {
    const p = this.selectedProduct();
    if (!p) return;
    this.adminService.updateProductRates(p.id, this.rateBands()).subscribe({
      next: () => { this.toastService.success('Rates updated'); this.closeModal(); },
      error: () => this.toastService.error('Failed to update rates'),
    });
  }

  toggleDocRequired(index: number): void {
    this.productDocs.update(docs => docs.map((d, i) => i === index ? { ...d, isMandatory: !d.isMandatory } : d));
  }

  saveDocs(): void {
    const p = this.selectedProduct();
    if (!p) return;
    const reqs = this.productDocs().map(d => ({
      entityType: d.entityType, domain: d.domain, documentKey: d.documentKey,
      label: d.label, description: d.description, isMandatory: d.isMandatory, isActive: d.isActive,
    }));
    this.adminService.updateProductDocuments(p.id, reqs).subscribe({
      next: () => { this.toastService.success('Document requirements saved'); this.closeModal(); },
      error: () => this.toastService.error('Failed to save documents'),
    });
  }
}
