# Requirements Document

## Introduction

This specification covers the Login and Users UI feature for the PAGA application (MVP-4). It encompasses the login screen with full authentication flow (AuthService, JWT interceptor, route guard) and the administrative CRUD interface for user management (listing with filters, create/edit form, delete with confirmation). These features correspond to Jira Stories PP-69 (Tela de Login) and PP-70 (CRUD Usuários Frontend).

## Glossary

- **Login_Screen**: The standalone page (outside the shell layout) where users authenticate with email and password credentials.
- **AuthService**: A root-provided Angular service that manages authentication state, token storage, login, refresh, and logout operations.
- **Auth_Interceptor**: A functional HTTP interceptor that attaches the Bearer token to outgoing requests and handles 401 responses with a single refresh attempt.
- **Auth_Guard**: A functional CanActivateFn route guard that protects all routes except `/login` by verifying authentication state.
- **User_List_Screen**: The page displaying a paginated, filterable table of system users with actions for create, edit, and delete.
- **User_Form_Screen**: A single form component that operates in `create` or `edit` mode based on the presence of a route `:id` parameter.
- **Confirm_Dialog**: The shared `ConfirmDialogComponent` that prompts users for delete confirmation before executing destructive actions.
- **Snackbar**: Angular Material `MatSnackBar` used for displaying success and error feedback messages with auto-dismiss behavior.
- **Inline_Error_Banner**: A styled inline element rendered within the login card (red background `#feeeee`, border `#ef4444`, text `#ef4444`) used to display login API errors.
- **Token_Response**: The API response containing `accessToken`, `refreshToken`, and `expiresIn` fields.
- **Paginated_Response**: The API response envelope containing `items`, `pageNumber`, `pageSize`, `totalCount`, and `totalPages`.

## Requirements

### Requirement 1: Login Screen Layout

**User Story:** As a user, I want a login screen with email and password fields, so that I can authenticate to access the application.

#### Acceptance Criteria

1. THE Login_Screen SHALL display an email input field, a password input field, and a submit button labeled "Entrar".
2. THE Login_Screen SHALL display a logo icon consisting of a blue square (48×48px, background `#3b82f6`, border-radius 12px) containing a white bold letter "P" (26px) above the heading.
3. THE Login_Screen SHALL display "PAGA" as a styled heading (28px bold, color `#1e293b`) below the logo icon.
4. THE Login_Screen SHALL display the subtitle "Acompanhamento de Gastos Automatizado" (12px regular, color `#64748b`) below the "PAGA" heading.
5. THE Login_Screen SHALL render outside the ShellComponent layout without sidebar or header.
6. THE Login_Screen SHALL use Reactive Forms with typed `FormGroup` for the email and password controls.

### Requirement 2: Login Form Validation

**User Story:** As a user, I want immediate validation feedback on the login form, so that I know what corrections are needed before submitting.

#### Acceptance Criteria

1. WHEN the email field is touched and contains an invalid email format, THE Login_Screen SHALL display a validation error message (12px, color `#ef4444`) below the field, and the input border and label SHALL turn red (`#ef4444`).
2. WHEN the password field is touched and is empty, THE Login_Screen SHALL display a validation error message indicating the field is required.
3. WHILE the form is invalid, THE Login_Screen SHALL disable the submit button.

### Requirement 3: Login Submission and Loading State

**User Story:** As a user, I want visual feedback during login submission, so that I know the system is processing my request.

#### Acceptance Criteria

1. WHEN the user submits the login form with valid credentials, THE Login_Screen SHALL change the submit button text from "Entrar" to "Entrando..." and display a spinner icon (16×16px) to the left of the text.
2. WHILE a login request is in progress, THE Login_Screen SHALL disable the submit button and apply a darker background color (`#2563eb`) to prevent duplicate submissions.
3. WHEN the login API returns a successful Token_Response, THE Login_Screen SHALL redirect the user to the Dashboard route.
4. WHEN a `returnUrl` query parameter is present, THE Login_Screen SHALL redirect the user to the specified URL after successful login instead of the Dashboard.

### Requirement 4: Login Error Handling

**User Story:** As a user, I want clear error messages when login fails, so that I can understand what went wrong.

#### Acceptance Criteria

1. WHEN the login API returns a 401 status, THE Login_Screen SHALL display an Inline_Error_Banner at the top of the form card with the API-provided error message (e.g., "Credenciais inválidas").
2. IF an unexpected error occurs during login, THEN THE Login_Screen SHALL display an Inline_Error_Banner with a generic error message "Erro ao realizar login. Tente novamente.".
3. WHEN a login error is displayed, THE Login_Screen SHALL restore the submit button to its interactive state (text "Entrar", original background color).

### Requirement 5: AuthService Token Management

**User Story:** As a developer, I want a centralized auth service managing tokens, so that authentication state is consistent across the application.

#### Acceptance Criteria

1. THE AuthService SHALL store the access token in an in-memory signal (never in localStorage or cookies).
2. THE AuthService SHALL store the refresh token in localStorage.
3. THE AuthService SHALL expose a computed signal indicating whether the user is currently authenticated.
4. WHEN the `login` method is called with valid credentials, THE AuthService SHALL send a POST request to `/api/auth/login` and store both tokens from the Token_Response.
5. WHEN the `refresh` method is called, THE AuthService SHALL send a POST request to `/api/auth/refresh` with the stored refresh token and update both tokens from the response.
6. WHEN the `logout` method is called, THE AuthService SHALL send a POST request to `/api/auth/logout`, clear both tokens, and redirect to the login route.
7. THE AuthService SHALL be provided in root scope as a singleton service.

### Requirement 6: Auth Interceptor

**User Story:** As a developer, I want an HTTP interceptor that automatically handles token attachment and refresh, so that authentication logic is centralized.

#### Acceptance Criteria

1. THE Auth_Interceptor SHALL attach an `Authorization: Bearer <accessToken>` header to every outgoing HTTP request when a valid access token exists.
2. THE Auth_Interceptor SHALL exclude token attachment for requests to `/api/auth/login` and `/api/auth/refresh`.
3. WHEN a request receives a 401 response, THE Auth_Interceptor SHALL attempt a single token refresh using the stored refresh token.
4. WHEN a token refresh succeeds after a 401, THE Auth_Interceptor SHALL retry the original request with the new access token.
5. WHILE a token refresh is in progress, THE Auth_Interceptor SHALL queue concurrent requests and replay them after the refresh completes.
6. IF the token refresh fails, THEN THE Auth_Interceptor SHALL invoke the AuthService logout method.
7. THE Auth_Interceptor SHALL be registered in `app.config.ts` via `provideHttpClient(withInterceptors([...]))`.

### Requirement 7: Auth Guard

**User Story:** As a developer, I want a route guard that prevents unauthenticated access, so that protected pages are only accessible to logged-in users.

#### Acceptance Criteria

1. THE Auth_Guard SHALL be a functional `CanActivateFn` implementation.
2. WHEN an unauthenticated user attempts to access a protected route, THE Auth_Guard SHALL redirect to `/login` with the attempted URL as a `returnUrl` query parameter.
3. WHEN an authenticated user accesses a protected route, THE Auth_Guard SHALL allow navigation to proceed.
4. THE Auth_Guard SHALL protect all routes except the `/login` route.

### Requirement 8: User Listing Screen

**User Story:** As an administrator, I want to see a paginated list of users, so that I can manage system users effectively.

#### Acceptance Criteria

1. THE User_List_Screen SHALL display a table with columns: Nome, Email, and Data de Criação.
2. THE User_List_Screen SHALL format the Data de Criação column using `dd/MM/yyyy` date format.
3. THE User_List_Screen SHALL display a "Novo Usuário" button that navigates to the user creation form.
4. THE User_List_Screen SHALL display "Editar" and "Excluir" action buttons for each row.
5. THE User_List_Screen SHALL display custom numbered pagination buttons (active page: blue background `#3b82f6` with white text; inactive: white background with border `#e2e8f0` and text `#64748b`; 30×30px, border-radius 6px, font 13px medium) reflecting the Paginated_Response metadata.
6. WHEN the screen loads, THE User_List_Screen SHALL fetch the first page of users from `GET /api/users`.

### Requirement 9: User Listing Filters

**User Story:** As an administrator, I want to filter users by name or email, so that I can quickly find specific users.

#### Acceptance Criteria

1. THE User_List_Screen SHALL provide a single search input field with placeholder "Buscar por nome ou email..." (360px wide, 40px height).
2. WHEN the user types in the search field, THE User_List_Screen SHALL apply a 300ms debounce before sending the API request.
3. WHEN the debounced search value differs from the previous value, THE User_List_Screen SHALL send a new request to `GET /api/users` with the search term as both `name` and `email` query parameters and reset to page 1.
4. WHILE a filter request is in progress, THE User_List_Screen SHALL display a loading state.

### Requirement 10: User Listing States

**User Story:** As an administrator, I want clear visual feedback for different list states, so that I understand the current status of the data.

#### Acceptance Criteria

1. WHILE data is loading, THE User_List_Screen SHALL display a loading skeleton placeholder.
2. WHEN the API returns zero results, THE User_List_Screen SHALL display an empty state message.
3. IF an error occurs during data fetch, THEN THE User_List_Screen SHALL display an error state with a retry button.
4. WHEN the retry button is clicked, THE User_List_Screen SHALL re-execute the current query.

### Requirement 11: User Creation Form

**User Story:** As an administrator, I want to create new users, so that I can grant system access to new people.

#### Acceptance Criteria

1. WHEN navigating to the user creation route (no `:id` parameter), THE User_Form_Screen SHALL operate in `create` mode.
2. THE User_Form_Screen in create mode SHALL display fields: Nome, Email, Senha, and Confirmação de Senha.
3. THE User_Form_Screen SHALL validate that Nome is required.
4. THE User_Form_Screen SHALL validate that Email is required and in valid email format.
5. THE User_Form_Screen SHALL validate that Senha is required in create mode.
6. THE User_Form_Screen SHALL validate that Confirmação de Senha matches Senha (only in create mode).
7. WHEN the form is submitted with valid data, THE User_Form_Screen SHALL send a POST request to `/api/users` with `{ name, email, password }`.
8. WHEN user creation succeeds, THE User_Form_Screen SHALL display a success Snackbar and navigate back to the user list.
9. THE User_Form_Screen SHALL display "Salvar" and "Cancelar" buttons.
10. WHEN the "Cancelar" button is clicked, THE User_Form_Screen SHALL navigate back to the user list without saving.

### Requirement 12: User Edit Form

**User Story:** As an administrator, I want to edit existing users, so that I can update their information or reset passwords.

#### Acceptance Criteria

1. WHEN navigating to the user edit route (with `:id` parameter), THE User_Form_Screen SHALL operate in `edit` mode.
2. THE User_Form_Screen in edit mode SHALL fetch the existing user data from `GET /api/users/{id}` and populate the form fields.
3. THE User_Form_Screen in edit mode SHALL display only three fields: Nome, Email, and "Nova Senha (opcional)" with placeholder "Deixe vazio para manter".
4. THE User_Form_Screen in edit mode SHALL NOT display a password confirmation field.
5. THE User_Form_Screen in edit mode SHALL not require the password field.
6. WHEN the form is submitted with valid data, THE User_Form_Screen SHALL send a PUT request to `/api/users/{id}` including the password field only when it has a value.
7. WHEN user update succeeds, THE User_Form_Screen SHALL display a success Snackbar and navigate back to the user list.

### Requirement 13: User Form Error Handling

**User Story:** As an administrator, I want clear feedback when user operations fail, so that I can take corrective action.

#### Acceptance Criteria

1. IF the API returns a 409 conflict (duplicate email), THEN THE User_Form_Screen SHALL display the API error message via Snackbar.
2. IF the API returns a 400 validation error, THEN THE User_Form_Screen SHALL display the API error messages via Snackbar.
3. WHILE a save request is in progress, THE User_Form_Screen SHALL disable the submit button and display a loading indicator.

### Requirement 14: User Deletion

**User Story:** As an administrator, I want to delete users with a confirmation step, so that accidental deletions are prevented.

#### Acceptance Criteria

1. WHEN the "Excluir" button is clicked on a user row, THE User_List_Screen SHALL open the Confirm_Dialog displaying a circular danger icon (48×48px, red/pink background) at the top, the title "Confirmar Exclusão", and a message following the pattern "Deseja excluir o usuário {name}? Esta ação não pode ser desfeita."
2. WHEN the user confirms deletion in the Confirm_Dialog, THE User_List_Screen SHALL send a DELETE request to `/api/users/{id}`.
3. WHEN user deletion succeeds, THE User_List_Screen SHALL display a success Snackbar and refresh the current list page.
4. IF user deletion fails, THEN THE User_List_Screen SHALL display an error message via Snackbar.
5. WHEN the user cancels the Confirm_Dialog, THE User_List_Screen SHALL not send the delete request.

### Requirement 15: Application Configuration

**User Story:** As a developer, I want the application properly configured with HTTP client and interceptors, so that all features work together correctly.

#### Acceptance Criteria

1. THE Application SHALL register `provideHttpClient(withInterceptors([authInterceptor]))` in `app.config.ts`.
2. THE Application SHALL define the `/login` route outside the ShellComponent layout.
3. THE Application SHALL apply the Auth_Guard to all routes except `/login`.
4. THE Application SHALL maintain the existing route structure for dashboard, users, expense-types, incomes, and expenses within the ShellComponent.

### Requirement 16: Unit Tests

**User Story:** As a developer, I want comprehensive unit tests, so that the authentication and user management features are verified.

#### Acceptance Criteria

1. THE AuthService tests SHALL verify login stores both tokens correctly.
2. THE AuthService tests SHALL verify refresh updates tokens correctly.
3. THE AuthService tests SHALL verify logout clears tokens and redirects.
4. THE Auth_Interceptor tests SHALL verify token attachment to outgoing requests.
5. THE Auth_Interceptor tests SHALL verify 401 handling triggers refresh and retry.
6. THE Auth_Interceptor tests SHALL verify failed refresh triggers logout.
7. THE Auth_Guard tests SHALL verify unauthenticated users are redirected to login with returnUrl.
8. THE Auth_Guard tests SHALL verify authenticated users pass through.
9. THE Login_Screen tests SHALL verify form validation rules.
10. THE Login_Screen tests SHALL verify successful login navigation.
11. THE Login_Screen tests SHALL verify inline error banner display on failed login.
12. THE User_List_Screen tests SHALL verify data loading and display.
13. THE User_List_Screen tests SHALL verify single search field debounce behavior.
14. THE User_List_Screen tests SHALL verify delete flow with confirmation dialog.
15. THE User_Form_Screen tests SHALL verify create mode validation and submission (including password confirmation).
16. THE User_Form_Screen tests SHALL verify edit mode data population and submission (without password confirmation).
17. ALL tests SHALL pass when executing `ng test --watch=false`.
