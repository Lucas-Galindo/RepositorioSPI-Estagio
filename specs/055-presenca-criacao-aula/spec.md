# Feature Specification: Registrar Presença na Criação da Aula

**Feature Branch**: `055-presenca-criacao-aula`

**Created**: 2026-10-18

**Status**: Draft

**Input**: User description: "Melhoramento do cadastro de aulas: permitir registrar a presença dos alunos já na criação da aula, em vez de exigir uma ação separada depois. Ao criar uma aula cuja data é hoje ou passada, a tela de criação deve mostrar também a lista de alunos da turma selecionada (ou o aluno individual) com um checkbox de presença por aluno — mesmo padrão já usado em 'Registrar Sessão' (marcado = Presente). Ao salvar, a aula já é criada diretamente como 'Realizada', com os mesmos efeitos financeiros que já acontecem hoje em 'Registrar Sessão'. Se a data for futura, a tela continua exatamente como é hoje. Além disso, o campo de descrição da aula deve passar a poder ser editado mesmo depois da aula estar 'Realizada' (hoje qualquer edição é bloqueada nesse status) — as demais informações continuam bloqueadas. Fora de escopo: agenda fixa de aulas recorrentes e a página nova de gerenciamento na aba 'Aulas' (ficam para uma spec futura); nenhuma mudança na regra de geração de cobrança/decremento de saldo em si, só em quando ela é disparada."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Lançar uma aula que já aconteceu em um único passo (Priority: P1) 🎯 MVP

Hoje, para registrar uma aula que já aconteceu (por exemplo, a professora esqueceu de cadastrá-la antes e está lançando retroativamente), ela precisa primeiro criar a aula e, depois, abrir essa mesma aula numa tela separada ("Registrar Sessão") para marcar a presença de cada aluno. A professora quer fazer isso em um único passo: ao criar uma aula cuja data já chegou, o próprio formulário de criação já mostra a lista de alunos com checkbox de presença, e salvar o formulário já registra tudo de uma vez — a aula nasce "Realizada", com as presenças e os efeitos financeiros correspondentes já aplicados.

**Why this priority**: É o pedido central do card que originou esta spec — elimina a necessidade de dois passos separados para o caso mais comum de lançamento retroativo, sem mudar nenhuma regra financeira existente.

**Independent Test**: Pode ser testado criando uma aula de turma com data de ontem, marcando presença de parte dos alunos, salvando, e confirmando que a aula já aparece como "Realizada" com as presenças corretas e os mesmos efeitos financeiros que "Registrar Sessão" já produz hoje para o mesmo cenário.

**Acceptance Scenarios**:

1. **Given** a professora está preenchendo o formulário de criação de aula com uma Turma selecionada e a Data de Início igual a hoje, **When** a data é preenchida, **Then** o formulário passa a exibir a lista de alunos ativos da turma, cada um com um checkbox de presença marcado por padrão.
2. **Given** a lista de presença exibida com algum aluno desmarcado pela professora, **When** ela salva o formulário, **Then** a aula é criada com Status "Realizada", as presenças refletem exatamente o que foi marcado, e os efeitos financeiros de sempre são aplicados (cobrança gerada para presença sem vínculo/Avulsa, saldo de Pacote decrementado para quem tem Pacote, nenhum efeito avulso para Mensalidade).
3. **Given** a professora está criando uma aula com Data de Início no futuro, **When** o formulário é exibido, **Then** nenhuma lista de presença aparece — o formulário se comporta exatamente como hoje, e a aula é criada com Status "Agendada".
4. **Given** o formulário de criação já exibindo a lista de presença (data de hoje/passada), **When** a professora muda a data para uma data futura antes de salvar, **Then** a lista de presença desaparece e o formulário volta ao comportamento de aula agendada.
5. **Given** uma aula individual (sem turma, só um aluno) com data de hoje ou passada, **When** o formulário é exibido, **Then** a lista de presença mostra esse único aluno, com o mesmo comportamento de uma aula de turma.

---

### User Story 2 - Atualizar as observações de uma aula depois dela já ter acontecido (Priority: P2)

A professora quer conseguir anotar ou corrigir o que foi efetivamente lecionado numa aula mesmo depois dela já estar marcada como "Realizada" — hoje isso é impossível, porque qualquer edição de uma aula Realizada é bloqueada.

**Why this priority**: Complementa a User Story 1 — sem isso, uma aula lançada retroativamente (ou uma aula comum que já aconteceu) fica sem nenhuma forma de registrar ajustes no conteúdo dado depois do fato.

**Independent Test**: Pode ser testado abrindo uma aula já "Realizada", editando somente a descrição, salvando, e confirmando que a mudança persiste e que nenhum outro campo (data, hora, turma, matéria, aluno, presenças) pôde ser alterado na mesma operação.

**Acceptance Scenarios**:

1. **Given** uma aula com Status "Realizada", **When** a professora edita somente o campo de Descrição e salva, **Then** a alteração é aplicada com sucesso.
2. **Given** a mesma aula "Realizada", **When** a professora tenta alterar data, hora, turma, matéria ou aluno, **Then** o sistema rejeita a alteração, exatamente como já acontece hoje.

---

### Edge Cases

- Aluno com vínculo de Pacote já esgotado (saldo zero) numa aula lançada retroativamente: a presença desse aluno específico é bloqueada com o mesmo motivo/aviso já usado hoje em "Registrar Sessão" (specs/041); a aula continua sendo criada como "Realizada" normalmente para os demais alunos.
- Turma sem nenhum aluno ativo no momento da criação retroativa: a lista de presença aparece vazia, e a aula é criada normalmente como "Realizada" sem nenhuma presença para marcar.
- Presença de algum aluno não informada ao salvar uma aula retroativa: o sistema rejeita o envio, exigindo que todos os alunos da lista tenham uma presença marcada (mesma exigência já aplicada hoje por "Registrar Sessão").
- Depois que uma aula é criada "Realizada" por este fluxo, uma correção de presença (por exemplo, a professora marcou errado) segue a mesma regra já existente: presença não é editável depois que a aula está "Realizada" — essa limitação não muda nesta feature.
- Divergência de relógio entre o navegador e o servidor perto da virada do dia (ex.: o navegador ainda mostra a data de ontem, mas o servidor já considera hoje uma data posterior): o backend é a autoridade final sobre se a aula é retroativa (FR-001/FR-004), então a submissão pode ser rejeitada exigindo presença mesmo que a lista não tenha aparecido na tela — nesse caso a professora vê a mensagem de erro do servidor e pode recarregar a página para que o formulário reavalie a data corretamente.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O sistema MUST exibir, no formulário de criação de aula, uma lista com um checkbox de presença por aluno (os alunos ativos da turma selecionada, ou o aluno individual informado) sempre que a Data de Início informada for igual ou anterior à data atual.
- **FR-002**: O sistema MUST manter o formulário de criação de aula sem a lista de presença quando a Data de Início informada for posterior à data atual — nenhuma mudança de comportamento nesse caso.
- **FR-003**: Checkbox marcado MUST representar presença confirmada e desmarcado MUST representar falta, a mesma convenção já usada hoje na tela "Registrar Sessão"; todos os checkboxes MUST vir marcados por padrão.
- **FR-004**: Ao salvar uma aula cuja Data de Início seja igual ou anterior à data atual, o sistema MUST criar a aula diretamente com Status "Realizada" e MUST aplicar, com base nas presenças informadas no mesmo formulário, exatamente os mesmos efeitos já existentes para "Registrar Sessão" (geração de cobrança para presença sem vínculo de cobrança ou com vínculo Avulsa, decremento do saldo de aulas para vínculo Pacote, nenhum efeito avulso para vínculo Mensalidade).
- **FR-005**: O sistema MUST reaproveitar a mesma lógica já existente de geração de cobrança e decremento de saldo usada por "Registrar Sessão" — MUST NOT duplicar ou reimplementar essa regra para o fluxo de criação.
- **FR-006**: O sistema MUST continuar bloqueando a presença de um aluno cujo vínculo de cobrança Pacote esteja com saldo de aulas esgotado no momento da criação retroativa, usando o mesmo motivo e aviso já exibidos hoje por "Registrar Sessão", sem impedir a criação da aula para os demais alunos presentes na lista.
- **FR-007**: O sistema MUST exigir que a presença de cada aluno da lista seja informada antes de permitir salvar uma aula cuja Data de Início seja igual ou anterior à data atual.
- **FR-008**: O sistema MUST permitir editar o campo de Descrição de uma aula mesmo quando o Status dessa aula for "Realizada".
- **FR-009**: O sistema MUST continuar rejeitando qualquer tentativa de editar a Data de Início, Hora de Início, Hora de Fim, Turma, Matéria ou Aluno de uma aula cujo Status seja "Realizada" — nenhuma mudança nesse bloqueio, exceto a exceção explícita do campo Descrição (FR-008).
- **FR-010**: O sistema MUST NOT alterar a regra de quais vínculos de cobrança geram cobrança avulsa, decrementam saldo de Pacote, ou não têm efeito avulso (Mensalidade) — esta feature MUST NOT introduzir nenhuma mudança na regra financeira em si, apenas no momento em que ela passa a poder ser disparada (na criação, quando a data já chegou, em vez de exclusivamente depois via "Registrar Sessão").

### Key Entities

- **Aula**: entidade já existente (`Status`, `Descricao`, `DataInicio`, `AulaAlunos`); nenhum atributo novo é necessário — a feature reaproveita os campos já existentes.
- **AulaAluno**: entidade já existente (`Presente`, `MotivoNaoRegistro`); reaproveitada sem alteração de estrutura.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Uma aula com data de hoje ou anterior pode ser criada com a presença de todos os alunos já registrada em uma única submissão de formulário, sem exigir uma segunda ação do usuário em outra tela.
- **SC-002**: 100% dos efeitos financeiros aplicados por uma aula criada com presença incluída são idênticos aos que "Registrar Sessão" já produz hoje para o mesmo cenário de presença (mesma cobrança gerada, mesmo saldo decrementado).
- **SC-003**: O fluxo de criação de uma aula com data futura permanece idêntico ao comportamento atual, sem nenhuma diferença perceptível.
- **SC-004**: A descrição de uma aula "Realizada" pode ser atualizada em qualquer momento após sua criação, sem exigir alteração de nenhum outro campo da aula.

## Assumptions

- "Data igual ou anterior à atual" é comparada apenas pela data (sem considerar a hora de início) — uma aula marcada para mais tarde no mesmo dia já é tratada como elegível para registro de presença assim que a data atual chega, mesmo que o horário combinado ainda não tenha passado.
- A convenção de checkbox (marcado = presente, desmarcado = falta) é a mesma já usada hoje em "Registrar Sessão" — nenhuma inversão foi solicitada, conforme confirmado com o usuário.
- Esta feature não introduz nenhuma forma de corrigir a presença de uma aula depois que ela já foi criada como "Realizada" — essa limitação (presença imutável após "Realizada") já existe hoje e continua sem alteração; apenas a Descrição passa a ser editável nesse status.
- A agenda fixa de aulas recorrentes e a página nova de gerenciamento de aulas agendadas automaticamente na aba "Aulas" (mencionadas no pedido original) estão fora do escopo desta spec, por decisão explícita do usuário, e ficarão para uma especificação separada no futuro.
- Nenhuma mudança é feita na regra de quais modalidades de vínculo de cobrança geram cobrança, decrementam saldo, ou não têm efeito — apenas no momento em que essa regra passa a poder ser disparada.
