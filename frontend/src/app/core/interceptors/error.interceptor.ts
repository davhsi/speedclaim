import { HttpContextToken, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { BehaviorSubject, catchError, filter, switchMap, take, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';
import { TokenService } from '../services/token.service';
import { ToastService } from '../../shared/components/toast/toast.service';

/** Set true on a request's HttpContext to suppress the global error toast (e.g. an expected 404 the caller handles inline). */
export const SKIP_ERROR_TOAST = new HttpContextToken<boolean>(() => false);

let isRefreshing = false;
const refreshSubject = new BehaviorSubject<boolean>(false);

/** ASP.NET ValidationProblemDetails responses carry field errors under `errors`, not `detail`/`message`. */
function extractValidationErrors(body: unknown): string | null {
  const errors = (body as { errors?: Record<string, string[]> } | null)?.errors;
  if (!errors || typeof errors !== 'object') return null;
  const messages = Object.values(errors).flat().filter(Boolean);
  return messages.length ? messages.join(' ') : null;
}

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const tokenService = inject(TokenService);
  const toast = inject(ToastService);

  return next(req).pipe(
    catchError(error => {
      if (error.status === 401 && !req.url.includes('/auth/')) {
        if (!isRefreshing) {
          isRefreshing = true;
          refreshSubject.next(false);

          return authService.refreshToken().pipe(
            switchMap(result => {
              isRefreshing = false;
              refreshSubject.next(true);

              if (!result) {
                authService.logout();
                return throwError(() => error);
              }

              return next(req.clone({
                setHeaders: { Authorization: `Bearer ${tokenService.getAccessToken()!}` },
              }));
            }),
            catchError(err => {
              isRefreshing = false;
              authService.logout();
              return throwError(() => err);
            }),
          );
        }

        return refreshSubject.pipe(
          filter(ready => ready),
          take(1),
          switchMap(() =>
            next(req.clone({
              setHeaders: { Authorization: `Bearer ${tokenService.getAccessToken()!}` },
            })),
          ),
        );
      }

      if (req.context.get(SKIP_ERROR_TOAST)) {
        return throwError(() => error);
      }

      const isAuthEndpoint = req.url.includes('/auth/');

      if (error.status === 429) {
        toast.warning('Too many requests. Please wait a moment.');
      } else if (isAuthEndpoint) {
        // Auth pages handle their own errors inline — only 429 gets a toast
      } else if (error.status === 403) {
        const errorMsg = error.error?.detail || error.error?.message || 'You do not have permission to perform this action.';
        toast.error(errorMsg);
      } else if (error.status >= 500) {
        toast.error('Something went wrong. Please try again later.');
      } else if (error.status >= 400 && error.status < 500) {
        const errorMsg = error.error?.detail || error.error?.message || extractValidationErrors(error.error) || 'An error occurred. Please try again.';
        toast.error(errorMsg);
      }

      return throwError(() => error);
    }),
  );
};
