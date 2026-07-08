import { Component, computed, inject, signal, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { AbstractControl, FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { from, of } from 'rxjs';
import { catchError, concatMap } from 'rxjs/operators';
import { ClaimService } from '../services/claim.service';
import { PolicyService } from '../../policies/services/policy.service';
import { PolicyDto, IntimateClaimRequest } from '../../../../core/models/api.models';
import { ClaimType } from '../../../../core/models/enums';
import { FileUploadComponent } from '../../../../shared/components/file-upload/file-upload';
import { MoneyPipe } from '../../../../shared/pipes/money.pipe';
import { ToastService } from '../../../../shared/components/toast/toast.service';
import { DateFormatPipe } from '../../../../shared/pipes/date-format.pipe';

type ClaimTypeOption = { value: ClaimType; label: string };

@Component({
  selector: 'app-claim-file',
  standalone: true,
  imports: [ReactiveFormsModule, FileUploadComponent, MoneyPipe, DateFormatPipe],
  templateUrl: './claim-file.html',
})
export class ClaimFileComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly claimService = inject(ClaimService);
  private readonly policyService = inject(PolicyService);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);

  step = signal(0);
  activePolicies = signal<PolicyDto[]>([]);
  policiesLoading = signal(true);
  submitting = signal(false);
  uploadedFiles: File[] = [];
  stepLabels = ['Select Policy', 'Details', 'Documents'];
  today = new Date().toISOString().slice(0, 10);
  readonly minClaimAmount = 500;
  readonly maxDocuments = 5;
  readonly maxFileSizeMb = 5;
  private readonly claimTypesByDomain: Record<string, ClaimTypeOption[]> = {
    health: [{ value: 'Health', label: 'Health' }],
    life: [
      { value: 'Death', label: 'Death' },
      { value: 'Maturity', label: 'Maturity' },
    ],
    motor: [
      { value: 'Accident', label: 'Accident' },
      { value: 'Theft', label: 'Theft' },
      { value: 'NaturalDamage', label: 'Natural Damage' },
    ],
  };

  policyControl = this.fb.control('', Validators.required);
  selectedPolicy = signal<PolicyDto | null>(null);
  maxIncidentDate = computed(() => {
    const policyEndDate = this.selectedPolicy()?.endDate?.slice(0, 10);
    return policyEndDate && policyEndDate < this.today ? policyEndDate : this.today;
  });

  private readonly withinPolicyCoverage = (control: AbstractControl): { aboveCoverage: true } | null => {
    const policy = this.selectedPolicy();
    const amount = Number(control.value);
    if (!policy || !amount) return null;

    return amount > policy.coverageAmount ? { aboveCoverage: true } : null;
  };

  private readonly withinPolicyPeriod = (control: AbstractControl): { outsidePolicyPeriod: true } | null => {
    const policy = this.selectedPolicy();
    const value = control.value;
    if (!policy || !value) return null;

    const incident = this.toLocalDate(value);
    const start = this.toLocalDate(policy.startDate);
    const end = this.toLocalDate(policy.endDate);

    return incident < start || incident > end ? { outsidePolicyPeriod: true } : null;
  };

  claimForm = this.fb.group({
    claimType: ['Health' as ClaimType, Validators.required],
    claimAmountRequested: [0, [Validators.required, Validators.min(this.minClaimAmount), this.withinPolicyCoverage]],
    incidentDate: ['', [Validators.required, this.notFutureDate, this.withinPolicyPeriod]],
    incidentDescription: ['', [Validators.required, Validators.minLength(10)]],
  });

  ngOnInit(): void {
    this.policyService.getMyPolicies('Active').subscribe({
      next: p => { this.activePolicies.set(p); this.policiesLoading.set(false); },
      error: () => this.policiesLoading.set(false),
    });
  }

  onFileSelected(file: File): void {
    if (this.uploadedFiles.length >= this.maxDocuments) {
      this.toast.warning(`You can attach up to ${this.maxDocuments} documents`);
      return;
    }
    this.uploadedFiles = [...this.uploadedFiles, file];
  }

  onFileRemoved(file: File): void {
    this.uploadedFiles = this.uploadedFiles.filter(uploaded => uploaded !== file);
  }

  selectPolicy(policy: PolicyDto): void {
    if (!this.isPolicyClaimable(policy)) return;
    this.policyControl.setValue(policy.id);
    this.selectedPolicy.set(policy);
    this.claimForm.controls.claimType.setValue(this.claimTypeOptions()[0]?.value ?? 'Health');
    this.claimForm.controls.claimAmountRequested.updateValueAndValidity();
    this.claimForm.controls.incidentDate.updateValueAndValidity();
  }

  claimTypeOptions(): ClaimTypeOption[] {
    const domain = this.selectedPolicy()?.domain?.toString().toLowerCase();
    if (!domain) return this.claimTypesByDomain['health'];
    return this.claimTypesByDomain[domain] ?? this.claimTypesByDomain['health'];
  }

  isPolicyClaimable(policy: PolicyDto): boolean {
    const startDate = policy.startDate?.slice(0, 10);
    const endDate = policy.endDate?.slice(0, 10);
    return (!startDate || startDate <= this.today) && (!endDate || endDate >= this.today);
  }

  policyClaimAvailability(policy: PolicyDto): string | null {
    const startDate = policy.startDate?.slice(0, 10);
    const endDate = policy.endDate?.slice(0, 10);
    if (startDate && startDate > this.today) return 'Claims open from';
    if (endDate && endDate < this.today) return 'Coverage ended on';
    return null;
  }

  policyClaimAvailabilityDate(policy: PolicyDto): string | null {
    const startDate = policy.startDate?.slice(0, 10);
    const endDate = policy.endDate?.slice(0, 10);
    if (startDate && startDate > this.today) return policy.startDate;
    if (endDate && endDate < this.today) return policy.endDate;
    return null;
  }

  submit(): void {
    if (this.submitting() || !this.policyControl.value || this.claimForm.invalid) return;
    this.submitting.set(true);
    const req: IntimateClaimRequest = {
      policyId: this.policyControl.value ?? '',
      ...this.claimForm.getRawValue() as any,
      isCashless: false,
    };
    this.claimService.intimate(req).subscribe({
      next: claim => {
        if (this.uploadedFiles.length === 0) {
          this.toast.success('Claim filed successfully');
          this.router.navigate(['/claims', claim.id]);
          return;
        }
        const usedKeys = new Set<string>();
        const files = this.uploadedFiles.map(file => {
          const base = this.documentKeyFor(file);
          let key = base;
          let suffix = 2;
          while (usedKeys.has(key)) key = `${base}_${suffix++}`;
          usedKeys.add(key);
          return { file, key };
        });
        // Uploaded sequentially, not in parallel: each upload can flip the claim's
        // status from Intimated to UnderReview server-side, and concurrent requests
        // would each read the pre-transition status and duplicate that transition
        // (and its notification/email/audit entries) once per file.
        let anyFailed = false;
        from(files).pipe(
          concatMap(({ file, key }) =>
            this.claimService.uploadDocument(claim.id, key, file).pipe(
              catchError(() => { anyFailed = true; return of(null); }),
            ),
          ),
        ).subscribe({
          complete: () => {
            if (anyFailed) this.toast.warning('Claim filed but some documents failed to upload');
            else this.toast.success('Claim filed successfully');
            this.router.navigate(['/claims', claim.id]);
          },
        });
      },
      error: () => { this.submitting.set(false); this.toast.error('Failed to file claim. Please try again.'); },
    });
  }

  private notFutureDate(control: AbstractControl): { futureDate: true } | null {
    const value = control.value;
    if (!value) return null;

    const selected = new Date(`${value}T00:00:00`);
    const today = new Date();
    today.setHours(23, 59, 59, 999);

    return selected > today ? { futureDate: true } : null;
  }

  private toLocalDate(value: string): Date {
    const [datePart] = value.split('T');
    const [year, month, day] = datePart.split('-').map(Number);
    return new Date(year, month - 1, day);
  }

  private documentKeyFor(file: File): string {
    const baseName = file.name.replace(/\.[^/.]+$/, '');
    const sanitized = baseName.replace(/[^a-zA-Z0-9_-]+/g, '_');
    let start = 0;
    let end = sanitized.length;
    while (start < end && sanitized[start] === '_') start++;
    while (end > start && sanitized[end - 1] === '_') end--;
    const key = sanitized.slice(start, end);
    return (key || 'SUPPORTING_DOCUMENT').slice(0, 100).toUpperCase();
  }
}
