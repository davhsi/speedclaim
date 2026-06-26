import { Component, inject, signal, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
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

  policyControl = this.fb.control('', Validators.required);

  claimForm = this.fb.group({
    claimType: ['Health', Validators.required],
    claimAmountRequested: [0, [Validators.required, Validators.min(1)]],
    incidentDate: ['', Validators.required],
    incidentDescription: ['', [Validators.required, Validators.minLength(10)]],
    isCashless: [false],
  });

  ngOnInit(): void {
    this.policyService.getMyPolicies('Active').subscribe({
      next: p => { this.activePolicies.set(p); this.policiesLoading.set(false); },
      error: () => this.policiesLoading.set(false),
    });
  }

  onFileSelected(file: File): void { this.uploadedFiles.push(file); }

  submit(): void {
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
        let done = 0;
        for (const file of this.uploadedFiles) {
          this.claimService.uploadDocument(claim.id, file.name.split('.')[0], file).subscribe({
            next: () => { if (++done === this.uploadedFiles.length) { this.toast.success('Claim filed successfully'); this.router.navigate(['/claims', claim.id]); } },
            error: () => { if (++done === this.uploadedFiles.length) { this.toast.warning('Claim filed but some documents failed to upload'); this.router.navigate(['/claims', claim.id]); } },
          });
        }
      },
      error: () => { this.submitting.set(false); },
    });
  }
}
