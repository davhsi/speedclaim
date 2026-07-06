import { vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { FinanceOfficerProfileComponent } from './finance-officer-profile';
import { FinanceOfficerService } from '../services/finance-officer.service';
import { AuthService } from '../../../core/services/auth.service';
import { ToastService } from '../../../shared/components/toast/toast.service';
import { AuthUserDto } from '../../../core/models/api.models';

describe('FinanceOfficerProfileComponent', () => {
  let financeService: { updateProfile: ReturnType<typeof vi.fn> };
  let authService: { currentUser: ReturnType<typeof vi.fn>; logout: ReturnType<typeof vi.fn>; forgotPassword: ReturnType<typeof vi.fn>; patchCurrentUser: ReturnType<typeof vi.fn> };
  let toast: { success: ReturnType<typeof vi.fn>; error: ReturnType<typeof vi.fn> };

  function create() {
    const fixture = TestBed.createComponent(FinanceOfficerProfileComponent);
    fixture.detectChanges();
    return fixture;
  }

  beforeEach(() => {
    financeService = { updateProfile: vi.fn() };
    authService = {
      currentUser: vi.fn(() => ({
        firstName: 'Priya', lastName: 'Rao', email: 'priya@example.com', phone: '8888888888',
        salutation: 'Ms', maritalStatus: 'Single',
      }) as AuthUserDto),
      logout: vi.fn(),
      forgotPassword: vi.fn(),
      patchCurrentUser: vi.fn(),
    };
    toast = { success: vi.fn(), error: vi.fn() };

    TestBed.configureTestingModule({
      imports: [FinanceOfficerProfileComponent],
      providers: [
        { provide: FinanceOfficerService, useValue: financeService },
        { provide: AuthService, useValue: authService },
        { provide: ToastService, useValue: toast },
      ],
    });
  });

  describe('ngOnInit', () => {
    it('populates the plain fields from the current user', () => {
      const c = create().componentInstance;
      expect(c.profileName).toBe('Priya Rao');
      expect(c.profileEmail).toBe('priya@example.com');
      expect(c.profilePhone).toBe('8888888888');
    });

    it('leaves fields blank with no current user', () => {
      authService.currentUser.mockReturnValue(null);
      const c = create().componentInstance;
      expect(c.profileName).toBe('');
      expect(c.profileEmail).toBe('');
      expect(c.profilePhone).toBe('');
    });
  });

  it('userInitials derives from the current user, or "?" with none', () => {
    const firstFixture = create();
    expect(firstFixture.componentInstance.userInitials()).toBe('PR');
    firstFixture.destroy();

    authService.currentUser.mockReturnValue(null);
    expect(create().componentInstance.userInitials()).toBe('?');
  });

  it('onLogout delegates to authService.logout', () => {
    const c = create().componentInstance;
    c.onLogout();
    expect(authService.logout).toHaveBeenCalled();
  });

  describe('onSave', () => {
    it('rejects a name with no last name and shows an error toast', () => {
      const c = create().componentInstance;
      c.profileName = 'Priya';
      c.onSave();
      expect(toast.error).toHaveBeenCalledWith('Enter both first and last name');
      expect(financeService.updateProfile).not.toHaveBeenCalled();
    });

    it('rejects an empty name', () => {
      const c = create().componentInstance;
      c.profileName = '   ';
      c.onSave();
      expect(financeService.updateProfile).not.toHaveBeenCalled();
    });

    it('saves a valid full name, patches the current user, and shows success', () => {
      const c = create().componentInstance;
      c.profileName = 'Priya Sharma';
      c.profilePhone = '7777777777';
      financeService.updateProfile.mockReturnValue(of({ message: 'ok' }));

      c.onSave();

      expect(financeService.updateProfile).toHaveBeenCalledWith({
        firstName: 'Priya', lastName: 'Sharma', phone: '7777777777', salutation: 'Ms', maritalStatus: 'Single',
      });
      expect(authService.patchCurrentUser).toHaveBeenCalledWith(expect.objectContaining({ firstName: 'Priya', lastName: 'Sharma' }));
      expect(toast.success).toHaveBeenCalledWith('Profile updated successfully');
      expect(c.saving()).toBe(false);
    });

    it('defaults salutation/maritalStatus when there is no current user', () => {
      authService.currentUser.mockReturnValue(null);
      const c = create().componentInstance;
      c.profileName = 'Priya Sharma';
      financeService.updateProfile.mockReturnValue(of({ message: 'ok' }));

      c.onSave();

      expect(financeService.updateProfile).toHaveBeenCalledWith(expect.objectContaining({ salutation: 'Mr', maritalStatus: 'Single' }));
    });

    it('shows an error toast when the save fails', () => {
      const c = create().componentInstance;
      c.profileName = 'Priya Sharma';
      financeService.updateProfile.mockReturnValue(throwError(() => ({ status: 500 })));
      c.onSave();
      expect(toast.error).toHaveBeenCalledWith('Failed to update profile');
      expect(c.saving()).toBe(false);
    });

    it('ignores a second call while saving is already in flight', () => {
      const c = create().componentInstance;
      c.saving.set(true);
      c.profileName = 'Priya Sharma';
      c.onSave();
      expect(financeService.updateProfile).not.toHaveBeenCalled();
    });
  });

  describe('onResetPassword', () => {
    it('requests a reset link and shows success', () => {
      const c = create().componentInstance;
      authService.forgotPassword.mockReturnValue(of({ message: 'sent' }));
      c.onResetPassword();
      expect(authService.forgotPassword).toHaveBeenCalledWith({ email: 'priya@example.com' });
      expect(toast.success).toHaveBeenCalledWith('Password reset link sent to your registered email');
    });

    it('shows an error toast on failure', () => {
      const c = create().componentInstance;
      authService.forgotPassword.mockReturnValue(throwError(() => ({ status: 500 })));
      c.onResetPassword();
      expect(toast.error).toHaveBeenCalledWith('Could not send reset link');
    });
  });
});
