# Requirements Document

## Introduction

**module-incomes — CRUD Receitas (API + Frontend).** Segunda fatia vertical de módulo de negócio
pós-walking skeleton. Cobre as Stories **PP-32 (CRUD Receitas — API)** e **PP-72 (CRUD Receitas —
Frontend)** do backlog PAGA.

Implementa a API REST completa para gerenciamento de Receitas (listagem com filtros de data,
descrição e recorrência + paginação, consulta por ID, criação, atualização e exclusão), além das
telas Angular de listagem com filtros avançados, inclusão e alteração — substituindo o placeholder
"Em construção" atualmente exibido no menu.

A entidade `Income` (com `RecurrenceFrequency` converter e índice `(UserId, Date)`), sua
configuração EF Core e a migration correspondente já existem desde a `mvp-1`. Nenhuma nova
migration é necessária.

**Dentro do escopo:** `IncomesController`, `IIncomeService` e implementação, DTOs, validadores
FluentValidation (incluindo validação condicional de recorrência), testes unitários e de integração
da API; `IncomeService` Angular, `IncomeListComponent` com filtros (dateFrom, dateTo, description,
isRecurring), `IncomeFormComponent` com lógica condicional de recorrência, directive de máscara
monetária reutilizável em `shared/`, componente de recorrência compartilhado em `shared/`, rotas
do módulo, testes unitários do frontend.

**Fora do escopo:** módulos de Tipos de Despesa, Despesas e Dashboard; alterações em autenticação
ou usuários; migrations; mudanças de infraestrutura AWS.

## Glossary

| Termo | Significado |
|-------|-------------|
| **Income** | Entrada de dinheiro (receita); pertence a um único usuário (`UserId`). Entidade com `Id` (int, identity), `UserId` (FK), `Date` (DateOnly), `Description` (max 300), `Value` (decimal 18,2), `IsRecurring` (bool), `Frequency` (RecurrenceFrequency? — weekly/monthly/yearly) |
| **API** | Backend ASP.NET Core exposto em `/api/incomes` |
| **Frontend** | Aplicação Angular 19 SPA que consome a API |
| **Paginação** | Envelope padrão `{ items, pageNumber, pageSize, totalCount, totalPages }` com `pageNumber` começando em 1 e `pageSize` default 10, máximo 100 |
| **ProblemDetails** | Formato RFC 7807 para respostas de erro HTTP, com campos `type`, `title`, `status` e `errors` |
| **Multi-tenant** | Isolamento de dados por usuário; toda query filtra por `UserId` derivado do token via `ICurrentUserService` |
| **Recorrência** | Quando `IsRecurring = true`, o campo `Frequency` é obrigatório (`weekly`, `monthly` ou `yearly`). Quando `IsRecurring = false`, `Frequency` deve ser `null` |
| **Debounce** | Atraso de 300ms entre a última tecla digitada e o disparo da requisição de busca, evitando chamadas excessivas |
| **CurrencyMask** | Directive Angular reutilizável que formata entrada numérica como moeda BRL (`R$ 1.234,56`) no input, enviando o valor numérico puro no payload |
| **RecurrenceSelector** | Componente compartilhado em `shared/` que encapsula o toggle de recorrência e o select de frequência, reutilizável pelos módulos de Receitas e Despesas |
| **ConfirmDialog** | Componente Angular Material compartilhado que exibe modal de confirmação antes de ações destrutivas |

## Requirements

### Requirement 1: Listagem de receitas (API)

**User Story:** Como usuário autenticado, quero listar minhas receitas com filtros e paginação,
para que eu possa localizar entradas financeiras por período, descrição ou recorrência.

#### Acceptance Criteria

1. QUANDO uma requisição `GET /api/incomes` for recebida com autenticação válida, A API DEVE
   responder HTTP 200 com o envelope de paginação contendo apenas as receitas do usuário
   autenticado.
2. QUANDO o parâmetro `dateFrom` for informado, A API DEVE filtrar receitas com `Date >=
   dateFrom`.
3. QUANDO o parâmetro `dateTo` for informado, A API DEVE filtrar receitas com `Date <= dateTo`.
4. QUANDO o parâmetro `description` for informado, A API DEVE filtrar receitas cuja descrição
   contenha o valor informado, sem distinção de maiúsculas e minúsculas.
5. QUANDO o parâmetro `isRecurring` for informado, A API DEVE filtrar receitas com valor de
   `IsRecurring` correspondente.
6. A API DEVE aplicar paginação com `pageNumber` (default 1) e `pageSize` (default 10, máximo
   100).
7. A API DEVE retornar cada receita com `id`, `date`, `description`, `value`, `isRecurring` e
   `frequency`.
8. A API DEVE ordenar os resultados por `Date` decrescente (mais recentes primeiro) como padrão.
9. QUANDO a requisição não possuir token de autenticação válido, A API DEVE responder HTTP 401.
10. A API DEVE executar a consulta com `AsNoTracking` e projetar diretamente para DTO.

### Requirement 2: Consulta de receita por ID (API)

**User Story:** Como usuário autenticado, quero consultar uma receita específica, para que eu
possa obter seus detalhes antes de editá-la.

#### Acceptance Criteria

1. QUANDO uma requisição `GET /api/incomes/{id}` for recebida com ID existente e pertencente ao
   usuário autenticado, A API DEVE responder HTTP 200 com `id`, `date`, `description`, `value`,
   `isRecurring` e `frequency`.
2. QUANDO o ID não existir ou pertencer a outro usuário, A API DEVE responder HTTP 404.
3. QUANDO a requisição não possuir token de autenticação válido, A API DEVE responder HTTP 401.

### Requirement 3: Criação de receita (API)

**User Story:** Como usuário autenticado, quero criar novas receitas, para que eu possa registrar
minhas entradas financeiras.

#### Acceptance Criteria

1. QUANDO uma requisição `POST /api/incomes` for recebida com payload válido, A API DEVE criar a
   receita associada ao usuário autenticado e responder HTTP 201 com `id`, `date`, `description`,
   `value`, `isRecurring` e `frequency`.
2. A API DEVE validar que `date` é obrigatório.
3. A API DEVE validar que `description` é obrigatória e não vazia.
4. A API DEVE validar que `value` é obrigatório e maior que zero.
5. QUANDO `isRecurring` for `true`, A API DEVE validar que `frequency` é obrigatório e contém
   valor válido (`weekly`, `monthly` ou `yearly`).
6. QUANDO `isRecurring` for `false`, A API DEVE validar que `frequency` é `null`.
7. QUANDO a validação falhar, A API DEVE responder HTTP 400 com `ProblemDetails` e mensagens em
   pt-BR.
8. A API DEVE derivar o `UserId` exclusivamente das claims do token, ignorando qualquer valor no
   payload.
9. QUANDO a requisição não possuir token de autenticação válido, A API DEVE responder HTTP 401.

### Requirement 4: Atualização de receita (API)

**User Story:** Como usuário autenticado, quero alterar uma receita existente, para que eu possa
corrigir dados ou atualizar recorrência.

#### Acceptance Criteria

1. QUANDO uma requisição `PUT /api/incomes/{id}` for recebida com payload válido e ID existente
   pertencente ao usuário autenticado, A API DEVE atualizar todos os campos e responder HTTP 200
   com `id`, `date`, `description`, `value`, `isRecurring` e `frequency` atualizados.
2. QUANDO o ID não existir ou pertencer a outro usuário, A API DEVE responder HTTP 404.
3. A API DEVE validar que `date` é obrigatório.
4. A API DEVE validar que `description` é obrigatória e não vazia.
5. A API DEVE validar que `value` é obrigatório e maior que zero.
6. QUANDO `isRecurring` for `true`, A API DEVE validar que `frequency` é obrigatório e contém
   valor válido (`weekly`, `monthly` ou `yearly`).
7. QUANDO `isRecurring` for `false`, A API DEVE validar que `frequency` é `null`.
8. QUANDO a validação falhar, A API DEVE responder HTTP 400 com `ProblemDetails` e mensagens em
   pt-BR.
9. QUANDO a requisição não possuir token de autenticação válido, A API DEVE responder HTTP 401.

### Requirement 5: Exclusão de receita (API)

**User Story:** Como usuário autenticado, quero excluir uma receita, para que entradas incorretas
ou obsoletas possam ser removidas.

#### Acceptance Criteria

1. QUANDO uma requisição `DELETE /api/incomes/{id}` for recebida com ID existente pertencente ao
   usuário autenticado, A API DEVE excluir a receita e responder HTTP 204.
2. QUANDO o ID não existir ou pertencer a outro usuário, A API DEVE responder HTTP 404.
3. QUANDO a requisição não possuir token de autenticação válido, A API DEVE responder HTTP 401.

### Requirement 6: Isolamento multi-tenant (API)

**User Story:** Como usuário autenticado, quero que minhas receitas sejam privadas, para que
outros usuários não possam visualizar, alterar ou excluir meus lançamentos.

#### Acceptance Criteria

1. A API DEVE filtrar todas as consultas de receitas por `UserId` derivado do token via
   `ICurrentUserService`.
2. QUANDO um usuário tentar acessar, atualizar ou excluir uma receita pertencente a outro
   usuário, A API DEVE responder HTTP 404 sem revelar que o recurso existe.
3. A API DEVE garantir que a criação de receita associe o registro exclusivamente ao `UserId` do
   token autenticado.

### Requirement 7: Tela de listagem de receitas (Frontend)

**User Story:** Como usuário autenticado, quero visualizar minhas receitas em uma tabela com
filtros avançados e paginação, para que eu possa navegar e gerenciar meus lançamentos.

#### Acceptance Criteria

1. QUANDO o usuário navegar para a rota de receitas, O FRONTEND DEVE exibir uma tabela com colunas
   Data, Descrição, Valor e Recorrente, populada via `GET /api/incomes`.
2. O FRONTEND DEVE exibir barra de filtros com: Data de (datepicker), Data até (datepicker),
   Descrição (input com debounce de 300ms) e Recorrente (select: Todos/Sim/Não).
3. QUANDO qualquer filtro for alterado, O FRONTEND DEVE enviar os parâmetros correspondentes à API
   e exibir os resultados filtrados, reiniciando a paginação para a primeira página.
4. O FRONTEND DEVE formatar a coluna Data no padrão `dd/mm/yyyy`.
5. O FRONTEND DEVE formatar a coluna Valor no padrão `R$ 1.234,56` usando a cor de sucesso
   (`--success` / `#10b981`).
6. O FRONTEND DEVE exibir a coluna Recorrente como texto "Sim" ou "Não".
7. O FRONTEND DEVE exibir um botão "+ Nova Receita" (primário) que navegue para a tela de
   inclusão.
8. O FRONTEND DEVE exibir botões "Editar" e "Excluir" em cada linha da tabela.
9. O FRONTEND DEVE implementar paginação funcional sincronizada com a API.
10. ENQUANTO os dados estiverem carregando, O FRONTEND DEVE exibir um skeleton de loading.
11. QUANDO a API retornar lista vazia, O FRONTEND DEVE exibir o estado vazio com mensagem
    "Nenhum registro encontrado" e sugestão para ajustar filtros ou adicionar novo item.
12. SE a API retornar erro, O FRONTEND DEVE exibir o estado de erro com mensagem "Erro ao
    carregar dados" e botão "Tentar Novamente".

### Requirement 8: Tela de inclusão de receita (Frontend)

**User Story:** Como usuário autenticado, quero criar uma nova receita via formulário com suporte
a recorrência, para que eu possa registrar minhas entradas financeiras.

#### Acceptance Criteria

1. QUANDO o usuário navegar para a rota de inclusão, O FRONTEND DEVE exibir formulário com título
   "Nova Receita", campos Data (datepicker, placeholder "dd/mm/aaaa"), Descrição (text input),
   Valor (com máscara monetária, placeholder "R$ 0,00"), toggle Recorrente e botões "Salvar"
   (primário) e "Cancelar" (secundário).
2. O FRONTEND DEVE validar que Data, Descrição e Valor são obrigatórios antes de permitir o envio.
3. O FRONTEND DEVE validar que Valor é maior que zero.
4. QUANDO o toggle Recorrente for ativado, O FRONTEND DEVE exibir o campo Frequência (select com
   opções: Semanal, Mensal, Anual) e validar que é obrigatório.
5. QUANDO o toggle Recorrente for desativado, O FRONTEND DEVE ocultar o campo Frequência e enviar
   `frequency: null` no payload.
6. QUANDO o usuário clicar em "Salvar" com dados válidos, O FRONTEND DEVE enviar
   `POST /api/incomes` e, em caso de sucesso, exibir snackbar de sucesso e navegar de volta para a
   listagem.
7. QUANDO a API retornar erro (400, 500), O FRONTEND DEVE exibir a mensagem de erro retornada pela
   API em snackbar.
8. QUANDO o usuário clicar em "Cancelar", O FRONTEND DEVE navegar de volta para a listagem sem
   enviar requisição.
9. ENQUANTO a requisição de criação estiver em andamento, O FRONTEND DEVE desabilitar o botão
   "Salvar" para evitar envio duplicado.
10. O FRONTEND DEVE utilizar o componente compartilhado `RecurrenceSelector` para o toggle e o
    select de frequência.
11. O FRONTEND DEVE utilizar a directive compartilhada de máscara monetária (`CurrencyMask`) no
    campo Valor.

### Requirement 9: Tela de alteração de receita (Frontend)

**User Story:** Como usuário autenticado, quero editar uma receita existente, para que eu possa
corrigir dados ou alterar a recorrência.

#### Acceptance Criteria

1. QUANDO o usuário navegar para a rota de alteração, O FRONTEND DEVE carregar os dados da receita
   via `GET /api/incomes/{id}` e preencher o formulário com título "Editar Receita".
2. QUANDO a API retornar 404 para o ID informado, O FRONTEND DEVE navegar de volta para a listagem
   e exibir snackbar de erro.
3. O FRONTEND DEVE preencher o toggle Recorrente e exibir o campo Frequência caso a receita seja
   recorrente.
4. QUANDO o usuário clicar em "Salvar" com dados válidos, O FRONTEND DEVE enviar
   `PUT /api/incomes/{id}` e, em caso de sucesso, exibir snackbar de sucesso e navegar de volta
   para a listagem.
5. QUANDO a API retornar erro (400, 500), O FRONTEND DEVE exibir a mensagem de erro retornada pela
   API em snackbar.
6. QUANDO o usuário clicar em "Cancelar", O FRONTEND DEVE navegar de volta para a listagem sem
   enviar requisição.
7. ENQUANTO a requisição de atualização estiver em andamento, O FRONTEND DEVE desabilitar o botão
   "Salvar" para evitar envio duplicado.

### Requirement 10: Exclusão com confirmação (Frontend)

**User Story:** Como usuário autenticado, quero confirmar antes de excluir uma receita, para que
exclusões acidentais sejam evitadas.

#### Acceptance Criteria

1. QUANDO o usuário clicar em "Excluir" em uma linha da tabela, O FRONTEND DEVE exibir o
   `ConfirmDialogComponent` com título "Confirmar Exclusão" e mensagem "Deseja excluir a receita
   \"{descrição}\"? Esta ação não pode ser desfeita.".
2. QUANDO o usuário confirmar a exclusão, O FRONTEND DEVE enviar `DELETE /api/incomes/{id}` e, em
   caso de sucesso, exibir snackbar de sucesso e atualizar a listagem.
3. QUANDO a API retornar erro, O FRONTEND DEVE exibir a mensagem de erro retornada pela API em
   snackbar.
4. QUANDO o usuário clicar em "Cancelar" no modal, O FRONTEND DEVE fechar o modal sem enviar
   requisição.
5. O FRONTEND DEVE reutilizar o `ConfirmDialogComponent` compartilhado em `shared/`.

### Requirement 11: Máscara monetária reutilizável (Frontend)

**User Story:** Como desenvolvedor, quero uma directive de máscara monetária em `shared/`, para
que campos de valor em Receitas e Despesas usem o mesmo comportamento de formatação.

#### Acceptance Criteria

1. A DIRECTIVE DEVE formatar o valor digitado no padrão BRL (`R$ 1.234,56`) enquanto o usuário
   digita.
2. A DIRECTIVE DEVE aceitar apenas dígitos e separadores decimais como entrada válida.
3. A DIRECTIVE DEVE expor o valor numérico puro (sem formatação) para o form control vinculado.
4. A DIRECTIVE DEVE residir em `shared/` para reuso em Receitas e Despesas.
5. QUANDO o campo receber foco, A DIRECTIVE DEVE posicionar o cursor de forma consistente.

### Requirement 12: Componente de recorrência compartilhado (Frontend)

**User Story:** Como desenvolvedor, quero um componente de recorrência em `shared/`, para que
Receitas e Despesas compartilhem a mesma lógica de toggle + frequência.

#### Acceptance Criteria

1. O COMPONENTE DEVE encapsular um `mat-slide-toggle` para `isRecurring` e um `mat-select` para
   `frequency` com opções Semanal, Mensal e Anual.
2. QUANDO o toggle for ativado, O COMPONENTE DEVE exibir o campo de frequência e torná-lo
   obrigatório.
3. QUANDO o toggle for desativado, O COMPONENTE DEVE ocultar o campo de frequência e emitir
   `frequency: null`.
4. O COMPONENTE DEVE implementar `ControlValueAccessor` para integrar-se a formulários reativos
   como um único form control que emite `{ isRecurring: boolean, frequency: string | null }`.
5. O COMPONENTE DEVE residir em `shared/` para reuso em Receitas e Despesas.
6. QUANDO o toggle for ativado, O COMPONENTE DEVE aplicar estilo visual de destaque (label azul
   `#3b82f6`) conforme o design Figma.

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

**User Story:** Como usuário autenticado, quero acessar receitas pelo menu lateral, para que a
navegação seja consistente com os demais módulos.

#### Acceptance Criteria

1. O FRONTEND DEVE substituir o placeholder "Em construção" na rota de receitas pelo módulo
   funcional, mantendo o mesmo item no menu lateral.
2. O FRONTEND DEVE configurar rotas lazy-loaded: listagem como rota padrão, `/new` para inclusão e
   `/:id/edit` para alteração.
3. O FRONTEND DEVE utilizar um único `IncomeFormComponent` para inclusão e alteração,
   diferenciando o modo pela presença do parâmetro de rota `id`.

### Requirement 15: Testes da API (Backend)

**User Story:** Como desenvolvedor, quero cobertura de testes unitários e de integração para a API
de receitas, para que regressões sejam detectadas automaticamente.

#### Acceptance Criteria

1. O SISTEMA DEVE cobrir por teste unitário o `IncomeService`: criação com dados válidos, criação
   com recorrência válida (isRecurring=true + frequency), criação com recorrência inválida
   (isRecurring=true + frequency=null), atualização com dados válidos, atualização alternando
   recorrência de false para true, consulta por ID existente, consulta por ID inexistente (404),
   exclusão de receita existente.
2. O SISTEMA DEVE cobrir por teste unitário os validadores de `CreateIncomeDto` e
   `UpdateIncomeDto`: campo `date` obrigatório, campo `description` obrigatório e não vazio,
   campo `value` obrigatório e maior que zero, `frequency` obrigatório quando `isRecurring=true`,
   `frequency` deve ser null quando `isRecurring=false`, `frequency` com valor inválido rejeitado.
3. O SISTEMA DEVE cobrir por teste de integração o fluxo HTTP completo: POST retorna 201 com todos
   os campos, GET lista com filtros (`dateFrom`, `dateTo`, `description`, `isRecurring`) e
   paginação, GET por ID retorna 200, PUT retorna 200, DELETE retorna 204.
4. O SISTEMA DEVE cobrir por teste de integração que POST com `isRecurring=true` e
   `frequency=null` retorna 400.
5. O SISTEMA DEVE cobrir por teste de integração que POST com `value <= 0` retorna 400.
6. O SISTEMA DEVE cobrir por teste de integração o isolamento entre usuários: usuário A não
   visualiza, altera nem exclui receita de usuário B.
7. O SISTEMA DEVE cobrir por teste de integração que requisições sem token retornam 401.
8. O SISTEMA DEVE cobrir por teste de integração que requisição para ID inexistente retorna 404.
9. O SISTEMA DEVE executar testes de integração contra container PostgreSQL efêmero via
   Testcontainers.
10. QUANDO `dotnet test` for executado, O SISTEMA DEVE passar todos os testes sem falhas.

### Requirement 16: Testes do Frontend

**User Story:** Como desenvolvedor, quero cobertura de testes unitários para os componentes e
services do frontend de receitas, para que regressões sejam detectadas automaticamente.

#### Acceptance Criteria

1. O SISTEMA DEVE cobrir por teste unitário o `IncomeService` Angular: chamadas HTTP corretas
   (URL, método, parâmetros) para listagem com filtros, consulta por ID, criação, atualização e
   exclusão.
2. O SISTEMA DEVE cobrir por teste unitário o `IncomeListComponent`: renderização da tabela,
   disparo de busca com debounce, filtragem por data/descrição/recorrência, estados de loading,
   vazio e erro, navegação para inclusão e edição.
3. O SISTEMA DEVE cobrir por teste unitário o `IncomeFormComponent`: validação dos campos
   obrigatórios (Data, Descrição, Valor), validação de valor maior que zero, comportamento
   condicional do toggle de recorrência (exibir/ocultar frequência), envio com dados válidos,
   preenchimento no modo edição, navegação ao cancelar.
4. O SISTEMA DEVE cobrir por teste unitário o `RecurrenceSelector`: exibição/ocultação do select
   de frequência ao alternar o toggle, emissão do valor correto para o form control, validação de
   frequência obrigatória quando ativo.
5. O SISTEMA DEVE cobrir por teste unitário a directive `CurrencyMask`: formatação de entrada
   numérica para padrão BRL, exposição do valor numérico puro ao form control.
6. QUANDO `ng test --watch=false` for executado, O SISTEMA DEVE passar todos os testes sem falhas.
