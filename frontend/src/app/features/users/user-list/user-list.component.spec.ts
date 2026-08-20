import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { of, throwError, Subject } from 'rxjs';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { MatDialog, MatDialogRef } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { By } from '@angular/platform-browser';

import { UserListComponent } from './user-list.component';
import { UserService } from '../user.service';
import { PaginatedResponse } from '../../../core/models';
import { User } from '../user.model';
import { ConfirmDialogComponent, ConfirmDialogData } from '../../../shared/confirm-dialog/confirm-dialog.component';

describe('UserListComponent', () => {
  let component: UserListComponent;
  let fixture: ComponentFixture<UserListComponent>;
  let userServiceSpy: jasmine.SpyObj<UserService>;
  let dialogSpy: jasmine.SpyObj<MatDialog>;
  let snackBarSpy: jasmine.SpyObj<MatSnackBar>;

  const mockUsers: PaginatedResponse<User> = {
    items: [
      { id: '1', name: 'João Silva', email: 'joao@test.com', createdAt: '2024-01-15T10:00:00Z' },
      { id: '2', name: 'Maria Santos', email: 'maria@test.com', createdAt: '2024-02-20T08:30:00Z' },
    ],
    pageNumber: 1,
    pageSize: 10,
    totalCount: 2,
    totalPages: 1,
  };

  const mockUsersPaginated: PaginatedResponse<User> = {
    items: [
      { id: '1', name: 'João Silva', email: 'joao@test.com', createdAt: '2024-01-15T10:00:00Z' },
    ],
    pageNumber: 1,
    pageSize: 10,
    totalCount: 15,
    totalPages: 2,
  };

  const emptyResponse: PaginatedResponse<User> = {
    items: [],
    pageNumber: 1,
    pageSize: 10,
    totalCount: 0,
    totalPages: 0,
  };

  beforeEach(async () => {
    userServiceSpy = jasmine.createSpyObj('UserService', ['getUsers', 'deleteUser']);
    userServiceSpy.getUsers.and.returnValue(of(mockUsers));

    dialogSpy = jasmine.createSpyObj('MatDialog', ['open']);
    snackBarSpy = jasmine.createSpyObj('MatSnackBar', ['open']);

    await TestBed.configureTestingModule({
      imports: [UserListComponent],
      providers: [
        provideAnimationsAsync(),
        provideRouter([]),
        { provide: UserService, useValue: userServiceSpy },
        { provide: MatDialog, useValue: dialogSpy },
        { provide: MatSnackBar, useValue: snackBarSpy },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(UserListComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should load users on init and display them in the table', () => {
    expect(userServiceSpy.getUsers).toHaveBeenCalledWith({
      name: undefined,
      email: undefined,
      pageNumber: 1,
      pageSize: 10,
    });

    const rows = fixture.debugElement.queryAll(By.css('tr[mat-row]'));
    expect(rows.length).toBe(2);

    const firstRowCells = rows[0].queryAll(By.css('td'));
    expect(firstRowCells[0].nativeElement.textContent.trim()).toBe('João Silva');
    expect(firstRowCells[1].nativeElement.textContent.trim()).toBe('joao@test.com');

    const secondRowCells = rows[1].queryAll(By.css('td'));
    expect(secondRowCells[0].nativeElement.textContent.trim()).toBe('Maria Santos');
    expect(secondRowCells[1].nativeElement.textContent.trim()).toBe('maria@test.com');
  });

  it('should show loading skeleton while loading', () => {
    component.isLoading.set(true);
    component.error.set(null);
    fixture.detectChanges();

    const skeleton = fixture.debugElement.query(By.css('.loading-skeleton'));
    expect(skeleton).toBeTruthy();

    const table = fixture.debugElement.query(By.css('.table-wrapper'));
    expect(table).toBeNull();
  });

  it('should show empty state when no users returned', () => {
    userServiceSpy.getUsers.and.returnValue(of(emptyResponse));
    component.loadUsers();
    fixture.detectChanges();

    const emptyState = fixture.debugElement.query(By.css('.empty-state'));
    expect(emptyState).toBeTruthy();

    const emptyMessage = emptyState.query(By.css('.empty-state__message'));
    expect(emptyMessage.nativeElement.textContent.trim()).toBe('Nenhum usuário encontrado.');
  });

  it('should show error state with retry button on error', () => {
    userServiceSpy.getUsers.and.returnValue(throwError(() => new Error('Network error')));
    component.loadUsers();
    fixture.detectChanges();

    const errorState = fixture.debugElement.query(By.css('.error-state'));
    expect(errorState).toBeTruthy();

    const errorMessage = errorState.query(By.css('.error-state__message'));
    expect(errorMessage.nativeElement.textContent.trim()).toBe('Erro ao carregar usuários. Tente novamente.');

    const retryButton = errorState.query(By.css('.error-state__retry'));
    expect(retryButton).toBeTruthy();
  });

  it('should retry loading when retry button is clicked', () => {
    userServiceSpy.getUsers.and.returnValue(throwError(() => new Error('Network error')));
    component.loadUsers();
    fixture.detectChanges();

    userServiceSpy.getUsers.calls.reset();
    userServiceSpy.getUsers.and.returnValue(of(mockUsers));

    const retryButton = fixture.debugElement.query(By.css('.error-state__retry'));
    retryButton.nativeElement.click();
    fixture.detectChanges();

    expect(userServiceSpy.getUsers).toHaveBeenCalledTimes(1);
  });

  it('should debounce search input by 300ms', fakeAsync(() => {
    userServiceSpy.getUsers.calls.reset();

    component.searchFilter.setValue('joao');
    tick(299);
    expect(userServiceSpy.getUsers).not.toHaveBeenCalled();

    tick(1);
    expect(userServiceSpy.getUsers).toHaveBeenCalledTimes(1);
    expect(userServiceSpy.getUsers).toHaveBeenCalledWith({
      name: 'joao',
      email: 'joao',
      pageNumber: 1,
      pageSize: 10,
    });
  }));

  it('should reset page to 1 when search changes', fakeAsync(() => {
    component.pageNumber.set(2);
    userServiceSpy.getUsers.calls.reset();

    component.searchFilter.setValue('test');
    tick(300);

    expect(component.pageNumber()).toBe(1);
    expect(userServiceSpy.getUsers).toHaveBeenCalledWith(
      jasmine.objectContaining({ pageNumber: 1 })
    );
  }));

  it('should not send duplicate requests for same search value', fakeAsync(() => {
    userServiceSpy.getUsers.calls.reset();

    component.searchFilter.setValue('maria');
    tick(300);
    expect(userServiceSpy.getUsers).toHaveBeenCalledTimes(1);

    userServiceSpy.getUsers.calls.reset();
    component.searchFilter.setValue('maria');
    tick(300);
    expect(userServiceSpy.getUsers).not.toHaveBeenCalled();
  }));

  it('should display active page button with correct styling', () => {
    userServiceSpy.getUsers.and.returnValue(of(mockUsersPaginated));
    component.loadUsers();
    fixture.detectChanges();

    const activeBtn = fixture.debugElement.query(By.css('.pagination__btn--active'));
    expect(activeBtn).toBeTruthy();
    expect(activeBtn.nativeElement.textContent.trim()).toBe('1');
  });

  it('should change page when pagination button clicked', () => {
    userServiceSpy.getUsers.and.returnValue(of(mockUsersPaginated));
    component.loadUsers();
    fixture.detectChanges();

    userServiceSpy.getUsers.calls.reset();
    userServiceSpy.getUsers.and.returnValue(of({
      ...mockUsersPaginated,
      pageNumber: 2,
    }));

    component.goToPage(2);
    fixture.detectChanges();

    expect(userServiceSpy.getUsers).toHaveBeenCalledWith(
      jasmine.objectContaining({ pageNumber: 2 })
    );
  });

  it('should open confirm dialog on delete with correct data', () => {
    const dialogRefSpy = jasmine.createSpyObj<MatDialogRef<ConfirmDialogComponent, boolean>>('MatDialogRef', ['afterClosed']);
    dialogRefSpy.afterClosed.and.returnValue(of(undefined));
    dialogSpy.open.and.returnValue(dialogRefSpy);

    const user: User = { id: '1', name: 'João Silva', email: 'joao@test.com', createdAt: '2024-01-15T10:00:00Z' };
    component.deleteUser(user);

    expect(dialogSpy.open).toHaveBeenCalledWith(ConfirmDialogComponent, {
      data: {
        title: 'Confirmar Exclusão',
        message: 'Deseja excluir o usuário João Silva? Esta ação não pode ser desfeita.',
        confirmLabel: 'Excluir',
        type: 'danger',
      } as ConfirmDialogData,
    });
  });

  it('should delete user and show snackbar on confirm', () => {
    const dialogRefSpy = jasmine.createSpyObj<MatDialogRef<ConfirmDialogComponent, boolean>>('MatDialogRef', ['afterClosed']);
    dialogRefSpy.afterClosed.and.returnValue(of(true));
    dialogSpy.open.and.returnValue(dialogRefSpy);
    userServiceSpy.deleteUser.and.returnValue(of(undefined as unknown as void));
    userServiceSpy.getUsers.calls.reset();

    const user: User = { id: '1', name: 'João Silva', email: 'joao@test.com', createdAt: '2024-01-15T10:00:00Z' };
    component.deleteUser(user);

    expect(userServiceSpy.deleteUser).toHaveBeenCalledWith('1');
    expect(snackBarSpy.open).toHaveBeenCalledWith('Usuário excluído com sucesso', 'Fechar', { duration: 3000 });
    expect(userServiceSpy.getUsers).toHaveBeenCalled();
  });

  it('should not delete when dialog is cancelled', () => {
    const dialogRefSpy = jasmine.createSpyObj<MatDialogRef<ConfirmDialogComponent, boolean>>('MatDialogRef', ['afterClosed']);
    dialogRefSpy.afterClosed.and.returnValue(of(undefined));
    dialogSpy.open.and.returnValue(dialogRefSpy);

    const user: User = { id: '1', name: 'João Silva', email: 'joao@test.com', createdAt: '2024-01-15T10:00:00Z' };
    component.deleteUser(user);

    expect(userServiceSpy.deleteUser).not.toHaveBeenCalled();
  });
});
