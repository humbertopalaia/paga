# Jira Board - PAGA (Palaia Acompanhamento de Gastos Automatizado)

> Estrutura: **Epic > Story > Subtask**
> Campos por card: Título, Descrição, Acceptance Criteria, Labels, Figma Reference
> **Figma:** [Abrir Projeto](https://www.figma.com/design/PKgbbuZodtcKGlSIFuvkcO/PAGA-%E2%80%94-Palaia-Acompanhamento-de-Gastos-Automatizado?node-id=0-1&t=bmkQyCuGELTI3Iuk-0)

---

## EPIC 1: Autenticação e Usuários

### Story 1.1: Setup do projeto backend .NET 10

| Campo | Valor |
|-------|-------|
| **Labels** | `backend`, `setup` |
| **Figma** | N/A |

**Descrição:**
Criar a solução .NET com estrutura de camadas (Api, Application, Domain, Infrastructure), configurar EF Core com Npgsql, adicionar pacotes NuGet necessários e projeto de testes.

**Acceptance Criteria:**
- [ ] Solution criada com 4 projetos (Api, Application, Domain, Infrastructure) + 1 de testes
- [ ] API sobe com `dotnet run` sem erros
- [ ] Endpoint GET /health retorna 200
- [ ] Connection string configurada para PostgreSQL
- [ ] Swagger acessível em /swagger

---

### Story 1.2: Modelagem de domínio e migrations

| Campo | Valor |
|-------|-------|
| **Labels** | `backend`, `database` |
| **Figma** | N/A |

**Descrição:**
Criar entidades (User, ExpenseType, Income, Expense, RefreshToken), DbContext com configurações, e migration inicial.

**Acceptance Criteria:**
- [ ] Todas as entidades criadas com propriedades conforme modelo de dados
- [ ] Configurações de entidade (IEntityTypeConfiguration) aplicadas
- [ ] Migration inicial gerada sem erros
- [ ] `dotnet ef database update` cria todas as tabelas no PostgreSQL
- [ ] Relacionamentos FK configurados corretamente

**Subtasks:**
1. Criar entidade User (Id UUID, Name, Email, PasswordHash, CreatedAt)
2. Criar entidade ExpenseType (Id int, UserId FK, Name)
3. Criar entidade Income (Id, UserId FK, Date, Description, Value, IsRecurring, Frequency)
4. Criar entidade Expense (Id, UserId FK, DueDate, Description, ExpenseTypeId FK, Value, IsRecurring, Frequency)
5. Criar entidade RefreshToken (Id, UserId FK, Token, ExpiresAt, IsRevoked)
6. Criar DbContext e configurações
7. Gerar e testar migration inicial

---

### Story 1.3: Endpoints de autenticação (Login + Refresh)

| Campo | Valor |
|-------|-------|
| **Labels** | `backend`, `auth` |
| **Figma** | N/A |

**Descrição:**
Implementar autenticação JWT com login, refresh token e logout.

**Acceptance Criteria:**
- [ ] POST /api/auth/login retorna access token (30min) + refresh token
- [ ] POST /api/auth/login retorna 401 para credenciais inválidas
- [ ] POST /api/auth/refresh renova token com refresh token válido
- [ ] POST /api/auth/logout revoga refresh token
- [ ] Endpoints protegidos retornam 401 sem token
- [ ] Testes unitários do TokenService passando
- [ ] Testes de integração dos endpoints passando

**Subtasks:**
1. Implementar TokenService (geração JWT + refresh token)
2. Implementar AuthService (login, refresh, logout)
3. Criar DTOs de request/response (LoginRequest, TokenResponse)
4. Criar AuthController com endpoints
5. Configurar JWT middleware no Program.cs
6. Escrever testes unitários
7. Escrever testes de integração

---

### Story 1.4: CRUD Usuários (API)

| Campo | Valor |
|-------|-------|
| **Labels** | `backend`, `crud`, `usuarios` |
| **Figma** | N/A |

**Descrição:**
Implementar API REST completa para gerenciamento de usuários (cadastro, listagem, alteração, exclusão). Não é auto-registro público — é uma tela de cadastro de usuários do sistema.

**Acceptance Criteria:**
- [ ] GET /api/users retorna lista paginada com filtros (name, email)
- [ ] GET /api/users/{id} retorna usuário específico ou 404
- [ ] POST /api/users cria novo usuário (valida email único, senha hasheada com BCrypt)
- [ ] PUT /api/users/{id} atualiza dados do usuário (nome, email, senha opcional)
- [ ] DELETE /api/users/{id} exclui usuário
- [ ] Todos os endpoints exigem autenticação
- [ ] Testes passando

**Subtasks:**
1. Criar DTOs (CreateUserDto, UpdateUserDto, UserResponseDto)
2. Criar IUserService e implementação
3. Criar validadores FluentValidation (email válido, senha mínima 6 chars, email único)
4. Criar UsersController
5. Implementar filtro e paginação
6. Escrever testes unitários e de integração

---

## EPIC 2: Backend - Módulos de Negócio

### Story 2.1: CRUD Tipo de Despesa (API)

| Campo | Valor |
|-------|-------|
| **Labels** | `backend`, `crud`, `tipo-despesa` |
| **Figma** | N/A |

**Descrição:**
Implementar API REST completa para gerenciamento de Tipos de Despesa com filtros e paginação.

**Acceptance Criteria:**
- [ ] GET /api/expense-types retorna lista paginada (pageSize, pageNumber)
- [ ] GET /api/expense-types aceita filtro por nome (query param `?name=`)
- [ ] GET /api/expense-types/{id} retorna item específico ou 404
- [ ] POST /api/expense-types cria novo tipo (valida nome não vazio e único por usuário)
- [ ] PUT /api/expense-types/{id} atualiza tipo existente
- [ ] DELETE /api/expense-types/{id} exclui tipo (retorna 409 se houver despesas vinculadas)
- [ ] Todos os endpoints exigem autenticação
- [ ] Dados isolados por usuário (multi-tenant)
- [ ] Testes passando

**Subtasks:**
1. Criar DTOs (CreateExpenseTypeDto, UpdateExpenseTypeDto, ExpenseTypeResponseDto)
2. Criar IExpenseTypeService e implementação
3. Criar validadores FluentValidation
4. Criar ExpenseTypesController
5. Implementar filtro e paginação no repository
6. Escrever testes unitários e de integração

---

### Story 2.2: CRUD Receitas (API)

| Campo | Valor |
|-------|-------|
| **Labels** | `backend`, `crud`, `receitas` |
| **Figma** | N/A |

**Descrição:**
Implementar API REST completa para Receitas com suporte a recorrência configurável.

**Acceptance Criteria:**
- [ ] GET /api/incomes retorna lista paginada com filtros (dateFrom, dateTo, description, isRecurring)
- [ ] GET /api/incomes/{id} retorna item específico ou 404
- [ ] POST /api/incomes cria receita (valida valor > 0, data obrigatória)
- [ ] POST /api/incomes valida que Frequency é obrigatório quando IsRecurring = true
- [ ] PUT /api/incomes/{id} atualiza receita existente
- [ ] DELETE /api/incomes/{id} exclui receita
- [ ] Frequency aceita apenas: weekly, monthly, yearly
- [ ] Dados isolados por usuário
- [ ] Testes passando

**Subtasks:**
1. Criar DTOs de Income
2. Criar IIncomeService e implementação
3. Criar validadores (incluindo validação condicional de Frequency)
4. Criar IncomesController
5. Implementar filtros e paginação
6. Escrever testes

---

### Story 2.3: CRUD Despesas (API)

| Campo | Valor |
|-------|-------|
| **Labels** | `backend`, `crud`, `despesas` |
| **Figma** | N/A |

**Descrição:**
Implementar API REST completa para Despesas com relacionamento a Tipo e recorrência.

**Acceptance Criteria:**
- [ ] GET /api/expenses retorna lista paginada com filtros (dueDateFrom, dueDateTo, expenseTypeId, description, isRecurring)
- [ ] GET /api/expenses/{id} retorna item com dados do tipo incluso ou 404
- [ ] POST /api/expenses cria despesa (valida valor > 0, vencimento obrigatório, expenseTypeId válido)
- [ ] POST /api/expenses valida Frequency obrigatório quando IsRecurring = true
- [ ] PUT /api/expenses/{id} atualiza despesa
- [ ] DELETE /api/expenses/{id} exclui despesa
- [ ] Response inclui nome do tipo de despesa (join)
- [ ] Dados isolados por usuário
- [ ] Testes passando

**Subtasks:**
1. Criar DTOs de Expense (incluindo ExpenseTypeName no response)
2. Criar IExpenseService e implementação
3. Criar validadores
4. Criar ExpensesController
5. Implementar filtros, paginação e include de tipo
6. Escrever testes

---

### Story 2.4: Endpoint de Dashboard (API)

| Campo | Valor |
|-------|-------|
| **Labels** | `backend`, `dashboard` |
| **Figma** | N/A |

**Descrição:**
Implementar endpoint que calcula e retorna métricas financeiras do mês corrente.

**Acceptance Criteria:**
- [ ] GET /api/dashboard retorna JSON com:
  - `currentBalance` (total receitas all time - total despesas all time)
  - `monthlyIncome` (soma receitas do mês corrente)
  - `monthlyExpense` (soma despesas do mês corrente)
  - `expensesByType` (array com `{typeName, total}` para gráfico de barras, mês corrente)
- [ ] Aceita query param `?month=YYYY-MM` para consultar mês específico (default: mês atual)
- [ ] Exige autenticação
- [ ] Dados isolados por usuário
- [ ] Testes passando

**Subtasks:**
1. Criar DashboardResponseDto
2. Criar IDashboardService e implementação com queries otimizadas
3. Criar DashboardController
4. Escrever testes com cenários de dados variados

---

## EPIC 3: Frontend - Estrutura e Tema

### Story 3.1: Setup do projeto Angular 19

| Campo | Valor |
|-------|-------|
| **Labels** | `frontend`, `setup` |
| **Figma** | N/A |

**Descrição:**
Criar projeto Angular com estrutura de pastas (core, shared, features), configurar SCSS, roteamento com lazy loading, e adicionar dependências base.

**Acceptance Criteria:**
- [ ] Projeto criado com Angular 19 (standalone components)
- [ ] Estrutura de pastas: core/, shared/, features/
- [ ] Roteamento configurado com lazy loading por feature
- [ ] Build (`ng build`) sem erros
- [ ] `ng test` rodando sem falhas
- [ ] Angular Material ou biblioteca de UI configurada

---

### Story 3.2: Sistema de tema (Dark/Light + Paleta Azul)

| Campo | Valor |
|-------|-------|
| **Labels** | `frontend`, `tema`, `ux` |
| **Figma** | `Page: Componentes > Frame: Design Tokens` |

**Descrição:**
Implementar sistema de temas com CSS custom properties, paleta azul, toggle dark/light mode com persistência em localStorage.

**Acceptance Criteria:**
- [ ] CSS custom properties definidas para light e dark mode
- [ ] Paleta de cores azul implementada (primary, secondary, accent, backgrounds, text)
- [ ] ThemeService com signal para estado do tema
- [ ] Toggle funcional com animação suave na transição
- [ ] Preferência do sistema (prefers-color-scheme) respeitada no primeiro acesso
- [ ] Persistência em localStorage
- [ ] Todos os componentes compartilhados usando as variáveis CSS

**Subtasks:**
1. Criar variáveis SCSS (light e dark)
2. Criar ThemeService com signals
3. Criar componente de toggle (ícone sol/lua)
4. Testar ThemeService

---

### Story 3.3: Layout principal (Shell)

| Campo | Valor |
|-------|-------|
| **Labels** | `frontend`, `layout`, `ux` |
| **Figma** | `Page: Layout > Frame: Sidebar/Navbar`, `Frame: Layout - Light Mode`, `Frame: Layout - Dark Mode` |

**Descrição:**
Criar layout da aplicação com sidebar/navbar, área de conteúdo, e header com info do usuário e toggle de tema.

**Acceptance Criteria:**
- [ ] Sidebar com menu de navegação (Dashboard, Usuários, Tipos de Despesa, Receitas, Despesas)
- [ ] Header com nome do usuário, toggle de tema e botão logout
- [ ] Área de conteúdo com router-outlet
- [ ] Layout responsivo (sidebar collapsa em mobile)
- [ ] Visual consistente em dark e light mode
- [ ] Ícones para itens do menu

**Subtasks:**
1. Criar componente Shell (layout wrapper)
2. Criar componente Sidebar com navegação
3. Criar componente Header
4. Implementar responsividade
5. Testar em ambos os temas

---

## EPIC 4: Frontend - Módulos de Negócio

### Story 4.1: Tela de Login

| Campo | Valor |
|-------|-------|
| **Labels** | `frontend`, `auth` |
| **Figma** | `Page: Auth > Frame: Login`, `Frame: Login - Estados` |

**Descrição:**
Implementar tela de login, AuthService, interceptor JWT e guard de rotas.

**Acceptance Criteria:**
- [ ] Tela de login com campos email e senha, botão entrar
- [ ] Validações reativas (email válido, senha obrigatória)
- [ ] Mensagens de erro da API exibidas (ex: "Credenciais inválidas")
- [ ] Loading state no botão durante requisição
- [ ] Após login, redireciona para Dashboard
- [ ] AuthService gerencia tokens (armazena, refresh automático, logout)
- [ ] Interceptor adiciona Bearer token em todas as requests
- [ ] AuthGuard redireciona para login se não autenticado
- [ ] Testes unitários passando

**Subtasks:**
1. Criar AuthService (login, refresh, logout, token storage)
2. Criar auth interceptor (functional)
3. Criar auth guard
4. Criar componente LoginComponent
5. Configurar rotas de auth
6. Escrever testes

---

### Story 4.2: CRUD Usuários (Frontend)

| Campo | Valor |
|-------|-------|
| **Labels** | `frontend`, `crud`, `usuarios` |
| **Figma** | `Page: Usuários > Frame: Listagem - Usuários`, `Frame: Form - Inclusão Usuário`, `Frame: Form - Alteração Usuário`, `Frame: Modal - Confirmação Exclusão Usuário` |

**Descrição:**
Implementar telas de listagem, inclusão e alteração de Usuários.

**Acceptance Criteria:**
- [ ] Tela de listagem com tabela mostrando Nome, Email, Data de Criação
- [ ] Filtro por nome/email funcional (busca com debounce)
- [ ] Botão "Novo Usuário" que navega para tela de inclusão
- [ ] Botão "Editar" por linha que navega para tela de alteração
- [ ] Botão "Excluir" por linha com modal de confirmação
- [ ] Tela de inclusão: campos Nome, Email, Senha, Confirmação + botões Salvar/Cancelar
- [ ] Tela de alteração: Nome, Email, Senha (opcional para reset)
- [ ] Feedback visual: toast/snackbar de sucesso e erro
- [ ] Paginação funcional
- [ ] Testes unitários passando

**Subtasks:**
1. Criar UserService (HTTP client)
2. Criar componente de listagem (tabela + filtro + paginação)
3. Criar componente de formulário (inclusão/alteração)
4. Configurar rotas do módulo
5. Escrever testes

---

### Story 4.3: CRUD Tipo de Despesa (Frontend)

| Campo | Valor |
|-------|-------|
| **Labels** | `frontend`, `crud`, `tipo-despesa` |
| **Figma** | `Page: Tipos de Despesa > Frame: Listagem`, `Frame: Form - Inclusão Tipo`, `Frame: Form - Alteração Tipo`, `Frame: Modal - Confirmação Exclusão` |

**Descrição:**
Implementar telas de listagem (com filtro e exclusão), inclusão e alteração de Tipo de Despesa.

**Acceptance Criteria:**
- [ ] Tela de listagem com tabela mostrando ID e Nome
- [ ] Filtro por nome funcional (busca ao digitar com debounce)
- [ ] Botão "Novo" que navega para tela de inclusão
- [ ] Botão "Editar" por linha que navega para tela de alteração
- [ ] Botão "Excluir" por linha com modal de confirmação
- [ ] Mensagem de erro se tipo tiver despesas vinculadas (409)
- [ ] Tela de inclusão: campo Nome + botões Salvar/Cancelar
- [ ] Tela de alteração: mesmo form preenchido com dados existentes
- [ ] Feedback visual: toast/snackbar de sucesso e erro
- [ ] Paginação funcional
- [ ] Testes unitários passando

**Subtasks:**
1. Criar ExpenseTypeService (HTTP client)
2. Criar componente de listagem (tabela + filtro + paginação)
3. Criar componente de formulário (inclusão/alteração)
4. Criar modal de confirmação de exclusão (compartilhado)
5. Configurar rotas do módulo
6. Escrever testes

---

### Story 4.4: CRUD Receitas (Frontend)

| Campo | Valor |
|-------|-------|
| **Labels** | `frontend`, `crud`, `receitas` |
| **Figma** | `Page: Receitas > Frame: Listagem - Receitas`, `Frame: Form - Inclusão Receita`, `Frame: Form - Alteração Receita`, `Frame: Form - Recorrência` |

**Descrição:**
Implementar telas de listagem, inclusão e alteração de Receitas com suporte visual a recorrência.

**Acceptance Criteria:**
- [ ] Tela de listagem com tabela (Data, Descrição, Valor formatado R$, Recorrente ícone/badge)
- [ ] Filtros: data de/até (datepicker), descrição, recorrente (sim/não/todos)
- [ ] CRUD completo (incluir, editar, excluir com confirmação)
- [ ] Formulário com campos: Data (datepicker), Descrição, Valor (máscara monetária), Recorrente (toggle)
- [ ] Ao ativar Recorrente: campo Frequência aparece (select: Semanal, Mensal, Anual)
- [ ] Validações: todos campos obrigatórios, valor > 0, frequência obrigatória se recorrente
- [ ] Paginação funcional
- [ ] Testes passando

**Subtasks:**
1. Criar IncomeService
2. Criar componente de listagem com filtros
3. Criar componente de formulário com lógica condicional de recorrência
4. Implementar máscara monetária (R$)
5. Configurar rotas
6. Escrever testes

---

### Story 4.5: CRUD Despesas (Frontend)

| Campo | Valor |
|-------|-------|
| **Labels** | `frontend`, `crud`, `despesas` |
| **Figma** | `Page: Despesas > Frame: Listagem - Despesas`, `Frame: Form - Inclusão Despesa`, `Frame: Form - Alteração Despesa`, `Frame: Form - Recorrência` |

**Descrição:**
Implementar telas de listagem, inclusão e alteração de Despesas com seleção de tipo e recorrência.

**Acceptance Criteria:**
- [ ] Tela de listagem com tabela (Vencimento, Descrição, Tipo, Valor formatado R$, Recorrente)
- [ ] Filtros: vencimento de/até, tipo (select carregado da API), descrição, recorrente
- [ ] CRUD completo (incluir, editar, excluir com confirmação)
- [ ] Formulário: Vencimento (datepicker), Descrição, Tipo (select), Valor (máscara), Recorrente (toggle) + Frequência condicional
- [ ] Validações completas
- [ ] Highlight visual para despesas vencidas (data < hoje)
- [ ] Paginação funcional
- [ ] Testes passando

**Subtasks:**
1. Criar ExpenseService
2. Criar componente de listagem com filtros (incluindo select de tipo)
3. Criar componente de formulário
4. Implementar highlight de vencidos
5. Configurar rotas
6. Escrever testes

---

### Story 4.6: Dashboard (Frontend)

| Campo | Valor |
|-------|-------|
| **Labels** | `frontend`, `dashboard` |
| **Figma** | `Page: Dashboard > Frame: Dashboard - Visão Geral`, `Frame: Dashboard - Gráficos` |

**Descrição:**
Implementar tela de dashboard com cards de métricas e gráfico de barras por tipo de despesa.

**Acceptance Criteria:**
- [ ] Card "Saldo Atual" com valor formatado (verde se positivo, vermelho se negativo)
- [ ] Card "Receitas do Mês" com valor formatado em verde
- [ ] Card "Despesas do Mês" com valor formatado em vermelho
- [ ] Gráfico de barras horizontal mostrando despesas por tipo (mês corrente)
- [ ] Loading skeleton enquanto carrega dados
- [ ] Layout responsivo (cards em grid, gráfico abaixo)
- [ ] Visual adequado em dark e light mode
- [ ] Testes passando

**Subtasks:**
1. Criar DashboardService
2. Criar componente card de métrica (reutilizável)
3. Criar componente de gráfico (ngx-charts ou Chart.js)
4. Criar DashboardComponent com layout
5. Implementar loading skeleton
6. Escrever testes

---

## EPIC 5: Infraestrutura e Deploy AWS

### Story 5.1: Provisionamento de infraestrutura AWS (IaC)

| Campo | Valor |
|-------|-------|
| **Labels** | `infra`, `aws`, `iac` |
| **Figma** | N/A |

**Descrição:**
Criar templates CloudFormation para provisionar toda a infra necessária: EC2, S3, CloudFront, Security Groups, IAM.

**Acceptance Criteria:**
- [ ] EC2 t3.small com Amazon Linux 2023
- [ ] Security Group: porta 443 (HTTPS), 80 (HTTP redirect), 22 (SSH restrito)
- [ ] PostgreSQL instalado via user data script
- [ ] S3 Bucket configurado para static website hosting
- [ ] CloudFront distribution com behaviors: default → S3, `/api/*` → EC2
- [ ] Certificado SSL via ACM
- [ ] IAM roles para CodePipeline/CodeBuild/CodeDeploy
- [ ] Secrets no Parameter Store (JWT_KEY, DB_PASSWORD)
- [ ] Template validado e executável

**Subtasks:**
1. Criar template para VPC/Subnet/Security Group
2. Criar template para EC2 com user data (instala .NET runtime + PostgreSQL + Nginx)
3. Criar template para S3 + CloudFront
4. Criar template para IAM roles
5. Configurar Parameter Store
6. Documentar como executar/atualizar o stack

---

### Story 5.2: CI/CD com AWS CodePipeline

| Campo | Valor |
|-------|-------|
| **Labels** | `infra`, `aws`, `cicd` |
| **Figma** | N/A |

**Descrição:**
Configurar pipeline automatizado: Source (repositório) → Build (CodeBuild) → Deploy (S3 para front, CodeDeploy para back).

**Acceptance Criteria:**
- [ ] Pipeline Source conectado ao repositório (GitHub ou CodeCommit)
- [ ] CodeBuild frontend: `npm install` → `ng build --configuration production` → sync S3 → invalidate CloudFront
- [ ] CodeBuild backend: `dotnet publish` → artefato para CodeDeploy
- [ ] CodeDeploy instala/reinicia aplicação na EC2 via appspec.yml
- [ ] Pipeline executa automaticamente em push na branch main
- [ ] buildspec.yml para frontend e backend separados
- [ ] appspec.yml com hooks (stop, install, start, validate)
- [ ] Pipeline testado end-to-end com commit real

**Subtasks:**
1. Criar buildspec-frontend.yml
2. Criar buildspec-backend.yml
3. Criar appspec.yml + scripts de deploy (stop.sh, install.sh, start.sh)
4. Configurar Nginx como reverse proxy (`/api` → localhost:5000)
5. Criar/configurar CodePipeline via IaC ou console
6. Testar pipeline completo

---

### Story 5.3: Configuração de produção e hardening

| Campo | Valor |
|-------|-------|
| **Labels** | `infra`, `aws`, `segurança` |
| **Figma** | N/A |

**Descrição:**
Configurar HTTPS, CORS, rate limiting, health checks, logs e backup do banco.

**Acceptance Criteria:**
- [ ] HTTPS funcional via CloudFront (certificado ACM)
- [ ] CORS configurado na API (apenas domínio do CloudFront autorizado)
- [ ] Rate limiting na API (100 requests/min por IP)
- [ ] Health check endpoint monitorável
- [ ] Logs da aplicação escritos em arquivo + rotação (ou CloudWatch)
- [ ] Backup diário do PostgreSQL via cron → upload para S3
- [ ] Nginx com headers de segurança (HSTS, X-Frame-Options, etc.)
- [ ] Aplicação funcional e acessível via HTTPS

**Subtasks:**
1. Configurar CORS no .NET
2. Implementar rate limiting (AspNetCoreRateLimit ou middleware custom)
3. Configurar Nginx com SSL termination e security headers
4. Criar script de backup PostgreSQL + upload S3
5. Configurar cron para backup diário
6. Configurar logging (Serilog → arquivo ou CloudWatch)
7. Testar tudo em produção

---

## EPIC 6: Documentação

### Story 6.1: Documentação completa do projeto

| Campo | Valor |
|-------|-------|
| **Labels** | `docs` |
| **Figma** | N/A |

**Descrição:**
Criar documentação técnica e operacional do projeto.

**Acceptance Criteria:**
- [ ] README.md com: descrição do projeto, stack, pré-requisitos, instruções de setup local (backend + frontend + banco)
- [ ] Documentação de API via Swagger/OpenAPI acessível em produção
- [ ] Diagrama de arquitetura (Mermaid no README ou doc separado)
- [ ] Runbook de operações: deploy manual, restore de backup, troubleshooting comum
- [ ] ADRs (Architecture Decision Records) para decisões técnicas principais

**Subtasks:**
1. Criar README.md completo
2. Configurar Swagger com descrições nos endpoints
3. Criar docs/architecture.md com diagramas Mermaid
4. Criar docs/runbook.md
5. Criar docs/adr/ com decisões (stack, auth, infra)

---

## Resumo dos Epics

| Epic | Stories | Prioridade |
|------|---------|------------|
| 1. Autenticação e Usuários | 4 stories | Alta |
| 2. Backend - Módulos de Negócio | 4 stories | Alta |
| 3. Frontend - Estrutura e Tema | 3 stories | Alta |
| 4. Frontend - Módulos de Negócio | 6 stories | Alta |
| 5. Infraestrutura e Deploy AWS | 3 stories | Média |
| 6. Documentação | 1 story | Baixa |

## Ordem de execução sugerida (Sprints)

| Sprint | Stories | Foco |
|--------|---------|------|
| Sprint 1 | 1.1, 1.2, 1.3, 1.4, 3.1 | Setup backend + frontend + auth + CRUD usuários backend |
| Sprint 2 | 2.1, 2.2, 2.3, 2.4 | Backend completo (CRUDs + Dashboard) |
| Sprint 3 | 3.2, 3.3, 4.1, 4.2 | Tema + Layout + Login + CRUD usuários frontend |
| Sprint 4 | 4.3, 4.4, 4.5, 4.6 | Frontend completo (CRUDs + Dashboard) |
| Sprint 5 | 5.1, 5.2, 5.3 | Infraestrutura e Deploy |
| Sprint 6 | 6.1 | Documentação final |
