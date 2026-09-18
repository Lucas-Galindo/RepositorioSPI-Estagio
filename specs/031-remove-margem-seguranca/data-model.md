# Data Model: Remover Indicador "Margem de Segurança %"

Nenhuma entidade de domínio, nenhuma coluna de banco, nenhuma migração. Esta mudança remove uma
propriedade calculada de um DTO de resposta já existente, e o elemento visual correspondente.

## DTO afetado

### `IndicadoresFinanceirosResponse`
([IndicadoresFinanceirosResponse.cs](../../src/SPI.Application/Dashboard/Dtos/IndicadoresFinanceirosResponse.cs))
— compartilhado por `GET /api/dashboard` e `GET /api/relatorios/indicadores-financeiros`.

**Antes**:

| Campo | Tipo | Observação |
|---|---|---|
| `TaxaInadimplenciaPercentual` | `decimal` | mantido |
| `PrazoMedioAtrasoDias` | `decimal?` | mantido |
| `MargemSegurancaPercentual` | `decimal` | **removido** — `FluxoCaixaOperacional / ValorFaturado × 100`, arredondado a 1 casa; `0` se `ValorFaturado == 0` |
| `FluxoCaixaOperacional` | `decimal` | mantido (indicador independente, usado também pelo campo removido como numerador, mas não depende dele) |
| `GargaloCaixa` | `GargaloCaixaResponse` | mantido |

**Depois**: mesma lista, sem a linha `MargemSegurancaPercentual`. Nota: `IndiceCoberturaCustosFixos`
já não existe desde a spec 030 — este DTO chega a 4 campos + `GargaloCaixa` depois desta mudança
(era 6 antes da spec 030).

Nenhum outro campo muda de nome, tipo ou regra de cálculo (FR-006 da spec).

## Contrato de resposta (API) — mudança de shape

`GET /api/dashboard` (campo `indicadores`) e `GET /api/relatorios/indicadores-financeiros` (campo
`indicadores`, dentro de `IndicadoresFinanceirosFiltradosResponse`) deixam de incluir a chave
`margemSegurancaPercentual` no JSON de resposta. Sem clientes externos conhecidos (spec.md,
Assumptions), sem necessidade de versionamento de API ou depreciação.

## Espelho de tipo no frontend

`frontend/lib/api/dashboard.ts`, interface `IndicadoresFinanceiros`, perde o campo
`margemSegurancaPercentual: number`. Reaproveitada sem alteração própria por
`frontend/lib/api/relatorios.ts` (`IndicadoresFinanceirosFiltrados.indicadores`).

## Elemento visual removido

`frontend/app/(app)/relatorios/financeiro/page.tsx`, dentro da fileira `<div className="kpi-row
kpi-row-3">` (aba "Visão de Indicadores"):

**Antes**: 3 cards — Inadimplência, Prazo médio de atraso, Margem de segurança (classe `kpi-row-3`,
grid de 3 colunas).

**Depois**: 2 cards — Inadimplência, Prazo médio de atraso (classe `kpi-row-2`, grid de 2 colunas
já existente em `frontend/styles/dashboard.css` — ver [research.md — R3](./research.md#r3--padrão-de-reflow-visual-para-a-fileira-de-2-cards-restantes)).
