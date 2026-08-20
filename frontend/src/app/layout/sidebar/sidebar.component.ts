import { Component, ChangeDetectionStrategy, output } from '@angular/core';
import { MatListModule } from '@angular/material/list';
import { MatIconModule } from '@angular/material/icon';
import { RouterLink, RouterLinkActive } from '@angular/router';

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [MatListModule, MatIconModule, RouterLink, RouterLinkActive],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './sidebar.component.html',
  styleUrl: './sidebar.component.scss'
})
export class SidebarComponent {
  navigated = output<void>();

  menuItems = [
    { label: 'Dashboard', icon: 'dashboard', route: '/dashboard' },
    { label: 'Usuários', icon: 'people', route: '/users' },
    { label: 'Tipos de Despesa', icon: 'category', route: '/expense-types' },
    { label: 'Receitas', icon: 'trending_up', route: '/incomes' },
    { label: 'Despesas', icon: 'trending_down', route: '/expenses' },
  ];

  onItemClick(): void {
    this.navigated.emit();
  }
}
