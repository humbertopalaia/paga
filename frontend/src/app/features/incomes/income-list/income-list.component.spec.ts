import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { MatDialog, MatDialogRef } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { provideNativeDateAdapter } from '@angular/material/core';
import { By } from '@angular/platform-browser';
import { HttpErrorResponse } from '@angular/common/http';

import { IncomeListComponent } from './income-list.component';
import { IncomeService } from '../income.service';
import { PaginatedResponse } from '../../../core/models';
import { Income } from '../income.model';
import { ConfirmDialogComponent, ConfirmDialogData } from '../../../shared/confirm-dialog/confirm-dialog.component';

describe('IncomeListComponent', () => {
  let component: IncomeListComponent;
  let fixture: ComponentFixture<IncomeListComponent>;
  let incomeServiceSpy: jasmine.SpyObj<IncomeService>;
  let dialogSpy: jasmine.SpyObj<MatDialog>;
  let snackBarSpy: jasmine.SpyObj<MatSnackBar>;
  let router: Router;

  const mockIncomes: PaginatedResponse<Income> = {
    items: [
      { id: 1, date: '2024-03-15', description: 'Salário', value: 5000.5, isRecurring: true, frequency: 'monthly' },
      { id: 2, date: '2024-02-10', description: 'Freelance', value: 1200.0, isRecurring: false, frequency: null },
    ],
    pageNumber: 1,
    pageSize: 10,
    totalCount: 2,
    totalPages: 1,
  };

  const emptyResponse: PaginatedResponse<Income> = {
    items: [],
    pageNumber: 1,
    pageSize: 10,
    totalCount: 0,
    totalPages: 0,
  };

  beforeEach(async () => {
    incomeServiceSpy = jasmine.createSpyObj('IncomeService', [
      'getIncomes',
      'deleteIncome',
    ]);
    incomeServiceSpy.getIncomes.and.returnValue(of(mockIncomes));

    dialogSpy = jasmine.createSpyObj('MatDialog', ['open']);
    snackBarSpy = jasmine.createSpyObj('MatSnackBar', ['open']);

    await TestBed.configureTestingModule({
      imports: [IncomeListComponent],
      providers: [
        provideAnimationsAsync(),
        provideRouter([]),
        provideNativeDateAdapter(),
        { provide: IncomeService, useValue: incomeServiceSpy },
        { provide: MatDialog, useValue: dialogSpy },
        { provide: MatSnackBar, useValue: snackBarSpy },
      ],
    }).compileComponents();

    router = TestBed.inject(Router);
    spyOn(router, 'navigate');

    fixture = TestBed.createComponent(IncomeListComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should render table with correct columns', () => {
    const headerCells = fixture.debugElement.queryAll(By.css('th.mat-mdc-header-cell'));
    const columnLabels = headerCells.map(cell => cell.nativeElement.textContent.trim());

    expect(columnLabels).toEqual(['Data', 'Descrição', 'Valor', 'Recorrente', 'Ações']);
  });

  it('should render table rows with data from service', () => {
    const rows = fixture.debugElement.queryAll(By.css('tr[mat-row]'));
    expect(rows.length).toBe(2);
  });

  it('should format date as dd/MM/yyyy', () => {
    const rows = fixture.debugElement.queryAll(By.css('tr[mat-row]'));
    const dateCells = rows.map(row => row.queryAll(By.css('td'))[0]);

    expect(dateCells[0].nativeElement.textContent.trim()).toBe('15/03/2024');
    expect(dateCells[1].nativeElement.textContent.trim()).toBe('10/02/2024');
  });

  it('should format value as BRL currency with success color', () => {
    const valueCells = fixture.debugElement.queryAll(By.css('.value-cell'));
    expect(valueCells.length).toBe(2);

    // CurrencyPipe with BRL — format depends on registered locale
    expect(valueCells[0].nativeElement.textContent.trim()).toContain('5,000.50');
    expect(valueCells[1].nativeElement.textContent.trim()).toContain('1,200.00');
  });

  it('should display recurrence as Sim/Não', () => {
    const rows = fixture.debugElement.queryAll(By.css('tr[mat-row]'));
    const recurringCells = rows.map(row => row.queryAll(By.css('td'))[3]);

    expect(recurringCells[0].nativeElement.textContent.trim()).toBe('Sim');
    expect(recurringCells[1].nativeElement.textContent.trim()).toBe('Não');
  });

  it('should trigger search with 300ms debounce on description filter', fakeAsync(() => {
    incomeServiceSpy.getIncomes.calls.reset();

    component.descriptionFilter.setValue('Salário');
    tick(299);
    expect(incomeServiceSpy.getIncomes).not.toHaveBeenCalled();

    tick(1);
    expect(incomeServiceSpy.getIncomes).toHaveBeenCalledTimes(1);
    expect(incomeServiceSpy.getIncomes).toHaveBeenCalledWith(
      jasmine.objectContaining({ description: 'Salário' })
    );
  }));

  it('should reset pagination to page 1 when description filter changes', fakeAsync(() => {
    component.pageNumber.set(3);
    incomeServiceSpy.getIncomes.calls.reset();

    component.descriptionFilter.setValue('test');
    tick(300);

    expect(component.pageNumber()).toBe(1);
    expect(incomeServiceSpy.getIncomes).toHaveBeenCalledWith(
      jasmine.objectContaining({ pageNumber: 1 })
    );
  }));

  it('should trigger API call and reset pagination on date picker change', () => {
    component.pageNumber.set(2);
    incomeServiceSpy.getIncomes.calls.reset();

    component.dateFromFilter.setValue(new Date(2024, 0, 1));
    component.onDateFilterChange();
    fixture.detectChanges();

    expect(component.pageNumber()).toBe(1);
    expect(incomeServiceSpy.getIncomes).toHaveBeenCalledWith(
      jasmine.objectContaining({ dateFrom: '2024-01-01', pageNumber: 1 })
    );
  });

  it('should trigger API call and reset pagination on dateTo change', () => {
    component.pageNumber.set(2);
    incomeServiceSpy.getIncomes.calls.reset();

    component.dateToFilter.setValue(new Date(2024, 11, 31));
    component.onDateFilterChange();
    fixture.detectChanges();

    expect(component.pageNumber()).toBe(1);
    expect(incomeServiceSpy.getIncomes).toHaveBeenCalledWith(
      jasmine.objectContaining({ dateTo: '2024-12-31', pageNumber: 1 })
    );
  });

  it('should trigger API call on isRecurring select change', () => {
    component.pageNumber.set(2);
    incomeServiceSpy.getIncomes.calls.reset();

    component.isRecurringFilter.setValue(true);
    component.onRecurringFilterChange();
    fixture.detectChanges();

    expect(component.pageNumber()).toBe(1);
    expect(incomeServiceSpy.getIncomes).toHaveBeenCalledWith(
      jasmine.objectContaining({ isRecurring: true, pageNumber: 1 })
    );
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
    incomeServiceSpy.getIncomes.and.returnValue(of(emptyResponse));
    component.loadIncomes();
    fixture.detectChanges();

    const emptyState = fixture.debugElement.query(By.css('.empty-state'));
    expect(emptyState).toBeTruthy();

    const emptyMessage = emptyState.query(By.css('.empty-state__message'));
    expect(emptyMessage.nativeElement.textContent.trim()).toBe('Nenhum registro encontrado');
  });

  it('should display error state with retry button on failure', () => {
    incomeServiceSpy.getIncomes.and.returnValue(
      throwError(() => new Error('Network error'))
    );
    component.loadIncomes();
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
    incomeServiceSpy.getIncomes.and.returnValue(
      throwError(() => new Error('Network error'))
    );
    component.loadIncomes();
    fixture.detectChanges();

    incomeServiceSpy.getIncomes.calls.reset();
    incomeServiceSpy.getIncomes.and.returnValue(of(mockIncomes));

    const retryButton = fixture.debugElement.query(By.css('.error-state__retry'));
    retryButton.nativeElement.click();
    fixture.detectChanges();

    expect(incomeServiceSpy.getIncomes).toHaveBeenCalledTimes(1);
  });

  it('should navigate to edit route when edit button clicked', () => {
    const income: Income = { id: 1, date: '2024-03-15', description: 'Salário', value: 5000, isRecurring: true, frequency: 'monthly' };
    component.editIncome(income);

    expect(router.navigate).toHaveBeenCalledWith(['/incomes', 1, 'edit']);
  });

  it('should open confirm dialog on delete', () => {
    const dialogRefSpy = jasmine.createSpyObj<MatDialogRef<ConfirmDialogComponent, boolean>>(
      'MatDialogRef',
      ['afterClosed']
    );
    dialogRefSpy.afterClosed.and.returnValue(of(undefined));
    dialogSpy.open.and.returnValue(dialogRefSpy);

    const income: Income = { id: 1, date: '2024-03-15', description: 'Salário', value: 5000, isRecurring: true, frequency: 'monthly' };
    component.deleteIncome(income);

    expect(dialogSpy.open).toHaveBeenCalledWith(ConfirmDialogComponent, {
      data: {
        title: 'Confirmar Exclusão',
        message: 'Deseja excluir a receita "Salário"? Esta ação não pode ser desfeita.',
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
    incomeServiceSpy.deleteIncome.and.returnValue(of(undefined as unknown as void));
    incomeServiceSpy.getIncomes.calls.reset();

    const income: Income = { id: 1, date: '2024-03-15', description: 'Salário', value: 5000, isRecurring: true, frequency: 'monthly' };
    component.deleteIncome(income);

    expect(incomeServiceSpy.deleteIncome).toHaveBeenCalledWith(1);
    expect(snackBarSpy.open).toHaveBeenCalledWith(
      'Receita excluída com sucesso',
      'Fechar',
      { duration: 3000 }
    );
    expect(incomeServiceSpy.getIncomes).toHaveBeenCalled();
  });

  it('should not call delete API when dialog is cancelled', () => {
    const dialogRefSpy = jasmine.createSpyObj<MatDialogRef<ConfirmDialogComponent, boolean>>(
      'MatDialogRef',
      ['afterClosed']
    );
    dialogRefSpy.afterClosed.and.returnValue(of(undefined));
    dialogSpy.open.and.returnValue(dialogRefSpy);

    const income: Income = { id: 1, date: '2024-03-15', description: 'Salário', value: 5000, isRecurring: false, frequency: null };
    component.deleteIncome(income);

    expect(incomeServiceSpy.deleteIncome).not.toHaveBeenCalled();
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
      error: { title: 'Erro ao excluir receita.' },
    });
    incomeServiceSpy.deleteIncome.and.returnValue(throwError(() => errorResponse));

    const income: Income = { id: 1, date: '2024-03-15', description: 'Salário', value: 5000, isRecurring: false, frequency: null };
    component.deleteIncome(income);

    expect(snackBarSpy.open).toHaveBeenCalledWith(
      'Erro ao excluir receita.',
      'Fechar',
      { duration: 5000 }
    );
  });

  it('should have "+ Nova Receita" button linking to /incomes/new', () => {
    const newBtn = fixture.debugElement.query(By.css('.new-income-btn'));
    expect(newBtn).toBeTruthy();
    expect(newBtn.nativeElement.textContent.trim()).toContain('Nova Receita');
    expect(newBtn.attributes['routerLink']).toBe('/incomes/new');
  });
});
