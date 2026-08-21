---
inclusion: fileMatch
fileMatchPattern: 'frontend/**/*'
---

# Design system

Fonte de verdade completa (frames, paleta em light/dark, tipografia, espaçamentos e mapa
Figma → componente): #[[file:docs/figma-structure.md]]

## Regra central

Nenhum valor de cor, tamanho de fonte ou espaçamento é escrito literal em componente.
Sempre `var(--token)`. Se um token necessário não existe, adicione-o ao arquivo de tokens
primeiro e use-o depois.

```scss
// certo
.card { background: var(--bg-secondary); padding: var(--spacing-lg); color: var(--text-primary); }

// errado
.card { background: #F8FAFC; padding: 24px; color: #1E293B; }
```

## Tokens

Definidos em `frontend/src/app/styles/` — `_tokens.scss` (tipografia, espaçamento, radius, shadow)
e `_themes.scss` (as duas paletas). Light mode em `:root`, dark mode em `[data-theme="dark"]`,
com os **mesmos nomes de token** nos dois temas. Trocar o tema é trocar o atributo no `<html>`.

Grupos: `--primary-50..900`, `--bg-primary|secondary|tertiary`,
`--text-primary|secondary|muted`, `--border`, `--success`, `--danger`, `--warning`,
`--spacing-xs|sm|md|lg|xl|2xl` (grid de 4px).

## Semântica de cor

| Uso | Token |
|-----|-------|
| Ações primárias, links, accent | `--primary-500` (hover `600`, active `700`) |
| Receitas, saldo positivo | `--success` |
| Despesas, saldo negativo, exclusão, erro | `--danger` |
| Vencimento próximo, alertas | `--warning` |

Despesa vencida recebe destaque sutil com `--danger` (fundo com baixa opacidade), nunca a cor cheia.

## Tipografia

Inter. H1 24/700, H2 20/600, H3 16/600, body 14/400, small e label 12, botão 14/500,
valor de métrica do dashboard 32/700.

## Tema

`ThemeService` (core/) mantém o tema em um signal, respeita `prefers-color-scheme` no primeiro
acesso, persiste a escolha em `localStorage` e aplica `data-theme` no elemento raiz.
Transição suave apenas em `background-color` e `color` — não animar `all`.

## Componentes compartilhados

Botão (primary, secondary, danger, disabled), input (default, focus, error, disabled), card de
métrica, tabela, select, datepicker, toggle, toast/snackbar, paginação, confirm dialog, empty
state, loading skeleton e error state moram em `shared/`. Antes de criar um componente visual,
verifique se já existe um em `shared/`.

Todo componente interativo implementa os estados default, hover, focus, active e disabled.

## Responsividade

Mobile `< 768px`, tablet `768–1024px`, desktop `> 1024px`. Sidebar colapsa em mobile.
Cards do dashboard em grid que degrada para coluna única. Tabelas ganham scroll horizontal
ou layout empilhado no mobile.

## Mapa de frames no Figma

Arquivo `PKgbbuZodtcKGlSIFuvkcO`. Node ids verificados. Antes de implementar qualquer tela, rode
`get_design_context` no node id do frame correspondente.

| Página | Frame | node-id |
|--------|-------|---------|
| Auth (`0:1`) | Login | `15:2` |
| Auth | Login - Estados | `16:2` |
| Usuários (`9:2`) | Listagem - Usuários | `20:2` |
| Usuários | Form - Inclusão Usuário | `21:2` |
| Usuários | Form - Alteração Usuário | `21:25` |
| Usuários | Modal - Confirmação Exclusão Usuário | `21:44` |
| Layout (`9:3`) | Sidebar/Navbar | `18:2` |
| Layout | Layout - Light Mode | `18:22` |
| Layout | Layout - Dark Mode | `18:50` |
| Dashboard (`9:4`) | Dashboard - Visão Geral | `19:2` |
| Dashboard | Dashboard - Gráficos | `19:17` |
| Tipos de Despesa (`9:5`) | Listagem - Tipos de Despesa | `22:2` |
| Tipos de Despesa | Form - Inclusão Tipo | `22:54` |
| Tipos de Despesa | Form - Alteração Tipo | `22:65` |
| Tipos de Despesa | Modal - Confirmação Exclusão | `22:76` |
| Receitas (`9:6`) | Listagem - Receitas | `23:2` |
| Receitas | Form - Inclusão Receita | `24:2` |
| Receitas | Form - Alteração Receita | `24:25` |
| Receitas | Form - Recorrência | `24:48` |
| Receitas | Modal - Confirmação Exclusão | `32:2` |
| Despesas (`9:7`) | Listagem - Despesas | `25:2` |
| Despesas | Form - Inclusão Despesa | `26:2` |
| Despesas | Form - Alteração Despesa | `26:30` |
| Despesas | Form - Recorrência | `26:54` |
| Despesas | Modal - Confirmação Exclusão | `32:13` |
| Componentes (`9:8`) | Design Tokens | `11:2` |
| Componentes | Componentes Compartilhados | `12:2` |
| Componentes | Estados | `28:2` |

Duas peculiaridades deste arquivo:

- **Não há variables.** `get_variable_defs` volta vazio; os tokens estão como texto e fills dentro
  do frame Design Tokens (`11:2`). Extraia por `get_design_context`, não por variables.
- **A listagem de páginas do `get_metadata` é incompleta** neste arquivo: sem `nodeId` ela devolve
  apenas a página Auth. Para enumerar páginas, use `use_figma` percorrendo `figma.root.children`
  com `await page.loadAsync()`.
