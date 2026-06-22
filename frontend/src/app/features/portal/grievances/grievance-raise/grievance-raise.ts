import { Component, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { GrievanceService } from '../services/grievance.service';
import { RaiseGrievanceRequest } from '../../../../core/models/api.models';
import { ToastService } from '../../../../shared/components/toast/toast.service';

@Component({
  selector: 'app-grievance-raise',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './grievance-raise.html',
})
export class GrievanceRaiseComponent {
  private fb = inject(FormBuilder);
  private grievanceService = inject(GrievanceService);
  private toast = inject(ToastService);
  router = inject(Router);

  submitting = signal(false);

  form = this.fb.group({
    category: ['ClaimDelay', Validators.required],
    description: ['', [Validators.required, Validators.minLength(10), Validators.maxLength(2000)]],
  });

  submit(): void {
    this.submitting.set(true);
    this.grievanceService.raise(this.form.getRawValue() as RaiseGrievanceRequest).subscribe({
      next: () => {
        this.toast.success('Grievance submitted');
        this.router.navigate(['/grievances']);
      },
      error: () => { this.submitting.set(false); this.toast.error('Submission failed'); },
    });
  }
}
