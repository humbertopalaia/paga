# Implementation Plan: MVP-4 Login and Users UI

## Overview

This plan implements the Login screen, Auth infrastructure (AuthService, interceptor, guard), and User CRUD UI for the PAGA application. The implementation follows a bottom-up dependency order: core models/services first, then shared components, then feature components, and finally route wiring and tests. The existing app already has the shell layout, sidebar, header, theme service, and placeholder routes from MVP-3.

## Tasks

- [x] 1. Create core models and auth infrastructure
  - [x] 1.1 Create shared response models (`PaginatedResponse`, `ProblemDetails`)
    - Create `core/models/paginated-response.model.ts` with the `PaginatedResponse<T>` interface
    - Create `core/models/problem-details.model.ts` with the `ProblemDetails` interface
    - Create `core/models/index.ts` barrel export
    - _Requirements: 8.5, 13.1, 13.2_

  - [x] 1.2 Create auth models and AuthService
    - Create `core/auth/auth.models.ts` with `LoginRequest`, `RefreshRequest`, `TokenResponse` interfaces
    - Create `core/auth/auth.service.ts` implementing token management with in-memory signal for access token, localStorage for refresh token
    - Implement `login()`, `refresh()`, `logout()`, `getAccessToken()`, `getRefreshToken()`, `isAuthenticated` computed signal
    - Implement `isTokenExpired()` private method for JWT expiry check
    - Implement bootstrap logic: attempt silent refresh if refresh token exists on service init
    - Use `environment.apiUrl` for all API base URLs
    - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5, 5.6, 5.7_

  - [x] 1.3 Create Auth Interceptor
    - Create `core/auth/auth.interceptor.ts` as a functional `HttpInterceptorFn`
    - Attach `Authorization: Bearer <token>` to all requests except `/auth/login` and `/auth/refresh`
    - Implement 401 handling: single refresh attempt with request queuing via `BehaviorSubject`
    - On refresh failure, invoke `authService.logout()`
    - _Requirements: 6.1, 6.2, 6.3, 6.4, 6.5, 6.6_

  - [x] 1.4 Create Auth Guard
    - Create `core/auth/auth.guard.ts` as a functional `CanActivateFn`
    - Redirect unauthenticated users to `/login` with `returnUrl` query param
    - Allow authenticated users through
    - _Requirements: 7.1, 7.2, 7.3, 7.4_

- [x] 2. Restructure app configuration and routing
  - [x] 2.1 Update `app.config.ts` to add `provideHttpClient(withInterceptors([authInterceptor]))`
    - Import and register the auth interceptor
    - _Requirements: 15.1_

  - [x] 2.2 Refactor `app.component.ts` and `app.routes.ts` for auth-aware routing
    - Change `AppComponent` template from `<app-shell />` to `<router-outlet />`
    - Restructure `app.routes.ts`: `/login` route outside ShellComponent, all other routes nested inside ShellComponent with `canActivate: [authGuard]`
    - Maintain existing feature routes (dashboard, users, expense-types, incomes, expenses) inside the shell
    - Add wildcard redirect to dashboard
    - _Requirements: 15.2, 15.3, 15.4_

- [x] 3. Implement Login feature
  - [x] 3.1 Create LoginComponent
    - Create `features/auth/login/login.component.ts`, `.html`, `.scss`
    - Logo icon: blue square div (48×48px, bg `#3b82f6`, border-radius 12px) with white bold "P" (26px)
    - "PAGA" heading (28px bold, `#1e293b`) and subtitle "Acompanhamento de Gastos Automatizado" (12px, `#64748b`)
    - Centered card layout
    - Reactive form with email (required, email validator) and password (required) controls
    - Inline error banner (bg `#feeeee`, border `#ef4444`, text `#ef4444`, 14px medium) — shown when `loginError()` signal is set; displays API error message
    - Field-level validation errors (12px, `#ef4444`) below inputs; input border and label turn red on error
    - "Entrar" submit button — changes to "Entrando..." with 16×16 spinner during loading, button bg darkens to `#2563eb`, disabled while loading or form invalid
    - On success: navigate to `returnUrl` or `/dashboard`
    - On 401: set `loginError` signal with API message (inline banner, NOT snackbar)
    - On other error: set `loginError` with generic "Erro ao realizar login. Tente novamente."
    - Restore button state on error
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 1.6, 2.1, 2.2, 2.3, 3.1, 3.2, 3.3, 3.4, 4.1, 4.2, 4.3_

  - [x] 3.2 Create auth feature routes
    - Create `features/auth/auth.routes.ts` exposing `AUTH_ROUTES` with the LoginComponent
    - _Requirements: 15.2_

- [x] 4. Implement shared ConfirmDialogComponent
  - [x] 4.1 Create ConfirmDialogComponent
    - Create `shared/confirm-dialog/confirm-dialog.component.ts`, `.html`, `.scss`
    - Accept `ConfirmDialogData` via `MAT_DIALOG_DATA` (title, message, confirmLabel, cancelLabel, type)
    - `type` field: `'danger' | 'warning' | 'info'` — drives circular icon (48×48px) above the title
    - For `'danger'` type: red/pink background circle with exclamation/trash icon
    - Return `true` on confirm, close without value on cancel
    - Use MatDialogTitle, MatDialogContent, MatDialogActions
    - Trap focus, close on Esc, warn-colored confirm button for danger type
    - _Requirements: 14.1, 14.5_

- [x] 5. Implement User CRUD feature
  - [x] 5.1 Create User models and UserService
    - Create `features/users/user.model.ts` with `User`, `CreateUserRequest`, `UpdateUserRequest` interfaces
    - Create `features/users/user.service.ts` with `getUsers()`, `getUser()`, `createUser()`, `updateUser()`, `deleteUser()` methods
    - Use `environment.apiUrl` and typed `PaginatedResponse<User>` for list
    - _Requirements: 8.6, 11.7, 12.6_

  - [x] 5.2 Create UserListComponent
    - Create `features/users/user-list/user-list.component.ts`, `.html`, `.scss`
    - MatTable with columns: Nome, Email, Data de Criação (formatted `dd/MM/yyyy`), Ações (Editar, Excluir)
    - "Novo Usuário" button with routerLink to `/users/new`
    - Signals for `users`, `totalCount`, `totalPages`, `isLoading`, `error`, `pageNumber`, `pageSize`
    - Single search input field (360px wide, 40px height, placeholder "Buscar por nome ou email...") — `searchFilter` FormControl with `debounceTime(300)` + `distinctUntilChanged()`, sends term as both `name` and `email` query params, resets to page 1
    - Loading skeleton, empty state, error state with retry button
    - Custom numbered pagination buttons (NOT MatPaginator): active page blue bg `#3b82f6` + white text, inactive white bg + border `#e2e8f0` + text `#64748b`, 30×30px, border-radius 6px, font 13px medium
    - _Requirements: 8.1, 8.2, 8.3, 8.4, 8.5, 8.6, 9.1, 9.2, 9.3, 9.4, 10.1, 10.2, 10.3, 10.4_

  - [x] 5.3 Add delete functionality to UserListComponent
    - Open ConfirmDialogComponent on "Excluir" click with `type: 'danger'`, title "Confirmar Exclusão", message "Deseja excluir o usuário {name}? Esta ação não pode ser desfeita."
    - On confirm: call `userService.deleteUser()`, show success snackbar, refresh list
    - On cancel: do nothing
    - On error: show error snackbar
    - _Requirements: 14.1, 14.2, 14.3, 14.4, 14.5_

  - [x] 5.4 Create UserFormComponent (create mode)
    - Create `features/users/user-form/user-form.component.ts`, `.html`, `.scss`
    - Create `features/users/user-form/password-match.validator.ts`
    - Determine mode from route `:id` param presence
    - In create mode: fields Nome, Email, Senha (required, minLength 6), Confirmação de Senha with `passwordMatchValidator` on form group
    - Submit: POST to `/api/users`, success snackbar + navigate to `/users`
    - Error handling: 409 → API message snackbar, 400 → validation messages snackbar
    - "Salvar" and "Cancelar" buttons, submit disabled while invalid/loading
    - _Requirements: 11.1, 11.2, 11.3, 11.4, 11.5, 11.6, 11.7, 11.8, 11.9, 11.10, 13.1, 13.2, 13.3_

  - [x] 5.5 Add edit mode to UserFormComponent
    - In edit mode: fetch user by id, patch name/email into form
    - Only 3 fields: Nome, Email, "Nova Senha (opcional)" with placeholder "Deixe vazio para manter"
    - NO passwordConfirmation field in edit mode; `passwordMatchValidator` NOT applied
    - Password field not required in edit mode
    - Submit: PUT to `/api/users/{id}`, include password only if non-empty
    - Success snackbar + navigate to `/users`
    - _Requirements: 12.1, 12.2, 12.3, 12.4, 12.5, 12.6, 12.7, 13.1, 13.2, 13.3_

  - [x] 5.6 Update users routes file
    - Replace placeholder route in `features/users/users.routes.ts`
    - Define routes: `''` → UserListComponent, `'new'` → UserFormComponent, `':id'` → UserFormComponent
    - _Requirements: 15.4_

- [x] 6. Checkpoint - Build verification
  - Ensure `ng build` passes without errors, ask the user if questions arise.

- [x] 7. Unit tests
  - [x] 7.1 Write AuthService tests
    - Create `core/auth/auth.service.spec.ts`
    - Test login stores both tokens, refresh updates tokens, logout clears and redirects
    - Test `isAuthenticated` reflects token validity
    - Use `HttpTestingController` for HTTP verification
    - _Requirements: 16.1, 16.2, 16.3_

  - [x] 7.2 Write Auth Interceptor tests
    - Create `core/auth/auth.interceptor.spec.ts`
    - Test token attachment to requests, skipping for auth URLs
    - Test 401 triggers refresh and retry
    - Test failed refresh triggers logout
    - Test concurrent request queuing during refresh
    - _Requirements: 16.4, 16.5, 16.6_

  - [x] 7.3 Write Auth Guard tests
    - Create `core/auth/auth.guard.spec.ts`
    - Test unauthenticated redirect with returnUrl
    - Test authenticated pass-through
    - _Requirements: 16.7, 16.8_

  - [x] 7.4 Write LoginComponent tests
    - Create `features/auth/login/login.component.spec.ts`
    - Test form validation (email format, required password)
    - Test successful login navigation (default and returnUrl)
    - Test inline error banner display on 401 (NOT snackbar)
    - Test loading state management ("Entrando..." text, spinner, darker button)
    - _Requirements: 16.9, 16.10, 16.11_

  - [x] 7.5 Write UserService tests
    - Create `features/users/user.service.spec.ts`
    - Test all HTTP methods (GET list with params, GET by id, POST, PUT, DELETE)
    - Verify correct URLs, methods, and params sent
    - _Requirements: 16.12_

  - [x] 7.6 Write UserListComponent tests
    - Create `features/users/user-list/user-list.component.spec.ts`
    - Test data loading and display
    - Test single search field debounce behavior (300ms, distinct, sends both name+email params)
    - Test custom pagination buttons (active state, page change)
    - Test delete flow with confirmation dialog (danger type, user name in message)
    - Test loading, empty, and error states
    - _Requirements: 16.12, 16.13, 16.14_

  - [x] 7.7 Write UserFormComponent tests
    - Create `features/users/user-form/user-form.component.spec.ts`
    - Test create mode: validation rules, password confirmation required, submission with correct payload
    - Test edit mode: data population from API, NO password confirmation field, password optional, correct PUT payload
    - Test password match validation (create mode only)
    - Test error handling (409, 400)
    - _Requirements: 16.15, 16.16_

  - [x] 7.8 Write ConfirmDialogComponent tests
    - Create `shared/confirm-dialog/confirm-dialog.component.spec.ts`
    - Test confirm returns true
    - Test cancel returns undefined/close
    - Test custom labels display
    - Test danger type renders circular icon
    - _Requirements: 14.1, 14.5_

- [x] 8. Final checkpoint - All tests pass
  - Ensure `ng build` and `ng test --watch=false` both pass without errors, ask the user if questions arise.
  - _Requirements: 16.17_

## Notes

- The language is TypeScript/Angular 19 (already determined by the project)
- Each task references specific requirements for traceability
- The implementation builds bottom-up: models → services → components → routing → tests
- Existing placeholder routes and `.gitkeep` files will be replaced as real components are implemented
- The major architectural change is moving from `AppComponent` directly embedding `<app-shell>` to using `<router-outlet>` at the app level, with the shell as a route layout and login outside it
- All components use `ChangeDetectionStrategy.OnPush`, signals, standalone, and `inject()` for DI
- `environment.apiUrl` is used for all HTTP calls — no hardcoded URLs
- `ConfirmDialogComponent` is shared and reusable for all future delete operations (expense-types, incomes, expenses)
- **Login errors use inline banner (not snackbar)** per Figma; CRUD screens continue using MatSnackBar
- **Single search field** replaces separate name/email filters — term sent as both query params
- **Custom pagination** replaces MatPaginator — simple numbered buttons matching Figma specs
- **Password confirmation** is only shown/validated in create mode; edit mode has 3 fields only
- **ConfirmDialog `type` field** drives circular icon display for destructive actions

## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["1.1"] },
    { "id": 1, "tasks": ["1.2"] },
    { "id": 2, "tasks": ["1.3", "1.4"] },
    { "id": 3, "tasks": ["2.1", "2.2", "3.2", "4.1"] },
    { "id": 4, "tasks": ["3.1", "5.1"] },
    { "id": 5, "tasks": ["5.2", "5.4"] },
    { "id": 6, "tasks": ["5.3", "5.5"] },
    { "id": 7, "tasks": ["5.6"] },
    { "id": 8, "tasks": ["7.1", "7.3", "7.5", "7.8"] },
    { "id": 9, "tasks": ["7.2", "7.4", "7.6", "7.7"] }
  ]
}
```
