# Tasks: Calendário próprio no padrão do sistema

**Input**: `specs/070-calendario-proprio/` (spec.md, plan.md, research.md, contracts/calendario.md, quickstart.md)
**Prerequisites**: a spec 069 (botão dentro do campo) já está no código. **Somente frontend**: sem backend, sem banco, sem migração, sem dependência nova.

**Formato**: `- [ ] TNNN [P] [USn] descrição com caminho`. `[P]` = pode rodar em paralelo. Regras do `CLAUDE.md`: cada "MUST NOT" tem tarefa de verificação (`git status --porcelain` para "sem arquivo novo indevido"; **leitura do diff** para "sem conversão de data" e "contrato intacto").

**Testes**: a lógica pura tem verificação automatizada própria (T003); a interface é validada por `tsc`, `eslint`, contraste, build e Chrome automatizado **com capturas** (o calendário agora faz parte da página e aparece nelas). Se `src/` ou `tests/` forem tocados por engano, `dotnet test tests/SPI.Application.Tests` (sem `--no-build`) deve continuar verde.

**Segurança**: sem dados de teste (nada é gravado); senha só por variável de ambiente; nada em produção. Memória baixa: `NODE_OPTIONS=--max-old-space-size=1536` em `tsc`/`eslint`/`build`; para o `next build`, API parada e `experimental.cpus: 2` temporário (restaurar `next.config.ts`); saída dos roteiros longos gravada em arquivo.

## Phase 1: Linha de base

- [X] T001 Anotar a linha de base: `git status --porcelain` limpo; em `frontend/` `npx tsc --noEmit`, `npx eslint` (projeto inteiro: 0 problemas), `npm run verificar-contraste` (0 pares abaixo de 4,5:1); `grep -rn 'type="date"\|type="month"' app components` (esperado: o auxiliar em `DateInput.tsx` e 4 `type="month"` nas telas de conta); contar os usos de `<DateInput` (esperado: 23 em 14 arquivos); `git diff` de `package.json` vazio. Linha de base: git limpo (só os docs da 070), tsc limpo, eslint 0, contraste 48/0.

## Phase 2: Fundação — lógica pura (bloqueia as histórias)

- [X] T002 [P] Criar `frontend/lib/calendario.ts` conforme `contracts/calendario.md`: `ehBissexto`, `diasDoMes`, `diaDaSemana` (aritmética, sem `Date`), `montarGrade` (sempre 6 semanas × 7 dias, domingo primeiro, vizinhos com `doMes: false`), `adicionarMeses`, `adicionarDias`, `estaBloqueado`, `lerIso`, `lerMes`, `tituloDoMes`, `rotuloDoDia`, `anosDaPagina` e as constantes (`MESES`, `DIAS_ABREVIADOS`, `ANO_MIN = 1900`, `ANO_MAX = 2100`). Comentar o porquê de não usar `Date` (fuso/horário de verão). **Nenhum `new Date(`, `Date.parse`, `toISOString` ou `getTimezoneOffset`.**
- [X] T003 [P] Criar `frontend/scripts/verificar-calendario.mjs` e o script `"verificar-calendario"` no `package.json` (mesmo padrão do `verificar-contraste`, Node executa o `.ts`; **só o script entra no `package.json`, nenhuma dependência**) com os casos de borda: bissexto (1900 não, 2000 sim, 2024 sim, 2100 não); `diasDoMes` de todos os meses de um ano comum e de um bissexto; `diaDaSemana` de datas de referência (2000-01-01 sábado, 1900-01-01 segunda, 2026-11-05 quinta, 2024-02-29 quinta); `montarGrade` com 42 células, primeira célula domingo, mês que começa no domingo, no sábado e fevereiro de 28 e 29 dias, vizinhos marcados; `adicionarMeses` virando o ano nos dois sentidos; `adicionarDias` atravessando mês, ano e 29/02; `estaBloqueado` (igual ao mínimo não bloqueia; um dia antes bloqueia); `lerIso`/`lerMes` com formatos e limites inválidos (inclusive ano 1899 e 2101, que devolvem `null`); títulos e rótulos em português ("novembro de 2026", "5 de novembro de 2026"); `anosDaPagina` nos limites 1900 e 2100.
- [X] T004 Rodar `npm run verificar-calendario` até passar tudo (corrigir `lib/calendario.ts` se preciso) e `npx tsc --noEmit`.

**Checkpoint**: a lógica está provada antes de qualquer interface.

## Phase 3: User Story 1 — Escolher o dia num calendário do SPI (P1) 🎯 MVP

**Meta**: o botão do `DateInput` abre o calendário do sistema (visão de dias) e a escolha preenche o campo.
**Teste independente**: Contas a Receber → botão do filtro → escolher um dia (claro e escuro).

- [X] T005 [US1] Criar `frontend/components/shared/Calendario.tsx` (contrato em `contracts/calendario.md`), visão de **dias** primeiro: cabeçalho com setas de mês (ícone `back` existente; a seta de "próximo" é o mesmo ícone girado por CSS, sem ícone novo) e título, linha `D S T Q Q S S`, grade de 6 semanas, dia escolhido, hoje destacado (de `dataIsoLocal`), vizinhos apagados; renderizado por `createPortal` em `document.body`, `position: fixed`, camada 120, posicionado pelo retângulo da `ancora` (abaixo; acima se não couber; margem de 8 px; largura fixa), **recalculado em rolagem (captura) e redimensionamento mexendo direto no estilo do elemento (sem `setState` em efeito)**, fechando se o campo sair da tela; fecha com Esc, `pointerdown` fora e escolha. Abre no mês do `valor` (vazio/inválido: hoje). Só tokens de cor.
- [X] T006 [P] [US1] `frontend/styles/dashboard.css`: seção `/* ===== specs/070: calendario proprio ===== */` com `.cal-*` (painel `var(--c-surface)`/`var(--c-text)`, `var(--r-md)`, `var(--shadow-out)`; dia com `var(--r-sm)`; escolhido `var(--c-accent)` + `var(--c-on-accent)` + `var(--shadow-out-sm)`; hover/foco `var(--c-accent-tint)` + `var(--c-accent-text)`; hoje com anel de 2 px `var(--c-accent-text)`; vizinhos `var(--c-text-muted)`; bloqueados com opacidade e `cursor: not-allowed`; indicador de foco visível). **Nenhuma cor literal.**
- [X] T007 [US1] `frontend/components/shared/DateInput.tsx`: o botão passa a alternar o estado `aberto` e renderizar `<Calendario modo="dia" valor={...} min={min} ancora={invólucro} aoEscolher={onChange} aoLimpar={() => onChange("")} aoFechar={...} />`, devolvendo o foco ao botão ao fechar; o valor passado ao painel é a data do **texto exibido** (como na 069); **remover o `<input type="date">` auxiliar, o `showPicker()` e a `ref` dele**. Propriedades públicas e valor `yyyy-MM-dd` **inalterados**.
- [X] T008 [US1] Chrome (claro e escuro, **com capturas**): em Contas a Receber o botão abre o calendário do SPI (não o do navegador) com hoje destacado e semana no domingo; escolher um dia preenche `dd/mm/aaaa`, fecha e gera a mesma requisição que digitar; campo com data completa abre no mês dela; parcial/inválido abre em hoje sem mexer no texto; Esc, clique fora e clicar de novo no botão fecham sem alterar o campo. **Executado na página de teste temporária (`app/cal-teste`, já apagada), pois não há senha de teste nesta sessão; ver o relatório.**

**Checkpoint**: US1 funcional nos 23 usos; `tsc` e `eslint` verdes.

## Phase 4: User Story 2 — Navegar rápido, hoje e atalhos (P1)

- [X] T009 [US2] `Calendario.tsx`: visões de **meses** e **anos** (título do mês abre os meses; título do ano abre os anos em páginas de 12; escolher ano volta aos meses, escolher mês volta aos dias; setas de ano/página), faixa 1900 a 2100 (setas desabilitadas nos limites), mais o CSS correspondente em `.cal-*`.
- [X] T010 [US2] `Calendario.tsx`: rodapé com **"Hoje"** (escolhe a data atual e fecha; **desabilitado** se hoje for anterior ao mínimo) e **"Limpar"** (`aoLimpar` e fecha), usando `btn btn-ghost btn-sm`; **dias bloqueados** pelo `min` (apagados, sem clique); o dia escolhido anterior ao mínimo continua marcado, sem erro.
- [X] T011 [US2] Chrome (claro e escuro, capturas): depois de abrir, chegar a março de 2024 em 4 cliques (título, ano, 2024, março); setas virando o ano; "Hoje"; "Limpar" num filtro (a lista volta ao total); em "Fim da turma" com início escolhido os dias anteriores ficam apagados e sem clique e "Hoje" fica desabilitado quando hoje < mínimo. **Executado na página de teste temporária (`app/cal-teste`, já apagada), pois não há senha de teste nesta sessão; ver o relatório.**

**Checkpoint**: US2 funcional.

## Phase 5: User Story 3 — Teclado, leitor de tela, celular e sem cortes (P1)

- [X] T012 [US3] `Calendario.tsx`: **teclado** — ao abrir o foco vai ao dia escolhido (ou hoje) e fica **preso** no painel (Tab circula); foco "móvel" na grade (só o dia atual com `tabIndex=0`); setas (±1 dia, ±7 dias, virando mês), PageUp/PageDown (mês), Home/End (início/fim da semana), Enter/Espaço escolhem, Esc fecha **por escuta em captura com `stopPropagation`** (não fecha o modal que contém o campo); nas visões de mês/ano setas ±1 e ±3. **ARIA**: painel `role="dialog"` com `aria-label="Calendário"`, grade `role="grid"`, dias `role="gridcell"` com `aria-label` completo (`rotuloDoDia`), `aria-selected`, `aria-current="date"` (hoje) e `aria-disabled`; título do mês em região `aria-live="polite"`.
- [X] T013 [US3] `Calendario.tsx` + `dashboard.css`: **celular** — até 640 px (lido por `matchMedia` com `useSyncExternalStore`, sem estado em efeito) o painel vira **janela centralizada** sobre `.modal-overlay` (fundo escurecido da classe existente, sem nova cor), cabe na largura sem rolagem horizontal e tocar fora fecha.
- [X] T014 [US3] Chrome (claro e escuro, capturas): só teclado (abrir com Enter, navegar, escolher, Tab preso, Esc devolve o foco ao botão); dentro de um modal o Esc fecha só o calendário; o calendário aberto **dentro do modal do plano Semestral/Anual**, **numa tabela** e **num campo perto do rodapé** aparece inteiro, é o elemento no topo (`elementFromPoint` no centro) e abre para cima se faltar espaço; ao rolar a página/modal acompanha ou fecha; 360 px como janela centralizada sem rolagem horizontal. **Executado na página de teste temporária (`app/cal-teste`, já apagada), pois não há senha de teste nesta sessão; ver o relatório.**

**Checkpoint**: US3 funcional.

## Phase 6: User Story 4 — Competência no mesmo padrão (P2)

- [X] T015 [US4] Criar `frontend/components/shared/MonthInput.tsx` (contrato em `contracts/calendario.md`): texto `mm/aaaa` com máscara (6 dígitos, mês 1 a 12) emitindo `yyyy-MM` (ou vazio) e botão `.date-cal-btn` dentro do invólucro `.date-wrap`, que abre `<Calendario modo="mes" ... />` já na visão de meses ("Hoje" vira **"Este mês"**); sincronização do texto por mudança de `value` (mesmo padrão do `DateInput`, sem efeito).
- [X] T016 [P] [US4] Trocar o `<input type="month" value={competencia} onChange={(e) => setCompetencia(e.target.value)} />` por `<MonthInput value={competencia} onChange={setCompetencia} />` (e importar) em `frontend/app/(app)/financeiro/contas-a-pagar/novo/page.tsx`, `contas-a-pagar/[id]/editar/page.tsx`, `contas-a-receber/novo/page.tsx` e `contas-a-receber/[id]/editar/page.tsx`, sem mudar rótulos nem o restante das telas.
- [X] T017 [US4] Chrome (claro e escuro, capturas): "Nova conta a receber" e "Editar conta a receber": o botão abre o seletor de meses do ano do campo (ou de hoje) com o mês marcado; escolher fecha e mostra `11/2026`, valor `2026-11`; digitar `11/2026` emite `2026-11`; "Este mês" e "Limpar"; a competência salva é a mesma de antes (comparar a requisição de criar a conta com a de antes da mudança, sem gravar: interceptar e abortar a requisição). **Executado na página de teste temporária (`app/cal-teste`, já apagada), pois não há senha de teste nesta sessão; ver o relatório.**

**Checkpoint**: US4 funcional; as quatro histórias entregues.

## Phase 7: Acabamento e validação final

- [X] T018 Limpeza da spec 069: remover de `dashboard.css` o `.date-nativo` e as regras `color-scheme` (auxiliar e `input[type="month"]`) e confirmar que o `DateInput` não tem mais nenhum resíduo do auxiliar; manter `.date-wrap`, `.date-cal-btn` e a propriedade `min`. Acrescentar a nota de Princípio V em `specs/069-calendario-campos-de-data/spec.md` (o calendário do navegador foi substituído pelo calendário próprio, spec 070). Conferir por `git diff` que só entraram as remoções e a nota.
- [X] T019 [P] Contraste: ler `frontend/scripts/verificar-contraste.mjs` e confirmar que cada par usado pelo calendário (`--c-text` e `--c-text-muted` sobre `--c-surface`; `--c-on-accent` sobre `--c-accent`; `--c-accent-text` sobre `--c-surface` e sobre a tinta de destaque) **já está na lista de pares** (se faltar algum, acrescentar o par); rodar `npm run verificar-contraste` (0 pares abaixo de 4,5:1) e medir no Chrome, em claro e escuro, a razão calculada de dia, dia escolhido, hoje, setas, atalhos, meses e anos.
- [X] T020 **Verificação dos "MUST NOT" FR-002, FR-008, FR-014, FR-015, FR-016**: (a) ler o `git diff` de `lib/calendario.ts`, `Calendario.tsx`, `DateInput.tsx` e `MonthInput.tsx` e conferir `grep -nE "new Date\(|Date\.parse|toISOString|getTimezoneOffset"` **sem resultado** (o "hoje" vem de `dataIsoLocal`); (b) `grep -rn 'type="date"\|type="month"' frontend/app frontend/components` **sem resultado** e `grep -n "date-nativo\|color-scheme" frontend/styles/dashboard.css` **sem resultado**; (c) `git diff -- frontend/package.json frontend/package-lock.json` mostra **somente** o script `verificar-calendario` (nenhuma linha em `dependencies`); `git status --porcelain -- src tests database` e `git diff --stat -- src tests database` vazios; (d) `grep -nE "#[0-9a-fA-F]{3,6}\b|rgb\(|rgba\("` nas linhas adicionadas a `dashboard.css`, `Calendario.tsx` e `MonthInput.tsx` **sem resultado**; (e) o diff do `DateInput.tsx` mostra as propriedades públicas e o valor `yyyy-MM-dd` iguais, e `git status` mostra as 14 telas que o usam **sem alteração**; (f) `git status --porcelain -- frontend` com arquivos novos **somente** `lib/calendario.ts`, `scripts/verificar-calendario.mjs`, `components/shared/Calendario.tsx` e `components/shared/MonthInput.tsx`.
- [X] T021 Varredura final no Chrome (claro e escuro; 360 px; **capturas conferidas**): as 16 telas com campo de data e as 4 de competência abrem o calendário próprio em todos os campos, sem erro de console e sem rolagem horizontal; comparar a requisição de um filtro digitado com a do mesmo filtro escolhido pelo calendário; troca de tema com o calendário aberto muda as cores na hora; abrir um calendário fecha o outro. **Executado em 2026-11-05 com login real: 141 PASS e 2 FAIL do roteiro (turma só mostra datas com tipo Pacote; reteste com Pacote 6/6). Não havia conta a pagar/receber para abrir a edição; ver o relatório.**
- [X] T022 Rodar `cd frontend`: `npm run verificar-calendario`, `npx tsc --noEmit`, `npx eslint` (projeto inteiro, 0 problemas), `npm run verificar-contraste` e `npm run build` (API **parada**, `experimental.cpus: 2` temporário, `NODE_OPTIONS=--max-old-space-size=1536`, restaurando `next.config.ts`). Confirmar `git status --porcelain` só com os arquivos previstos.
- [X] T023 Fechamento: `docs/relatorios/<data>-calendario-proprio.md` (pedido, o que foi feito, validação, o que ficou de fora); hook `revisao-codigo` do Trello (com a API parada) e comentário no card; parar os servidores iniciados por mim; atualizar a memória do projeto; **não** commitar nem fazer push sem pedido.

## Dependências e ordem

- Fase 1 → Fase 2 (T002 e T003 em paralelo, depois T004) → US1 (T005 e T006 em paralelo, depois T007 e T008) → US2 (T009, T010, depois T011) → US3 (T012 e T013, depois T014) → US4 (T015, depois T016 e T017) → Fase 7.
- US2, US3 e US4 dependem do painel de US1; US4 depende também das visões de mês de US2 (T009).
- T018 só depois de T015/T016 (o `type="month"` some); T019 é independente; T020 a T023 são o fechamento, nessa ordem.

## Paralelismo

T002 com T003; T005 com T006; T016 em paralelo entre as quatro páginas; T019 a qualquer momento depois de T006.

## Estratégia de implementação

1. **MVP**: Fase 1 + Fase 2 + US1: já entrega o calendário próprio (dias) em todos os campos de data.
2. US2 acrescenta a navegação rápida, os atalhos e o mínimo; US3 fecha acessibilidade, celular e posicionamento; US4 leva o mesmo padrão à competência.
3. Acabamento e validação final só com as quatro histórias verdes.

## Cobertura dos requisitos

| FR | Tarefas | FR | Tarefas |
|---|---|---|---|
| FR-001 | T005, T007, T008 | FR-010 | T013, T014 |
| FR-002 | T005, T006, T020 | FR-011 | T005, T008, T012, T021 |
| FR-003 | T006, T019 | FR-012 | T012, T014 |
| FR-004 | T002, T003, T005 | FR-013 | T015, T016, T017 |
| FR-005 | T009, T011 | FR-014 | T007, T018, T020 |
| FR-006 | T010, T011 | FR-015 | T003, T020 |
| FR-007 | T010, T011 | FR-016 | T007, T020 |
| FR-008 | T002, T003, T020 | FR-017 | T002, T003, T004 |
| FR-009 | T005, T014 | | |

| SC | Tarefas |
|---|---|
| SC-001 (100% dos campos; 0 nativos) | T020, T021 |
| SC-002 (≤ 4 cliques no ano atual; ≤ 6 em ano a até 12 anos) | T011, T021 |
| SC-003 (contraste ≥ 4,5:1) | T019, T022 |
| SC-004 (mesmo valor, mesma requisição) | T008, T017, T021 |
| SC-005 (sem corte; sem rolagem horizontal em 360 px) | T014, T021 |
| SC-006 (uso completo só pelo teclado) | T012, T014 |
| SC-007 (0 dependências; 0 linhas de backend) | T020 |
