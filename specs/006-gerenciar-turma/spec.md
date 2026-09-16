# Feature Specification: Gerenciar Turma

**Feature Branch**: `006-gerenciar-turma`

**Created**: 2026-09-11

**Status**: Implemented (documentação retroativa)

**Input**: Registro retroativo do comportamento real da Estória 6 (Gerenciar Turma) da ERS "PROFESSORAS INDEPENDENTES_v1.4", com base no código-fonte atual, não na ERS original.

## Nota histórica

A ERS original previa uma tela com filtro por status (ativa/inativa). Na implementação real, a listagem de turmas **não filtra por status por padrão** e a tela não oferece filtro de status na interface — turmas ativas e inativas aparecem juntas, distinguidas apenas por um indicador visual. Este documento descreve esse comportamento real.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Cadastrar turma (Priority: P1)

Como professora, eu cadastro uma nova turma informando apenas o nome, para organizar meus alunos em atendimento coletivo.

**Why this priority**: É pré-requisito para vincular alunos em grupo e para cadastrar aulas de turma.

**Independent Test**: Chamar `POST /api/turmas` com um nome preenchido.

**Acceptance Scenarios**:

1. **Given** um nome informado, **When** a professora cadastra a turma, **Then** ela é criada como Ativa, automaticamente associada à professora autenticada (não há seleção manual de professor).
2. **Given** o cadastro de turma, **When** a professora tenta informar alunos diretamente nesse formulário, **Then** isso não é suportado — o vínculo de alunos é feito em um passo separado, após a turma existir.

---

### User Story 2 - Vincular e desvincular alunos de uma turma (Priority: P1)

Como professora, eu vinculo e desvinculo alunos individuais a uma turma já existente, para manter a composição do grupo atualizada.

**Why this priority**: É o uso recorrente que mantém a turma útil para agendamento de aulas coletivas.

**Independent Test**: Chamar `POST /api/turmas/{id}/alunos/{alunoId}` e depois `DELETE /api/turmas/{id}/alunos/{alunoId}`.

**Acceptance Scenarios**:

1. **Given** um aluno ainda não vinculado à turma, **When** a professora o vincula, **Then** o vínculo é criado com sucesso.
2. **Given** um aluno já vinculado, **When** a professora tenta vincular novamente, **Then** o sistema rejeita por duplicidade.
3. **Given** um aluno não vinculado, **When** a professora tenta desvinculá-lo, **Then** o sistema rejeita, pois não há vínculo para remover.
4. **Given** um aluno inativo, **When** a professora tenta vinculá-lo a uma turma, **Then** o sistema permite — não há validação que impeça vincular alunos inativos.

---

### User Story 3 - Listar, buscar e editar turmas (Priority: P1)

Como professora, eu vejo a lista de todas as turmas cadastradas (ativas e inativas juntas), busco por nome, e edito o nome de uma turma existente.

**Why this priority**: É o uso recorrente para navegar entre turmas.

**Independent Test**: Chamar `GET /api/turmas?nome=...` sem informar o parâmetro `ativo`, e confirmar que turmas inativas aparecem na resposta.

**Acceptance Scenarios**:

1. **Given** turmas ativas e inativas cadastradas, **When** a professora acessa a listagem sem aplicar nenhum filtro de status, **Then** todas aparecem juntas na lista, diferenciadas apenas por um indicador visual de status (Ativo/Inativo).
2. **Given** uma turma selecionada, **When** a professora edita o Nome e salva, **Then** a alteração é persistida (não há outros campos editáveis na turma além do nome).

---

### User Story 4 - Inativar turma (Priority: P2)

Como professora, ao excluir uma turma, o sistema a marca como inativa, preservando o histórico de aulas associadas a ela.

**Why this priority**: Evita perda de histórico pedagógico e financeiro vinculado à turma.

**Independent Test**: Chamar `DELETE /api/turmas/{id}` e confirmar que a turma continua acessível por Id, apenas com `Ativo = false`.

**Acceptance Scenarios**:

1. **Given** uma turma ativa, **When** a professora a exclui, **Then** o sistema marca a turma como Inativa, sem remover o registro nem os vínculos históricos de alunos e aulas.
2. **Given** uma turma inativada com alunos ainda vinculados, **When** a professora a exclui, **Then** o sistema não exige desvincular os alunos antes — a exclusão lógica é permitida independentemente de vínculos existentes.

---

### Edge Cases

- Existe algum filtro de status na interface de Turmas? Não — a única ferramenta de busca disponível é por nome; a distinção ativa/inativa aparece apenas como selo visual em cada linha.
- O que acontece se a professora tentar vincular um aluno inativo a uma turma? É permitido, pois não há validação de status do aluno no vínculo.
- O que acontece com uma aula agendada de uma turma que depois é inativada? A aula permanece normalmente, já que o cadastro de aula também não valida se a turma está ativa (ver [Gerenciar Aula](../005-gerenciar-aula/spec.md)).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O sistema MUST permitir o cadastro de uma turma apenas com Nome, associando automaticamente a professora autenticada como responsável.
- **FR-002**: O sistema MUST permitir vincular e desvincular alunos individuais a uma turma por meio de uma ação dedicada, distinta do cadastro/edição da turma.
- **FR-003**: O sistema MUST rejeitar a tentativa de vincular um aluno já vinculado à mesma turma, e MUST rejeitar a tentativa de desvincular um aluno que não está vinculado.
- **FR-004**: O sistema MUST permitir listar e buscar turmas por nome, retornando por padrão tanto turmas ativas quanto inativas juntas.
- **FR-005**: O sistema MUST permitir editar apenas o Nome de uma turma existente.
- **FR-006**: O sistema MUST realizar exclusão lógica de turma (marcar como inativa), preservando o histórico de aulas e o vínculo de alunos já registrados, independentemente de a turma ter alunos vinculados no momento da exclusão.

### Key Entities *(include if feature involves data)*

- **Turma**: Id, ProfessorId, Nome, Ativo.
- **AlunoTurma**: vínculo N:N entre Aluno e Turma.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% das tentativas de vincular um aluno já vinculado, ou desvincular um aluno não vinculado, são rejeitadas.
- **SC-002**: Nenhuma turma é fisicamente removida do sistema por meio da funcionalidade de exclusão.
- **SC-003**: A professora consegue localizar qualquer turma (ativa ou inativa) por nome em uma única busca, sem precisar de um filtro de status separado.

## Assumptions

- A ausência de filtro de status na tela de Turmas e de validação de "aluno ativo" ao vincular são aceitas como comportamento atual do sistema.
- Turma não possui campos além de Nome (sem descrição, nível ou valor próprio) — o valor da aula é sempre definido a nível de Aluno, não de Turma.
