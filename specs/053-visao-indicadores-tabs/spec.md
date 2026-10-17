# Feature Specification: Padronizar Visual do Alternador Visão Financeiro/Indicadores

**Feature Branch**: `053-visao-indicadores-tabs`

**Created**: 2026-10-13

**Status**: Draft

**Input**: User description (card Trello "Botao visao de indicadores", Backlog): "Botão de Visão de indicadores ser igual ao de contas a pagar e contas a receber"

## Nota de investigação prévia

Confirmado por leitura do código atual (grounding antes de especificar):

- **O alternador "Visão Financeiro" / "Visão de Indicadores"** (`frontend/app/(app)/relatorios/financeiro/page.tsx:204-209`) já usa as mesmas classes de botão que as abas de referência (`btn btn-sm`, alternando `btn-primary`/`btn-ghost` conforme a aba ativa) — a cor e o formato do botão em si já são idênticos. A diferença está só no contêiner: os 2 botões vivem dentro de `<div className="row-gap" style={{ marginBottom: 18, gap: 8 }}>`.
- **As abas "Visão Geral" / "Contas a Receber" / "Contas a Pagar"** (`frontend/app/(app)/financeiro/layout.tsx:17-26`) usam as mesmas classes de botão, mas dentro de `<div className="financeiro-tabs">` — um `<Link>` por aba (navegação real, não troca de estado local).
- **A causa visual exata** (`frontend/styles/dashboard.css`): `.row-gap` é `display:flex; justify-content:space-between`, deixando os 2 botões encostados cada um numa ponta da linha; `.financeiro-tabs { display:flex; align-items:stretch; }` combinado com `.financeiro-tabs > a { flex: 1 1 0; justify-content:center; }` faz cada aba esticar e dividir a largura total igualmente, dando o visual de barra de abas cheia que o card pede.
- **Diferença estrutural a resolver**: a regra `.financeiro-tabs > a` seleciona especificamente elementos `<a>` — as abas de Contas a Receber/Pagar são links de navegação (`<Link>`), enquanto "Visão Financeiro"/"Visão de Indicadores" são `<button>` (troca de estado local `aba`, sem navegação, specs/048 já depende desse comportamento local). A regra de CSS precisa cobrir também `<button>` para o novo visual funcionar, sem alterar o comportamento de clique de nenhum dos dois pares.
- **Escopo confirmado único**: `Visão de Indicadores`/`Visão Financeiro` é o único par de botões desse tipo na aplicação (busca por "Visão de Indicadores" no frontend não retorna outro arquivo) — nenhuma outra tela precisa do mesmo ajuste.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Alternador de Visão Financeiro/Indicadores com o mesmo visual das abas de Contas a Receber/Pagar (Priority: P1)

Como professora, quero que o alternador "Visão Financeiro"/"Visão de Indicadores" na tela de Relatório Financeiro tenha a mesma aparência de barra de abas (largura cheia, dividida igualmente) que já uso em Financeiro > Contas a Receber/Contas a Pagar, para que a interface pareça consistente em vez de ter dois estilos diferentes de alternância de visão.

**Why this priority**: É o único pedido do card — uma correção visual isolada, sem lógica de negócio nova.

**Independent Test**: Abrir Relatórios > Relatório Financeiro e comparar visualmente o alternador "Visão Financeiro"/"Visão de Indicadores" com as abas de Financeiro > Contas a Receber/Contas a Pagar — ambos devem ocupar a largura total do contêiner, divididos em partes iguais, com o mesmo espaçamento e alinhamento.

**Acceptance Scenarios**:

1. **Given** a tela de Relatório Financeiro aberta, **When** a professora observa o alternador "Visão Financeiro"/"Visão de Indicadores", **Then** os 2 botões ocupam a largura total do contêiner, cada um com metade do espaço, no mesmo padrão visual das abas de Contas a Receber/Contas a Pagar.
2. **Given** o alternador com o novo visual, **When** a professora clica em "Visão de Indicadores" ou "Visão Financeiro", **Then** a troca de aba continua funcionando exatamente como antes (troca de conteúdo local, sem navegação, sem perda dos filtros já persistidos por specs/048).
3. **Given** a tela de Contas a Receber/Contas a Pagar aberta, **When** a professora observa as abas "Visão Geral"/"Contas a Receber"/"Contas a Pagar", **Then** elas continuam com o mesmo visual de hoje, sem nenhuma mudança (esta feature não as altera, só serve de referência).

---

### Edge Cases

- O que acontece em telas estreitas (mobile/tablet)? O alternador deve continuar legível e clicável, esticando os 2 botões proporcionalmente à largura disponível — mesmo comportamento responsivo que `.financeiro-tabs` já tem hoje para as 3 abas de Financeiro.
- O que acontece com o filtro ativo (`aba`, persistido em `sessionStorage` por specs/048) ao navegar para a tela e voltar? Nenhuma mudança — esta feature é só visual, não toca o estado ou a persistência de filtros.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O alternador "Visão Financeiro"/"Visão de Indicadores" (Relatório Financeiro) MUST ter o mesmo visual de barra de abas em largura total, dividida igualmente entre os 2 botões, já usado pelas abas "Visão Geral"/"Contas a Receber"/"Contas a Pagar" (Financeiro).
- **FR-002**: Esta feature MUST NOT alterar o comportamento de clique do alternador "Visão Financeiro"/"Visão de Indicadores" — continua trocando o conteúdo exibido localmente (estado `aba`), sem navegação de rota e sem afetar os filtros persistidos (specs/048).
- **FR-003**: Esta feature MUST NOT alterar o visual, o comportamento ou o markup das abas "Visão Geral"/"Contas a Receber"/"Contas a Pagar" (Financeiro) — elas servem só de referência visual, permanecem como estão.
- **FR-004**: Esta feature MUST NOT introduzir nenhuma tela, rota ou componente novo — é um ajuste de estilo num componente já existente.

### Key Entities

Não aplicável — feature puramente visual, sem entidade de dados nova ou alterada.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: O alternador "Visão Financeiro"/"Visão de Indicadores" ocupa 100% da largura do contêiner, com os 2 botões do mesmo tamanho, visualmente indistinguível em padrão de layout das abas de Contas a Receber/Contas a Pagar.
- **SC-002**: 100% dos cliques no alternador continuam trocando a aba exibida corretamente, sem nenhuma regressão de comportamento ou perda do filtro persistido.
- **SC-003**: 0 mudanças visuais ou de comportamento nas abas "Visão Geral"/"Contas a Receber"/"Contas a Pagar".

## Assumptions

- "Igual ao de contas a pagar e contas a receber" (texto do card) refere-se ao **layout do contêiner** (largura total, dividido igualmente) — a cor/formato dos botões em si (`btn btn-sm`, `btn-primary`/`btn-ghost`) já é idêntica hoje entre os dois pares; não há mudança de paleta de cor a fazer.
- A forma técnica de estender o visual de `.financeiro-tabs` (hoje só `> a`) para também cobrir `> button`, sem afetar as abas de navegação já existentes, fica para `/speckit-plan`.
- Classificado como ajuste visual isolado e de baixo risco (sem mudança de regra de negócio) — ainda assim especificado via Spec Kit a pedido explícito do usuário.
