import { vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { QuoteComponent } from './quote';
import { QuoteService } from './services/quote.service';
import { ProductService } from '../products/services/product.service';
import { ToastService } from '../../../shared/components/toast/toast.service';
import { GenerateQuoteResponse, ProductDto } from '../../../core/models/api.models';

describe('QuoteComponent', () => {
  let quoteService: { generateQuote: ReturnType<typeof vi.fn> };
  let productService: { getAll: ReturnType<typeof vi.fn> };
  let toast: { warning: ReturnType<typeof vi.fn>; error: ReturnType<typeof vi.fn> };
  let router: { navigate: ReturnType<typeof vi.fn> };

  const product: ProductDto = {
    id: 'prod1', productName: 'Health Shield', uin: 'U1', description: '', domain: 'Health',
    minAge: 18, maxAge: 65, minSumAssured: 100000, maxSumAssured: 1000000,
    minTenureYears: 1, maxTenureYears: 10, waitingPeriodDays: 30,
    allowsFamilyFloater: true, maxFamilyMembers: 5, isActive: true,
  } as ProductDto;

  const motorProduct: ProductDto = {
    id: 'prod-motor', productName: 'SpeedDrive Motor', uin: 'U2', description: '', domain: 'Motor',
    minAge: 18, maxAge: 70, minSumAssured: 100000, maxSumAssured: 2000000,
    minTenureYears: 1, maxTenureYears: 3, waitingPeriodDays: 0,
    allowsFamilyFloater: false, maxFamilyMembers: 1, isActive: true,
  } as ProductDto;

  function create(productId: string | null = null) {
    TestBed.configureTestingModule({
      imports: [QuoteComponent],
      providers: [
        { provide: QuoteService, useValue: quoteService },
        { provide: ProductService, useValue: productService },
        { provide: ToastService, useValue: toast },
        { provide: Router, useValue: router },
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: { get: (key: string) => (key === 'productId' ? productId : null) } } } },
      ],
    });
    const fixture = TestBed.createComponent(QuoteComponent);
    fixture.detectChanges();
    return fixture;
  }

  beforeEach(() => {
    quoteService = { generateQuote: vi.fn() };
    productService = { getAll: vi.fn(() => of([product])) };
    toast = { warning: vi.fn(), error: vi.fn() };
    router = { navigate: vi.fn() };
  });

  describe('ngOnInit', () => {
    it('loads products without preselecting one when there is no productId param', () => {
      const fixture = create();
      expect(fixture.componentInstance.products()).toEqual([product]);
      expect(fixture.componentInstance.selectedProduct()).toBeNull();
    });

    it('preselects the product matching the productId route param and applies its validators', () => {
      const fixture = create('prod1');
      expect(fixture.componentInstance.preselectedProduct()).toEqual(product);
      expect(fixture.componentInstance.selectedProduct()).toEqual(product);
      expect(fixture.componentInstance.form.controls.productId.value).toBe('prod1');
      expect(fixture.componentInstance.form.controls.sumAssured.hasError('min')).toBe(false);
      fixture.componentInstance.form.controls.sumAssured.setValue(50);
      expect(fixture.componentInstance.form.controls.sumAssured.hasError('min')).toBe(true);
    });

    it('does not preselect when the productId param does not match any product', () => {
      const fixture = create('missing-product');
      expect(fixture.componentInstance.preselectedProduct()).toBeNull();
    });
  });

  describe('onProductChange', () => {
    it('sets the selected product, clears any prior quote, and applies its validators', () => {
      const fixture = create();
      fixture.componentInstance.quoteResult.set({ premiumAmount: 1 } as GenerateQuoteResponse);
      fixture.componentInstance.form.patchValue({ productId: 'prod1' });

      fixture.componentInstance.onProductChange();

      expect(fixture.componentInstance.selectedProduct()).toEqual(product);
      expect(fixture.componentInstance.quoteResult()).toBeNull();
    });

    it('sets selectedProduct to null when the chosen id matches nothing', () => {
      const fixture = create();
      fixture.componentInstance.form.patchValue({ productId: 'unknown' });
      fixture.componentInstance.onProductChange();
      expect(fixture.componentInstance.selectedProduct()).toBeNull();
    });
  });

  describe('onSubmit', () => {
    function fillValidForm(fixture: ReturnType<typeof create>) {
      fixture.componentInstance.form.setValue({
        productId: 'prod1', sumAssured: 500000, tenureYears: 5, paymentFrequency: 'Monthly',
        age: 36, gender: 'Male', vehicleMake: '', vehicleModel: '',
        manufactureYear: null, insuredDeclaredValue: null, preExistingConditions: '', isSmoker: false,
      });
    }

    it('does nothing when the form is invalid', () => {
      const fixture = create();
      fixture.componentInstance.onSubmit();
      expect(quoteService.generateQuote).not.toHaveBeenCalled();
    });

    it('warns and does not submit when the form is valid but no product is selected', () => {
      const fixture = create();
      fillValidForm(fixture);
      // form is valid, but selectedProduct was never set (onProductChange was never called)
      fixture.componentInstance.onSubmit();
      expect(toast.warning).toHaveBeenCalledWith('Please select a product before calculating premium.');
      expect(quoteService.generateQuote).not.toHaveBeenCalled();
    });

    it('submits the quote request with the entered age', () => {
      const fixture = create('prod1'); // preselects product1 via ngOnInit
      fillValidForm(fixture);
      quoteService.generateQuote.mockReturnValue(of({ premiumAmount: 2000 } as GenerateQuoteResponse));

      fixture.componentInstance.onSubmit();

      expect(quoteService.generateQuote).toHaveBeenCalledWith({
        productId: 'prod1', age: 36, sumAssured: 500000, tenureYears: 5, gender: 'Male',
      });
      expect(fixture.componentInstance.quoteResult()).toEqual({ premiumAmount: 2000 });
      expect(fixture.componentInstance.submitting()).toBe(false);
    });

    it('shows a server-provided error title (or a fallback) when quote generation fails', () => {
      const fixture = create('prod1');
      fillValidForm(fixture);
      quoteService.generateQuote.mockReturnValue(throwError(() => ({ error: { title: 'Age out of range' } })));

      fixture.componentInstance.onSubmit();

      expect(toast.error).toHaveBeenCalledWith('Age out of range');
      expect(fixture.componentInstance.submitting()).toBe(false);
    });

    it('falls back to a generic error message when there is no title', () => {
      const fixture = create('prod1');
      fillValidForm(fixture);
      quoteService.generateQuote.mockReturnValue(throwError(() => ({ error: {} })));

      fixture.componentInstance.onSubmit();

      expect(toast.error).toHaveBeenCalledWith('Failed to generate quote');
    });
  });

  describe('Motor domain (not age-rated)', () => {
    beforeEach(() => {
      productService.getAll = vi.fn(() => of([product, motorProduct]));
    });

    it('clears the age requirement and value when a Motor product is selected', () => {
      const fixture = create('prod-motor');

      expect(fixture.componentInstance.form.controls.age.value).toBeNull();
      expect(fixture.componentInstance.form.controls.age.validator).toBeNull();
    });

    it('submits the quote request without an age for Motor', () => {
      const fixture = create('prod-motor');
      fixture.componentInstance.form.patchValue({
        sumAssured: 900000, tenureYears: 1, vehicleMake: 'Maruti', vehicleModel: 'Swift',
        manufactureYear: 2022, insuredDeclaredValue: 900000,
      });
      quoteService.generateQuote.mockReturnValue(of({ premiumAmount: 18000 } as GenerateQuoteResponse));

      fixture.componentInstance.onSubmit();

      expect(quoteService.generateQuote).toHaveBeenCalledWith(expect.objectContaining({
        productId: 'prod-motor', age: undefined, sumAssured: 900000, tenureYears: 1,
      }));
    });
  });

  describe('applyNow', () => {
    it('does nothing without a quote result', () => {
      const fixture = create();
      fixture.componentInstance.applyNow();
      expect(router.navigate).not.toHaveBeenCalled();
    });

    it('navigates to /proposals/new with the quote details in router state', () => {
      const fixture = create('prod1');
      fixture.componentInstance.quoteResult.set({
        premiumAmount: 2000, paymentFrequency: 'Monthly', sumAssured: 500000, tenureYears: 5,
      });

      fixture.componentInstance.applyNow();

      expect(router.navigate).toHaveBeenCalledWith(['/proposals/new'], {
        state: {
          productId: 'prod1', sumAssured: 500000, tenureYears: 5, premiumAmount: 2000, paymentFrequency: 'Monthly',
        },
      });
    });
  });
});
