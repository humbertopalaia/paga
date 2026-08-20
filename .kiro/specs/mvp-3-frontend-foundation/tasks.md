# Implementation Plan: MVP-3 Frontend Foundation

## Overview

Scaffold the Angular 19 project, implement the design token system with dark/light theming, build the responsive shell layout with sidebar navigation, configure lazy-loaded routes for all five feature areas (three as placeholders), and write unit tests for all components and services. The result is a fully navigable SPA skeleton ready for auth and business module integration in mvp-4.

## Tasks

- [x] 1. Scaffold Angular 19 project and configure dependencies
  - [x] 1.1 Create Angular 19 project with Angular CLI
    - Run `ng new` with standalone, SCSS, routing options inside `frontend/`
    - Verify `ng build` passes with zero errors
    - _Requirements: 1.1, 1.4, 1.7_

  - [x] 1.2 Install and configure Angular Material
    - Add `@angular/material` and `@angular/cdk` via `ng add`
    - Configure custom theme entry point in `angular.json`
    - Remove default Material prebuilt theme if added
    - _Requirements: 1.6_

  - [x] 1.3 Create folder structure and environment files
    - Create directories: `src/app/core/`, `src/app/shared/`, `src/app/features/`, `src/app/layout/`, `src/app/styles/`
    - Create feature folders: `features/auth/`, `features/dashboard/`, `features/users/`, `features/expense-types/`, `features/incomes/`, `features/expenses/`
    - Create `src/environments/environment.ts` with `apiUrl: 'http://localhost:5062/api'`
    - Create `src/environments/environment.production.ts` with `apiUrl: '/api'`
    - _Requirements: 1.2, 1.3, 1.8_

- [x] 2. Implement design tokens and Angular Material theme
  - [x] 2.1 Create `_tokens.scss` with CSS custom properties
    - Define light theme tokens in `:root` selector (blue palette, bg, text, border, semantic, spacing)
    - Define dark theme tokens in `[data-theme="dark"]` selector with identical variable names
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5, 3.6_

  - [x] 2.2 Create `_themes.scss` with Angular Material custom theme
    - Define light and dark Material palettes using `mat.define-theme()`
    - Use Inter font as Material typography
    - Apply dark theme under `[data-theme="dark"]` selector
    - _Requirements: 5.1, 5.2, 5.3_

  - [x] 2.3 Create `styles.scss` global styles
    - Import tokens and themes
    - Import Inter font from Google Fonts
    - Apply global reset, `height: 100%` on html/body, Inter as default font
    - Add CSS transition on `background-color` and `color` for smooth theme switching
    - _Requirements: 3.7, 4.7_

  - [ ]* 2.4 Write property test for design token name symmetry
    - **Property 1: Design token name symmetry**
    - Parse `_tokens.scss` and verify `:root` and `[data-theme="dark"]` have identical variable name sets
    - **Validates: Requirements 3.3**

- [x] 3. Implement ThemeService
  - [x] 3.1 Create `ThemeService` in `core/theme/`
    - Signal-based service with `theme` readonly signal (`'light' | 'dark'`)
    - `toggle()` method that flips theme
    - Initial theme resolution: localStorage → system preference → fallback to `'light'`
    - `effect()` that syncs signal value to `document.documentElement.setAttribute('data-theme', ...)` and `localStorage.setItem('paga-theme', ...)`
    - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5, 4.6_

  - [ ]* 3.2 Write property test for theme signal invariant
    - **Property 2: Theme signal invariant**
    - Verify `theme()` only ever returns `'light'` or `'dark'` regardless of localStorage content or missing matchMedia
    - **Validates: Requirements 4.1**

  - [ ]* 3.3 Write property test for toggle involution
    - **Property 3: Toggle involution**
    - Verify `toggle(toggle(T)) === T` for any starting state
    - **Validates: Requirements 4.2**

  - [ ]* 3.4 Write property test for toggle state synchronisation
    - **Property 4: Toggle state synchronisation**
    - After each toggle, verify localStorage, DOM attribute, and signal are all consistent
    - **Validates: Requirements 4.4, 4.6**

- [x] 4. Checkpoint - Verify tokens and theme service
  - Ensure `ng build` passes and `ng test --watch=false` passes for all tests written so far. Ask the user if questions arise.

- [x] 5. Implement Shell layout components
  - [x] 5.1 Create `ShellComponent` in `layout/shell/`
    - `mat-sidenav-container` with responsive sidebar (`side`/`over` mode via `BreakpointObserver`)
    - Inject sidebar, header, and `<router-outlet>` in the template
    - Full viewport height, content area scrolls independently
    - Implement `isMobile` signal, `onNavigation()`, `toggleSidenav()` methods
    - _Requirements: 6.1, 6.2, 6.9, 7.1, 7.2_

  - [x] 5.2 Create `SidebarComponent` in `layout/sidebar/`
    - `mat-nav-list` with 5 menu items in order: Dashboard, Usuários, Tipos de Despesa, Receitas, Despesas
    - Each item has Material icon + label + `routerLink` + `routerLinkActive`
    - Emit `navigated` output event on item click
    - PAGA logo/text at top of sidebar
    - _Requirements: 6.3, 6.4, 6.5, 7.4_

  - [x] 5.3 Create `HeaderComponent` in `layout/header/`
    - `mat-toolbar` with placeholder user name ("Administrador")
    - ThemeToggle control, logout icon button (non-functional)
    - Conditional hamburger menu button visible when `isMobile` input is true
    - Emit `menuToggle` output event on hamburger click
    - _Requirements: 6.6, 6.7, 6.8, 7.3_

  - [x] 5.4 Create `ThemeToggleComponent` in `shared/theme-toggle/`
    - Icon button that calls `ThemeService.toggle()`
    - Shows `dark_mode` icon when theme is `'light'`, `light_mode` when `'dark'`
    - `aria-label` describing the available action
    - _Requirements: 8.1, 8.2, 8.3_

  - [ ]* 5.5 Write property test for responsive sidebar mode
    - **Property 5: Responsive sidebar mode**
    - Verify mode is `'side'` when viewport ≥ 768px and `'over'` when < 768px, as a pure function of width
    - **Validates: Requirements 7.1, 7.2**

  - [ ]* 5.6 Write property test for auto-close on navigation in overlay mode
    - **Property 6: Auto-close on navigation in overlay mode**
    - Verify clicking any nav item in `'over'` mode causes sidebar to close
    - **Validates: Requirements 7.4**

- [x] 6. Implement routing and placeholder components
  - [x] 6.1 Create `PlaceholderComponent` in `shared/placeholder/`
    - Displays "Em construção" message with `construction` icon
    - Standalone component, styled centered on page
    - _Requirements: 2.4_

  - [x] 6.2 Create `DashboardComponent` placeholder in `features/dashboard/`
    - Similar to PlaceholderComponent but distinct component identity at definitive route
    - Title shows "Dashboard" + "Em construção"
    - _Requirements: 2.5_

  - [x] 6.3 Configure `app.routes.ts` with lazy-loaded routes
    - Redirect `''` to `/dashboard`
    - `/dashboard` loads `DashboardComponent` via `loadComponent`
    - `/users`, `/expense-types`, `/incomes`, `/expenses` each loads via `loadChildren` to feature route files
    - No auth guards applied
    - _Requirements: 2.1, 2.2, 2.3, 2.6_

  - [x] 6.4 Create feature route files pointing to PlaceholderComponent
    - `features/users/users.routes.ts` → PlaceholderComponent
    - `features/expense-types/expense-types.routes.ts` → PlaceholderComponent
    - `features/incomes/incomes.routes.ts` → PlaceholderComponent
    - `features/expenses/expenses.routes.ts` → PlaceholderComponent
    - _Requirements: 2.4_

- [x] 7. Wire AppComponent and integrate Shell
  - [x] 7.1 Update `AppComponent` to render `ShellComponent`
    - Minimal template: `<app-shell />`
    - Import `ShellComponent` in standalone imports
    - Remove default Angular boilerplate content
    - _Requirements: 1.1, 6.1_

- [x] 8. Checkpoint - Full build and navigation verification
  - Run `ng build` with zero errors and `ng test --watch=false` with zero failures. Verify all 5 routes render correctly. Ask the user if questions arise.

- [x] 9. Write unit tests for all components and services
  - [x] 9.1 Write unit tests for `ThemeService`
    - Test default theme from system preference (mock `matchMedia`)
    - Test default theme from localStorage
    - Test `toggle()` changes signal value
    - Test localStorage persistence on toggle
    - Test `data-theme` attribute updates on toggle
    - _Requirements: 9.1_

  - [x] 9.2 Write unit tests for `ShellComponent`
    - Verify sidebar, header, and router-outlet are rendered
    - Test sidebar mode changes based on mocked viewport breakpoint
    - _Requirements: 9.2_

  - [x] 9.3 Write unit tests for `SidebarComponent`
    - Verify all 5 menu items are present with correct labels and order
    - Verify `navigated` event emits on item click
    - _Requirements: 9.3_

  - [x] 9.4 Write unit tests for `HeaderComponent`
    - Verify user name is displayed
    - Verify theme toggle control is present
    - Verify logout button is present
    - Verify hamburger menu button visible only when `isMobile` is true
    - _Requirements: 9.2 (header rendered in shell)_

  - [x] 9.5 Write unit tests for `ThemeToggleComponent`
    - Verify icon is `dark_mode` when theme is light
    - Verify icon is `light_mode` when theme is dark
    - Verify clicking calls `ThemeService.toggle()`
    - _Requirements: 9.4_

  - [x] 9.6 Write unit tests for `PlaceholderComponent`
    - Verify "Em construção" text is rendered
    - _Requirements: 2.4_

- [x] 10. Final checkpoint - All tests pass
  - Run `ng build` with zero errors and `ng test --watch=false` with zero failures. Confirm all acceptance criteria from Requirements 1–9 are satisfied. Ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional property-based tests and can be skipped for faster MVP delivery
- The project is scaffolded from scratch; no existing `frontend/` directory
- Auth guards and HTTP interceptors are deferred to mvp-4
- The `DashboardComponent` is a placeholder but lives at its final route so mvp-4 replaces it in-place
- All 5 sidebar items are visible from day one; 3 point to PlaceholderComponent
- ThemeService uses Angular signals and `effect()` for reactive DOM/storage synchronisation
- Windows + PowerShell environment; use `;` as command separator, not `&&`

## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["1.1"] },
    { "id": 1, "tasks": ["1.2", "1.3"] },
    { "id": 2, "tasks": ["2.1", "2.2", "2.3"] },
    { "id": 3, "tasks": ["3.1", "2.4"] },
    { "id": 4, "tasks": ["3.2", "3.3", "3.4", "5.4", "6.1", "6.2"] },
    { "id": 5, "tasks": ["5.1", "5.2", "5.3", "6.3", "6.4"] },
    { "id": 6, "tasks": ["7.1", "5.5", "5.6"] },
    { "id": 7, "tasks": ["9.1", "9.2", "9.3", "9.4", "9.5", "9.6"] }
  ]
}
```
