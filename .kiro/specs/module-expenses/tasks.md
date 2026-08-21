# Implementation Plan: module-expenses

## Overview

Full vertical slice for Expenses: backend CRUD API (entity update method, DTOs, validators, service with ExpenseType ownership validation and join projection, controller, tests) followed by the Angular frontend module (service, list component with advanced filters including type select and overdue highlight, form component with type select and recurrence, route wiring, tests). The domain entity `Expense` and EF Core configuration already exist — implementation starts with adding the `Update` method to the entity, then builds the Application layer. Shared components (`RecurrenceSelector`, `CurrencyMask`, `ConfirmDialogComponent`) and `ExpenseTypeService` Angular already exist from previous modules — they are reused without modification.

## Tasks

- [x] 1. Backend — Entity Update Method, DTOs, Validators, and Service Interface
  - [x] 1.1 Add `Update` method to `Expense` entity
    - Add public `Update(DateOnly dueDate, string description, int expenseTypeId, decimal value, bool isRecurring, RecurrenceFrequency? frequency)` method to `Paga.Domain` `Expense` entity
    - Method sets all mutable fields for controlled mutation (same pattern as `Income.Update()`)
    - _Requirements: 4.1_
  - [x] 1.2 Create DTOs and filter record for expenses
    - Create `CreateExpenseRequest`, `UpdateExpenseRequest`, `ExpenseResponse`, and `ExpenseFilter` records in `Paga.Application/Expenses/`
    - `ExpenseFilter` includes: `DueDateFrom`, `DueDateTo`, `ExpenseTypeId`, `Description`, `IsRecurring`, `PageNumber`, `PageSize`
    - `ExpenseResponse` includes: `Id`, `DueDate` (string yyyy-MM-dd), `Description`, `ExpenseTypeId`, `ExpenseTypeName`, `Value`, `IsRecurring`, `Frequency`
    - `CreateExpenseRequest` and `UpdateExpenseRequest` include: `DueDate`, `Description`, `ExpenseTypeId`, `Value`, `IsRecurring`, `Frequency`
    - _Requirements: 1.8, 3.1, 4.1_
  - [x] 1.3 Create FluentValidation validators
    - Create `CreateExpenseRequestValidator` and `UpdateExpenseRequestValidator` in `Paga.Application/Expenses/`
    - Rules: `DueDate` not empty ("A data de vencimento é obrigatória."), `Description` not empty ("A descrição é obrigatória.") + max 300 ("A descrição deve ter no máximo 300 caracteres."), `ExpenseTypeId` > 0 ("O tipo de despesa é obrigatório."), `Value` > 0 ("O valor deve ser maior que zero.")
    - When `IsRecurring = true`: `Frequency` not empty ("A frequência é obrigatória para despesas recorrentes.") + must be one of weekly/monthly/yearly ("Frequência inválida. Valores aceitos: weekly, monthly, yearly.")
    - When `IsRecurring = false`: `Frequency` must be null ("A frequência deve ser nula para despesas não recorrentes.")
    - _Requirements: 3.2, 3.3, 3.4, 3.5, 3.7, 3.8, 3.9, 4.3, 4.4, 4.5, 4.6, 4.8, 4.9, 4.10_
  - [x] 1.4 Create `IExpenseService` interface
    - Create in `Paga.Application/Abstractions/`
    - Methods: `GetAllAsync(ExpenseFilter, CancellationToken)`, `GetByIdAsync(int, CancellationToken)`, `CreateAsync(CreateExpenseRequest, CancellationToken)`, `UpdateAsync(int, UpdateExpenseRequest, CancellationToken)`, `DeleteAsync(int, CancellationToken)`
    - Returns `PagedResult<ExpenseResponse>` for list, `ExpenseResponse` for single operations
    - _Requirements: 1.1, 2.1, 3.1, 4.1, 5.1_

- [x] 2. Backend — Service Implementation
  - [x] 2.1 Implement `ExpenseService`
    - Create in `Paga.Infrastructure/Services/`
    - Inject `PagaDbContext` and `ICurrentUserService`
    - `GetAllAsync`: filter by `UserId`, optional `DueDateFrom`/`DueDateTo`/`ExpenseTypeId`/`Description` (case-insensitive contains)/`IsRecurring`, `AsNoTracking`, **join `ExpenseTypes`** to project `ExpenseTypeName` directly in `Select`, order by `DueDate DESC`, apply `ToPagedResultAsync`
    - `GetByIdAsync`: query by `Id` AND `UserId`; join `ExpenseTypes` for `ExpenseTypeName`; throw `NotFoundException` if not found
    - `CreateAsync`: derive `UserId` from `ICurrentUserService`; **validate that `ExpenseTypeId` belongs to authenticated user** (query `ExpenseTypes` table — if not found, throw `ValidationException` with "O tipo de despesa informado não existe ou não pertence ao usuário."); parse `Frequency` string to `RecurrenceFrequency?` enum; create entity; after save, query back with join for `ExpenseTypeName`; return DTO
    - `UpdateAsync`: load by `Id` AND `UserId`; throw `NotFoundException` if missing; **validate that `ExpenseTypeId` belongs to authenticated user** (if changed); call `entity.Update(...)` with parsed values; save; query back with join for `ExpenseTypeName`; return updated DTO
    - `DeleteAsync`: load by `Id` AND `UserId`; throw `NotFoundException` if missing; remove entity
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 1.6, 1.7, 1.8, 1.9, 1.10, 1.12, 2.1, 2.2, 3.1, 3.6, 3.10, 4.1, 4.2, 4.7, 5.1, 5.2, 6.1, 6.2, 6.3, 6.4_
  - [x] 2.2 Register `IExpenseService` in DI
    - Add `builder.Services.AddScoped<IExpenseService, ExpenseService>()` in `Program.cs`
    - _Requirements: 3.1_

- [x] 3. Backend — Controller
  - [x] 3.1 Create `ExpensesController`
    - Create in `Paga.Api/Controllers/`
    - `[ApiController]`, `[Route("api/expenses")]`, `[Authorize]`
    - `GET /` → `GetAll` with query params `dueDateFrom`, `dueDateTo`, `expenseTypeId`, `description`, `isRecurring`, `pageNumber`, `pageSize` → 200
    - `GET /{id:int}` → `GetById` → 200 or 404
    - `POST /` → `Create` → 201 with `CreatedAtAction` + Location header
    - `PUT /{id:int}` → `Update` → 200 or 404
    - `DELETE /{id:int}` → `Delete` → 204 or 404
    - Follow exact same thin-controller pattern as `IncomesController`
    - _Requirements: 1.1, 1.11, 2.1, 2.3, 3.1, 3.11, 4.1, 4.11, 5.1, 5.3_

- [x] 4. Checkpoint — Backend builds
  - Ensure `dotnet build` passes with no errors or warnings. Ask the user if questions arise.

- [x] 5. Backend — Unit Tests
  - [x] 5.1 Create `ExpenseServiceTests`
    - Create in `Paga.Tests/Unit/Expenses/ExpenseServiceTests.cs`
    - Test cases: `GetAllAsync` returns only current user's expenses; filters by `DueDateFrom`; filters by `DueDateTo`; filters by `ExpenseTypeId`; filters by `Description` case-insensitive; filters by `IsRecurring`; orders by `DueDate DESC`; includes `ExpenseTypeName` in response; `GetByIdAsync` returns for own expense with `ExpenseTypeName`; `GetByIdAsync` throws `NotFoundException` for non-existent id; `GetByIdAsync` throws `NotFoundException` for other user's expense; `CreateAsync` creates and returns DTO when valid (includes `expenseTypeName`); `CreateAsync` creates recurring expense with frequency; `CreateAsync` rejects when `ExpenseTypeId` belongs to other user; `CreateAsync` rejects when `ExpenseTypeId` does not exist; `UpdateAsync` updates all fields when valid; `UpdateAsync` throws `NotFoundException` for missing id; `UpdateAsync` throws `NotFoundException` for other user's expense; `UpdateAsync` toggles recurrence from false to true; `UpdateAsync` changes `ExpenseTypeId`; `UpdateAsync` rejects when new `ExpenseTypeId` belongs to other user; `DeleteAsync` deletes when exists for current user; `DeleteAsync` throws `NotFoundException` for missing id; `DeleteAsync` throws `NotFoundException` for other user's expense
    - _Requirements: 15.1_
  - [x] 5.2 Create validator unit tests
    - Create `CreateExpenseRequestValidatorTests` and `UpdateExpenseRequestValidatorTests` in `Paga.Tests/Unit/Expenses/`
    - Tests: fail when dueDate missing, fail when description empty, fail when expenseTypeId zero, fail when value zero, fail when value negative, fail when recurring without frequency, fail when recurring with invalid frequency, fail when not recurring with frequency set, pass when all fields valid non-recurring, pass when all fields valid recurring
    - _Requirements: 15.2_
  - [ ]* 5.3 Write property tests for recurrence validation
    - Create `ExpenseValidationPropertyTests` in `Paga.Tests/Unit/Expenses/`
    - Use FsCheck.Xunit with minimum 100 iterations
    - **Property 1: Recurrence validation consistency** — for any isRecurring=true with null/invalid frequency → validation fails; for any isRecurring=false with non-null frequency → validation fails
    - **Validates: Requirements 3.7, 3.8, 4.8, 4.9**
  - [ ]* 5.4 Write property tests for value positivity
    - Add to `ExpenseValidationPropertyTests`
    - **Property 4: Value positivity invariant** — for any value <= 0 → validation fails; for any value > 0 with valid fields → validation passes
    - **Validates: Requirements 3.4, 4.5**
  - [ ]* 5.5 Write property tests for ExpenseType ownership
    - Add to `ExpenseValidationPropertyTests`
    - **Property 7: ExpenseType ownership validation** — for any expenseTypeId not belonging to authenticated user → service rejects with ValidationException
    - **Validates: Requirements 3.6, 6.4**

- [x] 6. Backend — Integration Tests
  - [x] 6.1 Create `ExpensesEndpointsTests`
    - Create in `Paga.Tests/Integration/Expenses/ExpensesEndpointsTests.cs`
    - Use `WebApplicationFactory` + Testcontainers PostgreSQL (follow `IncomesEndpointsTests` pattern with `IntegrationTestBase`)
    - Tests: POST 201 with valid non-recurring payload (includes `expenseTypeName`); POST 201 with valid recurring payload (frequency=monthly); POST 400 with isRecurring=true and frequency=null; POST 400 with value <= 0; POST 400 with missing description; POST 400 with expenseTypeId not belonging to user; POST 400 with non-existent expenseTypeId; GET 200 paginated list only current user's expenses; GET 200 with dueDateFrom filter; GET 200 with dueDateTo filter; GET 200 with dueDateFrom+dueDateTo range; GET 200 with expenseTypeId filter; GET 200 with description filter case-insensitive; GET 200 with isRecurring filter; GET by ID 200 for own expense (with `expenseTypeName`); GET by ID 404 for other user's expense; GET by ID 404 non-existent; PUT 200 with updated fields (including changed type); PUT 200 toggling isRecurring from false to true; PUT 404 for other user's expense; PUT 400 with expenseTypeId belonging to other user; PUT 400 with isRecurring=false and frequency=monthly; DELETE 204 for own expense; DELETE 404 for other user's expense; requests without token 401; isolation: user A cannot read, update, or delete user B's expense; referential integrity: DELETE /api/expense-types/{id} returns 409 when type has linked expenses; referential integrity: DELETE /api/expense-types/{id} returns 204 when type has no expenses
    - _Requirements: 15.3, 15.4, 15.5, 15.6, 15.7, 15.8, 15.9, 15.10, 15.11, 15.12, 15.13_

- [x] 7. Checkpoint — Backend tests pass
  - Ensure `dotnet build` and `dotnet test` pass with no failures. Ask the user if questions arise.

- [x] 8. Frontend — Model and Service
  - [x] 8.1 Create expense model interfaces
    - Create `frontend/src/app/features/expenses/expense.model.ts`
    - Interfaces: `Expense` (id, dueDate, description, expenseTypeId, expenseTypeName, value, isRecurring, frequency), `CreateExpenseRequest`, `UpdateExpenseRequest`, `ExpenseListParams` (dueDateFrom?, dueDateTo?, expenseTypeId?, description?, isRecurring?, pageNumber, pageSize)
    - _Requirements: 8.1, 9.1, 10.1_
  - [x] 8.2 Create `ExpenseService`
    - Create `frontend/src/app/features/expenses/expense.service.ts`
    - Methods: `getExpenses(params)`, `getExpense(id)`, `createExpense(data)`, `updateExpense(id, data)`, `deleteExpense(id)`
    - Follow same pattern as `IncomeService` — `inject(HttpClient)`, `environment.apiUrl`, typed observables
    - Query params correctly serialize date filters as `yyyy-MM-dd` strings, `expenseTypeId` as number
    - _Requirements: 8.1, 9.6, 10.5, 11.2_

- [x] 9. Frontend — List Component
  - [x] 9.1 Create `ExpenseListComponent`
    - Create `frontend/src/app/features/expenses/expense-list/` (`.ts`, `.html`, `.scss`, `.spec.ts`)
    - Standalone component with `OnPush` change detection, signals for state management
    - Table columns: Vencimento, Descrição, Tipo, Valor, Recorrente, Ações (Editar, Excluir)
    - Filter bar: Vencimento de (datepicker), Vencimento até (datepicker), Tipo (mat-select loaded from `ExpenseTypeService.getExpenseTypes()`), Descrição (FormControl + `debounceTime(300)` + `distinctUntilChanged`), Recorrente (select: Todos/Sim/Não)
    - Any filter change resets pagination to page 1
    - DueDate formatted as `dd/MM/yyyy`; Value formatted as `R$ 1.234,56` with danger color (#ef4444)
    - ExpenseTypeName displayed in Tipo column
    - Recurrence displayed as "Sim" / "Não"
    - **Overdue highlight:** when `dueDate < today` → row background `#fef6f6`, date text color `#ef4444`
    - `isOverdue(expense)` method: compares date-only (no time component)
    - "+ Nova Despesa" primary button navigating to `/expenses/new`
    - Edit/Delete action buttons per row
    - Delete: opens `ConfirmDialogComponent` with title "Confirmar Exclusão" and message "Deseja excluir a despesa \"{descrição}\"? Esta ação não pode ser desfeita."
    - On delete confirm: call API, snackbar feedback, reload list
    - States: loading skeleton, empty state ("Nenhum registro encontrado"), error state ("Erro ao carregar dados" + "Tentar Novamente")
    - Pagination synchronized with API (pageNumber, pageSize, totalPages)
    - Expense types loaded on init from `ExpenseTypeService` for filter select
    - _Requirements: 8.1, 8.2, 8.3, 8.4, 8.5, 8.6, 8.7, 8.8, 8.9, 8.10, 8.11, 8.12, 8.13, 8.14, 11.1, 11.2, 11.3, 11.4, 11.5, 12.1, 12.2, 12.3, 12.4, 13.1, 13.2, 13.4_

- [x] 10. Frontend — Form Component
  - [x] 10.1 Create `ExpenseFormComponent`
    - Create `frontend/src/app/features/expenses/expense-form/` (`.ts`, `.html`, `.scss`, `.spec.ts`)
    - Single component for create and edit mode, derived from route param `:id`
    - Create mode: title "Nova Despesa", empty form
    - Edit mode: title "Editar Despesa", load via `getExpense(id)` → populate form (including pre-selecting `expenseTypeId`); navigate back with error snackbar on 404
    - Reactive form with: `dueDate` (datepicker, required), `description` (text input, required), `expenseTypeId` (mat-select loaded from `ExpenseTypeService`, required), `value` (CurrencyMask directive, required, > 0), `recurrence` (RecurrenceSelector CVA)
    - On init: loads expense types from `ExpenseTypeService` for the select; shows error state if load fails
    - Save button disabled while form invalid or request in progress
    - On success: snackbar + navigate to list
    - On error (400/500): API message in snackbar
    - Cancel: navigate back without request
    - _Requirements: 9.1, 9.2, 9.3, 9.4, 9.5, 9.6, 9.7, 9.8, 9.9, 9.10, 9.11, 9.12, 10.1, 10.2, 10.3, 10.4, 10.5, 10.6, 10.7, 10.8, 13.1, 13.2_

- [x] 11. Frontend — Routes and Navigation Wiring
  - [x] 11.1 Create/update `expenses.routes.ts`
    - Configure routes: `''` → `ExpenseListComponent`, `'new'` → `ExpenseFormComponent`, `':id/edit'` → `ExpenseFormComponent`
    - Replace placeholder "Em construção" route with functional module
    - Remove import of `PlaceholderComponent` if present
    - _Requirements: 14.1, 14.2, 14.3_

- [x] 12. Checkpoint — Frontend builds
  - Ensure `ng build` passes with no errors. Ask the user if questions arise.

- [x] 13. Frontend — Unit Tests
  - [x] 13.1 Write unit tests for `ExpenseService`
    - Create/complete `expense.service.spec.ts`
    - Test correct HTTP method, URL, and params for each operation (getExpenses with all filter combinations including `expenseTypeId`, getExpense, createExpense, updateExpense, deleteExpense)
    - Verify date params serialized as `yyyy-MM-dd` strings
    - Use `HttpTestingController`
    - _Requirements: 16.1_
  - [x] 13.2 Write unit tests for `ExpenseListComponent`
    - Complete `expense-list.component.spec.ts`
    - Tests: renders table with correct columns (Vencimento, Descrição, Tipo, Valor, Recorrente); dueDate formatted dd/MM/yyyy; value formatted R$ with danger color (#ef4444); expenseTypeName displayed in Tipo column; recurrence shows Sim/Não; description filter triggers with 300ms debounce; date pickers trigger API call and reset pagination; expenseTypeId select triggers API call; isRecurring select triggers API call; expense types loaded on init for filter select; displays loading skeleton; displays empty state; displays error state with retry button; edit button navigates to `:id/edit`; delete opens confirm dialog; on confirm calls API and shows snackbar; "+ Nova Despesa" button navigates to `/expenses/new`; overdue rows get background #fef6f6; overdue date text gets color #ef4444; non-overdue rows have default styling
    - _Requirements: 16.2_
  - [x] 13.3 Write unit tests for `ExpenseFormComponent`
    - Complete `expense-form.component.spec.ts`
    - Tests: create mode shows "Nova Despesa" title and empty form; edit mode loads data and pre-fills form (including pre-selected type); edit mode navigates back on 404 with error snackbar; validates dueDate required; validates description required; validates expenseTypeId required (select must have value); validates value required and > 0; RecurrenceSelector integration (toggle shows/hides frequency); loads expense types on init for type select; shows error state when expense type loading fails; submit calls correct API method (POST for create, PUT for edit); disables save button during submission; shows success snackbar and navigates on success; shows API error on 400/500; cancel navigates back without API call
    - _Requirements: 16.3_
  - [x] 13.4 Write unit tests for overdue detection
    - Add dedicated `describe` block in `expense-list.component.spec.ts`
    - Tests: `isOverdue` returns true for dueDate yesterday; `isOverdue` returns false for dueDate today; `isOverdue` returns false for dueDate tomorrow; correct CSS class applied to overdue rows; no CSS class applied to non-overdue rows
    - _Requirements: 16.4_
  - [ ]* 13.5 Write property test for overdue detection
    - **Property 8: Overdue detection correctness** — for any date string in yyyy-MM-dd format, `isOverdue` returns true if and only if the parsed date is strictly before today (date-only comparison)
    - **Validates: Requirements 12.1, 12.2, 12.3, 12.4**

- [x] 14. Final Checkpoint — All tests pass
  - Ensure `dotnet build`, `dotnet test`, `ng build`, and `ng test --watch=false` all pass without failures. Ask the user if questions arise.

## Notes

- The `Expense` entity and EF Core configuration (with `RecurrenceFrequencyConverter`, FK Restrict to `ExpenseType`, and composite index `(UserId, DueDate)`) already exist — no migration needed
- `RecurrenceSelectorComponent` already exists in `shared/` from module-incomes — reuse without modification
- `CurrencyMaskDirective` already exists in `shared/` from module-incomes — reuse without modification
- `ConfirmDialogComponent` already exists in `shared/` from the Users module — reuse without modification
- `ExpenseTypeService` Angular already exists from module-expense-types — reuse for type select and filter
- The `GlobalExceptionHandler` already maps `NotFoundException` → 404 and `ConflictException` → 409 — no new exception types needed
- `FluentValidationFilter` already handles validation pipeline — validators are auto-registered by assembly scanning
- `PagedResult<T>` and `ToPagedResultAsync` already exist in `Paga.Application/Common/`
- Integration tests use the existing `IntegrationTestBase` class with Testcontainers PostgreSQL
- Frontend `PaginatedResponse<T>` is already defined in `core/models`
- The route in `app.routes.ts` already points to `expenses` with lazy loading — only the inner routes need updating
- ExpenseType ownership validation is a cross-entity check unique to this module (not present in Incomes)
- Overdue detection (`dueDate < today`) is a frontend-only computation using browser date — no backend field needed
- The `409 Conflict` scenario for deleting expense types with linked expenses can now be tested end-to-end
- Tasks marked with `*` are optional property-based tests and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation
- Property tests validate universal correctness properties using FsCheck.Xunit (backend) and generated date inputs (frontend)

## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["1.1", "1.2", "1.4", "8.1"] },
    { "id": 1, "tasks": ["1.3", "2.1", "8.2"] },
    { "id": 2, "tasks": ["2.2", "5.2"] },
    { "id": 3, "tasks": ["3.1", "5.1"] },
    { "id": 4, "tasks": ["5.3", "5.4", "5.5", "6.1", "9.1"] },
    { "id": 5, "tasks": ["10.1"] },
    { "id": 6, "tasks": ["11.1"] },
    { "id": 7, "tasks": ["13.1", "13.4", "13.5"] },
    { "id": 8, "tasks": ["13.2", "13.3"] }
  ]
}
```
