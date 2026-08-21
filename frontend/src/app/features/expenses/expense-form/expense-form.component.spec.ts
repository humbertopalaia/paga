import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { provideRouter, Router, ActivatedRoute } from '@angular/router';
import { of, throwError, Subject } from 'rxjs';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { provideNativeDateAdapter } from '@angular/material/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { HttpErrorResponse } from '@angular/common/http';

import { ExpenseFormComponent } from './expense-form.component';
import { ExpenseService } from '../expense.service';
import { ExpenseTypeService } from '../../expense-types/expense-type.service';
import { Expense } from '../expense.model';

describe('ExpenseFormComponent', () => {
  let component: ExpenseFormComponent;
  let fixture: ComponentFixture<ExpenseFormComponent>;
  let expenseServiceSpy: jasmine.SpyObj<ExpenseService>;
  let expenseTypeServiceSpy: jasmine.SpyObj<ExpenseTypeService>;
  let snackBarSpy: jasmine.SpyObj<MatSnackBar>;
  let router: Router;

  const mockExpense: Expense = {
    id: 1,
    dueDate: '2024-06-15',
    description: 'Aluguel',
    expenseTypeId: 2,
    expenseTypeName: 'Moradia',
    value: 2500,
    isRecurring: true,
    frequency: 'monthly',
  };

  const mockExpenseTypes = {
    items: [
      { id: 1, name: 'Transporte' },
      { id: 2, name: 'Moradia' },
      { id: 3, name: 'Alimentação' },
    ],
    pageNumber: 1,
    pageSize: 100,
    totalCount: 3,
    totalPages: 1,
  };

  function createComponent(routeParams: Record<string, string> = {}): void {
    expenseServiceSpy = jasmine.createSpyObj('ExpenseService', [
      'getExpense',
      'createExpense',
      'updateExpense',
    ]);
    expenseTypeServiceSpy = jasmine.createSpyObj('ExpenseTypeService', ['getExpenseTypes']);
    expenseTypeServiceSpy.getExpenseTypes.and.returnValue(of(mockExpenseTypes));
    snackBarSpy = jasmine.createSpyObj('MatSnackBar', ['open']);

    TestBed.configureTestingModule({
      imports: [ExpenseFormComponent],
      providers: [
        provideAnimationsAsync(),
        provideNativeDateAdapter(),
        provideRouter([{ path: 'expenses', children: [] }]),
        { provide: ExpenseService, useValue: expenseServiceSpy },
        { provide: ExpenseTypeService, useValue: expenseTypeServiceSpy },
        { provide: MatSnackBar, useValue: snackBarSpy },
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { params: routeParams } },
        },
      ],
    });

    fixture = TestBed.createComponent(ExpenseFormComponent);
    component = fixture.componentInstance;
    router = TestBed.inject(Router);
    spyOn(router, 'navigate').and.returnValue(Promise.resolve(true));
  }

  describe('Create Mode', () => {
    beforeEach(() => {
      createComponent({});
      fixture.detectChanges();
    });

    it('should show "Nova Despesa" title in create mode', () => {
      const h1 = fixture.nativeElement.querySelector('h1') as HTMLElement;
      expect(h1.textContent?.trim()).toBe('Nova Despesa');
    });

    it('should show empty form in create mode', () => {
      expect(component.form.get('dueDate')!.value).toBeNull();
      expect(component.form.get('description')!.value).toBe('');
      expect(component.form.get('expenseTypeId')!.value).toBeNull();
      expect(component.form.get('value')!.value).toBeNull();
      expect(component.form.get('recurrence')!.value).toEqual({ isRecurring: false, frequency: null });
    });

    it('should be in create mode when no id param', () => {
      expect(component.mode()).toBe('create');
    });

    it('should validate dueDate required', () => {
      const dueDateControl = component.form.get('dueDate')!;
      dueDateControl.setValue(null);
      dueDateControl.markAsTouched();

      expect(dueDateControl.hasError('required')).toBeTrue();
      expect(component.form.invalid).toBeTrue();
    });

    it('should validate description required', () => {
      const descControl = component.form.get('description')!;
      descControl.setValue('');
      descControl.markAsTouched();

      expect(descControl.hasError('required')).toBeTrue();
      expect(component.form.invalid).toBeTrue();
    });

    it('should validate expenseTypeId required', () => {
      const typeControl = component.form.get('expenseTypeId')!;
      typeControl.setValue(null);
      typeControl.markAsTouched();

      expect(typeControl.hasError('required')).toBeTrue();
      expect(component.form.invalid).toBeTrue();
    });

    it('should validate value required', () => {
      const valueControl = component.form.get('value')!;
      valueControl.setValue(null);
      valueControl.markAsTouched();

      expect(valueControl.hasError('required')).toBeTrue();
    });

    it('should validate value must be greater than zero', () => {
      const valueControl = component.form.get('value')!;
      valueControl.setValue(0);
      valueControl.markAsTouched();

      expect(valueControl.hasError('positiveValue')).toBeTrue();
      expect(component.form.invalid).toBeTrue();
    });

    it('should accept positive value', () => {
      const valueControl = component.form.get('value')!;
      valueControl.setValue(100.5);

      expect(valueControl.hasError('positiveValue')).toBeFalse();
      expect(valueControl.hasError('required')).toBeFalse();
    });

    it('should call createExpense on submit in create mode', fakeAsync(() => {
      expenseServiceSpy.createExpense.and.returnValue(of(mockExpense));

      component.form.patchValue({
        dueDate: new Date(2024, 5, 15),
        description: 'Aluguel',
        expenseTypeId: 2,
        value: 2500,
        recurrence: { isRecurring: false, frequency: null },
      });
      component.onSubmit();
      tick();

      expect(expenseServiceSpy.createExpense).toHaveBeenCalledWith({
        dueDate: '2024-06-15',
        description: 'Aluguel',
        expenseTypeId: 2,
        value: 2500,
        isRecurring: false,
        frequency: null,
      });
    }));

    it('should disable save button while form invalid', () => {
      fixture.detectChanges();
      const submitButton = fixture.nativeElement.querySelector('button[type="submit"]') as HTMLButtonElement;
      expect(submitButton.disabled).toBeTrue();
    });

    it('should disable save button during submission', fakeAsync(() => {
      const subject = new Subject<Expense>();
      expenseServiceSpy.createExpense.and.returnValue(subject.asObservable());

      component.form.patchValue({
        dueDate: new Date(2024, 5, 15),
        description: 'Aluguel',
        expenseTypeId: 2,
        value: 2500,
        recurrence: { isRecurring: false, frequency: null },
      });
      component.onSubmit();

      expect(component.isLoading()).toBeTrue();

      fixture.detectChanges();
      const submitButton = fixture.nativeElement.querySelector('button[type="submit"]') as HTMLButtonElement;
      expect(submitButton.disabled).toBeTrue();

      subject.next(mockExpense);
      subject.complete();
      tick();
    }));

    it('should show success snackbar and navigate on create success', fakeAsync(() => {
      expenseServiceSpy.createExpense.and.returnValue(of(mockExpense));

      component.form.patchValue({
        dueDate: new Date(2024, 5, 15),
        description: 'Aluguel',
        expenseTypeId: 2,
        value: 2500,
        recurrence: { isRecurring: false, frequency: null },
      });
      component.onSubmit();
      tick();

      expect(snackBarSpy.open).toHaveBeenCalledWith(
        'Despesa criada com sucesso',
        'Fechar',
        jasmine.objectContaining({ duration: 3000 })
      );
      expect(router.navigate).toHaveBeenCalledWith(['/expenses']);
    }));

    it('should not make API call when form is invalid', () => {
      component.form.get('description')!.setValue('');
      component.onSubmit();

      expect(expenseServiceSpy.createExpense).not.toHaveBeenCalled();
    });

    it('should not make API call when cancel is clicked', () => {
      const cancelLink = fixture.nativeElement.querySelector('a[routerLink="/expenses"]') as HTMLElement;
      expect(cancelLink).toBeTruthy();
      expect(expenseServiceSpy.createExpense).not.toHaveBeenCalled();
      expect(expenseServiceSpy.updateExpense).not.toHaveBeenCalled();
    });
  });

  describe('Edit Mode', () => {
    it('should be in edit mode when id param present', () => {
      createComponent({ id: '1' });
      expenseServiceSpy.getExpense.and.returnValue(of(mockExpense));
      fixture.detectChanges();

      expect(component.mode()).toBe('edit');
    });

    it('should show "Editar Despesa" title in edit mode', fakeAsync(() => {
      createComponent({ id: '1' });
      expenseServiceSpy.getExpense.and.returnValue(of(mockExpense));
      fixture.detectChanges();
      tick();
      fixture.detectChanges();

      const h1 = fixture.nativeElement.querySelector('h1') as HTMLElement;
      expect(h1.textContent?.trim()).toBe('Editar Despesa');
    }));

    it('should load data and pre-fill form in edit mode', fakeAsync(() => {
      createComponent({ id: '1' });
      expenseServiceSpy.getExpense.and.returnValue(of(mockExpense));
      fixture.detectChanges();
      tick();

      expect(expenseServiceSpy.getExpense).toHaveBeenCalledWith(1);
      expect(component.form.get('description')!.value).toBe('Aluguel');
      expect(component.form.get('value')!.value).toBe(2500);
      expect(component.form.get('recurrence')!.value).toEqual({ isRecurring: true, frequency: 'monthly' });
    }));

    it('should pre-fill expenseTypeId from loaded expense', fakeAsync(() => {
      createComponent({ id: '1' });
      expenseServiceSpy.getExpense.and.returnValue(of(mockExpense));
      fixture.detectChanges();
      tick();

      expect(component.form.get('expenseTypeId')!.value).toBe(2);
    }));

    it('should navigate back with error snackbar on 404 in edit mode', fakeAsync(() => {
      createComponent({ id: '99' });
      const errorResponse = new HttpErrorResponse({
        status: 404,
        error: { title: 'Not Found', status: 404 },
      });
      expenseServiceSpy.getExpense.and.returnValue(throwError(() => errorResponse));
      fixture.detectChanges();
      tick();

      expect(snackBarSpy.open).toHaveBeenCalledWith(
        'Despesa não encontrada.',
        'Fechar',
        jasmine.objectContaining({ duration: 5000 })
      );
      expect(router.navigate).toHaveBeenCalledWith(['/expenses']);
    }));

    it('should call updateExpense on submit in edit mode', fakeAsync(() => {
      createComponent({ id: '1' });
      expenseServiceSpy.getExpense.and.returnValue(of(mockExpense));
      fixture.detectChanges();
      tick();

      expenseServiceSpy.updateExpense.and.returnValue(of({ ...mockExpense, description: 'Aluguel Atualizado' }));

      component.form.patchValue({ description: 'Aluguel Atualizado' });
      component.onSubmit();
      tick();

      expect(expenseServiceSpy.updateExpense).toHaveBeenCalledWith(1, jasmine.objectContaining({
        description: 'Aluguel Atualizado',
        expenseTypeId: 2,
        dueDate: '2024-06-15',
      }));
    }));

    it('should show success snackbar and navigate on update success', fakeAsync(() => {
      createComponent({ id: '1' });
      expenseServiceSpy.getExpense.and.returnValue(of(mockExpense));
      fixture.detectChanges();
      tick();

      expenseServiceSpy.updateExpense.and.returnValue(of(mockExpense));

      component.onSubmit();
      tick();

      expect(snackBarSpy.open).toHaveBeenCalledWith(
        'Despesa atualizada com sucesso',
        'Fechar',
        jasmine.objectContaining({ duration: 3000 })
      );
      expect(router.navigate).toHaveBeenCalledWith(['/expenses']);
    }));
  });

  describe('ExpenseType select', () => {
    it('should load expense types on init', () => {
      createComponent({});
      fixture.detectChanges();

      expect(expenseTypeServiceSpy.getExpenseTypes).toHaveBeenCalledWith({
        pageNumber: 1,
        pageSize: 100,
        name: '',
      });
      expect(component.expenseTypes()).toEqual(mockExpenseTypes.items);
    });

    it('should set typesError to true when expense type loading fails', () => {
      createComponent({});
      expenseTypeServiceSpy.getExpenseTypes.and.returnValue(
        throwError(() => new HttpErrorResponse({ status: 500 }))
      );
      fixture.detectChanges();

      expect(component.typesError()).toBeTrue();
    });

    it('should show error state when typesError is true', () => {
      createComponent({});
      expenseTypeServiceSpy.getExpenseTypes.and.returnValue(
        throwError(() => new HttpErrorResponse({ status: 500 }))
      );
      fixture.detectChanges();

      const errorState = fixture.nativeElement.querySelector('.error-state') as HTMLElement;
      expect(errorState).toBeTruthy();
      expect(errorState.textContent).toContain('Não foi possível carregar os tipos de despesa');
    });

    it('should not show error state when types load successfully', () => {
      createComponent({});
      fixture.detectChanges();

      const errorState = fixture.nativeElement.querySelector('.error-state');
      expect(errorState).toBeNull();
      expect(component.typesError()).toBeFalse();
    });
  });

  describe('RecurrenceSelector Integration', () => {
    beforeEach(() => {
      createComponent({});
      fixture.detectChanges();
    });

    it('should have recurrence control with default non-recurring value', () => {
      expect(component.form.get('recurrence')!.value).toEqual({ isRecurring: false, frequency: null });
    });

    it('should include recurrence in payload when recurring is enabled', fakeAsync(() => {
      expenseServiceSpy.createExpense.and.returnValue(of(mockExpense));

      component.form.patchValue({
        dueDate: new Date(2024, 5, 15),
        description: 'Aluguel',
        expenseTypeId: 2,
        value: 2500,
        recurrence: { isRecurring: true, frequency: 'monthly' },
      });
      component.onSubmit();
      tick();

      expect(expenseServiceSpy.createExpense).toHaveBeenCalledWith(jasmine.objectContaining({
        isRecurring: true,
        frequency: 'monthly',
      }));
    }));

    it('should set frequency to null when recurring is disabled', fakeAsync(() => {
      expenseServiceSpy.createExpense.and.returnValue(of(mockExpense));

      component.form.patchValue({
        dueDate: new Date(2024, 5, 15),
        description: 'Internet',
        expenseTypeId: 1,
        value: 100,
        recurrence: { isRecurring: false, frequency: null },
      });
      component.onSubmit();
      tick();

      expect(expenseServiceSpy.createExpense).toHaveBeenCalledWith(jasmine.objectContaining({
        isRecurring: false,
        frequency: null,
      }));
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
            value: ['O valor deve ser maior que zero.'],
            description: ['A descrição é obrigatória.'],
          },
        },
      });
      expenseServiceSpy.createExpense.and.returnValue(throwError(() => errorResponse));

      component.form.patchValue({
        dueDate: new Date(2024, 5, 15),
        description: 'Teste',
        expenseTypeId: 1,
        value: 100,
        recurrence: { isRecurring: false, frequency: null },
      });
      component.onSubmit();
      tick();

      expect(snackBarSpy.open).toHaveBeenCalledWith(
        'O valor deve ser maior que zero.. A descrição é obrigatória.',
        'Fechar',
        jasmine.objectContaining({ duration: 5000 })
      );
    }));

    it('should show generic error on 500', fakeAsync(() => {
      const errorResponse = new HttpErrorResponse({
        status: 500,
        error: { title: 'Internal Server Error', status: 500 },
      });
      expenseServiceSpy.createExpense.and.returnValue(throwError(() => errorResponse));

      component.form.patchValue({
        dueDate: new Date(2024, 5, 15),
        description: 'Teste',
        expenseTypeId: 1,
        value: 100,
        recurrence: { isRecurring: false, frequency: null },
      });
      component.onSubmit();
      tick();

      expect(snackBarSpy.open).toHaveBeenCalledWith(
        'Erro inesperado. Tente novamente.',
        'Fechar',
        jasmine.objectContaining({ duration: 5000 })
      );
    }));

    it('should show title from problem details on 409', fakeAsync(() => {
      const errorResponse = new HttpErrorResponse({
        status: 409,
        error: {
          type: 'conflict',
          title: 'Conflito de dados',
          status: 409,
        },
      });
      expenseServiceSpy.createExpense.and.returnValue(throwError(() => errorResponse));

      component.form.patchValue({
        dueDate: new Date(2024, 5, 15),
        description: 'Teste',
        expenseTypeId: 1,
        value: 100,
        recurrence: { isRecurring: false, frequency: null },
      });
      component.onSubmit();
      tick();

      expect(snackBarSpy.open).toHaveBeenCalledWith(
        'Conflito de dados',
        'Fechar',
        jasmine.objectContaining({ duration: 5000 })
      );
    }));

    it('should reset loading state after error', fakeAsync(() => {
      const errorResponse = new HttpErrorResponse({
        status: 500,
        error: { title: 'Internal Server Error', status: 500 },
      });
      expenseServiceSpy.createExpense.and.returnValue(throwError(() => errorResponse));

      component.form.patchValue({
        dueDate: new Date(2024, 5, 15),
        description: 'Teste',
        expenseTypeId: 1,
        value: 100,
        recurrence: { isRecurring: false, frequency: null },
      });
      component.onSubmit();
      tick();

      expect(component.isLoading()).toBeFalse();
    }));
  });
});
