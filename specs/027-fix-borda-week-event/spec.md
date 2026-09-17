# Feature Specification: Corrigir Borda Curva Sobrepondo Texto na Agenda Semanal da Home

**Feature Branch**: `027-fix-borda-week-event`

**Created**: 2026-09-17

**Status**: Draft

**Input**: User description: "Corrigir bug visual em .week-event (agenda semanal da Home, dashboard.css:533-542): o border-radius: 11px combinado com border-left: 3px solid var(--c-accent) faz o navegador curvar a borda esquerda colorida nos cantos superior e inferior esquerdo, criando um efeito de \"meia-lua\" que sobrepõe o texto do evento (horário e nome da aula/matéria). Correção: seguir o mesmo padrão já usado com sucesso no componente irmão .cal-event (dashboard.css:708-723), que usa raio de borda menor e reset explícito dos cantos onde a borda colorida fica (border-top-right-radius, border-bottom-right-radius, etc. — ajustar para não curvar o canto onde está a borda esquerda). Resultado esperado: a borda colorida da esquerda fica reta, sem curvatura visível, e o texto do evento (ex: \"16:00 - Matemática\") fica totalmente legível, sem sobreposição. Aplicar apenas em .week-event — não mexer em .cal-event, que já está correto."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Ler o horário e a matéria de cada aula na agenda semanal da Home (Priority: P1)

Como professora visualizando a Home, eu quero ler o horário e o nome da matéria de cada aula nos
cards da agenda semanal sem que um elemento decorativo curvo sobreponha o texto, para que eu
identifique rapidamente meus compromissos do dia sem precisar clicar em cada card para conferir
o que está escrito.

**Why this priority**: É o único problema desta correção — sem ele, a informação mais básica de
cada card (horário + matéria) fica parcialmente ilegível, prejudicando o uso diário da tela
inicial do sistema.

**Independent Test**: Abrir a Home com pelo menos um dia da semana tendo mais de uma aula
cadastrada (para cobrir cards de alturas diferentes) e confirmar visualmente que a borda
colorida de cada card tem a mesma curvatura sutil de `.cal-event` (tela cheia de Agenda), sem
cobrir nenhuma parte do texto, em nenhum dos 7 dias da semana.

**Acceptance Scenarios**:

1. **Given** a Home exibe a agenda semanal com pelo menos uma aula num dia, **When** a
   professora olha o card dessa aula, **Then** o texto do horário e da matéria (ex.: "16:00
   Matemática") está totalmente visível, sem nenhuma parte coberta por um elemento gráfico.
2. **Given** um dia tem múltiplas aulas cadastradas, **When** a professora visualiza a coluna
   desse dia, **Then** todos os cards de aula daquele dia (independente da altura de cada um)
   exibem a borda colorida da esquerda com a mesma curvatura sutil já usada pelo componente
   irmão da tela cheia de Agenda (`.cal-event`) — pequena o bastante para não cobrir nenhuma
   parte do texto, em nenhum card.
3. **Given** uma aula tem status "Realizada" ou "Cancelada" (cor de borda diferente da cor
   padrão), **When** a professora visualiza o card, **Then** a cor de status continua sendo
   exibida corretamente (cinza para Realizada, vermelho para Cancelada) e a curvatura sutil da
   borda permanece a mesma.
4. **Given** a tela cheia de Agenda (fora da Home) já exibe seus próprios cards de evento
   corretamente hoje, **When** a correção desta funcionalidade é aplicada, **Then** a aparência
   da tela de Agenda permanece exatamente a mesma de antes — nenhuma mudança visual lá.
5. **Given** o card de evento é um link clicável (leva ao detalhe da aula), **When** a
   professora visualiza o texto do card, **Then** ele aparece em cor neutra (a mesma cor de
   texto usada no resto do card), sem sublinhado, e sem nenhum caractere separador entre o
   horário e a matéria — só espaço (ex.: "16:00 Matemática"), igual ao formato já usado em
   `.cal-event`.

---

### Edge Cases

- Card de evento com nome de matéria/turma longo o suficiente para quebrar em mais de uma linha:
  a curvatura sutil da borda colorida da esquerda deve continuar pequena e sem cobrir texto em
  toda a altura do card, do mesmo jeito que já acontece em `.cal-event`.
- Card na coluna do dia atual (destacada com fundo diferente): a correção deve funcionar da
  mesma forma independente do destaque de "hoje".
- Card muito baixo (uma única aula curta, sem quebra de linha): o arredondamento dos demais
  cantos do card MUST continuar existindo, com a mesma curvatura sutil em todos os cantos.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: A borda colorida do lado esquerdo de cada card de evento da agenda semanal da Home
  MUST ter a mesma curvatura sutil (raio de borda) já usada pelo componente irmão `.cal-event`
  da tela cheia de Agenda — não uma borda totalmente reta, e não a curva exagerada do bug
  original.
- **FR-002**: Nenhum elemento decorativo (borda, indicador de status) do card de evento da
  agenda semanal da Home MUST sobrepor ou tornar ilegível o texto do horário ou da matéria/nome
  da aula exibido no card.
- **FR-003**: A correção MUST ser restrita ao componente da agenda semanal da Home — MUST NOT
  alterar a aparência dos cards de evento da tela cheia de Agenda, que já exibe a borda
  corretamente hoje.
- **FR-004**: O significado das cores da borda esquerda (cor padrão para aula agendada, cinza
  para "Realizada", vermelho para "Cancelada") MUST continuar exatamente o mesmo de hoje.
- **FR-005**: O arredondamento visual dos cantos do card (onde há ou não borda colorida) MUST
  usar o mesmo raio em todos os cantos, igual ao já usado por `.cal-event` — sem cantos zerados
  nem cantos com raio diferente entre si.
- **FR-006**: Os valores exatos de `border-radius`, `border-left` e `padding` de `.week-event`
  MUST ser copiados fielmente dos valores já em uso em `.cal-event` na data desta correção, sem
  reinterpretação ou ajuste — garantindo curvatura e espaçamento visualmente idênticos entre os
  dois componentes.
- **FR-007**: O texto do card de evento da agenda semanal da Home MUST ser exibido em cor
  neutra herdada do card (não a cor padrão de link do navegador) e MUST NOT ter sublinhado,
  mesmo sendo um elemento clicável que navega para o detalhe da aula — mesmo tratamento visual
  já usado em `.cal-event`.
- **FR-008**: O horário e a matéria exibidos no card MUST ser separados apenas por espaço, sem
  nenhum caractere separador (ponto médio, hífen, ou similar) entre eles — mesmo formato
  ("HH:mm Matéria") já usado em `.cal-event`.
- **FR-009**: A borda colorida decorativa do card MUST permanecer sempre na mesma linha
  horizontal do texto do horário/matéria, como parte de uma única caixa visual — MUST NOT
  aparecer separada (ex.: numa faixa isolada acima do texto), em nenhuma largura de card.

### Key Entities

Não aplicável — esta funcionalidade é uma correção puramente visual, sem entidade de domínio
envolvida.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% dos cards de evento exibidos na agenda semanal da Home mostram o texto de
  horário e matéria completamente legível, sem sobreposição, testado nos 7 dias da semana com
  pelo menos um dia contendo múltiplas aulas.
- **SC-002**: A borda colorida da esquerda aparece com a mesma curvatura sutil de `.cal-event`
  (nem reta, nem exagerada) em 100% dos cards testados, independente da altura do card (uma
  aula ou várias) ou do status da aula (Agendada, Realizada, Cancelada).
- **SC-003**: A tela cheia de Agenda não apresenta nenhuma mudança visual perceptível antes e
  depois da correção (comparação lado a lado).
- **SC-004**: 100% dos cards de evento testados exibem o texto sem estilo de link do navegador
  (cor neutra, sem sublinhado) e sem caractere separador entre horário e matéria — visualmente
  idêntico ao tratamento de texto já usado em `.cal-event`.
- **SC-005**: 100% dos cards de evento testados exibem a borda colorida e o texto como uma
  única caixa visual coesa (borda ao lado do texto, nunca numa linha separada acima dele), em
  qualquer largura de tela testada.

## Assumptions

- O componente irmão da agenda semanal (usado na tela cheia de Agenda, `.cal-event`) já exibe
  essa borda corretamente hoje e serve de referência de comportamento esperado — a correção
  copia fielmente os valores exatos de `border-radius`, `border-left` e `padding` já em uso em
  `.cal-event` (confirmado: sem nenhum reset de canto específico, como
  `border-top-left-radius`/`border-bottom-left-radius` — a curvatura sutil vem só de um
  `border-radius` menor combinado com padding menor), em vez de zerar os cantos onde a borda
  colorida está (abordagem tentada numa primeira iteração desta correção e revertida por deixar
  a borda totalmente reta, diferente do visual esperado).
- É uma correção somente de estilo (CSS) — nenhuma mudança de estrutura de componente, de dado
  exibido ou de comportamento de clique/navegação do card é necessária ou esperada.
- Por ser um ajuste visual isolado e de baixíssimo risco, não é necessário `/speckit-clarify`
  antes do `/speckit-plan`.
