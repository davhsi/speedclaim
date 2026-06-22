import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { BehaviorSubject, catchError, filter, switchMap, take, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';
import { TokenService } from '../services/token.service';
import { ToastService } from '../../shared/components/toast/toast.service';

let isRefreshing = false;
const refreshSubject = new BehaviorSubject<boolean>(false);

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

      if (error.status === 429) {
        toast.warning('Too many requests. Please wait a moment.');
      } else if (error.status === 403) {
        toast.error('You do not have permission to perform this action.');
      } else if (error.status >= 500) {
        toast.error('Something went wrong. Please try again later.');
      }

      return throwError(() => error);
    }),
  );
};
