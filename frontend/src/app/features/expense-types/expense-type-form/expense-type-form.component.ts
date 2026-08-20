import { Component, ChangeDetectionStrategy, OnInit, signal, inject } from '@angular/core';
import { ReactiveFormsModule, FormGroup, FormControl, Validators } from '@angular/forms';
import { Router, ActivatedRoute, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatSnackBar } from '@angular/material/snack-bar';

import { ExpenseTypeService } from '../expense-type.service';
import { ProblemDetails } from '../../../core/models/problem-details.model';

@Component({
  selector: 'app-expense-type-form',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    RouterLink,
  ],
  templateUrl: './expense-type-form.component.html',
  styleUrl: './expense-type-form.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ExpenseTypeFormComponent implements OnInit {
  private readonly expenseTypeService = inject(ExpenseTypeService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly snackBar = inject(MatSnackBar);

  readonly mode = signal<'create' | 'edit'>('create');
  readonly isLoading = signal(false);
  readonly expenseTypeId = signal<number | null>(null);

  form = new FormGroup({
    name: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
  });

  ngOnInit(): void {
    const id = this.route.snapshot.params['id'];
    if (id) {
      this.mode.set('edit');
      this.expenseTypeId.set(Number(id));
      this.loadExpenseType(Number(id));
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

  private loadExpenseType(id: number): void {
    this.isLoading.set(true);
    this.expenseTypeService.getExpenseType(id).subscribe({
      next: (expenseType) => {
        this.form.patchValue({ name: expenseType.name });
        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
        this.snackBar.open('Tipo de despesa não encontrado.', 'Fechar', { duration: 5000 });
        this.router.navigate(['/expense-types']);
      },
    });
  }

  private submitCreate(): void {
    const { name } = this.form.getRawValue();

    this.expenseTypeService.createExpenseType({ name }).subscribe({
      next: () => {
        this.isLoading.set(false);
        this.snackBar.open('Tipo de despesa criado com sucesso', 'Fechar', { duration: 3000 });
        this.router.navigate(['/expense-types']);
      },
      error: (err: HttpErrorResponse) => {
        this.isLoading.set(false);
        this.handleError(err);
      },
    });
  }

  private submitEdit(): void {
    const { name } = this.form.getRawValue();

    this.expenseTypeService.updateExpenseType(this.expenseTypeId()!, { name }).subscribe({
      next: () => {
        this.isLoading.set(false);
        this.snackBar.open('Tipo de despesa atualizado com sucesso', 'Fechar', { duration: 3000 });
        this.router.navigate(['/expense-types']);
      },
      error: (err: HttpErrorResponse) => {
        this.isLoading.set(false);
        this.handleError(err);
      },
    });
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
}
