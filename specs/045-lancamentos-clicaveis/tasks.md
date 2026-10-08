---

description: "Task list for Lançamentos Clicáveis na Tabela de Indicadores"
---

# Tasks: Lançamentos Clicáveis na Tabela de Indicadores

**Input**: Design documents from `specs/045-lancamentos-clicaveis/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, quickstart.md

**Tests**: não solicitados no spec (feature puramente de navegação/apresentação, sem regra de negócio nova); o projeto não tem suíte de testes de frontend configurada (mesma decisão já aceita nas specs 020-044). Toda validação é manual via `quickstart.md`.

**Organization**: tarefas agrupadas por user story (US1 P1 clique em entrada; US2 P1 clique em saída; US3 P3 preservação de filtro ao voltar). US1 e US2 compartilham literalmente a mesma edição de código (um único `onClick` com `if/else` por `tipo`, ver `research.md` R2) — por isso a implementação em si vive na Fase Foundational (serve as duas stories ao mesmo tempo, per Task Generation Rules), e as fases de US1/US2 ficam só com a validação específica de cada tipo.

## Format: `[ID] [P?] [Story] Description`

- **(parte manual)**: tarefa que exige app rodando e clique real no navegador — mesma convenção usada em specs anteriores (ex.: 041, 043, 044).

## Path Conventions

Projeto web existente (frontend Next.js + backend ASP.NET Core). Esta feature toca **somente** `frontend/app/(app)/relatorios/financeiro/page.tsx` — nenhum outro arquivo, nenhuma mudança de backend/banco (FR-006).

---

## Phase 1: Setup

Não aplicável — projeto já inicializado, nenhuma dependência nova (ver `plan.md` Technical Context: "Nenhuma nova — reaproveita `useRouter`"). Prosseguir direto para a Fase 2.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: a edição de código que implementa a navegação ao clicar — serve tanto US1 (entrada) quanto US2 (saída) na mesma mudança, porque ambas são ramos do mesmo `onClick` numa única linha de tabela (não há como implementar uma sem a outra de forma independente sem duplicar o bloco `<tr>`).

**⚠️ CRITICAL**: T001 bloqueia as Fases 3 e 4 (US1/US2). Não bloqueia a Fase 5 (US3), que só depende do comportamento de navegação já existente hoje (ver `research.md` R1) e pode ser validada em paralelo assim que T001 estiver pronta (a validação de US3 também clica numa linha, então na prática espera T001).

- [X] T001 Em `frontend/app/(app)/relatorios/financeiro/page.tsx`, no bloco da tabela de lançamentos (hoje linhas ~437-442: `{lancamentosFiltrados.map((lancamento) => (<tr key={...}>...)}`): (a) importar `useRouter` de `next/navigation` e instanciar `const router = useRouter()` no topo do componente, junto das demais hooks já existentes; (b) adicionar `className="row-link"` ao `<tr>`; (c) adicionar `onClick={() => router.push(lancamento.tipo === "Entrada" ? \`/financeiro/contas-a-receber/${lancamento.id}\` : \`/financeiro/contas-a-pagar/${lancamento.id}\`)}`; (d) não alterar nenhum outro conteúdo da linha (`<td>{lancamento.descricao}</td>`/`<td>{fmtData(lancamento.dataVencimento)}</td>` permanecem idênticos, FR-007) nem nenhuma outra parte do arquivo (filtros Entradas/Saídas, KPIs, fluxo de caixa — FR-008)

**Checkpoint**: navegação implementada para os dois tipos de lançamento — Fases 3 e 4 (validação por tipo) podem começar.

---

## Phase 3: User Story 1 - Clicar numa linha de entrada abre o detalhe em Contas a Receber (Priority: P1) 🎯 MVP

**Goal**: qualquer clique numa linha de entrada da tabela de lançamentos navega para `/financeiro/contas-a-receber/{id}` do pagamento correspondente, com sinal visual de clicável.

**Independent Test**: com uma entrada visível na tabela, passar o mouse sobre a linha (cursor de ponteiro + hover) e clicar — confirma que abre o detalhe do mesmo pagamento em Contas a Receber.

- [ ] T002 (parte manual) [US1] Validar o §1 de [quickstart.md](./quickstart.md): hover mostra cursor de ponteiro + destaque visual na linha de entrada (FR-005); clique navega para `/financeiro/contas-a-receber/{id}` mostrando o mesmo pagamento (nome/valor/data coincidem com a linha clicada) (FR-002, SC-001, SC-003)

**Checkpoint**: User Story 1 completa e testável de forma independente.

---

## Phase 4: User Story 2 - Clicar numa linha de saída abre o detalhe em Contas a Pagar (Priority: P1)

**Goal**: qualquer clique numa linha de saída da tabela de lançamentos navega para `/financeiro/contas-a-pagar/{id}` da conta correspondente, com o mesmo sinal visual de clicável da US1.

**Independent Test**: com uma saída visível na tabela, passar o mouse sobre a linha e clicar — confirma que abre o detalhe da mesma conta em Contas a Pagar.

- [ ] T003 (parte manual) [US2] Validar o §2 de [quickstart.md](./quickstart.md): clique numa linha de saída navega para `/financeiro/contas-a-pagar/{id}` mostrando a mesma conta (FR-003, SC-002); hover já confirmado igual na T002 (mesma classe `row-link` em ambos os tipos)

**Checkpoint**: User Stories 1 e 2 completas — navegação funcionando para os dois tipos de lançamento.

---

## Phase 5: User Story 3 - Preservação de filtros ao voltar (Priority: P3)

**Goal**: documentar, com um teste real, se os filtros de período/turma/matéria/aluno da aba Indicadores continuam aplicados depois que a professora clica num lançamento e volta pelo navegador — qualquer resultado (preservado ou resetado) é aceito pela spec (FR-009), mas precisa ser um fato observado, não uma suposição.

**Independent Test**: aplicar um filtro de período (e turma/aluno, se houver dados), clicar num lançamento, voltar pelo botão/gesto do navegador, e observar se o filtro continua aplicado.

- [ ] T004 (parte manual) [US3] Validar o §3 de [quickstart.md](./quickstart.md): aplicar filtro de período (+ turma/aluno se aplicável), clicar num lançamento, voltar pelo navegador, e registrar o resultado observado. **Em seguida**, atualizar `research.md` R1 acrescentando uma linha "Resultado observado na implementação: preservado" ou "Resultado observado na implementação: resetado (limitação aceita conforme FR-009)", conforme o que foi visto de fato (FR-009)

**Checkpoint**: todas as 3 user stories validadas; comportamento de FR-009 documentado como fato, não como suposição.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: não-regressão e confirmação final de escopo.

- [ ] T005 (parte manual) Validar o §4 de [quickstart.md](./quickstart.md) (não-regressão): botões "Entradas"/"Saídas" continuam filtrando normalmente e não são interceptados pelo clique da linha; cálculo/ordenação da tabela inalterados; KPIs/fluxo de caixa/prazo médio de atraso inalterados (FR-007, FR-008, SC-004)
- [X] T006 Rodar `git diff --stat -- src frontend/lib database` e confirmar saída vazia — nenhuma mudança fora de `frontend/app/(app)/relatorios/financeiro/page.tsx` (FR-006, SC-005). **Além disso** (FR-004 — garante que nenhuma tela/painel/modal novo foi criado): rodar `git status --porcelain -- frontend/app` e confirmar que não aparece nenhum arquivo não rastreado; e rodar `git diff --stat -- frontend/app` e confirmar que aparece exatamente uma linha, referente a `relatorios/financeiro/page.tsx`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: não aplicável.
- **Foundational (Phase 2)**: T001 bloqueia as Fases 3 e 4. A Fase 5 (US3) também depende de T001 na prática (precisa de uma linha clicável para testar o "voltar"), mesmo não estando listada como bloqueio formal de US1/US2.
- **User Story 1 (Phase 3)** e **User Story 2 (Phase 4)**: dependem só de T001; são independentes entre si e podem ser validadas em paralelo.
- **User Story 3 (Phase 5)**: depende de T001 (precisa clicar numa linha para testar o retorno).
- **Polish (Phase 6)**: depende das Fases 3, 4 e 5 completas.

### Parallel Opportunities

- T002 (US1) e T003 (US2) podem ser validadas em paralelo — tipos de lançamento diferentes, mesma implementação já pronta (T001).
- T004 (US3) pode ser feita em paralelo com T002/T003 (mesma implementação, foco de verificação diferente).
- T006 (git diff) pode ser feito a qualquer momento depois de T001, em paralelo com qualquer validação manual.

---

## Implementation Strategy

### MVP First (User Story 1 + User Story 2)

1. Completar Phase 1 (Setup, não aplicável) e Phase 2 (T001 — implementação única que cobre os dois tipos)
2. Completar Phase 3 (T002) e Phase 4 (T003) — navegação funcionando para entrada e saída, entregável como MVP
3. Phase 5 (T004, preservação de filtro) e Phase 6 (Polish) são a entrega incremental seguinte — menor prioridade, sem risco de bloquear o MVP

### Incremental Delivery

Foundational (T001) → US1+US2 (T002+T003, MVP) → US3 (T004, documenta FR-009) → Polish (T005+T006).

## Notes

- Tests opcionais e não solicitados nesta feature — toda validação é manual (ver `quickstart.md`).
- Nenhuma tarefa desta lista deve alterar o cálculo da tabela, os filtros Entradas/Saídas, os demais indicadores da aba, ou introduzir qualquer tela/painel/modal novo (FR-004, FR-007, FR-008).
- Se T004 revelar que os filtros **não** são preservados ao voltar, isso não é um defeito a corrigir nesta feature — é o resultado de fallback já previsto e aceito por FR-009; só precisa ser documentado em `research.md`, não implica nenhuma tarefa adicional de código.
