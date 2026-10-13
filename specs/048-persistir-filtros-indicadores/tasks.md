---

description: "Task list for Persistir Filtros da Visão de Indicadores"
---

# Tasks: Persistir Filtros da Visão de Indicadores

**Input**: Design documents from `specs/048-persistir-filtros-indicadores/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, quickstart.md

**Tests**: não solicitados no spec (persistência de UI, sem regra de negócio nova); o projeto não tem suíte de testes de frontend configurada. Toda validação é manual via `quickstart.md`, complementada pelas verificações de código exigidas pelo `CLAUDE.md` (regra "MUST NOT").

**Organization**: tarefas agrupadas por user story (US1 P1 restaurar filtros ao voltar; US2 P2 limpar todos os filtros). As duas stories compartilham a mesma implementação (`relatorios/financeiro/page.tsx`) — por isso a implementação real vive na Fase Foundational, e as fases de US1/US2 ficam com a validação específica de cada aspecto.

## Format: `[ID] [P?] [Story] Description`

- **(parte manual)**: tarefa que exige app rodando e navegação real no navegador (incluindo fechar/reabrir o navegador para SC-005).

## Path Conventions

Esta feature toca **somente** `frontend/app/(app)/relatorios/financeiro/page.tsx` — nenhum outro arquivo, nenhuma mudança de backend/banco, nenhuma mudança nas telas de detalhe (FR-007/FR-009).

---

## Phase 1: Setup

Não aplicável — nenhuma dependência nova, nenhuma configuração a alterar.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: a implementação da persistência e da extensão do botão "Limpar filtros" — serve as duas user stories ao mesmo tempo.

**⚠️ CRITICAL**: T001-T003 bloqueiam as Fases 3 e 4 (US1/US2).

- [X] T001 Em `frontend/app/(app)/relatorios/financeiro/page.tsx`, adicionar `const CHAVE_FILTROS = "spi:filtros-relatorio-financeiro";` próximo ao topo do arquivo (fora do componente), e um `useEffect(() => {...}, [])` dentro do componente (logo após as declarações de `useState`, antes dos `useEffect` de busca de dados existentes) que: dentro de `try/catch`, lê `sessionStorage.getItem(CHAVE_FILTROS)`, faz `JSON.parse` se houver valor, e chama `setAba`/`setInicio`/`setFim`/`setFormaPagamentoId`/`setAlunoId`/`setTurmaId`/`setMateriaId`/`setSemestre`/`setTipoLancamento` com os 9 campos lidos (só os campos presentes no objeto salvo; nenhuma mudança se a chave não existir) — mesmo padrão de `ThemeContext.tsx` (ler só após o mount, nunca como inicializador do `useState`, comentário explicando o motivo, `// eslint-disable-next-line react-hooks/set-state-in-effect` onde necessário) (ver `research.md` R2)
- [X] T002 No mesmo arquivo, adicionar um segundo `useEffect(() => {...}, [aba, inicio, fim, formaPagamentoId, alunoId, turmaId, materiaId, semestre, tipoLancamento])` que, dentro de `try/catch`, serializa esses 9 valores num objeto e grava com `sessionStorage.setItem(CHAVE_FILTROS, JSON.stringify(...))` a cada mudança (depende de T001)
- [X] T003 No mesmo arquivo, estender `temFiltro` para `Boolean(inicio || fim || formaPagamentoId || alunoId || turmaId || materiaId || tipoLancamento !== "todos")` e estender `limparFiltros` para também chamar `setTipoLancamento("todos")` (ver `research.md` R4) — não alterar `aba` dentro de `limparFiltros` (FR da US2 é só sobre filtros, não sobre aba ativa)

**Checkpoint**: persistência e extensão do botão implementadas — Fases 3 e 4 (validação por aspecto) podem começar.

---

## Phase 3: User Story 1 - Filtros restaurados ao voltar do detalhe (Priority: P1) 🎯 MVP

**Goal**: ao voltar da tela de detalhe de Entradas/Saídas, todos os filtros (incluindo a aba ativa) continuam exatamente como estavam, e os dados exibidos correspondem a eles.

**Independent Test**: aplicar período + turma/aluno + filtro Entradas/Saídas na aba Indicadores, clicar num lançamento, voltar, e confirmar que tudo continua igual.

- [ ] T004 (parte manual) [US1] Validar o §1 de [quickstart.md](./quickstart.md): período, turma/aluno, filtro Entradas/Saídas e a aba "Visão de Indicadores" continuam restaurados ao voltar; os números exibidos correspondem aos filtros restaurados (FR-001 a FR-003, SC-001)
- [ ] T005 (parte manual) [US1] Validar o §3 de [quickstart.md](./quickstart.md): sem nenhum filtro aplicado, clicar num lançamento e voltar não introduz nenhuma mudança de comportamento em relação a antes desta feature

**Checkpoint**: User Story 1 completa e testável de forma independente.

---

## Phase 4: User Story 2 - Limpar todos os filtros de uma vez (Priority: P2)

**Goal**: o botão "Limpar filtros" já existente passa a cobrir também o filtro Entradas/Saídas, restaurando todos os filtros ao padrão numa única ação.

**Independent Test**: com qualquer combinação de filtros aplicados (incluindo só o filtro de tipo), clicar em "Limpar filtros" e confirmar que tudo volta ao padrão.

- [ ] T006 (parte manual) [US2] Validar o §2 de [quickstart.md](./quickstart.md): botão "Limpar filtros" visível/ativo com qualquer filtro aplicado (incluindo só Entradas/Saídas); ao clicar, todos voltam ao padrão e os dados recarregam sem filtro; o botão desaparece/fica inativo depois; o estado "limpo" permanece ao clicar num lançamento e voltar (FR-005/FR-006, SC-002)

**Checkpoint**: ambas as user stories completas e testáveis de forma independente.

---

## Phase 5: Polish & Cross-Cutting Concerns

**Purpose**: não-regressão e verificação dos requisitos "MUST NOT" (regra do `CLAUDE.md`).

- [ ] T007 (parte manual) Validar o §4 de [quickstart.md](./quickstart.md) (não-regressão visual): tela de detalhe de Entradas/Saídas inalterada (FR-007); fechar o navegador (ou abrir aba nova) e reabrir a Visão de Indicadores mostra os filtros no padrão, não os últimos usados (FR-004, SC-005)
- [X] T008 Regra do `CLAUDE.md` para FR-009 ("MUST NOT exigir mudança de backend/API") e para "não criar arquivo novo": rodar `git status --porcelain -- frontend` e confirmar que não aparece nenhum arquivo não rastreado; rodar `git diff --stat` e confirmar que só `frontend/app/(app)/relatorios/financeiro/page.tsx` foi alterado — nenhuma mudança em `src`/`database`/`contas-a-pagar`/`contas-a-receber` (FR-007/FR-009, SC-003)
- [X] T009 Regra do `CLAUDE.md` para FR-008 ("MUST NOT recalcular/duplicar lógica no frontend") e para FR-010 ("MUST NOT introduzir cor/componente novo"): ler diretamente o diff de `relatorios/financeiro/page.tsx` e confirmar que (a) os dois `useEffect` novos (T001/T002) só leem/escrevem `sessionStorage` e chamam `setState` já existentes — nenhuma chamada a `obterRelatorioFinanceiro`/`obterIndicadoresFinanceiros` nova, nenhum cálculo novo; (b) nenhuma classe CSS nova foi introduzida — o botão "Limpar filtros" continua sendo o mesmo `className="filter-clear"` já existente (SC-004)
- [X] T010 Regra do `CLAUDE.md` para FR-011 ("MUST NOT persistir de forma que afete ou seja afetada pelos filtros de nenhuma outra página"): rodar `grep -rn "spi:filtros-relatorio-financeiro" frontend/` e confirmar que a chave aparece **somente** em `relatorios/financeiro/page.tsx` (nas duas ocorrências esperadas, leitura e escrita) — nenhuma outra página/componente lê ou escreve essa chave, e nenhuma outra chave de `sessionStorage` compartilhada é introduzida

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: não aplicável.
- **Foundational (Phase 2)**: T001 bloqueia T002 (mesmo objeto/campos); T003 é independente de T001/T002 (toca só `temFiltro`/`limparFiltros`) mas listada na mesma fase por tocar o mesmo arquivo na mesma leva de implementação. T001-T003 bloqueiam as Fases 3 e 4.
- **User Stories (Phases 3-4)**: dependem só da Fase 2; são independentes entre si e podem ser validadas em paralelo.
- **Polish (Phase 5)**: depende das Fases 3 e 4 completas.

### Parallel Opportunities

- T004/T005 (US1) e T006 (US2) podem ser validadas em paralelo — mesma implementação já pronta (Fase 2), aspectos diferentes.
- T008/T009/T010 (verificação de git/diff) são independentes das validações manuais — podem ser feitas a qualquer momento depois da Fase 2.

---

## Implementation Strategy

### MVP First (User Story 1)

1. Completar Phase 1 (Setup, N/A) e Phase 2 (T001-T003)
2. Completar Phase 3 (T004+T005) — persistência funcionando, entregável como MVP
3. Phase 4 (T006, extensão do Limpar filtros) e Phase 5 (Polish) são a entrega incremental seguinte

### Incremental Delivery

Foundational (T001-T003) → US1 (T004+T005, MVP) → US2 (T006) → Polish (T007-T010).

## Notes

- Tests opcionais e não solicitados nesta feature — toda validação é manual (ver `quickstart.md`), complementada pela leitura de diff exigida pelo `CLAUDE.md` para os requisitos "MUST NOT" (T008/T009/T010).
- Nenhuma tarefa desta lista deve introduzir uma chamada de API nova, duplicar cálculo no frontend, criar arquivo/componente/classe CSS novo, ou alterar as telas de detalhe de Entradas/Saídas (FR-007 a FR-010).
