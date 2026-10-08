---

description: "Task list for Cards do Painel de Pacotes em Atenção"
---

# Tasks: Cards do Painel de Pacotes em Atenção

**Input**: Design documents from `specs/047-cards-pacotes-atencao/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, quickstart.md

**Tests**: não solicitados no spec (redesenho visual, sem lógica de negócio nova); o projeto não tem suíte de testes de frontend configurada. Toda validação é manual via `quickstart.md`.

**Organization**: tarefas agrupadas por user story (US1 P1 saldo em destaque; US2 P1 card de largura fixa com rolagem horizontal própria — **revisado em 2026-10-07**; US3 P2 ordenação/diferenciação de estado). As três stories compartilham a mesma reescrita de componente (`PacotesEmAtencaoPanel.tsx`) — por isso a implementação real vive na Fase Foundational (serve as três stories ao mesmo tempo, per Task Generation Rules), e as fases de US1/US2/US3 ficam com a validação específica de cada aspecto.

## Format: `[ID] [P?] [Story] Description`

- **(parte manual)**: tarefa que exige app rodando e inspeção visual real no navegador (incluindo redimensionamento/rolagem horizontal do painel para US2).

## Path Conventions

Esta feature toca `frontend/components/dashboard/PacotesEmAtencaoPanel.tsx` e `frontend/app/(app)/dashboard/page.tsx` — nenhum outro arquivo, nenhuma mudança de backend/banco (FR-010). `frontend/styles/dashboard.css` não tem mudança líquida no resultado final (ver T001/research.md R1).

---

## Phase 1: Setup

Não aplicável — nenhuma dependência nova, nenhuma configuração a alterar.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: a reescrita do componente — serve as três user stories ao mesmo tempo, porque todas dependem da mesma estrutura de card.

**⚠️ CRITICAL**: T001/T002 bloqueiam as Fases 3, 4 e 5 (US1/US2/US3).

- [X] T001 Em `frontend/components/dashboard/PacotesEmAtencaoPanel.tsx`, reescrever o retorno do `.map()` com cards de largura fixa (não grid responsivo — revisão de 2026-10-07): wrapper externo `<div style={{ overflowX: "auto" }}>` envolvendo `<div style={{ display: "flex", gap: 18 }}>`; cada item é `<Link href={`/alunos/${p.alunoId}`} key={p.vinculoId} className="card card-small" style={{ width: 200, flexShrink: 0, textDecoration: "none", color: "inherit" }}>` contendo, nesta ordem: `<div className="card-eyebrow">{p.alunoNome}</div>`, `<div className="big-label" style={{marginTop: 2}}>{p.contexto}</div>`, `<div className="big-value">{p.saldoAulas}</div>`, `<div className="big-label">{p.saldoAulas === 1 ? "aula restante" : "aulas restantes"}</div>`, e o mesmo `<span className={p.estado === "Esgotado" ? "badge badge-cancel" : "badge badge-pending"}><Icon name="warn" size={10} /> {p.estado === "Esgotado" ? "Esgotado" : "Atenção"}</span>` já existente hoje (sem alterar essa lógica). Manter o `EmptyState` existente inalterado quando `pacotes.length === 0`. Não adicionar `.sort()`/reordenação — renderizar na ordem recebida (FR-006, Princípio II). Se uma classe CSS nova (`.pacotes-atencao-grid`, de uma iteração anterior) ainda existir em `dashboard.css`, remover (sem uso, ver research.md R1)
- [X] T002 Em `frontend/app/(app)/dashboard/page.tsx`, no wrapper do painel (`<div style={{ marginTop: 18 }}>` que envolve o título "Pacotes em atenção" e o `PacotesEmAtencaoPanel`), adicionar `marginBottom: 24` (mesmo valor já usado por `.stat-row{margin-bottom:24px}` no restante da página) para separar visualmente o painel da "Agenda da semana" abaixo dele (depende de T001)

**Checkpoint**: card de largura fixa + espaçamento implementados — Fases 3, 4 e 5 (validação por aspecto) podem começar.

---

## Phase 3: User Story 1 - Ver o saldo de aulas em destaque em formato de card (Priority: P1) 🎯 MVP

**Goal**: cada pacote em atenção aparece como um card com o saldo de aulas em destaque visual grande, mantendo todas as informações já exibidas hoje, com a mesma referência visual do card "Alunos atendidos".

**Independent Test**: na Home, com pelo menos um aluno em "Atenção" e um em "Esgotado", confirmar que cada um aparece como um card com nome, contexto, saldo em destaque, e estado, visualmente equivalente ao card "Alunos atendidos".

- [ ] T003 (parte manual) [US1] Validar o §1 de [quickstart.md](./quickstart.md): cards em vez de linhas; saldo visivelmente maior que nome/contexto/estado; nenhuma informação da versão anterior (nome, turma/atendimento individual, saldo, estado) ausente; mesmo raio de borda/padding/sombra/hierarquia do card "Alunos atendidos" (FR-001 a FR-003, SC-001/SC-002)

**Checkpoint**: User Story 1 completa e testável de forma independente.

---

## Phase 4: User Story 2 - Cards de largura fixa, em fila horizontal rolável (Priority: P1) — revisado em 2026-10-07

**Goal**: cada card tem largura fixa e compacta (igual ao card "Alunos atendidos"), nunca esticado; vários cards ficam lado a lado em uma única fileira; quando não cabem todos, o painel (não a página) ganha rolagem horizontal própria.

**Independent Test**: com um único pacote, confirmar que o card é pequeno e alinhado à esquerda (não esticado). Com muitos pacotes, confirmar fileira horizontal com rolagem própria do painel, sem quebra de linha nem encolhimento dos cards.

- [ ] T004 (parte manual) [US2] Validar o §2 de [quickstart.md](./quickstart.md): 1 pacote → card pequeno, alinhado à esquerda, não esticado; vários pacotes que cabem → fileira única lado a lado; pacotes demais → rolagem horizontal própria do painel, cards não quebram linha nem encolhem; página em si nunca rola horizontalmente por causa do painel (FR-004/FR-005, SC-003)

**Checkpoint**: User Stories 1 e 2 completas — cards com destaque visual e layout de fila fixa funcionando.

---

## Phase 5: User Story 3 - Esgotados continuam aparecendo primeiro, com diferença clara (Priority: P2)

**Goal**: a ordenação (Esgotado antes de Atenção) e a diferenciação por texto do estado continuam funcionando no novo layout de card, sem nenhum recálculo no frontend.

**Independent Test**: com pacotes "Esgotado" e "Atenção" misturados, confirmar que a ordem e a diferenciação por rótulo de texto (não só cor) se mantêm.

- [X] T005 [US3] Verificação de código (FR-006, regra do `CLAUDE.md` para requisitos "MUST NOT"): ler diretamente o diff de `PacotesEmAtencaoPanel.tsx` e confirmar que entre o recebimento de `pacotes` (prop) e o `.map()` que renderiza os cards não foi introduzido nenhum `.sort()`, `.reverse()`, `.filter()`, agrupamento, ou qualquer outra lógica que reordene/recalcule o que já vem do backend — confirmado: o diff mostra só mudanças de JSX/classe/estilo, nenhuma lógica nova
- [ ] T006 (parte manual) [US3] Validar o §3 de [quickstart.md](./quickstart.md): todos os cards "Esgotado" aparecem antes de todos os "Atenção" (ordem vinda do backend); a diferença entre os dois estados é identificável pelo rótulo de texto do badge, mesmo ignorando a cor (FR-006/FR-007, SC-004)

**Checkpoint**: todas as três user stories validadas.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: não-regressão, espaçamento, e confirmação final de escopo.

- [ ] T007 (parte manual) Validar o §4 de [quickstart.md](./quickstart.md) (espaçamento): distância do painel até o título acima inalterada; distância até a "Agenda da semana" abaixo visivelmente coerente com o resto da página (nem colada, nem desproporcional)
- [ ] T008 (parte manual) Validar o §5 de [quickstart.md](./quickstart.md) (não-regressão): clique em qualquer card continua navegando para `/alunos/{id}`; estado vazio inalterado; nenhuma mudança de aparência em outras telas que usam `card-small`/`big-value`/`big-label`/`badge-cancel`/`badge-pending` (KPIs da Home, Relatórios, Turmas) (FR-010/FR-011, SC-005)
- [X] T009 Rodar `git diff --stat` e confirmar que só `frontend/components/dashboard/PacotesEmAtencaoPanel.tsx` e `frontend/app/(app)/dashboard/page.tsx` foram alterados, e que `git diff -- frontend/styles/dashboard.css` fica **vazio** (nenhuma mudança líquida em CSS — confirma FR-008/FR-009: nenhuma cor ou classe nova) (FR-008/FR-009/FR-010, SC-005)
- [X] T010 Regra do `CLAUDE.md` para FR-009 ("MUST NOT introduzir componente novo"): rodar `git status --porcelain -- frontend/components frontend/styles frontend/app` e confirmar que não aparece nenhum arquivo não rastreado (nenhum componente/arquivo novo foi criado) — confirmado: só arquivos já rastreados (`M`), nenhum `??`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: não aplicável.
- **Foundational (Phase 2)**: T001 bloqueia T002; T001+T002 bloqueiam as Fases 3, 4 e 5.
- **User Stories (Phases 3-5)**: todas dependem só de T001+T002; são independentes entre si e podem ser validadas em paralelo. T005 (verificação de código) não depende de nada além de T001.
- **Polish (Phase 6)**: depende das Fases 3, 4 e 5 completas.

### Parallel Opportunities

- T003 (US1), T004 (US2) e T006 (US3) podem ser validadas em paralelo — mesma implementação já pronta (T001+T002), aspectos diferentes.
- T009/T010 (verificação de git) são independentes das validações manuais — podem ser feitas a qualquer momento depois de T001/T002.

---

## Implementation Strategy

### MVP First (User Story 1 + User Story 2)

1. Completar Phase 1 (Setup, N/A) e Phase 2 (T001+T002 — implementação única que cobre as três stories)
2. Completar Phase 3 (T003) e Phase 4 (T004) — destaque visual e layout de fila fixa, entregável como MVP
3. Phase 5 (T005+T006, ordenação/estado) e Phase 6 (Polish) são a entrega incremental seguinte — menor risco, comportamento já existente só sendo preservado

### Incremental Delivery

Foundational (T001+T002) → US1+US2 (T003+T004, MVP) → US3 (T005+T006) → Polish (T007-T010).

## Notes

- Tests opcionais e não solicitados nesta feature — toda validação é manual (ver `quickstart.md`).
- Nenhuma tarefa desta lista deve alterar a ordenação/cálculo de `estado` no frontend, introduzir cor nova, ou introduzir qualquer classe CSS nova (FR-006, FR-008, FR-009) — a revisão de 2026-10-07 eliminou até a única classe nova da primeira passada (`.pacotes-atencao-grid`), substituída por estilo inline de layout.
