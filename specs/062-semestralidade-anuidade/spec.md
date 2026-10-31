# Feature Specification: Semestralidade e Anuidade como Formas de Cobrança do Aluno

**Feature Branch**: `062-semestralidade-anuidade`

**Created**: 2026-10-29

**Status**: Draft

**Input**: Card Trello "Novas formas de cobrança: Semestralidade e Anuidade para o aluno" (origem: anotações da reunião do dia 22). As dúvidas de negócio do card foram respondidas pelo usuário em 2026-10-29 e estão registradas nesta spec.

## Contexto (estado atual, lido do código)

- O aluno pode ter vínculos de cobrança de três modalidades: **Avulsa** (por aula, em uma turma ou individual), **Mensalidade** e **Pacote** (saldo de aulas).
- A **Mensalidade** é do aluno (spec 056), vale para todas as aulas, não cobra por presença e é **exclusiva**: não convive com outro vínculo ativo do mesmo aluno. Um processo automático gera uma cobrança por mês, com vencimento no dia 10 do mês seguinte (spec 039).
- A decisão de cobrança de cada presença fica num único lugar (`RegraCobrancaPresenca`, spec 056).
- Cada cobrança gerada por um vínculo guarda o vínculo de origem, e o banco impede duas cobranças do mesmo vínculo na mesma competência (mês). A professora acompanha tudo em Contas a Receber.
- O cadastro é feito na ficha do aluno (`Vínculos de Cobrança`).
- O sino de notificações (specs 059 e 060) já avisa aula pendente, aula próxima e pagamento pendente, sem duplicar.

## Decisões de negócio (respondidas pelo usuário)

| Tema | Decisão |
|---|---|
| Forma de cobrança | **À vista** (1 parcela) ou **parcelada**. A professora decide, aluno a aluno, quantas parcelas e quais valores. |
| Montagem das parcelas | Ela informa o número de parcelas, o valor total e o 1º vencimento; o sistema **sugere parcelas mensais iguais** e ela pode **ajustar o valor e a data de cada uma** antes de salvar. |
| Valor e desconto | A professora informa o **valor total do período**. O desconto já está dentro desse valor; não há campo de desconto. |
| Taxa de maquininha | **Já está no valor de cada parcela.** Não há campo de taxa. O valor total é só referência e **não precisa bater** com a soma das parcelas; o sistema mostra a diferença como aviso, sem bloquear. |
| Período | Começa na **data de início informada no cadastro** e termina 6 meses (Semestral) ou 12 meses (Anual) depois. |
| Geração | **Todas as parcelas são criadas ao salvar**, como contas a receber "Pendente". |
| Renovação | **Manual.** Perto do fim, e depois dele, a professora é avisada no sino. Nada é cobrado sem ela decidir. |
| Combinação | **Exclusivas como a Mensalidade**: cobrem todas as aulas, sem cobrança por presença, e não convivem com outro vínculo ativo. |
| Edição | O plano **fica travado** depois de salvo. Ajustes de cada conta ficam em Contas a Receber, como hoje. |
| Exclusão | Pede confirmação e **cancela as parcelas pendentes**. As pagas ficam intactas e nada é apagado. |

## Decisões menores adotadas (a confirmar na aprovação desta spec)

1. **A exclusão cancela só as parcelas pendentes futuras** (vencimento depois de hoje). As vencidas e não pagas continuam como dívida, para uma exclusão no fim do contrato não "perdoar" cobrança atrasada sem querer.
2. **Reativar** um Semestral ou Anual excluído é recusado ("crie um novo vínculo"), porque as parcelas canceladas não são recriadas.
3. **Uma parcela por mês** dentro do mesmo vínculo; a competência da parcela é o mês do seu vencimento.
4. **Vigência**: o plano só vale para aulas com data entre o início e o fim do período. Fora disso, a presença segue as demais regras (sem vínculo, cobra o valor da aula). Nenhuma rotina "desativa" o vínculo sozinha.
5. **Renovação antecipada**: um novo Semestral ou Anual pode ser cadastrado com início **depois do fim** do atual, sem excluir o vigente.
6. **Aviso de renovação** a partir de **15 dias** antes do fim (configurável no servidor), até a renovação ser cadastrada ou o vínculo ser excluído.
7. **Lembrete de vencimento por e-mail (5 dias antes)** passa a valer também para as parcelas, com o texto geral ("pagamento"), e não só "mensalidade".
8. **Máximo de 24 parcelas** por vínculo.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Cadastrar um plano Semestral ou Anual, à vista ou parcelado (Priority: P1)

Na ficha do aluno, a professora escolhe Semestral ou Anual, informa a data de início, o valor total e como será paga: à vista ou em N parcelas. O sistema sugere as parcelas e ela ajusta o que precisar. Ao salvar, o vínculo e todas as parcelas passam a existir.

**Why this priority**: É o objetivo do card; sem isso nada mais funciona.

**Independent Test**: Cadastrar um Semestral de R$ 1.800, início em 01/11/2026, em 6 parcelas; conferir 6 contas a receber "Pendente" com os vencimentos e valores, ligadas ao vínculo.

**Acceptance Scenarios**:

1. **Given** um aluno sem outro vínculo ativo, **When** a professora cadastra um Semestral à vista (1 parcela), **Then** existe o vínculo com período de 6 meses e uma conta a receber pendente com o valor e a data informados.
2. **Given** um Anual com 12 parcelas, **When** ela salva, **Then** existem 12 contas a receber pendentes, uma por mês, com descrição que identifica o plano e o número da parcela (ex.: "Anuidade - parcela 3/12").
3. **Given** o formulário com número de parcelas, valor total e 1º vencimento, **When** ela pede as parcelas, **Then** o sistema sugere parcelas mensais iguais e ela pode mudar o valor e a data de cada uma antes de salvar.
4. **Given** parcelas cuja soma difere do valor total (por exemplo, por taxa de maquininha), **When** ela revisa, **Then** o sistema mostra a diferença como aviso e permite salvar.
5. **Given** duas parcelas no mesmo mês, **When** ela tenta salvar, **Then** o sistema recusa com mensagem clara.
6. **Given** que o salvamento falha no meio, **When** isso acontece, **Then** nem o vínculo nem nenhuma parcela são gravados (tudo ou nada).

---

### User Story 2 - Aulas do aluno com plano vigente não geram cobrança por presença (Priority: P1)

Com um Semestral ou Anual vigente, a presença do aluno em qualquer aula (turma ou individual) não gera conta a receber. Fora do período do plano, a presença volta a seguir as regras comuns.

**Why this priority**: É o que dá sentido ao plano: pago o período, a aula já está coberta.

**Independent Test**: Registrar a presença de um aluno com plano vigente numa aula da turma e numa individual: nenhuma conta nova. Registrar numa aula depois do fim do período: cobra o valor da aula.

**Acceptance Scenarios**:

1. **Given** um plano vigente na data da aula, **When** a presença é registrada, **Then** nenhuma conta a receber é criada e a presença fica ligada ao plano.
2. **Given** um plano cujo período já terminou, **When** a presença é registrada numa aula depois do fim, **Then** o plano não vale e a presença segue as demais regras.
3. **Given** uma aula anterior ao início do período, **When** a presença é registrada, **Then** o plano não vale para ela.
4. **Given** uma falta, **When** ela é registrada, **Then** nada muda na cobrança, como hoje.

---

### User Story 3 - Excluir o plano cancelando as parcelas pendentes futuras (Priority: P2)

Ao excluir um Semestral ou Anual, o sistema pede confirmação e cancela as parcelas pendentes que ainda vão vencer. As parcelas pagas e as vencidas continuam como estão.

**Why this priority**: Evita cobrança de um contrato que acabou, sem apagar nada.

**Independent Test**: Plano de 6 parcelas com 2 pagas, 1 vencida e 3 futuras: excluir cancela só as 3 futuras.

**Acceptance Scenarios**:

1. **Given** um plano com parcelas pagas, vencidas e futuras, **When** a professora confirma a exclusão, **Then** as futuras pendentes ficam "Cancelado" e as demais não mudam.
2. **Given** a tela de exclusão, **When** ela abre, **Then** avisa que as parcelas futuras pendentes serão canceladas.
3. **Given** um plano excluído, **When** ela tenta reativá-lo, **Then** o sistema recusa e orienta a criar um novo vínculo.
4. **Given** o plano excluído, **When** ela consulta Contas a Receber, **Then** nenhuma conta foi apagada.

---

### User Story 4 - Ser avisada de que o plano está acabando (Priority: P2)

Quando faltam 15 dias ou menos para o fim do período, ou o período já acabou, a professora vê no sino um aviso de renovação com o nome do aluno e a data de fim. O aviso some quando existe um novo plano cadastrado para depois ou quando o vínculo é excluído.

**Why this priority**: A renovação é manual; sem o aviso o aluno ficaria sem cobertura e sem ninguém perceber.

**Independent Test**: Plano que termina em 10 dias: aparece o aviso. Cadastrar a renovação (início depois do fim): o aviso some.

**Acceptance Scenarios**:

1. **Given** um plano que termina em até 15 dias, **When** o sistema verifica, **Then** existe exatamente um aviso de renovação para o aluno.
2. **Given** um plano já terminado e ainda ativo, **When** o sistema verifica, **Then** o aviso continua ativo.
3. **Given** a renovação cadastrada (novo plano com início depois do fim do atual), **When** o sistema verifica, **Then** o aviso é resolvido.
4. **Given** o aviso aberto, **When** a professora o abre, **Then** vê aluno, plano e data de fim, e um atalho para a ficha do aluno.
5. **Given** verificações repetidas, **When** elas acontecem, **Then** nunca há dois avisos para o mesmo aluno.

### Edge Cases

- **Plano sem parcelas**: não é permitido; precisa ter ao menos 1 parcela (à vista).
- **Parcela com vencimento antes do início do período**: permitido (ex.: entrada paga antes de começar).
- **Parcela com valor zero ou negativo**: recusada.
- **Plano com início no passado**: permitido, para registrar um contrato já em andamento; as parcelas com vencimento passado nascem "Pendente" e aparecem como atrasadas, para a professora dar baixa.
- **Aluno com Mensalidade, Pacote ou Avulsa ativa**: o cadastro do plano é recusado, e vice-versa (exclusividade).
- **Dois planos com períodos que se sobrepõem**: recusado. Período que começa depois do fim do atual: permitido (renovação antecipada).
- **Plano parcelado depois do fim do período** (ex.: última parcela vence após o fim): permitido; a cobrança continua mesmo depois do período.
- **Excluir plano com todas as parcelas pagas**: nada a cancelar; o vínculo é só encerrado.
- **Editar um plano salvo**: não é possível; para mudar, exclui e cria outro.
- **Plano vencido e ainda ativo**: deixa de valer para as aulas (fora do período), mas continua ativo e, pela exclusividade, **bloqueia** o cadastro de outro vínculo (Mensalidade, Pacote ou Avulsa) até a professora excluí-lo ou renová-lo. É para isso que existe o aviso de renovação.
- **Parcela que vence hoje**: não é cancelada na exclusão (só as que vencem depois de hoje).

## Requirements *(mandatory)*

### Functional Requirements

**Cadastro**
- **FR-001**: O sistema MUST oferecer as modalidades **Semestral** (período de 6 meses) e **Anual** (12 meses) no vínculo de cobrança do aluno, sem turma.
- **FR-002**: O cadastro MUST exigir data de início, valor total do período (maior que zero) e ao menos uma parcela; MUST calcular a data de fim a partir do início e do tipo.
- **FR-003**: Cada parcela MUST ter vencimento e valor (maior que zero). O sistema MUST permitir de 1 a 24 parcelas, com no máximo uma por mês (mês do vencimento).
- **FR-004**: O formulário MUST sugerir parcelas mensais iguais a partir do número de parcelas, do valor total e do 1º vencimento, e MUST permitir ajustar valor e data de cada uma.
- **FR-005**: A soma das parcelas MAY diferir do valor total; o sistema MUST mostrar a diferença como aviso e MUST NOT bloquear o salvamento.
- **FR-006**: Ao salvar, o sistema MUST criar, de uma só vez, o vínculo e todas as parcelas como contas a receber "Pendente", ligadas ao vínculo, em categoria própria ("Semestralidade" ou "Anuidade") e com descrição que identifica o plano e o número da parcela. Se qualquer parte falhar, MUST NOT gravar nada.
- **FR-007**: O sistema MUST NOT criar parcelas duplicadas para o mesmo vínculo, em nenhuma circunstância.

**Combinação e vigência**
- **FR-008**: Semestral e Anual MUST ser exclusivos: o cadastro MUST ser recusado se o aluno tem outro vínculo ativo, exceto outro Semestral ou Anual cujo período **não se sobreponha** (renovação antecipada). Cadastrar Mensalidade, Pacote ou Avulsa MUST ser recusado se o aluno tem um plano Semestral ou Anual ativo.
- **FR-009**: Um plano MUST valer para uma presença só se a data da aula estiver entre o início e o fim do período. Quando vale, a presença MUST NOT gerar conta a receber nem consumir pacote, e MUST ficar ligada ao plano. Quando não vale, a presença MUST seguir as demais regras de cobrança.
- **FR-010**: A regra de cobrança por presença MUST continuar num único lugar do sistema; o frontend MUST NOT decidir cobrança.

**Edição, exclusão e reativação**
- **FR-011**: Um plano salvo MUST NOT ser editado (início, tipo, valor, parcelas); a interface MUST NOT oferecer a edição e a API MUST recusá-la. Ajustes de cada conta continuam em Contas a Receber.
- **FR-012**: A exclusão MUST pedir confirmação e MUST cancelar as parcelas **pendentes com vencimento depois de hoje**, mantendo intactas as pagas e as vencidas. A exclusão MUST NOT apagar nenhum pagamento.
- **FR-013**: Reativar um plano excluído MUST ser recusado, com orientação para criar um novo vínculo.

**Renovação**
- **FR-014**: Para cada aluno com plano ativo cujo fim esteja a até 15 dias (valor configurável no servidor) ou já tenha passado, o sistema MUST manter **uma** notificação de renovação na professora, sem duplicar entre verificações.
- **FR-015**: A notificação MUST ser resolvida quando existir outro plano do aluno com início depois do fim do atual, ou quando o plano for excluído.
- **FR-016**: O popup MUST mostrar o aluno, o tipo do plano e a data de fim, com atalho para a ficha do aluno, e o sino MUST permitir filtrar essas notificações.

**Lembretes e relatórios**
- **FR-017**: O lembrete por e-mail 5 dias antes do vencimento MUST valer também para as parcelas, com texto que não se refira só a "mensalidade". Em desenvolvimento, todo e-mail MUST continuar indo só ao endereço de teste.
- **FR-018**: As parcelas MUST aparecer em Contas a Receber, no aviso de pagamento pendente (spec 060) e nos relatórios financeiros, sem tratamento especial.

### Key Entities

- **Vínculo de cobrança** (existente): ganha as modalidades Semestral e Anual e as datas de início e fim do período (só para elas).
- **Parcela** (a conta a receber já existente): ligada ao vínculo de origem, com competência no mês do vencimento. Não há tabela nova de parcelas.
- **Notificação de renovação**: aviso por aluno (mesma estrutura das notificações existentes), com um novo tipo.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Cadastrar um plano com N parcelas cria exatamente N contas a receber ligadas a ele; nenhuma tentativa de salvar produz duplicidade.
- **SC-002**: 100% das presenças de aulas dentro do período de um plano vigente não geram conta a receber; 100% das fora do período seguem as regras comuns.
- **SC-003**: Excluir um plano cancela todas as parcelas pendentes futuras e nenhuma outra; zero pagamentos apagados.
- **SC-004**: Todo plano que termina em até 15 dias ou já terminou tem exatamente um aviso de renovação até ser renovado ou excluído.
- **SC-005**: Um erro no meio do cadastro nunca deixa o vínculo sem as parcelas, nem parcelas sem o vínculo.

## Assumptions

- Valores em reais, com duas casas decimais, como no restante do sistema.
- A descrição das parcelas segue o padrão "{Plano} - parcela X/N" (ex.: "Semestralidade - parcela 2/6").
- O fuso e a data de hoje são os do servidor, como nas demais rotinas.
- O cálculo do fim do período soma 6 ou 12 meses ao início e termina um dia antes do aniversário (início 01/11/2026 + 6 meses → fim 30/04/2027).
- A Mensalidade existente e o processo mensal dela não mudam.
- **Glossário**: nesta spec, **plano** é o vínculo Semestral ou Anual (os dois têm período e parcelas). No código, o termo técnico **plano periódico** inclui também a Mensalidade, que cobre todas as aulas mas não tem período nem parcelas.
