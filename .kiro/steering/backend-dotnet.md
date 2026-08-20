---
inclusion: fileMatch
fileMatchPattern: 'backend/**/*'
---

# Padrões do backend .NET

## Camadas e fluxo

`Controller` → `IXxxService` (Application) → `DbContext`/repository (Infrastructure) → `Domain`.
Controller só faz: receber DTO, chamar service, mapear resultado para status HTTP. Zero regra
de negócio, zero LINQ, zero acesso a `DbContext`.

## Entidades

```
User          Id uuid, Name, Email, PasswordHash, CreatedAt
ExpenseType   Id int, UserId FK, Name
Income        Id, UserId FK, Date, Description, Value, IsRecurring, Frequency?
Expense       Id, UserId FK, DueDate, Description, ExpenseTypeId FK, Value, IsRecurring, Frequency?
RefreshToken  Id, UserId FK, Token, ExpiresAt, IsRevoked
```

- Uma classe `IEntityTypeConfiguration<T>` por entidade em `Paga.Infrastructure/Persistence/Configurations`.
  Nada de mapeamento inline no `OnModelCreating` além do `ApplyConfigurationsFromAssembly`.
- `Value` como `decimal(18,2)`. `Frequency` como enum persistido em texto (`weekly`/`monthly`/`yearly`).
- Índice único em `User.Email`; único composto em `(UserId, Name)` de `ExpenseType`.
- FK de `Expense → ExpenseType` com `DeleteBehavior.Restrict` (a regra de 409 é validada no service).
- Migrations sempre geradas por comando `dotnet ef`, nunca escritas à mão.

## Multi-tenant

Todo service recebe/obtém o `UserId` autenticado por um `ICurrentUserService` (lê as claims) e
aplica `Where(x => x.UserId == currentUserId)` em **toda** query, incluindo os `GetById`.
Um id que existe mas pertence a outro usuário responde 404, não 403 — não vazar existência.

## Validação

FluentValidation, um validator por DTO de entrada, registrado por assembly scanning e executado
no pipeline (não dentro do service). Regras recorrentes:

- `Value > 0`; `Description` obrigatória; datas obrigatórias.
- `Frequency` obrigatório quando `IsRecurring = true`, e deve ser nulo quando `false`.
- Senha mínima de 6 caracteres; email em formato válido e único.

Mensagens de validação em pt-BR (chegam ao usuário).

## Async, queries e performance

- Tudo assíncrono com `Async` no sufixo e `CancellationToken` propagado até o EF.
- Leitura usa `AsNoTracking()`. Projete direto para DTO com `Select` — não carregue entidade
  inteira para depois mapear.
- Paginação no banco (`Skip`/`Take`) mais um `CountAsync`. Nunca paginar em memória.
- Agregações do dashboard resolvidas em query (`GroupBy` + `Sum`), não em C#.

## Segurança

- Senha só com BCrypt (`HashPassword`/`Verify`). Nunca logar senha, hash, token ou PII.
- Access token 30 min; refresh token opaco, persistido, com expiração e revogação. Refresh usado
  rotaciona o token anterior.
- `[Authorize]` como padrão global; use `[AllowAnonymous]` pontualmente em login/refresh/health.
- Nunca retornar entidade do EF direto no response — sempre DTO.

## Erros e logging

Exception handler global converte exceções de domínio em `ProblemDetails` conforme
`api-contract.md`. Serilog estruturado (`_logger.LogInformation("... {Id}", id)`), sem interpolação
de string. Erro 500 não expõe detalhe ao cliente.

## Estilo C#

`var` quando o tipo é óbvio, expressões `switch`, `required`/`init` em DTOs, `record` para DTOs,
nullable reference types habilitado, sem warnings no build. XML doc em membros públicos de service
e em endpoints (alimentam o Swagger).
