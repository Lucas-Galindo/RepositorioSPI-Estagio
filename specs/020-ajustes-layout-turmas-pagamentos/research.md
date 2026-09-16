# Phase 0 Research: Ajustes de Layout — Turmas e Pagamentos

**Feature**: [spec.md](./spec.md) | **Date**: 2026-09-11

Nenhum item do Technical Context ficou marcado como `NEEDS CLARIFICATION` — o stack (Next.js/React no frontend), a ausência de framework de teste de UI, e os 3 arquivos afetados já foram confirmados por leitura direta do código durante a fase de `/speckit-specify`. Este documento registra a causa raiz e a decisão de correção para cada um dos três ajustes, com as alternativas consideradas.

## Decisão 1 — Estado vazio de Turmas não centralizado

**Causa raiz confirmada**: `frontend/app/(app)/turmas/page.tsx:51` renderiza `<EmptyState .../>` como filho direto de um `<div className="report-grid">` (linha 47). Em `frontend/styles/dashboard.css:1162-1166`, `.report-grid` é `display: grid; grid-template-columns: repeat(3, 1fr);`. Como `EmptyState` é o único item do grid quando não há turmas, ele ocupa apenas a primeira das 3 colunas (1fr), ficando visualmente à esquerda — mesmo o componente `.empty-state` já sendo internamente centralizado (`text-align: center; align-items: center`, `dashboard.css:1329-1335`).

**Decision**: Quando `total === 0`, renderizar o `EmptyState` fora do container `.report-grid` (ou dar a ele um `style`/classe que faça `grid-column: 1 / -1` dentro do grid), para que ocupe toda a largura das 3 colunas e a centralização interna do componente passe a valer em relação à largura total da área de conteúdo.

**Rationale**: É a correção mínima e localizada — não exige tocar em `.report-grid` (usado por outras telas, como `/relatorios/turmas`, que teriam o mesmo comportamento potencialmente desejado, mas isso está fora do escopo pedido) nem criar um novo componente.

**Alternatives considered**:
- Alterar `.report-grid` globalmente para `justify-items: center` — rejeitada por afetar todas as telas que usam essa classe (incluindo grades com cartões reais, onde centralizar os cartões individualmente não é o comportamento desejado).
- Envolver o `EmptyState` num wrapper com `width: 100%; display: flex; justify-content: center` fora do grid — equivalente em efeito a `grid-column: 1 / -1`, mas menos idiomático dado que o restante do projeto já usa `grid-column` em nenhum outro lugar; a opção de `grid-column: 1 / -1` foi preferida por ser mais direta dentro do próprio grid.

## Decisão 2 — Espaçamento insuficiente entre select de aluno e botão "Vincular"

**Causa raiz confirmada**: `frontend/app/(app)/turmas/[id]/page.tsx:160` usa `<div className="field-row" style={{ marginTop: 14, gap: 8 }}>` envolvendo o `<select>` (linha 161) e o `<button>` "Vincular" (linha 174). Um `gap: 8` (8px) entre um campo de formulário e um botão de ação imediatamente adjacente é estreito o suficiente para causar cliques acidentais, especialmente em telas menores ou touch.

**Decision**: Aumentar o valor de `gap` inline nesse `div` (de `8` para um valor perceptivelmente maior, ex. `16`–`20`px), mantendo a mesma estrutura `flex`/`field-row` já usada.

**Rationale**: Mudança de uma linha, sem tocar em CSS compartilhado (`field-row` é usado em outros formulários do projeto com gaps variados via `style` inline, então o padrão já é sobrescrever `gap` por instância) — não há risco de regressão em outras telas.

**Alternatives considered**:
- Aumentar o `gap` padrão da classe `.field-row` no CSS global — rejeitada porque `field-row` é reutilizada em outros formulários com espaçamentos já calibrados; mudar o padrão global arriscaria desalinhar outras telas não mencionadas no pedido do usuário.
- Adicionar `margin-left` só no botão em vez de aumentar o `gap` do container — equivalente em resultado visual, mas menos consistente com o padrão já usado no arquivo (gap no container pai). `gap` foi preferido.

## Decisão 3 — Seção "Métodos de pagamento" abaixo da tabela de pagamentos

**Causa raiz confirmada**: Em `frontend/app/(app)/pagamentos/page.tsx`, o bloco `<div className="table-wrap">` com a tabela de registros de pagamento está nas linhas 145-192; o bloco `<div className="panel" style={{ marginTop: 18 }}>` com a seção "Métodos de pagamento" (título, subtítulo e tabela de formas cadastradas) está nas linhas 194-219, ou seja, **depois** do primeiro bloco na árvore JSX (e portanto abaixo dele na página renderizada).

**Decision**: Mover o bloco JSX inteiro da seção "Métodos de pagamento" (linhas 194-219) para antes do bloco `<div className="table-wrap">` da tabela de pagamentos (antes da linha 145) — mantendo intacto tanto o `filter-bar` (linhas 101-137, que inclui o filtro "Forma — todas", já acima e que não deve mudar de posição) quanto o botão "Registrar pagamento" (linhas 139-143). A ordem final passa a ser: filtros → botão "Registrar pagamento" → seção "Métodos de pagamento" → tabela de registros de pagamento. Alternativamente, se for preferível manter o botão "Registrar pagamento" imediatamente acima da tabela de registros (mais perto da ação relacionada), a seção "Métodos de pagamento" pode ser movida para logo após o `filter-bar` e antes do botão — ambas as ordens satisfazem o requisito FR-003 (seção acima da tabela); a primeira opção é a recomendada por manter o agrupamento visual "filtros + ação principal" já existente hoje.

**Rationale**: É um recorte-e-cola de um bloco JSX autocontido — o bloco já busca seus próprios dados (`formas`, carregado no mesmo `useEffect` que já roda hoje) e não depende de nenhum estado calculado pela tabela de pagamentos, então a reordenação não tem efeito colateral funcional.

**Alternatives considered**:
- Transformar a seção "Métodos de pagamento" numa aba/toggle separada da tabela principal — rejeitada por ser uma mudança de interação, não apenas de posição, o que vai além do que foi pedido ("são ajustes visuais/de layout, sem mudança de comportamento").
- Mover apenas o título da seção para cima, mantendo a tabela de formas onde está — rejeitada por não atender ao pedido explícito de que a seção (como um todo) fique acima da tabela de pagamentos.
