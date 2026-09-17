# Feature Specification: Remover Card "Cobertura de Custos" da Aba Indicadores

**Feature Branch**: `024-remover-card-cobertura-custos`

**Created**: 2026-09-16

**Status**: Draft

**Input**: User description: "Na aba Relatórios → Relatório Financeiro → visão Indicadores, remover o card/caixa 'Cobertura de custos' (não agrega valor real para o usuário) e realinhar os demais cards restantes para preencher o espaço de forma equilibrada."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Visualizar a visão Indicadores sem o card irrelevante (Priority: P1)

Como professora consultando a aba Relatórios → Relatório Financeiro → visão Indicadores, eu quero ver apenas os indicadores que realmente me ajudam a entender a saúde financeira do negócio, sem um card cujo cálculo ("Recebido ÷ pago no período") não me diz nada de útil na prática, para que a tela fique mais limpa e eu gaste menos tempo tentando interpretar um número que não uso.

**Why this priority**: É o pedido central da mudança — sem a remoção do card, nenhuma das demais expectativas (layout equilibrado) faz sentido. Sozinha já entrega o valor principal: uma tela de Indicadores mais enxuta e relevante.

**Independent Test**: Abrir Relatórios → Relatório Financeiro → aba Indicadores e verificar que o card "Cobertura de custos" não aparece em nenhuma combinação de filtros (período, turma, matéria, aluno) nem em nenhum estado de dados (com dados, sem dados, carregando, erro).

**Acceptance Scenarios**:

1. **Given** a professora está na aba Indicadores do Relatório Financeiro, **When** a tela carrega com dados do período selecionado, **Then** o card "Cobertura de custos" não é exibido em nenhum lugar da tela.
2. **Given** a professora está na aba Indicadores, **When** ela altera os filtros (período, turma, matéria ou aluno), **Then** o card "Cobertura de custos" continua ausente, independentemente do resultado retornado.
3. **Given** a professora está na aba Indicadores, **When** não há dados para o período/filtros selecionados, **Then** a tela exibe o estado vazio normalmente, sem qualquer referência ao indicador de cobertura de custos.

---

### User Story 2 - Ver os indicadores restantes organizados de forma equilibrada (Priority: P2)

Como professora vendo a aba Indicadores, eu quero que os cards restantes (Inadimplência, Prazo médio de atraso, Margem de segurança) ocupem o espaço da fileira de forma equilibrada — sem um vão vazio no lugar do card removido — para que a tela continue com uma aparência organizada e intencional, não como se algo estivesse faltando.

**Why this priority**: É um refinamento visual sobre a User Story 1 — a remoção já entrega o valor principal (tela mais relevante), mas sem o realinhamento a tela ficaria com uma quebra visual perceptível, sinalizando um "espaço quebrado" em vez de uma limpeza intencional.

**Independent Test**: Abrir a aba Indicadores em diferentes larguras de tela (desktop e mobile) e verificar visualmente que os três cards restantes se distribuem de forma equilibrada na fileira, sem espaço vazio nem card fora de proporção.

**Acceptance Scenarios**:

1. **Given** o card "Cobertura de custos" foi removido, **When** a professora visualiza a fileira de indicadores em tela desktop, **Then** os três cards restantes ocupam a largura total da fileira de forma equilibrada entre si (mesma largura entre eles), sem espaço vazio visível.
2. **Given** a mesma tela em uma largura menor (mobile/tablet), **When** a fileira de indicadores é exibida, **Then** os três cards restantes continuam legíveis e organizados, seguindo o mesmo padrão responsivo já usado nas demais fileiras de indicadores do sistema.

---

### Edge Cases

- O que acontece com o valor/cálculo de cobertura de custos que já existia (índice de cobertura de custos fixos)? Ele deixa de ser exibido na tela; não há requisito de continuar calculando-o em segundo plano para uso futuro nesta mudança.
- Existe algum outro lugar do sistema (fora da aba Indicadores do Relatório Financeiro) que também exiba ou dependa da exibição do card "Cobertura de custos"? Não — esta mudança está restrita à fileira de cards da aba Indicadores; nenhum outro relatório ou tela exibe esse card.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O sistema MUST deixar de exibir o card "Cobertura de custos" na fileira de indicadores da aba Indicadores do Relatório Financeiro, em qualquer combinação de filtros (período, turma, matéria, aluno) e em qualquer estado de dados (com dados, sem dados, carregando, erro).
- **FR-002**: O sistema MUST continuar exibindo os demais cards da fileira de indicadores (Inadimplência, Prazo médio de atraso, Margem de segurança) exatamente com o mesmo conteúdo, cálculo e comportamento que já têm hoje — esta mudança não MUST alterar nenhum desses indicadores.
- **FR-003**: O sistema MUST distribuir os cards restantes na fileira de forma equilibrada (larguras iguais entre si, preenchendo o espaço da fileira), sem deixar um vão vazio no lugar do card removido.
- **FR-004**: O realinhamento dos cards restantes MUST seguir o mesmo padrão visual e responsivo (incluindo comportamento em telas menores) já usado pelas demais fileiras de indicadores/cards do sistema, sem introduzir um estilo visual novo e inconsistente.
- **FR-005**: Esta mudança MUST ficar restrita à fileira de cards da aba Indicadores do Relatório Financeiro — nenhuma outra aba, relatório ou tela do sistema MUST ser alterada por esta mudança.

### Key Entities

Não aplicável — esta mudança é puramente de apresentação (remoção de um elemento visual e reorganização de layout) e não envolve novas entidades de dados nem alteração de entidades existentes.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% das visitas à aba Indicadores do Relatório Financeiro, em qualquer filtro ou estado de dados, não exibem o card "Cobertura de custos".
- **SC-002**: Os três cards restantes preenchem visualmente toda a largura da fileira de indicadores, sem espaço vazio perceptível, em telas desktop e mobile.
- **SC-003**: Nenhum dos demais indicadores (Inadimplência, Prazo médio de atraso, Margem de segurança) sofre qualquer alteração de valor, cálculo ou comportamento após a mudança.

## Assumptions

- "Cobertura de custos" refere-se ao card hoje exibido na fileira de indicadores com o rótulo "Cobertura de custos" (ícone de dinheiro, valor no formato "Nx", legenda "Recebido ÷ pago no período") — o único card com esse nome na aba Indicadores do Relatório Financeiro.
- A remoção é apenas de apresentação (deixar de exibir o card na tela); não há requisito, nesta mudança, de remover o cálculo subjacente no backend nem de removê-lo de qualquer contrato de API — apenas de não exibi-lo nesta tela. Caso o valor deixe de ser necessário em qualquer resposta de API, isso é uma decisão técnica a ser avaliada na fase de planejamento, não uma exigência funcional desta especificação.
- "Realinhar de forma equilibrada" significa distribuir os cards restantes em larguras iguais entre si, ocupando o espaço total da fileira — o mesmo padrão já observado em outras fileiras de indicadores do sistema que têm um número diferente de cards.
- Nenhum outro texto, filtro ou explicação da tela (como o aviso sobre filtros de turma/matéria/aluno se aplicarem só ao lado da receita) precisa mudar em decorrência desta remoção.
