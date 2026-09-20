# Phase 0 Research: Remover Código Morto do Módulo Financeiro

## R1 — Confirmar ausência total de consumidor no frontend (Item 1)

**Decision**: Confirmado via busca em todo `frontend/**/*.ts*` que `dashboard?.indicadores` e `dashboard?.fluxoCaixaMensal` não são lidos em nenhum arquivo — `frontend/app/(app)/dashboard/page.tsx` (único consumidor de `obterDashboard()`) só lê `alunosAtendidosNoPeriodo` e `valorFaturadoNoPeriodo` ([dashboard/page.tsx:53-54](../../frontend/app/(app)/dashboard/page.tsx#L53-L54)). Re-confirmado após as remoções das specs 030/031/032 (que já haviam removido indicadores individuais) que nenhum indicador remanescente de `DashboardService.ObterIndicadoresFinanceirosAsync`/`ObterFluxoCaixaMensalAsync` está em uso.

**Rationale**: Base factual para FR-001/FR-002 — confirma que a remoção é segura (sem quebra de consumidor real).

**Alternatives considered**: N/A — investigação, não decisão de design.

## R2 — Escopo exato da remoção do Item 1 (o que fica, o que sai)

**Decision**: Removem-se exclusivamente:
- `DashboardService.ObterIndicadoresFinanceirosAsync` (método privado, linhas 62-94) e `ObterFluxoCaixaMensalAsync` (método privado, linhas 96-119), e suas chamadas em `ObterAsync` (linhas 35-36).
- A constante `MesesFluxoCaixa` ([DashboardService.cs:9](../../src/SPI.Application/Dashboard/Services/DashboardService.cs#L9)) — confirmado, por busca no arquivo, que é usada exclusivamente dentro de `ObterFluxoCaixaMensalAsync` (linha 100), logo fica órfã e deve sair junto.
- As propriedades `Indicadores` e `FluxoCaixaMensal` de `DashboardResponse`.
- Os campos `indicadores`/`fluxoCaixaMensal` da interface TypeScript `Dashboard` em `frontend/lib/api/dashboard.ts`.

**NÃO são removidos** (confirmado em uso por `RelatorioService.ObterIndicadoresFinanceirosAsync`, que alimenta a aba "Indicadores" ativa do Relatório Financeiro):
- Os tipos DTO `IndicadoresFinanceirosResponse`, `FluxoCaixaMensalItem`, `GargaloCaixaResponse` (backend) e `IndicadoresFinanceiros`, `FluxoCaixaMensalItem`, `GargaloCaixa` (frontend) — continuam sendo o shape de retorno de `IndicadoresFinanceirosFiltradosResponse`/`obterIndicadoresFinanceiros`.
- Os métodos de `IRelatorioRepository` chamados pelos métodos removidos (`ObterInadimplenciaNoPeriodoAsync`, `ObterPagamentosComAtrasoNoPeriodoAsync`, `ObterValorPagoNoPeriodoAsync`, `ObterEntradasPorDiaDoMesAsync`, `ObterSaidasPorDiaDoMesAsync`) — confirmado, por leitura direta de `RelatorioService.cs:196-291`, que `RelatorioService.ObterIndicadoresFinanceirosAsync` chama todos eles também.

**Rationale**: Evita o erro de remover tipos/métodos compartilhados só porque um dos dois consumidores (Dashboard) parou de usá-los — o outro consumidor (RelatorioService/aba Indicadores) continua ativo.

**Alternatives considered**: Remover também os tipos DTO e duplicar um novo DTO menor só para `RelatorioService` — rejeitado; não há necessidade, os tipos já são genéricos o suficiente para o único consumidor restante.

## R3 — Confirmar ausência total de chamador de ObterValorAPagarAsync (Item 2)

**Decision**: Confirmado via busca em todo `src/` e `tests/` (excluindo artefatos de build) que `ObterValorAPagarAsync` só aparece em 3 lugares: a declaração na interface, a implementação no repositório, e os 3 stubs obrigatórios (interface completa) nos fakes de teste `RelatorioServiceFinanceiroPorTurmaTests.cs`, `RelatorioServiceLancamentosTests.cs`, `RelatorioServiceFinanceiroPendenteTests.cs` — nenhum deles chama o método, apenas o implementam para satisfazer `IRelatorioRepository`.

**Rationale**: Base factual para FR-005 — os 3 stubs devem ser removidos junto, já que deixam de ser exigidos pela interface e não servem a nenhum propósito depois da remoção.

**Alternatives considered**: N/A — investigação, não decisão de design.

## R4 — Cobertura de teste existente para os métodos removidos

**Decision**: Confirmado, por busca em `tests/`, que não existe nenhum teste dedicado a `DashboardService.ObterAsync`/`ObterIndicadoresFinanceirosAsync`/`ObterFluxoCaixaMensalAsync` hoje — são métodos privados, só alcançáveis via `ObterAsync`, que também não tem teste próprio. A validação desta feature é, portanto, negativa por natureza: confirmar que a suíte completa continua passando (FR-006) e que nenhuma referência aos símbolos removidos permanece no código (SC-005, verificável por busca).

**Rationale**: Não há teste a atualizar para o comportamento de `DashboardService` (diferente do Item 2, onde os 3 stubs precisam ser removidos). Não se justifica adicionar teste novo para código que está sendo excluído.

**Alternatives considered**: N/A.
