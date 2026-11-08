# Feature Specification: Recorrência na criação de aulas

**Feature Branch**: `077-recorrencia-aulas`

**Created**: 2026-11-07

**Status**: Draft

**Input**: Card do Trello "Recorrência na criação de aulas (dias e horários fixos da turma)", pedido do usuário em 2026-10-30: hoje cada aula é cadastrada uma por vez; a professora combina dias e horários fixos com a turma (ex.: 2 aulas por semana) e precisa cadastrar cada aula na mão. Pedido: ter a opção de **recorrência na hora de cadastrar a aula**.

## Contexto (estado atual, lido do código)

- `AulaForm` (usado na tela `/aulas/nova`, na edição e no pop-up "Agendar aula" da Agenda) cadastra **uma** aula por envio; `AulaService.CadastrarAsync` cria uma aula por chamada: valida matéria/turma/aluno, valida **conflito de horário** (qualquer aula ativa da professora que se sobreponha), cria a aula "Agendada" (spec 064, inclusive de data passada), vincula os **alunos ativos da turma naquele momento** (ou o aluno da aula individual) e recalcula o lembrete da turma.
- A turma não tem horário fixo cadastrado; a aula guarda data, hora de início e hora de fim.
- Uma aula individual tem um aluno; uma aula de turma recebe todos os alunos ativos matriculados.

## Clarificações (respondidas pelo usuário em 2026-11-07)

- **Conflito**: se qualquer data da série conflitar com outra aula, **bloquear tudo** e mostrar os conflitos; nada é criado.
- **Passado**: a série só pode ter datas **de hoje em diante** (aulas passadas continuam podendo ser criadas uma a uma, como hoje).
- **Edição**: as aulas criadas são aulas comuns e independentes; editar ou cancelar é **aula por aula** nesta primeira versão (sem conceito de "série" no banco).
- **Limite**: no máximo **60 aulas** por série.
- **Dias e horários**: **cada dia da semana com seu próprio horário** (ex.: terça 14:00–15:00 e quinta 10:00–11:00).
- **Término**: a professora escolhe **por número de aulas OU por data final**.
- **Onde**: no cadastro de **nova aula**, na tela e no **pop-up da Agenda**; não aparece na edição de uma aula existente.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Criar várias aulas de uma vez com dias e horários fixos (Priority: P1)

Ao agendar uma aula (de turma ou individual), a professora marca "Repetir", informa a data inicial, adiciona os dias da semana com horários, escolhe o término (nº de aulas ou data final), confere a prévia das datas e agenda tudo com um clique.

**Why this priority**: É o pedido inteiro.

**Independent Test**: Em Nova aula, repetir terça 14:00–15:00 e quinta 10:00–11:00 por 10 aulas, a partir de uma segunda futura; conferir a prévia (10 datas) e que 10 aulas aparecem em Aulas.

**Acceptance Scenarios**:

1. **Given** o formulário de nova aula, **When** ela marca "Repetir", **Then** aparecem data inicial, a lista de dias/horários (com "Adicionar dia"), o término e o botão "Ver prévia".
2. **Given** dias e horários e um término, **When** ela pede a prévia, **Then** o sistema lista todas as datas que seriam criadas (data, dia da semana, horário) e marca as que conflitam com aulas existentes, com o total.
3. **Given** a prévia sem conflitos, **When** ela agenda, **Then** todas as aulas são criadas "Agendadas", com os alunos ativos da turma (ou o aluno da aula individual), e ela vê "N aulas agendadas".
4. **Given** término por **número de aulas** (ex.: 10), **Then** a série tem exatamente 10 aulas contando as ocorrências na ordem das datas.
5. **Given** término por **data final**, **Then** a série inclui todas as ocorrências até essa data (inclusive).
6. **Given** a data inicial que não cai num dos dias escolhidos, **Then** a primeira aula é a primeira data, a partir dela, que cai num dos dias.
7. **Given** o pop-up da Agenda, **When** ela agenda a série, **Then** o pop-up fecha e o calendário do mês mostra as novas aulas.

---

### User Story 2 - Conflitos e limites bloqueiam a série inteira (Priority: P1)

Se alguma data da série conflitar com outra aula, ou a série for maior que o limite ou começar no passado, nada é criado e a professora vê exatamente o que ajustar.

**Why this priority**: Evita séries "furadas" e aulas retroativas em lote por engano.

**Independent Test**: Criar uma aula numa das datas da série e tentar agendar a série; conferir que nenhuma aula nova foi criada e que o aviso lista a data em conflito.

**Acceptance Scenarios**:

1. **Given** uma data da série em conflito, **When** ela pede a prévia, **Then** a data aparece marcada como "conflito" e o botão de agendar fica indisponível (ou a tentativa de agendar é recusada).
2. **Given** um conflito, **When** o envio chega ao servidor, **Then** a resposta recusa com a lista das datas em conflito e **nenhuma aula é criada** (tudo ou nada).
3. **Given** uma série com mais de 60 aulas (por número ou por data final), **Then** é recusada com mensagem clara do limite (máximo de 60 aulas).
4. **Given** uma data inicial anterior a hoje, **Then** é recusada ("a série só pode começar de hoje em diante").
5. **Given** dois horários no mesmo dia da semana que se sobrepõem, ou fim não posterior ao início, **Then** é recusado antes de qualquer criação.
6. **Given** matéria, turma ou aluno inexistentes, **Then** a mesma recusa do cadastro de uma aula.

---

### Edge Cases

- Uma turma sem alunos ativos: as aulas são criadas sem alunos (igual ao cadastro de uma aula).
- Mesmo dia da semana escolhido duas vezes com horários diferentes (sem sobreposição): vale (ex.: duas aulas no mesmo dia).
- Término por data final anterior à data inicial, ou por quantidade < 1: recusado.
- Informar quantidade e data final juntas (ou nenhuma das duas): recusado.
- O horário das aulas passa por meia-noite: não suportado (fim > início, como hoje).
- Telas estreitas (360 px): o formulário de série não gera rolagem horizontal da página.
- Falha no meio da criação: nenhuma aula da série fica criada.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O cadastro de **nova** aula (tela e pop-up da Agenda) MUST oferecer a opção "Repetir"; a edição de uma aula existente MUST NOT oferecê-la.
- **FR-002**: A série MUST ser definida por data inicial, uma lista de **dias da semana, cada um com seu horário de início e fim**, e um término por **número de aulas OU data final** (exclusivos).
- **FR-003**: As datas da série MUST ser calculadas **no servidor**; a prévia MUST mostrar cada data com dia da semana, horário e se há conflito, e o total.
- **FR-004**: A série MUST começar em hoje ou depois e MUST ter no máximo **60 aulas**; fora disso é recusada com mensagem clara.
- **FR-005**: Se **qualquer** data conflitar com outra aula ativa da professora, o servidor MUST recusar a série inteira, listando as datas em conflito, e MUST NOT criar nenhuma aula.
- **FR-006**: A criação MUST ser **atômica**: ou todas as aulas (com seus alunos) são criadas, ou nenhuma.
- **FR-007**: Cada aula criada MUST ser uma aula comum, "Agendada", com os alunos ativos da turma no momento da criação (ou o aluno da aula individual), e o lembrete da turma MUST ser recalculado; não existe vínculo entre as aulas da série (editar e cancelar continuam aula por aula).
- **FR-008**: Os mesmos requisitos de referência do cadastro de uma aula (matéria, turma, aluno existentes; aula de turma sem `alunoId`; individual com `alunoId`) MUST valer para a série.
- **FR-009**: O cadastro e a edição de **uma** aula MUST NOT mudar de comportamento nem de resposta.
- **FR-010**: O frontend MUST NOT calcular datas, detectar conflitos nem contar aulas da série: só envia os parâmetros e exibe o que o servidor devolve.
- **FR-011**: A mudança MUST NOT criar tabela, coluna ou migração e MUST NOT adicionar dependência de pacote.

### Key Entities

Nenhuma nova. Cada ocorrência é uma `Aula` comum (com `AulaAluno`).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Uma série semanal de 10 aulas é criada com **um** envio, e as 10 aparecem em Aulas e na Agenda.
- **SC-002**: Com um conflito na série, **0** aulas são criadas e a resposta lista a(s) data(s) em conflito.
- **SC-003**: Uma série de 60 aulas é aceita e uma de 61 é recusada; uma série que começa ontem é recusada.
- **SC-004**: O cadastro de uma aula única continua idêntico (mesma resposta e mesmas regras).
- **SC-005**: Sem rolagem horizontal da página em 360 px no formulário de série.

## Assumptions

- "Dia da semana" segue o calendário (domingo a sábado); o fuso é o do sistema (relógio local, como no resto do projeto).
- "De hoje em diante" compara só a **data** (hoje é permitido).
- Os alunos de cada aula são os alunos ativos da turma **no momento do envio** (não se reavalia a cada data futura), como no cadastro de uma aula.
- A navegação após criar: na tela de Nova aula vai para a lista de Aulas; no pop-up fecha e recarrega o mês.

## Fora de escopo

- Editar ou cancelar a série inteira, guardar o conceito de série no banco e reagendar em bloco.
- Feriados e exceções de calendário.
- Recorrência mensal ou diária (só semanal por dias da semana).
- Horário fixo cadastrado na turma.
