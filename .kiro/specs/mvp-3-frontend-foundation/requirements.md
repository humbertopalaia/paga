# Requirements Document

## Introduction

This specification covers the frontend foundation for the PAGA application: Angular 19 project scaffolding (Story 3.1), the dark/light theme system with design tokens (Story 3.2), and the main layout shell with responsive sidebar navigation (Story 3.3). Together these deliver a working SPA skeleton with stable navigation, theming, and placeholder routes for all business modules.

## Glossary

- **Frontend_App**: The Angular 19 single-page application that serves the PAGA user interface.
- **ThemeService**: An Angular injectable service that manages the active visual theme (light or dark) using Angular signals.
- **Shell**: The top-level layout component that wraps the sidebar, header, and routed content area.
- **Sidebar**: The lateral navigation panel built with Angular Material `mat-sidenav`, containing menu items for all application modules.
- **Header**: The top bar displaying the logged-in user name, theme toggle control, and logout button.
- **ThemeToggle**: A UI control (icon button) that switches between light and dark modes.
- **DesignTokens**: CSS custom properties defining colours, typography, and spacing, declared in SCSS files under `src/app/styles/`.
- **Placeholder**: A minimal component displaying an "Em construção" message, used for routes whose features are not yet implemented.
- **LazyLoading**: Angular routing strategy where feature modules are loaded on demand via `loadChildren` or `loadComponent`.

## Requirements

### Requirement 1: Project Scaffolding

**User Story:** As a developer, I want the Angular 19 project created with a standard folder structure and build tooling, so that all team members share a consistent development baseline.

#### Acceptance Criteria

1. THE Frontend_App SHALL be an Angular 19 project using standalone components by default.
2. THE Frontend_App SHALL contain the folder structure `src/app/core/`, `src/app/shared/`, `src/app/features/`, `src/app/layout/`, and `src/app/styles/`.
3. THE Frontend_App SHALL contain feature folders `features/auth/`, `features/dashboard/`, `features/users/`, `features/expense-types/`, `features/incomes/`, and `features/expenses/`.
4. THE Frontend_App SHALL produce a successful build when `ng build` is executed with zero errors.
5. THE Frontend_App SHALL pass all unit tests when `ng test --watch=false` is executed with zero failures.
6. THE Frontend_App SHALL include Angular Material (`@angular/material`) as a configured dependency with a custom theme.
7. THE Frontend_App SHALL use SCSS as the stylesheet preprocessor.
8. THE Frontend_App SHALL define environment files (`environment.ts` and `environment.production.ts`) where `apiUrl` equals the local API port in development and `'/api'` in production.

### Requirement 2: Routing with Lazy Loading

**User Story:** As a developer, I want feature routes loaded on demand, so that initial bundle size stays small and the navigation structure is stable from the start.

#### Acceptance Criteria

1. THE Frontend_App SHALL configure routing with lazy loading per feature using `loadChildren` or `loadComponent`.
2. THE Frontend_App SHALL register routes for all five sidebar destinations: Dashboard (`/dashboard`), Usuários (`/users`), Tipos de Despesa (`/expense-types`), Receitas (`/incomes`), and Despesas (`/expenses`).
3. THE Frontend_App SHALL redirect the empty path (`/`) to `/dashboard`.
4. WHEN a user navigates to `/expense-types`, `/incomes`, or `/expenses`, THE Frontend_App SHALL render a Placeholder component displaying the text "Em construção".
5. WHEN a user navigates to `/dashboard`, THE Frontend_App SHALL render the DashboardComponent placeholder on its definitive route.
6. THE Frontend_App SHALL NOT apply any authentication guard to routes in this delivery.

### Requirement 3: Design Tokens

**User Story:** As a developer, I want a single source of truth for colours, typography, and spacing declared as CSS custom properties, so that all components reference consistent visual values.

#### Acceptance Criteria

1. THE Frontend_App SHALL define CSS custom properties for the light theme in the `:root` selector inside `src/app/styles/_tokens.scss`.
2. THE Frontend_App SHALL define CSS custom properties for the dark theme in the `[data-theme="dark"]` selector inside `src/app/styles/_tokens.scss`.
3. THE Frontend_App SHALL use identical variable names in both light and dark theme declarations.
4. THE Frontend_App SHALL declare the full blue palette (`--primary-50` through `--primary-900`) with values matching the Figma specification for each theme.
5. THE Frontend_App SHALL declare background tokens (`--bg-primary`, `--bg-secondary`, `--bg-tertiary`), text tokens (`--text-primary`, `--text-secondary`, `--text-muted`), border token (`--border`), and semantic tokens (`--success`, `--danger`, `--warning`) for each theme.
6. THE Frontend_App SHALL declare spacing tokens (`--spacing-xs` through `--spacing-2xl`) based on a 4px grid.
7. THE Frontend_App SHALL use the Inter font family for all typography, with sizes ranging from 12px to 32px and weights from 400 to 700 as specified in the Figma design tokens.

### Requirement 4: Theme Service

**User Story:** As a user, I want the application to respect my system colour preference on first access and remember my choice afterward, so that I always see my preferred visual mode.

#### Acceptance Criteria

1. THE ThemeService SHALL expose the current theme state via an Angular signal with values `'light'` or `'dark'`.
2. THE ThemeService SHALL provide a `toggle()` method that switches the theme between light and dark.
3. WHEN the application loads for the first time and no preference exists in localStorage, THE ThemeService SHALL read the operating system preference via `prefers-color-scheme` media query and apply the matching theme.
4. WHEN the user toggles the theme, THE ThemeService SHALL persist the new preference to localStorage under a defined key.
5. WHEN the application loads and a preference exists in localStorage, THE ThemeService SHALL apply the stored preference regardless of the system setting.
6. THE ThemeService SHALL set a `data-theme` attribute on the document root element (`<html>`) reflecting the active theme value.
7. THE Frontend_App SHALL apply a CSS transition on `background-color` and `color` properties to produce a smooth visual animation when the theme changes.

### Requirement 5: Angular Material Custom Theme

**User Story:** As a developer, I want Angular Material components styled to match our design tokens, so that the UI is visually consistent without per-component overrides.

#### Acceptance Criteria

1. THE Frontend_App SHALL configure an Angular Material custom theme (in `src/app/styles/_themes.scss`) that references the blue palette defined in DesignTokens.
2. THE Frontend_App SHALL define both a light and a dark Material palette so that Material components adapt when the theme changes.
3. THE Frontend_App SHALL use the Inter font as the default typography for Angular Material components.

### Requirement 6: Shell Layout

**User Story:** As a user, I want a persistent layout with a sidebar for navigation and a header with user info, so that I can access all sections of the application from any page.

#### Acceptance Criteria

1. THE Shell SHALL render a Sidebar, a Header, and a content area containing a `<router-outlet>`.
2. THE Shell SHALL occupy the full viewport height without page-level scrolling (content scrolls independently).
3. THE Sidebar SHALL display five navigation items in this order: Dashboard, Usuários, Tipos de Despesa, Receitas, Despesas.
4. THE Sidebar SHALL display a Material icon alongside the label for each navigation item.
5. THE Sidebar SHALL visually indicate the currently active route.
6. THE Header SHALL display the logged-in user name (placeholder text acceptable until auth integration in mvp-4).
7. THE Header SHALL contain the ThemeToggle control.
8. THE Header SHALL contain a logout button (non-functional until auth integration in mvp-4).
9. THE Shell SHALL render consistently in both light and dark modes, using DesignTokens for all colour values.

### Requirement 7: Responsive Sidebar

**User Story:** As a user on a mobile device, I want the sidebar to overlay the content instead of pushing it, so that I have full use of the available screen width.

#### Acceptance Criteria

1. WHILE the viewport width is greater than or equal to 768px, THE Sidebar SHALL render in `side` mode (permanently visible alongside content).
2. WHILE the viewport width is less than 768px, THE Sidebar SHALL render in `over` mode (hidden by default, overlays content when opened).
3. WHEN the viewport width is less than 768px, THE Header SHALL display a menu icon button that opens the Sidebar.
4. WHEN a navigation item is selected and the Sidebar is in `over` mode, THE Sidebar SHALL close automatically.

### Requirement 8: Theme Toggle Control

**User Story:** As a user, I want a clearly visible toggle in the header to switch between light and dark mode with immediate visual feedback.

#### Acceptance Criteria

1. THE ThemeToggle SHALL display a sun icon when the current theme is dark (indicating a switch to light is available).
2. THE ThemeToggle SHALL display a moon icon when the current theme is light (indicating a switch to dark is available).
3. WHEN the user activates the ThemeToggle, THE ThemeToggle SHALL invoke ThemeService.toggle() and the entire UI SHALL transition smoothly to the new theme.

### Requirement 9: Unit Tests

**User Story:** As a developer, I want unit tests for the theme service and layout components, so that regressions are caught automatically.

#### Acceptance Criteria

1. THE Frontend_App SHALL include unit tests for ThemeService verifying: default theme detection from system preference, toggle behaviour, localStorage persistence, and `data-theme` attribute application.
2. THE Frontend_App SHALL include unit tests for the Shell component verifying that Sidebar, Header, and router-outlet are rendered.
3. THE Frontend_App SHALL include unit tests for the Sidebar component verifying that all five menu items are present.
4. THE Frontend_App SHALL include unit tests for the ThemeToggle verifying icon changes on toggle.
5. WHEN `ng test --watch=false` is executed, THE Frontend_App SHALL report zero test failures for all tests in this delivery.
