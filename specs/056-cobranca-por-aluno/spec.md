# Feature Specification: Cobrança por Aluno e Reposição de Aulas

> **Regra substituída em parte pela [spec 063](../063-pacote-fora-da-turma/spec.md) (2026-10-30).** O pacote deixa de ser "do aluno, sem turma, válido em qualquer aula": todo pacote pertence a um grupo (uma turma de pacote ou as aulas individuais) e só é consumido em aula do seu grupo; fora dele o aluno paga como avulso. O aluno pode ter 0 ou N pacotes de turma (um por turma) e no máximo um de aulas individuais (no lugar de "um pacote ativo por aluno"). A turma passa a ter tipo (Mensalidade ou Pacote), a turma de pacote é fechada depois que começa e, quando acaba, os pacotes dela ficam inativos com o saldo preservado. Mensalidade, Avulsa e reposição não mudam.

**Feature Branch**: `056-cobranca-por-aluno`

**Created**: 2026-10-19

**Status**: Draft

**Input**: User description: "Mudança de regra de negócio decidida com o usuário em 2026-10-19: Mensalidade e Pacote deixam de ser vínculos por contexto (turma ou atendimento individual) e passam a ser do aluno. Mensalidade é exclusiva e dá direito a tudo, sem cobrança por aula. Pacote é um saldo de aulas usável em qualquer turma ou aula individual; presença consome, falta não. Avulsa continua por contexto e entra quando o pacote acaba. A professora pode adicionar um aluno a uma aula agendada de qualquer turma (reposição). Combinações já existentes que o novo modelo proíbe são listadas para a professora decidir. Specs 037/038/039/041/052 recebem nota apontando para esta."

## Contexto

Hoje todo vínculo de cobrança (Avulsa, Mensalidade ou Pacote) pertence a um **contexto**: uma turma específica, ou "atendimento individual" quando não tem turma. Quando uma aula acontece, o sistema procura o vínculo do aluno **exatamente** naquele contexto. Isso produz situações que não fazem sentido para o negócio:

- Um aluno com mensalidade cadastrada "sem turma" só fica coberto em aulas individuais. Nas aulas de turma, o sistema não acha vínculo e cobra cada presença pelo "Valor da aula" — o aluno pagaria a mensalidade **e** as aulas.
- Um aluno com mensalidade em duas turmas recebe duas mensalidades por mês.
- Um pacote de aulas só pode ser gasto na turma em que foi cadastrado; o aluno que faltou não consegue repor numa outra turma ou numa aula individual.
- Não há como colocar um aluno numa aula de uma turma da qual ele não é matriculado.

Esta feature troca o modelo: **Mensalidade e Pacote passam a ser do aluno**, **Avulsa continua por contexto**, e a professora ganha a possibilidade de **adicionar um aluno a uma aula de outra turma** (reposição).

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Mensalidade cobre todas as aulas do aluno (Priority: P1) 🎯 MVP

A professora cadastra uma mensalidade para o aluno — sem escolher turma. A partir daí, nenhuma presença desse aluno gera cobrança por aula, seja em qualquer turma, seja em aula individual. Ele recebe só a cobrança mensal automática, uma por mês. Um aluno com mensalidade não pode ter também pacote ou cobrança avulsa ativos.

**Why this priority**: É o erro mais grave do modelo atual: hoje um aluno mensalista pode ser cobrado por aula nas turmas, além da mensalidade.

**Independent Test**: Cadastrar mensalidade para um aluno matriculado numa turma, registrar presença dele numa aula dessa turma e numa aula individual, e confirmar que nenhuma conta a receber por aula foi gerada — só existe a cobrança mensal.

**Acceptance Scenarios**:

1. **Given** um aluno sem vínculos ativos, **When** a professora cadastra uma mensalidade para ele, **Then** a mensalidade é salva sem turma, e o formulário não oferece escolha de turma para essa modalidade.
2. **Given** um aluno com mensalidade ativa matriculado na Turma A, **When** a presença dele é registrada numa aula da Turma A, **Then** nenhuma conta a receber é gerada para essa presença.
3. **Given** o mesmo aluno, **When** a presença dele é registrada numa aula individual, **Then** nenhuma conta a receber é gerada para essa presença.
4. **Given** um aluno com mensalidade ativa, **When** a professora tenta cadastrar ou reativar um pacote ou uma cobrança avulsa para ele, **Then** o sistema recusa com uma mensagem explicando que a mensalidade já cobre todas as aulas.
5. **Given** um aluno com pacote ou cobrança avulsa ativos, **When** a professora tenta cadastrar ou reativar uma mensalidade para ele, **Then** o sistema recusa com uma mensagem explicando que é preciso desativar os outros vínculos antes.
6. **Given** um aluno com mensalidade ativa, **When** a cobrança mensal automática roda no fim do mês, **Then** é gerada exatamente uma cobrança de mensalidade para ele (não uma por turma).

---

### User Story 2 - Pacote como saldo de aulas do aluno (Priority: P1)

A professora cadastra um pacote de N aulas para o aluno — sem escolher turma. Cada presença do aluno, em qualquer turma ou em aula individual, consome uma aula do saldo. Falta não consome: o aluno fica registrado como "Faltou" e pode repor depois. Se o aluno também tiver uma cobrança avulsa para aquele contexto, ela só passa a ser usada quando o pacote acabar.

**Why this priority**: Junto com a User Story 1, é o centro da mudança de modelo; sem ela o pacote continua preso a uma turma.

**Independent Test**: Cadastrar um pacote de 3 aulas para um aluno, registrar presença dele numa aula da Turma A, numa aula da Turma B (via reposição) e numa aula individual, e confirmar que o saldo foi de 3 para 0 sem gerar contas a receber; registrar uma falta e confirmar que o saldo não mudou.

**Acceptance Scenarios**:

1. **Given** um aluno sem vínculos ativos, **When** a professora cadastra um pacote para ele, **Then** o pacote é salvo sem turma, e o formulário não oferece escolha de turma para essa modalidade.
2. **Given** um aluno com pacote ativo de saldo 5, **When** a presença dele é registrada numa aula de qualquer turma ou numa aula individual, **Then** o saldo passa para 4 e nenhuma conta a receber é gerada.
3. **Given** o mesmo aluno, **When** ele é marcado como "Faltou" numa aula, **Then** o saldo não muda.
4. **Given** um aluno com pacote ativo, **When** a professora tenta cadastrar ou reativar um segundo pacote para ele, **Then** o sistema recusa, informando que já existe um pacote ativo.
5. **Given** um aluno com pacote de saldo 0 e uma cobrança avulsa ativa para atendimento individual, **When** a presença dele é registrada numa aula individual, **Then** é gerada uma conta a receber pelo valor da cobrança avulsa, o saldo continua 0, e a professora vê um aviso informando que o pacote acabou.
6. **Given** um aluno com pacote de saldo 0 e sem cobrança avulsa para o contexto da aula, **When** a presença dele é registrada, **Then** a presença é bloqueada com o mesmo aviso de renovação de pacote já usado hoje.
7. **Given** um aluno com pacote de saldo 2 e uma cobrança avulsa ativa para a Turma A, **When** a presença dele é registrada numa aula da Turma A, **Then** o pacote é consumido (saldo 1) e nenhuma cobrança avulsa é gerada.

---

### User Story 3 - Reposição: adicionar um aluno a uma aula de outra turma (Priority: P2)

O aluno faltou e quer repor a aula numa outra turma. A professora abre uma aula ainda agendada daquela turma e adiciona o aluno, mesmo que ele não seja matriculado nela. O aluno passa a aparecer na lista da aula e segue as mesmas regras de presença e cobrança de qualquer outro participante. A professora também pode remover esse aluno adicionado antes de a aula acontecer.

**Why this priority**: Torna o pacote "do aluno" utilizável na prática; depende das User Stories 1 e 2 para ter efeito financeiro correto, mas pode ser testada isoladamente quanto à participação na aula.

**Independent Test**: Adicionar a uma aula agendada da Turma B um aluno matriculado só na Turma A, confirmar que ele aparece na lista de presença, registrar a aula com ele presente e confirmar que a cobrança seguiu as regras do vínculo dele.

**Acceptance Scenarios**:

1. **Given** uma aula agendada da Turma B e um aluno ativo que não é matriculado na Turma B, **When** a professora adiciona esse aluno à aula, **Then** ele passa a constar na lista de participantes da aula.
2. **Given** um aluno adicionado a uma aula agendada, **When** a professora o remove antes de a aula ser registrada, **Then** ele deixa de constar na aula.
3. **Given** um aluno adicionado a uma aula, **When** a aula tem a sessão registrada, **Then** a presença/falta dele segue exatamente as mesmas regras de cobrança dos demais participantes.
4. **Given** uma aula com um aluno adicionado, **When** a professora edita outros dados da aula (data, horário, descrição) mantendo a mesma turma, **Then** o aluno adicionado continua na aula.
5. **Given** uma aula já realizada ou cancelada, **When** a professora tenta adicionar ou remover um participante, **Then** o sistema recusa.
6. **Given** uma aula agendada, **When** a professora tenta remover um aluno que é matriculado na turma da aula, **Then** o sistema recusa (só alunos adicionados por reposição podem ser removidos individualmente).

---

### User Story 4 - Vínculos já existentes no novo modelo (Priority: P3)

Os pacotes e mensalidades que hoje estão amarrados a uma turma passam a ser do aluno, sem que a professora precise recadastrá-los. Se algum aluno ficar numa combinação que o modelo novo proíbe (por exemplo, mensalidade e pacote ativos ao mesmo tempo, ou dois pacotes ativos), o sistema não decide sozinho: a professora vê um aviso na ficha do aluno listando os vínculos em conflito para resolver.

**Why this priority**: Necessária para a transição, mas o volume de dados afetados é pequeno (sistema de uma única professora) e não bloqueia o funcionamento das regras novas para cadastros novos.

**Independent Test**: Com um aluno que tenha dois pacotes ativos em turmas diferentes antes da mudança, aplicar a mudança e confirmar que os dois continuam ativos, agora sem turma, e que a ficha do aluno mostra um aviso de conflito listando ambos.

**Acceptance Scenarios**:

1. **Given** um pacote ativo amarrado à Turma A antes da mudança, **When** a mudança é aplicada, **Then** o pacote passa a ser do aluno, com o mesmo saldo e total de aulas.
2. **Given** um aluno com mensalidade e pacote ativos antes da mudança, **When** a ficha do aluno é aberta, **Then** a professora vê um aviso listando os vínculos em conflito com a regra nova.
3. **Given** um aluno em conflito, **When** a professora desativa os vínculos excedentes, **Then** o aviso desaparece.

---

### Edge Cases

- Aluno com dois pacotes ativos herdados (conflito ainda não resolvido): cada presença consome do pacote mais antigo que ainda tiver saldo; quando todos acabarem, valem as regras de pacote esgotado.
- Aluno com mensalidade e outros vínculos ativos herdados (conflito ainda não resolvido): a mensalidade prevalece — nenhuma presença gera cobrança por aula nem consome pacote, para não cobrar o aluno duas vezes.
- Aluno com duas mensalidades ativas herdadas: continua recebendo uma cobrança mensal por mensalidade ativa até a professora resolver o conflito (o sistema não soma nem descarta valores sozinho).
- Edição de um vínculo trocando a modalidade (ex.: de Avulsa para Mensalidade): valem as mesmas regras de exclusividade de um cadastro novo; ao virar Mensalidade ou Pacote, a turma do vínculo é removida.
- Aluno adicionado por reposição numa aula e que depois é matriculado na mesma turma: passa a contar como matriculado; não aparece duplicado na aula.
- Tentativa de adicionar à aula um aluno inativo, ou um aluno que já participa da aula: recusada com mensagem clara.
- Aula individual: não aceita participantes adicionais (reposição vale só para aulas de turma).
- Mudança de turma de uma aula agendada que tem alunos adicionados por reposição: os alunos adicionados continuam na aula; a lista de matriculados passa a ser a da turma nova.

## Requirements *(mandatory)*

### Functional Requirements

**Mensalidade**

- **FR-001**: O sistema MUST permitir cadastrar mensalidade apenas sem turma (vínculo do aluno), recusando qualquer tentativa de associá-la a uma turma.
- **FR-002**: O sistema MUST recusar o cadastro, a edição ou a reativação de uma mensalidade para um aluno que tenha qualquer outro vínculo de cobrança ativo.
- **FR-003**: O sistema MUST recusar o cadastro, a edição ou a reativação de um pacote ou de uma cobrança avulsa para um aluno que tenha mensalidade ativa.
- **FR-004**: O sistema MUST NOT gerar cobrança por aula nem consumir pacote para presenças de um aluno com mensalidade ativa, em qualquer aula (de turma ou individual).
- **FR-005**: A cobrança mensal automática MUST continuar gerando exatamente uma cobrança por mensalidade ativa por mês, como hoje (specs/039).
- **FR-006**: No teto opcional de "aulas incluídas" da mensalidade, o sistema MUST contar apenas presenças do aluno no mês, em qualquer aula (de turma ou individual).

**Pacote**

- **FR-007**: O sistema MUST permitir cadastrar pacote apenas sem turma (vínculo do aluno), recusando qualquer tentativa de associá-lo a uma turma.
- **FR-008**: O sistema MUST permitir no máximo um pacote ativo por aluno, recusando cadastro ou reativação de um segundo.
- **FR-009**: Cada presença de um aluno com pacote ativo de saldo maior que zero MUST consumir uma aula do saldo, em qualquer aula (de turma ou individual), sem gerar conta a receber.
- **FR-010**: Falta MUST NOT consumir aula do pacote.

**Avulsa e ordem de aplicação**

- **FR-011**: A cobrança avulsa MUST continuar sendo por contexto (uma por turma e uma para atendimento individual, no máximo uma ativa por contexto), cobrando apenas presenças, como hoje.
- **FR-012**: Para cada presença, o sistema MUST aplicar os vínculos nesta ordem: (1) mensalidade ativa do aluno — nenhuma cobrança; (2) pacote ativo do aluno com saldo — consome o pacote; (3) cobrança avulsa ativa do contexto da aula — gera cobrança pelo valor dela; (4) se o aluno tem pacote esgotado e nenhuma avulsa no contexto — bloqueia a presença com o aviso de renovação já existente (specs/041); (5) aluno sem nenhum vínculo aplicável — gera cobrança pelo "Valor da aula" do aluno, como hoje.
- **FR-013**: Quando uma presença for cobrada como avulsa porque o pacote do aluno está esgotado (passo 3 com pacote esgotado), o sistema MUST exibir um aviso informativo à professora de que o pacote acabou.
- **FR-014**: O sistema MUST registrar em cada presença qual vínculo foi efetivamente aplicado (mensalidade, pacote ou avulsa), mantendo a auditoria de aulas dadas por vínculo (specs/052) correta no novo modelo.

**Reposição**

- **FR-015**: A professora MUST poder adicionar a uma aula agendada de turma qualquer aluno ativo que ainda não participe dela, matriculado ou não naquela turma.
- **FR-016**: A professora MUST poder remover de uma aula agendada um aluno que foi adicionado por reposição; o sistema MUST recusar a remoção individual de alunos matriculados na turma.
- **FR-017**: O sistema MUST recusar adicionar ou remover participantes em aulas realizadas, canceladas ou individuais.
- **FR-018**: Editar uma aula agendada MUST preservar os alunos adicionados por reposição, recalculando apenas a lista de matriculados quando a turma da aula mudar.
- **FR-019**: O aluno adicionado por reposição MUST seguir exatamente as mesmas regras de presença e cobrança (FR-004, FR-009, FR-010, FR-012) dos demais participantes.

**Transição e documentação**

- **FR-020**: Pacotes e mensalidades existentes amarrados a uma turma MUST passar a ser do aluno, preservando valor, saldo, total de aulas, aulas incluídas e histórico.
- **FR-021**: O sistema MUST NOT desativar, somar ou alterar automaticamente vínculos que fiquem em combinação proibida pelo novo modelo; MUST, em vez disso, exibir na ficha do aluno um aviso listando os vínculos ativos em conflito.
- **FR-022**: Enquanto um conflito herdado não for resolvido, o sistema MUST aplicar: mensalidade prevalece sobre os demais vínculos; com mais de um pacote ativo, consome primeiro o mais antigo com saldo.
- **FR-023**: As especificações que descrevem o modelo anterior por turma (specs/037, 038, 039, 041, 052) MUST receber uma nota indicando que esta especificação passa a ser a regra vigente para os pontos que ela altera.

### Key Entities

- **Vínculo de cobrança**: já existente. Passa a ter dois "escopos": **do aluno** (Mensalidade e Pacote, sempre sem turma) e **por contexto** (Avulsa, por turma ou individual). As regras de unicidade mudam: no máximo uma mensalidade ou um pacote ativos por aluno (mutuamente exclusivos com mensalidade), e no máximo uma avulsa ativa por contexto.
- **Participação na aula**: já existente (aluno × aula, com presença). Passa a distinguir se o aluno está na aula por ser matriculado na turma ou por ter sido adicionado por reposição.
- **Presença registrada**: já registra o vínculo aplicado (specs/052); passa a registrar também presenças cobertas por mensalidade e pacote em qualquer contexto.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 0 contas a receber por aula geradas para alunos com mensalidade ativa, em qualquer tipo de aula.
- **SC-002**: Um aluno com mensalidade recebe exatamente 1 cobrança de mensalidade por mês, independentemente de quantas turmas frequenta.
- **SC-003**: Um pacote de N aulas pode ser totalmente consumido combinando aulas de turmas diferentes e aulas individuais, com o saldo chegando exatamente a 0 após N presenças e permanecendo inalterado em faltas.
- **SC-004**: A professora consegue colocar um aluno numa aula de outra turma em uma única ação a partir da tela da aula.
- **SC-005**: 100% dos pacotes e mensalidades existentes antes da mudança continuam ativos e com os mesmos valores/saldos após a transição; 100% dos alunos em combinação proibida aparecem sinalizados na própria ficha.

## Assumptions

- O aluno não tem acesso ao sistema; reposição e cadastro de vínculos são feitos pela professora (confirmado com o usuário).
- Reposição vale para qualquer turma ativa, sem lista de turmas "permitidas" por aluno (confirmado com o usuário).
- Lembretes de aula por e-mail continuam indo apenas para os alunos matriculados na turma; alunos adicionados por reposição não recebem o lembrete daquela turma. Pode ser revisto numa spec futura.
- O aviso de conflito na ficha do aluno é a forma de "listar para a professora decidir"; não há tela separada de relatório de conflitos.
- A regra de prioridade para conflitos herdados (mensalidade prevalece; pacote mais antigo primeiro) é uma salvaguarda temporária até a professora resolver, escolhida para nunca cobrar o aluno a mais.
- Telas de acompanhamento de progresso do pacote ficam para a spec 058; notificações ficam para as specs 059/060.
