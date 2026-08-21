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

import { IncomeService } from '../income.service';
import { Income } from '../income.model';
import { ConfirmDialogComponent, ConfirmDialogData } from '../../../shared/confirm-dialog/confirm-dialog.component';

@Component({
  selector: 'app-income-list',
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
  templateUrl: './income-list.component.html',
  styleUrl: './income-list.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class IncomeListComponent implements OnInit, OnDestroy {
  private readonly incomeService = inject(IncomeService);
  private readonly router = inject(Router);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);
  private readonly destroy$ = new Subject<void>();

  readonly incomes = signal<Income[]>([]);
  readonly totalCount = signal(0);
  readonly totalPages = signal(0);
  readonly isLoading = signal(false);
  readonly error = signal<string | null>(null);
  readonly pageNumber = signal(1);
  readonly pageSize = signal(10);

  readonly descriptionFilter = new FormControl('', { nonNullable: true });
  readonly dateFromFilter = new FormControl<Date | null>(null);
  readonly dateToFilter = new FormControl<Date | null>(null);
  readonly isRecurringFilter = new FormControl<boolean | null>(null);

  readonly displayedColumns = ['date', 'description', 'value', 'isRecurring', 'actions'];

  readonly pages = computed(() => {
    const total = this.totalPages();
    return Array.from({ length: total }, (_, i) => i + 1);
  });

  ngOnInit(): void {
    this.descriptionFilter.valueChanges
      .pipe(debounceTime(300), distinctUntilChanged(), takeUntil(this.destroy$))
      .subscribe(() => {
        this.pageNumber.set(1);
        this.loadIncomes();
      });

    this.loadIncomes();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadIncomes(): void {
    this.isLoading.set(true);
    this.error.set(null);

    const description = this.descriptionFilter.value.trim();

    this.incomeService
      .getIncomes({
        dateFrom: this.formatDate(this.dateFromFilter.value),
        dateTo: this.formatDate(this.dateToFilter.value),
        description: description || undefined,
        isRecurring: this.isRecurringFilter.value ?? undefined,
        pageNumber: this.pageNumber(),
        pageSize: this.pageSize(),
      })
      .subscribe({
        next: (response) => {
          this.incomes.set(response.items);
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

  onDateFilterChange(): void {
    this.pageNumber.set(1);
    this.loadIncomes();
  }

  onRecurringFilterChange(): void {
    this.pageNumber.set(1);
    this.loadIncomes();
  }

  goToPage(page: number): void {
    this.pageNumber.set(page);
    this.loadIncomes();
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

  editIncome(income: Income): void {
    this.router.navigate(['/incomes', income.id, 'edit']);
  }

  deleteIncome(income: Income): void {
    const data: ConfirmDialogData = {
      title: 'Confirmar Exclusão',
      message: `Deseja excluir a receita "${income.description}"? Esta ação não pode ser desfeita.`,
      confirmLabel: 'Excluir',
      type: 'danger',
    };

    this.dialog
      .open(ConfirmDialogComponent, { data })
      .afterClosed()
      .subscribe((confirmed) => {
        if (confirmed) {
          this.incomeService.deleteIncome(income.id).subscribe({
            next: () => {
              this.snackBar.open('Receita excluída com sucesso', 'Fechar', { duration: 3000 });
              this.loadIncomes();
            },
            error: (err: HttpErrorResponse) => {
              const message = err.error?.title || err.error?.message || 'Erro ao excluir receita.';
              this.snackBar.open(message, 'Fechar', { duration: 5000 });
            },
          });
        }
      });
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
