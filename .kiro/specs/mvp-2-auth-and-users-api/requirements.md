# Requirements Document

## Introduction

**MVP 2 — Auth and Users API.** Segunda fatia do walking skeleton. Cobre as Stories **1.3
(Endpoints de autenticação — Login + Refresh + Logout)** e **1.4 (CRUD Usuários API)** de
`docs/jira-board.md`. Referência de Figma: N/A.

Implementa a autenticação JWT completa (login, refresh com rotação, logout), o middleware de
autorização, o serviço de identidade do usuário corrente (`ICurrentUserService`), o handler global
de exceções produzindo `ProblemDetails` (RFC 7807), e o CRUD administrativo de usuários — tudo
sobre a fundação criada na `mvp-1`.

Ao final desta spec, o administrador semeado consegue logar, obter token, criar novos usuários e
gerenciá-los. Todos os endpoints de negócio passam a exigir autenticação.

**Dentro do escopo:** JWT middleware, `ITokenService`, `IAuthService`, `IUserService`,
`ICurrentUserService`, services de autenticação e usuários, DTOs, validadores FluentValidation,
`AuthController`, `UsersController`, handler global de exceções, helper de paginação, testes
unitários e de integração.

**Fora do escopo:** frontend, CRUDs de módulos de negócio (`expense-types`, `incomes`, `expenses`,
`dashboard`), novas migrations (entidades já existem), infraestrutura AWS.

## Glossary

| Termo | Significado |
|-------|-------------|
| **Access token** | JWT assinado com expiração de 30 minutos, carrega as claims `sub` (UserId) e `email` |
| **Refresh token** | String opaca persistida na tabela `refresh_tokens`, usada para obter novo par de tokens sem re-autenticação |
| **Rotação de refresh token** | Ao usar um refresh token, o anterior é revogado e um novo é emitido; impede reuso de tokens antigos |
| **ICurrentUserService** | Abstração que extrai o `UserId` das claims do token autenticado, usada por todos os services de negócio |
| **ProblemDetails** | Formato RFC 7807 para respostas de erro HTTP, com campos `type`, `title`, `status` e `errors` |
| **Handler global de exceções** | Middleware que captura exceções de domínio e as traduz para respostas `ProblemDetails` |
| **Paginação** | Envelope padrão `{ items, pageNumber, pageSize, totalCount, totalPages }` com `pageNumber` começando em 1 e `pageSize` máximo 100 |
| **Multi-tenant** | Isolamento de dados por usuário; toda query filtra por `UserId` derivado do token |

## Requirements

### Requirement 1: Configuração do middleware JWT

**User Story:** Como desenvolvedor, quero que a API valide tokens JWT em todas as requisições
protegidas, para que somente usuários autenticados acessem os recursos.

#### Acceptance Criteria

1. QUANDO a aplicação iniciar, O SISTEMA DEVE registrar o middleware de autenticação JWT com a
   chave de assinatura lida da configuração `Jwt:Key`.
2. O SISTEMA DEVE configurar o access token com expiração de 30 minutos.
3. O SISTEMA DEVE incluir as claims `sub` (UserId como Guid) e `email` no payload do access token.
4. QUANDO uma requisição chegar a um endpoint protegido sem header `Authorization`, O SISTEMA DEVE
   responder HTTP 401.
5. QUANDO uma requisição chegar com token expirado ou assinatura inválida, O SISTEMA DEVE responder
   HTTP 401.
6. O SISTEMA DEVE permitir acesso sem autenticação apenas aos endpoints `POST /api/auth/login`,
   `POST /api/auth/refresh` e `GET /health`.
7. QUANDO a chave `Jwt:Key` estiver ausente ou vazia na inicialização, O SISTEMA DEVE falhar
   imediatamente com mensagem explícita.

### Requirement 2: Login

**User Story:** Como usuário, quero me autenticar com email e senha, para que eu receba credenciais
de acesso à API.

#### Acceptance Criteria

1. QUANDO uma requisição `POST /api/auth/login` for recebida com email e senha válidos, O SISTEMA
   DEVE responder HTTP 200 com `TokenResponse` contendo `accessToken`, `refreshToken` e
   `expiresIn`.
2. QUANDO o email não corresponder a nenhum usuário cadastrado, O SISTEMA DEVE responder HTTP 401
   com mensagem genérica que não revele se o email existe ou não.
3. QUANDO a senha não corresponder ao hash armazenado, O SISTEMA DEVE responder HTTP 401 com a
   mesma mensagem genérica do cenário de email inexistente.
4. O SISTEMA DEVE persistir o refresh token gerado na tabela `refresh_tokens` com data de
   expiração e `is_revoked = false`.
5. O SISTEMA DEVE atender `POST /api/auth/login` sem exigir autenticação.
6. QUANDO o payload estiver ausente ou com campos vazios, O SISTEMA DEVE responder HTTP 400 com
   `ProblemDetails` e mensagens de validação em pt-BR.

### Requirement 3: Refresh token

**User Story:** Como cliente da API, quero renovar meu acesso sem re-autenticação, para que a
sessão persista além dos 30 minutos do access token.

#### Acceptance Criteria

1. QUANDO uma requisição `POST /api/auth/refresh` for recebida com refresh token válido e não
   expirado, O SISTEMA DEVE responder HTTP 200 com um novo `TokenResponse`.
2. QUANDO o refresh token for consumido com sucesso, O SISTEMA DEVE revogar o token anterior
   marcando `is_revoked = true`.
3. QUANDO o refresh token não existir na base, O SISTEMA DEVE responder HTTP 401.
4. QUANDO o refresh token estiver expirado, O SISTEMA DEVE responder HTTP 401.
5. QUANDO o refresh token já estiver revogado, O SISTEMA DEVE responder HTTP 401.
6. O SISTEMA DEVE atender `POST /api/auth/refresh` sem exigir autenticação.
7. O SISTEMA DEVE gerar refresh tokens com entropia criptograficamente segura.

### Requirement 4: Logout

**User Story:** Como usuário, quero encerrar minha sessão, para que meu refresh token não possa
ser reutilizado.

#### Acceptance Criteria

1. QUANDO uma requisição `POST /api/auth/logout` for recebida com autenticação válida, O SISTEMA
   DEVE revogar o refresh token informado no corpo da requisição.
2. QUANDO o refresh token informado não pertencer ao usuário autenticado, O SISTEMA DEVE responder
   HTTP 401.
3. QUANDO o refresh token já estiver revogado ou não existir, O SISTEMA DEVE responder HTTP 200
   sem erro, garantindo idempotência.
4. O SISTEMA DEVE exigir autenticação para `POST /api/auth/logout`.

### Requirement 5: Serviço de identidade do usuário corrente

**User Story:** Como desenvolvedor, quero uma abstração que forneça o UserId do token autenticado,
para que os services de negócio isolem dados por usuário sem acessar HttpContext diretamente.

#### Acceptance Criteria

1. O SISTEMA DEVE expor a interface `ICurrentUserService` em `Paga.Application` com propriedade
   `UserId` do tipo `Guid`.
2. O SISTEMA DEVE implementar `ICurrentUserService` em `Paga.Infrastructure` (ou `Paga.Api`)
   extraindo o valor da claim `sub` do `HttpContext.User`.
3. QUANDO a claim `sub` estiver ausente ou inválida, O SISTEMA DEVE lançar exceção, pois
   indica falha no middleware de autenticação.

### Requirement 6: Handler global de exceções

**User Story:** Como consumidor da API, quero respostas de erro padronizadas em formato
`ProblemDetails`, para que o frontend trate erros de forma consistente.

#### Acceptance Criteria

1. O SISTEMA DEVE interceptar exceções não tratadas e responder com `ProblemDetails` HTTP 500 sem
   expor stack trace ou detalhe de infraestrutura no corpo.
2. O SISTEMA DEVE registrar a exceção completa no log com nível Error.
3. O SISTEMA DEVE mapear exceções de validação do FluentValidation para HTTP 400 com o campo
   `errors` contendo os erros por propriedade.
4. O SISTEMA DEVE mapear exceções de domínio de conflito (email duplicado, recurso vinculado)
   para HTTP 409 com mensagem em pt-BR.
5. O SISTEMA DEVE mapear exceções de domínio de entidade não encontrada para HTTP 404.
6. O SISTEMA DEVE mapear exceções de domínio de acesso negado (recurso de outro usuário) para
   HTTP 404, sem revelar que o recurso existe.
7. O SISTEMA DEVE retornar todas as mensagens de validação e de conflito em pt-BR.

### Requirement 7: Listagem de usuários

**User Story:** Como administrador, quero listar os usuários do sistema com filtros e paginação,
para que eu possa localizar e gerenciar contas.

#### Acceptance Criteria

1. QUANDO uma requisição `GET /api/users` for recebida, O SISTEMA DEVE responder com o envelope
   de paginação contendo os usuários.
2. QUANDO o parâmetro `name` for informado, O SISTEMA DEVE filtrar usuários cujo nome contenha o
   valor informado, sem distinção de maiúsculas e minúsculas.
3. QUANDO o parâmetro `email` for informado, O SISTEMA DEVE filtrar usuários cujo email contenha
   o valor informado, sem distinção de maiúsculas e minúsculas.
4. O SISTEMA DEVE aplicar paginação com `pageNumber` (default 1) e `pageSize` (default 10,
   máximo 100).
5. O SISTEMA DEVE retornar cada usuário com `id`, `name`, `email` e `createdAt`, sem expor
   `passwordHash`.
6. O SISTEMA DEVE exigir autenticação para `GET /api/users`.

### Requirement 8: Consulta de usuário por ID

**User Story:** Como administrador, quero consultar os detalhes de um usuário específico, para
que eu possa verificar dados antes de alterá-los.

#### Acceptance Criteria

1. QUANDO uma requisição `GET /api/users/{id}` for recebida com ID existente, O SISTEMA DEVE
   responder HTTP 200 com os dados do usuário.
2. QUANDO o ID não corresponder a nenhum usuário, O SISTEMA DEVE responder HTTP 404.
3. O SISTEMA DEVE retornar `id`, `name`, `email` e `createdAt`, sem expor `passwordHash`.
4. O SISTEMA DEVE exigir autenticação para `GET /api/users/{id}`.

### Requirement 9: Criação de usuário

**User Story:** Como administrador, quero cadastrar novos usuários no sistema, para que novas
pessoas possam acessar a aplicação.

#### Acceptance Criteria

1. QUANDO uma requisição `POST /api/users` for recebida com payload válido, O SISTEMA DEVE criar
   o usuário e responder HTTP 201 com os dados do usuário criado.
2. O SISTEMA DEVE armazenar a senha com hash BCrypt, usando o `IPasswordHasher` existente.
3. QUANDO o email informado já existir na base, O SISTEMA DEVE responder HTTP 409 com mensagem
   em pt-BR informando que o email já está cadastrado.
4. O SISTEMA DEVE validar: nome obrigatório e não vazio, email obrigatório e em formato válido,
   senha obrigatória com mínimo de 6 caracteres.
5. QUANDO a validação falhar, O SISTEMA DEVE responder HTTP 400 com `ProblemDetails` e mensagens
   em pt-BR.
6. O SISTEMA DEVE gerar o `Id` (Guid) e `CreatedAt` (UTC) no servidor, ignorando qualquer valor
   enviado pelo cliente.
7. O SISTEMA DEVE exigir autenticação para `POST /api/users`.

### Requirement 10: Atualização de usuário

**User Story:** Como administrador, quero alterar dados de um usuário, para que eu possa corrigir
informações ou resetar senhas.

#### Acceptance Criteria

1. QUANDO uma requisição `PUT /api/users/{id}` for recebida com payload válido e ID existente,
   O SISTEMA DEVE atualizar o usuário e responder HTTP 200 com os dados atualizados.
2. QUANDO o campo `password` estiver presente e não vazio no payload, O SISTEMA DEVE atualizar o
   hash da senha.
3. QUANDO o campo `password` estiver ausente ou vazio, O SISTEMA DEVE manter o hash atual
   inalterado.
4. QUANDO o email informado já pertencer a outro usuário, O SISTEMA DEVE responder HTTP 409 com
   mensagem em pt-BR.
5. QUANDO o ID não corresponder a nenhum usuário, O SISTEMA DEVE responder HTTP 404.
6. O SISTEMA DEVE validar: nome obrigatório, email obrigatório e em formato válido; senha, quando
   presente, com mínimo de 6 caracteres.
7. O SISTEMA DEVE exigir autenticação para `PUT /api/users/{id}`.

### Requirement 11: Exclusão de usuário

**User Story:** Como administrador, quero excluir um usuário, para que contas inativas possam ser
removidas do sistema.

#### Acceptance Criteria

1. QUANDO uma requisição `DELETE /api/users/{id}` for recebida com ID existente, O SISTEMA DEVE
   excluir o usuário e responder HTTP 204.
2. QUANDO o ID não corresponder a nenhum usuário, O SISTEMA DEVE responder HTTP 404.
3. O SISTEMA DEVE excluir em cascata os dados relacionados ao usuário (expense types, incomes,
   expenses, refresh tokens), conforme configurado nas FKs da `mvp-1`.
4. O SISTEMA DEVE exigir autenticação para `DELETE /api/users/{id}`.

### Requirement 12: Testes unitários

**User Story:** Como desenvolvedor, quero cobertura unitária dos services e validators, para que
regressões sejam detectadas antes da integração.

#### Acceptance Criteria

1. O SISTEMA DEVE cobrir por teste unitário a geração de JWT com claims corretas e expiração de
   30 minutos.
2. O SISTEMA DEVE cobrir por teste unitário a geração de refresh token com entropia adequada e
   unicidade.
3. O SISTEMA DEVE cobrir por teste unitário os cenários de login: credenciais válidas, email
   inexistente e senha incorreta.
4. O SISTEMA DEVE cobrir por teste unitário os cenários de refresh: token válido, token expirado,
   token revogado e token inexistente.
5. O SISTEMA DEVE cobrir por teste unitário os validators de criação e atualização de usuário:
   campos obrigatórios, formato de email, tamanho mínimo de senha.
6. O SISTEMA DEVE cobrir por teste unitário o `UserService`: criação com email duplicado, criação
   com dados válidos, atualização com e sem senha, exclusão de existente e inexistente.
7. QUANDO `dotnet test` for executado com filtro de testes unitários, O SISTEMA DEVE passar todos
   os testes sem falhas.

### Requirement 13: Testes de integração

**User Story:** Como desenvolvedor, quero testes de integração que validem o fluxo real HTTP com
banco, para que o comportamento end-to-end esteja garantido.

#### Acceptance Criteria

1. O SISTEMA DEVE cobrir por teste de integração o fluxo completo de login do administrador
   semeado e obtenção de token válido.
2. O SISTEMA DEVE cobrir por teste de integração que o refresh token rotaciona corretamente:
   usar um refresh retorna novo par e invalida o anterior.
3. O SISTEMA DEVE cobrir por teste de integração que logout revoga o token e que o token revogado
   não funciona mais no refresh.
4. O SISTEMA DEVE cobrir por teste de integração que endpoints protegidos retornam 401 sem token.
5. O SISTEMA DEVE cobrir por teste de integração o CRUD completo de usuários: criação, listagem
   com filtro e paginação, consulta por ID, atualização (com e sem senha) e exclusão.
6. O SISTEMA DEVE cobrir por teste de integração que criação de usuário com email duplicado
   retorna 409.
7. O SISTEMA DEVE cobrir por teste de integração que operações sobre ID inexistente retornam 404.
8. O SISTEMA DEVE executar todos os testes de integração contra um container PostgreSQL efêmero
   via Testcontainers, sem acessar o banco de desenvolvimento.
9. QUANDO `dotnet test` for executado, O SISTEMA DEVE passar todos os testes sem falhas.
