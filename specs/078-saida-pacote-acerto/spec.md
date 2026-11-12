# Feature Specification: Saída do Aluno de Turma de Pacote com Acerto Proporcional ao Consumo

**Feature Branch**: `078-saida-pacote-acerto`

**Created**: 2026-11-08

**Status**: Draft

**Input**: Card do Trello "Saída do aluno de turma de pacote: acerto proporcional ao consumo" (Backlog). Regra dada pelo usuário em 2026-10-30, durante a spec 063: pacote de R$ 100 para 10 aulas, o aluno consumiu 7 → paga R$ 70 no total, porque cada aula vale R$ 10. As dúvidas de negócio do card foram respondidas pelo usuário em 2026-11-08 e viram as regras abaixo.

## Contexto (estado atual, lido do código e das specs)

- O **pacote do aluno** (vínculo de cobrança com modalidade Pacote) guarda o valor, o total de aulas e o saldo, mas **não gera contas a receber nem parcelas**: o saldo é tratado como "já pago" (spec 038). **O sistema não sabe quanto o aluno pagou pelo pacote.**
- "**Aulas dadas**" do pacote = presenças registradas com ele (spec 058); falta não consome; esgotado bloqueia a presença (specs 041/056).
- A **turma de pacote** (spec 063) é fechada: começa e termina junta, ninguém novo entra depois de iniciada e **os alunos podem sair**. Hoje "sair" é só **desvincular** o aluno da turma: o pacote, o saldo e os pagamentos **não mudam** e o aluno **pode voltar** enquanto a turma está em andamento, desde que o pacote dele esteja ativo. A spec 063 deixou o acerto financeiro da saída para este card.
- Quando a turma acaba, os pacotes dela ficam **inativos sem perder o saldo** (spec 063); a professora só reativa um pacote de turma se a turma voltar a estar em andamento.
- A conta a receber já existe como conceito próprio (Contas a Receber, com status Pendente/Pago/Cancelado e categorias de receita) e a spec 062 já cria contas a receber ligadas a um vínculo de cobrança (Semestral/Anual). O pacote ainda não usa esse mecanismo, e **esta spec não o estende ao pacote**.

## Clarifications

### Session 2026-11-08

- Q: Como o acerto sabe quanto o aluno já pagou pelo pacote? → A: A professora **informa**, no resumo da saída, quanto o aluno já pagou. O pacote **não** passa a gerar parcelas nesta spec (vira outra spec, "pacote parcelado").
- Q: E se o aluno já pagou mais do que consumiu? → A: **Não devolver.** O acerto só registra o consumo: sem devolução, sem crédito e sem saída de dinheiro.
- Q: E se o aluno pagou menos do que consumiu? → A: O sistema **gera uma conta a receber Pendente** com a diferença.
- Q: Quando o acerto acontece e o que muda no pacote? → A: Num **botão próprio "Registrar saída"**, com resumo e confirmação. Ao confirmar: o aluno sai da turma, o pacote é encerrado (aulas não usadas perdidas) e ele **não volta** pelo pacote. "Desvincular" continua como hoje.
- Q: O que a saída faz com as aulas futuras já agendadas da turma, onde o aluno ainda está listado? → A: **Tira o aluno** das aulas agendadas da turma que **ainda não começaram** (só do rol de matriculados), na mesma operação; aulas realizadas, canceladas, já iniciadas e as participações de reposição **não mudam**. O resumo avisa em quantas aulas ele deixa de participar.
- Q: A saída registrada por engano pode ser desfeita? → A: **Não nesta versão.** A proteção é o resumo com confirmação; se errar, a professora cancela a conta a receber gerada e corrige o resto fora do sistema.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Ver o resumo do acerto antes de confirmar a saída (Priority: P1)

Como professora, quero abrir "Registrar saída" para um aluno de uma turma de pacote e ver, antes de gravar qualquer coisa, quanto ele consumiu, quanto devia pelo consumo, quanto me diz que já pagou e o que o sistema fará por causa da diferença.

**Why this priority**: É a regra de negócio nova e a base da decisão financeira. Mostrar o resumo sem gravar nada já entrega valor (a professora enxerga o acerto) e permite validar o cálculo antes de qualquer efeito.

**Independent Test**: Numa turma de pacote em andamento, com um aluno de pacote de R$ 100 e 10 aulas, 7 presenças registradas: abrir "Registrar saída", informar "já pagou R$ 30" e ver o resumo (7 aulas, R$ 10 por aula, devido R$ 70, pago R$ 30, diferença R$ 40 a receber) sem que nada seja gravado.

**Acceptance Scenarios**:

1. **Given** um pacote de R$ 100 para 10 aulas com 7 presenças, **When** a professora abre "Registrar saída" e informa que o aluno já pagou R$ 30, **Then** o resumo mostra 7 aulas consumidas, valor por aula R$ 10,00, valor devido R$ 70,00, pago R$ 30,00 e "será gerada uma conta a receber de R$ 40,00".
2. **Given** o mesmo pacote, **When** ela informa que o aluno já pagou R$ 100, **Then** o resumo mostra devido R$ 70,00, pago R$ 100,00 e avisa que **nada será gerado** e que o valor pago a mais **não é devolvido**.
3. **Given** o mesmo pacote, **When** ela informa que o aluno já pagou exatamente R$ 70, **Then** o resumo mostra que o acerto está fechado e que nada será gerado.
4. **Given** um pacote de R$ 100 para 3 aulas com 2 presenças, **When** ela abre o resumo, **Then** o valor devido é R$ 66,67 (R$ 100 × 2 ÷ 3 arredondado a centavos).
5. **Given** o resumo aberto, **When** ela fecha sem confirmar, **Then** nada é gravado: o aluno continua na turma e o pacote continua ativo.

---

### User Story 2 - Registrar a saída com confirmação (Priority: P1)

Como professora, quero confirmar a saída e ter, numa única operação, o aluno retirado da turma, o pacote encerrado e, se o aluno pagou menos do que consumiu, a conta a receber da diferença criada.

**Why this priority**: É o objetivo do card: fechar financeiramente a saída sem deixar o aluno pela metade (fora da turma com pacote ainda aberto, ou com o pacote encerrado sem a cobrança do que ele deve).

**Independent Test**: Com o resumo da história 1 (devido R$ 70, pago R$ 30), confirmar com vencimento informado e conferir: aluno fora da turma, pacote inativo, **uma** conta a receber Pendente de R$ 40,00 com o vencimento informado, e o aluno **sem** conseguir voltar pelo pacote.

**Acceptance Scenarios**:

1. **Given** devido R$ 70 e pago R$ 30, **When** a professora informa o vencimento e confirma, **Then** o aluno é desvinculado da turma, é retirado das aulas agendadas da turma que ainda não começaram, o pacote fica inativo, e é criada **uma** conta a receber Pendente de R$ 40,00 com o vencimento informado, ligada ao aluno e ao pacote.
2. **Given** devido R$ 70 e pago R$ 100, **When** ela confirma, **Then** o aluno sai e o pacote é encerrado, **nenhuma** conta a receber é criada e **nenhuma** devolução ou crédito é registrado.
3. **Given** devido R$ 70 e pago R$ 70, **When** ela confirma, **Then** o aluno sai, o pacote é encerrado e nenhuma conta é criada.
4. **Given** um aluno que **saiu pelo acerto**, **When** a professora tenta matriculá-lo de novo nessa turma (ou reativar o pacote), **Then** o sistema recusa: o pacote foi encerrado pelo acerto e o aluno não volta pelo pacote.
5. **Given** a confirmação em andamento, **When** qualquer parte da gravação falha, **Then** **nada** muda (o aluno continua na turma, o pacote ativo e nenhuma conta criada).
6. **Given** um aluno que participa de 2 aulas agendadas da turma que ainda não começaram, de 1 aula já realizada e de 1 aula de reposição, **When** a saída é confirmada, **Then** ele sai **só** das 2 aulas agendadas que ainda não começaram; a aula realizada (com a presença dele) e a reposição ficam como estavam.
7. **Given** a saída já registrada, **When** a professora (ou um segundo clique) tenta registrá-la de novo, **Then** o sistema recusa e **não** cria uma segunda conta.

---

### User Story 3 - A diferença aparece em Contas a Receber como qualquer outra conta (Priority: P2)

Como professora, quero que o que o aluno ainda me deve pelo consumo apareça em Contas a Receber, para cobrar, marcar como pago ou cancelar como faço com as outras contas.

**Why this priority**: Fecha o ciclo: o acerto só vale se a cobrança da diferença entra na rotina financeira já existente. Depende da história 2.

**Independent Test**: Depois da saída com diferença de R$ 40, abrir Contas a Receber e conferir a conta (categoria de acerto de pacote, valor R$ 40,00, vencimento informado, status Pendente), marcá-la como paga e ver o valor nos relatórios financeiros.

**Acceptance Scenarios**:

1. **Given** uma saída com diferença a receber, **When** a professora abre Contas a Receber, **Then** vê a conta Pendente com uma descrição que identifica o aluno, a turma e a conta (ex.: 7 de 10 aulas consumidas, devido R$ 70,00, pago R$ 30,00).
2. **Given** essa conta, **When** ela a marca como paga ou a cancela, **Then** o comportamento é o das demais contas (relatórios, status), sem tratamento especial.

---

### Edge Cases

- **Nenhuma aula consumida (0 presenças)**: devido R$ 0,00; nenhuma conta; o que o aluno pagou **não** é devolvido; a saída é aceita e o pacote é encerrado.
- **Pacote esgotado (todas as aulas dadas)**: devido = valor do pacote; só gera conta se ele pagou menos que o valor total.
- **Valor pago em branco**: vale R$ 0,00 (a professora precisa confirmar ciente). **Negativo** ou com mais de 2 casas decimais: recusado.
- **Diferença a receber e vencimento**: o vencimento é obrigatório quando haverá conta; padrão = hoje; **não pode ser anterior a hoje**. Sem diferença, o campo de vencimento não é exigido.
- **Aluno que consumiu aulas como "outro aluno" (reposição) com o pacote desta turma**: essas presenças contam como consumidas (são aulas dadas do pacote).
- **Aluno com pacote em outra turma de pacote ou com Mensalidade/Avulsa**: o acerto trata **só** o pacote desta turma; os demais vínculos não mudam.
- **Aluno não matriculado, pacote já inativo, turma que não é de pacote ou turma que já acabou**: o botão não aparece e o servidor recusa o pedido mesmo se a tela for burlada.
- **Aulas da turma onde o aluno está listado**: sai das **agendadas que ainda não começaram** (para uma presença marcada por engano não cobrar "valor da aula" com o pacote já encerrado). Ficam como estão: aulas realizadas, canceladas, **já iniciadas** (a professora ainda registra a chamada delas) e participações de reposição.
- **Tela desatualizada**: se entre abrir o resumo e confirmar uma presença for registrada (o consumo mudou) ou uma aula agendada for criada ou começar (muda de quantas aulas o aluno sairia), a gravação **recalcula** e, se o resultado for diferente do resumo mostrado, **recusa** e mostra o novo resumo para a professora conferir.
- **Pacote antigo sem total de aulas informado**: o total é o que a ficha do aluno já mostra (estimado a partir do saldo, spec 058); se nem assim existir total, a saída é recusada pedindo para informar o total do pacote primeiro.
- **Mais presenças do que o total** (total editado para baixo depois): o devido nunca passa do valor do pacote.
- **Valor do pacote com centavos e total de aulas que não divide exato**: o devido é arredondado a centavos uma única vez (ver FR-003).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Na turma de pacote em andamento, para cada aluno matriculado que tem o pacote **ativo** dessa turma, o sistema MUST oferecer a ação "Registrar saída", separada de "Desvincular". Nas demais situações (aluno não matriculado, pacote inativo, turma que não é de pacote, turma já encerrada) a ação MUST NOT ser oferecida.
- **FR-002**: Ao abrir "Registrar saída", o sistema MUST calcular e exibir o resumo do acerto: aulas consumidas, total de aulas do pacote, valor do pacote, valor por aula, valor devido pelo consumo, valor já pago informado, diferença a consequência (conta a receber da diferença, ou nada a gerar) e **em quantas aulas agendadas ainda não iniciadas da turma ele deixará de participar**. O cálculo MUST ser feito **no servidor**; a tela MUST apenas exibir (Princípio II).
- **FR-003**: O **valor devido** MUST ser o valor do pacote × aulas consumidas ÷ total de aulas do pacote, arredondado a centavos **uma única vez**; com todas as aulas consumidas o devido MUST ser exatamente o valor do pacote; com nenhuma, R$ 0,00. O **valor por aula** exibido é o valor do pacote ÷ total de aulas, arredondado a centavos, só como informação. **Aulas consumidas** são as aulas dadas do pacote (presenças registradas com ele, spec 058); falta não consome.
- **FR-004**: A professora MUST informar quanto o aluno **já pagou** (valor ≥ 0, no máximo 2 casas decimais; em branco = R$ 0,00). O sistema MUST NOT presumir esse valor a partir de nenhum registro, porque o pacote não registra pagamentos.
- **FR-005**: Se o valor pago for **menor** que o devido, o sistema MUST criar, ao confirmar, **exatamente uma** conta a receber **Pendente** no valor (devido − pago), com o vencimento informado pela professora (obrigatório, de hoje em diante), ligada ao aluno e ao pacote, numa categoria de receita própria para o acerto de pacote, com descrição que identifica o aluno, a turma e os números do acerto.
- **FR-006**: Se o valor pago for **maior ou igual** ao devido, o sistema MUST NOT criar conta a receber. Se for maior, MUST NOT criar devolução, crédito, saída de dinheiro ou qualquer lançamento para o excedente; o resumo MUST avisar que o excedente não é devolvido.
- **FR-007**: A saída MUST exigir **confirmação explícita** depois do resumo; nada MUST ser gravado antes dela. Na gravação o servidor MUST **recalcular** o acerto com os dados atuais (a tela só envia o valor pago, o vencimento e os números do resumo que a professora viu) e MUST recusar, mostrando o novo resumo, se o consumo, o valor devido **ou o número de aulas de que o aluno sairia** diferirem do resumo confirmado.
- **FR-008**: A confirmação MUST ser **atômica**: desvincular o aluno da turma, retirá-lo das aulas agendadas que ainda não começaram (FR-017), encerrar o pacote (inativo, sem exclusão física, Princípio I) e criar a conta da diferença (se houver) acontecem juntos ou nenhum acontece.
- **FR-009**: Ao encerrar o pacote pelo acerto, as aulas não usadas MUST ficar **perdidas**; o histórico (valor, total de aulas, presenças registradas e saldo gravado) MUST ser preservado, como no encerramento ao fim da turma.
- **FR-010**: O aluno que saiu pelo acerto MUST NOT voltar pelo pacote: a matrícula direta dele nessa turma e a reativação desse pacote MUST ser recusadas com mensagem clara. (O pacote encerrado pelo acerto é diferente do pacote que só ficou inativo porque a turma acabou.)
- **FR-011**: "Desvincular" MUST NOT mudar: continua reversível, sem acerto, com o pacote, o saldo e os pagamentos intactos (spec 063).
- **FR-012**: O servidor MUST recusar a saída quando: a turma não existe ou o aluno não existe (404); o aluno não está matriculado na turma, o pacote dele dessa turma não está ativo, a turma não é de pacote ou já acabou, ou a saída desse pacote já foi registrada (409); o pacote não tem total de aulas definido (409); há diferença a receber e o vencimento falta ou é anterior a hoje (409); o consumo mudou desde o resumo (409, FR-007); ou o valor pago é inválido (negativo ou com mais de 2 casas) (400). Um segundo pedido MUST NOT criar uma segunda conta.
- **FR-013**: A conta a receber gerada MUST seguir as regras comuns de Contas a Receber (pagar, cancelar, relatórios) sem tratamento especial, e MUST NOT alterar nenhum outro vínculo de cobrança do aluno (Mensalidade, Avulsa, pacote de outra turma).
- **FR-014**: A tela MUST NOT recalcular, arredondar nem duplicar nenhuma regra do acerto: valores e mensagens vêm da resposta do servidor (Princípio II).
- **FR-015**: A spec 063 MUST receber uma nota apontando para esta spec como a regra vigente do acerto financeiro da saída (Princípio V).
- **FR-017**: Ao registrar a saída, o sistema MUST retirar o aluno das aulas **agendadas** da turma cuja data e horário de início **ainda não passaram** (só do rol de matriculados). A saída MUST NOT alterar aulas realizadas, canceladas ou já iniciadas, nem as participações de **reposição** do aluno, nem aulas de outras turmas ou individuais.
- **FR-016**: O sistema MUST NOT oferecer "Desfazer saída" nesta versão: a proteção contra engano é o resumo com confirmação (FR-007). Se a professora errar, ela cancela a conta a receber gerada em Contas a Receber e corrige o restante fora do sistema; o pacote encerrado pelo acerto continua não reativável (FR-010).

### Key Entities *(include if feature involves data)*

- **Pacote do aluno**: o vínculo de cobrança de modalidade Pacote de um aluno numa turma de pacote: valor, total de aulas, saldo e estado ativo/inativo. A saída o encerra.
- **Acerto da saída**: o cálculo (consumidas, valor por aula, devido, pago informado, diferença, consequência) mostrado na prévia e usado na gravação. Resultado visível ao usuário: a conta da diferença, quando existe.
- **Conta a receber do acerto**: conta Pendente, ligada ao aluno e ao pacote, numa categoria de receita própria ("acerto de pacote"), com descrição dos números do acerto.
- **Matrícula na turma**: o vínculo aluno–turma que a saída desfaz.
- **Participação do aluno em aula agendada**: o aluno listado numa aula da turma que ainda não começou; a saída a retira (só a de matriculado, não a de reposição).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Em 100% dos casos de uma grade de teste (valores com e sem centavos, totais que não dividem exato, 0 e todas as aulas consumidas), o valor devido é igual a valor × consumidas ÷ total arredondado a centavos; o exemplo da regra (R$ 100, 10 aulas, 7 consumidas) dá R$ 70,00.
- **SC-002**: A professora conclui uma saída (abrir o resumo, informar o pago, confirmar) em menos de 1 minuto.
- **SC-003**: Em 100% das saídas com pago menor que o devido existe exatamente uma conta Pendente com o valor da diferença; em 100% das saídas com pago maior ou igual ao devido nenhuma conta é criada e nenhum lançamento de devolução existe.
- **SC-004**: 0 saídas parciais: em nenhuma situação (inclusive falha simulada na gravação) o aluno fica fora da turma com o pacote ativo, ou com o pacote encerrado sem a conta devida.
- **SC-005**: "Desvincular" continua com o comportamento de antes em 100% dos testes existentes da spec 063.
- **SC-006**: Os valores do resumo mostrado e os gravados são idênticos em 100% dos casos; quando o consumo mudou entre abrir e confirmar, a gravação é recusada e o novo resumo é mostrado.
- **SC-007**: Em 100% das saídas, o aluno deixa de constar em toda aula agendada da turma que ainda não começou e **nenhuma** aula realizada, cancelada, já iniciada, de reposição ou de outra turma muda; o número de aulas anunciado no resumo é igual ao de aulas de fato retiradas.

## Assumptions

- **Valor devido**: usa a regra do usuário (cada aula vale valor ÷ total de aulas). Arredondamento a centavos uma vez, sobre o total; o "valor por aula" mostrado é informativo.
- **Quem informa o pago**: o usuário decidiu que é a professora, na saída; o sistema não registra pagamentos do pacote (parcelas do pacote ficam para outra spec, "pacote parcelado").
- **Sem histórico próprio do acerto**: esta spec não cria uma tela ou tabela de histórico de acertos; os números ficam na descrição da conta a receber (quando existe) e o pacote fica inativo. Se a professora precisar auditar acertos sem diferença, isso é outra evolução.
- **Vencimento**: padrão hoje, sem datas passadas, para não criar uma conta já atrasada por engano.
- **Aulas futuras**: decidido pelo usuário em 2026-11-08 (ver Clarifications e FR-017).
- **Só pacote de turma**: o card e a regra tratam a saída de **turma de pacote**. O pacote de atendimento individual (sem turma) não tem "saída" e fica fora desta spec.
- **Só a professora logada** age (sem outros papéis); o admin não participa.
- **Fora de escopo**: parcelas/contas a receber do pacote, devolução ou crédito do aluno, "Desfazer saída", portal do aluno, layout para celular.
- **Dependências**: specs 038 (consumo e pacote), 058 (aulas dadas), 062 (contas ligadas ao vínculo, base da conta do acerto) e 063 (turma de pacote e saída), todas implementadas.
