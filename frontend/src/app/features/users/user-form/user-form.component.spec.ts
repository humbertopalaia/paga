import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { provideRouter, Router, ActivatedRoute } from '@angular/router';
import { of, throwError } from 'rxjs';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { MatSnackBar } from '@angular/material/snack-bar';
import { HttpErrorResponse } from '@angular/common/http';

import { UserFormComponent } from './user-form.component';
import { UserService } from '../user.service';
import { User } from '../user.model';

describe('UserFormComponent', () => {
  let component: UserFormComponent;
  let fixture: ComponentFixture<UserFormComponent>;
  let userServiceSpy: jasmine.SpyObj<UserService>;
  let snackBarSpy: jasmine.SpyObj<MatSnackBar>;
  let router: Router;

  function createComponent(routeParams: Record<string, string> = {}): void {
    userServiceSpy = jasmine.createSpyObj('UserService', ['getUser', 'createUser', 'updateUser']);
    snackBarSpy = jasmine.createSpyObj('MatSnackBar', ['open']);

    TestBed.configureTestingModule({
      imports: [UserFormComponent],
      providers: [
        provideAnimationsAsync(),
        provideRouter([{ path: 'users', children: [] }]),
        { provide: UserService, useValue: userServiceSpy },
        { provide: MatSnackBar, useValue: snackBarSpy },
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { params: routeParams } },
        },
      ],
    });

    fixture = TestBed.createComponent(UserFormComponent);
    component = fixture.componentInstance;
    router = TestBed.inject(Router);
    spyOn(router, 'navigate').and.returnValue(Promise.resolve(true));
  }

  const mockUser: User = {
    id: '123',
    name: 'João Silva',
    email: 'joao@test.com',
    createdAt: '2024-01-01T00:00:00Z',
  };

  describe('Create Mode', () => {
    beforeEach(() => {
      createComponent({});
      fixture.detectChanges();
    });

    it('should be in create mode when no id param', () => {
      expect(component.mode()).toBe('create');
    });

    it('should have 4 fields in create mode including passwordConfirmation', () => {
      expect(component.form.get('name')).toBeTruthy();
      expect(component.form.get('email')).toBeTruthy();
      expect(component.form.get('password')).toBeTruthy();
      expect(component.form.get('passwordConfirmation')).toBeTruthy();
    });

    it('should require name field', () => {
      const nameControl = component.form.get('name')!;
      nameControl.setValue('');
      nameControl.markAsTouched();

      expect(nameControl.hasError('required')).toBeTrue();
      expect(component.form.invalid).toBeTrue();
    });

    it('should require valid email format', () => {
      const emailControl = component.form.get('email')!;

      emailControl.setValue('');
      emailControl.markAsTouched();
      expect(emailControl.hasError('required')).toBeTrue();

      emailControl.setValue('invalid-email');
      expect(emailControl.hasError('email')).toBeTrue();

      emailControl.setValue('valid@test.com');
      expect(emailControl.valid).toBeTrue();
    });

    it('should require password with min length 6', () => {
      const passwordControl = component.form.get('password')!;

      passwordControl.setValue('');
      passwordControl.markAsTouched();
      expect(passwordControl.hasError('required')).toBeTrue();

      passwordControl.setValue('12345');
      expect(passwordControl.hasError('minlength')).toBeTrue();

      passwordControl.setValue('123456');
      expect(passwordControl.errors).toBeNull();
    });

    it('should require password confirmation', () => {
      const confirmControl = component.form.get('passwordConfirmation')!;

      confirmControl.setValue('');
      confirmControl.markAsTouched();
      expect(confirmControl.hasError('required')).toBeTrue();
    });

    it('should fail passwordMatch validation when passwords dont match', () => {
      component.form.get('password')!.setValue('password1');
      component.form.get('passwordConfirmation')!.setValue('different');

      expect(component.form.hasError('passwordMismatch')).toBeTrue();
    });

    it('should pass passwordMatch validation when passwords match', () => {
      component.form.get('password')!.setValue('password1');
      component.form.get('passwordConfirmation')!.setValue('password1');

      expect(component.form.hasError('passwordMismatch')).toBeFalse();
    });

    it('should submit POST with correct payload on create', fakeAsync(() => {
      userServiceSpy.createUser.and.returnValue(of(mockUser));

      component.form.get('name')!.setValue('João Silva');
      component.form.get('email')!.setValue('joao@test.com');
      component.form.get('password')!.setValue('senha123');
      component.form.get('passwordConfirmation')!.setValue('senha123');

      component.onSubmit();
      tick();

      expect(userServiceSpy.createUser).toHaveBeenCalledWith({
        name: 'João Silva',
        email: 'joao@test.com',
        password: 'senha123',
      });
    }));

    it('should show success snackbar and navigate after create', fakeAsync(() => {
      userServiceSpy.createUser.and.returnValue(of(mockUser));

      component.form.get('name')!.setValue('João Silva');
      component.form.get('email')!.setValue('joao@test.com');
      component.form.get('password')!.setValue('senha123');
      component.form.get('passwordConfirmation')!.setValue('senha123');

      component.onSubmit();
      tick();

      expect(snackBarSpy.open).toHaveBeenCalledWith(
        'Usuário criado com sucesso',
        'Fechar',
        jasmine.objectContaining({ duration: 3000 })
      );
      expect(router.navigate).toHaveBeenCalledWith(['/users']);
    }));
  });

  describe('Edit Mode', () => {
    beforeEach(() => {
      createComponent({ id: '123' });
      userServiceSpy.getUser.and.returnValue(of(mockUser));
      fixture.detectChanges();
    });

    it('should be in edit mode when id param present', () => {
      expect(component.mode()).toBe('edit');
    });

    it('should fetch user and populate name/email on init', fakeAsync(() => {
      tick();

      expect(userServiceSpy.getUser).toHaveBeenCalledWith('123');
      expect(component.form.get('name')!.value).toBe('João Silva');
      expect(component.form.get('email')!.value).toBe('joao@test.com');
    }));

    it('should NOT have passwordConfirmation field in edit mode', () => {
      expect(component.form.get('passwordConfirmation')).toBeNull();
    });

    it('should NOT apply passwordMatchValidator in edit mode', () => {
      component.form.get('password')!.setValue('abc');
      expect(component.form.hasError('passwordMismatch')).toBeFalse();
    });

    it('should not require password in edit mode', fakeAsync(() => {
      tick();

      const passwordControl = component.form.get('password')!;
      passwordControl.setValue('');
      passwordControl.markAsTouched();

      expect(passwordControl.hasError('required')).toBeFalse();
      expect(component.form.valid).toBeTrue();
    }));

    it('should submit PUT with password only when non-empty', fakeAsync(() => {
      tick();
      userServiceSpy.updateUser.and.returnValue(of(mockUser));

      component.form.get('name')!.setValue('Novo Nome');
      component.form.get('email')!.setValue('novo@test.com');
      component.form.get('password')!.setValue('novaSenha');

      component.onSubmit();
      tick();

      expect(userServiceSpy.updateUser).toHaveBeenCalledWith('123', {
        name: 'Novo Nome',
        email: 'novo@test.com',
        password: 'novaSenha',
      });
    }));

    it('should submit PUT without password when empty', fakeAsync(() => {
      tick();
      userServiceSpy.updateUser.and.returnValue(of(mockUser));

      component.form.get('name')!.setValue('Novo Nome');
      component.form.get('email')!.setValue('novo@test.com');
      component.form.get('password')!.setValue('');

      component.onSubmit();
      tick();

      expect(userServiceSpy.updateUser).toHaveBeenCalledWith('123', {
        name: 'Novo Nome',
        email: 'novo@test.com',
      });
    }));

    it('should show success snackbar and navigate after update', fakeAsync(() => {
      tick();
      userServiceSpy.updateUser.and.returnValue(of(mockUser));

      component.form.get('name')!.setValue('Novo Nome');
      component.form.get('email')!.setValue('novo@test.com');
      component.form.get('password')!.setValue('');

      component.onSubmit();
      tick();

      expect(snackBarSpy.open).toHaveBeenCalledWith(
        'Usuário atualizado com sucesso',
        'Fechar',
        jasmine.objectContaining({ duration: 3000 })
      );
      expect(router.navigate).toHaveBeenCalledWith(['/users']);
    }));
  });

  describe('Error Handling', () => {
    beforeEach(() => {
      createComponent({});
      fixture.detectChanges();
    });

    it('should show snackbar with API message on 409 error', fakeAsync(() => {
      const errorResponse = new HttpErrorResponse({
        status: 409,
        error: {
          type: 'conflict',
          title: 'E-mail já está em uso',
          status: 409,
        },
      });
      userServiceSpy.createUser.and.returnValue(throwError(() => errorResponse));

      component.form.get('name')!.setValue('João');
      component.form.get('email')!.setValue('joao@test.com');
      component.form.get('password')!.setValue('senha123');
      component.form.get('passwordConfirmation')!.setValue('senha123');

      component.onSubmit();
      tick();

      expect(snackBarSpy.open).toHaveBeenCalledWith(
        'E-mail já está em uso',
        'Fechar',
        jasmine.objectContaining({ duration: 5000 })
      );
    }));

    it('should show snackbar with validation errors on 400', fakeAsync(() => {
      const errorResponse = new HttpErrorResponse({
        status: 400,
        error: {
          type: 'validation',
          title: 'Validation failed',
          status: 400,
          errors: {
            email: ['O email é inválido'],
            name: ['O nome é obrigatório'],
          },
        },
      });
      userServiceSpy.createUser.and.returnValue(throwError(() => errorResponse));

      component.form.get('name')!.setValue('João');
      component.form.get('email')!.setValue('joao@test.com');
      component.form.get('password')!.setValue('senha123');
      component.form.get('passwordConfirmation')!.setValue('senha123');

      component.onSubmit();
      tick();

      expect(snackBarSpy.open).toHaveBeenCalledWith(
        'O email é inválido. O nome é obrigatório',
        'Fechar',
        jasmine.objectContaining({ duration: 5000 })
      );
    }));

    it('should show generic error snackbar on other errors', fakeAsync(() => {
      const errorResponse = new HttpErrorResponse({
        status: 500,
        error: { title: 'Internal Server Error', status: 500 },
      });
      userServiceSpy.createUser.and.returnValue(throwError(() => errorResponse));

      component.form.get('name')!.setValue('João');
      component.form.get('email')!.setValue('joao@test.com');
      component.form.get('password')!.setValue('senha123');
      component.form.get('passwordConfirmation')!.setValue('senha123');

      component.onSubmit();
      tick();

      expect(snackBarSpy.open).toHaveBeenCalledWith(
        'Erro inesperado. Tente novamente.',
        'Fechar',
        jasmine.objectContaining({ duration: 5000 })
      );
    }));

    it('should reset loading state after error', fakeAsync(() => {
      const errorResponse = new HttpErrorResponse({
        status: 500,
        error: { title: 'Internal Server Error', status: 500 },
      });
      userServiceSpy.createUser.and.returnValue(throwError(() => errorResponse));

      component.form.get('name')!.setValue('João');
      component.form.get('email')!.setValue('joao@test.com');
      component.form.get('password')!.setValue('senha123');
      component.form.get('passwordConfirmation')!.setValue('senha123');

      component.onSubmit();
      tick();

      expect(component.isLoading()).toBeFalse();
    }));
  });
});
