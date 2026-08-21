# Requirements Document

## Introduction

**module-expenses — CRUD Despesas (API + Frontend).** Terceira fatia vertical de módulo de negócio
pós-walking skeleton. Cobre as Stories **PP-33 (CRUD Despesas — API)** e **PP-73 (CRUD Despesas —
Frontend)** do backlog PAGA.

Implementa a API REST completa para gerenciamento de Despesas (listagem com filtros de vencimento,
tipo, descrição e recorrência + paginação, consulta por ID, criação, atualização e exclusão), além
das telas Angular de listagem com filtros avançados (incluindo select de tipo carregado da API),
inclusão e alteração — substituindo o placeholder "Em construção" atualmente exibido no menu.

A entidade `Expense` (com FK Restrict para `ExpenseType`, `RecurrenceFrequency` converter e índice
`(UserId, DueDate)`), sua configuração EF Core e a migration correspondente já existem desde a
`mvp-1`. Nenhuma nova migration é necessária.

**Dependência:** este módulo depende de `module-expense-types` (PP-31 + PP-71), já entregue, que
fornece a API `GET /api/expense-types` consumida pelo select de tipo no formulário e pelo filtro na
listagem.

**Dentro do escopo:** `ExpensesController`, `IExpenseService` e implementação, DTOs (com
`ExpenseTypeName` no response via projeção Select), validadores FluentValidation (incluindo
validação condicional de recorrência e validação de `ExpenseTypeId` pertencente ao usuário),
testes unitários e de integração da API; `ExpenseService` Angular, `ExpenseListComponent` com
filtros (dueDateFrom, dueDateTo, expenseTypeId, description, isRecurring) e highlight visual de
despesas vencidas, `ExpenseFormComponent` com select de tipo carregado da API e lógica condicional
de recorrência, rotas do módulo, testes unitários do frontend.

**Componentes reutilizados (já existentes de module-incomes):** `RecurrenceSelectorComponent`,
`CurrencyMaskDirective`, `ConfirmDialogComponent`.

**Fora do escopo:** módulos de Receitas e Dashboard; alterações em autenticação, usuários ou Tipos
de Despesa; migrations; mudanças de infraestrutura AWS.

## Glossary

| Termo | Significado |
|-------|-------------|
| **Expense** | Saída de dinheiro (despesa); pertence a um único usuário (`UserId`). Entidade com `Id` (int, identity), `UserId` (FK), `DueDate` (DateOnly — vencimento), `Description` (max 300), `ExpenseTypeId` (FK para ExpenseType), `Value` (decimal 18,2), `IsRecurring` (bool), `Frequency` (RecurrenceFrequency? — weekly/monthly/yearly) |
| **ExpenseType** | Categoria de despesa; pertence a um único usuário. Possui `Id` (int) e `Name`. Fornecido pelo módulo `module-expense-types` já entregue |
| **ExpenseTypeName** | Nome do tipo de despesa incluído no response da API via projeção Select (join), nunca via Include da entidade completa |
| **Overdue (Vencida)** | Despesa cujo `DueDate < hoje`. Recebe destaque visual na listagem: fundo `#fef6f6` (--danger em baixa opacidade) e texto da data em `#ef4444` (--danger) |
| **API** | Backend ASP.NET Core exposto em `/api/expenses` |
| **Frontend** | Aplicação Angular 19 SPA que consome a API |
| **Paginação** | Envelope padrão `{ items, pageNumber, pageSize, totalCount, totalPages }` com `pageNumber` começando em 1 e `pageSize` default 10, máximo 100 |
| **ProblemDetails** | Formato RFC 7807 para respostas de erro HTTP, com campos `type`, `title`, `status` e `errors` |
| **Multi-tenant** | Isolamento de dados por usuário; toda query filtra por `UserId` derivado do token via `ICurrentUserService` |
| **Recorrência** | Quando `IsRecurring = true`, o campo `Frequency` é obrigatório (`weekly`, `monthly` ou `yearly`). Quando `IsRecurring = false`, `Frequency` deve ser `null` |
| **Debounce** | Atraso de 300ms entre a última tecla digitada e o disparo da requisição de busca, evitando chamadas excessivas |
| **RecurrenceSelector** | Componente compartilhado em `shared/` (já existente) que encapsula o toggle de recorrência e o select de frequência |
| **CurrencyMask** | Directive Angular reutilizável (já existente) que formata entrada numérica como moeda BRL (`R$ 1.234,56`) no input |
| **ConfirmDialog** | Componente Angular Material compartilhado (já existente) que exibe modal de confirmação antes de ações destrutivas |
| **409 Conflict** | Status HTTP retornado ao tentar excluir um `ExpenseType` que possui despesas vinculadas. A lógica existe em `module-expense-types` mas agora é verificável end-to-end com despesas existindo |

## Requirements

### Requirement 1: Listagem de despesas (API)

**User Story:** Como usuário autenticado, quero listar minhas despesas com filtros e paginação,
para que eu possa localizar saídas financeiras por período, tipo, descrição ou recorrência.

#### Acceptance Criteria

1. QUANDO uma requisição `GET /api/expenses` for recebida com autenticação válida, A API DEVE
   responder HTTP 200 com o envelope de paginação contendo apenas as despesas do usuário
   autenticado.
2. QUANDO o parâmetro `dueDateFrom` for informado, A API DEVE filtrar despesas com `DueDate >=
   dueDateFrom`.
3. QUANDO o parâmetro `dueDateTo` for informado, A API DEVE filtrar despesas com `DueDate <=
   dueDateTo`.
4. QUANDO o parâmetro `expenseTypeId` for informado, A API DEVE filtrar despesas com
   `ExpenseTypeId` correspondente.
5. QUANDO o parâmetro `description` for informado, A API DEVE filtrar despesas cuja descrição
   contenha o valor informado, sem distinção de maiúsculas e minúsculas.
6. QUANDO o parâmetro `isRecurring` for informado, A API DEVE filtrar despesas com valor de
   `IsRecurring` correspondente.
7. A API DEVE aplicar paginação com `pageNumber` (default 1) e `pageSize` (default 10, máximo
   100).
8. A API DEVE retornar cada despesa com `id`, `dueDate`, `description`, `expenseTypeId`,
   `expenseTypeName`, `value`, `isRecurring` e `frequency`.
9. A API DEVE obter `expenseTypeName` via projeção Select (join), sem usar Include da entidade
   completa.
10. A API DEVE ordenar os resultados por `DueDate` decrescente (mais recentes primeiro) como
    padrão.
11. QUANDO a requisição não possuir token de autenticação válido, A API DEVE responder HTTP 401.
12. A API DEVE executar a consulta com `AsNoTracking` e projetar diretamente para DTO.

### Requirement 2: Consulta de despesa por ID (API)

**User Story:** Como usuário autenticado, quero consultar uma despesa específica, para que eu
possa obter seus detalhes antes de editá-la.

#### Acceptance Criteria

1. QUANDO uma requisição `GET /api/expenses/{id}` for recebida com ID existente e pertencente ao
   usuário autenticado, A API DEVE responder HTTP 200 com `id`, `dueDate`, `description`,
   `expenseTypeId`, `expenseTypeName`, `value`, `isRecurring` e `frequency`.
2. QUANDO o ID não existir ou pertencer a outro usuário, A API DEVE responder HTTP 404.
3. QUANDO a requisição não possuir token de autenticação válido, A API DEVE responder HTTP 401.

### Requirement 3: Criação de despesa (API)

**User Story:** Como usuário autenticado, quero criar novas despesas, para que eu possa registrar
minhas saídas financeiras com classificação por tipo.

#### Acceptance Criteria

1. QUANDO uma requisição `POST /api/expenses` for recebida com payload válido, A API DEVE criar a
   despesa associada ao usuário autenticado e responder HTTP 201 com `id`, `dueDate`,
   `description`, `expenseTypeId`, `expenseTypeName`, `value`, `isRecurring` e `frequency`.
2. A API DEVE validar que `dueDate` é obrigatório.
3. A API DEVE validar que `description` é obrigatória e não vazia.
4. A API DEVE validar que `value` é obrigatório e maior que zero.
5. A API DEVE validar que `expenseTypeId` é obrigatório.
6. A API DEVE validar que `expenseTypeId` referencia um tipo de despesa existente e pertencente ao
   usuário autenticado.
7. QUANDO `isRecurring` for `true`, A API DEVE validar que `frequency` é obrigatório e contém
   valor válido (`weekly`, `monthly` ou `yearly`).
8. QUANDO `isRecurring` for `false`, A API DEVE validar que `frequency` é `null`.
9. QUANDO a validação falhar, A API DEVE responder HTTP 400 com `ProblemDetails` e mensagens em
   pt-BR.
10. A API DEVE derivar o `UserId` exclusivamente das claims do token, ignorando qualquer valor no
    payload.
11. QUANDO a requisição não possuir token de autenticação válido, A API DEVE responder HTTP 401.

### Requirement 4: Atualização de despesa (API)

**User Story:** Como usuário autenticado, quero alterar uma despesa existente, para que eu possa
corrigir dados, mudar o tipo ou atualizar recorrência.

#### Acceptance Criteria

1. QUANDO uma requisição `PUT /api/expenses/{id}` for recebida com payload válido e ID existente
   pertencente ao usuário autenticado, A API DEVE atualizar todos os campos e responder HTTP 200
   com `id`, `dueDate`, `description`, `expenseTypeId`, `expenseTypeName`, `value`, `isRecurring`
   e `frequency` atualizados.
2. QUANDO o ID não existir ou pertencer a outro usuário, A API DEVE responder HTTP 404.
3. A API DEVE validar que `dueDate` é obrigatório.
4. A API DEVE validar que `description` é obrigatória e não vazia.
5. A API DEVE validar que `value` é obrigatório e maior que zero.
6. A API DEVE validar que `expenseTypeId` é obrigatório.
7. A API DEVE validar que `expenseTypeId` referencia um tipo de despesa existente e pertencente ao
   usuário autenticado.
8. QUANDO `isRecurring` for `true`, A API DEVE validar que `frequency` é obrigatório e contém
   valor válido (`weekly`, `monthly` ou `yearly`).
9. QUANDO `isRecurring` for `false`, A API DEVE validar que `frequency` é `null`.
10. QUANDO a validação falhar, A API DEVE responder HTTP 400 com `ProblemDetails` e mensagens em
    pt-BR.
11. QUANDO a requisição não possuir token de autenticação válido, A API DEVE responder HTTP 401.

### Requirement 5: Exclusão de despesa (API)

**User Story:** Como usuário autenticado, quero excluir uma despesa, para que saídas incorretas
ou obsoletas possam ser removidas.

#### Acceptance Criteria

1. QUANDO uma requisição `DELETE /api/expenses/{id}` for recebida com ID existente pertencente ao
   usuário autenticado, A API DEVE excluir a despesa e responder HTTP 204.
2. QUANDO o ID não existir ou pertencer a outro usuário, A API DEVE responder HTTP 404.
3. QUANDO a requisição não possuir token de autenticação válido, A API DEVE responder HTTP 401.

### Requirement 6: Isolamento multi-tenant (API)

**User Story:** Como usuário autenticado, quero que minhas despesas sejam privadas, para que
outros usuários não possam visualizar, alterar ou excluir meus lançamentos.

#### Acceptance Criteria

1. A API DEVE filtrar todas as consultas de despesas por `UserId` derivado do token via
   `ICurrentUserService`.
2. QUANDO um usuário tentar acessar, atualizar ou excluir uma despesa pertencente a outro
   usuário, A API DEVE responder HTTP 404 sem revelar que o recurso existe.
3. A API DEVE garantir que a criação de despesa associe o registro exclusivamente ao `UserId` do
   token autenticado.
4. A API DEVE garantir que o `expenseTypeId` informado pertença ao mesmo usuário autenticado,
   impedindo referência cruzada a tipos de outro usuário.

### Requirement 7: Integridade referencial — ExpenseType com despesas vinculadas

**User Story:** Como usuário autenticado, quero que o sistema impeça a exclusão de um tipo de
despesa que possua despesas vinculadas, para que a integridade dos meus dados seja preservada.

#### Acceptance Criteria

1. QUANDO uma requisição `DELETE /api/expense-types/{id}` for recebida e existirem despesas
   vinculadas ao tipo informado, A API DEVE responder HTTP 409 com mensagem em pt-BR indicando
   que o tipo possui despesas associadas.
2. QUANDO o tipo de despesa não possuir despesas vinculadas, A API DEVE permitir a exclusão
   normalmente (HTTP 204).
3. A verificação DEVE considerar apenas despesas do próprio usuário autenticado.

### Requirement 8: Tela de listagem de despesas (Frontend)

**User Story:** Como usuário autenticado, quero visualizar minhas despesas em uma tabela com
filtros avançados, paginação e destaque de vencidas, para que eu possa navegar e gerenciar meus
lançamentos.

#### Acceptance Criteria

1. QUANDO o usuário navegar para a rota de despesas, O FRONTEND DEVE exibir uma tabela com colunas
   Vencimento, Descrição, Tipo, Valor e Recorrente, populada via `GET /api/expenses`.
2. O FRONTEND DEVE exibir barra de filtros com: Vencimento de (datepicker), Vencimento até
   (datepicker), Tipo (select carregado via `GET /api/expense-types`), Descrição (input com
   debounce de 300ms) e Recorrente (select: Todos/Sim/Não).
3. QUANDO qualquer filtro for alterado, O FRONTEND DEVE enviar os parâmetros correspondentes à API
   e exibir os resultados filtrados, reiniciando a paginação para a primeira página.
4. O FRONTEND DEVE formatar a coluna Vencimento no padrão `dd/mm/yyyy`.
5. O FRONTEND DEVE formatar a coluna Valor no padrão `R$ 1.234,56` usando a cor danger
   (`--danger` / `#ef4444`).
6. O FRONTEND DEVE exibir a coluna Tipo com o nome do tipo de despesa (`expenseTypeName`).
7. O FRONTEND DEVE exibir a coluna Recorrente como texto "Sim" ou "Não".
8. QUANDO a despesa estiver vencida (`dueDate < hoje`), O FRONTEND DEVE aplicar fundo `#fef6f6`
   na linha e exibir o texto da coluna Vencimento na cor `#ef4444`.
9. O FRONTEND DEVE exibir um botão "+ Nova Despesa" (primário) que navegue para a tela de
   inclusão.
10. O FRONTEND DEVE exibir botões "Editar" e "Excluir" em cada linha da tabela.
11. O FRONTEND DEVE implementar paginação funcional sincronizada com a API.
12. ENQUANTO os dados estiverem carregando, O FRONTEND DEVE exibir um skeleton de loading.
13. QUANDO a API retornar lista vazia, O FRONTEND DEVE exibir o estado vazio com mensagem
    "Nenhum registro encontrado" e sugestão para ajustar filtros ou adicionar novo item.
14. SE a API retornar erro, O FRONTEND DEVE exibir o estado de erro com mensagem "Erro ao
    carregar dados" e botão "Tentar Novamente".

### Requirement 9: Tela de inclusão de despesa (Frontend)

**User Story:** Como usuário autenticado, quero criar uma nova despesa via formulário com seleção
de tipo e suporte a recorrência, para que eu possa registrar minhas saídas financeiras
classificadas.

#### Acceptance Criteria

1. QUANDO o usuário navegar para a rota de inclusão, O FRONTEND DEVE exibir formulário com título
   "Nova Despesa", campos Vencimento (datepicker, placeholder "dd/mm/aaaa"), Descrição (text
   input), Tipo de Despesa (select carregado via `GET /api/expense-types`, placeholder "Selecione
   um tipo"), Valor (com máscara monetária, placeholder "R$ 0,00"), toggle Recorrente e botões
   "Salvar" (primário) e "Cancelar" (secundário).
2. O FRONTEND DEVE validar que Vencimento, Descrição, Tipo de Despesa e Valor são obrigatórios
   antes de permitir o envio.
3. O FRONTEND DEVE validar que Valor é maior que zero.
4. QUANDO o toggle Recorrente for ativado, O FRONTEND DEVE exibir o campo Frequência (select com
   opções: Semanal, Mensal, Anual) e validar que é obrigatório.
5. QUANDO o toggle Recorrente for desativado, O FRONTEND DEVE ocultar o campo Frequência e enviar
   `frequency: null` no payload.
6. QUANDO o usuário clicar em "Salvar" com dados válidos, O FRONTEND DEVE enviar
   `POST /api/expenses` e, em caso de sucesso, exibir snackbar de sucesso e navegar de volta para
   a listagem.
7. QUANDO a API retornar erro (400, 500), O FRONTEND DEVE exibir a mensagem de erro retornada pela
   API em snackbar.
8. QUANDO o usuário clicar em "Cancelar", O FRONTEND DEVE navegar de volta para a listagem sem
   enviar requisição.
9. ENQUANTO a requisição de criação estiver em andamento, O FRONTEND DEVE desabilitar o botão
   "Salvar" para evitar envio duplicado.
10. O FRONTEND DEVE utilizar o componente compartilhado `RecurrenceSelector` para o toggle e o
    select de frequência.
11. O FRONTEND DEVE utilizar a directive compartilhada `CurrencyMask` no campo Valor.
12. O FRONTEND DEVE carregar a lista de tipos de despesa ao inicializar o formulário e exibir
    estado de erro caso a carga falhe.

### Requirement 10: Tela de alteração de despesa (Frontend)

**User Story:** Como usuário autenticado, quero editar uma despesa existente, para que eu possa
corrigir dados, mudar o tipo ou alterar a recorrência.

#### Acceptance Criteria

1. QUANDO o usuário navegar para a rota de alteração, O FRONTEND DEVE carregar os dados da despesa
   via `GET /api/expenses/{id}` e a lista de tipos via `GET /api/expense-types`, preenchendo o
   formulário com título "Editar Despesa".
2. QUANDO a API retornar 404 para o ID informado, O FRONTEND DEVE navegar de volta para a listagem
   e exibir snackbar de erro.
3. O FRONTEND DEVE preencher o select de Tipo de Despesa com o `expenseTypeId` da despesa.
4. O FRONTEND DEVE preencher o toggle Recorrente e exibir o campo Frequência caso a despesa seja
   recorrente.
5. QUANDO o usuário clicar em "Salvar" com dados válidos, O FRONTEND DEVE enviar
   `PUT /api/expenses/{id}` e, em caso de sucesso, exibir snackbar de sucesso e navegar de volta
   para a listagem.
6. QUANDO a API retornar erro (400, 500), O FRONTEND DEVE exibir a mensagem de erro retornada pela
   API em snackbar.
7. QUANDO o usuário clicar em "Cancelar", O FRONTEND DEVE navegar de volta para a listagem sem
   enviar requisição.
8. ENQUANTO a requisição de atualização estiver em andamento, O FRONTEND DEVE desabilitar o botão
   "Salvar" para evitar envio duplicado.

### Requirement 11: Exclusão com confirmação (Frontend)

**User Story:** Como usuário autenticado, quero confirmar antes de excluir uma despesa, para que
exclusões acidentais sejam evitadas.

#### Acceptance Criteria

1. QUANDO o usuário clicar em "Excluir" em uma linha da tabela, O FRONTEND DEVE exibir o
   `ConfirmDialogComponent` com título "Confirmar Exclusão" e mensagem "Deseja excluir a despesa
   \"{descrição}\"? Esta ação não pode ser desfeita.".
2. QUANDO o usuário confirmar a exclusão, O FRONTEND DEVE enviar `DELETE /api/expenses/{id}` e, em
   caso de sucesso, exibir snackbar de sucesso e atualizar a listagem.
3. QUANDO a API retornar erro, O FRONTEND DEVE exibir a mensagem de erro retornada pela API em
   snackbar.
4. QUANDO o usuário clicar em "Cancelar" no modal, O FRONTEND DEVE fechar o modal sem enviar
   requisição.
5. O FRONTEND DEVE reutilizar o `ConfirmDialogComponent` compartilhado em `shared/`.

### Requirement 12: Highlight de despesas vencidas (Frontend)

**User Story:** Como usuário autenticado, quero identificar visualmente despesas vencidas na
listagem, para que eu possa priorizar pagamentos em atraso.

#### Acceptance Criteria

1. QUANDO a despesa possuir `dueDate` anterior à data de hoje, O FRONTEND DEVE aplicar classe CSS
   que define fundo `#fef6f6` (--danger em baixa opacidade) na linha da tabela.
2. QUANDO a despesa estiver vencida, O FRONTEND DEVE exibir o texto da coluna Vencimento na cor
   `#ef4444` (--danger).
3. QUANDO a despesa não estiver vencida, O FRONTEND DEVE exibir a linha com fundo branco padrão e
   texto da data na cor padrão.
4. A detecção de vencimento DEVE comparar `dueDate` com a data atual do navegador do usuário
   (sem considerar hora).

### Requirement 13: Feedback visual e estados (Frontend)

**User Story:** Como usuário autenticado, quero feedback claro sobre o resultado de minhas ações,
para que eu saiba se operações foram bem-sucedidas ou se houve erro.

#### Acceptance Criteria

1. QUANDO uma operação de criação, atualização ou exclusão for bem-sucedida, O FRONTEND DEVE
   exibir snackbar com mensagem de sucesso em pt-BR.
2. QUANDO a API retornar erro (400, 409, 500), O FRONTEND DEVE exibir snackbar com a mensagem de
   erro retornada pela API.
3. QUANDO a API retornar 401, O FRONTEND DEVE redirecionar para a tela de login conforme
   comportamento do interceptor já existente.
4. O FRONTEND DEVE exibir skeleton de loading durante carregamento da listagem e estado de erro
   com botão "Tentar Novamente" em caso de falha.

### Requirement 14: Navegação e rotas (Frontend)

**User Story:** Como usuário autenticado, quero acessar despesas pelo menu lateral, para que a
navegação seja consistente com os demais módulos.

#### Acceptance Criteria

1. O FRONTEND DEVE substituir o placeholder "Em construção" na rota de despesas pelo módulo
   funcional, mantendo o mesmo item no menu lateral.
2. O FRONTEND DEVE configurar rotas lazy-loaded: listagem como rota padrão, `/new` para inclusão e
   `/:id/edit` para alteração.
3. O FRONTEND DEVE utilizar um único `ExpenseFormComponent` para inclusão e alteração,
   diferenciando o modo pela presença do parâmetro de rota `id`.

### Requirement 15: Testes da API (Backend)

**User Story:** Como desenvolvedor, quero cobertura de testes unitários e de integração para a API
de despesas, para que regressões sejam detectadas automaticamente.

#### Acceptance Criteria

1. O SISTEMA DEVE cobrir por teste unitário o `ExpenseService`: criação com dados válidos, criação
   com recorrência válida (isRecurring=true + frequency), criação com recorrência inválida
   (isRecurring=true + frequency=null), criação com expenseTypeId inválido (inexistente ou de outro
   usuário), atualização com dados válidos, atualização alternando recorrência de false para true,
   atualização mudando o expenseTypeId, consulta por ID existente, consulta por ID inexistente
   (404), exclusão de despesa existente.
2. O SISTEMA DEVE cobrir por teste unitário os validadores de `CreateExpenseDto` e
   `UpdateExpenseDto`: campo `dueDate` obrigatório, campo `description` obrigatório e não vazio,
   campo `value` obrigatório e maior que zero, campo `expenseTypeId` obrigatório, `frequency`
   obrigatório quando `isRecurring=true`, `frequency` deve ser null quando `isRecurring=false`,
   `frequency` com valor inválido rejeitado.
3. O SISTEMA DEVE cobrir por teste de integração o fluxo HTTP completo: POST retorna 201 com todos
   os campos (incluindo `expenseTypeName`), GET lista com filtros (`dueDateFrom`, `dueDateTo`,
   `expenseTypeId`, `description`, `isRecurring`) e paginação, GET por ID retorna 200, PUT retorna
   200, DELETE retorna 204.
4. O SISTEMA DEVE cobrir por teste de integração que POST com `isRecurring=true` e
   `frequency=null` retorna 400.
5. O SISTEMA DEVE cobrir por teste de integração que POST com `value <= 0` retorna 400.
6. O SISTEMA DEVE cobrir por teste de integração que POST com `expenseTypeId` inexistente retorna
   400.
7. O SISTEMA DEVE cobrir por teste de integração que POST com `expenseTypeId` pertencente a outro
   usuário retorna 400.
8. O SISTEMA DEVE cobrir por teste de integração o isolamento entre usuários: usuário A não
   visualiza, altera nem exclui despesa de usuário B.
9. O SISTEMA DEVE cobrir por teste de integração que requisições sem token retornam 401.
10. O SISTEMA DEVE cobrir por teste de integração que requisição para ID inexistente retorna 404.
11. O SISTEMA DEVE cobrir por teste de integração a integridade referencial: `DELETE
    /api/expense-types/{id}` retorna 409 quando o tipo possui despesas vinculadas e retorna 204
    quando não possui.
12. O SISTEMA DEVE executar testes de integração contra container PostgreSQL efêmero via
    Testcontainers.
13. QUANDO `dotnet test` for executado, O SISTEMA DEVE passar todos os testes sem falhas.

### Requirement 16: Testes do Frontend

**User Story:** Como desenvolvedor, quero cobertura de testes unitários para os componentes e
services do frontend de despesas, para que regressões sejam detectadas automaticamente.

#### Acceptance Criteria

1. O SISTEMA DEVE cobrir por teste unitário o `ExpenseService` Angular: chamadas HTTP corretas
   (URL, método, parâmetros) para listagem com filtros (incluindo `expenseTypeId`), consulta por
   ID, criação, atualização e exclusão.
2. O SISTEMA DEVE cobrir por teste unitário o `ExpenseListComponent`: renderização da tabela com
   coluna Tipo, disparo de busca com debounce, filtragem por vencimento/tipo/descrição/recorrência,
   highlight visual de despesas vencidas, carga do select de tipos via API, estados de loading,
   vazio e erro, navegação para inclusão e edição.
3. O SISTEMA DEVE cobrir por teste unitário o `ExpenseFormComponent`: validação dos campos
   obrigatórios (Vencimento, Descrição, Tipo de Despesa, Valor), validação de valor maior que
   zero, carga da lista de tipos no select, comportamento condicional do toggle de recorrência
   (exibir/ocultar frequência), envio com dados válidos, preenchimento no modo edição (incluindo
   tipo selecionado), navegação ao cancelar.
4. O SISTEMA DEVE cobrir por teste unitário a lógica de highlight de vencidos: detecção correta
   de `dueDate < hoje`, aplicação de classes CSS para fundo e cor de texto.
5. QUANDO `ng test --watch=false` for executado, O SISTEMA DEVE passar todos os testes sem falhas.
