import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { MatDialog, MatDialogRef } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { By } from '@angular/platform-browser';
import { HttpErrorResponse } from '@angular/common/http';

import { ExpenseTypeListComponent } from './expense-type-list.component';
import { ExpenseTypeService } from '../expense-type.service';
import { PaginatedResponse } from '../../../core/models';
import { ExpenseType } from '../expense-type.model';
import { ConfirmDialogComponent, ConfirmDialogData } from '../../../shared/confirm-dialog/confirm-dialog.component';

describe('ExpenseTypeListComponent', () => {
  let component: ExpenseTypeListComponent;
  let fixture: ComponentFixture<ExpenseTypeListComponent>;
  let expenseTypeServiceSpy: jasmine.SpyObj<ExpenseTypeService>;
  let dialogSpy: jasmine.SpyObj<MatDialog>;
  let snackBarSpy: jasmine.SpyObj<MatSnackBar>;
  let router: Router;

  const mockExpenseTypes: PaginatedResponse<ExpenseType> = {
    items: [
      { id: 1, name: 'Alimentacao' },
      { id: 2, name: 'Transporte' },
    ],
    pageNumber: 1,
    pageSize: 10,
    totalCount: 2,
    totalPages: 1,
  };

  const emptyResponse: PaginatedResponse<ExpenseType> = {
    items: [],
    pageNumber: 1,
    pageSize: 10,
    totalCount: 0,
    totalPages: 0,
  };

  beforeEach(async () => {
    expenseTypeServiceSpy = jasmine.createSpyObj('ExpenseTypeService', [
      'getExpenseTypes',
      'deleteExpenseType',
    ]);
    expenseTypeServiceSpy.getExpenseTypes.and.returnValue(of(mockExpenseTypes));

    dialogSpy = jasmine.createSpyObj('MatDialog', ['open']);
    snackBarSpy = jasmine.createSpyObj('MatSnackBar', ['open']);

    await TestBed.configureTestingModule({
      imports: [ExpenseTypeListComponent],
      providers: [
        provideAnimationsAsync(),
        provideRouter([]),
        { provide: ExpenseTypeService, useValue: expenseTypeServiceSpy },
        { provide: MatDialog, useValue: dialogSpy },
        { provide: MatSnackBar, useValue: snackBarSpy },
      ],
    }).compileComponents();

    router = TestBed.inject(Router);
    spyOn(router, 'navigate');

    fixture = TestBed.createComponent(ExpenseTypeListComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should render table with data from service', () => {
    expect(expenseTypeServiceSpy.getExpenseTypes).toHaveBeenCalledWith({
      name: undefined,
      pageNumber: 1,
      pageSize: 10,
    });

    const rows = fixture.debugElement.queryAll(By.css('tr[mat-row]'));
    expect(rows.length).toBe(2);

    const firstRowCells = rows[0].queryAll(By.css('td'));
    expect(firstRowCells[0].nativeElement.textContent.trim()).toBe('1');
    expect(firstRowCells[1].nativeElement.textContent.trim()).toBe('Alimentacao');

    const secondRowCells = rows[1].queryAll(By.css('td'));
    expect(secondRowCells[0].nativeElement.textContent.trim()).toBe('2');
    expect(secondRowCells[1].nativeElement.textContent.trim()).toBe('Transporte');
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
    expenseTypeServiceSpy.getExpenseTypes.and.returnValue(of(emptyResponse));
    component.loadExpenseTypes();
    fixture.detectChanges();

    const emptyState = fixture.debugElement.query(By.css('.empty-state'));
    expect(emptyState).toBeTruthy();

    const emptyMessage = emptyState.query(By.css('.empty-state__message'));
    expect(emptyMessage.nativeElement.textContent.trim()).toBe('Nenhum registro encontrado');
  });

  it('should display error state with retry button on failure', () => {
    expenseTypeServiceSpy.getExpenseTypes.and.returnValue(
      throwError(() => new Error('Network error'))
    );
    component.loadExpenseTypes();
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
    expenseTypeServiceSpy.getExpenseTypes.and.returnValue(
      throwError(() => new Error('Network error'))
    );
    component.loadExpenseTypes();
    fixture.detectChanges();

    expenseTypeServiceSpy.getExpenseTypes.calls.reset();
    expenseTypeServiceSpy.getExpenseTypes.and.returnValue(of(mockExpenseTypes));

    const retryButton = fixture.debugElement.query(By.css('.error-state__retry'));
    retryButton.nativeElement.click();
    fixture.detectChanges();

    expect(expenseTypeServiceSpy.getExpenseTypes).toHaveBeenCalledTimes(1);
  });

  it('should trigger search with 300ms debounce', fakeAsync(() => {
    expenseTypeServiceSpy.getExpenseTypes.calls.reset();

    component.searchFilter.setValue('Alimentacao');
    tick(299);
    expect(expenseTypeServiceSpy.getExpenseTypes).not.toHaveBeenCalled();

    tick(1);
    expect(expenseTypeServiceSpy.getExpenseTypes).toHaveBeenCalledTimes(1);
    expect(expenseTypeServiceSpy.getExpenseTypes).toHaveBeenCalledWith({
      name: 'Alimentacao',
      pageNumber: 1,
      pageSize: 10,
    });
  }));

  it('should reset page to 1 when search changes', fakeAsync(() => {
    component.pageNumber.set(2);
    expenseTypeServiceSpy.getExpenseTypes.calls.reset();

    component.searchFilter.setValue('test');
    tick(300);

    expect(component.pageNumber()).toBe(1);
    expect(expenseTypeServiceSpy.getExpenseTypes).toHaveBeenCalledWith(
      jasmine.objectContaining({ pageNumber: 1 })
    );
  }));

  it('should navigate to edit route when edit button clicked', () => {
    const expenseType: ExpenseType = { id: 1, name: 'Alimentacao' };
    component.editExpenseType(expenseType);

    expect(router.navigate).toHaveBeenCalledWith(['/expense-types', 1, 'edit']);
  });

  it('should open confirm dialog on delete', () => {
    const dialogRefSpy = jasmine.createSpyObj<MatDialogRef<ConfirmDialogComponent, boolean>>(
      'MatDialogRef',
      ['afterClosed']
    );
    dialogRefSpy.afterClosed.and.returnValue(of(undefined));
    dialogSpy.open.and.returnValue(dialogRefSpy);

    const expenseType: ExpenseType = { id: 1, name: 'Alimentacao' };
    component.deleteExpenseType(expenseType);

    expect(dialogSpy.open).toHaveBeenCalledWith(ConfirmDialogComponent, {
      data: {
        title: 'Confirmar Exclusão',
        message: 'Deseja excluir o tipo "Alimentacao"?',
        confirmLabel: 'Excluir',
        type: 'danger',
      } as ConfirmDialogData,
    });
  });

  it('should call API and show snackbar on delete confirm', () => {
    const dialogRefSpy = jasmine.createSpyObj<MatDialogRef<ConfirmDialogComponent, boolean>>(
      'MatDialogRef',
      ['afterClosed']
    );
    dialogRefSpy.afterClosed.and.returnValue(of(true));
    dialogSpy.open.and.returnValue(dialogRefSpy);
    expenseTypeServiceSpy.deleteExpenseType.and.returnValue(of(undefined as unknown as void));
    expenseTypeServiceSpy.getExpenseTypes.calls.reset();

    const expenseType: ExpenseType = { id: 1, name: 'Alimentacao' };
    component.deleteExpenseType(expenseType);

    expect(expenseTypeServiceSpy.deleteExpenseType).toHaveBeenCalledWith(1);
    expect(snackBarSpy.open).toHaveBeenCalledWith(
      'Tipo de despesa excluído com sucesso',
      'Fechar',
      { duration: 3000 }
    );
    expect(expenseTypeServiceSpy.getExpenseTypes).toHaveBeenCalled();
  });

  it('should not call delete API when dialog is cancelled', () => {
    const dialogRefSpy = jasmine.createSpyObj<MatDialogRef<ConfirmDialogComponent, boolean>>(
      'MatDialogRef',
      ['afterClosed']
    );
    dialogRefSpy.afterClosed.and.returnValue(of(undefined));
    dialogSpy.open.and.returnValue(dialogRefSpy);

    const expenseType: ExpenseType = { id: 1, name: 'Alimentacao' };
    component.deleteExpenseType(expenseType);

    expect(expenseTypeServiceSpy.deleteExpenseType).not.toHaveBeenCalled();
  });

  it('should show error snackbar when delete API returns 409', () => {
    const dialogRefSpy = jasmine.createSpyObj<MatDialogRef<ConfirmDialogComponent, boolean>>(
      'MatDialogRef',
      ['afterClosed']
    );
    dialogRefSpy.afterClosed.and.returnValue(of(true));
    dialogSpy.open.and.returnValue(dialogRefSpy);

    const errorResponse = new HttpErrorResponse({
      status: 409,
      error: { title: 'Não é possível excluir um tipo de despesa que possui despesas vinculadas.' },
    });
    expenseTypeServiceSpy.deleteExpenseType.and.returnValue(throwError(() => errorResponse));

    const expenseType: ExpenseType = { id: 1, name: 'Alimentacao' };
    component.deleteExpenseType(expenseType);

    expect(snackBarSpy.open).toHaveBeenCalledWith(
      'Não é possível excluir um tipo de despesa que possui despesas vinculadas.',
      'Fechar',
      { duration: 5000 }
    );
  });
});
