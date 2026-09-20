# Feature Specification: Tooltip Explicativo para Cards de Indicador

**Feature Branch**: `034-indicator-tooltip`

**Created**: 2026-09-19

**Status**: Draft

**Input**: User description: "Adicionar um componente de tooltip/balão informativo genérico e reutilizável para cards de indicador financeiro. Ao passar o mouse sobre um ícone indicador (ex: um '?' ou 'ℹ️' no canto do card), deve aparecer um balão explicando o que aquele indicador significa em linguagem simples, sem fórmula técnica. Aplicar esse tooltip em todos os cards de indicador existentes hoje: Valor Faturado no Período, Taxa de Inadimplência, Prazo Médio de Atraso, Fluxo de Caixa Operacional, Gargalo de Caixa, Fluxo de Caixa Mensal, Taxa de Ocupação, Total Recebido/Pago, Saldo Realizado/Previsto, Receita/Despesa Pendente — em qualquer tela onde aparecem (Dashboard, Relatório Financeiro, visão Indicadores). O componente deve ser genérico (reutilizável), recebendo o texto explicativo como propriedade."

## Nota de investigação prévia

Confirmado por leitura do código atual (frontend e backend):

- **Não existe hoje nenhum componente de card genérico** (`Card`/`KpiCard`) nem **nenhum componente de tooltip/popover** no frontend — cada tela repete manualmente o mesmo bloco de markup (classe CSS `.kpi` ou `.card.card-small`) com `label`/`value`/`delta`. Não há biblioteca de UI instalada (`frontend/package.json` só tem `next`/`react`/`react-dom`/fontes) e nenhum padrão de hover (`title="..."`) usado para explicar indicadores hoje. O componente de tooltip será construído do zero, junto aos demais componentes reutilizáveis já existentes em `frontend/components/shared/`.
- **Nem todos os itens da lista original têm um card na UI hoje**:
  - **"Valor Faturado no Período"**: não existe rótulo com esse nome exato — o card correspondente na Home é **"Recebido este mês"** (`frontend/app/(app)/dashboard/page.tsx`).
  - **"Gargalo de Caixa"**: foi **removido intencionalmente da UI** (ver [specs/022-tabela-lancamentos-indicadores/spec.md](../022-tabela-lancamentos-indicadores/spec.md)), substituído pela tabela "Lançamentos do período". O cálculo ainda existe no backend, mas não há card ativo hoje.
  - **"Fluxo de Caixa Operacional"**: é calculado e retornado pela API (`DashboardResponse.Indicadores`), mas **nenhuma tela exibe esse valor** hoje — não existe card correspondente.
  - **"Taxa de Ocupação"**: existe no backend com a fórmula exata pedida (`RelatorioService.ObterPeriodoAgendaAsync`, endpoint `GET /api/relatorios/periodo-agenda`, documentado em [specs/016-relatorio-periodo-agenda](../016-relatorio-periodo-agenda/spec.md)), mas **nenhuma tela consome esse endpoint** — mesma situação de "Fluxo de Caixa Operacional"/"Gargalo de Caixa". O card "Ocupação média" (Relatório de Turmas) é um indicador **diferente e independente** (média de alunos por turma, calculado no frontend a partir de outras APIs), não o mesmo dado com outro nome.
  - **"Fluxo de Caixa Mensal"**: na UI é uma **tabela** de 6 meses (Entradas/Saídas/Saldo), não um card de KPI único — o tooltip, se aplicado, deve ficar junto ao título da tabela, não em cada célula.
  - **"Saldo Realizado/Previsto"**: aparece em duas telas (Financeiro — Visão Geral, e Relatório Financeiro — Visão Financeiro) com **fórmulas ligeiramente diferentes** de "Saldo Previsto" (uma sempre usa o mês corrente; a outra respeita o período filtrado). O texto do tooltip deve descrever o conceito, não uma fórmula específica de uma das duas telas.
- Todos os demais itens (Taxa de Inadimplência, Prazo Médio de Atraso, Total Recebido/Pago, Receita/Despesa Pendente) têm card ativo hoje em `frontend/app/(app)/relatorios/financeiro/page.tsx`, com fórmula confirmada no backend (`RelatorioService.cs`/`RelatorioRepository.cs`).

## Clarifications

### Session 2026-09-19

- Q: A "Taxa de Ocupação" pedida (Realizadas ÷ Agendadas+Realizadas+Canceladas) existe de fato no backend (`RelatorioService.ObterPeriodoAgendaAsync`, ver [specs/016-relatorio-periodo-agenda](../016-relatorio-periodo-agenda/spec.md)), com a fórmula exata do texto sugerido — mas, confirmado por grep no frontend, nenhuma tela consome esse endpoint hoje. Como tratar esse item? → A: Fora do escopo desta feature, na mesma situação de "Fluxo de Caixa Operacional" e "Gargalo de Caixa" (sem card ativo na UI hoje). "Ocupação média" (Relatório de Turmas) é um indicador diferente e independente — recebe seu próprio tooltip normalmente, com texto correto sobre o que ele calcula de fato (média de alunos matriculados por turma ativa), não a fórmula de Taxa de Ocupação.
- Q: Deve o tooltip também ser acionável por teclado (foco no ícone) e legível por leitor de tela, ou o hover/toque cobre o requisito de acessibilidade desta feature? → A: Sim — o ícone MUST ser acionável por Tab/foco de teclado (mostrando o balão ao focar, escondendo ao perder foco) e MUST ter texto acessível para leitor de tela (`aria-label`/`aria-describedby`), além de hover/touch.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Entender um indicador sem sair da tela (Priority: P1)

Como professora usando o Dashboard, o Relatório Financeiro ou o Relatório de Turmas, eu quero passar o mouse (ou tocar, no celular) sobre um ícone de informação ao lado de cada indicador, para entender o que aquele número significa em linguagem simples, sem precisar adivinhar ou perguntar a alguém o que ele representa.

**Why this priority**: É o valor central do pedido — hoje os indicadores são exibidos sem nenhuma explicação, exigindo que a professora infira o significado (ou o cálculo exato) sozinha.

**Independent Test**: Abrir qualquer tela com cards de indicador (Dashboard, Relatório Financeiro, Relatório de Turmas), passar o mouse sobre o ícone de informação de um indicador, e confirmar que um balão com texto explicativo em linguagem simples aparece, sem fórmula técnica.

**Acceptance Scenarios**:

1. **Given** a professora está no Dashboard, **When** ela passa o mouse sobre o ícone de informação do card "Recebido este mês", **Then** um balão aparece explicando, em linguagem simples, que esse valor é o total de contas a receber pagas dentro do mês corrente.
2. **Given** a professora está na aba "Visão de Indicadores" do Relatório Financeiro, **When** ela passa o mouse sobre o ícone de informação do card "Inadimplência", **Then** o balão exibe o texto "Percentual do valor total vencido no período que ainda não foi recebido." (sem menção a `Status`, `DataVencimento` ou qualquer termo técnico).
3. **Given** qualquer card de indicador com tooltip aplicado, **When** o mouse sai da área do ícone (ou, em touch, a professora toca fora do balão), **Then** o balão desaparece.
4. **Given** um indicador que aparece em mais de uma tela com semântica ligeiramente diferente (ex.: "Saldo Previsto"), **When** o tooltip é exibido em cada tela, **Then** o texto descreve o conceito geral (o que o saldo previsto representa), sem citar uma fórmula específica que só se aplica a uma das telas.
5. **Given** uma professora navegando somente por teclado, **When** ela pressiona Tab até o ícone de informação de um indicador, **Then** o balão explicativo aparece (equivalente ao hover), e desaparece quando o foco sai do ícone (Tab novamente ou Shift+Tab).
6. **Given** uma professora usando leitor de tela, **When** o foco chega ao ícone de informação, **Then** o leitor de tela anuncia o texto explicativo do indicador (via `aria-label` ou `aria-describedby`), sem exigir que o balão visual esteja aberto.

---

### User Story 2 - Adicionar tooltip a um indicador novo sem duplicar código (Priority: P2)

Como desenvolvedora mantendo o sistema, eu quero que adicionar um tooltip a um indicador novo (ou a um card ainda sem tooltip) seja uma mudança de poucas linhas — passar um texto como propriedade a um componente já existente — em vez de reimplementar a lógica de balão/hover em cada tela.

**Why this priority**: Sem isso, cada novo indicador exigiria reimplementar a mesma interação, aumentando a chance de inconsistência visual e de comportamento entre telas — exatamente o tipo de duplicação que o pedido original quer evitar.

**Independent Test**: Adicionar o componente de tooltip a um card que ainda não o tem, usando apenas a prop de texto explicativo, sem escrever nenhum HTML/CSS de balão específico para aquele card.

**Acceptance Scenarios**:

1. **Given** o componente de tooltip genérico já existe, **When** uma desenvolvedora quer adicionar um tooltip a um indicador novo, **Then** ela só precisa renderizar o componente passando o texto explicativo como propriedade — nenhuma lógica de posicionamento, hover ou balão é duplicada.

---

### Edge Cases

- O que acontece em telas mobile/touch, onde não existe "hover" de mouse? O tooltip deve poder ser acionado por toque no ícone (e fechado tocando novamente ou fora dele) — comportamento equivalente ao hover em desktop.
- O que acontece para quem navega só por teclado ou usa leitor de tela? O ícone é alcançável por Tab (o balão abre ao focar e fecha ao perder o foco) e o texto explicativo é exposto via atributo de acessibilidade, para que um leitor de tela o anuncie sem depender de hover de mouse.
- O que acontece se o texto explicativo for muito longo para a largura do card? O balão deve quebrar linha e, se necessário, ajustar sua posição para não ser cortado pela borda da tela.
- O que acontece com indicadores da lista original sem card ativo na UI hoje ("Fluxo de Caixa Operacional", "Gargalo de Caixa")? Ficam fora do escopo desta feature — não há card onde anexar o ícone de informação; se esses indicadores ganharem uma tela própria no futuro, o tooltip pode ser adicionado então, reaproveitando o mesmo componente.
- O que acontece com "Fluxo de Caixa Mensal", que é uma tabela e não um card de valor único? O ícone de informação é aplicado ao título da tabela, explicando o que as colunas (Entradas/Saídas/Saldo) representam em conjunto, não uma célula específica.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O sistema MUST oferecer um componente de tooltip/balão informativo genérico e reutilizável, que recebe o texto explicativo como propriedade (não texto fixo/hardcoded dentro do componente).
- **FR-002**: O componente MUST ser acionado por um ícone visível inline, junto ao rótulo do indicador, dentro do mesmo container de label do card (`.label`/`.card-eyebrow`, ou, no caso de "Fluxo de Caixa Mensal", junto ao título da tabela) — não um ícone posicionado isoladamente no canto do card. O balão MUST aparecer ao passar o mouse (hover) sobre o ícone em telas com mouse, ao tocar no ícone em telas touch, e ao receber foco de teclado (Tab) no ícone.
- **FR-002a**: O ícone MUST ser alcançável por navegação de teclado (Tab) e MUST expor o texto explicativo a leitores de tela via atributo de acessibilidade (`aria-label` ou `aria-describedby`), independentemente do balão visual estar aberto.
- **FR-003**: O texto de cada tooltip MUST explicar o indicador em linguagem simples e não-técnica (sem nomes de campos, fórmulas matemáticas ou termos de implementação), com o texto real de cada indicador definido a partir do cálculo verdadeiro confirmado no backend.
- **FR-004**: O sistema MUST aplicar esse tooltip a todo card de indicador atualmente exibido em qualquer tela (Dashboard, Financeiro — Visão Geral, Relatório Financeiro — Visão Financeiro e Visão de Indicadores, Relatório de Turmas), cobrindo: "Recebido este mês" (Dashboard), "Inadimplência", "Prazo médio de atraso", tabela "Fluxo de caixa (últimos 6 meses)", "Ocupação média", "Recebido"/"Pago"/"Saldo realizado" (Visão Financeiro), "Saldo realizado (mês)"/"Saldo previsto" (ambas as telas em que aparecem), "Receita pendente"/"Despesa pendente".
- **FR-005**: O sistema MUST NOT tentar aplicar o tooltip a indicadores sem nenhum card ativo na UI hoje ("Fluxo de Caixa Operacional", "Gargalo de Caixa", "Taxa de Ocupação" no sentido de Realizadas/Agendadas+Realizadas+Canceladas) — fora do escopo por ausência de local para o ícone. Isso não impede o card "Ocupação média" (indicador diferente, já ativo na UI) de receber tooltip normalmente.
- **FR-006**: O balão MUST desaparecer quando o mouse sai da área do ícone, quando o ícone perde o foco de teclado (Tab/Shift+Tab), ou, em touch, quando a professora toca fora do balão — sem exigir nenhuma ação adicional de fechamento.
- **FR-007**: A adição de um tooltip a um indicador novo (ou ainda sem tooltip) MUST exigir apenas renderizar o componente com o texto explicativo como propriedade, sem duplicar lógica de balão/hover por tela.

### Key Entities

- Não introduz entidades de dados novas — é puramente um componente de apresentação (frontend) que recebe texto estático como propriedade; não há novo dado persistido ou consultado.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% dos cards de indicador atualmente exibidos nas telas listadas (Dashboard, Financeiro, Relatório Financeiro, Relatório de Turmas) passam a ter um ícone de informação com tooltip explicativo.
- **SC-002**: Nenhum texto de tooltip contém nomes de campos técnicos, operadores matemáticos ou termos de implementação (validável por revisão de conteúdo).
- **SC-003**: Adicionar um tooltip a um card novo requer alteração em uma única linha de código (renderizar o componente com a prop de texto), sem nova lógica de hover/balão.
- **SC-004**: 100% dos ícones de informação são alcançáveis via Tab e expõem o texto explicativo a leitores de tela, sem depender de hover de mouse.

## Assumptions

- O ícone de informação usa o componente `Icon` já existente em `frontend/components/shared/` (mesmo padrão visual dos demais ícones do sistema), não um novo sistema de ícones.
- O componente de tooltip é construído sem nenhuma nova dependência externa (sem biblioteca de UI), consistente com a abordagem atual do frontend (CSS/JS próprio, como `EmptyState`/`StatusPill`).
- Os textos explicativos de cada indicador são definidos nesta spec (ver tabela abaixo) com base no cálculo real confirmado no backend, priorizando linguagem simples sobre precisão técnica exaustiva.
- "Fluxo de Caixa Operacional" e "Gargalo de Caixa" ficam fora do escopo desta feature por não terem card ativo na UI hoje — não é responsabilidade desta feature criar novos cards para eles.
- "Valor Faturado no Período" da lista original corresponde ao card "Recebido este mês" do Dashboard.
- "Taxa de Ocupação" (Realizadas ÷ Agendadas+Realizadas+Canceladas) fica fora do escopo desta feature por não ter card ativo na UI hoje, mesma decisão aplicada a "Fluxo de Caixa Operacional" e "Gargalo de Caixa". "Ocupação média" (Relatório de Turmas) é tratado como indicador independente, com tooltip descrevendo o que ele calcula de fato.

### Textos de tooltip por indicador

| Indicador (rótulo na UI) | Tela(s) | Texto do tooltip |
|---|---|---|
| Recebido este mês | Dashboard | "Total de contas a receber que já foram pagas dentro do mês atual." |
| Inadimplência | Relatório Financeiro — Indicadores | "Percentual do valor total vencido no período que ainda não foi recebido." |
| Prazo médio de atraso | Relatório Financeiro — Indicadores | "Quantos dias, em média, os pagamentos atrasados demoram para ser recebidos após o vencimento." |
| Fluxo de caixa (últimos 6 meses) | Relatório Financeiro — Indicadores | "Quanto entrou, quanto saiu e qual foi o saldo do seu caixa em cada um dos últimos 6 meses." |
| Ocupação média | Relatório de Turmas | "Número médio de alunos matriculados em cada turma ativa." |
| Recebido / Pago | Relatório Financeiro — Visão Financeiro | "Total efetivamente recebido dos alunos" / "Total efetivamente pago em despesas" dentro do período filtrado. |
| Saldo realizado | Financeiro — Visão Geral; Relatório Financeiro — Visão Financeiro | "Diferença entre o que já entrou e o que já saiu no período — o saldo que de fato aconteceu." |
| Saldo previsto | Financeiro — Visão Geral; Relatório Financeiro — Visão Financeiro | "O saldo que você teria se tudo que está em aberto (a receber e a pagar) fosse recebido e pago." |
| Receita pendente | Relatório Financeiro — Visão Financeiro | "Total que ainda falta receber dos alunos, incluindo o que já está atrasado." |
| Despesa pendente | Relatório Financeiro — Visão Financeiro | "Total que ainda falta pagar em despesas, incluindo o que já está atrasado." |
