# Feature Specification: Reativar Registros Desativados (Turma, Aluno, Matéria)

**Feature Branch**: `026-reativar-registros-desativados`

**Created**: 2026-09-17

**Status**: Draft

**Input**: User description: "Adicionar capacidade de REATIVAR registros desativados (voltar o campo Ativo para true) para as três entidades que hoje só permitem exclusão lógica de mão única: Turma, Aluno e Matéria. Requisitos: 1. Backend: adicionar um endpoint de reativação para cada entidade (ex: PATCH /api/turmas/{id}/reativar, PATCH /api/alunos/{id}/reativar, PATCH /api/materias/{id}/reativar), que seta Ativo=true. Reaproveitar lógica comum entre os três onde fizer sentido, já que seguem o mesmo padrão. 2. Frontend: nas telas de listagem e/ou detalhe de Turma, Aluno e Matéria, quando um registro estiver Inativo, mostrar um botão \"Reativar\" (hoje só existe um indicador somente-leitura de status). Ao clicar, reativa e atualiza a tela. 3. Reativar um registro não deve alterar nenhum outro dado histórico associado (aulas passadas, vínculos, etc.) — só o campo Ativo volta a true, preservando tudo o resto exatamente como estava. 4. Não precisa de confirmação especial (diferente da exclusão, que já tem seu próprio fluxo) — reativar é uma ação de baixo risco, sem efeito colateral irreversível."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Reativar uma Turma desativada (Priority: P1)

Como professora, ao visualizar uma Turma que desativei por engano (ou que voltou a ser
relevante), eu quero reativá-la diretamente pela tela, sem precisar recriar a turma do zero ou
pedir suporte técnico, para que o histórico de aulas, alunos vinculados e demais dados
continuem exatamente como estavam antes da desativação.

**Why this priority**: É o caso que motivou a investigação — hoje não existe nenhum caminho de
volta para uma Turma desativada, nem por API nem por interface (confirmado por investigação no
código). Sem esta história, uma desativação por engano é irreversível para o usuário, obrigando
recriar a turma (perdendo a ligação com o histórico antigo).

**Independent Test**: Desativar uma Turma existente, localizá-la na tela de detalhe enquanto
Inativa, acionar "Reativar" e confirmar que ela volta a aparecer/operar como Ativa, com todos os
dados (nome, alunos vinculados, aulas passadas) inalterados.

**Acceptance Scenarios**:

1. **Given** uma Turma está Inativa, **When** a professora abre a tela de detalhe dessa Turma,
   **Then** um botão "Reativar" aparece junto ao indicador de status.
2. **Given** a professora clica em "Reativar" numa Turma Inativa, **When** a ação é concluída,
   **Then** a Turma passa a aparecer como Ativa na tela, sem necessidade de recarregar a página
   manualmente.
3. **Given** uma Turma foi reativada, **When** a professora consulta o histórico de aulas e
   alunos vinculados a essa Turma, **Then** todos os dados são idênticos aos que existiam antes
   da desativação — nenhuma aula, vínculo ou outro campo foi alterado.
4. **Given** uma Turma já está Ativa, **When** a professora visualiza a tela de detalhe,
   **Then** nenhum botão "Reativar" é exibido (só o indicador de status, como hoje).

---

### User Story 2 - Reativar um Aluno desativado (Priority: P1)

Como professora, ao visualizar um Aluno que desativei por engano (ou que voltou a estudar
comigo), eu quero reativá-lo diretamente pela tela, para que o histórico de aulas, pagamentos e
vínculos com turmas continue preservado exatamente como estava.

**Why this priority**: Mesmo problema da User Story 1, aplicado a Aluno — hoje também não há
caminho de volta (confirmado: `AtualizarAlunoRequest` não tem campo `Ativo`, e não existe ação
de reativação no backend nem botão no frontend).

**Independent Test**: Desativar um Aluno existente, localizá-lo na tela de detalhe enquanto
Inativo, acionar "Reativar" e confirmar que ele volta a aparecer como Ativo, com histórico de
aulas e pagamentos inalterado.

**Acceptance Scenarios**:

1. **Given** um Aluno está Inativo, **When** a professora abre a tela de detalhe desse Aluno,
   **Then** um botão "Reativar" aparece junto ao indicador de status.
2. **Given** a professora clica em "Reativar" num Aluno Inativo, **When** a ação é concluída,
   **Then** o Aluno passa a aparecer como Ativo na tela, sem necessidade de recarregar a página
   manualmente.
3. **Given** um Aluno foi reativado, **When** a professora consulta seu histórico de aulas e
   pagamentos, **Then** todos os dados são idênticos aos que existiam antes da desativação.

---

### User Story 3 - Reativar uma Matéria desativada (Priority: P1)

Como professora, ao visualizar uma Matéria que desativei por engano (ou que voltou a ser
lecionada), eu quero reativá-la diretamente pela tela, para que turmas e aulas associadas
continuem com o vínculo histórico preservado.

**Why this priority**: Mesmo problema das User Stories 1 e 2, aplicado a Matéria (confirmado:
`MateriaRequest` também não tem campo `Ativo`, sem caminho de reativação hoje).

**Independent Test**: Desativar uma Matéria existente, localizá-la na tela de detalhe enquanto
Inativa, acionar "Reativar" e confirmar que ela volta a aparecer como Ativa, com vínculos
existentes (turmas, aulas) inalterados.

**Acceptance Scenarios**:

1. **Given** uma Matéria está Inativa, **When** a professora abre a tela de detalhe dessa
   Matéria, **Then** um botão "Reativar" aparece junto ao indicador de status.
2. **Given** a professora clica em "Reativar" numa Matéria Inativa, **When** a ação é concluída,
   **Then** a Matéria passa a aparecer como Ativa na tela, sem necessidade de recarregar a
   página manualmente.

---

### Edge Cases

- Reativar um registro que, entre o carregamento da tela e o clique no botão, já foi reativado
  por outra ação/aba (condição de corrida): a ação MUST ser segura de repetir (reativar um
  registro já ativo não gera erro nem efeito colateral, apenas confirma o estado Ativo).
- Colisão de nome/CPF com outro registro já Ativo ao reativar: não se aplica — as regras de
  unicidade já existentes no sistema (nome de Matéria, CPF de Aluno) são verificadas contra
  todos os registros, ativos ou não, no momento do cadastro/edição. Por isso nunca existem dois
  registros (ativo ou inativo) com o mesmo nome/CPF, e reativar um registro nunca pode colidir
  com outro já Ativo.
- Reativar um registro não deve reativar automaticamente nenhuma outra entidade relacionada que
  também esteja inativa (ex.: reativar uma Turma não reativa uma Matéria inativa vinculada a
  ela) — cada reativação afeta somente o registro explicitamente reativado.
- Um usuário sem permissão para desativar registros também MUST NOT conseguir reativá-los.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O sistema MUST permitir reativar (voltar `Ativo` para `true`) um registro
  desativado de Turma, Aluno ou Matéria.
- **FR-002**: Reativar um registro MUST NOT alterar nenhum outro dado do próprio registro ou de
  registros associados (histórico de aulas, pagamentos, vínculos de turma/matéria/aluno) — só o
  campo `Ativo` muda.
- **FR-003**: O sistema MUST exibir uma ação "Reativar" na tela de detalhe de Turma, Aluno e
  Matéria, visível somente quando o registro correspondente estiver Inativo.
- **FR-004**: Ao concluir a reativação, a tela MUST refletir o novo status (Ativo) imediatamente,
  sem exigir que a usuária recarregue a página manualmente.
- **FR-005**: A ação de reativar MUST NOT exigir uma etapa de confirmação adicional (diferente
  do fluxo de exclusão/desativação, que já tem confirmação própria).
- **FR-006**: Qualquer usuária com permissão para desativar um registro de Turma, Aluno ou
  Matéria MUST também ter permissão para reativá-lo — reativação segue a mesma regra de
  permissão da desativação, sem criar um nível de acesso novo.
- **FR-007**: Reativar um registro que já está Ativo (ex.: condição de corrida entre abas) MUST
  ser uma operação segura de repetir, sem gerar erro para a usuária.
- **FR-008**: A reativação MUST NOT reaplicar nenhuma validação de unicidade (nome de Matéria,
  CPF de Aluno) além da que já existe hoje no cadastro/edição — essas regras já verificam todos
  os registros (ativos ou não), então nunca há colisão possível ao reativar (ver Edge Cases).

### Key Entities

- **Turma**: entidade já existente; ganha a capacidade de ter seu campo `Ativo` revertido para
  `true` por ação direta da usuária, além de já poder ser revertido para `false` (desativação
  existente).
- **Aluno**: idem, para o campo `Ativo` já existente na entidade.
- **Matéria**: idem, para o campo `Ativo` já existente na entidade.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% dos registros desativados de Turma, Aluno e Matéria podem ser reativados
  diretamente pela interface, sem necessidade de intervenção técnica ou acesso direto ao banco
  de dados.
- **SC-002**: Uma usuária consegue reativar um registro em no máximo 2 ações (localizar o
  registro Inativo + clicar em "Reativar"), sem etapa de confirmação adicional.
- **SC-003**: Após a reativação, 100% dos dados históricos associados ao registro (aulas,
  pagamentos, vínculos) permanecem idênticos aos valores de antes da desativação, verificado por
  comparação antes/depois.
- **SC-004**: 100% das usuárias que já podiam desativar um registro conseguem também reativá-lo,
  sem necessidade de solicitar acesso adicional.

## Assumptions

- A permissão para reativar um registro é a mesma já usada para desativá-lo (papel `Professor`,
  conforme o `[Authorize]` já aplicado hoje à exclusão lógica existente) — não é criado nenhum
  nível de acesso novo.
- A ação "Reativar" aparece na tela de detalhe de Turma, Aluno e Matéria, mesmo lugar onde hoje
  já existe o indicador de status (somente-leitura) e o botão "Excluir" — as listagens não
  ganham nenhuma ação nova nesta feature.
- Reativar um registro não dispara nenhuma notificação, e-mail ou efeito colateral além da
  própria mudança de status — mesmo espírito de "ação de baixo risco" descrito no pedido
  original.
- Não é necessário criar nenhum histórico/auditoria novo especificamente para o evento de
  reativação além do que já existe (se houver) para a desativação.
