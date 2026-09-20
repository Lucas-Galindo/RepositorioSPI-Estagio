# Phase 0 Research: Tooltip Explicativo para Cards de Indicador

## R1 — Mecanismo de exibição do balão (hover + toque + teclado, sem nova dependência)

**Decision**: Implementar o balão com CSS puro, usando um `<button type="button">` nativo como gatilho (já focável e ativável por teclado/toque sem nenhum atributo extra) e um `<span role="tooltip">` posicionado com `position:absolute`, controlado por `:hover` e `:focus-within` no wrapper (`.info-tooltip:hover .info-tooltip-bubble, .info-tooltip:focus-within .info-tooltip-bubble { opacity:1; visibility:visible; }`). Em telas touch, o toque no `<button>` move o foco para ele, o que já aciona `:focus-within` — não é necessário nenhum JavaScript de toggle.

**Rationale**: Cobre as três formas de acionamento exigidas (hover, toque, foco de teclado — FR-002, FR-002a) com uma única regra CSS, sem estado React, sem listener de evento e sem nenhuma biblioteca nova — consistente com a investigação prévia da spec (nenhuma dependência de UI hoje) e com o estilo dos demais componentes de `frontend/components/shared/` (`EmptyState`, `StatusPill`), que também são puramente declarativos.

**Alternatives considered**:
- Estado React (`useState` para `aberto`/`fechado`) com handlers de `onMouseEnter`/`onMouseLeave`/`onFocus`/`onBlur`/`onClick` — rejeitado por adicionar complexidade (gerenciar fechamento ao clicar fora, debouncing) que a abordagem CSS-only já resolve nativamente via `:focus-within`.
- Biblioteca de tooltip (Radix UI, Floating UI, etc.) — rejeitado por introduzir uma dependência nova nesta feature; o projeto não usa nenhuma biblioteca de UI hoje (confirmado na investigação prévia).

## R2 — Ícone do gatilho

**Decision**: Adicionar uma entrada nova `info` ao registro `PATHS` de `frontend/components/shared/Icon.tsx` (um círculo com "i", no mesmo estilo `stroke`/`viewBox 0 0 24 24` dos ícones já existentes), reaproveitado pelo componente `InfoTooltip` via `<Icon name="info" size={12} />` — mesmo padrão de tamanho já usado nos ícones de label dos cards `.kpi` (`size={12}`).

**Rationale**: Todos os ícones do sistema já passam por esse único registro central; criar um SVG solto dentro de `InfoTooltip.tsx` duplicaria o padrão em vez de reaproveitá-lo, indo contra FR-007 (reutilização sem duplicação) em espírito, mesmo não sendo o componente de tooltip em si.

**Alternatives considered**: Usar texto literal "?" como gatilho (sem SVG) — rejeitado por ficar visualmente inconsistente com os demais ícones dos cards, que são todos SVG line-icons no mesmo estilo.

## R3 — Onde inserir o gatilho em cada tipo de card

**Decision**: Confirmado por leitura de código que existem **dois** padrões de markup de card hoje, não um só:
1. `.kpi .label` (usado em `relatorios/financeiro/page.tsx`, `relatorios/turmas/page.tsx`, `financeiro/page.tsx`) — um `<div className="label"><Icon .../> Texto</div>` com `display:flex; gap:6px`. O `InfoTooltip` é inserido como último filho desse mesmo `<div>`.
2. `.card-eyebrow` (usado em `dashboard/page.tsx`, dentro de `.card.card-small`) — mesmo padrão flex, mas classe diferente. O card "Recebido este mês" usa esse padrão.

Em ambos os casos, o `InfoTooltip` é inserido como último elemento dentro do container de label existente (`.label` ou `.card-eyebrow`), aproveitando o `display:flex; gap` já presente em ambas as classes — nenhuma mudança de layout adicional é necessária além do posicionamento absoluto do próprio balão.

Para a tabela "Fluxo de caixa (últimos 6 meses)" (não é um `.kpi`, é um `<h4>` dentro de `.mini-panel`), o `InfoTooltip` é inserido como último filho do `<h4>` (mesmo padrão `<Icon/> Texto`), explicando a tabela como um todo (Edge Case da spec).

**Rationale**: Reaproveitar o `display:flex; gap` já existente em `.label`/`.card-eyebrow`/`<h4>` evita qualquer CSS de posicionamento novo para alinhar o ícone ao lado do texto — só o balão em si (`.info-tooltip-bubble`) precisa de CSS novo.

**Alternatives considered**: Posicionar o ícone fora do container de label (ex.: canto absoluto do card, conforme o texto original do pedido "ícone no canto do card") — rejeitado após inspeção do CSS: os cards (`.kpi`, `.card-small`) não têm `position:relative` hoje nem um canto vazio reservado; colocar o ícone ao lado do texto do label é visualmente equivalente (mesmo objetivo: sinalizar que aquele indicador específico tem uma explicação) e não exige nenhuma mudança estrutural adicional nos containers de card.

## R4 — Verificação de cobertura de teste

**Decision**: Confirmado que o frontend não tem nenhum framework de teste automatizado instalado (`package.json` sem Jest/Vitest/Testing Library/Playwright). A validação desta feature é manual: rodar o servidor de desenvolvimento (`npm run dev`) e verificar, em cada uma das 4 telas, que o balão aparece em hover, toque (emulado) e Tab, e que o texto corresponde ao definido na spec.

**Rationale**: Consistente com o padrão já estabelecido nesta sessão para mudanças de frontend/UI — sem suíte de teste para reaproveitar ou estender, a verificação via navegador é a única forma disponível de validar a feature ponta a ponta.

**Alternatives considered**: Introduzir Testing Library/Playwright para esta feature — rejeitado por ser desproporcional ao escopo (um componente de apresentação simples) e por introduzir uma decisão de tooling de testes que está fora do pedido original do usuário.
