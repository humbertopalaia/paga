# Requirements Document

## Introduction

**MVP 1 — Backend Foundation.** Primeira fatia do walking skeleton descrito em
`.kiro/steering/workflow.md`. Cobre as Stories **1.1 (Setup do projeto backend .NET 10)** e
**1.2 (Modelagem de domínio e migrations)** de `docs/jira-board.md`. Referência de Figma: N/A.

Estabelece a solução .NET em camadas, a persistência em PostgreSQL com o modelo de dados completo
e o usuário administrador semeado — pré-requisitos de todas as specs seguintes. Nenhum endpoint de
negócio é implementado aqui além do `/health`.

**Dentro do escopo:** estrutura da solução e referências entre projetos, pacotes NuGet,
configuração e segredos, logging estruturado, health check, Swagger, as cinco entidades de domínio,
`DbContext` e configurações de mapeamento, migration inicial, seed condicional do administrador,
projeto de testes.

**Fora do escopo:** autenticação e JWT, endpoints de usuários (ambos na `mvp-2`), services de
negócio de `ExpenseType`/`Income`/`Expense`, frontend, infraestrutura AWS.

## Glossary

| Termo | Significado |
|-------|-------------|
| **Walking skeleton** | Fatia vertical fina que atravessa todas as camadas até o deploy, usada para validar o caminho completo antes de implementar os demais módulos |
| **Camadas** | Os quatro projetos `Paga.Api`, `Paga.Application`, `Paga.Domain` e `Paga.Infrastructure`, com as direções de referência definidas em `.kiro/steering/structure.md` |
| **Frequency** | Periodicidade de um lançamento recorrente. Enum de domínio com três valores, persistidos como `weekly`, `monthly` e `yearly` |
| **Migration inicial** | A primeira migration do EF Core, nomeada `InitialCreate`, que cria o schema completo das cinco entidades |
| **Seed do administrador** | Criação automática e condicional do usuário `palaia@increvasenocanal.com` quando a base não tem nenhum usuário, viabilizando o primeiro login |
| **Banco limpo** | Instância PostgreSQL sem schema aplicado, usada por testes de integração e nunca compartilhada com o banco de desenvolvimento |

## Requirements

### Requirement 1: Estrutura da solução e camadas

**User Story:** Como desenvolvedor, quero a solução organizada em camadas com dependências
controladas, para que a regra de negócio não vaze para a borda HTTP nem para a persistência.

#### Acceptance Criteria

1. QUANDO a solução for restaurada e compilada, O SISTEMA DEVE conter os projetos `Paga.Api`,
   `Paga.Application`, `Paga.Domain`, `Paga.Infrastructure` e `Paga.Tests`, todos em .NET 10.
2. O SISTEMA DEVE declarar referências entre projetos somente nas direções `Api → Application`,
   `Api → Infrastructure`, `Application → Domain` e `Infrastructure → Domain`.
3. O SISTEMA DEVE manter `Paga.Domain` sem referência a qualquer outro projeto da solução e sem
   pacote de infraestrutura ou de acesso a dados.
4. O SISTEMA NÃO DEVE permitir que `Paga.Application` referencie `Paga.Infrastructure`.
5. O SISTEMA DEVE habilitar nullable reference types em todos os projetos.
6. QUANDO `dotnet build` for executado na raiz de `backend/`, O SISTEMA DEVE compilar sem erros e
   sem warnings.

### Requirement 2: Execução da API e health check

**User Story:** Como operador, quero um endpoint de saúde, para que o processo de deploy e o
CloudFront tenham como verificar se a aplicação subiu e está íntegra.

#### Acceptance Criteria

1. QUANDO `dotnet run --project src/Paga.Api` for executado, O SISTEMA DEVE iniciar sem erros.
2. QUANDO uma requisição `GET /health` for recebida E o banco estiver acessível, O SISTEMA DEVE
   responder HTTP 200.
3. QUANDO uma requisição `GET /health` for recebida E o banco estiver inacessível, O SISTEMA DEVE
   responder HTTP 503.
4. O SISTEMA DEVE atender `GET /health` sem exigir autenticação.
5. O SISTEMA NÃO DEVE expor detalhe de infraestrutura, connection string ou stack trace no corpo
   da resposta de `/health`.

### Requirement 3: Documentação OpenAPI

**User Story:** Como consumidor da API, quero a documentação navegável dos endpoints, para que eu
possa explorar e testar os contratos sem ler o código.

#### Acceptance Criteria

1. QUANDO a API estiver rodando em ambiente de desenvolvimento, O SISTEMA DEVE servir a UI do
   Swagger em `/swagger`.
2. O SISTEMA DEVE gerar o documento OpenAPI a partir dos endpoints registrados e dos comentários
   XML dos membros públicos.
3. O SISTEMA DEVE habilitar a geração do arquivo de documentação XML no projeto `Paga.Api` sem
   produzir warning de membro não documentado.

### Requirement 4: Configuração e segredos

**User Story:** Como responsável pelo repositório, quero que nenhum segredo seja versionado, para
que credenciais não vazem no GitHub público.

#### Acceptance Criteria

1. O SISTEMA DEVE ler a connection string do PostgreSQL da chave `ConnectionStrings:Default`.
2. O SISTEMA DEVE permitir sobrescrever qualquer chave de configuração por variável de ambiente.
3. O SISTEMA NÃO DEVE conter senha, chave criptográfica ou connection string real em arquivo
   versionado.
4. O SISTEMA DEVE excluir `appsettings.Development.json` do controle de versão.
5. QUANDO a aplicação iniciar E a connection string estiver ausente ou vazia, O SISTEMA DEVE
   falhar imediatamente com mensagem explícita, em vez de subir e falhar na primeira query.

### Requirement 5: Logging estruturado

**User Story:** Como operador, quero logs estruturados e livres de dado sensível, para que eu possa
diagnosticar problemas em produção sem criar risco de vazamento.

#### Acceptance Criteria

1. O SISTEMA DEVE usar Serilog como provider de log da aplicação.
2. O SISTEMA DEVE registrar eventos com propriedades nomeadas, não com interpolação de string.
3. O SISTEMA DEVE registrar em log o resultado do seed do administrador, incluindo o motivo quando
   ele for ignorado.
4. O SISTEMA NÃO DEVE registrar senha, hash de senha, token ou dado pessoal em log.

### Requirement 6: Modelo de domínio

**User Story:** Como desenvolvedor, quero as cinco entidades criadas de uma vez, para que os
módulos seguintes não exijam uma nova migration estrutural.

#### Acceptance Criteria

1. O SISTEMA DEVE definir `User` com `Id` (Guid), `Name`, `Email`, `PasswordHash` e `CreatedAt`.
2. O SISTEMA DEVE definir `ExpenseType` com `Id` (int), `UserId` e `Name`.
3. O SISTEMA DEVE definir `Income` com `Id` (int), `UserId`, `Date`, `Description`, `Value`,
   `IsRecurring` e `Frequency` opcional.
4. O SISTEMA DEVE definir `Expense` com `Id` (int), `UserId`, `DueDate`, `Description`,
   `ExpenseTypeId`, `Value`, `IsRecurring` e `Frequency` opcional.
5. O SISTEMA DEVE definir `RefreshToken` com `Id` (Guid), `UserId`, `Token`, `ExpiresAt` e
   `IsRevoked`.
6. O SISTEMA DEVE representar `Frequency` como enum com exatamente três valores.
7. O SISTEMA DEVE manter `Paga.Domain` livre de atributos, tipos e anotações do EF Core.

### Requirement 7: Persistência e migration inicial

**User Story:** Como desenvolvedor, quero o schema versionado por migration, para que qualquer
ambiente seja recriado de forma reproduzível.

#### Acceptance Criteria

1. O SISTEMA DEVE declarar uma classe `IEntityTypeConfiguration<T>` por entidade, carregadas por
   `ApplyConfigurationsFromAssembly`.
2. O SISTEMA NÃO DEVE conter mapeamento inline no `OnModelCreating` além dessa chamada.
3. O SISTEMA DEVE nomear tabelas e colunas em `snake_case`.
4. O SISTEMA DEVE mapear `Value` como `decimal(18,2)`.
5. O SISTEMA DEVE persistir `Frequency` como texto, com os valores `weekly`, `monthly` e `yearly`.
6. O SISTEMA DEVE criar índice único em `User.Email`, índice único composto em
   `ExpenseType(UserId, Name)` e índice único em `RefreshToken.Token`.
7. O SISTEMA DEVE configurar a chave estrangeira de `Expense` para `ExpenseType` com comportamento
   de exclusão `Restrict`.
8. O SISTEMA DEVE configurar as chaves estrangeiras para `User` com comportamento de exclusão
   `Cascade`.
9. QUANDO `dotnet ef migrations add InitialCreate` for executado, O SISTEMA DEVE gerar a migration
   sem erros.
10. QUANDO `dotnet ef database update` for executado contra um PostgreSQL sem schema, O SISTEMA
    DEVE criar todas as tabelas, chaves estrangeiras e índices.

### Requirement 8: Usuário administrador semeado

**User Story:** Como usuário, quero poder fazer o primeiro login em uma base nova, para que seja
possível cadastrar os demais usuários — já que o produto não tem auto-registro público.

#### Acceptance Criteria

1. QUANDO a aplicação iniciar E não existir nenhum usuário na base, O SISTEMA DEVE criar o
   administrador com email `palaia@increvasenocanal.com`.
2. O SISTEMA DEVE obter a senha do seed da chave de configuração `Seed:AdminPassword`,
   sobrescrevível por variável de ambiente.
3. QUANDO a aplicação iniciar E a base estiver sem usuários E a senha do seed não estiver
   configurada, O SISTEMA DEVE registrar um aviso e não criar usuário algum.
4. O SISTEMA NÃO DEVE assumir uma senha default para o administrador.
5. O SISTEMA DEVE armazenar a senha do administrador com hash BCrypt.
6. QUANDO a aplicação iniciar E já existir ao menos um usuário, O SISTEMA NÃO DEVE criar, alterar
   ou sobrescrever qualquer usuário.
7. O SISTEMA DEVE tornar o seed idempotente, tolerando execução concorrente ou repetida sem
   duplicar o administrador.

### Requirement 9: Projeto de testes

**User Story:** Como desenvolvedor, quero a base de testes montada desde o início, para que cada
spec seguinte tenha onde acrescentar cobertura sem retrabalho de setup.

#### Acceptance Criteria

1. QUANDO `dotnet test` for executado, O SISTEMA DEVE rodar a suíte sem falhas.
2. O SISTEMA DEVE separar os testes nas pastas `Unit/` e `Integration/`.
3. O SISTEMA DEVE cobrir por teste de integração que `GET /health` responde 200 com o banco
   disponível.
4. O SISTEMA DEVE cobrir por teste de integração que a migration inicial cria o schema esperado em
   um banco limpo.
5. O SISTEMA DEVE cobrir por teste os três cenários do seed: base vazia com senha configurada,
   base vazia sem senha configurada e base já populada.
6. O SISTEMA NÃO DEVE executar teste de integração contra o banco de desenvolvimento.
