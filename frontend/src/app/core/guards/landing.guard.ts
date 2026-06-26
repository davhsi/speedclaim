import { inject } from '@angular/core';
import { CanActivateFn, Router, UrlTree } from '@angular/router';
import { map } from 'rxjs';
import { AuthService } from '../services/auth.service';

export const landingGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  const roleRoutes: Record<string, string> = {
    Admin: '/admin',
    Agent: '/agent',
    ClaimsOfficer: '/claims-officer',
    FinanceOfficer: '/finance-officer',
    Underwriter: '/underwriter',
    Surveyor: '/surveyor',
  };

  const redirect = (): boolean | UrlTree => {
    if (!auth.isAuthenticated()) return true;
    const role = auth.currentUser()?.role;
    return router.createUrlTree([role && roleRoutes[role] ? roleRoutes[role] : '/dashboard']);
  };

  if (auth.initialized()) return redirect();
  return auth.initFromStorage().pipe(map(() => redirect()));
};
