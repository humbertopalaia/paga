import { Component, ChangeDetectionStrategy, OnInit, OnDestroy, signal, computed, inject } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { Subject } from 'rxjs';
import { debounceTime, distinctUntilChanged, takeUntil } from 'rxjs/operators';
import { HttpErrorResponse } from '@angular/common/http';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatSelectModule } from '@angular/material/select';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';

import { ExpenseService } from '../expense.service';
import { Expense } from '../expense.model';
import { ExpenseTypeService } from '../../expense-types/expense-type.service';
import { ExpenseType } from '../../expense-types/expense-type.model';
import { ConfirmDialogComponent, ConfirmDialogData } from '../../../shared/confirm-dialog/confirm-dialog.component';

@Component({
  selector: 'app-expense-list',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    RouterLink,
    CurrencyPipe,
    DatePipe,
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatInputModule,
    MatFormFieldModule,
    MatDatepickerModule,
    MatSelectModule,
  ],
  templateUrl: './expense-list.component.html',
  styleUrl: './expense-list.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ExpenseListComponent implements OnInit, OnDestroy {
  private readonly expenseService = inject(ExpenseService);
  private readonly expenseTypeService = inject(ExpenseTypeService);
  private readonly router = inject(Router);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);
  private readonly destroy$ = new Subject<void>();

  readonly expenses = signal<Expense[]>([]);
  readonly expenseTypes = signal<ExpenseType[]>([]);
  readonly totalCount = signal(0);
  readonly totalPages = signal(0);
  readonly isLoading = signal(false);
  readonly error = signal<string | null>(null);
  readonly pageNumber = signal(1);
  readonly pageSize = signal(10);

  readonly dueDateFromFilter = new FormControl<Date | null>(null);
  readonly dueDateToFilter = new FormControl<Date | null>(null);
  readonly descriptionFilter = new FormControl('', { nonNullable: true });
  readonly expenseTypeFilter = new FormControl<number | null>(null);
  readonly isRecurringFilter = new FormControl<boolean | null>(null);

  readonly displayedColumns = ['dueDate', 'description', 'expenseTypeName', 'value', 'isRecurring', 'actions'];

  readonly pages = computed(() => {
    const total = this.totalPages();
    return Array.from({ length: total }, (_, i) => i + 1);
  });

  ngOnInit(): void {
    this.descriptionFilter.valueChanges
      .pipe(debounceTime(300), distinctUntilChanged(), takeUntil(this.destroy$))
      .subscribe(() => {
        this.pageNumber.set(1);
        this.loadExpenses();
      });

    this.loadExpenseTypes();
    this.loadExpenses();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadExpenses(): void {
    this.isLoading.set(true);
    this.error.set(null);

    const description = this.descriptionFilter.value.trim();

    this.expenseService
      .getExpenses({
        dueDateFrom: this.formatDate(this.dueDateFromFilter.value),
        dueDateTo: this.formatDate(this.dueDateToFilter.value),
        expenseTypeId: this.expenseTypeFilter.value ?? undefined,
        description: description || undefined,
        isRecurring: this.isRecurringFilter.value ?? undefined,
        pageNumber: this.pageNumber(),
        pageSize: this.pageSize(),
      })
      .subscribe({
        next: (response) => {
          this.expenses.set(response.items);
          this.totalCount.set(response.totalCount);
          this.totalPages.set(response.totalPages);
          this.isLoading.set(false);
        },
        error: () => {
          this.error.set('Erro ao carregar dados');
          this.isLoading.set(false);
        },
      });
  }

  loadExpenseTypes(): void {
    this.expenseTypeService
      .getExpenseTypes({ pageNumber: 1, pageSize: 100, name: '' })
      .subscribe(res => this.expenseTypes.set(res.items));
  }

  onDateFilterChange(): void {
    this.pageNumber.set(1);
    this.loadExpenses();
  }

  onTypeFilterChange(): void {
    this.pageNumber.set(1);
    this.loadExpenses();
  }

  onRecurringFilterChange(): void {
    this.pageNumber.set(1);
    this.loadExpenses();
  }

  goToPage(page: number): void {
    this.pageNumber.set(page);
    this.loadExpenses();
  }

  editExpense(expense: Expense): void {
    this.router.navigate(['/expenses', expense.id, 'edit']);
  }

  deleteExpense(expense: Expense): void {
    const data: ConfirmDialogData = {
      title: 'Confirmar Exclusão',
      message: `Deseja excluir a despesa "${expense.description}"? Esta ação não pode ser desfeita.`,
      confirmLabel: 'Excluir',
      type: 'danger',
    };

    this.dialog
      .open(ConfirmDialogComponent, { data })
      .afterClosed()
      .subscribe((confirmed) => {
        if (confirmed) {
          this.expenseService.deleteExpense(expense.id).subscribe({
            next: () => {
              this.snackBar.open('Despesa excluída com sucesso', 'Fechar', { duration: 3000 });
              this.loadExpenses();
            },
            error: (err: HttpErrorResponse) => {
              const message = err.error?.title || err.error?.message || 'Erro ao excluir despesa.';
              this.snackBar.open(message, 'Fechar', { duration: 5000 });
            },
          });
        }
      });
  }

  isOverdue(expense: Expense): boolean {
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    const [year, month, day] = expense.dueDate.split('-').map(Number);
    const dueDate = new Date(year, month - 1, day);
    return dueDate < today;
  }

  parseDate(dateStr: string): Date {
    const [year, month, day] = dateStr.split('-').map(Number);
    return new Date(year, month - 1, day);
  }

  private formatDate(date: Date | null): string | undefined {
    if (!date) return undefined;
    const y = date.getFullYear();
    const m = String(date.getMonth() + 1).padStart(2, '0');
    const d = String(date.getDate()).padStart(2, '0');
    return `${y}-${m}-${d}`;
  }
}
