---

description: "Task list for Padronizar Botões da Zona de Risco"
---

# Tasks: Padronizar Botões da Zona de Risco

**Input**: Design documents from `specs/046-padronizar-botoes-admin/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md (N/A), quickstart.md

**Tests**: não solicitados no spec (troca pontual de classe CSS, sem lógica nova); o projeto não tem suíte de testes de frontend configurada. Toda validação é manual via `quickstart.md`.

**Organization**: feature com uma única user story (US1, P1) — não há US2/US3 no spec. Fases de Setup/Foundational não aplicáveis (nenhuma dependência nova, nenhum pré-requisito compartilhado).

## Format: `[ID] [P?] [Story] Description`

- **(parte manual)**: tarefa que exige app rodando e inspeção visual real no navegador.

## Path Conventions

Esta feature toca **somente** `frontend/components/admin/CadastrarProfessoraForm.tsx` — nenhum outro arquivo (FR-005).

---

## Phase 1: Setup

Não aplicável — nenhuma dependência nova, nenhuma configuração a alterar.

---

## Phase 2: Foundational

Não aplicável — não há pré-requisito compartilhado entre tarefas; a única mudança de código é uma única edição que já resolve toda a US1.

---

## Phase 3: User Story 1 - Botões "Cancelar" da Zona de Risco usam a cor padrão do sistema (Priority: P1) 🎯 MVP

**Goal**: os dois botões "Cancelar" da Zona de Risco passam a usar a classe secundária padrão (`btn-ghost`) já usada em outras confirmações do sistema, sem nenhuma mudança de comportamento.

**Independent Test**: abrir a tela de gerenciamento da professora, chegar a cada uma das duas etapas que têm botão "Cancelar" (aviso e código), e comparar visualmente com o botão "Cancelar" de uma confirmação de exclusão em outra tela (ex.: Aula).

- [X] T001 [US1] Em `frontend/components/admin/CadastrarProfessoraForm.tsx`, trocar `className="btn"` por `className="btn btn-ghost"` nos dois botões "Cancelar" (linha ~380, dentro do bloco `etapaExclusao === "aviso"`; linha ~409, dentro do bloco `etapaExclusao === "codigo"`) — **sem** adicionar `btn-sm` (ver `research.md` R1, consistência de tamanho com os demais botões da própria tela). Não alterar `onClick`, `disabled`, nem nenhum outro atributo desses dois botões. Não tocar nos três botões `btn-danger` ("Excluir Professor", "Entendi, enviar código de confirmação", "Confirmar exclusão") nem no botão `btn-primary` ("Reativar Professor") (FR-003, FR-004)
- [ ] T002 (parte manual) [US1] Validar o §1 de [quickstart.md](./quickstart.md): botão "Cancelar" da etapa de aviso com fundo neutro/sombra sutil (não a aparência padrão do navegador), e clique volta para a etapa inicial sem nenhuma requisição de rede (FR-001, SC-001, SC-002)
- [ ] T003 (parte manual) [US1] Validar o §2 de [quickstart.md](./quickstart.md): botão "Cancelar" da etapa de código com a mesma aparência da T002, e clique volta para a etapa inicial sem consumir o código pendente (FR-001, SC-001, SC-002)

**Checkpoint**: User Story 1 completa e testável de forma independente — único MVP desta feature.

---

## Phase 4: Polish & Cross-Cutting Concerns

**Purpose**: não-regressão e confirmação final de escopo.

- [ ] T004 (parte manual) Validar o §3 de [quickstart.md](./quickstart.md) (não-regressão): os três botões `btn-danger` e o botão `btn-primary` da mesma tela continuam com a aparência de antes; nenhuma outra tela que usa `btn-ghost`/`btn-danger`/`btn-primary` (Aula, Agenda, Alunos, etc.) muda de aparência (FR-003, SC-004)
- [X] T005 Rodar `git diff --stat` e confirmar que só `frontend/components/admin/CadastrarProfessoraForm.tsx` foi alterado, com nenhuma linha tocada em nenhum arquivo `.css` do projeto (FR-002, FR-005, SC-003)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup/Foundational**: não aplicável.
- **User Story 1 (Phase 3)**: sem dependências — pode começar imediatamente.
- **Polish (Phase 4)**: depende de T001 (precisa da edição aplicada para validar não-regressão e `git diff`).

### Parallel Opportunities

- T002 e T003 podem ser validadas em paralelo (etapas diferentes da mesma tela, mesma edição já aplicada em T001).
- T005 (git diff) é independente de T002/T003/T004 — pode ser feito a qualquer momento depois de T001.

---

## Implementation Strategy

### MVP First (única story)

1. T001 (edição) → T002 + T003 (validação dos dois "Cancelar" em paralelo) → MVP completo
2. T004 + T005 (Polish) — confirmação final de não-regressão e escopo

### Incremental Delivery

Esta feature é pequena o suficiente para ser entregue de uma vez: T001 → T002/T003 → T004/T005.

## Notes

- Tests opcionais e não solicitados — toda validação é manual (ver `quickstart.md`).
- Nenhuma tarefa desta lista deve alterar `.btn`, `.btn-ghost`, `.btn-danger`, `.btn-primary`, ou qualquer outro arquivo CSS do projeto (FR-002, FR-005) — só o atributo `className` dos 2 botões "Cancelar".
