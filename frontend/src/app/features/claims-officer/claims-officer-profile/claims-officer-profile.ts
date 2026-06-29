import { Component, inject, signal } from '@angular/core';
import { AuthService } from '../../../core/services/auth.service';
import { ToastService } from '../../../shared/components/toast/toast.service';

@Component({
  selector: 'app-claims-officer-profile',
  standalone: true,
  templateUrl: './claims-officer-profile.html',
})
export class ClaimsOfficerProfileComponent {
  private authService = inject(AuthService);
  private toast = inject(ToastService);
  resettingPassword = signal(false);

  userInitials(): string {
    const u = this.authService.currentUser();
    if (!u) return '?';
    return (u.firstName.charAt(0) + u.lastName.charAt(0)).toUpperCase();
  }

  fullName(): string {
    const u = this.authService.currentUser();
    return u ? `${u.firstName} ${u.lastName}` : '';
  }

  email(): string {
    return this.authService.currentUser()?.email ?? '';
  }

  phone(): string {
    return this.authService.currentUser()?.phone ?? '';
  }

  onLogout(): void {
    this.authService.logout();
  }

  onResetPassword(): void {
    if (this.resettingPassword()) return;
    const email = this.authService.currentUser()?.email;
    if (!email) return;
    this.resettingPassword.set(true);
    this.authService.forgotPassword({ email }).subscribe({
      next: () => {
        this.toast.success('Password reset link sent to your registered email');
        this.resettingPassword.set(false);
      },
      error: () => {
        this.toast.error('Could not send reset link');
        this.resettingPassword.set(false);
      },
    });
  }
}
