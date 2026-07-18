import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { StatCardComponent } from '../../../shared/components/stat-card/stat-card';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge';
import { AdminService } from '../services/admin.service';
import { ToastService } from '../../../shared/components/toast/toast.service';
import { ProductDto, DocumentRequirementResponseDto, PremiumRateDto, UpdateProductRequest } from '../../../core/models/api.models';
import { PaginationComponent } from '../../../shared/components/pagination/pagination';

@Component({
  selector: 'app-admin-products',
  standalone: true,
  imports: [FormsModule, StatCardComponent, StatusBadgeComponent, PaginationComponent],
  templateUrl: './admin-products.html',
})
export class AdminProductsComponent implements OnInit {
  private readonly adminService = inject(AdminService);
  private readonly toastService = inject(ToastService);

  products = signal<ProductDto[]>([]);
  loading = signal(true);
  searchQuery = signal('');

  activeModal = signal<'create' | 'editProduct' | 'editRates' | 'editDocs' | null>(null);
  selectedProduct = signal<ProductDto | null>(null);
  actionMenuProduct = signal<ProductDto | null>(null);
  productDocs = signal<DocumentRequirementResponseDto[]>([]);
  rateBands = signal<PremiumRateDto[]>([]);
  createSubmitting = signal(false);
  productUpdating = signal(false);
  ratesLoading = signal(false);
  ratesSubmitting = signal(false);
  docsSubmitting = signal(false);
  statusUpdatingId = signal<string | null>(null);
  saleUpdatingId = signal<string | null>(null);

  // Sensible per-domain starting points for a new product's underwriting envelope — Motor cover
  // is typically 1-3 years with no waiting period, Health is annually renewable with a 30-day
  // waiting period, Life runs much longer with no waiting period. Admins can still edit any value.
  private readonly domainDefaults: Record<string, { minAge: number; maxAge: number; minSumAssured: number; maxSumAssured: number; minTenureYears: number; maxTenureYears: number; waitingPeriodDays: number }> = {
    Motor: { minAge: 18, maxAge: 70, minSumAssured: 100000, maxSumAssured: 2000000, minTenureYears: 1, maxTenureYears: 3, waitingPeriodDays: 0 },
    Health: { minAge: 18, maxAge: 65, minSumAssured: 100000, maxSumAssured: 5000000, minTenureYears: 1, maxTenureYears: 10, waitingPeriodDays: 30 },
    Life: { minAge: 18, maxAge: 60, minSumAssured: 100000, maxSumAssured: 5000000, minTenureYears: 5, maxTenureYears: 30, waitingPeriodDays: 0 },
  };

  createForm = { productName: '', domain: 'Motor', description: '', ...this.domainDefaults['Motor'], allowsFamilyFloater: false, maxFamilyMembers: 1, motorVehicleType: 'TwoWheeler' as string | null };
  coverageOptionsText = '300000, 500000, 1000000, 1500000';
  lifeSumAssuredIncrement = 100000;
  editCoverageOptionsText = '';
  editLifeSumAssuredIncrement = 100000;
  editForm: UpdateProductRequest = {
    productName: '', description: '', minAge: 18, maxAge: 65,
    minSumAssured: 100000, maxSumAssured: 5000000, minTenureYears: 1,
    maxTenureYears: 10, waitingPeriodDays: 0, allowsFamilyFloater: false,
    maxFamilyMembers: 1, motorVehicleType: null,
  };

  currentPage = signal(1);
  readonly pageSize = 10;

  filteredProducts = computed(() => {
    const q = this.searchQuery().toLowerCase();
    return q ? this.products().filter(p => p.productName.toLowerCase().includes(q) || p.domain.toLowerCase().includes(q)) : this.products();
  });

  totalPages = computed(() => Math.max(1, Math.ceil(this.filteredProducts().length / this.pageSize)));
  pagedProducts = computed(() => {
    const start = (this.currentPage() - 1) * this.pageSize;
    return this.filteredProducts().slice(start, start + this.pageSize);
  });

  onSearch(val: string): void { this.searchQuery.set(val); this.currentPage.set(1); }
  onPageChange(page: number): void { this.currentPage.set(page); }

  totalProducts = computed(() => this.products().length);
  activeProducts = computed(() => this.products().filter(p => p.isActive).length);
  onSaleProducts = computed(() => this.products().filter(p => p.isActive && p.isAvailableForSale).length);
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
    this.adminService.getAdminProducts().subscribe({
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

  onDomainChange(domain: string): void {
    Object.assign(this.createForm, this.domainDefaults[domain] ?? this.domainDefaults['Motor']);
    this.createForm.motorVehicleType = domain === 'Motor' ? 'TwoWheeler' : null;
    if (domain !== 'Health') {
      this.createForm.allowsFamilyFloater = false;
      this.createForm.maxFamilyMembers = 1;
    }
    if (domain === 'Health') this.coverageOptionsText = '300000, 500000, 1000000, 1500000';
    if (domain === 'Life') this.lifeSumAssuredIncrement = 100000;
  }

  openCreateModal(): void {
    this.createForm = { productName: '', domain: 'Motor', description: '', ...this.domainDefaults['Motor'], allowsFamilyFloater: false, maxFamilyMembers: 1, motorVehicleType: 'TwoWheeler' };
    this.activeModal.set('create');
  }

  openActionMenu(product: ProductDto): void {
    this.actionMenuProduct.set(product);
  }

  closeActionMenu(): void {
    this.actionMenuProduct.set(null);
  }

  manageProductDetails(): void {
    const product = this.actionMenuProduct();
    if (!product) return;
    this.closeActionMenu();
    this.openEditProductModal(product);
  }

  manageProductRates(): void {
    const product = this.actionMenuProduct();
    if (!product) return;
    this.closeActionMenu();
    this.openEditRatesModal(product);
  }

  manageProductDocuments(): void {
    const product = this.actionMenuProduct();
    if (!product) return;
    this.closeActionMenu();
    this.openEditDocsModal(product);
  }

  manageProductSaleAvailability(): void {
    const product = this.actionMenuProduct();
    if (!product) return;
    this.closeActionMenu();
    this.toggleProductSaleAvailability(product);
  }

  manageProductStatus(): void {
    const product = this.actionMenuProduct();
    if (!product) return;
    this.closeActionMenu();
    this.toggleProductStatus(product);
  }

  openEditProductModal(product: ProductDto): void {
    this.selectedProduct.set(product);
    this.editForm = {
      productName: product.productName,
      description: product.description,
      minAge: product.minAge,
      maxAge: product.maxAge,
      minSumAssured: product.minSumAssured,
      maxSumAssured: product.maxSumAssured,
      minTenureYears: product.minTenureYears,
      maxTenureYears: product.maxTenureYears,
      waitingPeriodDays: product.waitingPeriodDays,
      allowsFamilyFloater: product.allowsFamilyFloater,
      maxFamilyMembers: product.maxFamilyMembers,
      motorVehicleType: product.motorVehicleType ?? null,
    };
    this.editCoverageOptionsText = (product.coverageOptions?.length ? product.coverageOptions : [product.minSumAssured, product.maxSumAssured]).join(', ');
    this.editLifeSumAssuredIncrement = product.sumAssuredIncrement ?? 100000;
    this.activeModal.set('editProduct');
  }

  openEditRatesModal(product: ProductDto): void {
    this.selectedProduct.set(product);
    this.rateBands.set([]);
    this.ratesLoading.set(true);
    this.activeModal.set('editRates');
    this.adminService.getProductRates(product.id).subscribe({
      next: rates => {
        this.rateBands.set(rates);
        this.ratesLoading.set(false);
      },
      error: () => {
        this.ratesLoading.set(false);
        this.toastService.error('Failed to load rates');
      },
    });
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
    if (this.createSubmitting() || this.productUpdating() || this.ratesSubmitting() || this.docsSubmitting()) return;
    this.activeModal.set(null);
  }

  createProductInvalid(): boolean {
    const f = this.createForm;
    return !f.productName.trim()
      || !f.description.trim()
      || f.minAge < 1
      || f.maxAge < f.minAge
      || f.minSumAssured <= 0
      || f.maxSumAssured < f.minSumAssured
      || f.minTenureYears <= 0
      || f.maxTenureYears < f.minTenureYears
      || f.waitingPeriodDays < 0
      || (f.domain === 'Motor' && !f.motorVehicleType)
      || (f.domain === 'Health' && this.parseCoverageOptions(this.coverageOptionsText).length === 0)
      || (f.domain === 'Life' && this.lifeSumAssuredIncrement <= 0)
      || (f.allowsFamilyFloater && f.maxFamilyMembers < 2);
  }

  createProduct(): void {
    if (this.createSubmitting() || this.createProductInvalid()) return;
    this.createSubmitting.set(true);
    this.adminService.createProduct({
      ...this.createForm,
      coverageOptions: this.createForm.domain === 'Health' ? this.parseCoverageOptions(this.coverageOptionsText) : undefined,
      sumAssuredIncrement: this.createForm.domain === 'Life' ? this.lifeSumAssuredIncrement : undefined,
    } as any).subscribe({
      next: product => {
        this.createSubmitting.set(false);
        this.toastService.success(`Product created — ${product.uin}`);
        this.closeModal();
        this.loadProducts();
      },
      error: () => {
        this.createSubmitting.set(false);
        this.toastService.error('Failed to create product');
      },
    });
  }

  editProductInvalid(): boolean {
    const f = this.editForm;
    const product = this.selectedProduct();
    return !f.productName.trim()
      || !f.description.trim()
      || f.minAge < 1
      || f.maxAge < f.minAge
      || f.minSumAssured <= 0
      || f.maxSumAssured < f.minSumAssured
      || f.minTenureYears <= 0
      || f.maxTenureYears < f.minTenureYears
      || f.waitingPeriodDays < 0
      || (product?.domain === 'Motor' && !f.motorVehicleType)
      || (product?.domain === 'Health' && this.parseCoverageOptions(this.editCoverageOptionsText).length === 0)
      || (product?.domain === 'Life' && this.editLifeSumAssuredIncrement <= 0)
      || (f.allowsFamilyFloater && f.maxFamilyMembers < 2);
  }

  saveProductDetails(): void {
    const product = this.selectedProduct();
    if (!product || this.productUpdating() || this.editProductInvalid()) return;
    this.productUpdating.set(true);
    this.adminService.updateProduct(product.id, {
      ...this.editForm,
      coverageOptions: product.domain === 'Health' ? this.parseCoverageOptions(this.editCoverageOptionsText) : undefined,
      sumAssuredIncrement: product.domain === 'Life' ? this.editLifeSumAssuredIncrement : undefined,
    }).subscribe({
      next: updated => {
        this.products.update(list => list.map(p => p.id === updated.id ? updated : p));
        this.selectedProduct.set(updated);
        this.productUpdating.set(false);
        this.toastService.success('Product details updated');
        this.closeModal();
      },
      error: err => {
        this.productUpdating.set(false);
        this.toastService.error(err?.error?.title ?? err?.error?.message ?? 'Failed to update product details');
      },
    });
  }

  private parseCoverageOptions(value: string): number[] {
    return value.split(',').map(option => Number(option.trim())).filter(option => Number.isFinite(option) && option > 0);
  }

  toggleProductStatus(product: ProductDto): void {
    if (this.statusUpdatingId()) return;
    const next = !product.isActive;
    this.statusUpdatingId.set(product.id);
    this.adminService.toggleProductStatus(product.id, next).subscribe({
      next: () => {
        this.products.update(list => list.map(p => p.id === product.id ? { ...p, isActive: next } : p));
        this.toastService.success(product.productName + (next ? ' activated' : ' deactivated'));
        this.statusUpdatingId.set(null);
      },
      error: err => {
        this.statusUpdatingId.set(null);
        this.toastService.error(err?.error?.title ?? err?.error?.message ?? 'Failed to update product status');
      },
    });
  }

  toggleProductSaleAvailability(product: ProductDto): void {
    if (this.saleUpdatingId()) return;
    const next = !product.isAvailableForSale;
    this.saleUpdatingId.set(product.id);
    this.adminService.toggleProductSaleAvailability(product.id, next).subscribe({
      next: () => {
        this.products.update(list => list.map(p => p.id === product.id ? { ...p, isAvailableForSale: next } : p));
        this.toastService.success(product.productName + (next ? ' restored to sale' : ' withdrawn from sale'));
        this.saleUpdatingId.set(null);
      },
      error: err => {
        this.saleUpdatingId.set(null);
        this.toastService.error(err?.error?.title ?? err?.error?.message ?? 'Failed to update sale availability');
      },
    });
  }

  isMotorProduct(): boolean {
    return this.selectedProduct()?.domain.toUpperCase() === 'MOTOR';
  }

  addRateBand(): void {
    // Motor isn't age-rated (see backend ProposalService.IsAgeRatedDomain) — rate lookup for
    // Motor products matches on sum assured alone, so age bands are hidden in the UI and
    // auto-filled with a full-range placeholder rather than asking the admin to fill them in.
    const ageDefaults = this.isMotorProduct() ? { ageMin: 0, ageMax: 150 } : { ageMin: 0, ageMax: 0 };
    this.rateBands.update(bands => {
      const previousMax = this.highestValidPreviousSumMax(bands.length, bands);
      return [...bands, { ...ageDefaults, sumAssuredMin: previousMax === null ? 0 : previousMax + 1, sumAssuredMax: 0, annualPremium: 0 }];
    });
  }

  removeRateBand(index: number): void {
    this.rateBands.update(bands => bands.filter((_, i) => i !== index));
  }

  updateRateBand(index: number, field: string, event: Event): void {
    const val = +(event.target as HTMLInputElement).value;
    this.rateBands.update(bands => bands.map((b, i) => i === index ? { ...b, [field]: val } : b));
  }

  ratesInvalid(): boolean {
    const bands = this.rateBands();
    return bands.length === 0 || bands.some(b =>
      b.ageMin < 0
      || b.ageMax < b.ageMin
      || b.sumAssuredMin <= 0
      || b.sumAssuredMax < b.sumAssuredMin
      || b.annualPremium <= 0)
      || this.rateBandCollisionMessage() !== null;
  }

  rateValidationMessage(): string | null {
    const bands = this.rateBands();
    if (bands.length === 0) return 'Add at least one rate band.';
    if (bands.some(b => b.ageMin < 0 || b.ageMax < b.ageMin))
      return 'Age ranges must be valid.';
    const sumRangeMessage = this.sumRangeValidationMessage(bands);
    if (sumRangeMessage) return sumRangeMessage;
    if (bands.some(b => b.annualPremium <= 0))
      return 'Annual premium must be greater than 0.';
    return this.rateBandCollisionMessage();
  }

  private sumRangeValidationMessage(bands: PremiumRateDto[]): string | null {
    for (let index = 0; index < bands.length; index++) {
      const band = bands[index];
      const previousMax = this.highestValidPreviousSumMax(index, bands);
      const suggestedMin = previousMax === null ? 1 : previousMax + 1;
      const bandLabel = `Rate band ${index + 1}`;

      if (band.sumAssuredMin <= 0) {
        return `${bandLabel} needs a Sum Min of ${suggestedMin} or higher.`;
      }

      if (band.sumAssuredMax < band.sumAssuredMin) {
        if (band.sumAssuredMin < suggestedMin) {
          return `${bandLabel} has an invalid sum range. Set Sum Min to ${suggestedMin} or higher, and Sum Max to at least the Sum Min.`;
        }

        if (previousMax !== null) {
          return `${bandLabel} has an invalid sum range. Sum Max must be ${band.sumAssuredMin} or higher. The next non-overlapping Sum Min is ${suggestedMin} or higher.`;
        }

        return `${bandLabel} has an invalid sum range. Sum Max must be ${band.sumAssuredMin} or higher.`;
      }
    }
    return null;
  }

  private rateBandCollisionMessage(): string | null {
    const bands = this.rateBands();
    const isMotor = this.isMotorProduct();
    for (let currentIndex = 1; currentIndex < bands.length; currentIndex++) {
      const current = bands[currentIndex];
      const previousBands = bands.slice(0, currentIndex)
        .filter(previous => isMotor || this.rangesOverlap(previous.ageMin, previous.ageMax, current.ageMin, current.ageMax));
      if (previousBands.length === 0) continue;

      const previousMax = Math.max(...previousBands.map(previous => previous.sumAssuredMax));
      const previousOverlapsCurrent = previousBands.some(previous =>
        this.rangesOverlap(previous.sumAssuredMin, previous.sumAssuredMax, current.sumAssuredMin, current.sumAssuredMax));
      if (previousOverlapsCurrent) {
        return `Rate band ${currentIndex + 1} overlaps with an earlier band. Set its Sum Min to ${previousMax + 1} or higher.`;
      }
    }
    return null;
  }

  private highestValidPreviousSumMax(index: number, bands = this.rateBands()): number | null {
    const previousMaxes = bands
      .slice(0, index)
      .filter(band => band.sumAssuredMin > 0 && band.sumAssuredMax >= band.sumAssuredMin)
      .map(band => band.sumAssuredMax);
    return previousMaxes.length === 0 ? null : Math.max(...previousMaxes);
  }

  private rangesOverlap(leftMin: number, leftMax: number, rightMin: number, rightMax: number): boolean {
    return leftMin <= rightMax && rightMin <= leftMax;
  }

  saveRates(): void {
    const p = this.selectedProduct();
    if (!p || this.ratesSubmitting() || this.ratesInvalid()) return;
    this.ratesSubmitting.set(true);
    this.adminService.updateProductRates(p.id, this.rateBands()).subscribe({
      next: () => {
        this.ratesSubmitting.set(false);
        this.toastService.success('Rates updated');
        this.closeModal();
      },
      error: () => {
        this.ratesSubmitting.set(false);
        this.toastService.error('Failed to update rates');
      },
    });
  }

  toggleDocRequired(index: number): void {
    this.productDocs.update(docs => docs.map((d, i) => i === index ? { ...d, isMandatory: !d.isMandatory } : d));
  }

  saveDocs(): void {
    const p = this.selectedProduct();
    if (!p || this.docsSubmitting()) return;
    const reqs = this.productDocs().map(d => ({
      entityType: d.entityType, domain: d.domain, documentKey: d.documentKey,
      label: d.label, description: d.description, isMandatory: d.isMandatory, isActive: d.isActive,
    }));
    this.docsSubmitting.set(true);
    this.adminService.updateProductDocuments(p.id, reqs).subscribe({
      next: () => {
        this.docsSubmitting.set(false);
        this.toastService.success('Document requirements saved');
        this.closeModal();
      },
      error: () => {
        this.docsSubmitting.set(false);
        this.toastService.error('Failed to save documents');
      },
    });
  }
}
