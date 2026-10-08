# Research: Cards do Painel de Pacotes em Atenção

Decisões técnicas da Phase 0, **revisadas em 2026-10-07** após feedback visual real (prints) sobre a primeira implementação. A investigação prévia do `spec.md` já havia identificado que o sistema não tem nenhum padrão de grade responsiva pré-existente — a primeira passada (R1-R4 originais, abaixo substituídas) resolveu isso com um grid `auto-fit`/`minmax`, mas isso esticava o card até a largura da coluna quando havia poucos pacotes (ex.: 1 card ocupando a largura inteira) e deixava o card alto demais. R1-R3 abaixo já refletem a correção; R4-R6 continuam válidas sem mudança.

## R1. Layout final: fila horizontal de cards de largura fixa, com rolagem própria do painel — zero CSS novo

- **Decision**: nenhuma classe CSS nova (a `.pacotes-atencao-grid` criada na primeira passada foi removida — `git diff` em `dashboard.css` fica vazio no resultado final). O painel é `<div style={{ overflowX: "auto" }}>` envolvendo `<div style={{ display: "flex", gap: 18 }}>`; cada card é `<Link className="card card-small" style={{ width: 200, flexShrink: 0, ... }}>`. Tudo via estilo inline de layout (largura, `flexShrink`, `overflowX`, `display: flex`/`gap`) — mesmo padrão de inline-style-para-layout já usado em várias telas do projeto (ex.: `CadastrarProfessoraForm.tsx`, o próprio `dashboard/page.tsx`).
- **Rationale**: o pedido de correção foi explícito e preciso — card de largura fixa igual ao "Alunos atendidos" (`card card-small`), nunca esticado, em uma única fileira horizontal, com rolagem própria do painel (`overflow-x: auto`, sem afetar a página). `width`/`flexShrink`/`overflowX` como estilo inline evita criar qualquer classe nova — mais simples ainda que a abordagem anterior (que já era mínima, mas ainda exigia 1 classe nova).
- **Por que não reaproveitar `.table-wrap`** (que já tem `overflow-x: auto`): `.table-wrap` faz parte do seletor combinado que também define `background`/`border-radius`/`box-shadow` (o mesmo usado por `.card`/`.report-card`) — usá-la aqui criaria um "card dentro de card" (uma caixa visível envolvendo os cards individuais, já visíveis por si). Um `overflowX: "auto"` inline no wrapper evita essa sobreposição sem precisar neutralizar propriedades indesejadas.
- **Alternatives considered**:
  - Manter o grid `auto-fit`/`minmax` da primeira passada — descartado: é exatamente o que o feedback visual apontou como errado (card esticado com poucos itens).
  - Criar uma classe `.pacotes-atencao-row`/`.pacotes-atencao-card` para a largura fixa/flex-shrink — descartado: o resultado é idêntico ao estilo inline, e o projeto já usa estilo inline para ajustes de layout pontuais em vários lugares; uma classe nova não traria benefício real para um uso único.
  - Reaproveitar `.table-wrap` para o scroll — descartado pelo motivo acima (sombra/fundo duplicados).

## R2. Card: `card card-small` (não `report-card`) — a referência visual explícita do pedido de correção

- **Decision**: cada card é `<Link href={...} className="card card-small" style={{ width: 200, flexShrink: 0, textDecoration: "none", color: "inherit" }}>` — a combinação exata usada pelos cards "Alunos atendidos"/"Recebido este mês" no topo da própria Home (`dashboard/page.tsx`), referência visual que o pedido de correção citou explicitamente.
- **Rationale**: `report-card` (decisão original) tem padding/gap/hover pensados para um card maior, em grid responsivo — mais alto do que o pedido quer. `card card-small` é literalmente a referência visual indicada; `.card{padding:22px}` cobre fundo/raio/sombra/padding, e `.card-small .icon-chip` é a única regra extra (não usada aqui, já que o pedido não lista ícone entre os 3 conteúdos do card).
- **Alternatives considered**: manter `report-card` só reduzindo padding via estilo inline — descartado: `card card-small` já é a combinação usada pela referência visual citada, sem precisar sobrescrever nenhum padding.

## R3. Título/subtítulo/destaque: `card-eyebrow` + `big-label` (contexto) + `big-value` + `big-label` (legenda) — mesma hierarquia de "Alunos atendidos"

- **Decision**: `<div className="card-eyebrow">{p.alunoNome}</div>`, depois `<div className="big-label" style={{marginTop: 2}}>{p.contexto}</div>`, depois `<div className="big-value">{p.saldoAulas}</div>`, depois `<div className="big-label">{...legenda...}</div>` — todas classes já existentes, reaproveitadas (inclusive `big-label` duas vezes, uma para o contexto acima do número, outra para a legenda abaixo — é só uma classe CSS, reutilizável quantas vezes for preciso).
- **Rationale**: `card-eyebrow` é exatamente o rótulo pequeno em maiúsculas que "Alunos atendidos" usa para seu próprio texto descritivo; não existe mais `h3`/`p` porque a referência agora é `card-small`, não `report-card` (que tinha seu próprio `h3`/`p` escopado). O pedido de correção pede "rótulo pequeno com nome + contexto" — duas linhas pequenas acima do número, replicando a mesma ideia de `card-eyebrow`+`big-label` já estabelecida pelo padrão.
- **Alternatives considered**: usar `h3`/`p` (decisão original, R3 antiga) — descartado junto com a troca de `report-card` para `card-small` (R2), já que `h3`/`p` só eram estilizados via `.report-card h3`/`.report-card p`, que não se aplica mais.

## R4. Estado: `.badge.badge-cancel`/`.badge.badge-pending`, sem mudança

- **Decision**: manter exatamente o `<span>` de badge já usado hoje (`badge-cancel` para "Esgotado", `badge-pending` para "Atenção", com o `Icon name="warn"` dentro), só reposicionado dentro do novo layout de card.
- **Rationale**: já satisfaz FR-007/FR-008 (rótulo em texto, cores já existentes) sem nenhuma mudança — é literalmente o mesmo elemento, só em um contêiner visual diferente.
- **Alternatives considered**: nenhuma — já está correto hoje.

## R5. Ordenação: nenhuma mudança no componente

- **Decision**: o componente continua recebendo `pacotes: PacoteEmAtencao[]` já ordenado pelo backend e só faz `.map()` na ordem recebida — sem `.sort()` nem lógica de agrupamento no frontend.
- **Rationale**: confirma FR-006 e o Princípio II (validação/regra de negócio centralizada no backend) — a ordenação já é uma regra de negócio da spec 041, calculada em `DashboardService.MontarPacotesEmAtencao`, e não deve ser duplicada ou recalculada no frontend.
