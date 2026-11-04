# Tasks: Calendário nos campos de data

**Input**: `specs/069-calendario-campos-de-data/` (spec.md, plan.md, research.md, contracts/date-input.md, quickstart.md)
**Prerequisites**: nenhum. **Somente frontend**: sem backend, sem banco, sem migração.

**Formato**: `- [ ] TNNN [P] [USn] descrição com caminho`. `[P]` = pode rodar em paralelo. Regras do `CLAUDE.md`: cada "MUST NOT" tem tarefa de verificação (`git status --porcelain` para "sem arquivo novo"; **leitura do diff** para "sem conversão de data").

**Testes**: não há framework de testes de interface; a validação é `tsc`, `eslint`, contraste, build e Chrome automatizado (como nas specs 063 a 068). Se `src/` ou `tests/` forem tocados por engano, `dotnet test tests/SPI.Application.Tests` (sem `--no-build`) deve continuar verde.

**Segurança**: sem dados de teste (nada é gravado); a senha de teste só por variável de ambiente; nada em produção; para o `next build`, parar a API e usar `experimental.cpus: 2` temporário, restaurando `next.config.ts`.

## Phase 1: Linha de base

- [X] T001 Anotar a linha de base: `git status --porcelain` limpo; em `frontend/` `npx tsc --noEmit`, `npx eslint` (projeto inteiro: 0 problemas) e `npm run verificar-contraste` (0 pares abaixo de 4,5:1); `grep -rn 'type="date"\|type="month"' frontend/app frontend/components` (esperado: 5 de data em `TurmaForm.tsx` e `VinculoCobrancaFormModal.tsx` e 4 de mês nas telas de conta); contar os usos de `<DateInput` (esperado: 23 em 14 arquivos).

## Phase 2: User Story 1 — Abrir o calendário dentro do campo (P1) 🎯 MVP

**Meta**: todo `DateInput` tem o botão dentro do campo, que abre o calendário do navegador e devolve a data em `yyyy-MM-dd`.
**Teste independente**: na tela de Contas a Receber, abrir o calendário do filtro "a partir de", escolher um dia e conferir campo e lista.

- [X] T002 [US1] `frontend/components/shared/DateInput.tsx` (contrato em `contracts/date-input.md`): envolver o campo num `span.date-wrap` (com `style` opcional); manter o `<input type="text">` mascarado **sem mudar** `value`/`onChange`/`className`; acrescentar `ariaLabel` opcional (vai como `aria-label` do campo de texto) e `min` opcional (vai só para o auxiliar); acrescentar o `<button type="button" className="date-cal-btn" aria-label="Abrir calendário" disabled={disabled}>` com o `Icon name="cal"`; acrescentar o auxiliar `<input type="date" className="date-nativo" tabIndex={-1} aria-hidden="true" disabled={disabled}>` com `ref`, cujo `value` é o `value` do componente **só** quando é uma data completa `yyyy-MM-dd` (senão vazio) e cujo `onChange` chama `onChange(e.target.value)` quando não vazio. O clique do botão chama `auxiliar.showPicker()` dentro de `try/catch` e, se faltar o método ou ele lançar erro, `auxiliar.focus()` (nunca propaga erro). Comentar o porquê (o auxiliar só serve para abrir o calendário nativo; o texto mascarado continua sendo o campo da usuária). **Nenhum `new Date(`, `Date.parse` ou `toISOString`.**
- [X] T003 [P] [US1] `frontend/styles/dashboard.css`: `.date-wrap` (`position: relative; display: inline-block; max-width: 100%`), `.field .date-wrap` (`display: block`) e `.field .date-wrap .date-input { width: 100% }` (o `.field` estica os filhos diretos), recuo à direita do campo para o botão (`.date-wrap .date-input`), `.date-cal-btn` (borda direita, centralizado na vertical, sem fundo, `color: var(--c-text-muted)`, `:hover` e `:focus-visible` com `var(--c-accent-text)`, anel de foco visível, `:disabled` esmaecido e sem cursor de clique) e `.date-wrap .date-nativo` (seletor mais específico que `.field input`; `position: absolute; left: 0; bottom: 0; width: 100%; height: 0; opacity: 0; pointer-events: none; border: 0; padding: 0`). **Só tokens de cor, nenhuma cor literal.**
- [X] T004 [US1] Verificar no Chrome numa tela de filtro (Contas a Receber): botão presente nos dois campos "de/até", clicar abre sem erro de console, escolher um dia (disparando o `change` do auxiliar) preenche `dd/mm/aaaa` e a requisição enviada é a mesma de digitar a mesma data; abrir com campo vazio, parcial e inválido sem erro e sem alterar o texto; Tab até o botão e Enter/Espaço; campo desabilitado (editar aula já realizada) com botão desabilitado; sem rolagem horizontal em 360 px.

**Checkpoint**: US1 funcional nos 23 usos; `tsc` e `eslint` verdes.

## Phase 3: User Story 2 — O botão e o calendário acompanham o tema (P1)

- [X] T005 [US2] `frontend/styles/dashboard.css`: `color-scheme: light` em `.date-nativo` e em `input[type="month"]`, e `color-scheme: dark` para os mesmos seletores sob `.dark-mode` (**só** esses controles, nunca na página inteira); comentar o porquê (sem isso o ícone e o seletor nativos ficam escuros sobre fundo escuro).
- [X] T006 [US2] Rodar `npm run verificar-contraste` (0 pares abaixo de 4,5:1; os tokens `--c-text-muted` e `--c-accent-text` do botão já entram nas 48 combinações) e conferir no Chrome, em claro e escuro: cor calculada do ícone com contraste ≥ 4,5:1 contra o fundo do campo (cálculo da razão com os valores calculados), `color-scheme` calculado do auxiliar e dos campos de mês (`light`/`dark`), a cor do ícone mudando na hora ao alternar o tema, e o ícone nativo do campo de competência claro no escuro. Capturas das telas em claro e escuro, olhadas.

**Checkpoint**: US2 funcional.

## Phase 4: User Story 3 — Todos os campos de data iguais (P2)

- [X] T007 [P] [US3] `frontend/components/turmas/TurmaForm.tsx`: trocar os 2 `<input type="date">` (início e fim da turma de pacote) por `<DateInput>` (`value`/`onChange` em `yyyy-MM-dd`; `required={!editando}` e `disabled={inicioTravado}` como já eram; **no fim da turma, `min={dataInicio || undefined}` como já era**), mantendo rótulos, dicas e a regra de data de início imutável; importar o componente.
- [X] T008 [P] [US3] `frontend/components/alunos/VinculoCobrancaFormModal.tsx`: trocar os 3 `<input type="date">` (início do plano, 1º vencimento e a data de cada parcela) por `<DateInput>`; nas parcelas usar `ariaLabel` (`Vencimento da parcela N`) e `style` (`flex: 1; minWidth: 140`) no lugar do `aria-label` e do `style` do campo antigo; manter `regerarParcelas` e as chamadas dos `onChange` (o valor continua `yyyy-MM-dd`).
- [X] T009 [US3] Conferir no Chrome, em claro e escuro e em 360 px: "Nova turma de pacote", "Novo vínculo" Semestral/Anual (com as parcelas) e "Nova conta" a receber/pagar e "Editar conta": formato `dd/mm/aaaa`, botão e cores iguais aos filtros, datas salvas iguais às de antes (comparar a requisição de criar turma/vínculo com a de antes da mudança), "Fim da turma" com início escolhido (o calendário aberto recebe o `min` do auxiliar; conferir o atributo `min` do auxiliar e que digitar um fim anterior ao início é recusado pelo servidor com a mensagem de sempre), campos de competência com o tema escuro, sem rolagem horizontal.

**Checkpoint**: US3 funcional; as três histórias entregues.

## Phase 5: Acabamento e validação final

- [X] T010 [P] Notas de Princípio V: em `specs/062-semestralidade-anuidade/spec.md` e `specs/063-pacote-fora-da-turma/spec.md` acrescentar uma nota curta apontando para `specs/069-calendario-campos-de-data/spec.md` (os campos de data desses formulários passaram a usar o campo compartilhado com calendário, sem mudar as datas). Conferir por `git diff` que só entraram as notas.
- [X] T011 **Verificação dos "MUST NOT" FR-003, FR-010, FR-011 e FR-012**: (a) ler o `git diff` de `DateInput.tsx` e confirmar que não entrou `new Date(`, `Date.parse` nem `toISOString` (`grep -nE "new Date\(|Date\.parse|toISOString" frontend/components/shared/DateInput.tsx` sem resultado); (b) `git status --porcelain -- src tests database` e `git diff --stat -- src tests database` vazios; (c) `git status --porcelain -- frontend` **sem nenhum arquivo novo** (`??`); (d) `git diff -U0 -- frontend/styles frontend/components | grep -E "^\+" | grep -nE "#[0-9a-fA-F]{3,6}|rgb\("` sem resultado; (e) `grep -rn 'type="date"' frontend/app frontend/components` mostra **somente** o auxiliar dentro de `DateInput.tsx`.
- [X] T012 Varredura final no Chrome (claro e escuro; 360 px): todas as telas com `<DateInput` (Home, Financeiro, Contas a Pagar e a Receber — lista, novo e editar —, Lembretes, os 4 Relatórios, Aulas — nova e editar) mostram o botão em **todos** os campos (contagem de `.date-cal-btn` por tela igual à de campos de data), sem erro de console e sem rolagem horizontal; comparar a requisição de um filtro digitado com a do mesmo filtro escolhido pelo calendário.
- [X] T013 Rodar `cd frontend`: `npx tsc --noEmit`, `npx eslint` (projeto inteiro, 0 problemas), `npm run verificar-contraste` e `npm run build` (com a API **parada**, `experimental.cpus: 2` temporário e `NODE_OPTIONS=--max-old-space-size=1536`, restaurando `next.config.ts`). Confirmar `git status --porcelain` só com os arquivos previstos.
- [X] T014 Fechamento: `docs/relatorios/<data>-calendario-campos-de-data.md` (pedido, o que foi feito, validação, o que ficou de fora e o limite de que o calendário aberto não sai em captura); hook `revisao-codigo` do Trello (com a API parada) e comentário no card; parar os servidores iniciados por mim; atualizar a memória do projeto; **não** commitar nem fazer push sem pedido.

## Dependências e ordem

- Fase 1 → US1 (T002 e T003 em paralelo, depois T004) → US2 (T005 depende do CSS de T003; T006 depois) → US3 (T007 e T008 em paralelo depois de T002) → Fase 5.
- T010 é independente. T011 a T014 são o fechamento, nessa ordem.

## Paralelismo

T002 com T003; T007 com T008; T010 em qualquer momento.

## Estratégia de implementação

1. **MVP**: Fase 1 + US1: já entrega o botão de calendário em todos os filtros e formulários que usam o `DateInput`.
2. US2 acerta as cores (o outro pedaço do card) e US3 padroniza os formulários que ainda tinham campo nativo.
3. Acabamento e validação final só com as três histórias verdes.

## Cobertura dos requisitos

| FR | Tarefas | FR | Tarefas |
|---|---|---|---|
| FR-001 | T002, T003, T004, T012 | FR-007 | T007, T008, T009 |
| FR-002 | T002, T004 | FR-008 | T005, T006, T009 |
| FR-003 | T002, T011 | FR-009 | T003, T004, T009, T012 |
| FR-004 | T002, T003, T004 | FR-010 | T011, T012 |
| FR-005 | T003, T006, T013 | FR-011 | T003, T011 |
| FR-006 | T005, T006 | FR-012 | T007, T008, T011 | FR-013 | T002, T007, T009 |

| SC | Tarefas |
|---|---|
| SC-001 (100% dos campos com botão; 0 `type="date"` fora do componente) | T011, T012 |
| SC-002 (no máximo 2 cliques) | T004, T012 |
| SC-003 (0 pares abaixo de 4,5:1) | T006, T013 |
| SC-004 (mesmo valor, mesma requisição) | T004, T009, T012 |
| SC-005 (0 rolagens horizontais novas em 360 px) | T004, T009, T012 |
