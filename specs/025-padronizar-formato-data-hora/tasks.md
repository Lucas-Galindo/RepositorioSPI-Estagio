# Tasks: Padronizar Formato Brasileiro de Data e Hora

**Input**: Design documents from `/specs/025-padronizar-formato-data-hora/`

**Prerequisites**: [plan.md](./plan.md) (required), [spec.md](./spec.md) (required),
[research.md](./research.md), [quickstart.md](./quickstart.md)

**Tests**: Não solicitados. O frontend não tem framework de teste automatizado configurado (ver
plan.md Technical Context) — validação via [quickstart.md](./quickstart.md).

**Organization**: Tasks agrupadas por user story. US1 (P1, datas dd/mm/aaaa) e US2 (P1,
horários 24h) dependem da Fase 2 (Foundational — os novos componentes `DateInput`/`TimeInput`).
US1 e US2 tocam `components/aulas/AulaForm.tsx` no mesmo arquivo (campos diferentes) e por isso
têm uma dependência sequencial pontual ali, mas continuam independentemente testáveis conforme
spec.md.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Pode rodar em paralelo (arquivos diferentes, sem dependência)
- **[Story]**: US1 (datas dd/mm/aaaa), US2 (horários 24h), US3 (centralização/consistência)
- Caminhos de arquivo exatos em cada descrição

## Path Conventions

Projeto web existente (frontend Next.js + backend ASP.NET Core + MySQL). Esta feature toca
SOMENTE o frontend, dentro de `frontend/`.

---

## Phase 1: Setup

Não aplicável — projeto já inicializado, nenhuma dependência nova (ver research.md Decisão 1).
Prosseguir direto para a Fase 2 (Foundational): os novos componentes `DateInput`/`TimeInput`
bloqueiam parte do trabalho de US1 e US2, então entram como pré-requisito comum.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Criar os componentes compartilhados que US1 e US2 usam para substituir os inputs
nativos de data/hora nos formulários (FR-008).

**⚠️ CRITICAL**: Nenhuma task de substituição de input nativo (US1/US2) pode começar antes desta
fase estar completa.

- [X] T001 [P] Criar `frontend/components/shared/DateInput.tsx`: componente de texto mascarado
  que exibe/aceita sempre `dd/mm/aaaa`, com contrato de valor `value`/`onChange` em
  `yyyy-MM-dd` (mesmo formato que `<input type="date">` já produzia) para não exigir mudança em
  nenhum ponto de chamada além da troca do elemento. Parsing MUST ser manual
  (split/slice de dígitos), nunca `new Date(stringDataPura)` (ver research.md Decisão 4).
  Suportar as props já usadas hoje pelos inputs nativos: `value`, `onChange`, `required`, `id`.
- [X] T002 [P] Criar `frontend/components/shared/TimeInput.tsx`: componente de texto mascarado
  que exibe/aceita sempre `HH:mm` em 24h, com contrato de valor `value`/`onChange` em `HH:mm`
  (mesmo formato que `<input type="time">` já produzia). Mesma regra de parsing manual da
  Decisão 4 do research.md. Suportar `value`, `onChange`, `required`, `id`.
- [X] T003 Adicionar as classes CSS dos novos componentes em `frontend/styles/dashboard.css`,
  reaproveitando o padrão visual já usado pelos campos de formulário existentes (ex.: mesma
  altura/borda/foco de `.filter-input` e dos `<input>` de formulário). Depende de T001 e T002
  (a marcação final dos componentes precisa existir antes de estilizar).

**Checkpoint**: `DateInput` e `TimeInput` prontos e estilizados — US1 e US2 podem começar.

---

## Phase 3: User Story 1 - Ler qualquer data no formato brasileiro (Priority: P1) 🎯 MVP

**Goal**: Toda data exibida em qualquer tela, tabela, relatório ou formulário aparece em
dd/mm/aaaa; todo campo de formulário de data usa o novo `DateInput`, não o seletor nativo do
navegador.

**Independent Test**: Percorrer as telas do sistema que exibem ou coletam datas e confirmar
visualmente dd/mm/aaaa em todo lugar, com os formulários usando o novo componente (Cenários 1 e
3 do quickstart.md).

### Implementation for User Story 1

- [X] T004 [P] [US1] Em `frontend/app/(app)/dashboard/page.tsx:27`, trocar
  `dia.toLocaleDateString("pt-BR", { day: "2-digit", month: "2-digit" })` por uma chamada a
  `fmtData`/helper equivalente de `frontend/lib/format.ts`, para que não haja mais formatação de
  data fora do ponto central (FR-004). Resultado visual não muda (já é dd/mm hoje).
- [X] T005 [US1] Em `frontend/components/aulas/AulaForm.tsx:165`, substituir o
  `<input type="date">` do campo `dataInicio` pelo novo `DateInput` (FR-008).
- [X] T006 [P] [US1] Em `frontend/app/(app)/financeiro/page.tsx:66,67`, substituir os 2
  `<input type="date">` do filtro de período pelo novo `DateInput`.
- [X] T007 [P] [US1] Em `frontend/app/(app)/financeiro/contas-a-pagar/novo/page.tsx:133`,
  substituir o `<input type="date">` (data de vencimento) pelo novo `DateInput`.
- [X] T008 [P] [US1] Em `frontend/app/(app)/financeiro/contas-a-pagar/page.tsx:148,155`,
  substituir os 2 `<input type="date">` (filtro) pelo novo `DateInput`.
- [X] T009 [P] [US1] Em
  `frontend/app/(app)/financeiro/contas-a-pagar/[id]/editar/page.tsx:155`, substituir o
  `<input type="date">` (data de vencimento) pelo novo `DateInput`.
- [X] T010 [P] [US1] Em `frontend/app/(app)/financeiro/contas-a-receber/page.tsx:182,189`,
  substituir os 2 `<input type="date">` (filtro) pelo novo `DateInput`.
- [X] T011 [P] [US1] Em `frontend/app/(app)/financeiro/contas-a-receber/novo/page.tsx:187`,
  substituir o `<input type="date">` pelo novo `DateInput`.
- [X] T012 [P] [US1] Em
  `frontend/app/(app)/financeiro/contas-a-receber/[id]/editar/page.tsx:148`, substituir o
  `<input type="date">` pelo novo `DateInput`.
- [X] T013 [P] [US1] Em `frontend/app/(app)/lembretes/page.tsx:243,244`, substituir os 2
  `<input type="date">` (filtro) pelo novo `DateInput`.
- [X] T014 [P] [US1] Em `frontend/app/(app)/relatorios/financeiro/page.tsx:156,157`, substituir
  os 2 `<input type="date">` (filtro) pelo novo `DateInput`.
- [X] T015 [P] [US1] Em `frontend/app/(app)/relatorios/alunos/page.tsx:157,158`, substituir os 2
  `<input type="date">` (filtro) pelo novo `DateInput`.
- [X] T016 [P] [US1] Em `frontend/app/(app)/relatorios/pagamentos/page.tsx:136,141`, substituir
  os 2 `<input type="date">` (filtro) pelo novo `DateInput`.
- [X] T017 [P] [US1] Em `frontend/app/(app)/relatorios/turmas/page.tsx:112,113`, substituir os 2
  `<input type="date">` (filtro) pelo novo `DateInput`.

**Checkpoint**: User Story 1 completa e testável de forma independente — nenhuma data em
mm/dd/aaaa, nenhum `<input type="date">` nativo restante.

---

## Phase 4: User Story 2 - Ver qualquer horário em formato 24h (Priority: P1)

**Goal**: Todo horário exibido aparece em 24h; todo campo de formulário de horário usa o novo
`TimeInput`, não o seletor nativo do navegador.

**Independent Test**: Percorrer as telas com horário (Agenda, cards de aula, formulário de
Aula) e confirmar 24h em toda exibição e no formulário (Cenários 2 e 3 do quickstart.md).

### Implementation for User Story 2

- [X] T018 [US2] Em `frontend/components/aulas/AulaForm.tsx:171,177`, substituir os 2
  `<input type="time">` dos campos `horaInicio`/`horaFim` pelo novo `TimeInput` (FR-008). Depende
  de T005 (mesmo arquivo — o campo de data já deve estar trocado antes de mexer nos campos de
  horário, para evitar um diff intermediário confuso no mesmo arquivo).

**Checkpoint**: User Story 2 completa e testável de forma independente — nenhum horário em
AM/PM, nenhum `<input type="time">` nativo restante. Nenhum outro ponto de exibição de horário
precisou de mudança (research.md confirmou que todos já usam `fmtHora`/`fmtDataHora`).

---

## Phase 5: User Story 3 - Consistência garantida em telas novas (Priority: P2)

**Goal**: Confirmar que não sobrou nenhuma formatação de data/hora divergente do ponto central
(`lib/format.ts` + `DateInput`/`TimeInput`), de modo que uma tela nova, ao reaproveitá-los,
nasça correta por padrão.

**Independent Test**: Auditar o código alterado e confirmar que toda formatação/entrada de
data-hora passa pelos pontos centrais, sem lógica própria divergente (Cenário 6 do
quickstart.md).

### Implementation for User Story 3

- [X] T019 [US3] Rodar uma varredura (`grep`/busca) em `frontend/` por
  `toLocaleDateString|toLocaleTimeString|AM|PM` e por parsing manual de data/hora fora de
  `frontend/lib/format.ts`, `frontend/components/shared/DateInput.tsx` e
  `frontend/components/shared/TimeInput.tsx`. Depende de T004-T018 (fases 3 e 4) estarem
  completas. Confirmar que nenhuma ocorrência divergente restou; se restar, corrigi-la antes de
  prosseguir.

**Checkpoint**: As três user stories funcionam em conjunto — datas e horários padronizados, sem
regressão, com um único ponto de formatação/entrada confirmado por auditoria.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Verificação final cruzando as três user stories, conforme
[quickstart.md](./quickstart.md).

- [X] T020 [P] Rodar `npx tsc --noEmit` e o lint do frontend (`frontend/`) — sem erros.
- [ ] T021 Executar a validação completa do [quickstart.md](./quickstart.md) (Cenários 1 a 6):
  datas em dd/mm/aaaa, horários em 24h, novos componentes nos formulários, nenhum dado alterado,
  independência de idioma/SO do navegador (dois navegadores), e ausência de formatação
  divergente do ponto central (FR-001 a FR-008, SC-001 a SC-005).
- [X] T022 Confirmar, por `git diff --stat`, que somente arquivos dentro de `frontend/` foram
  alterados — nenhum arquivo de backend ou banco de dados tocado (ver plan.md Project
  Structure).

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: N/A.
- **Foundational (Phase 2)**: Sem dependências externas — pode começar imediatamente. BLOQUEIA
  todas as tasks de substituição de input nativo em US1 e US2.
- **User Story 1 (Phase 3)**: Depende da Fase 2 completa (T001-T003).
- **User Story 2 (Phase 4)**: Depende da Fase 2 completa (T001-T003) e de T005 (US1), por
  tocarem o mesmo arquivo `AulaForm.tsx`.
- **User Story 3 (Phase 5)**: Depende de todas as tasks de US1 e US2 (T004-T018) estarem
  completas — é uma auditoria do resultado combinado.
- **Polish (Phase 6)**: Depende das Fases 3, 4 e 5 completas.

### Dentro de cada User Story

- T004 e T006-T017 (US1) são independentes entre si (arquivos diferentes) — podem rodar em
  paralelo. T005 (US1, `AulaForm.tsx`) também é paralelizável com as demais tasks de US1, mas
  bloqueia T018 (US2, mesmo arquivo).
- T018 (US2) é a única task da fase, sem paralelização interna, e depende de T005.
- T019 (US3) é uma única task de auditoria, sem paralelização interna, e depende de todo o
  restante.

### Parallel Opportunities

- T001 e T002 (Foundational) podem rodar em paralelo (arquivos diferentes); T003 depende de
  ambos.
- T004, T006-T017 (US1) podem todas rodar em paralelo entre si — 13 arquivos distintos.
- T005 (US1) pode rodar em paralelo com as demais tasks de US1, mas T018 (US2) só pode começar
  depois de T005 terminar (mesmo arquivo).

---

## Parallel Example: User Story 1

```bash
# Após a Fase 2 (Foundational) completa, lançar em paralelo:
Task: "Fix dashboard/page.tsx:27 para usar fmtData"
Task: "Substituir date input em financeiro/page.tsx"
Task: "Substituir date input em relatorios/turmas/page.tsx"
# ...demais arquivos de T006-T017
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Completar Fase 2: Foundational (T001-T003) → componentes prontos
2. Completar Fase 3: User Story 1 (T004-T017) → datas padronizadas em toda tela e formulário →
   **entregável isoladamente como MVP**
3. Parar e validar Cenários 1 e 3 (parte de data) do quickstart.md

### Incremental Delivery

1. Foundational (T001-T003) → base pronta
2. User Story 1 (T004-T017) → datas padronizadas → MVP entregável
3. User Story 2 (T018) → horários padronizados → entregável isoladamente por cima do MVP
4. User Story 3 (T019) → auditoria de consistência confirmada
5. Polish (T020-T022) → verificação final cruzada

## Notes

- Tests: nenhuma automatizada nesta feature (ver seção "Tests" acima) — validação inteiramente
  manual via quickstart.md, mesma limitação pré-existente do projeto já registrada em specs
  anteriores (ex. specs/023, specs/024).
- Nenhuma task desta lista deve alterar o formato de valor trocado com o backend
  (`yyyy-MM-dd`/`HH:mm:ss`) nem duplicar validação de negócio nos novos componentes — eles
  MUST permanecer responsáveis apenas por formato de exibição/entrada (ver research.md Decisão 2
  e Constitution Check do plan.md, Princípio II).
