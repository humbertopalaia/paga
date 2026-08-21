import { Component, ChangeDetectionStrategy, OnInit, signal, inject } from '@angular/core';
import { ReactiveFormsModule, FormGroup, FormControl, Validators, AbstractControl, ValidationErrors } from '@angular/forms';
import { Router, ActivatedRoute, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatSnackBar } from '@angular/material/snack-bar';

import { IncomeService } from '../income.service';
import { ProblemDetails } from '../../../core/models/problem-details.model';
import { RecurrenceSelectorComponent, RecurrenceValue } from '../../../shared/recurrence-selector/recurrence-selector.component';
import { CurrencyMaskDirective } from '../../../shared/currency-mask/currency-mask.directive';

function positiveValueValidator(control: AbstractControl): ValidationErrors | null {
  return control.value != null && control.value > 0 ? null : { positiveValue: true };
}

@Component({
  selector: 'app-income-form',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatDatepickerModule,
    RouterLink,
    RecurrenceSelectorComponent,
    CurrencyMaskDirective,
  ],
  templateUrl: './income-form.component.html',
  styleUrl: './income-form.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class IncomeFormComponent implements OnInit {
  private readonly incomeService = inject(IncomeService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly snackBar = inject(MatSnackBar);

  readonly mode = signal<'create' | 'edit'>('create');
  readonly isLoading = signal(false);
  readonly incomeId = signal<number | null>(null);

  form = new FormGroup({
    date: new FormControl<Date | null>(null, { validators: [Validators.required] }),
    description: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    value: new FormControl<number | null>(null, { validators: [Validators.required, positiveValueValidator] }),
    recurrence: new FormControl<RecurrenceValue>({ isRecurring: false, frequency: null }, { nonNullable: true }),
  });

  ngOnInit(): void {
    const id = this.route.snapshot.params['id'];
    if (id) {
      this.mode.set('edit');
      this.incomeId.set(Number(id));
      this.loadIncome(Number(id));
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

  private loadIncome(id: number): void {
    this.isLoading.set(true);
    this.incomeService.getIncome(id).subscribe({
      next: (income) => {
        this.form.patchValue({
          date: this.parseDate(income.date),
          description: income.description,
          value: income.value,
          recurrence: { isRecurring: income.isRecurring, frequency: income.frequency },
        });
        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
        this.snackBar.open('Receita não encontrada.', 'Fechar', { duration: 5000 });
        this.router.navigate(['/incomes']);
      },
    });
  }

  private submitCreate(): void {
    const payload = this.buildPayload();

    this.incomeService.createIncome(payload).subscribe({
      next: () => {
        this.isLoading.set(false);
        this.snackBar.open('Receita criada com sucesso', 'Fechar', { duration: 3000 });
        this.router.navigate(['/incomes']);
      },
      error: (err: HttpErrorResponse) => {
        this.isLoading.set(false);
        this.handleError(err);
      },
    });
  }

  private submitEdit(): void {
    const payload = this.buildPayload();

    this.incomeService.updateIncome(this.incomeId()!, payload).subscribe({
      next: () => {
        this.isLoading.set(false);
        this.snackBar.open('Receita atualizada com sucesso', 'Fechar', { duration: 3000 });
        this.router.navigate(['/incomes']);
      },
      error: (err: HttpErrorResponse) => {
        this.isLoading.set(false);
        this.handleError(err);
      },
    });
  }

  private buildPayload() {
    const { date, description, value, recurrence } = this.form.getRawValue();
    return {
      date: this.formatDate(date!),
      description,
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
