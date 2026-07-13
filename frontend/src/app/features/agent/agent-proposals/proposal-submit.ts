import { Component, inject, OnInit, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { forkJoin, Observable, Subject } from 'rxjs';
import { AgentService, AgentCustomerDto } from '../services/agent.service';
import { DocumentRequirementDto, ProductDto } from '../../../core/models/api.models';
import { FileUploadComponent } from '../../../shared/components/file-upload/file-upload';
import { MoneyPipe } from '../../../shared/pipes/money.pipe';
import { SafeHtmlPipe } from '../../../shared/pipes/safe-html.pipe';
import { ConfirmDialogComponent } from '../../../shared/components/confirm-dialog/confirm-dialog';
import { ToastService } from '../../../shared/components/toast/toast.service';
import { CanComponentDeactivate } from '../../../core/guards/unsaved-changes.guard';

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
  imports: [RouterLink, FormsModule, MoneyPipe, SafeHtmlPipe, FileUploadComponent, ConfirmDialogComponent],
  templateUrl: './proposal-submit.html',
})
export class AgentProposalSubmitComponent implements OnInit, CanComponentDeactivate {
  private readonly agentService = inject(AgentService);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);

  customers = signal<AgentCustomerDto[]>([]);
  products = signal<ProductDto[]>([]);
  quoteResult = signal<any>(null);
  calculatingQuote = signal(false);
  submitting = signal(false);

  currentStep = 0;
  selectedCustomerId: string | null = null;
  selectedCustomerName: string | null = null;
  selectedCustomerKycApproved = false;
  selectedCustomerKycStatus: string | null = null;
  selectedCustomerKycRejectionReason: string | null = null;
  selectedType: ProductType | null = null;
  selectedProductId: string | null = null;
  confirmReady = false;

  customerSearchQuery = signal('');
  customerSearchResults = signal<AgentCustomerDto[]>([]);
  searchingCustomers = signal(false);
  private customerSearchTimeout: ReturnType<typeof setTimeout> | undefined;

  productTypes: ProductType[] = ['Health', 'Motor', 'Life'];

  // Defaults sit inside the seeded Health product's limits (age ≤ 65, rate bands up to ₹5,00,000)
  // so the happy path works on a fresh database without tripping eligibility/rate errors.
  healthForm = { coverType: 'Family floater', members: 4, sumAssured: '₹5,00,000', eldestAge: 35 };
  motorForm = { vehicleNumber: '', vehicleMake: '', vehicleModel: '', regYear: 2022, idv: '', engineNumber: '', chassisNumber: '', coverType: 'Comprehensive' };
  lifeForm = { sumAssured: '', term: '30 years', age: 38, tobaccoUser: 'No' };
  proposerForm = { fullName: '', dob: '', annualIncome: '', occupation: '', pan: '', aadhaarLast4: '' };

  nominees: { fullName: string; relationship: string; dateOfBirth: string; sharePercentage: number; appointeeName: string }[] = [];
  relationships = ['Spouse', 'Husband', 'Wife', 'Son', 'Daughter', 'Father', 'Mother', 'Brother', 'Sister', 'Guardian', 'Other'];

  docRequirements = signal<DocumentRequirementDto[]>([]);
  docRequirementsLoaded = signal(false);
  uploadedFiles = new Map<string, File>();

  stepLabels = ['Customer', 'Quote', 'Details', 'Documents'];

  steps = signal<StepDef[]>(this.buildSteps());

  showLeaveConfirm = signal(false);
  private navigatedAfterSubmit = false;
  private leaveSubject: Subject<boolean> | null = null;

  // Step 0 is just picking a customer/product type — trivially redoable, so nothing
  // worth guarding exists until the agent has actually progressed into the quote/
  // details/documents steps (calculated quote, typed nominees, attached files).
  canDeactivate(): boolean | Observable<boolean> {
    if (this.navigatedAfterSubmit || this.currentStep === 0) return true;

    this.showLeaveConfirm.set(true);
    this.leaveSubject = new Subject<boolean>();
    return this.leaveSubject.asObservable();
  }

  confirmLeave(): void {
    this.showLeaveConfirm.set(false);
    this.leaveSubject?.next(true);
    this.leaveSubject?.complete();
    this.leaveSubject = null;
  }

  cancelLeave(): void {
    this.showLeaveConfirm.set(false);
    this.leaveSubject?.next(false);
    this.leaveSubject?.complete();
    this.leaveSubject = null;
  }

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
          this.applySelectedCustomer(customers[0]);
        }
      },
    });
  }

  private applySelectedCustomer(c: AgentCustomerDto): void {
    this.selectedCustomerName = c.fullName;
    this.selectedCustomerKycApproved = c.kycApproved === true;
    this.selectedCustomerKycStatus = c.kycStatus ?? null;
    this.selectedCustomerKycRejectionReason = c.kycRejectionReason ?? null;

    this.proposerForm.fullName = c.fullName;
    this.proposerForm.dob = c.dateOfBirth ?? '';
    this.proposerForm.annualIncome = c.annualIncome != null ? `₹${c.annualIncome.toLocaleString('en-IN')}` : '';
    this.proposerForm.occupation = c.occupation ?? '';
    this.proposerForm.pan = '';
    this.proposerForm.aadhaarLast4 = '';

    // GetCustomerKyc matches against the User ID (c.id), not the Customer row's own PK
    // (c.customerId) — see CLAUDE.md §21 for the analogous agent-management ID mixup.
    this.agentService.getCustomerKyc(c.id).subscribe({
      next: kyc => {
        this.proposerForm.pan = kyc?.panNumber ?? '';
        this.proposerForm.aadhaarLast4 = kyc?.aadhaarNumber ?? '';
      },
      error: () => {},
    });
  }

  onMyCustomerSelected(id: string): void {
    const cust = this.customers().find(c => (c.customerId ?? c.id) === id);
    if (cust) this.applySelectedCustomer(cust);
  }

  onCustomerSearchInput(query: string): void {
    this.customerSearchQuery.set(query);
    clearTimeout(this.customerSearchTimeout);
    if (query.trim().length < 2) {
      this.customerSearchResults.set([]);
      return;
    }
    this.customerSearchTimeout = setTimeout(() => {
      this.searchingCustomers.set(true);
      this.agentService.searchCustomers(query.trim()).subscribe({
        next: results => {
          this.customerSearchResults.set(results);
          this.searchingCustomers.set(false);
        },
        error: () => {
          this.customerSearchResults.set([]);
          this.searchingCustomers.set(false);
        },
      });
    }, 300);
  }

  selectCustomerFromSearch(c: AgentCustomerDto): void {
    this.selectedCustomerId = c.customerId ?? c.id;
    this.applySelectedCustomer(c);
    this.customerSearchResults.set([]);
    this.customerSearchQuery.set('');
  }

  addNominee(): void {
    this.nominees.push({ fullName: '', relationship: 'Spouse', dateOfBirth: '', sharePercentage: this.nominees.length === 0 ? 100 : 0, appointeeName: '' });
  }

  removeNominee(index: number): void {
    this.nominees.splice(index, 1);
  }

  isMinorNominee(index: number): boolean {
    const dob = this.nominees[index]?.dateOfBirth;
    return !!dob && this.calculateAge(dob) < 18;
  }

  totalNomineeShares(): number {
    return this.nominees.reduce((sum, n) => sum + (Number(n.sharePercentage) || 0), 0);
  }

  private nomineesError(): string | null {
    if (this.selectedType === 'Life' && this.nominees.length === 0) {
      return 'Life proposals require at least one nominee.';
    }
    if (this.nominees.length === 0) return null;
    for (const [i, n] of this.nominees.entries()) {
      if (!n.fullName.trim() || !n.dateOfBirth) return `Nominee ${i + 1} needs a name and date of birth.`;
      if (this.isMinorNominee(i) && !n.appointeeName.trim()) return `Nominee ${i + 1} is a minor — an appointee name is required.`;
    }
    if (this.totalNomineeShares() !== 100) return 'Nominee shares must total 100%.';
    return null;
  }

  private motorDetailsError(): string | null {
    if (this.selectedType !== 'Motor') return null;
    if (!this.motorForm.vehicleNumber.trim()) return 'Vehicle registration number is required.';
    if (!this.motorForm.vehicleMake.trim()) return 'Vehicle make is required.';
    if (!this.motorForm.vehicleModel.trim()) return 'Vehicle model is required.';
    if (!this.motorForm.engineNumber.trim()) return 'Engine number is required.';
    if (!this.motorForm.chassisNumber.trim()) return 'Chassis number is required.';
    return null;
  }

  private calculateAge(dateOfBirth: string): number {
    const dob = new Date(dateOfBirth);
    const today = new Date();
    let age = today.getFullYear() - dob.getFullYear();
    const monthDelta = today.getMonth() - dob.getMonth();
    if (monthDelta < 0 || (monthDelta === 0 && today.getDate() < dob.getDate())) {
      age--;
    }
    return age;
  }

  hasActiveProduct(type: ProductType): boolean {
    return this.products().some(p => p.domain.toUpperCase() === type.toUpperCase());
  }

  productsForSelectedType(): ProductDto[] {
    if (!this.selectedType) return [];
    return this.products().filter(p => p.domain.toUpperCase() === this.selectedType?.toUpperCase());
  }

  selectedProduct(): ProductDto | null {
    if (!this.selectedType) return null;
    return this.products().find(p => p.id === this.selectedProductId && p.domain.toUpperCase() === this.selectedType?.toUpperCase()) ?? null;
  }

  onProductTypeSelected(type: ProductType): void {
    if (this.selectedType === type) return;
    this.selectedType = type;
    this.selectedProductId = null;
    this.clearProductDependentState();

    const products = this.productsForSelectedType();
    if (products.length === 1) {
      this.selectedProductId = products[0].id;
    }
  }

  onProductSelected(productId: string | null): void {
    if (this.selectedProductId === productId) return;
    this.selectedProductId = productId;
    this.clearProductDependentState();
  }

  private clearProductDependentState(): void {
    this.quoteResult.set(null);
    this.docRequirements.set([]);
    this.docRequirementsLoaded.set(false);
    this.uploadedFiles.clear();
    this.confirmReady = false;
  }

  nextStep(): void {
    if (this.currentStep === 0) {
      if (!this.selectedCustomerId) {
        this.toast.warning('Please select a customer before continuing.');
        return;
      }
      if (!this.selectedCustomerKycApproved) {
        this.toast.warning('This customer\'s KYC must be approved before you can submit a proposal for them.');
        return;
      }
      if (!this.selectedType) {
        this.toast.warning('Please select a product type before continuing.');
        return;
      }
      if (!this.hasActiveProduct(this.selectedType)) {
        this.toast.warning(`No active ${this.selectedType} product is available right now. Please choose a different type or contact an admin.`);
        return;
      }
      if (!this.selectedProduct()) {
        this.toast.warning('Please select a product before continuing.');
        return;
      }
    }

    if (this.currentStep === 1) {
      if (!this.quoteResult()) {
        this.toast.warning('Please calculate the premium before continuing.');
        return;
      }
      const motorError = this.motorDetailsError();
      if (motorError) {
        this.toast.warning(motorError);
        return;
      }
      // Life requires a nominee — seed one row so the Details step isn't empty.
      if (this.selectedType === 'Life' && this.nominees.length === 0) {
        this.addNominee();
      }
    }

    if (this.currentStep === 2) {
      const nomineeError = this.nomineesError();
      if (nomineeError) {
        this.toast.warning(nomineeError);
        return;
      }
      this.loadDocRequirements();
    }

    if (this.currentStep === 3) {
      if (this.submitting()) return;
      const motorError = this.motorDetailsError();
      if (motorError) {
        this.toast.warning(motorError);
        return;
      }
      if (!this.requiredDocumentsUploaded()) {
        this.toast.warning('Please upload all required documents before submitting.');
        return;
      }
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
    if (this.calculatingQuote()) return;
    const product = this.selectedProduct();
    if (!product) {
      this.toast.error('Please select a product before calculating the premium.');
      return;
    }

    // Motor isn't age-rated (see backend ProposalService.IsAgeRatedDomain) — the Motor form
    // never collects a driver/policyholder age, so age stays undefined for Motor quotes.
    let age: number | undefined;
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

    this.calculatingQuote.set(true);
    this.agentService.generateQuote({
      productId: product.id,
      age,
      sumAssured,
      tenureYears,
    }).subscribe({
      next: res => { this.quoteResult.set(res); this.calculatingQuote.set(false); },
      error: () => {
        this.toast.error('Failed to calculate quote. Please check the values and try again.');
        this.calculatingQuote.set(false);
      },
    });
  }

  private parseMoney(val: string): number {
    return Number.parseInt(val.replace(/[₹,\s]/g, ''), 10) || 0;
  }

  private parseTerm(val: string): number {
    return Number.parseInt(val, 10) || 0;
  }

  private loadDocRequirements(): void {
    const product = this.selectedProduct();
    if (!product || this.docRequirementsLoaded()) return;
    this.agentService.getProductDocuments(product.id).subscribe({
      next: docs => {
        this.docRequirements.set(docs);
        this.docRequirementsLoaded.set(true);
      },
      error: () => {
        this.docRequirementsLoaded.set(true);
        this.toast.warning('Document requirements could not be loaded.');
      },
    });
  }

  onDocSelected(key: string, file: File): void {
    this.uploadedFiles.set(key, file);
  }

  requiredDocumentsUploaded(): boolean {
    return this.docRequirementsLoaded() && this.docRequirements()
      .filter(d => d.isMandatory)
      .every(d => this.uploadedFiles.has(d.documentKey));
  }

  private submitProposal(): void {
    const product = this.selectedProduct();
    const quote = this.quoteResult();
    if (!product || !this.selectedCustomerId || !quote) return;

    this.submitting.set(true);
    this.agentService.submitProposal({
      productId: product.id,
      customerId: this.selectedCustomerId,
      sumAssured: quote.sumAssured,
      tenureYears: quote.tenureYears,
      premiumAmount: quote.premiumAmount,
      paymentFrequency: 'Annually',
      motorDetail: this.selectedType === 'Motor' ? {
        vehicleNumber: this.motorForm.vehicleNumber.trim(),
        vehicleMake: this.motorForm.vehicleMake.trim(),
        vehicleModel: this.motorForm.vehicleModel.trim(),
        manufactureYear: Number(this.motorForm.regYear) || 0,
        vehicleType: 'PrivateCar',
        idv: quote.sumAssured,
        engineNumber: this.motorForm.engineNumber.trim(),
        chassisNumber: this.motorForm.chassisNumber.trim(),
        coverType: this.motorForm.coverType,
      } : undefined,
      customerMemberIds: [],
      nominees: this.nominees.map((n, i) => ({
        fullName: n.fullName.trim(),
        relationship: n.relationship as any,
        sharePercentage: Number(n.sharePercentage),
        dateOfBirth: n.dateOfBirth,
        isMinor: this.isMinorNominee(i),
        appointeeName: n.appointeeName.trim() || undefined,
      })),
    }).subscribe({
      next: proposal => {
        this.navigatedAfterSubmit = true;
        const uploads = Array.from(this.uploadedFiles.entries());
        if (uploads.length === 0) {
          this.submitting.set(false);
          this.router.navigate(['/agent/proposals']);
          return;
        }
        let done = 0;
        for (const [key, file] of uploads) {
          this.agentService.uploadProposalDocument(proposal.id, key, file).subscribe({
            next: () => {
              if (++done === uploads.length) {
                this.submitting.set(false);
                this.router.navigate(['/agent/proposals']);
              }
            },
            error: () => {
              if (++done === uploads.length) {
                this.submitting.set(false);
                this.toast.warning('Proposal submitted but some documents failed to upload.');
                this.router.navigate(['/agent/proposals']);
              }
            },
          });
        }
      },
      error: () => {
        this.submitting.set(false);
        this.toast.error('Failed to submit proposal. Please try again.');
      },
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
