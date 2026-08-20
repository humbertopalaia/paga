# Testes

Testes fazem parte dos *Acceptance Criteria* de praticamente toda Story do board — quando
implementar uma Story, implemente os testes dela. Não crie testes fora desse escopo sem o
usuário pedir.

## Backend (xUnit)

Estrutura: `backend/tests/Paga.Tests/Unit/` e `.../Integration/`.
Nome: `MetodoOuCenario_DeveResultado` (ex: `CreateAsync_ShouldReject_WhenFrequencyMissing`).
Padrão Arrange / Act / Assert explícito.

Unitários cobrem services e validators com dependências fakes/mocks: regras de recorrência,
email único, valor positivo, geração e expiração de token, cálculo do dashboard.

Integração usa `WebApplicationFactory` com banco isolado por classe de teste (Testcontainers
PostgreSQL ou banco dedicado, nunca o banco de desenvolvimento) e valida status code, shape do
JSON e persistência real.

Cobertura mínima esperada em qualquer módulo de CRUD:

- caminho feliz de cada verbo
- 401 sem token
- 404 para id inexistente
- 400 para payload inválido
- 409 nos conflitos previstos
- **isolamento entre usuários**: usuário A não lê, altera nem exclui dado de B

## Frontend (Karma/Jasmine)

`ng test --watch=false` (nunca em modo watch). `HttpTestingController` para services,
`ComponentFixture` para componentes. Sempre testar: chamada HTTP correta (URL, método, params),
validações do formulário, campo condicional de recorrência, estados de loading/erro/vazio
e o `ThemeService` (default, toggle, persistência).

## Definição de pronto

Uma tarefa só está concluída quando `dotnet build`, `dotnet test`, `ng build` e
`ng test --watch=false` (os que forem aplicáveis ao que mudou) rodam sem erro, e cada
*Acceptance Criteria* da Story foi verificado. Se algo não pôde ser verificado, diga
explicitamente o que e por quê.
