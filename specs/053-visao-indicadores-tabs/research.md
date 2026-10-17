# Research: Padronizar Visual do Alternador Visão Financeiro/Indicadores

Decisões técnicas da Phase 0. O grounding visual já veio fechado da investigação que precedeu a spec (ver spec.md, "Nota de investigação prévia"); esta fase resolve o único detalhe técnico em aberto: como estender `.financeiro-tabs` para cobrir `<button>` sem afetar as abas `<a>` já existentes.

## R1. Estender o seletor existente (`.financeiro-tabs > a`) para incluir `> button`, em vez de criar uma classe nova

- **Decision**: em `frontend/styles/dashboard.css`, trocar o seletor `.financeiro-tabs > a` por `.financeiro-tabs > a,\n.financeiro-tabs > button` (mesmas 2 declarações, `flex: 1 1 0; justify-content: center;`, aplicadas aos dois tipos de filho).
- **Rationale**: `.financeiro-tabs` (o `display:flex; align-items:stretch;` do contêiner) já é agnóstico ao tipo de filho — só a regra de distribuição de largura (`> a`) é específica demais. Reaproveitar a mesma classe mantém uma única fonte de verdade para "visual de aba esticada", em vez de duplicar a regra numa classe nova (`.financeiro-tabs-button` ou similar) que teria que ser mantida sincronizada manualmente com a original.
- **Alternatives considered**: criar uma classe CSS nova (`.tabs-fullwidth`) usada pelos dois pares de botões (financeiro-tabs e o alternador) — descartado: exigiria também trocar a classe já aplicada nas 3 abas de Contas a Receber/Pagar (`financeiro/layout.tsx`), tocando um arquivo que a spec explicitamente marca como não alterado (FR-003); estender o seletor existente evita tocar `financeiro/layout.tsx` por completo.

## R2. Troca de `className` isolada em `relatorios/financeiro/page.tsx`, sem tocar a estrutura do JSX

- **Decision**: em `frontend/app/(app)/relatorios/financeiro/page.tsx:203`, trocar `<div className="row-gap" style={{ marginBottom: 18, gap: 8 }}>` por `<div className="financeiro-tabs" style={{ marginBottom: 18 }}>` — remove `gap: 8` do `style` inline (o `.financeiro-tabs` não precisa de gap entre os botões, já que `flex: 1 1 0` faz cada um ocupar sua fração igual, encostados, mesmo visual das abas de referência). Nenhuma outra linha do arquivo muda — os dois `<button>` continuam exatamente como estão (mesmo `onClick`, mesmas classes `btn btn-sm ${...}`).
- **Rationale**: a mudança mínima que satisfaz FR-001/FR-002 — só o contêiner muda, nada do comportamento de estado (`aba`) ou da lógica de classe condicional dos botões é tocado.
- **Alternatives considered**: manter `gap: 8` também no novo contêiner — descartado: nas abas de Contas a Receber/Pagar não há gap nenhum entre os links (eles ficam colados, cada um ocupando sua fração); manter o gap aqui produziria um visual sutilmente diferente do de referência, contrariando o próprio objetivo de FR-001.

## R3. Sem teste automatizado — validação manual via `quickstart.md`

- **Decision**: sem testes (frontend puro, projeto sem suíte de teste de frontend configurada).
- **Rationale**: mesma decisão já aceita para toda mudança de frontend anterior nesta sessão (specs/045-048, 050, 051).
- **Alternatives considered**: nenhuma.
