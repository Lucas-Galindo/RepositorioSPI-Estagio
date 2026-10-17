---

description: "Task list for Modo de Cores Personalizado"
---

# Tasks: Modo de Cores Personalizado

**Input**: Design documents from `specs/051-cores-personalizadas/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, quickstart.md

**Tests**: não solicitados no spec (preferência de UI local, sem lógica de negócio); o projeto não tem suíte de testes de frontend configurada. Toda validação é manual via `quickstart.md`.

**Organization**: tarefas agrupadas por user story (US1 P1 ativar o modo e escolher as 4 cores; US2 P2 restaurar as cores ao padrão). Os campos/tipos novos em `ThemeContext.tsx` são compartilhados pelas duas stories e vivem na Fase Foundational.

## Format: `[ID] [P?] [Story] Description`

Nenhuma tarefa `(parte manual)` que exija credenciais reais ou app rodando além do próprio frontend — mas a validação visual (T006-T009) ainda exige o app rodando no navegador, por isso seguem marcadas como manuais.

## Path Conventions

Esta feature toca **somente** `frontend/contexts/ThemeContext.tsx`, `frontend/components/layout/AppShell.tsx`, e `frontend/app/(app)/configuracoes/page.tsx` — nenhum outro arquivo, nenhuma mudança de backend/banco.

---

## Phase 1: Setup

Não aplicável — nenhuma dependência nova, nenhuma configuração a alterar.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: o tipo/estado novo em `ThemeContext.tsx` — bloqueia as duas user stories, já que ambas dependem dos 4 campos novos e do novo valor de `colorMode`.

**⚠️ CRITICAL**: T001 bloqueia as Fases 3 e 4 (US1/US2).

- [X] T001 Em `frontend/contexts/ThemeContext.tsx`: (a) estender o tipo `ColorMode` para incluir `"personalizado"`; (b) adicionar `customAccent`, `customAccent2`, `customAccent3`, `customDanger` (todos `string`, hex) à interface `Preferencias`, com valores padrão `"#4ecdc4"`, `"#bb9457"`, `"#ffe66d"`, `"#c0392b"` respectivamente (mesmos valores de `COLOR_MODES[0].swatches` já usados em `configuracoes/page.tsx` e do `:root` de `tokens.css`) no objeto `PADRAO`; (c) adicionar setters `setCustomAccent`, `setCustomAccent2`, `setCustomAccent3`, `setCustomDanger` (cada um chamando `persistir({...prefs, campo: valor})`, mesmo padrão dos setters já existentes) e `resetCustomColors()` (chama `persistir` com os 4 campos de volta aos valores padrão, sem tocar em `colorMode`); (d) expor os 4 campos + os 5 novos métodos em `ThemeContextValue` e no `value` do `useMemo`

**Checkpoint**: tipo/estado prontos — Fases 3 e 4 podem começar.

---

## Phase 3: User Story 1 - Ativar o modo Personalizado e escolher as 4 cores (Priority: P1) 🎯 MVP

**Goal**: o usuário vê "Personalizado" como uma 5ª opção de modo de cores; ao ativá-la, vê 4 seletores de cor (em vez de 1); qualquer cor escolhida se reflete em toda a interface imediatamente e persiste entre sessões.

**Independent Test**: ativar o modo Personalizado, escolher as 4 cores, confirmar que a interface reflete as mudanças e que elas persistem ao navegar/recarregar.

- [X] T002 [US1] Em `frontend/components/layout/AppShell.tsx`, estender a lógica do `style` condicional: quando `colorMode === "personalizado"`, injetar `--c-accent`/`--c-accent-deep`/`--c-accent-tint` (a partir de `customAccent`, usando `darkenHex(customAccent, 0.22)` e `hexToRgba(customAccent, 0.15)` — mesmos helpers e mesmos fatores já usados para `accentColor`), `--c-accent2`/`--c-accent2-tint` (de `customAccent2`, `hexToRgba(customAccent2, 0.15)`), `--c-accent3`/`--c-accent3-tint` (de `customAccent3`, `hexToRgba(customAccent3, 0.35)`), `--c-danger`/`--c-danger-tint` (de `customDanger`, `hexToRgba(customDanger, 0.12)`) — valores de alpha copiados exatamente dos mesmos usados hoje no `:root` de `tokens.css` para cada variável (depende de T001)
- [X] T003 [US1] Em `frontend/app/(app)/configuracoes/page.tsx`, adicionar a 5ª entrada ao grid "Modo de cores": card `"personalizado"` com rótulo "Personalizado", descrição curta (ex.: "Escolha livremente as 4 cores da interface."), e `swatches` calculadas a partir de `customAccent`/`customAccent2`/`customAccent3`/`customDanger` do `useTheme()` (não um array estático, já que precisam refletir a escolha atual do usuário) (depende de T001)
- [X] T004 [US1] No mesmo arquivo, no bloco "Cor de destaque": quando `colorMode === "personalizado"`, renderizar 4 `<input type="color">` (um por campo — `customAccent`, `customAccent2`, `customAccent3`, `customDanger` — reaproveitando a estrutura `accent-picker-row`/`accent-swatch-label` já existente, repetida 4 vezes, cada um com seu próprio rótulo identificando qual papel ele tem na interface, ex.: "Destaque principal"/"Destaque secundário"/"Destaque terciário"/"Alerta/perigo"); quando `colorMode === "normal"`, manter o bloco atual de 1 seletor inalterado; quando for um dos 3 modos de daltonismo, manter o comportamento atual (bloco desabilitado) (depende de T001, T003)
- [X] T005 [US1] No mesmo arquivo, garantir que trocar `colorMode` para `"personalizado"` (clique no novo card) só chama `setColorMode("personalizado")` — nenhuma chamada a `resetCustomColors()` nem a nenhum setter de cor nesse clique (FR-007: trocar de modo não deve apagar personalização; os valores já persistidos continuam os mesmos, sejam eles o padrão inicial ou uma escolha anterior do usuário) (depende de T001, T003)
- [X] T006 (parte manual) [US1] Validar o §1 de [quickstart.md](./quickstart.md): 5 cards no grid; ativar Personalizado mostra 4 seletores com os valores padrão; escolher cores diferentes reflete na interface imediatamente, inclusive fora da tela de Configurações (FR-001 a FR-004, SC-001/SC-002) — validado pelo usuário
- [X] T007 (parte manual) [US1] Validar o §3 de [quickstart.md](./quickstart.md): trocar para Padrão/daltonismo e voltar para Personalizado preserva as 4 cores escolhidas (FR-007, SC-003) — validado pelo usuário
- [X] T008 (parte manual) [US1] Validar o §4 de [quickstart.md](./quickstart.md): recarregar a página (F5) ou reabrir o navegador preserva o modo Personalizado e as 4 cores (FR-006) — validado pelo usuário

**Checkpoint**: User Story 1 completa e testável de forma independente — já entrega o valor central do pedido.

---

## Phase 4: User Story 2 - Restaurar as cores personalizadas para o padrão (Priority: P2)

**Goal**: um botão "Restaurar padrão", visível quando o modo Personalizado está ativo, devolve as 4 cores aos valores originais numa única ação, sem trocar de modo.

**Independent Test**: com cores customizadas escolhidas, clicar em "Restaurar padrão" e confirmar que as 4 voltam ao original e o modo continua "Personalizado".

- [X] T009 [US2] Em `frontend/app/(app)/configuracoes/page.tsx`, no bloco de 4 seletores (T004), adicionar um botão "Restaurar padrão" (mesma classe `btn btn-ghost btn-sm` já usada pelo botão equivalente do modo "Padrão") que chama `resetCustomColors()` e mostra um toast de confirmação (mesmo padrão do botão já existente) — visível só quando `colorMode === "personalizado"` (depende de T001, T004)
- [X] T010 (parte manual) [US2] Validar o §2 de [quickstart.md](./quickstart.md): com cores alteradas, "Restaurar padrão" devolve as 4 cores ao original numa única ação, sem trocar o modo ativo (FR-005, SC-004) — validado pelo usuário

**Checkpoint**: ambas as user stories completas e testáveis de forma independente.

---

## Phase 5: Polish & Cross-Cutting Concerns

**Purpose**: não-regressão e verificação dos requisitos "MUST NOT" (regra do `CLAUDE.md`).

- [X] T011 (parte manual) Validar o §5 de [quickstart.md](./quickstart.md) (não-regressão): nenhuma chamada de rede ao trocar cor/modo; combinação de cores ruim não é bloqueada; os 3 modos de daltonismo continuam com paletas fixas inalteradas (FR-008/FR-009, SC-002) — validado pelo usuário, feature confirmada como resolvida
- [X] T012 Regra do `CLAUDE.md` para FR-008 ("MUST NOT exigir mudança de backend/API") e para "não criar arquivo além do necessário": rodar `git status --porcelain -- frontend` e confirmar que não aparece nenhum arquivo não rastreado; rodar `git diff --stat` e confirmar que só os 3 arquivos esperados (`ThemeContext.tsx`, `AppShell.tsx`, `configuracoes/page.tsx`) foram alterados
- [X] T013 Regra do `CLAUDE.md` para FR-009 ("MUST NOT validar/restringir cores") e Princípio III ("nenhuma opção sem implementação real"): ler diretamente o diff dos 3 arquivos e confirmar que (a) nenhuma lógica de validação de contraste/cor foi adicionada aos `<input type="color">`; (b) o card "Personalizado" realmente aplica as 4 cores via `AppShell.tsx` (não é uma opção visual sem efeito)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: não aplicável.
- **Foundational (Phase 2)**: T001 bloqueia as Fases 3 e 4.
- **User Story 1 (Phase 3)**: T002 e T003 podem ser feitos em paralelo (arquivos diferentes); T004/T005 dependem de T003 (mesmo arquivo, mesmo bloco).
- **User Story 2 (Phase 4)**: depende de T004 (o botão vive dentro do bloco que T004 cria).
- **Polish (Phase 5)**: depende das Fases 3 e 4 completas.

### Parallel Opportunities

- T002 (`AppShell.tsx`) e T003 (`configuracoes/page.tsx`, parte do grid) podem ser feitos em paralelo — arquivos diferentes.
- T006/T007/T008 (US1) podem ser validados em paralelo entre si depois da implementação pronta.
- T012/T013 (verificação de git/diff) são independentes das validações manuais.

---

## Implementation Strategy

### MVP First (User Story 1)

1. Completar Phase 1 (N/A) e Phase 2 (T001)
2. Completar Phase 3 (T002-T008) — modo Personalizado funcionando de ponta a ponta, entregável como MVP
3. Phase 4 (US2, restaurar padrão) e Phase 5 (Polish) são a entrega incremental seguinte

### Incremental Delivery

Foundational (T001) → US1 (T002-T008, MVP) → US2 (T009-T010) → Polish (T011-T013).

## Notes

- Nenhuma tarefa desta lista deve introduzir validação de contraste/cor, nenhuma chamada de API, ou nenhum arquivo novo — tudo vive dentro dos 3 arquivos já existentes (FR-008/FR-009).
