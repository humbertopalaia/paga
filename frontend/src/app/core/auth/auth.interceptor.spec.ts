import { TestBed } from '@angular/core/testing';
import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { of, throwError } from 'rxjs';
import { tap } from 'rxjs/operators';

import { authInterceptor } from './auth.interceptor';
import { AuthService } from './auth.service';

describe('authInterceptor', () => {
  let httpClient: HttpClient;
  let httpMock: HttpTestingController;
  let authServiceSpy: jasmine.SpyObj<AuthService>;

  beforeEach(() => {
    authServiceSpy = jasmine.createSpyObj('AuthService', [
      'getAccessToken',
      'getRefreshToken',
      'refresh',
      'logout',
    ]);
    authServiceSpy.getAccessToken.and.returnValue('valid-token');

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        { provide: AuthService, useValue: authServiceSpy },
      ],
    });

    httpClient = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should attach Bearer token to outgoing requests', () => {
    httpClient.get('/api/users').subscribe();

    const req = httpMock.expectOne('/api/users');
    expect(req.request.headers.get('Authorization')).toBe('Bearer valid-token');
    req.flush([]);
  });

  it('should not attach token for /auth/login requests', () => {
    httpClient.post('/api/auth/login', { email: 'a@b.com', password: '123' }).subscribe();

    const req = httpMock.expectOne('/api/auth/login');
    expect(req.request.headers.has('Authorization')).toBeFalse();
    req.flush({});
  });

  it('should not attach token for /auth/refresh requests', () => {
    httpClient.post('/api/auth/refresh', { refreshToken: 'rt' }).subscribe();

    const req = httpMock.expectOne('/api/auth/refresh');
    expect(req.request.headers.has('Authorization')).toBeFalse();
    req.flush({});
  });

  it('should not attach token when no access token exists', () => {
    authServiceSpy.getAccessToken.and.returnValue(null);

    httpClient.get('/api/expense-types').subscribe();

    const req = httpMock.expectOne('/api/expense-types');
    expect(req.request.headers.has('Authorization')).toBeFalse();
    req.flush([]);
  });

  it('should attempt refresh on 401 and retry the original request', () => {
    authServiceSpy.refresh.and.returnValue(
      of(undefined as void).pipe(
        tap(() => authServiceSpy.getAccessToken.and.returnValue('new-token'))
      )
    );

    httpClient.get('/api/users').subscribe();

    // First request gets 401
    const firstReq = httpMock.expectOne('/api/users');
    firstReq.flush(null, { status: 401, statusText: 'Unauthorized' });

    // After refresh, request is retried with the new token
    const retryReq = httpMock.expectOne('/api/users');
    expect(retryReq.request.headers.get('Authorization')).toBe('Bearer new-token');
    retryReq.flush([{ id: '1', name: 'User' }]);
  });

  it('should call logout when refresh fails', () => {
    authServiceSpy.refresh.and.returnValue(
      throwError(() => new Error('refresh failed'))
    );

    httpClient.get('/api/users').subscribe({
      error: () => {
        // expected to error
      },
    });

    const req = httpMock.expectOne('/api/users');
    req.flush(null, { status: 401, statusText: 'Unauthorized' });

    expect(authServiceSpy.logout).toHaveBeenCalled();
  });

  it('should queue concurrent requests during refresh and retry all after', () => {
    let refreshCompleted = false;

    authServiceSpy.refresh.and.returnValue(
      of(undefined as void).pipe(
        tap(() => {
          refreshCompleted = true;
          authServiceSpy.getAccessToken.and.returnValue('refreshed-token');
        })
      )
    );

    // Fire two concurrent requests
    httpClient.get('/api/users').subscribe();
    httpClient.get('/api/incomes').subscribe();

    // Both get 401
    const requests = httpMock.match((r) => r.url === '/api/users' || r.url === '/api/incomes');
    expect(requests.length).toBe(2);
    requests.forEach((r) => r.flush(null, { status: 401, statusText: 'Unauthorized' }));

    // Both should be retried with the refreshed token
    expect(refreshCompleted).toBeTrue();

    const retriedRequests = httpMock.match(
      (r) => r.url === '/api/users' || r.url === '/api/incomes'
    );
    expect(retriedRequests.length).toBe(2);
    retriedRequests.forEach((r) => {
      expect(r.request.headers.get('Authorization')).toBe('Bearer refreshed-token');
      r.flush([]);
    });
  });
});
