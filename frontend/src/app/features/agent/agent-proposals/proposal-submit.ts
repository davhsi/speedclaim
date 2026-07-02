import { Component, inject, OnInit, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { forkJoin } from 'rxjs';
import { AgentService, AgentCustomerDto } from '../services/agent.service';
import { ProductDto } from '../../../core/models/api.models';
import { MoneyPipe } from '../../../shared/pipes/money.pipe';
import { SafeHtmlPipe } from '../../../shared/pipes/safe-html.pipe';
import { ToastService } from '../../../shared/components/toast/toast.service';

type ProductType = 'Health' | 'Motor' | 'Life';

interface StepDef {
  n: number;
  label: string;
  active: boolean;
  done: boolean;
  showLine: boolean;
}

@Component({
  selector: 'app-agent-proposal-submit',
  standalone: true,
  imports: [RouterLink, FormsModule, MoneyPipe, SafeHtmlPipe],
  templateUrl: './proposal-submit.html',
})
export class AgentProposalSubmitComponent implements OnInit {
  private agentService = inject(AgentService);
  private router = inject(Router);
  private toast = inject(ToastService);

  customers = signal<AgentCustomerDto[]>([]);
  products = signal<ProductDto[]>([]);
  quoteResult = signal<any>(null);

  currentStep = 0;
  selectedCustomerId: string | null = null;
  selectedType: ProductType | null = null;
  confirmReady = false;

  productTypes: ProductType[] = ['Health', 'Motor', 'Life'];

  healthForm = { coverType: 'Family floater', members: 4, sumAssured: '₹10,00,000', eldestAge: 67, discount: 5 };
  motorForm = { vehicleMakeModel: '', regYear: 2022, idv: '', ncb: '20%', discount: 5 };
  lifeForm = { sumAssured: '', term: '30 years', age: 38, tobaccoUser: 'No', discount: 5 };
  proposerForm = { fullName: '', dob: '', annualIncome: '', occupation: '', pan: '', aadhaarLast4: '' };

  kycDocs = [
    { label: 'Aadhaar card (front & back)', hint: 'JPG, PNG, or PDF — max 5 MB' },
    { label: 'PAN card', hint: 'JPG, PNG, or PDF — max 5 MB' },
  ];

  proposalDocs = [
    { label: 'Proposal form (signed)', hint: 'PDF only — max 10 MB' },
    { label: 'Medical records (if applicable)', hint: 'PDF, JPG, or PNG — max 10 MB' },
  ];

  stepLabels = ['Customer', 'Quote', 'Details', 'Documents'];

  steps = signal<StepDef[]>(this.buildSteps());

  ngOnInit(): void {
    forkJoin({
      customers: this.agentService.getCustomers(),
      products: this.agentService.getProducts(),
    }).subscribe({
      next: ({ customers, products }) => {
        this.customers.set(customers);
        this.products.set(products);

        if (customers.length > 0) {
          this.selectedCustomerId = customers[0].customerId ?? customers[0].id;
          const cust = customers[0];
          this.proposerForm.fullName = cust.fullName;
        }
      },
    });
  }

  nextStep(): void {
    if (this.currentStep === 0) {
      if (!this.selectedCustomerId) {
        this.toast.warning('Please select a customer before continuing.');
        return;
      }
      if (!this.selectedType) {
        this.toast.warning('Please select a product type before continuing.');
        return;
      }
    }

    if (this.currentStep === 1 && !this.quoteResult()) {
      this.toast.warning('Please calculate the premium before continuing.');
      return;
    }

    if (this.currentStep === 3) {
      if (!this.confirmReady) {
        this.toast.warning('Please confirm that all documents are accurate before submitting.');
        return;
      }
      this.submitProposal();
      return;
    }

    this.currentStep++;
    this.steps.set(this.buildSteps());
  }

  prevStep(): void {
    if (this.currentStep > 0) {
      this.currentStep--;
      this.steps.set(this.buildSteps());
    }
  }

  calculateQuote(): void {
    const product = this.products().find(p => p.domain.toUpperCase() === this.selectedType?.toUpperCase());
    if (!product) return;

    let age = 35;
    let sumAssured = product.minSumAssured;
    let tenureYears = product.minTenureYears;

    if (this.selectedType === 'Health') {
      age = this.healthForm.eldestAge;
      sumAssured = this.parseMoney(this.healthForm.sumAssured) || product.minSumAssured;
      tenureYears = 1;
    } else if (this.selectedType === 'Motor') {
      sumAssured = this.parseMoney(this.motorForm.idv) || product.minSumAssured;
      tenureYears = 1;
    } else if (this.selectedType === 'Life') {
      age = this.lifeForm.age;
      sumAssured = this.parseMoney(this.lifeForm.sumAssured) || product.minSumAssured;
      tenureYears = this.parseTerm(this.lifeForm.term) || product.minTenureYears;
    }

    // Clamp to product limits
    sumAssured = Math.max(product.minSumAssured, Math.min(product.maxSumAssured, sumAssured));
    tenureYears = Math.max(product.minTenureYears, Math.min(product.maxTenureYears, tenureYears));

    this.agentService.generateQuote({
      productId: product.id,
      age,
      sumAssured,
      tenureYears,
    }).subscribe({
      next: res => this.quoteResult.set(res),
      error: () => this.toast.error('Failed to calculate quote. Please check the values and try again.'),
    });
  }

  private parseMoney(val: string): number {
    return parseInt(val.replace(/[₹,\s]/g, ''), 10) || 0;
  }

  private parseTerm(val: string): number {
    return parseInt(val, 10) || 0;
  }

  private submitProposal(): void {
    const product = this.products().find(p => p.domain.toUpperCase() === this.selectedType?.toUpperCase());
    const quote = this.quoteResult();
    if (!product || !this.selectedCustomerId || !quote) return;

    this.agentService.submitProposal({
      productId: product.id,
      customerId: this.selectedCustomerId,
      sumAssured: quote.sumAssured,
      tenureYears: quote.tenureYears,
      premiumAmount: quote.premiumAmount,
      paymentFrequency: 'Annually',
      customerMemberIds: [],
      nominees: [],
    }).subscribe({
      next: () => this.router.navigate(['/agent/proposals']),
      error: () => this.toast.error('Failed to submit proposal. Please try again.'),
    });
  }

  typeIcon(type: ProductType): string {
    const icons: Record<ProductType, string> = {
      Health: '<svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75"><path d="M20.84 4.61a5.5 5.5 0 00-7.78 0L12 5.67l-1.06-1.06a5.5 5.5 0 00-7.78 7.78l1.06 1.06L12 21.23l7.78-7.78 1.06-1.06a5.5 5.5 0 000-7.78z"/></svg>',
      Motor: '<svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75"><rect x="1" y="3" width="15" height="13"/><polygon points="16 8 20 8 23 11 23 16 16 16 16 8"/><circle cx="5.5" cy="18.5" r="2.5"/><circle cx="18.5" cy="18.5" r="2.5"/></svg>',
      Life: '<svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.75"><path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z"/></svg>',
    };
    return icons[type];
  }

  private buildSteps(): StepDef[] {
    return this.stepLabels.map((label, i) => ({
      n: i + 1,
      label,
      active: i === this.currentStep,
      done: i < this.currentStep,
      showLine: i < this.stepLabels.length - 1,
    }));
  }
}
