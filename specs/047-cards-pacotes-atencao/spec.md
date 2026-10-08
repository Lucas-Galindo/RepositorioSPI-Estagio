# Feature Specification: Cards do Painel de Pacotes em Atenção

**Feature Branch**: `047-cards-pacotes-atencao`

**Created**: 2026-10-06

**Status**: Draft

**Input**: User description: "Redesenhar o painel de pacotes em atenção da Home (spec 041). Hoje cada aluno aparece com o número de aulas restantes em tamanho pequeno. Passar para blocos retangulares (cards), mantendo as mesmas informações: nome do aluno, turma ou 'Atendimento individual', saldo de aulas e o estado ('Atenção' para saldo 1 ou 2, 'Esgotado' para saldo 0). Requisitos: 1. Saldo em destaque visual grande. 2. Grade responsiva (várias colunas em tela larga, menos em média, uma no celular, sem rolagem horizontal). 3. Esgotados primeiro, diferença visual clara, com rótulo em texto (não só cor). 4. Visual limpo, reaproveitando classes/cores/espaçamentos já existentes, sem criar cor ou componente novo se houver equivalente. 5. Sem mudança de backend. Fora de escopo: mudar a regra de quais pacotes aparecem ou o limite de 2 aulas."

## Nota de investigação prévia

Confirmado por leitura do código atual (grounding antes de especificar):

- **Estado atual do painel**: `frontend/components/dashboard/PacotesEmAtencaoPanel.tsx` (spec 041) renderiza cada pacote como uma linha dentro de um único `<div className="card" style={{padding: 0}}>`, usando a classe `week-event` (pensada originalmente para eventos de agenda) com `.t`/`.s` (título/subtítulo em texto pequeno) e um `<span>` com `badge-cancel` (Esgotado) ou `badge-pending` (Atenção) à direita. O saldo de aulas hoje é só um número dentro do texto do subtítulo (`.s`), no mesmo tamanho de fonte do resto — não há destaque visual nenhum nele, confirmando o Requisito 1 do pedido.
- **O rótulo em texto do estado já existe hoje, mas é pequeno**: o badge já mostra "Esgotado"/"Atenção" por extenso (não depende só da cor) — o Requisito 3 ("rótulo em texto, não só cor") já está tecnicamente atendido pelo badge atual; o que falta é só torná-lo mais proeminente dentro do novo layout de card, não criar um rótulo que não existia.
- **Existe um padrão de "número grande com rótulo" já pronto e usado na mesma tela**: os cards de KPI do topo da própria Home (`frontend/app/(app)/dashboard/page.tsx`, linhas ~87-105) já usam exatamente `<div className="card card-small"><div className="icon-chip ...">...</div><div className="big-value">{valor}</div><div className="big-label">{rótulo}</div></div>` — a classe `.big-value` (`font-size: 23px; font-weight: 800`) é o destaque numérico grande que o Requisito 1 pede, e `.big-label` é o texto pequeno de legenda. Este é o equivalente direto a reaproveitar para o saldo de aulas, sem inventar tipografia nova.
- **Achado importante sobre o Requisito 2 (grade responsiva)**: o sistema hoje **não tem nenhum padrão de grade responsiva com quebra de colunas por largura de tela** — uma busca em todo `frontend/styles/*.css` encontra só duas regras `@media` no projeto inteiro (uma em `login.css`, de uma tela sem relação; uma em `dashboard.css`, que só adiciona `overflow-x: auto` ao `<div class="app">` abaixo de 1300px). Todas as grades existentes (`.kpi-row`, `.report-grid`, `.grid-2`, etc.) usam um número fixo de colunas (`repeat(N, 1fr)`), sem nenhuma variação por tamanho de tela. Por isso, **esta feature precisa introduzir uma classe de grade nova com breakpoints** (algo que, hoje, literalmente não existe em nenhum lugar do sistema para reaproveitar) — o que é compatível com o Requisito 4 do pedido, que proíbe criar **cor** ou **componente** novo quando há equivalente, mas não proíbe uma regra de **layout** nova quando não há nenhum equivalente a reaproveitar (as cores do estado e a tipografia do número grande, essas sim, já existem e serão reaproveitadas tal como estão).
- **Achado relacionado, sobre "sem rolagem horizontal"**: o contêiner `.app` (que envolve toda a aplicação) tem `min-width: 1260px` definido globalmente — ou seja, a aplicação como um todo já rola horizontalmente hoje em qualquer tela mais estreita que isso, independentemente desta feature. Esta feature só pode garantir a ausência de rolagem horizontal **dentro do próprio painel** (o grid de cards nunca força uma largura maior que o espaço disponível dentro do seu contêiner) — ela não resolve, e não é seu papel resolver, o comportamento mais amplo do `.app` em telas estreitas, que é uma característica pré-existente de todas as páginas do sistema, não específica deste painel.
- **Nenhuma mudança de backend necessária**: `PacoteEmAtencao` (`frontend/lib/api/dashboard.ts`) já expõe `alunoId`, `alunoNome`, `contexto`, `saldoAulas` e `estado` — exatamente os campos que o pedido lista; a ordenação (esgotados primeiro) já é feita no backend (`DashboardService.MontarPacotesEmAtencao`, spec 041) e não muda.

## Clarifications

_Nenhuma pendente — a investigação prévia já esclareceu o único ponto técnico que poderia gerar ambiguidade (a ausência de um padrão de grade responsiva pré-existente, e o que isso implica para o Requisito 4). O restante do pedido já é preciso o suficiente para especificar sem necessidade de `/speckit-clarify`._

### Revisão pós-implementação (2026-10-07)

A primeira implementação (grid `repeat(auto-fit, minmax(200px, 1fr))`, US2 original abaixo) esticava cada card até preencher a largura da coluna quando havia poucos pacotes (ex.: 1 aluno = 1 card ocupando a largura inteira do painel) e deixava o card alto demais (pilha de `h3`+`p`+`big-value`+`big-label`+badge com o padding cheio de `.report-card`). Validação visual real (prints) mostrou isso e pediu o formato correto: card de **largura fixa e compacta**, igual ao card "Alunos atendidos" do topo da Home (`card card-small`), em **uma única fila horizontal** que rola para o lado quando há muitos alunos — nunca quebra linha, nunca estica. US2, FR-004, FR-005 e SC-003 abaixo já refletem essa correção (a redação original, que descrevia colunas responsivas, foi substituída — não é mais o comportamento alvo). As demais stories/FRs (destaque do saldo, ordenação, rótulo de texto, nenhuma mudança de backend) continuam exatamente como antes.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Ver o saldo de aulas em destaque em formato de card (Priority: P1)

Como professora olhando a Home, eu quero que cada aluno com pacote em atenção apareça como um bloco (card) com o saldo de aulas bem grande e visível, para identificar rapidamente, sem precisar ler com atenção, quem está perto de esgotar o pacote.

**Why this priority**: é a mudança central pedida — sem o destaque visual do saldo, o redesenho não entrega o valor principal (identificação rápida), mesmo que o layout em grid já esteja pronto.

**Independent Test**: Na Home, com ao menos um aluno em "Atenção" e um em "Esgotado", verificar que cada um aparece como um bloco retangular (card) com nome do aluno, turma/"Atendimento individual", e o número de aulas restantes em destaque visual grande — maior que qualquer outro texto do card.

**Acceptance Scenarios**:

1. **Given** a Home com pelo menos um pacote em atenção, **When** a professora observa o painel, **Then** cada pacote aparece como um bloco retangular (card) distinto, não mais como uma linha de lista.
2. **Given** um card de pacote específico, **When** a professora observa o card, **Then** o saldo de aulas restantes aparece em tamanho visivelmente maior que o nome do aluno, a turma, e o rótulo do estado — é o elemento mais chamativo do card.
3. **Given** um card de pacote, **When** a professora observa seu conteúdo completo, **Then** nome do aluno, turma (ou "Atendimento individual" quando não houver turma), saldo de aulas, e o rótulo do estado ("Atenção" ou "Esgotado") estão todos visíveis — nenhuma informação que já existia no formato anterior foi removida.

---

### User Story 2 - Cards compactos de largura fixa, em uma fila horizontal rolável (Priority: P1)

Como professora olhando o painel, eu quero que cada card tenha um tamanho pequeno e fixo (igual ao card "Alunos atendidos" do topo da Home), lado a lado numa única fila, com rolagem horizontal própria do painel quando há muitos alunos — nunca um card gigante esticado quando há só um ou dois alunos.

**Why this priority**: tem a mesma prioridade da US1 porque, sem isso, o redesenho faz o painel parecer quebrado (cards esticados e altos demais quando há poucos alunos) — é tão essencial quanto o destaque visual para a entrega não regredir visualmente.

**Independent Test**: Com um único pacote em atenção, confirmar que o card aparece pequeno e alinhado à esquerda (não esticado até a largura do painel). Com vários pacotes, confirmar que todos aparecem lado a lado em uma única fileira, com rolagem horizontal própria do painel (não da página) quando não cabem todos.

**Acceptance Scenarios**:

1. **Given** um único pacote em atenção, **When** a professora observa o painel, **Then** aparece um único card pequeno, de largura igual à do card "Alunos atendidos", alinhado à esquerda — não esticado para ocupar o espaço disponível.
2. **Given** vários pacotes em atenção que cabem na largura do painel, **When** a professora observa o painel, **Then** os cards aparecem lado a lado, todos do mesmo tamanho fixo, numa única fileira.
3. **Given** pacotes suficientes para não caberem todos na largura do painel, **When** a professora observa o painel, **Then** o painel (não a página inteira) ganha uma barra de rolagem horizontal própria; os cards não quebram linha nem encolhem para caber.
4. **Given** qualquer quantidade de pacotes, **When** a professora olha o restante da página (acima/abaixo do painel), **Then** a página em si nunca rola horizontalmente por causa deste painel.

---

### User Story 3 - Esgotados continuam aparecendo primeiro, com diferença clara (Priority: P2)

Como professora olhando o painel, eu quero que os alunos com pacote esgotado continuem aparecendo antes dos que estão só em atenção, com uma diferença visual óbvia entre os dois estados (não só uma cor sutil), para agir primeiro em quem já não tem nenhuma aula sobrando.

**Why this priority**: a ordem e a distinção de estado já funcionam hoje (spec 041) — esta história garante que o redesenho não perca esse comportamento já validado, por isso é P2 (preservação de comportamento existente, não uma capacidade nova).

**Independent Test**: Com uma mistura de pacotes "Esgotado" e "Atenção", verificar que todos os "Esgotado" aparecem antes de todos os "Atenção" no grid, e que a diferença entre os dois é identificável tanto pela cor quanto por um rótulo de texto, mesmo sem reconhecer cores.

**Acceptance Scenarios**:

1. **Given** uma mistura de pacotes "Esgotado" e "Atenção", **When** a professora observa a ordem dos cards no grid, **Then** todos os cards "Esgotado" aparecem antes de todos os cards "Atenção" (mesma ordem que o backend já entrega, spec 041 — esta feature só exibe, não reordena).
2. **Given** um card "Esgotado" e um card "Atenção" lado a lado, **When** a professora observa os dois sem considerar a cor, **Then** ainda é possível diferenciar os dois pelo rótulo de texto do estado, visível em cada card.

---

### Edge Cases

- **Nenhum pacote em atenção**: o painel continua mostrando o estado vazio já existente hoje (spec 041) — esta feature não altera esse comportamento.
- **Muitos pacotes em atenção (lista longa)**: a fila de cards cresce em largura, não em altura — o painel ganha rolagem horizontal própria (FR-005); não é exigida paginação nesta versão (mesma decisão já aceita em specs anteriores de listas do dashboard).
- **Aluno sem turma (Atendimento individual)**: o card mostra "Atendimento individual" no lugar do nome da turma — mesmo texto já usado hoje (`contexto`), sem mudança.
- **Tela muito estreita (celular, ou zoom alto)**: os cards continuam com a mesma largura fixa e a mesma altura de antes — o painel simplesmente rola horizontalmente mais perto do início da lista; não há uma "versão de 1 coluna" diferente para telas estreitas (ver Revisão pós-implementação acima). O comportamento do restante da aplicação fora deste painel (o contêiner `.app` como um todo) não é alterado por esta feature.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O sistema MUST exibir cada pacote em atenção como um bloco retangular (card) individual, em vez da linha de lista usada hoje.
- **FR-002**: Cada card MUST exibir, no mínimo, as mesmas informações já exibidas hoje: nome do aluno, turma (ou "Atendimento individual" quando não houver turma vinculada), saldo de aulas restantes, e o estado ("Atenção" ou "Esgotado").
- **FR-003**: O saldo de aulas restantes MUST ser exibido com destaque visual (tamanho de fonte maior) em relação a qualquer outro texto do mesmo card.
- **FR-004**: Cada card MUST ter largura fixa e compacta (igual à do card "Alunos atendidos" do topo da Home), alinhado à esquerda, e MUST NOT esticar para ocupar o espaço disponível quando houver poucos pacotes — os cards MUST se organizar lado a lado em uma única fileira horizontal, sem quebrar linha.
- **FR-005**: Quando os cards não couberem todos na largura do painel, o painel (não a página) MUST ganhar rolagem horizontal própria, contida a ele — a página MUST NOT rolar horizontalmente por causa deste painel.
- **FR-006**: Os cards com estado "Esgotado" MUST continuar aparecendo antes dos cards com estado "Atenção" (mesma ordenação já calculada pelo backend, spec 041) — esta feature MUST NOT reordenar nem recalcular o estado no frontend.
- **FR-007**: Cada card MUST exibir um rótulo de texto explícito do estado ("Esgotado" ou "Atenção"), não apenas uma diferença de cor — a diferenciação entre os dois estados MUST permanecer identificável mesmo sem percepção de cor.
- **FR-008**: O sistema MUST NOT introduzir nenhuma cor nova — a diferenciação visual entre "Esgotado" e "Atenção" MUST reaproveitar as cores/classes de estado já existentes no sistema.
- **FR-009**: O sistema MUST NOT introduzir nenhum componente visual novo quando já existir um equivalente no sistema — em particular, o destaque numérico do saldo de aulas MUST reaproveitar o padrão já existente de "número grande com rótulo" usado em outros cards da mesma tela.
- **FR-010**: O sistema MUST NOT alterar o contrato da API `GET /api/dashboard` (campo `pacotesEmAtencao`) nem a regra de quais pacotes aparecem ou o limite de 2 aulas (spec 041) — esta feature é só de apresentação.
- **FR-011**: O sistema MUST NOT alterar o comportamento de clique/navegação de cada card (hoje, clicar leva ao perfil do aluno) — só a aparência visual muda.

### Key Entities

- **Pacote em atenção** (existente, spec 041, sem campo novo): `alunoId`, `alunoNome`, `contexto` (turma ou "Atendimento individual"), `saldoAulas`, `estado` ("Esgotado"/"Atencao") — usados exatamente como hoje, só reapresentados em formato de card.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% dos pacotes em atenção aparecem como cards individuais, com o saldo de aulas identificável de longe (maior que qualquer outro texto do card) em qualquer uma das larguras de tela suportadas.
- **SC-002**: 0 informações presentes na versão anterior do painel (nome, turma/atendimento individual, saldo, estado) desaparecem na nova versão.
- **SC-003**: Com 1 pacote, o card aparece pequeno e alinhado à esquerda (nunca esticado); com N pacotes que excedem a largura do painel, o painel ganha rolagem horizontal própria, sem nenhum card cortado e sem a página em si rolar horizontalmente.
- **SC-004**: 100% dos cards "Esgotado" continuam aparecendo antes de todos os cards "Atenção", e o estado de cada card é identificável por texto, mesmo sem considerar a cor.
- **SC-005**: 0 cores CSS novas introduzidas e 0 mudanças na API/regra de negócio do painel (confirmável por inspeção do código após a implementação).

## Assumptions

- A classe de "número grande com rótulo" já usada nos cards de KPI do topo da Home (`big-value`/`big-label`) é o padrão a ser reaproveitado para o destaque do saldo de aulas — não uma tipografia nova.
- As cores de estado já existentes (usadas hoje nos badges "Esgotado"/"Atenção" do painel atual) são as mesmas a reaproveitar no novo layout de card — nenhuma cor nova.
- O card "Alunos atendidos" (`card card-small`, topo da Home) é a referência visual exata a reaproveitar — mesmo raio de borda, padding, sombra e hierarquia tipográfica (`card-eyebrow`/`big-value`/`big-label`); a largura fixa e a rolagem horizontal do painel são obtidas por estilo inline de layout (largura, `flex-shrink`, `overflow-x`), não por nenhuma classe CSS nova.
- A rolagem horizontal exigida por FR-005 é a do painel em si, contida a ele — o comportamento mais amplo da aplicação (`.app` com largura mínima própria) em telas muito estreitas é uma característica pré-existente do sistema inteiro, não introduzida nem corrigida por esta feature.
- O clique em cada card continua levando ao perfil do aluno, exatamente como a linha clicável fazia antes — nenhuma mudança de navegação.
- Fora de escopo: mudar a regra de quais pacotes aparecem no painel, o limite de 2 aulas para entrar em "Atenção", qualquer mudança de backend, paginação da lista, ou qualquer alteração no comportamento responsivo geral da aplicação fora deste painel.
