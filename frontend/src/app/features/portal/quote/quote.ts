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
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private fb = inject(FormBuilder);
  private quoteService = inject(QuoteService);
  private productService = inject(ProductService);
  private toast = inject(ToastService);

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
    dateOfBirth: ['', Validators.required],
    gender: [''],
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
        const p = products.find(pr => pr.id === Number(productId));
        if (p) {
          this.preselectedProduct.set(p);
          this.selectedProduct.set(p);
          this.form.patchValue({ productId: p.id.toString() });
        }
      }
    });
  }

  onProductChange(): void {
    const id = Number(this.form.value.productId);
    const p = this.products().find(pr => pr.id === id) ?? null;
    this.selectedProduct.set(p);
  }

  onSubmit(): void {
    if (this.form.invalid) return;
    const v = this.form.value;
    const sp = this.selectedProduct()!;

    const req: GenerateQuoteRequest = {
      productId: Number(v.productId),
      sumAssured: v.sumAssured!,
      tenureYears: v.tenureYears!,
      paymentFrequency: v.paymentFrequency as any,
      dateOfBirth: v.dateOfBirth!,
      gender: v.gender as any || undefined,
    };

    if (sp.domain === 'Motor') {
      req.motorDetail = {
        vehicleMake: v.vehicleMake!,
        vehicleModel: v.vehicleModel!,
        manufactureYear: v.manufactureYear!,
        insuredDeclaredValue: v.insuredDeclaredValue!,
      };
    }
    if (sp.domain === 'Health') {
      req.healthDetail = { preExistingConditions: v.preExistingConditions || undefined };
    }
    if (sp.domain === 'Life') {
      req.lifeDetail = { isSmoker: v.isSmoker! };
    }

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
    this.router.navigate(['/proposals/new'], {
      state: {
        quote: this.quoteResult(),
        productId: Number(this.form.value.productId),
      },
    });
  }
}
