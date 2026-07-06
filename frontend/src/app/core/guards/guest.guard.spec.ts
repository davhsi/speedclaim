import { vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { Router, UrlTree } from '@angular/router';
import { signal } from '@angular/core';
import { of } from 'rxjs';
import { AuthService } from '../services/auth.service';
import { AuthUserDto } from '../models/api.models';
import { guestGuard } from './guest.guard';

async function runGuard() {
  const result = TestBed.runInInjectionContext(() => guestGuard({} as never, {} as never));
  if (result && typeof (result as { subscribe?: unknown }).subscribe === 'function') {
    return new Promise(resolve => (result as ReturnType<typeof of>).subscribe(resolve));
  }
  return result;
}

describe('guestGuard', () => {
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

  it('allows access when initialized and not authenticated', async () => {
    initialized.set(true);
    currentUser.set(null);

    const result = await runGuard();

    expect(result).toBe(true);
  });

  it('redirects to /dashboard when initialized and already authenticated', async () => {
    initialized.set(true);
    currentUser.set({ id: 'u1' } as AuthUserDto);

    const result = await runGuard();

    expect(createUrlTree).toHaveBeenCalledWith(['/dashboard']);
    expect(result).toEqual({ __urlTree: true, commands: ['/dashboard'] });
  });

  it('waits on initFromStorage when not yet initialized, then allows if still unauthenticated', async () => {
    initialized.set(false);
    initFromStorage.mockReturnValue(of(null));

    const result = await runGuard();

    expect(initFromStorage).toHaveBeenCalled();
    expect(result).toBe(true);
  });

  it('waits on initFromStorage when not yet initialized, then redirects if it resolved to an authenticated user', async () => {
    initialized.set(false);
    initFromStorage.mockImplementation(() => {
      currentUser.set({ id: 'u1' } as AuthUserDto);
      return of(null);
    });

    const result = await runGuard();

    expect(result).toEqual({ __urlTree: true, commands: ['/dashboard'] });
  });
});
