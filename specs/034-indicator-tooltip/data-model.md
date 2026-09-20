# Data Model: Tooltip Explicativo para Cards de Indicador

Nenhuma entidade de domínio/banco de dados envolvida — esta feature é puramente de apresentação
(frontend). O "modelo de dados" relevante aqui é o contrato de props do componente novo e o
mapeamento completo de onde ele é aplicado.

## Componente: `InfoTooltip`

| Prop | Tipo | Obrigatório | Descrição |
|---|---|---|---|
| `text` | `string` | Sim | Texto explicativo exibido no balão (linguagem simples, sem fórmula — FR-003). Também usado como `aria-label` do gatilho (FR-002a). |

Sem estado interno controlado externamente, sem callbacks — o componente é totalmente
autocontido (abre/fecha via CSS `:hover`/`:focus-within`, ver [research.md — R1](./research.md#r1--mecanismo-de-exibição-do-balão-hover--toque--teclado-sem-nova-dependência)).

## Mapeamento indicador → tela → arquivo → texto

| Indicador (rótulo na UI) | Tela | Arquivo | Container do gatilho | Texto do tooltip (FR-003) |
|---|---|---|---|---|
| Recebido este mês | Dashboard | `frontend/app/(app)/dashboard/page.tsx` | `.card-eyebrow` | "Total de contas a receber que já foram pagas dentro do mês atual." |
| Saldo realizado (mês) | Financeiro — Visão Geral | `frontend/app/(app)/financeiro/page.tsx` | `.kpi .label` | "Diferença entre o que já entrou e o que já saiu no período — o saldo que de fato aconteceu." |
| Saldo previsto | Financeiro — Visão Geral | `frontend/app/(app)/financeiro/page.tsx` | `.kpi .label` | "O saldo que você teria se tudo que está em aberto (a receber e a pagar) fosse recebido e pago." |
| Recebido | Relatório Financeiro — Visão Financeiro | `frontend/app/(app)/relatorios/financeiro/page.tsx` | `.kpi .label` | "Total efetivamente recebido dos alunos dentro do período filtrado." |
| Pago | Relatório Financeiro — Visão Financeiro | `frontend/app/(app)/relatorios/financeiro/page.tsx` | `.kpi .label` | "Total efetivamente pago em despesas dentro do período filtrado." |
| Saldo realizado | Relatório Financeiro — Visão Financeiro | `frontend/app/(app)/relatorios/financeiro/page.tsx` | `.kpi .label` | "Diferença entre o que já entrou e o que já saiu no período — o saldo que de fato aconteceu." |
| Receita pendente | Relatório Financeiro — Visão Financeiro | `frontend/app/(app)/relatorios/financeiro/page.tsx` | `.kpi .label` | "Total que ainda falta receber dos alunos, incluindo o que já está atrasado." |
| Despesa pendente | Relatório Financeiro — Visão Financeiro | `frontend/app/(app)/relatorios/financeiro/page.tsx` | `.kpi .label` | "Total que ainda falta pagar em despesas, incluindo o que já está atrasado." |
| Saldo previsto | Relatório Financeiro — Visão Financeiro | `frontend/app/(app)/relatorios/financeiro/page.tsx` | `.kpi .label` | "O saldo que você teria se tudo que está em aberto (a receber e a pagar) fosse recebido e pago." |
| Inadimplência | Relatório Financeiro — Indicadores | `frontend/app/(app)/relatorios/financeiro/page.tsx` | `.kpi .label` | "Percentual do valor total vencido no período que ainda não foi recebido." |
| Prazo médio de atraso | Relatório Financeiro — Indicadores | `frontend/app/(app)/relatorios/financeiro/page.tsx` | `.kpi .label` | "Quantos dias, em média, os pagamentos atrasados demoram para ser recebidos após o vencimento." |
| Fluxo de caixa (últimos 6 meses) | Relatório Financeiro — Indicadores | `frontend/app/(app)/relatorios/financeiro/page.tsx` | `<h4>` do `.mini-panel` | "Quanto entrou, quanto saiu e qual foi o saldo do seu caixa em cada um dos últimos 6 meses." |
| Ocupação média | Relatório de Turmas | `frontend/app/(app)/relatorios/turmas/page.tsx` | `.kpi .label` | "Número médio de alunos matriculados em cada turma ativa." |

13 pontos de inserção no total: "Recebido este mês" (1, de "Valor Faturado no Período"),
"Inadimplência" (1), "Prazo médio de atraso" (1), "Fluxo de caixa 6 meses" (1), "Ocupação média"
(1), "Recebido"/"Pago" (2, de "Total Recebido/Pago"), "Receita pendente"/"Despesa pendente" (2),
e "Saldo realizado"/"Saldo previsto" (4 — aparecem em 2 telas cada). "Fluxo de Caixa
Operacional", "Gargalo de Caixa" e "Taxa de Ocupação" real ficam fora do escopo (ver spec.md —
Assumptions).

## Fora de escopo (sem card ativo na UI hoje)

| Indicador | Motivo |
|---|---|
| Fluxo de Caixa Operacional | Calculado pela API, sem nenhuma tela consumidora. |
| Gargalo de Caixa | Removido intencionalmente da UI (specs/022), substituído pela tabela de lançamentos. |
| Taxa de Ocupação (Realizadas/Agendadas+Realizadas+Canceladas) | Calculado pela API (specs/016), sem nenhuma tela consumidora — "Ocupação média" é um indicador diferente e permanece em escopo. |
