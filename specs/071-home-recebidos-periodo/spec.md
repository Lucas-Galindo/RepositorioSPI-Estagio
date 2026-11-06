# Feature Specification: Home — "Recebidos no período" e cards que acompanham os filtros

**Feature Branch**: `071-home-recebidos-periodo`

**Created**: 2026-11-05

**Status**: Draft

**Input**: Pedido do usuário (2026-11-05), card do Trello "Home: card 'Recebidos no período' e rótulos dos cards acompanhando o filtro de período": "Conferir se na home os filtros também se aplicam aos cards de alunos atendidos e recebidos no mês. No caso, eu preciso que este card de recebidos no mês seja 'Recebidos no período'."

## Contexto (estado atual, lido do código e conferido no Chrome em 2026-11-05)

- A Home tem três filtros na seção de pacotes: **aluno** ("Todos os alunos"), **período a partir de** e **período até**. A tela envia os três ao servidor (`GET /api/dashboard`).
- Os cards **"Alunos atendidos"** e **"Recebido este mês"** vêm do servidor (`alunosAtendidosNoPeriodo`, `valorFaturadoNoPeriodo`) e **já seguem o período** (sem período informado, assumem o mês atual). Conferido no Chrome: mês atual → 0 alunos e R$ 350,00; ano de 2026 → 1 aluno e R$ 870,00.
- O filtro **de aluno não altera** esses dois cards (só o bloco de pacotes usa o aluno).
- Os **rótulos são fixos em "este mês"** ("Recebido este mês", "Com aula realizada este mês", "Contas a receber pagas no mês" e a dica "dentro do mês atual"), mesmo quando o período filtrado é outro.
- "Recebido" soma as contas a receber com status Pago pela **data de pagamento** dentro do período. O cálculo já aceita filtrar por aluno (usado em Financeiro/Relatórios); "Alunos atendidos" ainda não.
- Se só o "até" é preenchido, o início vira o **1º dia do mês atual** (e não o do mês do "até"), o que gera períodos estranhos (ex.: até 15/09 vira 01/11 a 15/09, vazio). Com só o "a partir de", o fim é **um mês depois do início, menos um dia** (ex.: 10/09 → 09/10); sem nenhum dos dois, vale o mês atual inteiro.

## Clarificações (respondidas pelo usuário em 2026-11-05)

- **Filtro de aluno**: aluno e período filtram os dois cards. Com um aluno escolhido, "Recebidos no período" soma só os pagamentos dele e "Alunos atendidos" mostra só ele (1 ou 0).
- **Só o "até"**: o início passa a ser o 1º dia do mês do "até".

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Cards com o nome certo: "Recebidos no período" (Priority: P1)

A professora filtra um período na Home e os cards falam do período, não de "este mês": o card de recebidos se chama **"Recebidos no período"** e os textos dos dois cards deixam claro que valem para o período escolhido (ou para o mês atual, quando não há filtro).

**Why this priority**: É o pedido central; hoje o rótulo "este mês" engana quando o período é outro.

**Independent Test**: Abrir a Home, escolher um período de 2026 inteiro e conferir que os dois cards trazem "período" nos textos e que os valores correspondem ao filtro.

**Acceptance Scenarios**:

1. **Given** a Home sem filtro, **When** ela abre, **Then** o card mostra **"Recebidos no período"** com a legenda indicando o mês atual, e o valor é o mesmo de antes.
2. **Given** um período escolhido, **When** os cards recarregam, **Then** os textos dos dois cards falam do período (não de "este mês") e os valores acompanham o filtro.
3. **Given** a dica (ícone de informação) do card de recebidos, **When** ela a abre, **Then** o texto fala do período escolhido (sem filtro, do mês atual).

---

### User Story 2 - O filtro de aluno também filtra os dois cards (Priority: P1)

Ao escolher um aluno, "Recebidos no período" e "Alunos atendidos" passam a considerar só aquele aluno, junto com o período.

**Why this priority**: Pedido de conferência do usuário: "os filtros também se aplicam aos cards".

**Independent Test**: Escolher um aluno com pagamentos e aulas no período e conferir que o recebido é a soma só dos pagamentos dele e que "Alunos atendidos" é 1; voltar a "Todos os alunos" e conferir que os valores voltam ao total.

**Acceptance Scenarios**:

1. **Given** um aluno escolhido com aula realizada no período, **When** os cards recarregam, **Then** "Alunos atendidos" mostra 1 e "Recebidos no período" soma só os pagamentos pagos dele naquele período.
2. **Given** um aluno escolhido sem aula realizada nem pagamento no período, **When** os cards recarregam, **Then** mostram 0 e R$ 0,00.
3. **Given** "Todos os alunos", **When** os cards recarregam, **Then** os valores são exatamente os de hoje (sem regressão).
4. **Given** um aluno escolhido, **Then** a legenda dos cards indica que valem para o aluno selecionado.

---

### User Story 3 - Período coerente quando só o "até" é preenchido (Priority: P2)

Se a professora preenche só o "até", o período considerado vai do 1º dia do mês desse "até" até o próprio "até".

**Why this priority**: Corrige um comportamento estranho do mesmo filtro; menos visível que as duas anteriores.

**Independent Test**: Preencher só o "até" com 15/09/2026 e conferir que os cards consideram 01/09/2026 a 15/09/2026.

**Acceptance Scenarios**:

1. **Given** só o "até" preenchido (15/09/2026), **When** os cards recarregam, **Then** o período considerado é 01/09/2026 a 15/09/2026, para todos os números do dashboard.
2. **Given** só o "a partir de" preenchido, **Then** o fim continua sendo um mês depois do início, menos um dia (sem mudança).
3. **Given** nenhum dos dois, **Then** vale o mês atual (sem mudança).

---

### Edge Cases

- Aluno escolhido **sem nenhum pagamento ou aula** no período: 0 e R$ 0,00 (não é erro).
- Aluno escolhido e depois "Todos os alunos": os cards voltam ao total, sem resquício do filtro anterior.
- "Alunos atendidos" com aluno escolhido só pode ser 0 ou 1.
- Período com início depois do fim: continua retornando zeros, como hoje (fora do escopo).
- O valor "Recebido" continua usando a **data de pagamento** (não a competência nem o vencimento).
- O bloco de pacotes, os outros cards da Home e as telas Financeiro e Relatórios não mudam.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O card de recebidos da Home MUST se chamar **"Recebidos no período"**, com legenda e dica que falem do período escolhido (sem filtro: "mês atual").
- **FR-002**: O card "Alunos atendidos" MUST ter a legenda falando do período ("com aula realizada no período"), não de "este mês".
- **FR-003**: Com um aluno escolhido, "Recebidos no período" MUST somar só os pagamentos pagos desse aluno no período, e "Alunos atendidos" MUST contar só esse aluno (1 se teve aula realizada no período, senão 0). Sem aluno, o comportamento atual MUST ser mantido.
- **FR-004**: Os valores MUST continuar calculados no servidor; o frontend MUST NOT recalcular, somar, contar nem filtrar esses números (só envia aluno e período e exibe o que recebe).
- **FR-005**: Quando só o "até" for informado, o início do período MUST ser o 1º dia do mês do "até"; só o "a partir de" mantém o fim como está hoje (um mês depois do início, menos um dia); sem nenhum dos dois, o mês atual. A regra MUST valer para todos os números do dashboard (o período é único).
- **FR-006**: Sem filtro de aluno, os valores dos dois cards MUST NOT mudar em relação aos atuais (mesmas regras de "Realizada", "Pago" e data de pagamento).
- **FR-007**: As telas Financeiro e Relatórios, e o cálculo de recebido que elas usam, MUST NOT mudar de resultado.
- **FR-008**: O contrato da API do dashboard MUST NOT ganhar, perder nem renomear campos (`alunosAtendidosNoPeriodo` e `valorFaturadoNoPeriodo` continuam). O parâmetro de aluno já existe.
- **FR-009**: A mudança MUST NOT criar tabela, coluna ou migração, MUST NOT criar arquivo novo no frontend e MUST NOT adicionar dependência.

### Key Entities

Nenhuma entidade nova. Reaproveita Pagamento (status, data de pagamento, aluno) e AulaAluno/Aula (status, data, aluno).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% dos textos dos dois cards deixam de citar "este mês" e passam a citar "período" (ou "mês atual" quando não há filtro).
- **SC-002**: Com um aluno escolhido, o valor de "Recebidos no período" é igual à soma dos pagamentos pagos dele no período (conferido contra a lista de pagamentos) e "Alunos atendidos" é 0 ou 1.
- **SC-003**: Sem aluno escolhido, 0 diferença nos dois valores em relação a antes da mudança (mesmo período).
- **SC-004**: Só com o "até" informado, o período considerado começa no dia 1 do mês do "até" em 100% dos casos.
- **SC-005**: 0 mudança nos resultados de Financeiro e Relatórios; 0 migração, 0 arquivo novo no frontend, 0 dependência nova.

## Assumptions

- "Período" é o definido pelos campos "a partir de" e "até" da Home (o mesmo que já alimenta os pacotes).
- A mensagem da legenda com aluno selecionado é curta ("do aluno selecionado"); o nome do aluno não precisa aparecer no card.
- O nome do campo da API (`valorFaturadoNoPeriodo`) permanece, para não quebrar nada; só o rótulo exibido muda.

## Fora de escopo

- Outros cards da Home (próxima aula, pacotes) e as telas Financeiro e Relatórios.
- Filtros por turma ou matéria na Home.
- Período com início depois do fim.
