---

description: "Task list for feature 034 - indicator-tooltip"
---

# Tasks: Tooltip Explicativo para Cards de Indicador

**Input**: Design documents from `/specs/034-indicator-tooltip/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, quickstart.md

**Tests**: Sem framework de teste automatizado no frontend (research.md — R4). Validação é manual,
via quickstart.md, ao final da implementação — não há tarefas de teste automatizado nesta lista.

**Organization**: Tarefas organizadas pelas 2 user stories de spec.md (US1 = P1, US2 = P2).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Pode rodar em paralelo (arquivos diferentes, sem dependência)
- **[Story]**: US1 ou US2
- Caminhos de arquivo exatos incluídos em cada descrição

## Path Conventions

Frontend Next.js/TypeScript em `frontend/` (ver plan.md — Project Structure). Nenhum arquivo de
`src/` (backend .NET) é tocado nesta feature.

---

## Phase 1: Setup (Shared Infrastructure)

Não aplicável — nenhuma dependência nova, nenhuma configuração de projeto necessária (ver plan.md
— Primary Dependencies: "Nenhuma nova dependência").

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Componente reutilizável e infraestrutura visual que ambas as user stories (US1 e
US2) dependem — nenhuma tela pode receber o tooltip antes disso existir.

**⚠️ CRITICAL**: Nenhuma tarefa de US1/US2 pode começar antes desta fase estar completa.

- [X] T001 [P] Adicionar entrada `info` (círculo com "i", mesmo estilo `stroke`/`viewBox 0 0 24 24` dos ícones existentes) ao registro `PATHS` em `frontend/components/shared/Icon.tsx` (research.md — R2).
- [X] T002 [P] Adicionar regras CSS `.info-tooltip`, `.info-tooltip-trigger` e `.info-tooltip-bubble` em `frontend/styles/dashboard.css`: wrapper `position:relative; display:inline-flex`; gatilho como `<button>` sem borda/fundo próprio (herda `currentColor`, tamanho igual ao ícone `size={12}` já usado nos labels); balão com `position:absolute`, `opacity:0`, `visibility:hidden`, `transition`, exibido via `.info-tooltip:hover .info-tooltip-bubble, .info-tooltip:focus-within .info-tooltip-bubble { opacity:1; visibility:visible; }` (research.md — R1).
- [X] T003 Criar `frontend/components/shared/InfoTooltip.tsx`: componente que recebe a prop `text: string` (obrigatória, data-model.md — contrato de props), renderiza `<span className="info-tooltip"><button type="button" className="info-tooltip-trigger" aria-label={text}><Icon name="info" size={12} /></button><span className="info-tooltip-bubble" role="tooltip">{text}</span></span>` — depende de T001 (ícone `info` deve existir) e T002 (classes CSS devem existir).

**Checkpoint**: `InfoTooltip` pronto para ser importado e usado em qualquer página — nenhuma
lógica de hover/toque/foco duplicada por tela (research.md — R1; FR-001, FR-002, FR-002a).

---

## Phase 3: User Story 1 - Entender um indicador sem sair da tela (Priority: P1) 🎯 MVP

**Goal**: Todo card/tabela de indicador atualmente exibido nas 4 telas em escopo passa a ter um
ícone de informação que exibe, via hover, toque ou foco de teclado, um balão com o texto
explicativo definido em data-model.md — nunca fórmula técnica.

**Independent Test**: Abrir cada uma das 4 telas (Dashboard, Financeiro, Relatório Financeiro,
Relatório de Turmas), passar o mouse sobre cada ícone de informação, e confirmar que o balão
mostra o texto exato definido em data-model.md, sem nome de campo/fórmula (quickstart.md, passos
1-5).

### Implementation for User Story 1

- [X] T004 [US1] Em `frontend/app/(app)/dashboard/page.tsx`, inserir `<InfoTooltip text="Total de contas a receber que já foram pagas dentro do mês atual." />` como último filho do `.card-eyebrow` do card "Recebido este mês" (data-model.md, linha 1; research.md — R3).
- [X] T005 [US1] Em `frontend/app/(app)/financeiro/page.tsx`, inserir `<InfoTooltip>` nos labels `.kpi .label` de "Saldo realizado (mês)" (texto: "Diferença entre o que já entrou e o que já saiu no período — o saldo que de fato aconteceu.") e "Saldo previsto" (texto: "O saldo que você teria se tudo que está em aberto (a receber e a pagar) fosse recebido e pago.") — data-model.md, linhas 2-3.
- [X] T006 [US1] Em `frontend/app/(app)/relatorios/financeiro/page.tsx`, aba Visão Financeiro (linhas ~210-249), inserir `<InfoTooltip>` nos labels `.kpi .label` de "Recebido" (texto: "Total efetivamente recebido dos alunos dentro do período filtrado."), "Pago" (texto: "Total efetivamente pago em despesas dentro do período filtrado."), "Saldo realizado" (texto: "Diferença entre o que já entrou e o que já saiu no período — o saldo que de fato aconteceu."), "Receita pendente" (texto: "Total que ainda falta receber dos alunos, incluindo o que já está atrasado."), "Despesa pendente" (texto: "Total que ainda falta pagar em despesas, incluindo o que já está atrasado.") e "Saldo previsto" (texto: "O saldo que você teria se tudo que está em aberto (a receber e a pagar) fosse recebido e pago.") — data-model.md, linhas 4-9.
- [X] T007 [US1] Em `frontend/app/(app)/relatorios/financeiro/page.tsx`, aba Indicadores (linhas ~350-369), inserir `<InfoTooltip>` nos labels `.kpi .label` de "Inadimplência" (texto: "Percentual do valor total vencido no período que ainda não foi recebido.") e "Prazo médio de atraso" (texto: "Quantos dias, em média, os pagamentos atrasados demoram para ser recebidos após o vencimento.") — data-model.md, linhas 10-11. Depende de T006 (mesmo arquivo).
- [X] T008 [US1] Em `frontend/app/(app)/relatorios/financeiro/page.tsx`, aba Indicadores (linhas ~441-444), inserir `<InfoTooltip text="Quanto entrou, quanto saiu e qual foi o saldo do seu caixa em cada um dos últimos 6 meses." />` como último filho do `<h4>` do `.mini-panel` "Fluxo de caixa (últimos 6 meses)" (data-model.md, linha 12; research.md — R3, tooltip no título da tabela, não em cada célula). Depende de T007 (mesmo arquivo).
- [X] T009 [US1] Em `frontend/app/(app)/relatorios/turmas/page.tsx`, inserir `<InfoTooltip text="Número médio de alunos matriculados em cada turma ativa." />` como último filho do `.kpi .label` de "Ocupação média" (data-model.md, linha 13) — texto descreve o que o card realmente calcula (média de alunos por turma), não a fórmula de "Taxa de Ocupação" original (spec.md — Clarifications).
- [ ] T010 [US1] Validar manualmente hover, toque (emulado) e Tab/foco de teclado em todos os 13 pontos de inserção das 4 telas, seguindo quickstart.md passos 1-8, incluindo a checagem de `aria-label`/`aria-describedby` no DevTools (FR-002, FR-002a, FR-006).

**Checkpoint**: Todos os 13 pontos de inserção (SC-001) têm tooltip funcional via hover, toque e
teclado, com texto sem fórmula técnica (SC-002) — User Story 1 completa e testável de forma
independente.

---

## Phase 4: User Story 2 - Adicionar tooltip a um indicador novo sem duplicar código (Priority: P2)

**Goal**: Confirmar que o padrão usado nas inserções da Fase 3 realmente exige só uma linha de
código por indicador (renderizar `<InfoTooltip text="..."/>`), sem nenhuma lógica de
hover/balão/posicionamento duplicada por tela.

**Independent Test**: Revisar o diff de cada arquivo alterado na Fase 3 e confirmar que a única
mudança por indicador é a inserção de `<InfoTooltip text="..."/>` dentro do container de label já
existente — nenhuma nova classe CSS, `useState`, ou handler de evento por página (spec.md —
Acceptance Scenario US2 #1).

### Implementation for User Story 2

- [X] T011 [US2] Revisar as edições de T004-T009 e confirmar que cada inserção é uma única linha (`<InfoTooltip text="..."/>`) sem CSS/lógica nova por página — nenhuma tarefa adicional de código esperada; se alguma inserção precisou de mais que isso, documentar o desvio e avaliar se `InfoTooltip`/CSS de T001-T003 precisa de ajuste (SC-003, quickstart.md passo 9).

**Checkpoint**: Reutilização confirmada — adicionar um tooltip a um indicador futuro (fora desta
feature) exigirá apenas o mesmo padrão de uma linha.

---

## Phase Final: Polish & Cross-Cutting Concerns

- [ ] T012 Rodar a validação de regressão visual de quickstart.md (seção "Regressão"): abrir as 4 telas e confirmar que o layout dos cards (posição de ícone de categoria, valor, delta) permanece igual ao anterior, só com o ícone de informação novo visível ao lado do texto do label.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: Não aplicável.
- **Foundational (Phase 2)**: Sem dependências — pode começar imediatamente. BLOQUEIA todas as tarefas de US1 e US2.
- **User Story 1 (Phase 3)**: Depende de Phase 2 completa (T001-T003).
- **User Story 2 (Phase 4)**: Depende de Phase 3 completa (revisa as inserções feitas nela).
- **Polish (Phase Final)**: Depende de Phase 3 completa; pode rodar em paralelo com Phase 4.

### Within Phase 2

- T001 e T002 são independentes entre si (arquivos diferentes) — `[P]`.
- T003 depende de T001 (usa `<Icon name="info"/>`) e T002 (usa as classes CSS).

### Within Phase 3 (US1)

- T004, T005, T006 são independentes entre si (arquivos diferentes) — `[P]` implícito, mas todas dependem de T001-T003.
- T007 e T008 dependem de T006 (mesmo arquivo `relatorios/financeiro/page.tsx`, edições sequenciais).
- T009 é independente de T004-T008 (arquivo diferente).
- T010 depende de T004-T009 completos (valida o resultado final).

### Parallel Opportunities

- T001 e T002 em paralelo (Foundational).
- T004, T005, T006 e T009 em paralelo entre si, depois de T001-T003 completos (arquivos diferentes).
- T012 (Polish) pode rodar em paralelo com T011 (US2), já que ambas são apenas revisão/validação sem novo código.

---

## Parallel Example: Foundational + User Story 1

```bash
# Fase 2, em paralelo:
Task: "Adicionar ícone 'info' em Icon.tsx (T001)"
Task: "Adicionar regras CSS .info-tooltip em dashboard.css (T002)"

# Fase 3, em paralelo (após T001-T003 completos):
Task: "Inserir InfoTooltip em dashboard/page.tsx (T004)"
Task: "Inserir InfoTooltip em financeiro/page.tsx (T005)"
Task: "Inserir InfoTooltip em relatorios/financeiro/page.tsx - Visão Financeiro (T006)"
Task: "Inserir InfoTooltip em relatorios/turmas/page.tsx (T009)"
```

---

## Implementation Strategy

### MVP First (User Story 1)

1. Completar Phase 2: Foundational (T001-T003) — componente pronto.
2. Completar Phase 3: User Story 1 (T004-T010) — todos os 13 pontos de inserção com tooltip funcional.
3. **STOP and VALIDATE**: rodar quickstart.md passos 1-8 (feito em T010).
4. Isso já entrega o valor central do pedido — MVP completo.

### Incremental Delivery

1. Foundational → componente pronto.
2. User Story 1 → todas as telas com tooltip (valor entregue, MVP).
3. User Story 2 → confirmação de reutilização (T011) — não adiciona nenhuma tela nova, apenas valida a qualidade da implementação já feita.
4. Polish → validação de regressão visual (T012).

---

## Notes

- [P] = arquivos diferentes, sem dependência sequencial.
- Nenhuma tarefa introduz dependência nova, migração de banco ou mudança de contrato de API (ver plan.md).
- Sem framework de teste automatizado no frontend — toda validação é manual via quickstart.md.
- Textos de tooltip usados nas tarefas são citados verbatim de data-model.md — não usar paráfrase diferente durante a implementação.
