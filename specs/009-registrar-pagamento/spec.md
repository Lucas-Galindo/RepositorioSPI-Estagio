# Feature Specification: Registrar Pagamento

**Feature Branch**: `009-registrar-pagamento`

**Created**: 2026-09-11

**Status**: Implemented (documentação retroativa)

**Input**: Registro retroativo do comportamento real da Estória 9 (Registrar Pagamento) da ERS "PROFESSORAS INDEPENDENTES_v1.4", com base no código-fonte atual, não na ERS original.

## Nota histórica

A ERS original previa um Status "Pendente"/"Pago" simples, sempre persistido. A implementação real calcula o status "Atrasado" **dinamicamente a cada leitura** (comparando a Data de Vencimento com a data atual), em vez de gravá-lo automaticamente no banco — só é persistido quando explicitamente definido por uma atualização de status manual. Além disso, o cadastro de pagamento hoje também pode nascer automaticamente do registro de sessão de aula (ver [Registrar Sessão de Aula](../008-registrar-sessao-aula/spec.md)), não apenas de um lançamento manual da professora.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Registrar pagamento manualmente (Priority: P1)

Como professora, eu registro manualmente um pagamento de um aluno, vinculando uma ou mais aulas cobertas, informando valor, data de vencimento e forma de pagamento, para controlar o que ainda preciso receber.

**Why this priority**: É o fluxo principal de controle financeiro por aluno.

**Independent Test**: Chamar `POST /api/pagamentos` informando Aluno, Forma de Pagamento, Valor, Data de Vencimento e, opcionalmente, uma lista de Aulas cobertas.

**Acceptance Scenarios**:

1. **Given** Aluno, Forma de Pagamento e Valor válidos, **When** a professora registra o pagamento com Status inicial "Pendente", **Then** o pagamento é salvo sem Data de Pagamento preenchida.
2. **Given** os mesmos dados, **When** a professora registra o pagamento já com Status "Pago" (recebimento no ato), **Then** o sistema preenche automaticamente a Data de Pagamento com a data atual.
3. **Given** uma lista de Aulas informada para vincular ao pagamento, **When** alguma delas não existir, **Then** o sistema rejeita o registro.

---

### User Story 2 - Listar e filtrar pagamentos (Priority: P1)

Como professora, eu vejo a lista de pagamentos, filtro por aluno, período de vencimento e status (incluindo "Atrasado"), para acompanhar o que está pendente ou em atraso.

**Why this priority**: É o uso recorrente para cobrar valores pendentes.

**Independent Test**: Chamar `GET /api/pagamentos?status=Atrasado` e confirmar que retorna pagamentos "Pendentes" cuja Data de Vencimento já passou, sem que esse status esteja necessariamente gravado no banco.

**Acceptance Scenarios**:

1. **Given** um pagamento com Status "Pendente" salvo e Data de Vencimento no passado, **When** a professora consulta a lista, **Then** ele aparece com status efetivo "Atrasado", calculado no momento da consulta.
2. **Given** o mesmo pagamento, **When** a Data de Vencimento ainda não chegou, **Then** ele aparece com status "Pendente".
3. **Given** filtros de Aluno e/ou período de vencimento, **When** aplicados, **Then** a lista é restringida de acordo.

---

### User Story 3 - Editar dados de um pagamento (Priority: P2)

Como professora, eu edito descrição, categoria, forma de pagamento, data de vencimento, valor, competência e observações de um pagamento já registrado, sem poder alterar o aluno vinculado, as aulas cobertas ou o status por esse mesmo fluxo.

**Why this priority**: Corrige lançamentos incorretos sem comprometer a integridade do vínculo aluno/aulas.

**Independent Test**: Chamar `PUT /api/pagamentos/{id}` alterando Valor e Data de Vencimento, e confirmar que Aluno e Status permanecem inalterados.

**Acceptance Scenarios**:

1. **Given** um pagamento existente, **When** a professora altera Valor, Data de Vencimento, Forma de Pagamento, Categoria, Competência ou Observações, **Then** as alterações são salvas.
2. **Given** o mesmo formulário, **When** a professora tenta alterar o Aluno ou as Aulas vinculadas, **Then** isso não é suportado por esse endpoint — exige um novo registro.

---

### User Story 4 - Atualizar o status de um pagamento (Priority: P1)

Como professora, eu atualizo o status de um pagamento (por exemplo, de "Pendente" para "Pago"), e o sistema preenche automaticamente a Data de Pagamento quando aplicável.

**Why this priority**: É a ação que efetivamente baixa uma cobrança como recebida.

**Independent Test**: Chamar `PUT /api/pagamentos/{id}/status` com Status = "Pago" em um pagamento sem Data de Pagamento prévia.

**Acceptance Scenarios**:

1. **Given** um pagamento "Pendente" sem Data de Pagamento, **When** a professora muda o status para "Pago", **Then** a Data de Pagamento é preenchida automaticamente com a data atual.
2. **Given** um pagamento que já possui Data de Pagamento preenchida, **When** o status é alterado novamente para "Pago", **Then** a Data de Pagamento existente **não** é sobrescrita.
3. **Given** a atualização de status, **When** a professora informa qualquer um dos valores "Pendente", "Pago", "Atrasado" ou "Cancelado", **Then** o sistema aceita e persiste o valor informado (mais permissivo que o registro inicial, que só aceita "Pendente" ou "Pago").

---

### Edge Cases

- O status "Atrasado" fica gravado permanentemente no banco quando calculado automaticamente na listagem? Não — ele é recalculado a cada consulta; o valor persistido continua "Pendente" até uma atualização de status explícita.
- É possível gravar manualmente "Atrasado" ou "Cancelado" como status? Sim, via atualização explícita de status (`PUT /.../status`), embora nenhum fluxo automático do sistema faça isso sozinho.
- Existe algum indicador de exclusão lógica (`Ativo`) em Pagamento? Não — o estado do pagamento é controlado inteiramente pelo campo Status, sem um campo Ativo separado.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O sistema MUST permitir registrar um pagamento vinculado a um Aluno, com Forma de Pagamento, Valor, Data de Vencimento, e opcionalmente uma ou mais Aulas cobertas.
- **FR-002**: O sistema MUST aceitar Status inicial "Pendente" ou "Pago" no registro, e MUST preencher automaticamente a Data de Pagamento com a data atual quando o Status inicial for "Pago".
- **FR-003**: O sistema MUST calcular o status "Atrasado" dinamicamente sempre que um pagamento "Pendente" tiver Data de Vencimento anterior à data atual, sem exigir que esse valor esteja persistido para que apareça corretamente nas consultas e filtros.
- **FR-004**: O sistema MUST permitir editar Descrição, Categoria, Forma de Pagamento, Data de Vencimento, Valor, Competência e Observações de um pagamento existente, sem permitir alterar o Aluno ou as Aulas vinculadas por esse mesmo fluxo.
- **FR-005**: O sistema MUST permitir atualizar explicitamente o status de um pagamento para qualquer um dos valores "Pendente", "Pago", "Atrasado" ou "Cancelado".
- **FR-006**: Ao atualizar o status de um pagamento para "Pago", o sistema MUST preencher a Data de Pagamento com a data atual somente se ela ainda não estiver preenchida, preservando uma data de pagamento já existente.
- **FR-007**: O sistema MUST permitir listar e filtrar pagamentos por Aluno, período de Vencimento e Status (incluindo o status "Atrasado" calculado).

### Key Entities *(include if feature involves data)*

- **Pagamento**: Id, AlunoId, Descricao (opcional), FormaPagamentoId (opcional), CategoriaReceitaId (opcional), DataVencimento, DataPagamento (opcional), Competencia (opcional), ValorFinal, Status ("Pendente"/"Pago"/"Atrasado" calculado/"Cancelado"), Observacoes (opcional).
- **PagamentoAula**: vínculo N:N entre Pagamento e Aula.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% dos pagamentos "Pendentes" com vencimento já passado aparecem como "Atrasado" em qualquer consulta/listagem, sem depender de um job ou atualização manual prévia.
- **SC-002**: 100% dos pagamentos marcados como "Pago" recebem Data de Pagamento preenchida, sem sobrescrever uma data já existente em atualizações repetidas.
- **SC-003**: 0% das tentativas de alterar Aluno ou Aulas vinculadas por meio do endpoint de edição são aceitas.

## Assumptions

- O status "Atrasado" calculado dinamicamente é aceito como o comportamento correto atual, mesmo divergindo de uma ERS que talvez esperasse um valor sempre persistido.
- A geração automática de pagamentos a partir do registro de sessão de aula é tratada como parte de [Registrar Sessão de Aula](../008-registrar-sessao-aula/spec.md), não duplicada aqui.
- Este módulo é acessível apenas pela professora — não há hoje nenhuma via de consulta de pagamentos pelo próprio Aluno.
