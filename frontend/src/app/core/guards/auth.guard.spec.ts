import { vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, Router, UrlTree } from '@angular/router';
import { signal } from '@angular/core';
import { of } from 'rxjs';
import { AuthService } from '../services/auth.service';
import { AuthUserDto } from '../models/api.models';
import { UserRole } from '../models/enums';
import { authGuard } from './auth.guard';

function userWithRole(role: UserRole): AuthUserDto {
  return { role } as AuthUserDto;
}

function routeWithPath(path: string): ActivatedRouteSnapshot {
  return { routeConfig: { path } } as unknown as ActivatedRouteSnapshot;
}

async function runGuard(route: ActivatedRouteSnapshot) {
  const result = TestBed.runInInjectionContext(() => authGuard(route, {} as never));
  if (result && typeof (result as { subscribe?: unknown }).subscribe === 'function') {
    return new Promise(resolve => (result as ReturnType<typeof of>).subscribe(resolve));
  }
  return result;
}

describe('authGuard', () => {
  let currentUser: ReturnType<typeof signal<AuthUserDto | null>>;
  let initialized: ReturnType<typeof signal<boolean>>;
  let initFromStorage: ReturnType<typeof vi.fn>;
  let createUrlTree: ReturnType<typeof vi.fn>;
  let getCurrentNavigation: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    currentUser = signal<AuthUserDto | null>(null);
    initialized = signal(false);
    initFromStorage = vi.fn(() => of(null));
    createUrlTree = vi.fn((commands: string[]) => ({ __urlTree: true, commands }) as unknown as UrlTree);
    getCurrentNavigation = vi.fn(() => ({ trigger: 'imperative' }));

    TestBed.configureTestingModule({
      providers: [
        {
          provide: AuthService,
          useValue: {
            currentUser,
            initialized,
            initFromStorage,
            isAuthenticated: () => !!currentUser(),
          },
        },
        { provide: Router, useValue: { createUrlTree, getCurrentNavigation } },
      ],
    });
  });

  it('redirects to /auth/login when not authenticated and already initialized', async () => {
    initialized.set(true);
    currentUser.set(null);

    const result = await runGuard(routeWithPath('admin'));

    expect(createUrlTree).toHaveBeenCalledWith(['/auth/login']);
    expect(result).toEqual({ __urlTree: true, commands: ['/auth/login'] });
  });

  it('redirects browser-back navigation from a protected route to landing when unauthenticated', async () => {
    initialized.set(true);
    currentUser.set(null);
    getCurrentNavigation.mockReturnValue({ trigger: 'popstate' });

    const result = await runGuard(routeWithPath('admin'));

    expect(createUrlTree).toHaveBeenCalledWith(['/']);
    expect(result).toEqual({ __urlTree: true, commands: ['/'] });
  });

  it('allows a role-matching route (e.g. Admin hitting /admin)', async () => {
    currentUser.set(userWithRole('Admin'));

    const result = await runGuard(routeWithPath('admin'));

    expect(result).toBe(true);
    expect(createUrlTree).not.toHaveBeenCalled();
  });

  it('redirects to the correct role home when the route does not match the user role', async () => {
    currentUser.set(userWithRole('Agent'));

    const result = await runGuard(routeWithPath('admin'));

    expect(createUrlTree).toHaveBeenCalledWith(['/agent']);
    expect(result).toEqual({ __urlTree: true, commands: ['/agent'] });
  });

  it('allows a Customer (no role-route mapping) to hit any customer-portal route', async () => {
    currentUser.set(userWithRole('Customer'));

    const result = await runGuard(routeWithPath('claims'));

    expect(result).toBe(true);
    expect(createUrlTree).not.toHaveBeenCalled();
  });

  it('always allows the "auth" route regardless of role', async () => {
    currentUser.set(userWithRole('Admin'));

    const result = await runGuard(routeWithPath('auth'));

    expect(result).toBe(true);
    expect(createUrlTree).not.toHaveBeenCalled();
  });

  it('waits on initFromStorage when not yet initialized and not authenticated, then evaluates with the resolved state', async () => {
    initialized.set(false);
    initFromStorage.mockImplementation(() => {
      currentUser.set(userWithRole('Admin'));
      return of(null);
    });

    const result = await runGuard(routeWithPath('admin'));

    expect(initFromStorage).toHaveBeenCalled();
    expect(result).toBe(true);
  });

  it('redirects to /auth/login when already initialized, not authenticated, and initFromStorage is skipped', async () => {
    initialized.set(true);
    currentUser.set(null);

    const result = await runGuard(routeWithPath('admin'));

    expect(initFromStorage).not.toHaveBeenCalled();
    expect(result).toEqual({ __urlTree: true, commands: ['/auth/login'] });
  });
});
