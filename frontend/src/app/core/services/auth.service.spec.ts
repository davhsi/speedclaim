import { vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Router } from '@angular/router';
import { AuthService } from './auth.service';
import { TokenService } from './token.service';
import { NotificationService } from './notification.service';
import { AuthResponse, AuthUserDto, LoginRequest, RegisterUserRequest } from '../models/api.models';

describe('AuthService', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;
  let tokenService: { setTokens: ReturnType<typeof vi.fn>; clearTokens: ReturnType<typeof vi.fn>; getRefreshToken: ReturnType<typeof vi.fn> };
  let notificationService: { startRealtime: ReturnType<typeof vi.fn>; stopRealtime: ReturnType<typeof vi.fn> };
  let router: { navigate: ReturnType<typeof vi.fn>; createUrlTree: ReturnType<typeof vi.fn> };

  const user: AuthUserDto = {
    id: 'user-1',
    email: 'jane@example.com',
    salutation: 'Ms' as AuthUserDto['salutation'],
    firstName: 'Jane',
    lastName: 'Doe',
    fullName: 'Jane Doe',
    phone: '9999999999',
    role: 'Customer' as AuthUserDto['role'],
    maritalStatus: 'Single' as AuthUserDto['maritalStatus'],
  };

  const authResponse: AuthResponse = {
    accessToken: 'access-1',
    refreshToken: 'refresh-1',
    user,
  };

  beforeEach(() => {
    tokenService = { setTokens: vi.fn(), clearTokens: vi.fn(), getRefreshToken: vi.fn() };
    notificationService = { startRealtime: vi.fn(), stopRealtime: vi.fn() };
    router = { navigate: vi.fn(), createUrlTree: vi.fn() };

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: TokenService, useValue: tokenService },
        { provide: NotificationService, useValue: notificationService },
        { provide: Router, useValue: router },
      ],
    });

    service = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  describe('login', () => {
    it('posts credentials, stores tokens with rememberMe, sets currentUser, and starts realtime', () => {
      const req: LoginRequest = { email: user.email, password: 'secret' };

      service.login(req, true).subscribe(res => {
        expect(res).toEqual(authResponse);
      });

      const call = httpMock.expectOne('/api/v1/auth/login');
      expect(call.request.method).toBe('POST');
      expect(call.request.body).toEqual(req);
      call.flush(authResponse);

      expect(tokenService.setTokens).toHaveBeenCalledWith('access-1', 'refresh-1', true);
      expect(service.currentUser()).toEqual(user);
      expect(service.isAuthenticated()).toBe(true);
      expect(notificationService.startRealtime).toHaveBeenCalled();
    });

    it('defaults rememberMe to false when not provided', () => {
      const req: LoginRequest = { email: user.email, password: 'secret' };

      service.login(req).subscribe();

      const call = httpMock.expectOne('/api/v1/auth/login');
      call.flush(authResponse);

      expect(tokenService.setTokens).toHaveBeenCalledWith('access-1', 'refresh-1', false);
    });
  });

  describe('register', () => {
    it('posts the registration payload', () => {
      const req: RegisterUserRequest = {
        salutationTitle: 'Ms' as RegisterUserRequest['salutationTitle'],
        firstName: 'Jane',
        lastName: 'Doe',
        email: user.email,
        phone: '9999999999',
        password: 'secret123',
        dateOfBirth: '1995-01-01',
        gender: 'Female' as RegisterUserRequest['gender'],
        maritalStatus: 'Single' as RegisterUserRequest['maritalStatus'],
        aadhaarNumber: '123456789012',
        panNumber: 'ABCDE1234F',
        permanentAddress: { line1: 'x', city: 'y', state: 'z', postalCode: '000000', country: 'IN' },
        currentAddress: { line1: 'x', city: 'y', state: 'z', postalCode: '000000', country: 'IN' },
        consentDataProcessing: true,
        consentKycCollection: true,
      };

      service.register(req).subscribe(res => {
        expect(res.message).toBe('Registered');
      });

      const call = httpMock.expectOne('/api/v1/auth/register');
      expect(call.request.method).toBe('POST');
      call.flush({ message: 'Registered' });
    });
  });

  describe('logout', () => {
    it('clears tokens, currentUser, stops realtime, navigates to login, and fires the logout request', () => {
      service.currentUser.set(user);

      service.logout();

      const call = httpMock.expectOne('/api/v1/auth/logout');
      expect(call.request.method).toBe('POST');
      call.flush({});

      expect(tokenService.clearTokens).toHaveBeenCalled();
      expect(service.currentUser()).toBeNull();
      expect(notificationService.stopRealtime).toHaveBeenCalled();
      expect(router.navigate).toHaveBeenCalledWith(['/auth/login']);
    });

    it('still clears local state even if the logout request fails', () => {
      service.currentUser.set(user);

      service.logout();

      const call = httpMock.expectOne('/api/v1/auth/logout');
      call.error(new ProgressEvent('error'));

      expect(tokenService.clearTokens).toHaveBeenCalled();
      expect(service.currentUser()).toBeNull();
    });
  });

  describe('refreshToken', () => {
    it('returns of(null) without an HTTP call when there is no stored refresh token', () => {
      tokenService.getRefreshToken.mockReturnValue(null);

      let result: AuthResponse | null | undefined;
      service.refreshToken().subscribe(res => (result = res));

      expect(result).toBeNull();
      httpMock.expectNone('/api/v1/auth/refresh');
    });

    it('exchanges the refresh token, updates tokens/currentUser, and starts realtime on success', () => {
      tokenService.getRefreshToken.mockReturnValue('refresh-1');

      let result: AuthResponse | null | undefined;
      service.refreshToken().subscribe(res => (result = res));

      const call = httpMock.expectOne('/api/v1/auth/refresh');
      expect(call.request.body).toEqual({ refreshToken: 'refresh-1' });
      call.flush(authResponse);

      expect(result).toEqual(authResponse);
      expect(tokenService.setTokens).toHaveBeenCalledWith('access-1', 'refresh-1');
      expect(service.currentUser()).toEqual(user);
      expect(notificationService.startRealtime).toHaveBeenCalled();
    });

    it('clears tokens/currentUser and resolves to null when the refresh call fails', () => {
      tokenService.getRefreshToken.mockReturnValue('stale-refresh');

      let result: AuthResponse | null | undefined;
      service.refreshToken().subscribe(res => (result = res));

      const call = httpMock.expectOne('/api/v1/auth/refresh');
      call.flush({ message: 'invalid' }, { status: 401, statusText: 'Unauthorized' });

      expect(result).toBeNull();
      expect(tokenService.clearTokens).toHaveBeenCalled();
      expect(service.currentUser()).toBeNull();
      expect(notificationService.stopRealtime).toHaveBeenCalled();
    });
  });

  describe('initFromStorage', () => {
    it('calls refreshToken, then marks initialized and stops loading', () => {
      tokenService.getRefreshToken.mockReturnValue('refresh-1');

      service.initFromStorage().subscribe();

      const call = httpMock.expectOne('/api/v1/auth/refresh');
      call.flush(authResponse);

      expect(service.initialized()).toBe(true);
      expect(service.isLoading()).toBe(false);
    });

    it('is a no-op returning of(null) if already initialized', () => {
      tokenService.getRefreshToken.mockReturnValue(null);
      service.initFromStorage().subscribe();
      httpMock.expectNone('/api/v1/auth/refresh');
      expect(service.initialized()).toBe(true);

      let result: AuthResponse | null | undefined = undefined;
      service.initFromStorage().subscribe(res => (result = res));
      expect(result).toBeNull();
    });
  });

  describe('patchCurrentUser', () => {
    it('merges the patch into the current user when one is set', () => {
      service.currentUser.set(user);
      service.patchCurrentUser({ phone: '8888888888' });
      expect(service.currentUser()?.phone).toBe('8888888888');
      expect(service.currentUser()?.email).toBe(user.email);
    });

    it('does nothing when there is no current user', () => {
      service.currentUser.set(null);
      service.patchCurrentUser({ phone: '8888888888' });
      expect(service.currentUser()).toBeNull();
    });
  });
});
