import { Component, inject, signal, OnInit, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { ToastService } from '../../../shared/components/toast/toast.service';

const SAVED_EMAIL_KEY = 'sc_saved_email';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './login.html',
})
export class LoginComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);
  private readonly platformId = inject(PLATFORM_ID);

  loading = signal(false);
  errorMessage = signal('');
  showResendVerification = signal(false);
  showPassword = signal(false);
  resendLoading = signal(false);
  resendSuccess = signal(false);
  private lastEmail = '';

  form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', Validators.required],
    rememberMe: [false],
  });

  ngOnInit(): void {
    if (isPlatformBrowser(this.platformId)) {
      const savedEmail = localStorage.getItem(SAVED_EMAIL_KEY);
      if (savedEmail) {
        this.form.controls.email.setValue(savedEmail);
        this.form.controls.rememberMe.setValue(true);
      }
    }
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading.set(true);
    this.errorMessage.set('');
    this.showResendVerification.set(false);
    this.resendSuccess.set(false);

    const { email, password, rememberMe } = this.form.getRawValue();

    if (isPlatformBrowser(this.platformId)) {
      if (rememberMe) {
        localStorage.setItem(SAVED_EMAIL_KEY, email);
      } else {
        localStorage.removeItem(SAVED_EMAIL_KEY);
      }
    }

    this.authService.login({ email, password }, rememberMe).subscribe({
      next: (res) => {
        this.loading.set(false);
        this.toast.success('Welcome back!');
        const roleRoutes: Record<string, string> = {
          Admin: '/admin',
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
        if (err.status === 422) {
          this.errorMessage.set(err.error?.detail || 'Please verify your email address before signing in.');
          this.showResendVerification.set(true);
          this.lastEmail = email;
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
