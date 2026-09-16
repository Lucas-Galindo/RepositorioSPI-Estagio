# Feature Specification: Relatório de Pendências Financeiras

**Feature Branch**: `014-relatorio-pendencias-financeiras`

**Created**: 2026-09-11

**Status**: Implemented (reorganizado — a funcionalidade existe, mas não pelo endpoint originalmente previsto)

**Input**: Registro retroativo do comportamento real da Estória 14 (Relatório de Pendências Financeiras/Pagamentos) da ERS "PROFESSORAS INDEPENDENTES_v1.4", com base no código-fonte atual, não na ERS original.

## Nota histórica

A ERS original previa uma tela única de "Relatório de Pagamentos". O endpoint que a implementaria literalmente (`GET /api/relatorios/pagamentos`) existe no backend, mas está **órfão** — nenhuma tela o consome. Na prática, a necessidade de acompanhar pendências financeiras foi **reorganizada em duas telas reais**, ambas consumindo `GET /api/pagamentos` diretamente (não o endpoint de relatório): "Financeiro → Contas a Receber" (CRUD com filtros) e "Relatórios → Dashboard de Pagamentos" (indicadores e gráfico por status). Este documento descreve o que existe de fato.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Consultar e gerenciar contas a receber pendentes (Priority: P1)

Como professora, eu acesso "Financeiro → Contas a Receber" para ver, filtrar e gerenciar os pagamentos pendentes e atrasados dos meus alunos.

**Why this priority**: É o fluxo real usado hoje para cobrar e acompanhar recebimentos — ver detalhes em [Registrar Pagamento](../009-registrar-pagamento/spec.md).

**Independent Test**: Acessar "Contas a Receber", filtrar por status "Atrasado" e conferir que a lista bate com pagamentos pendentes vencidos.

**Acceptance Scenarios**:

1. **Given** a tela Contas a Receber, **When** a professora filtra por Aluno, período de vencimento e/ou Status, **Then** a lista é restringida conforme os filtros aplicados no servidor.
2. **Given** a mesma tela, **When** a professora aplica filtros adicionais de turma, matéria, categoria de receita ou forma de pagamento, **Then** esses filtros são aplicados no lado do cliente, sobre os dados já carregados do servidor.

---

### User Story 2 - Ver dashboard de pagamentos com indicadores (Priority: P1)

Como professora, eu acesso "Relatórios → Dashboard de Pagamentos" para ver um resumo visual (KPIs de Recebido/Pendente/Atrasado/Total, gráfico por status, últimos lançamentos).

**Why this priority**: Dá uma visão consolidada e visual, complementar à tela de gerenciamento (Contas a Receber).

**Independent Test**: Acessar "Dashboard de Pagamentos", aplicar um filtro de período e conferir que os KPIs refletem a soma correta por status.

**Acceptance Scenarios**:

1. **Given** o Dashboard de Pagamentos, **When** a professora aplica filtros de vencimento (ou um atalho de semestre), **Then** os KPIs e o gráfico por status são recalculados no servidor.
2. **Given** a mesma tela, **When** a professora aplica filtros de matéria, turma, forma de pagamento ou aluno, **Then** esses filtros são aplicados no lado do cliente sobre os dados já carregados.
3. **Given** o Dashboard de Pagamentos, **When** carregado, **Then** exibe os 6 lançamentos de pagamento mais recentes em uma tabela de "Últimos lançamentos".

---

### Edge Cases

- Existe uma tela chamada literalmente "Relatório de Pagamentos" consumindo o endpoint homônimo da API? Não — esse endpoint específico (`/api/relatorios/pagamentos`) está órfão; a funcionalidade equivalente foi implementada via `/api/pagamentos` em duas telas distintas.
- Os filtros de turma/matéria/categoria/forma de pagamento em ambas as telas são aplicados no servidor? Não — eles são aplicados no cliente, após os dados virem filtrados apenas por aluno/período/status do servidor (decisão documentada explicitamente em comentário no próprio código).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O sistema MUST permitir consultar pagamentos pendentes e atrasados filtrados por Aluno, período de vencimento e Status, com os resultados calculados no servidor.
- **FR-002**: O sistema MUST oferecer filtros adicionais de turma, matéria, categoria de receita e forma de pagamento sobre esse mesmo conjunto de dados (aplicados no cliente sobre o resultado já obtido do servidor).
- **FR-003**: O sistema MUST exibir um total consolidado de valores pendentes (Pendente + Atrasado) na visão de gerenciamento de Contas a Receber.
- **FR-004**: O sistema MUST oferecer uma visão de dashboard separada com KPIs de Recebido/Pendente/Atrasado/Total, um gráfico de distribuição por status, e uma lista dos lançamentos mais recentes.

### Key Entities *(include if feature involves data)*

- Não introduz entidades próprias — reaproveita a entidade Pagamento (ver [Registrar Pagamento](../009-registrar-pagamento/spec.md)).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A professora consegue identificar todos os pagamentos pendentes e atrasados de um aluno específico em uma única consulta filtrada.
- **SC-002**: Os KPIs do Dashboard de Pagamentos refletem corretamente a soma dos valores por status para o período filtrado.

## Assumptions

- O endpoint de relatório dedicado (`/api/relatorios/pagamentos`) é mantido como capacidade latente no backend, mas não corresponde a nenhum fluxo de usuário ativo — a funcionalidade real está descrita nas duas telas documentadas acima.
- A aplicação de alguns filtros no cliente (não no servidor) é aceita como decisão de implementação documentada no próprio código, não tratada aqui como defeito.
