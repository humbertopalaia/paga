# Stack técnica

## Definida (não substituir sem aprovação explícita do usuário)

| Camada | Tecnologia |
|--------|-----------|
| Backend | .NET 10 / ASP.NET Core Web API |
| ORM | EF Core + Npgsql |
| Banco | PostgreSQL |
| Validação | FluentValidation |
| Hash de senha | BCrypt |
| Auth | JWT (access token 30 min) + refresh token persistido |
| Logging | Serilog |
| Testes backend | xUnit (unitários + integração) |
| Frontend | Angular 19, standalone components, signals |
| Estilo | SCSS + CSS custom properties (dark/light) |
| UI | Angular Material |
| Gráficos | ngx-charts ou Chart.js |
| Testes frontend | Karma/Jasmine (`ng test`) |
| Infra | AWS: EC2 t3.small (API + PostgreSQL + Nginx), S3 + CloudFront (SPA), ACM, Parameter Store |
| IaC | CloudFormation |
| CI/CD | CodePipeline + CodeBuild + CodeDeploy |

## Comandos

Backend (a partir de `backend/`):

```powershell
dotnet restore
dotnet build
dotnet run --project src/Paga.Api
dotnet test
dotnet ef migrations add <Nome> --project src/Paga.Infrastructure --startup-project src/Paga.Api
dotnet ef database update --project src/Paga.Infrastructure --startup-project src/Paga.Api
```

Frontend (a partir de `frontend/`):

```powershell
npm install
npm start                  # ng serve
ng build --configuration production
ng test --watch=false      # sempre sem watch quando executado pelo agente
ng lint
```

> Nunca inicie `dotnet run`, `ng serve` ou qualquer watcher de forma bloqueante durante uma
> tarefa. Para verificar, use `dotnet build` / `ng build` / `dotnet test` / `ng test --watch=false`.
> Se um servidor precisar subir, peça ao usuário que rode no terminal dele.

## Configuração e segredos

- Nada de segredo em código ou commit. Local: `appsettings.Development.json` (gitignored) ou
  variáveis de ambiente. Produção: AWS Parameter Store (`JWT_KEY`, `DB_PASSWORD`).
- Frontend usa `environment.ts` / `environment.production.ts` apenas para `apiUrl`.
- Em produção o front chama `/api/*` no mesmo domínio (CloudFront → EC2), sem URL absoluta.

## Ambiente de desenvolvimento

Windows + PowerShell. Separador de comandos é `;`, não `&&`. Variáveis de ambiente: `$env:NOME`.
