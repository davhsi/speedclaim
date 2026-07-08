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
    getProductRates: ReturnType<typeof vi.fn>;
    updateProductRates: ReturnType<typeof vi.fn>;
    getProductDocuments: ReturnType<typeof vi.fn>;
    updateProductDocuments: ReturnType<typeof vi.fn>;
    toggleProductStatus: ReturnType<typeof vi.fn>;
  };
  let toast: { success: ReturnType<typeof vi.fn>; error: ReturnType<typeof vi.fn>; warning: ReturnType<typeof vi.fn> };

  function product(overrides: Partial<ProductDto> = {}): ProductDto {
    return {
      id: 'p1', productName: 'SpeedCare Motor', uin: 'UIN001', description: 'Motor cover', domain: 'Motor',
      minAge: 18, maxAge: 65, minSumAssured: 100000, maxSumAssured: 500000, minTenureYears: 1, maxTenureYears: 10,
      waitingPeriodDays: 30, allowsFamilyFloater: false, maxFamilyMembers: 1, isActive: true,
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
      getAdminProducts: vi.fn(), createProduct: vi.fn(), getProductRates: vi.fn(),
      updateProductRates: vi.fn(), getProductDocuments: vi.fn(), updateProductDocuments: vi.fn(),
      toggleProductStatus: vi.fn(),
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
      expect(fixture.componentInstance.products().length).toBe(2);
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

      c.onDomainChange('Health');
      expect(c.createForm.maxTenureYears).toBe(10);
      expect(c.createForm.waitingPeriodDays).toBe(30);

      c.onDomainChange('Life');
      expect(c.createForm.minTenureYears).toBe(5);
      expect(c.createForm.maxTenureYears).toBe(30);
      expect(c.createForm.waitingPeriodDays).toBe(0);
    });
  });

  describe('createProductInvalid / createProduct', () => {
    function validForm() {
      return { productName: 'New Plan', domain: 'Motor', description: 'desc', minAge: 18, maxAge: 65, minSumAssured: 100000, maxSumAssured: 500000, minTenureYears: 1, maxTenureYears: 10, waitingPeriodDays: 30, allowsFamilyFloater: false, maxFamilyMembers: 1 };
    }

    it('is invalid when maxAge is below minAge', () => {
      const fixture = create();
      fixture.componentInstance.createForm = { ...validForm(), minAge: 50, maxAge: 30 };
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
      expect(c.rateBands().length).toBe(1);
      c.addRateBand();
      c.removeRateBand(0);
      expect(c.rateBands().length).toBe(1);
    });

    it('addRateBand defaults to a full age range for Motor products', () => {
      const fixture = create();
      const c = fixture.componentInstance;
      c.selectedProduct.set(product({ domain: 'Motor' }));

      c.addRateBand();

      expect(c.isMotorProduct()).toBe(true);
      expect(c.rateBands()).toEqual([{ ageMin: 0, ageMax: 150, sumAssuredMin: 0, sumAssuredMax: 0, annualPremium: 0 }]);
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
      expect(fixture.componentInstance.productDocs().length).toBe(1);
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
