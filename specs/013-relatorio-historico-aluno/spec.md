# Feature Specification: Relatório de Histórico do Aluno

**Feature Branch**: `013-relatorio-historico-aluno`

**Created**: 2026-09-11

**Status**: Implemented no backend; sem uso no frontend hoje (documentação retroativa)

**Input**: Registro retroativo do comportamento real da Estória 13 (Relatório de Histórico do Aluno) da ERS "PROFESSORAS INDEPENDENTES_v1.4", com base no código-fonte atual, não na ERS original.

## Nota histórica

Assim como o Relatório de Agenda, este endpoint (`GET /api/relatorios/historico-aluno/{alunoId}`) existe no backend, implementado fielmente conforme a ERS original, mas **nenhuma tela do frontend o consome hoje**. Não há, no momento, nenhuma tela equivalente que exiba o histórico de aulas e frequência de um aluno específico por outro caminho — é uma capacidade que ficou sem interface.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Consultar histórico e frequência de um aluno via API (Priority: P3)

Como consumidor da API (hoje, nenhuma tela o faz), eu escolho um aluno e um período, e recebo o histórico de aulas em que ele esteve vinculado, junto com seu percentual de frequência no período.

**Why this priority**: Funcionalidade tecnicamente completa, mas sem consumidor ativo — prioridade baixa por ausência de uso real hoje.

**Independent Test**: Chamar `GET /api/relatorios/historico-aluno/{alunoId}?inicio=...&fim=...&status=...&turmaId=...` e conferir que o percentual de frequência retornado corresponde à Frequência acumulada do aluno dividida pelas aulas Realizadas no filtro.

**Acceptance Scenarios**:

1. **Given** um aluno e um período informados, **When** a consulta é feita, **Then** o sistema retorna as aulas em que o aluno esteve vinculado dentro do período, com Data, horários, Matéria, Turma e Status de cada uma.
2. **Given** o mesmo aluno, **When** o cálculo de frequência é realizado e algum filtro (período, status ou turma) restringe o conjunto de aulas consideradas, **Then** o percentual retornado usa as presenças reais do aluno dentro desse mesmo filtro como numerador — nunca a Frequência acumulada (vitalícia) — garantindo que o resultado nunca ultrapasse 100% (correção aplicada em [specs/033-fix-percentual-frequencia](../033-fix-percentual-frequencia/spec.md); antes dessa correção, o numerador era sempre a Frequência vitalícia, podendo gerar percentuais acima de 100% quando o filtro era mais restrito que o histórico total do aluno). Sem nenhum filtro restritivo, o percentual continua usando a Frequência acumulada do aluno como numerador.

---

### Edge Cases

- Existe alguma tela hoje que mostra o histórico de aulas de um aluno específico? Não foi encontrada nenhuma tela equivalente no frontend que consuma este endpoint ou ofereça a mesma informação combinada (histórico + frequência).
- O cálculo de frequência é isolado ao período filtrado? Sim, quando há filtro restritivo (período, status ou turma) — o numerador passa a ser a contagem de presenças reais do aluno dentro do mesmo filtro que já delimita o denominador (aulas Realizadas), não mais o contador vitalício `Aluno.Frequencia` (correção de [specs/033-fix-percentual-frequencia](../033-fix-percentual-frequencia/spec.md)). Sem nenhum filtro restritivo, o numerador continua sendo `Aluno.Frequencia`.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O sistema MUST permitir consultar o histórico de aulas de um aluno específico, filtrável por período, status e turma, retornando Data, horários, Matéria, Turma e Status de cada aula.
- **FR-002**: O sistema MUST calcular um percentual de frequência do aluno como a razão entre presenças reais do aluno e a quantidade de aulas com Status "Realizada", ambas contadas dentro do mesmo filtro aplicado, sempre que algum filtro (período, status ou turma) restringir o conjunto de aulas consideradas; sem nenhum filtro restritivo, o numerador é a Frequência acumulada (contador vitalício) do aluno (correção de [specs/033-fix-percentual-frequencia](../033-fix-percentual-frequencia/spec.md)).

### Key Entities *(include if feature involves data)*

- Não introduz entidades próprias — projeta dados de Aluno e Aula (ver [Gerenciar Alunos](../003-gerenciar-alunos/spec.md) e [Gerenciar Aula](../005-gerenciar-aula/spec.md)).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A API retorna corretamente o histórico filtrado e o percentual de frequência calculado para qualquer aluno existente.

## Assumptions

- Esta funcionalidade é mantida no backend como capacidade disponível, mas não é uma funcionalidade ativa do produto hoje, por ausência de tela consumidora.
- O uso de um contador vitalício de Frequência no numerador foi corrigido: com qualquer filtro restritivo aplicado (período, status ou turma), o numerador passa a ser as presenças reais do aluno dentro do mesmo filtro que já delimita o denominador, eliminando percentuais acima de 100% (ver [specs/033-fix-percentual-frequencia](../033-fix-percentual-frequencia/spec.md)). Sem nenhum filtro restritivo, o contador vitalício continua sendo usado, já que nesse caso ambos coincidem.
