# Feature Specification: Notificações de Status de Aula Pendente

> **Estendida pela [spec 060](../060-centro-notificacoes/spec.md) (2026-10-19).** O sino virou um centro de notificações com vários tipos: além de "aula pendente", agora há "aula futura" (véspera, a partir das 08:00) e "pagamento pendente" (um por aluno), com filtro Todas/Aulas/Pagamentos. A regra desta spec para aula pendente não mudou.

**Feature Branch**: `059-notificacoes-aula-pendente`

**Created**: 2026-10-19

**Status**: Draft

**Input**: Card Trello "Notificações de status de aula pendente" mais as decisões tomadas com o usuário em 2026-10-19:
- só aulas que terminarem depois da entrada no ar geram aviso;
- aula individual aparece como "Atendimento individual".

## Contexto (estado atual, lido do código)

- Uma aula nasce "Agendada" e muda para "Realizada" (registrar sessão) ou "Cancelada". Ela também pode ser excluída (exclusão lógica).
- O cabeçalho já mostra um ícone de sino com um ponto, mas o ícone é só decorativo: não tem contagem nem lista e não faz nada.
- O sistema já tem rotinas automáticas periódicas (lembretes por e-mail, cobrança de mensalidade).
- A spec 060 vai estender o sino para um centro com vários tipos de notificação (aula futura, pagamento pendente). Esta spec já constrói a notificação com um **tipo**, para a 060 só acrescentar tipos.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Ser avisada de aula que passou sem status (Priority: P1)

A professora termina o dia sem marcar uma aula como realizada ou cancelada. Assim que o horário de término passa, o sino do cabeçalho mostra um contador, e a lista traz o aviso daquela aula.

**Why this priority**: É o objetivo do card. Sem isso, aulas ficam "Agendadas" para sempre, e a cobrança e a frequência não acontecem.

**Independent Test**: Criar uma aula de hoje que termina daqui a poucos minutos, esperar o término e conferir que aparece exatamente um aviso e que o contador sobe.

**Acceptance Scenarios**:

1. **Given** uma aula "Agendada" cujo término já passou, **When** a rotina roda ou a professora abre o sistema, **Then** existe exatamente uma notificação para essa aula.
2. **Given** uma aula "Agendada" cujo término ainda não chegou (inclusive uma de hoje que ainda não acabou), **When** a rotina roda, **Then** não há notificação para ela.
3. **Given** uma notificação já criada, **When** a rotina roda de novo (no mesmo dia ou nos seguintes), **Then** não se cria outra: continua a mesma, ainda ativa.
4. **Given** aulas de duas professoras, **When** cada uma abre o sino, **Then** cada uma vê só as notificações das próprias aulas.

---

### User Story 2 - Abrir o aviso e ir direto à aula (Priority: P1)

Ao clicar no sino, a professora vê a lista (mais recentes primeiro), com distinção entre lidas e não lidas. Ao clicar num aviso, abre um popup com:
- a mensagem;
- a data e o horário da aula;
- a matéria;
- a turma, ou "Atendimento individual" com o nome do aluno;
- o botão "Ir para a aula".

**Why this priority**: Sem o atalho, o aviso não ajuda a resolver.

**Independent Test**: Com um aviso não lido, abrir o popup e conferir os dados, que ele vira "lido" (o contador desce) e que o botão abre o registro daquela aula.

**Acceptance Scenarios**:

1. **Given** um aviso não lido, **When** a professora o abre, **Then** ele passa a "lido", o contador diminui e o aviso continua na lista.
2. **Given** o popup aberto, **When** a professora clica em "Ir para a aula", **Then** abre a tela de detalhes daquela aula, onde ela registra a sessão ou cancela.
3. **Given** uma aula individual, **When** o popup abre, **Then** mostra "Atendimento individual" e o nome do aluno no lugar da turma.

---

### User Story 3 - Aviso some quando a aula é atualizada (Priority: P1)

Ao registrar a aula como realizada, cancelá-la ou excluí-la, o aviso sai das pendências sozinho.

**Why this priority**: Sem isso, a lista acumularia avisos já resolvidos e perderia o sentido.

**Independent Test**: Com um aviso ativo, registrar a sessão da aula e conferir que ele some da lista e do contador.

**Acceptance Scenarios**:

1. **Given** um aviso ativo, **When** a aula vira "Realizada" ou "Cancelada", **Then** o aviso é resolvido e sai da lista e do contador.
2. **Given** um aviso ativo, **When** a aula é excluída, **Then** o aviso também é resolvido.
3. **Given** um aviso resolvido, **When** a rotina roda de novo, **Then** ele não volta.

### Edge Cases

- **Aula editada antes de acabar** para outra data ou horário: o término considerado é sempre o atual.
- **Aula editada depois de gerar o aviso**, passando a terminar no futuro: o aviso é resolvido, porque deixou de estar pendente. Se a aula passar de novo sem status, o mesmo aviso volta a ficar ativo; não se cria um segundo.
- **Aulas que terminaram antes da entrada no ar** da funcionalidade: não geram aviso (decisão do usuário), mesmo que estejam "Agendadas".
- **Fuso**: datas e horários das aulas são de Brasília, como no resto do sistema.
- **Sistema fora do ar** na hora do término: o aviso aparece na próxima execução da rotina ou na próxima abertura do sistema.

## Requirements *(mandatory)*

### Functional Requirements

**Quando há aviso**
- **FR-001**: Uma aula é **pendente** quando está ativa, com status "Agendada", e a data e o horário de término já passaram.
- **FR-002**: Para cada aula pendente, o sistema MUST manter exatamente uma notificação do tipo "aula pendente".
- **FR-003**: O sistema MUST NOT criar mais de uma notificação para a mesma aula, em nenhuma execução da rotina nem em nenhuma abertura do sistema, inclusive em execuções simultâneas.
- **FR-004**: Aulas cujo término é anterior ao início de vigência desta funcionalidade MUST NOT gerar notificação.
- **FR-005**: A verificação MUST rodar periodicamente, por rotina automática, e também quando a professora carrega a lista de notificações.

**Lista e popup**
- **FR-006**: A professora MUST ver só as notificações das próprias aulas.
- **FR-007**: O sino do cabeçalho MUST mostrar a quantidade de notificações ativas não lidas (sem número quando é zero).
- **FR-008**: A lista MUST mostrar as notificações ativas, das mais recentes para as mais antigas, distinguindo lidas e não lidas.
- **FR-009**: Abrir uma notificação MUST marcá-la como lida, sem resolvê-la.
- **FR-010**: O popup MUST mostrar:
  - a mensagem "Você ainda não atualizou o status desta aula.";
  - a data e o horário da aula;
  - a matéria;
  - a turma, ou "Atendimento individual" com o nome do aluno;
  - o botão "Ir para a aula", que abre os detalhes daquela aula.

**Resolução**
- **FR-011**: Quando a aula deixa de ser pendente (realizada, cancelada, excluída ou com término movido para o futuro), a notificação MUST ser resolvida e sair da lista e do contador.
- **FR-012**: Uma notificação resolvida MUST NOT voltar a aparecer, a não ser que a mesma aula volte a ficar pendente; nesse caso, a mesma notificação é reativada, como não lida.

**Base para a spec 060**
- **FR-013**: A notificação MUST ter um tipo, para a spec 060 acrescentar outros tipos sem refazer a estrutura.

### Key Entities

- **Notificação**:
  - professora destinatária, tipo ("aula pendente") e referência à aula;
  - quando foi criada e quando foi lida (vazio = não lida);
  - quando foi resolvida (vazio = ativa).
  - Uma por aula e por tipo.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% das aulas que terminam sem status, depois da entrada no ar, têm aviso em até 5 minutos após o término (ou na próxima abertura do sistema).
- **SC-002**: Zero avisos duplicados para a mesma aula, depois de qualquer número de execuções.
- **SC-003**: O aviso sai da lista na próxima atualização do sino depois que a aula é atualizada (no máximo 1 minuto com a tela aberta, ou imediatamente ao navegar).
- **SC-004**: Do sino até a tela da aula, a professora chega em no máximo 2 cliques.

## Assumptions

- **Início de vigência** (FR-004): é uma configuração do servidor, com o valor padrão 2026-10-19 00:00, a data de implementação. É ajustável na implantação sem mexer no código.
- **Rotina**: roda a cada 5 minutos, intervalo configurável como as rotinas existentes.
- **Atualização do sino**: o sino consulta a lista ao carregar cada tela da área logada e a cada 60 segundos com a tela aberta.
- **Perfil**: só a Professora vê o sino funcional; o Administrador não tem aulas.
- **E-mail**: as notificações desta spec não enviam e-mail (e-mail é tema da spec 060).
