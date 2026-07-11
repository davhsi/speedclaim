import { vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { of, throwError, Subject } from 'rxjs';
import { AgentProfileComponent } from './agent-profile';
import { AgentService, AgentProfileDto } from '../services/agent.service';
import { AuthService } from '../../../core/services/auth.service';
import { ToastService } from '../../../shared/components/toast/toast.service';
import { AuthUserDto } from '../../../core/models/api.models';

describe('AgentProfileComponent', () => {
  let agentService: { getProfile: ReturnType<typeof vi.fn>; updateProfile: ReturnType<typeof vi.fn> };
  let authService: { currentUser: ReturnType<typeof vi.fn>; forgotPassword: ReturnType<typeof vi.fn> };
  let toast: { success: ReturnType<typeof vi.fn>; error: ReturnType<typeof vi.fn>; info: ReturnType<typeof vi.fn> };

  const baseAgentProfile: AgentProfileDto = {
    agentId: 'a1', userId: 'u1', email: 'agent@example.com', salutation: 'Mr',
    firstName: 'John', lastName: 'Smith', fullName: 'Mr John Smith', phone: '9876543210',
    agentCode: 'AG1', agentType: 'Individual', licenseNumber: 'LIC1', licenseExpiry: '2030-01-01',
    commissionRate: 5, isActive: true,
  };

  function create(profile: AgentProfileDto = baseAgentProfile) {
    agentService.getProfile.mockReturnValue(of(profile));
    const fixture = TestBed.createComponent(AgentProfileComponent);
    fixture.detectChanges();
    return fixture;
  }

  beforeEach(() => {
    agentService = { getProfile: vi.fn(), updateProfile: vi.fn() };
    authService = { currentUser: vi.fn(() => ({ firstName: 'John', lastName: 'Smith', phone: '9876543210' }) as AuthUserDto), forgotPassword: vi.fn() };
    toast = { success: vi.fn(), error: vi.fn(), info: vi.fn() };

    TestBed.configureTestingModule({
      imports: [AgentProfileComponent],
      providers: [
        { provide: AgentService, useValue: agentService },
        { provide: AuthService, useValue: authService },
        { provide: ToastService, useValue: toast },
      ],
    });
  });

  describe('ngOnInit', () => {
    it('patches the form directly from firstName/lastName/phone when present', () => {
      const fixture = create();
      const c = fixture.componentInstance;
      expect(c.profileForm.controls.firstName.value).toBe('John');
      expect(c.profileForm.controls.lastName.value).toBe('Smith');
      expect(c.profileForm.controls.phone.value).toBe('9876543210');
    });

    it('falls back to parsing fullName when firstName/lastName are blank', () => {
      const fixture = create({ ...baseAgentProfile, firstName: '', lastName: '', fullName: 'Mr Alex Carter' });
      expect(fixture.componentInstance.profileForm.controls.firstName.value).toBe('Alex');
      expect(fixture.componentInstance.profileForm.controls.lastName.value).toBe('Carter');
    });

    it('falls back to the current user when neither profile fields nor fullName parsing yield a name', () => {
      const fixture = create({ ...baseAgentProfile, firstName: '', lastName: '', fullName: '', phone: '' });
      expect(fixture.componentInstance.profileForm.controls.firstName.value).toBe('John');
      expect(fixture.componentInstance.profileForm.controls.phone.value).toBe('9876543210');
    });
  });

  describe('userInitials', () => {
    it('returns initials from the current user', () => {
      expect(create().componentInstance.userInitials()).toBe('JS');
    });

    it('returns "?" with no current user', () => {
      authService.currentUser.mockReturnValue(null);
      expect(create().componentInstance.userInitials()).toBe('?');
    });
  });

  describe('saveProfile', () => {
    it('does nothing when the form is invalid', () => {
      const fixture = create();
      fixture.componentInstance.profileForm.controls.firstName.setValue('');
      fixture.componentInstance.saveProfile();
      expect(agentService.updateProfile).not.toHaveBeenCalled();
    });

    it('saves a valid form, updates the local profile, and shows a success toast', () => {
      const fixture = create();
      agentService.updateProfile.mockReturnValue(of({ message: 'ok' }));
      fixture.componentInstance.profileForm.patchValue({ firstName: 'Jane' });

      fixture.componentInstance.saveProfile();

      expect(agentService.updateProfile).toHaveBeenCalledWith(expect.objectContaining({ firstName: 'Jane' }));
      expect(fixture.componentInstance.profile()?.firstName).toBe('Jane');
      expect(fixture.componentInstance.profile()?.fullName).toBe('Jane Smith');
      expect(toast.success).toHaveBeenCalledWith('Profile updated');
      expect(fixture.componentInstance.saving()).toBe(false);
    });

    it('shows an error toast when the update fails', () => {
      const fixture = create();
      agentService.updateProfile.mockReturnValue(throwError(() => ({ status: 500 })));
      fixture.componentInstance.saveProfile();
      expect(toast.error).toHaveBeenCalledWith('Update failed');
      expect(fixture.componentInstance.saving()).toBe(false);
    });

    it('sets saving while in flight and blocks a duplicate submit until it clears', () => {
      const fixture = create();
      const c = fixture.componentInstance;
      const subject = new Subject<{ message: string }>();
      agentService.updateProfile.mockReturnValue(subject);

      c.saveProfile();

      expect(c.saving()).toBe(true);
      c.saveProfile();
      expect(agentService.updateProfile).toHaveBeenCalledTimes(1);

      subject.next({ message: 'ok' });
      subject.complete();

      expect(c.saving()).toBe(false);
    });
  });

  describe('resetPassword', () => {
    it('does nothing when there is no profile email loaded', () => {
      agentService.getProfile.mockReturnValue(of({ ...baseAgentProfile, email: '' }));
      const fixture = TestBed.createComponent(AgentProfileComponent);
      fixture.detectChanges();
      fixture.componentInstance.resetPassword();
      expect(authService.forgotPassword).not.toHaveBeenCalled();
    });

    it('requests a reset link for the profile email and shows an info toast', () => {
      const fixture = create();
      authService.forgotPassword.mockReturnValue(of({ message: 'sent' }));
      fixture.componentInstance.resetPassword();
      expect(authService.forgotPassword).toHaveBeenCalledWith({ email: 'agent@example.com' });
      expect(toast.info).toHaveBeenCalledWith('Password reset link sent to your email.');
      expect(fixture.componentInstance.resettingPassword()).toBe(false);
    });

    it('shows an error toast when the reset request fails', () => {
      const fixture = create();
      authService.forgotPassword.mockReturnValue(throwError(() => ({ status: 500 })));
      fixture.componentInstance.resetPassword();
      expect(toast.error).toHaveBeenCalledWith('Could not send reset link.');
    });

    it('ignores a second call while a reset is already in flight', () => {
      const fixture = create();
      fixture.componentInstance.resettingPassword.set(true);
      fixture.componentInstance.resetPassword();
      expect(authService.forgotPassword).not.toHaveBeenCalled();
    });
  });
});
