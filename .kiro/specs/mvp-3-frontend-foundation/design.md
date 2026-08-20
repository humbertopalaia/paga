# Design Document

## Introduction

This document describes the architecture, component design, data models, and interfaces for the PAGA frontend foundation (Stories 3.1, 3.2, 3.3). It covers Angular 19 project scaffolding, design token system with dark/light theming, and the shell layout with responsive sidebar navigation.

The design targets a minimal but complete SPA skeleton: all five navigation destinations are routed (three as placeholders), the theme system is fully functional, and the layout adapts responsively. Authentication guards and real data are deferred to mvp-4.

---

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────┐
│                     Angular 19 SPA                           │
├─────────────────────────────────────────────────────────────┤
│  AppComponent (bootstrapped)                                │
│  └── ShellComponent (layout wrapper)                        │
│       ├── SidebarComponent (mat-sidenav)                    │
│       ├── HeaderComponent (toolbar)                         │
│       └── <router-outlet> (lazy-loaded feature)             │
├─────────────────────────────────────────────────────────────┤
│  Core Layer                                                 │
│  └── ThemeService (signal-based, localStorage, DOM sync)    │
├─────────────────────────────────────────────────────────────┤
│  Styles Layer                                               │
│  ├── _tokens.scss (CSS custom properties, light + dark)     │
│  ├── _themes.scss (Angular Material custom theme)           │
│  └── styles.scss (global resets, Inter import, transitions) │
├─────────────────────────────────────────────────────────────┤
│  Features (lazy-loaded routes)                              │
│  ├── /dashboard → DashboardComponent (placeholder)          │
│  ├── /users → placeholder route (mvp-4 implementation)      │
│  ├── /expense-types → PlaceholderComponent                  │
│  ├── /incomes → PlaceholderComponent                        │
│  └── /expenses → PlaceholderComponent                       │
└─────────────────────────────────────────────────────────────┘
```

### Dependency Flow

```
AppComponent → ShellComponent → { SidebarComponent, HeaderComponent }
                                         ↓
                              ThemeService (injected in root)
                                         ↓
                              DOM: <html data-theme="...">
                              localStorage: 'paga-theme'
```

---

## Components

### AppComponent

- Bootstrapped component, minimal template: `<app-shell />`
- Standalone, imports `ShellComponent`
- No logic; serves as Angular entry point

### ShellComponent (`layout/shell/`)

```typescript
@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [MatSidenavModule, SidebarComponent, HeaderComponent, RouterOutlet],
  templateUrl: './shell.component.html',
  styleUrl: './shell.component.scss'
})
export class ShellComponent {
  private breakpointObserver = inject(BreakpointObserver);

  isMobile = signal(false);
  @ViewChild('sidenav') sidenav!: MatSidenav;

  constructor() {
    this.breakpointObserver
      .observe(['(max-width: 767px)'])
      .subscribe(result => this.isMobile.set(result.matches));
  }

  get sidenavMode(): 'side' | 'over' {
    return this.isMobile() ? 'over' : 'side';
  }

  get sidenavOpened(): boolean {
    return !this.isMobile();
  }

  onNavigation(): void {
    if (this.isMobile()) {
      this.sidenav.close();
    }
  }

  toggleSidenav(): void {
    this.sidenav.toggle();
  }
}
```

**Template structure:**
```html
<mat-sidenav-container class="shell-container">
  <mat-sidenav #sidenav [mode]="sidenavMode" [opened]="sidenavOpened">
    <app-sidebar (navigated)="onNavigation()" />
  </mat-sidenav>
  <mat-sidenav-content>
    <app-header [isMobile]="isMobile()" (menuToggle)="toggleSidenav()" />
    <main class="content">
      <router-outlet />
    </main>
  </mat-sidenav-content>
</mat-sidenav-container>
```

**CSS:** Full viewport height via `height: 100vh`, content area scrolls independently with `overflow-y: auto`.

### SidebarComponent (`layout/sidebar/`)

```typescript
@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [MatListModule, MatIconModule, RouterLink, RouterLinkActive],
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
```

**Template:** `mat-nav-list` with `routerLink`, `routerLinkActive` for highlighting, and PAGA logo/text at top.

### HeaderComponent (`layout/header/`)

```typescript
@Component({
  selector: 'app-header',
  standalone: true,
  imports: [MatToolbarModule, MatIconModule, MatButtonModule, ThemeToggleComponent],
  templateUrl: './header.component.html',
  styleUrl: './header.component.scss'
})
export class HeaderComponent {
  isMobile = input<boolean>(false);
  menuToggle = output<void>();

  userName = 'Administrador'; // placeholder until mvp-4 auth integration

  onLogout(): void {
    // Non-functional until mvp-4
  }
}
```

**Template:** `mat-toolbar` with conditional hamburger button (visible when `isMobile`), user name, `<app-theme-toggle>`, and logout icon button.

### ThemeToggleComponent (`shared/theme-toggle/`)

```typescript
@Component({
  selector: 'app-theme-toggle',
  standalone: true,
  imports: [MatIconModule, MatButtonModule],
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
```

### PlaceholderComponent (`shared/placeholder/`)

```typescript
@Component({
  selector: 'app-placeholder',
  standalone: true,
  imports: [MatIconModule],
  template: `
    <div class="placeholder-container">
      <mat-icon class="placeholder-icon">construction</mat-icon>
      <h2>Em construção</h2>
      <p>Esta funcionalidade estará disponível em breve.</p>
    </div>
  `,
  styleUrl: './placeholder.component.scss'
})
export class PlaceholderComponent {}
```

### DashboardComponent (`features/dashboard/`)

Placeholder implementation at the definitive route. Same visual as PlaceholderComponent but with its own component identity so it can be replaced in-place during mvp-4.

```typescript
@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [MatIconModule],
  template: `
    <div class="placeholder-container">
      <mat-icon class="placeholder-icon">construction</mat-icon>
      <h2>Dashboard</h2>
      <p>Em construção</p>
    </div>
  `
})
export class DashboardComponent {}
```

---

## Services

### ThemeService (`core/theme/theme.service.ts`)

```typescript
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
```

**Behaviour:**
1. On instantiation, check localStorage for `'paga-theme'`.
2. If found and valid (`'light'` or `'dark'`), use it.
3. Otherwise, query `prefers-color-scheme` media query.
4. An `effect` synchronises the signal to both DOM and localStorage on every change.

---

## Routing

### app.routes.ts

```typescript
export const appRoutes: Routes = [
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
];
```

### Feature route files (placeholder pattern)

Each feature route file (`expense-types.routes.ts`, `incomes.routes.ts`, `expenses.routes.ts`, `users.routes.ts`) exports a `Routes` array with a default path pointing to `PlaceholderComponent`:

```typescript
// features/expense-types/expense-types.routes.ts
export const EXPENSE_TYPES_ROUTES: Routes = [
  { path: '', loadComponent: () => import('../../shared/placeholder/placeholder.component')
      .then(m => m.PlaceholderComponent) }
];
```

No auth guards are applied in this delivery.

---

## Styles Architecture

### _tokens.scss

```scss
:root {
  // Blue palette
  --primary-50: #EFF6FF;
  --primary-100: #DBEAFE;
  --primary-200: #BFDBFE;
  --primary-300: #93C5FD;
  --primary-400: #60A5FA;
  --primary-500: #3B82F6;
  --primary-600: #2563EB;
  --primary-700: #1D4ED8;
  --primary-800: #1E40AF;
  --primary-900: #1E3A8A;

  // Background
  --bg-primary: #FFFFFF;
  --bg-secondary: #F8FAFC;
  --bg-tertiary: #F1F5F9;

  // Text
  --text-primary: #1E293B;
  --text-secondary: #64748B;
  --text-muted: #94A3B8;

  // Border
  --border: #E2E8F0;

  // Semantic
  --success: #10B981;
  --danger: #EF4444;
  --warning: #F59E0B;

  // Spacing (4px grid)
  --spacing-xs: 4px;
  --spacing-sm: 8px;
  --spacing-md: 16px;
  --spacing-lg: 24px;
  --spacing-xl: 32px;
  --spacing-2xl: 48px;
}

[data-theme="dark"] {
  --primary-50: #172554;
  --primary-100: #1E3A8A;
  --primary-200: #1E40AF;
  --primary-300: #2563EB;
  --primary-400: #3B82F6;
  --primary-500: #60A5FA;
  --primary-600: #93C5FD;
  --primary-700: #BFDBFE;
  --primary-800: #DBEAFE;
  --primary-900: #EFF6FF;

  --bg-primary: #0F172A;
  --bg-secondary: #1E293B;
  --bg-tertiary: #334155;

  --text-primary: #F8FAFC;
  --text-secondary: #CBD5E1;
  --text-muted: #64748B;

  --border: #334155;

  --success: #34D399;
  --danger: #F87171;
  --warning: #FBBF24;
}
```

### _themes.scss

Configures Angular Material's theming system using `@use '@angular/material' as mat` with custom palettes derived from the design tokens. Defines `$paga-light-theme` and `$paga-dark-theme` using `mat.define-theme()` with the blue primary palette and Inter typography.

The dark theme is applied via `[data-theme="dark"]` selector wrapper around Material's dark theme mixin, so it activates in sync with the CSS custom properties.

### styles.scss (global)

```scss
@use './app/styles/tokens';
@use './app/styles/themes';

@import url('https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&display=swap');

* {
  margin: 0;
  padding: 0;
  box-sizing: border-box;
}

html, body {
  height: 100%;
  font-family: 'Inter', sans-serif;
  font-size: 14px;
  font-weight: 400;
  background-color: var(--bg-primary);
  color: var(--text-primary);
  transition: background-color 0.3s ease, color 0.3s ease;
}
```

---

## Environment Configuration

### environment.ts (development)

```typescript
export const environment = {
  production: false,
  apiUrl: 'http://localhost:5062/api'
};
```

### environment.production.ts

```typescript
export const environment = {
  production: true,
  apiUrl: '/api'
};
```

---

## Responsive Behaviour

| Viewport | Sidebar Mode | Sidebar Default State | Menu Button |
|----------|-------------|----------------------|-------------|
| ≥ 768px | `side` | Open (always visible) | Hidden |
| < 768px | `over` | Closed | Visible in header |

The `BreakpointObserver` from `@angular/cdk/layout` detects viewport changes reactively. When mode is `over`, clicking a nav item emits a `navigated` event that the shell uses to close the sidenav.

---

## Error Handling

No API calls are made in this delivery. Error handling for HTTP will be implemented in mvp-4 with an HTTP interceptor. In this phase, errors are limited to:

- **Build errors:** Resolved during development; `ng build` must pass cleanly.
- **Theme resolution:** If `localStorage` contains an invalid value, it's ignored and system preference is used (fallback to `'light'` if media query is unsupported).

---

## Accessibility

- `ThemeToggle` uses `aria-label` describing the action (switch to light/dark).
- Sidebar navigation uses `mat-nav-list` (semantic `<nav>` role).
- Active route indicated via `routerLinkActive` adds `aria-current="page"` equivalent styling.
- All colours meet WCAG AA contrast ratios as defined in the Figma design tokens.
- Keyboard navigation supported through Angular Material's built-in focus management.

---

## Testing Strategy

### Unit Tests (Karma/Jasmine)

| Component/Service | What to Test |
|-------------------|--------------|
| ThemeService | Initial theme from system preference, initial theme from localStorage, toggle changes signal, toggle persists to localStorage, `data-theme` attribute updates |
| ShellComponent | Renders sidebar, header, router-outlet; sidebar mode changes with viewport |
| SidebarComponent | Renders all 5 menu items in correct order; emits `navigated` on click |
| HeaderComponent | Shows user name, theme toggle, logout button; shows menu button when mobile |
| ThemeToggleComponent | Shows `dark_mode` icon in light theme, `light_mode` in dark; calls toggle on click |
| PlaceholderComponent | Renders "Em construção" text |

### Test Utilities

- `HttpTestingController`: Not needed in this delivery (no HTTP calls).
- `BreakpointObserver`: Mock via `{ provide: BreakpointObserver, useValue: mockObserver }` to test responsive behaviour.
- `localStorage`: Use `spyOn(localStorage, 'getItem')` and `spyOn(localStorage, 'setItem')`.
- `matchMedia`: Mock via `spyOn(window, 'matchMedia')`.

---

## Correctness Properties

*A property is a characteristic or behavior that should hold true across all valid executions of a system — essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*

### Property 1: Design token name symmetry

*For any* CSS custom property name defined in the `:root` selector of `_tokens.scss`, that same property name SHALL exist in the `[data-theme="dark"]` selector, and vice versa. The set of variable names must be identical across both theme declarations.

**Validates: Requirements 3.3**

### Property 2: Theme signal invariant

*For any* sequence of operations on `ThemeService` (including zero or more calls to `toggle()`, and any initialization scenario), the value of `theme()` signal SHALL always be exactly `'light'` or `'dark'` — never `null`, `undefined`, or any other string.

**Validates: Requirements 4.1**

### Property 3: Toggle involution

*For any* initial theme state `T` (either `'light'` or `'dark'`), calling `ThemeService.toggle()` exactly twice SHALL return the theme signal to the original value `T`. Equivalently, `toggle` is its own inverse: `toggle(toggle(T)) === T`.

**Validates: Requirements 4.2**

### Property 4: Toggle state synchronisation

*For any* theme state, after `ThemeService.toggle()` is called, both `localStorage.getItem('paga-theme')` and `document.documentElement.getAttribute('data-theme')` SHALL equal the new value of the `theme()` signal. The three representations (signal, DOM, storage) are always consistent.

**Validates: Requirements 4.4, 4.6**

### Property 5: Responsive sidebar mode

*For any* viewport width `w` (where `w > 0`), the sidebar mode SHALL be `'side'` if `w >= 768` and `'over'` if `w < 768`. There is no third state and no hysteresis — the mode is a pure function of the current viewport width.

**Validates: Requirements 7.1, 7.2**

### Property 6: Auto-close on navigation in overlay mode

*For any* navigation item in the sidebar, if the sidebar is currently in `'over'` mode and is open, clicking that item SHALL cause the sidebar to close. This holds regardless of which specific item is clicked.

**Validates: Requirements 7.4**
