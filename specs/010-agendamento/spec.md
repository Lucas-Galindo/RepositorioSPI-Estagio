# Feature Specification: Agendamento

**Feature Branch**: `010-agendamento`

**Created**: 2026-09-11

**Status**: Implemented (documentação retroativa)

**Input**: Registro retroativo do comportamento real da Estória 10 (Agendamento) da ERS "PROFESSORAS INDEPENDENTES_v1.4", com base no código-fonte atual, não na ERS original.

## Nota histórica

A ERS original descrevia uma única tela de agenda com calendário, filtros por status/turma/aluno e indicadores. Na implementação real, essas responsabilidades estão **divididas em duas telas distintas**: "Aulas" (lista/tabela com filtros de status e turma, sem indicadores) e "Agenda" (calendário mensal com indicadores "Hoje/Agendadas/Realizadas/Canceladas", sem filtros de status/turma/aluno). Um comentário no próprio código documenta que essa divisão é resultado de uma evolução de produto: a agenda existia no protótipo, foi temporariamente substituída por uma lista simples, e depois restaurada como tela separada — hoje as duas coexistem. Este documento descreve o comportamento real de ambas.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Visualizar calendário mensal com indicadores (Priority: P1)

Como professora, eu acesso a tela "Agenda" e vejo um calendário mensal com todas as minhas aulas, além de indicadores rápidos de quantas aulas tenho Hoje, Agendadas, Realizadas e Canceladas no mês.

**Why this priority**: É a visão rápida do dia a dia da professora sobre sua rotina de aulas.

**Independent Test**: Acessar a tela Agenda em um mês com aulas cadastradas e conferir que os quatro indicadores batem com a contagem real das aulas daquele mês.

**Acceptance Scenarios**:

1. **Given** o mês corrente selecionado, **When** a professora acessa a Agenda, **Then** vê um calendário com as aulas daquele mês distribuídas por dia, e os contadores "Agendadas", "Realizadas" e "Canceladas" refletem o total do mês selecionado.
2. **Given** a tela Agenda, **When** a professora navega para o mês anterior ou seguinte, **Then** o calendário e os indicadores são recalculados para o novo mês.
3. **Given** o indicador "Hoje", **When** a professora consulta, **Then** ele reflete a contagem de aulas do dia atual, independentemente do mês navegado no calendário.

---

### User Story 2 - Agendar aula rapidamente pelo calendário (Priority: P1)

Como professora, eu clico em um dia vazio do calendário para agendar rapidamente uma nova aula naquela data, sem precisar navegar até a tela completa de cadastro de aulas.

**Why this priority**: Agiliza o agendamento no fluxo natural de "olhar o calendário e marcar uma aula".

**Independent Test**: Clicar em um dia vazio do calendário, preencher o formulário rápido e confirmar que a aula aparece corretamente no calendário.

**Acceptance Scenarios**:

1. **Given** um dia vazio do calendário, **When** a professora clica nele, **Then** um formulário de agendamento rápido é aberto, pré-preenchido com aquela data.
2. **Given** o formulário de agendamento rápido, **When** a professora tenta cadastrar uma aula cujo horário conflita com outra já existente, **Then** o sistema rejeita com a mesma validação de conflito usada no formulário completo de Aulas (não há lógica de conflito duplicada ou divergente entre os dois pontos de entrada).
3. **Given** um dia com aulas já cadastradas, **When** a professora clica nele, **Then** vê a lista das aulas daquele dia e pode agendar uma aula adicional, editar ou excluir uma existente diretamente por esse modal.

---

### User Story 3 - Ver detalhes de uma aula pelo calendário (Priority: P2)

Como professora, eu clico em uma aula já marcada no calendário para ver ou editar seus detalhes.

**Why this priority**: Permite navegação fluida sem sair da visão de calendário.

**Independent Test**: Clicar em um evento existente do calendário e confirmar que abre o mesmo formulário em modo de edição.

**Acceptance Scenarios**:

1. **Given** uma aula existente no calendário, **When** a professora clica nela, **Then** o mesmo formulário de aula abre em modo de edição, pré-preenchido com os dados daquela aula.

---

### User Story 4 - Filtrar aulas na tela de lista (Priority: P1)

Como professora, na tela "Aulas" (separada da Agenda), eu filtro a lista de aulas por Status e por Turma, para revisar rapidamente um subconjunto específico de aulas.

**Why this priority**: Complementa a visão de calendário com uma visão tabular filtrável, mais adequada para revisão detalhada.

**Independent Test**: Acessar a tela "Aulas", aplicar um filtro de Status e outro de Turma, e conferir que a lista resultante respeita ambos.

**Acceptance Scenarios**:

1. **Given** a tela "Aulas", **When** a professora aplica um filtro de Status, **Then** somente aulas com aquele status aparecem na lista.
2. **Given** a mesma tela, **When** a professora aplica um filtro de Turma, **Then** somente aulas daquela turma aparecem.
3. **Given** essa mesma tela, **When** a professora procura um filtro por Aluno na interface, **Then** não o encontra — esse filtro existe apenas na API, não é exposto nesta tela.

---

### Edge Cases

- A tela "Agenda" tem filtros de status, turma ou aluno? Não — apenas navegação por mês/dia; os indicadores agregados substituem a necessidade de filtro nessa tela.
- A tela "Aulas" tem indicadores como "Hoje/Agendadas/Realizadas/Canceladas"? Não — apenas uma contagem textual do total de resultados encontrados; os indicadores ficam exclusivamente na tela Agenda.
- O que acontece se duas aulas da mesma professora se sobrepõem no horário, uma cadastrada pela tela Aulas e outra pelo agendamento rápido da Agenda? É impossível — ambas usam a mesma validação de conflito no backend, então a segunda tentativa (por qualquer um dos dois caminhos) é sempre rejeitada.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O sistema MUST oferecer uma visão de calendário mensal (tela Agenda) exibindo todas as aulas da professora distribuídas por dia, navegável entre meses.
- **FR-002**: A tela Agenda MUST exibir indicadores agregados de "Hoje", "Agendadas", "Realizadas" e "Canceladas", recalculados conforme o mês navegado (exceto "Hoje", que reflete sempre o dia atual).
- **FR-003**: O sistema MUST permitir agendar uma nova aula diretamente ao clicar em um dia do calendário, usando a mesma validação de conflito de horário aplicada ao formulário completo de cadastro de aula.
- **FR-004**: O sistema MUST permitir visualizar, editar ou excluir uma aula diretamente a partir do calendário, sem navegação adicional para outra tela.
- **FR-005**: O sistema MUST oferecer uma visão de lista/tabela separada (tela Aulas) com filtros de Status e Turma, distinta da visão de calendário.

### Key Entities *(include if feature involves data)*

- **Aula**: mesma entidade de [Gerenciar Aula](../005-gerenciar-aula/spec.md) — este módulo é uma camada de visualização/interação sobre ela, sem entidade própria.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A professora consegue visualizar todas as aulas de um mês e seus indicadores agregados em uma única tela, sem precisar navegar por múltiplas páginas.
- **SC-002**: 100% dos agendamentos feitos pelo calendário rápido recebem a mesma validação de conflito de horário que os agendamentos feitos pelo formulário completo — nenhum conflito escapa por um dos dois caminhos.
- **SC-003**: A professora consegue filtrar a lista de aulas por Status e Turma sem sair da tela "Aulas".

## Assumptions

- A divisão em duas telas (Agenda com indicadores, Aulas com filtros) é aceita como a organização de produto atual, ainda que uma leitura literal da ERS original sugerisse uma única tela combinando ambas as capacidades.
- O filtro por Aluno, existente na API mas não exposto nas telas de Agenda nem de Aulas, é aceito como lacuna atual de interface, não uma ausência de capacidade no backend.
