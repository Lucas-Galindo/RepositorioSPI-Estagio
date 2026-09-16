---

description: "Task list template for feature implementation"
---

# Tasks: Ajustes de Layout — Turmas e Pagamentos

**Input**: Design documents from `/specs/020-ajustes-layout-turmas-pagamentos/`

**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md), [data-model.md](./data-model.md) (N/A), [quickstart.md](./quickstart.md)

**Tests**: Not requested. O spec não pede testes automatizados e o projeto não tem framework de teste de UI configurado (`frontend/package.json` sem Jest/Vitest/Playwright) — a validação é o roteiro manual em `quickstart.md`, referenciado na Polish phase.

**Organization**: Tasks agrupadas por user story (US1, US2, US3, conforme `spec.md`), em ordem de prioridade (P1 primeiro).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Pode rodar em paralelo (arquivo diferente, sem dependência de outra task pendente)
- **[Story]**: A qual user story do `spec.md` a task pertence (US1, US2, US3)
- Caminhos de arquivo exatos incluídos em cada descrição

## Path Conventions

Esta é uma aplicação web (frontend Next.js + backend ASP.NET Core). Esta feature toca **somente** `frontend/`, sem nenhuma alteração em `src/` (backend). Caminhos abaixo são relativos à raiz do repositório.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Confirmar o ambiente de desenvolvimento antes de iniciar os ajustes.

- [X] T001 Rodar `npm run dev` em `frontend/` e confirmar que `/turmas`, `/turmas/{id}` (com uma turma existente) e `/pagamentos` carregam sem erro no console — este é o baseline "antes" para comparar com o resultado de cada user story

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Infraestrutura compartilhada que bloquearia as user stories.

**Não aplicável a esta feature.** As três user stories editam três arquivos `.tsx` totalmente distintos, sem nenhuma dependência de código, componente ou infraestrutura nova compartilhada entre elas (confirmado em `plan.md` → Project Structure). Não há tarefas foundational — a implementação pode começar diretamente pela Phase 3.

**Checkpoint**: Nenhum — prossiga direto para as user stories.

---

## Phase 3: User Story 2 - Espaçamento select de aluno / botão "Vincular" (Priority: P1) 🎯 MVP

**Goal**: Aumentar o espaço entre o campo "Selecione um aluno..." e o botão "Vincular" na tela de detalhe de Turma, reduzindo cliques acidentais (FR-002).

**Independent Test**: Abrir `/turmas/{id}` de uma turma com ao menos um aluno disponível para vincular; confirmar visualmente que o espaço entre o `<select>` e o botão "Vincular" é perceptivelmente maior que antes; selecionar um aluno e clicar em "Vincular" e confirmar que o vínculo é criado normalmente.

### Implementation for User Story 2

- [X] T002 [P] [US2] Em `frontend/app/(app)/turmas/[id]/page.tsx:160`, aumentar o valor de `gap` no `style={{ marginTop: 14, gap: 8 }}` do `<div className="field-row">` que envolve o `<select>` (linha 161) e o `<button>` "Vincular" (linha 174) — de `8` para um valor perceptivelmente maior (referência de `research.md` Decisão 2: `16`–`20`), mantendo `marginTop: 14` inalterado
- [ ] T003 [US2] Validar manualmente: com um aluno selecionado no `<select>`, clicar em "Vincular" e confirmar que o aluno passa a aparecer na lista de vinculados (regressão de comportamento, FR-004) — depende de T002

**Checkpoint**: A tela de detalhe de Turma tem o espaçamento corrigido e o vínculo de aluno continua funcionando normalmente.

---

## Phase 4: User Story 1 - Estado vazio de Turmas centralizado (Priority: P2)

**Goal**: Centralizar a mensagem "Nenhuma turma encontrada" em relação à largura total da área de conteúdo (FR-001).

**Independent Test**: Acessar `/turmas` sem nenhuma turma cadastrada (ou com um filtro de busca sem correspondência); confirmar visualmente que a mensagem de estado vazio aparece centralizada na largura total do conteúdo, em vez de deslocada à esquerda.

### Implementation for User Story 1

- [X] T004 [P] [US1] Em `frontend/app/(app)/turmas/page.tsx` (~linha 47-51), fazer o `<EmptyState title="Nenhuma turma encontrada" .../>` ocupar a largura total do `<div className="report-grid">` quando `total === 0` — aplicando `style={{ gridColumn: "1 / -1" }}` ao `EmptyState` (ou ao wrapper direto dele) dentro do grid, conforme `research.md` Decisão 1; não alterar `.report-grid` em `frontend/styles/dashboard.css` (evitar regressão em outras telas que reusam essa classe, como `/relatorios/turmas`)
- [ ] T005 [US1] Validar manualmente em largura desktop e em largura mobile (~400px) que a mensagem permanece centralizada em ambas (Edge Case do spec) — depende de T004
- [ ] T006 [P] [US1] Validar manualmente que, com ao menos uma turma cadastrada, a grade de cartões de turma continua sendo renderizada normalmente em 3 colunas, sem nenhuma mudança de comportamento (regressão de FR-004) — depende de T004

**Checkpoint**: A aba Turmas exibe o estado vazio corretamente centralizado, sem regressão na grade de cartões quando há turmas.

---

## Phase 5: User Story 3 - Seção "Métodos de pagamento" acima da tabela de pagamentos (Priority: P2)

**Goal**: Reordenar a seção "Métodos de pagamento" para cima da tabela de registros de pagamento na aba Pagamentos (FR-003).

**Independent Test**: Acessar `/pagamentos` e rolar a página de cima para baixo; confirmar que a ordem passa a ser filtros → seção "Métodos de pagamento" → tabela de registros de pagamento; confirmar que o filtro "Forma — todas" continua funcionando normalmente.

### Implementation for User Story 3

- [X] T007 [P] [US3] Em `frontend/app/(app)/pagamentos/page.tsx`, mover o bloco `<div className="panel" style={{ marginTop: 18 }}>` da seção "Métodos de pagamento" (atualmente linhas ~194-219, contendo título, subtítulo e a tabela de formas cadastradas) para **antes** do bloco `<div className="table-wrap">` da tabela de registros de pagamento (atualmente linha ~145) — posicionando-o logo após o `row-gap` do botão "Registrar pagamento" (linhas ~139-143) e mantendo o `filter-bar` (linhas ~101-137, incluindo o filtro "Forma — todas") inalterado em sua posição atual, conforme `research.md` Decisão 3
- [X] T008 [US3] Ajustar os estilos inline de espaçamento (`marginTop`/equivalente) do bloco movido e do `table-wrap` adjacente, se necessário, para manter um espaçamento vertical visualmente consistente com o resto da página após a reordenação — depende de T007
- [ ] T009 [P] [US3] Validar manualmente que o filtro "Forma — todas" na barra de filtros continua restringindo corretamente a tabela de registros de pagamento, e que os dados exibidos em ambas as tabelas (pagamentos e formas de pagamento) são idênticos aos de antes da mudança (FR-004) — depende de T007

**Checkpoint**: A aba Pagamentos exibe a seção "Métodos de pagamento" acima da tabela de registros, sem nenhuma mudança de dado ou comportamento de filtro.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Validação final cruzando as três user stories.

- [X] T010 [P] Rodar `npm run lint` em `frontend/` e confirmar que nenhum novo erro de lint foi introduzido pelas alterações de T002, T004 e T007 (os três arquivos tocados)
- [ ] T011 Executar o roteiro completo de `specs/020-ajustes-layout-turmas-pagamentos/quickstart.md` (Cenários 1, 2 e 3) e confirmar que todos passam, incluindo a checagem de que nenhuma chamada de API mudou (aba Network do navegador) — depende de T003, T005, T006, T009

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: Sem dependências — pode começar imediatamente
- **Foundational (Phase 2)**: Não aplicável — nenhuma task bloqueante
- **User Stories (Phase 3-5)**: Todas podem começar assim que o Setup (Phase 1) terminar; são totalmente independentes entre si (arquivos distintos), podendo ser feitas em paralelo ou na ordem de prioridade (US2 → US1 → US3)
- **Polish (Phase 6)**: Depende da conclusão de todas as três user stories

### User Story Dependencies

- **User Story 2 (P1)**: Sem dependência de US1 ou US3
- **User Story 1 (P2)**: Sem dependência de US2 ou US3
- **User Story 3 (P2)**: Sem dependência de US1 ou US2

### Within Each User Story

- Ajuste de código antes da validação manual correspondente (ex.: T002 antes de T003; T004 antes de T005/T006; T007 antes de T008/T009)

### Parallel Opportunities

- T002 (US2), T004 (US1) e T007 (US3) podem ser feitas em paralelo por serem arquivos diferentes sem dependência entre si
- T010 (lint) pode rodar em paralelo com a validação manual de qualquer story, desde que após T002/T004/T007 estarem aplicadas

---

## Parallel Example: As três user stories

```bash
# As três correções de arquivo podem ser feitas em paralelo, pois não compartilham arquivo nem estado:
Task: "T002 [US2] Aumentar gap em frontend/app/(app)/turmas/[id]/page.tsx:160"
Task: "T004 [US1] Corrigir centralização em frontend/app/(app)/turmas/page.tsx (~linha 51)"
Task: "T007 [US3] Mover seção Métodos de pagamento em frontend/app/(app)/pagamentos/page.tsx"
```

---

## Implementation Strategy

### MVP First (User Story 2 apenas)

1. Completar Phase 1: Setup
2. Completar Phase 3: User Story 2 (maior impacto prático — evita cliques acidentais)
3. **PARAR e VALIDAR**: testar US2 isoladamente (T003)
4. Entregar/demonstrar se pronto

### Incremental Delivery

1. Setup → ambiente confirmado
2. US2 (P1) → validar isoladamente → entregar (MVP)
3. US1 (P2) → validar isoladamente → entregar
4. US3 (P2) → validar isoladamente → entregar
5. Polish (lint + quickstart completo) → entrega final consolidada

### Parallel Team Strategy

Com mais de uma pessoa disponível, após o Setup (Phase 1):

- Pessoa A: User Story 2 (`turmas/[id]/page.tsx`)
- Pessoa B: User Story 1 (`turmas/page.tsx`)
- Pessoa C: User Story 3 (`pagamentos/page.tsx`)

Como os três arquivos são independentes, não há conflito de merge esperado entre as três stories.

---

## Notes

- **Status de implementação (2026-09-11)**: T001, T002, T004, T007, T008 e T010 concluídas e verificadas (typecheck limpo, lint sem novos erros nos 3 arquivos tocados, rotas `/turmas` e `/pagamentos` respondendo 200 antes e depois das mudanças, revisão de código confirmando a estrutura JSX esperada). T003, T005, T006, T009 e T011 exigem clique/inspeção visual em uma sessão de navegador autenticada (login de professora), que não foi possível executar neste ambiente de agente — ficam pendentes de validação manual por alguém com acesso ao app rodando no navegador, seguindo exatamente os passos descritos em cada task e em `quickstart.md`.
- Nenhuma task tem rótulo `[Story]` nas Phases 1, 2 e 6, conforme a convenção do formato.
- Nenhum teste automatizado foi incluído — não solicitado no spec e sem framework de teste de UI configurado no projeto; a validação de cada story é manual, e a Phase 6 fecha com o roteiro completo do `quickstart.md`.
- Evitar tocar em `frontend/styles/dashboard.css` (`.report-grid`, `.field-row`) de forma global — todas as correções foram desenhadas para serem localizadas ao componente/instância afetada, evitando regressão em outras telas que reusam essas classes (ver `research.md` para as alternativas rejeitadas).
