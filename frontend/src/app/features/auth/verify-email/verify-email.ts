import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-verify-email',
  standalone: true,
  imports: [RouterLink, ReactiveFormsModule],
  templateUrl: './verify-email.html',
})
export class VerifyEmailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private authService = inject(AuthService);
  private fb = inject(FormBuilder);

  loading = signal(true);
  success = signal(false);
  errorMessage = signal('');
  showResendForm = signal(false);
  resendLoading = signal(false);
  resendSuccess = signal(false);

  resendForm = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
  });

  ngOnInit(): void {
    const token = this.route.snapshot.queryParams['token'];
    if (!token) {
      this.loading.set(false);
      this.errorMessage.set('Invalid or missing verification token.');
      this.showResendForm.set(true);
      return;
    }

    this.authService.verifyEmail({ token }).subscribe({
      next: () => {
        this.loading.set(false);
        this.success.set(true);
        setTimeout(() => this.router.navigate(['/auth/login']), 3000);
      },
      error: () => {
        this.loading.set(false);
        this.errorMessage.set('Verification link is invalid or has expired.');
        this.showResendForm.set(true);
      },
    });
  }

  resendVerification(): void {
    if (this.resendForm.invalid) {
      this.resendForm.markAllAsTouched();
      return;
    }
    this.resendLoading.set(true);
    this.authService.resendVerificationEmail(this.resendForm.getRawValue()).subscribe({
      next: () => {
        this.resendLoading.set(false);
        this.resendSuccess.set(true);
      },
      error: () => {
        this.resendLoading.set(false);
      },
    });
  }
}
