# Phase 0 Research: Área de Clique Expandida no Menu do Financeiro e Correção de Hitbox na Tabela de Contas a Pagar

**Feature**: [spec.md](./spec.md) | **Date**: 2026-09-14

## Contexto da investigação

Esta é a segunda rodada de pesquisa desta feature (021). A rodada anterior (2026-09-11) tratava
o item do menu do Financeiro (US1) como um bug de hitbox *a remover*; o usuário corrigiu a
direção em 2026-09-14 — é uma melhoria de UX *a adicionar* (expandir a área de clique, não
restringi-la), com a abordagem de "divisão estática do espaço" já decidida pelo usuário antes
desta pesquisa (não uma decisão em aberto). O item da tabela de Contas a Pagar (US2) não mudou
de direção; a pesquisa anterior sobre ele permanece válida e é reproduzida aqui.

## Decisão 1 — Menu de abas do Financeiro: como implementar a divisão estática (US1)

**O que foi inspecionado**: `frontend/app/(app)/financeiro/layout.tsx` (3 `<Link
className="btn btn-sm ...">` dentro de `<div className="row-gap" style={{ marginBottom: 18, gap:
8 }}>`) e a classe `.row-gap` em `frontend/styles/dashboard.css:245-250`
(`display:flex; align-items:center; justify-content:space-between`). Busca confirmou que
`.row-gap` é usada em **14 arquivos** do frontend (`pagamentos`, `turmas`, `lembretes`,
`dashboard`, `financeiro/contas-a-receber`, `relatorios/financeiro`, `agenda`,
`relatorios/pagamentos`, `financeiro/contas-a-pagar`, `financeiro/layout`, `alunos`, `aulas`,
`materias`), sempre no padrão "texto/contador à esquerda + botão de ação à direita" — nenhum
outro uso é uma lista de abas de navegação.

**Decision**: Não reaproveitar nem modificar `.row-gap`. Criar uma classe CSS nova e escopada
(por exemplo `.financeiro-tabs`) aplicada apenas ao `<div>` que envolve as três abas em
`financeiro/layout.tsx`, com `display: flex` e cada `<Link>` recebendo `flex: 1 1 0` (ou a classe
nova aplicando `flex: 1 1 0` diretamente aos filhos via seletor, ex. `.financeiro-tabs > a`).
Isso faz cada aba ocupar automaticamente um terço da largura do container (ou a fração
proporcional, se larguras mínimas de conteúdo diferirem), cobrindo também o espaço antes vazio
entre e ao redor delas — a divisão do espaço passa a ser uma propriedade do layout (calculada
pelo motor de flexbox do navegador a cada renderização/resize), não um cálculo de distância em
JavaScript disparado por evento de clique. Manter `padding`/aparência visual do texto do botão
centralizados dentro de cada `<Link>` (ex. `justify-content: center` no próprio link, ou manter
o alinhamento à esquerda atual dentro de cada slot — decisão visual de baixo risco, validada
visualmente na Fase de implementação) para não alterar a aparência do menu, apenas sua área de
resposta a clique.

**Rationale**: Atende à decisão de UX do usuário (divisão estática, não dependente de posição do
mouse) com a implementação mais simples possível — nenhum novo estado React, nenhum listener de
clique customizado, nenhum cálculo de `getBoundingClientRect()` em runtime. O comportamento de
"a aba mais próxima" emerge naturalmente da geometria: cada pixel da faixa do menu pertence a
exatamente um `<Link>` (o flex item que ocupa aquele espaço), e a fronteira entre dois `<Link>`
adjacentes já é, por definição de flexbox, a borda entre os dois — equivalente ao "ponto médio
estático" pedido, sem necessidade de calculá-lo explicitamente. Como cada `<Link>` já é um
elemento `<a>` nativo, clicar em qualquer ponto dentro do seu retângulo (incluindo a parte antes
vazia) já dispara a navegação do Next.js normalmente — nenhuma mudança de comportamento de
navegação, apenas de área.

**Alternatives considered**:
- Modificar a classe `.row-gap` existente para adicionar `flex: 1 1 0` aos filhos — rejeitada
  por afetar as outras 13 telas que reusam essa classe no padrão "texto + botão", onde um botão
  ocupando 1/2 ou 1/3 da largura do container quebraria o layout esperado (botão de ação deve
  permanecer com largura de conteúdo, não esticado).
- Calcular a "aba mais próxima" via JavaScript no `onClick` do container (`getBoundingClientRect()`
  de cada `<Link>` comparado ao `event.clientX` do clique) — rejeitada por ser exatamente o
  comportamento dinâmico/dependente de mouse que o usuário decidiu explicitamente não usar
  ("sem depender da posição do mouse" — decisão de UX registrada em `spec.md` Assumptions);
  também introduziria uma superfície de manutenção maior (lógica JS customizada) para um
  resultado que o CSS já resolve de forma declarativa.
- Adicionar um elemento invisível (`::before`/overlay) cobrindo o espaço entre botões com um
  `onClick` próprio decidindo a aba mais próxima — rejeitada pela mesma razão da alternativa
  anterior (ainda seria uma decisão dinâmica calculada em JS) e por reintroduzir a complexidade
  de posicionamento que a spec quer evitar (ver Edge Cases do spec.md sobre layouts responsivos).

## Decisão 2 — Cabeçalho da tabela de Contas a Pagar (US2)

*(Reproduzido sem alteração da pesquisa anterior desta feature — o item 2 não mudou de direção.)*

**O que foi inspecionado**: `frontend/app/(app)/financeiro/contas-a-pagar/page.tsx` (tabela com
`<thead><tr><th>...</th></tr></thead>` sem nenhum `onClick`/lógica de ordenação — busca
exaustiva por termos como `sortField`, `onSort`, `.sortable`, `ordenaç` em todo `frontend/` não
retornou nenhum resultado) e as classes CSS envolvidas: `thead th` (`padding: 11px 24px`,
`dashboard.css:1069-1078`), `tbody td` (`padding: 13px 24px`, linhas 1079-1082), `tbody tr`
(`border-top: 1px solid var(--c-bg)`, linhas 1083-1085) e `tbody tr.row-link` (`cursor: pointer`,
linhas 1086-1089, com `onClick={() => router.push(...)}` no JSX da linha, linha 197 do
`page.tsx`, não no `<td>` individual).

**Confirmado**: não existe, hoje, nenhuma funcionalidade de ordenação de coluna implementada
nesta tabela (nem em nenhuma outra tabela do sistema). O `onClick` de navegação já está
corretamente escopado ao `<tr>` do `tbody`, não ao `<thead>`.

**Hipótese mais provável**: vazamento geométrico do hitbox da primeira linha de dados para a
área do cabeçalho — como `tbody tr.row-link` cobre toda a largura da linha via os paddings de
`13px 24px` em cada `<td>`, e o `thead` fica imediatamente acima sem espaçamento extra (`table {
margin-top: 6px }`, sem margem entre `thead` e `tbody`), um clique próximo à borda inferior do
cabeçalho pode, dependendo de arredondamento/zoom do navegador, estar geometricamente dentro do
retângulo da primeira linha de dados.

**Decision**: Confirmar esta hipótese por inspeção interativa no DevTools (comparar
`getBoundingClientRect()` do `<thead>` e da primeira `<tr class="row-link">`, testar clique perto
da borda em zooms 90%-125%) como primeira tarefa de implementação de US2. Se confirmada, a
correção mínima é garantir separação geométrica clara entre as duas áreas — por exemplo, um
espaçamento vertical explícito entre `thead` e `tbody`, ou revisão do `border-top` de `tbody tr`
— sem alterar o `onClick` já corretamente escopado ao `tbody tr.row-link`.

**Rationale**: Escrever a correção antes de confirmar a causa arriscaria alterar CSS que não é a
causa real. A tabela de Contas a Pagar é estruturalmente idêntica ao padrão `row-link` usado em
várias outras tabelas do sistema; se a hipótese se confirmar, o ajuste de CSS deve ficar restrito
às classes específicas desta tabela (ou, se a causa for uma classe verdadeiramente compartilhada
como `thead th`/`tbody tr`, avaliar impacto visual nas outras telas antes de aplicar — mesmo
cuidado de escopo já aplicado à Decisão 1).

**Alternatives considered**: (idênticas à pesquisa anterior) implementar ordenação de coluna de
fato — rejeitada por estar fora do escopo (FR-005/FR-007: nenhuma ação nova); mover o `onClick`
de navegação para um botão explícito dentro da linha — rejeitada por ser mudança de UX maior que
quebraria consistência com todas as outras tabelas do sistema.

## Resumo das tarefas de confirmação necessárias antes da correção de US2

US1 não depende de investigação interativa prévia — a causa (`.row-gap` com `space-between` não
cobre o espaço vazio) já é conhecida por leitura estática de código, e a solução (nova classe
flex) é direta. US2 continua dependendo de uma tarefa de investigação interativa (DevTools) antes
de qualquer alteração de CSS, registrada como tarefa inicial de US2 em `tasks.md`.
