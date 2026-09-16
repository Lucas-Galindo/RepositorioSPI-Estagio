# Feature Specification: Relatório de Agenda

**Feature Branch**: `012-relatorio-agenda`

**Created**: 2026-09-11

**Status**: Implemented no backend; sem uso no frontend hoje (documentação retroativa)

**Input**: Registro retroativo do comportamento real da Estória 12 (Relatório de Agenda) da ERS "PROFESSORAS INDEPENDENTES_v1.4", com base no código-fonte atual, não na ERS original.

## Nota histórica

A ERS original previa uma tela dedicada de "Relatório de Agenda", exportável em Excel/PDF. A API backend (`GET /api/relatorios/agenda`) existe e funciona exatamente como descrito, mas **nenhuma tela do frontend a consome hoje** — é um endpoint órfão. Na prática, a necessidade de "ver a agenda filtrada" é atendida por duas telas diferentes que consultam a API de Aulas diretamente (não este endpoint de relatório): a tela "Aulas" (lista com filtros de status/turma) e a tela "Agenda" (calendário com indicadores) — ver [Agendamento](../010-agendamento/spec.md). Além disso, **nenhum formato de exportação (PDF/Excel/CSV) foi implementado** em nenhum relatório do sistema. Este documento registra o que existe de fato na API, e a divergência de uso real.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Consultar aulas filtradas via API de relatório (Priority: P3)

Como consumidor da API (hoje, nenhuma tela do sistema o faz), eu consulto uma lista de aulas filtrada por período, status, turma e/ou aluno, formatada como itens de relatório (data, horários, matéria, turma/aluno, status).

**Why this priority**: A funcionalidade existe e é tecnicamente íntegra, mas não é usada pela interface hoje — prioridade baixa por ausência de uso real.

**Independent Test**: Chamar `GET /api/relatorios/agenda?inicio=...&fim=...&status=...&turmaId=...&alunoId=...` e conferir que a lista retornada respeita todos os filtros combinados.

**Acceptance Scenarios**:

1. **Given** um período, status, turma e/ou aluno informados, **When** a consulta é feita, **Then** o sistema retorna apenas as aulas que atendem a todos os filtros informados simultaneamente.
2. **Given** nenhum filtro além do período, **When** a consulta é feita, **Then** todas as aulas da professora naquele período são retornadas.

---

### Edge Cases

- Existe alguma tela do sistema que chama este endpoint hoje? Não — foi confirmado por busca no código-fonte do frontend que nenhuma página faz essa chamada.
- É possível exportar esse relatório em PDF, Excel ou CSV como a ERS original previa? Não — nenhum mecanismo de exportação foi implementado em nenhum relatório do sistema até o momento.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O sistema MUST oferecer uma consulta de aulas formatada como itens de relatório (data, hora início, hora fim, matéria, turma ou aluno, status), filtrável por período, status, turma e aluno.
- **FR-002**: Esta funcionalidade, embora disponível na API, MUST NOT ser considerada uma tela ativa do produto até que uma interface a consuma — o registro aqui documenta a capacidade latente, não um fluxo de usuário completo hoje.

### Key Entities *(include if feature involves data)*

- Não introduz entidades próprias — projeta dados de [Gerenciar Aula](../005-gerenciar-aula/spec.md) em um formato de leitura.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A API responde corretamente a qualquer combinação dos filtros suportados (período, status, turma, aluno), retornando somente aulas compatíveis com todos eles.

## Assumptions

- Este relatório é mantido no backend como capacidade disponível para uso futuro, mas não é uma funcionalidade ativa do produto hoje — os fluxos reais de consulta de agenda estão documentados em [Agendamento](../010-agendamento/spec.md).
- A ausência de exportação (PDF/Excel/CSV) é tratada como lacuna transversal a todos os relatórios do sistema, não específica deste.
