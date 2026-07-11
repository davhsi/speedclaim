import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { HttpClient, HttpContext } from '@angular/common/http';
import { from, Observable, of, Subject } from 'rxjs';
import { catchError, concatMap, map, toArray } from 'rxjs/operators';
import { SKIP_ERROR_TOAST } from '../../../core/interceptors/error.interceptor';
import { AgentService, AgentCustomerDto } from '../services/agent.service';
import { KycRecordDto } from '../../../core/models/api.models';
import { FileUploadComponent } from '../../../shared/components/file-upload/file-upload';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge';
import { ConfirmDialogComponent } from '../../../shared/components/confirm-dialog/confirm-dialog';
import { ToastService } from '../../../shared/components/toast/toast.service';
import { CanComponentDeactivate } from '../../../core/guards/unsaved-changes.guard';

const AADHAAR_PATTERN = /^\d{12}$/;
const PAN_PATTERN = /^[A-Z]{5}\d{4}[A-Z]$/;

@Component({
  selector: 'app-agent-customer-kyc',
  standalone: true,
  imports: [FormsModule, RouterLink, FileUploadComponent, StatusBadgeComponent, ConfirmDialogComponent],
  templateUrl: './customer-kyc.html',
})
export class AgentCustomerKycComponent implements OnInit, CanComponentDeactivate {
  private readonly agentService = inject(AgentService);
  private readonly http = inject(HttpClient);
  private readonly toast = inject(ToastService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  customers = signal<AgentCustomerDto[]>([]);
  selectedCustomerId = signal<string | null>(null);
  existingKyc = signal<KycRecordDto | null>(null);
  idType = signal<'aadhaar' | 'pan'>('aadhaar');
  submitting = signal(false);

  // Each document type keeps its own number + file so switching tabs never
  // discards what's already been filled in for the other one.
  aadhaarNumber = signal('');
  panNumber = signal('');
  aadhaarFile = signal<File | null>(null);
  panFile = signal<File | null>(null);

  aadhaarValid = computed(() => AADHAAR_PATTERN.test(this.aadhaarNumber().trim()));
  panValid = computed(() => PAN_PATTERN.test(this.panNumber().trim().toUpperCase()));
  aadhaarReady = computed(() => this.aadhaarValid() && !!this.aadhaarFile());
  panReady = computed(() => this.panValid() && !!this.panFile());

  currentNumber = computed(() => (this.idType() === 'aadhaar' ? this.aadhaarNumber() : this.panNumber()));

  idValid = computed(() => (this.idType() === 'aadhaar' ? this.aadhaarValid() : this.panValid()));

  idError = computed(() => {
    const v = this.currentNumber().trim();
    if (!v) return '';
    if (this.idValid()) return '';
    return this.idType() === 'aadhaar'
      ? 'Aadhaar must be exactly 12 digits.'
      : 'PAN must be in the format ABCDE1234F.';
  });

  // Once submitted, KYC is locked while an Underwriter reviews it — only a Rejected
  // record (or a customer who never submitted at all) can be (re)uploaded here.
  canEditKyc = computed(() => {
    const kyc = this.existingKyc();
    return !kyc || kyc.kycStatus === 'Rejected';
  });

  canSubmit = computed(
    () => !!this.selectedCustomerId() && this.canEditKyc() && (this.aadhaarReady() || this.panReady()),
  );

  hasUnsavedInput = computed(
    () => !!this.aadhaarNumber().trim() || !!this.panNumber().trim() || !!this.aadhaarFile() || !!this.panFile(),
  );

  showLeaveConfirm = signal(false);
  private navigatedAfterSubmit = false;
  private leaveSubject: Subject<boolean> | null = null;

  canDeactivate(): boolean | Observable<boolean> {
    if (this.navigatedAfterSubmit || !this.hasUnsavedInput()) return true;

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

  selectIdType(type: 'aadhaar' | 'pan'): void {
    this.idType.set(type);
  }

  onNumberInput(value: string): void {
    if (this.idType() === 'aadhaar') this.aadhaarNumber.set(value);
    else this.panNumber.set(value.toUpperCase());
  }

  ngOnInit(): void {
    this.agentService.getCustomers().subscribe(c => {
      this.customers.set(c);
      const preselectId = this.route.snapshot.queryParamMap.get('customerId');
      if (preselectId && c.some(cust => cust.id === preselectId)) {
        this.selectCustomer(preselectId);
      }
    });
  }

  onCustomerChange(event: Event): void {
    const id = (event.target as HTMLSelectElement).value;
    this.selectCustomer(id || null);
  }

  private selectCustomer(id: string | null): void {
    this.selectedCustomerId.set(id);
    this.existingKyc.set(null);
    if (id) {
      this.http
        .get<KycRecordDto>(`/api/v1/agents/customers/${id}/kyc`, {
          context: new HttpContext().set(SKIP_ERROR_TOAST, true),
        })
        .subscribe({
          next: k => this.existingKyc.set(k),
          error: () => {},
        });
    }
  }

  submit(): void {
    if (this.submitting() || !this.canSubmit()) return;
    this.submitting.set(true);

    const customerId = this.selectedCustomerId()!;
    const calls: { label: string; obs: Observable<{ label: string; ok: boolean }> }[] = [];

    if (this.aadhaarReady()) {
      const fd = new FormData();
      fd.append('document', this.aadhaarFile()!);
      fd.append('customerId', customerId);
      fd.append('aadhaarNumber', this.aadhaarNumber().trim());
      calls.push({
        label: 'Aadhaar',
        obs: this.http.post('/api/v1/users/kyc/aadhaar', fd).pipe(
          map(() => ({ label: 'Aadhaar', ok: true })),
          catchError(() => of({ label: 'Aadhaar', ok: false })),
        ),
      });
    }
    if (this.panReady()) {
      const fd = new FormData();
      fd.append('document', this.panFile()!);
      fd.append('customerId', customerId);
      fd.append('panNumber', this.panNumber().trim().toUpperCase());
      calls.push({
        label: 'PAN',
        obs: this.http.post('/api/v1/users/kyc/pan', fd).pipe(
          map(() => ({ label: 'PAN', ok: true })),
          catchError(() => of({ label: 'PAN', ok: false })),
        ),
      });
    }

    // Run sequentially, not in parallel: both endpoints lazily create a shared
    // KycRecord row for this customer if one doesn't exist yet, and two concurrent
    // requests for a brand-new customer both see "no row" and race to insert it,
    // tripping the IX_kyc_records_user_id unique constraint on the loser.
    from(calls).pipe(
      concatMap(c => c.obs),
      toArray(),
    ).subscribe(results => {
      this.submitting.set(false);
      const succeeded = results.filter(r => r.ok).map(r => r.label);
      const failed = results.filter(r => !r.ok).map(r => r.label);
      if (succeeded.length) this.toast.success(`${succeeded.join(' & ')} uploaded successfully`);
      if (failed.length) this.toast.error(`${failed.join(' & ')} upload failed`);
      if (!failed.length) {
        this.navigatedAfterSubmit = true;
        this.router.navigate(['/agent/customers']);
      }
    });
  }
}
