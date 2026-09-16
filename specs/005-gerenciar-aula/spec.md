# Feature Specification: Gerenciar Aula

**Feature Branch**: `005-gerenciar-aula`

**Created**: 2026-09-11

**Status**: Implemented (documentação retroativa)

**Input**: Registro retroativo do comportamento real da Estória 4 (Gerenciar Aula) da ERS "PROFESSORAS INDEPENDENTES_v1.4", com base no código-fonte atual, não na ERS original.

## Nota histórica

A ERS original descrevia o cadastro/edição/exclusão de aulas de forma genérica. A implementação real adiciona regras não previstas (aula individual exige um Aluno específico quando não há Turma; aula vinculada a Turma associa automaticamente todos os alunos ativos dela) e mantém algumas lacunas de validação (não valida se a Turma está ativa; permite editar ou excluir aulas já Canceladas ou Realizadas sem bloqueio total). Este documento descreve o comportamento real.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Cadastrar aula individual ou de turma (Priority: P1)

Como professora, eu cadastro uma aula escolhendo a matéria e, opcionalmente, uma turma; quando não escolho turma, informo diretamente o aluno individual atendido.

**Why this priority**: É o registro central de toda a operação pedagógica do sistema.

**Independent Test**: Chamar `POST /api/aulas` informando Matéria, Data, Hora Início/Fim e, alternativamente, TurmaId ou AlunoId.

**Acceptance Scenarios**:

1. **Given** uma Turma informada, **When** a professora cadastra a aula, **Then** o sistema vincula automaticamente todos os alunos atualmente ativos daquela turma à aula — não é necessário informar AlunoId nesse caso (e informá-lo é rejeitado).
2. **Given** nenhuma Turma informada, **When** a professora cadastra a aula, **Then** o sistema exige que um AlunoId seja informado (aula individual).
3. **Given** Hora de Fim menor ou igual à Hora de Início, **When** a professora tenta salvar, **Then** o sistema rejeita.
4. **Given** uma nova aula salva com sucesso, **When** o sistema confirma o cadastro, **Then** a aula recebe Status = "Agendada" automaticamente (não é um campo escolhido pela professora).

---

### User Story 2 - Impedir conflito de horário (Priority: P1)

Como professora, o sistema impede que eu cadastre ou altere uma aula cujo horário se sobreponha a outra aula minha já existente no mesmo dia.

**Why this priority**: Evita double-booking na agenda, um dos principais motivadores de adotar o sistema.

**Independent Test**: Cadastrar duas aulas da mesma professora no mesmo dia com horários sobrepostos e confirmar que a segunda é rejeitada.

**Acceptance Scenarios**:

1. **Given** uma aula já cadastrada das 14h às 15h em um dia, **When** a professora tenta cadastrar outra aula sua nesse mesmo dia com sobreposição de horário (ex.: 14h30 às 15h30), **Then** o sistema rejeita com um erro de conflito.
2. **Given** uma aula marcada como "Cancelada" naquele mesmo intervalo, **When** a professora cadastra uma nova aula sobreposta, **Then** o sistema permite, pois aulas canceladas não contam para a detecção de conflito.
3. **Given** a edição de uma aula existente, **When** a professora mantém o mesmo horário (ou ajusta sem conflitar), **Then** o sistema não a compara consigo mesma (ela é ignorada na checagem de conflito).
4. **Given** o mesmo formulário de aula é usado tanto na tela completa de Aulas quanto no agendamento rápido pela Agenda, **When** um conflito ocorre em qualquer um dos dois pontos de entrada, **Then** o mesmo erro de conflito é exibido, pois ambos usam a mesma validação central.

---

### User Story 3 - Listar, buscar e editar aulas (Priority: P1)

Como professora, eu vejo a lista de aulas, filtro por status e turma, e edito uma aula existente.

**Why this priority**: É o uso recorrente para acompanhar a agenda pedagógica.

**Independent Test**: Chamar `GET /api/aulas` com filtros de `status`/`turmaId` e depois `PUT /api/aulas/{id}`.

**Acceptance Scenarios**:

1. **Given** a lista de aulas, **When** a professora filtra por Status e/ou Turma, **Then** a lista é restringida de acordo.
2. **Given** uma aula com Status = "Realizada", **When** a professora tenta editar Matéria, Data ou horários dessa aula, **Then** o sistema rejeita a alteração.
3. **Given** uma aula com Status = "Cancelada", **When** a professora tenta editá-la, **Then** o sistema **permite** a edição (não há bloqueio para aulas canceladas, apenas para realizadas).

---

### Edge Cases

- O que acontece se a professora excluir uma aula já Realizada? É permitido — a exclusão lógica de aula não verifica o status atual.
- O que acontece se a Turma escolhida estiver inativa? O sistema não valida isso — é possível cadastrar uma aula vinculada a uma turma inativa, pois a checagem de referências só confirma que a turma existe, não que está ativa.
- Aulas que atravessam a meia-noite são suportadas? Não — a aula é sempre de um único dia (uma data com hora de início e hora de fim dentro do mesmo dia).
- O que acontece ao cancelar uma aula? A frequência dos alunos não é alterada, e lembretes vinculados à turma daquela aula são recalculados automaticamente como efeito colateral.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O sistema MUST exigir Matéria obrigatória em toda aula, e MUST exigir Turma ou Aluno individual (mutuamente exclusivos: se Turma é informada, Aluno individual não pode ser informado, e vice-versa).
- **FR-002**: Ao vincular uma aula a uma Turma, o sistema MUST associar automaticamente todos os alunos atualmente ativos daquela turma à aula.
- **FR-003**: O sistema MUST validar que a Hora de Fim é posterior à Hora de Início.
- **FR-004**: O sistema MUST impedir o cadastro ou a alteração de uma aula cujo horário se sobreponha, no mesmo dia e para a mesma professora, a outra aula ativa e não cancelada — usando a mesma validação central tanto no cadastro pelo formulário completo quanto no agendamento rápido pela Agenda.
- **FR-005**: O sistema MUST atribuir automaticamente Status = "Agendada" a toda nova aula, sem permitir que esse valor seja definido diretamente pela professora no cadastro.
- **FR-006**: O sistema MUST impedir a edição de campos de uma aula com Status = "Realizada" (matéria, data, horários, turma).
- **FR-007**: O sistema MUST permitir listar e filtrar aulas por Status e Turma (e, via API, também por Aluno e por período, ainda que esse último filtro não seja exposto em todas as telas).
- **FR-008**: O sistema MUST realizar exclusão lógica de aula, sem restringir a exclusão por status atual da aula.

### Key Entities *(include if feature involves data)*

- **Aula**: Id, Descricao (opcional), MateriaId, ProfessorId, TurmaId (opcional), DataInicio, HoraInicio, HoraFim, Status (Agendada/Realizada/Cancelada), Ativo.
- **AulaAluno**: vínculo entre Aula e Aluno, com indicador de presença (nulo até a sessão ser registrada) — ver [Registrar Sessão de Aula](../008-registrar-sessao-aula/spec.md).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% das tentativas de cadastrar uma aula com sobreposição de horário para a mesma professora são rejeitadas, independentemente de o cadastro ter sido feito pela tela de Aulas ou pelo agendamento rápido da Agenda.
- **SC-002**: 100% das aulas novas iniciam com Status = "Agendada", sem exceção.
- **SC-003**: 100% das tentativas de editar campos de uma aula já Realizada são rejeitadas.
- **SC-004**: A professora consegue localizar qualquer aula cadastrada filtrando por Status e Turma em uma única consulta.

## Assumptions

- A ausência de validação de "turma ativa" ao cadastrar aula é aceita como comportamento atual do sistema, não corrigida neste registro retroativo.
- A permissão de editar/excluir aulas Canceladas (e excluir aulas Realizadas) é aceita como comportamento atual, ainda que uma ERS mais restritiva pudesse esperar bloqueios adicionais.
- A integração automática com o módulo Financeiro (geração de conta a receber ao registrar sessão) é tratada na spec [Registrar Sessão de Aula](../008-registrar-sessao-aula/spec.md), não nesta.
