# Feature Specification: Modo de Cores Personalizado

**Feature Branch**: `051-cores-personalizadas`

**Created**: 2026-10-13

**Status**: Draft

**Input**: User description: "Card de Backlog 'Alteração de Cores Personalizadas': dentro das Configurações, dar ao usuário comum a opção de escolher as 4 cores da interface livremente (não só 1, como hoje), do mesmo jeito que já existe para os modos de daltonismo. Decisão da conversa: isso é um novo modo de cores, 'Personalizado', ao lado do Padrão e dos 3 modos de daltonismo (Protanopia/Deuteranopia/Tritanopia) — não uma edição por cima de um preset de daltonismo ativo. Ao ativar 'Personalizado', o usuário vê 4 seletores de cor (em vez do 1 que já existe hoje), começando dos mesmos valores do modo Padrão; 'Restaurar padrão' volta essas 4 cores ao valor original, sem trocar de modo."

## Nota de investigação prévia

Confirmado por leitura do código atual (grounding antes de especificar):

- **A peça que esta feature generaliza já existe e já funciona**: `frontend/app/(app)/configuracoes/page.tsx` já tem um bloco "Cor de destaque" que deixa o usuário escolher **1** cor (`accentColor`, via `<input type="color">` nativo), só ativo quando `colorMode === "normal"` ("Padrão") — com um botão "Restaurar padrão" que volta a 1 cor ao valor original (`#4ecdc4`). O mecanismo de aplicar a cor escolhida à interface (`frontend/components/layout/AppShell.tsx`) já injeta `--c-accent`/`--c-accent-deep`/`--c-accent-tint` como CSS custom properties no elemento raiz, usando helpers já existentes (`darkenHex`/`hexToRgba`, `frontend/lib/color.ts`).
- **As "4 cores" que o pedido cita já são um conceito existente e nomeado no sistema**: os 4 modos de daltonismo (`frontend/contexts/ThemeContext.tsx`, tipo `ColorMode`) já sobrescrevem exatamente 4 variáveis (`--c-accent`, `--c-accent2`, `--c-accent3`, `--c-danger`, cada uma com sua variante `-tint` — só `--c-accent` tem também uma variante `-deep`, usada em hover/estados de botão primário) com paletas fixas, pré-escolhidas para contraste de acessibilidade (`frontend/styles/tokens.css`). Esta feature é, estruturalmente, adicionar uma quinta opção ao mesmo seletor (`colorMode`), chamada "Personalizado", que deixa essas mesmas 4 variáveis livres em vez de fixas — reaproveitando 100% do mecanismo de aplicação (CSS custom properties) e dos helpers de cálculo de variante já existentes.
- **Persistência já é local e não sensível**: `ThemeContext.tsx` já persiste `dark`/`colorMode`/`accentColor` em `localStorage` (decisão já tomada e documentada no próprio código: preferência de tema não é segredo, diferente de token de autenticação). As 4 cores do modo Personalizado seguem exatamente o mesmo padrão — mais 3 valores no mesmo objeto já persistido, nenhum mecanismo novo de armazenamento.
- **Nenhuma mudança de backend**: cor de interface é inteiramente uma preferência de UI local ao navegador — não há, e não deve haver, nenhuma chamada de API envolvida.

## Clarifications

_Nenhuma pendente — a decisão central (modo "Personalizado" como uma quinta opção separada, não uma edição sobre um preset de daltonismo ativo) já foi tomada na conversa que precedeu esta spec. O restante é a generalização direta de um mecanismo já existente (1 cor → 4 cores), sem ambiguidade de produto restante._

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Ativar o modo Personalizado e escolher as 4 cores (Priority: P1)

Como usuário do sistema, quero poder escolher livremente as 4 cores principais da interface (não só 1), para deixar o visual do sistema com a aparência que eu prefiro, da mesma forma que já posso escolher entre as paletas de daltonismo prontas.

**Why this priority**: é o núcleo do pedido — sem isso, a feature não entrega nenhuma capacidade nova sobre o que já existe hoje (1 cor personalizável).

**Independent Test**: na tela de Configurações, ativar o modo "Personalizado", escolher 4 cores diferentes das atuais, e confirmar que a interface (botões, destaques, alertas) reflete as 4 cores escolhidas.

**Acceptance Scenarios**:

1. **Given** a tela de Configurações, **When** o usuário olha o bloco "Modo de cores", **Then** vê 5 opções: Padrão, Protanopia, Deuteranopia, Tritanopia, e Personalizado.
2. **Given** o usuário ativa o modo "Personalizado", **When** ele olha o bloco de cor abaixo, **Then** vê 4 seletores de cor (não mais 1), cada um começando com o mesmo valor que o modo Padrão usa hoje.
3. **Given** o modo "Personalizado" ativo, **When** o usuário escolhe uma cor diferente em qualquer um dos 4 seletores, **Then** a interface do sistema (botões, destaques, estados de alerta/perigo) reflete essa cor imediatamente, sem precisar recarregar a página.
4. **Given** o modo "Personalizado" ativo com cores já escolhidas, **When** o usuário sai da tela de Configurações e navega para qualquer outra tela do sistema, **Then** as 4 cores escolhidas continuam aplicadas em toda a interface, não só na tela de Configurações.

---

### User Story 2 - Restaurar as cores personalizadas para o padrão (Priority: P2)

Como usuário que personalizou as 4 cores e não gostou do resultado, quero um jeito rápido de voltar para os valores originais, sem precisar lembrar ou redigitar cada cor manualmente.

**Why this priority**: é um complemento de conveniência à US1 — o valor central (poder personalizar) já está entregue antes desta vir; sem ela, o usuário ainda consegue voltar manualmente escolhendo as cores originais de novo, só que de forma mais trabalhosa.

**Independent Test**: com o modo Personalizado ativo e pelo menos uma cor alterada, clicar em "Restaurar padrão" e confirmar que as 4 cores voltam aos valores originais, sem trocar de modo.

**Acceptance Scenarios**:

1. **Given** o modo "Personalizado" ativo com uma ou mais das 4 cores alteradas, **When** o usuário clica em "Restaurar padrão", **Then** as 4 cores voltam aos mesmos valores do modo Padrão, e o modo continua sendo "Personalizado" (não volta para "Padrão").

---

### Edge Cases

- **Usuário troca do modo "Personalizado" para um dos modos de daltonismo (ou para o Padrão) e depois volta para "Personalizado"**: as 4 cores personalizadas escolhidas anteriormente continuam lá, exatamente como estavam antes da troca — trocar de modo não apaga a personalização.
- **Primeira vez que o modo "Personalizado" é ativado (nenhuma cor customizada salva ainda)**: as 4 cores começam exatamente iguais às do modo Padrão (`#4ecdc4`, `#bb9457`, `#ffe66d`, `#c0392b`) — o usuário nunca vê um estado "vazio"/sem cor.
- **Contraste/legibilidade de uma combinação de cores escolhida pelo usuário**: fora do escopo desta feature — o sistema não valida nem bloqueia nenhuma combinação de cores escolhida; é responsabilidade do próprio usuário, com "Restaurar padrão" como saída fácil.
- **`localStorage` indisponível** (navegação privada restrita): mesmo comportamento já aceito hoje para `dark`/`colorMode`/`accentColor` — a personalização simplesmente não persiste entre sessões, sem erro visível para o usuário.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O sistema MUST oferecer, na tela de Configurações, um modo de cores "Personalizado" como uma quinta opção, ao lado do modo Padrão e dos 3 modos de daltonismo já existentes.
- **FR-002**: Quando o modo "Personalizado" estiver ativo, o sistema MUST permitir que o usuário escolha livremente as mesmas 4 cores que os modos de daltonismo já sobrescrevem (destaque principal, destaque secundário, destaque terciário, e cor de alerta/perigo) — cada uma com seu próprio seletor de cor.
- **FR-003**: Ao ativar o modo "Personalizado" pela primeira vez (sem nenhuma cor customizada salva ainda), as 4 cores MUST iniciar com os mesmos valores usados pelo modo Padrão.
- **FR-004**: Qualquer cor escolhida pelo usuário no modo "Personalizado" MUST se refletir em toda a interface do sistema (não só na tela de Configurações), imediatamente, sem exigir recarregar a página.
- **FR-005**: O sistema MUST oferecer uma ação "Restaurar padrão" que devolve as 4 cores personalizadas aos valores originais do modo Padrão, sem alterar qual modo de cores está ativo.
- **FR-006**: As 4 cores personalizadas escolhidas MUST persistir entre sessões de uso (fechar e reabrir o navegador), da mesma forma que as preferências de tema/modo de cores já persistem hoje.
- **FR-007**: Trocar para um modo de cores diferente (Padrão ou um dos daltonismo) e depois voltar para "Personalizado" MUST preservar as 4 cores personalizadas escolhidas anteriormente — a troca de modo MUST NOT apagar a personalização.
- **FR-008**: Esta feature MUST NOT exigir nenhuma mudança de backend/API — a personalização de cores é inteiramente uma preferência de interface local.
- **FR-009**: Esta feature MUST NOT validar ou impedir nenhuma combinação específica de cores escolhida pelo usuário (ex.: por contraste/legibilidade) — a escolha é livre, com "Restaurar padrão" como saída.

### Key Entities

- **Preferência de cores personalizadas** (extensão da preferência de tema já existente, sem entidade de backend): 4 valores de cor (destaque principal, secundário, terciário, alerta/perigo), associados ao modo "Personalizado" — persistidos localmente no navegador, junto com as demais preferências de aparência já existentes (tema claro/escuro, modo de cores ativo).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: O usuário consegue personalizar as 4 cores da interface em menos de 1 minuto, sem nenhuma instrução adicional além do que já existe hoje para escolher 1 cor.
- **SC-002**: 100% das 4 cores escolhidas no modo Personalizado aparecem refletidas em pelo menos 3 áreas distintas da interface (ex.: um botão de ação principal, um alerta/destaque secundário, e um estado de erro/perigo).
- **SC-003**: Trocar de modo de cores e voltar para "Personalizado" preserva as 4 cores escolhidas em 100% dos casos, enquanto o navegador/sessão não for encerrado de forma que limpe o armazenamento local.
- **SC-004**: "Restaurar padrão" devolve as 4 cores aos valores originais em uma única ação, sem exigir repetir a escolha de cada cor manualmente.

## Assumptions

- As "4 cores" referidas no pedido original correspondem exatamente às 4 variáveis que os modos de daltonismo já sobrescrevem hoje (`--c-accent`, `--c-accent2`, `--c-accent3`, `--c-danger`) — não um conjunto diferente ou adicional.
- O modo "Personalizado" é uma opção separada e mutuamente exclusiva dos modos de daltonismo (decisão já confirmada na conversa) — não uma camada de edição por cima de um preset de daltonismo ativo.
- "Restaurar padrão" no modo Personalizado reseta as 4 cores para os valores do modo Padrão, sem trocar o modo de cores ativo (continua em "Personalizado" depois do reset).
- Mecanismo de persistência: o mesmo `localStorage` já usado hoje para tema/modo de cores/cor de destaque — nenhum armazenamento novo, nenhuma mudança de backend.
- Fora de escopo: qualquer validação de contraste/legibilidade das cores escolhidas; qualquer personalização além dessas 4 cores (ex.: fontes, espaçamento, mais de 4 cores); editar uma cor específica enquanto um modo de daltonismo está ativo (ver Clarifications); qualquer sincronização dessa preferência entre dispositivos/sessões diferentes (é local ao navegador, mesma limitação já aceita para as preferências de tema existentes).
