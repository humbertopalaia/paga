import { Component, ChangeDetectionStrategy, computed, inject } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { ThemeService } from '../../core/theme/theme.service';

@Component({
  selector: 'app-theme-toggle',
  standalone: true,
  imports: [MatIconModule, MatButtonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <button mat-icon-button (click)="toggle()" [attr.aria-label]="ariaLabel()">
      <mat-icon>{{ icon() }}</mat-icon>
    </button>
  `
})
export class ThemeToggleComponent {
  private themeService = inject(ThemeService);

  icon = computed(() => this.themeService.theme() === 'dark' ? 'light_mode' : 'dark_mode');
  ariaLabel = computed(() =>
    this.themeService.theme() === 'dark' ? 'Mudar para tema claro' : 'Mudar para tema escuro'
  );

  toggle(): void {
    this.themeService.toggle();
  }
}
