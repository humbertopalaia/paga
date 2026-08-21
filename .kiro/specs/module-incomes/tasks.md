# Implementation Plan: module-incomes

## Overview

Full vertical slice for Incomes: backend CRUD API (entity update method, DTOs, validators, service, controller, tests) followed by the Angular frontend module (shared components RecurrenceSelector and CurrencyMask, service, list component with advanced filters, form component with recurrence and currency mask, route wiring, tests). The domain entity `Income` and EF Core configuration already exist — implementation starts with adding the `Update` method to the entity, then builds the Application layer.

## Tasks

- [x] 1. Backend — Entity Update Method, DTOs, Validators, and Service Interface
  - [x] 1.1 Add `Update` method to `Income` entity
    - Add public `Update(DateOnly date, string description, decimal value, bool isRecurring, RecurrenceFrequency? frequency)` method to `Paga.Domain` `Income` entity
    - Method sets all mutable fields for controlled mutation (same pattern as `ExpenseType.UpdateName()`)
    - _Requirements: 4.1_
  - [x] 1.2 Create DTOs and filter record for incomes
    - Create `CreateIncomeRequest`, `UpdateIncomeRequest`, `IncomeResponse`, and `IncomeFilter` records in `Paga.Application/Incomes/`
    - `IncomeFilter` includes: `DateFrom`, `DateTo`, `Description`, `IsRecurring`, `PageNumber`, `PageSize`
    - `IncomeResponse` includes: `Id`, `Date` (string yyyy-MM-dd), `Description`, `Value`, `IsRecurring`, `Frequency`
    - _Requirements: 1.7, 3.1, 4.1_
  - [x] 1.3 Create FluentValidation validators
    - Create `CreateIncomeRequestValidator` and `UpdateIncomeRequestValidator` in `Paga.Application/Incomes/`
    - Rules: `Date` not empty ("A data é obrigatória."), `Description` not empty ("A descrição é obrigatória.") + max 300 ("A descrição deve ter no máximo 300 caracteres."), `Value` > 0 ("O valor deve ser maior que zero."), conditional `Frequency` rules based on `IsRecurring`
    - When `IsRecurring = true`: `Frequency` not empty ("A frequência é obrigatória para receitas recorrentes.") + must be one of weekly/monthly/yearly ("Frequência inválida. Valores aceitos: weekly, monthly, yearly.")
    - When `IsRecurring = false`: `Frequency` must be null ("A frequência deve ser nula para receitas não recorrentes.")
    - _Requirements: 3.2, 3.3, 3.4, 3.5, 3.6, 3.7, 4.3, 4.4, 4.5, 4.6, 4.7, 4.8_
  - [x] 1.4 Create `IIncomeService` interface
    - Create in `Paga.Application/Abstractions/`
    - Methods: `GetAllAsync(IncomeFilter, CancellationToken)`, `GetByIdAsync(int, CancellationToken)`, `CreateAsync(CreateIncomeRequest, CancellationToken)`, `UpdateAsync(int, UpdateIncomeRequest, CancellationToken)`, `DeleteAsync(int, CancellationToken)`
    - Returns `PagedResult<IncomeResponse>` for list, `IncomeResponse` for single operations
    - _Requirements: 1.1, 2.1, 3.1, 4.1, 5.1_

- [x] 2. Backend — Service Implementation
  - [x] 2.1 Implement `IncomeService`
    - Create in `Paga.Infrastructure/Services/`
    - Inject `PagaDbContext` and `ICurrentUserService`
    - `GetAllAsync`: filter by `UserId`, optional `DateFrom`/`DateTo`/`Description` (case-insensitive contains)/`IsRecurring`, `AsNoTracking`, project to DTO with `Select`, order by `Date DESC`, apply `ToPagedResultAsync`
    - `GetByIdAsync`: query by `Id` AND `UserId`; throw `NotFoundException` if not found
    - `CreateAsync`: derive `UserId` from `ICurrentUserService`; parse `Frequency` string to `RecurrenceFrequency?` enum; create entity and return DTO
    - `UpdateAsync`: load by `Id` AND `UserId`; throw `NotFoundException` if missing; call `entity.Update(...)` with parsed values; save and return updated DTO
    - `DeleteAsync`: load by `Id` AND `UserId`; throw `NotFoundException` if missing; remove entity
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 1.6, 1.7, 1.8, 1.10, 2.1, 2.2, 3.1, 3.8, 4.1, 4.2, 5.1, 5.2, 6.1, 6.2, 6.3_
  - [x] 2.2 Register `IIncomeService` in DI
    - Add `builder.Services.AddScoped<IIncomeService, IncomeService>()` in `Program.cs`
    - _Requirements: 3.1_

- [x] 3. Backend — Controller
  - [x] 3.1 Create `IncomesController`
    - Create in `Paga.Api/Controllers/`
    - `[ApiController]`, `[Route("api/incomes")]`, `[Authorize]`
    - `GET /` → `GetAll` with query params `dateFrom`, `dateTo`, `description`, `isRecurring`, `pageNumber`, `pageSize` → 200
    - `GET /{id:int}` → `GetById` → 200 or 404
    - `POST /` → `Create` → 201 with `CreatedAtAction` + Location header
    - `PUT /{id:int}` → `Update` → 200 or 404
    - `DELETE /{id:int}` → `Delete` → 204 or 404
    - Follow exact same thin-controller pattern as `ExpenseTypesController`
    - _Requirements: 1.1, 1.9, 2.1, 2.3, 3.1, 3.9, 4.1, 4.9, 5.1, 5.3_

- [x] 4. Checkpoint — Backend builds
  - Ensure `dotnet build` passes with no errors or warnings. Ask the user if questions arise.

- [x] 5. Backend — Unit Tests
  - [x] 5.1 Create `IncomeServiceTests`
    - Create in `Paga.Tests/Unit/Incomes/IncomeServiceTests.cs`
    - Test cases: `GetAllAsync` returns only current user's incomes; filters by `DateFrom`; filters by `DateTo`; filters by `Description` case-insensitive; filters by `IsRecurring`; orders by `Date DESC`; `GetByIdAsync` returns for own income; `GetByIdAsync` throws `NotFoundException` for non-existent id; `GetByIdAsync` throws `NotFoundException` for other user's income; `CreateAsync` creates and returns DTO when valid; `CreateAsync` creates recurring income with frequency; `UpdateAsync` updates all fields when valid; `UpdateAsync` throws `NotFoundException` for missing id; `UpdateAsync` throws `NotFoundException` for other user's income; `UpdateAsync` toggles recurrence from false to true; `DeleteAsync` deletes when exists for current user; `DeleteAsync` throws `NotFoundException` for missing id; `DeleteAsync` throws `NotFoundException` for other user's income
    - _Requirements: 15.1_
  - [x] 5.2 Create validator unit tests
    - Create `CreateIncomeRequestValidatorTests` and `UpdateIncomeRequestValidatorTests` in `Paga.Tests/Unit/Incomes/`
    - Tests: fail when date missing, fail when description empty, fail when description exceeds 300 chars, fail when value zero, fail when value negative, fail when recurring without frequency, fail when recurring with invalid frequency, fail when not recurring with frequency set, pass when all fields valid non-recurring, pass when all fields valid recurring
    - _Requirements: 15.2_
  - [ ]* 5.3 Write property tests for recurrence validation
    - Create `IncomeValidationPropertyTests` in `Paga.Tests/Unit/Incomes/`
    - Use FsCheck.Xunit with minimum 100 iterations
    - **Property 1: Recurrence validation consistency** — for any isRecurring=true with null/invalid frequency → validation fails; for any isRecurring=false with non-null frequency → validation fails
    - **Validates: Requirements 3.5, 3.6, 4.6, 4.7**
  - [ ]* 5.4 Write property tests for value positivity
    - Add to `IncomeValidationPropertyTests`
    - **Property 4: Value positivity invariant** — for any value <= 0 → validation fails; for any value > 0 with valid fields → validation passes
    - **Validates: Requirements 3.4, 4.5**

- [x] 6. Backend — Integration Tests
  - [x] 6.1 Create `IncomesEndpointsTests`
    - Create in `Paga.Tests/Integration/Incomes/IncomesEndpointsTests.cs`
    - Use `WebApplicationFactory` + Testcontainers PostgreSQL (follow `ExpenseTypesEndpointsTests` pattern with `IntegrationTestBase`)
    - Tests: POST 201 with valid non-recurring payload; POST 201 with valid recurring payload (frequency=monthly); POST 400 with isRecurring=true and frequency=null; POST 400 with value <= 0; POST 400 with missing description; GET 200 paginated list only current user's incomes; GET 200 with dateFrom filter; GET 200 with dateTo filter; GET 200 with dateFrom+dateTo range; GET 200 with description filter case-insensitive; GET 200 with isRecurring filter; GET by ID 200 for own income; GET by ID 404 for other user's income; GET by ID 404 non-existent; PUT 200 with updated fields; PUT 200 toggling isRecurring from false to true; PUT 404 for other user's income; PUT 400 with isRecurring=false and frequency=monthly; DELETE 204 for own income; DELETE 404 for other user's income; requests without token 401; isolation: user A cannot read, update, or delete user B's income
    - _Requirements: 15.3, 15.4, 15.5, 15.6, 15.7, 15.8, 15.9, 15.10_

- [x] 7. Checkpoint — Backend tests pass
  - Ensure `dotnet build` and `dotnet test` pass with no failures. Ask the user if questions arise.

- [x] 8. Frontend — Shared Components
  - [x] 8.1 Create `RecurrenceSelector` component
    - Create `frontend/src/app/shared/recurrence-selector/` (`.ts`, `.html`, `.scss`, `.spec.ts`)
    - Standalone component implementing `ControlValueAccessor`
    - Encapsulates `mat-slide-toggle` for `isRecurring` + `mat-select` for `frequency` (Semanal/Mensal/Anual)
    - When toggle ON: shows frequency select, frequency required; label styled blue (#3b82f6)
    - When toggle OFF: hides frequency select, emits `{ isRecurring: false, frequency: null }`
    - Emits `{ isRecurring: boolean, frequency: string | null }` as form value
    - _Requirements: 12.1, 12.2, 12.3, 12.4, 12.5, 12.6_
  - [x] 8.2 Create `CurrencyMask` directive
    - Create `frontend/src/app/shared/currency-mask/` (`.ts`, `.spec.ts`)
    - Standalone directive implementing `ControlValueAccessor`
    - Formats display as `R$ 1.234,56` while typing
    - Accepts only digits and decimal separators
    - Exposes raw numeric value to form control
    - Consistent cursor positioning on focus
    - _Requirements: 11.1, 11.2, 11.3, 11.4, 11.5_

- [x] 9. Frontend — Model and Service
  - [x] 9.1 Create income model interfaces
    - Create `frontend/src/app/features/incomes/income.model.ts`
    - Interfaces: `Income` (id, date, description, value, isRecurring, frequency), `CreateIncomeRequest`, `UpdateIncomeRequest`, `IncomeListParams` (dateFrom?, dateTo?, description?, isRecurring?, pageNumber, pageSize)
    - _Requirements: 7.1, 8.1, 9.1_
  - [x] 9.2 Create `IncomeService`
    - Create `frontend/src/app/features/incomes/income.service.ts`
    - Methods: `getIncomes(params)`, `getIncome(id)`, `createIncome(data)`, `updateIncome(id, data)`, `deleteIncome(id)`
    - Follow same pattern as `ExpenseTypeService` — `inject(HttpClient)`, `environment.apiUrl`, typed observables
    - Query params correctly serialize date filters as `yyyy-MM-dd` strings
    - _Requirements: 7.1, 8.6, 9.4, 10.2_

- [x] 10. Frontend — List Component
  - [x] 10.1 Create `IncomeListComponent`
    - Create `frontend/src/app/features/incomes/income-list/` (`.ts`, `.html`, `.scss`, `.spec.ts`)
    - Standalone component with `OnPush` change detection, signals for state management
    - Table columns: Data, Descrição, Valor, Recorrente, Ações (Editar, Excluir)
    - Filter bar: Data de (datepicker), Data até (datepicker), Descrição (FormControl + `debounceTime(300)` + `distinctUntilChanged`), Recorrente (select: Todos/Sim/Não)
    - Any filter change resets pagination to page 1
    - Date formatted as `dd/MM/yyyy`; Value formatted as `R$ 1.234,56` with success color (#10b981)
    - Recurrence displayed as "Sim" / "Não"
    - "+ Nova Receita" primary button navigating to `/incomes/new`
    - Edit/Delete action buttons per row
    - Delete: opens `ConfirmDialogComponent` with title "Confirmar Exclusão" and message "Deseja excluir a receita \"{descrição}\"? Esta ação não pode ser desfeita."
    - On delete confirm: call API, snackbar feedback, reload list
    - States: loading skeleton, empty state ("Nenhum registro encontrado"), error state ("Erro ao carregar dados" + "Tentar Novamente")
    - Pagination synchronized with API (pageNumber, pageSize, totalPages)
    - _Requirements: 7.1, 7.2, 7.3, 7.4, 7.5, 7.6, 7.7, 7.8, 7.9, 7.10, 7.11, 7.12, 10.1, 10.2, 10.3, 10.4, 10.5, 13.1, 13.2, 13.4_

- [x] 11. Frontend — Form Component
  - [x] 11.1 Create `IncomeFormComponent`
    - Create `frontend/src/app/features/incomes/income-form/` (`.ts`, `.html`, `.scss`, `.spec.ts`)
    - Single component for create and edit mode, derived from route param `:id`
    - Create mode: title "Nova Receita", empty form
    - Edit mode: title "Editar Receita", load via `getIncome(id)` → populate form; navigate back with error snackbar on 404
    - Reactive form with: `date` (datepicker, required), `description` (text input, required), `value` (CurrencyMask directive, required, > 0), `recurrence` (RecurrenceSelector CVA)
    - Save button disabled while form invalid or request in progress
    - On success: snackbar + navigate to list
    - On error (400/500): API message in snackbar
    - Cancel: navigate back without request
    - _Requirements: 8.1, 8.2, 8.3, 8.4, 8.5, 8.6, 8.7, 8.8, 8.9, 8.10, 8.11, 9.1, 9.2, 9.3, 9.4, 9.5, 9.6, 9.7, 13.1, 13.2_

- [x] 12. Frontend — Routes and Navigation Wiring
  - [x] 12.1 Create/update `incomes.routes.ts`
    - Configure routes: `''` → `IncomeListComponent`, `'new'` → `IncomeFormComponent`, `':id/edit'` → `IncomeFormComponent`
    - Replace placeholder "Em construção" route with functional module
    - Remove import of `PlaceholderComponent` if present
    - _Requirements: 14.1, 14.2, 14.3_

- [x] 13. Checkpoint — Frontend builds
  - Ensure `ng build` passes with no errors. Ask the user if questions arise.

- [x] 14. Frontend — Unit Tests
  - [x] 14.1 Write unit tests for `IncomeService`
    - Create/complete `income.service.spec.ts`
    - Test correct HTTP method, URL, and params for each operation (getIncomes with all filter combinations, getIncome, createIncome, updateIncome, deleteIncome)
    - Verify date params serialized as `yyyy-MM-dd` strings
    - Use `HttpTestingController`
    - _Requirements: 16.1_
  - [x] 14.2 Write unit tests for `IncomeListComponent`
    - Complete `income-list.component.spec.ts`
    - Tests: renders table with correct columns; date formatted dd/MM/yyyy; value formatted R$ with success color; recurrence shows Sim/Não; description filter triggers with 300ms debounce; date pickers trigger API call and reset pagination; isRecurring select triggers API call; displays loading skeleton; displays empty state; displays error state with retry button; edit button navigates to `:id/edit`; delete opens confirm dialog; on confirm calls API and shows snackbar; "+ Nova Receita" button navigates to `/incomes/new`
    - _Requirements: 16.2_
  - [x] 14.3 Write unit tests for `IncomeFormComponent`
    - Complete `income-form.component.spec.ts`
    - Tests: create mode shows "Nova Receita" title and empty form; edit mode loads data and pre-fills form; edit mode navigates back on 404 with error snackbar; validates date required; validates description required; validates value required and > 0; RecurrenceSelector integration (toggle shows/hides frequency); submit calls correct API method (POST for create, PUT for edit); disables save button during submission; shows success snackbar and navigates on success; shows API error on 400/500; cancel navigates back without API call
    - _Requirements: 16.3_
  - [x] 14.4 Write unit tests for `RecurrenceSelector`
    - Complete `recurrence-selector.component.spec.ts`
    - Tests: toggle ON shows frequency select; toggle OFF hides frequency select; emits correct value when frequency selected; emits `{ isRecurring: false, frequency: null }` when toggle off; frequency required validation when toggle ON; integrates with reactive forms (ControlValueAccessor contract)
    - _Requirements: 16.4_
  - [x] 14.5 Write unit tests for `CurrencyMask`
    - Complete `currency-mask.directive.spec.ts`
    - Tests: formats input "123456" as "R$ 1.234,56"; exposes numeric value to form control; rejects non-numeric characters; consistent cursor position on focus
    - _Requirements: 16.5_

- [x] 15. Final Checkpoint — All tests pass
  - Ensure `dotnet build`, `dotnet test`, `ng build`, and `ng test --watch=false` all pass without failures. Ask the user if questions arise.

## Notes

- The `Income` entity and EF Core configuration (with `RecurrenceFrequencyConverter` and composite index) already exist — no migration needed
- `ConfirmDialogComponent` already exists in `shared/` from the Users module — reuse it
- The `GlobalExceptionHandler` already maps `NotFoundException` → 404 — no new exception types needed
- `FluentValidationFilter` already handles validation pipeline — validators are auto-registered by assembly scanning
- `PagedResult<T>` and `ToPagedResultAsync` already exist in `Paga.Application/Common/`
- Integration tests use the existing `IntegrationTestBase` class with Testcontainers PostgreSQL
- Frontend `PaginatedResponse<T>` is already defined in `core/models`
- The route in `app.routes.ts` already points to `incomes` with lazy loading — only the inner routes need updating
- `RecurrenceSelector` and `CurrencyMask` are designed as shared components for reuse by the future Expenses module
- Tasks marked with `*` are optional property-based tests and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation
- Property tests validate universal correctness properties using FsCheck.Xunit

## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["1.1", "1.2", "1.4", "9.1"] },
    { "id": 1, "tasks": ["1.3", "2.1", "8.1", "8.2"] },
    { "id": 2, "tasks": ["2.2", "5.2", "9.2"] },
    { "id": 3, "tasks": ["3.1", "5.1"] },
    { "id": 4, "tasks": ["5.3", "5.4", "6.1", "10.1"] },
    { "id": 5, "tasks": ["11.1"] },
    { "id": 6, "tasks": ["12.1"] },
    { "id": 7, "tasks": ["14.1", "14.4", "14.5"] },
    { "id": 8, "tasks": ["14.2", "14.3"] }
  ]
}
```
