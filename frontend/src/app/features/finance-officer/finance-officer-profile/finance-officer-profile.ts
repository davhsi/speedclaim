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
  private readonly authService = inject(AuthService);
  private readonly financeService = inject(FinanceOfficerService);
  private readonly toast = inject(ToastService);

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
    const [firstName, ...lastNameParts] = this.profileName.trim().split(/\s+/).filter(Boolean);
    if (!firstName || lastNameParts.length === 0) {
      this.toast.error('Enter both first and last name');
      return;
    }

    const currentUser = this.authService.currentUser();
    this.saving.set(true);
    this.financeService.updateProfile({
      firstName,
      lastName: lastNameParts.join(' '),
      phone: this.profilePhone,
      salutation: currentUser?.salutation ?? 'Mr',
      maritalStatus: currentUser?.maritalStatus ?? 'Single',
    }).subscribe({
      next: () => {
        this.authService.patchCurrentUser({
          firstName,
          lastName: lastNameParts.join(' '),
          fullName: `${firstName} ${lastNameParts.join(' ')}`,
          phone: this.profilePhone,
        });
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
