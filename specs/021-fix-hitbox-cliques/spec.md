# Feature Specification: Área de Clique Expandida no Menu do Financeiro e Correção de Hitbox na Tabela de Contas a Pagar

**Feature Branch**: `021-fix-hitbox-cliques`

**Created**: 2026-09-11

**Updated**: 2026-09-14 — direção do item 1 corrigida pelo usuário, e abordagem de "aba mais próxima" decidida como divisão estática do espaço (ver notas abaixo)

**Status**: Draft

**Input**: User description original: "Bug de área de clique (hitbox): em dois lugares do sistema — (1) menu/navegação superior do módulo Financeiro, e (2) cabeçalho de ordenação da tabela de Gargalos em Contas a Pagar — clicar próximo a um botão/elemento clicável (mas fora dele) ainda dispara a ação daquele elemento (troca de tela, ou reordenação da tabela)." Clarificado pelo usuário que o local do item 2 é a tabela de registros de "Contas a Pagar".

**Correção de direção (2026-09-14)**: O usuário esclareceu que a descrição do item (1) estava invertida. Hoje, clicar no espaço vazio do menu do Financeiro (fora dos botões "Visão Geral"/"Contas a Receber"/"Contas a Pagar", mas dentro da faixa do menu) **não faz nada** — não é um bug de clique indevido a remover. O usuário quer o oposto: que esse espaço passe a ser clicável, trocando para a aba mais próxima do ponto clicado. Ou seja, o item (1) deixa de ser uma correção de bug e passa a ser uma melhoria de UX a **adicionar** (expandir a área de resposta ao clique do menu). O item (2) — tabela de Contas a Pagar — não mudou: continua sendo um bug de área de clique a corrigir, sem nenhuma ação nova.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Clicar em qualquer ponto do menu do Financeiro troca para a aba mais próxima (Priority: P1)

Como professora navegando entre "Visão Geral", "Contas a Receber" e "Contas a Pagar" no menu superior do módulo Financeiro, quando eu clico em qualquer ponto dentro da faixa do menu — mesmo fora do texto/botão de uma aba específica, como no espaço vazio entre duas abas — o sistema deve trocar para a aba mais próxima do ponto onde cliquei, em vez de não fazer nada.

**Why this priority**: É o ponto de navegação mais usado do módulo Financeiro; hoje, cliques que erram ligeiramente o botão são ignorados, obrigando a professora a mirar com precisão em uma faixa de texto pequena — uma área de clique maior e mais tolerante reduz esforço e frustração em um fluxo de uso frequente.

**Independent Test**: Acessar `/financeiro` (ou qualquer uma das sub-telas do módulo), clicar em pontos dentro da faixa do menu que estão fora dos limites visuais de qualquer botão (o espaço entre duas abas adjacentes, ou nas bordas da faixa) e verificar que o sistema navega para a aba visualmente mais próxima do ponto clicado.

**Acceptance Scenarios**:

1. **Given** o menu superior do Financeiro com as abas "Visão Geral", "Contas a Receber" e "Contas a Pagar", **When** a professora clica exatamente sobre o texto ou dentro dos limites visuais de uma aba, **Then** o sistema navega para a tela correspondente normalmente (comportamento correto, não deve mudar).
2. **Given** o mesmo menu, **When** a professora clica em um ponto do espaço vazio entre "Contas a Receber" e "Contas a Pagar" (mas dentro da faixa do menu), **Then** o sistema navega para a aba cujo botão está visualmente mais próximo do ponto clicado.
3. **Given** a professora clica em um ponto do espaço vazio à esquerda de "Visão Geral" ou à direita de "Contas a Pagar" (ainda dentro da faixa do menu), **When** o clique ocorre, **Then** o sistema navega para a aba correspondente àquela extremidade ("Visão Geral" ou "Contas a Pagar", respectivamente) — a aba mais próxima da borda clicada.
4. **Given** a professora já está na tela de uma aba (por exemplo, "Contas a Receber") e clica em um ponto vazio cuja aba mais próxima é a mesma em que ela já está, **When** o clique ocorre, **Then** o sistema permanece na tela atual sem erro (navegar para a própria rota atual é um resultado aceitável, não uma falha).
5. **Given** a professora clica em um ponto claramente fora da faixa do menu (acima ou abaixo dela, fora do container do menu), **When** o clique ocorre, **Then** nenhuma navegação é disparada — a expansão da área de clique vale apenas dentro dos limites visuais da faixa do menu, não da tela inteira.

---

### User Story 2 - Clicar perto do cabeçalho da tabela de Contas a Pagar não dispara ação inesperada (Priority: P1)

Como professora consultando a tabela de registros de Contas a Pagar, quando eu clico próximo ao cabeçalho de uma coluna — mas fora da área realmente destinada a essa ação — o sistema não deve reordenar a tabela nem executar nenhuma outra ação não intencional (como abrir o detalhe de um registro da tabela).

**Why this priority**: Mesma severidade do item 1 — é uma tela de uso financeiro frequente, e uma ação inesperada ao consultar a tabela (como abrir o registro errado) pode levar a decisões baseadas em dados mal interpretados.

**Independent Test**: Acessar `/financeiro/contas-a-pagar`, clicar em pontos progressivamente mais distantes da área visualmente ativa do cabeçalho da tabela (incluindo a transição entre a última linha do cabeçalho e a primeira linha de dados) e verificar se alguma ação é disparada indevidamente.

**Acceptance Scenarios**:

1. **Given** a tabela de Contas a Pagar carregada com registros, **When** a professora clica dentro dos limites visuais de um elemento de cabeçalho realmente clicável (se houver), **Then** apenas a ação correspondente a esse elemento específico é executada.
2. **Given** a mesma tabela, **When** a professora clica em um ponto do cabeçalho (ou próximo à borda entre o cabeçalho e a primeira linha de dados) que visualmente não corresponde a nenhum elemento clicável, **Then** o sistema não deve abrir o detalhe de nenhum registro da tabela nem executar qualquer outra ação não solicitada.
3. **Given** a professora clica em uma linha de dados real da tabela (fora do cabeçalho), **When** o clique ocorre dentro dos limites visuais dessa linha, **Then** o comportamento atual de abrir o detalhe do registro correspondente continua funcionando normalmente (nenhuma regressão).

---

### Edge Cases

- **(US1)** Se a faixa do menu tiver largura variável (por exemplo, em mobile ~400px, onde os botões podem quebrar linha), a divisão estática do espaço vazio é recalculada a partir do layout renderizado naquele momento (posição/largura real de cada botão adjacente após a quebra de linha, se houver) — a divisão continua sendo fixa em relação ao layout, nunca dependente da posição do clique, mas o layout em si pode mudar com a tela.
- **(US1)** Como a divisão do espaço vazio entre duas abas adjacentes é estática (linha divisória fixa no ponto médio — ou proporcional, se as abas tiverem larguras diferentes — entre as bordas dos dois botões), não existe ambiguidade de "empate": cada ponto do espaço vazio pertence deterministicamente a um único lado da divisão, sempre o mesmo para aquele layout.
- **(US1)** Isso deve valer também para toque em telas sensíveis ao toque (mobile/tablet)? Sim — a mesma lógica de "aba mais próxima" deve responder a toque, já que a imprecisão do dedo é o principal motivador desta melhoria.
- **(US2)** O que acontece se a professora clicar exatamente na borda entre o cabeçalho e a primeira linha da tabela? Nenhuma ação deve ser disparada nesse ponto de transição — apenas um clique claramente dentro dos limites visuais de uma linha de dados real deve abrir seu detalhe.
- **(US2)** Isso deve valer também para toque em telas sensíveis ao toque? Sim — a área clicável real de cada elemento deve corresponder à sua área visual em qualquer dispositivo de entrada.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: No menu de navegação superior do módulo Financeiro, o sistema MUST disparar a troca de tela para uma aba sempre que o clique ocorrer dentro dos limites visuais da faixa do menu (o container que agrupa as três abas), incluindo cliques no espaço vazio entre abas adjacentes ou nas bordas da faixa — nenhum clique dentro da faixa do menu MUST ficar sem ação.
- **FR-002**: O sistema MUST NOT disparar nenhuma navegação do menu do Financeiro para cliques fora dos limites visuais da faixa do menu (acima, abaixo, ou fora do container que agrupa as abas).
- **FR-003**: Quando o clique ocorrer exatamente sobre o texto/botão de uma aba, o sistema MUST continuar navegando para essa aba especificamente (comportamento já existente, sem regressão).
- **FR-004**: O espaço vazio entre duas abas adjacentes MUST ser dividido de forma estática (não dependente da posição do clique dentro do gap) entre as duas abas vizinhas — no ponto médio do espaço, ou proporcionalmente, se as abas tiverem larguras visuais diferentes — de modo que cada ponto do espaço vazio pertença deterministicamente à área de uma única aba adjacente, calculada a partir do layout renderizado (não recalculada por movimento do mouse).
- **FR-005**: Na tabela de registros de Contas a Pagar, o sistema MUST disparar qualquer ação associada ao cabeçalho da tabela (incluindo eventual reordenação de coluna, se essa capacidade existir ou vier a existir) somente quando o clique ocorrer dentro dos limites visuais do elemento de cabeçalho correspondente, e MUST NOT disparar essa ação, nem abrir o detalhe de um registro, para cliques fora desses limites.
- **FR-006**: Na tabela de Contas a Pagar, a área efetivamente clicável de cada elemento interativo do cabeçalho (se houver) MUST corresponder à sua área visualmente delimitada, sem se estender de forma perceptível além dela.
- **FR-007**: A melhoria do menu do Financeiro (FR-001 a FR-004) MUST NOT alterar as rotas de destino de nenhuma aba, nem introduzir nenhuma aba nova; a correção da tabela de Contas a Pagar (FR-005/FR-006) MUST NOT alterar o comportamento de abrir o detalhe de um registro ao clicar em uma linha de dados real — em ambos os casos, apenas os limites de detecção do clique mudam (expandindo no caso do menu, restringindo no caso do cabeçalho da tabela).

### Key Entities *(include if feature involves data)*

- Não aplicável — esta funcionalidade não introduz nem altera nenhuma entidade de dados; é uma melhoria/correção de comportamento de interação (área de clique) sobre telas e dados já existentes (ver [Relatório Financeiro](../015-relatorio-financeiro/spec.md) para o comportamento funcional do módulo Financeiro, que permanece inalterado).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% dos cliques realizados dentro da faixa do menu do Financeiro (dentro ou fora dos botões) navegam para uma aba — nenhum clique dentro da faixa fica sem ação, e a aba escolhida para cada ponto do espaço vazio é sempre a mesma (determinística), conforme a divisão estática do espaço.
- **SC-002**: 100% dos cliques realizados fora da faixa do menu do Financeiro não disparam nenhuma navegação.
- **SC-003**: 100% dos cliques realizados fora dos limites visuais de um elemento de cabeçalho clicável (ou fora de uma linha de dados real) na tabela de Contas a Pagar não disparam nenhuma ação de reordenação nem abertura de detalhe.
- **SC-004**: Nenhuma regressão é observada no comportamento correto existente — cliques diretamente sobre uma aba do menu, ou sobre uma linha de dados real da tabela, continuam funcionando exatamente como antes.

## Assumptions

- **Decisão de UX (2026-09-14)**: a área clicável do menu é **estática**, não dependente da posição do clique/mouse. O espaço vazio entre duas abas adjacentes é dividido ao meio (ou proporcionalmente, se as abas tiverem tamanhos visuais diferentes) entre as duas — essa linha divisória é definida pelo layout renderizado (largura/posição real de cada botão), recalculada apenas quando o layout muda (resize, quebra de linha em mobile, zoom), nunca pelo ponto exato onde o clique ocorreu dentro do gap. Na prática, isso equivale a estender a área clicável real de cada botão de aba (via padding/pseudo-elemento no container ou nos próprios elementos `<a>`) até a metade do espaço vazio em direção a cada vizinho, e até a borda do container nas extremidades — não a um cálculo de distância em JavaScript disparado a cada clique.
- A expansão da área de clique do menu do Financeiro (US1) está contida à faixa/container do menu (o elemento que hoje agrupa os três botões) — não se estende ao restante da página.
- O usuário confirmou que o local do item (2) é a tabela de registros de Contas a Pagar (`/financeiro/contas-a-pagar`), não um painel separado. A causa técnica exata do vazamento de hitbox entre o cabeçalho e a primeira linha de dados (se confirmada) permanece a ser verificada na fase de planejamento (`/speckit-plan`), assim como a versão anterior desta spec já registrava.
- Esta é tratada como uma feature combinada: uma melhoria de UX nova (US1, expandir área de clique do menu) e uma correção de bug (US2, restringir área de clique do cabeçalho da tabela) — ambas de prioridade P1, tratadas juntas por terem sido relatadas na mesma solicitação original do usuário, mas são independentes entre si (nenhuma depende da outra para ser implementada ou testada).
