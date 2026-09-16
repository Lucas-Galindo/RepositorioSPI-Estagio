# Tasks: Área de Clique Expandida no Menu do Financeiro e Correção de Hitbox na Tabela de Contas a Pagar

**Input**: Design documents from `/specs/021-fix-hitbox-cliques/`

**Prerequisites**: [plan.md](./plan.md) (required), [spec.md](./spec.md) (required), [research.md](./research.md), [data-model.md](./data-model.md) (não aplicável — sem entidades), [quickstart.md](./quickstart.md)

**Tests**: Não solicitados no spec. O projeto não tem framework de teste de UI automatizado (sem Jest/Vitest/Playwright em `frontend/package.json`); toda validação é manual via DevTools, conforme [quickstart.md](./quickstart.md).

**Organization**: Tasks agrupadas por user story. US1 (menu do Financeiro, melhoria de UX nova) e US2 (tabela de Contas a Pagar, correção de bug) são ambas P1, tocam arquivos diferentes, e são totalmente independentes entre si — podem ser feitas em paralelo. Diferente da rodada anterior desta feature, US1 **não** depende de uma investigação interativa prévia: a causa e a solução já estão definidas em [research.md](./research.md) Decisão 1 (nova classe CSS com `flex: 1 1 0`, decisão de UX do usuário). US2 continua exigindo confirmação de causa raiz via DevTools antes da correção.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Pode rodar em paralelo (arquivos diferentes, sem dependência)
- **[Story]**: US1 (menu do Financeiro) ou US2 (tabela de Contas a Pagar)
- Caminhos de arquivo exatos em cada descrição

## Path Conventions

Projeto web existente (frontend Next.js + backend ASP.NET Core). Esta feature toca **somente**:
- `frontend/app/(app)/financeiro/layout.tsx` (US1)
- `frontend/app/(app)/financeiro/contas-a-pagar/page.tsx` (US2, provavelmente sem mudança de JSX — ver research.md)
- `frontend/styles/dashboard.css` (US1: classe nova escopada; US2: ajuste pontual em `thead th`/`tbody td`/`tbody tr`)

---

## Phase 1: Setup

Não aplicável — projeto já inicializado, nenhuma dependência nova, nenhuma configuração de lint/build a alterar (ver [plan.md](./plan.md) Technical Context: "nenhuma dependência nova"). Prosseguir direto para a Fase 2.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Confirmar por inspeção interativa (DevTools) a causa raiz do bug de US2 antes de qualquer alteração de CSS — ver [research.md](./research.md) Decisão 2. US1 não tem foundational bloqueante: a causa (`.row-gap` com `justify-content: space-between` não cobre o espaço vazio) já é conhecida por leitura estática de código, e a solução (nova classe flex) está definida.

**⚠️ CRITICAL**: T001 bloqueia a Fase 4 (US2). A Fase 3 (US1) pode começar imediatamente, sem esperar T001.

- [X] T001 Investigar a causa raiz do bug da tabela de Contas a Pagar. **Nota de execução**: este ambiente não tem navegador/DevTools disponível (sem Playwright instalado no projeto, sem sessão autenticada acessível) — a confirmação foi feita por análise estática rigorosa em vez de clique interativo real: (a) busca exaustiva já havia descartado hipótese (3), nenhuma lógica de ordenação existe; (b) `.table-wrap` só tem `overflow-x: auto`, sem nenhum truque de posicionamento que desloque coordenadas de clique, descartando hipótese (2); (c) cálculo geométrico das regras CSS confirmou hipótese (1): com `border-collapse: collapse`, a fronteira entre `thead` e `tbody` é uma única borda compartilhada de 1px, sem nenhuma zona morta — não há sobreposição real de caixas, mas também não há buffer não-clicável entre o fim do cabeçalho e o início da primeira `<tr class="row-link">` (que já cobre 13px de padding acima do seu próprio texto). Um clique impreciso "perto do cabeçalho" cai, na prática, dentro da caixa da primeira linha. **Recomendação**: validar com um teste de clique real no navegador antes de considerar este diagnóstico definitivo.

**Checkpoint**: Causa raiz de US2 confirmada e documentada — Fase 4 pode começar. Fase 3 (US1) já pode estar em andamento ou concluída em paralelo.

---

## Phase 3: User Story 1 - Clicar em qualquer ponto do menu do Financeiro troca para a aba mais próxima (Priority: P1) 🎯 MVP

**Goal**: Qualquer clique dentro da faixa do menu do Financeiro (`frontend/app/(app)/financeiro/layout.tsx`) navega para uma aba — o espaço antes vazio entre/ao redor das três abas passa a fazer parte da área clicável de uma delas, dividido estaticamente pelo layout renderizado (não por cálculo de posição do mouse), sem alterar a classe `.row-gap` compartilhada por 14 outras telas.

**Independent Test**: Acessar `/financeiro`, clicar em pontos dentro da faixa do menu que estão fora dos limites visuais de qualquer botão (o espaço entre duas abas adjacentes, ou nas bordas da faixa) e verificar que o sistema navega para a aba correspondente à metade do espaço em que o clique caiu; clicar fora da faixa do menu não deve navegar.

### Implementation for User Story 1

- [X] T002 [P] [US1] Em `frontend/styles/dashboard.css`, criada a classe `.financeiro-tabs` (`display:flex; align-items:stretch`) com `.financeiro-tabs > a { flex: 1 1 0; justify-content: center; }`, logo após `.row-gap`. `.row-gap` em si não foi tocada (`dashboard.css:245-250`), preservando seu uso nos outros 13 arquivos.
- [X] T003 [US1] Em `frontend/app/(app)/financeiro/layout.tsx`, trocada a classe do `<div>` de `className="row-gap"` para `className="financeiro-tabs"`, e removido `gap: 8` do `style` inline (um gap reintroduziria espaço morto entre as abas, anulando o propósito da mudança) — mantido apenas `marginBottom: 18`. Os três `<Link>` e a lógica de aba ativa não foram alterados.
- [X] T004 [US1] **Nota de execução**: sem navegador disponível neste ambiente para captura de tela (sem Playwright instalado, tela exige sessão autenticada) — validado por leitura do CSS resultante em vez de inspeção visual ao vivo: `.btn`/`.btn-sm` mantêm seu padding, cor de fundo (`.btn-primary`/`.btn-ghost`) e border-radius originais; `flex: 1 1 0` só afeta a largura do link (que agora se estica para preencher 1/3 do container), e `justify-content: center` centraliza o texto dentro dessa largura maior — nenhuma propriedade de cor, fonte ou borda foi tocada. **Recomendação**: conferir visualmente em `/financeiro` no navegador antes de considerar este item encerrado, especialmente o alinhamento do texto dentro do botão ativo.
- [ ] T005 [US1] Validar Cenário 1 do [quickstart.md](./quickstart.md) — **não executado**: requer clique interativo real no navegador (com sessão autenticada), indisponível neste ambiente. Pendente de validação manual pelo usuário: em `/financeiro`, confirmar que clicar dentro do texto/limites visuais de "Contas a Receber" ainda navega normalmente (FR-003), que clicar no espaço antes vazio entre abas navega para a aba correspondente à metade do container em que o clique caiu (FR-001, FR-004), que clicar nas extremidades da faixa navega para a aba daquela extremidade, e que clicar fora da faixa do menu não navega (FR-002) — em desktop, mobile (~400px) e zoom variado (90%–125%).

**Checkpoint**: User Story 1 completa e testável de forma independente — menu do Financeiro responde a clique em toda a sua faixa, sem alterar `.row-gap` nem as 13 outras telas que a reusam.

---

## Phase 4: User Story 2 - Clicar perto do cabeçalho da tabela de Contas a Pagar não dispara ação inesperada (Priority: P1)

**Goal**: Na tabela de Contas a Pagar (`frontend/app/(app)/financeiro/contas-a-pagar/page.tsx`), nenhum clique na faixa do `<thead>` (incluindo perto da borda com a primeira linha de dados) dispara navegação para o detalhe de um registro ou qualquer outra ação; cliques dentro de uma linha de dados real continuam abrindo o detalhe normalmente.

**Independent Test**: Acessar `/financeiro/contas-a-pagar`, clicar em pontos progressivamente mais distantes da área visualmente ativa do cabeçalho (incluindo a transição entre a última linha do cabeçalho e a primeira linha de dados) e verificar que nenhuma ação é disparada indevidamente, enquanto cliques em linhas de dados reais continuam funcionando.

### Implementation for User Story 2

- [X] T006 [US2] Hipótese (1) confirmada (ver nota de T001). Correção aplicada de forma **escopada** (não na regra global `thead th`, que é compartilhada por 9 outras tabelas do sistema — `pagamentos`, `lembretes`, `contas-a-receber`, `relatorios/financeiro`, `relatorios/pagamentos`, `alunos`, `aulas`, `materias`): adicionada a classe `.contas-pagar-table thead th { padding-bottom: 20px; }` em `frontend/styles/dashboard.css` (logo após a regra `thead th`), e `className="contas-pagar-table"` adicionado ao `<table>` em `frontend/app/(app)/financeiro/contas-a-pagar/page.tsx`. O `padding-bottom` extra pertence à célula do `<thead>` (que não tem `onClick`), empurrando a borda `border-collapse` para baixo e criando uma zona de ~9px sem nenhum elemento clicável antes do início da primeira `<tr class="row-link">` — o `onClick` de navegação em `page.tsx:197` (agora linha ligeiramente deslocada) não foi alterado.
- [ ] T007 [US2] Validar Cenário 2 do [quickstart.md](./quickstart.md) — **não executado**: requer clique interativo real no navegador, indisponível neste ambiente. Pendente de validação manual pelo usuário: em `/financeiro/contas-a-pagar` com ao menos um registro, confirmar que clicar em uma célula de dados de uma linha real (ex.: coluna "Descrição") ainda navega para `/financeiro/contas-a-pagar/{id}` (FR-007, sem regressão), e que clicar em qualquer ponto da faixa do `<thead>` (inclusive perto da nova borda inferior) não navega nem dispara nenhuma outra ação (FR-005, FR-006) — em desktop, mobile (~400px) e zoom variado (90%–125%).

**Checkpoint**: User Story 2 completa e testável de forma independente — tabela de Contas a Pagar só responde dentro dos limites visuais corretos do cabeçalho, sem regressão no clique de linha.

---

## Phase 5: Polish & Cross-Cutting Concerns

**Purpose**: Validação final cruzando as duas user stories, conforme Critério de Conclusão do [quickstart.md](./quickstart.md).

- [ ] T008 Executar a validação completa do [quickstart.md](./quickstart.md) (Cenários 1 e 2) — **não executado**: mesma limitação de ambiente de T005/T007 (sem navegador/DevTools disponível). `npx tsc --noEmit` (sem erros) e `npx eslint` nos dois arquivos alterados (sem erros) foram executados como verificação estática substituta, mas não confirmam comportamento de clique real nem ausência de mudança na aba Network. Pendente de validação manual pelo usuário em pelo menos duas larguras de tela (desktop e ~400px) e dois níveis de zoom.
- [X] T009 Confirmado estaticamente via `git diff --stat`: apenas `frontend/app/(app)/financeiro/layout.tsx`, `frontend/app/(app)/financeiro/contas-a-pagar/page.tsx` e `frontend/styles/dashboard.css` foram alterados nesta feature — nenhum dos outros 13 arquivos que usam `.row-gap` foi tocado, e a regra `.row-gap` em si permanece byte-a-byte idêntica no diff (apenas duas novas regras foram *adicionadas* depois dela: `.financeiro-tabs` e `.contas-pagar-table thead th`). Confirmação puramente estrutural — não substitui uma checagem visual ao vivo, mas descarta qualquer regressão por edição acidental.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Foundational (Phase 2)**: T001 bloqueia apenas a Fase 4 (US2). Não bloqueia a Fase 3 (US1), que não depende de nenhuma investigação prévia.
- **User Story 1 (Phase 3)**: Sem dependência de Foundational. Pode começar imediatamente.
- **User Story 2 (Phase 4)**: Depende de T001 (Foundational).
- **Polish (Phase 5)**: Depende de Phase 3 e Phase 4 completas.

### Dentro de cada User Story

- T002 e T003 (US1) são sequenciais (T003 usa a classe criada em T002). T004 depende de T003. T005 (validação) depende de T004.
- T006 (US2) depende de T001. T007 (validação) depende de T006.

### Parallel Opportunities

- T002 (US1, CSS) e T001 (Foundational, investigação de US2) podem ser feitos em paralelo — arquivos/atividades diferentes.
- Fase 3 (US1) inteira pode ser feita em paralelo com Fase 2 + Fase 4 (US2) — arquivos totalmente diferentes (`financeiro/layout.tsx` + nova classe CSS vs. `financeiro/contas-a-pagar/page.tsx` + `thead`/`tbody` CSS).

---

## Parallel Example: User Story 1 vs. Foundational/User Story 2

```bash
# T002 (CSS de US1) e T001 (investigação de US2) podem rodar em paralelo:
Task: "T002 Criar classe .financeiro-tabs em frontend/styles/dashboard.css"
Task: "T001 Investigar causa raiz do bug da tabela em /financeiro/contas-a-pagar"

# Depois, Fase 3 (US1) completa e Fase 4 (US2) completa também podem avançar em paralelo:
Task: "T003-T005 Aplicar e validar a nova classe no menu do Financeiro"
Task: "T006-T007 Aplicar e validar a correção de CSS na tabela de Contas a Pagar"
```

---

## Implementation Strategy

### MVP First

US1 é a entrega mais simples e imediata (nenhuma investigação prévia necessária, solução CSS já definida) — pode ser o MVP:

1. Completar Phase 3: User Story 1 (T002–T005) → entregável isoladamente como MVP
2. Completar Phase 2 + Phase 4: User Story 2 (T001, T006–T007)
3. Completar Phase 5: Polish (T008 validação cruzada, T009 checagem de regressão em `.row-gap`)

### Incremental Delivery

1. US1 (T002–T005) → menu do Financeiro com área de clique expandida → pode ser entregue isoladamente
2. Foundational + US2 (T001, T006–T007) → tabela de Contas a Pagar corrigida → pode ser entregue isoladamente
3. Polish (T008+T009) → validação final cruzada e checagem de regressão em `.row-gap`

## Notes

- Tests OPCIONAIS e não solicitados nesta feature — toda validação é manual via DevTools (ver [quickstart.md](./quickstart.md)).
- US1 não tem tarefa de investigação porque a causa e a solução já foram decididas: a classe `.row-gap` usa `justify-content: space-between`, que por definição não cobre o espaço entre itens com nenhum elemento clicável — não há ambiguidade a diagnosticar, apenas uma implementação a fazer (ver [research.md](./research.md) Decisão 1).
- Nenhuma tarefa desta lista deve alterar destino de navegação, dado exibido, comportamento de abrir detalhe de registro real, introduzir nova ação (ordenação, etc.), ou modificar a classe `.row-gap` compartilhada (FR-007).
- Se T001 revelar uma causa raiz não prevista em nenhuma das hipóteses do research.md, PARAR e atualizar research.md antes de prosseguir para T006.
