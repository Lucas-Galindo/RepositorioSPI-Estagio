# Feature Specification: Gerenciar Alunos

**Feature Branch**: `003-gerenciar-alunos`

**Created**: 2026-09-11

**Status**: Implemented (documentação retroativa)

**Input**: Registro retroativo do comportamento real da Estória 2 (Gerenciar Alunos) da ERS "PROFESSORAS INDEPENDENTES_v1.4", com base no código-fonte atual, não na ERS original.

## Nota histórica

A ERS original previa que "aluno menor de idade" seria determinado a partir de uma data de nascimento, e que o cadastro herdaria de uma classe `PessoaInfo` compartilhada com Professor. Nenhuma das duas coisas existe: o schema atual não guarda data de nascimento (limitação documentada no próprio código) e cada entidade tem campos próprios (ver [PessoaInfo](../019-modelo-pessoainfo/spec.md)). Este documento descreve o comportamento real implementado.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Cadastrar aluno (Priority: P1)

Como professora, eu cadastro um novo aluno informando nome, contatos, matéria principal, turma opcional e valor da aula, para começar a atendê-lo.

**Why this priority**: É o ponto de entrada de todo o resto do sistema (aulas, pagamentos, turmas dependem de um aluno existente).

**Independent Test**: Pode ser testado chamando `POST /api/alunos` com os campos obrigatórios e conferindo que um RA único é gerado automaticamente.

**Acceptance Scenarios**:

1. **Given** nome preenchido e valor de aula ≥ 0, **When** a professora confirma o cadastro, **Then** o sistema gera automaticamente um RA único (via rotina própria do banco de dados) e marca o aluno como Ativo.
2. **Given** um CPF informado que já pertence a outro aluno, **When** a professora tenta salvar, **Then** o sistema rejeita por duplicidade (CPF é opcional, mas único quando informado).
3. **Given** apenas o Email do aluno informado, sem Senha (ou vice-versa), **When** a professora tenta salvar, **Then** o sistema exige que ambos sejam informados juntos, pois login próprio do aluno é opcional mas não pode ficar parcialmente configurado.

---

### User Story 2 - Cadastrar aluno menor de idade (Priority: P1)

Como professora, ao marcar um aluno como menor de idade, sou obrigada a informar telefone e email de um responsável, para garantir um canal de contato válido com o responsável legal.

**Why this priority**: Impacta diretamente a comunicação com o responsável e é uma regra de negócio explícita e sensível.

**Independent Test**: Cadastrar um aluno com o indicador de menor de idade ativado e sem telefone/email do responsável, e confirmar que o sistema rejeita.

**Acceptance Scenarios**:

1. **Given** o indicador "é menor de idade" marcado, **When** a professora tenta salvar sem Telefone do Responsável, **Then** o sistema rejeita.
2. **Given** o indicador "é menor de idade" marcado, **When** a professora tenta salvar sem um Email do Responsável em formato válido, **Then** o sistema rejeita.
3. **Given** o indicador "é menor de idade" **desmarcado**, **When** a professora salva sem dados de responsável, **Then** o cadastro é aceito normalmente.

---

### User Story 3 - Listar, buscar e editar alunos (Priority: P1)

Como professora, eu vejo a lista de todos os meus alunos, busco por nome/RA/matéria, filtro por status e edito os dados de um aluno específico.

**Why this priority**: É o uso do dia a dia para manter o cadastro atualizado.

**Independent Test**: Chamar `GET /api/alunos` com filtros e depois `PUT /api/alunos/{id}` alterando um campo.

**Acceptance Scenarios**:

1. **Given** a lista de alunos, **When** a professora busca por nome, RA ou matéria, **Then** o sistema retorna os alunos correspondentes.
2. **Given** um aluno selecionado, **When** a professora edita campos como telefone, email ou valor da aula e salva, **Then** as alterações são persistidas; campos deixados em branco no formulário não apagam os valores já existentes.
3. **Given** a tela de edição, **When** a professora tenta alterar o RA ou a Turma do aluno por esse mesmo formulário, **Then** o sistema não permite — RA nunca é editável, e Turma é gerenciada por um fluxo separado (vínculo/desvínculo em Turma).

---

### User Story 4 - Inativar aluno (Priority: P2)

Como professora, ao "excluir" um aluno, o sistema o marca como inativo em vez de apagar seus dados, para preservar o histórico de aulas e pagamentos.

**Why this priority**: Protege a integridade do histórico financeiro e pedagógico, essencial para os relatórios.

**Independent Test**: Chamar `DELETE /api/alunos/{id}` e confirmar que o registro continua existindo no banco, apenas com `Ativo = false`.

**Acceptance Scenarios**:

1. **Given** um aluno ativo, **When** a professora seleciona excluir, **Then** o sistema marca o aluno como Inativo, sem remover a linha do banco de dados.
2. **Given** um aluno inativado, **When** a professora consulta o histórico de aulas e pagamentos desse aluno, **Then** os registros anteriores continuam disponíveis.

---

### Edge Cases

- O que acontece se o CPF não for informado? É aceito — CPF é opcional para Aluno (diferente de Professor, onde é obrigatório).
- O que acontece se a professora informar Email do aluno mas não Senha? O cadastro é rejeitado, pois os dois campos de login do aluno são exigidos juntos quando um deles é preenchido.
- O que acontece com o campo Frequência ao cadastrar ou editar um aluno? Não é possível defini-lo diretamente — não existe em nenhum DTO de cadastro/atualização; é somente leitura via API de Alunos, controlado pelo módulo de Aulas.
- Existe alguma verificação de unicidade de Email de aluno? Não — apenas o CPF (quando informado) é verificado como único; o Email do aluno não passa por checagem de duplicidade.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O sistema MUST gerar automaticamente um RA único para cada aluno no momento do cadastro, sem permitir que a professora o defina ou altere manualmente.
- **FR-002**: O sistema MUST validar CPF de aluno como opcional, mas único e com dígito verificador válido quando informado.
- **FR-003**: O sistema MUST exigir Email e Senha do aluno em conjunto (ambos ou nenhum) quando a professora optar por configurar login próprio do aluno.
- **FR-004**: O sistema MUST exigir Telefone e Email do Responsável, com formato de email válido, sempre que o aluno for marcado como menor de idade no cadastro, sem depender de nenhuma validação de idade real (não há data de nascimento no sistema).
- **FR-005**: O sistema MUST permitir listar e buscar alunos por nome, RA ou matéria, e filtrar por status de atividade.
- **FR-006**: O sistema MUST permitir editar os dados de contato, valor de aula e status de menor de idade de um aluno, preservando valores já existentes em campos não informados na requisição de atualização.
- **FR-007**: O sistema MUST impedir a alteração de RA e de Turma pelo fluxo padrão de atualização do aluno (Turma é gerenciada por endpoints dedicados de vínculo/desvínculo em Turma).
- **FR-008**: O sistema MUST realizar exclusão lógica de aluno (marcar como inativo), nunca remoção física do registro, preservando o histórico de aulas e pagamentos vinculados.

### Key Entities *(include if feature involves data)*

- **Aluno**: Id, Ra (gerado automaticamente), Nome, Cpf (opcional), TelefoneAluno, TelefoneResponsavel, Email, Senha (opcional, para login próprio), EmailResponsavel, ValorAula, Frequencia (somente leitura via este módulo), Ativo. Não compartilha uma classe base com Professor.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% dos cadastros de aluno recebem um RA único, gerado automaticamente, sem intervenção manual.
- **SC-002**: 100% das tentativas de cadastrar aluno menor de idade sem telefone e email de responsável válidos são rejeitadas.
- **SC-003**: Nenhum registro de aluno é fisicamente removido do sistema por meio da funcionalidade de exclusão — 100% das exclusões resultam em inativação lógica.
- **SC-004**: A professora consegue localizar qualquer aluno cadastrado (ativo ou inativo) por nome, RA ou matéria em uma única busca.

## Assumptions

- A ausência de data de nascimento é uma limitação de escopo aceita do schema atual; "menor de idade" permanece uma declaração manual e confiável da professora, não uma verificação automática.
- Não há hoje verificação de unicidade de Email de aluno — presume-se que isso não é crítico porque o Email do aluno não é usado como identificador único de negócio (o RA cumpre esse papel).
- O gerenciamento de vínculo aluno-turma é tratado como parte do módulo [Gerenciar Turma](../006-gerenciar-turma/spec.md), não deste.
