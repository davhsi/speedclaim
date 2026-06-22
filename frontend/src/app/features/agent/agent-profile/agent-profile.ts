import { Component, inject, signal, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { AgentService, AgentProfileDto } from '../services/agent.service';
import { AuthService } from '../../../core/services/auth.service';
import { ToastService } from '../../../shared/components/toast/toast.service';
import { DateFormatPipe } from '../../../shared/pipes/date-format.pipe';
import { phoneValidator } from '../../../shared/validators/input.validators';

@Component({
  selector: 'app-agent-profile',
  standalone: true,
  imports: [ReactiveFormsModule, DateFormatPipe],
  templateUrl: './agent-profile.html',
})
export class AgentProfileComponent implements OnInit {
  private fb = inject(FormBuilder);
  private agentService = inject(AgentService);
  private authService = inject(AuthService);
  private toast = inject(ToastService);

  profile = signal<AgentProfileDto | null>(null);
  saving = signal(false);

  profileForm = this.fb.group({
    salutation: ['Mr', Validators.required],
    firstName: ['', Validators.required],
    lastName: ['', Validators.required],
    phone: ['', [Validators.required, phoneValidator()]],
  });

  userInitials(): string {
    const u = this.authService.currentUser();
    if (!u) return '?';
    return (u.firstName.charAt(0) + u.lastName.charAt(0)).toUpperCase();
  }

  ngOnInit(): void {
    this.agentService.getProfile().subscribe(p => {
      this.profile.set(p);
      const nameParts = p.fullName.split(' ');
      this.profileForm.patchValue({
        firstName: nameParts[0] || '',
        lastName: nameParts.slice(1).join(' ') || '',
      });
    });
  }

  saveProfile(): void {
    if (this.profileForm.invalid) return;
    this.saving.set(true);
    this.agentService.updateProfile(this.profileForm.getRawValue() as any).subscribe({
      next: () => {
        this.toast.success('Profile updated');
        this.saving.set(false);
      },
      error: () => {
        this.toast.error('Update failed');
        this.saving.set(false);
      },
    });
  }

  resetPassword(): void {
    this.toast.info('Password reset link sent to your email.');
  }
}
