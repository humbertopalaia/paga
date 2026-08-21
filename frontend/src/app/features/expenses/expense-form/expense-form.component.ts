import { Component, ChangeDetectionStrategy, OnInit, signal, inject } from '@angular/core';
import { ReactiveFormsModule, FormGroup, FormControl, Validators, AbstractControl, ValidationErrors } from '@angular/forms';
import { Router, ActivatedRoute, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar } from '@angular/material/snack-bar';

import { ExpenseService } from '../expense.service';
import { ExpenseTypeService } from '../../expense-types/expense-type.service';
import { ExpenseType } from '../../expense-types/expense-type.model';
import { ProblemDetails } from '../../../core/models/problem-details.model';
import { RecurrenceSelectorComponent, RecurrenceValue } from '../../../shared/recurrence-selector/recurrence-selector.component';
import { CurrencyMaskDirective } from '../../../shared/currency-mask/currency-mask.directive';

function positiveValueValidator(control: AbstractControl): ValidationErrors | null {
  return control.value != null && control.value > 0 ? null : { positiveValue: true };
}

@Component({
  selector: 'app-expense-form',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatDatepickerModule,
    MatSelectModule,
    RouterLink,
    RecurrenceSelectorComponent,
    CurrencyMaskDirective,
  ],
  templateUrl: './expense-form.component.html',
  styleUrl: './expense-form.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ExpenseFormComponent implements OnInit {
  private readonly expenseService = inject(ExpenseService);
  private readonly expenseTypeService = inject(ExpenseTypeService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly snackBar = inject(MatSnackBar);

  readonly mode = signal<'create' | 'edit'>('create');
  readonly isLoading = signal(false);
  readonly expenseId = signal<number | null>(null);
  readonly expenseTypes = signal<ExpenseType[]>([]);
  readonly typesError = signal(false);

  form = new FormGroup({
    dueDate: new FormControl<Date | null>(null, { validators: [Validators.required] }),
    description: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    expenseTypeId: new FormControl<number | null>(null, { validators: [Validators.required] }),
    value: new FormControl<number | null>(null, { validators: [Validators.required, positiveValueValidator] }),
    recurrence: new FormControl<RecurrenceValue>({ isRecurring: false, frequency: null }, { nonNullable: true }),
  });

  ngOnInit(): void {
    this.loadExpenseTypes();

    const id = this.route.snapshot.params['id'];
    if (id) {
      this.mode.set('edit');
      this.expenseId.set(Number(id));
      this.loadExpense(Number(id));
    }
  }

  onSubmit(): void {
    if (this.form.invalid || this.isLoading()) return;

    this.isLoading.set(true);

    if (this.mode() === 'create') {
      this.submitCreate();
    } else {
      this.submitEdit();
    }
  }

  loadExpenseTypes(): void {
    this.expenseTypeService.getExpenseTypes({ pageNumber: 1, pageSize: 100, name: '' }).subscribe({
      next: (response) => {
        this.expenseTypes.set(response.items);
        this.typesError.set(false);
      },
      error: () => {
        this.typesError.set(true);
      },
    });
  }

  private loadExpense(id: number): void {
    this.isLoading.set(true);
    this.expenseService.getExpense(id).subscribe({
      next: (expense) => {
        this.form.patchValue({
          dueDate: this.parseDate(expense.dueDate),
          description: expense.description,
          expenseTypeId: expense.expenseTypeId,
          value: expense.value,
          recurrence: { isRecurring: expense.isRecurring, frequency: expense.frequency },
        });
        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
        this.snackBar.open('Despesa não encontrada.', 'Fechar', { duration: 5000 });
        this.router.navigate(['/expenses']);
      },
    });
  }

  private submitCreate(): void {
    const payload = this.buildPayload();

    this.expenseService.createExpense(payload).subscribe({
      next: () => {
        this.isLoading.set(false);
        this.snackBar.open('Despesa criada com sucesso', 'Fechar', { duration: 3000 });
        this.router.navigate(['/expenses']);
      },
      error: (err: HttpErrorResponse) => {
        this.isLoading.set(false);
        this.handleError(err);
      },
    });
  }

  private submitEdit(): void {
    const payload = this.buildPayload();

    this.expenseService.updateExpense(this.expenseId()!, payload).subscribe({
      next: () => {
        this.isLoading.set(false);
        this.snackBar.open('Despesa atualizada com sucesso', 'Fechar', { duration: 3000 });
        this.router.navigate(['/expenses']);
      },
      error: (err: HttpErrorResponse) => {
        this.isLoading.set(false);
        this.handleError(err);
      },
    });
  }

  private buildPayload() {
    const { dueDate, description, expenseTypeId, value, recurrence } = this.form.getRawValue();
    return {
      dueDate: this.formatDate(dueDate!),
      description,
      expenseTypeId: expenseTypeId!,
      value: value!,
      isRecurring: recurrence.isRecurring,
      frequency: recurrence.frequency,
    };
  }

  private handleError(err: HttpErrorResponse): void {
    if (err.status === 409 || err.status === 400) {
      const problem = err.error as ProblemDetails;
      const message = problem.errors
        ? Object.values(problem.errors).flat().join('. ')
        : problem.title;
      this.snackBar.open(message, 'Fechar', { duration: 5000 });
    } else {
      this.snackBar.open('Erro inesperado. Tente novamente.', 'Fechar', { duration: 5000 });
    }
  }

  private formatDate(date: Date): string {
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  }

  private parseDate(dateStr: string): Date {
    const [year, month, day] = dateStr.split('-').map(Number);
    return new Date(year, month - 1, day);
  }
}
