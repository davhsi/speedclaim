import { Injectable, PLATFORM_ID, inject } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';

@Injectable({ providedIn: 'root' })
export class TokenService {
  private platformId = inject(PLATFORM_ID);
  private accessToken: string | null = null;

  private get isBrowser(): boolean {
    return isPlatformBrowser(this.platformId);
  }

  getAccessToken(): string | null {
    return this.accessToken;
  }

  getRefreshToken(): string | null {
    if (!this.isBrowser) return null;
    return localStorage.getItem('sc_refresh_token');
  }

  setTokens(accessToken: string, refreshToken: string): void {
    this.accessToken = accessToken;
    if (this.isBrowser) {
      localStorage.setItem('sc_refresh_token', refreshToken);
    }
  }

  clearTokens(): void {
    this.accessToken = null;
    if (this.isBrowser) {
      localStorage.removeItem('sc_refresh_token');
    }
  }

  isAccessTokenExpired(): boolean {
    if (!this.accessToken) return true;
    try {
      const payload = JSON.parse(atob(this.accessToken.split('.')[1]));
      return payload.exp * 1000 < Date.now();
    } catch {
      return true;
    }
  }
}
