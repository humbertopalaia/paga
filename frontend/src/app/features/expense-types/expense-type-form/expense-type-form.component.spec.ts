import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { provideRouter, Router, ActivatedRoute } from '@angular/router';
import { of, throwError, Subject } from 'rxjs';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { MatSnackBar } from '@angular/material/snack-bar';
import { HttpErrorResponse } from '@angular/common/http';

import { ExpenseTypeFormComponent } from './expense-type-form.component';
import { ExpenseTypeService } from '../expense-type.service';
import { ExpenseType } from '../expense-type.model';

describe('ExpenseTypeFormComponent', () => {
  let component: ExpenseTypeFormComponent;
  let fixture: ComponentFixture<ExpenseTypeFormComponent>;
  let expenseTypeServiceSpy: jasmine.SpyObj<ExpenseTypeService>;
  let snackBarSpy: jasmine.SpyObj<MatSnackBar>;
  let router: Router;

  function createComponent(routeParams: Record<string, string> = {}): void {
    expenseTypeServiceSpy = jasmine.createSpyObj('ExpenseTypeService', [
      'getExpenseType',
      'createExpenseType',
      'updateExpenseType',
    ]);
    snackBarSpy = jasmine.createSpyObj('MatSnackBar', ['open']);

    TestBed.configureTestingModule({
      imports: [ExpenseTypeFormComponent],
      providers: [
        provideAnimationsAsync(),
        provideRouter([{ path: 'expense-types', children: [] }]),
        { provide: ExpenseTypeService, useValue: expenseTypeServiceSpy },
        { provide: MatSnackBar, useValue: snackBarSpy },
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { params: routeParams } },
        },
      ],
    });

    fixture = TestBed.createComponent(ExpenseTypeFormComponent);
    component = fixture.componentInstance;
    router = TestBed.inject(Router);
    spyOn(router, 'navigate').and.returnValue(Promise.resolve(true));
  }

  const mockExpenseType: ExpenseType = {
    id: 5,
    name: 'Alimentação',
  };

  describe('Create Mode', () => {
    beforeEach(() => {
      createComponent({});
      fixture.detectChanges();
    });

    it('should show "Novo Tipo de Despesa" title in create mode', () => {
      const h1 = fixture.nativeElement.querySelector('h1') as HTMLElement;
      expect(h1.textContent?.trim()).toBe('Novo Tipo de Despesa');
    });

    it('should show empty form in create mode', () => {
      expect(component.form.get('name')!.value).toBe('');
    });

    it('should be in create mode when no id param', () => {
      expect(component.mode()).toBe('create');
    });

    it('should validate name required', () => {
      const nameControl = component.form.get('name')!;
      nameControl.setValue('');
      nameControl.markAsTouched();

      expect(nameControl.hasError('required')).toBeTrue();
      expect(component.form.invalid).toBeTrue();
    });

    it('should call createExpenseType on submit in create mode', fakeAsync(() => {
      expenseTypeServiceSpy.createExpenseType.and.returnValue(of(mockExpenseType));

      component.form.get('name')!.setValue('Transporte');
      component.onSubmit();
      tick();

      expect(expenseTypeServiceSpy.createExpenseType).toHaveBeenCalledWith({ name: 'Transporte' });
    }));

    it('should disable save button during submission', fakeAsync(() => {
      const subject = new Subject<ExpenseType>();
      expenseTypeServiceSpy.createExpenseType.and.returnValue(subject.asObservable());

      component.form.get('name')!.setValue('Transporte');
      component.onSubmit();

      expect(component.isLoading()).toBeTrue();

      fixture.detectChanges();
      const submitButton = fixture.nativeElement.querySelector('button[type="submit"]') as HTMLButtonElement;
      expect(submitButton.disabled).toBeTrue();

      subject.next(mockExpenseType);
      subject.complete();
      tick();
    }));

    it('should show success snackbar and navigate on create success', fakeAsync(() => {
      expenseTypeServiceSpy.createExpenseType.and.returnValue(of(mockExpenseType));

      component.form.get('name')!.setValue('Transporte');
      component.onSubmit();
      tick();

      expect(snackBarSpy.open).toHaveBeenCalledWith(
        'Tipo de despesa criado com sucesso',
        'Fechar',
        jasmine.objectContaining({ duration: 3000 })
      );
      expect(router.navigate).toHaveBeenCalledWith(['/expense-types']);
    }));

    it('should show API error message on 409', fakeAsync(() => {
      const errorResponse = new HttpErrorResponse({
        status: 409,
        error: {
          type: 'conflict',
          title: 'Tipo de despesa já existe',
          status: 409,
        },
      });
      expenseTypeServiceSpy.createExpenseType.and.returnValue(throwError(() => errorResponse));

      component.form.get('name')!.setValue('Alimentação');
      component.onSubmit();
      tick();

      expect(snackBarSpy.open).toHaveBeenCalledWith(
        'Tipo de despesa já existe',
        'Fechar',
        jasmine.objectContaining({ duration: 5000 })
      );
    }));

    it('should not make API call when form is invalid', () => {
      component.form.get('name')!.setValue('');
      component.onSubmit();

      expect(expenseTypeServiceSpy.createExpenseType).not.toHaveBeenCalled();
    });

    it('should not make API call when cancel is clicked', () => {
      const cancelLink = fixture.nativeElement.querySelector('a[routerLink="/expense-types"]') as HTMLElement;
      expect(cancelLink).toBeTruthy();
      expect(expenseTypeServiceSpy.createExpenseType).not.toHaveBeenCalled();
      expect(expenseTypeServiceSpy.updateExpenseType).not.toHaveBeenCalled();
    });
  });

  describe('Edit Mode', () => {
    it('should be in edit mode when id param present', () => {
      createComponent({ id: '5' });
      expenseTypeServiceSpy.getExpenseType.and.returnValue(of(mockExpenseType));
      fixture.detectChanges();

      expect(component.mode()).toBe('edit');
    });

    it('should load data and show "Editar Tipo de Despesa" title', fakeAsync(() => {
      createComponent({ id: '5' });
      expenseTypeServiceSpy.getExpenseType.and.returnValue(of(mockExpenseType));
      fixture.detectChanges();
      tick();
      fixture.detectChanges();

      const h1 = fixture.nativeElement.querySelector('h1') as HTMLElement;
      expect(h1.textContent?.trim()).toBe('Editar Tipo de Despesa');
    }));

    it('should load data and pre-fill form in edit mode', fakeAsync(() => {
      createComponent({ id: '5' });
      expenseTypeServiceSpy.getExpenseType.and.returnValue(of(mockExpenseType));
      fixture.detectChanges();
      tick();

      expect(expenseTypeServiceSpy.getExpenseType).toHaveBeenCalledWith(5);
      expect(component.form.get('name')!.value).toBe('Alimentação');
    }));

    it('should navigate back with error snackbar on 404 in edit mode', fakeAsync(() => {
      createComponent({ id: '99' });
      const errorResponse = new HttpErrorResponse({
        status: 404,
        error: { title: 'Not Found', status: 404 },
      });
      expenseTypeServiceSpy.getExpenseType.and.returnValue(throwError(() => errorResponse));
      fixture.detectChanges();
      tick();

      expect(snackBarSpy.open).toHaveBeenCalledWith(
        'Tipo de despesa não encontrado.',
        'Fechar',
        jasmine.objectContaining({ duration: 5000 })
      );
      expect(router.navigate).toHaveBeenCalledWith(['/expense-types']);
    }));

    it('should call updateExpenseType on submit in edit mode', fakeAsync(() => {
      createComponent({ id: '5' });
      expenseTypeServiceSpy.getExpenseType.and.returnValue(of(mockExpenseType));
      fixture.detectChanges();
      tick();

      expenseTypeServiceSpy.updateExpenseType.and.returnValue(of(mockExpenseType));

      component.form.get('name')!.setValue('Transporte');
      component.onSubmit();
      tick();

      expect(expenseTypeServiceSpy.updateExpenseType).toHaveBeenCalledWith(5, { name: 'Transporte' });
    }));

    it('should show success snackbar and navigate on update success', fakeAsync(() => {
      createComponent({ id: '5' });
      expenseTypeServiceSpy.getExpenseType.and.returnValue(of(mockExpenseType));
      fixture.detectChanges();
      tick();

      expenseTypeServiceSpy.updateExpenseType.and.returnValue(of(mockExpenseType));

      component.form.get('name')!.setValue('Transporte');
      component.onSubmit();
      tick();

      expect(snackBarSpy.open).toHaveBeenCalledWith(
        'Tipo de despesa atualizado com sucesso',
        'Fechar',
        jasmine.objectContaining({ duration: 3000 })
      );
      expect(router.navigate).toHaveBeenCalledWith(['/expense-types']);
    }));
  });

  describe('Error Handling', () => {
    beforeEach(() => {
      createComponent({});
      fixture.detectChanges();
    });

    it('should show validation errors from 400 response', fakeAsync(() => {
      const errorResponse = new HttpErrorResponse({
        status: 400,
        error: {
          type: 'validation',
          title: 'Validation failed',
          status: 400,
          errors: {
            name: ['O nome é obrigatório', 'O nome deve ter no máximo 100 caracteres'],
          },
        },
      });
      expenseTypeServiceSpy.createExpenseType.and.returnValue(throwError(() => errorResponse));

      component.form.get('name')!.setValue('x');
      component.onSubmit();
      tick();

      expect(snackBarSpy.open).toHaveBeenCalledWith(
        'O nome é obrigatório. O nome deve ter no máximo 100 caracteres',
        'Fechar',
        jasmine.objectContaining({ duration: 5000 })
      );
    }));

    it('should show generic error on 500', fakeAsync(() => {
      const errorResponse = new HttpErrorResponse({
        status: 500,
        error: { title: 'Internal Server Error', status: 500 },
      });
      expenseTypeServiceSpy.createExpenseType.and.returnValue(throwError(() => errorResponse));

      component.form.get('name')!.setValue('Teste');
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
      expenseTypeServiceSpy.createExpenseType.and.returnValue(throwError(() => errorResponse));

      component.form.get('name')!.setValue('Teste');
      component.onSubmit();
      tick();

      expect(component.isLoading()).toBeFalse();
    }));
  });
});
