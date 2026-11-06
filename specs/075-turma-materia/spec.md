# Feature Specification: Turma ↔ Matéria — vincular a matéria no cadastro da turma

**Feature Branch**: `075-turma-materia`

**Created**: 2026-11-05

**Status**: Draft

**Input**: Pedido do usuário (2026-11-05), card do Trello "Turma ↔ Matéria: vincular matéria no cadastro da turma (banco + backend + formulário)": "Vamos incluir na opção de cadastro o vínculo daquela turma com uma matéria. Isso vai mudar o banco e mais algumas estruturas." Base para o filtro de matéria da tela de Turmas (card separado).

## Contexto (estado atual, lido do código)

- A turma tem nome, tipo (Mensalidade/Pacote), ativo e as datas da turma de pacote. **Não tem matéria**: a matéria só existe em cada aula (`aula.materia_id` e `aula.turma_id`).
- O formulário da turma (cadastro e edição) pede nome, tipo e, no pacote, as datas. O formulário de nova aula tem o seletor de turma e o de matéria, independentes.
- As matérias têm "ativo" (exclusão lógica). O banco local tem 4 turmas e 11 aulas.
- O banco é alterado por scripts numerados em `database/` (o último é o 26).

## Clarificações (respondidas pelo usuário em 2026-11-05)

- **Uma matéria por turma**, como pedido.
- A matéria da turma é **opcional** (turma pode ficar sem matéria; as atuais continuam válidas).
- As turmas **já existentes** recebem a matéria **a partir das aulas** quando todas as aulas delas usam a mesma matéria; as demais ficam sem matéria (nada é adivinhado nem apagado).
- No formulário de **nova aula**, quando a turma escolhida tem matéria, a matéria da aula vem **pré-selecionada, mas pode ser trocada** (a matéria da turma não restringe as aulas).

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Escolher a matéria ao cadastrar ou editar a turma (Priority: P1)

A professora cria ou edita uma turma e, no formulário, escolhe a matéria (opcional) numa lista; a matéria fica gravada e aparece na turma.

**Why this priority**: É o pedido central e a base do filtro de matéria.

**Independent Test**: Criar uma turma escolhendo "Matemática", salvar e conferir que a matéria aparece na turma; editar trocando para outra e depois removendo.

**Acceptance Scenarios**:

1. **Given** o formulário de nova turma, **When** ela abre, **Then** há o campo "Matéria (opcional)" com as matérias **ativas** e a opção "Sem matéria".
2. **Given** uma matéria escolhida, **When** ela salva, **Then** a turma é criada com essa matéria e a mostra (na lista de turmas e nos detalhes).
3. **Given** uma turma com matéria, **When** ela edita, **Then** o campo vem preenchido, pode ser trocado ou esvaziado ("Sem matéria") e o resultado é gravado.
4. **Given** uma turma cuja matéria foi inativada depois, **When** ela edita a turma, **Then** a matéria atual continua aparecendo no campo (marcada como inativa) e pode ser mantida; **só as ativas** são oferecidas para novas escolhas.
5. **Given** uma matéria inexistente ou inativa enviada por outro meio, **Then** o servidor recusa com mensagem clara.

---

### User Story 2 - Turmas existentes já vêm com a matéria quando dá para saber (Priority: P1)

Ao aplicar a migração, cada turma cujas aulas usam **uma única matéria** recebe essa matéria; as demais ficam sem matéria, sem nenhuma alteração em aulas, vínculos ou pagamentos.

**Why this priority**: Evita a professora ter de preencher tudo à mão e dá resultado imediato no filtro.

**Independent Test**: Aplicar a migração no banco local e conferir, turma por turma, a matéria esperada contra as aulas.

**Acceptance Scenarios**:

1. **Given** uma turma com aulas só de "Matemática", **When** a migração roda, **Then** a turma fica com "Matemática".
2. **Given** uma turma com aulas de duas matérias, **Then** fica sem matéria.
3. **Given** uma turma sem aulas, **Then** fica sem matéria.
4. **Given** a migração aplicada duas vezes, **Then** nada muda na segunda (idempotente) e nada é apagado.

---

### User Story 3 - Aula da turma já sugere a matéria da turma (Priority: P2)

Ao agendar uma aula e escolher uma turma que tem matéria, a matéria da aula vem preenchida com a da turma; a professora pode trocar.

**Why this priority**: Conveniência; não muda regra alguma.

**Independent Test**: Em Nova aula, escolher uma turma com matéria e ver a matéria pré-selecionada; trocar e salvar uma aula de outra matéria com sucesso.

**Acceptance Scenarios**:

1. **Given** Nova aula sem matéria escolhida, **When** ela escolhe uma turma com matéria, **Then** a matéria da aula é preenchida com a da turma.
2. **Given** uma matéria já escolhida pela professora, **When** ela escolhe a turma, **Then** a escolha dela **não** é sobrescrita.
3. **Given** a matéria pré-selecionada, **When** ela escolhe outra, **Then** a aula é salva com a outra (sem erro).
4. **Given** uma turma sem matéria ou aula individual, **Then** nada é pré-selecionado.
5. **Given** a edição de uma aula existente, **Then** nada é pré-selecionado nem alterado.

---

### Edge Cases

- Turma sem matéria continua funcionando em tudo (aulas, matrículas, cobranças, relatórios).
- Matéria inativada depois do vínculo: a turma mantém o vínculo e o nome da matéria continua visível.
- Matéria excluída logicamente nunca é apagada do banco (exclusão lógica), então o vínculo nunca fica órfão.
- Aula de matéria diferente da matéria da turma é permitida (a matéria da turma não restringe).
- A migração não pode falhar se uma turma tiver aulas inativas ou canceladas: a regra considera as aulas ativas da turma.
- Turma de pacote e de mensalidade usam o mesmo campo.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: A turma MUST poder ter **no máximo uma matéria** (opcional), gravada no banco com chave para a matéria.
- **FR-002**: O cadastro e a edição de turma (API e formulário) MUST aceitar a matéria opcional; no PUT, matéria ausente/nula **remove** o vínculo (o formulário sempre envia o valor atual).
- **FR-003**: O servidor MUST recusar matéria inexistente e matéria **inativa** ao **criar o vínculo ou trocá-lo**; uma turma que já tem uma matéria depois inativada MUST poder ser editada mantendo-a.
- **FR-004**: A resposta da turma (lista e detalhe) MUST trazer `materiaId` e `materiaNome`; a tela de Turmas e os detalhes da turma MUST mostrar a matéria quando houver.
- **FR-005**: A migração MUST preencher a matéria das turmas existentes **somente** quando todas as aulas ativas da turma usam a mesma matéria, MUST NOT alterar nem apagar aulas, vínculos de cobrança, pagamentos, matrículas ou qualquer outra coluna, e MUST ser idempotente.
- **FR-006**: No formulário de **nova** aula, escolher uma turma com matéria MUST pré-selecionar a matéria da aula **apenas se a professora ainda não tiver escolhido uma**; a troca MUST continuar livre.
- **FR-007**: A regra de cadastro de aula (validação, conflito de horário, cobrança) MUST NOT mudar: a matéria da turma **não** pode ser exigida nem comparada com a da aula no servidor.
- **FR-008**: Turmas sem matéria MUST continuar com o comportamento de hoje em todas as telas; os valores e a ordem das listas existentes MUST NOT mudar.
- **FR-009**: A mudança MUST NOT adicionar dependência de pacote nem alterar outras tabelas além de `turma`; MUST NOT aplicar a migração em produção (fica no banco local; produção em standby por decisão do usuário).

### Key Entities

- **Turma** ganha `materia_id` (opcional) e a navegação para a matéria; **Matéria** não muda.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Turma criada ou editada com matéria devolve o `materiaNome` correto em 100% dos casos; recusa 100% das matérias inexistentes ou inativas escolhidas.
- **SC-002**: Após a migração, 100% das turmas com aulas de uma só matéria recebem essa matéria e 0 turma com aulas de várias matérias recebe alguma; 0 linha apagada e 0 linha de outra tabela alterada (contagens antes/depois iguais).
- **SC-003**: 0 diferença nas respostas e telas existentes para turmas sem matéria; a regra de aulas não muda (nenhum teste de aula alterado de resultado).
- **SC-004**: Em Nova aula, a pré-seleção acontece só quando permitido (matéria ainda não escolhida) e a troca sempre funciona.

## Assumptions

- "Matérias ativas" = `ativo = TRUE` na tabela de matérias.
- Aulas **ativas** (`ativo = TRUE`) entram no preenchimento da migração, inclusive realizadas e canceladas.
- O filtro de matéria na tela de Turmas é outro card (usa este campo); aqui só se grava e exibe.

## Fora de escopo

- Filtro de matéria na tela de Turmas, várias matérias por turma e restringir a matéria das aulas.
- Aplicar a migração em produção.
