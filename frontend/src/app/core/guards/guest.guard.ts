import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { map } from 'rxjs';
import { AuthService } from '../services/auth.service';

export const guestGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (authService.initialized()) {
    return authService.isAuthenticated() ? router.createUrlTree(['/dashboard']) : true;
  }

  return authService.initFromStorage().pipe(
    map(() => {
      if (authService.isAuthenticated()) return router.createUrlTree(['/dashboard']);
      return true;
    }),
  );
};
