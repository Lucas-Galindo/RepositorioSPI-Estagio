# Data Model: Remover Índice de Cobertura de Custos Fixos do Backend

Nenhuma entidade de domínio, nenhuma coluna de banco, nenhuma migração. Esta mudança remove uma
única propriedade calculada de um DTO de resposta já existente.

## DTO afetado

### `IndicadoresFinanceirosResponse`
([IndicadoresFinanceirosResponse.cs](../../src/SPI.Application/Dashboard/Dtos/IndicadoresFinanceirosResponse.cs))
— compartilhado por `GET /api/dashboard` e `GET /api/relatorios/indicadores-financeiros`.

**Antes**:

| Campo | Tipo | Observação |
|---|---|---|
| `TaxaInadimplenciaPercentual` | `decimal` | mantido |
| `PrazoMedioAtrasoDias` | `decimal?` | mantido |
| `MargemSegurancaPercentual` | `decimal` | mantido |
| `FluxoCaixaOperacional` | `decimal` | mantido |
| `IndiceCoberturaCustosFixos` | `decimal?` | **removido** — `valorFaturado / valorPago`, arredondado a 2 casas; `null` se `valorPago == 0` |
| `GargaloCaixa` | `GargaloCaixaResponse` | mantido |

**Depois**: mesma lista, sem a linha `IndiceCoberturaCustosFixos`.

Nenhum outro campo muda de nome, tipo ou regra de cálculo (FR-004 da spec).

## Contrato de resposta (API) — mudança de shape

`GET /api/dashboard` (campo `indicadores`) e `GET /api/relatorios/indicadores-financeiros` (campo
`indicadores`, dentro de `IndicadoresFinanceirosFiltradosResponse`) deixam de incluir a chave
`indiceCoberturaCustosFixos` no JSON de resposta. Isso é uma remoção de campo em um contrato já
versionado apenas implicitamente (sem clientes externos conhecidos — ver spec.md, Assumptions),
então não há necessidade de versionamento de API ou período de depreciação.

## Espelho de tipo no frontend

`frontend/lib/api/dashboard.ts`, interface `IndicadoresFinanceiros`, perde o campo
`indiceCoberturaCustosFixos: number | null`. É reaproveitada sem alteração própria por
`frontend/lib/api/relatorios.ts` (`IndicadoresFinanceirosFiltrados.indicadores`), então uma única
edição cobre os dois usos, exatamente como no backend.
