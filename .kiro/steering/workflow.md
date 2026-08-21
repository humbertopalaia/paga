# Fluxo de trabalho

## Status da entrega

O MVP walking skeleton está **concluído e em produção**: Login, shell com menus e cadastro de
usuário, publicados na AWS por deploy manual.

| Spec | Stories | Status |
|------|---------|--------|
| `mvp-1-backend-foundation` | PP-7, PP-8 | Concluída |
| `mvp-2-auth-and-users-api` | PP-9, PP-10 | Concluída — restam testes de propriedade opcionais |
| `mvp-3-frontend-foundation` | PP-57, PP-58, PP-59 | Concluída — restam testes de propriedade opcionais |
| `mvp-4-login-and-users-ui` | PP-69, PP-70 | Concluída |
| `mvp-5-infra-manual-deploy` | PP-110 + parte de PP-112 | Concluída |
| `aws-deploy-checklist` | — | Concluída |
| `mvp-6-cicd` | PP-111 | Concluída — pipeline no ar, deploy automático em push na main |

Ajustes do MVP que continuam valendo no código:

- A sidebar mostra os cinco itens; Dashboard, Tipos de Despesa, Receitas e Despesas apontam para
  placeholder "Em construção" até o módulo correspondente ser entregue.
- `DashboardComponent` existe como placeholder na rota definitiva.
- O administrador semeado é `palaia@increvasenocanal.com`.

## Roadmap a partir daqui

Fatias verticais por módulo: cada spec junta a história de API com a de tela do mesmo módulo, e
termina com a fatia funcionando em produção — agora automaticamente, via pipeline.

| Ordem | Spec | Stories | Observação |
|-------|------|---------|------------|
| 1 | `module-expense-types` | PP-31 + PP-71 | Pré-requisito de Despesas |
| 2 | `module-incomes` | PP-32 + PP-72 | Independente |
| 3 | `module-expenses` | PP-33 + PP-73 | Depende de `module-expense-types` |
| 4 | `module-dashboard` | PP-34 + PP-74 | Depende de Receitas e Despesas |
| 5 | `mvp-7-hardening` | PP-112: PP-127, PP-128, PP-129, PP-130, PP-131 | Rate limiting, backup, logging, teste em produção |
| 6 | `mvp-8-custom-domain` | PP-112: PP-126 | Domínio próprio, ACM, TLS na origem |
| 7 | `docs-and-adrs` | PP-132 | Fecha o board |

**Dívida técnica em produção, postergada por decisão explícita do usuário** para depois dos módulos
de negócio: CloudFront → EC2 em HTTP, sem rate limiting, sem security headers e **sem backup do
banco**. Aceitável enquanto a base só tem o administrador semeado e dados de teste. O gatilho para
antecipar a `mvp-7` é o usuário começar a inserir lançamento financeiro real — a partir daí, perder
o banco passa a doer.

Ao concluir cada spec, transicione as histórias no Jira para Concluído (transição id `41`).

## Ao implementar uma Story

1. **Fonte de verdade é o Jira**, projeto **PP (PalaIA-PAGA)** em `upd8suporte.atlassian.net`
   (cloudId `3171b246-a4c7-4a72-ad59-6c18aa2a91a5`). Leia a história pelas ferramentas do Jira:
   descrição, acceptance criteria e subtarefas. Os arquivos em `docs/` são snapshot derivado e
   podem estar defasados.
2. Se a história tem tela, rode `get_design_context` no frame do Figma correspondente. Os node ids
   verificados estão em `.kiro/steering/design-system.md`.
3. Implemente seguindo a estrutura e os padrões de steering — não invente pasta, camada ou
   biblioteca nova.
4. Rode build e testes aplicáveis.
5. No resumo final, cite a história pelo key (ex: `PP-31`) e liste os acceptance criteria
   atendidos, sinalizando qualquer um que ficou pendente.
6. Ao concluir, transicione a história no Jira para Concluído (transição id `41`).

Se um acceptance criteria estiver ambíguo ou conflitar com o design ou com
`.kiro/steering/api-contract.md`, pergunte antes de escolher por conta própria.

## Git

- Remote: `https://github.com/humbertopalaia/paga.git` (`origin`), branch principal `main`.
- Branch por story, usando o key do Jira: `feature/<KEY>-<slug>` (ex: `feature/PP-32-incomes-api`).
- Nunca commitar direto em `main`.
- Commits em inglês, imperativo, escopo pequeno: `feat(incomes): add recurrence validation`.
  Prefixos: `feat`, `fix`, `refactor`, `test`, `docs`, `chore`, `infra`.
- Commit só quando o usuário pedir. Ao commitar, adicione arquivos por nome — nunca `git add .`.
- `appsettings.Development.json`, `.env`, chaves e artefatos de build ficam fora do versionamento.

## Manutenção da documentação

Mudança de contrato de API atualiza `.kiro/steering/api-contract.md` no mesmo trabalho.
Nova decisão técnica relevante gera um ADR em `docs/adr/`. Novo token de design entra no arquivo
de tokens e, se mudar o design system, em `docs/figma-structure.md`.
