import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideRouter, Router } from '@angular/router';
import { AuthService } from './auth.service';
import { environment } from '../../../environments/environment';

describe('AuthService', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;
  let router: Router;

  const apiUrl = environment.apiUrl;

  function createToken(expInSeconds: number): string {
    const header = btoa(JSON.stringify({ alg: 'HS256', typ: 'JWT' }));
    const payload = btoa(JSON.stringify({ exp: Math.floor(Date.now() / 1000) + expInSeconds }));
    return `${header}.${payload}.signature`;
  }

  beforeEach(() => {
    localStorage.clear();

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([{ path: 'login', component: {} as any }]),
      ]
    });

    service = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);
    router = TestBed.inject(Router);
    spyOn(router, 'navigate');
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.clear();
  });

  it('should store access token and refresh token on login', () => {
    const validToken = createToken(3600);
    const mockResponse = {
      accessToken: validToken,
      refreshToken: 'refresh-abc-123',
      expiresIn: 1800
    };

    service.login('user@test.com', 'password123').subscribe();

    const req = httpMock.expectOne(`${apiUrl}/auth/login`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ email: 'user@test.com', password: 'password123' });
    req.flush(mockResponse);

    expect(service.getAccessToken()).toBe(validToken);
    expect(service.getRefreshToken()).toBe('refresh-abc-123');
  });

  it('should update both tokens on refresh', () => {
    const initialToken = createToken(3600);
    const newToken = createToken(7200);

    // First login to set initial tokens
    service.login('user@test.com', 'pass').subscribe();
    httpMock.expectOne(`${apiUrl}/auth/login`).flush({
      accessToken: initialToken,
      refreshToken: 'initial-refresh',
      expiresIn: 1800
    });

    // Now refresh
    service.refresh().subscribe();

    const req = httpMock.expectOne(`${apiUrl}/auth/refresh`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ refreshToken: 'initial-refresh' });
    req.flush({
      accessToken: newToken,
      refreshToken: 'new-refresh-token',
      expiresIn: 1800
    });

    expect(service.getAccessToken()).toBe(newToken);
    expect(service.getRefreshToken()).toBe('new-refresh-token');
  });

  it('should clear tokens and navigate to /login on logout', () => {
    const validToken = createToken(3600);

    // Login first
    service.login('user@test.com', 'pass').subscribe();
    httpMock.expectOne(`${apiUrl}/auth/login`).flush({
      accessToken: validToken,
      refreshToken: 'refresh-to-revoke',
      expiresIn: 1800
    });

    // Logout
    service.logout();

    // The logout fires a POST (fire-and-forget)
    const req = httpMock.expectOne(`${apiUrl}/auth/logout`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ refreshToken: 'refresh-to-revoke' });
    req.flush({});

    expect(service.getAccessToken()).toBeNull();
    expect(service.getRefreshToken()).toBeNull();
    expect(router.navigate).toHaveBeenCalledWith(['/login']);
  });

  it('should return true for isAuthenticated when token exists and is not expired', () => {
    const validToken = createToken(3600);

    service.login('user@test.com', 'pass').subscribe();
    httpMock.expectOne(`${apiUrl}/auth/login`).flush({
      accessToken: validToken,
      refreshToken: 'refresh',
      expiresIn: 1800
    });

    expect(service.isAuthenticated()).toBeTrue();
  });

  it('should return false for isAuthenticated when no token exists', () => {
    expect(service.isAuthenticated()).toBeFalse();
  });

  it('should return false for isAuthenticated when token is expired', () => {
    const expiredToken = createToken(-100);

    service.login('user@test.com', 'pass').subscribe();
    httpMock.expectOne(`${apiUrl}/auth/login`).flush({
      accessToken: expiredToken,
      refreshToken: 'refresh',
      expiresIn: 1800
    });

    expect(service.isAuthenticated()).toBeFalse();
  });

  it('should attempt refresh on bootstrap when refresh token exists in localStorage', () => {
    // We need to set localStorage BEFORE creating a new service instance
    localStorage.setItem('paga-refresh-token', 'existing-refresh-token');

    // Reset TestBed to create a fresh service instance that reads localStorage
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([{ path: 'login', component: {} as any }]),
      ]
    });

    const freshHttpMock = TestBed.inject(HttpTestingController);
    TestBed.inject(AuthService); // this triggers bootstrap

    const req = freshHttpMock.expectOne(`${apiUrl}/auth/refresh`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ refreshToken: 'existing-refresh-token' });

    const validToken = createToken(3600);
    req.flush({
      accessToken: validToken,
      refreshToken: 'refreshed-token',
      expiresIn: 1800
    });

    freshHttpMock.verify();
  });
});
