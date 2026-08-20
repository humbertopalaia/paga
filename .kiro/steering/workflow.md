# Fluxo de trabalho

## Entrega em curso: MVP walking skeleton

**A ordem dos sprints do board está suspensa em favor de uma fatia vertical.** O objetivo é ter
Login + shell com menus + cadastro de usuário rodando em produção na AWS antes de implementar os
demais módulos. Siga esta ordem:

| Spec | Stories | Entrega |
|------|---------|---------|
| `mvp-1-backend-foundation` | 1.1 + 1.2 | Solução .NET, 5 entidades, migration inicial, `/health`, admin semeado |
| `mvp-2-auth-and-users-api` | 1.3 + 1.4 | Login/refresh/logout + CRUD de usuários na API |
| `mvp-3-frontend-foundation` | 3.1 + 3.2 + 3.3 | Projeto Angular, tokens/tema, shell com sidebar |
| `mvp-4-login-and-users-ui` | 4.1 + 4.2 | Tela de login, guard/interceptor, telas de usuário |
| `mvp-5-infra-manual-deploy` | 5.1 + parte da 5.3 | Infra provisionada e deploy manual validado |
| `mvp-6-cicd` | 5.2 | CodePipeline end-to-end |

Ajustes que essa fatia exige:

- A sidebar mostra os **cinco** itens de menu desde a `mvp-3`. Dashboard, Tipos de Despesa,
  Receitas e Despesas têm rota registrada apontando para um placeholder "Em construção".
  Não esconda itens — a navegação precisa ficar estável.
- `DashboardComponent` existe como placeholder na rota definitiva, porque o login redireciona
  para ele.
- Existe um **usuário administrador semeado**, sem o qual é impossível logar em um banco novo.

Módulos fora do MVP (Stories 2.1–2.4 e 4.3–4.6) voltam à ordem original do board depois que a
`mvp-6` estiver validada.

## Ordem de execução (board completo, retomada após o MVP)

O board (`docs/jira-board.md`) define os sprints. Respeite as dependências: backend do módulo antes
do frontend do módulo, setup e tema antes das telas.

| Sprint | Stories | Foco |
|--------|---------|------|
| 1 | 1.1, 1.2, 1.3, 1.4, 3.1 | Setup backend e frontend, auth, CRUD usuários (API) |
| 2 | 2.1, 2.2, 2.3, 2.4 | Backend completo (CRUDs + dashboard) |
| 3 | 3.2, 3.3, 4.1, 4.2 | Tema, layout, login, CRUD usuários (front) |
| 4 | 4.3, 4.4, 4.5, 4.6 | Frontend completo (CRUDs + dashboard) |
| 5 | 5.1, 5.2, 5.3 | Infra e deploy |
| 6 | 6.1 | Documentação |

## Ao implementar uma Story

1. Leia a Story no board: descrição, *Acceptance Criteria* e subtasks.
2. Se houver referência de Figma, consulte o frame correspondente em `docs/figma-structure.md`
   antes de montar o layout.
3. Implemente seguindo a estrutura e os padrões de steering — não invente pasta, camada ou
   biblioteca nova.
4. Rode build e testes aplicáveis.
5. No resumo final, cite a Story e liste os *Acceptance Criteria* atendidos, sinalizando qualquer
   um que ficou pendente.

Se um *Acceptance Criteria* estiver ambíguo ou conflitar com o design/contrato, pergunte antes de
escolher por conta própria.

## Git

- Remote: `https://github.com/humbertopalaia/paga.git` (`origin`), branch principal `main`.
- Branch por story: `feature/<numero>-<slug>` (ex: `feature/2.2-incomes-api`).
- Nunca commitar direto em `main`.
- Commits em inglês, imperativo, escopo pequeno: `feat(incomes): add recurrence validation`.
  Prefixos: `feat`, `fix`, `refactor`, `test`, `docs`, `chore`, `infra`.
- Commit só quando o usuário pedir. Ao commitar, adicione arquivos por nome — nunca `git add .`.
- `appsettings.Development.json`, `.env`, chaves e artefatos de build ficam fora do versionamento.

## Manutenção da documentação

Mudança de contrato de API atualiza `.kiro/steering/api-contract.md` no mesmo trabalho.
Nova decisão técnica relevante gera um ADR em `docs/adr/`. Novo token de design entra no arquivo
de tokens e, se mudar o design system, em `docs/figma-structure.md`.
