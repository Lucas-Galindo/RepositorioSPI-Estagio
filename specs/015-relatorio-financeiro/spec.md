# Feature Specification: Relatório Financeiro (Faturamento)

**Feature Branch**: `015-relatorio-financeiro`

**Created**: 2026-09-11

**Status**: Implemented (evoluído significativamente além da ERS original)

**Input**: Registro retroativo do comportamento real da Estória 15 (Relatório Financeiro/Faturamento) da ERS "PROFESSORAS INDEPENDENTES_v1.4", com base no código-fonte atual, não na ERS original.

## Nota histórica

Este é o único dos relatórios "de listagem" (Estórias 12-14, 16-18) que o endpoint original (`GET /api/relatorios/financeiro`) é efetivamente consumido por uma tela. Além disso, o sistema evoluiu bem além do que a ERS previa: existe hoje uma tela "Financeiro → Visão Geral" separada (não baseada em `/api/relatorios`, mas em um endpoint próprio combinando Contas a Receber e a Pagar) e uma segunda aba de "Visão de Indicadores" (inadimplência, prazo médio de atraso, fluxo de caixa) adicionada em sprint posterior, sem equivalente na ERS original.

**Atualização (2026-09-17, ver [029-fix-relatorio-financeiro-turma](../029-fix-relatorio-financeiro-turma/spec.md))**: o agrupamento "Somatória por turma" (User Story 1/FR-002 abaixo) foi corrigido. Antes, a turma de um pagamento era decidida pela primeira turma da lista geral de turmas do aluno (`Aluno.AlunosTurma.FirstOrDefault()`) — o que atribuía incorretamente todo o valor de um aluno matriculado em várias turmas a apenas uma delas. A partir desta correção, a turma de cada pagamento é determinada pelas aulas que ele efetivamente cobre (`Pagamento` → `PagamentoAula` → `Aula.TurmaId`), com dois grupos especiais: "Atendimento particular" (nenhuma aula vinculada tem turma) e "Múltiplas turmas" (as aulas cobertas pertencem a mais de uma turma distinta). Ver detalhes da regra em [029-fix-relatorio-financeiro-turma/data-model.md](../029-fix-relatorio-financeiro-turma/data-model.md).

**Atualização (2026-09-18, ver [030-remove-indice-cobertura-custos](../030-remove-indice-cobertura-custos/spec.md))**: o indicador "Cobertura de custos" (recebido ÷ pago), citado na User Story 3 e no FR-005 abaixo, foi removido — tanto da exibição (já removida pela spec [024](../024-remover-card-cobertura-custos/spec.md)) quanto do cálculo e do contrato de resposta da API. A aba "Visão de Indicadores" passa a exibir apenas Inadimplência %, Prazo médio de atraso, Margem de segurança % e o painel "Gargalo de caixa".

**Atualização (2026-09-18, ver [031-remove-margem-seguranca](../031-remove-margem-seguranca/spec.md))**: o indicador "Margem de segurança %" (fluxo de caixa operacional ÷ faturado), citado na User Story 3 e no FR-005 abaixo, também foi removido — da exibição (o único card real, nessa mesma aba), do cálculo e do contrato de resposta da API. O nome sugeria uma métrica de mercado (folga sobre o ponto de equilíbrio) que o cálculo real não sustentava. A aba "Visão de Indicadores" passa a exibir apenas Inadimplência % e Prazo médio de atraso, além do painel "Gargalo de caixa".

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Consultar relatório financeiro consolidado (Priority: P1)

Como professora, eu acesso "Relatórios → Dashboard Financeiro → Visão Financeiro" para ver o total recebido, pago, saldo realizado, receita/despesa pendente e saldo previsto de um período, com agrupamentos por forma de pagamento, aluno e turma.

**Why this priority**: É a visão financeira mais completa oferecida pelo sistema hoje.

**Independent Test**: Aplicar filtros de período, forma de pagamento, aluno, turma e/ou matéria e conferir que os totais batem com a soma dos pagamentos correspondentes.

**Acceptance Scenarios**:

1. **Given** um período informado (por data de pagamento), **When** a professora consulta, **Then** o sistema retorna Total Recebido, Total Pago, Saldo Realizado, Receita Pendente, Despesa Pendente e Saldo Previsto.
2. **Given** filtros de forma de pagamento, aluno, turma e/ou matéria, **When** aplicados, **Then** os totais e os agrupamentos ("Somatória por turma", "Recebido por forma de pagamento", "Recebido por aluno") são recalculados de acordo.

---

### User Story 2 - Consultar Visão Geral Financeira combinando Receitas e Despesas (Priority: P1)

Como professora, eu acesso "Financeiro → Visão Geral" para ver um resumo simples do mês corrente combinando Contas a Receber e Contas a Pagar, com os próximos vencimentos dos próximos 7 dias.

**Why this priority**: É a tela financeira de uso mais frequente, com visão rápida do mês corrente.

**Independent Test**: Acessar "Financeiro → Visão Geral" sem filtro e conferir o resumo de Receitas (Recebido/A Receber/Atrasado), Despesas (Pago/A Pagar/Atrasado) e Resultado (Saldo Realizado/Saldo Previsto).

**Acceptance Scenarios**:

1. **Given** a tela Visão Geral, **When** carregada sem filtros, **Then** exibe o resumo do mês corrente por padrão.
2. **Given** filtros de período, nome de turma, nome de matéria e/ou busca por aluno, **When** aplicados, **Then** o resumo é recalculado — a busca por texto (turma/matéria/aluno) afeta apenas o lado da receita, não das despesas.
3. **Given** a tela Visão Geral, **When** carregada, **Then** exibe uma lista de "Próximos Vencimentos" dos próximos 7 dias.

---

### User Story 3 - Consultar indicadores financeiros avançados (Priority: P2)

Como professora, eu acesso a aba "Visão de Indicadores" do Dashboard Financeiro para ver Inadimplência %, Prazo médio de atraso e um painel de "Gargalo de caixa", além do fluxo de caixa dos últimos 6 meses.

**Why this priority**: É uma capacidade analítica avançada, útil mas não essencial ao uso diário — não prevista na ERS original.

**Independent Test**: Acessar a aba "Visão de Indicadores" e conferir que os KPIs mudam ao aplicar um filtro de período diferente.

**Acceptance Scenarios**:

1. **Given** a aba Visão de Indicadores, **When** carregada, **Then** exibe Inadimplência %, Prazo médio de atraso e o dia de maior entrada/saída ("Gargalo de caixa").
2. **Given** a mesma aba, **When** a professora consulta o histórico, **Then** vê uma tabela de fluxo de caixa dos últimos 6 meses.

---

### Edge Cases

- A "Visão Geral" e o "Relatório Financeiro" (Visão Financeiro) usam o mesmo endpoint? Não — são endpoints diferentes (`/api/financeiro/visao-geral` vs. `/api/relatorios/financeiro`), com propósitos e granularidades diferentes.
- A busca textual de turma/matéria/aluno na Visão Geral afeta as despesas também? Não — afeta apenas o lado da receita, comportamento documentado explicitamente no código.
- Existe exportação (PDF/CSV) para qualquer uma dessas visões? Não — nenhum relatório do sistema tem exportação implementada.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O sistema MUST calcular, para um período filtrável por data de pagamento, forma de pagamento, aluno, turma e matéria: Total Recebido, Total Pago, Saldo Realizado, Receita Pendente, Despesa Pendente e Saldo Previsto.
- **FR-002**: O sistema MUST agrupar os valores recebidos por Forma de Pagamento, por Aluno e por Turma nessa mesma consulta. O agrupamento por Turma MUST usar a turma da(s) aula(s) efetivamente cobertas por cada pagamento (não a lista geral de turmas do aluno), com "Atendimento particular" para pagamentos sem aula com turma vinculada e "Múltiplas turmas" para pagamentos cujas aulas cobrem mais de uma turma distinta (ver [029-fix-relatorio-financeiro-turma](../029-fix-relatorio-financeiro-turma/spec.md)).
- **FR-003**: O sistema MUST oferecer uma visão geral separada e mais simples, combinando Contas a Receber e Contas a Pagar do período corrente por padrão, filtrável por período e por texto de turma/matéria/aluno (a busca textual afetando apenas o lado da receita).
- **FR-004**: A visão geral MUST exibir os vencimentos dos próximos 7 dias.
- **FR-005**: O sistema MUST calcular indicadores financeiros avançados (inadimplência, prazo médio de atraso, dia de maior entrada/saída, fluxo de caixa dos últimos 6 meses), filtráveis por período, turma, matéria e aluno.

### Key Entities *(include if feature involves data)*

- Não introduz entidades próprias — projeta agregações sobre Pagamento (ver [Registrar Pagamento](../009-registrar-pagamento/spec.md)).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Os totais consolidados (recebido, pago, saldo) batem exatamente com a soma dos pagamentos que atendem aos filtros aplicados.
- **SC-002**: A professora consegue visualizar, em até dois cliques a partir do menu Relatórios, tanto a visão financeira consolidada quanto os indicadores avançados.

## Assumptions

- A separação entre "Financeiro → Visão Geral" e "Relatórios → Dashboard Financeiro" é aceita como reorganização deliberada de produto, não uma duplicidade acidental.
- Os indicadores financeiros avançados (Sprint 3) são tratados como evolução legítima do sistema além da ERS original, documentados aqui como comportamento real atual.
