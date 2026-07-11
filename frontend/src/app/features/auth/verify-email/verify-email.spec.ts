import { vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { of, throwError, Subject } from 'rxjs';
import { VerifyEmailComponent } from './verify-email';
import { AuthService } from '../../../core/services/auth.service';

describe('VerifyEmailComponent', () => {
  let authService: { verifyEmail: ReturnType<typeof vi.fn>; resendVerificationEmail: ReturnType<typeof vi.fn> };
  let router: { navigate: ReturnType<typeof vi.fn> };

  function create(queryParams: Record<string, string> = { token: 'verify-token' }) {
    TestBed.configureTestingModule({
      imports: [VerifyEmailComponent],
      providers: [
        { provide: AuthService, useValue: authService },
        { provide: Router, useValue: router },
        { provide: ActivatedRoute, useValue: { snapshot: { queryParams } } },
      ],
    });
    const fixture = TestBed.createComponent(VerifyEmailComponent);
    fixture.detectChanges();
    return fixture;
  }

  beforeEach(() => {
    authService = { verifyEmail: vi.fn(), resendVerificationEmail: vi.fn() };
    router = { navigate: vi.fn() };
  });

  describe('ngOnInit', () => {
    it('shows an error and the resend form when there is no token, without calling the API', () => {
      const fixture = create({});
      expect(fixture.componentInstance.loading()).toBe(false);
      expect(fixture.componentInstance.errorMessage()).toBe('Invalid or missing verification token.');
      expect(fixture.componentInstance.showResendForm()).toBe(true);
      expect(authService.verifyEmail).not.toHaveBeenCalled();
    });

    it('verifies the token and redirects to login after a delay on success', () => {
      vi.useFakeTimers();
      authService.verifyEmail.mockReturnValue(of({ message: 'verified' }));

      const fixture = create({ token: 'verify-token' });

      expect(authService.verifyEmail).toHaveBeenCalledWith({ token: 'verify-token' });
      expect(fixture.componentInstance.loading()).toBe(false);
      expect(fixture.componentInstance.success()).toBe(true);
      expect(router.navigate).not.toHaveBeenCalled();

      vi.advanceTimersByTime(3000);
      expect(router.navigate).toHaveBeenCalledWith(['/auth/login']);

      vi.useRealTimers();
    });

    it('shows an error and the resend form when verification fails', () => {
      authService.verifyEmail.mockReturnValue(throwError(() => ({ status: 400 })));

      const fixture = create({ token: 'expired-token' });

      expect(fixture.componentInstance.loading()).toBe(false);
      expect(fixture.componentInstance.errorMessage()).toBe('Verification link is invalid or has expired.');
      expect(fixture.componentInstance.showResendForm()).toBe(true);
    });
  });

  describe('resendVerification', () => {
    it('marks the field touched and does not submit an invalid email', () => {
      const fixture = create({});
      fixture.componentInstance.resendVerification();
      expect(authService.resendVerificationEmail).not.toHaveBeenCalled();
      expect(fixture.componentInstance.resendForm.controls.email.touched).toBe(true);
    });

    it('submits a valid email and shows success', () => {
      const fixture = create({});
      fixture.componentInstance.resendForm.setValue({ email: 'jane@example.com' });
      authService.resendVerificationEmail.mockReturnValue(of({ message: 'sent' }));

      fixture.componentInstance.resendVerification();

      expect(authService.resendVerificationEmail).toHaveBeenCalledWith({ email: 'jane@example.com' });
      expect(fixture.componentInstance.resendLoading()).toBe(false);
      expect(fixture.componentInstance.resendSuccess()).toBe(true);
    });

    it('stops loading without success when the resend call fails', () => {
      const fixture = create({});
      fixture.componentInstance.resendForm.setValue({ email: 'jane@example.com' });
      authService.resendVerificationEmail.mockReturnValue(throwError(() => ({ status: 500 })));

      fixture.componentInstance.resendVerification();

      expect(fixture.componentInstance.resendLoading()).toBe(false);
      expect(fixture.componentInstance.resendSuccess()).toBe(false);
    });

    it('shows a loading state while in flight, blocks a duplicate resend, and clears on success', () => {
      const fixture = create({});
      fixture.componentInstance.resendForm.setValue({ email: 'jane@example.com' });
      const request$ = new Subject<{ message: string }>();
      authService.resendVerificationEmail.mockReturnValue(request$);

      fixture.componentInstance.resendVerification();
      expect(fixture.componentInstance.resendLoading()).toBe(true);

      fixture.componentInstance.resendVerification();
      expect(authService.resendVerificationEmail).toHaveBeenCalledTimes(1);

      request$.next({ message: 'sent' });
      request$.complete();

      expect(fixture.componentInstance.resendLoading()).toBe(false);
      expect(fixture.componentInstance.resendSuccess()).toBe(true);
    });
  });
});
