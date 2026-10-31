# Feature Specification: Turmas de Pacote e Pacote Coletivo por Turma

**Feature Branch**: `063-pacote-fora-da-turma`

**Created**: 2026-10-29 (revisada em 2026-10-30)

**Status**: Draft

**Input**: Card Trello "Alunos de pacote fora do cadastro fixo das turmas" (origem: anotações da reunião do dia 22). As dúvidas de negócio do card foram respondidas pelo usuário em 2026-10-29 e 2026-10-30 e estão registradas nesta spec. **Esta spec muda parte da regra da spec 056** (ver "O que muda em relação à spec 056").

> **Revisão de 2026-10-30.** A primeira versão desta spec assumia que o aluno de pacote *não ficava matriculado* na turma. O usuário esclareceu que o aluno **pode se matricular na turma e contratar o pacote**, participando junto com os demais ("pacote coletivo"). A spec foi refeita nesse modelo, que é mais simples: a participação continua pela matrícula, e o pacote passa a pertencer a uma turma.

## Contexto (estado atual, lido do código)

- A turma não tem tipo: qualquer aluno pode ser **matriculado** nela (`alunos_turma`) e toda aula nova da turma inclui os alunos ativos matriculados naquele momento. A turma não tem horário fixo cadastrado; cada aula é cadastrada com data e horário, **uma por vez** (não existe recorrência de aulas; ela virou um card próprio, ver "Fora de escopo").
- O **Pacote** é do aluno, sem turma (spec 056): vale em qualquer aula, de turma ou individual, e cada presença consome 1 aula do saldo. O sistema trata o pacote como já pago: não gera contas a receber nem parcelas dele. A **Mensalidade** é do aluno, vale para tudo e não cobra por presença. A **Avulsa** cobra por presença, num contexto (uma turma, ou atendimento individual) e exige que o aluno esteja matriculado na turma.
- A **reposição** (spec 056) já permite à professora colocar, aula a aula, um aluno de fora numa aula agendada de turma e removê-lo antes de a aula acontecer.
- O **conflito de horário** já é verificado para a professora em todas as aulas dela; como todo aluno só participa de aulas da própria professora, ele nunca fica em duas aulas sobrepostas.
- A chamada é feita na página da aula (`aulas/[id]`), e a regra de presença e cobrança fica em `AulaService` + `RegraCobrancaPresenca` (spec 056).

## Decisões de negócio (respondidas pelo usuário)

| Tema | Decisão |
|---|---|
| Tipo de turma | Toda turma é **de Mensalidade** ou **de Pacote**. Quem paga mensalidade tem aulas com quem paga mensalidade; quem paga pacote fica na turma de pacote. |
| Pacote coletivo | O aluno se **matricula na turma de pacote e contrata o pacote dela**, participando das aulas junto com os demais. Exemplo do usuário: a Turma A, com 10 alunos, começa na próxima semana e tem 10 aulas no mês. |
| Turma fechada | A turma de pacote começa e termina junta, com **data de início e de fim**. **A turma não muda**: depois que começa, **ninguém novo entra** (nem por matrícula nem por pacote). **Os alunos podem sair.** |
| Aulas individuais extras | Quem tem pacote coletivo pode **pagar aulas individuais extras** (aula avulsa particular), cobradas individualmente **sem decrementar** o pacote. |
| Todo pacote tem um grupo | **Não existe pacote com saldo que vale para qualquer aula.** Todo pacote pertence a um grupo: ou **aulas individuais** (aulas só do aluno com a professora) ou **uma turma de pacote** (um pacote por turma). |
| Pacote só vale no seu grupo | O pacote de aulas individuais só é consumido em aula individual; o pacote de uma turma só é consumido em aula **dessa** turma. **Fora do grupo, o aluno paga** uma cobrança normal (avulsa), gerada na presença e acompanhada em Contas a Receber; não consome saldo. |
| Aluno avulso numa turma de pacote | Participa como "outro aluno" e a cobrança continua **avulsa**. |
| Mensalidade, Semestral e Anual | **Acesso a tudo**, sem custo adicional, inclusive entrar numa turma de pacote como outro aluno. |
| Mensalidade × pacote | Quem tem pacote não tem mensalidade (regra da spec 056). |
| Pacote esgotado | A presença é **barrada** (regra atual da spec 041; cai na Avulsa do contexto se houver). |
| Presença | **Todos** os alunos vinculados a uma aula, de qualquer tipo, têm direito a **presença e falta**. |
| Chamada | Os alunos que não são da turma aparecem numa seção abaixo da lista, com o título **"Outros alunos"**. |
| Pacotes já cadastrados | **Migrar diretamente** para o novo modelo. |
| Reposição avulsa | **Continua existindo.** |
| Turmas misturadas existentes | Os dados atuais são de teste; pode-se **ajustar ou excluir** (sempre com backup antes). |
| Saída do aluno | O aluno que sai sem ter pago tudo **acerta pelo que consumiu** (pacote de R$ 100 para 10 aulas com 7 aulas consumidas: paga R$ 70). **Vira outro card** (ver "Fora de escopo"). |
| Vários pacotes | O aluno pode ter **0 ou N pacotes de turma ativos ao mesmo tempo** (um por turma; ele pode estar, por exemplo, na turma de Matemática Básica e na de Matemática Ensino Médio). **A única limitação é o pacote de aulas individuais: no máximo um ativo.** |
| Cobrança da presença | Quando o aluno vinculado à aula tem **presença**, a cobrança é **gerada automaticamente em Contas a Receber** (quando a regra manda cobrar). |
| Fim da turma | Quando a turma de pacote acaba, **os pacotes dela ficam inativos**, **sem zerar o saldo**. A turma acaba **pelos dois jeitos, o que vier primeiro**: passou a data de fim (uma rotina automática diária inativa os pacotes) ou a professora inativou a turma. |
| Aluno excluído que volta | Se o aluno é excluído de forma lógica e volta com a **turma ainda em andamento**, ele **mantém o direito de usar as aulas do pacote** daquela turma (excluir o aluno não mexe no pacote nem na matrícula). |
| Reativar pacote encerrado | A professora só reativa um pacote de turma **se a turma voltar a estar em andamento** (ativa e com data de fim de hoje em diante, por exemplo depois de prorrogar a data de fim). |
| Voltar à turma | Quem **saiu** da turma de pacote (foi desvinculado) **pode voltar** enquanto a turma está em andamento, **se o pacote dele daquela turma está ativo**. É exceção à regra "ninguém novo entra", porque ele não é novo. |
| Qual pacote consome | **Sempre o do grupo da aula** (o da própria turma, ou o de aulas individuais). Como há no máximo um pacote ativo por grupo, nunca há dúvida de qual consumir. |

## Decisões menores adotadas (a confirmar na aprovação desta spec)

1. **O cadastro do pacote de turma matricula o aluno** nessa turma, na mesma gravação. A **matrícula direta** na turma de pacote é recusada para quem **não** tem pacote ativo dela (a turma só recebe aluno pelo pacote), para não entrar aluno de mensalidade ou avulso como membro. A única exceção é a volta de quem já tem pacote ativo dela (FR-004).
2. **Pacote de turma exige turma de pacote** ativa, da professora e **ainda não iniciada** (data de início de hoje em diante) no cadastro. Turma sem data (migrada) nunca é considerada iniciada.
3. **Tipo da turma, turma do pacote e data de início (depois de preenchida) não mudam** (a turma "não muda"; mudar a data de início reabriria uma turma fechada). Para trocar, cria-se outro. A data de fim pode ser ajustada (prorrogar ou antecipar).
4. **Pacotes de turma: 0 ou N ativos por aluno, um por turma** (não faz sentido contratar a mesma turma duas vezes). **Pacote de aulas individuais: no máximo um ativo por aluno.** Isso substitui o "um pacote ativo por aluno" da spec 056, que passa a valer só para as aulas individuais. Para renovar o pacote de um grupo, inativa-se o anterior e cadastra-se outro.
5. **Sair da turma continua permitido** (desvincular). Enquanto a turma está em andamento, o pacote do aluno **não é alterado** pela saída; quando a turma acaba, ele fica inativo como qualquer outro pacote dela. O acerto financeiro da saída é do card de saída.
6. **Fim de turma verificado automaticamente**: ao iniciar a aplicação e a cada poucos minutos configuráveis (padrão de uma hora), sem depender de alguém abrir o sistema; o atraso máximo para inativar os pacotes é, portanto, de uma hora depois da virada do dia. A turma **sem data de fim** (migrada) só acaba quando a professora a inativa. **Reativar a turma não reativa os pacotes**: a professora reativa cada pacote, se quiser.
7. **Pacote já inativo por outro motivo** (exclusão pela professora) não é reativado por nada automático.
8. **Turma de pacote migrada em andamento**: a data de fim fica vazia e a professora preenche ao editar. **Turma nova de pacote** exige início e fim.
9. **Migração** (hoje todo pacote existente é "sem turma"): (a) aluno com pacote ativo matriculado em **exatamente uma** turma **ativa** onde **todos os alunos ativos têm pacote ativo**: a turma vira Pacote e o pacote aponta para ela; (b) aluno com pacote ativo **sem nenhuma matrícula**: o pacote vira o **pacote de aulas individuais**; (c) **os demais** (matriculado em várias turmas, em turma **inativa** ou numa turma que mistura modalidades): nada é alterado e o caso é **listado** para a professora decidir. Turma inativa não é convertida porque a rotina de fim de turma inativaria o pacote logo em seguida. Nenhuma matrícula é removida. **Atenção**: o pacote sem turma passa a valer só em aula individual; até a professora decidir os casos (c), as presenças desses alunos em turma serão cobradas como avulsas.
10. **Conflito de horário do aluno** já é impedido pela regra existente da professora; a spec exige **teste** disso e **proíbe** uma segunda regra duplicada.
11. **Numeração da migração do banco**: `25` (o `24` está reservado pela spec 062).

## O que muda em relação à spec 056

| Tema | Spec 056 (vigente hoje) | Esta spec |
|---|---|---|
| Pacote | Do aluno, **sem turma**, vale em qualquer aula | **De um grupo**: aulas individuais, ou uma turma de pacote |
| Pacote em aula individual | Consome o pacote | Só consome o pacote **de aulas individuais**; o de turma não consome (cobra à parte) |
| Pacote em aula de turma | Consome, de qualquer turma | Só consome o pacote **daquela turma**; em outra turma, ou com pacote de aulas individuais, o aluno paga (avulsa) |
| Turma | Sem tipo | **Mensalidade** ou **Pacote**, fechada depois que começa |
| Participação | Por matrícula | **Continua por matrícula** (na turma de pacote, feita pelo cadastro do pacote) |
| Pacotes por aluno | Um pacote ativo por aluno | **0 ou N pacotes de turma** (um por turma) e **no máximo um** de aulas individuais |
| Fim do pacote | Só quando a professora o inativa ou o saldo acaba | **Também quando a turma acaba** (saldo preservado) |

Mensalidade, Avulsa, reposição e o bloqueio por pacote esgotado (spec 041) **não mudam**.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Cobrar a presença conforme o escopo do pacote (Priority: P1)

A professora faz a chamada e o sistema cobra cada presença conforme o vínculo do aluno e o grupo da aula: o pacote só é consumido numa aula do **seu grupo** (o de uma turma, só em aula dessa turma; o de aulas individuais, só em aula individual). Fora do grupo o aluno paga uma cobrança normal. A mensalidade nunca cobra; quem não tem pacote paga avulso.

**Why this priority**: É a regra de negócio nova e a que mexe em dinheiro.

**Independent Test**: Registrar presença de um aluno com pacote da turma A em aula da turma A, aula da turma B e aula individual; conferir que só a primeira consome saldo e que as outras duas geram cobrança.

**Acceptance Scenarios**:

1. **Given** um aluno com pacote da turma A com saldo, **When** a presença dele é registrada numa aula da turma A, **Then** o saldo diminui 1 e nenhuma conta a receber é criada.
2. **Given** o mesmo aluno, **When** a presença é registrada numa aula da turma B (fora do grupo), **Then** o saldo **não** muda, ele aparece em "Outros alunos" dessa aula e é cobrado como avulso (avulsa do contexto, ou o valor da aula do aluno).
3. **Given** o mesmo aluno, **When** a presença é registrada numa aula individual extra, **Then** o saldo **não** muda e a aula individual é cobrada automaticamente em Contas a Receber (avulsa do atendimento individual, ou o valor da aula do aluno; FR-023).
4. **Given** um aluno com pacote de aulas individuais com saldo, **When** a presença é registrada numa aula individual, **Then** o saldo diminui 1; **When** a presença é registrada numa aula de turma (como "outro aluno"), **Then** o saldo **não** muda e ele é cobrado como avulso.
5. **Given** um aluno com Mensalidade (ou Semestral/Anual), **When** a presença é registrada em qualquer aula, inclusive de turma de pacote, **Then** nada é cobrado.
6. **Given** um aluno avulso (ou sem vínculo), **When** a presença é registrada numa aula de turma de pacote, **Then** ele é cobrado como avulso, como em qualquer outra aula.
7. **Given** um aluno com pacote esgotado, **When** a presença é registrada numa aula do grupo desse pacote e ele não tem Avulsa para cair, **Then** ela é barrada com o aviso de renovar o pacote. Numa aula fora do grupo, o pacote esgotado não interfere (cobrança avulsa).
8. **Given** qualquer aluno de pacote, **When** ele falta, **Then** o saldo não muda.

---

### User Story 2 - Turma de pacote fechada, com o pacote coletivo do aluno (Priority: P1)

A professora cria uma turma do tipo Pacote, com data de início e de fim, e cadastra o pacote de cada aluno apontando para essa turma; o cadastro matricula o aluno. Depois que a turma começa, ela não recebe ninguém novo, mas os alunos podem sair.

**Why this priority**: É o objetivo do card: o pacote passa a ser coletivo e fechado.

**Independent Test**: Criar uma turma de pacote que começa amanhã, cadastrar o pacote de dois alunos e conferir que os dois ficaram matriculados e entram nas aulas; depois da data de início, tentar cadastrar um terceiro e ser recusada.

**Acceptance Scenarios**:

1. **Given** uma turma de pacote ainda não iniciada, **When** a professora cadastra um pacote de aluno apontando para ela, **Then** o vínculo é criado e o aluno fica matriculado na turma.
2. **Given** uma turma de pacote, **When** é criada uma aula nova, **Then** ela inclui os alunos matriculados, como em qualquer turma.
3. **Given** uma turma de pacote que já começou, **When** a professora tenta cadastrar um pacote novo para ela ou matricular alguém que **não** tem pacote ativo dela, **Then** o sistema recusa, explicando que a turma está fechada.
4. **Given** uma turma de pacote, **When** a professora tenta matricular diretamente um aluno que não tem pacote ativo dela, **Then** o sistema recusa, explicando que a entrada é pelo cadastro do pacote.
5. **Given** uma turma de pacote já iniciada, **When** a professora tenta mudar a data de início ou o tipo da turma, **Then** o sistema recusa; a data de fim pode ser ajustada.
6. **Given** um aluno matriculado numa turma de pacote, **When** a professora o desvincula, **Then** a saída é aceita e o pacote, o saldo e os pagamentos dele não são alterados.
7. **Given** uma turma de mensalidade, **When** é criada uma aula ou um aluno é matriculado, **Then** tudo funciona como hoje.
8. **Given** um aluno com pacote de turma, **When** a professora cadastra uma Avulsa de atendimento individual para ele, **Then** o cadastro é aceito (aulas individuais extras).

---

### User Story 3 - "Outros alunos" na chamada (Priority: P1)

Na chamada, os alunos da turma ficam na lista principal e os que estão na aula por reposição (de fora da turma ou do pacote) ficam numa seção abaixo, "Outros alunos". Todos têm presença e falta e entram no registro da aula.

**Why this priority**: O card exige que o aluno de fora conste na chamada como presença e como registro de aula.

**Independent Test**: Adicionar um aluno de fora a uma aula agendada, abrir a chamada e conferir as duas seções e que a presença dele gera o efeito de cobrança certo.

**Acceptance Scenarios**:

1. **Given** uma aula com alunos da turma e um aluno por reposição, **When** a professora abre a chamada, **Then** a lista principal mostra os da turma e a seção "Outros alunos" mostra o de fora.
2. **Given** uma aula sem alunos de fora, **When** a professora abre a chamada, **Then** a seção "Outros alunos" não aparece.
3. **Given** um aluno em "Outros alunos", **When** a presença ou a falta é registrada, **Then** a regra de cobrança é a da User Story 1 e o registro entra na aula como os demais.
4. **Given** um aluno de mensalidade que entra por reposição numa turma de pacote, **When** a presença é registrada, **Then** não há custo adicional.

---

### User Story 4 - Migrar os pacotes que já existem (Priority: P2)

Os alunos de pacote hoje matriculados numa turma passam a ter o pacote apontando para ela, e a turma vira do tipo Pacote, sem apagar vínculo, matrícula nem aula.

**Why this priority**: Sem a migração o modelo novo e os dados atuais ficam inconsistentes.

**Independent Test**: Rodar a migração num banco de teste com um aluno de pacote matriculado em uma turma, e conferir o vínculo apontando para a turma, o tipo Pacote e a matrícula intacta.

**Acceptance Scenarios**:

1. **Given** um aluno com pacote ativo matriculado em exatamente uma turma **ativa** onde todos os alunos ativos têm pacote ativo, **When** a migração roda, **Then** a turma vira do tipo Pacote e o pacote passa a apontar para ela.
2. **Given** um aluno com pacote ativo **sem nenhuma matrícula**, **When** a migração roda, **Then** o pacote continua sem turma e passa a ser o **pacote de aulas individuais** dele.
3. **Given** um aluno de pacote matriculado em várias turmas, numa turma **inativa** ou numa turma que mistura pacote com outras modalidades, **When** a migração roda, **Then** nada é alterado para ele e o caso fica listado para a professora decidir (enquanto isso, o pacote sem turma vale só em aula individual).
4. **Given** qualquer banco, **When** a migração roda, **Then** nenhum vínculo é desativado, somado ou apagado, nenhuma matrícula é removida e nenhuma aula é alterada.

---

### User Story 5 - Fim da turma e volta do aluno (Priority: P2)

Quando a turma de pacote acaba, os pacotes dela ficam inativos sem perder o saldo. Enquanto ela está em andamento, quem saiu ou foi excluído e voltou continua com direito às aulas do pacote.

**Why this priority**: Fecha o ciclo de vida do pacote coletivo e evita pacote "vivo" em turma que já terminou.

**Independent Test**: Com uma turma de pacote cuja data de fim passou, rodar a rotina e conferir que os pacotes ficaram inativos com o mesmo saldo; com outra turma em andamento, excluir e reativar um aluno e conferir que o saldo dele continua utilizável.

**Acceptance Scenarios**:

1. **Given** uma turma de pacote com data de fim anterior a hoje e dois pacotes ativos com saldo, **When** a rotina diária roda, **Then** os dois pacotes ficam inativos e mantêm o saldo, o valor e o total de aulas.
2. **Given** uma turma de pacote que a professora inativa, **When** a inativação é salva, **Then** os pacotes ativos dela ficam inativos com o saldo preservado.
3. **Given** um aluno com pacote de uma turma em andamento, **When** ele é excluído de forma lógica e depois reativado, **Then** o pacote e a matrícula estão como antes e o saldo continua utilizável na presença.
4. **Given** um aluno que saiu da turma em andamento e tem o pacote dela ativo, **When** a professora o matricula de novo, **Then** a matrícula é aceita, mesmo com a turma já iniciada.
5. **Given** um pacote inativado pelo fim da turma, **When** a professora tenta reativá-lo com a turma encerrada, **Then** o sistema recusa; depois de prorrogar a data de fim, a reativação é aceita.
6. **Given** um aluno com pacote da turma de Matemática Básica e da turma de Matemática Ensino Médio, ambos ativos, **When** a presença é registrada numa aula de cada turma, **Then** cada uma consome o pacote da sua própria turma. **Given** esse mesmo aluno também com o pacote de aulas individuais, **When** a presença é registrada numa aula individual, **Then** consome o de aulas individuais. Numa aula de uma terceira turma, nenhum deles é consumido e ele paga avulso.
7. **Given** um aluno que já tem pacote ativo de aulas individuais, **When** a professora tenta cadastrar outro pacote de aulas individuais, **Then** o sistema recusa; para renovar, ela inativa o anterior e cadastra o novo.
8. **Given** um aluno que já tem pacote ativo da turma A, **When** a professora tenta cadastrar outro pacote para a turma A, **Then** o sistema recusa; para a turma B, o cadastro é aceito.

---

### Edge Cases

- **Conflito de horário do aluno**: já é impossível, porque a professora não pode ter duas aulas sobrepostas e todo aluno só participa das aulas dela. Fica coberto por um teste e **sem regra nova**.
- **Aluno de pacote em outra turma** (o pacote é da turma A, ele entra por reposição na turma B): **não** consome o saldo; aparece em "Outros alunos" da turma B e paga avulso.
- **Aluno com pacote de aulas individuais numa aula de turma** (como "outro aluno"): não consome o saldo e paga avulso.
- **Aluno que saiu da turma A mas ainda participa de uma aula dela como "outro aluno"**: o pacote da turma A **vale**, porque a aula é do grupo do pacote (o que decide é a turma da aula, não a matrícula).
- **Pacote esgotado dentro da própria turma**: continua na chamada; a presença é barrada ou cai na Avulsa da turma, conforme a regra atual.
- **Aluno que saiu da turma de pacote com a turma em andamento**: o pacote continua ativo. Ele deixa de entrar nas aulas novas da turma, mas pode ser matriculado de novo (FR-004) ou participar como "outro aluno". O acerto financeiro é do card de saída.
- **Aluno que saiu e a turma acabou**: o pacote fica inativo (FR-019), com o saldo preservado.
- **Aluno excluído de forma lógica e reativado com a turma em andamento**: o pacote e a matrícula nunca foram tocados, então ele volta usando o saldo (FR-020). Se a turma já acabou nesse meio-tempo, o pacote já está inativo.
- **Aluno excluído quando a turma acaba**: a rotina inativa o pacote dele também, mesmo com o aluno inativo.
- **Reativar um pacote de turma encerrada**: só com a turma ativa e em andamento (FR-021), por exemplo depois de prorrogar a data de fim; sem isso, é recusado.
- **Turma inativada e reativada**: os pacotes que a inativação desativou continuam inativos até a professora reativá-los um a um.
- **Aluno com pacote de turma e pacote de aulas individuais**: convivem; na aula da turma consome o da turma, na aula individual consome o de aulas individuais (FR-007 e FR-008).
- **Turma de pacote migrada, sem datas**: nunca é considerada "iniciada" e continua aceitando pacote novo até a professora preencher as datas. É a consequência assumida de a migração não inventar datas.
- **Data de fim da turma de pacote já passada**: a rotina diária inativa os pacotes ativos dela, sem mexer em saldo, matrícula, aulas ou pagamentos (FR-019). A turma em si continua ativa; quem a inativa é a professora.
- **Prorrogar a data de fim depois de os pacotes terem sido inativados**: os pacotes **não** voltam sozinhos; a professora os reativa (FR-021).
- **Data de fim hoje**: a turma ainda está em andamento até o fim do dia; a rotina só inativa quando a data de fim é anterior a hoje.
- **Aluno com pacote de turma e uma Avulsa da mesma turma**: convivem (a Avulsa é o destino do pacote esgotado); a matrícula exigida pela Avulsa já existe.
- **Aluno com pacote de turma que também está matriculado numa turma de mensalidade**: permitido; nas aulas dessa outra turma o pacote **não** se aplica (outro grupo) e ele paga como avulso daquele contexto.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Toda turma MUST ter um tipo, **Mensalidade** ou **Pacote**. A turma de pacote nova MUST ter data de início e de fim (`fim >= início`); a de mensalidade MUST NOT ter datas.
- **FR-002**: Todo pacote MUST pertencer a um grupo: **uma turma de pacote** (`turmaId` preenchido) ou **aulas individuais** (sem turma). MUST NOT existir pacote com saldo que valha para qualquer aula. A Mensalidade (e os planos da spec 062) MUST continuar sem turma.
- **FR-003**: Para **cadastrar** um pacote de turma, a turma MUST existir, ser da professora, estar ativa, ser do tipo Pacote e não ter começado (data de início de hoje em diante). O cadastro MUST matricular o aluno nessa turma na mesma gravação, se ele ainda não estiver. O aluno MUST NOT ter dois pacotes ativos da mesma turma.
- **FR-004**: A turma de pacote MUST NOT receber matrícula direta de quem **não** tem pacote ativo dela. Depois que ela começa, MUST NOT receber aluno novo, nem por matrícula nem por pacote. **Exceção**: quem já tem pacote **ativo** daquela turma e saiu dela MUST poder ser matriculado de novo enquanto a turma está em andamento. A saída de alunos MUST continuar permitida.
- **FR-005**: O tipo da turma, a turma de um pacote e a data de início da turma (depois de preenchida) MUST NOT mudar depois de criados. A data de fim MAY ser ajustada (prorrogar ou antecipar), respeitando `fim >= início`.
- **FR-006**: A saída do aluno de uma turma MUST NOT apagar nem alterar vínculo, saldo ou pagamentos. O acerto financeiro da saída está fora desta spec.
- **FR-007**: A decisão de cobrança de cada presença MUST seguir, nesta ordem: (1) mensalidade ou plano (spec 062) ativo: nenhuma cobrança; (2) pacote **aplicável** com saldo: consome 1 aula; (3) Avulsa do contexto da aula; (4) pacote aplicável esgotado e nada para cair: presença barrada; (5) sem vínculo aplicável: valor da aula do aluno.
- **FR-008**: Um pacote MUST ser aplicável **somente** a aulas do seu grupo: o pacote de uma turma, só a aulas **dessa** turma; o pacote de aulas individuais, só a aulas individuais. O grupo é decidido pela **turma da aula**, não pela matrícula do aluno. Fora do grupo, a presença MUST ser cobrada como avulsa (Avulsa do contexto ou valor da aula) e o pacote MUST NOT ser decrementado nem barrar a presença. Como há no máximo um pacote ativo por grupo (FR-022), MUST NOT haver regra de escolha entre pacotes.
- **FR-009**: Falta MUST NOT consumir aula de pacote (regra atual).
- **FR-010**: A ordem de prioridade da cobrança MUST existir somente em `RegraCobrancaPresenca`; nenhum outro lugar do backend ou do frontend MUST decidir cobrança por modalidade ou por turma.
- **FR-011**: Todo aluno vinculado a uma aula, matriculado ou de reposição, MUST ter direito a presença e falta, e a presença MUST entrar no registro da aula e nas regras de cobrança acima.
- **FR-012**: A chamada MUST mostrar os alunos de reposição numa seção separada chamada **"Outros alunos"**, abaixo da lista principal, e MUST ocultá-la quando não houver nenhum. O frontend MUST NOT decidir quem é "outro aluno": ele só agrupa pelo indicador de reposição que a API devolve.
- **FR-013**: A reposição avulsa (adicionar e remover um aluno de fora numa aula agendada de turma) MUST continuar funcionando como na spec 056, inclusive para aluno de pacote (que paga avulso, exceto se a aula for da turma do pacote dele) e para aluno de mensalidade (sem custo).
- **FR-014**: A migração do banco MUST converter o pacote ativo de aluno matriculado em exatamente uma turma **ativa** onde todos os alunos ativos têm pacote ativo: a turma vira do tipo Pacote e o pacote aponta para ela. O pacote ativo de aluno **sem matrícula** MUST ficar como pacote de aulas individuais (sem turma). MUST preservar valor, saldo e total de aulas do pacote.
- **FR-015**: A migração MUST NOT desativar, somar, apagar nem alterar vínculos (além de apontar o pacote para a turma), MUST NOT remover matrículas e MUST NOT alterar aulas. Os casos que ela não converte (matriculado em várias turmas, turma inativa, turma mista) MUST ser listados para a professora, no padrão da spec 056.
- **FR-016**: O sistema MUST NOT criar uma segunda regra de conflito de horário do aluno: a regra existente da professora continua sendo a única, e MUST haver um teste de que um aluno não participa de duas aulas sobrepostas.
- **FR-017**: A tela da turma MUST mostrar o tipo e, na turma de pacote, as datas e o aviso de que a entrada é pelo pacote, oferecendo para "voltar" só os alunos que saíram e ainda têm o pacote ativo dela; a ficha do aluno MUST mostrar a turma do pacote.
- **FR-018**: As specs 056 (pacote do aluno sem turma, pacote consumido em aula individual, um pacote ativo por aluno), 041 e 052 MUST receber uma nota apontando para esta spec como a regra vigente (Princípio V da constituição).
- **FR-019**: Quando a turma de pacote acaba, os pacotes ativos dela MUST ficar inativos, **sem alterar o saldo**, o valor, o total de aulas nem os pagamentos. A turma acaba quando a data de fim já passou (verificado por uma rotina automática, ao iniciar a aplicação e periodicamente ao longo do dia) ou quando a professora a inativa, o que vier primeiro. A turma sem data de fim MUST só acabar pela inativação. O sistema MUST NOT apagar nem zerar nada nesse processo.
- **FR-020**: Excluir ou reativar logicamente o aluno MUST NOT alterar o pacote, o saldo nem a matrícula dele. O aluno que volta com a turma em andamento MUST poder usar o saldo do pacote dessa turma.
- **FR-021**: A professora MUST poder reativar um pacote de turma inativado **somente** se a turma estiver ativa e em andamento (data de fim vazia ou de hoje em diante). A reativação MUST NOT exigir que a turma não tenha começado, e MUST matricular o aluno de novo se ele não estiver. Reativar a turma MUST NOT reativar os pacotes sozinha.
- **FR-022**: O aluno MUST poder ter 0 ou N pacotes de turma ativos ao mesmo tempo, **um por turma**, e MUST NOT ter dois pacotes ativos da mesma turma. A única limitação por aluno MUST ser o pacote de aulas individuais: no máximo um ativo. Pacotes de grupos diferentes MUST poder conviver. O aviso de "combinação herdada" da ficha do aluno MUST valer só para mais de um pacote ativo do mesmo grupo.
- **FR-023**: Quando um aluno vinculado a uma aula tem presença confirmada, o sistema MUST gerar a cobrança em Contas a Receber automaticamente sempre que a regra de cobrança (FR-007) mandar cobrar. Presença com pacote aplicável consumido, mensalidade ou plano MUST NOT gerar conta a receber. (Comportamento atual da spec 056, mantido e agora coberto por teste também para "outros alunos".)

### Key Entities

- **Turma**: ganha **tipo** (Mensalidade ou Pacote) e, quando de pacote, **data de início** e **data de fim**.
- **Vínculo de cobrança (Pacote)**: pertence a um grupo: aponta para uma **turma de pacote** ou, sem turma, é o **pacote de aulas individuais**.
- **Matrícula (aluno × turma)**: sem mudança de estrutura; na turma de pacote só nasce pelo cadastro do pacote.
- **Participante da aula**: já existe; os de reposição formam a seção "Outros alunos". Nada de estrutura nova nele.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Fora do grupo do pacote (aula individual para pacote de turma; aula de turma para pacote de aulas individuais; aula de outra turma), 0 pacotes são decrementados; dentro do grupo, 100% das presenças de aluno com saldo consomem exatamente 1 aula.
- **SC-002**: 100% dos alunos com pacote ativo de uma turma de pacote estão matriculados nela e aparecem na chamada das aulas dessa turma.
- **SC-003**: 0 matrículas, vínculos, aulas ou saldos são alterados ou removidos pela migração, e 100% dos casos que ela não converte aparecem na lista.
- **SC-004**: A professora distingue na chamada, sem abrir outra tela, quem é da turma e quem é "outro aluno", e registra presença ou falta de todos.
- **SC-005**: 0 alunos aparecem em duas aulas sobrepostas; 0 alunos novos entram numa turma de pacote depois do início.
- **SC-006**: Depois de cada execução da rotina de fim de turma, 0 pacotes de turma encerrada continuam ativos e 100% deles mantêm o saldo que tinham.
- **SC-007**: Um aluno excluído e reativado com a turma ainda em andamento volta a poder usar 100% do saldo do pacote dessa turma, sem cadastrar nada de novo.

## Assumptions

- Não há login do aluno; toda ação é da professora.
- O desenho do que cabe em cada tela (campos de tipo e datas, seção "Outros alunos") é detalhado no plano; só os tokens de tema da spec 057 valem para as cores.
- A cobrança automática de mensalidade (spec 039) e as parcelas da spec 062 não mudam.
- A implementação desta spec não depende da 062, mas o nome do tipo "plano periódico" (Mensalidade, Semestral, Anual) é compartilhado: quem implementar depois ajusta o outro.
- A turma não tem horário fixo cadastrado; "todos os horários da turma" significa todas as aulas dela.
- Glossário: "pacote de turma" é o pacote que aponta para uma turma de pacote; "pacote de aulas individuais" é o pacote sem turma, só para aulas do aluno com a professora (o termo "pacote avulso" foi abandonado: não existe pacote que valha para qualquer aula); "outro aluno" é o participante de uma aula marcado como reposição.

## Fora de escopo (viraram cards do Backlog)

- **Recorrência na criação de aulas** (dias e horários fixos da turma): hoje cada aula é cadastrada uma por vez. A opção de repetir no formulário da aula é um card próprio ("Recorrência na criação de aulas"). Esta spec não depende dele, porque cada aula criada, uma a uma ou em série, já segue as regras desta spec.
- **Saída do aluno de turma de pacote com acerto proporcional ao consumo** (pacote de R$ 100 para 10 aulas, 7 consumidas, paga R$ 70): card próprio ("Saída do aluno de turma de pacote"). O sistema ainda não registra o pagamento do pacote, então o acerto exige decidir o modelo de cobrança do pacote parcelado (ver o card).
