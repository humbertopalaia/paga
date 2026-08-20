# Estrutura do Figma - PAGA (Palaia Acompanhamento de Gastos Automatizado)

> Este documento define a estrutura de páginas e frames do Figma que deve ser seguida para alinhar com os cards do Jira. Os cards referenciam os frames pelo nome exato descrito aqui.
>
> **Projeto:** [Abrir no Figma](https://www.figma.com/design/PKgbbuZodtcKGlSIFuvkcO/PAGA-%E2%80%94-Palaia-Acompanhamento-de-Gastos-Automatizado?node-id=0-1&t=bmkQyCuGELTI3Iuk-0)

---

## Estrutura de Páginas e Frames

```
📁 PAGA — Palaia Acompanhamento de Gastos Automatizado
│
├── 📄 Page: Auth
│   ├── Frame: Login
│   └── Frame: Login - Estados (erro, loading, campo inválido)
│
├── 📄 Page: Usuários
│   ├── Frame: Listagem - Usuários
│   ├── Frame: Form - Inclusão Usuário
│   ├── Frame: Form - Alteração Usuário
│   └── Frame: Modal - Confirmação Exclusão Usuário
│
├── 📄 Page: Layout
│   ├── Frame: Sidebar/Navbar
│   ├── Frame: Layout - Light Mode
│   └── Frame: Layout - Dark Mode
│
├── 📄 Page: Dashboard
│   ├── Frame: Dashboard - Visão Geral (cards de métricas)
│   └── Frame: Dashboard - Gráficos (barras por tipo de despesa)
│
├── 📄 Page: Tipos de Despesa
│   ├── Frame: Listagem - Tipos de Despesa
│   ├── Frame: Form - Inclusão Tipo
│   ├── Frame: Form - Alteração Tipo
│   └── Frame: Modal - Confirmação Exclusão
│
├── 📄 Page: Receitas
│   ├── Frame: Listagem - Receitas
│   ├── Frame: Form - Inclusão Receita
│   ├── Frame: Form - Alteração Receita
│   └── Frame: Form - Recorrência (estado condicional do toggle)
│
├── 📄 Page: Despesas
│   ├── Frame: Listagem - Despesas
│   ├── Frame: Form - Inclusão Despesa
│   ├── Frame: Form - Alteração Despesa
│   └── Frame: Form - Recorrência (estado condicional do toggle)
│
└── 📄 Page: Componentes
    ├── Frame: Design Tokens (cores, tipografia, espaçamentos)
    ├── Frame: Componentes Compartilhados (botões, inputs, cards, tabela)
    └── Frame: Estados (empty state, loading skeleton, error state)
```

---

## Detalhamento por Página

### Page: Auth

| Frame | Conteúdo |
|-------|----------|
| **Login** | Formulário centralizado com logo, campo email, campo senha, botão "Entrar" |
| **Login - Estados** | Variantes: campo com erro de validação, mensagem de erro da API (credenciais inválidas), botão em loading state |

### Page: Usuários

| Frame | Conteúdo |
|-------|----------|
| **Listagem - Usuários** | Header com título + botão "Novo Usuário", campo de busca por nome/email, tabela (Nome, Email, Data Criação, Ações), paginação |
| **Form - Inclusão Usuário** | Formulário: Nome, Email, Senha, Confirmação de Senha. Botões "Salvar" / "Cancelar" |
| **Form - Alteração Usuário** | Formulário: Nome, Email (campo de senha opcional para reset). Botões "Salvar" / "Cancelar" |
| **Modal - Confirmação Exclusão Usuário** | Modal overlay: ícone de alerta, texto "Deseja excluir o usuário X?", botões "Confirmar" e "Cancelar" |

### Page: Layout

| Frame | Conteúdo |
|-------|----------|
| **Sidebar/Navbar** | Menu lateral com ícones + texto: Dashboard, Usuários, Tipos de Despesa, Receitas, Despesas. Logo no topo. Estado colapsado para mobile |
| **Layout - Light Mode** | Layout completo (sidebar + header + content area) no tema claro |
| **Layout - Dark Mode** | Mesmo layout no tema escuro |

### Page: Dashboard

| Frame | Conteúdo |
|-------|----------|
| **Dashboard - Visão Geral** | 3 cards em grid: "Saldo Atual" (azul), "Receitas do Mês" (verde), "Despesas do Mês" (vermelho). Valores em destaque |
| **Dashboard - Gráficos** | Gráfico de barras horizontal com legenda mostrando despesas agrupadas por tipo no mês corrente |

### Page: Tipos de Despesa

| Frame | Conteúdo |
|-------|----------|
| **Listagem** | Header com título + botão "Novo Tipo", campo de busca por nome, tabela (ID, Nome, Ações), paginação no rodapé |
| **Form - Inclusão Tipo** | Formulário simples: campo Nome, botões "Salvar" e "Cancelar" |
| **Form - Alteração Tipo** | Mesmo formulário preenchido com dados existentes |
| **Modal - Confirmação Exclusão** | Modal overlay: ícone de alerta, texto "Deseja excluir o tipo X?", botões "Confirmar" e "Cancelar" |

### Page: Receitas

| Frame | Conteúdo |
|-------|----------|
| **Listagem - Receitas** | Header com título + botão "Nova Receita", área de filtros (data de/até, descrição, recorrente), tabela (Data, Descrição, Valor, Recorrente, Ações), paginação |
| **Form - Inclusão Receita** | Formulário: Data (datepicker), Descrição (input text), Valor (input com máscara R$), Recorrente (toggle switch). Botões "Salvar" / "Cancelar" |
| **Form - Alteração Receita** | Mesmo formulário preenchido com dados existentes |
| **Form - Recorrência** | Estado do formulário quando toggle Recorrente está ativo: campo adicional "Frequência" (select: Semanal, Mensal, Anual) aparece com animação |

### Page: Despesas

| Frame | Conteúdo |
|-------|----------|
| **Listagem - Despesas** | Header com título + botão "Nova Despesa", filtros (vencimento de/até, tipo [select], descrição, recorrente), tabela (Vencimento, Descrição, Tipo, Valor, Recorrente, Ações). Linhas vencidas com highlight vermelho sutil |
| **Form - Inclusão Despesa** | Formulário: Vencimento (datepicker), Descrição, Tipo (select carregado), Valor (máscara R$), Recorrente (toggle) + Frequência condicional. Botões "Salvar" / "Cancelar" |
| **Form - Alteração Despesa** | Mesmo formulário preenchido |
| **Form - Recorrência** | Estado com campo Frequência visível |

### Page: Componentes

| Frame | Conteúdo |
|-------|----------|
| **Design Tokens** | Paleta de cores (ver seção abaixo), tipografia (font family, sizes, weights), espaçamentos (4px grid), border-radius, shadows |
| **Componentes Compartilhados** | Botões (primary, secondary, danger, disabled), Inputs (default, focus, error, disabled), Cards (métrica), Tabela (header, row, hover), Select, Datepicker, Toggle, Toast/Snackbar (success, error), Paginação |
| **Estados** | Empty state (ilustração + texto "Nenhum registro encontrado"), Loading skeleton (placeholder animado), Error state (ícone + mensagem + botão retry) |

---

## Paleta de Cores

### Light Mode

| Token | Cor | Uso |
|-------|-----|-----|
| `--primary-50` | `#EFF6FF` | Background de cards, hover sutil |
| `--primary-100` | `#DBEAFE` | Background de elementos selecionados |
| `--primary-200` | `#BFDBFE` | Borders ativos |
| `--primary-300` | `#93C5FD` | Ícones secundários |
| `--primary-400` | `#60A5FA` | Links, ícones |
| `--primary-500` | `#3B82F6` | **Primary color** - botões, accent |
| `--primary-600` | `#2563EB` | Hover de botões primary |
| `--primary-700` | `#1D4ED8` | Active/pressed |
| `--primary-800` | `#1E40AF` | Texto em destaque |
| `--primary-900` | `#1E3A8A` | Headers, títulos fortes |
| `--bg-primary` | `#FFFFFF` | Background principal |
| `--bg-secondary` | `#F8FAFC` | Background de sidebar, cards |
| `--bg-tertiary` | `#F1F5F9` | Background de inputs, tabela header |
| `--text-primary` | `#1E293B` | Texto principal |
| `--text-secondary` | `#64748B` | Texto secundário, labels |
| `--text-muted` | `#94A3B8` | Placeholders |
| `--border` | `#E2E8F0` | Bordas gerais |
| `--success` | `#10B981` | Receitas, saldo positivo |
| `--danger` | `#EF4444` | Despesas, erros, exclusão |
| `--warning` | `#F59E0B` | Alertas, vencimento próximo |

### Dark Mode

| Token | Cor | Uso |
|-------|-----|-----|
| `--primary-50` | `#172554` | Background de cards, hover sutil |
| `--primary-100` | `#1E3A8A` | Background de elementos selecionados |
| `--primary-200` | `#1E40AF` | Borders ativos |
| `--primary-300` | `#2563EB` | Ícones secundários |
| `--primary-400` | `#3B82F6` | Links, ícones |
| `--primary-500` | `#60A5FA` | **Primary color** - botões, accent |
| `--primary-600` | `#93C5FD` | Hover de botões primary |
| `--primary-700` | `#BFDBFE` | Active/pressed |
| `--primary-800` | `#DBEAFE` | Texto em destaque |
| `--primary-900` | `#EFF6FF` | Headers, títulos fortes |
| `--bg-primary` | `#0F172A` | Background principal |
| `--bg-secondary` | `#1E293B` | Background de sidebar, cards |
| `--bg-tertiary` | `#334155` | Background de inputs, tabela header |
| `--text-primary` | `#F8FAFC` | Texto principal |
| `--text-secondary` | `#CBD5E1` | Texto secundário, labels |
| `--text-muted` | `#64748B` | Placeholders |
| `--border` | `#334155` | Bordas gerais |
| `--success` | `#34D399` | Receitas, saldo positivo |
| `--danger` | `#F87171` | Despesas, erros, exclusão |
| `--warning` | `#FBBF24` | Alertas, vencimento próximo |

---

## Tipografia

| Elemento | Font | Size | Weight |
|----------|------|------|--------|
| H1 (Título de página) | Inter | 24px | 700 (Bold) |
| H2 (Subtítulo) | Inter | 20px | 600 (SemiBold) |
| H3 (Seção) | Inter | 16px | 600 (SemiBold) |
| Body | Inter | 14px | 400 (Regular) |
| Body Small | Inter | 12px | 400 (Regular) |
| Label | Inter | 12px | 500 (Medium) |
| Button | Inter | 14px | 500 (Medium) |
| Metric Value (Dashboard) | Inter | 32px | 700 (Bold) |

---

## Espaçamentos (4px grid)

| Token | Valor | Uso |
|-------|-------|-----|
| `--spacing-xs` | 4px | Espaço entre ícone e texto |
| `--spacing-sm` | 8px | Padding interno de badges |
| `--spacing-md` | 16px | Padding de inputs, gap entre elementos |
| `--spacing-lg` | 24px | Padding de cards, seções |
| `--spacing-xl` | 32px | Margin entre blocos |
| `--spacing-2xl` | 48px | Padding da área de conteúdo |

---

## Convenção de Nomenclatura

Para manter consistência entre Figma e código:

| Figma | Angular Component |
|-------|-------------------|
| Frame: Login | `LoginComponent` |
| Frame: Listagem - Usuários | `UserListComponent` |
| Frame: Form - Inclusão Usuário | `UserFormComponent` (mode: create) |
| Frame: Form - Alteração Usuário | `UserFormComponent` (mode: edit) |
| Frame: Sidebar/Navbar | `SidebarComponent` |
| Frame: Dashboard - Visão Geral | `DashboardComponent` |
| Frame: Listagem - Tipos de Despesa | `ExpenseTypeListComponent` |
| Frame: Form - Inclusão Tipo | `ExpenseTypeFormComponent` (mode: create) |
| Frame: Form - Alteração Tipo | `ExpenseTypeFormComponent` (mode: edit) |
| Frame: Listagem - Receitas | `IncomeListComponent` |
| Frame: Form - Inclusão Receita | `IncomeFormComponent` (mode: create) |
| Frame: Listagem - Despesas | `ExpenseListComponent` |
| Frame: Form - Inclusão Despesa | `ExpenseFormComponent` (mode: create) |
| Frame: Modal - Confirmação Exclusão | `ConfirmDialogComponent` |

---

## Notas para o Designer

1. **Responsividade:** Considerar breakpoints: Mobile (< 768px), Tablet (768-1024px), Desktop (> 1024px)
2. **Estados interativos:** Todo componente deve ter estados: default, hover, focus, active, disabled
3. **Acessibilidade:** Contraste mínimo AA (4.5:1 para texto normal, 3:1 para texto grande)
4. **Consistência:** Usar Auto Layout no Figma para manter espaçamentos do grid
5. **Componentização:** Criar components no Figma para botões, inputs, cards — reusáveis entre páginas
6. **Dark Mode:** Duplicar frames principais com variante dark para validação visual

---

## Link do Figma

> **Projeto Figma:** [PAGA — Palaia Acompanhamento de Gastos Automatizado](https://www.figma.com/design/PKgbbuZodtcKGlSIFuvkcO/PAGA-%E2%80%94-Palaia-Acompanhamento-de-Gastos-Automatizado?node-id=0-1&t=bmkQyCuGELTI3Iuk-0)
>
> Para linkar frames específicos nos cards do Jira, use o formato: `https://www.figma.com/design/PKgbbuZodtcKGlSIFuvkcO/PAGA-%E2%80%94-Palaia-Acompanhamento-de-Gastos-Automatizado?node-id={NODE_ID}`
