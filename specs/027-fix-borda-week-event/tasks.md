# Tasks: Corrigir Borda Curva Sobrepondo Texto na Agenda Semanal da Home

**Input**: Design documents from `/specs/027-fix-borda-week-event/`

**Prerequisites**: [plan.md](./plan.md) (required), [spec.md](./spec.md) (required),
[research.md](./research.md), [quickstart.md](./quickstart.md)

**Tests**: Não solicitados. O frontend não tem framework de teste automatizado configurado (ver
plan.md Technical Context) — validação via [quickstart.md](./quickstart.md).

**Organization**: Correção isolada de 1 user story (US1), tocando uma única regra CSS em um
único arquivo. Não há Setup nem Foundational separados — a mudança em si já é a implementação
completa.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Pode rodar em paralelo (arquivos diferentes, sem dependência)
- **[Story]**: US1 (única user story desta correção)
- Caminhos de arquivo exatos em cada descrição

## Path Conventions

Projeto web existente (frontend Next.js). Esta correção toca `frontend/styles/dashboard.css` e,
para o ajuste de texto (T004), também `frontend/app/(app)/dashboard/page.tsx`.

---

## Phase 1: Setup

Não aplicável — nenhuma dependência nova, nenhum arquivo novo a criar.

---

## Phase 2: Foundational

Não aplicável — a única user story não depende de nenhuma infraestrutura compartilhada além do
próprio arquivo CSS já existente.

---

## Phase 3: User Story 1 - Ler o horário e a matéria de cada aula na agenda semanal da Home (Priority: P1) 🎯 MVP

**Goal**: A borda colorida da esquerda de cada card de evento da agenda semanal da Home passa a
ter a mesma curvatura sutil já usada por `.cal-event`, parando de sobrepor o texto do
horário/matéria.

**Independent Test**: Abrir a Home com um dia tendo múltiplas aulas cadastradas e confirmar
visualmente que a borda de cada card tem a mesma curvatura sutil de `.cal-event`, sem cobrir
texto, em nenhum dos 7 dias.

### Implementation for User Story 1

- [X] T001 [US1] Em `frontend/styles/dashboard.css`, na regra `.week-event`, trocar
  `border-radius: 11px` por `border-radius: 7px` e `padding: 8px 9px` por `padding: 4px 6px` —
  copiando fielmente os valores exatos já em uso em `.cal-event` para essas duas propriedades
  (`border-left: 3px solid var(--c-accent)` já era idêntico nos dois, não muda). Não adicionar
  `border-top-left-radius`/`border-bottom-left-radius` (a primeira iteração desta task fez isso
  e foi revertida — `.cal-event` não usa essa técnica, ver research.md Decisão revisada). Não
  alterar nenhuma outra propriedade da regra, nem `.week-event.st-realizada`/
  `.week-event.st-cancelada`, nem `.cal-event` (FR-001, FR-003, FR-004, FR-005, FR-006).

- [X] T004 [US1] Achado adicional durante validação: `.week-event` é renderizado como
  `<Link>` (âncora real), diferente de `.cal-event` que é um `<div onClick>` — por isso
  `.week-event` herdava o estilo padrão de link do navegador (azul, sublinhado), enquanto
  `.cal-event` nunca teve esse problema por não ser uma âncora. Em
  `frontend/styles/dashboard.css`, adicionar `color: inherit;` e `text-decoration: none;` à
  regra `.week-event`. Em `frontend/app/(app)/dashboard/page.tsx`, trocar
  `{fmtHora(a.horaInicio)} · {a.materiaNome}` por `{fmtHora(a.horaInicio)} {a.materiaNome}`
  (remover o separador `·`, deixando só espaço, igual ao formato "HH:mm Matéria" já usado em
  `.cal-event`). Não alterar o `href`/comportamento de navegação do `<Link>` (FR-007, FR-008).

- [X] T005 [US1] Achado adicional durante validação: a borda/fundo decorativos de `.week-event`
  apareciam numa faixa separada acima do texto, em vez de envolver o card inteiro como uma
  única caixa. Causa: `.week-event` (`<Link>`, renderiza como `<a>`) nunca teve `display`
  definido (herda `display: inline` do navegador) enquanto contém dois filhos de bloco (`<div
  className="t">`, `<div className="s">`) — cenário clássico de "block-in-inline" que pode
  fragmentar a caixa da âncora. Confirmado que `.lesson-item` (linhas 901-911), usado em outras
  telas para o mesmo padrão `<Link>` + `<div>`s filhos, já resolve isso com `display: flex`. Em
  `frontend/styles/dashboard.css`, adicionar `display: block;` à regra `.week-event` (bloco
  simples, não flex, já que `.t`/`.s` devem empilhar verticalmente, diferente do layout
  horizontal de `.lesson-item`) — ver research.md Decisão (FR-009).

**Checkpoint**: User Story 1 completa — borda, cor de texto, formato do texto e layout de
`.week-event` agora espelham fielmente `.cal-event`.

---

## Phase 4: Polish & Cross-Cutting Concerns

**Purpose**: Verificação final conforme [quickstart.md](./quickstart.md).

- [X] T002 [P] Confirmar, por `git diff`, que a única mudança é o ajuste de valor de
  `border-radius` e `padding`, mais a adição de `display`/`color`/`text-decoration`, em
  `.week-event` em `frontend/styles/dashboard.css`, e a remoção do separador em
  `frontend/app/(app)/dashboard/page.tsx` — nenhuma outra regra CSS, componente React ou arquivo
  foi tocado (FR-003, SC-003).
- [ ] T003 Executar a validação completa do [quickstart.md](./quickstart.md) (Cenários 1 a 7):
  texto legível em todos os cards da semana, borda com curvatura sutil (não reta, não exagerada)
  em cards de alturas diferentes, cores de status preservadas, texto sem estilo de link
  (cor neutra, sem sublinhado) e sem separador entre hora e matéria, borda e texto sempre na
  mesma caixa visual (mesmo em cards estreitos), nenhuma mudança na tela de Agenda
  (`.cal-event`), e arredondamento dos demais cantos preservado.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: N/A.
- **Foundational (Phase 2)**: N/A.
- **User Story 1 (Phase 3)**: Sem dependências — pode começar imediatamente. T004 e T005 foram
  adicionadas após validação visual de T001 apontar problemas distintos (T004: estilo de
  link/formato de texto; T005: borda separada do texto por falta de `display`), nenhuma depende
  tecnicamente de T001, mas foram implementadas depois por ordem de descoberta.
- **Polish (Phase 4)**: Depende de T001, T004 e T005 completas.

### Parallel Opportunities

- T001, T004 e T005 tocam a mesma regra `.week-event` em `dashboard.css` (T004 também toca
  `page.tsx`) — nesta execução foram feitas em sequência, por ordem de descoberta do problema.

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Completar T001 → correção aplicada → **entregável isoladamente como MVP** (é a totalidade da
   mudança)
2. Validar com T002-T003

## Notes

- Tests: nenhuma automatizada nesta feature — validação inteiramente manual via quickstart.md,
  mesma limitação pré-existente do projeto já registrada em specs anteriores.
- Nenhuma task desta lista deve tocar `.cal-event` — ele já está correto e fora do escopo desta
  correção (ver spec.md FR-003).
