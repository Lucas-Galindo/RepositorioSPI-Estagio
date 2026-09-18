# Feature Specification: Corrigir Cálculo de "% Frequência do Aluno"

**Feature Branch**: `033-fix-percentual-frequencia`

**Created**: 2026-09-18

**Status**: Draft

**Input**: User description: "Corrigir o cálculo de '% Frequência do Aluno' (Relatório Histórico do Aluno, Estória 13): hoje o valor é Aluno.Frequencia (contador vitalício, incrementado a cada presença desde sempre) dividido pelo total de aulas Realizadas dentro do filtro de período atual, multiplicado por 100 — o que pode gerar percentuais acima de 100% quando o filtro de período é mais restrito que o histórico total do aluno (ex: contador vitalício de 50 presenças ÷ 10 aulas no mês filtrado = 500%). Correção: o percentual de frequência deve ser calculado dentro do MESMO período do filtro aplicado — presenças reais do aluno no período filtrado ÷ total de aulas Realizadas no mesmo período filtrado × 100, nunca usando o contador vitalício como numerador quando há filtro de período. Quando não há filtro de período (relatório sem recorte de data), o comportamento pode continuar usando o contador vitalício normalmente, já que nesse caso os dois teoricamente coincidem."

## Nota de investigação prévia

Confirmado por leitura do código atual:

- O cálculo vive em `RelatorioService.ObterHistoricoAlunoAsync` ([RelatorioService.cs:49-70](../../src/SPI.Application/Relatorios/Services/RelatorioService.cs#L49-L70)), endpoint `GET /api/relatorios/historico-aluno/{alunoId}`. Hoje: `percentual = Aluno.Frequencia / totalRealizadasNoFiltro * 100`, onde `Aluno.Frequencia` é um contador vitalício (incrementado a cada presença desde o cadastro do aluno, nunca resetado) e `totalRealizadasNoFiltro` é a contagem de aulas com `Status == "Realizada"` dentro dos filtros aplicados (`inicio`, `fim`, `status`, `turmaId`).
- **Este é um bug já documentado como comportamento retroativo aceito**: `specs/013-relatorio-historico-aluno/spec.md` (Acceptance Scenario 2, FR-002, Edge Cases, Assumptions) descreve explicitamente esse comportamento como "não corrigido neste registro retroativo" — esta feature é a correção que aquela spec já antecipava como pendência.
- **Confirmado: este endpoint não tem nenhum consumidor no frontend hoje** (mesma situação já documentada em `specs/013`) — a correção não afeta nenhuma tela ativa, mas corrige um valor incorreto que a API já expõe e que poderia ser consumido a qualquer momento.
- Os dados necessários para o numerador correto já estão disponíveis sem nenhuma consulta nova: `_aulaRepository.ListarAsync(status, turmaId, alunoId, inicio, fim)` (chamado indiretamente via `ObterAgendaAsync`) já retorna as entidades `Aula` completas, incluindo `AulaAlunos` com o campo `Presente` (`bool?`) por aluno — hoje esse dado é descartado no mapeamento para `RelatorioAgendaItem` (que não inclui presença). A correção reaproveita esse mesmo carregamento para contar as presenças reais do aluno dentro do filtro, em vez de descartar essa informação.

## Clarifications

### Session 2026-09-18

- Q: Quando o filtro aplicado usa `turmaId` e/ou `status`, mas **não** usa período (`inicio`/`fim`), o percentual deve ser recalculado por presenças reais (mesma correção) ou pode continuar usando o contador vitalício como numerador? → A: Qualquer filtro restritivo dispara a correção — período, turma ou status isoladamente já disparam o recálculo por presenças reais, eliminando toda ocorrência possível de percentual acima de 100%, não apenas o caso de período citado no pedido original.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Percentual de frequência nunca ultrapassa 100% com filtro aplicado (Priority: P1)

Como consumidora da API de Relatório de Histórico do Aluno (hoje sem tela própria, mas uma capacidade já exposta), eu quero que o percentual de frequência retornado reflita as presenças reais do aluno dentro do período e demais filtros que eu apliquei, para que o número seja sempre interpretável como uma taxa de comparecimento válida (0% a 100%), nunca um valor absurdo como 500%.

**Why this priority**: É o defeito relatado — o valor incorreto pode induzir a uma leitura completamente equivocada da frequência do aluno sempre que qualquer filtro restringe o conjunto de aulas consideradas.

**Independent Test**: Para um aluno com histórico de presenças anterior ao período filtrado, chamar `GET /api/relatorios/historico-aluno/{alunoId}` com um filtro de período mais restrito que o histórico total do aluno, e confirmar que o percentual retornado nunca excede 100% e corresponde exatamente às presenças reais dentro desse período.

**Acceptance Scenarios**:

1. **Given** um aluno com Frequência vitalícia de 50 presenças, mas apenas 8 presenças reais dentro de um período filtrado que teve 10 aulas Realizadas, **When** `GET /api/relatorios/historico-aluno/{alunoId}?inicio=...&fim=...` é consultado com esse período, **Then** o percentual retornado é 80% (8 ÷ 10 × 100), nunca 500% (50 ÷ 10 × 100).
2. **Given** um filtro de `turmaId` e/ou `status` aplicado sem filtro de período, **When** o percentual é calculado, **Then** ele também é recalculado a partir das presenças reais dentro desse filtro — qualquer filtro que restrinja o conjunto de aulas (período, turma ou status) dispara o recálculo, não apenas o filtro de período.
3. **Given** nenhum filtro de período, turma ou status aplicado (consulta ampla, sem nenhum recorte), **When** o percentual é calculado, **Then** o comportamento pode continuar usando a Frequência vitalícia do aluno como numerador, já que nesse caso ela deveria coincidir com a contagem de presenças reais dentro do escopo consultado (todas as aulas do aluno).
4. **Given** qualquer combinação de filtros, **When** o total de aulas Realizadas no filtro é zero, **Then** o percentual retornado é 0% (sem divisão por zero) — mesmo comportamento de proteção já existente hoje.

---

### Edge Cases

- E se o aluno tiver presenças reais dentro do filtro maiores que a Frequência vitalícia (inconsistência de dados)? Não deveria ocorrer no fluxo normal (a Frequência vitalícia é incrementada a cada presença, incluindo as do período filtrado, então sempre é >= presenças de qualquer subperíodo) — fora do escopo tratar essa inconsistência como um caso especial; o cálculo correto (presenças reais ÷ aulas Realizadas no filtro) naturalmente produz um valor válido independentemente disso.
- E se o filtro de período coincidir exatamente com todo o histórico do aluno? O resultado do cálculo por presenças reais e do cálculo por Frequência vitalícia coincidem nesse caso — não há diferença observável.
- Esta correção precisa de alguma mudança de contrato de API (novo campo, novo parâmetro)? Não — `PercentualFrequencia` continua sendo o único campo, com o mesmo tipo e mesmo intervalo esperado (0-100%); só o cálculo interno muda.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O sistema MUST calcular o percentual de frequência do aluno como a razão entre presenças reais do aluno e o total de aulas com Status "Realizada", ambos contados dentro do mesmo conjunto de aulas definido pelos filtros aplicados (período, status, turma), sempre que qualquer um desses filtros restringir o conjunto de aulas consideradas.
- **FR-002**: O sistema MUST continuar aceitando o uso da Frequência acumulada (contador vitalício) do aluno como numerador apenas quando nenhum filtro de período, status ou turma for aplicado (consulta sem nenhum recorte).
- **FR-003**: O sistema MUST NUNCA retornar um percentual de frequência acima de 100%, para qualquer combinação de filtros.
- **FR-004**: O sistema MUST continuar retornando 0% quando o total de aulas Realizadas no filtro aplicado for zero, sem erro de divisão por zero.
- **FR-005**: Esta correção MUST NOT alterar nenhum outro campo da resposta de `GET /api/relatorios/historico-aluno/{alunoId}` (lista de aulas, dados do aluno) nem introduzir nenhum novo parâmetro de filtro.

### Key Entities

- Não introduz entidades novas — usa dados já existentes de `Aula` e `AulaAluno` (especificamente o campo `Presente` por vínculo aluno-aula, já usado desde a Estória 8) e `Aluno.Frequencia` (mantido para o caso sem filtro).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% das consultas com qualquer filtro (período, turma ou status) que restrinja o conjunto de aulas retornam um percentual de frequência entre 0% e 100%, nunca acima disso.
- **SC-002**: Para uma consulta sem nenhum filtro de restrição, o percentual retornado continua idêntico ao comportamento já existente (baseado na Frequência vitalícia).
- **SC-003**: Para o mesmo aluno e mesmo filtro, o percentual corrigido corresponde exatamente à contagem manual de presenças reais dentro do filtro dividida pelo total de aulas Realizadas no mesmo filtro.

## Assumptions

- "Presenças reais dentro do filtro" é contado a partir do vínculo `AulaAluno.Presente == true` para o aluno consultado, restrito às mesmas aulas que já compõem o denominador (mesmos filtros de período/status/turma já aplicados hoje ao endpoint) — não uma nova fonte de dado, apenas reaproveitando informação já carregada e hoje descartada no mapeamento da resposta.
- `specs/013-relatorio-historico-aluno/spec.md` MUST ser atualizada (Princípio V da constituição) para refletir o novo comportamento — essa spec hoje documenta explicitamente o cálculo antigo como comportamento aceito e não corrigido; esta feature é exatamente essa correção.
- Como o endpoint não tem consumidor no frontend hoje (confirmado na investigação prévia), esta correção não requer nenhuma mudança de UI — é uma correção de cálculo em uma capacidade de API já exposta, mas inativa no produto.
- Nenhuma migração de banco de dados é necessária — `Aluno.Frequencia` continua existindo e sendo usado no caso sem filtro; o cálculo alternativo usa dados já persistidos em `AulaAluno.Presente`.
