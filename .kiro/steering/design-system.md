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
