# Feature Specification: Exibição de Dashboard

**Feature Branch**: `011-dashboard`

**Created**: 2026-09-11

**Status**: Implemented (documentação retroativa)

**Input**: Registro retroativo do comportamento real da Estória 11 (Exibição de Dashboard) da ERS "PROFESSORAS INDEPENDENTES_v1.4", com base no código-fonte atual, não na ERS original.

## Nota histórica

A API do Dashboard hoje calcula e retorna muito mais indicadores do que a tela inicial ("Home") efetivamente exibe — incluindo indicadores financeiros avançados (inadimplência, prazo médio de atraso, margem de segurança, fluxo de caixa mensal) que não constavam na ERS original e foram adicionados em sprints posteriores. A tela Home, porém, usa apenas uma pequena parte desses dados; a maior parte dos indicadores financeiros avançados é exibida em telas de Relatórios separadas (ver [Relatório Financeiro](../015-relatorio-financeiro/spec.md)). Este documento descreve o que é calculado pela API versus o que é efetivamente mostrado na Home.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Ver indicadores resumidos na Home após login (Priority: P1)

Como professora, ao entrar no sistema, eu sou direcionada à tela Home, onde vejo rapidamente quantos alunos atendi e quanto recebi no período corrente.

**Why this priority**: É a primeira tela vista a cada acesso, dando uma visão imediata do negócio.

**Independent Test**: Fazer login e conferir que a Home exibe "Alunos atendidos" e "Recebido este mês" consistentes com os dados reais do período corrente.

**Acceptance Scenarios**:

1. **Given** a professora autenticada, **When** ela acessa a Home, **Then** vê o número de alunos atendidos no período corrente e o valor total recebido nesse mesmo período.
2. **Given** o período corrente sem nenhum pagamento recebido, **When** a Home carrega, **Then** o valor recebido é exibido como zero, sem erro.

---

### User Story 2 - Ver agenda semanal e próxima aula na Home (Priority: P1)

Como professora, na Home eu vejo minha agenda semanal e a próxima aula agendada, para saber imediatamente meu próximo compromisso.

**Why this priority**: Dá contexto operacional imediato, complementando os indicadores financeiros.

**Independent Test**: Ter uma aula agendada nos próximos dias e conferir que ela aparece destacada na Home.

**Acceptance Scenarios**:

1. **Given** aulas agendadas na semana corrente, **When** a professora acessa a Home, **Then** vê uma grade semanal com essas aulas.
2. **Given** uma próxima aula futura mais próxima, **When** a Home carrega, **Then** ela é destacada separadamente como "próxima aula".

---

### Edge Cases

- A Home exibe o total de aulas agendadas, total de alunos ativos, total de turmas ativas, valor pendente de recebimento ou lembretes pendentes? Não — embora a API do Dashboard calcule e retorne todos esses valores, a tela Home hoje não os renderiza; apenas "alunos atendidos no período" e "valor faturado no período" são exibidos.
- Onde ficam os indicadores financeiros avançados (inadimplência, margem de segurança, fluxo de caixa)? Não aparecem na Home — são calculados pela mesma API do Dashboard, mas consumidos apenas pela aba "Visão de Indicadores" do Relatório Financeiro (ver [Relatório Financeiro](../015-relatorio-financeiro/spec.md)).
- A agenda semanal e a próxima aula vêm da mesma chamada que os indicadores financeiros? Não — são obtidas por uma consulta separada às aulas, não pelo endpoint de Dashboard.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O sistema MUST calcular, para um período informado (com padrão de mês corrente quando não especificado), pelo menos: total de aulas agendadas, total de alunos ativos, alunos atendidos no período, total de turmas ativas, valor pendente de recebimento, valor faturado no período, lembretes pendentes, e um conjunto de indicadores financeiros agregados.
- **FR-002**: A tela inicial (Home) MUST exibir, no mínimo, a contagem de alunos atendidos no período e o valor faturado no período.
- **FR-003**: A tela inicial MUST exibir a agenda semanal de aulas e a próxima aula agendada, obtidas independentemente dos indicadores financeiros.
- **FR-004**: O sistema MUST permitir consultar os indicadores do Dashboard para um período customizado (não apenas o mês corrente), ainda que a tela Home use sempre o período padrão.

### Key Entities *(include if feature involves data)*

- Este módulo não introduz entidades próprias — consolida dados de Aula, Aluno, Turma, Pagamento e Lembrete calculados sob demanda.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A professora consegue ver, em uma única tela após o login, quantos alunos atendeu e quanto recebeu no mês corrente, sem precisar navegar para outra página.
- **SC-002**: A professora consegue identificar sua próxima aula agendada diretamente na tela inicial.

## Assumptions

- O fato de a API calcular mais indicadores do que a Home exibe é aceito como estado atual do produto (parte desses dados foi redirecionada para telas de Relatórios em vez de acumular tudo na Home) — não é tratado aqui como defeito a corrigir.
- Indicadores financeiros avançados (inadimplência, fluxo de caixa mensal etc.) são documentados com mais detalhe na spec [Relatório Financeiro](../015-relatorio-financeiro/spec.md), evitando duplicação de conteúdo.
