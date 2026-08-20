import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { provideRouter, Router, ActivatedRoute } from '@angular/router';
import { of, throwError, Subject } from 'rxjs';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { HttpErrorResponse } from '@angular/common/http';

import { LoginComponent } from './login.component';
import { AuthService } from '../../../core/auth/auth.service';

describe('LoginComponent', () => {
  let component: LoginComponent;
  let fixture: ComponentFixture<LoginComponent>;
  let authServiceSpy: jasmine.SpyObj<AuthService>;
  let router: Router;
  let activatedRoute: { snapshot: { queryParams: Record<string, string> } };

  beforeEach(async () => {
    authServiceSpy = jasmine.createSpyObj('AuthService', ['login']);
    activatedRoute = { snapshot: { queryParams: {} } };

    await TestBed.configureTestingModule({
      imports: [LoginComponent],
      providers: [
        provideAnimationsAsync(),
        provideRouter([{ path: 'dashboard', children: [] }, { path: 'users', children: [] }]),
        { provide: AuthService, useValue: authServiceSpy },
        { provide: ActivatedRoute, useValue: activatedRoute },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(LoginComponent);
    component = fixture.componentInstance;
    router = TestBed.inject(Router);
    spyOn(router, 'navigateByUrl').and.returnValue(Promise.resolve(true));
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  describe('Form Validation', () => {
    it('should disable submit button when form is invalid', () => {
      const button = fixture.nativeElement.querySelector('button[type="submit"]') as HTMLButtonElement;
      expect(button.disabled).toBeTrue();
    });

    it('should show email validation error for invalid format', () => {
      const emailControl = component.form.controls.email;
      emailControl.setValue('invalid-email');
      emailControl.markAsTouched();
      fixture.detectChanges();

      const error = fixture.nativeElement.querySelector('mat-error');
      expect(error).toBeTruthy();
      expect(error.textContent).toContain('email válido');
    });

    it('should show required error for empty email', () => {
      const emailControl = component.form.controls.email;
      emailControl.setValue('');
      emailControl.markAsTouched();
      fixture.detectChanges();

      const error = fixture.nativeElement.querySelector('mat-error');
      expect(error).toBeTruthy();
      expect(error.textContent).toContain('obrigatório');
    });

    it('should show required error for empty password', () => {
      const passwordControl = component.form.controls.password;
      passwordControl.setValue('');
      passwordControl.markAsTouched();
      fixture.detectChanges();

      const error = fixture.nativeElement.querySelector('mat-error');
      expect(error).toBeTruthy();
      expect(error.textContent).toContain('obrigatória');
    });

    it('should enable submit button when form is valid', () => {
      component.form.controls.email.setValue('user@test.com');
      component.form.controls.password.setValue('password123');
      fixture.detectChanges();

      const button = fixture.nativeElement.querySelector('button[type="submit"]') as HTMLButtonElement;
      expect(button.disabled).toBeFalse();
    });
  });

  describe('Successful Login Navigation', () => {
    it('should navigate to /dashboard on successful login', fakeAsync(() => {
      authServiceSpy.login.and.returnValue(of(undefined));

      component.form.controls.email.setValue('user@test.com');
      component.form.controls.password.setValue('password123');
      component.onSubmit();
      tick();

      expect(authServiceSpy.login).toHaveBeenCalledWith('user@test.com', 'password123');
      expect(router.navigateByUrl).toHaveBeenCalledWith('/dashboard');
    }));

    it('should navigate to returnUrl when present', fakeAsync(() => {
      activatedRoute.snapshot.queryParams = { returnUrl: '/users' };
      authServiceSpy.login.and.returnValue(of(undefined));

      component.form.controls.email.setValue('user@test.com');
      component.form.controls.password.setValue('password123');
      component.onSubmit();
      tick();

      expect(router.navigateByUrl).toHaveBeenCalledWith('/users');
    }));
  });

  describe('Error Handling', () => {
    it('should display inline error banner with API message on 401', fakeAsync(() => {
      const errorResponse = new HttpErrorResponse({
        status: 401,
        error: { type: 'auth-error', title: 'Credenciais inválidas', status: 401 },
      });
      authServiceSpy.login.and.returnValue(throwError(() => errorResponse));

      component.form.controls.email.setValue('user@test.com');
      component.form.controls.password.setValue('wrongpass');
      component.onSubmit();
      tick();
      fixture.detectChanges();

      const banner = fixture.nativeElement.querySelector('.error-banner');
      expect(banner).toBeTruthy();
      expect(banner.textContent).toContain('Credenciais inválidas');
    }));

    it('should display default 401 message when API title is missing', fakeAsync(() => {
      const errorResponse = new HttpErrorResponse({
        status: 401,
        error: {},
      });
      authServiceSpy.login.and.returnValue(throwError(() => errorResponse));

      component.form.controls.email.setValue('user@test.com');
      component.form.controls.password.setValue('wrongpass');
      component.onSubmit();
      tick();
      fixture.detectChanges();

      const banner = fixture.nativeElement.querySelector('.error-banner');
      expect(banner).toBeTruthy();
      expect(banner.textContent).toContain('Credenciais inválidas');
    }));

    it('should display generic error on non-401 errors', fakeAsync(() => {
      const errorResponse = new HttpErrorResponse({
        status: 500,
        error: { title: 'Internal Server Error', status: 500 },
      });
      authServiceSpy.login.and.returnValue(throwError(() => errorResponse));

      component.form.controls.email.setValue('user@test.com');
      component.form.controls.password.setValue('password123');
      component.onSubmit();
      tick();
      fixture.detectChanges();

      const banner = fixture.nativeElement.querySelector('.error-banner');
      expect(banner).toBeTruthy();
      expect(banner.textContent).toContain('Erro ao realizar login. Tente novamente.');
    }));

    it('should not display error banner initially', () => {
      const banner = fixture.nativeElement.querySelector('.error-banner');
      expect(banner).toBeNull();
    });
  });

  describe('Loading State', () => {
    it('should show "Entrando..." text and spinner during loading', fakeAsync(() => {
      const loginSubject = new Subject<void>();
      authServiceSpy.login.and.returnValue(loginSubject.asObservable());

      component.form.controls.email.setValue('user@test.com');
      component.form.controls.password.setValue('password123');
      component.onSubmit();
      fixture.detectChanges();

      const button = fixture.nativeElement.querySelector('button[type="submit"]') as HTMLButtonElement;
      expect(button.textContent).toContain('Entrando...');
      expect(button.disabled).toBeTrue();

      const spinner = fixture.nativeElement.querySelector('mat-spinner');
      expect(spinner).toBeTruthy();

      loginSubject.next(undefined);
      loginSubject.complete();
      tick();
    }));

    it('should apply loading class to button during loading', fakeAsync(() => {
      const loginSubject = new Subject<void>();
      authServiceSpy.login.and.returnValue(loginSubject.asObservable());

      component.form.controls.email.setValue('user@test.com');
      component.form.controls.password.setValue('password123');
      component.onSubmit();
      fixture.detectChanges();

      const button = fixture.nativeElement.querySelector('button[type="submit"]') as HTMLButtonElement;
      expect(button.classList.contains('loading')).toBeTrue();

      loginSubject.next(undefined);
      loginSubject.complete();
      tick();
    }));

    it('should restore button state after error', fakeAsync(() => {
      const errorResponse = new HttpErrorResponse({
        status: 401,
        error: { title: 'Credenciais inválidas', status: 401 },
      });
      authServiceSpy.login.and.returnValue(throwError(() => errorResponse));

      component.form.controls.email.setValue('user@test.com');
      component.form.controls.password.setValue('wrongpass');
      component.onSubmit();
      tick();
      fixture.detectChanges();

      expect(component.isLoading()).toBeFalse();

      const button = fixture.nativeElement.querySelector('button[type="submit"]') as HTMLButtonElement;
      expect(button.textContent).toContain('Entrar');
      expect(button.disabled).toBeFalse();
      expect(button.classList.contains('loading')).toBeFalse();
    }));

    it('should not submit when already loading', fakeAsync(() => {
      const loginSubject = new Subject<void>();
      authServiceSpy.login.and.returnValue(loginSubject.asObservable());

      component.form.controls.email.setValue('user@test.com');
      component.form.controls.password.setValue('password123');
      component.onSubmit();
      component.onSubmit();

      expect(authServiceSpy.login).toHaveBeenCalledTimes(1);

      loginSubject.next(undefined);
      loginSubject.complete();
      tick();
    }));
  });
});
