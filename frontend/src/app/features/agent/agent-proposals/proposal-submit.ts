import { Component, inject, OnInit, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { forkJoin } from 'rxjs';
import { AgentService, AgentCustomerDto } from '../services/agent.service';
import { ProductDto } from '../../../core/models/api.models';
import { MoneyPipe } from '../../../shared/pipes/money.pipe';
import { SafeHtmlPipe } from '../../../shared/pipes/safe-html.pipe';

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
          this.selectedCustomerId = customers[0].id;
          const cust = customers[0];
          this.proposerForm.fullName = cust.fullName;
        }
      },
    });
  }

  nextStep(): void {
    if (this.currentStep === 3) {
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
    const product = this.products().find(p => p.domain === this.selectedType);
    if (!product) return;

    this.agentService.generateQuote({
      productId: product.id,
      age: 38,
      sumAssured: product.minSumAssured,
      tenureYears: product.minTenureYears,
    }).subscribe({
      next: res => this.quoteResult.set(res),
    });
  }

  private submitProposal(): void {
    const product = this.products().find(p => p.domain === this.selectedType);
    if (!product || !this.selectedCustomerId) return;

    this.agentService.submitProposal({
      productId: product.id,
      customerId: this.selectedCustomerId,
      sumAssured: product.minSumAssured,
      tenureYears: product.minTenureYears,
      premiumAmount: this.quoteResult()?.premiumAmount ?? product.minSumAssured * 0.01,
      paymentFrequency: 'Annually',
      customerMemberIds: [],
      nominees: [],
    }).subscribe({
      next: () => this.router.navigate(['/agent/proposals']),
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
