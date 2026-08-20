# Contrato da API

Contrato compartilhado entre backend e frontend. Alterar aqui antes de alterar código dos dois lados.

Base: `/api`. Todos os endpoints exigem `Authorization: Bearer <accessToken>`, exceto
`POST /api/auth/login`, `POST /api/auth/refresh` e `GET /health`.

## Endpoints

| Método | Rota | Observações |
|--------|------|-------------|
| POST | `/api/auth/login` | `{ email, password }` → `TokenResponse`; 401 em credencial inválida |
| POST | `/api/auth/refresh` | `{ refreshToken }` → novo `TokenResponse` |
| POST | `/api/auth/logout` | revoga o refresh token |
| GET | `/api/users` | filtros: `name`, `email` + paginação |
| GET/POST/PUT/DELETE | `/api/users/{id}` | senha opcional no PUT (reset) |
| GET | `/api/expense-types` | filtro: `name` + paginação |
| GET/POST/PUT/DELETE | `/api/expense-types/{id}` | DELETE retorna 409 se houver despesas vinculadas |
| GET | `/api/incomes` | filtros: `dateFrom`, `dateTo`, `description`, `isRecurring` + paginação |
| GET/POST/PUT/DELETE | `/api/incomes/{id}` | |
| GET | `/api/expenses` | filtros: `dueDateFrom`, `dueDateTo`, `expenseTypeId`, `description`, `isRecurring` + paginação |
| GET/POST/PUT/DELETE | `/api/expenses/{id}` | response inclui `expenseTypeName` |
| GET | `/api/dashboard` | query opcional `month=YYYY-MM` (default: mês corrente) |
| GET | `/health` | 200 |

## Paginação

Request: `?pageNumber=1&pageSize=10`. `pageNumber` começa em 1, `pageSize` default 10, máximo 100.

Response envelope para toda listagem:

```json
{ "items": [], "pageNumber": 1, "pageSize": 10, "totalCount": 0, "totalPages": 0 }
```

## Erros

`ProblemDetails` (RFC 7807) em todas as falhas, produzido por um exception handler global:

```json
{ "type": "...", "title": "Validation failed", "status": 400,
  "errors": { "value": ["O valor deve ser maior que zero."] } }
```

| Status | Quando |
|--------|--------|
| 400 | validação de payload/query |
| 401 | token ausente, inválido ou expirado |
| 403 | recurso de outro usuário |
| 404 | id inexistente (ou pertencente a outro usuário) |
| 409 | conflito de regra (email duplicado, tipo com despesas vinculadas) |
| 500 | erro não tratado (mensagem genérica, detalhe só no log) |

Mensagens de validação e de conflito são exibidas ao usuário, portanto escritas em **pt-BR**.

## Convenções de payload

- JSON camelCase nos dois sentidos.
- Datas: `yyyy-MM-dd` para `date` / `dueDate`; ISO 8601 UTC para timestamps (`createdAt`).
- Valores monetários: `decimal` numérico (nunca string, nunca centavos inteiros). Formatação é
  responsabilidade do frontend.
- `frequency`: `weekly` | `monthly` | `yearly`; `null` quando `isRecurring = false` e
  obrigatório quando `true`.
- `userId` **nunca** aparece em request body — é derivado do token.
- Nenhum response expõe `passwordHash`.

## Contratos principais

```ts
TokenResponse   { accessToken, refreshToken, expiresIn }
User            { id: uuid, name, email, createdAt }
ExpenseType     { id: number, name }
Income          { id, date, description, value, isRecurring, frequency }
Expense         { id, dueDate, description, expenseTypeId, expenseTypeName, value, isRecurring, frequency }
Dashboard       { currentBalance, monthlyIncome, monthlyExpense,
                  expensesByType: [{ typeName, total }] }
```
