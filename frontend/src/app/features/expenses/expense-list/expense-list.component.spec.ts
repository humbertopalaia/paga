import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { MatDialog, MatDialogRef } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { provideNativeDateAdapter } from '@angular/material/core';
import { By } from '@angular/platform-browser';
import { HttpErrorResponse } from '@angular/common/http';

import { ExpenseListComponent } from './expense-list.component';
import { ExpenseService } from '../expense.service';
import { ExpenseTypeService } from '../../expense-types/expense-type.service';
import { PaginatedResponse } from '../../../core/models';
import { Expense } from '../expense.model';
import { ExpenseType } from '../../expense-types/expense-type.model';
import { ConfirmDialogComponent, ConfirmDialogData } from '../../../shared/confirm-dialog/confirm-dialog.component';

function formatDateStr(date: Date): string {
  const y = date.getFullYear();
  const m = String(date.getMonth() + 1).padStart(2, '0');
  const d = String(date.getDate()).padStart(2, '0');
  return `${y}-${m}-${d}`;
}

describe('ExpenseListComponent', () => {
  let component: ExpenseListComponent;
  let fixture: ComponentFixture<ExpenseListComponent>;
  let expenseServiceSpy: jasmine.SpyObj<ExpenseService>;
  let expenseTypeServiceSpy: jasmine.SpyObj<ExpenseTypeService>;
  let dialogSpy: jasmine.SpyObj<MatDialog>;
  let snackBarSpy: jasmine.SpyObj<MatSnackBar>;
  let router: Router;

  const mockExpenses: PaginatedResponse<Expense> = {
    items: [
      { id: 1, dueDate: '2024-03-15', description: 'Internet', expenseTypeId: 1, expenseTypeName: 'Serviços', value: 120.50, isRecurring: true, frequency: 'monthly' },
      { id: 2, dueDate: '2024-02-10', description: 'Supermercado', expenseTypeId: 2, expenseTypeName: 'Alimentação', value: 450.00, isRecurring: false, frequency: null },
    ],
    pageNumber: 1,
    pageSize: 10,
    totalCount: 2,
    totalPages: 1,
  };

  const mockExpenseTypes: PaginatedResponse<ExpenseType> = {
    items: [{ id: 1, name: 'Serviços' }, { id: 2, name: 'Alimentação' }],
    pageNumber: 1,
    pageSize: 100,
    totalCount: 2,
    totalPages: 1,
  };

  const emptyResponse: PaginatedResponse<Expense> = {
    items: [],
    pageNumber: 1,
    pageSize: 10,
    totalCount: 0,
    totalPages: 0,
  };

  beforeEach(async () => {
    expenseServiceSpy = jasmine.createSpyObj('ExpenseService', ['getExpenses', 'deleteExpense']);
    expenseTypeServiceSpy = jasmine.createSpyObj('ExpenseTypeService', ['getExpenseTypes']);
    expenseServiceSpy.getExpenses.and.returnValue(of(mockExpenses));
    expenseTypeServiceSpy.getExpenseTypes.and.returnValue(of(mockExpenseTypes));

    dialogSpy = jasmine.createSpyObj('MatDialog', ['open']);
    snackBarSpy = jasmine.createSpyObj('MatSnackBar', ['open']);

    await TestBed.configureTestingModule({
      imports: [ExpenseListComponent],
      providers: [
        provideAnimationsAsync(),
        provideRouter([]),
        provideNativeDateAdapter(),
        { provide: ExpenseService, useValue: expenseServiceSpy },
        { provide: ExpenseTypeService, useValue: expenseTypeServiceSpy },
        { provide: MatDialog, useValue: dialogSpy },
        { provide: MatSnackBar, useValue: snackBarSpy },
      ],
    }).compileComponents();

    router = TestBed.inject(Router);
    spyOn(router, 'navigate');

    fixture = TestBed.createComponent(ExpenseListComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should render table with correct columns', () => {
    const headerCells = fixture.debugElement.queryAll(By.css('th.mat-mdc-header-cell'));
    const columnLabels = headerCells.map(cell => cell.nativeElement.textContent.trim());

    expect(columnLabels).toEqual(['Vencimento', 'Descrição', 'Tipo', 'Valor', 'Recorrente', 'Ações']);
  });

  it('should render table rows with data from service', () => {
    const rows = fixture.debugElement.queryAll(By.css('tr[mat-row]'));
    expect(rows.length).toBe(2);
  });

  it('should format dueDate as dd/MM/yyyy', () => {
    const rows = fixture.debugElement.queryAll(By.css('tr[mat-row]'));
    const dateCells = rows.map(row => row.queryAll(By.css('td'))[0]);

    expect(dateCells[0].nativeElement.textContent.trim()).toBe('15/03/2024');
    expect(dateCells[1].nativeElement.textContent.trim()).toBe('10/02/2024');
  });

  it('should format value as BRL currency with danger color', () => {
    const valueCells = fixture.debugElement.queryAll(By.css('.value-cell'));
    expect(valueCells.length).toBe(2);

    expect(valueCells[0].nativeElement.textContent.trim()).toContain('120.50');
    expect(valueCells[1].nativeElement.textContent.trim()).toContain('450.00');
  });

  it('should display expenseTypeName in Tipo column', () => {
    const rows = fixture.debugElement.queryAll(By.css('tr[mat-row]'));
    const typeCells = rows.map(row => row.queryAll(By.css('td'))[2]);

    expect(typeCells[0].nativeElement.textContent.trim()).toBe('Serviços');
    expect(typeCells[1].nativeElement.textContent.trim()).toBe('Alimentação');
  });

  it('should display recurrence as Sim/Não', () => {
    const rows = fixture.debugElement.queryAll(By.css('tr[mat-row]'));
    const recurringCells = rows.map(row => row.queryAll(By.css('td'))[4]);

    expect(recurringCells[0].nativeElement.textContent.trim()).toBe('Sim');
    expect(recurringCells[1].nativeElement.textContent.trim()).toBe('Não');
  });

  describe('description filter', () => {
    it('should trigger search with 300ms debounce', fakeAsync(() => {
      expenseServiceSpy.getExpenses.calls.reset();

      component.descriptionFilter.setValue('Internet');
      tick(299);
      expect(expenseServiceSpy.getExpenses).not.toHaveBeenCalled();

      tick(1);
      expect(expenseServiceSpy.getExpenses).toHaveBeenCalledTimes(1);
      expect(expenseServiceSpy.getExpenses).toHaveBeenCalledWith(
        jasmine.objectContaining({ description: 'Internet' })
      );
    }));

    it('should reset pagination to page 1 when filter changes', fakeAsync(() => {
      component.pageNumber.set(3);
      expenseServiceSpy.getExpenses.calls.reset();

      component.descriptionFilter.setValue('test');
      tick(300);

      expect(component.pageNumber()).toBe(1);
      expect(expenseServiceSpy.getExpenses).toHaveBeenCalledWith(
        jasmine.objectContaining({ pageNumber: 1 })
      );
    }));
  });

  describe('date filters', () => {
    it('should trigger API call and reset pagination on dueDateFrom change', () => {
      component.pageNumber.set(2);
      expenseServiceSpy.getExpenses.calls.reset();

      component.dueDateFromFilter.setValue(new Date(2024, 0, 1));
      component.onDateFilterChange();
      fixture.detectChanges();

      expect(component.pageNumber()).toBe(1);
      expect(expenseServiceSpy.getExpenses).toHaveBeenCalledWith(
        jasmine.objectContaining({ dueDateFrom: '2024-01-01', pageNumber: 1 })
      );
    });

    it('should trigger API call and reset pagination on dueDateTo change', () => {
      component.pageNumber.set(2);
      expenseServiceSpy.getExpenses.calls.reset();

      component.dueDateToFilter.setValue(new Date(2024, 11, 31));
      component.onDateFilterChange();
      fixture.detectChanges();

      expect(component.pageNumber()).toBe(1);
      expect(expenseServiceSpy.getExpenses).toHaveBeenCalledWith(
        jasmine.objectContaining({ dueDateTo: '2024-12-31', pageNumber: 1 })
      );
    });
  });

  describe('expenseTypeId filter', () => {
    it('should trigger API call and reset pagination on type change', () => {
      component.pageNumber.set(2);
      expenseServiceSpy.getExpenses.calls.reset();

      component.expenseTypeFilter.setValue(1);
      component.onTypeFilterChange();
      fixture.detectChanges();

      expect(component.pageNumber()).toBe(1);
      expect(expenseServiceSpy.getExpenses).toHaveBeenCalledWith(
        jasmine.objectContaining({ expenseTypeId: 1, pageNumber: 1 })
      );
    });
  });

  describe('isRecurring filter', () => {
    it('should trigger API call and reset pagination on recurring change', () => {
      component.pageNumber.set(2);
      expenseServiceSpy.getExpenses.calls.reset();

      component.isRecurringFilter.setValue(true);
      component.onRecurringFilterChange();
      fixture.detectChanges();

      expect(component.pageNumber()).toBe(1);
      expect(expenseServiceSpy.getExpenses).toHaveBeenCalledWith(
        jasmine.objectContaining({ isRecurring: true, pageNumber: 1 })
      );
    });
  });

  it('should load expense types on init for filter select', () => {
    expect(expenseTypeServiceSpy.getExpenseTypes).toHaveBeenCalledWith(
      jasmine.objectContaining({ pageNumber: 1, pageSize: 100 })
    );
    expect(component.expenseTypes().length).toBe(2);
    expect(component.expenseTypes()[0].name).toBe('Serviços');
    expect(component.expenseTypes()[1].name).toBe('Alimentação');
  });

  it('should display loading skeleton during fetch', () => {
    component.isLoading.set(true);
    component.error.set(null);
    fixture.detectChanges();

    const skeleton = fixture.debugElement.query(By.css('.loading-skeleton'));
    expect(skeleton).toBeTruthy();

    const table = fixture.debugElement.query(By.css('table'));
    expect(table).toBeNull();
  });

  it('should display empty state when no results', () => {
    expenseServiceSpy.getExpenses.and.returnValue(of(emptyResponse));
    component.loadExpenses();
    fixture.detectChanges();

    const emptyState = fixture.debugElement.query(By.css('.empty-state'));
    expect(emptyState).toBeTruthy();

    const emptyMessage = emptyState.query(By.css('.empty-state__message'));
    expect(emptyMessage.nativeElement.textContent.trim()).toBe('Nenhum registro encontrado');
  });

  it('should display error state with retry button on failure', () => {
    expenseServiceSpy.getExpenses.and.returnValue(
      throwError(() => new Error('Network error'))
    );
    component.loadExpenses();
    fixture.detectChanges();

    const errorState = fixture.debugElement.query(By.css('.error-state'));
    expect(errorState).toBeTruthy();

    const errorMessage = errorState.query(By.css('.error-state__message'));
    expect(errorMessage.nativeElement.textContent.trim()).toBe('Erro ao carregar dados');

    const retryButton = errorState.query(By.css('.error-state__retry'));
    expect(retryButton).toBeTruthy();
    expect(retryButton.nativeElement.textContent.trim()).toContain('Tentar Novamente');
  });

  it('should retry loading when retry button is clicked', () => {
    expenseServiceSpy.getExpenses.and.returnValue(
      throwError(() => new Error('Network error'))
    );
    component.loadExpenses();
    fixture.detectChanges();

    expenseServiceSpy.getExpenses.calls.reset();
    expenseServiceSpy.getExpenses.and.returnValue(of(mockExpenses));

    const retryButton = fixture.debugElement.query(By.css('.error-state__retry'));
    retryButton.nativeElement.click();
    fixture.detectChanges();

    expect(expenseServiceSpy.getExpenses).toHaveBeenCalledTimes(1);
  });

  it('should navigate to /expenses/:id/edit when edit button clicked', () => {
    const expense: Expense = mockExpenses.items[0];
    component.editExpense(expense);

    expect(router.navigate).toHaveBeenCalledWith(['/expenses', 1, 'edit']);
  });

  describe('delete flow', () => {
    it('should open confirm dialog on delete', () => {
      const dialogRefSpy = jasmine.createSpyObj<MatDialogRef<ConfirmDialogComponent, boolean>>(
        'MatDialogRef',
        ['afterClosed']
      );
      dialogRefSpy.afterClosed.and.returnValue(of(undefined));
      dialogSpy.open.and.returnValue(dialogRefSpy);

      const expense: Expense = mockExpenses.items[0];
      component.deleteExpense(expense);

      expect(dialogSpy.open).toHaveBeenCalledWith(ConfirmDialogComponent, {
        data: {
          title: 'Confirmar Exclusão',
          message: 'Deseja excluir a despesa "Internet"? Esta ação não pode ser desfeita.',
          confirmLabel: 'Excluir',
          type: 'danger',
        } as ConfirmDialogData,
      });
    });

    it('should call API and show snackbar on confirm', () => {
      const dialogRefSpy = jasmine.createSpyObj<MatDialogRef<ConfirmDialogComponent, boolean>>(
        'MatDialogRef',
        ['afterClosed']
      );
      dialogRefSpy.afterClosed.and.returnValue(of(true));
      dialogSpy.open.and.returnValue(dialogRefSpy);
      expenseServiceSpy.deleteExpense.and.returnValue(of(undefined as unknown as void));
      expenseServiceSpy.getExpenses.calls.reset();

      const expense: Expense = mockExpenses.items[0];
      component.deleteExpense(expense);

      expect(expenseServiceSpy.deleteExpense).toHaveBeenCalledWith(1);
      expect(snackBarSpy.open).toHaveBeenCalledWith(
        'Despesa excluída com sucesso',
        'Fechar',
        { duration: 3000 }
      );
      expect(expenseServiceSpy.getExpenses).toHaveBeenCalled();
    });

    it('should not call delete API when dialog is cancelled', () => {
      const dialogRefSpy = jasmine.createSpyObj<MatDialogRef<ConfirmDialogComponent, boolean>>(
        'MatDialogRef',
        ['afterClosed']
      );
      dialogRefSpy.afterClosed.and.returnValue(of(undefined));
      dialogSpy.open.and.returnValue(dialogRefSpy);

      const expense: Expense = mockExpenses.items[1];
      component.deleteExpense(expense);

      expect(expenseServiceSpy.deleteExpense).not.toHaveBeenCalled();
    });

    it('should show error snackbar when delete API fails', () => {
      const dialogRefSpy = jasmine.createSpyObj<MatDialogRef<ConfirmDialogComponent, boolean>>(
        'MatDialogRef',
        ['afterClosed']
      );
      dialogRefSpy.afterClosed.and.returnValue(of(true));
      dialogSpy.open.and.returnValue(dialogRefSpy);

      const errorResponse = new HttpErrorResponse({
        status: 500,
        error: { title: 'Erro ao excluir despesa.' },
      });
      expenseServiceSpy.deleteExpense.and.returnValue(throwError(() => errorResponse));

      const expense: Expense = mockExpenses.items[0];
      component.deleteExpense(expense);

      expect(snackBarSpy.open).toHaveBeenCalledWith(
        'Erro ao excluir despesa.',
        'Fechar',
        { duration: 5000 }
      );
    });
  });

  it('should have "+ Nova Despesa" button linking to /expenses/new', () => {
    const newBtn = fixture.debugElement.query(By.css('.new-expense-btn'));
    expect(newBtn).toBeTruthy();
    expect(newBtn.nativeElement.textContent.trim()).toContain('Nova Despesa');
    expect(newBtn.attributes['routerLink']).toBe('/expenses/new');
  });

  describe('overdue detection', () => {
    it('should return true for dueDate yesterday', () => {
      const yesterday = new Date();
      yesterday.setDate(yesterday.getDate() - 1);
      const expense: Expense = { ...mockExpenses.items[0], dueDate: formatDateStr(yesterday) };
      expect(component.isOverdue(expense)).toBeTrue();
    });

    it('should return false for dueDate today', () => {
      const today = new Date();
      const expense: Expense = { ...mockExpenses.items[0], dueDate: formatDateStr(today) };
      expect(component.isOverdue(expense)).toBeFalse();
    });

    it('should return false for dueDate tomorrow', () => {
      const tomorrow = new Date();
      tomorrow.setDate(tomorrow.getDate() + 1);
      const expense: Expense = { ...mockExpenses.items[0], dueDate: formatDateStr(tomorrow) };
      expect(component.isOverdue(expense)).toBeFalse();
    });

    it('should apply overdue-row CSS class to overdue rows', () => {
      const yesterday = new Date();
      yesterday.setDate(yesterday.getDate() - 1);
      const overdueExpense: Expense = { ...mockExpenses.items[0], dueDate: formatDateStr(yesterday) };

      component.expenses.set([overdueExpense]);
      component.isLoading.set(false);
      component.error.set(null);
      fixture.detectChanges();

      const row = fixture.nativeElement.querySelector('tr.overdue-row');
      expect(row).toBeTruthy();
    });

    it('should apply overdue-date CSS class to overdue date text', () => {
      const yesterday = new Date();
      yesterday.setDate(yesterday.getDate() - 1);
      const overdueExpense: Expense = { ...mockExpenses.items[0], dueDate: formatDateStr(yesterday) };

      component.expenses.set([overdueExpense]);
      component.isLoading.set(false);
      component.error.set(null);
      fixture.detectChanges();

      const dateCell = fixture.nativeElement.querySelector('td.overdue-date');
      expect(dateCell).toBeTruthy();
    });

    it('should not apply overdue-row CSS class to non-overdue rows', () => {
      const tomorrow = new Date();
      tomorrow.setDate(tomorrow.getDate() + 1);
      const futureExpense: Expense = { ...mockExpenses.items[0], dueDate: formatDateStr(tomorrow) };

      component.expenses.set([futureExpense]);
      component.isLoading.set(false);
      component.error.set(null);
      fixture.detectChanges();

      const row = fixture.nativeElement.querySelector('tr.overdue-row');
      expect(row).toBeFalsy();
    });

    it('should not apply overdue-date CSS class to non-overdue rows', () => {
      const tomorrow = new Date();
      tomorrow.setDate(tomorrow.getDate() + 1);
      const futureExpense: Expense = { ...mockExpenses.items[0], dueDate: formatDateStr(tomorrow) };

      component.expenses.set([futureExpense]);
      component.isLoading.set(false);
      component.error.set(null);
      fixture.detectChanges();

      const dateCell = fixture.nativeElement.querySelector('td.overdue-date');
      expect(dateCell).toBeFalsy();
    });
  });
});
