# Research: Progresso de Pacote (spec 058)

## R1 — Fonte de "aulas dadas" (decisão do usuário)

**Decision**: aulas dadas = `COUNT(aula_aluno WHERE vinculo_cobranca_id = pacote)`. Só presenças confirmadas recebem `vinculo_cobranca_id` (`AulaService.GerarContasAReceberAsync`); faltas e aulas canceladas nunca o recebem, então não entram (FR-001).

Restantes = `max(0, total − dadas)`.

**Rationale**: decisão do usuário de 2026-10-19 (opção A). Ela substitui o "total − saldo" da spec 052.

## R2 — Cálculo único

**Decision**: `ProgressoPacote.Calcular(VinculoCobranca pacote, int aulasDadas)` é estático e puro. Devolve `{ Total, TotalEstimado, AulasDadas, AulasRestantes, Estado }`:

- **Total**: `TotalAulasPacote`. Se estiver vazio e o saldo for conhecido, usa `aulasDadas + SaldoAulas` e marca `TotalEstimado = true`. Sem nenhum dos dois, fica nulo.
- **Estado**:
  - `SemTotal`, quando o total é nulo;
  - `Esgotado`, com 0 restantes;
  - `Atencao`, com restantes até `VinculoCobranca.LimiteAlertaPacote` (2);
  - `EmDia`, nos demais casos.

**Rationale**: FR-002/FR-003/FR-004; ficha e Home chamam a mesma função.

## R3 — Contagem em lote

**Decision**: `IAulaRepository.ContarPresencasPorVinculosAsync(IReadOnlyCollection<int> vinculoIds, DateOnly? de, DateOnly? ate)` → `Dictionary<int, int>`, feita com `GROUP BY`. Uma chamada com o período para "aulas dadas no período" e outra sem período para o progresso.

`ListarPresencasDoVinculoAsync(vinculoId)` traz as presenças com `Aula.Materia` e `Aula.Turma`, mais recentes primeiro, para a lista de aulas.

**Rationale**: evita uma consulta por pacote na listagem e no dashboard.

## R4 — Cadastro e edição

**Decision**:
- **Cadastro**: um Pacote com `TotalAulasPacote` informado e `SaldoAulas` vazio nasce com `SaldoAulas = TotalAulasPacote`.
- **Edição**: a validação "total ≥ aulas dadas" passa a usar a contagem de presenças.
- **Formulário**: "Total de aulas" é sempre exibido para o Pacote. O "Saldo de aulas" só aparece na edição, porque é o ajuste manual do bloqueio (spec 041).

## R5 — Dashboard

**Decision**: `GET /api/dashboard?periodoInicio&periodoFim&alunoId`. `DashboardResponse.PacotesEmAtencao` é substituído por `Pacotes`:

| Campo | Conteúdo |
|---|---|
| `ativos` | pacotes ativos de alunos ativos, com estado diferente de Esgotado |
| `concluidos` | pacotes (ativos ou não) com estado Esgotado |
| `proximosDoFim` | ativos com estado Atenção |
| `aulasDadas` | soma das dadas dos pacotes ativos |
| `aulasRestantes` | soma das restantes dos pacotes ativos |
| `aulasDadasNoPeriodo` | presenças, em qualquer pacote filtrado, dentro do período |
| `renovacao` | lista dos pacotes ativos de alunos ativos com estado Esgotado ou Atenção |

A lista `renovacao` vem com os esgotados primeiro, depois menos restantes, depois o nome do aluno. Ela substitui o painel por saldo.

`alunoId` filtra todos esses números. `IVinculoCobrancaRepository.ListarPacotesAsync(int? alunoId)` traz pacotes ativos e inativos com o `Aluno`.

**Rationale**: "dashboard e visão individual mostram os mesmos números".

## R6 — Lista de aulas do pacote

**Decision**: `GET /api/alunos/{alunoId}/vinculos-cobranca/{id}/aulas` devolve `AulaDoPacoteResponse { aulaId, data, horaInicio, horaFim, materiaNome, contexto, status }`. Responde 404 se o vínculo não for do aluno.

O contexto é a turma, ou "Atendimento individual". As presenças só existem em aulas "Realizadas", mas o status é exibido mesmo assim, como o card pede.
