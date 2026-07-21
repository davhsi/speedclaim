import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, computed, effect, inject, signal, ElementRef, ViewChild } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { concatMap } from 'rxjs';
import { AuthService } from '../../../core/services/auth.service';
import { ClaimDto, CreatePaymentIntentResponse, DocumentRequirementDto, GenerateQuoteResponse, GrievanceDto, KycRecordDto, PolicyAssistantCitation, PolicyDto, PremiumScheduleDto, ProductDto, ProposalDto, SpeedyWorkspaceAction, SpeedyWorkspaceConversation, SubmitProposalRequest, UserDto } from '../../../core/models/api.models';
import { ProfileService } from '../profile/services/profile.service';
import { SpeedyAssistantService } from '../services/speedy-assistant.service';
import { ProductService } from '../products/services/product.service';
import { QuoteService } from '../quote/services/quote.service';
import { ClaimService } from '../claims/services/claim.service';
import { PolicyService } from '../policies/services/policy.service';
import { ProposalService } from '../proposals/services/proposal.service';
import { PaymentService } from '../payments/services/payment.service';
import { GrievanceService } from '../grievances/services/grievance.service';

declare const Stripe: any;

interface WorkspaceMessage {
  role: 'user' | 'assistant';
  content: string;
  actions?: SpeedyWorkspaceAction[];
  evidenceStatus?: 'Grounded' | 'InsufficientEvidence' | 'Rejected' | null;
  brochureVersion?: string | null;
  citations?: PolicyAssistantCitation[];
}

interface ConversationSection {
  messageIndex: number;
  label: string;
}

interface JourneyCard {
  title: string;
  detail: string;
  prompt: string;
  complete?: boolean;
}

const AADHAAR_PATTERN = /^\d{12}$/;
const PAN_PATTERN = /^[A-Z]{5}\d{4}[A-Z]$/;
const KYC_ALLOWED_EXTENSIONS = ['.pdf', '.jpg', '.jpeg', '.png'];
const KYC_MAX_FILE_SIZE_BYTES = 5 * 1024 * 1024;

interface BrowserSpeechRecognition {
  lang: string;
  interimResults: boolean;
  maxAlternatives: number;
  onstart: (() => void) | null;
  onend: (() => void) | null;
  onerror: (() => void) | null;
  onresult: ((event: { results: ArrayLike<{ 0: { transcript: string } }> }) => void) | null;
  start(): void;
}

@Component({
  selector: 'app-speedy-workspace',
  standalone: true,
  imports: [FormsModule, RouterLink, DatePipe, DecimalPipe],
  templateUrl: './speedy-workspace.html',
})
export class SpeedyWorkspaceComponent {
  @ViewChild('questionBox') private readonly questionBox?: ElementRef<HTMLTextAreaElement>;
  private readonly speedy = inject(SpeedyAssistantService);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly profile = inject(ProfileService);
  private readonly products = inject(ProductService);
  private readonly quotes = inject(QuoteService);
  private readonly claims = inject(ClaimService);
  private readonly policies = inject(PolicyService);
  private readonly proposals = inject(ProposalService);
  private readonly http = inject(HttpClient);
  private readonly payments = inject(PaymentService);
  private readonly grievances = inject(GrievanceService);

  readonly signedIn = computed(() => this.auth.currentUser()?.role === 'Customer');
  readonly today = new Date().toISOString().slice(0, 10);
  readonly question = signal('');
  readonly messages = signal<WorkspaceMessage[]>([]);
  readonly conversationId = signal<string | null>(null);
  readonly conversations = signal<SpeedyWorkspaceConversation[]>([]);
  readonly sectionNavigatorOpen = signal(false);
  readonly activeSectionIndex = signal<number | null>(null);
  readonly historyLoaded = signal(false);
  readonly historyError = signal(false);
  readonly sending = signal(false);
  readonly error = signal<string | null>(null);
  readonly voiceAvailable = signal(typeof window !== 'undefined' && ('SpeechRecognition' in window || 'webkitSpeechRecognition' in window));
  readonly listening = signal(false);
  readonly showKyc = signal(false);
  readonly kycSubmitting = signal(false);
  readonly kycError = signal<string | null>(null);
  readonly kycRecord = signal<KycRecordDto | null>(null);
  readonly kycLoaded = signal(false);
  readonly aadhaarNumber = signal('');
  readonly panNumber = signal('');
  readonly aadhaarFile = signal<File | null>(null);
  readonly panFile = signal<File | null>(null);
  readonly aadhaarFileError = signal('');
  readonly panFileError = signal('');
  readonly workspaceTask = signal<'quote' | 'proposal' | 'claim' | 'claimStatus' | 'grievanceStatus' | 'policyStatus' | 'payment' | null>(null);
  readonly taskLoading = signal(false);
  readonly taskSubmitting = signal(false);
  readonly taskError = signal<string | null>(null);
  readonly taskProducts = signal<ProductDto[]>([]);
  readonly taskPolicies = signal<PolicyDto[]>([]);
  readonly taskClaims = signal<ClaimDto[]>([]);
  readonly taskGrievances = signal<GrievanceDto[]>([]);
  readonly taskProposals = signal<ProposalDto[]>([]);
  readonly quoteProductId = signal('');
  readonly quoteProductNumber = signal<number | null>(null);
  readonly quoteAge = signal<number | null>(null);
  readonly quoteCoverage = signal<number | null>(null);
  readonly quoteTenure = signal<number | null>(null);
  readonly quoteVehicleMarketValue = signal<number | null>(null);
  readonly quoteVehicleYear = signal<number | null>(null);
  readonly quoteResult = signal<GenerateQuoteResponse | null>(null);
  readonly claimPolicyId = signal('');
  readonly claimAmount = signal<number | null>(null);
  readonly claimDate = signal('');
  readonly claimDescription = signal('');
  readonly claimFiles = signal<File[]>([]);
  readonly proposalCustomer = signal<UserDto | null>(null);
  readonly proposalDocuments = signal<DocumentRequirementDto[]>([]);
  readonly proposalFiles = signal<Record<string, File>>({});
  readonly nomineeName = signal('');
  readonly nomineeRelationship = signal('Spouse');
  readonly nomineeDateOfBirth = signal('');
  readonly motorVehicleNumber = signal('');
  readonly motorMake = signal('');
  readonly motorModel = signal('');
  readonly motorYear = signal<number | null>(null);
  readonly motorEngine = signal('');
  readonly motorChassis = signal('');
  readonly paymentSchedules = signal<PremiumScheduleDto[]>([]);
  readonly paymentPolicy = signal<PolicyDto | null>(null);
  readonly paymentOpening = signal(false);
  readonly paymentConfirming = signal(false);
  readonly paymentSuccess = signal(false);
  readonly paymentClientSecret = signal<string | null>(null);
  readonly paymentReady = signal(false);
  readonly paymentAmount = signal<number | null>(null);
  readonly nextPayableSchedule = computed(() => this.paymentSchedules()
    .filter(schedule => this.isUnpaidSchedule(schedule))
    .sort((left, right) => left.installmentNumber - right.installmentNumber)[0] ?? null);
  private stripeInstance: any = null;
  private stripeElements: any = null;
  readonly aadhaarError = computed(() => {
    const value = this.aadhaarNumber().trim();
    if (!value) return '';
    return AADHAAR_PATTERN.test(value) ? '' : 'Aadhaar must be exactly 12 digits.';
  });
  readonly panError = computed(() => {
    const value = this.panNumber().trim().toUpperCase();
    if (!value) return '';
    return PAN_PATTERN.test(value) ? '' : 'PAN must be in the format ABCDE1234F.';
  });
  readonly kycReady = computed(() => AADHAAR_PATTERN.test(this.aadhaarNumber().trim())
    && PAN_PATTERN.test(this.panNumber().trim().toUpperCase()) && !!this.aadhaarFile() && !!this.panFile());
  readonly kycUnderReviewOrVerified = computed(() => {
    const kyc = this.kycRecord();
    return !!kyc && kyc.aadhaarUploaded && kyc.panUploaded
      && ['Pending', 'UnderReview', 'Approved'].includes(kyc.kycStatus);
  });
  readonly selectedQuoteProduct = computed(() => this.taskProducts().find(product => product.id === this.quoteProductId()) ?? null);
  readonly selectedClaimPolicy = computed(() => this.taskPolicies().find(policy => policy.id === this.claimPolicyId()) ?? null);
  readonly quoteReady = computed(() => {
    const product = this.selectedQuoteProduct();
    if (!product || !this.quoteTenure() || this.quoteTenure()! < product.minTenureYears || this.quoteTenure()! > product.maxTenureYears) return false;
    if (product.domain.toUpperCase() === 'MOTOR') return !!this.quoteVehicleMarketValue() && !!this.quoteVehicleYear()
      && this.quoteVehicleYear()! >= 1900 && this.quoteVehicleYear()! <= new Date().getFullYear();
    return !!this.quoteAge() && this.quoteAge()! >= product.minAge && this.quoteAge()! <= product.maxAge
      && !!this.quoteCoverage() && this.quoteCoverage()! >= product.minSumAssured && this.quoteCoverage()! <= product.maxSumAssured;
  });
  readonly claimReady = computed(() => {
    const policy = this.selectedClaimPolicy();
    return !!policy && !!this.claimAmount() && this.claimAmount()! >= 500 && this.claimAmount()! <= policy.coverageAmount
      && !!this.claimDate() && this.claimDate() <= new Date().toISOString().slice(0, 10)
      && this.claimDescription().trim().length >= 10;
  });
  readonly proposalReady = computed(() => {
    const product = this.selectedQuoteProduct();
    const quote = this.quoteResult();
    if (!product || !quote || !this.proposalCustomer()?.customerId) return false;
    if (!this.proposalDocuments().filter(document => document.isMandatory).every(document => !!this.proposalFiles()[document.documentKey])) return false;
    if (product.domain.toUpperCase() === 'LIFE') return !!this.nomineeName().trim() && !!this.nomineeDateOfBirth();
    if (product.domain.toUpperCase() === 'MOTOR') return !!this.motorVehicleNumber().trim() && !!this.motorMake().trim() && !!this.motorModel().trim()
      && !!this.motorYear() && !!this.motorEngine().trim() && !!this.motorChassis().trim();
    return true;
  });

  readonly journeyCards = computed<JourneyCard[]>(() => {
    const kyc = this.kycRecord();
    const submitted = this.kycUnderReviewOrVerified();
    const kycDetail = this.kycJourneyDetail(kyc, submitted);
    return [
      { title: submitted ? 'KYC submitted' : 'Start KYC', detail: kycDetail, prompt: submitted ? 'My KYC is submitted. What should I do next?' : 'Help me complete my KYC.', complete: submitted },
      { title: 'Explore cover', detail: 'Browse products and find cover that fits your life.', prompt: 'What insurance products are available, and which might suit me?' },
      { title: 'Plan your payment', detail: 'Check when your next premium is due and how to pay it.', prompt: 'What is my next premium schedule and when do I need to pay?' },
      { title: 'Get claim help', detail: 'Understand how to file or track a claim for an active policy.', prompt: 'Help me understand how to file a claim or track my existing claim.' },
    ];
  });
  readonly recentConversations = computed(() => this.conversations().filter(conversation => this.ageInDays(conversation.updatedAt) <= 7));
  readonly previousConversations = computed(() => this.conversations().filter(conversation => {
    const days = this.ageInDays(conversation.updatedAt);
    return days > 7 && days <= 30;
  }));
  readonly conversationSections = computed<ConversationSection[]>(() => this.messages()
    .map((message, messageIndex) => ({ message, messageIndex }))
    .filter(({ message }) => message.role === 'user')
    .map(({ message, messageIndex }) => ({ messageIndex, label: message.content })));

  constructor() {
    effect(() => {
      if (this.signedIn() && !this.historyLoaded()) this.loadConversations();
    });
    effect(() => {
      if (this.signedIn() && !this.kycLoaded()) this.loadKyc();
    });
  }

  ask(value?: string): void {
    const question = (value ?? this.question()).trim();
    if (!question || this.sending()) return;
    this.question.set('');
    this.error.set(null);
    this.messages.update(messages => [...messages, { role: 'user', content: question }]);
    this.activeSectionIndex.set(this.messages().length - 1);
    this.sending.set(true);
    this.speedy.askWorkspace(question, this.conversationId()).subscribe({
      next: response => {
        if (response.conversationId) this.conversationId.set(response.conversationId);
        this.messages.update(messages => [...messages, {
          role: 'assistant', content: response.answer, actions: response.actions,
          evidenceStatus: response.evidenceStatus, brochureVersion: response.brochureVersion,
          citations: response.citations ?? [],
        }]);
        this.sending.set(false);
        if (this.signedIn()) this.refreshConversations();
      },
      error: failure => {
        this.sending.set(false);
        this.error.set(failure?.status === 401 ? 'Sign in to use account-specific help.' : 'Speedy is temporarily unavailable. Please try again.');
      },
    });
  }

  prepareQuestion(prompt: string): void {
    this.question.set(prompt);
    this.error.set(null);
    queueMicrotask(() => this.questionBox?.nativeElement.focus());
  }

  private kycJourneyDetail(kyc: KycRecordDto | null, submitted: boolean): string {
    if (!submitted) return 'Submit Aadhaar and PAN to unlock applications, payments, and claims.';
    if (kyc?.kycStatus === 'Approved') return 'Submitted and verified. You are ready for the next step.';
    return 'Submitted. Verification is pending — you do not need to resubmit.';
  }

  onQuestionKeydown(event: KeyboardEvent): void {
    if (event.key !== 'Enter' || event.shiftKey || event.isComposing) return;
    event.preventDefault();
    this.ask();
  }

  startNewChat(): void {
    if (this.sending()) return;
    this.sectionNavigatorOpen.set(false);
    this.conversationId.set(null);
    this.messages.set([]);
    this.activeSectionIndex.set(null);
    this.question.set('');
    this.error.set(null);
  }

  backToSpeedClaim(): void {
    void this.router.navigateByUrl(this.signedIn() ? '/dashboard' : '/');
  }

  openConversation(conversationId: string): void {
    if (this.sending() || this.conversationId() === conversationId) return;
    this.error.set(null);
    this.speedy.getWorkspaceConversation(conversationId).subscribe({
      next: conversation => {
        this.sectionNavigatorOpen.set(false);
        this.conversationId.set(conversation.id);
        this.messages.set((conversation.messages ?? []).map(message => ({
          role: message.role.toLowerCase() as WorkspaceMessage['role'],
          content: message.content,
          actions: message.actions,
          evidenceStatus: message.evidenceStatus,
          brochureVersion: message.brochureVersion,
          citations: message.citations ?? [],
        })));
        this.activeSectionIndex.set([...this.messages()].map((message, index) => ({ message, index }))
          .filter(({ message }) => message.role === 'user').at(-1)?.index ?? null);
      },
      error: () => this.error.set('That conversation could not be opened. Please try again.'),
    });
  }

  toggleSectionNavigator(): void {
    this.sectionNavigatorOpen.update(open => !open);
  }

  jumpToMessage(index: number): void {
    if (typeof document !== 'undefined') document.getElementById(`speedy-message-${index}`)?.scrollIntoView({ behavior: 'smooth', block: 'center' });
    this.activeSectionIndex.set(index);
  }

  moveSection(direction: -1 | 1): void {
    const sections = this.conversationSections();
    if (!sections.length) return;
    const current = sections.findIndex(section => section.messageIndex === this.activeSectionIndex());
    const base = current < 0 ? (direction === 1 ? -1 : sections.length) : current;
    const target = sections[Math.max(0, Math.min(sections.length - 1, base + direction))];
    this.jumpToMessage(target.messageIndex);
  }

  runAction(action: SpeedyWorkspaceAction): void {
    if (action.kind === 'guided_kyc') {
      if (this.kycUnderReviewOrVerified()) {
        const status = this.kycRecord()!.kycStatus;
        const message = status === 'Approved'
          ? 'Your KYC is already verified. You do not need to submit documents again.'
          : 'Your Aadhaar and PAN are already submitted and awaiting underwriter review. You do not need to submit them again. We will notify you in SpeedClaim and by email once review is complete.';
        this.messages.update(messages => [...messages, { role: 'assistant', content: message }]);
        this.announce(message);
        return;
      }
      this.showKyc.set(true);
      this.announce('I’ve opened the secure KYC checklist. Attach both labelled documents before continuing.');
      return;
    }
    if (!this.signedIn() && ['guided_quote', 'guided_claim', 'claim_status', 'grievance_status', 'policy_status', 'claim_documents', 'guided_application', 'payment'].includes(action.kind)) {
      this.error.set('Sign in to calculate a quote or securely access policy, claim, grievance, and document actions.');
      return;
    }
    if (action.kind === 'guided_quote') { this.openTask('quote'); return; }
    if (action.kind === 'guided_claim') { this.openTask('claim'); return; }
    if (action.kind === 'claim_status') { this.openTask('claimStatus'); return; }
    if (action.kind === 'grievance_status') { this.openTask('grievanceStatus'); return; }
    if (action.kind === 'policy_status') { this.openTask('policyStatus'); return; }
    if (action.kind === 'payment') { this.openTask('payment'); return; }
    if (action.kind === 'navigate' && action.route) this.router.navigateByUrl(action.route);
  }

  openTask(task: 'quote' | 'claim' | 'claimStatus' | 'grievanceStatus' | 'policyStatus' | 'payment'): void {
    this.workspaceTask.set(task);
    this.taskError.set(null);
    this.taskLoading.set(true);
    if (task === 'quote') {
      this.quoteResult.set(null);
      this.products.getAll().subscribe({ next: products => { this.taskProducts.set(products.filter(product => product.isAvailableForSale)); this.taskLoading.set(false); }, error: () => this.taskFailed('Products could not be loaded. Please try again.') });
      return;
    }
    if (task === 'claim') {
      this.claimFiles.set([]);
      this.policies.getMyPolicies('Active').subscribe({ next: policies => { this.taskPolicies.set(policies); this.taskLoading.set(false); }, error: () => this.taskFailed('Your active policies could not be loaded.') });
      return;
    }
    if (task === 'claimStatus') {
      this.claims.getMyClaims().subscribe({ next: claims => { this.taskClaims.set(claims); this.taskLoading.set(false); }, error: () => this.taskFailed('Your claims could not be loaded.') });
      return;
    }
    if (task === 'grievanceStatus') {
      this.grievances.getMyGrievances().subscribe({ next: grievances => { this.taskGrievances.set(grievances); this.taskLoading.set(false); }, error: () => this.taskFailed('Your grievances could not be loaded.') });
      return;
    }
    if (task === 'payment') {
      this.paymentPolicy.set(null);
      this.paymentSchedules.set([]);
      this.paymentClientSecret.set(null);
      this.paymentSuccess.set(false);
      this.policies.getMyPolicies().subscribe({ next: policies => { this.taskPolicies.set(policies.filter(policy => policy.status === 'Pending' || policy.status === 'Active')); this.taskLoading.set(false); }, error: () => this.taskFailed('Your policies could not be loaded.') });
      return;
    }
    this.proposals.getMyProposals().subscribe({ next: proposals => this.taskProposals.set(proposals), error: () => this.taskProposals.set([]) });
    this.policies.getMyPolicies().subscribe({ next: policies => { this.taskPolicies.set(policies); this.taskLoading.set(false); }, error: () => this.taskFailed('Your policies could not be loaded.') });
  }

  openPaymentSchedule(policy: PolicyDto): void {
    this.paymentPolicy.set(policy);
    this.taskError.set(null);
    this.taskLoading.set(true);
    this.payments.getSchedule(policy.id).subscribe({ next: schedules => { this.paymentSchedules.set(schedules); this.taskLoading.set(false); }, error: () => this.taskFailed('The premium schedule could not be loaded.') });
  }

  openStripeCheckout(schedule: PremiumScheduleDto): void {
    const policy = this.paymentPolicy();
    if (!policy || this.paymentOpening()) return;
    if (!this.canPaySchedule(schedule)) {
      const next = this.nextPayableSchedule();
      this.taskError.set(next
        ? `Installment #${next.installmentNumber} must be paid before installment #${schedule.installmentNumber}.`
        : 'This installment is not currently available for payment.');
      return;
    }
    this.paymentOpening.set(true);
    this.taskError.set(null);
    this.payments.createPaymentIntent(schedule.id, { policyId: policy.id }).subscribe({
      next: response => this.initializeStripeCheckout(response, schedule.amountDue),
      error: failure => {
        this.paymentOpening.set(false);
        const next = this.nextPayableSchedule();
        this.taskError.set(failure?.status === 409 && next
          ? `Installment #${next.installmentNumber} must be paid first. Please select that installment to continue.`
          : failure?.error?.detail ?? failure?.error?.title ?? 'The payment could not be started.');
      },
    });
  }

  canPaySchedule(schedule: PremiumScheduleDto): boolean {
    return this.nextPayableSchedule()?.id === schedule.id;
  }

  paymentAvailabilityMessage(schedule: PremiumScheduleDto): string {
    const next = this.nextPayableSchedule();
    return next
      ? `Available after installment #${next.installmentNumber} is paid`
      : 'Not currently payable';
  }

  private isUnpaidSchedule(schedule: PremiumScheduleDto): boolean {
    return schedule.status === 'Upcoming' || schedule.status === 'Due' || schedule.status === 'Overdue';
  }

  private initializeStripeCheckout(response: CreatePaymentIntentResponse, amount: number): void {
    this.paymentOpening.set(false);
    if (!response.publishableKey || !response.clientSecret || typeof Stripe === 'undefined') {
      this.taskError.set('Secure payment is not ready yet. Please try again in a moment.');
      return;
    }
    this.paymentAmount.set(amount);
    this.paymentReady.set(false);
    this.stripeInstance = Stripe(response.publishableKey);
    this.stripeElements = this.stripeInstance.elements({ clientSecret: response.clientSecret, appearance: { theme: 'stripe', variables: { colorPrimary: '#D48B13', borderRadius: '10px' } } });
    this.paymentClientSecret.set(response.clientSecret);
    setTimeout(() => {
      const mount = document.getElementById('speedy-stripe-payment-element');
      if (!mount || !this.stripeElements) return;
      const element = this.stripeElements.create('payment');
      element.mount('#speedy-stripe-payment-element');
      element.on('ready', () => this.paymentReady.set(true));
    }, 0);
  }

  async confirmWorkspacePayment(): Promise<void> {
    if (!this.stripeInstance || !this.stripeElements || this.paymentConfirming()) return;
    this.paymentConfirming.set(true);
    this.taskError.set(null);
    const { error } = await this.stripeInstance.confirmPayment({ elements: this.stripeElements, confirmParams: { return_url: `${globalThis.location.origin}/speedy` }, redirect: 'if_required' });
    this.paymentConfirming.set(false);
    if (error) { this.taskError.set(error.message ?? 'Payment could not be completed.'); return; }
    this.paymentSuccess.set(true);
  }

  selectQuoteProduct(id: string): void {
    this.quoteProductId.set(id);
    const product = this.selectedQuoteProduct();
    this.quoteCoverage.set(product?.coverageOptions?.[0] ?? null);
    this.quoteTenure.set(product?.minTenureYears ?? null);
    this.quoteAge.set(null);
    this.quoteVehicleMarketValue.set(null);
    this.quoteVehicleYear.set(null);
    this.quoteResult.set(null);
  }

  selectQuoteProductNumber(value: number | null): void {
    this.quoteProductNumber.set(value);
    const product = value && value > 0 ? this.taskProducts()[value - 1] : null;
    this.selectQuoteProduct(product?.id ?? '');
  }

  calculateWorkspaceQuote(): void {
    const product = this.selectedQuoteProduct();
    if (!product || !this.quoteReady() || this.taskSubmitting()) return;
    this.taskSubmitting.set(true);
    this.taskError.set(null);
    this.quotes.generateQuote({ productId: product.id, age: product.domain.toUpperCase() === 'MOTOR' ? undefined : this.quoteAge()!, sumAssured: product.domain.toUpperCase() === 'MOTOR' ? this.quoteVehicleMarketValue()! : this.quoteCoverage()!, tenureYears: this.quoteTenure()!, vehicleMarketValue: product.domain.toUpperCase() === 'MOTOR' ? this.quoteVehicleMarketValue()! : undefined, vehicleManufactureYear: product.domain.toUpperCase() === 'MOTOR' ? this.quoteVehicleYear()! : undefined }).subscribe({
      next: quote => { this.quoteResult.set(quote); this.taskSubmitting.set(false); },
      error: failure => { this.taskSubmitting.set(false); this.taskError.set(failure?.error?.title ?? 'The quote could not be calculated.'); },
    });
  }

  continueToApplication(): void {
    const product = this.selectedQuoteProduct();
    const quote = this.quoteResult();
    if (!product || !quote) return;
    this.workspaceTask.set('proposal');
    this.taskError.set(null);
    this.taskLoading.set(true);
    this.proposalFiles.set({});
    this.profile.getProfile().subscribe({
      next: profile => {
        this.proposalCustomer.set(profile);
        this.http.get<DocumentRequirementDto[]>(`/api/v1/products/${product.id}/documents`).subscribe({
          next: documents => { this.proposalDocuments.set(documents); this.taskLoading.set(false); },
          error: () => this.taskFailed('Required proposal documents could not be loaded.'),
        });
      },
      error: () => this.taskFailed('Your customer profile could not be loaded.'),
    });
  }

  chooseProposalFile(documentKey: string, event: Event): void {
    const file = (event.target as HTMLInputElement).files?.item(0) ?? null;
    if (!file) return;
    const validation = this.kycFileError(file);
    if (validation) { this.taskError.set(validation); return; }
    this.proposalFiles.update(files => ({ ...files, [documentKey]: file }));
  }

  submitWorkspaceProposal(): void {
    const product = this.selectedQuoteProduct();
    const quote = this.quoteResult();
    const customer = this.proposalCustomer();
    if (!product || !quote || !customer?.customerId || !this.proposalReady() || this.taskSubmitting()) return;
    const domain = product.domain.toUpperCase();
    const request: SubmitProposalRequest = {
      customerId: customer.customerId,
      productId: product.id,
      sumAssured: quote.sumAssured,
      tenureYears: quote.tenureYears,
      premiumAmount: quote.premiumAmount,
      paymentFrequency: quote.paymentFrequency as any,
      customerMemberIds: [],
      nominees: domain === 'LIFE' ? [{ fullName: this.nomineeName().trim(), relationship: this.nomineeRelationship() as any, sharePercentage: 100, dateOfBirth: this.nomineeDateOfBirth(), isMinor: false }] : [],
      motorDetail: domain === 'MOTOR' ? { vehicleNumber: this.motorVehicleNumber().trim(), vehicleMake: this.motorMake().trim(), vehicleModel: this.motorModel().trim(), manufactureYear: this.motorYear()!, vehicleType: product.motorVehicleType ?? 'PrivateCar', idv: quote.sumAssured, engineNumber: this.motorEngine().trim(), chassisNumber: this.motorChassis().trim(), coverType: 'Comprehensive' } : undefined,
    };
    this.taskSubmitting.set(true);
    this.taskError.set(null);
    this.proposals.submit(request).subscribe({
      next: proposal => this.uploadProposalFiles(proposal.id, Object.entries(this.proposalFiles()), 0, proposal.proposalNumber),
      error: failure => { this.taskSubmitting.set(false); this.taskError.set(failure?.error?.title ?? 'The proposal could not be submitted.'); },
    });
  }

  private uploadProposalFiles(proposalId: string, files: [string, File][], index: number, proposalNumber: string): void {
    if (index >= files.length) {
      this.taskSubmitting.set(false);
      this.workspaceTask.set(null);
      const message = `Your proposal ${proposalNumber} has been submitted for underwriter review. We will notify you in SpeedClaim and by email when its status changes. Once approved, Speedy will show your first premium payment.`;
      this.messages.update(messages => [...messages, { role: 'assistant', content: message }]);
      this.announce(message);
      return;
    }
    const [key, file] = files[index];
    this.proposals.uploadDocument(proposalId, key, file).subscribe({
      next: () => this.uploadProposalFiles(proposalId, files, index + 1, proposalNumber),
      error: () => { this.taskSubmitting.set(false); this.taskError.set('Your proposal was submitted, but a document could not be uploaded. Open the proposal from the customer portal to retry it safely.'); },
    });
  }

  selectClaimPolicy(id: string): void {
    this.claimPolicyId.set(id);
    this.claimAmount.set(null);
  }

  chooseClaimFiles(event: Event): void {
    const files = Array.from((event.target as HTMLInputElement).files ?? []);
    const valid = files.filter(file => file.size <= KYC_MAX_FILE_SIZE_BYTES && KYC_ALLOWED_EXTENSIONS.includes(`.${file.name.split('.').pop()?.toLowerCase() ?? ''}`));
    if (valid.length !== files.length) this.taskError.set('Only PDF, JPG, JPEG, or PNG files up to 5 MB can be attached.');
    this.claimFiles.set(valid.slice(0, 5));
  }

  submitWorkspaceClaim(): void {
    const policy = this.selectedClaimPolicy();
    if (!policy || !this.claimReady() || this.taskSubmitting()) return;
    const typeByDomain: Record<string, string> = { HEALTH: 'Health', LIFE: 'Death', MOTOR: 'Accident' };
    this.taskSubmitting.set(true);
    this.taskError.set(null);
    this.claims.intimate({ policyId: policy.id, claimType: (typeByDomain[policy.domain.toUpperCase()] ?? 'Health') as any, claimAmountRequested: this.claimAmount()!, incidentDate: this.claimDate(), incidentDescription: this.claimDescription().trim(), isCashless: false }).subscribe({
      next: claim => this.uploadWorkspaceClaimFiles(claim.id, 0),
      error: failure => { this.taskSubmitting.set(false); this.taskError.set(failure?.error?.title ?? 'The claim could not be submitted.'); },
    });
  }

  private uploadWorkspaceClaimFiles(claimId: string, index: number): void {
    const files = this.claimFiles();
    if (index >= files.length) {
      this.taskSubmitting.set(false);
      this.workspaceTask.set(null);
      const message = files.length ? 'Your claim and supporting documents have been submitted for review. You can track its live status here at any time.' : 'Your claim has been submitted. You can add supporting documents and track its live status here at any time.';
      this.messages.update(messages => [...messages, { role: 'assistant', content: message }]);
      this.announce(message);
      return;
    }
    const file = files[index];
    const key = `SUPPORTING_DOCUMENT_${index + 1}`;
    this.claims.uploadDocument(claimId, key, file).subscribe({
      next: () => this.uploadWorkspaceClaimFiles(claimId, index + 1),
      error: () => { this.taskSubmitting.set(false); this.taskError.set('Your claim was submitted, but one or more documents could not be uploaded. Use “Track my claims” to retry safely.'); },
    });
  }

  private taskFailed(message: string): void { this.taskLoading.set(false); this.taskError.set(message); }

  chooseFile(kind: 'aadhaar' | 'pan', event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.item(0) ?? null;
    const error = file ? this.kycFileError(file) : '';
    if (kind === 'aadhaar') {
      this.aadhaarFileError.set(error);
      this.aadhaarFile.set(error ? null : file);
    } else {
      this.panFileError.set(error);
      this.panFile.set(error ? null : file);
    }
    if (error) input.value = '';
  }

  submitKyc(): void {
    if (!this.kycReady() || this.kycSubmitting()) return;
    this.kycSubmitting.set(true);
    this.kycError.set(null);
    this.profile.uploadAadhaar(this.aadhaarFile()!, this.aadhaarNumber().trim()).pipe(
      concatMap(() => this.profile.uploadPan(this.panFile()!, this.panNumber().trim().toUpperCase())),
    ).subscribe({
      next: kyc => {
        this.kycSubmitting.set(false);
        this.kycRecord.set(kyc);
        this.showKyc.set(false);
        this.resetKycForm();
        const message = 'Your Aadhaar and PAN have been submitted and are awaiting underwriter review. You do not need to submit them again. We will notify you in SpeedClaim and by email once review is complete.';
        this.messages.update(messages => [...messages, { role: 'assistant', content: message }]);
        this.announce(message);
      },
      error: () => {
        this.kycSubmitting.set(false);
        this.kycError.set('We could not upload both documents. Any completed upload remains visible in KYC, where you can safely finish the missing document.');
      },
    });
  }

  toggleVoice(): void {
    if (this.listening()) return;
    const BrowserWindow = window as Window & { SpeechRecognition?: new () => BrowserSpeechRecognition; webkitSpeechRecognition?: new () => BrowserSpeechRecognition };
    const Recognition = BrowserWindow.SpeechRecognition ?? BrowserWindow.webkitSpeechRecognition;
    if (!Recognition) {
      this.error.set('Voice input is not available in this browser. You can continue by typing.');
      return;
    }
    const recognition = new Recognition();
    recognition.lang = 'en-IN';
    recognition.interimResults = false;
    recognition.maxAlternatives = 1;
    recognition.onstart = () => this.listening.set(true);
    recognition.onend = () => this.listening.set(false);
    recognition.onerror = () => { this.listening.set(false); this.error.set('Voice input could not be captured. Please try again or type your question.'); };
    recognition.onresult = event => this.question.set(event.results[0][0].transcript);
    recognition.start();
  }

  /**
   * Speedy returns a deliberately small Markdown subset. Escape first so model
   * output cannot introduce executable HTML, then add only presentation tags.
   */
  renderMarkdown(content: string): string {
    const lines = content.replace(/\r\n?/g, '\n').split('\n');
    const output: string[] = [];
    let openList: 'ol' | 'ul' | null = null;

    const closeList = (): void => {
      if (openList) output.push(`</${openList}>`);
      openList = null;
    };

    for (const line of lines) {
      const numbered = /^\s*\d+[.)]\s+(.+)$/.exec(line);
      const bullet = /^\s*[-*+]\s+(.+)$/.exec(line);
      const listType = numbered ? 'ol' : bullet ? 'ul' : null;
      if (listType) {
        if (openList && openList !== listType) closeList();
        if (!openList) {
          output.push(`<${listType}>`);
          openList = listType;
        }
        output.push(`<li>${this.renderInlineMarkdown((numbered ?? bullet)![1])}</li>`);
        continue;
      }
      closeList();
      if (!line.trim()) continue;
      output.push(`<p>${this.renderInlineMarkdown(line)}</p>`);
    }
    closeList();
    return output.join('');
  }

  private renderInlineMarkdown(value: string): string {
    return this.escapeHtml(value)
      .replace(/\*\*([^*]+)\*\*/g, '<strong>$1</strong>')
      .replace(/`([^`]+)`/g, '<code>$1</code>');
  }

  private kycFileError(file: File): string {
    if (file.size > KYC_MAX_FILE_SIZE_BYTES) return 'File exceeds 5 MB limit.';
    const extension = `.${file.name.split('.').pop()?.toLowerCase() ?? ''}`;
    return KYC_ALLOWED_EXTENSIONS.includes(extension) ? '' : 'Choose a PDF, JPG, JPEG, or PNG file.';
  }

  private loadKyc(): void {
    this.kycLoaded.set(true);
    this.profile.getKyc().subscribe({
      next: kyc => this.kycRecord.set(kyc),
      error: () => this.kycRecord.set(null),
    });
  }

  private resetKycForm(): void {
    this.aadhaarNumber.set('');
    this.panNumber.set('');
    this.aadhaarFile.set(null);
    this.panFile.set(null);
    this.aadhaarFileError.set('');
    this.panFileError.set('');
    this.kycError.set(null);
  }

  private escapeHtml(value: string): string {
    return value.replace(/[&<>"']/g, character => ({
      '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;',
    })[character]!);
  }

  private announce(text: string): void {
    if ('speechSynthesis' in window) window.speechSynthesis.speak(new SpeechSynthesisUtterance(text));
  }

  private loadConversations(): void {
    this.historyLoaded.set(true);
    this.refreshConversations();
  }

  private refreshConversations(): void {
    this.speedy.listWorkspaceConversations().subscribe({
      next: conversations => {
        this.conversations.set(conversations);
        this.historyError.set(false);
      },
      error: () => this.historyError.set(true),
    });
  }

  private ageInDays(value: string): number {
    return Math.max(0, (Date.now() - new Date(value).getTime()) / 86_400_000);
  }
}
