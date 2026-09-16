# Feature Specification: Registrar Sessão de Aula

**Feature Branch**: `008-registrar-sessao-aula`

**Created**: 2026-09-11

**Status**: Implemented (documentação retroativa)

**Input**: Registro retroativo do comportamento real da Estória 8 (Registrar Sessão de Aula) da ERS "PROFESSORAS INDEPENDENTES_v1.4", com base no código-fonte atual, não na ERS original.

## Nota histórica

A ERS original descrevia apenas presença/frequência. A implementação real vai além: registrar uma sessão como realizada **gera automaticamente uma conta a receber (Pagamento pendente) para cada aluno presente** — uma integração com o módulo Financeiro (decisão de negócio de uma sprint posterior à ERS original, documentada explicitamente no código como "Sprint 4"). Este comportamento não previsto na ERS é parte central e real do fluxo hoje.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Registrar presença e concluir a aula (Priority: P1)

Como professora, ao final de uma aula agendada, eu marco presença ou falta de cada aluno vinculado e confirmo, para que a aula seja registrada como realizada.

**Why this priority**: É o fechamento do ciclo pedagógico da aula e a fonte de verdade da frequência dos alunos.

**Independent Test**: Chamar `POST /api/aulas/{id}/registrar-sessao` com um dicionário de presença para todos os alunos da aula.

**Acceptance Scenarios**:

1. **Given** uma aula com Status = "Agendada", **When** a professora informa presença/falta para cada aluno vinculado e confirma, **Then** o sistema atualiza a Frequência de cada aluno marcado como presente (incrementando um contador) e altera o Status da aula para "Realizada".
2. **Given** uma aula que não está com Status = "Agendada" (já Realizada ou Cancelada), **When** a professora tenta registrar a sessão, **Then** o sistema rejeita.
3. **Given** a lista de presença enviada, **When** falta a marcação de presença de algum aluno vinculado à aula, **Then** o sistema rejeita o registro até que todos estejam contemplados.
4. **Given** um aluno marcado como falta, **When** a sessão é registrada, **Then** a Frequência desse aluno **não** é incrementada.

---

### User Story 2 - Geração automática de conta a receber ao registrar sessão (Priority: P1)

Como professora, ao registrar uma sessão de aula com alunos presentes, o sistema gera automaticamente uma cobrança pendente para cada aluno presente, para que eu não precise lançar manualmente cada aula ministrada no financeiro.

**Why this priority**: Elimina retrabalho manual e é o elo real entre pedagógico e financeiro no sistema hoje.

**Independent Test**: Registrar uma sessão com dois alunos presentes e um ausente, e confirmar que exatamente duas novas contas a receber pendentes aparecem no módulo financeiro.

**Acceptance Scenarios**:

1. **Given** uma sessão registrada com alunos presentes, **When** o registro é confirmado, **Then** o sistema cria automaticamente um Pagamento com Status "Pendente" para cada aluno presente, vinculado àquela aula.
2. **Given** um aluno marcado como falta na sessão, **When** o registro é confirmado, **Then** nenhuma cobrança é gerada para esse aluno referente àquela aula.

---

### User Story 3 - Cancelar aula em vez de registrar sessão (Priority: P2)

Como professora, em vez de registrar a realização de uma aula, eu posso cancelá-la, sem impactar a frequência dos alunos.

**Why this priority**: Cobre o caso em que a aula não ocorreu por algum motivo (falta da própria professora, imprevisto), sem distorcer o histórico de frequência.

**Independent Test**: Chamar `POST /api/aulas/{id}/cancelar` em uma aula agendada e confirmar que nenhuma Frequência de aluno é alterada.

**Acceptance Scenarios**:

1. **Given** uma aula com Status = "Agendada", **When** a professora a cancela, **Then** o Status muda para "Cancelada" e a Frequência de nenhum aluno é alterada.
2. **Given** uma aula com Status = "Realizada", **When** a professora tenta cancelá-la, **Then** o sistema rejeita — não é possível cancelar uma aula já registrada como realizada.
3. **Given** uma aula de turma que é cancelada, **When** o cancelamento é confirmado, **Then** os lembretes configurados para aquela turma são recalculados automaticamente como efeito colateral.

---

### Edge Cases

- O que acontece se a professora tentar registrar sessão de uma aula individual (sem turma)? Funciona da mesma forma — o dicionário de presença cobre o único aluno vinculado.
- O que acontece se a professora cancelar uma aula já cancelada? O comportamento não bloqueia explicitamente esse caso adicional (idempotente na prática, sem validação exclusiva para essa repetição).
- Existe alguma forma de desfazer o registro de uma sessão já realizada (reverter para Agendada)? Não foi identificado esse fluxo — uma vez "Realizada", a edição de campos da aula fica bloqueada (ver [Gerenciar Aula](../005-gerenciar-aula/spec.md)).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O sistema MUST permitir registrar a sessão de uma aula somente quando ela estiver com Status = "Agendada".
- **FR-002**: O sistema MUST exigir a marcação explícita de presença (presente/faltou) para cada aluno vinculado à aula antes de aceitar o registro da sessão.
- **FR-003**: O sistema MUST incrementar a Frequência de cada aluno marcado como presente, e MUST manter inalterada a Frequência de alunos marcados como falta.
- **FR-004**: O sistema MUST alterar o Status da aula para "Realizada" ao concluir o registro da sessão.
- **FR-005**: O sistema MUST gerar automaticamente uma conta a receber (Pagamento com Status "Pendente") para cada aluno presente na sessão registrada, vinculada àquela aula, e MUST NOT gerar cobrança para alunos marcados como falta.
- **FR-006**: O sistema MUST permitir cancelar uma aula agendada (mudando seu Status para "Cancelada") sem alterar a Frequência de nenhum aluno, e MUST rejeitar o cancelamento de uma aula já "Realizada".
- **FR-007**: O sistema MUST recalcular os lembretes configurados para a turma de uma aula sempre que essa aula for cancelada.

### Key Entities *(include if feature involves data)*

- **AulaAluno**: vínculo entre Aula e Aluno com o indicador `Presente` (nulo até a sessão ser registrada; true/false depois).
- **Pagamento**: quando gerado automaticamente por esta funcionalidade, nasce com Status "Pendente", vinculado ao Aluno presente e à Aula correspondente (ver [Registrar Pagamento](../009-registrar-pagamento/spec.md)).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% das aulas com sessão registrada têm seu Status alterado para "Realizada" e a Frequência de cada aluno presente corretamente incrementada em exatamente 1.
- **SC-002**: 100% dos alunos presentes em uma sessão registrada recebem uma cobrança pendente correspondente, sem exceção e sem duplicidade.
- **SC-003**: 0% das aulas já "Realizadas" podem ser canceladas ou ter sua sessão registrada novamente.

## Assumptions

- A geração automática de conta a receber é tratada como parte deste fluxo (e não do módulo de Pagamento em si), pois é disparada exclusivamente pelo registro de sessão, não por uma ação financeira direta da professora.
- Não há, hoje, um mecanismo de estorno automático das contas a receber geradas caso a professora precise reverter manualmente uma sessão registrada por engano — esse cenário está fora do escopo observado.
