# Requirements Document

## Introduction

**module-expense-types — CRUD Tipo de Despesa (API + Frontend).** Primeira fatia vertical de
módulo de negócio pós-walking skeleton. Cobre as Stories **PP-31 (CRUD Tipo de Despesa — API)** e
**PP-71 (CRUD Tipo de Despesa — Frontend)** do backlog PAGA.

Implementa a API REST completa para gerenciamento de Tipos de Despesa (listagem com filtro e
paginação, consulta por ID, criação, atualização e exclusão com proteção referencial), além das
telas Angular de listagem, inclusão e alteração — substituindo o placeholder "Em construção"
atualmente exibido no menu.

A entidade `ExpenseType`, sua configuração EF Core (índice único `(UserId, Name)`, FK para `User`)
e a migration correspondente já existem desde a `mvp-1`. Nenhuma nova migration é necessária.

**Dentro do escopo:** `ExpenseTypesController`, `IExpenseTypeService` e implementação, DTOs,
validadores FluentValidation, testes unitários e de integração da API; `ExpenseTypeService`
Angular, `ExpenseTypeListComponent`, `ExpenseTypeFormComponent`, `ConfirmDialogComponent`
compartilhado, rotas do módulo, testes unitários do frontend.

**Fora do escopo:** módulos de Receitas, Despesas e Dashboard; alterações em autenticação ou
usuários; migrations; mudanças de infraestrutura AWS.

## Glossary

| Termo | Significado |
|-------|-------------|
| **Expense_Type** | Categoria usada para classificar despesas; pertence a um único usuário (`UserId`). Entidade com `Id` (int, identity), `UserId` (FK) e `Name` (max 100 caracteres) |
| **API** | Backend ASP.NET Core exposto em `/api/expense-types` |
| **Frontend** | Aplicação Angular 19 SPA que consome a API |
| **Paginação** | Envelope padrão `{ items, pageNumber, pageSize, totalCount, totalPages }` com `pageNumber` começando em 1 e `pageSize` default 10, máximo 100 |
| **ProblemDetails** | Formato RFC 7807 para respostas de erro HTTP, com campos `type`, `title`, `status` e `errors` |
| **Multi-tenant** | Isolamento de dados por usuário; toda query filtra por `UserId` derivado do token via `ICurrentUserService` |
| **Debounce** | Atraso de 300ms entre a última tecla digitada e o disparo da requisição de busca, evitando chamadas excessivas |
| **ConfirmDialog** | Componente Angular Material compartilhado que exibe modal de confirmação antes de ações destrutivas |

## Requirements

### Requirement 1: Listagem de tipos de despesa (API)

**User Story:** Como usuário autenticado, quero listar meus tipos de despesa com filtro e
paginação, para que eu possa localizar categorias existentes.

#### Acceptance Criteria

1. QUANDO uma requisição `GET /api/expense-types` for recebida com autenticação válida, A API
   DEVE responder HTTP 200 com o envelope de paginação contendo apenas os tipos de despesa do
   usuário autenticado.
2. QUANDO o parâmetro `name` for informado, A API DEVE filtrar tipos de despesa cujo nome contenha
   o valor informado, sem distinção de maiúsculas e minúsculas.
3. A API DEVE aplicar paginação com `pageNumber` (default 1) e `pageSize` (default 10, máximo 100).
4. A API DEVE retornar cada tipo de despesa com `id` e `name`.
5. QUANDO a requisição não possuir token de autenticação válido, A API DEVE responder HTTP 401.
6. A API DEVE executar a consulta com `AsNoTracking` e projetar diretamente para DTO.

### Requirement 2: Consulta de tipo de despesa por ID (API)

**User Story:** Como usuário autenticado, quero consultar um tipo de despesa específico, para que
eu possa obter seus detalhes antes de editá-lo.

#### Acceptance Criteria

1. QUANDO uma requisição `GET /api/expense-types/{id}` for recebida com ID existente e pertencente
   ao usuário autenticado, A API DEVE responder HTTP 200 com `id` e `name`.
2. QUANDO o ID não existir ou pertencer a outro usuário, A API DEVE responder HTTP 404.
3. QUANDO a requisição não possuir token de autenticação válido, A API DEVE responder HTTP 401.

### Requirement 3: Criação de tipo de despesa (API)

**User Story:** Como usuário autenticado, quero criar novos tipos de despesa, para que eu possa
categorizar minhas despesas.

#### Acceptance Criteria

1. QUANDO uma requisição `POST /api/expense-types` for recebida com payload válido, A API DEVE
   criar o tipo de despesa associado ao usuário autenticado e responder HTTP 201 com `id` e `name`.
2. A API DEVE validar que `name` é obrigatório e não vazio.
3. QUANDO o nome informado já existir para o mesmo usuário (comparação case-insensitive), A API
   DEVE responder HTTP 409 com mensagem em pt-BR informando que o nome já está cadastrado.
4. QUANDO a validação falhar, A API DEVE responder HTTP 400 com `ProblemDetails` e mensagens em
   pt-BR.
5. A API DEVE derivar o `UserId` exclusivamente das claims do token, ignorando qualquer valor no
   payload.
6. QUANDO a requisição não possuir token de autenticação válido, A API DEVE responder HTTP 401.

### Requirement 4: Atualização de tipo de despesa (API)

**User Story:** Como usuário autenticado, quero alterar o nome de um tipo de despesa, para que eu
possa corrigir ou renomear categorias.

#### Acceptance Criteria

1. QUANDO uma requisição `PUT /api/expense-types/{id}` for recebida com payload válido e ID
   existente pertencente ao usuário autenticado, A API DEVE atualizar o nome e responder HTTP 200
   com `id` e `name` atualizados.
2. QUANDO o ID não existir ou pertencer a outro usuário, A API DEVE responder HTTP 404.
3. A API DEVE validar que `name` é obrigatório e não vazio.
4. QUANDO o novo nome já existir para o mesmo usuário (comparação case-insensitive) em outro
   registro, A API DEVE responder HTTP 409 com mensagem em pt-BR.
5. QUANDO a validação falhar, A API DEVE responder HTTP 400 com `ProblemDetails` e mensagens em
   pt-BR.
6. QUANDO a requisição não possuir token de autenticação válido, A API DEVE responder HTTP 401.

### Requirement 5: Exclusão de tipo de despesa (API)

**User Story:** Como usuário autenticado, quero excluir um tipo de despesa, para que categorias
obsoletas possam ser removidas.

#### Acceptance Criteria

1. QUANDO uma requisição `DELETE /api/expense-types/{id}` for recebida com ID existente
   pertencente ao usuário autenticado e sem despesas vinculadas, A API DEVE excluir o tipo e
   responder HTTP 204.
2. QUANDO o ID não existir ou pertencer a outro usuário, A API DEVE responder HTTP 404.
3. QUANDO o tipo de despesa possuir despesas vinculadas, A API DEVE responder HTTP 409 com
   mensagem em pt-BR informando que não é possível excluir um tipo com despesas associadas.
4. QUANDO a requisição não possuir token de autenticação válido, A API DEVE responder HTTP 401.
5. A API DEVE verificar a existência de despesas vinculadas no service antes de tentar a exclusão,
   sem depender da exceção do banco de dados.

### Requirement 6: Isolamento multi-tenant (API)

**User Story:** Como usuário autenticado, quero que meus tipos de despesa sejam privados, para que
outros usuários não possam visualizar, alterar ou excluir minhas categorias.

#### Acceptance Criteria

1. A API DEVE filtrar todas as consultas de tipos de despesa por `UserId` derivado do token via
   `ICurrentUserService`.
2. QUANDO um usuário tentar acessar, atualizar ou excluir um tipo de despesa pertencente a outro
   usuário, A API DEVE responder HTTP 404 sem revelar que o recurso existe.
3. A API DEVE garantir que a criação de tipo de despesa associe o registro exclusivamente ao
   `UserId` do token autenticado.

### Requirement 7: Tela de listagem de tipos de despesa (Frontend)

**User Story:** Como usuário autenticado, quero visualizar meus tipos de despesa em uma tabela
com busca e paginação, para que eu possa navegar e gerenciar minhas categorias.

#### Acceptance Criteria

1. QUANDO o usuário navegar para a rota de tipos de despesa, O FRONTEND DEVE exibir uma tabela
   com colunas ID e Nome, populada via `GET /api/expense-types`.
2. O FRONTEND DEVE exibir um campo de busca com placeholder "Buscar por nome..." que filtre a
   listagem com debounce de 300ms.
3. QUANDO a busca for acionada, O FRONTEND DEVE enviar o parâmetro `name` à API e exibir os
   resultados filtrados.
4. O FRONTEND DEVE exibir um botão "Novo Tipo de Despesa" (primário) que navegue para a tela de
   inclusão.
5. O FRONTEND DEVE exibir botões "Editar" e "Excluir" em cada linha da tabela.
6. O FRONTEND DEVE implementar paginação funcional sincronizada com a API.
7. ENQUANTO os dados estiverem carregando, O FRONTEND DEVE exibir um skeleton de loading.
8. QUANDO a API retornar lista vazia, O FRONTEND DEVE exibir o estado vazio com mensagem
   "Nenhum registro encontrado" e sugestão para ajustar filtros ou adicionar novo item.
9. SE a API retornar erro, O FRONTEND DEVE exibir o estado de erro com mensagem "Erro ao
   carregar dados" e botão "Tentar Novamente".

### Requirement 8: Tela de inclusão de tipo de despesa (Frontend)

**User Story:** Como usuário autenticado, quero criar um novo tipo de despesa via formulário,
para que eu possa adicionar categorias personalizadas.

#### Acceptance Criteria

1. QUANDO o usuário navegar para a rota de inclusão, O FRONTEND DEVE exibir formulário com título
   "Novo Tipo de Despesa", campo Nome (placeholder "Ex: Alimentação, Transporte...") e botões
   "Salvar" (primário) e "Cancelar" (secundário).
2. O FRONTEND DEVE validar que o campo Nome é obrigatório antes de permitir o envio.
3. QUANDO o usuário clicar em "Salvar" com dados válidos, O FRONTEND DEVE enviar
   `POST /api/expense-types` e, em caso de sucesso, exibir snackbar de sucesso e navegar de volta
   para a listagem.
4. QUANDO a API retornar 409 (nome duplicado), O FRONTEND DEVE exibir a mensagem de erro retornada
   pela API em snackbar.
5. QUANDO o usuário clicar em "Cancelar", O FRONTEND DEVE navegar de volta para a listagem sem
   enviar requisição.
6. ENQUANTO a requisição de criação estiver em andamento, O FRONTEND DEVE desabilitar o botão
   "Salvar" para evitar envio duplicado.

### Requirement 9: Tela de alteração de tipo de despesa (Frontend)

**User Story:** Como usuário autenticado, quero editar um tipo de despesa existente, para que eu
possa corrigir ou renomear categorias.

#### Acceptance Criteria

1. QUANDO o usuário navegar para a rota de alteração, O FRONTEND DEVE carregar os dados do tipo
   via `GET /api/expense-types/{id}` e preencher o formulário com título "Editar Tipo de Despesa".
2. QUANDO a API retornar 404 para o ID informado, O FRONTEND DEVE navegar de volta para a listagem
   e exibir snackbar de erro.
3. QUANDO o usuário clicar em "Salvar" com dados válidos, O FRONTEND DEVE enviar
   `PUT /api/expense-types/{id}` e, em caso de sucesso, exibir snackbar de sucesso e navegar de
   volta para a listagem.
4. QUANDO a API retornar 409 (nome duplicado), O FRONTEND DEVE exibir a mensagem de erro retornada
   pela API em snackbar.
5. QUANDO o usuário clicar em "Cancelar", O FRONTEND DEVE navegar de volta para a listagem sem
   enviar requisição.
6. ENQUANTO a requisição de atualização estiver em andamento, O FRONTEND DEVE desabilitar o botão
   "Salvar" para evitar envio duplicado.

### Requirement 10: Exclusão com confirmação (Frontend)

**User Story:** Como usuário autenticado, quero confirmar antes de excluir um tipo de despesa,
para que exclusões acidentais sejam evitadas.

#### Acceptance Criteria

1. QUANDO o usuário clicar em "Excluir" em uma linha da tabela, O FRONTEND DEVE exibir o
   `ConfirmDialogComponent` com título "Confirmar Exclusão" e mensagem "Deseja excluir o tipo
   \"{nome}\"?".
2. QUANDO o usuário confirmar a exclusão, O FRONTEND DEVE enviar `DELETE /api/expense-types/{id}`
   e, em caso de sucesso, exibir snackbar de sucesso e atualizar a listagem.
3. QUANDO a API retornar 409 (tipo com despesas vinculadas), O FRONTEND DEVE exibir a mensagem de
   erro retornada pela API em snackbar.
4. QUANDO o usuário clicar em "Cancelar" no modal, O FRONTEND DEVE fechar o modal sem enviar
   requisição.
5. O FRONTEND DEVE reutilizar o `ConfirmDialogComponent` compartilhado em `shared/`.

### Requirement 11: Feedback visual e estados (Frontend)

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

### Requirement 12: Navegação e rotas (Frontend)

**User Story:** Como usuário autenticado, quero acessar tipos de despesa pelo menu lateral, para
que a navegação seja consistente com os demais módulos.

#### Acceptance Criteria

1. O FRONTEND DEVE substituir o placeholder "Em construção" na rota de tipos de despesa pelo
   módulo funcional, mantendo o mesmo item no menu lateral.
2. O FRONTEND DEVE configurar rotas lazy-loaded: listagem como rota padrão, `/new` para inclusão e
   `/:id/edit` para alteração.
3. O FRONTEND DEVE utilizar um único `ExpenseTypeFormComponent` para inclusão e alteração,
   diferenciando o modo pela presença do parâmetro de rota `id`.

### Requirement 13: Testes da API (Backend)

**User Story:** Como desenvolvedor, quero cobertura de testes unitários e de integração para a API
de tipos de despesa, para que regressões sejam detectadas automaticamente.

#### Acceptance Criteria

1. O SISTEMA DEVE cobrir por teste unitário o `ExpenseTypeService`: criação com nome válido,
   criação com nome duplicado (409), atualização com nome válido, atualização com nome duplicado
   (409), exclusão de tipo sem despesas, exclusão de tipo com despesas vinculadas (409), consulta
   por ID existente, consulta por ID inexistente (404).
2. O SISTEMA DEVE cobrir por teste unitário o validador de `CreateExpenseTypeDto` e
   `UpdateExpenseTypeDto`: campo obrigatório vazio, campo válido.
3. O SISTEMA DEVE cobrir por teste de integração o fluxo HTTP completo: POST retorna 201, GET
   lista com filtro e paginação, GET por ID retorna 200, PUT retorna 200, DELETE retorna 204.
4. O SISTEMA DEVE cobrir por teste de integração que POST com nome duplicado para o mesmo
   usuário retorna 409.
5. O SISTEMA DEVE cobrir por teste de integração que DELETE de tipo com despesas vinculadas
   retorna 409. O teste DEVE inserir uma despesa diretamente via `DbContext` para simular o
   vínculo, dado que a API de Despesas ainda não existe.
6. O SISTEMA DEVE cobrir por teste de integração o isolamento entre usuários: usuário A não
   visualiza, altera nem exclui tipo de despesa de usuário B.
7. O SISTEMA DEVE cobrir por teste de integração que requisições sem token retornam 401.
8. O SISTEMA DEVE cobrir por teste de integração que requisição para ID inexistente retorna 404.
9. O SISTEMA DEVE executar testes de integração contra container PostgreSQL efêmero via
   Testcontainers.
10. QUANDO `dotnet test` for executado, O SISTEMA DEVE passar todos os testes sem falhas.

### Requirement 14: Testes do Frontend

**User Story:** Como desenvolvedor, quero cobertura de testes unitários para os componentes e
services do frontend de tipos de despesa, para que regressões sejam detectadas automaticamente.

#### Acceptance Criteria

1. O SISTEMA DEVE cobrir por teste unitário o `ExpenseTypeService` Angular: chamadas HTTP
   corretas (URL, método, parâmetros) para listagem, consulta por ID, criação, atualização e
   exclusão.
2. O SISTEMA DEVE cobrir por teste unitário o `ExpenseTypeListComponent`: renderização da tabela,
   disparo de busca com debounce, estados de loading, vazio e erro, navegação para inclusão e
   edição.
3. O SISTEMA DEVE cobrir por teste unitário o `ExpenseTypeFormComponent`: validação do campo Nome
   obrigatório, envio com dados válidos, preenchimento no modo edição, navegação ao cancelar.
4. O SISTEMA DEVE cobrir por teste unitário o `ConfirmDialogComponent`: exibição de mensagem
   dinâmica, emissão de evento ao confirmar, emissão de evento ao cancelar.
5. QUANDO `ng test --watch=false` for executado, O SISTEMA DEVE passar todos os testes sem falhas.
