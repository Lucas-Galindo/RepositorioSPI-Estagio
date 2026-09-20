# Data Model: Corrigir Filtro de Receita/Despesa Pendente no Relatório Financeiro

Nenhuma entidade nova, nenhuma coluna de banco, nenhuma migração. Esta mudança corrige a
aplicação de filtros já existentes (`alunoId`, `turmaId`, `materiaId`) a uma consulta que já
existe, usando um helper de filtro (`AplicarFiltroReceita`) já implementado e já usado por outro
método do mesmo repositório.

## Assinatura alterada

`IRelatorioRepository.ObterReceitasPendentesSegregadasAsync`:

```text
Antes:
Task<(decimal AVencer, decimal Atrasado)> ObterReceitasPendentesSegregadasAsync(
    CancellationToken cancellationToken = default,
    string? turmaNome = null, string? materiaNome = null, string? alunoBusca = null);

Depois (3 parametros novos, opcionais, mesma posicao dos usados em ObterValorFaturadoNoPeriodoAsync):
Task<(decimal AVencer, decimal Atrasado)> ObterReceitasPendentesSegregadasAsync(
    CancellationToken cancellationToken = default,
    int? turmaId = null, int? materiaId = null, int? alunoId = null,
    string? turmaNome = null, string? materiaNome = null, string? alunoBusca = null);
```

`ObterDespesasPendentesSegregadasAsync` — **sem alteração de assinatura** (FR-002).

## Regra de filtro (reaproveitada, não nova)

| Filtro | Campo/relação usada | Origem |
|---|---|---|
| `alunoId` | `Pagamento.AlunoId == alunoId` | `AplicarFiltroReceita` (já existe) |
| `turmaId` | `Pagamento.Aluno.AlunosTurma.Any(at => at.TurmaId == turmaId)` | `AplicarFiltroReceita` (já existe) |
| `materiaId` | `Pagamento.PagamentosAula.Any(pa => pa.Aula.MateriaId == materiaId)` | `AplicarFiltroReceita` (já existe) |

Idêntica à regra já usada por `ListarPagosNoPeriodoAsync` para `TotalRecebido` (ver research.md —
R1) — garante que `ReceitaPendente` e `TotalRecebido` sempre concordam sobre "quais pagamentos
pertencem a este aluno/turma/matéria".

## Fluxo de dados (comportamento novo)

Para uma chamada a `GET /api/relatorios/financeiro` com `alunoId`/`turmaId`/`materiaId` opcionais:

1. `TotalRecebido`/`SaldoRealizado`: já filtrados hoje (inalterado).
2. `ReceitaPendente`: `RelatorioService.ObterFinanceiroAsync` passa a repassar os mesmos 3
   filtros recebidos para `ObterReceitasPendentesSegregadasAsync`, que agora os aplica via
   `AplicarFiltroReceita` antes de somar `AVencer`/`Atrasado`.
3. `DespesaPendente`: continua vindo de `ObterDespesasPendentesSegregadasAsync()` sem nenhum
   argumento — sempre o total do negócio (ContaPagar não tem vínculo com aluno/turma/matéria).
4. `SaldoPrevisto = SaldoRealizado + (ReceitaPendente - DespesaPendente)` — cálculo já existente
   (linha 138), sem alteração de fórmula; só os dois operandos de entrada mudam de valor quando
   há filtro.

## Contrato de resposta (API) — sem alteração de shape

`RelatorioFinanceiroResponse` continua com os mesmos campos (`TotalRecebido`, `TotalPago`,
`SaldoRealizado`, `ReceitaPendente`, `DespesaPendente`, `SaldoPrevisto`, `PorFormaPagamento`,
`PorAluno`, `PorTurma`) e o controller (`GET /api/relatorios/financeiro`) não ganha nenhum
parâmetro de query novo — `alunoId`/`turmaId`/`materiaId` já são aceitos hoje.
