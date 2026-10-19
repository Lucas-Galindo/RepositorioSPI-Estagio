# Data Model: Progresso de Pacote (spec 058)

Sem mudança no banco (FR-011). Estruturas calculadas:

## `ProgressoPacoteResponse`

| Campo | Tipo | Regra |
|---|---|---|
| `total` | int? | `TotalAulasPacote`, ou `dadas + saldo` (estimado); nulo se nenhum dos dois existir |
| `totalEstimado` | bool | true quando o total foi estimado |
| `aulasDadas` | int | presenças com este vínculo |
| `aulasRestantes` | int? | `max(0, total − dadas)`; nulo sem total |
| `estado` | `"EmDia" \| "Atencao" \| "Esgotado" \| "SemTotal"` | research.md R2 |

`VinculoCobrancaResponse.Progresso` é preenchido só para Pacote. O antigo `AulasDadas` (total − saldo, nunca exibido) é removido.

## `AulaDoPacoteResponse`

`aulaId`, `data`, `horaInicio`, `horaFim`, `materiaNome`, `contexto`, `status`.

## `PacotesResumoResponse` (em `DashboardResponse.Pacotes`)

Os campos são `ativos`, `concluidos`, `proximosDoFim`, `aulasDadas`, `aulasRestantes` e `aulasDadasNoPeriodo`, mais `renovacao: PacoteEmAtencaoResponse[]`.

Cada item da renovação tem `vinculoId`, `alunoId`, `alunoNome` e o `progresso`. O `saldoAulas`/`contexto` antigos saem: o contexto era sempre "Todas as aulas do aluno".
