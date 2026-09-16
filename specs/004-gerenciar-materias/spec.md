# Feature Specification: Gerenciar Matérias

**Feature Branch**: `004-gerenciar-materias`

**Created**: 2026-09-11

**Status**: Implemented (documentação retroativa)

**Input**: Registro retroativo do comportamento real da Estória 3 (Gerenciar Matéria) da ERS "PROFESSORAS INDEPENDENTES_v1.4", com base no código-fonte atual, não na ERS original.

## Nota histórica

Neste módulo, a implementação real está bastante alinhada à ERS original — a principal diferença é de detalhe: o campo "Nível" é texto livre sem lista fixa de valores, e a exclusão é lógica (não física), consistente com o padrão adotado nos demais módulos do sistema.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Cadastrar matéria (Priority: P1)

Como professora, eu cadastro uma nova matéria informando nome, nível de ensino (texto livre) e descrição opcional, para organizar meu conteúdo pedagógico sem me limitar a uma lista fixa.

**Why this priority**: É pré-requisito para cadastrar aulas, já que toda aula exige uma matéria.

**Independent Test**: Chamar `POST /api/materias` com um nome não utilizado por nenhuma outra matéria.

**Acceptance Scenarios**:

1. **Given** um nome não utilizado, **When** a professora cadastra a matéria com nível e descrição livres, **Then** o cadastro é salvo com sucesso.
2. **Given** um nome já usado por outra matéria, **When** a professora tenta cadastrar, **Then** o sistema rejeita por duplicidade de nome.
3. **Given** o campo Nível, **When** a professora digita qualquer texto (ex.: "Fundamental II", "Inglês Conversação"), **Then** o sistema aceita livremente, sem restringir a uma lista pré-definida de valores.

---

### User Story 2 - Listar, buscar e editar matérias (Priority: P1)

Como professora, eu vejo a lista de matérias cadastradas, busco por nome ou nível, e edito nome, nível ou descrição de uma matéria existente.

**Why this priority**: Mantém o catálogo de conteúdos organizado e atualizado ao longo do tempo.

**Independent Test**: Chamar `GET /api/materias` com filtro de busca e depois `PUT /api/materias/{id}`.

**Acceptance Scenarios**:

1. **Given** a lista de matérias, **When** a professora busca por nome ou nível, **Then** o sistema retorna as correspondências.
2. **Given** uma matéria selecionada, **When** a professora edita nome, nível ou descrição e salva, **Then** as alterações são persistidas; campos não informados na requisição preservam o valor anterior.
3. **Given** um novo nome que colide com outra matéria já existente, **When** a professora tenta salvar a edição, **Then** o sistema rejeita por duplicidade.

---

### User Story 3 - Inativar matéria (Priority: P2)

Como professora, ao excluir uma matéria, o sistema a marca como inativa, sem apagar o histórico de aulas já vinculadas a ela.

**Why this priority**: Preserva a integridade de relatórios e do histórico de aulas.

**Independent Test**: Chamar `DELETE /api/materias/{id}` e confirmar que aulas antigas vinculadas a essa matéria continuam acessíveis.

**Acceptance Scenarios**:

1. **Given** uma matéria ativa sem aulas futuras, **When** a professora a exclui, **Then** o sistema marca a matéria como Inativa, sem remover o registro do banco.
2. **Given** uma matéria inativada, **When** a professora consulta o histórico de aulas antigas dessa matéria, **Then** os registros continuam disponíveis normalmente.

---

### Edge Cases

- O que acontece se a professora tentar cadastrar duas matérias com o mesmo nome (mesmo com nível diferente)? É rejeitado — a unicidade é só pelo nome, independentemente do nível.
- O que acontece ao editar uma matéria e deixar o campo Nível em branco? O valor anterior é preservado (a atualização segue o padrão "campo não informado não apaga o existente").
- Existe alguma validação impedindo excluir uma matéria com aulas futuras agendadas? Não foi identificada tal validação — a exclusão lógica é permitida independentemente de aulas futuras vinculadas.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O sistema MUST exigir Nome obrigatório e único para cada matéria (verificado tanto no cadastro quanto na atualização, ignorando a própria matéria ao editar).
- **FR-002**: O sistema MUST tratar o campo Nível como texto livre, sem restringir a um conjunto fixo de valores predefinidos.
- **FR-003**: O sistema MUST permitir listar e buscar matérias por nome ou nível.
- **FR-004**: O sistema MUST permitir editar nome, nível e descrição de uma matéria, preservando valores não informados na requisição de atualização.
- **FR-005**: O sistema MUST realizar exclusão lógica de matéria (marcar como inativa), nunca remoção física, preservando o histórico de aulas vinculadas.

### Key Entities *(include if feature involves data)*

- **Materia**: Id, Nome (único), Descricao (opcional), Nivel (texto livre, opcional), Ativo.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% das tentativas de cadastrar ou renomear uma matéria para um nome já existente são rejeitadas.
- **SC-002**: Nenhuma matéria é fisicamente removida do sistema por meio da funcionalidade de exclusão.
- **SC-003**: A professora consegue localizar qualquer matéria cadastrada por nome ou nível em uma única busca.

## Assumptions

- A flexibilidade de "Nível" como texto livre é intencional, alinhada ao objetivo da ERS de não limitar o sistema a um único tipo de conteúdo pedagógico.
- Não há necessidade de bloquear exclusão de matéria com aulas futuras, pois a inativação é reversível pela mesma lógica de outros módulos e o histórico é sempre preservado.
