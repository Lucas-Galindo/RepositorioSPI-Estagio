# Feature Specification: Relatório de Matérias

**Feature Branch**: `018-relatorio-materias`

**Created**: 2026-09-11

**Status**: Implemented no backend; sem uso no frontend hoje (documentação retroativa)

**Input**: Registro retroativo do comportamento real da Estória 18 (Relatório de Matérias) da ERS "PROFESSORAS INDEPENDENTES_v1.4", com base no código-fonte atual, não na ERS original.

## Nota histórica

O endpoint (`GET /api/relatorios/materias`) existe no backend, calculando exatamente o que a ERS original previa (quantidade de aulas no período e quantidade de alunos atendidos por matéria), mas está órfão — nenhuma tela o consome. A tela "Matérias" (`/materias`) é apenas um CRUD (nome, nível, descrição), sem exibir essas quantidades agregadas.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Consultar quantidade de aulas e alunos por matéria via API (Priority: P3)

Como consumidor da API (hoje, nenhuma tela o faz), eu informo um nível e/ou período, e recebo, para cada matéria, a quantidade de aulas no período e a quantidade de alunos atendidos.

**Why this priority**: Funcionalidade tecnicamente completa, mas sem consumidor ativo.

**Independent Test**: Chamar `GET /api/relatorios/materias?nivel=...&inicio=...&fim=...` e conferir que a contagem de aulas/alunos por matéria bate com os dados reais.

**Acceptance Scenarios**:

1. **Given** um nível e/ou período informados, **When** a consulta é feita, **Then** o sistema retorna, para cada matéria correspondente, Nome, Descrição, Nível, quantidade de aulas no período e quantidade de alunos atendidos.

---

### Edge Cases

- A tela "Matérias" exibe quantidade de aulas ou alunos por matéria? Não — é um CRUD simples (Nome, Nível, Descrição, Ativo), sem esses agregados.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O sistema MUST calcular, para cada matéria filtrável por nível e período, a quantidade de aulas ministradas no período e a quantidade de alunos atendidos.

### Key Entities *(include if feature involves data)*

- Não introduz entidades próprias — projeta agregações sobre Materia e Aula (ver [Gerenciar Matérias](../004-gerenciar-materias/spec.md) e [Gerenciar Aula](../005-gerenciar-aula/spec.md)).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A API retorna corretamente as quantidades de aulas e alunos atendidos por matéria para qualquer combinação válida de filtros.

## Assumptions

- Esta funcionalidade é mantida no backend como capacidade disponível, mas não corresponde a nenhum fluxo de usuário ativo hoje.
