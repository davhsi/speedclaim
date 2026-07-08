import { vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { HttpHandlerFn, HttpRequest, HttpResponse } from '@angular/common/http';
import { of } from 'rxjs';
import { authInterceptor } from './auth.interceptor';
import { TokenService } from '../services/token.service';

describe('authInterceptor', () => {
  let getAccessToken: ReturnType<typeof vi.fn>;
  let capturedReq: HttpRequest<unknown> | undefined;
  const next: HttpHandlerFn = (req) => {
    capturedReq = req;
    return of(new HttpResponse({ status: 200 }));
  };

  beforeEach(() => {
    capturedReq = undefined;
    getAccessToken = vi.fn(() => 'the-access-token');

    TestBed.configureTestingModule({
      providers: [{ provide: TokenService, useValue: { getAccessToken } }],
    });
  });

  function run(req: HttpRequest<unknown>) {
    return TestBed.runInInjectionContext(() => authInterceptor(req, next));
  }

  it('does not attach a token to the login request, even with a token available', () => {
    const req = new HttpRequest('POST', '/api/v1/auth/login', {});
    run(req).subscribe();
    expect(capturedReq?.headers.has('Authorization')).toBe(false);
  });

  it('does not attach a token to the refresh request', () => {
    const req = new HttpRequest('POST', '/api/v1/auth/refresh', {});
    run(req).subscribe();
    expect(capturedReq?.headers.has('Authorization')).toBe(false);
  });

  it.each([
    ['a public GET product list request', '/api/v1/products'],
    ['a public GET single-product request', '/api/v1/products/abc-123'],
    ['a public GET product-documents request', '/api/v1/products/abc-123/documents'],
  ])('does not attach a token to %s', (_description, url) => {
    const req = new HttpRequest('GET', url);
    run(req).subscribe();
    expect(capturedReq?.headers.has('Authorization')).toBe(false);
  });

  it('treats a POST to the products endpoint as protected (not public, since it is not a GET)', () => {
    const req = new HttpRequest('POST', '/api/v1/products', {});
    run(req).subscribe();
    expect(capturedReq?.headers.get('Authorization')).toBe('Bearer the-access-token');
  });

  it('attaches the bearer token to a protected request when a token is available', () => {
    const req = new HttpRequest('GET', '/api/v1/claims');
    run(req).subscribe();
    expect(capturedReq?.headers.get('Authorization')).toBe('Bearer the-access-token');
  });

  it('does not attach an Authorization header when no token is available', () => {
    getAccessToken.mockReturnValue(null);
    const req = new HttpRequest('GET', '/api/v1/claims');
    run(req).subscribe();
    expect(capturedReq?.headers.has('Authorization')).toBe(false);
  });

  it('does not match a products sub-route other than a trailing /documents as public', () => {
    const req = new HttpRequest('GET', '/api/v1/products/abc-123/reviews');
    run(req).subscribe();
    expect(capturedReq?.headers.get('Authorization')).toBe('Bearer the-access-token');
  });
});
