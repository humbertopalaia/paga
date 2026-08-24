# Implementation Plan: module-dashboard

## Overview

Full-stack vertical slice implementing the financial dashboard (Stories PP-34 + PP-74). The backend
provides a single `GET /api/dashboard?month=YYYY-MM` endpoint with optimized DB aggregations. The
frontend replaces the existing `DashboardComponent` placeholder with a functional implementation
featuring metric cards, an ngx-charts horizontal bar chart, a month selector, and loading/error/empty
states.

## Tasks

- [x] 1. Backend — DTOs and service interface
  - [x] 1.1 Create `DashboardResponse` and `ExpenseByTypeItem` DTOs
    - Create `backend/src/Paga.Application/DTOs/DashboardResponse.cs` with records `DashboardResponse(decimal CurrentBalance, decimal MonthlyIncome, decimal MonthlyExpense, IReadOnlyList<ExpenseByTypeItem> ExpensesByType)` and `ExpenseByTypeItem(string TypeName, decimal Total)`
    - _Requirements: 1.1, 1.6_
  - [x] 1.2 Create `IDashboardService` interface
    - Create `backend/src/Paga.Application/Abstractions/IDashboardService.cs` with method `Task<DashboardResponse> GetAsync(int year, int month, CancellationToken ct = default)`
    - _Requirements: 1.1, 1.2_

- [x] 2. Backend — Service implementation
  - [x] 2.1 Implement `DashboardService`
    - Create `backend/src/Paga.Infrastructure/Services/DashboardService.cs`
    - Inject `PagaDbContext` and `ICurrentUserService`
    - Implement three queries: (1) currentBalance via all-time SUM of incomes minus SUM of expenses, (2) monthlyIncome via SUM filtered by Date within month boundaries, (3) expensesByType via GroupBy ExpenseType.Name with SUM, deriving monthlyExpense from group totals
    - All queries use `AsNoTracking()`, filter by `UserId`, cast to `decimal?` for empty-set safety
    - Compute date boundaries: `firstDay = new DateOnly(year, month, 1)`, `lastDay = firstDay.AddMonths(1).AddDays(-1)`
    - _Requirements: 1.3, 1.4, 1.5, 1.6, 1.7, 1.8, 1.9, 3.1, 3.2, 3.3_

- [x] 3. Backend — Controller and DI
  - [x] 3.1 Create `DashboardController`
    - Create `backend/src/Paga.Api/Controllers/DashboardController.cs`
    - `[Authorize]`, route `api/dashboard`, single `[HttpGet]` action
    - Private `ParseMonth` method: if null/empty → use current UTC month; otherwise validate with regex `^\d{4}-(0[1-9]|1[0-2])$` → 400 ProblemDetails with pt-BR message on failure
    - Call `IDashboardService.GetAsync(year, month, ct)` and return `Ok(result)`
    - _Requirements: 1.1, 1.2, 1.10, 2.1, 2.2, 2.3_
  - [x] 3.2 Register `DashboardService` in DI
    - In `backend/src/Paga.Api/Program.cs`, add `builder.Services.AddScoped<IDashboardService, DashboardService>()`
    - _Requirements: 1.1_

- [x] 4. Backend — Unit tests
  - [x] 4.1 Write unit tests for `DashboardService`
    - Create `backend/tests/Paga.Tests/Unit/DashboardServiceTests.cs`
    - Scenarios: no data (returns zeros and empty array), incomes and expenses in month (correct sums), specific month different from current, multiple expense types in expensesByType, negative balance (expenses > incomes all-time), incomes in different months (only queried month contributes), isolation (other userId data excluded)
    - Use in-memory database or Testcontainers PostgreSQL for EF Core queries
    - _Requirements: 11.1_

- [x] 5. Backend — Integration tests
  - [x] 5.1 Write integration tests for dashboard endpoint
    - Create `backend/tests/Paga.Tests/Integration/DashboardEndpointTests.cs`
    - Use `WebApplicationFactory` with Testcontainers PostgreSQL
    - Scenarios: GET /api/dashboard → 200 with correct shape, GET with `?month=2024-03` → filtered data, GET with `?month=invalid` → 400, no token → 401, user isolation (user A data not in user B's response), month with no data → zeros and empty array
    - _Requirements: 11.2, 11.3, 11.4, 11.5, 11.6, 11.7, 11.8_

- [~] 6. Checkpoint — Backend verification
  - Run `dotnet build` and `dotnet test` from `backend/`. Ensure all tests pass, ask the user if questions arise.

- [x] 7. Frontend — Install ngx-charts and create model/service
  - [x] 7.1 Install ngx-charts dependency
    - Run `npm install @swimlane/ngx-charts` in `frontend/`
    - _Requirements: 6.1_
  - [x] 7.2 Create `dashboard.model.ts`
    - Create `frontend/src/app/features/dashboard/dashboard.model.ts` with interfaces `DashboardResponse` and `ExpenseByType`
    - _Requirements: 1.1_
  - [x] 7.3 Create `DashboardService` (Angular)
    - Create `frontend/src/app/features/dashboard/dashboard.service.ts`
    - `providedIn: 'root'`, inject `HttpClient`, use `environment.apiUrl`
    - Method `getDashboard(month?: string): Observable<DashboardResponse>` with optional `month` HttpParam
    - _Requirements: 7.2, 10.3_

- [x] 8. Frontend — MetricCardComponent (shared)
  - [x] 8.1 Create `MetricCardComponent`
    - Create `frontend/src/app/shared/metric-card/metric-card.component.ts`, `.html`, `.scss`
    - Standalone, OnPush, inputs: `title` (required string), `value` (required number), `valueColor` (required string), `subtitle` (optional string)
    - Format value with `CurrencyPipe` (BRL), apply color via `[style.color]`
    - Styling: `var(--bg-primary)` background, `var(--border)` border, `border-radius: 12px`, `var(--spacing-lg)` padding
    - Must work in both light and dark mode
    - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5, 5.6_

- [x] 9. Frontend — ExpensesChartComponent
  - [x] 9.1 Create `ExpensesChartComponent`
    - Create `frontend/src/app/features/dashboard/expenses-chart/expenses-chart.component.ts`, `.html`, `.scss`
    - Standalone, OnPush, inputs: `expensesByType` (ExpenseByType[]), `monthLabel` (string)
    - Transform data to ngx-charts format `{ name, value }[]`
    - Use `ngx-charts-bar-horizontal` with BRL value formatting and custom blue color scheme
    - Section title: "Despesas por Tipo - [monthLabel]"
    - Wrap in card with white background, border `var(--border)`, border-radius 12px
    - Empty state: "Nenhuma despesa registrada neste mês" when array is empty
    - Responsive width
    - _Requirements: 6.1, 6.2, 6.3, 6.4, 6.5, 6.6_

- [x] 10. Frontend — DashboardComponent (replace placeholder)
  - [x] 10.1 Implement `DashboardComponent`
    - Replace existing placeholder files at `frontend/src/app/features/dashboard/dashboard.component.ts`, `.html`, `.scss`
    - Signals: `loading`, `error`, `data`, `selectedMonth` (default current month YYYY-MM)
    - On init and month change: set loading, call service, update data/error signals
    - Template: `@if (loading())` → skeleton; `@if (error())` → error state with retry button; `@if (data())` → cards + chart
    - Three MetricCardComponents: "Saldo Atual" (blue #3b82f6 positive / red #ef4444 negative, subtitle "Total acumulado"), "Receitas do Mês" (green #10b981, subtitle month name pt-BR), "Despesas do Mês" (red #ef4444, subtitle month name pt-BR)
    - Responsive grid: 3 columns desktop (≥1024px), 1 column mobile (<768px)
    - Month selector: `mat-datepicker` in month mode with `(monthSelected)` event, Portuguese month display
    - Loading skeleton: CSS pulse/shimmer animation, gray bars `var(--border)` with matching dimensions
    - Error state: "Erro ao carregar dados" message + "Tentar Novamente" button that retries
    - Dark mode support via CSS custom properties
    - _Requirements: 4.1–4.9, 7.1–7.5, 8.1–8.5, 9.1–9.4, 10.1–10.3_

- [~] 11. Frontend — Unit tests
  - [-] 11.1 Write tests for `DashboardService`
    - Create `frontend/src/app/features/dashboard/dashboard.service.spec.ts`
    - Test: correct URL and method, month query param passed when provided, no param when month is undefined
    - Use `HttpTestingController`
    - _Requirements: 12.1_
  - [-] 11.2 Write tests for `MetricCardComponent`
    - Create `frontend/src/app/shared/metric-card/metric-card.component.spec.ts`
    - Test: title rendered, value formatted as BRL, color applied, subtitle shown when provided
    - _Requirements: 12.3_
  - [-] 11.3 Write tests for `ExpensesChartComponent`
    - Create `frontend/src/app/features/dashboard/expenses-chart/expenses-chart.component.spec.ts`
    - Test: chart rendered with mock data, empty state shown when array empty, section title with month label
    - _Requirements: 12.4_
  - [x] 11.4 Write tests for `DashboardComponent`
    - Create `frontend/src/app/features/dashboard/dashboard.component.spec.ts`
    - Test: three cards displayed with formatted values, skeleton during loading, error state with retry button, chart rendered when expensesByType has data, empty state when expensesByType empty, month change triggers new request
    - _Requirements: 12.2, 12.5_

- [~] 12. Checkpoint — Frontend verification
  - Run `ng build --configuration production` and `ng test --watch=false` from `frontend/`. Ensure all tests pass, ask the user if questions arise.

- [ ] 13. Backend — Property-based tests (optional)
  - [ ]* 13.1 Write property test for balance invariant
    - **Property 1: Balance equals sum of all incomes minus sum of all expenses**
    - Generate random sets of incomes and expenses, verify currentBalance = Σincomes - Σexpenses regardless of month queried
    - **Validates: Requirements 1.3, 1.8**
  - [ ]* 13.2 Write property test for monthly income filtering
    - **Property 2: Monthly income equals sum of incomes whose Date falls within the queried month**
    - Generate incomes across multiple months, verify monthlyIncome only sums those in the target month
    - **Validates: Requirements 1.4, 1.2**
  - [ ]* 13.3 Write property test for monthly expense filtering
    - **Property 3: Monthly expense equals sum of expenses whose DueDate falls within the queried month**
    - Generate expenses across multiple months, verify monthlyExpense only sums those in the target month
    - **Validates: Requirements 1.5, 1.2**
  - [ ]* 13.4 Write property test for expensesByType grouping integrity
    - **Property 4: Group totals equal per-type sums and overall monthlyExpense**
    - Generate expenses with multiple types in a month, verify each group total and that sum of groups = monthlyExpense
    - **Validates: Requirements 1.6, 1.7**
  - [ ]* 13.5 Write property test for invalid month rejection
    - **Property 5: Invalid month strings produce HTTP 400**
    - Generate random strings that don't match YYYY-MM with valid month, verify 400 response
    - **Validates: Requirements 2.1, 2.2**
  - [ ]* 13.6 Write property test for multi-tenant isolation
    - **Property 6: User data is isolated — other user's data does not affect dashboard**
    - Generate data for two users, verify each user's dashboard only reflects their own data
    - **Validates: Requirements 3.1, 3.2, 3.3**

- [~] 14. Final checkpoint
  - Run `dotnet build`, `dotnet test`, `ng build --configuration production`, and `ng test --watch=false`. Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional property-based tests and can be skipped for faster delivery
- The existing `DashboardComponent` placeholder at `frontend/src/app/features/dashboard/` is replaced in-place (task 10.1)
- `MetricCardComponent` goes in `shared/` for reuse across future modules
- ngx-charts must be installed before implementing the chart component (task 7.1)
- Backend service uses `ICurrentUserService` (already exists) for multi-tenant isolation
- All validation messages in pt-BR per product rules
- Backend PBT uses FsCheck.Xunit; tests target the service layer with Testcontainers PostgreSQL

## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["1.1", "1.2", "7.1"] },
    { "id": 1, "tasks": ["2.1", "7.2", "7.3", "8.1"] },
    { "id": 2, "tasks": ["3.1", "3.2", "9.1"] },
    { "id": 3, "tasks": ["4.1", "5.1", "10.1"] },
    { "id": 4, "tasks": ["11.1", "11.2", "11.3"] },
    { "id": 5, "tasks": ["11.4"] },
    { "id": 6, "tasks": ["13.1", "13.2", "13.3", "13.4", "13.5", "13.6"] }
  ]
}
```
