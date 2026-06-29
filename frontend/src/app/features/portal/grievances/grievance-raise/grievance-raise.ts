import { Component, inject, signal, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { GrievanceService } from '../services/grievance.service';
import { ClaimDto, PolicyDto, RaiseGrievanceRequest } from '../../../../core/models/api.models';
import { ToastService } from '../../../../shared/components/toast/toast.service';
import { PolicyService } from '../../policies/services/policy.service';
import { ClaimService } from '../../claims/services/claim.service';

@Component({
  selector: 'app-grievance-raise',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './grievance-raise.html',
})
export class GrievanceRaiseComponent implements OnInit {
  private fb = inject(FormBuilder);
  private grievanceService = inject(GrievanceService);
  private policyService = inject(PolicyService);
  private claimService = inject(ClaimService);
  private toast = inject(ToastService);
  router = inject(Router);

  submitting = signal(false);
  policies = signal<PolicyDto[]>([]);
  claims = signal<ClaimDto[]>([]);

  form = this.fb.group({
    category: ['ClaimDelay', Validators.required],
    policyId: [''],
    claimId: [''],
    description: ['', [Validators.required, Validators.minLength(10), Validators.maxLength(2000)]],
  });

  ngOnInit(): void {
    this.policyService.getMyPolicies().subscribe({ next: policies => this.policies.set(policies) });
    this.claimService.getMyClaims().subscribe({ next: claims => this.claims.set(claims) });
  }

  submit(): void {
    if (this.form.invalid || this.submitting()) return;

    const raw = this.form.getRawValue();
    const request: RaiseGrievanceRequest = {
      category: raw.category as RaiseGrievanceRequest['category'],
      description: raw.description ?? '',
      policyId: raw.policyId || undefined,
      claimId: raw.claimId || undefined,
    };

    this.submitting.set(true);
    this.grievanceService.raise(request).subscribe({
      next: () => {
        this.toast.success('Grievance submitted');
        this.router.navigate(['/grievances']);
      },
      error: () => { this.submitting.set(false); this.toast.error('Submission failed'); },
    });
  }
}
