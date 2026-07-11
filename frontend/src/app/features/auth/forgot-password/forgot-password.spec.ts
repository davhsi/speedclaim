import { vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { of, throwError, Subject } from 'rxjs';
import { ForgotPasswordComponent } from './forgot-password';
import { AuthService } from '../../../core/services/auth.service';

describe('ForgotPasswordComponent', () => {
  let authService: { forgotPassword: ReturnType<typeof vi.fn> };
  let router: { navigate: ReturnType<typeof vi.fn> };

  function create() {
    const fixture = TestBed.createComponent(ForgotPasswordComponent);
    fixture.detectChanges();
    return fixture;
  }

  beforeEach(() => {
    authService = { forgotPassword: vi.fn() };
    router = { navigate: vi.fn() };

    TestBed.configureTestingModule({
      imports: [ForgotPasswordComponent],
      providers: [
        { provide: AuthService, useValue: authService },
        { provide: Router, useValue: router },
        { provide: ActivatedRoute, useValue: {} },
      ],
    });
  });

  it('marks the field touched and does not submit an invalid email', () => {
    const fixture = create();
    fixture.componentInstance.onSubmit();
    expect(authService.forgotPassword).not.toHaveBeenCalled();
    expect(fixture.componentInstance.form.controls.email.touched).toBe(true);
  });

  it('submits the email and navigates to /auth/reset-sent on success', () => {
    const fixture = create();
    fixture.componentInstance.form.setValue({ email: 'jane@example.com' });
    authService.forgotPassword.mockReturnValue(of({ message: 'sent' }));

    fixture.componentInstance.onSubmit();

    expect(authService.forgotPassword).toHaveBeenCalledWith({ email: 'jane@example.com' });
    expect(router.navigate).toHaveBeenCalledWith(['/auth/reset-sent']);
  });

  it('shows a connectivity message on a status-0 error', () => {
    const fixture = create();
    fixture.componentInstance.form.setValue({ email: 'jane@example.com' });
    authService.forgotPassword.mockReturnValue(throwError(() => ({ status: 0 })));

    fixture.componentInstance.onSubmit();

    expect(fixture.componentInstance.errorMessage()).toBe('Unable to connect. Please check your internet connection.');
    expect(fixture.componentInstance.loading()).toBe(false);
  });

  it('shows a rate-limit message on a 429 error', () => {
    const fixture = create();
    fixture.componentInstance.form.setValue({ email: 'jane@example.com' });
    authService.forgotPassword.mockReturnValue(throwError(() => ({ status: 429 })));

    fixture.componentInstance.onSubmit();

    expect(fixture.componentInstance.errorMessage()).toBe('Too many requests. Please wait a moment before trying again.');
  });

  it('shows a generic message on any other error', () => {
    const fixture = create();
    fixture.componentInstance.form.setValue({ email: 'jane@example.com' });
    authService.forgotPassword.mockReturnValue(throwError(() => ({ status: 500 })));

    fixture.componentInstance.onSubmit();

    expect(fixture.componentInstance.errorMessage()).toBe('Something went wrong. Please try again.');
  });

  it('shows a loading state while in flight and blocks a duplicate submit', () => {
    const fixture = create();
    fixture.componentInstance.form.setValue({ email: 'jane@example.com' });
    const request$ = new Subject<{ message: string }>();
    authService.forgotPassword.mockReturnValue(request$);

    fixture.componentInstance.onSubmit();
    expect(fixture.componentInstance.loading()).toBe(true);

    fixture.componentInstance.onSubmit();
    expect(authService.forgotPassword).toHaveBeenCalledTimes(1);
  });

  it('clears the loading state on failure so the form can be retried', () => {
    const fixture = create();
    fixture.componentInstance.form.setValue({ email: 'jane@example.com' });
    const request$ = new Subject<{ message: string }>();
    authService.forgotPassword.mockReturnValue(request$);

    fixture.componentInstance.onSubmit();
    request$.error({ status: 500 });

    expect(fixture.componentInstance.loading()).toBe(false);
  });
});
