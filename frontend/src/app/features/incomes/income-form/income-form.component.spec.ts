import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { provideRouter, Router, ActivatedRoute } from '@angular/router';
import { of, throwError, Subject } from 'rxjs';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { provideNativeDateAdapter } from '@angular/material/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { HttpErrorResponse } from '@angular/common/http';

import { IncomeFormComponent } from './income-form.component';
import { IncomeService } from '../income.service';
import { Income } from '../income.model';

describe('IncomeFormComponent', () => {
  let component: IncomeFormComponent;
  let fixture: ComponentFixture<IncomeFormComponent>;
  let incomeServiceSpy: jasmine.SpyObj<IncomeService>;
  let snackBarSpy: jasmine.SpyObj<MatSnackBar>;
  let router: Router;

  function createComponent(routeParams: Record<string, string> = {}): void {
    incomeServiceSpy = jasmine.createSpyObj('IncomeService', [
      'getIncome',
      'createIncome',
      'updateIncome',
    ]);
    snackBarSpy = jasmine.createSpyObj('MatSnackBar', ['open']);

    TestBed.configureTestingModule({
      imports: [IncomeFormComponent],
      providers: [
        provideAnimationsAsync(),
        provideNativeDateAdapter(),
        provideRouter([{ path: 'incomes', children: [] }]),
        { provide: IncomeService, useValue: incomeServiceSpy },
        { provide: MatSnackBar, useValue: snackBarSpy },
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { params: routeParams } },
        },
      ],
    });

    fixture = TestBed.createComponent(IncomeFormComponent);
    component = fixture.componentInstance;
    router = TestBed.inject(Router);
    spyOn(router, 'navigate').and.returnValue(Promise.resolve(true));
  }

  const mockIncome: Income = {
    id: 1,
    date: '2024-06-15',
    description: 'Salário',
    value: 5000,
    isRecurring: true,
    frequency: 'monthly',
  };

  describe('Create Mode', () => {
    beforeEach(() => {
      createComponent({});
      fixture.detectChanges();
    });

    it('should show "Nova Receita" title in create mode', () => {
      const h1 = fixture.nativeElement.querySelector('h1') as HTMLElement;
      expect(h1.textContent?.trim()).toBe('Nova Receita');
    });

    it('should show empty form in create mode', () => {
      expect(component.form.get('date')!.value).toBeNull();
      expect(component.form.get('description')!.value).toBe('');
      expect(component.form.get('value')!.value).toBeNull();
      expect(component.form.get('recurrence')!.value).toEqual({ isRecurring: false, frequency: null });
    });

    it('should be in create mode when no id param', () => {
      expect(component.mode()).toBe('create');
    });

    it('should validate date required', () => {
      const dateControl = component.form.get('date')!;
      dateControl.setValue(null);
      dateControl.markAsTouched();

      expect(dateControl.hasError('required')).toBeTrue();
      expect(component.form.invalid).toBeTrue();
    });

    it('should validate description required', () => {
      const descControl = component.form.get('description')!;
      descControl.setValue('');
      descControl.markAsTouched();

      expect(descControl.hasError('required')).toBeTrue();
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

    it('should call createIncome on submit in create mode', fakeAsync(() => {
      incomeServiceSpy.createIncome.and.returnValue(of(mockIncome));

      component.form.patchValue({
        date: new Date(2024, 5, 15),
        description: 'Freelance',
        value: 3000,
        recurrence: { isRecurring: false, frequency: null },
      });
      component.onSubmit();
      tick();

      expect(incomeServiceSpy.createIncome).toHaveBeenCalledWith({
        date: '2024-06-15',
        description: 'Freelance',
        value: 3000,
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
      const subject = new Subject<Income>();
      incomeServiceSpy.createIncome.and.returnValue(subject.asObservable());

      component.form.patchValue({
        date: new Date(2024, 5, 15),
        description: 'Salário',
        value: 5000,
        recurrence: { isRecurring: false, frequency: null },
      });
      component.onSubmit();

      expect(component.isLoading()).toBeTrue();

      fixture.detectChanges();
      const submitButton = fixture.nativeElement.querySelector('button[type="submit"]') as HTMLButtonElement;
      expect(submitButton.disabled).toBeTrue();

      subject.next(mockIncome);
      subject.complete();
      tick();
    }));

    it('should show success snackbar and navigate on create success', fakeAsync(() => {
      incomeServiceSpy.createIncome.and.returnValue(of(mockIncome));

      component.form.patchValue({
        date: new Date(2024, 5, 15),
        description: 'Freelance',
        value: 3000,
        recurrence: { isRecurring: false, frequency: null },
      });
      component.onSubmit();
      tick();

      expect(snackBarSpy.open).toHaveBeenCalledWith(
        'Receita criada com sucesso',
        'Fechar',
        jasmine.objectContaining({ duration: 3000 })
      );
      expect(router.navigate).toHaveBeenCalledWith(['/incomes']);
    }));

    it('should not make API call when form is invalid', () => {
      component.form.get('description')!.setValue('');
      component.onSubmit();

      expect(incomeServiceSpy.createIncome).not.toHaveBeenCalled();
    });

    it('should not make API call when cancel is clicked', () => {
      const cancelLink = fixture.nativeElement.querySelector('a[routerLink="/incomes"]') as HTMLElement;
      expect(cancelLink).toBeTruthy();
      expect(incomeServiceSpy.createIncome).not.toHaveBeenCalled();
      expect(incomeServiceSpy.updateIncome).not.toHaveBeenCalled();
    });
  });

  describe('Edit Mode', () => {
    it('should be in edit mode when id param present', () => {
      createComponent({ id: '1' });
      incomeServiceSpy.getIncome.and.returnValue(of(mockIncome));
      fixture.detectChanges();

      expect(component.mode()).toBe('edit');
    });

    it('should show "Editar Receita" title in edit mode', fakeAsync(() => {
      createComponent({ id: '1' });
      incomeServiceSpy.getIncome.and.returnValue(of(mockIncome));
      fixture.detectChanges();
      tick();
      fixture.detectChanges();

      const h1 = fixture.nativeElement.querySelector('h1') as HTMLElement;
      expect(h1.textContent?.trim()).toBe('Editar Receita');
    }));

    it('should load data and pre-fill form in edit mode', fakeAsync(() => {
      createComponent({ id: '1' });
      incomeServiceSpy.getIncome.and.returnValue(of(mockIncome));
      fixture.detectChanges();
      tick();

      expect(incomeServiceSpy.getIncome).toHaveBeenCalledWith(1);
      expect(component.form.get('description')!.value).toBe('Salário');
      expect(component.form.get('value')!.value).toBe(5000);
      expect(component.form.get('recurrence')!.value).toEqual({ isRecurring: true, frequency: 'monthly' });
    }));

    it('should navigate back with error snackbar on 404 in edit mode', fakeAsync(() => {
      createComponent({ id: '99' });
      const errorResponse = new HttpErrorResponse({
        status: 404,
        error: { title: 'Not Found', status: 404 },
      });
      incomeServiceSpy.getIncome.and.returnValue(throwError(() => errorResponse));
      fixture.detectChanges();
      tick();

      expect(snackBarSpy.open).toHaveBeenCalledWith(
        'Receita não encontrada.',
        'Fechar',
        jasmine.objectContaining({ duration: 5000 })
      );
      expect(router.navigate).toHaveBeenCalledWith(['/incomes']);
    }));

    it('should call updateIncome on submit in edit mode', fakeAsync(() => {
      createComponent({ id: '1' });
      incomeServiceSpy.getIncome.and.returnValue(of(mockIncome));
      fixture.detectChanges();
      tick();

      incomeServiceSpy.updateIncome.and.returnValue(of({ ...mockIncome, description: 'Salário Atualizado' }));

      component.form.patchValue({ description: 'Salário Atualizado' });
      component.onSubmit();
      tick();

      expect(incomeServiceSpy.updateIncome).toHaveBeenCalledWith(1, jasmine.objectContaining({
        description: 'Salário Atualizado',
      }));
    }));

    it('should show success snackbar and navigate on update success', fakeAsync(() => {
      createComponent({ id: '1' });
      incomeServiceSpy.getIncome.and.returnValue(of(mockIncome));
      fixture.detectChanges();
      tick();

      incomeServiceSpy.updateIncome.and.returnValue(of(mockIncome));

      component.onSubmit();
      tick();

      expect(snackBarSpy.open).toHaveBeenCalledWith(
        'Receita atualizada com sucesso',
        'Fechar',
        jasmine.objectContaining({ duration: 3000 })
      );
      expect(router.navigate).toHaveBeenCalledWith(['/incomes']);
    }));
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
      incomeServiceSpy.createIncome.and.returnValue(of(mockIncome));

      component.form.patchValue({
        date: new Date(2024, 5, 15),
        description: 'Salário Mensal',
        value: 5000,
        recurrence: { isRecurring: true, frequency: 'monthly' },
      });
      component.onSubmit();
      tick();

      expect(incomeServiceSpy.createIncome).toHaveBeenCalledWith(jasmine.objectContaining({
        isRecurring: true,
        frequency: 'monthly',
      }));
    }));

    it('should set frequency to null when recurring is disabled', fakeAsync(() => {
      incomeServiceSpy.createIncome.and.returnValue(of(mockIncome));

      component.form.patchValue({
        date: new Date(2024, 5, 15),
        description: 'Freelance',
        value: 3000,
        recurrence: { isRecurring: false, frequency: null },
      });
      component.onSubmit();
      tick();

      expect(incomeServiceSpy.createIncome).toHaveBeenCalledWith(jasmine.objectContaining({
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
      incomeServiceSpy.createIncome.and.returnValue(throwError(() => errorResponse));

      component.form.patchValue({
        date: new Date(2024, 5, 15),
        description: 'Teste',
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
      incomeServiceSpy.createIncome.and.returnValue(throwError(() => errorResponse));

      component.form.patchValue({
        date: new Date(2024, 5, 15),
        description: 'Teste',
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
      incomeServiceSpy.createIncome.and.returnValue(throwError(() => errorResponse));

      component.form.patchValue({
        date: new Date(2024, 5, 15),
        description: 'Teste',
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
      incomeServiceSpy.createIncome.and.returnValue(throwError(() => errorResponse));

      component.form.patchValue({
        date: new Date(2024, 5, 15),
        description: 'Teste',
        value: 100,
        recurrence: { isRecurring: false, frequency: null },
      });
      component.onSubmit();
      tick();

      expect(component.isLoading()).toBeFalse();
    }));
  });
});
