import { Component, ChangeDetectionStrategy, OnInit, OnDestroy, signal, computed, inject } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { Subject } from 'rxjs';
import { debounceTime, distinctUntilChanged, takeUntil } from 'rxjs/operators';
import { HttpErrorResponse } from '@angular/common/http';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';

import { ExpenseTypeService } from '../expense-type.service';
import { ExpenseType } from '../expense-type.model';
import { ConfirmDialogComponent, ConfirmDialogData } from '../../../shared/confirm-dialog/confirm-dialog.component';

@Component({
  selector: 'app-expense-type-list',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    RouterLink,
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatInputModule,
    MatFormFieldModule,
  ],
  templateUrl: './expense-type-list.component.html',
  styleUrl: './expense-type-list.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ExpenseTypeListComponent implements OnInit, OnDestroy {
  private readonly expenseTypeService = inject(ExpenseTypeService);
  private readonly router = inject(Router);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);
  private readonly destroy$ = new Subject<void>();

  readonly expenseTypes = signal<ExpenseType[]>([]);
  readonly totalCount = signal(0);
  readonly totalPages = signal(0);
  readonly isLoading = signal(false);
  readonly error = signal<string | null>(null);
  readonly pageNumber = signal(1);
  readonly pageSize = signal(10);

  readonly searchFilter = new FormControl('', { nonNullable: true });
  readonly displayedColumns = ['id', 'name', 'actions'];

  readonly pages = computed(() => {
    const total = this.totalPages();
    return Array.from({ length: total }, (_, i) => i + 1);
  });

  ngOnInit(): void {
    this.searchFilter.valueChanges
      .pipe(debounceTime(300), distinctUntilChanged(), takeUntil(this.destroy$))
      .subscribe(() => {
        this.pageNumber.set(1);
        this.loadExpenseTypes();
      });
    this.loadExpenseTypes();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadExpenseTypes(): void {
    this.isLoading.set(true);
    this.error.set(null);
    const name = this.searchFilter.value.trim();

    this.expenseTypeService
      .getExpenseTypes({
        name: name || undefined,
        pageNumber: this.pageNumber(),
        pageSize: this.pageSize(),
      })
      .subscribe({
        next: (response) => {
          this.expenseTypes.set(response.items);
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

  goToPage(page: number): void {
    this.pageNumber.set(page);
    this.loadExpenseTypes();
  }

  previousPage(): void {
    if (this.pageNumber() > 1) {
      this.goToPage(this.pageNumber() - 1);
    }
  }

  nextPage(): void {
    if (this.pageNumber() < this.totalPages()) {
      this.goToPage(this.pageNumber() + 1);
    }
  }

  editExpenseType(expenseType: ExpenseType): void {
    this.router.navigate(['/expense-types', expenseType.id, 'edit']);
  }

  deleteExpenseType(expenseType: ExpenseType): void {
    const data: ConfirmDialogData = {
      title: 'Confirmar Exclusão',
      message: `Deseja excluir o tipo "${expenseType.name}"?`,
      confirmLabel: 'Excluir',
      type: 'danger',
    };

    this.dialog
      .open(ConfirmDialogComponent, { data })
      .afterClosed()
      .subscribe((confirmed) => {
        if (confirmed) {
          this.expenseTypeService.deleteExpenseType(expenseType.id).subscribe({
            next: () => {
              this.snackBar.open('Tipo de despesa excluído com sucesso', 'Fechar', { duration: 3000 });
              this.loadExpenseTypes();
            },
            error: (err: HttpErrorResponse) => {
              const message = err.error?.title || err.error?.message || 'Erro ao excluir tipo de despesa.';
              this.snackBar.open(message, 'Fechar', { duration: 5000 });
            },
          });
        }
      });
  }
}
