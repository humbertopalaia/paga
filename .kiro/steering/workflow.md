# Fluxo de trabalho

## Status da entrega

O produto está **completo e em produção**. Os seis módulos funcionais — autenticação, usuários,
tipos de despesa, receitas, despesas e dashboard — estão publicados na AWS, com deploy automático
por pipeline a cada push na `main`.

| Spec | Stories | Status |
|------|---------|--------|
| `mvp-1-backend-foundation` | PP-7, PP-8 | Concluída |
| `mvp-2-auth-and-users-api` | PP-9, PP-10 | Concluída — restam property tests opcionais |
| `mvp-3-frontend-foundation` | PP-57, PP-58, PP-59 | Concluída — restam property tests opcionais |
| `mvp-4-login-and-users-ui` | PP-69, PP-70 | Concluída |
| `mvp-5-infra-manual-deploy` | PP-110, PP-125 | Concluída |
| `aws-deploy-checklist` | — | Concluída |
| `mvp-6-cicd` | PP-111 | Concluída — deploy automático em push na `main` |
| `module-expense-types` | PP-31, PP-71 | Concluída |
| `module-incomes` | PP-32, PP-72 | Concluída — restam property tests opcionais |
| `module-expenses` | PP-33, PP-73 | Concluída — restam property tests opcionais |
| `module-dashboard` | PP-34, PP-74 | Concluída — restam property tests opcionais |

Estado do código que vale saber antes de mexer:

- Os cinco itens da sidebar apontam para módulos reais. O `PlaceholderComponent` em `shared/` ficou
  órfão e pode ser removido.
- O administrador semeado é `palaia@increvasenocanal.com`.
- Compartilhados em uso por mais de um módulo: `RecurrenceSelectorComponent`, `CurrencyMaskDirective`
  e `ConfirmDialogComponent`. Reutilize, não recrie.
- Há **18 tarefas de property test opcionais** acumuladas em `mvp-2`, `mvp-3`, `module-incomes`,
  `module-expenses` e `module-dashboard`. Decisão pendente: executar num bloco único ou remover dos
  `tasks.md` para não virar pendência permanente.
- `DesignTimeDbContextFactory.cs` tem credencial de dev fixa (`postgres/postgres`) num repositório
  público. Valor inofensivo, padrão ruim. Decisão pendente.

## Roadmap a partir daqui

| Ordem | Spec | Stories | Observação |
|-------|------|---------|------------|
| 1 | `mvp-7-hardening` | PP-127, PP-128, PP-129, PP-130, PP-131 | Rate limiting, security headers, backup, logging, teste em produção |
| 2 | `mvp-8-custom-domain` | PP-126 | Domínio próprio, ACM, TLS na origem |
| 3 | `docs-and-adrs` | PP-132 | Fecha o board |

**Dívida técnica em produção:** CloudFront → EC2 em HTTP, sem rate limiting, sem security headers e
sem backup do banco. Com o produto completo, o gatilho que registramos — começar a lançar dado
financeiro real — está iminente, então a `mvp-7` deixou de ser postergável com conforto.

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
