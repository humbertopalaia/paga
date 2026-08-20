import { Routes } from '@angular/router';

export const EXPENSES_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('../../shared/placeholder/placeholder.component')
      .then(m => m.PlaceholderComponent)
  }
];
