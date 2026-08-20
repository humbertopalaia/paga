# Implementation Plan: module-expense-types

## Overview

Full vertical slice for Expense Types: backend CRUD API (DTOs, validators, service, controller, tests) followed by the Angular frontend module (service, list component, form component, route wiring, tests). The domain entity and EF Core configuration already exist — implementation starts at the Application layer.

## Tasks

- [x] 1. Backend — DTOs, Validators, and Service Interface
  - [x] 1.1 Create DTOs and filter record for expense types
    - Create `CreateExpenseTypeRequest`, `UpdateExpenseTypeRequest`, `ExpenseTypeResponse`, and `ExpenseTypeFilter` records in `Paga.Application/DTOs/`
    - Follow the same pattern as `CreateUserRequest`/`UserResponse`/`UserFilter`
    - _Requirements: 1.4, 3.1, 4.1_
  - [x] 1.2 Create FluentValidation validators
    - Create `CreateExpenseTypeRequestValidator` and `UpdateExpenseTypeRequestValidator` in `Paga.Application/Validators/`
    - Rules: `Name` not empty with message "O nome é obrigatório.", max length 100 with message "O nome deve ter no máximo 100 caracteres."
    - Validators will be auto-registered by assembly scanning (already configured)
    - _Requirements: 3.2, 3.4, 4.3, 4.5_
  - [x] 1.3 Create `IExpenseTypeService` interface
    - Create in `Paga.Application/Abstractions/`
    - Methods: `GetAllAsync`, `GetByIdAsync`, `CreateAsync`, `UpdateAsync`, `DeleteAsync`
    - All async with `CancellationToken`, returns `PagedResult<ExpenseTypeResponse>` for list
    - _Requirements: 1.1, 2.1, 3.1, 4.1, 5.1_

- [x] 2. Backend — Service Implementation
  - [x] 2.1 Implement `ExpenseTypeService`
    - Create in `Paga.Application/Services/` (or same location as `UserService`)
    - Inject `PagaDbContext` and `ICurrentUserService`
    - `GetAllAsync`: filter by `UserId`, optional case-insensitive `Name` contains, `AsNoTracking`, project to DTO with `Select`, use `ToPagedResultAsync`
    - `GetByIdAsync`: query by `Id` AND `UserId`; throw `NotFoundException` if not found
    - `CreateAsync`: derive `UserId` from `ICurrentUserService`; check case-insensitive name uniqueness; throw `ConflictException("Já existe um tipo de despesa com este nome.")` on duplicate; create entity and return DTO
    - `UpdateAsync`: load by `Id` AND `UserId`; throw `NotFoundException` if missing; check uniqueness excluding current record; throw `ConflictException` on duplicate; update name and save
    - `DeleteAsync`: load by `Id` AND `UserId`; throw `NotFoundException` if missing; check `_context.Expenses.AnyAsync(e => e.ExpenseTypeId == id)`; throw `ConflictException("Não é possível excluir um tipo de despesa que possui despesas vinculadas.")` if linked; remove entity
    - _Requirements: 1.1, 1.2, 1.3, 1.6, 2.1, 2.2, 3.1, 3.3, 3.5, 4.1, 4.2, 4.4, 5.1, 5.2, 5.3, 5.5, 6.1, 6.2, 6.3_
  - [x] 2.2 Register `IExpenseTypeService` in DI
    - Add `builder.Services.AddScoped<IExpenseTypeService, ExpenseTypeService>()` in `Program.cs`
    - _Requirements: 3.1_

- [x] 3. Backend — Controller
  - [x] 3.1 Create `ExpenseTypesController`
    - Create in `Paga.Api/Controllers/`
    - `[ApiController]`, `[Route("api/expense-types")]`, `[Authorize]`
    - `GET /` → `GetAll` with query params `name`, `pageNumber`, `pageSize` → 200
    - `GET /{id:int}` → `GetById` → 200 or 404
    - `POST /` → `Create` → 201 with `CreatedAtAction` + Location header
    - `PUT /{id:int}` → `Update` → 200 or 404/409
    - `DELETE /{id:int}` → `Delete` → 204 or 404/409
    - Follow exact same thin-controller pattern as `UsersController`
    - _Requirements: 1.1, 1.5, 2.1, 2.3, 3.1, 3.6, 4.1, 4.6, 5.1, 5.4_

- [x] 4. Checkpoint — Backend builds and runs
  - Ensure `dotnet build` passes with no errors or warnings. Ask the user if questions arise.

- [x] 5. Backend — Unit Tests
  - [x] 5.1 Create `ExpenseTypeServiceTests`
    - Create in `Paga.Tests/Unit/ExpenseTypeServiceTests.cs`
    - Test cases: `GetAllAsync` returns only current user's types; `GetByIdAsync` returns for own type; `GetByIdAsync` throws `NotFoundException` for non-existent id; `GetByIdAsync` throws `NotFoundException` for other user's type; `CreateAsync` creates and returns DTO when unique; `CreateAsync` throws `ConflictException` when duplicate name; `UpdateAsync` updates when valid; `UpdateAsync` throws `NotFoundException` for missing id; `UpdateAsync` throws `ConflictException` for duplicate name; `DeleteAsync` deletes when no linked expenses; `DeleteAsync` throws `NotFoundException` for missing id; `DeleteAsync` throws `ConflictException` when expenses exist
    - Mock `PagaDbContext` DbSets and `ICurrentUserService`
    - _Requirements: 13.1_
  - [x] 5.2 Create validator unit tests
    - Create in `Paga.Tests/Unit/CreateExpenseTypeRequestValidatorTests.cs` and `UpdateExpenseTypeRequestValidatorTests.cs`
    - Test: fail when name empty, pass when name valid, fail when name exceeds 100 chars
    - _Requirements: 13.2_

- [x] 6. Backend — Integration Tests
  - [x] 6.1 Create `ExpenseTypesEndpointsTests`
    - Create in `Paga.Tests/Integration/ExpenseTypesEndpointsTests.cs`
    - Use `WebApplicationFactory` + Testcontainers PostgreSQL (follow `UsersEndpointsTests` pattern with `IntegrationTestBase`)
    - Tests: POST 201 with valid payload; POST 409 duplicate name same user; POST 201 same name different user (isolation); GET 200 paginated list only current user's types; GET 200 with name filter; GET by ID 200 for own type; GET by ID 404 for other user's type; GET by ID 404 non-existent; PUT 200 updated name; PUT 409 duplicate name; PUT 404 for other user's type; DELETE 204 no linked expenses; DELETE 409 when expenses exist (insert expense directly via DbContext); DELETE 404 for other user's type; requests without token 401
    - _Requirements: 13.3, 13.4, 13.5, 13.6, 13.7, 13.8, 13.9_

- [x] 7. Checkpoint — Backend tests pass
  - Ensure `dotnet build` and `dotnet test` pass with no failures. Ask the user if questions arise.

- [x] 8. Frontend — Model and Service
  - [x] 8.1 Create expense type model interfaces
    - Create `frontend/src/app/features/expense-types/expense-type.model.ts`
    - Interfaces: `ExpenseType` (id, name), `CreateExpenseTypeRequest` (name), `UpdateExpenseTypeRequest` (name), `ExpenseTypeListParams` (name?, pageNumber, pageSize)
    - _Requirements: 7.1, 8.1, 9.1_
  - [x] 8.2 Create `ExpenseTypeService`
    - Create `frontend/src/app/features/expense-types/expense-type.service.ts`
    - Methods: `getExpenseTypes(params)`, `getExpenseType(id)`, `createExpenseType(data)`, `updateExpenseType(id, data)`, `deleteExpenseType(id)`
    - Follow same pattern as `UserService` — `inject(HttpClient)`, `environment.apiUrl`, typed observables
    - _Requirements: 7.1, 8.3, 9.3, 10.2_

- [x] 9. Frontend — List Component
  - [x] 9.1 Create `ExpenseTypeListComponent`
    - Create `frontend/src/app/features/expense-types/expense-type-list/` (`.ts`, `.html`, `.scss`, `.spec.ts`)
    - Standalone component with `OnPush` change detection, signals for state management
    - Table columns: ID, Nome, Ações (Editar, Excluir)
    - Search field with placeholder "Buscar por nome..." using `FormControl` + `debounceTime(300)` + `distinctUntilChanged`
    - "Novo Tipo de Despesa" button navigating to `/expense-types/new`
    - Pagination synchronized with API (pageNumber, pageSize, totalPages)
    - States: loading skeleton, empty state ("Nenhum registro encontrado" + suggestion), error state ("Erro ao carregar dados" + "Tentar Novamente" button)
    - Delete: opens `ConfirmDialogComponent` with title "Confirmar Exclusão" and message "Deseja excluir o tipo \"{nome}\"?"
    - On delete confirm: call API, snackbar feedback, reload list
    - On 409: show API error message in snackbar
    - _Requirements: 7.1, 7.2, 7.3, 7.4, 7.5, 7.6, 7.7, 7.8, 7.9, 10.1, 10.2, 10.3, 10.4, 10.5, 11.1, 11.2, 11.4_

- [x] 10. Frontend — Form Component
  - [x] 10.1 Create `ExpenseTypeFormComponent`
    - Create `frontend/src/app/features/expense-types/expense-type-form/` (`.ts`, `.html`, `.scss`, `.spec.ts`)
    - Single component for create and edit mode, derived from route param `:id`
    - Create mode: title "Novo Tipo de Despesa", empty form, field placeholder "Ex: Alimentação, Transporte..."
    - Edit mode: title "Editar Tipo de Despesa", load via `getExpenseType(id)` and populate; navigate back with error snackbar on 404
    - Reactive form with `name` control, required validator
    - Save button disabled while form invalid or request in progress
    - On success: snackbar + navigate to list
    - On 409: show API error message in snackbar
    - Cancel: navigate back without request
    - _Requirements: 8.1, 8.2, 8.3, 8.4, 8.5, 8.6, 9.1, 9.2, 9.3, 9.4, 9.5, 9.6, 11.1, 11.2, 11.3_

- [x] 11. Frontend — Routes and Navigation Wiring
  - [x] 11.1 Update `expense-types.routes.ts`
    - Replace placeholder route with: `''` → `ExpenseTypeListComponent`, `'new'` → `ExpenseTypeFormComponent`, `':id/edit'` → `ExpenseTypeFormComponent`
    - Remove import of `PlaceholderComponent`
    - _Requirements: 12.1, 12.2, 12.3_

- [x] 12. Checkpoint — Frontend builds
  - Ensure `ng build` passes with no errors. Ask the user if questions arise.

- [x] 13. Frontend — Unit Tests
  - [x] 13.1 Write unit tests for `ExpenseTypeService`
    - Create/complete `expense-type.service.spec.ts`
    - Test correct HTTP method, URL, and params for each operation (getExpenseTypes, getExpenseType, createExpenseType, updateExpenseType, deleteExpenseType)
    - Use `HttpTestingController`
    - _Requirements: 14.1_
  - [x] 13.2 Write unit tests for `ExpenseTypeListComponent`
    - Complete `expense-type-list.component.spec.ts`
    - Tests: renders table with data; search triggers API call with 300ms debounce; displays loading skeleton; displays empty state; displays error state with retry button; edit button navigates to `:id/edit`; delete opens confirm dialog; on confirm calls API and shows snackbar
    - _Requirements: 14.2_
  - [x] 13.3 Write unit tests for `ExpenseTypeFormComponent`
    - Complete `expense-type-form.component.spec.ts`
    - Tests: create mode shows correct title and empty form; edit mode loads data and pre-fills form; edit mode navigates back on 404; validates name required; submit calls correct API method (POST/PUT); disables save during submission; shows success snackbar and navigates on success; shows API error on 409; cancel navigates back without API call
    - _Requirements: 14.3_
  - [x] 13.4 Verify `ConfirmDialogComponent` test coverage
    - Check that existing tests cover: dynamic title/message display, emit on confirm, close on cancel
    - Add any missing tests if needed
    - _Requirements: 14.4_

- [x] 14. Final Checkpoint — All tests pass
  - Ensure `dotnet build`, `dotnet test`, `ng build`, and `ng test --watch=false` all pass without failures. Ask the user if questions arise.

## Notes

- The `ExpenseType` entity and EF Core configuration already exist — no migration needed
- `ConfirmDialogComponent` already exists in `shared/` from the Users module — reuse it
- The `GlobalExceptionHandler` already maps `NotFoundException` → 404 and `ConflictException` → 409
- `FluentValidationFilter` already handles validation pipeline — validators just need to be registered
- `PagedResult<T>` and `ToPagedResultAsync` already exist in `Paga.Application/Common/`
- Integration tests use the existing `IntegrationTestBase` class with Testcontainers PostgreSQL
- Frontend `PaginatedResponse<T>` is already defined in `core/models`
- The route in `app.routes.ts` already points to `expense-types.routes.ts` with lazy loading — only the inner routes need updating

## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["1.1", "1.3", "8.1"] },
    { "id": 1, "tasks": ["1.2", "2.1"] },
    { "id": 2, "tasks": ["2.2", "5.2"] },
    { "id": 3, "tasks": ["3.1", "5.1"] },
    { "id": 4, "tasks": ["6.1", "8.2"] },
    { "id": 5, "tasks": ["9.1"] },
    { "id": 6, "tasks": ["10.1"] },
    { "id": 7, "tasks": ["11.1"] },
    { "id": 8, "tasks": ["13.1", "13.2", "13.3", "13.4"] }
  ]
}
```
