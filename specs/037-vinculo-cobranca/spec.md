# Feature Specification: Vínculo de Cobrança do Aluno

**Feature Branch**: `037-vinculo-cobranca`

**Created**: 2026-09-20

**Status**: Draft

**Input**: User description: "Criar a entidade VinculoCobranca, representando a configuração de cobrança de um aluno em um contexto específico (uma turma, ou atendimento individual sem turma). Campos: AlunoId (obrigatório), TurmaId (opcional — nulo significa atendimento individual), Modalidade (Avulsa/Mensalidade/Pacote), Valor (obrigatório, decimal), AulasIncluidas (opcional, só para Mensalidade), SaldoAulas (opcional, só para Pacote). Regras: um Aluno pode ter múltiplos vínculos — no máximo um por Turma, no máximo um com Turma nula; CRUD completo com exclusão lógica (padrão Ativo); seção 'Vínculos de Cobrança' na tela de detalhe do Aluno; NÃO altera a geração automática de cobrança (AulaService.GerarContasAReceberAsync) nem o cálculo de ValorAula do Aluno nesta fatia."

## Nota de investigação prévia

Confirmado por leitura do código atual (grounding antes de especificar):

- **Padrão "Turma (opcional)"**: já existe exatamente esse padrão em `Aula.TurmaId` (nullable) e no formulário de Aula (`AulaForm.tsx`) — um único `<select>` cujo valor vazio significa "Atendimento individual" (texto literal já usado no sistema, inclusive na tela de detalhe do Aluno hoje, para o caso de nenhuma turma vinculada). `VinculoCobranca.TurmaId` seguirá esse mesmo padrão.
- **Padrão de exclusão lógica**: confirmado em `Turma`, `Materia`, `Aula`, `Aluno`, `Lembrete` — todos com campo `Ativo` (bool). O verbo já estabelecido para o serviço é `ExcluirAsync` (define `Ativo = false`), com `ReativarAsync` como contrapartida (define `Ativo = true`) — não existe `DesativarAsync` em lugar nenhum do código; "Excluir" já é o verbo usado tanto no backend quanto no texto de botão do frontend (`excluirAluno`/`reativarAluno`, botões "Excluir"/"Reativar"). `VinculoCobranca` seguirá esse mesmo verbo/padrão.
- **`AulaService.GerarContasAReceberAsync`**: confirmado (`AulaService.cs`, chamado por `RegistrarSessaoAsync`) que hoje gera cada conta a receber usando `Aluno.ValorAula` diretamente, sem nenhuma relação com Turma ou modalidade — confirma que esta feature (cadastro puro de `VinculoCobranca`) não tem nenhum ponto de contato real com esse método hoje, consistente com a Regra 4 do pedido.
- **Tela de detalhe do Aluno**: já existe um painel "Turmas" (lista as turmas do aluno) e um painel "Contas a receber recentes" (lista pagamentos com status), ambos no padrão `mini-panel` com lista de itens ou `EmptyState` quando vazio. Não existe hoje, em nenhuma tela do sistema, um padrão de "lista com botão de adicionar item inline" — toda criação hoje navega para uma página dedicada (ex.: `/turmas/novo`, `/aulas/nova`). Isso foi decidido em Clarifications: esta feature introduz o padrão de adicionar/editar dentro da própria seção, sem navegar para outra página.
- **Restrição de unicidade "no máximo um por combinação, no máximo um com valor nulo"**: não existe hoje, em nenhuma outra entidade do sistema, uma regra equivalente (as únicas unicidades hoje são de coluna única ou de chave composta 100% obrigatória, como `AlunoTurma`). Esta é uma regra de negócio genuinamente nova para o sistema — tratada nesta spec como requisito funcional, com o mecanismo exato de garantia (banco vs. aplicação) deixado para `/speckit-plan`.

## Clarifications

### Session 2026-09-20

- Q: Como a professora adiciona e edita um Vínculo de Cobrança na tela de detalhe do Aluno — modal/formulário inline na própria seção, ou página dedicada? → A: Modal ou formulário inline na própria seção "Vínculos de Cobrança", sem sair da tela do aluno (nenhuma página/rota nova).
- Q: Ao cadastrar um Vínculo de Cobrança, a turma escolhida precisa ser uma da qual o aluno já participa, ou pode ser qualquer turma ativa? → A: Só turmas ativas das quais o aluno já participa; o sistema rejeita qualquer outra.
- Q: Se a professora tentar reativar um vínculo excluído e já existir outro vínculo ativo para a mesma combinação, o que o sistema faz? → A: Rejeita a reativação com mensagem clara; a professora precisa excluir/editar o vínculo ativo existente primeiro.
- Q: Como a professora encontra e reativa um vínculo excluído na seção "Vínculos de Cobrança"? → A: Um controle "Mostrar excluídos" na seção revela os vínculos inativos, cada um com botão "Reativar"; por padrão só os ativos aparecem.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Cadastrar a configuração de cobrança de um aluno em uma turma ou atendimento individual (Priority: P1)

Como professora, eu quero registrar, para um aluno específico, como ele é cobrado em cada contexto em que participa (uma turma específica, ou atendimento individual sem turma) — se é por aula avulsa, mensalidade com um número de aulas incluídas, ou pacote com um saldo de aulas — para ter esse registro centralizado e pronto para uso em fases futuras do sistema (cobrança automática), mesmo que hoje ele ainda não afete nada automaticamente.

**Why this priority**: É o núcleo do pedido — sem o cadastro em si, nenhuma das demais capacidades (edição, listagem, exclusão) tem propósito.

**Independent Test**: Cadastrar um Vínculo de Cobrança para um aluno em uma turma específica (Modalidade Mensalidade, com Aulas Incluídas), e confirmar que ele aparece na seção "Vínculos de Cobrança" da tela desse aluno com os dados corretos.

**Acceptance Scenarios**:

1. **Given** um aluno sem nenhum vínculo de cobrança, **When** a professora cadastra um vínculo escolhendo uma turma, Modalidade "Mensalidade", um Valor e uma quantidade de Aulas Incluídas, **Then** o vínculo é criado e aparece na seção "Vínculos de Cobrança" do aluno com esses dados.
2. **Given** um aluno, **When** a professora cadastra um vínculo escolhendo "Atendimento individual" (sem turma) em vez de uma turma, **Then** o vínculo é criado com `TurmaId` nulo, representando a cobrança do aluno fora de qualquer turma.
3. **Given** um aluno, **When** a professora cadastra um vínculo com Modalidade "Pacote" e um Saldo de Aulas, **Then** o vínculo é criado com esse saldo, e o campo Aulas Incluídas (exclusivo de Mensalidade) permanece vazio.
4. **Given** um aluno, **When** a professora cadastra um vínculo com Modalidade "Avulsa", **Then** nem Aulas Incluídas nem Saldo de Aulas são exigidos ou exibidos como aplicáveis a esse vínculo.
5. **Given** dois alunos, cada um com seus próprios vínculos, **When** a professora abre a tela de detalhe de um deles, **Then** a seção "Vínculos de Cobrança" lista apenas os vínculos daquele aluno, mostrando Turma (ou "Atendimento individual"), Modalidade, Valor e o campo específico da modalidade.
6. **Given** um aluno sem nenhum vínculo de cobrança, **When** a professora abre a tela de detalhe desse aluno, **Then** a seção "Vínculos de Cobrança" exibe um estado vazio e a opção de adicionar um novo vínculo.
7. **Given** um aluno com um vínculo de cobrança cadastrado (qualquer modalidade/valor) e `Aluno.ValorAula` definido, **When** uma sessão de aula é registrada e as contas a receber são geradas, **Then** o valor de cada conta continua sendo calculado exatamente como antes desta feature (a partir de `Aluno.ValorAula`), sem nenhuma influência do vínculo — e nenhum Saldo de Aulas é decrementado.

---

### User Story 2 - Editar e desativar um Vínculo de Cobrança existente (Priority: P2)

Como professora, eu quero corrigir ou atualizar um Vínculo de Cobrança já cadastrado (ex.: mudar o Valor, trocar a Modalidade, ajustar o Saldo de Aulas de um pacote), e desativá-lo quando o aluno deixa de ser cobrado daquela forma naquele contexto, sem perder o histórico do vínculo.

**Why this priority**: Complementa o cadastro (US1) com o ciclo de vida completo — sem isso, um erro de cadastro seria permanente ou exigiria intervenção manual no banco.

**Independent Test**: Editar o Valor de um vínculo existente e confirmar que a mudança é refletida na listagem; em seguida, excluí-lo (lógica) e confirmar que ele deixa de aparecer na listagem ativa, mas pode ser reativado.

**Acceptance Scenarios**:

1. **Given** um vínculo de cobrança existente, **When** a professora edita seu Valor, **Then** o novo valor é salvo e refletido imediatamente na seção "Vínculos de Cobrança".
2. **Given** um vínculo de cobrança existente com Modalidade "Mensalidade", **When** a professora troca a Modalidade para "Pacote" durante a edição, **Then** o campo Aulas Incluídas deixa de ser aplicável e o campo Saldo de Aulas passa a ser aplicável/exibido (continua opcional, como na definição do campo).
3. **Given** um vínculo de cobrança ativo, **When** a professora o exclui, **Then** ele passa a `Ativo = false` (exclusão lógica, nunca removido do banco) e some da listagem ativa padrão da seção "Vínculos de Cobrança", mas pode ser reativado posteriormente.
4. **Given** um vínculo de cobrança desativado para uma combinação Aluno+Turma, **When** a professora cadastra um NOVO vínculo ativo para essa mesma combinação, **Then** o cadastro é permitido — a restrição de "no máximo um por combinação" só considera vínculos ativos.
5. **Given** um vínculo desativado e um vínculo ativo para a mesma combinação Aluno+Turma (criado depois), **When** a professora tenta reativar o vínculo desativado, **Then** a reativação é rejeitada com mensagem clara e o vínculo ativo permanece inalterado.
6. **Given** um aluno com vínculos ativos e excluídos, **When** a professora abre a seção "Vínculos de Cobrança", **Then** só os ativos aparecem; ao acionar "Mostrar excluídos", os inativos também aparecem, cada um com o botão "Reativar".
7. **Given** um vínculo desativado sem nenhum outro ativo para a mesma combinação, **When** a professora o reativa, **Then** ele volta a `Ativo = true` e reaparece na listagem ativa.

---

### Edge Cases

- O que acontece se a professora tentar cadastrar um segundo vínculo ativo para o mesmo Aluno na mesma Turma? O sistema rejeita, informando que já existe um vínculo ativo para essa combinação.
- O que acontece se a professora tentar cadastrar um segundo vínculo ativo com "Atendimento individual" (Turma nula) para o mesmo aluno? O sistema rejeita, pelo mesmo motivo — no máximo um vínculo ativo com Turma nula por aluno.
- O que acontece se a professora tentar reativar um vínculo excluído enquanto já existe outro ativo para a mesma combinação? O sistema rejeita com mensagem clara e não altera o vínculo ativo (FR-014).
- O que acontece se a professora tentar vincular a cobrança a uma turma da qual o aluno não participa, ou a uma turma inativa? O sistema rejeita (FR-013); a turma nem aparece como opção no formulário.
- O que acontece se a professora informar Aulas Incluídas com Modalidade diferente de "Mensalidade", ou Saldo de Aulas com Modalidade diferente de "Pacote"? O sistema rejeita o cadastro/edição, já que esses campos só se aplicam à modalidade correspondente.
- O que acontece se a Turma escolhida for posteriormente desativada (excluída logicamente)? Fora do escopo desta feature — o vínculo permanece como está; o comportamento de vínculos órfãos de uma turma desativada fica para uma fatia futura.
- Esta feature altera a geração automática de cobrança (`AulaService.GerarContasAReceberAsync`) ou o campo `Aluno.ValorAula`? Não — confirmado na investigação prévia que nenhum dos dois é tocado; `VinculoCobranca` existe apenas como cadastro nesta fatia.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O sistema MUST permitir cadastrar um Vínculo de Cobrança para um aluno, sempre associado a um Aluno (obrigatório) e a uma Turma específica OU a nenhuma Turma ("Atendimento individual").
- **FR-002**: O sistema MUST exigir, em todo Vínculo de Cobrança, uma Modalidade dentre exatamente três opções: Avulsa, Mensalidade, ou Pacote, e um Valor (numérico, maior que zero).
- **FR-003**: O sistema MUST permitir informar uma quantidade de Aulas Incluídas apenas quando a Modalidade for Mensalidade, rejeitando esse campo para as demais modalidades.
- **FR-004**: O sistema MUST permitir informar um Saldo de Aulas apenas quando a Modalidade for Pacote, rejeitando esse campo para as demais modalidades.
- **FR-005**: O sistema MUST impedir a existência de mais de um Vínculo de Cobrança ATIVO para a mesma combinação de Aluno e Turma específica.
- **FR-006**: O sistema MUST impedir a existência de mais de um Vínculo de Cobrança ATIVO com Turma nula ("Atendimento individual") para o mesmo Aluno.
- **FR-007**: Vínculos de Cobrança desativados (exclusão lógica) MUST NOT contar para as restrições de FR-005/FR-006 — uma nova combinação idêntica pode ser cadastrada normalmente depois que a anterior é desativada.
- **FR-008**: O sistema MUST permitir editar todos os campos de um Vínculo de Cobrança existente (Turma, Modalidade, Valor, Aulas Incluídas, Saldo de Aulas), reaplicando as mesmas validações de FR-002 a FR-006 no momento da edição.
- **FR-009**: O sistema MUST permitir listar todos os Vínculos de Cobrança de um aluno específico, exibindo ao menos: Turma (ou "Atendimento individual"), Modalidade, Valor, e o campo específico da modalidade (Aulas Incluídas ou Saldo de Aulas, quando aplicável).
- **FR-010**: O sistema MUST permitir excluir (exclusão lógica, campo Ativo) um Vínculo de Cobrança, preservando o registro e permitindo reativação posterior.
- **FR-011**: A seção "Vínculos de Cobrança" MUST estar disponível na tela de detalhe do Aluno, listando os vínculos existentes desse aluno, e MUST permitir adicionar, editar e excluir vínculos por modal ou formulário dentro da própria seção, sem navegar para outra página.
- **FR-012**: Esta feature MUST NOT alterar o comportamento de `AulaService.GerarContasAReceberAsync` nem o uso de `Aluno.ValorAula` para gerar cobranças — `VinculoCobranca` não tem nenhum efeito automático nesta fatia.
- **FR-013**: Ao associar um Vínculo de Cobrança a uma Turma específica (no cadastro ou na edição), o sistema MUST aceitar apenas turmas ativas das quais o aluno já participa, rejeitando qualquer outra; o formulário MUST oferecer apenas essas turmas (mais a opção "Atendimento individual"). Na edição, essa regra só é reaplicada quando a Turma do vínculo é alterada (uma Turma inalterada não é revalidada, para que editar Valor ou Modalidade de um vínculo cuja turma foi desativada depois continue possível). O vínculo MUST NOT criar nem alterar a participação do aluno em nenhuma turma.
- **FR-014**: O sistema MUST permitir reativar um Vínculo de Cobrança excluído. Se já existir outro vínculo ativo para a mesma combinação (mesmo Aluno e mesma Turma, ou mesmo Aluno com Turma nula), a reativação MUST ser rejeitada com uma mensagem clara, sem desativar nem alterar automaticamente o vínculo ativo existente; a professora deve excluir ou editar o vínculo ativo antes de reativar o outro.
- **FR-015**: A seção "Vínculos de Cobrança" MUST exibir por padrão apenas os vínculos ativos e MUST oferecer um controle "Mostrar excluídos" que revela também os vínculos inativos, cada um com um botão "Reativar" (comportamento de FR-014).

### Key Entities

- **Vínculo de Cobrança (VinculoCobranca)**: representa como um aluno é cobrado em um contexto específico. Atributos: Aluno (obrigatório), Turma (opcional — ausente significa atendimento individual), Modalidade (Avulsa | Mensalidade | Pacote), Valor (obrigatório), Aulas Incluídas (só aplicável a Mensalidade), Saldo de Aulas (só aplicável a Pacote), Ativo (exclusão lógica). Relaciona-se com Aluno (muitos vínculos por aluno) e opcionalmente com Turma (um vínculo por turma, no máximo, por aluno).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Uma professora consegue cadastrar um Vínculo de Cobrança completo (turma ou atendimento individual, modalidade, valor, campo específico) em menos de 1 minuto, sem sair da tela de detalhe do aluno mais do que o estritamente necessário.
- **SC-002**: 100% das tentativas de cadastrar um segundo vínculo ativo para a mesma combinação Aluno+Turma (ou Aluno+atendimento individual) são rejeitadas com uma mensagem clara, nunca resultando em dois vínculos ativos conflitantes.
- **SC-003**: 100% dos vínculos excluídos permanecem no sistema (nunca removidos fisicamente) e podem ser reativados a qualquer momento.
- **SC-004**: A seção "Vínculos de Cobrança" reflete, sem necessidade de recarregar a página manualmente fora do fluxo normal, qualquer cadastro/edição/exclusão feita nela.

## Assumptions

- O Valor de um Vínculo de Cobrança MUST ser maior que zero (nenhum vínculo gratuito nesta fatia) — segue o padrão de validação numérica já usado em outros valores monetários do sistema.
- Editar a Turma de um vínculo existente é permitido (não é um campo imutável após criação), desde que a nova combinação Aluno+Turma não viole FR-005/FR-006 — mesma lógica de revalidação já aplicada a outros campos editáveis no sistema.
- O Saldo de Aulas de um vínculo Pacote é um número informado e editável manualmente nesta fatia — nenhum mecanismo automático o decrementa ainda (Regra 4 do pedido original: "sem nenhum efeito automático").
- Esta feature não introduz nenhuma página/rota nova além da seção "Vínculos de Cobrança" (com seu modal ou formulário inline) na tela de detalhe do Aluno já existente.
- Não há limite máximo de Vínculos de Cobrança ativos por aluno além das restrições de FR-005/FR-006 (um por turma, um sem turma) — um aluno em 3 turmas pode ter até 4 vínculos ativos (3 por turma + 1 individual).
