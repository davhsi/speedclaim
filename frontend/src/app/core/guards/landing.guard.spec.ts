import { vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { Router, UrlTree } from '@angular/router';
import { signal } from '@angular/core';
import { of } from 'rxjs';
import { AuthService } from '../services/auth.service';
import { AuthUserDto } from '../models/api.models';
import { UserRole } from '../models/enums';
import { landingGuard } from './landing.guard';

function userWithRole(role: UserRole): AuthUserDto {
  return { role } as AuthUserDto;
}

async function runGuard() {
  const result = TestBed.runInInjectionContext(() => landingGuard({} as never, {} as never));
  if (result && typeof (result as { subscribe?: unknown }).subscribe === 'function') {
    return new Promise(resolve => (result as ReturnType<typeof of>).subscribe(resolve));
  }
  return result;
}

describe('landingGuard', () => {
  let currentUser: ReturnType<typeof signal<AuthUserDto | null>>;
  let initialized: ReturnType<typeof signal<boolean>>;
  let initFromStorage: ReturnType<typeof vi.fn>;
  let createUrlTree: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    currentUser = signal<AuthUserDto | null>(null);
    initialized = signal(false);
    initFromStorage = vi.fn(() => of(null));
    createUrlTree = vi.fn((commands: string[]) => ({ __urlTree: true, commands }) as unknown as UrlTree);

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
        { provide: Router, useValue: { createUrlTree } },
      ],
    });
  });

  it('allows access to the landing page when unauthenticated', async () => {
    initialized.set(true);
    currentUser.set(null);

    const result = await runGuard();

    expect(result).toBe(true);
    expect(createUrlTree).not.toHaveBeenCalled();
  });

  it.each([
    ['Admin', '/admin'],
    ['Agent', '/agent'],
    ['ClaimsOfficer', '/claims-officer'],
    ['FinanceOfficer', '/finance-officer'],
    ['Underwriter', '/underwriter'],
    ['Surveyor', '/surveyor'],
  ] as [UserRole, string][])('redirects an authenticated %s to %s', async (role, expectedPath) => {
    initialized.set(true);
    currentUser.set(userWithRole(role));

    const result = await runGuard();

    expect(createUrlTree).toHaveBeenCalledWith([expectedPath]);
    expect(result).toEqual({ __urlTree: true, commands: [expectedPath] });
  });

  it('redirects an authenticated Customer to /dashboard (no role-specific mapping)', async () => {
    initialized.set(true);
    currentUser.set(userWithRole('Customer'));

    const result = await runGuard();

    expect(createUrlTree).toHaveBeenCalledWith(['/dashboard']);
    expect(result).toEqual({ __urlTree: true, commands: ['/dashboard'] });
  });

  it('waits on initFromStorage when not yet initialized', async () => {
    initialized.set(false);
    initFromStorage.mockImplementation(() => {
      currentUser.set(userWithRole('Admin'));
      return of(null);
    });

    const result = await runGuard();

    expect(initFromStorage).toHaveBeenCalled();
    expect(result).toEqual({ __urlTree: true, commands: ['/admin'] });
  });
});
