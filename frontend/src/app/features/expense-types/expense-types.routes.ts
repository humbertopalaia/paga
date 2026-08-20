import { Routes } from '@angular/router';

export const EXPENSE_TYPES_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./expense-type-list/expense-type-list.component')
      .then(m => m.ExpenseTypeListComponent)
  },
  {
    path: 'new',
    loadComponent: () => import('./expense-type-form/expense-type-form.component')
      .then(m => m.ExpenseTypeFormComponent)
  },
  {
    path: ':id/edit',
    loadComponent: () => import('./expense-type-form/expense-type-form.component')
      .then(m => m.ExpenseTypeFormComponent)
  }
];
