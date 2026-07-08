import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { PolicyService } from '../services/policy.service';
import { PaymentRecordDto, PolicyDto, EndorsementDto, PolicyNomineeDto, ProductDto, PremiumScheduleDto } from '../../../../core/models/api.models';
import { PaymentService } from '../../payments/services/payment.service';
import { StatusBadgeComponent } from '../../../../shared/components/status-badge/status-badge';
import { TimelineComponent, TimelineItem } from '../../../../shared/components/timeline/timeline';
import { ConfirmDialogComponent } from '../../../../shared/components/confirm-dialog/confirm-dialog';
import { PaginationComponent } from '../../../../shared/components/pagination/pagination';
import { MoneyPipe } from '../../../../shared/pipes/money.pipe';
import { DateFormatPipe } from '../../../../shared/pipes/date-format.pipe';
import { SafeHtmlPipe } from '../../../../shared/pipes/safe-html.pipe';
import { ToastService } from '../../../../shared/components/toast/toast.service';
import { ProductService } from '../../products/services/product.service';

type ScheduleFilter = 'All' | 'Paid' | 'Upcoming' | 'Due' | 'Overdue';

@Component({
  selector: 'app-policy-detail',
  standalone: true,
  imports: [StatusBadgeComponent, TimelineComponent, ConfirmDialogComponent, PaginationComponent, MoneyPipe, DateFormatPipe, SafeHtmlPipe, ReactiveFormsModule],
  templateUrl: './policy-detail.html',
})
export class PolicyDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly policyService = inject(PolicyService);
  private readonly paymentService = inject(PaymentService);
  private readonly productService = inject(ProductService);
  private readonly fb = inject(FormBuilder);
  private readonly toast = inject(ToastService);
  router = inject(Router);

  policy = signal<PolicyDto | null>(null);
  product = signal<ProductDto | null>(null);
  nominees = signal<PolicyNomineeDto[]>([]);
  endorsements = signal<EndorsementDto[]>([]);
  schedules = signal<PremiumScheduleDto[]>([]);
  private readonly policyHistoryItems = signal<TimelineItem[]>([]);
  private readonly paymentHistoryItems = signal<TimelineItem[]>([]);
  historyItems = computed(() => [...this.paymentHistoryItems(), ...this.policyHistoryItems()]
    .sort((a, b) => new Date(b.date).getTime() - new Date(a.date).getTime()));
  loading = signal(true);
  activeTab = signal(0);
  showCancelDialog = signal(false);
  showEndorsementForm = signal(false);
  tabs = ['Overview', 'Nominees', 'Endorsements', 'Schedule', 'History'];
  readonly schedulePageSize = 10;
  readonly scheduleFilters: ScheduleFilter[] = ['All', 'Paid', 'Upcoming', 'Due', 'Overdue'];
  scheduleFilter = signal<ScheduleFilter>('All');
  schedulePage = signal(1);

  filteredSchedules = computed(() => {
    const filter = this.scheduleFilter();
    return this.schedules().filter(s => filter === 'All' || s.status === filter);
  });

  pagedSchedules = computed(() => {
    const start = (this.schedulePage() - 1) * this.schedulePageSize;
    return this.filteredSchedules().slice(start, start + this.schedulePageSize);
  });

  scheduleTotalPages = computed(() => Math.max(1, Math.ceil(this.filteredSchedules().length / this.schedulePageSize)));

  scheduleCount = computed(() => ({
    total: this.schedules().length,
    paid: this.schedules().filter(s => s.status === 'Paid').length,
    upcoming: this.schedules().filter(s => s.status === 'Upcoming' || s.status === 'Due').length,
    overdue: this.schedules().filter(s => s.status === 'Overdue').length,
  }));

  nextPremium = computed(() => this.schedules().find(s => s.status === 'Overdue' || s.status === 'Due' || s.status === 'Upcoming') ?? null);

  daysToExpiry = computed(() => {
    const p = this.policy();
    if (!p?.endDate || p.status !== 'Active') return null;
    return Math.ceil((new Date(p.endDate).getTime() - Date.now()) / 86400000);
  });

  endorsementForm = this.fb.group({
    endorsementType: ['NomineeChange', Validators.required],
    description: ['', [Validators.required, Validators.minLength(10)]],
  });

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id') ?? '';
    this.policyService.getById(id).subscribe({
      next: p => {
        this.policy.set(p);
        this.productService.getById(p.productId).subscribe({
          next: product => this.product.set(product),
          error: () => this.product.set(null),
        });
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
    this.policyService.getNominees(id).subscribe(n => this.nominees.set(n));
    this.policyService.getEndorsements(id).subscribe(e => this.endorsements.set(e));
    this.policyService.getSchedule(id).subscribe({ next: s => this.schedules.set(s), error: () => {} });
    this.policyService.getHistory(id).subscribe(h =>
      this.policyHistoryItems.set(h.map(i => ({ status: i.status, date: i.changedAt, remarks: i.remarks }))),
    );
    this.paymentService.getHistory().subscribe({
      next: payments => this.paymentHistoryItems.set(payments
        .filter(p => p.policyId === id)
        .map(p => this.mapPaymentToTimelineItem(p))),
      error: () => {},
    });
  }

  setScheduleFilter(filter: ScheduleFilter): void {
    this.scheduleFilter.set(filter);
    this.schedulePage.set(1);
  }

  onSchedulePageChange(page: number): void {
    this.schedulePage.set(Math.min(Math.max(page, 1), this.scheduleTotalPages()));
  }

  domainBgClass(): string {
    const map: Record<string, string> = { HEALTH: 'bg-success-bg', MOTOR: 'bg-info-bg', LIFE: 'bg-[#F3EEFF]' };
    return map[this.displayDomain().toUpperCase()] ?? 'bg-surface-alt';
  }

  domainIcon(): string {
    const map: Record<string, string> = {
      HEALTH: '<svg width="26" height="26" viewBox="0 0 24 24" fill="none" stroke="#1F9D6B" stroke-width="1.75"><path d="M22 12h-4l-3 9L9 3l-3 9H2"/></svg>',
      MOTOR: '<svg width="26" height="26" viewBox="0 0 24 24" fill="none" stroke="#2D7FF9" stroke-width="1.75"><circle cx="7" cy="17" r="2"/><circle cx="17" cy="17" r="2"/><path d="M5 17H3v-6l2-5h9l4 5h3v6h-2"/></svg>',
      LIFE: '<svg width="26" height="26" viewBox="0 0 24 24" fill="none" stroke="#7C3AED" stroke-width="1.75"><path d="M20.84 4.61a5.5 5.5 0 00-7.78 0L12 5.67l-1.06-1.06a5.5 5.5 0 00-7.78 7.78l1.06 1.06L12 21.23l7.78-7.78 1.06-1.06a5.5 5.5 0 000-7.78z"/></svg>',
    };
    return map[this.displayDomain().toUpperCase()] ?? '';
  }

  productName(): string {
    return this.product()?.productName ?? this.policy()?.productName ?? 'Insurance product';
  }

  displayDomain(): string {
    return this.product()?.domain ?? this.policy()?.domain ?? 'Unknown';
  }

  private mapPaymentToTimelineItem(payment: PaymentRecordDto): TimelineItem {
    return {
      status: payment.status,
      date: payment.paidAt ?? payment.createdAt,
      remarks: `${this.formatPaymentType(payment.paymentType)} ${payment.status.toLowerCase()} for ${this.formatAmount(payment.amount, payment.currency)}.`,
    };
  }

  private formatPaymentType(type: string): string {
    const map: Record<string, string> = {
      FirstPremium: 'First premium',
      Renewal: 'Renewal premium',
      Reinstatement: 'Reinstatement premium',
      ClaimPayout: 'Claim payout',
    };
    return map[type] ?? type;
  }

  private formatAmount(amount: number, currency: string): string {
    return new Intl.NumberFormat('en-IN', {
      style: 'currency',
      currency: currency || 'INR',
      maximumFractionDigits: 2,
    }).format(amount);
  }

  downloadCert(): void {
    this.policyService.downloadCertificate(this.policy()!.id).subscribe(blob => {
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `${this.policy()!.policyNumber}-certificate.pdf`;
      a.click();
      URL.revokeObjectURL(url);
    });
  }

  submitEndorsement(): void {
    if (this.endorsementForm.invalid) {
      this.endorsementForm.markAllAsTouched();
      return;
    }
    this.policyService.requestEndorsement(this.policy()!.id, this.endorsementForm.getRawValue() as any).subscribe({
      next: () => {
        this.toast.success('Endorsement request submitted');
        this.showEndorsementForm.set(false);
        this.endorsementForm.reset({ endorsementType: 'NomineeChange' });
        this.policyService.getEndorsements(this.policy()!.id).subscribe(e => this.endorsements.set(e));
      },
      error: () => this.toast.error('Failed to submit endorsement'),
    });
  }

  confirmCancel(): void {
    this.policyService.cancelPolicy(this.policy()!.id).subscribe({
      next: () => {
        this.toast.success('Policy cancelled');
        this.showCancelDialog.set(false);
        this.policy.update(p => p ? { ...p, status: 'Cancelled' as any } : p);
      },
      error: () => this.toast.error('Cancellation failed'),
    });
  }
}
