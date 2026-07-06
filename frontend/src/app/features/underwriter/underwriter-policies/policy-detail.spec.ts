import { vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { PolicyDetailComponent } from './policy-detail';
import { UnderwriterService } from '../services/underwriter.service';
import { ProductService } from '../../portal/products/services/product.service';
import { PolicyDto, PolicyStatusHistoryDto, ProductDto } from '../../../core/models/api.models';

describe('PolicyDetailComponent', () => {
  let uwService: { getPolicyById: ReturnType<typeof vi.fn>; getPolicyHistory: ReturnType<typeof vi.fn> };
  let productService: { getById: ReturnType<typeof vi.fn> };
  let router: { navigate: ReturnType<typeof vi.fn> };

  const policy: PolicyDto = {
    id: 'pol1', policyNumber: 'PN1', userId: 'u1', productId: 'prod1', productName: 'Fallback Product',
    status: 'Active', paymentFrequency: 'Annually', premiumAmount: 1000, coverageAmount: 100000,
    currency: 'INR', startDate: '2026-01-01', endDate: '2027-01-01', domain: 'Motor', type: 'Comprehensive',
  };

  function create(id = 'pol1') {
    TestBed.configureTestingModule({
      imports: [PolicyDetailComponent],
      providers: [
        { provide: UnderwriterService, useValue: uwService },
        { provide: ProductService, useValue: productService },
        { provide: Router, useValue: router },
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: new Map([['id', id]]) as unknown as { get: (k: string) => string } } } },
      ],
    });
    const fixture = TestBed.createComponent(PolicyDetailComponent);
    fixture.detectChanges();
    return fixture;
  }

  beforeEach(() => {
    uwService = { getPolicyById: vi.fn(() => of(policy)), getPolicyHistory: vi.fn(() => of([])) };
    productService = { getById: vi.fn(() => of({ productName: 'Comprehensive Motor Cover', domain: 'Motor' } as ProductDto)) };
    router = { navigate: vi.fn() };
  });

  describe('ngOnInit', () => {
    it('loads the policy by route id, then its product, and its history', () => {
      const fixture = create('pol1');
      expect(uwService.getPolicyById).toHaveBeenCalledWith('pol1');
      expect(productService.getById).toHaveBeenCalledWith('prod1');
      expect(uwService.getPolicyHistory).toHaveBeenCalledWith('pol1');
      expect(fixture.componentInstance.policy()).toEqual(policy);
    });

    it('sets product to null when the product lookup fails, without breaking the page', () => {
      productService.getById.mockReturnValue(throwError(() => ({ status: 404 })));
      const fixture = create();
      expect(fixture.componentInstance.product()).toBeNull();
      expect(fixture.componentInstance.policy()).toEqual(policy);
    });

    it('stores the returned status history', () => {
      const history: PolicyStatusHistoryDto[] = [{ id: 'h1' } as PolicyStatusHistoryDto];
      uwService.getPolicyHistory.mockReturnValue(of(history));
      const fixture = create();
      expect(fixture.componentInstance.history()).toEqual(history);
    });
  });

  describe('getDotClass', () => {
    it('maps known statuses to color classes', () => {
      const fixture = create();
      expect(fixture.componentInstance.getDotClass('Active')).toBe('bg-success');
      expect(fixture.componentInstance.getDotClass('Pending')).toBe('bg-warning');
      expect(fixture.componentInstance.getDotClass('Lapsed')).toBe('bg-danger');
    });

    it('falls back to bg-info for an unknown status', () => {
      const fixture = create();
      expect(fixture.componentInstance.getDotClass('Unknown')).toBe('bg-info');
    });
  });

  describe('productName / displayDomain', () => {
    it('prefers the fetched product name/domain over the policy fallback', () => {
      const fixture = create();
      expect(fixture.componentInstance.productName()).toBe('Comprehensive Motor Cover');
      expect(fixture.componentInstance.displayDomain()).toBe('Motor');
    });

    it('falls back to the policy fields when the product could not be loaded', () => {
      productService.getById.mockReturnValue(throwError(() => ({ status: 404 })));
      const fixture = create();
      expect(fixture.componentInstance.productName()).toBe('Fallback Product');
      expect(fixture.componentInstance.displayDomain()).toBe('Motor');
    });
  });

  describe('goBack', () => {
    it('navigates to the underwriter policies list', () => {
      const fixture = create();
      fixture.componentInstance.goBack();
      expect(router.navigate).toHaveBeenCalledWith(['/underwriter/policies']);
    });
  });
});
