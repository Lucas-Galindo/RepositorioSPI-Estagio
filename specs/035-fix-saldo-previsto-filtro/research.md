# Phase 0 Research: Corrigir Filtro de Receita/Despesa Pendente no Relatório Financeiro

## R1 — Confirmar que a semântica de filtro por id já existe e é a mesma usada por TotalRecebido

**Decision**: `RelatorioRepository.cs` já tem um helper privado `AplicarFiltroReceita(IQueryable<Pagamento>, int? turmaId, int? materiaId, int? alunoId)` ([RelatorioRepository.cs:310-325](../../src/SPI.Infrastructure/Repositories/RelatorioRepository.cs#L310-L325)), usado hoje por `ObterValorFaturadoNoPeriodoAsync`. Comparado linha a linha com o filtro inline de `ListarPagosNoPeriodoAsync` (usado por `TotalRecebido`, [RelatorioRepository.cs:283-293](../../src/SPI.Infrastructure/Repositories/RelatorioRepository.cs#L283-L293)), a lógica é idêntica: `alunoId` filtra por `AlunoId` exato; `turmaId` filtra por `Aluno.AlunosTurma.Any(at => at.TurmaId == turmaId)`; `materiaId` filtra por `PagamentosAula.Any(pa => pa.Aula.MateriaId == materiaId)`.

**Rationale**: Confirma que basta reaproveitar `AplicarFiltroReceita` em `ObterReceitasPendentesSegregadasAsync` para obter exatamente a mesma semântica de filtro que `TotalRecebido` já usa — sem inventar uma nova regra de associação aluno/turma/matéria, e sem risco de divergência entre os dois lados do cálculo (FR-001, Constraints do plan.md).

**Alternatives considered**: Usar a variante por nome já existente (`AplicarFiltroReceitaPorNome`, usada pela Visão Geral) traduzindo os ids recebidos para nome antes de filtrar — rejeitada: exigiria uma consulta extra para resolver id→nome, introduziria risco de correspondência imprecisa (`Contains`, não igualdade exata) e uma turma/matéria/aluno renomeado ou com nome duplicado poderia produzir um resultado diferente do que `TotalRecebido` (filtrado por id) mostra — violaria a garantia de FR-001 de que os dois valores usam o mesmo filtro.

## R2 — Escopo da mudança de assinatura

**Decision**: Estender apenas `IRelatorioRepository.ObterReceitasPendentesSegregadasAsync` (interface + implementação) com os 3 parâmetros opcionais `int? turmaId = null, int? materiaId = null, int? alunoId = null`, na mesma posição/estilo dos parâmetros já existentes de `ObterValorFaturadoNoPeriodoAsync`. `ObterDespesasPendentesSegregadasAsync` **não muda** — sem parâmetro novo, mantendo `DespesaPendente` sempre global (FR-002).

**Rationale**: Menor mudança de superfície possível que resolve o FR-001 sem afetar nenhum outro chamador do método (`FinanceiroService.ObterVisaoGeralAsync`, que já usa os parâmetros de nome existentes e não passa ids — chamada inalterada, pois os novos parâmetros são opcionais).

**Alternatives considered**: Criar um método novo dedicado (`ObterReceitasPendentesSegregadasPorIdAsync`) em vez de estender o existente — rejeitado por duplicar a mesma query com uma pequena variação de filtro, quando parâmetros opcionais já resolvem isso sem duplicação (mesmo padrão já usado em `ObterValorFaturadoNoPeriodoAsync`, que aceita tanto filtros por id quanto por nome no mesmo método).

## R3 — `RelatorioService.ObterFinanceiroAsync`: repasse dos filtros já recebidos

**Decision**: A chamada em `RelatorioService.cs:124` passa a incluir `turmaId, materiaId, alunoId` (os mesmos três parâmetros que o método já recebe e já usa para `ListarPagosNoPeriodoAsync` na linha 114) na chamada a `ObterReceitasPendentesSegregadasAsync`. A chamada a `ObterDespesasPendentesSegregadasAsync` (linha 125) permanece sem argumentos.

**Rationale**: Nenhum dado novo é necessário — os três filtros já chegam como parâmetros do próprio método, e já são usados pela consulta de `TotalRecebido` logo acima na mesma função.

## R4 — Cobertura de teste existente e fakes a atualizar

**Decision**: Confirmado que `ObterFinanceiroAsync` já tem cobertura de teste em `RelatorioServiceFinanceiroPorTurmaTests.cs` (specs/029), e que **dois** arquivos de teste (`RelatorioServiceFinanceiroPorTurmaTests.cs` e `RelatorioServiceLancamentosTests.cs`) implementam `FakeRelatorioRepository : IRelatorioRepository` com um método `ObterReceitasPendentesSegregadasAsync` próprio. Ao estender a interface com os 3 novos parâmetros opcionais, **ambos os fakes precisam da assinatura atualizada** para continuar compilando (mesmo tipo de ajuste não planejado encontrado na spec 032 ao estender `ObterValorPendenteAsync`) — registrado aqui preventivamente para não ser "não planejado" desta vez.

**Rationale**: Antecipar essa atualização em `tasks.md` evita descobrir o erro de compilação (`CS0535`) só depois da implementação, como aconteceu na spec 032.

**Alternatives considered**: N/A — atualizar os fakes é a única forma de manter o projeto compilando após estender uma interface implementada por múltiplos test doubles manuais (sem biblioteca de mock no projeto de testes).
