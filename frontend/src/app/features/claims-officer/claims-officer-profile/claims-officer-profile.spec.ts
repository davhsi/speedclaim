import { vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { ClaimsOfficerProfileComponent } from './claims-officer-profile';
import { AuthService } from '../../../core/services/auth.service';
import { ToastService } from '../../../shared/components/toast/toast.service';
import { AuthUserDto } from '../../../core/models/api.models';

describe('ClaimsOfficerProfileComponent', () => {
  let authService: { currentUser: ReturnType<typeof vi.fn>; logout: ReturnType<typeof vi.fn>; forgotPassword: ReturnType<typeof vi.fn> };
  let toast: { success: ReturnType<typeof vi.fn>; error: ReturnType<typeof vi.fn> };

  function create() {
    const fixture = TestBed.createComponent(ClaimsOfficerProfileComponent);
    fixture.detectChanges();
    return fixture;
  }

  beforeEach(() => {
    authService = {
      currentUser: vi.fn(() => ({ firstName: 'Ravi', lastName: 'Kumar', email: 'ravi@example.com', phone: '9999999999' }) as AuthUserDto),
      logout: vi.fn(),
      forgotPassword: vi.fn(),
    };
    toast = { success: vi.fn(), error: vi.fn() };

    TestBed.configureTestingModule({
      imports: [ClaimsOfficerProfileComponent],
      providers: [
        { provide: AuthService, useValue: authService },
        { provide: ToastService, useValue: toast },
      ],
    });
  });

  describe('derived display fields', () => {
    it('returns initials, fullName, email, and phone from the current user', () => {
      const c = create().componentInstance;
      expect(c.userInitials()).toBe('RK');
      expect(c.fullName()).toBe('Ravi Kumar');
      expect(c.email()).toBe('ravi@example.com');
      expect(c.phone()).toBe('9999999999');
    });

    it('falls back sensibly with no current user', () => {
      authService.currentUser.mockReturnValue(null);
      const c = create().componentInstance;
      expect(c.userInitials()).toBe('?');
      expect(c.fullName()).toBe('');
      expect(c.email()).toBe('');
      expect(c.phone()).toBe('');
    });
  });

  it('onLogout delegates to authService.logout', () => {
    const c = create().componentInstance;
    c.onLogout();
    expect(authService.logout).toHaveBeenCalled();
  });

  describe('onResetPassword', () => {
    it('does nothing when there is no current-user email', () => {
      authService.currentUser.mockReturnValue(null);
      const c = create().componentInstance;
      c.onResetPassword();
      expect(authService.forgotPassword).not.toHaveBeenCalled();
    });

    it('requests a reset link and shows a success toast', () => {
      const c = create().componentInstance;
      authService.forgotPassword.mockReturnValue(of({ message: 'sent' }));
      c.onResetPassword();
      expect(authService.forgotPassword).toHaveBeenCalledWith({ email: 'ravi@example.com' });
      expect(toast.success).toHaveBeenCalledWith('Password reset link sent to your registered email');
      expect(c.resettingPassword()).toBe(false);
    });

    it('shows an error toast when the request fails', () => {
      const c = create().componentInstance;
      authService.forgotPassword.mockReturnValue(throwError(() => ({ status: 500 })));
      c.onResetPassword();
      expect(toast.error).toHaveBeenCalledWith('Could not send reset link');
    });

    it('ignores a second call while a reset is already in flight', () => {
      const c = create().componentInstance;
      c.resettingPassword.set(true);
      c.onResetPassword();
      expect(authService.forgotPassword).not.toHaveBeenCalled();
    });
  });
});
