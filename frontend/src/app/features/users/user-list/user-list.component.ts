import { Component, ChangeDetectionStrategy, OnInit, OnDestroy, signal, computed, inject } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { Subject } from 'rxjs';
import { debounceTime, distinctUntilChanged, takeUntil } from 'rxjs/operators';
import { DatePipe } from '@angular/common';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';

import { UserService } from '../user.service';
import { User } from '../user.model';
import { ConfirmDialogComponent, ConfirmDialogData } from '../../../shared/confirm-dialog/confirm-dialog.component';

@Component({
  selector: 'app-user-list',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    RouterLink,
    DatePipe,
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatInputModule,
    MatFormFieldModule,
  ],
  templateUrl: './user-list.component.html',
  styleUrl: './user-list.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UserListComponent implements OnInit, OnDestroy {
  private readonly userService = inject(UserService);
  private readonly router = inject(Router);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);
  private readonly destroy$ = new Subject<void>();

  readonly users = signal<User[]>([]);
  readonly totalCount = signal(0);
  readonly totalPages = signal(0);
  readonly isLoading = signal(false);
  readonly error = signal<string | null>(null);
  readonly pageNumber = signal(1);
  readonly pageSize = signal(10);

  readonly searchFilter = new FormControl('', { nonNullable: true });
  readonly displayedColumns = ['name', 'email', 'createdAt', 'actions'];

  readonly pages = computed(() => {
    const total = this.totalPages();
    return Array.from({ length: total }, (_, i) => i + 1);
  });

  ngOnInit(): void {
    this.searchFilter.valueChanges
      .pipe(
        debounceTime(300),
        distinctUntilChanged(),
        takeUntil(this.destroy$)
      )
      .subscribe(() => {
        this.pageNumber.set(1);
        this.loadUsers();
      });

    this.loadUsers();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadUsers(): void {
    this.isLoading.set(true);
    this.error.set(null);

    const search = this.searchFilter.value.trim();

    this.userService.getUsers({
      name: search || undefined,
      email: search || undefined,
      pageNumber: this.pageNumber(),
      pageSize: this.pageSize(),
    }).subscribe({
      next: (response) => {
        this.users.set(response.items);
        this.totalCount.set(response.totalCount);
        this.totalPages.set(response.totalPages);
        this.isLoading.set(false);
      },
      error: () => {
        this.error.set('Erro ao carregar usuários. Tente novamente.');
        this.isLoading.set(false);
      },
    });
  }

  goToPage(page: number): void {
    this.pageNumber.set(page);
    this.loadUsers();
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

  editUser(user: User): void {
    this.router.navigate(['/users', user.id]);
  }

  deleteUser(user: User): void {
    const data: ConfirmDialogData = {
      title: 'Confirmar Exclusão',
      message: `Deseja excluir o usuário ${user.name}? Esta ação não pode ser desfeita.`,
      confirmLabel: 'Excluir',
      type: 'danger',
    };

    this.dialog.open(ConfirmDialogComponent, { data })
      .afterClosed()
      .subscribe(confirmed => {
        if (confirmed) {
          this.userService.deleteUser(user.id).subscribe({
            next: () => {
              this.snackBar.open('Usuário excluído com sucesso', 'Fechar', { duration: 3000 });
              this.loadUsers();
            },
            error: () => {
              this.snackBar.open('Erro ao excluir usuário. Tente novamente.', 'Fechar', { duration: 5000 });
            },
          });
        }
      });
  }
}
