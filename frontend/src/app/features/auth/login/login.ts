import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { ToastService } from '../../../shared/components/toast/toast.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './login.html',
})
export class LoginComponent {
  private fb = inject(FormBuilder);
  private authService = inject(AuthService);
  private router = inject(Router);
  private toast = inject(ToastService);

  loading = signal(false);
  errorMessage = signal('');
  showResendVerification = signal(false);
  showRegisterLink = signal(false);
  showPassword = signal(false);
  resendLoading = signal(false);
  resendSuccess = signal(false);
  private lastEmail = '';

  form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', Validators.required],
  });

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading.set(true);
    this.errorMessage.set('');
    this.showResendVerification.set(false);
    this.showRegisterLink.set(false);
    this.resendSuccess.set(false);

    this.authService.login(this.form.getRawValue()).subscribe({
      next: (res) => {
        this.loading.set(false);
        this.toast.success('Welcome back!');
        const roleRoutes: Record<string, string> = {
          Agent: '/agent',
          ClaimsOfficer: '/claims-officer',
          FinanceOfficer: '/finance-officer',
          Underwriter: '/underwriter',
          Surveyor: '/surveyor',
        };
        this.router.navigate([roleRoutes[res.user.role] ?? '/dashboard']);
      },
      error: (err) => {
        this.loading.set(false);
        if (err.status === 404) {
          this.errorMessage.set(err.error?.detail || 'No account found with this email address.');
          this.showRegisterLink.set(true);
        } else if (err.status === 422) {
          this.errorMessage.set(err.error?.detail || 'Please verify your email address before signing in.');
          this.showResendVerification.set(true);
          this.lastEmail = this.form.getRawValue().email;
        } else if (err.status === 0) {
          this.errorMessage.set('Unable to connect. Please check your internet connection.');
        } else {
          this.errorMessage.set(err.error?.detail || 'Something went wrong. Please try again.');
        }
      },
    });
  }

  resendVerification(): void {
    if (!this.lastEmail) return;
    this.resendLoading.set(true);
    this.authService.resendVerificationEmail({ email: this.lastEmail }).subscribe({
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
