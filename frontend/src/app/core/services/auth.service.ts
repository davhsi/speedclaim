import { Injectable, inject, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, tap, catchError, of } from 'rxjs';
import { TokenService } from './token.service';
import {
  AuthResponse, AuthUserDto, LoginRequest, RegisterUserRequest,
  RegistrationResponse, ForgotPasswordRequest, ResetPasswordRequest,
  VerifyEmailRequest, ResendVerificationRequest, ApiMessage,
} from '../models/api.models';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private http = inject(HttpClient);
  private router = inject(Router);
  private tokenService = inject(TokenService);

  private readonly apiUrl = '/api/v1/auth';

  currentUser = signal<AuthUserDto | null>(null);
  isAuthenticated = computed(() => !!this.currentUser());
  isLoading = signal(false);
  initialized = signal(false);

  login(req: LoginRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/login`, req).pipe(
      tap(res => {
        this.tokenService.setTokens(res.accessToken, res.refreshToken);
        this.currentUser.set(res.user);
      }),
    );
  }

  register(req: RegisterUserRequest): Observable<RegistrationResponse> {
    return this.http.post<RegistrationResponse>(`${this.apiUrl}/register`, req);
  }

  logout(): void {
    this.http.post(`${this.apiUrl}/logout`, {}).subscribe({ error: () => {} });
    this.tokenService.clearTokens();
    this.currentUser.set(null);
    this.router.navigate(['/auth/login']);
  }

  refreshToken(): Observable<AuthResponse | null> {
    const refreshToken = this.tokenService.getRefreshToken();
    if (!refreshToken) return of(null);

    return this.http.post<AuthResponse>(`${this.apiUrl}/refresh`, { refreshToken }).pipe(
      tap(res => {
        this.tokenService.setTokens(res.accessToken, res.refreshToken);
        this.currentUser.set(res.user);
      }),
      catchError(() => {
        this.tokenService.clearTokens();
        this.currentUser.set(null);
        return of(null);
      }),
    );
  }

  verifyEmail(req: VerifyEmailRequest): Observable<ApiMessage> {
    return this.http.post<ApiMessage>(`${this.apiUrl}/verify-email`, req);
  }

  resendVerificationEmail(req: ResendVerificationRequest): Observable<ApiMessage> {
    return this.http.post<ApiMessage>(`${this.apiUrl}/resend-verification`, req);
  }

  forgotPassword(req: ForgotPasswordRequest): Observable<ApiMessage> {
    return this.http.post<ApiMessage>(`${this.apiUrl}/forgot-password`, req);
  }

  resetPassword(req: ResetPasswordRequest): Observable<ApiMessage> {
    return this.http.post<ApiMessage>(`${this.apiUrl}/reset-password`, req);
  }

  initFromStorage(): Observable<AuthResponse | null> {
    if (this.initialized()) return of(null);
    this.isLoading.set(true);
    return this.refreshToken().pipe(
      tap(() => {
        this.initialized.set(true);
        this.isLoading.set(false);
      }),
      catchError(() => {
        this.initialized.set(true);
        this.isLoading.set(false);
        return of(null);
      }),
    );
  }
}
