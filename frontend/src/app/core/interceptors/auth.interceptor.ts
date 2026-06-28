import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { TokenService } from '../services/token.service';

const PUBLIC_AUTH_URLS = [
  '/api/v1/auth/login',
  '/api/v1/auth/register',
  '/api/v1/auth/forgot-password',
  '/api/v1/auth/reset-password',
  '/api/v1/auth/verify-email',
  '/api/v1/auth/resend-verification',
  '/api/v1/auth/refresh',
];

function isPublicRequest(method: string, url: string): boolean {
  const path = url.split('?')[0];
  if (PUBLIC_AUTH_URLS.some(publicUrl => path.endsWith(publicUrl))) return true;
  if (method !== 'GET') return false;

  return path.endsWith('/api/v1/products')
    || /\/api\/v1\/products\/[^/]+$/.test(path)
    || /\/api\/v1\/products\/[^/]+\/documents$/.test(path);
}

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const tokenService = inject(TokenService);

  if (isPublicRequest(req.method, req.url)) {
    return next(req);
  }

  const token = tokenService.getAccessToken();
  if (token) {
    req = req.clone({
      setHeaders: { Authorization: `Bearer ${token}` },
    });
  }

  return next(req);
};
