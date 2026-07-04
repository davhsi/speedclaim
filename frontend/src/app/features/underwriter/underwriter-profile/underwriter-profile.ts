import { Component, inject, signal, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../../core/services/auth.service';
import { UnderwriterService } from '../services/underwriter.service';
import { ToastService } from '../../../shared/components/toast/toast.service';

@Component({
  selector: 'app-underwriter-profile',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './underwriter-profile.html',
})
export class UnderwriterProfileComponent implements OnInit {
  private readonly authService = inject(AuthService);
  private readonly uwService = inject(UnderwriterService);
  private readonly toast = inject(ToastService);

  name = '';
  email = '';
  phone = '';
  saving = signal(false);
  resettingPassword = signal(false);

  ngOnInit(): void {
    const u = this.authService.currentUser();
    if (u) {
      this.name = `${u.firstName} ${u.lastName}`;
      this.email = u.email;
      this.phone = u.phone;
    }
  }

  userInitials(): string {
    const u = this.authService.currentUser();
    if (!u) return '?';
    return (u.firstName.charAt(0) + u.lastName.charAt(0)).toUpperCase();
  }

  fullName(): string {
    const u = this.authService.currentUser();
    return u ? `${u.firstName} ${u.lastName}` : '';
  }

  onSave(): void {
    if (this.saving()) return;
    const parts = this.name.trim().split(/\s+/);
    const firstName = parts[0] || '';
    const lastName = parts.slice(1).join(' ') || '';
    this.saving.set(true);
    this.uwService.updateProfile({ firstName, lastName, phone: this.phone }).subscribe({
      next: () => {
        this.toast.success('Profile changes saved.');
        this.saving.set(false);
      },
      error: () => {
        this.toast.error('Failed to save profile changes.');
        this.saving.set(false);
      },
    });
  }

  onResetPassword(): void {
    if (this.resettingPassword()) return;
    const email = this.authService.currentUser()?.email;
    if (!email) return;
    this.resettingPassword.set(true);
    this.uwService.requestPasswordReset(email).subscribe({
      next: () => {
        this.toast.success('Password reset link sent to your registered email.');
        this.resettingPassword.set(false);
      },
      error: () => {
        this.toast.error('Could not send reset link.');
        this.resettingPassword.set(false);
      },
    });
  }

  onLogout(): void {
    this.authService.logout();
  }
}
