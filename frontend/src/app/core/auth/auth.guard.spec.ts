import { TestBed } from '@angular/core/testing';
import { Router, ActivatedRouteSnapshot, RouterStateSnapshot, UrlTree } from '@angular/router';
import { provideRouter } from '@angular/router';
import { authGuard } from './auth.guard';
import { AuthService } from './auth.service';

describe('authGuard', () => {
  let mockAuthService: { isAuthenticated: jasmine.Spy };
  let router: Router;

  beforeEach(() => {
    mockAuthService = {
      isAuthenticated: jasmine.createSpy('isAuthenticated').and.returnValue(false),
    };

    TestBed.configureTestingModule({
      providers: [
        provideRouter([{ path: 'login', children: [] }]),
        { provide: AuthService, useValue: mockAuthService },
      ],
    });

    router = TestBed.inject(Router);
  });

  function runGuard(url: string): boolean | UrlTree {
    const route = {} as ActivatedRouteSnapshot;
    const state = { url } as RouterStateSnapshot;

    return TestBed.runInInjectionContext(() => authGuard(route, state)) as boolean | UrlTree;
  }

  it('should allow access when user is authenticated', () => {
    mockAuthService.isAuthenticated.and.returnValue(true);

    const result = runGuard('/users');

    expect(result).toBeTrue();
  });

  it('should redirect to /login with returnUrl when not authenticated', () => {
    mockAuthService.isAuthenticated.and.returnValue(false);

    const result = runGuard('/users');

    expect(result).toBeInstanceOf(UrlTree);
    const urlTree = result as UrlTree;
    expect(urlTree.toString()).toContain('/login');
    expect(urlTree.queryParams['returnUrl']).toBe('/users');
  });

  it('should preserve the full attempted URL in returnUrl param', () => {
    mockAuthService.isAuthenticated.and.returnValue(false);

    const result = runGuard('/expense-types');

    const urlTree = result as UrlTree;
    expect(urlTree.queryParams['returnUrl']).toBe('/expense-types');
  });
});
