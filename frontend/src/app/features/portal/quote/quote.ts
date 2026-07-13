import { Component, inject, OnInit, signal } from '@angular/core';
import { LowerCasePipe } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { QuoteService } from './services/quote.service';
import { ProductService } from '../products/services/product.service';
import { ProductDto, GenerateQuoteRequest, GenerateQuoteResponse } from '../../../core/models/api.models';
import { MoneyPipe } from '../../../shared/pipes/money.pipe';
import { ToastService } from '../../../shared/components/toast/toast.service';

@Component({
  selector: 'app-quote',
  standalone: true,
  imports: [ReactiveFormsModule, MoneyPipe, LowerCasePipe],
  templateUrl: './quote.html',
})
export class QuoteComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);
  private readonly quoteService = inject(QuoteService);
  private readonly productService = inject(ProductService);
  private readonly toast = inject(ToastService);

  products = signal<ProductDto[]>([]);
  preselectedProduct = signal<ProductDto | null>(null);
  selectedProduct = signal<ProductDto | null>(null);
  quoteResult = signal<GenerateQuoteResponse | null>(null);
  submitting = signal(false);

  form = this.fb.group({
    productId: ['', Validators.required],
    sumAssured: [null as number | null, Validators.required],
    tenureYears: [null as number | null, Validators.required],
    paymentFrequency: ['Monthly', Validators.required],
    age: [null as number | null, Validators.required],
    vehicleMake: [''],
    vehicleModel: [''],
    manufactureYear: [null as number | null],
    insuredDeclaredValue: [null as number | null],
    preExistingConditions: [''],
    isSmoker: [false],
  });

  ngOnInit(): void {
    this.productService.getAll().subscribe(products => {
      this.products.set(products);

      const productId = this.route.snapshot.paramMap.get('productId');
      if (productId) {
        const p = products.find(pr => pr.id === productId);
        if (p) {
          this.preselectedProduct.set(p);
          this.selectedProduct.set(p);
          this.form.patchValue({ productId: p.id });
          this.applyProductValidators(p);
        }
      }
    });
  }

  onProductChange(): void {
    const id = this.form.value.productId;
    const p = this.products().find(pr => pr.id === id) ?? null;
    this.selectedProduct.set(p);
    this.quoteResult.set(null);
    if (p) this.applyProductValidators(p);
  }

  onSubmit(): void {
    if (this.submitting()) return;
    if (this.form.invalid) return;
    const v = this.form.value;
    if (!this.selectedProduct()) {
      this.toast.warning('Please select a product before calculating premium.');
      return;
    }

    const req: GenerateQuoteRequest = {
      productId: v.productId!,
      age: v.age ?? undefined,
      sumAssured: v.sumAssured!,
      tenureYears: v.tenureYears!,
    };

    this.submitting.set(true);
    this.quoteService.generateQuote(req).subscribe({
      next: result => { this.quoteResult.set(result); this.submitting.set(false); },
      error: err => {
        this.toast.error(err?.error?.title ?? 'Failed to generate quote');
        this.submitting.set(false);
      },
    });
  }

  applyNow(): void {
    const quote = this.quoteResult();
    if (!quote) return;

    this.router.navigate(['/proposals/new'], {
      state: {
        productId: this.form.value.productId,
        sumAssured: quote.sumAssured,
        tenureYears: quote.tenureYears,
        premiumAmount: quote.premiumAmount,
        paymentFrequency: quote.paymentFrequency,
        domain: this.selectedProduct()?.domain,
        motorVehicleType: this.selectedProduct()?.motorVehicleType,
        motorDetail: this.selectedProduct()?.domain.toUpperCase() === 'MOTOR' ? {
          vehicleMake: this.form.value.vehicleMake,
          vehicleModel: this.form.value.vehicleModel,
          manufactureYear: this.form.value.manufactureYear,
          insuredDeclaredValue: this.form.value.insuredDeclaredValue,
        } : undefined,
      },
    });
  }

  private applyProductValidators(product: ProductDto): void {
    this.form.controls.sumAssured.setValidators([
      Validators.required,
      Validators.min(product.minSumAssured),
      Validators.max(product.maxSumAssured),
    ]);
    this.form.controls.tenureYears.setValidators([
      Validators.required,
      Validators.min(product.minTenureYears),
      Validators.max(product.maxTenureYears),
    ]);
    // Motor isn't age-rated — the driver/policyholder's age doesn't affect vehicle premium
    // here, so the Age field is hidden and not required for Motor products (see quote.html).
    if (product.domain.toUpperCase() === 'MOTOR') {
      this.form.controls.age.clearValidators();
      this.form.controls.age.setValue(null);
    } else {
      this.form.controls.age.setValidators([
        Validators.required,
        Validators.min(product.minAge),
        Validators.max(product.maxAge),
      ]);
    }
    this.form.controls.sumAssured.updateValueAndValidity();
    this.form.controls.tenureYears.updateValueAndValidity();
    this.form.controls.age.updateValueAndValidity();
  }
}
