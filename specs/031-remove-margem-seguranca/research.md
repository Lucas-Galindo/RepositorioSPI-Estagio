# Phase 0 Research: Remover Indicador "Margem de Segurança %"

## R1 — Confirmar todos os pontos de existência do indicador

**Decision**: O indicador existe em exatamente 5 lugares no código-fonte atual:
1. `IndicadoresFinanceirosResponse.MargemSegurancaPercentual` — [IndicadoresFinanceirosResponse.cs:25](../../src/SPI.Application/Dashboard/Dtos/IndicadoresFinanceirosResponse.cs#L25) (propriedade do DTO compartilhado).
2. `DashboardService.ObterIndicadoresFinanceirosAsync` — cálculo em [DashboardService.cs:75](../../src/SPI.Application/Dashboard/Services/DashboardService.cs#L75) e atribuição em [DashboardService.cs:86](../../src/SPI.Application/Dashboard/Services/DashboardService.cs#L86).
3. `RelatorioService.ObterIndicadoresFinanceirosAsync` — cálculo em [RelatorioService.cs:206](../../src/SPI.Application/Relatorios/Services/RelatorioService.cs#L206) e atribuição em [RelatorioService.cs:218](../../src/SPI.Application/Relatorios/Services/RelatorioService.cs#L218).
4. `frontend/lib/api/dashboard.ts`, interface `IndicadoresFinanceiros` — campo `margemSegurancaPercentual: number`, reaproveitado por `frontend/lib/api/relatorios.ts` (`IndicadoresFinanceirosFiltrados.indicadores`) sem declaração própria.
5. `frontend/app/(app)/relatorios/financeiro/page.tsx`, linhas 369-377 — bloco JSX do card "Margem de segurança", terceiro item dentro de `<div className="kpi-row kpi-row-3">` (linha 350), ao lado dos cards "Inadimplência" e "Prazo médio de atraso".

**Rationale**: Mesma estrutura já mapeada pela spec 030 para "Cobertura de custos" — DTO compartilhado pelos dois services, um mirror de tipo no frontend. A diferença é que aqui existe também um card visual real (item 5), que "Cobertura de custos" já não tinha (removido pela spec 024).

**Alternatives considered**: Nenhuma — superfície de mudança pequena e totalmente enumerável.

## R2 — Confirmação de que o Dashboard (`/dashboard`) não exibe o indicador

**Decision**: Confirmado por busca em `frontend/app/(app)/dashboard/page.tsx`: nenhuma referência a `margemSeguranca` nem a `indicadores.` nesse arquivo. A tela Dashboard consome `GET /api/dashboard` (que inclui `indicadores.margemSegurancaPercentual` na resposta), mas nunca renderiza esse sub-objeto — mesmo comportamento já documentado em `specs/011-dashboard/spec.md` ("a API calcula mais indicadores do que a Home exibe").

**Rationale**: A premissa do pedido original ("remover a exibição do card no Dashboard") não corresponde ao estado real do sistema — não existe esse card para remover. A spec já documenta essa correção explicitamente; esta pesquisa apenas confirma a verificação com uma segunda leitura direta do arquivo antes de planejar.

**Alternatives considered**: N/A — é uma verificação factual, não uma decisão de design.

## R3 — Padrão de reflow visual para a fileira de 2 cards restantes

**Decision**: Reaproveitar a classe CSS já existente `.kpi-row-2` ([frontend/styles/dashboard.css:365-367](../../frontend/styles/dashboard.css#L365-L367)), que já implementa `grid-template-columns: repeat(2, 1fr)` — o mesmo mecanismo usado por `.kpi-row-3` (usada hoje) e já consumida por outras telas do projeto (`frontend/app/(app)/financeiro/page.tsx`). Trocar a classe do container de `kpi-row kpi-row-3` para `kpi-row kpi-row-2` ao remover o terceiro `<div className="kpi">` resolve o reflow sem CSS novo.

**Rationale**: Não é necessário criar nenhuma regra de layout nova — o padrão de grid responsivo por número de colunas já existe e já é usado em produção para 2, 3 e 4 cards (`.kpi-row` base já é `repeat(4, 1fr)`). Isso também resolve FR-002/SC-002 da spec (distribuição equilibrada, sem vão vazio) com uma mudança de uma linha de classe CSS, análoga à que a spec 024 fez para o caso de 4→3 cards (mesmo componente, `kpi-row`/`kpi-row-3`, usado por 024 na época).

**Alternatives considered**: Criar uma nova classe CSS específica — rejeitado, redundante com `.kpi-row-2` já existente.

## R4 — Nenhum teste automatizado cobre o indicador hoje

**Decision**: Confirmado (busca por `MargemSegurancaPercentual`/`margemSeguranca` em `tests/`) que nenhum teste de unidade existente referencia esse campo. Nenhum teste precisa ser removido ou ajustado.

**Rationale**: Mesma situação já verificada pela spec 030 para o indicador irmão — reduz o risco de regressão de teste nesta remoção.

## R5 — Specs retroativas que precisam de atualização (Princípio V)

**Decision**: Apenas `specs/015-relatorio-financeiro/spec.md` cita "Margem de segurança" nominalmente como comportamento atual (User Story 3, Acceptance Scenario 1, FR-005) e precisa ser atualizada — mesmos três pontos já tocados pela spec 030 para "Cobertura de custos" nesse mesmo arquivo. `specs/024-remover-card-cobertura-custos/spec.md` cita "Margem de segurança" quatro vezes, mas sempre como parte da descrição do estado dos "3 cards restantes" *daquela* mudança já concluída (por exemplo, FR-002 de 024: "O sistema MUST continuar exibindo os demais cards... exatamente com o mesmo conteúdo... que já têm hoje" — falando do "hoje" de setembro de 2026, na época de 024). Isso continua sendo uma afirmação historicamente correta sobre o que 024 entregou; não precisa de edição. `specs/022-tabela-lancamentos-indicadores/contracts/indicadores-financeiros.md` inclui `margemSegurancaPercentual` num exemplo de payload JSON — não é uma spec retroativa de comportamento (não afirma nada sobre "o que o sistema faz hoje" no sentido do Princípio V), mas fica desatualizada como referência técnica; tratada como atualização de qualidade de documentação opcional (ver spec.md, Assumptions), não obrigatória.

**Rationale**: Mesmo critério de distinção já estabelecido pela spec 030 entre "spec que afirma comportamento atual" (precisa atualizar) e "spec que registra o que uma mudança passada entregou" (não precisa, permanece correta).

**Alternatives considered**: Atualizar as três specs por uniformidade — rejeitado pelo mesmo motivo já usado em 030 (edição desnecessária em documentos já corretos/neutros).
