# Produto — PAGA

**PAGA** = Palaia Acompanhamento de Gastos Automatizado.

Aplicação web de controle financeiro pessoal multiusuário. O usuário registra receitas e despesas
(com suporte a recorrência), classifica despesas por tipo e acompanha métricas do mês em um dashboard.

## Escopo funcional

| Módulo | O que faz |
|--------|-----------|
| **Autenticação** | Login com email/senha, JWT + refresh token, logout. Não existe auto-registro público |
| **Usuários** | CRUD administrativo de usuários do sistema (listagem com filtro, inclusão, alteração, exclusão) |
| **Tipos de Despesa** | CRUD de categorias de despesa, por usuário |
| **Receitas** | CRUD com data, descrição, valor e recorrência opcional |
| **Despesas** | CRUD com vencimento, descrição, tipo, valor e recorrência opcional |
| **Dashboard** | Saldo atual, receitas do mês, despesas do mês e despesas agrupadas por tipo |

## Domínio (glossário)

- **Receita / Income** — entrada de dinheiro. Possui `Date`.
- **Despesa / Expense** — saída de dinheiro. Possui `DueDate` (vencimento) e um `ExpenseType`.
- **Tipo de Despesa / ExpenseType** — categoria da despesa. Pertence a um usuário.
- **Recorrência** — quando `IsRecurring = true`, `Frequency` passa a ser obrigatório e aceita
  apenas `weekly`, `monthly`, `yearly`.
- **Saldo atual** — soma de todas as receitas menos soma de todas as despesas (all time).
- **Despesa vencida** — `DueDate < hoje`. Recebe destaque visual na listagem.

## Regras de produto que valem em qualquer camada

1. **Isolamento por usuário (multi-tenant):** todo dado de negócio pertence a um `UserId`.
   Nenhuma consulta, alteração ou exclusão pode atravessar usuários. O `UserId` vem sempre
   das claims do token, nunca do payload da request.
2. **Tudo autenticado:** exceto `/api/auth/login`, `/api/auth/refresh` e `/health`.
3. **Integridade referencial:** não é permitido excluir um Tipo de Despesa que tenha despesas
   vinculadas (a API responde 409).
4. **Valores monetários:** sempre `> 0`, exibidos em BRL (`R$ 1.234,56`).
5. **Idioma:** interface e mensagens ao usuário em **pt-BR**; código, identificadores, nomes de
   arquivo, commits e comentários em **inglês**.
6. **Usuário inicial:** como não há auto-registro, um banco novo é semeado com o administrador
   `palaia@increvasenocanal.com`. A senha vem de configuração (Parameter Store em produção),
   nunca de valor fixo em código. O seed só roda se não existir nenhum usuário.

## Rastreabilidade

O backlog vive em `docs/jira-board.md` (Epic > Story > Subtask) e o design em
`docs/figma-structure.md`. Toda implementação deve corresponder a uma Story e satisfazer
os *Acceptance Criteria* dela. Ao concluir, cite a Story (ex: `Story 4.3`) no resumo.
