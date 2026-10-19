# Feature Specification: Progresso de Pacote de Aulas

**Feature Branch**: `058-progresso-pacote`

**Created**: 2026-10-19

**Status**: Draft

**Input**: Card Trello "Administração de aulas vencidas", mais as decisões do usuário em 2026-10-19:
- **aulas dadas** = presenças registradas no pacote;
- **aulas restantes** = total do pacote − aulas dadas.

## Contexto (estado atual, lido do código)

- Desde a spec 056, o pacote é do aluno e cada presença confirmada consome 1 do saldo. Desde a spec 052, a presença guarda qual vínculo de cobrança foi aplicado, e o pacote tem um "total de aulas" opcional.
- Hoje a ficha do aluno mostra só "Saldo: N aulas". O total do pacote não pode ser informado pela tela e não aparece em lugar nenhum. As "aulas dadas" calculadas pelo sistema (total − saldo) nunca são exibidas.
- A Home (dashboard) tem o painel "Pacotes em atenção". Ele é baseado no **saldo**: pacotes ativos com saldo de 2 ou menos.
- Aula cancelada nunca gera presença, então já não consome pacote.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Ver o progresso de cada pacote do aluno (Priority: P1)

Na ficha do aluno, cada pacote mostra:
- total contratado;
- aulas dadas;
- aulas restantes;
- uma barra de progresso ("6 de 10 aulas");
- destaque quando está perto do fim ou esgotado.

Ao abrir o pacote, a professora vê a lista das aulas dadas, com data, horário, matéria, turma e status.

**Why this priority**: É o pedido central do card.

**Independent Test**: Cadastrar um pacote de 10 aulas, registrar 3 presenças e 1 falta em aulas diferentes, cancelar uma aula, e conferir "3 de 10", "restam 7" e a lista com 3 aulas.

**Acceptance Scenarios**:

1. **Given** um pacote de 10 aulas com 3 presenças registradas, **When** a professora abre a ficha do aluno, **Then** vê "3 de 10 aulas", "restam 7" e a barra em 30%.
2. **Given** uma falta ou uma aula cancelada, **When** a professora confere o pacote, **Then** elas não contam como aula dada.
3. **Given** um pacote com 1 ou 2 aulas restantes, ou com 0, **When** a ficha é exibida, **Then** ele aparece destacado como "Atenção" ou "Esgotado".
4. **Given** pacotes de tamanhos diferentes (ex.: 4, 10, 20), **When** exibidos, **Then** cada um usa o próprio total.
5. **Given** a professora abre a lista de aulas de um pacote, **When** ela carrega, **Then** mostra exatamente as aulas que entraram na contagem de aulas dadas, das mais recentes para as mais antigas.
6. **Given** pacotes antigos (inativos) do aluno, **When** a professora mostra os excluídos, **Then** eles aparecem com o próprio progresso.

---

### User Story 2 - Informar o total ao cadastrar o pacote (Priority: P1)

Ao cadastrar ou editar um pacote, a professora informa o **total de aulas**. No cadastro, o saldo começa igual ao total.

**Why this priority**: Sem o total, não há "X de N" nem "restantes".

**Independent Test**: Cadastrar um pacote informando só o total (10) e conferir que o saldo também ficou 10 e o progresso mostra "0 de 10".

**Acceptance Scenarios**:

1. **Given** o formulário de vínculo com modalidade Pacote, **When** a professora informa total 10 e salva, **Then** o pacote nasce com total 10 e saldo 10.
2. **Given** um pacote existente, **When** a professora edita o total, **Then** o sistema recusa um total menor que as aulas já dadas (presenças registradas).

---

### User Story 3 - Visão de negócio dos pacotes na Home (Priority: P2)

Na Home, uma seção "Pacotes de aulas" mostra:
- **indicadores**: pacotes ativos, pacotes concluídos, pacotes próximos do fim, total de aulas dadas e total de aulas restantes;
- **lista de renovação**: os pacotes que precisam ser renovados.

Os filtros são por aluno e por período.

**Why this priority**: É a visão consolidada pedida no card; depende do mesmo cálculo da US1.

**Independent Test**: Com 3 alunos e pacotes em estados diferentes, conferir que os indicadores e a lista batem com a ficha de cada aluno.

**Acceptance Scenarios**:

1. **Given** pacotes em estados variados, **When** a Home carrega, **Then** os indicadores contam:
   - **ativos**: pacotes ativos com aulas restantes;
   - **concluídos**: pacotes, ativos ou não, com 0 restantes;
   - **próximos do fim**: pacotes ativos com 1 ou 2 restantes;
   - a soma de aulas dadas e de restantes dos pacotes ativos.
2. **Given** a lista de renovação, **When** exibida, **Then** traz os pacotes ativos esgotados ou próximos do fim, esgotados primeiro. Os números são os mesmos da ficha do aluno.
3. **Given** um filtro de aluno, **When** aplicado, **Then** indicadores e lista consideram só os pacotes desse aluno.
4. **Given** um período, **When** aplicado, **Then** o indicador "aulas dadas no período" conta só as presenças desse período. Os demais indicadores continuam retratando a situação atual dos pacotes.

### Edge Cases

- **Pacote sem total informado** (criado antes desta spec): o total é estimado como aulas dadas + saldo atual e aparece marcado como "estimado". Sem saldo nem total, mostra só as aulas dadas, sem barra.
- **Presenças antigas**, de antes de a presença guardar o vínculo (spec 052): não entram na contagem, por decisão do usuário. O progresso pode mostrar menos aulas dadas que o uso real.
- **Saldo editado à mão**: o progresso continua contando presenças. O bloqueio por pacote esgotado (spec 041) segue usando o saldo, como hoje.
- **Mais aulas dadas que o total** (ex.: total editado para baixo antes desta regra): restantes = 0 e o estado é "Esgotado"; nunca aparece número negativo.

## Requirements *(mandatory)*

### Functional Requirements

**Cálculo**
- **FR-001**: Aulas dadas de um pacote MUST ser o número de presenças registradas com esse pacote como vínculo aplicado. Faltas e aulas canceladas MUST NOT contar.
- **FR-002**: Aulas restantes MUST ser o total do pacote − as aulas dadas, com mínimo de 0. O total vem do informado, ou é estimado como aulas dadas + saldo quando não foi informado.
- **FR-003**: O estado do pacote MUST ser:
  - "Esgotado" com 0 restantes;
  - "Atenção" com 1 ou 2 restantes;
  - "Em dia" nos demais casos;
  - "Sem total" quando o total não pode ser determinado.
- **FR-004**: Ficha do aluno e Home MUST usar o mesmo cálculo, num único lugar do sistema.

**Visão individual**
- **FR-005**: A ficha do aluno MUST mostrar, por pacote: total (com a marca "estimado" quando for o caso), aulas dadas, restantes, barra de progresso e estado.
- **FR-006**: A professora MUST poder abrir a lista das aulas dadas de um pacote, com data, horário, matéria, turma (ou "Atendimento individual") e status.

**Cadastro**
- **FR-007**: O formulário de pacote MUST ter o campo "Total de aulas". No cadastro com total e sem saldo, o saldo MUST começar igual ao total.
- **FR-008**: A edição MUST recusar um total menor que as aulas dadas (FR-001).

**Visão de negócio**
- **FR-009**: A Home MUST mostrar a seção "Pacotes de aulas" com os indicadores da US3 e a lista de renovação. Essa lista substitui o painel "Pacotes em atenção", que era baseado no saldo.
- **FR-010**: A seção MUST permitir filtrar por aluno e por período (o período afeta "aulas dadas no período").

**Restrição**
- **FR-011**: O sistema MUST NOT criar campo ou tabela nova: o cálculo usa o vínculo de cobrança e as presenças que já existem.

### Key Entities

- **Progresso de pacote** (calculado, nunca gravado): total, se o total é estimado, aulas dadas, restantes e estado.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Para qualquer pacote, aulas dadas = número de aulas listadas no detalhe dele.
- **SC-002**: Ficha do aluno e Home mostram os mesmos números de dadas, restantes e estado para o mesmo pacote.
- **SC-003**: Falta e aula cancelada nunca alteram o progresso.
- **SC-004**: Pacotes de tamanhos diferentes mostram percentuais corretos (ex.: 3 de 4 = 75%; 3 de 20 = 15%).

## Assumptions

- **Limite de "Atenção"**: é o limite já usado no sistema (2 aulas).
- **Período padrão**: o mesmo do dashboard (mês corrente).
- **Bloqueio por pacote esgotado** (spec 041): não muda; continua baseado no saldo.
- **Quem vê**: a visão de negócio é da professora (Home). Admin não tem essa tela.
