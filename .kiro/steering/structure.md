# Estrutura do repositório

```
AplicacaoDoZero/
├── backend/
│   ├── Paga.sln
│   ├── src/
│   │   ├── Paga.Api/              # Controllers, Program.cs, middlewares, DI, appsettings
│   │   ├── Paga.Application/      # Services, DTOs, validators, interfaces, mapeamentos
│   │   ├── Paga.Domain/           # Entidades, enums, exceções de domínio
│   │   └── Paga.Infrastructure/   # DbContext, IEntityTypeConfiguration, repositories, migrations
│   └── tests/
│       └── Paga.Tests/            # xUnit: Unit/ e Integration/
├── frontend/
│   └── src/app/
│       ├── core/                  # auth, interceptors, guards, theme, models, api base
│       ├── shared/                # componentes, diretivas e pipes reutilizáveis
│       ├── features/              # uma pasta por módulo de negócio (lazy loaded)
│       │   ├── auth/
│       │   ├── dashboard/
│       │   ├── users/
│       │   ├── expense-types/
│       │   ├── incomes/
│       │   └── expenses/
│       ├── layout/                # shell, sidebar, header
│       └── styles/                # tokens, temas, mixins globais
├── infra/                         # CloudFormation, buildspecs, appspec, scripts de deploy
├── docs/                          # jira-board.md, figma-structure.md, architecture, runbook, adr/
└── .kiro/                         # steering e specs
```

## Regra de dependência do backend

`Api → Application → Domain` e `Api → Infrastructure → Domain`.
`Domain` não referencia nada. `Application` não referencia `Infrastructure` (só interfaces).
Controllers nunca acessam `DbContext` direto — sempre via service da Application.

## Organização de uma feature no frontend

```
features/expense-types/
├── expense-type-list/     # .ts .html .scss .spec.ts
├── expense-type-form/
├── expense-type.service.ts
├── expense-type.model.ts
└── expense-types.routes.ts
```

Cada feature expõe suas rotas em `<feature>.routes.ts` e é carregada por `loadChildren`.

## Nomenclatura

| Contexto | Convenção | Exemplo |
|----------|-----------|---------|
| Arquivos C# | PascalCase, um tipo por arquivo | `ExpenseTypeService.cs` |
| Namespaces | `Paga.<Projeto>.<Pasta>` | `Paga.Application.ExpenseTypes` |
| Arquivos Angular | kebab-case | `expense-type-list.component.ts` |
| Classes Angular | PascalCase com sufixo | `ExpenseTypeListComponent` |
| Rotas de API | kebab-case plural | `/api/expense-types` |
| Tabelas/colunas | snake_case | `expense_types.due_date` |
| Branches | `feature/<story>-<slug>` | `feature/4.3-expense-type-crud` |

## Mapa Figma → código

Nomes de componentes seguem a tabela de `docs/figma-structure.md`. Ex: `Frame: Listagem - Receitas`
→ `IncomeListComponent`; `Frame: Form - Inclusão Receita` / `Form - Alteração Receita` →
um único `IncomeFormComponent` com modo `create` | `edit`; `Modal - Confirmação Exclusão` →
`ConfirmDialogComponent` compartilhado em `shared/`.
