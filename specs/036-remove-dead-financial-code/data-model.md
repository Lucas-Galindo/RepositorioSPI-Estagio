# Data Model: Remover Código Morto do Módulo Financeiro

Nenhuma entidade nova, nenhuma coluna de banco, nenhuma migração. Esta mudança remove código já
existente confirmado sem nenhum consumidor — o "modelo de dados" relevante aqui é o inventário
exato do que sai e do que fica.

## Inventário de remoção

| Item | Arquivo | O que remove | Fica ou sai? |
|---|---|---|---|
| 1 | `src/SPI.Application/Dashboard/Services/DashboardService.cs` | Método privado `ObterIndicadoresFinanceirosAsync` (linhas 62-94) | Sai |
| 1 | idem | Método privado `ObterFluxoCaixaMensalAsync` (linhas 96-119) | Sai |
| 1 | idem | Constante `MesesFluxoCaixa` (linha 9) — órfã após remoção acima | Sai |
| 1 | idem | Chamadas a ambos em `ObterAsync` (linhas 35-36) | Sai |
| 1 | `src/SPI.Application/Dashboard/Dtos/DashboardResponse.cs` | Propriedades `Indicadores`, `FluxoCaixaMensal` | Sai |
| 1 | `frontend/lib/api/dashboard.ts` | Campos `indicadores`, `fluxoCaixaMensal` da interface `Dashboard` | Sai |
| 2 | `src/SPI.Domain/Repositories/IRelatorioRepository.cs` | `ObterValorAPagarAsync` (linha 47) | Sai |
| 2 | `src/SPI.Infrastructure/Repositories/RelatorioRepository.cs` | Implementação de `ObterValorAPagarAsync` (linhas 96-107) | Sai |
| 2 | 3 arquivos em `tests/SPI.Application.Tests/Relatorios/` | Stub de `ObterValorAPagarAsync` em cada `FakeRelatorioRepository` | Sai |

## Explicitamente preservado (não fazem parte desta remoção)

| Item | Onde | Por quê |
|---|---|---|
| `IndicadoresFinanceirosResponse`, `FluxoCaixaMensalItem`, `GargaloCaixaResponse` (C#) | `SPI.Application.Dashboard.Dtos` | Reaproveitados por `RelatorioService.ObterIndicadoresFinanceirosAsync` via `IndicadoresFinanceirosFiltradosResponse` |
| `IndicadoresFinanceiros`, `FluxoCaixaMensalItem`, `GargaloCaixa` (TypeScript) | `frontend/lib/api/dashboard.ts` | Importados por `frontend/lib/api/relatorios.ts` (`IndicadoresFinanceirosFiltrados`) |
| `ObterInadimplenciaNoPeriodoAsync`, `ObterPagamentosComAtrasoNoPeriodoAsync`, `ObterValorPagoNoPeriodoAsync`, `ObterEntradasPorDiaDoMesAsync`, `ObterSaidasPorDiaDoMesAsync` | `IRelatorioRepository`/`RelatorioRepository` | Chamados por `RelatorioService.ObterIndicadoresFinanceirosAsync` (aba Indicadores, tela ativa) |
| Demais campos de `DashboardResponse` (`TotalAulasAgendadasNoPeriodo`, `TotalAlunosAtivos`, `AlunosAtendidosNoPeriodo`, `TotalTurmasAtivas`, `ValorPendenteRecebimento`, `ValorFaturadoNoPeriodo`, `ProximasAulasHoje`, `LembretesPendentes`) | `DashboardResponse.cs` | Fora do escopo desta feature — não confirmados como mortos, alguns usados pela Home |

## Contrato de resposta (API) — mudança escopada

`GET /api/dashboard` deixa de retornar os campos `indicadores`/`fluxoCaixaMensal` — uma redução
de shape, não uma quebra, já que nenhum consumidor real os lê (FR-002, SC-001). `GET
/api/relatorios/indicadores-financeiros` (usado pela aba Indicadores) **não muda** — endpoint,
service e DTO completamente separados (FR-003).
