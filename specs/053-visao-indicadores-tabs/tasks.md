---

description: "Task list for Padronizar Visual do Alternador Visão Financeiro/Indicadores"
---

# Tasks: Padronizar Visual do Alternador Visão Financeiro/Indicadores

**Input**: Design documents from `specs/053-visao-indicadores-tabs/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md (N/A), quickstart.md

**Tests**: não solicitados no spec (ajuste visual isolado, sem lógica de negócio); o projeto não tem suíte de testes de frontend configurada. Toda validação é manual via `quickstart.md`.

**Organization**: tarefas agrupadas pela única user story (US1 P1 — visual igual ao de Contas a Receber/Pagar).

## Format: `[ID] [P?] [Story] Description`

Nenhuma tarefa exige credenciais reais ou dado de produção — mas a validação visual (T003-T005) exige o app rodando no navegador, por isso seguem marcadas como manuais.

## Path Conventions

Esta feature toca **somente** `frontend/styles/dashboard.css` e `frontend/app/(app)/relatorios/financeiro/page.tsx` — nenhum outro arquivo, nenhuma mudança de backend/banco/`financeiro/layout.tsx`.

---

## Phase 1: Setup

Não aplicável — nenhuma dependência nova, nenhuma configuração a alterar.

---

## Phase 2: Foundational (Blocking Prerequisites)

Não aplicável — feature pequena o suficiente para viver inteira na única user story (2 arquivos, sem pré-requisito compartilhado além dela mesma).

---

## Phase 3: User Story 1 - Alternador com o mesmo visual das abas de Contas a Receber/Pagar (Priority: P1) 🎯 MVP

**Goal**: o alternador "Visão Financeiro"/"Visão de Indicadores" passa a ocupar a largura total do contêiner, dividida igualmente entre os 2 botões — mesmo padrão visual das abas "Visão Geral"/"Contas a Receber"/"Contas a Pagar" — sem nenhuma mudança de comportamento de clique.

**Independent Test**: abrir Relatórios → Relatório Financeiro e comparar visualmente o alternador com as abas de Financeiro → Contas a Receber/Pagar; clicar nos 2 botões e confirmar que a troca de aba continua funcionando.

- [X] T001 [US1] Em `frontend/styles/dashboard.css`, estender o seletor `.financeiro-tabs > a` (hoje `flex: 1 1 0; justify-content: center;`) para também cobrir `.financeiro-tabs > button`, usando a mesma declaração para os dois seletores (research.md R1) — não criar nenhuma classe nova, não alterar a regra `.financeiro-tabs { display:flex; align-items:stretch; }` nem nenhuma outra regra do arquivo
- [X] T002 [US1] Em `frontend/app/(app)/relatorios/financeiro/page.tsx` (linha ~203, depende de T001): trocar `<div className="row-gap" style={{ marginBottom: 18, gap: 8 }}>` (o `<div>` que envolve os botões "Visão Financeiro"/"Visão de Indicadores") por `<div className="financeiro-tabs" style={{ marginBottom: 18 }}>` — remover `gap: 8` do `style` (research.md R2); não alterar nenhuma outra linha do arquivo: os 2 `<button>` internos continuam com o mesmo `onClick`, a mesma classe condicional `btn btn-sm ${aba === "..." ? "btn-primary" : "btn-ghost"}` e o mesmo texto
- [X] T003 (parte manual) [US1] Validar o §1 de [quickstart.md](./quickstart.md): o alternador ocupa a largura total, 2 botões do mesmo tamanho, mesmo padrão visual das abas de Contas a Receber/Pagar (FR-001, SC-001) — validado pelo usuário em produção local
- [X] T004 (parte manual) [US1] Validar o §2 de [quickstart.md](./quickstart.md): clicar nos 2 botões continua trocando o conteúdo local sem navegar de URL; aplicar um filtro, navegar e voltar confirma que a persistência de specs/048 continua intacta (FR-002, SC-002) — validado pelo usuário em produção local

**Checkpoint**: User Story 1 completa e testável de forma independente — já entrega o pedido inteiro do card.

---

## Phase 4: Polish & Cross-Cutting Concerns

**Purpose**: não-regressão e verificação dos requisitos "MUST NOT" (regra do `CLAUDE.md`)

- [X] T005 (parte manual) Validar o §3 de [quickstart.md](./quickstart.md) (FR-003, SC-003): as 3 abas de Financeiro (Visão Geral/Contas a Receber/Contas a Pagar) continuam com o visual e comportamento de sempre, nenhuma mudança perceptível — validado pelo usuário em produção local
- [ ] T006 (parte manual) Validar o §4 de [quickstart.md](./quickstart.md) (Edge Case): em tela estreita, o alternador continua esticando os 2 botões proporcionalmente, legível e clicável — ainda não confirmado explicitamente pelo usuário (validação geral não mencionou tela estreita)
- [X] T007 Regra do `CLAUDE.md` para FR-003/FR-004 ("MUST NOT alterar as abas de referência" e "MUST NOT criar arquivo além do necessário"): rodar `git status --porcelain -- frontend` e confirmar que não aparece nenhum arquivo não rastreado; rodar `git diff --stat -- frontend` e confirmar que só os 2 arquivos esperados (`dashboard.css`, `relatorios/financeiro/page.tsx`) foram alterados — em especial, confirmar que `frontend/app/(app)/financeiro/layout.tsx` não aparece no diff
- [X] T008 Regra do `CLAUDE.md` para FR-002 ("MUST NOT alterar o comportamento de clique"): ler diretamente o diff de `relatorios/financeiro/page.tsx` e confirmar que a única mudança é no `className`/`style` do `<div>` wrapper (T002) — nenhuma linha de `onClick`, `useState("aba")`, ou da lógica condicional de classe dos botões foi tocada

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: não aplicável.
- **Foundational (Phase 2)**: não aplicável.
- **User Story 1 (Phase 3)**: T002 depende de T001 (mesmo efeito visual só aparece depois do CSS estendido); T003/T004 dependem de T001+T002 implementados.
- **Polish (Phase 4)**: depende da Phase 3 completa.

### Parallel Opportunities

- T003 e T004 podem ser validados em paralelo entre si, depois da implementação pronta.
- T005/T006 (validação visual) e T007/T008 (verificação de git/diff) são independentes entre si.

---

## Implementation Strategy

### MVP First (User Story 1)

1. Completar Phase 1 (N/A) e Phase 2 (N/A).
2. Completar Phase 3 (T001-T004) — alternador com o novo visual, entregável como MVP (e como entrega completa, já que não há outra user story).
3. Phase 4 (Polish) é a validação final de não-regressão.

### Incremental Delivery

Foundational (N/A) → US1 (T001-T004, MVP e entrega completa) → Polish (T005-T008).

## Notes

- Nenhuma tarefa desta lista deve tocar `frontend/app/(app)/financeiro/layout.tsx`, nenhuma lógica de negócio, nem criar nenhum arquivo novo — tudo vive dentro dos 2 arquivos já existentes (FR-003/FR-004).
