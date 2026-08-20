import { Injectable, signal, effect, Signal } from '@angular/core';

export type Theme = 'light' | 'dark';

const STORAGE_KEY = 'paga-theme';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  private _theme = signal<Theme>(this.resolveInitialTheme());

  readonly theme: Signal<Theme> = this._theme.asReadonly();

  constructor() {
    effect(() => {
      const t = this._theme();
      document.documentElement.setAttribute('data-theme', t);
      localStorage.setItem(STORAGE_KEY, t);
    });
  }

  toggle(): void {
    this._theme.update(current => current === 'light' ? 'dark' : 'light');
  }

  private resolveInitialTheme(): Theme {
    const stored = localStorage.getItem(STORAGE_KEY);
    if (stored === 'light' || stored === 'dark') {
      return stored;
    }
    const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches;
    return prefersDark ? 'dark' : 'light';
  }
}
