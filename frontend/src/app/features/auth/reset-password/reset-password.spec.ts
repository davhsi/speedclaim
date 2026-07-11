import { vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { of, throwError, Subject } from 'rxjs';
import { ResetPasswordComponent } from './reset-password';
import { AuthService } from '../../../core/services/auth.service';
import { ToastService } from '../../../shared/components/toast/toast.service';

describe('ResetPasswordComponent', () => {
  let authService: { resetPassword: ReturnType<typeof vi.fn> };
  let toast: { success: ReturnType<typeof vi.fn> };

  function create(queryParams: Record<string, string> = { token: 'reset-token-123' }) {
    TestBed.configureTestingModule({
      imports: [ResetPasswordComponent],
      providers: [
        { provide: AuthService, useValue: authService },
        { provide: Router, useValue: { navigate: vi.fn() } },
        { provide: ToastService, useValue: toast },
        { provide: ActivatedRoute, useValue: { snapshot: { queryParams } } },
      ],
    });
    const fixture = TestBed.createComponent(ResetPasswordComponent);
    fixture.detectChanges();
    return fixture;
  }

  function fillValidForm(fixture: ReturnType<typeof create>) {
    fixture.componentInstance.form.setValue({ newPassword: 'Secret123!', confirmPassword: 'Secret123!' });
  }

  beforeEach(() => {
    authService = { resetPassword: vi.fn() };
    toast = { success: vi.fn() };
  });

  describe('ngOnInit (token from query params)', () => {
    it('sets an error when the token is missing', () => {
      const fixture = create({});
      expect(fixture.componentInstance.errorMessage()).toBe('Invalid or missing reset token.');
    });

    it('does not set an error when a token is present', () => {
      const fixture = create({ token: 'abc' });
      expect(fixture.componentInstance.errorMessage()).toBe('');
    });
  });

  describe('onSubmit', () => {
    it('marks fields touched and does not submit an invalid form', () => {
      const fixture = create();
      fixture.componentInstance.onSubmit();
      expect(authService.resetPassword).not.toHaveBeenCalled();
      expect(fixture.componentInstance.form.controls.newPassword.touched).toBe(true);
    });

    it('does not submit a valid form when the token is missing', () => {
      const fixture = create({});
      fillValidForm(fixture);
      fixture.componentInstance.onSubmit();
      expect(authService.resetPassword).not.toHaveBeenCalled();
    });

    it('submits the token and new password when the form is valid', () => {
      const fixture = create({ token: 'reset-token-123' });
      fillValidForm(fixture);
      authService.resetPassword.mockReturnValue(of({ message: 'ok' }));

      fixture.componentInstance.onSubmit();

      expect(authService.resetPassword).toHaveBeenCalledWith({ token: 'reset-token-123', newPassword: 'Secret123!' });
      expect(fixture.componentInstance.success()).toBe(true);
      expect(fixture.componentInstance.loading()).toBe(false);
      expect(toast.success).toHaveBeenCalledWith('Password reset successfully.');
    });

    it('shows an expired-link message on failure', () => {
      const fixture = create();
      fillValidForm(fixture);
      authService.resetPassword.mockReturnValue(throwError(() => ({ status: 400 })));

      fixture.componentInstance.onSubmit();

      expect(fixture.componentInstance.errorMessage()).toBe('Reset link is invalid or has expired. Please request a new one.');
      expect(fixture.componentInstance.loading()).toBe(false);
      expect(fixture.componentInstance.success()).toBe(false);
    });

    it('shows a loading state while in flight, blocks a duplicate submit, and clears on success', () => {
      const fixture = create();
      fillValidForm(fixture);
      const request$ = new Subject<{ message: string }>();
      authService.resetPassword.mockReturnValue(request$);

      fixture.componentInstance.onSubmit();
      expect(fixture.componentInstance.loading()).toBe(true);

      fixture.componentInstance.onSubmit();
      expect(authService.resetPassword).toHaveBeenCalledTimes(1);

      request$.next({ message: 'ok' });
      request$.complete();

      expect(fixture.componentInstance.loading()).toBe(false);
      expect(fixture.componentInstance.success()).toBe(true);
    });
  });
});
