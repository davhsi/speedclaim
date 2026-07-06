import { inject } from '@angular/core';
import { CanActivateFn, Router, UrlTree } from '@angular/router';
import { map } from 'rxjs';
import { AuthService } from '../services/auth.service';

export const authGuard: CanActivateFn = (route) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  const unauthenticatedRedirect = (): UrlTree => {
    const navigation = router.getCurrentNavigation();
    return router.createUrlTree([navigation?.trigger === 'popstate' ? '/' : '/auth/login']);
  };

  const checkAndRedirect = (): boolean | UrlTree => {
    if (!authService.isAuthenticated()) {
      return unauthenticatedRedirect();
    }

    const user = authService.currentUser();
    const routePath = route.routeConfig?.path;

    const roleRoutes: Record<string, string> = {
      Admin: 'admin',
      Agent: 'agent',
      ClaimsOfficer: 'claims-officer',
      FinanceOfficer: 'finance-officer',
      Underwriter: 'underwriter',
      Surveyor: 'surveyor',
    };

    const expectedRoute = user?.role ? roleRoutes[user.role] : undefined;
    if (expectedRoute && routePath !== expectedRoute && routePath !== 'auth') {
      return router.createUrlTree([`/${expectedRoute}`]);
    }

    return true;
  };

  if (authService.isAuthenticated()) return checkAndRedirect();

  if (!authService.initialized()) {
    return authService.initFromStorage().pipe(
      map(() => checkAndRedirect()),
    );
  }

  return unauthenticatedRedirect();
};
