import { vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { AdminProductsComponent } from './admin-products';
import { AdminService } from '../services/admin.service';
import { ToastService } from '../../../shared/components/toast/toast.service';
import { ProductDto, DocumentRequirementResponseDto, PremiumRateDto } from '../../../core/models/api.models';

describe('AdminProductsComponent', () => {
  let adminService: {
    getAdminProducts: ReturnType<typeof vi.fn>;
    createProduct: ReturnType<typeof vi.fn>;
    updateProduct: ReturnType<typeof vi.fn>;
    getProductRates: ReturnType<typeof vi.fn>;
    updateProductRates: ReturnType<typeof vi.fn>;
    getProductDocuments: ReturnType<typeof vi.fn>;
    updateProductDocuments: ReturnType<typeof vi.fn>;
    toggleProductStatus: ReturnType<typeof vi.fn>;
    toggleProductSaleAvailability: ReturnType<typeof vi.fn>;
  };
  let toast: { success: ReturnType<typeof vi.fn>; error: ReturnType<typeof vi.fn>; warning: ReturnType<typeof vi.fn> };

  function product(overrides: Partial<ProductDto> = {}): ProductDto {
    return {
      id: 'p1', productName: 'SpeedCare Motor', uin: 'UIN001', description: 'Motor cover', domain: 'Motor',
      minAge: 18, maxAge: 65, minSumAssured: 100000, maxSumAssured: 500000, minTenureYears: 1, maxTenureYears: 10,
      waitingPeriodDays: 30, allowsFamilyFloater: false, maxFamilyMembers: 1, isActive: true, isAvailableForSale: true,
      ...overrides,
    };
  }

  function create(products: ProductDto[] = [product()]) {
    adminService.getAdminProducts.mockReturnValue(of(products));
    const fixture = TestBed.createComponent(AdminProductsComponent);
    fixture.detectChanges();
    return fixture;
  }

  beforeEach(() => {
    adminService = {
      getAdminProducts: vi.fn(), createProduct: vi.fn(), updateProduct: vi.fn(), getProductRates: vi.fn(),
      updateProductRates: vi.fn(), getProductDocuments: vi.fn(), updateProductDocuments: vi.fn(),
      toggleProductStatus: vi.fn(), toggleProductSaleAvailability: vi.fn(),
    };
    toast = { success: vi.fn(), error: vi.fn(), warning: vi.fn() };

    TestBed.configureTestingModule({
      imports: [AdminProductsComponent],
      providers: [
        { provide: AdminService, useValue: adminService },
        { provide: ToastService, useValue: toast },
      ],
    });
  });

  describe('loadProducts', () => {
    it('loads products and clears the loading flag', () => {
      const fixture = create([product({ id: 'p1' }), product({ id: 'p2' })]);
      expect(fixture.componentInstance.products()).toHaveLength(2);
      expect(fixture.componentInstance.loading()).toBe(false);
    });

    it('clears the loading flag even on error', () => {
      adminService.getAdminProducts.mockReturnValue(throwError(() => ({ status: 500 })));
      const fixture = TestBed.createComponent(AdminProductsComponent);
      fixture.detectChanges();
      expect(fixture.componentInstance.loading()).toBe(false);
    });
  });

  describe('filteredProducts and stats', () => {
    it('filters by product name or domain', () => {
      const fixture = create([product({ id: 'p1', productName: 'SpeedCare Motor', domain: 'Motor' }), product({ id: 'p2', productName: 'SpeedCare Health', domain: 'Health' })]);
      fixture.componentInstance.searchQuery.set('health');
      expect(fixture.componentInstance.filteredProducts().map(p => p.id)).toEqual(['p2']);
    });

    it('computes total/active/motor/inactive counts', () => {
      const fixture = create([
        product({ id: 'p1', domain: 'Motor', isActive: true }),
        product({ id: 'p2', domain: 'Health', isActive: false }),
      ]);
      const c = fixture.componentInstance;
      expect(c.totalProducts()).toBe(2);
      expect(c.activeProducts()).toBe(1);
      expect(c.motorCount()).toBe(1);
      expect(c.inactiveCount()).toBe(1);
    });
  });

  describe('onDomainChange', () => {
    it('resets family-floater fields when domain is not Health', () => {
      const fixture = create();
      const c = fixture.componentInstance;
      c.createForm.allowsFamilyFloater = true;
      c.createForm.maxFamilyMembers = 4;
      c.onDomainChange('Motor');
      expect(c.createForm.allowsFamilyFloater).toBe(false);
      expect(c.createForm.maxFamilyMembers).toBe(1);
    });

    it('leaves family-floater fields untouched for Health', () => {
      const fixture = create();
      const c = fixture.componentInstance;
      c.createForm.allowsFamilyFloater = true;
      c.createForm.maxFamilyMembers = 4;
      c.onDomainChange('Health');
      expect(c.createForm.allowsFamilyFloater).toBe(true);
      expect(c.createForm.maxFamilyMembers).toBe(4);
    });

    it('applies domain-specific underwriting defaults on switch', () => {
      const fixture = create();
      const c = fixture.componentInstance;

      c.onDomainChange('Motor');
      expect(c.createForm.maxTenureYears).toBe(3);
      expect(c.createForm.waitingPeriodDays).toBe(0);
      expect(c.createForm.motorVehicleType).toBe('TwoWheeler');

      c.onDomainChange('Health');
      expect(c.createForm.maxTenureYears).toBe(10);
      expect(c.createForm.waitingPeriodDays).toBe(30);
      expect(c.createForm.motorVehicleType).toBeNull();

      c.onDomainChange('Life');
      expect(c.createForm.minTenureYears).toBe(5);
      expect(c.createForm.maxTenureYears).toBe(30);
      expect(c.createForm.waitingPeriodDays).toBe(0);
    });
  });

  describe('createProductInvalid / createProduct', () => {
    function validForm() {
      return { productName: 'New Plan', domain: 'Motor', description: 'desc', minAge: 18, maxAge: 65, minSumAssured: 100000, maxSumAssured: 500000, minTenureYears: 1, maxTenureYears: 10, waitingPeriodDays: 30, allowsFamilyFloater: false, maxFamilyMembers: 1, motorVehicleType: 'TwoWheeler' };
    }

    it('is invalid when maxAge is below minAge', () => {
      const fixture = create();
      fixture.componentInstance.createForm = { ...validForm(), minAge: 50, maxAge: 30 };
      expect(fixture.componentInstance.createProductInvalid()).toBe(true);
    });

    it('is invalid when minAge is below the backend minimum', () => {
      const fixture = create();
      fixture.componentInstance.createForm = { ...validForm(), minAge: 0 };
      expect(fixture.componentInstance.createProductInvalid()).toBe(true);
    });

    it('is invalid when family floater is allowed but maxFamilyMembers < 2', () => {
      const fixture = create();
      fixture.componentInstance.createForm = { ...validForm(), allowsFamilyFloater: true, maxFamilyMembers: 1 };
      expect(fixture.componentInstance.createProductInvalid()).toBe(true);
    });

    it('is valid for a well-formed form', () => {
      const fixture = create();
      fixture.componentInstance.createForm = validForm();
      expect(fixture.componentInstance.createProductInvalid()).toBe(false);
    });

    it('does not submit an invalid form', () => {
      const fixture = create();
      fixture.componentInstance.createForm = { ...validForm(), productName: '' };
      fixture.componentInstance.createProduct();
      expect(adminService.createProduct).not.toHaveBeenCalled();
    });

    it('creates the product, toasts with the UIN, closes the modal, and reloads', () => {
      const fixture = create();
      const c = fixture.componentInstance;
      c.createForm = validForm();
      adminService.createProduct.mockReturnValue(of(product({ uin: 'UIN999' })));

      c.createProduct();

      expect(toast.success).toHaveBeenCalledWith('Product created — UIN999');
      expect(c.activeModal()).toBeNull();
      expect(adminService.getAdminProducts).toHaveBeenCalledTimes(2);
    });

    it('shows an error toast on failure', () => {
      const fixture = create();
      const c = fixture.componentInstance;
      c.createForm = validForm();
      adminService.createProduct.mockReturnValue(throwError(() => ({ status: 500 })));
      c.createProduct();
      expect(toast.error).toHaveBeenCalledWith('Failed to create product');
    });
  });

  describe('toggleProductStatus', () => {
    it('flips isActive and toasts success', () => {
      const fixture = create([product({ id: 'p1', isActive: true, productName: 'Motor Plan' })]);
      const c = fixture.componentInstance;
      adminService.toggleProductStatus.mockReturnValue(of({ message: 'ok' }));

      c.toggleProductStatus(product({ id: 'p1', isActive: true, productName: 'Motor Plan' }));

      expect(adminService.toggleProductStatus).toHaveBeenCalledWith('p1', false);
      expect(c.products().find(p => p.id === 'p1')?.isActive).toBe(false);
      expect(toast.success).toHaveBeenCalledWith('Motor Plan deactivated');
    });

    it('ignores a second call while one is already in flight', () => {
      const fixture = create([product({ id: 'p1' })]);
      const c = fixture.componentInstance;
      c.statusUpdatingId.set('p1');
      c.toggleProductStatus(product({ id: 'p1' }));
      expect(adminService.toggleProductStatus).not.toHaveBeenCalled();
    });

    it('shows the server reason when product deactivation is blocked', () => {
      const fixture = create([product({ id: 'p1', isActive: true })]);
      const c = fixture.componentInstance;
      adminService.toggleProductStatus.mockReturnValue(throwError(() => ({ error: { title: 'Cannot deactivate this product because it has live policies.' } })));

      c.toggleProductStatus(product({ id: 'p1', isActive: true }));

      expect(toast.error).toHaveBeenCalledWith('Cannot deactivate this product because it has live policies.');
      expect(c.statusUpdatingId()).toBeNull();
    });
  });

  describe('product details', () => {
    it('opens details with the current values and saves the edited product', () => {
      const fixture = create([product({ id: 'p1', productName: 'SpeedTest Health', minAge: 1, maxAge: 10, domain: 'Health' })]);
      const c = fixture.componentInstance;
      const updated = product({ id: 'p1', productName: 'SpeedTest Health', minAge: 18, maxAge: 65, domain: 'Health' });
      adminService.updateProduct.mockReturnValue(of(updated));

      c.openEditProductModal(product({ id: 'p1', productName: 'SpeedTest Health', minAge: 1, maxAge: 10, domain: 'Health' }));
      c.editForm.minAge = 18;
      c.editForm.maxAge = 65;
      c.saveProductDetails();

      expect(c.activeModal()).toBeNull();
      expect(adminService.updateProduct).toHaveBeenCalledWith('p1', expect.objectContaining({ minAge: 18, maxAge: 65 }));
      expect(c.products().find(p => p.id === 'p1')?.minAge).toBe(18);
      expect(toast.success).toHaveBeenCalledWith('Product details updated');
    });
  });

  describe('product action panel', () => {
    it('opens a named product panel and routes configuration choices through the existing flows', () => {
      const fixture = create([product({ id: 'p1', productName: 'Motor Plan' })]);
      const c = fixture.componentInstance;
      const selected = c.products()[0];
      const detailsSpy = vi.spyOn(c, 'openEditProductModal');

      c.openActionMenu(selected);
      expect(c.actionMenuProduct()).toBe(selected);

      c.manageProductDetails();
      expect(c.actionMenuProduct()).toBeNull();
      expect(detailsSpy).toHaveBeenCalledWith(selected);
    });
  });

  describe('toggleProductSaleAvailability', () => {
    it('withdraws a product from sale without deactivating it', () => {
      const fixture = create([product({ id: 'p1', isActive: true, isAvailableForSale: true, productName: 'Motor Plan' })]);
      const c = fixture.componentInstance;
      adminService.toggleProductSaleAvailability.mockReturnValue(of({ message: 'ok' }));

      c.toggleProductSaleAvailability(product({ id: 'p1', isActive: true, isAvailableForSale: true, productName: 'Motor Plan' }));

      expect(adminService.toggleProductSaleAvailability).toHaveBeenCalledWith('p1', false);
      expect(c.products().find(p => p.id === 'p1')?.isActive).toBe(true);
      expect(c.products().find(p => p.id === 'p1')?.isAvailableForSale).toBe(false);
      expect(toast.success).toHaveBeenCalledWith('Motor Plan withdrawn from sale');
    });
  });

  describe('rate bands', () => {
    it('openEditRatesModal loads rates and toggles ratesLoading', () => {
      const fixture = create([product({ id: 'p1' })]);
      const rates: PremiumRateDto[] = [{ ageMin: 18, ageMax: 30, sumAssuredMin: 100000, sumAssuredMax: 200000, annualPremium: 5000 }];
      adminService.getProductRates.mockReturnValue(of(rates));

      fixture.componentInstance.openEditRatesModal(product({ id: 'p1' }));

      expect(fixture.componentInstance.rateBands()).toEqual(rates);
      expect(fixture.componentInstance.ratesLoading()).toBe(false);
    });

    it('shows an error toast when rates fail to load', () => {
      const fixture = create();
      adminService.getProductRates.mockReturnValue(throwError(() => ({ status: 500 })));
      fixture.componentInstance.openEditRatesModal(product());
      expect(toast.error).toHaveBeenCalledWith('Failed to load rates');
    });

    it('addRateBand appends a zeroed band, removeRateBand removes by index', () => {
      const fixture = create();
      const c = fixture.componentInstance;
      c.addRateBand();
      expect(c.rateBands()).toHaveLength(1);
      c.addRateBand();
      c.removeRateBand(0);
      expect(c.rateBands()).toHaveLength(1);
    });

    it('addRateBand defaults to a full age range for Motor products', () => {
      const fixture = create();
      const c = fixture.componentInstance;
      c.selectedProduct.set(product({ domain: 'Motor' }));

      c.addRateBand();

      expect(c.isMotorProduct()).toBe(true);
      expect(c.rateBands()).toEqual([{ ageMin: 0, ageMax: 150, sumAssuredMin: 0, sumAssuredMax: 0, annualPremium: 0 }]);
    });

    it('addRateBand starts the next band after the previous valid sum max', () => {
      const fixture = create();
      const c = fixture.componentInstance;
      c.selectedProduct.set(product({ domain: 'Motor' }));
      c.rateBands.set([{ ageMin: 0, ageMax: 150, sumAssuredMin: 30000, sumAssuredMax: 50000, annualPremium: 1200 }]);

      c.addRateBand();

      expect(c.rateBands()[1]).toEqual({ ageMin: 0, ageMax: 150, sumAssuredMin: 50001, sumAssuredMax: 0, annualPremium: 0 });
    });

    it('addRateBand leaves a zeroed age range for age-rated products', () => {
      const fixture = create();
      const c = fixture.componentInstance;
      c.selectedProduct.set(product({ domain: 'Health' }));

      c.addRateBand();

      expect(c.isMotorProduct()).toBe(false);
      expect(c.rateBands()).toEqual([{ ageMin: 0, ageMax: 0, sumAssuredMin: 0, sumAssuredMax: 0, annualPremium: 0 }]);
    });

    it('ratesInvalid is true for an empty band list or a malformed band', () => {
      const fixture = create();
      const c = fixture.componentInstance;
      expect(c.ratesInvalid()).toBe(true); // empty
      c.rateBands.set([{ ageMin: 30, ageMax: 20, sumAssuredMin: 100, sumAssuredMax: 200, annualPremium: 10 }]);
      expect(c.ratesInvalid()).toBe(true); // ageMax < ageMin
    });

    it('ratesInvalid rejects overlapping motor sum bands but accepts adjacent bands', () => {
      const fixture = create();
      const c = fixture.componentInstance;
      c.selectedProduct.set(product({ domain: 'Motor' }));

      c.rateBands.set([
        { ageMin: 0, ageMax: 150, sumAssuredMin: 30000, sumAssuredMax: 50000, annualPremium: 1200 },
        { ageMin: 0, ageMax: 150, sumAssuredMin: 50000, sumAssuredMax: 75000, annualPremium: 1800 },
      ]);
      expect(c.ratesInvalid()).toBe(true);
      expect(c.rateValidationMessage()).toContain('Set its Sum Min to 50001 or higher.');

      c.rateBands.set([
        { ageMin: 0, ageMax: 150, sumAssuredMin: 30000, sumAssuredMax: 50000, annualPremium: 1200 },
        { ageMin: 0, ageMax: 150, sumAssuredMin: 50001, sumAssuredMax: 75000, annualPremium: 1800 },
      ]);
      expect(c.ratesInvalid()).toBe(false);
      expect(c.rateValidationMessage()).toBeNull();
    });

    it('rateValidationMessage suggests fixing the later displayed band when its min is lowered', () => {
      const fixture = create();
      const c = fixture.componentInstance;
      c.selectedProduct.set(product({ domain: 'Motor' }));

      c.rateBands.set([
        { ageMin: 0, ageMax: 150, sumAssuredMin: 30000, sumAssuredMax: 50000, annualPremium: 1200 },
        { ageMin: 0, ageMax: 150, sumAssuredMin: 50001, sumAssuredMax: 75000, annualPremium: 1800 },
        { ageMin: 0, ageMax: 150, sumAssuredMin: 75001, sumAssuredMax: 100000, annualPremium: 2400 },
        { ageMin: 0, ageMax: 150, sumAssuredMin: 1000, sumAssuredMax: 150000, annualPremium: 3200 },
      ]);

      expect(c.rateValidationMessage()).toBe('Rate band 4 overlaps with an earlier band. Set its Sum Min to 100001 or higher.');
    });

    it('rateValidationMessage explains the next valid start for an incomplete later band', () => {
      const fixture = create();
      const c = fixture.componentInstance;
      c.selectedProduct.set(product({ domain: 'Motor' }));

      c.rateBands.set([
        { ageMin: 0, ageMax: 150, sumAssuredMin: 30000, sumAssuredMax: 50000, annualPremium: 1200 },
        { ageMin: 0, ageMax: 150, sumAssuredMin: 500001, sumAssuredMax: 0, annualPremium: 0 },
      ]);

      expect(c.rateValidationMessage()).toBe('Rate band 2 has an invalid sum range. Sum Max must be 500001 or higher. The next non-overlapping Sum Min is 50001 or higher.');
    });

    it('saveRates saves valid bands and closes the modal', () => {
      const fixture = create([product({ id: 'p1' })]);
      const c = fixture.componentInstance;
      c.selectedProduct.set(product({ id: 'p1' }));
      c.rateBands.set([{ ageMin: 18, ageMax: 30, sumAssuredMin: 100000, sumAssuredMax: 200000, annualPremium: 5000 }]);
      adminService.updateProductRates.mockReturnValue(of({ message: 'ok' }));

      c.saveRates();

      expect(adminService.updateProductRates).toHaveBeenCalledWith('p1', c.rateBands());
      expect(toast.success).toHaveBeenCalledWith('Rates updated');
    });
  });

  describe('document requirements', () => {
    function doc(overrides: Partial<DocumentRequirementResponseDto> = {}): DocumentRequirementResponseDto {
      return { id: 'd1', entityType: 'Proposal', domain: 'Motor', documentKey: 'rcBook', label: 'RC Book', description: 'Vehicle RC', isMandatory: true, isActive: true, ...overrides };
    }

    it('openEditDocsModal loads documents', () => {
      const fixture = create();
      adminService.getProductDocuments.mockReturnValue(of([doc()]));
      fixture.componentInstance.openEditDocsModal(product());
      expect(fixture.componentInstance.productDocs()).toHaveLength(1);
      expect(fixture.componentInstance.activeModal()).toBe('editDocs');
    });

    it('falls back to an empty list (no toast) when documents fail to load', () => {
      const fixture = create();
      adminService.getProductDocuments.mockReturnValue(throwError(() => ({ status: 500 })));
      fixture.componentInstance.openEditDocsModal(product());
      expect(fixture.componentInstance.productDocs()).toEqual([]);
      expect(toast.error).not.toHaveBeenCalled();
    });

    it('toggleDocRequired flips isMandatory at the given index', () => {
      const fixture = create();
      const c = fixture.componentInstance;
      c.productDocs.set([doc({ isMandatory: true })]);
      c.toggleDocRequired(0);
      expect(c.productDocs()[0].isMandatory).toBe(false);
    });

    it('saveDocs maps and saves the document requirements', () => {
      const fixture = create([product({ id: 'p1' })]);
      const c = fixture.componentInstance;
      c.selectedProduct.set(product({ id: 'p1' }));
      c.productDocs.set([doc()]);
      adminService.updateProductDocuments.mockReturnValue(of({ message: 'ok' }));

      c.saveDocs();

      expect(adminService.updateProductDocuments).toHaveBeenCalledWith('p1', [
        { entityType: 'Proposal', domain: 'Motor', documentKey: 'rcBook', label: 'RC Book', description: 'Vehicle RC', isMandatory: true, isActive: true },
      ]);
      expect(toast.success).toHaveBeenCalledWith('Document requirements saved');
    });
  });
});
