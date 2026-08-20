---
inclusion: fileMatch
fileMatchPattern: 'frontend/**/*'
---

# Padrões do frontend Angular

## Base

Angular 19 com **standalone components** apenas — sem `NgModule`. `ChangeDetectionStrategy.OnPush`
em todo componente. Injeção por `inject()`, não por construtor. Estado local com **signals**
(`signal`, `computed`, `effect`); `resource`/`toSignal` para dados assíncronos quando fizer sentido.

Templates usam a sintaxe de controle de fluxo nova: `@if`, `@for` (com `track`), `@switch`.
Não usar `*ngIf` / `*ngFor` em código novo.

## Componentes

- Um componente = uma responsabilidade. Listagem e formulário são componentes separados.
- Formulário de inclusão e alteração é **um só** componente, com o modo derivado da rota
  (`create` quando não há `:id`, `edit` quando há).
- Arquivos separados: `.ts`, `.html`, `.scss`, `.spec.ts`. Sem template ou estilo inline em
  componente de feature.
- Nada de lógica de negócio ou `HttpClient` no componente — sempre via service da feature.

## HTTP e services

- Um service por recurso (`UserService`, `ExpenseTypeService`, `IncomeService`, `ExpenseService`,
  `DashboardService`), `providedIn: 'root'`, tipado com as interfaces de `api-contract.md`.
- `apiUrl` vem de `environment`; nenhuma URL hardcoded no componente.
- Token é adicionado pelo `authInterceptor` (functional interceptor). Componentes e services
  nunca montam o header `Authorization` manualmente.
- O interceptor trata 401: tenta refresh uma única vez, enfileira as requests concorrentes e,
  se falhar, faz logout e redireciona para `/login`.
- Guard de rota funcional (`CanActivateFn`) protege tudo que não é `/login`.

## Formulários

Reactive Forms tipados (`FormGroup<...>`), nunca template-driven. Validação exibida somente
após `touched`/`dirty`. Botão de submit desabilitado enquanto inválido ou em loading.
Campos condicionais (Frequência quando Recorrente está ativo) adicionam/removem o control
com `addControl`/`removeControl` para não enviar valor órfão.

## Listagens

Toda listagem tem: filtro com `debounceTime(300)` + `distinctUntilChanged`, paginação servida
pela API, estado de loading (skeleton), empty state e error state com retry. Exclusão sempre
passa pelo `ConfirmDialogComponent` compartilhado e dá feedback por toast/snackbar.

## Formatação e i18n

Locale `pt-BR` registrado globalmente. Moeda via `CurrencyPipe` (`| currency:'BRL'`), datas via
`DatePipe` (`dd/MM/yyyy`). Entrada de valor monetário usa máscara e envia `number` para a API.

## Acessibilidade

Labels associados a inputs, `aria-label` em botões de ícone, foco visível, navegação por teclado
em tabelas e modais, contraste mínimo AA. Modal com foco preso e fechamento por `Esc`.

## Qualidade

`ng build` e `ng test --watch=false` sem erro antes de considerar a tarefa concluída.
Sem `any`, sem `console.log` remanescente, imports não usados removidos.
