import { Routes } from '@angular/router';

export const INCOMES_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./income-list/income-list.component')
      .then(m => m.IncomeListComponent),
  },
  {
    path: 'new',
    loadComponent: () => import('./income-form/income-form.component')
      .then(m => m.IncomeFormComponent),
  },
  {
    path: ':id/edit',
    loadComponent: () => import('./income-form/income-form.component')
      .then(m => m.IncomeFormComponent),
  },
];
