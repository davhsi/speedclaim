import { vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { UnderwriterProfileComponent } from './underwriter-profile';
import { UnderwriterService } from '../services/underwriter.service';
import { AuthService } from '../../../core/services/auth.service';
import { ToastService } from '../../../shared/components/toast/toast.service';
import { AuthUserDto } from '../../../core/models/api.models';

describe('UnderwriterProfileComponent', () => {
  let uwService: { updateProfile: ReturnType<typeof vi.fn>; requestPasswordReset: ReturnType<typeof vi.fn> };
  let authService: { currentUser: ReturnType<typeof vi.fn>; logout: ReturnType<typeof vi.fn> };
  let toast: { success: ReturnType<typeof vi.fn>; error: ReturnType<typeof vi.fn> };

  function create() {
    const fixture = TestBed.createComponent(UnderwriterProfileComponent);
    fixture.detectChanges();
    return fixture;
  }

  beforeEach(() => {
    uwService = { updateProfile: vi.fn(), requestPasswordReset: vi.fn() };
    authService = {
      currentUser: vi.fn(() => ({ firstName: 'Uday', lastName: 'Nair', email: 'uday@example.com', phone: '9876500000' }) as AuthUserDto),
      logout: vi.fn(),
    };
    toast = { success: vi.fn(), error: vi.fn() };

    TestBed.configureTestingModule({
      imports: [UnderwriterProfileComponent],
      providers: [
        { provide: UnderwriterService, useValue: uwService },
        { provide: AuthService, useValue: authService },
        { provide: ToastService, useValue: toast },
      ],
    });
  });

  describe('ngOnInit', () => {
    it('populates plain fields from the current user', () => {
      const c = create().componentInstance;
      expect(c.name).toBe('Uday Nair');
      expect(c.email).toBe('uday@example.com');
      expect(c.phone).toBe('9876500000');
    });

    it('leaves fields blank with no current user', () => {
      authService.currentUser.mockReturnValue(null);
      const c = create().componentInstance;
      expect(c.name).toBe('');
      expect(c.email).toBe('');
      expect(c.phone).toBe('');
    });
  });

  it('userInitials/fullName derive from the current user', () => {
    const c = create().componentInstance;
    expect(c.userInitials()).toBe('UN');
    expect(c.fullName()).toBe('Uday Nair');
  });

  it('onLogout delegates to authService.logout', () => {
    const c = create().componentInstance;
    c.onLogout();
    expect(authService.logout).toHaveBeenCalled();
  });

  describe('onSave', () => {
    it('splits the edited name into firstName/lastName and saves', () => {
      const c = create().componentInstance;
      c.name = 'Uday Prakash Nair';
      c.phone = '9999999999';
      uwService.updateProfile.mockReturnValue(of({ message: 'ok' }));

      c.onSave();

      expect(uwService.updateProfile).toHaveBeenCalledWith({ firstName: 'Uday', lastName: 'Prakash Nair', phone: '9999999999' });
      expect(toast.success).toHaveBeenCalledWith('Profile changes saved.');
      expect(c.saving()).toBe(false);
    });

    it('shows an error toast when the save fails', () => {
      const c = create().componentInstance;
      uwService.updateProfile.mockReturnValue(throwError(() => ({ status: 500 })));
      c.onSave();
      expect(toast.error).toHaveBeenCalledWith('Failed to save profile changes.');
    });

    it('ignores a second call while saving is already in flight', () => {
      const c = create().componentInstance;
      c.saving.set(true);
      c.onSave();
      expect(uwService.updateProfile).not.toHaveBeenCalled();
    });
  });

  describe('onResetPassword', () => {
    it('requests a reset via the underwriter service (not authService) and shows success', () => {
      const c = create().componentInstance;
      uwService.requestPasswordReset.mockReturnValue(of({ message: 'sent' }));

      c.onResetPassword();

      expect(uwService.requestPasswordReset).toHaveBeenCalledWith('uday@example.com');
      expect(toast.success).toHaveBeenCalledWith('Password reset link sent to your registered email.');
      expect(c.resettingPassword()).toBe(false);
    });

    it('does nothing when there is no current-user email', () => {
      authService.currentUser.mockReturnValue(null);
      const c = create().componentInstance;
      c.onResetPassword();
      expect(uwService.requestPasswordReset).not.toHaveBeenCalled();
    });

    it('shows an error toast when the request fails', () => {
      const c = create().componentInstance;
      uwService.requestPasswordReset.mockReturnValue(throwError(() => ({ status: 500 })));
      c.onResetPassword();
      expect(toast.error).toHaveBeenCalledWith('Could not send reset link.');
    });

    it('ignores a second call while a reset is already in flight', () => {
      const c = create().componentInstance;
      c.resettingPassword.set(true);
      c.onResetPassword();
      expect(uwService.requestPasswordReset).not.toHaveBeenCalled();
    });
  });
});
