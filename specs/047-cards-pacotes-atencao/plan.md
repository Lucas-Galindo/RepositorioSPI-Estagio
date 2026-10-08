# Implementation Plan: Cards do Painel de Pacotes em Atenção

**Branch**: `047-cards-pacotes-atencao` | **Date**: 2026-10-06 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/047-cards-pacotes-atencao/spec.md`

## Summary

Redesenha `PacotesEmAtencaoPanel.tsx` (spec 041) de uma lista de linhas (`week-event`) para cards compactos de largura fixa, em uma única fileira horizontal rolável — **revisado em 2026-10-07** após feedback visual real sobre a primeira passada (grid responsivo que esticava o card quando havia poucos pacotes). O card reaproveita `className="card card-small"`, a mesma combinação do card "Alunos atendidos" no topo da Home, com `card-eyebrow` (nome do aluno), `big-label` (contexto) acima de `big-value`/`big-label` (saldo em destaque + legenda) e o badge de estado já existente. A largura fixa, o `flex-shrink: 0` e a rolagem horizontal do próprio painel (`overflow-x: auto`) são aplicados via estilo inline — **nenhuma classe CSS nova** no resultado final (a `.pacotes-atencao-grid` da primeira passada foi removida; `git diff` em `dashboard.css` fica vazio).

## Technical Context

**Language/Version**: TypeScript / Next.js App Router (frontend) — nenhuma mudança de backend.

**Primary Dependencies**: Nenhuma nova — reaproveita `Icon` (já usado no badge atual) e as classes CSS já existentes em `frontend/styles/dashboard.css`.

**Storage**: N/A — nenhuma mudança de API/banco (FR-010).

**Testing**: Nenhuma suíte automatizada de frontend (mesma decisão já aceita nas specs 020-046) — validação via `quickstart.md`.

**Target Platform**: Web (Next.js App Router), mesma stack já em produção/dev local.

**Project Type**: Web application — esta feature é só frontend (1 componente + 1 regra CSS nova).

**Performance Goals**: Sem meta nova.

**Constraints**:
- FR-008/FR-009: MUST NOT introduzir cor ou componente novo quando há equivalente — resolvido reaproveitando `card card-small` (shell, igual ao card "Alunos atendidos"), `card-eyebrow`/`big-value`/`big-label` (título/contexto/destaque numérico/legenda) e `.badge-cancel`/`.badge-pending` (estado), todos já existentes e inalterados; `dashboard.css` fica sem nenhuma mudança líquida.
- FR-004/FR-005 (card de largura fixa, rolagem horizontal própria do painel): resolvido inteiramente com estilo inline de layout (`width`, `flexShrink`, `overflowX`, `display: flex`/`gap`) — ver `research.md` R1. Nenhuma classe CSS nova.
- FR-006: a ordenação (Esgotado antes de Atenção) continua vindo do backend (`DashboardService.MontarPacotesEmAtencao`, spec 041) — o componente só renderiza na ordem recebida, sem `sort` no frontend.
- FR-011: o `onClick`/navegação de cada card para `/alunos/{alunoId}` não muda — só a estrutura visual do elemento clicável.

**Scale/Scope**: 2 arquivos modificados — `frontend/components/dashboard/PacotesEmAtencaoPanel.tsx` (reescrita do JSX interno, mesma assinatura de props) e `frontend/app/(app)/dashboard/page.tsx` (1 ajuste de margem, `marginBottom: 24`, para o espaçamento pedido entre o painel e a Agenda da semana). `frontend/styles/dashboard.css` não tem mudança líquida (uma regra foi adicionada e depois removida ao longo da iteração). Nenhum arquivo novo, nenhuma mudança de backend/banco.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Princípio | Aplica-se? | Avaliação |
|---|---|---|
| I. Exclusão Lógica, Nunca Física | Não | Nenhuma exclusão de registro envolvida. |
| II. Validação de Negócio Única e Centralizada no Backend | Sim, preservado | A ordenação (Esgotado primeiro) e o cálculo do estado continuam só no backend (spec 041); o frontend só exibe na ordem recebida — nenhuma lógica de negócio duplicada ou movida para o frontend. |
| III. Nenhuma Capacidade Configurável Sem Implementação Real (NON-NEGOTIABLE) | Não | Nenhuma capacidade nova exposta — é um redesenho visual de uma capacidade já funcional. |
| IV. Autenticação e Segredos Seguros por Padrão | Não | Nenhuma mudança de autenticação/segredo. |
| V. Documentação Retroativa como Registro Histórico Vinculante | Não | Feature nova; não diverge de nenhuma spec retroativa — estende a spec 041 (comportamento de dados preservado). |

**Resultado**: Sem violação.

## Project Structure

### Documentation (this feature)

```text
specs/047-cards-pacotes-atencao/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── checklists/requirements.md
└── tasks.md             # Phase 2 output (/speckit-tasks - NOT created by /speckit-plan)
```

Sem diretório `contracts/` — nenhum contrato de API novo ou alterado (mesma decisão das specs 021/045/046, features frontend-only sem mudança de interface externa).

### Source Code (repository root)

```text
frontend/
├── components/dashboard/PacotesEmAtencaoPanel.tsx   # MODIFICADO: JSX interno trocado de lista (`week-event`) para cards de largura fixa (`card card-small`) em fila horizontal rolável, mesma prop `pacotes: PacoteEmAtencao[]`, mesmo EmptyState
└── app/(app)/dashboard/page.tsx                      # MODIFICADO: + `marginBottom: 24` no wrapper do painel (espaçamento em relação à Agenda da semana)
```

`frontend/styles/dashboard.css`: sem mudança líquida (ver `research.md` R1).

**Structure Decision**: Nenhum arquivo novo. A mudança é uma reescrita interna de um componente já existente, mais uma única regra de layout nova no stylesheet já existente.

## Post-Design Constitution Check

*Re-avaliação após Phase 1 (research.md, data-model.md, quickstart.md).*

- Princípio II: `data-model.md` confirma que o componente não reordena nem recalcula `estado`/`saldoAulas` — só mapeia os mesmos campos já recebidos do backend para a nova estrutura visual.
- Nenhum dos outros princípios é tocado pelo design (mesma conclusão da Constitution Check inicial).

**Resultado**: Sem violação bloqueante.

## Complexity Tracking

Não aplicável — nenhuma violação identificada em nenhum dos dois Constitution Checks.
