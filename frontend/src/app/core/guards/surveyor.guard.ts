import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { map } from 'rxjs';
import { AuthService } from '../services/auth.service';

export const surveyorGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  const checkRole = () => {
    const user = authService.currentUser();
    if (user?.role === 'Surveyor') return true;
    return router.createUrlTree(['/dashboard']);
  };

  if (authService.initialized()) return checkRole();
  return authService.initFromStorage().pipe(map(() => checkRole()));
};
