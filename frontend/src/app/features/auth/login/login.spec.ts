import { vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { of, throwError, Subject } from 'rxjs';
import { LoginComponent } from './login';
import { AuthService } from '../../../core/services/auth.service';
import { ToastService } from '../../../shared/components/toast/toast.service';
import { AuthResponse, AuthUserDto } from '../../../core/models/api.models';

function createStorage(): Storage {
  const values = new Map<string, string>();
  return {
    get length() { return values.size; },
    clear: () => values.clear(),
    getItem: (key: string) => values.get(key) ?? null,
    key: (index: number) => Array.from(values.keys())[index] ?? null,
    removeItem: (key: string) => values.delete(key),
    setItem: (key: string, value: string) => values.set(key, value),
  };
}

describe('LoginComponent', () => {
  let authService: { login: ReturnType<typeof vi.fn>; resendVerificationEmail: ReturnType<typeof vi.fn> };
  let router: { navigate: ReturnType<typeof vi.fn> };
  let toast: { success: ReturnType<typeof vi.fn> };

  function create() {
    const fixture = TestBed.createComponent(LoginComponent);
    fixture.detectChanges();
    return fixture;
  }

  function authResponse(role: AuthUserDto['role']): AuthResponse {
    return { accessToken: 'a', refreshToken: 'r', user: { role } as AuthUserDto };
  }

  beforeEach(() => {
    vi.stubGlobal('localStorage', createStorage());
    localStorage.clear();
    authService = { login: vi.fn(), resendVerificationEmail: vi.fn() };
    router = { navigate: vi.fn() };
    toast = { success: vi.fn() };

    TestBed.configureTestingModule({
      imports: [LoginComponent],
      providers: [
        { provide: AuthService, useValue: authService },
        { provide: Router, useValue: router },
        { provide: ToastService, useValue: toast },
        { provide: ActivatedRoute, useValue: {} },
      ],
    });
  });

  afterEach(() => {
    localStorage.clear();
  });

  describe('ngOnInit (remembered email)', () => {
    it('pre-fills the email and checks remember-me when a saved email exists', () => {
      localStorage.setItem('sc_saved_email', 'saved@example.com');
      const fixture = create();
      expect(fixture.componentInstance.form.controls.email.value).toBe('saved@example.com');
      expect(fixture.componentInstance.form.controls.rememberMe.value).toBe(true);
    });

    it('leaves the form blank when there is no saved email', () => {
      const fixture = create();
      expect(fixture.componentInstance.form.controls.email.value).toBe('');
      expect(fixture.componentInstance.form.controls.rememberMe.value).toBe(false);
    });
  });

  describe('onSubmit with an invalid form', () => {
    it('marks all fields touched and does not call the login API', () => {
      const fixture = create();
      fixture.componentInstance.onSubmit();
      expect(authService.login).not.toHaveBeenCalled();
      expect(fixture.componentInstance.form.controls.email.touched).toBe(true);
      expect(fixture.componentInstance.form.controls.password.touched).toBe(true);
    });
  });

  describe('onSubmit with a valid form', () => {
    function fillForm(fixture: ReturnType<typeof create>, rememberMe = false) {
      fixture.componentInstance.form.setValue({ email: 'jane@example.com', password: 'secret123', rememberMe });
    }

    it('calls login with the credentials and rememberMe flag', () => {
      const fixture = create();
      fillForm(fixture, true);
      authService.login.mockReturnValue(of(authResponse('Customer')));

      fixture.componentInstance.onSubmit();

      expect(authService.login).toHaveBeenCalledWith({ email: 'jane@example.com', password: 'secret123' }, true);
    });

    it('saves the email to localStorage when rememberMe is checked', () => {
      const fixture = create();
      fillForm(fixture, true);
      authService.login.mockReturnValue(of(authResponse('Customer')));
      fixture.componentInstance.onSubmit();
      expect(localStorage.getItem('sc_saved_email')).toBe('jane@example.com');
    });

    it('clears any saved email when rememberMe is unchecked', () => {
      localStorage.setItem('sc_saved_email', 'stale@example.com');
      const fixture = create();
      fillForm(fixture, false);
      authService.login.mockReturnValue(of(authResponse('Customer')));
      fixture.componentInstance.onSubmit();
      expect(localStorage.getItem('sc_saved_email')).toBeNull();
    });

    it('shows a welcome toast and routes to the role-specific dashboard on success', () => {
      const fixture = create();
      fillForm(fixture);
      authService.login.mockReturnValue(of(authResponse('Admin')));

      fixture.componentInstance.onSubmit();

      expect(toast.success).toHaveBeenCalledWith('Welcome back!');
      expect(router.navigate).toHaveBeenCalledWith(['/admin']);
      expect(fixture.componentInstance.loading()).toBe(false);
    });

    it('routes Customers (no role-specific mapping) to /dashboard', () => {
      const fixture = create();
      fillForm(fixture);
      authService.login.mockReturnValue(of(authResponse('Customer')));

      fixture.componentInstance.onSubmit();

      expect(router.navigate).toHaveBeenCalledWith(['/dashboard']);
    });

    it('shows a resend-verification prompt on a 422 error', () => {
      const fixture = create();
      fillForm(fixture);
      authService.login.mockReturnValue(throwError(() => ({ status: 422, error: { detail: 'Please verify first' } })));

      fixture.componentInstance.onSubmit();

      expect(fixture.componentInstance.errorMessage()).toBe('Please verify first');
      expect(fixture.componentInstance.showResendVerification()).toBe(true);
      expect(fixture.componentInstance.loading()).toBe(false);
    });

    it('falls back to a default message on 422 with no detail', () => {
      const fixture = create();
      fillForm(fixture);
      authService.login.mockReturnValue(throwError(() => ({ status: 422, error: {} })));
      fixture.componentInstance.onSubmit();
      expect(fixture.componentInstance.errorMessage()).toBe('Please verify your email address before signing in.');
    });

    it('shows a connectivity message on a status-0 error', () => {
      const fixture = create();
      fillForm(fixture);
      authService.login.mockReturnValue(throwError(() => ({ status: 0, error: {} })));
      fixture.componentInstance.onSubmit();
      expect(fixture.componentInstance.errorMessage()).toBe('Unable to connect. Please check your internet connection.');
    });

    it('shows the server message (or a generic fallback) on other errors', () => {
      const fixture = create();
      fillForm(fixture);
      authService.login.mockReturnValue(throwError(() => ({ status: 401, error: { detail: 'Invalid credentials' } })));
      fixture.componentInstance.onSubmit();
      expect(fixture.componentInstance.errorMessage()).toBe('Invalid credentials');
    });

    it('shows a loading state while in flight, blocks a duplicate submit, and clears on success', () => {
      const fixture = create();
      fillForm(fixture);
      const request$ = new Subject<AuthResponse>();
      authService.login.mockReturnValue(request$);

      fixture.componentInstance.onSubmit();
      expect(fixture.componentInstance.loading()).toBe(true);

      fixture.componentInstance.onSubmit();
      expect(authService.login).toHaveBeenCalledTimes(1);

      request$.next(authResponse('Customer'));
      request$.complete();

      expect(fixture.componentInstance.loading()).toBe(false);
    });

    it('clears the loading state on failure so the form can be retried', () => {
      const fixture = create();
      fillForm(fixture);
      const request$ = new Subject<AuthResponse>();
      authService.login.mockReturnValue(request$);

      fixture.componentInstance.onSubmit();
      request$.error({ status: 500, error: {} });

      expect(fixture.componentInstance.loading()).toBe(false);
    });
  });

  describe('resendVerification', () => {
    it('does nothing when there is no email on file (no prior 422)', () => {
      const fixture = create();
      fixture.componentInstance.resendVerification();
      expect(authService.resendVerificationEmail).not.toHaveBeenCalled();
    });

    it('resends to the email captured from the last 422 error', () => {
      const fixture = create();
      fixture.componentInstance.form.setValue({ email: 'jane@example.com', password: 'x', rememberMe: false });
      authService.login.mockReturnValue(throwError(() => ({ status: 422, error: {} })));
      fixture.componentInstance.onSubmit();

      authService.resendVerificationEmail.mockReturnValue(of({ message: 'sent' }));
      fixture.componentInstance.resendVerification();

      expect(authService.resendVerificationEmail).toHaveBeenCalledWith({ email: 'jane@example.com' });
      expect(fixture.componentInstance.resendSuccess()).toBe(true);
      expect(fixture.componentInstance.resendLoading()).toBe(false);
    });

    it('stops the loading spinner without setting success when the resend call fails', () => {
      const fixture = create();
      fixture.componentInstance.form.setValue({ email: 'jane@example.com', password: 'x', rememberMe: false });
      authService.login.mockReturnValue(throwError(() => ({ status: 422, error: {} })));
      fixture.componentInstance.onSubmit();

      authService.resendVerificationEmail.mockReturnValue(throwError(() => ({ status: 500 })));
      fixture.componentInstance.resendVerification();

      expect(fixture.componentInstance.resendLoading()).toBe(false);
      expect(fixture.componentInstance.resendSuccess()).toBe(false);
    });

    it('blocks a duplicate resend while one is already in flight', () => {
      const fixture = create();
      fixture.componentInstance.form.setValue({ email: 'jane@example.com', password: 'x', rememberMe: false });
      authService.login.mockReturnValue(throwError(() => ({ status: 422, error: {} })));
      fixture.componentInstance.onSubmit();

      const request$ = new Subject<{ message: string }>();
      authService.resendVerificationEmail.mockReturnValue(request$);

      fixture.componentInstance.resendVerification();
      expect(fixture.componentInstance.resendLoading()).toBe(true);

      fixture.componentInstance.resendVerification();
      expect(authService.resendVerificationEmail).toHaveBeenCalledTimes(1);

      request$.next({ message: 'sent' });
      request$.complete();
      expect(fixture.componentInstance.resendLoading()).toBe(false);
    });
  });
});
