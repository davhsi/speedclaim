import { vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { HttpErrorResponse, HttpHandlerFn, HttpRequest, HttpResponse } from '@angular/common/http';
import { of, throwError, Subject } from 'rxjs';
import { errorInterceptor as errorInterceptorFn } from './error.interceptor';
import { AuthService } from '../services/auth.service';
import { TokenService } from '../services/token.service';
import { ToastService } from '../../shared/components/toast/toast.service';
import { AuthResponse } from '../models/api.models';

// `isRefreshing` / `refreshSubject` in error.interceptor.ts are module-scoped singletons
// (by design, so concurrent requests share one in-flight refresh). Every test here uses
// synchronous observables (of/throwError), so by the time each test's assertions run,
// the interceptor has already reset isRefreshing back to false — the one test that
// deliberately leaves it "true" mid-test (the concurrency test) resolves it before
// finishing. That keeps tests order-independent without needing to reset the module.
describe('errorInterceptor', () => {
  let authService: { refreshToken: ReturnType<typeof vi.fn>; logout: ReturnType<typeof vi.fn> };
  let tokenService: { getAccessToken: ReturnType<typeof vi.fn> };
  let toast: { warning: ReturnType<typeof vi.fn>; error: ReturnType<typeof vi.fn> };

  beforeEach(() => {
    authService = { refreshToken: vi.fn(), logout: vi.fn() };
    tokenService = { getAccessToken: vi.fn(() => 'refreshed-token') };
    toast = { warning: vi.fn(), error: vi.fn() };

    TestBed.configureTestingModule({
      providers: [
        { provide: AuthService, useValue: authService },
        { provide: TokenService, useValue: tokenService },
        { provide: ToastService, useValue: toast },
      ],
    });
  });

  function run(req: HttpRequest<unknown>, next: HttpHandlerFn) {
    return TestBed.runInInjectionContext(() => errorInterceptorFn(req, next));
  }

  function errorResponse(status: number, body?: unknown): HttpErrorResponse {
    return new HttpErrorResponse({ status, error: body });
  }

  it('passes through a successful response untouched', () => {
    const req = new HttpRequest('GET', '/api/v1/claims');
    const next: HttpHandlerFn = () => of(new HttpResponse({ status: 200, body: { ok: true } }));

    let result: HttpResponse<unknown> | undefined;
    run(req, next).subscribe(res => (result = res as HttpResponse<unknown>));

    expect(result?.status).toBe(200);
    expect(toast.error).not.toHaveBeenCalled();
    expect(toast.warning).not.toHaveBeenCalled();
  });

  it('shows a warning toast on 429 without touching auth', () => {
    const req = new HttpRequest('GET', '/api/v1/claims');
    const next: HttpHandlerFn = () => throwError(() => errorResponse(429));

    run(req, next).subscribe({ error: () => {} });

    expect(toast.warning).toHaveBeenCalledWith('Too many requests. Please wait a moment.');
    expect(authService.refreshToken).not.toHaveBeenCalled();
  });

  it('shows the server-provided message on a 403', () => {
    const req = new HttpRequest('GET', '/api/v1/claims');
    const next: HttpHandlerFn = () => throwError(() => errorResponse(403, { detail: 'no access' }));

    run(req, next).subscribe({ error: () => {} });

    expect(toast.error).toHaveBeenCalledWith('no access');
  });

  it('falls back to a default message on 403 with no detail/message', () => {
    const req = new HttpRequest('GET', '/api/v1/claims');
    const next: HttpHandlerFn = () => throwError(() => errorResponse(403, {}));

    run(req, next).subscribe({ error: () => {} });

    expect(toast.error).toHaveBeenCalledWith('You do not have permission to perform this action.');
  });

  it('shows a generic message on a 5xx error', () => {
    const req = new HttpRequest('GET', '/api/v1/claims');
    const next: HttpHandlerFn = () => throwError(() => errorResponse(500));

    run(req, next).subscribe({ error: () => {} });

    expect(toast.error).toHaveBeenCalledWith('Something went wrong. Please try again later.');
  });

  it('shows the server-provided message on a generic 4xx error', () => {
    const req = new HttpRequest('GET', '/api/v1/claims');
    const next: HttpHandlerFn = () => throwError(() => errorResponse(422, { message: 'Invalid claim date' }));

    run(req, next).subscribe({ error: () => {} });

    expect(toast.error).toHaveBeenCalledWith('Invalid claim date');
  });

  it('suppresses toasts for non-429 errors on auth endpoints (auth pages handle their own errors)', () => {
    const req = new HttpRequest('POST', '/api/v1/auth/some-endpoint', {});
    const next: HttpHandlerFn = () => throwError(() => errorResponse(400, { message: 'bad request' }));

    run(req, next).subscribe({ error: () => {} });

    expect(toast.error).not.toHaveBeenCalled();
    expect(toast.warning).not.toHaveBeenCalled();
  });

  it('rethrows the original error after handling it', () => {
    const req = new HttpRequest('GET', '/api/v1/claims');
    const originalError = errorResponse(500);
    const next: HttpHandlerFn = () => throwError(() => originalError);

    let caught: unknown;
    run(req, next).subscribe({ error: err => (caught = err) });

    expect(caught).toBe(originalError);
  });

  describe('401 handling on non-auth endpoints', () => {
    it('refreshes the token, retries the request with the new bearer token, and returns the retried response', () => {
      const req = new HttpRequest('GET', '/api/v1/claims');
      const authResponse = { accessToken: 'new', refreshToken: 'new-r', user: {} } as AuthResponse;
      authService.refreshToken.mockReturnValue(of(authResponse));

      let callCount = 0;
      let retriedReq: HttpRequest<unknown> | undefined;
      const next: HttpHandlerFn = r => {
        callCount++;
        if (callCount === 1) return throwError(() => errorResponse(401));
        retriedReq = r;
        return of(new HttpResponse({ status: 200, body: { ok: true } }));
      };

      let result: HttpResponse<unknown> | undefined;
      run(req, next).subscribe(res => (result = res as HttpResponse<unknown>));

      expect(authService.refreshToken).toHaveBeenCalledTimes(1);
      expect(retriedReq?.headers.get('Authorization')).toBe('Bearer refreshed-token');
      expect(result?.status).toBe(200);
      expect(authService.logout).not.toHaveBeenCalled();
    });

    it('logs out and rethrows the original error when the refresh resolves to null', () => {
      const req = new HttpRequest('GET', '/api/v1/claims');
      const originalError = errorResponse(401);
      authService.refreshToken.mockReturnValue(of(null));

      const next: HttpHandlerFn = () => throwError(() => originalError);

      let caught: unknown;
      run(req, next).subscribe({ error: err => (caught = err) });

      expect(authService.logout).toHaveBeenCalled();
      expect(caught).toBe(originalError);
    });

    it('logs out and rethrows when the refresh call itself errors', () => {
      const req = new HttpRequest('GET', '/api/v1/claims');
      const refreshError = new Error('network down');
      authService.refreshToken.mockReturnValue(throwError(() => refreshError));

      const next: HttpHandlerFn = () => throwError(() => errorResponse(401));

      let caught: unknown;
      run(req, next).subscribe({ error: err => (caught = err) });

      expect(authService.logout).toHaveBeenCalled();
      expect(caught).toBe(refreshError);
    });

    it('does not attempt a refresh for a 401 on an auth endpoint', () => {
      const req = new HttpRequest('POST', '/api/v1/auth/login', {});
      const next: HttpHandlerFn = () => throwError(() => errorResponse(401, { message: 'bad creds' }));

      run(req, next).subscribe({ error: () => {} });

      expect(authService.refreshToken).not.toHaveBeenCalled();
      expect(toast.error).not.toHaveBeenCalled();
    });

    it('queues a second concurrent 401 behind the in-flight refresh and retries it once the refresh completes', () => {
      const refreshCall$ = new Subject<AuthResponse | null>();
      authService.refreshToken.mockReturnValue(refreshCall$.asObservable());

      const firstReq = new HttpRequest('GET', '/api/v1/claims/1');
      const secondReq = new HttpRequest('GET', '/api/v1/claims/2');
      const retriedReqs: HttpRequest<unknown>[] = [];

      let firstCallCount = 0;
      const firstNext: HttpHandlerFn = r => {
        firstCallCount++;
        if (firstCallCount === 1) return throwError(() => errorResponse(401));
        retriedReqs.push(r);
        return of(new HttpResponse({ status: 200, body: { from: 'first' } }));
      };

      let secondCallCount = 0;
      const secondNext: HttpHandlerFn = r => {
        secondCallCount++;
        if (secondCallCount === 1) return throwError(() => errorResponse(401));
        retriedReqs.push(r);
        return of(new HttpResponse({ status: 200, body: { from: 'second' } }));
      };

      let firstResult: HttpResponse<unknown> | undefined;
      let secondResult: HttpResponse<unknown> | undefined;

      run(firstReq, firstNext).subscribe(res => (firstResult = res as HttpResponse<unknown>));
      // second request arrives while the first refresh is still in flight
      run(secondReq, secondNext).subscribe(res => (secondResult = res as HttpResponse<unknown>));

      expect(authService.refreshToken).toHaveBeenCalledTimes(1);
      expect(firstResult).toBeUndefined();
      expect(secondResult).toBeUndefined();

      refreshCall$.next({ accessToken: 'new', refreshToken: 'new-r', user: {} } as AuthResponse);
      refreshCall$.complete();

      expect(firstResult?.status).toBe(200);
      expect(secondResult?.status).toBe(200);
      expect(retriedReqs.every(r => r.headers.get('Authorization') === 'Bearer refreshed-token')).toBe(true);
    });
  });
});
