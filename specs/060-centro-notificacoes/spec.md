# Feature Specification: Centro de Notificações (Aulas e Pagamentos) e Aba Lembretes

**Feature Branch**: `060-centro-notificacoes`

**Created**: 2026-10-19

**Status**: Draft

**Input**: Card Trello "Centro de notificações (aulas e pagamentos) e aba 'Lembretes'". Ele estende a spec 059. Decisões do usuário em 2026-10-19:
- o e-mail de mensalidade 5 dias antes continua como está;
- a regra de pagamento pendente é totalmente editável (ligar/desligar, dia e horário);
- a antecedência do aviso de aula futura fica em configuração do servidor.

Regra de segurança da sessão: em desenvolvimento, todo e-mail vai só para o endereço de teste do usuário.

## Contexto (estado atual, lido do código)

- A spec 059 entregou o sino com o tipo "aula pendente", uma notificação por aula (tabela `notificacao`, com tipo).
- **Pagamentos**: a cobrança nasce "Pendente". Ela é exibida como "Atrasado" (calculado, não gravado) quando o vencimento já passou e ninguém deu baixa.
- **Aluno**: tem e-mail próprio e e-mail do responsável, os dois opcionais. Aluno e pagamento não pertencem a uma professora específica.
- **Aba Lembretes**: hoje gerencia só lembretes de aula por turma.
- **E-mails já enviados pelo sistema**:
  - lembrete de aula;
  - lembrete de mensalidade 5 dias antes;
  - recuperação de senha.
- **Destinatário em desenvolvimento**: hoje nada impede um envio de desenvolvimento de chegar a um aluno real.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Aviso de pagamento pendente para a professora (Priority: P1)

Quando um aluno tem um pagamento vencido sem baixa, a professora recebe **uma** notificação para esse aluno.

O popup lista cada pendência com:
- vínculo ou descrição;
- valor;
- vencimento;
- dias em atraso;
- um atalho para o registro do pagamento.

**Why this priority**: É o principal ganho do card: a professora enxerga quem está devendo sem procurar.

**Independent Test**: Criar dois pagamentos vencidos para um aluno e um que vence hoje, e conferir uma notificação listando só os dois vencidos. Dar baixa nos dois e conferir que a notificação some.

**Acceptance Scenarios**:

1. **Given** um aluno com pagamentos "Pendente" vencidos antes de hoje, **When** o sistema verifica, **Then** existe exatamente uma notificação de pagamento pendente para esse aluno.
2. **Given** um pagamento que vence hoje (ou no futuro), **When** o sistema verifica, **Then** ele não conta como pendência.
3. **Given** a notificação aberta, **When** a professora a lê, **Then** vê cada pendência (vínculo ou descrição, valor, vencimento, dias em atraso) e pode abrir o registro do pagamento.
4. **Given** todas as pendências do aluno pagas ou canceladas, **When** o sistema verifica, **Then** a notificação é resolvida.
5. **Given** o mesmo aluno volta a ter pendência depois, **When** o sistema verifica, **Then** a mesma notificação volta como não lida; nenhuma duplicada é criada.

---

### User Story 2 - E-mail semanal de cobrança para aluno e responsável (Priority: P1)

Enquanto houver pendência, o aluno e o responsável recebem, toda segunda-feira às 08:00 (padrão editável), o mesmo e-mail com as pendências do aluno juntas. A professora não recebe e-mail.

**Why this priority**: É a cobrança ativa pedida no card.

**Independent Test**: Com um aluno com pendência vencida antes da segunda-feira, simular a execução de segunda às 08:00. Conferir um envio registrado para os dois e-mails; uma segunda execução no mesmo dia não reenvia.

**Acceptance Scenarios**:

1. **Given** a regra ativa (segunda, 08:00) e um aluno com pendência vencida antes daquela segunda, **When** chega segunda às 08:00, **Then** aluno e responsável recebem um único e-mail com todas as pendências do aluno (valor, vencimento, dias em atraso).
2. **Given** um pagamento que vence na própria segunda-feira, **When** chega aquela segunda, **Then** ele não entra; o primeiro envio será na segunda seguinte.
3. **Given** o e-mail já enviado com sucesso naquela segunda, **When** a rotina roda de novo no mesmo dia, **Then** não reenvia.
4. **Given** a situação atualizada (sem pendências), **When** chega a próxima segunda, **Then** nada é enviado.
5. **Given** o envio falhou, **When** a rotina roda de novo no mesmo dia, **Then** tenta novamente. A falha fica registrada, e a notificação para a professora existe de qualquer forma.
6. **Given** aluno sem e-mail próprio nem de responsável, **When** chega a segunda, **Then** o envio é registrado como "sem destinatário", sem erro.
7. **Given** a regra desligada, **When** chega a segunda, **Then** nada é enviado.

---

### User Story 3 - Aviso de aula futura (Priority: P2)

Na véspera de cada aula agendada, a partir das 08:00 (antecedência configurável), aparece um aviso informativo com data, horário, matéria, turma e o botão "Abrir aula".

**Why this priority**: É útil, mas informativo; não bloqueia nada.

**Independent Test**: Uma aula amanhã às 10h aparece hoje a partir das 08:00. Cancelada, some. Remarcada para depois de amanhã, some hoje e volta amanhã às 08:00.

**Acceptance Scenarios**:

1. **Given** uma aula agendada para amanhã, **When** são 08:00 de hoje ou mais tarde, **Then** existe um aviso de aula futura para ela.
2. **Given** o aviso ativo, **When** a aula termina, é cancelada ou é excluída, **Then** o aviso sai da lista. Se a aula terminar sem status, vale o aviso de aula pendente (spec 059).
3. **Given** a aula remarcada, **When** o sistema verifica, **Then** o aviso acompanha a nova data: sai, se ficou fora da janela, e volta na nova véspera. Os dados mostrados são sempre os atuais da aula.

---

### User Story 4 - Centro de notificações com tipos e filtro (Priority: P1)

O sino soma todas as notificações não lidas. A lista mostra um rótulo por tipo ("Aula" ou "Pagamento") e tem filtro: Todas, Aulas, Pagamentos.

**Why this priority**: Junta as US1-US3 numa só interface.

**Acceptance Scenarios**:

1. **Given** notificações de aula pendente, aula futura e pagamento, **When** a professora abre o sino, **Then** o contador soma as não lidas de todos os tipos e cada item tem o rótulo do tipo.
2. **Given** o filtro "Pagamentos", **When** aplicado, **Then** só aparecem as de pagamento.

---

### User Story 5 - Regra e histórico na aba Lembretes (Priority: P2)

A aba Lembretes ganha a seção "Pagamento pendente":
- a regra: ligada ou desligada, dia da semana, horário e a explicação "repete até resolver";
- o histórico de envios: data e hora, aluno, destinatários e resultado.

**Why this priority**: Gestão e auditoria dos e-mails.

**Acceptance Scenarios**:

1. **Given** a aba Lembretes, **When** a professora abre a seção, **Then** vê a regra atual (padrão: ligada, segunda-feira, 08:00) e pode alterá-la.
2. **Given** envios já feitos, **When** a professora abre o histórico, **Then** vê cada envio (mais recentes primeiro) com aluno, destinatários e resultado (Enviado, Falha ou Sem destinatário).

---

### User Story 6 - E-mail nunca chega a aluno real em desenvolvimento (Priority: P1)

Em ambiente de desenvolvimento, todo e-mail do sistema vai para o endereço de teste configurado, com o destinatário original indicado no assunto. Sem esse endereço configurado, o envio é recusado.

**Why this priority**: Regra de segurança do usuário; é pré-requisito para testar a US2.

**Acceptance Scenarios**:

1. **Given** o ambiente de desenvolvimento com o endereço de teste configurado, **When** qualquer e-mail é enviado, **Then** ele vai só para o endereço de teste.
2. **Given** o ambiente de desenvolvimento sem o endereço de teste, **When** qualquer e-mail seria enviado, **Then** o envio é recusado com erro, e nada sai.
3. **Given** produção, **When** um e-mail é enviado, **Then** ele vai ao destinatário real, como hoje.

### Edge Cases

- **Mesmo endereço** para aluno e responsável: um e-mail só.
- **Pagamento com baixa** entre a verificação e o e-mail: o e-mail usa as pendências do momento do envio.
- **Sistema fora do ar na segunda**: os envios daquela semana não são feitos depois; o próximo é na segunda seguinte. A notificação no sistema não depende do e-mail.
- **Aula futura de aula individual**: mostra "Atendimento individual · aluno", como na 059.
- **Aluno inativo com pendência**: continua gerando notificação e e-mail, porque a dívida continua.

## Requirements *(mandatory)*

### Functional Requirements

**Pagamento pendente: notificação**
- **FR-001**: Pendência é um pagamento com status "Pendente" e vencimento anterior à data de hoje.
- **FR-002**: Para cada aluno com pendência, a professora MUST ter exatamente uma notificação do tipo "pagamento pendente".
- **FR-003**: O sistema MUST NOT duplicar essa notificação entre execuções. Ela é reativada como não lida se o aluno voltar a ter pendência.
- **FR-004**: A notificação MUST ser resolvida quando o aluno não tem mais pendências.
- **FR-005**: O popup MUST listar, por pendência: descrição ou vínculo, valor, vencimento, dias em atraso e atalho para o registro do pagamento.

**Pagamento pendente: e-mail**
- **FR-006**: Na regra ativa (dia da semana D, hora H; padrão segunda, 08:00), a partir de H do dia D o sistema MUST enviar um e-mail por aluno com pendência vencida antes de D. O e-mail vai ao e-mail do aluno e ao do responsável (sem repetir endereço) e reúne todas as pendências.
- **FR-007**: O sistema MUST NOT enviar mais de um e-mail com sucesso por aluno por dia de envio, nem enviar e-mail à professora.
- **FR-008**: Envio com falha MUST ser registrado e tentado de novo nas execuções seguintes do mesmo dia de envio.
- **FR-009**: Cada tentativa MUST ser registrada com data e hora, aluno, destinatários e resultado.
- **FR-010**: A regra (ligada, dia, hora) MUST ser editável pela professora na aba Lembretes e MUST ter efeito real nos envios.

**Aula futura**
- **FR-011**: Para cada aula ativa e agendada, MUST existir um aviso "aula futura" desde N dias antes, às H (configuração do servidor; padrão 1 dia, 08:00), até o término da aula.
- **FR-012**: Aula cancelada, excluída, terminada ou remarcada para fora da janela MUST ter o aviso resolvido. A mesma notificação é reativada quando a aula volta à janela.

**Centro**
- **FR-013**: O sino MUST somar as não lidas de todos os tipos. A lista MUST rotular o tipo e oferecer o filtro Todas, Aulas ou Pagamentos.
- **FR-014**: Só a professora vê notificações; e-mail vai só para aluno e responsável.

**Segurança de e-mail**
- **FR-015**: Em desenvolvimento, todo e-mail do sistema MUST ir para o endereço de teste configurado; sem ele configurado, o sistema MUST NOT enviar.

### Key Entities

- **Notificação** (spec 059): ganha os tipos "aula futura" e "pagamento pendente". A de pagamento se refere a um aluno, e não a uma aula.
- **Regra de lembrete de pagamento pendente**: ligada, dia da semana, hora. Existe uma só.
- **Envio de lembrete de pagamento**: aluno, dia de envio, data e hora da última tentativa, destinatários, resultado, número de tentativas e detalhe da falha.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Zero notificações duplicadas por aluno ou aula, e zero e-mails duplicados por aluno no mesmo dia de envio.
- **SC-002**: Um aluno com pendência recebe e-mail em todas as segundas (ou no dia configurado) até a baixa, e em nenhuma depois.
- **SC-003**: Em desenvolvimento, 100% dos e-mails vão para o endereço de teste.
- **SC-004**: O contador do sino é igual ao número de notificações ativas não lidas, somando todos os tipos.

## Assumptions

- **Professora**: o sistema é de uma professora só (single-tenant, como já está no código). Aluno e pagamento não têm professora, então a notificação de pagamento vai para a professora cadastrada.
- **Fuso**: horários de Brasília (relógio do servidor, como as demais rotinas).
- **Atalho do pagamento**: abre a tela de detalhes de contas a receber já existente.
- **Mensalidade**: o lembrete de mensalidade 5 dias antes continua como está e não aparece na seção nova.
- **Endereço de teste**: fica na configuração de desenvolvimento, nunca na de produção.
