import { Component, ChangeDetectionStrategy, input, output, inject } from '@angular/core';
import { Router } from '@angular/router';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { ThemeToggleComponent } from '../../shared/theme-toggle/theme-toggle.component';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [MatToolbarModule, MatIconModule, MatButtonModule, ThemeToggleComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './header.component.html',
  styleUrl: './header.component.scss'
})
export class HeaderComponent {
  private router = inject(Router);

  isMobile = input<boolean>(false);
  menuToggle = output<void>();

  userName = 'Administrador';

  private readonly routeTitleMap: Record<string, string> = {
    '/dashboard': 'Dashboard',
    '/users': 'Usuários',
    '/expense-types': 'Tipos de Despesa',
    '/incomes': 'Receitas',
    '/expenses': 'Despesas',
  };

  get pageTitle(): string {
    const url = this.router.url;
    return this.routeTitleMap[url] ?? 'Dashboard';
  }

  onLogout(): void {
    // Non-functional until mvp-4
  }
}
