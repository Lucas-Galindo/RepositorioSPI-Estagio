# Tasks: Remover Card "Cobertura de Custos" da Aba Indicadores

**Input**: Design documents from `/specs/024-remover-card-cobertura-custos/`

**Prerequisites**: [plan.md](./plan.md) (required), [spec.md](./spec.md) (required), [research.md](./research.md), [quickstart.md](./quickstart.md)

**Tests**: Não solicitados. O frontend não tem framework de teste automatizado configurado (ver
plan.md Technical Context), e esta mudança não altera nenhuma lógica de negócio testável — é
remoção de um bloco de apresentação e troca de uma className já existente. Validação via
[quickstart.md](./quickstart.md).

**Organization**: Tasks agrupadas por user story. US1 (P1, remover o card) e US2 (P2, realinhar
o layout) tocam o mesmo arquivo e por isso são sequenciais, não paralelizáveis entre si — mas
cada uma é independentemente testável conforme spec.md.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Pode rodar em paralelo (arquivos diferentes, sem dependência)
- **[Story]**: US1 (remover card), US2 (realinhar layout)
- Caminhos de arquivo exatos em cada descrição

## Path Conventions

Projeto web existente (frontend Next.js + backend ASP.NET Core + MySQL). Esta feature toca
SOMENTE:
- Frontend: `frontend/app/(app)/relatorios/financeiro/page.tsx`

Nenhum arquivo de backend, banco de dados ou CSS é criado ou alterado (a classe `kpi-row-3`
usada por US2 já existe em `frontend/styles/dashboard.css`).

---

## Phase 1: Setup

Não aplicável — projeto já inicializado, nenhuma dependência nova, nenhum arquivo novo a criar.
Prosseguir direto para a Fase 3 (não há Fase 2 Foundational: as duas user stories tocam o mesmo
único arquivo já existente, sem infraestrutura compartilhada a preparar antes).

---

## Phase 3: User Story 1 - Visualizar a visão Indicadores sem o card irrelevante (Priority: P1) 🎯 MVP

**Goal**: O card "Cobertura de custos" deixa de ser exibido na fileira de indicadores da aba
Indicadores do Relatório Financeiro, em qualquer filtro ou estado de dados.

**Independent Test**: Abrir Relatórios → Relatório Financeiro → aba Indicadores e verificar que
o card "Cobertura de custos" não aparece, em nenhuma combinação de filtros nem estado de dados.

### Implementation for User Story 1

- [X] T001 [US1] Em `frontend/app/(app)/relatorios/financeiro/page.tsx`, remover o bloco JSX
  completo do card "Cobertura de custos" (o `<div className="kpi">` que contém o texto
  "Cobertura de custos", o ícone `money`, o valor `indiceCoberturaCustosFixos` formatado como
  `"Nx"` e a legenda "Recebido ÷ pago no período" — localizado dentro da `.kpi-row` da aba
  Indicadores, junto aos cards de Inadimplência, Prazo médio de atraso e Margem de segurança).
  Não alterar os 3 cards vizinhos nem qualquer outro código fora desse bloco (FR-001, FR-002).

**Checkpoint**: User Story 1 completa e testável de forma independente — o card não aparece mais
em nenhum cenário, e os demais indicadores continuam exibindo os mesmos valores de antes.

---

## Phase 4: User Story 2 - Ver os indicadores restantes organizados de forma equilibrada (Priority: P2)

**Goal**: Os 3 cards restantes (Inadimplência, Prazo médio de atraso, Margem de segurança)
preenchem a fileira de forma equilibrada, sem espaço vazio no lugar do card removido.

**Independent Test**: Abrir a aba Indicadores em desktop e em mobile e verificar visualmente que
os 3 cards se distribuem em larguras iguais, preenchendo toda a fileira.

### Implementation for User Story 2

- [X] T002 [US2] Em `frontend/app/(app)/relatorios/financeiro/page.tsx`, na `<div>` da fileira de
  indicadores da aba Indicadores (a mesma `.kpi-row` de T001), trocar `className="kpi-row"` por
  `className="kpi-row kpi-row-3"` — classe já definida em `frontend/styles/dashboard.css`
  (`grid-template-columns: repeat(3, 1fr)`, ver research.md Decisão 2), sem criar CSS nova
  (FR-003, FR-004). Depende de T001 (a fileira já deve ter só 3 cards antes de ajustar o grid
  para 3 colunas, para evitar um estado intermediário inconsistente ao revisar o diff).

**Checkpoint**: As duas user stories funcionam em conjunto — o card foi removido e os 3
restantes preenchem a fileira de forma equilibrada, sem regressão nos valores exibidos.

---

## Phase 5: Polish & Cross-Cutting Concerns

**Purpose**: Verificação final cruzando as duas user stories, conforme
[quickstart.md](./quickstart.md).

- [X] T003 [P] Rodar `npx tsc --noEmit` e o lint do frontend
  (`frontend/app/(app)/relatorios/financeiro/page.tsx`) — sem erros.
- [ ] T004 Executar a validação completa do [quickstart.md](./quickstart.md) (Cenários 1 a 4):
  card ausente em todo filtro/estado, layout equilibrado em desktop e mobile, estados
  vazio/erro sem referência ao card, e confirmação de que o Dashboard e as demais abas do
  Relatório Financeiro não sofreram nenhuma alteração (FR-005, SC-001 a SC-003).
- [X] T005 Confirmar, por `git diff --stat`, que somente
  `frontend/app/(app)/relatorios/financeiro/page.tsx` foi alterado — nenhum arquivo de backend,
  banco de dados ou CSS tocado (ver Path Conventions acima).

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: N/A.
- **User Story 1 (Phase 3)**: Sem dependências — pode começar imediatamente.
- **User Story 2 (Phase 4)**: Depende de T001 (US1) estar concluída, por tocarem o mesmo
  arquivo e a mesma `<div>` (ver T002).
- **Polish (Phase 5)**: Depende de Phases 3 e 4 completas.

### Dentro de cada User Story

- T001 é uma única task de remoção, sem paralelização interna.
- T002 é uma única task de troca de className, sem paralelização interna, e depende de T001.

### Parallel Opportunities

- T001 e T002 tocam o mesmo arquivo (`page.tsx`) e a mesma região de código — não são
  paralelizáveis entre si, apesar de pertencerem a user stories diferentes.
- T003 (Polish) pode rodar em paralelo com a preparação do ambiente para T004, mas ambas
  dependem de T001/T002 já aplicadas.

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Completar Phase 3: User Story 1 (T001) → card removido → **entregável isoladamente como
   MVP** (a fileira fica com um vão vazio até a Phase 4, mas o pedido central — não exibir o
   card irrelevante — já está atendido)
2. Parar e validar Cenário 1 do quickstart.md

### Incremental Delivery

1. User Story 1 (T001) → card removido → MVP entregável
2. User Story 2 (T002) → layout realinhado → entregável isoladamente por cima do MVP
3. Polish (T003-T005) → verificação final cruzada

## Notes

- Tests: nenhuma automatizada nesta feature (ver seção "Tests" acima) — a validação é
  inteiramente manual via quickstart.md, mesma limitação pré-existente do projeto já registrada
  em specs anteriores (ex. specs/023).
- Nenhuma task desta lista deve tocar `src/SPI.Application/Dashboard/` nem
  `IndicadoresFinanceirosResponse` — o campo `indiceCoberturaCustosFixos` é compartilhado com o
  Dashboard e permanece intacto (ver research.md Decisão 3, FR-005).
