---
inclusion: fileMatch
fileMatchPattern: 'infra/**/*'
---

# Infraestrutura AWS

Toda infra é declarada em CloudFormation dentro de `infra/`. Mudança feita no console tem que ser
refletida no template — o template é a fonte de verdade.

## Ambiente do usuário

- Credenciais AWS no profile **`palaia`**. Todo comando `aws` deve incluir `--profile palaia`.
- Repositório: `https://github.com/humbertopalaia/paga.git` (source do CodePipeline).

## Deploy de validação (fase MVP)

Simplificações aceitas apenas enquanto o objetivo é validar o walking skeleton:

- **Domínio default do CloudFront** (`*.cloudfront.net`) com o certificado default. Domínio próprio
  e ACM em `us-east-1` entram no hardening.
- **CloudFront → EC2 em HTTP.** O salto viewer → CloudFront é HTTPS, mas a origem não tem TLS.
  Isso é aceitável só para validação: restrinja o Security Group da EC2 à prefix list
  `com.amazonaws.global.cloudfront.origin-facing` para a origem não ficar acessível direto,
  e adicione TLS na origem antes de tratar dado real.
- **Migrations** aplicadas por script SQL idempotente
  (`dotnet ef migrations script --idempotent`) executado com `psql` num hook do CodeDeploy.
  Não instale as ferramentas do EF na EC2 e não chame `Migrate()` no startup em produção.
- Já entram nesta fase, não são opcionais: CORS restrito à origem do CloudFront, porta 5432
  fechada, porta 22 restrita ao IP do usuário e `/health` respondendo.

## Topologia

```
Internet → CloudFront (ACM/HTTPS)
             ├── default behavior  → S3 (SPA Angular, static website)
             └── /api/*            → EC2 t3.small
                                      └── Nginx (443/80) → Kestrel :5000
                                          PostgreSQL local
```

## Organização de `infra/`

```
infra/
├── cloudformation/   network.yaml, ec2.yaml, frontend.yaml (S3+CloudFront), iam.yaml
├── buildspec-frontend.yml
├── buildspec-backend.yml
├── appspec.yml
├── scripts/          stop.sh, install.sh, start.sh, validate.sh, backup-db.sh
└── nginx/            paga.conf
```

## Regras

- **Segurança do SG:** 443 e 80 abertos; 22 restrito a IP/prefix list conhecido. PostgreSQL
  (5432) nunca exposto — acesso só via localhost na EC2.
- **Segredos:** `JWT_KEY` e `DB_PASSWORD` no Parameter Store como `SecureString`, lidos em runtime.
  Nenhum segredo em template, buildspec, script ou variável de ambiente commitada.
- **IAM:** least privilege. Uma role por serviço (CodeBuild, CodeDeploy, EC2 instance profile),
  sem wildcard em `Resource` quando o ARN é conhecido.
- **Parametrize** ambiente, domínio, nome de bucket e tipo de instância; não hardcode.
- **Nginx:** SSL termination, reverse proxy `/api` → `localhost:5000` e headers de segurança
  (HSTS, X-Frame-Options, X-Content-Type-Options, Referrer-Policy).
- **CORS na API:** apenas o domínio do CloudFront. Nunca `AllowAnyOrigin` em produção.
- **Rate limiting:** 100 req/min por IP.
- **Backup:** `pg_dump` diário via cron, upload para S3 com versionamento e ciclo de vida.
- **Logs:** Serilog em arquivo com rotação, ou CloudWatch. Sem PII e sem token nos logs.

## Pipeline

Push em `main` dispara o CodePipeline. Frontend: `npm ci` → `ng build --configuration production`
→ `aws s3 sync` → invalidação do CloudFront. Backend: `dotnet publish` → artefato → CodeDeploy
com hooks `ApplicationStop`, `BeforeInstall`, `ApplicationStart`, `ValidateService` (o validate
bate no `/health`).

## Ações destrutivas

Nunca execute `aws cloudformation delete-stack`, `deploy`, `update-stack`, `s3 rm` ou qualquer
comando que altere recursos AWS reais sem o usuário pedir e confirmar explicitamente. Escrever
e validar template (`aws cloudformation validate-template`) é seguro; aplicar não é.
