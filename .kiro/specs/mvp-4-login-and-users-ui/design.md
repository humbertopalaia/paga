# Design Document

## Overview

This design covers the Login screen, AuthService (token management), Auth Interceptor, Auth Guard, and the User CRUD UI (listing with filters, create/edit form, delete with confirmation) for the PAGA application. It implements Stories PP-69 (Tela de Login) and PP-70 (CRUD Usuários Frontend) from the MVP-4 spec.

The design follows Angular 19 conventions: standalone components, signals, OnPush change detection, functional interceptor/guard, and reactive forms. All HTTP communication is handled through dedicated services. The auth infrastructure is placed in `core/auth/` while the UI components live under `features/auth/` and `features/users/`.

## Architecture

### Component Hierarchy

```
AppComponent
├── LoginComponent (route: /login, outside ShellComponent)
└── ShellComponent (canActivate: authGuard)
    ├── HeaderComponent
    ├── SidebarComponent
    └── <router-outlet>
         ├── DashboardComponent (route: /dashboard)
         ├── UserListComponent (route: /users)
         ├── UserFormComponent (route: /users/new, /users/:id)
         ├── ... (other feature placeholders)
```

### Routing Structure

```typescript
// app.routes.ts
export const routes: Routes = [
  { path: 'login', loadChildren: () => import('./features/auth/auth.routes').then(m => m.AUTH_ROUTES) },
  {
    path: '',
    component: ShellComponent,
    canActivate: [authGuard],
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      { path: 'dashboard', loadComponent: () => import('./features/dashboard/dashboard.component').then(m => m.DashboardComponent) },
      { path: 'users', loadChildren: () => import('./features/users/users.routes').then(m => m.USERS_ROUTES) },
      { path: 'expense-types', loadChildren: () => import('./features/expense-types/expense-types.routes').then(m => m.EXPENSE_TYPES_ROUTES) },
      { path: 'incomes', loadChildren: () => import('./features/incomes/incomes.routes').then(m => m.INCOMES_ROUTES) },
      { path: 'expenses', loadChildren: () => import('./features/expenses/expenses.routes').then(m => m.EXPENSES_ROUTES) },
    ]
  },
  { path: '**', redirectTo: 'dashboard' }
];
```

### Data Flow

```
LoginComponent → AuthService.login() → POST /api/auth/login → TokenResponse
                                       ↓
                        accessToken → in-memory signal
                        refreshToken → localStorage

AuthInterceptor → reads accessToken signal → attaches Bearer header
              → on 401 → AuthService.refresh() → retry original
              → on refresh fail → AuthService.logout()

AuthGuard → reads AuthService.isAuthenticated() → allow/redirect
```

## Components

### LoginComponent

**Location:** `features/auth/login/login.component.ts`

**Responsibility:** Renders the login form, handles validation, calls AuthService, manages loading/error states. Displays API errors as an inline banner (not snackbar).

**Template Structure:**
- Centered card layout (no shell)
- Logo icon: blue square div (48×48px, `background: #3b82f6`, `border-radius: 12px`) containing a white bold "P" (26px)
- "PAGA" heading (28px bold, color `#1e293b`)
- Subtitle "Acompanhamento de Gastos Automatizado" (12px regular, color `#64748b`)
- Inline error banner (conditionally shown): `background: #feeeee`, `border: 1px solid #ef4444`, text color `#ef4444`, font 14px medium — displays API error message (e.g., "Credenciais inválidas")
- Email field (MatFormField + MatInput) — border/label turn red on validation error
- Password field (MatFormField + MatInput, type=password) — border/label turn red on validation error
- Field-level validation errors below inputs (12px, color `#ef4444`)
- "Entrar" submit button (MatButton) — changes to "Entrando..." with 16×16 spinner during loading, background darkens to `#2563eb`

**Signals:**
- `isLoading = signal(false)` — controls button disabled state, text, and spinner
- `loginError = signal<string | null>(null)` — controls inline error banner visibility and message

**Form:**
```typescript
form = new FormGroup({
  email: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.email] }),
  password: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
});
```

**Behavior:**
1. On submit: set `isLoading(true)`, clear `loginError(null)`, call `authService.login(email, password)`
2. On success: navigate to `returnUrl` query param or `/dashboard`
3. On 401 error: set `loginError()` with API message (e.g., "Credenciais inválidas"), restore button
4. On other error: set `loginError('Erro ao realizar login. Tente novamente.')`, restore button

### UserListComponent

**Location:** `features/users/user-list/user-list.component.ts`

**Responsibility:** Displays paginated, filterable table of users with edit/delete actions.

**Signals:**
- `users = signal<User[]>([])` — current page data
- `totalCount = signal(0)` — total records for pagination
- `totalPages = signal(0)` — total pages from API
- `isLoading = signal(false)` — loading state
- `error = signal<string | null>(null)` — error state
- `pageNumber = signal(1)` — current page
- `pageSize = signal(10)` — items per page

**Filter Implementation:**
- Single `searchFilter` FormControl with placeholder "Buscar por nome ou email..."
- `valueChanges` pipe: `debounceTime(300)` → `distinctUntilChanged()` → reset page to 1 → fetch
- The search term is sent as both `name` and `email` query params to the API

**Pagination:**
- Custom pagination buttons rendered directly in the template (no MatPaginator)
- Active page: blue background `#3b82f6`, white text
- Inactive pages: white background, border `#e2e8f0`, text `#64748b`
- Buttons: 30×30px, border-radius 6px, font 13px medium
- Previous/Next disabled at boundaries
- Page numbers generated from `totalPages` signal

**Template Sections:**
1. Single search input field (360px wide, 40px height, placeholder "Buscar por nome ou email...")
2. "Novo Usuário" button (routerLink to /users/new)
3. Loading skeleton (while loading, no error)
4. Error state with retry button (when error, not loading)
5. Empty state message (when loaded, zero items)
6. MatTable with columns [nome, email, createdAt, actions]
7. Custom numbered pagination buttons

**Actions:**
- Edit: navigates to `/users/{id}`
- Delete: opens ConfirmDialogComponent → on confirm: DELETE → refresh list

### UserFormComponent

**Location:** `features/users/user-form/user-form.component.ts`

**Responsibility:** Create or edit a user. Mode determined by presence of `:id` route parameter.

**Signals:**
- `mode = signal<'create' | 'edit'>('create')`
- `isLoading = signal(false)`
- `userId = signal<string | null>(null)`

**Form (create mode):**
```typescript
form = new FormGroup({
  name: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
  email: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.email] }),
  password: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.minLength(6)] }),
  passwordConfirmation: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
}, { validators: [passwordMatchValidator] });
```

**Form (edit mode):**
- Only three fields: `name`, `email`, `password` — NO `passwordConfirmation` field
- `password` has no `Validators.required`, placeholder "Deixe vazio para manter"
- `passwordMatchValidator` is NOT applied in edit mode
- On init: fetch user by id from API, patch name and email into form
- The form is rebuilt/reconfigured when entering edit mode to exclude `passwordConfirmation`

**Behavior:**
- Submit in create: POST `/api/users` with `{ name, email, password }`
- Submit in edit: PUT `/api/users/{id}` with `{ name, email }` (add `password` only if non-empty)
- On success: snackbar "Usuário criado/atualizado com sucesso" + navigate to `/users`
- On 409: snackbar with API message (e.g., "E-mail já está em uso")
- On 400: snackbar with validation errors
- Cancel: navigate to `/users`

### ConfirmDialogComponent

**Location:** `shared/confirm-dialog/confirm-dialog.component.ts`

**Responsibility:** Generic confirmation dialog for destructive actions. Supports an optional top icon to convey severity.

**Input (MAT_DIALOG_DATA):**
```typescript
interface ConfirmDialogData {
  title: string;
  message: string;
  confirmLabel?: string;  // default: "Confirmar"
  cancelLabel?: string;   // default: "Cancelar"
  type?: 'danger' | 'warning' | 'info';  // drives icon color; default: no icon
}
```

**Visual:**
- When `type` is provided, a circular icon (48×48px) is displayed centered above the title:
  - `'danger'`: red/pink background circle with a trash/exclamation SVG icon
  - `'warning'`: amber/yellow background circle
  - `'info'`: blue background circle
- Dialog title (mat-dialog-title)
- Message body (mat-dialog-content)
- Actions: Cancel (mat-button) and Confirm (mat-raised-button, color warn for danger type)

**Output:** `MatDialogRef<ConfirmDialogComponent, boolean>` — returns `true` on confirm, `undefined`/`false` on cancel.

**Template:**
- Optional circular icon (conditionally rendered based on `data.type`)
- Dialog title (mat-dialog-title)
- Message body (mat-dialog-content)
- Actions: Cancel (mat-button) and Confirm (mat-raised-button, color warn)
- Trap focus, close on Esc

**Delete usage example:**
```typescript
this.dialog.open(ConfirmDialogComponent, {
  data: {
    title: 'Confirmar Exclusão',
    message: `Deseja excluir o usuário ${user.name}? Esta ação não pode ser desfeita.`,
    confirmLabel: 'Excluir',
    type: 'danger'
  }
});
```

## Services

### AuthService

**Location:** `core/auth/auth.service.ts`

**Provided in:** `'root'` (singleton)

**State:**
```typescript
private accessTokenSignal = signal<string | null>(null);
private readonly REFRESH_TOKEN_KEY = 'paga-refresh-token';

readonly isAuthenticated = computed(() => {
  const token = this.accessTokenSignal();
  if (!token) return false;
  return !this.isTokenExpired(token);
});
```

**Methods:**

| Method | Description |
|--------|-------------|
| `login(email, password): Observable<void>` | POST `/api/auth/login`, stores tokens |
| `refresh(): Observable<void>` | POST `/api/auth/refresh`, updates tokens |
| `logout(): void` | POST `/api/auth/logout` (fire-and-forget), clears tokens, navigates to `/login` |
| `getAccessToken(): string \| null` | Returns current access token from signal |
| `getRefreshToken(): string \| null` | Reads from localStorage |

**Token Expiry Check:**
```typescript
private isTokenExpired(token: string): boolean {
  try {
    const payload = JSON.parse(atob(token.split('.')[1]));
    return payload.exp * 1000 < Date.now();
  } catch {
    return true;
  }
}
```

**Bootstrap (on app init):**
- If a refresh token exists in localStorage, attempt a silent refresh to restore the session
- If refresh fails, clear localStorage (user will be redirected to login by the guard)

### AuthInterceptor

**Location:** `core/auth/auth.interceptor.ts`

**Type:** `HttpInterceptorFn`

**Logic:**
```typescript
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);

  // Skip token for auth endpoints
  if (req.url.includes('/auth/login') || req.url.includes('/auth/refresh')) {
    return next(req);
  }

  const token = authService.getAccessToken();
  const authReq = token
    ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : req;

  return next(authReq).pipe(
    catchError(error => {
      if (error.status === 401) {
        return handle401(authService, authReq, next);
      }
      return throwError(() => error);
    })
  );
};
```

**401 Handling (queue pattern):**
- Module-level `isRefreshing` flag and `refreshSubject: BehaviorSubject<string | null>`
- If not already refreshing: set flag, call `authService.refresh()`
  - On success: emit new token on `refreshSubject`, retry original
  - On failure: call `authService.logout()`
- If already refreshing: wait for `refreshSubject` to emit, then retry with new token

### UserService

**Location:** `features/users/user.service.ts`

**Provided in:** `'root'`

**Methods:**

| Method | HTTP | Returns |
|--------|------|---------|
| `getUsers(params)` | GET `/api/users` | `Observable<PaginatedResponse<User>>` |
| `getUser(id)` | GET `/api/users/{id}` | `Observable<User>` |
| `createUser(data)` | POST `/api/users` | `Observable<User>` |
| `updateUser(id, data)` | PUT `/api/users/{id}` | `Observable<User>` |
| `deleteUser(id)` | DELETE `/api/users/{id}` | `Observable<void>` |

**Params interface:**
```typescript
interface UserListParams {
  name?: string;
  email?: string;
  pageNumber: number;
  pageSize: number;
}
```

### AuthGuard

**Location:** `core/auth/auth.guard.ts`

**Type:** `CanActivateFn`

```typescript
export const authGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (authService.isAuthenticated()) {
    return true;
  }

  return router.createUrlTree(['/login'], {
    queryParams: { returnUrl: state.url }
  });
};
```

## Interfaces and Models

### Auth Models (`core/auth/auth.models.ts`)

```typescript
export interface LoginRequest {
  email: string;
  password: string;
}

export interface RefreshRequest {
  refreshToken: string;
}

export interface TokenResponse {
  accessToken: string;
  refreshToken: string;
  expiresIn: number;
}
```

### User Models (`features/users/user.model.ts`)

```typescript
export interface User {
  id: string;
  name: string;
  email: string;
  createdAt: string;
}

export interface CreateUserRequest {
  name: string;
  email: string;
  password: string;
}

export interface UpdateUserRequest {
  name: string;
  email: string;
  password?: string;
}
```

### Shared Models (`core/models/`)

```typescript
export interface PaginatedResponse<T> {
  items: T[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface ProblemDetails {
  type?: string;
  title: string;
  status: number;
  errors?: Record<string, string[]>;
}
```

## Error Handling

### Strategy

**Login screen:** API errors (401, network) are displayed as an inline error banner within the login card. This provides a focused, in-context error experience per the Figma design. No snackbar is used on the login screen.

**CRUD screens (User list/form):** Success and error feedback uses MatSnackBar for non-blocking notifications. This applies to user creation, update, deletion success/failure messages.

**Login error banner pattern:**
```typescript
// In LoginComponent
this.authService.login(email, password).subscribe({
  next: () => { /* navigate */ },
  error: (err: HttpErrorResponse) => {
    this.isLoading.set(false);
    if (err.status === 401) {
      const problem = err.error as ProblemDetails;
      this.loginError.set(problem.title || 'Credenciais inválidas');
    } else {
      this.loginError.set('Erro ao realizar login. Tente novamente.');
    }
  }
});
```

**CRUD error pattern (snackbar):**
```typescript
this.userService.createUser(payload).subscribe({
  next: () => {
    this.snackBar.open('Usuário criado com sucesso', 'Fechar', { duration: 3000 });
    this.router.navigate(['/users']);
  },
  error: (err: HttpErrorResponse) => {
    this.isLoading.set(false);
    if (err.status === 409 || err.status === 400) {
      const problem = err.error as ProblemDetails;
      const message = problem.errors
        ? Object.values(problem.errors).flat().join('. ')
        : problem.title;
      this.snackBar.open(message, 'Fechar', { duration: 5000 });
    } else {
      this.snackBar.open('Erro inesperado. Tente novamente.', 'Fechar', { duration: 5000 });
    }
  }
});
```

### Error Mapping

| HTTP Status | Login Screen | CRUD Screens |
|-------------|-------------|--------------|
| 400 | N/A | Show validation messages from `ProblemDetails.errors` via Snackbar |
| 401 | Inline error banner with API message | Interceptor handles: refresh or logout |
| 404 | N/A | Navigate to list with error snackbar "Registro não encontrado" |
| 409 | N/A | Show conflict message from `ProblemDetails.title` via Snackbar |
| 500+ | Inline error banner generic message | Show generic "Erro inesperado. Tente novamente." via Snackbar |

### Token Refresh Error Flow

1. Request receives 401
2. Interceptor attempts refresh (POST `/api/auth/refresh`)
3. If refresh succeeds: retry original request transparently
4. If refresh fails (401/network error): `AuthService.logout()` → clear tokens → redirect to `/login`
5. Concurrent requests during refresh are queued and replayed after

## Application Configuration Changes

### app.config.ts

```typescript
import { ApplicationConfig, provideZoneChangeDetection } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { authInterceptor } from './core/auth/auth.interceptor';
import { routes } from './app.routes';

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes),
    provideAnimationsAsync(),
    provideHttpClient(withInterceptors([authInterceptor])),
  ]
};
```

### Feature Routes

**auth.routes.ts:**
```typescript
export const AUTH_ROUTES: Routes = [
  { path: '', loadComponent: () => import('./login/login.component').then(m => m.LoginComponent) }
];
```

**users.routes.ts:**
```typescript
export const USERS_ROUTES: Routes = [
  { path: '', loadComponent: () => import('./user-list/user-list.component').then(m => m.UserListComponent) },
  { path: 'new', loadComponent: () => import('./user-form/user-form.component').then(m => m.UserFormComponent) },
  { path: ':id', loadComponent: () => import('./user-form/user-form.component').then(m => m.UserFormComponent) },
];
```

## File Structure

```
frontend/src/app/
├── core/
│   ├── auth/
│   │   ├── auth.service.ts
│   │   ├── auth.service.spec.ts
│   │   ├── auth.interceptor.ts
│   │   ├── auth.interceptor.spec.ts
│   │   ├── auth.guard.ts
│   │   ├── auth.guard.spec.ts
│   │   └── auth.models.ts
│   ├── models/
│   │   ├── paginated-response.model.ts
│   │   └── problem-details.model.ts
│   └── theme/
│       └── theme.service.ts (existing)
├── shared/
│   ├── confirm-dialog/
│   │   ├── confirm-dialog.component.ts
│   │   ├── confirm-dialog.component.html
│   │   ├── confirm-dialog.component.scss
│   │   └── confirm-dialog.component.spec.ts
│   ├── placeholder/ (existing)
│   └── theme-toggle/ (existing)
├── features/
│   ├── auth/
│   │   ├── login/
│   │   │   ├── login.component.ts
│   │   │   ├── login.component.html
│   │   │   ├── login.component.scss
│   │   │   └── login.component.spec.ts
│   │   └── auth.routes.ts
│   ├── users/
│   │   ├── user-list/
│   │   │   ├── user-list.component.ts
│   │   │   ├── user-list.component.html
│   │   │   ├── user-list.component.scss
│   │   │   └── user-list.component.spec.ts
│   │   ├── user-form/
│   │   │   ├── user-form.component.ts
│   │   │   ├── user-form.component.html
│   │   │   ├── user-form.component.scss
│   │   │   └── user-form.component.spec.ts
│   │   ├── user.service.ts
│   │   ├── user.service.spec.ts
│   │   ├── user.model.ts
│   │   └── users.routes.ts
│   └── ... (other features unchanged)
└── layout/ (existing, unchanged)
```

## Custom Validator

### passwordMatchValidator

```typescript
// features/users/user-form/password-match.validator.ts
import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

export const passwordMatchValidator: ValidatorFn = (control: AbstractControl): ValidationErrors | null => {
  const password = control.get('password');
  const confirmation = control.get('passwordConfirmation');

  if (!password || !confirmation) return null;
  if (!confirmation.value) return null; // don't trigger until user types

  return password.value === confirmation.value ? null : { passwordMismatch: true };
};
```

**Note:** This validator is only applied to the form in create mode. In edit mode, the `passwordConfirmation` field does not exist and this validator is not registered on the form group.

## Correctness Properties

*A property is a characteristic or behavior that should hold true across all valid executions of a system — essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*

### Property 1: Email validation rejects invalid formats

*For any* string that does not conform to standard email format (e.g., missing `@`, missing domain, whitespace-only), when set as the value of an email FormControl and the control is marked as touched, the control SHALL report a validation error.

**Validates: Requirements 2.1, 11.4**

### Property 2: Invalid form state disables submission

*For any* form state where at least one required field is empty or any validator reports an error, the submit button SHALL be disabled and no HTTP request SHALL be sent on form submission attempt.

**Validates: Requirements 2.3, 13.3**

### Property 3: returnUrl redirect after successful login

*For any* valid relative URL provided as the `returnUrl` query parameter on the `/login` route, after a successful login the application SHALL navigate to that URL instead of the default `/dashboard` route.

**Validates: Requirements 3.4**

### Property 4: isAuthenticated reflects token validity

*For any* state of the AuthService, the `isAuthenticated` computed signal SHALL return `true` if and only if an access token exists in the in-memory signal AND its `exp` claim represents a timestamp in the future.

**Validates: Requirements 5.3**

### Property 5: Interceptor attaches Bearer token to API requests

*For any* outgoing HTTP request whose URL does not contain `/auth/login` or `/auth/refresh`, when a valid access token exists in the AuthService, the interceptor SHALL clone the request with an `Authorization: Bearer <token>` header.

**Validates: Requirements 6.1, 6.2**

### Property 6: Interceptor queues concurrent requests during refresh

*For any* set of N concurrent HTTP requests that all receive a 401 response while a token refresh is already in progress, the interceptor SHALL issue exactly one refresh request and replay all N original requests with the new token after the refresh completes.

**Validates: Requirements 6.5**

### Property 7: Auth guard preserves attempted URL in returnUrl

*For any* protected route URL that an unauthenticated user attempts to access, the Auth_Guard SHALL redirect to `/login` with a `returnUrl` query parameter containing the exact attempted URL.

**Validates: Requirements 7.2**

### Property 8: Date formatting consistency

*For any* valid ISO 8601 date string provided as `createdAt` in a User record, the User_List_Screen SHALL display it formatted as `dd/MM/yyyy` according to pt-BR locale.

**Validates: Requirements 8.2**

### Property 9: Pagination controls reflect API metadata

*For any* Paginated_Response returned by the users API, the custom pagination buttons SHALL display page navigation consistent with `totalCount`, `pageSize`, and `totalPages` — showing the correct current page number and disabling previous/next appropriately at boundaries.

**Validates: Requirements 8.5**

### Property 10: Filter debounce coalesces rapid inputs

*For any* sequence of keystrokes entered within a 300ms window in the search input, the component SHALL emit at most one HTTP request with the final value, not intermediate values.

**Validates: Requirements 9.2**

### Property 11: Password confirmation must match password (create mode only)

*For any* pair of strings `(password, confirmation)` where `password !== confirmation`, the User_Form_Screen in create mode SHALL report a `passwordMismatch` validation error and prevent form submission. In edit mode, no confirmation field exists and this validation does not apply.

**Validates: Requirements 11.6**

### Property 12: Edit mode populates form from fetched data

*For any* User object returned by `GET /api/users/{id}`, when the User_Form_Screen is in edit mode, the form fields `name` and `email` SHALL be populated with the exact values from the API response.

**Validates: Requirements 12.2**

### Property 13: PUT payload conditionally includes password

*For any* form submission in edit mode, the PUT request payload SHALL include the `password` field if and only if the password form control contains a non-empty string value.

**Validates: Requirements 12.6**

### Property 14: Login inline error banner reflects API response

*For any* login attempt that results in a 401 HTTP response, the Login_Screen SHALL render an inline error banner displaying the error message from the API response, and SHALL NOT use MatSnackBar.

**Validates: Requirements 4.1**
