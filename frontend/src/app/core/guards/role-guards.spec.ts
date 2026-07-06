import { vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { Router, UrlTree } from '@angular/router';
import { signal } from '@angular/core';
import { of } from 'rxjs';
import { AuthService } from '../services/auth.service';
import { AuthUserDto } from '../models/api.models';
import { UserRole } from '../models/enums';
import { adminGuard } from './admin.guard';
import { agentGuard } from './agent.guard';
import { claimsOfficerGuard } from './claims-officer.guard';
import { financeOfficerGuard } from './finance-officer.guard';
import { surveyorGuard } from './surveyor.guard';
import { underwriterGuard } from './underwriter.guard';

type Guard = typeof adminGuard;

const cases: { name: string; guard: Guard; requiredRole: UserRole }[] = [
  { name: 'adminGuard', guard: adminGuard, requiredRole: 'Admin' },
  { name: 'agentGuard', guard: agentGuard, requiredRole: 'Agent' },
  { name: 'claimsOfficerGuard', guard: claimsOfficerGuard, requiredRole: 'ClaimsOfficer' },
  { name: 'financeOfficerGuard', guard: financeOfficerGuard, requiredRole: 'FinanceOfficer' },
  { name: 'surveyorGuard', guard: surveyorGuard, requiredRole: 'Surveyor' },
  { name: 'underwriterGuard', guard: underwriterGuard, requiredRole: 'Underwriter' },
];

function userWithRole(role: UserRole): AuthUserDto {
  return { role } as AuthUserDto;
}

async function runGuard(guard: Guard) {
  const result = TestBed.runInInjectionContext(() => guard({} as never, {} as never));
  if (result && typeof (result as { subscribe?: unknown }).subscribe === 'function') {
    return new Promise(resolve => (result as ReturnType<typeof of>).subscribe(resolve));
  }
  return result;
}

describe.each(cases)('$name', ({ guard, requiredRole }) => {
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
          useValue: { currentUser, initialized, initFromStorage },
        },
        { provide: Router, useValue: { createUrlTree } },
      ],
    });
  });

  it(`allows activation when the user has the ${requiredRole} role and auth is already initialized`, async () => {
    initialized.set(true);
    currentUser.set(userWithRole(requiredRole));

    const result = await runGuard(guard);

    expect(result).toBe(true);
    expect(createUrlTree).not.toHaveBeenCalled();
  });

  it('redirects to /dashboard when the user has a different role and auth is already initialized', async () => {
    initialized.set(true);
    currentUser.set(userWithRole('Customer' as UserRole));

    const result = await runGuard(guard);

    expect(createUrlTree).toHaveBeenCalledWith(['/dashboard']);
    expect(result).toEqual({ __urlTree: true, commands: ['/dashboard'] });
  });

  it('redirects to /dashboard when there is no current user and auth is already initialized', async () => {
    initialized.set(true);
    currentUser.set(null);

    const result = await runGuard(guard);

    expect(createUrlTree).toHaveBeenCalledWith(['/dashboard']);
    expect(result).toEqual({ __urlTree: true, commands: ['/dashboard'] });
  });

  it('waits on initFromStorage before checking role when auth is not yet initialized', async () => {
    initialized.set(false);
    initFromStorage.mockImplementation(() => {
      // simulate initFromStorage populating currentUser as a side effect, like the real service does
      currentUser.set(userWithRole(requiredRole));
      return of(null);
    });

    const result = await runGuard(guard);

    expect(initFromStorage).toHaveBeenCalled();
    expect(result).toBe(true);
  });
});
