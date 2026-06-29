import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../../core/services/auth.service';
import { ToastService } from '../../../shared/components/toast/toast.service';
import { FinanceOfficerService } from '../services/finance-officer.service';

@Component({
  selector: 'app-finance-officer-profile',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './finance-officer-profile.html',
})
export class FinanceOfficerProfileComponent implements OnInit {
  private authService = inject(AuthService);
  private financeService = inject(FinanceOfficerService);
  private toast = inject(ToastService);

  profileName = '';
  profileEmail = '';
  profilePhone = '';
  saving = signal(false);
  resettingPassword = signal(false);

  ngOnInit(): void {
    const u = this.authService.currentUser();
    if (u) {
      this.profileName = `${u.firstName} ${u.lastName}`;
      this.profileEmail = u.email;
      this.profilePhone = u.phone ?? '';
    }
  }

  userInitials(): string {
    const u = this.authService.currentUser();
    if (!u) return '?';
    return (u.firstName.charAt(0) + u.lastName.charAt(0)).toUpperCase();
  }

  branch(): string {
    return 'Head Office';
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

  onSave(): void {
    if (this.saving()) return;
    this.saving.set(true);
    this.financeService.updateProfile({
      name: this.profileName,
      email: this.profileEmail,
      phone: this.profilePhone,
    }).subscribe({
      next: () => {
        this.toast.success('Profile updated successfully');
        this.saving.set(false);
      },
      error: () => {
        this.toast.error('Failed to update profile');
        this.saving.set(false);
      },
    });
  }
}
