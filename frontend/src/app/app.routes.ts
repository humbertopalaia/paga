import { Routes } from '@angular/router';
import { authGuard } from './core/auth/auth.guard';
import { ShellComponent } from './layout/shell/shell.component';

export const routes: Routes = [
  {
    path: 'login',
    loadChildren: () => import('./features/auth/auth.routes').then(m => m.AUTH_ROUTES)
  },
  {
    path: '',
    component: ShellComponent,
    canActivate: [authGuard],
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      {
        path: 'dashboard',
        loadComponent: () => import('./features/dashboard/dashboard.component')
          .then(m => m.DashboardComponent)
      },
      {
        path: 'users',
        loadChildren: () => import('./features/users/users.routes')
          .then(m => m.USERS_ROUTES)
      },
      {
        path: 'expense-types',
        loadChildren: () => import('./features/expense-types/expense-types.routes')
          .then(m => m.EXPENSE_TYPES_ROUTES)
      },
      {
        path: 'incomes',
        loadChildren: () => import('./features/incomes/incomes.routes')
          .then(m => m.INCOMES_ROUTES)
      },
      {
        path: 'expenses',
        loadChildren: () => import('./features/expenses/expenses.routes')
          .then(m => m.EXPENSES_ROUTES)
      },
    ]
  },
  { path: '**', redirectTo: 'dashboard' }
];
