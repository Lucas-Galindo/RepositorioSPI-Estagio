# Data Model: Corrigir Cálculo de "% Frequência do Aluno"

Nenhuma entidade nova, nenhuma coluna de banco, nenhuma migração. Esta mudança corrige a lógica
de agregação de um método já existente, usando campos já existentes de `Aula`/`AulaAluno`/`Aluno`.

## Campos usados (já existentes)

| Campo | Entidade | Uso na correção |
|---|---|---|
| `AulaAluno.Presente` | `AulaAluno` | `bool?` — `true` quando o aluno esteve presente numa aula `Realizada`; fonte do numerador corrigido. Já carregado por `AulaRepository.ListarAsync` (ver [research.md — R1](./research.md#r1--confirmar-que-os-dados-do-numerador-correto-já-estão-disponíveis-sem-consulta-nova)), hoje descartado no mapeamento. |
| `Aula.Status` | `Aula` | Usado para o denominador (`"Realizada"`) — já usado hoje, sem alteração. |
| `Aluno.Frequencia` | `Aluno` | Contador vitalício — mantido como numerador apenas quando nenhum filtro restringe a consulta (ver R3). |

## Regra de cálculo (comportamento novo)

Para uma chamada com `alunoId` fixo e filtros opcionais `inicio`, `fim`, `status`, `turmaId`:

1. Buscar as aulas do aluno que atendem aos filtros (mesma consulta já existente).
2. `totalRealizadasNoFiltro` = quantidade dessas aulas com `Status == "Realizada"` (sem alteração).
3. `temFiltroRestritivo` = `inicio` OU `fim` OU `status` OU `turmaId` informado (ver [research.md — R3](./research.md#r3--regra-exata-de-quando-usar-presença-real-vs-contador-vitalício)).
4. Numerador:
   - Se `temFiltroRestritivo`: contagem de aulas `Realizada` no filtro em que `AulaAluno.Presente == true` para o `alunoId` consultado.
   - Senão: `Aluno.Frequencia` (comportamento atual, inalterado).
5. `PercentualFrequencia` = `totalRealizadasNoFiltro == 0 ? 0 : Round(numerador / totalRealizadasNoFiltro * 100, 1)` (proteção contra divisão por zero já existente, mantida).

## Contrato de resposta (API) — sem alteração de shape

`RelatorioHistoricoAlunoResponse.PercentualFrequencia` continua sendo `decimal`, mesmo campo,
mesmo endpoint (`GET /api/relatorios/historico-aluno/{alunoId}`). Apenas o valor calculado muda
para os casos com filtro restritivo. `Aulas` (lista de `RelatorioAgendaItem`) permanece idêntica.
