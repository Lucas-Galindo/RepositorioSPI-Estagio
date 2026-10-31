# Feature Specification: Chamada Só nos Detalhes da Aula e Aviso de Aula Sem Frequência

**Feature Branch**: `064-chamada-so-nos-detalhes`

**Created**: 2026-10-31

**Status**: Draft

**Input**: Pedido do usuário em 2026-10-31 (card Trello "Bloquear chamada no agendamento; liberar frequência só nos detalhes após o início da aula"). **Esta spec substitui em parte a spec 055** (lançamento retroativo de presença ao criar a aula) e **evolui a spec 059** (aviso de aula pendente): ver "O que muda em relação às specs 055 e 059".

## Contexto (estado atual, lido do código)

- O formulário "Agendar aula" decide que a aula é "realizada" **só pela data**: se a data é de hoje ou do passado, mostra a seção "Presença", o texto "A data já chegou, então a aula será registrada como realizada. Desmarque quem faltou." e troca o botão por "Registrar aula realizada". O servidor repete a mesma comparação só por data e cria a aula já "Realizada", com presença e cobrança (spec 055).
- Por isso é possível lançar presença e falta de uma aula **que ainda não começou** (por exemplo, hoje às 15h, lançada às 10h).
- Nos detalhes da aula, o botão "Registrar como realizada" e as caixas de presença aparecem para toda aula "Agendada", sem olhar o horário; o servidor também aceita o registro a qualquer momento.
- O sistema não trata fuso horário: usa o relógio do servidor. A data e o horário da aula são guardados como hora local, sem fuso.
- A notificação de **aula pendente** (spec 059) já existe: nasce quando uma aula "Agendada" **terminou** sem ter o status atualizado, mostra data e horário, matéria e turma (ou "Atendimento individual" com o aluno) e tem o botão "Ir para a aula". Porém **não avisa** das aulas que terminaram antes de 19/10/2026 (corte de vigência da spec 059). No banco local isso deixa 2 aulas antigas sem aviso.
- O conflito de horário é verificado para a professora em todas as aulas dela, em qualquer data.

## Decisões de negócio (respondidas pelo usuário)

| Tema | Decisão |
|---|---|
| Popup de agendar | Serve **só para agendar**: **sem** a seção "Presença" e sem o texto "A data já chegou…". O botão é **sempre** "Agendar aula". |
| Status ao criar | **Toda aula criada nasce "Agendada"**, inclusive a de data passada. |
| Onde se faz a chamada | **Somente nos detalhes da aula.** |
| Quando a chamada é liberada | Somente quando o **horário atual ≥ data + horário de início** da aula, comparados **juntos**, no fuso **America/Sao_Paulo**. "Antiga" é tudo que é anterior ao horário atual. |
| Aula antiga sem frequência | Pode ter a **chamada da turma** aplicada nos detalhes daquela aula. |
| Antes do início | A chamada fica **bloqueada** nos detalhes, com a mensagem "A chamada ficará disponível a partir das HH:MM de DD/MM/AAAA." |
| Segurança | O **servidor** recusa registrar frequência de aula que não começou (409), mesmo que a tela seja burlada. |
| Aviso de correção | **Toda aula antiga sem frequência** gera um aviso nas notificações, com data, turma (ou atendimento individual, ou outro tipo de aula) e matéria, e um botão para os detalhes da aula. **Sem o corte de 19/10/2026**: aulas antigas também. |
| Quando o aviso nasce | Quando a aula **terminou** (regra atual da spec 059). A chamada é liberada desde o início, mas a aula em andamento ainda não está atrasada. |
| Banco de dados | Pode ser alterado. Entra a coluna `aula.frequencia_registrada_em` (data e hora, opcional), gravada quando a chamada é registrada. |
| Dados antigos | Frequência lançada antes do início por causa do problema: **avaliar e avisar, sem apagar nada** sem confirmação do usuário. |
| Fora de escopo | Portal do aluno; layout responsivo para celular. |

## Decisões menores adotadas (a confirmar na aprovação desta spec)

1. **Fuso fixo America/Sao_Paulo** para toda comparação de "já começou" e "já terminou", no servidor. O relógio do navegador **não decide**; a tela só exibe a resposta do servidor.
2. **Aula que já tem a frequência registrada não aceita nova chamada** (regra atual: só a "Agendada" aceita).
3. **A criação da aula deixa de aceitar presença** (o campo some da API); aula nova nunca gera frequência nem cobrança.
4. **Texto do aviso** passa a falar em frequência: "Esta aula já terminou e está sem frequência registrada. Registre a chamada para corrigir." O botão continua "Ir para a aula".
5. **Aula cancelada ou excluída não gera aviso** e o aviso some se a aula for cancelada, excluída, registrada ou tiver o término movido para o futuro (regra atual da 059).
6. **A configuração do corte de vigência** (`Notificacoes:AulasPendentesDesde`) **é removida**, em vez de ficar sem efeito (Princípio III). Ao ligar, aparecem de uma vez os avisos das aulas antigas, que é o objetivo.
7. **Chamada bloqueada não bloqueia** editar, cancelar nem excluir a aula.
8. **A coluna nova não é preenchida retroativamente**: as aulas já realizadas ficam sem data de registro (não há como saber).
9. **Numeração da migração do banco**: `26` (o `24` é da spec 062 e o `25` da 063).

## O que muda em relação às specs 055 e 059

| Tema | Antes | Esta spec |
|---|---|---|
| Criar aula com data de hoje ou passada (055, FR-001 a FR-007) | Mostra a presença no formulário e cria a aula "Realizada" | Cria sempre "Agendada"; sem presença no formulário |
| Quando a aula conta como "realizada" | Pela data (055) | Só quando a chamada é registrada, e só depois do início |
| Registrar frequência (055/056) | Em qualquer momento | Só depois do início, e o servidor confere |
| Aviso de aula pendente (059, FR-004) | Corte de vigência em 19/10/2026 | **Sem corte**: toda aula antiga sem frequência |
| Editar a descrição de aula realizada (055, US2) | Permitido | **Continua valendo** |

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Agendar aula sem lançar presença (Priority: P1)

A professora abre "Agendar aula", escolhe matéria, turma (ou aluno), data e horário, e agenda. O formulário não mostra presença em nenhum caso, e a aula sempre nasce "Agendada".

**Why this priority**: É o erro que gera os dados inconsistentes.

**Independent Test**: Agendar uma aula para hoje às 23:00 e outra para uma data de ontem; conferir que o formulário não mostra "Presença" nos dois casos, que o botão é "Agendar aula" e que as duas nascem "Agendada", sem frequência nem conta a receber.

**Acceptance Scenarios**:

1. **Given** o formulário de agendar, **When** a professora escolhe uma data de hoje, do passado ou do futuro, **Then** não aparece a seção "Presença" nem o texto "A data já chegou…", e o botão se chama "Agendar aula".
2. **Given** uma aula agendada com data passada, **When** ela é salva, **Then** o status é "Agendada", nenhuma presença é gravada e nenhuma conta a receber é criada.
3. **Given** uma chamada direta à API de criação com presenças, **When** ela é enviada, **Then** as presenças são ignoradas ou recusadas e a aula nasce "Agendada" sem frequência.

---

### User Story 2 - Fazer a chamada só nos detalhes, depois do início (Priority: P1)

Nos detalhes da aula, a professora faz a chamada dos alunos quando a aula já começou. Antes do início, a chamada está bloqueada e diz quando será liberada.

**Why this priority**: É a regra de negócio pedida e protege a frequência e a cobrança.

**Independent Test**: Abrir os detalhes de uma aula de hoje às 23:00 e ver a chamada bloqueada com a mensagem; abrir os de uma aula de ontem e conseguir registrar; tentar registrar a de hoje às 23:00 direto pela API e receber 409.

**Acceptance Scenarios**:

1. **Given** uma aula agendada para hoje cujo horário de início ainda não chegou, **When** a professora abre os detalhes, **Then** a chamada fica bloqueada e a tela mostra "A chamada ficará disponível a partir das HH:MM de DD/MM/AAAA."
2. **Given** uma aula agendada para uma data futura, **When** ela abre os detalhes, **Then** a chamada fica bloqueada com a mesma mensagem.
3. **Given** uma aula de hoje cujo horário de início já passou e que ainda não terminou, **When** ela abre os detalhes, **Then** a chamada está liberada.
4. **Given** uma aula de uma data passada sem frequência registrada, **When** ela abre os detalhes, **Then** a chamada da turma está liberada e, ao registrar, a aula vira "Realizada" com a frequência e a cobrança de sempre.
5. **Given** uma aula que ainda não começou, **When** o registro de frequência é enviado direto ao servidor (sem passar pela tela), **Then** o servidor recusa com 409 e nada é gravado.
6. **Given** uma aula que ainda não começou, **When** a professora edita, cancela ou exclui a aula, **Then** essas ações continuam disponíveis.
7. **Given** uma aula que passou a ter o horário de início mudado para o futuro, **When** ela abre os detalhes, **Then** a chamada volta a ficar bloqueada.

---

### User Story 3 - Ser avisada das aulas antigas sem frequência (Priority: P1)

Toda aula que já terminou e ainda está sem frequência registrada aparece nas notificações, com os dados da aula e um botão para ir aos detalhes e corrigir.

**Why this priority**: Sem o aviso, a aula antiga fica esquecida: sem frequência, sem cobrança e sem histórico.

**Independent Test**: Com 2 aulas antigas sem frequência (uma anterior a 19/10/2026), conferir que as duas aparecem como aviso, com data, horário, matéria e turma ou atendimento individual, e que o botão abre os detalhes; registrar a chamada e ver o aviso sumir.

**Acceptance Scenarios**:

1. **Given** uma aula ativa, "Agendada", que já terminou e está sem frequência, **When** a rotina de avisos roda ou a professora abre as notificações, **Then** existe exatamente um aviso para essa aula, **mesmo que ela seja anterior a 19/10/2026**.
2. **Given** o aviso, **When** a professora o abre, **Then** vê a data e o horário, a matéria, a turma (ou "Atendimento individual" com o aluno, ou o tipo da aula) e o botão "Ir para a aula".
3. **Given** o botão, **When** ela clica, **Then** abre os detalhes daquela aula, com a chamada liberada.
4. **Given** uma aula que ainda está em andamento (já começou e não terminou), **When** a rotina roda, **Then** ela **não** gera aviso.
5. **Given** o aviso de uma aula, **When** a frequência é registrada, a aula é cancelada ou excluída, ou o término é movido para o futuro, **Then** o aviso é resolvido e sai da lista e do contador.
6. **Given** várias execuções da rotina, **When** a aula continua sem frequência, **Then** o aviso não é duplicado.

---

### User Story 4 - Auditar e avisar sobre dados inconsistentes (Priority: P2)

O sistema passa a guardar quando a frequência foi registrada, e há uma consulta somente de leitura que lista aulas realizadas cuja frequência foi lançada antes do início. Nada é alterado ou apagado sem confirmação.

**Why this priority**: Mostra o estrago do problema antigo e impede que ele volte sem ninguém notar.

**Independent Test**: Registrar uma chamada e conferir que a data e a hora do registro foram gravadas; rodar a consulta e conferir que ela só lista, sem alterar nada.

**Acceptance Scenarios**:

1. **Given** uma chamada registrada, **When** ela é salva, **Then** a data e a hora do registro ficam guardadas na aula.
2. **Given** aulas realizadas antes desta mudança, **When** a consulta de inconsistência roda, **Then** ela lista as realizadas cujo início ainda está no futuro e **não altera nem apaga** nenhum dado.
3. **Given** aulas realizadas antigas, **When** a coluna nova é criada, **Then** elas ficam com a data de registro vazia e continuam como estão.

---

### Edge Cases

- **Aula hoje, antes do início**: chamada bloqueada. **Hoje, já iniciada e não terminada**: liberada. **Data futura**: bloqueada. **Data passada**: liberada.
- **Início exato**: no minuto em que o horário de início chega, a chamada é liberada (horário atual ≥ início).
- **Relógio do navegador diferente do servidor**: vale o servidor. A tela mostra o que o servidor informa.
- **Aula criada com data passada**: continua sujeita ao conflito de horário da professora, como qualquer aula.
- **Aula com frequência já registrada**: não aceita nova chamada; a descrição ainda pode ser editada (spec 055, US2).
- **Aula individual**: a mesma regra de horário vale para a chamada do aluno da aula.
- **Aula cancelada ou excluída**: sem aviso e sem chamada.
- **Muitos avisos de uma vez**: ao remover o corte, aulas antigas passam a aparecer todas juntas; cada uma tem seu aviso, que some quando a frequência é registrada.
- **Horário de verão**: o fuso America/Sao_Paulo é usado justamente para a comparação ficar correta mesmo que o servidor esteja em outro fuso.
- **Aula "Realizada" sem data de registro** (feita antes desta mudança): é aceita normalmente; só não entra na auditoria.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O formulário de agendar aula MUST NOT mostrar a seção "Presença" nem o texto "A data já chegou…", em nenhuma data, e o botão MUST ser sempre "Agendar aula".
- **FR-002**: Toda aula criada MUST nascer "Agendada", inclusive com data passada. A criação MUST NOT registrar frequência, atualizar a frequência dos alunos nem gerar cobrança, e MUST NOT aceitar presenças.
- **FR-003**: A chamada (presente ou falta) MUST ser feita somente nos detalhes da aula.
- **FR-004**: A chamada MUST ser liberada somente quando o horário atual for maior ou igual a data mais horário de início da aula, comparados juntos, no fuso America/Sao_Paulo. A comparação MUST NOT usar só a data.
- **FR-005**: Antes do início, os detalhes MUST bloquear a chamada e mostrar "A chamada ficará disponível a partir das HH:MM de DD/MM/AAAA." com o horário e a data de início da aula.
- **FR-006**: O servidor MUST recusar com 409 o registro de frequência de uma aula que ainda não começou, sem gravar nada, independentemente da tela.
- **FR-007**: O servidor MUST decidir "já começou" e "já terminou" pelo relógio do servidor no fuso America/Sao_Paulo; a tela MUST NOT decidir pelo relógio do navegador.
- **FR-008**: Em aula antiga (início anterior ao horário atual) sem frequência, os detalhes MUST permitir aplicar a chamada da turma, com a frequência e a cobrança de sempre.
- **FR-009**: Uma aula que já tem a frequência registrada MUST NOT aceitar nova chamada. Editar, cancelar e excluir MUST continuar disponíveis independentemente de a chamada estar bloqueada.
- **FR-010**: Toda aula ativa, "Agendada", cujo término já passou e sem frequência registrada MUST ter exatamente uma notificação de aviso de correção, **sem corte de data de vigência**. O sistema MUST NOT criar mais de um aviso para a mesma aula.
- **FR-011**: Aula em andamento (já começou e não terminou) MUST NOT gerar aviso de correção.
- **FR-012**: O aviso MUST mostrar a data e o horário da aula, a matéria, a turma (ou "Atendimento individual" com o nome do aluno, ou o tipo da aula) e o botão "Ir para a aula", que abre os detalhes da aula.
- **FR-013**: O aviso MUST ser resolvido quando a frequência é registrada, a aula é cancelada ou excluída, ou o término é movido para o futuro.
- **FR-014**: A configuração do corte de vigência dos avisos MUST ser removida do sistema (sem opção configurável sem efeito).
- **FR-015**: Ao registrar a chamada, o sistema MUST guardar a data e a hora do registro na aula. A coluna nova MUST NOT ser preenchida retroativamente nas aulas já realizadas.
- **FR-016**: O sistema MUST oferecer uma consulta **somente de leitura** que lista as aulas realizadas cujo início ainda está no futuro. O sistema MUST NOT alterar nem apagar dados por causa dela sem confirmação do usuário.
- **FR-017**: As specs 055 (FR-001 a FR-007) e 059 (FR-004) MUST receber uma nota apontando para esta spec como a regra vigente (Princípio V da constituição). A US2 da 055 continua valendo.

### Key Entities

- **Aula**: ganha o registro de **quando a frequência foi registrada** (opcional). O status continua "Agendada", "Realizada" ou "Cancelada".
- **Notificação (aula pendente)**: sem estrutura nova; só deixa de ter o corte de data e ganha o texto de correção.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% das aulas criadas, em qualquer data, nascem "Agendada", sem frequência e sem cobrança.
- **SC-002**: 0 frequências registradas antes do início da aula, pela tela e pela API.
- **SC-003**: 100% das aulas antigas sem frequência, inclusive as anteriores a 19/10/2026, têm aviso na próxima atualização da lista de notificações, e 0 avisos duplicados.
- **SC-004**: Da notificação até a tela de chamada da aula são no máximo 2 cliques.
- **SC-005**: Os 5 casos de borda (hoje antes do início, hoje já iniciada, data futura, data passada, em andamento) se comportam como a tabela da spec, incluindo o instante exato do início.
- **SC-006**: 0 dados apagados ou alterados pela auditoria de inconsistência.

## Assumptions

- Não há login do aluno; toda ação é da professora.
- O servidor pode estar em outro fuso; por isso o fuso America/Sao_Paulo é fixo na comparação.
- A rotina de avisos continua rodando a cada 5 minutos e ao carregar as notificações, como na spec 059.
- A cobrança e a frequência dos alunos ao registrar a chamada continuam as das specs 056 e 063, sem mudança.
- Esta spec é independente das specs 062 e 063; só a numeração da migração do banco depende delas.

## Fora de escopo

- Portal ou login do aluno.
- Layout responsivo para celular (o layout geral do app já não é responsivo; é assunto de outro card).
- Apagar ou corrigir sozinho aulas já registradas antes do início; isso só com confirmação do usuário.
