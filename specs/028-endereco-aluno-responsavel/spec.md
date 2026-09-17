# Feature Specification: Endereço do Aluno e do Responsável

**Feature Branch**: `028-endereco-aluno-responsavel`

**Created**: 2026-09-17

**Status**: Draft

**Input**: User description: "Adicionar cadastro de endereço ao Aluno, incluindo o endereço do responsável (quando o aluno for menor de idade, flag já existente). Requisitos: 1. Campos de endereço: CEP, Rua, Número, Complemento (opcional), Bairro, Cidade, Estado. Obrigatórios: CEP, Rua e Número. Complemento, Bairro, Cidade e Estado são opcionais. 2. Esses campos se aplicam ao endereço do próprio Aluno. 3. Quando o Aluno é marcado como menor de idade (flag EhMenorDeIdade já existente, spec 002), exibir uma seção adicional para o endereço do responsável, com um checkbox \"Mesmo endereço do aluno\" — vem marcado por padrão. Quando marcado, o endereço do responsável é o mesmo do aluno. Quando desmarcado, exibe os mesmos 7 campos de endereço para o responsável, independentes do endereço do aluno. 4. Não é necessário capturar nome ou telefone do responsável nesta feature — só o endereço, condicionado ao checkbox. 5. Exibir os campos de endereço no formulário de cadastro/edição de Aluno e no detalhe do aluno. 6. Ao digitar um CEP válido (8 dígitos), buscar automaticamente Rua, Bairro, Cidade e Estado via API pública ViaCEP (https://viacep.com.br/ws/{cep}/json/), preenchendo esses campos automaticamente — o usuário pode editar os valores preenchidos se precisar corrigir algo. Se a API falhar (sem internet, CEP inválido, serviço fora do ar), os campos continuam editáveis manualmente, sem bloquear o cadastro. Aplicar essa busca automática tanto no endereço do Aluno quanto no do responsável (quando aplicável)."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Registrar e consultar o endereço do Aluno (Priority: P1)

Como professora, ao cadastrar ou editar um Aluno, eu quero informar o endereço completo dele
(CEP, rua, número e demais dados complementares), e depois consultá-lo na tela de detalhe, para
que eu tenha essa informação de contato disponível sempre que precisar (correspondência, visitas,
documentação).

**Why this priority**: É o núcleo do pedido — sem isso, nenhuma das demais histórias (endereço
do responsável, preenchimento automático) faz sentido, já que dependem da existência dos mesmos
campos de endereço.

**Independent Test**: Cadastrar um Aluno preenchendo CEP, Rua e Número (os obrigatórios), salvar,
e confirmar que o endereço aparece corretamente na tela de detalhe do Aluno.

**Acceptance Scenarios**:

1. **Given** a professora está cadastrando ou editando um Aluno, **When** ela preenche CEP, Rua
   e Número e salva, **Then** o sistema aceita o cadastro e associa o endereço a esse Aluno.
2. **Given** a professora tenta salvar um Aluno sem CEP, sem Rua ou sem Número, **When** ela
   confirma o cadastro, **Then** o sistema rejeita e indica quais campos obrigatórios de endereço
   estão faltando.
3. **Given** a professora não preenche Complemento, Bairro, Cidade ou Estado, **When** ela salva
   o cadastro, **Then** o sistema aceita normalmente, sem exigir esses campos.
4. **Given** um Aluno já tem endereço cadastrado, **When** a professora abre a tela de detalhe
   desse Aluno, **Then** o endereço completo (todos os campos preenchidos) é exibido.

---

### User Story 2 - Registrar o endereço do responsável quando o Aluno é menor de idade (Priority: P2)

Como professora, ao cadastrar ou editar um Aluno marcado como menor de idade, eu quero informar
o endereço do responsável — reaproveitando o endereço do próprio Aluno quando forem o mesmo, ou
informando um endereço diferente quando não forem —, para que eu tenha o endereço correto do
responsável legal disponível quando o aluno não morar sozinho ou os endereços divergirem.

**Why this priority**: É um requisito explícito do pedido, mas se aplica só ao subconjunto de
Alunos marcados como menores de idade — por isso vem depois da história central (endereço do
próprio Aluno, que se aplica a 100% dos cadastros).

**Independent Test**: Marcar um Aluno como menor de idade, desmarcar "Mesmo endereço do aluno",
preencher um endereço de responsável diferente do endereço do Aluno, salvar, e confirmar que os
dois endereços (Aluno e responsável) aparecem corretamente e de forma independente na tela de
detalhe.

**Acceptance Scenarios**:

1. **Given** a professora marca um Aluno como menor de idade, **When** o formulário é exibido,
   **Then** uma seção adicional de "Endereço do responsável" aparece, com o checkbox "Mesmo
   endereço do aluno" marcado por padrão.
2. **Given** o checkbox "Mesmo endereço do aluno" está marcado, **When** a professora salva o
   cadastro, **Then** o endereço do responsável exibido/associado é idêntico ao do Aluno, sem
   exigir preenchimento adicional.
3. **Given** a professora desmarca "Mesmo endereço do aluno", **When** o formulário é exibido,
   **Then** os mesmos 7 campos de endereço aparecem para o responsável, vazios e independentes
   dos campos do Aluno.
4. **Given** o checkbox está desmarcado e a professora preenche um endereço de responsável
   diferente, **When** ela salva, **Then** os dois endereços (Aluno e responsável) são
   persistidos de forma independente, cada um respeitando as mesmas regras de obrigatoriedade
   (CEP, Rua e Número).
5. **Given** um Aluno não está marcado como menor de idade, **When** a professora visualiza o
   formulário ou o detalhe desse Aluno, **Then** nenhuma seção de endereço de responsável é
   exibida.
6. **Given** a professora desmarca "é menor de idade" para um Aluno que tinha endereço de
   responsável cadastrado, **When** ela visualiza o formulário novamente, **Then** a seção de
   endereço do responsável fica oculta (o dado permanece armazenado, mas não é exibido nem
   editável enquanto o Aluno não for maior de idade).

---

### User Story 3 - Preencher endereço automaticamente a partir do CEP (Priority: P3)

Como professora, ao digitar um CEP válido no cadastro do Aluno (ou do responsável), eu quero que
Rua, Bairro, Cidade e Estado sejam preenchidos automaticamente, para que eu não precise digitar
manualmente dados que já estão associados a esse CEP.

**Why this priority**: É uma melhoria de conveniência sobre a User Story 1 — o cadastro de
endereço já funciona plenamente por digitação manual (User Story 1); esta história só acelera o
preenchimento, sem ser pré-requisito para o cadastro funcionar.

**Independent Test**: Digitar um CEP válido e existente num formulário de endereço (Aluno ou
responsável) e confirmar que Rua, Bairro, Cidade e Estado são preenchidos automaticamente, e que
esses campos continuam editáveis manualmente depois do preenchimento automático.

**Acceptance Scenarios**:

1. **Given** a professora digita um CEP com 8 dígitos válido, **When** a busca automática é
   concluída com sucesso, **Then** Rua, Bairro, Cidade e Estado são preenchidos automaticamente
   com os dados retornados.
2. **Given** os campos foram preenchidos automaticamente, **When** a professora edita
   manualmente qualquer um deles, **Then** o sistema aceita a edição, sem reverter para o valor
   automático.
3. **Given** a busca automática falha (sem internet, CEP inexistente, serviço indisponível),
   **When** a professora continua preenchendo o formulário manualmente, **Then** o sistema não
   bloqueia nem exibe erro impeditivo — os campos permanecem editáveis e o cadastro pode ser
   concluído normalmente.
4. **Given** a professora digita um CEP incompleto (menos de 8 dígitos), **When** ela continua
   digitando, **Then** nenhuma busca automática é disparada antes de completar os 8 dígitos.
5. **Given** o Aluno é menor de idade e a seção de endereço do responsável está com "Mesmo
   endereço do aluno" desmarcado, **When** a professora digita um CEP no endereço do
   responsável, **Then** a mesma busca automática se aplica a esse endereço, independente da do
   Aluno.

---

### Edge Cases

- Alunos já cadastrados antes desta funcionalidade existir não têm endereço registrado: abrir o
  formulário de edição desses Alunos MUST NOT forçar o preenchimento de endereço antes de
  permitir salvar alterações não relacionadas a endereço (ver Assumptions).
- CEP com formato inválido (não numérico, ou com quantidade errada de dígitos): o sistema MUST
  rejeitar o cadastro com uma mensagem clara, sem tentar disparar a busca automática.
- Resposta da API de CEP indicando explicitamente que o CEP não existe: tratada da mesma forma
  que uma falha de busca (Cenário 3 da User Story 3) — os campos continuam editáveis
  manualmente, sem bloquear o cadastro.
- Responsável com endereço próprio cadastrado (checkbox desmarcado) e a professora depois marca
  "Mesmo endereço do aluno" novamente: os campos de endereço do responsável deixam de ser
  editáveis/exibidos como independentes e passam a refletir o endereço do Aluno; o valor
  previamente digitado para o responsável é limpo nesse momento (não fica retido "escondido" no
  formulário) — se ela desmarcar de novo antes de salvar, os campos aparecem vazios, para
  preencher novamente.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O sistema MUST permitir registrar um endereço para o Aluno com os campos: CEP,
  Rua, Número, Complemento, Bairro, Cidade e Estado.
- **FR-002**: O sistema MUST exigir CEP, Rua e Número para o endereço do Aluno no cadastro de um
  Aluno novo; Complemento, Bairro, Cidade e Estado MUST ser opcionais. Na edição de um Aluno já
  existente, CEP, Rua e Número só se tornam obrigatórios em conjunto se pelo menos um dos três
  for alterado nessa edição — editar outros campos (ex.: valor da aula) MUST NOT exigir
  preenchimento de endereço num Aluno que ainda não o tem (ver Edge Cases e Assumptions).
- **FR-003**: O sistema MUST exibir os campos de endereço do Aluno no formulário de
  cadastro/edição de Aluno e na tela de detalhe do Aluno.
- **FR-004**: Quando o Aluno estiver marcado como menor de idade, o sistema MUST exibir uma
  seção adicional para o endereço do responsável, com os mesmos 7 campos de endereço e as mesmas
  regras de obrigatoriedade do endereço do Aluno (FR-002, incluindo a mesma assimetria
  cadastro/edição).
- **FR-005**: A seção de endereço do responsável MUST incluir um checkbox "Mesmo endereço do
  aluno", marcado por padrão.
- **FR-006**: Quando o checkbox estiver marcado, o sistema MUST considerar o endereço do
  responsável idêntico ao do Aluno, sem exigir preenchimento adicional dos 7 campos para o
  responsável.
- **FR-007**: Quando o checkbox estiver desmarcado, o sistema MUST exibir os 7 campos de
  endereço do responsável vazios (ou com o último valor próprio salvo, se houver) e
  independentes dos campos do Aluno, sujeitos às mesmas regras de obrigatoriedade (FR-002,
  incluindo a mesma assimetria cadastro/edição).
- **FR-008**: O sistema MUST NOT exibir a seção de endereço do responsável quando o Aluno não
  estiver marcado como menor de idade.
- **FR-009**: O sistema MUST NOT exigir nome ou telefone do responsável como parte desta
  funcionalidade — somente o endereço, condicionado ao indicador de menor de idade.
- **FR-010**: Ao digitar um CEP com 8 dígitos em qualquer campo de CEP do formulário (Aluno ou
  responsável), o sistema MUST buscar automaticamente Rua, Bairro, Cidade e Estado numa fonte
  pública de dados de CEP, preenchendo esses campos com o resultado.
- **FR-011**: Após o preenchimento automático, os campos preenchidos MUST continuar editáveis
  manualmente pela professora.
- **FR-012**: Se a busca automática de CEP falhar por qualquer motivo (indisponibilidade,
  ausência de conexão, CEP não encontrado), o sistema MUST permitir que a professora preencha os
  campos manualmente e conclua o cadastro normalmente, sem bloquear nem exibir erro impeditivo.
- **FR-013**: O preenchimento automático de CEP MUST funcionar tanto para o endereço do Aluno
  quanto para o do responsável (quando exibido), de forma independente.

### Key Entities

- **Endereço**: CEP, Rua, Número, Complemento (opcional), Bairro (opcional), Cidade (opcional),
  Estado (opcional). Associado a um Aluno (endereço próprio) ou a um responsável de Aluno menor
  de idade (endereço do responsável) — a mesma estrutura de campos serve para os dois casos.
- **Aluno**: entidade já existente; ganha um endereço próprio (CEP, Rua e Número obrigatórios no
  cadastro; na edição, obrigatórios em conjunto apenas se algum dos três for alterado) e, quando
  `EhMenorDeIdade` é verdadeiro, um indicador de "mesmo endereço do responsável" mais,
  opcionalmente, um endereço de responsável independente (mesma regra de obrigatoriedade).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% dos novos cadastros de Aluno exigem e armazenam CEP, Rua e Número antes de
  serem concluídos.
- **SC-002**: 100% dos Alunos marcados como menores de idade têm, após o cadastro, um endereço
  de responsável definido (seja reaproveitando o do Aluno, seja um próprio).
- **SC-003**: A professora consegue visualizar o endereço completo (Aluno e, se aplicável,
  responsável) na tela de detalhe do Aluno sem navegar para nenhuma outra tela.
- **SC-004**: Em pelo menos 90% das tentativas com CEP válido e existente, os campos de
  Rua/Bairro/Cidade/Estado são preenchidos automaticamente sem digitação manual.
- **SC-005**: 100% das tentativas de cadastro com falha na busca automática de CEP ainda
  permitem concluir o cadastro manualmente, sem erro impeditivo.

## Assumptions

- Alunos já cadastrados antes desta funcionalidade não têm endereço; a obrigatoriedade de
  CEP/Rua/Número (FR-002) se aplica a partir de agora para criação e para qualquer edição que
  inclua alteração dos campos de endereço, mas MUST NOT bloquear edições de outros campos (ex.:
  valor da aula) num Aluno antigo que ainda não tem endereço preenchido — mesmo espírito do
  padrão já usado para atualização parcial de Aluno (spec 003, FR-006: "preservando valores já
  existentes em campos não informados").
- O endereço do responsável, quando "Mesmo endereço do aluno" está marcado, é tratado como um
  espelho do endereço do Aluno (não uma cópia independente que possa divergir silenciosamente) —
  se o endereço do Aluno mudar depois, o endereço do responsável espelhado muda junto, enquanto
  o checkbox permanecer marcado.
- Ao desmarcar o checkbox pela primeira vez, os campos do responsável iniciam vazios (não
  pré-preenchidos com os valores do Aluno) — a professora digita o endereço do responsável do
  zero, ou usa a busca automática por CEP para acelerar.
- Estado é representado pela sigla de 2 letras (UF), consistente com o formato já retornado por
  serviços públicos de CEP.
- A busca automática de CEP consulta um serviço público de terceiros; nenhuma credencial ou
  configuração adicional é necessária, e nenhum dado sensível do sistema é enviado a esse
  serviço além do próprio CEP digitado.
- Não há necessidade de auditoria/histórico específico de mudanças de endereço além do que já
  existe (se houver) para outras edições de Aluno.
