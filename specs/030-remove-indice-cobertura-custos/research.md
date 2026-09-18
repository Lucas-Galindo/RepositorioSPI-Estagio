# Phase 0 Research: Remover Índice de Cobertura de Custos Fixos do Backend

## R1 — Confirmar todos os pontos de existência do campo

**Decision**: O campo existe em exatamente 4 lugares no código-fonte atual (confirmado por busca de texto em todo o repositório antes de escrever a spec e novamente antes do plano):
1. `IndicadoresFinanceirosResponse.IndiceCoberturaCustosFixos` — [IndicadoresFinanceirosResponse.cs:31](../../src/SPI.Application/Dashboard/Dtos/IndicadoresFinanceirosResponse.cs#L31) (propriedade do DTO).
2. `DashboardService.ObterIndicadoresFinanceirosAsync` — cálculo em [DashboardService.cs:76](../../src/SPI.Application/Dashboard/Services/DashboardService.cs#L76) (`decimal? indiceCobertura = ...`) e atribuição em [DashboardService.cs:89](../../src/SPI.Application/Dashboard/Services/DashboardService.cs#L89).
3. `RelatorioService.ObterIndicadoresFinanceirosAsync` — mesmo padrão, cálculo em [RelatorioService.cs:207](../../src/SPI.Application/Relatorios/Services/RelatorioService.cs#L207) e atribuição em [RelatorioService.cs:221](../../src/SPI.Application/Relatorios/Services/RelatorioService.cs#L221).
4. `frontend/lib/api/dashboard.ts`, interface `IndicadoresFinanceiros` — campo `indiceCoberturaCustosFixos: number | null` (linha 17), reaproveitada por `frontend/lib/api/relatorios.ts` (`IndicadoresFinanceirosFiltrados.indicadores: IndicadoresFinanceiros`) sem declaração própria.

**Rationale**: Como o DTO `IndicadoresFinanceirosResponse` é compartilhado pelos dois services (mesmo tipo, `SPI.Application.Dashboard.Dtos`), remover a propriedade dele automaticamente afeta as duas respostas de API assim que as duas atribuições que a preenchem também forem removidas — não há um terceiro service ou DTO paralelo a considerar. O tipo do frontend também é único e compartilhado (não há uma segunda declaração TypeScript para o mesmo conceito).

**Alternatives considered**: Nenhuma — a superfície de mudança é pequena e totalmente enumerável; não há ambiguidade sobre "onde mais" o campo poderia estar.

## R2 — Nenhum teste automatizado cobre o campo hoje

**Decision**: Confirmado (busca por `IndiceCoberturaCustosFixos`/`indiceCobertura` em `tests/`) que nenhum teste de unidade existente referencia esse campo. Nenhum teste precisa ser removido ou ajustado como parte desta mudança.

**Rationale**: Evita o risco de uma remoção "quebrar" um teste que dependia do campo — não existe esse teste.

## R3 — Specs retroativas que precisam de atualização (Princípio V da constituição)

**Decision**: Apenas `specs/015-relatorio-financeiro/spec.md` cita "Cobertura de custos" nominalmente (User Story 3, Acceptance Scenario 1, e FR-005) e precisa ser atualizada. `specs/011-dashboard/spec.md` foi verificada e **não** cita o indicador por nome — só menciona genericamente "indicadores financeiros avançados", remetendo a 015 para detalhe — então não precisa de edição direta. `specs/024-remover-card-cobertura-custos/spec.md` já registra, em suas Assumptions, que a remoção do cálculo no backend seria decidida numa fase de planejamento futura — esta é essa mudança, mas o texto de 024 já é compatível com o resultado (não afirma que o cálculo *continua* existindo para sempre) e não precisa de edição.

**Rationale**: Constitution Principle V exige que specs retroativas não fiquem descrevendo um comportamento que deixou de ser verdade, sem, ao mesmo tempo, exigir reescrever documentação que já está correta ou neutra em relação à mudança.

**Alternatives considered**: Atualizar as três specs por precaução — rejeitado por criar diffs desnecessários em documentos que já estão corretos (011 nunca nomeou o indicador; 024 já é neutra sobre o futuro do cálculo backend).
