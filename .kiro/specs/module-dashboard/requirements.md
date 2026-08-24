# Requirements Document

## Introduction

**module-dashboard — Dashboard financeiro (API + Frontend).** Quarta fatia vertical de módulo de
negócio pós-walking skeleton. Cobre as Stories **PP-34 (Endpoint de Dashboard — API)** e **PP-74
(Dashboard — Frontend)** do backlog PAGA.

Implementa o endpoint `GET /api/dashboard` que retorna métricas financeiras agregadas (saldo
atual, receitas do mês, despesas do mês e despesas agrupadas por tipo) com consulta otimizada no
banco via `GroupBy`/`Sum`, além do componente Angular que exibe cards de métricas, gráfico de
barras horizontal com ngx-charts e suporte a seleção de mês.

O dashboard é a página de destino após o login. O `DashboardComponent` placeholder existente
(criado na `mvp-3`) será substituído pela implementação funcional, mantendo a mesma rota.

**Dentro do escopo:** `DashboardController`, `IDashboardService` e implementação com queries
otimizadas, `DashboardResponseDto`, seleção de mês via query param `?month=YYYY-MM`;
`DashboardService` Angular, `DashboardComponent` com cards responsivos e gráfico,
`MetricCardComponent` reutilizável em `shared/`, skeleton de loading, estados vazio e de erro,
testes unitários e de integração de ambas as camadas.

**Fora do escopo:** CRUD de Receitas, Despesas ou Tipos de Despesa; alterações em autenticação ou
usuários; mudanças de infraestrutura AWS; seletor de período customizado (apenas mês inteiro).

## Glossary

| Termo | Significado |
|-------|-------------|
| **Dashboard** | Tela de visão geral financeira do usuário, exibindo métricas agregadas e gráfico |
| **CurrentBalance** | Saldo atual: soma de todas as receitas (all time) menos soma de todas as despesas (all time) do usuário |
| **MonthlyIncome** | Soma de receitas do mês consultado (campo `Date` dentro do mês) |
| **MonthlyExpense** | Soma de despesas do mês consultado (campo `DueDate` dentro do mês) |
| **ExpensesByType** | Array de objetos `{ typeName, total }` representando despesas do mês agrupadas por tipo de despesa |
| **API** | Backend ASP.NET Core exposto em `/api/dashboard` |
| **Frontend** | Aplicação Angular 19 SPA que consome a API |
| **MetricCard** | Componente visual reutilizável que exibe título, valor formatado e cor contextual |
| **Multi-tenant** | Isolamento de dados por usuário; toda query filtra por `UserId` derivado do token via `ICurrentUserService` |
| **ProblemDetails** | Formato RFC 7807 para respostas de erro HTTP |
| **MonthParam** | Query parameter `month` no formato `YYYY-MM`; quando omitido, default é o mês corrente |
| **ngx-charts** | Biblioteca Angular para gráficos; utilizada para o gráfico de barras horizontais |
| **Skeleton** | Placeholder animado (shimmer/pulse) exibido durante o carregamento dos dados |

## Requirements

### Requirement 1: Endpoint de dashboard (API)

**User Story:** Como usuário autenticado, quero obter minhas métricas financeiras agregadas em
uma única chamada, para que o dashboard possa exibir saldo, receitas e despesas do mês de forma
eficiente.

#### Acceptance Criteria

1. QUANDO uma requisição `GET /api/dashboard` for recebida com autenticação válida e sem
   parâmetro `month`, A API DEVE responder HTTP 200 com `currentBalance`, `monthlyIncome`,
   `monthlyExpense` e `expensesByType` calculados para o mês corrente.
2. QUANDO o parâmetro `month` for informado no formato `YYYY-MM`, A API DEVE calcular
   `monthlyIncome`, `monthlyExpense` e `expensesByType` para o mês especificado.
3. A API DEVE calcular `currentBalance` como a soma de todas as receitas (all time) menos a soma
   de todas as despesas (all time) do usuário, independentemente do mês consultado.
4. A API DEVE calcular `monthlyIncome` como a soma dos valores das receitas cujo campo `Date`
   esteja dentro do mês consultado.
5. A API DEVE calcular `monthlyExpense` como a soma dos valores das despesas cujo campo `DueDate`
   esteja dentro do mês consultado.
6. A API DEVE calcular `expensesByType` agrupando as despesas do mês consultado por
   `ExpenseType.Name` e somando seus valores, retornando array de `{ typeName, total }`.
7. QUANDO o usuário não possuir receitas nem despesas no mês consultado, A API DEVE responder com
   `monthlyIncome: 0`, `monthlyExpense: 0` e `expensesByType: []`.
8. QUANDO o usuário não possuir receitas nem despesas em nenhum período, A API DEVE responder com
   `currentBalance: 0`.
9. A API DEVE executar as agregações no banco de dados via `GroupBy` e `Sum`, sem carregar
   entidades em memória.
10. QUANDO a requisição não possuir token de autenticação válido, A API DEVE responder HTTP 401.

### Requirement 2: Validação do parâmetro de mês (API)

**User Story:** Como usuário autenticado, quero que a API rejeite formatos de mês inválidos, para
que erros de digitação sejam detectados imediatamente.

#### Acceptance Criteria

1. QUANDO o parâmetro `month` for informado com formato diferente de `YYYY-MM`, A API DEVE
   responder HTTP 400 com ProblemDetails e mensagem em pt-BR.
2. QUANDO o parâmetro `month` for informado com mês fora do intervalo válido (ex: `2024-13`,
   `2024-00`), A API DEVE responder HTTP 400.
3. QUANDO o parâmetro `month` não for informado, A API DEVE utilizar o mês corrente como padrão
   sem retornar erro.

### Requirement 3: Isolamento multi-tenant (API)

**User Story:** Como usuário autenticado, quero que o dashboard exiba apenas meus dados, para que
informações financeiras de outros usuários não sejam expostas.

#### Acceptance Criteria

1. A API DEVE filtrar todas as consultas de receitas e despesas por `UserId` derivado do token via
   `ICurrentUserService`.
2. QUANDO dois usuários consultarem o dashboard no mesmo mês, A API DEVE retornar métricas
   calculadas exclusivamente sobre os dados de cada respectivo usuário.
3. A API DEVE garantir que `expensesByType` contenha apenas tipos de despesa com lançamentos do
   próprio usuário no mês consultado.

### Requirement 4: Cards de métricas (Frontend)

**User Story:** Como usuário autenticado, quero ver saldo atual, receitas e despesas do mês em
cards destacados, para que eu tenha visão imediata da minha situação financeira.

#### Acceptance Criteria

1. QUANDO o dashboard carregar com sucesso, O FRONTEND DEVE exibir três cards de métrica: "Saldo
   Atual", "Receitas do Mês" e "Despesas do Mês".
2. O FRONTEND DEVE formatar os valores no padrão BRL (`R$ 1.234,56`).
3. O FRONTEND DEVE exibir o valor do card "Saldo Atual" em cor azul (`#3b82f6`) quando positivo ou
   zero, e em cor vermelha (`#ef4444`) quando negativo.
4. O FRONTEND DEVE exibir o valor do card "Receitas do Mês" em cor verde (`#10b981`).
5. O FRONTEND DEVE exibir o valor do card "Despesas do Mês" em cor vermelha (`#ef4444`).
6. O FRONTEND DEVE exibir como subtítulo do card "Saldo Atual" o texto "Total acumulado".
7. O FRONTEND DEVE exibir como subtítulo dos cards de receitas e despesas o nome do mês em
   português (ex: "Janeiro 2025").
8. O FRONTEND DEVE utilizar o componente reutilizável `MetricCardComponent` de `shared/` para
   renderizar cada card.
9. O FRONTEND DEVE renderizar os cards em layout de grid responsivo (três colunas em desktop,
   uma coluna em mobile).

### Requirement 5: MetricCardComponent reutilizável (Frontend)

**User Story:** Como desenvolvedor, quero um componente de card de métrica em `shared/`, para que
o dashboard e futuros módulos possam exibir valores destacados de forma consistente.

#### Acceptance Criteria

1. O COMPONENTE DEVE aceitar inputs para: título (string), valor (number), cor do valor (string) e
   subtítulo opcional (string).
2. O COMPONENTE DEVE formatar o valor numérico recebido no padrão BRL (`R$ 1.234,56`).
3. O COMPONENTE DEVE aplicar a cor recebida ao texto do valor.
4. O COMPONENTE DEVE renderizar com fundo branco, borda `#e2e8f0`, border-radius 12px e padding
   24px conforme o design Figma.
5. O COMPONENTE DEVE residir em `shared/` para reuso em qualquer módulo.
6. O COMPONENTE DEVE funcionar corretamente em dark mode e light mode.

### Requirement 6: Gráfico de despesas por tipo (Frontend)

**User Story:** Como usuário autenticado, quero ver um gráfico de barras horizontal com minhas
despesas agrupadas por tipo, para que eu identifique visualmente onde gasto mais.

#### Acceptance Criteria

1. QUANDO o dashboard carregar com sucesso e `expensesByType` contiver dados, O FRONTEND DEVE
   exibir um gráfico de barras horizontal utilizando ngx-charts.
2. O FRONTEND DEVE exibir o nome do tipo de despesa à esquerda de cada barra e o valor formatado
   (`R$ 1.234,56`) à direita.
3. O FRONTEND DEVE exibir o título da seção como "Despesas por Tipo - [Mês Ano]" (ex: "Despesas
   por Tipo - Janeiro 2025").
4. QUANDO `expensesByType` for um array vazio, O FRONTEND DEVE exibir o estado vazio com mensagem
   "Nenhuma despesa registrada neste mês" em vez do gráfico.
5. O FRONTEND DEVE renderizar o gráfico dentro de um card com fundo branco, borda `#e2e8f0` e
   border-radius 12px.
6. O FRONTEND DEVE dimensionar o gráfico de forma responsiva, ocupando a largura total disponível.

### Requirement 7: Seletor de mês (Frontend)

**User Story:** Como usuário autenticado, quero selecionar um mês específico para consultar, para
que eu possa analisar minhas finanças de períodos anteriores.

#### Acceptance Criteria

1. O FRONTEND DEVE exibir um seletor de mês que permita ao usuário escolher mês e ano.
2. QUANDO o usuário selecionar um mês diferente, O FRONTEND DEVE enviar `GET /api/dashboard?month=YYYY-MM`
   e atualizar os cards e o gráfico com os novos dados.
3. O FRONTEND DEVE exibir o mês corrente como valor padrão ao carregar o dashboard.
4. O FRONTEND DEVE exibir o nome do mês em português no seletor (ex: "Janeiro 2025").
5. ENQUANTO os dados do novo mês estiverem carregando, O FRONTEND DEVE exibir indicador de
   loading nos cards e no gráfico.

### Requirement 8: Estados de loading e erro (Frontend)

**User Story:** Como usuário autenticado, quero feedback visual durante o carregamento e em caso
de erro, para que eu saiba o que está acontecendo com a aplicação.

#### Acceptance Criteria

1. ENQUANTO a requisição ao endpoint de dashboard estiver em andamento, O FRONTEND DEVE exibir
   skeleton de loading (shimmer animado) nos cards e na área do gráfico.
2. SE a API retornar erro (500, timeout), O FRONTEND DEVE exibir o estado de erro com mensagem
   "Erro ao carregar dados" e botão "Tentar Novamente".
3. QUANDO o usuário clicar em "Tentar Novamente", O FRONTEND DEVE repetir a requisição ao
   endpoint com os mesmos parâmetros.
4. QUANDO a requisição não possuir token de autenticação válido (401), O FRONTEND DEVE
   redirecionar para a tela de login conforme comportamento do interceptor já existente.
5. O FRONTEND DEVE aplicar o skeleton com animação CSS pulse/shimmer, barras cinza (`#e2e8f0`)
   com dimensões e border-radius consistentes com o design Figma.

### Requirement 9: Responsividade e temas (Frontend)

**User Story:** Como usuário autenticado, quero que o dashboard se adapte a diferentes tamanhos de
tela e ao tema escolhido, para que eu tenha boa experiência em qualquer dispositivo e preferência
visual.

#### Acceptance Criteria

1. O FRONTEND DEVE renderizar os três cards em layout grid de três colunas em telas desktop
   (≥ 1024px) e uma coluna em telas mobile (< 768px).
2. O FRONTEND DEVE posicionar a seção do gráfico abaixo dos cards, ocupando a largura total.
3. O FRONTEND DEVE aplicar cores, backgrounds e bordas adequados em dark mode, utilizando CSS
   custom properties definidas no tema.
4. O FRONTEND DEVE manter legibilidade de textos e contraste adequado em ambos os temas (WCAG AA).

### Requirement 10: Navegação e integração com shell (Frontend)

**User Story:** Como usuário autenticado, quero que o dashboard seja minha página inicial após o
login, para que eu veja imediatamente minha situação financeira.

#### Acceptance Criteria

1. O FRONTEND DEVE substituir o `DashboardComponent` placeholder existente pela implementação
   funcional, mantendo a mesma rota (rota padrão após login).
2. O FRONTEND DEVE manter o item "Dashboard" no menu lateral com ícone e destaque de rota ativa.
3. O FRONTEND DEVE carregar os dados do dashboard automaticamente ao acessar a rota, sem ação
   adicional do usuário.

### Requirement 11: Testes da API (Backend)

**User Story:** Como desenvolvedor, quero cobertura de testes unitários e de integração para o
endpoint de dashboard, para que regressões sejam detectadas automaticamente.

#### Acceptance Criteria

1. O SISTEMA DEVE cobrir por teste unitário o `DashboardService`: cenário sem dados (zeros),
   cenário com receitas e despesas no mês retornando cálculos corretos, cenário com `month`
   específico diferente do mês corrente, cenário com múltiplos tipos de despesa em
   `expensesByType`, cenário com saldo negativo (despesas > receitas all time).
2. O SISTEMA DEVE cobrir por teste de integração o fluxo HTTP completo: `GET /api/dashboard`
   retorna 200 com shape correto (`currentBalance`, `monthlyIncome`, `monthlyExpense`,
   `expensesByType`).
3. O SISTEMA DEVE cobrir por teste de integração que `GET /api/dashboard?month=2024-03` retorna
   dados filtrados para março de 2024.
4. O SISTEMA DEVE cobrir por teste de integração que `GET /api/dashboard?month=invalid` retorna
   400.
5. O SISTEMA DEVE cobrir por teste de integração o isolamento entre usuários: dados do usuário A
   não afetam o dashboard do usuário B.
6. O SISTEMA DEVE cobrir por teste de integração que requisições sem token retornam 401.
7. O SISTEMA DEVE executar testes de integração contra container PostgreSQL efêmero via
   Testcontainers.
8. QUANDO `dotnet test` for executado, O SISTEMA DEVE passar todos os testes sem falhas.

### Requirement 12: Testes do Frontend

**User Story:** Como desenvolvedor, quero cobertura de testes unitários para os componentes e
services do frontend do dashboard, para que regressões sejam detectadas automaticamente.

#### Acceptance Criteria

1. O SISTEMA DEVE cobrir por teste unitário o `DashboardService` Angular: chamada HTTP correta
   (URL `GET /api/dashboard`, método, query param `month` quando informado).
2. O SISTEMA DEVE cobrir por teste unitário o `DashboardComponent`: exibição dos três cards com
   valores formatados, skeleton durante loading, estado de erro com botão "Tentar Novamente",
   renderização do gráfico quando há dados em `expensesByType`, estado vazio quando
   `expensesByType` é array vazio.
3. O SISTEMA DEVE cobrir por teste unitário o `MetricCardComponent`: renderização de título,
   valor formatado e cor correta.
4. O SISTEMA DEVE cobrir por teste unitário o componente de gráfico: renderização das barras
   com dados mockados, ausência do gráfico quando array vazio.
5. O SISTEMA DEVE cobrir por teste unitário a troca de mês: disparo de nova requisição e
   atualização dos dados exibidos.
6. QUANDO `ng test --watch=false` for executado, O SISTEMA DEVE passar todos os testes sem falhas.
