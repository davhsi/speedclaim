import { Component, computed, inject, signal, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { AbstractControl, FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ClaimService } from '../services/claim.service';
import { PolicyService } from '../../policies/services/policy.service';
import { PolicyDto, IntimateClaimRequest } from '../../../../core/models/api.models';
import { FileUploadComponent } from '../../../../shared/components/file-upload/file-upload';
import { MoneyPipe } from '../../../../shared/pipes/money.pipe';
import { ToastService } from '../../../../shared/components/toast/toast.service';

@Component({
  selector: 'app-claim-file',
  standalone: true,
  imports: [ReactiveFormsModule, FileUploadComponent, MoneyPipe],
  templateUrl: './claim-file.html',
})
export class ClaimFileComponent implements OnInit {
  private fb = inject(FormBuilder);
  private claimService = inject(ClaimService);
  private policyService = inject(PolicyService);
  private router = inject(Router);
  private toast = inject(ToastService);

  step = signal(0);
  activePolicies = signal<PolicyDto[]>([]);
  policiesLoading = signal(true);
  submitting = signal(false);
  uploadedFiles: File[] = [];
  stepLabels = ['Select Policy', 'Details', 'Documents'];
  today = new Date().toISOString().slice(0, 10);

  policyControl = this.fb.control('', Validators.required);
  selectedPolicy = signal<PolicyDto | null>(null);
  maxIncidentDate = computed(() => {
    const policyEndDate = this.selectedPolicy()?.endDate?.slice(0, 10);
    return policyEndDate && policyEndDate < this.today ? policyEndDate : this.today;
  });

  private withinPolicyCoverage = (control: AbstractControl): { aboveCoverage: true } | null => {
    const policy = this.selectedPolicy();
    const amount = Number(control.value);
    if (!policy || !amount) return null;

    return amount > policy.coverageAmount ? { aboveCoverage: true } : null;
  };

  private withinPolicyPeriod = (control: AbstractControl): { outsidePolicyPeriod: true } | null => {
    const policy = this.selectedPolicy();
    const value = control.value;
    if (!policy || !value) return null;

    const incident = this.toLocalDate(value);
    const start = this.toLocalDate(policy.startDate);
    const end = this.toLocalDate(policy.endDate);

    return incident < start || incident > end ? { outsidePolicyPeriod: true } : null;
  };

  claimForm = this.fb.group({
    claimType: ['Health', Validators.required],
    claimAmountRequested: [0, [Validators.required, Validators.min(1), this.withinPolicyCoverage]],
    incidentDate: ['', [Validators.required, this.notFutureDate, this.withinPolicyPeriod]],
    incidentDescription: ['', [Validators.required, Validators.minLength(10)]],
    isCashless: [false],
  });

  ngOnInit(): void {
    this.policyService.getMyPolicies('Active').subscribe({
      next: p => { this.activePolicies.set(p); this.policiesLoading.set(false); },
      error: () => this.policiesLoading.set(false),
    });
  }

  onFileSelected(file: File): void {
    this.uploadedFiles = [file];
  }

  onFileRemoved(file: File): void {
    this.uploadedFiles = this.uploadedFiles.filter(uploaded => uploaded !== file);
  }

  selectPolicy(policy: PolicyDto): void {
    this.policyControl.setValue(policy.id);
    this.selectedPolicy.set(policy);
    this.claimForm.controls.claimAmountRequested.updateValueAndValidity();
    this.claimForm.controls.incidentDate.updateValueAndValidity();
  }

  submit(): void {
    if (this.submitting() || !this.policyControl.value || this.claimForm.invalid) return;
    this.submitting.set(true);
    const req: IntimateClaimRequest = {
      policyId: this.policyControl.value ?? '',
      ...this.claimForm.getRawValue() as any,
    };
    this.claimService.intimate(req).subscribe({
      next: claim => {
        if (this.uploadedFiles.length === 0) {
          this.toast.success('Claim filed successfully');
          this.router.navigate(['/claims', claim.id]);
          return;
        }
        const files = [...this.uploadedFiles];
        let done = 0;
        for (const file of files) {
          this.claimService.uploadDocument(claim.id, this.documentKeyFor(file), file).subscribe({
            next: () => { if (++done === files.length) { this.toast.success('Claim filed successfully'); this.router.navigate(['/claims', claim.id]); } },
            error: () => { if (++done === files.length) { this.toast.warning('Claim filed but some documents failed to upload'); this.router.navigate(['/claims', claim.id]); } },
          });
        }
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
    const key = baseName.replace(/[^a-zA-Z0-9_-]+/g, '_').replace(/^_+|_+$/g, '');
    return (key || 'SUPPORTING_DOCUMENT').slice(0, 100).toUpperCase();
  }
}
