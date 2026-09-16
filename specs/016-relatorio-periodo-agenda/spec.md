# Feature Specification: Relatório do Período da Agenda

**Feature Branch**: `016-relatorio-periodo-agenda`

**Created**: 2026-09-11

**Status**: Implemented no backend; sem uso no frontend hoje (documentação retroativa)

**Input**: Registro retroativo do comportamento real da Estória 16 (Relatório do Período da Agenda) da ERS "PROFESSORAS INDEPENDENTES_v1.4", com base no código-fonte atual, não na ERS original.

## Nota histórica

O endpoint (`GET /api/relatorios/periodo-agenda`) existe no backend e calcula exatamente os indicadores agregados previstos na ERS (aulas por dia, comparativo Agendadas/Realizadas/Canceladas, taxa de ocupação), mas **nenhuma tela o consome**. A tela mais próxima em nome e propósito, "Relatórios → Dashboard de Turmas", **não usa este endpoint** — ela calcula indicadores semelhantes no próprio frontend, consultando Turmas, Aulas e Matérias diretamente.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Consultar indicadores agregados de ocupação da agenda via API (Priority: P3)

Como consumidor da API (hoje, nenhuma tela o faz), eu informo um período, turma e/ou matéria, e recebo a quantidade de aulas por dia, o comparativo entre aulas Agendadas/Realizadas/Canceladas, e a taxa de ocupação do período.

**Why this priority**: Funcionalidade tecnicamente completa, mas sem consumidor ativo.

**Independent Test**: Chamar `GET /api/relatorios/periodo-agenda?inicio=...&fim=...&turmaId=...&materiaId=...` (com período padrão de mês corrente se omitido) e conferir os totais retornados.

**Acceptance Scenarios**:

1. **Given** um período, turma e/ou matéria informados (ou o mês corrente como padrão), **When** a consulta é feita, **Then** o sistema retorna a quantidade de aulas por dia dentro do período e filtros.
2. **Given** o mesmo período, **When** a consulta é feita, **Then** o sistema retorna também o total de aulas Agendadas, Realizadas e Canceladas, e a Taxa de Ocupação (percentual de Realizadas sobre o total do período).

---

### Edge Cases

- A tela "Dashboard de Turmas" usa este endpoint? Não — calcula indicadores semelhantes de forma independente, no próprio frontend, a partir de outras APIs (Turmas, Aulas, Matérias).
- Existe alguma tela com o nome literal "Relatório do Período da Agenda"? Não foi encontrada.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O sistema MUST calcular, para um período (com padrão de mês corrente), filtrável por turma e matéria, a quantidade de aulas por dia.
- **FR-002**: O sistema MUST calcular o total de aulas Agendadas, Realizadas e Canceladas no período e filtros informados.
- **FR-003**: O sistema MUST calcular uma Taxa de Ocupação como o percentual de aulas Realizadas sobre o total de aulas do período filtrado.

### Key Entities *(include if feature involves data)*

- Não introduz entidades próprias — projeta agregações sobre Aula (ver [Gerenciar Aula](../005-gerenciar-aula/spec.md)).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A API retorna corretamente os indicadores agregados (aulas por dia, comparativo por status, taxa de ocupação) para qualquer combinação válida de filtros.

## Assumptions

- Esta funcionalidade é mantida no backend como capacidade disponível, mas não corresponde a nenhum fluxo de usuário ativo hoje.
- O "Dashboard de Turmas" real, embora tematicamente próximo, é tratado como uma implementação paralela e independente, não uma consumidora deste endpoint.
