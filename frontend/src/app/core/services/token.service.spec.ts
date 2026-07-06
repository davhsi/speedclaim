import { vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { TokenService } from './token.service';

function createStorage(): Storage {
  const values = new Map<string, string>();
  return {
    get length() { return values.size; },
    clear: () => values.clear(),
    getItem: (key: string) => values.get(key) ?? null,
    key: (index: number) => Array.from(values.keys())[index] ?? null,
    removeItem: (key: string) => values.delete(key),
    setItem: (key: string, value: string) => values.set(key, value),
  };
}

function fakeJwt(exp: number): string {
  const header = btoa(JSON.stringify({ alg: 'none' }));
  const payload = btoa(JSON.stringify({ exp }));
  return `${header}.${payload}.signature`;
}

describe('TokenService', () => {
  let service: TokenService;

  beforeEach(() => {
    vi.stubGlobal('localStorage', createStorage());
    vi.stubGlobal('sessionStorage', createStorage());
    localStorage.clear();
    sessionStorage.clear();
    TestBed.configureTestingModule({});
    service = TestBed.inject(TokenService);
  });

  afterEach(() => {
    localStorage.clear();
    sessionStorage.clear();
  });

  describe('access token (in-memory only)', () => {
    it('returns null before any token is set', () => {
      expect(service.getAccessToken()).toBeNull();
    });

    it('returns the token set via setTokens', () => {
      service.setTokens('access-1', 'refresh-1', false);
      expect(service.getAccessToken()).toBe('access-1');
    });
  });

  describe('setTokens persistence', () => {
    it('stores the refresh token in localStorage when persistent=true', () => {
      service.setTokens('access-1', 'refresh-1', true);
      expect(localStorage.getItem('sc_refresh_token')).toBe('refresh-1');
      expect(sessionStorage.getItem('sc_refresh_token')).toBeNull();
    });

    it('stores the refresh token in sessionStorage when persistent=false', () => {
      service.setTokens('access-1', 'refresh-1', false);
      expect(sessionStorage.getItem('sc_refresh_token')).toBe('refresh-1');
      expect(localStorage.getItem('sc_refresh_token')).toBeNull();
    });

    it('keeps the refresh token in localStorage on silent refresh (persistent omitted) when it was already there', () => {
      service.setTokens('access-1', 'refresh-1', true);
      service.setTokens('access-2', 'refresh-2');
      expect(localStorage.getItem('sc_refresh_token')).toBe('refresh-2');
      expect(sessionStorage.getItem('sc_refresh_token')).toBeNull();
    });

    it('keeps the refresh token in sessionStorage on silent refresh (persistent omitted) when it was already there', () => {
      service.setTokens('access-1', 'refresh-1', false);
      service.setTokens('access-2', 'refresh-2');
      expect(sessionStorage.getItem('sc_refresh_token')).toBe('refresh-2');
      expect(localStorage.getItem('sc_refresh_token')).toBeNull();
    });

    it('defaults to sessionStorage on silent refresh when no prior token exists anywhere', () => {
      service.setTokens('access-1', 'refresh-1');
      expect(sessionStorage.getItem('sc_refresh_token')).toBe('refresh-1');
      expect(localStorage.getItem('sc_refresh_token')).toBeNull();
    });
  });

  describe('getRefreshToken', () => {
    it('reads from localStorage when present', () => {
      localStorage.setItem('sc_refresh_token', 'from-local');
      expect(service.getRefreshToken()).toBe('from-local');
    });

    it('falls back to sessionStorage when localStorage is empty', () => {
      sessionStorage.setItem('sc_refresh_token', 'from-session');
      expect(service.getRefreshToken()).toBe('from-session');
    });

    it('returns null when neither storage has a token', () => {
      expect(service.getRefreshToken()).toBeNull();
    });
  });

  describe('clearTokens', () => {
    it('clears the in-memory access token and both storages', () => {
      service.setTokens('access-1', 'refresh-1', true);
      service.clearTokens();
      expect(service.getAccessToken()).toBeNull();
      expect(localStorage.getItem('sc_refresh_token')).toBeNull();
      expect(sessionStorage.getItem('sc_refresh_token')).toBeNull();
    });
  });

  describe('isAccessTokenExpired', () => {
    it('returns true when no access token is set', () => {
      expect(service.isAccessTokenExpired()).toBe(true);
    });

    it('returns false for a token with a future exp', () => {
      const future = Math.floor(Date.now() / 1000) + 3600;
      service.setTokens(fakeJwt(future), 'refresh-1', false);
      expect(service.isAccessTokenExpired()).toBe(false);
    });

    it('returns true for a token with a past exp', () => {
      const past = Math.floor(Date.now() / 1000) - 3600;
      service.setTokens(fakeJwt(past), 'refresh-1', false);
      expect(service.isAccessTokenExpired()).toBe(true);
    });

    it('returns true for a malformed token', () => {
      service.setTokens('not-a-jwt', 'refresh-1', false);
      expect(service.isAccessTokenExpired()).toBe(true);
    });
  });
});
