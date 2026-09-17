# Research: Remover Card "Cobertura de Custos" da Aba Indicadores

Nenhum `NEEDS CLARIFICATION` no Technical Context do plan.md — as decisões abaixo documentam o
que foi verificado no código antes de planejar, para justificar por que a mudança é tão pequena.

## Decisão 1: Onde vive o card e como removê-lo

**Decision**: O card "Cobertura de custos" é um bloco JSX isolado dentro da fileira `.kpi-row`
em `frontend/app/(app)/relatorios/financeiro/page.tsx` (linhas ~377-391, dentro do bloco
`{aba === "indicadores" && indicadoresDados && (...)}`). Removê-lo é deletar esse bloco `<div
className="kpi">...</div>` inteiro, sem tocar nos 3 cards vizinhos (Inadimplência, Prazo médio
de atraso, Margem de segurança), que são blocos JSX independentes na mesma fileira.

**Rationale**: Cada card é renderizado como um elemento JSX estático e independente (não um
`.map()` sobre uma lista de indicadores) — não há indireção a desfazer, é uma remoção direta de
um bloco de código.

**Alternatives considered**: Esconder o card via CSS (`display: none`) foi descartado — deixaria
o elemento no DOM e no bundle sem necessidade, e não atende ao pedido de "remover" nem à
User Story 2 (realinhar o espaço), já que um elemento com `display: none` ainda ocupa uma célula
do grid CSS a menos que o grid também seja ajustado, tornando a alternativa estritamente pior
que a remoção direta.

## Decisão 2: Como realinhar os 3 cards restantes

**Decision**: Trocar a className da fileira de `"kpi-row"` (grid de 4 colunas,
`grid-template-columns: repeat(4, 1fr)`) para `"kpi-row kpi-row-3"` (aplica
`grid-template-columns: repeat(3, 1fr)`, sobrescrevendo a regra de 4 colunas). Ambas as classes
já existem em `frontend/styles/dashboard.css` (linhas 348-359) e já são usadas juntas em outras
fileiras de indicadores do sistema com 3 cards — não é necessário criar nenhuma CSS nova.

**Rationale**: Reaproveita um padrão já validado e usado em produção no restante do sistema,
consistente com FR-004 da spec (mesmo padrão visual/responsivo já usado pelas demais fileiras).
Nenhuma media query adicional é necessária: `kpi-row-3` já herda o mesmo `gap` e o mesmo
comportamento responsivo de `kpi-row`.

**Alternatives considered**: Definir uma nova classe CSS específica para este caso foi
descartado — duplicaria uma regra (`repeat(3, 1fr)`) que já existe literalmente idêntica em
`kpi-row-3`, violando o princípio de reaproveitamento já seguido no restante do CSS do projeto.

## Decisão 3: O que fazer com o campo `indiceCoberturaCustosFixos` no backend

**Decision**: Não tocar em nada no backend. O campo `IndiceCoberturaCustosFixos` em
`IndicadoresFinanceirosResponse` (`src/SPI.Application/Dashboard/Dtos/IndicadoresFinanceirosResponse.cs`)
é calculado tanto por `RelatorioService` (usado pela tela de Relatório Financeiro) quanto por
`DashboardService.ObterIndicadoresAsync` (usado pela tela de Dashboard) — confirmado via busca
por `IndiceCoberturaCustosFixos` em todo o repositório. Remover o campo do DTO ou do cálculo
quebraria a tela de Dashboard, que não faz parte deste pedido.

**Rationale**: A spec (Assumptions) já previa essa possibilidade e definiu que a remoção é
"apenas de apresentação" nesta mudança. A investigação confirma que o campo é genuinamente
compartilhado, então manter o backend intacto não é só a opção mais simples — é a única correta
sem quebrar outra tela.

**Alternatives considered**: Remover o campo do backend e deixar o Dashboard sem esse
indicador foi descartado — está fora do escopo pedido pelo usuário (que falou especificamente
da aba Indicadores do Relatório Financeiro) e violaria FR-005 da spec ("nenhuma outra tela do
sistema MUST ser alterada por esta mudança").
