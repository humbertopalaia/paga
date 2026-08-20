import { Component, ChangeDetectionStrategy, OnInit, signal, inject } from '@angular/core';
import { ReactiveFormsModule, FormGroup, FormControl, Validators } from '@angular/forms';
import { Router, ActivatedRoute, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatSnackBar } from '@angular/material/snack-bar';

import { UserService } from '../user.service';
import { ProblemDetails } from '../../../core/models/problem-details.model';
import { passwordMatchValidator } from './password-match.validator';

@Component({
  selector: 'app-user-form',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    RouterLink,
  ],
  templateUrl: './user-form.component.html',
  styleUrl: './user-form.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UserFormComponent implements OnInit {
  private readonly userService = inject(UserService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly snackBar = inject(MatSnackBar);

  readonly mode = signal<'create' | 'edit'>('create');
  readonly isLoading = signal(false);
  readonly userId = signal<string | null>(null);

  form!: FormGroup;

  ngOnInit(): void {
    const id = this.route.snapshot.params['id'];
    if (id) {
      this.mode.set('edit');
      this.userId.set(id);
      this.buildEditForm();
      this.loadUser(id);
    } else {
      this.mode.set('create');
      this.buildCreateForm();
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

  private buildCreateForm(): void {
    this.form = new FormGroup({
      name: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
      email: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.email] }),
      password: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.minLength(6)] }),
      passwordConfirmation: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    }, { validators: [passwordMatchValidator] });
  }

  private buildEditForm(): void {
    this.form = new FormGroup({
      name: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
      email: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.email] }),
      password: new FormControl('', { nonNullable: true }),
    });
  }

  private loadUser(id: string): void {
    this.isLoading.set(true);
    this.userService.getUser(id).subscribe({
      next: (user) => {
        this.form.patchValue({ name: user.name, email: user.email });
        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
        this.snackBar.open('Erro ao carregar usuário.', 'Fechar', { duration: 5000 });
        this.router.navigate(['/users']);
      },
    });
  }

  private submitCreate(): void {
    const { name, email, password } = this.form.getRawValue();

    this.userService.createUser({ name, email, password }).subscribe({
      next: () => {
        this.isLoading.set(false);
        this.snackBar.open('Usuário criado com sucesso', 'Fechar', { duration: 3000 });
        this.router.navigate(['/users']);
      },
      error: (err: HttpErrorResponse) => {
        this.isLoading.set(false);
        this.handleError(err);
      },
    });
  }

  private submitEdit(): void {
    const { name, email, password } = this.form.getRawValue();
    const payload: { name: string; email: string; password?: string } = { name, email };

    if (password) {
      payload.password = password;
    }

    this.userService.updateUser(this.userId()!, payload).subscribe({
      next: () => {
        this.isLoading.set(false);
        this.snackBar.open('Usuário atualizado com sucesso', 'Fechar', { duration: 3000 });
        this.router.navigate(['/users']);
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
