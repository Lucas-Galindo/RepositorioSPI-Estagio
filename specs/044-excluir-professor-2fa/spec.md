# Feature Specification: Excluir Professor com Confirmação por 2FA via E-mail

**Feature Branch**: `044-excluir-professor-2fa`

**Created**: 2026-10-01

**Status**: Draft

**Input**: User description: "Adicionar um botão 'Excluir Professor', visível apenas para o papel Admin, com confirmação obrigatória por 2FA via e-mail antes de efetivar. Botão visível só para Admin, na tela de gerenciamento/perfil do Professor. Ao clicar, enviar código de verificação para o e-mail do Admin (não da professora sendo excluída) — reaproveitar a infraestrutura de e-mail da spec 043 (Brevo) se possível; investigar se já existe mecanismo de código/OTP a reaproveitar. Admin digita o código para confirmar; código expira em 10-15 minutos, uso único. Exclusão é lógica (Ativo=false, mesmo padrão de Turma/Aluno/Matéria/spec 026), preservando todos os dados vinculados. Professor inativo não deve conseguir logar — investigar se já é verificado. Tela de confirmação deve avisar explicitamente que isso torna o sistema single-tenant inacessível até nova professora ser cadastrada. Seguindo o padrão da spec 026, um Professor excluído deve poder ser reativado. Investigar antes de planejar: como funciona a autenticação do Admin (único ou múltiplos?) e se há e-mail de Admin já cadastrado."

## Nota de investigação prévia

Confirmado por leitura do código e do banco de dados atuais (grounding antes de especificar):

- **Admin não é único**: existem hoje **5 contas Admin ativas** no banco (`admin@spi.local` e 4 contas de teste de sessões anteriores), cada uma com seu próprio e-mail e senha — não existe "o Admin", existem administradores. Por isso o código de verificação (Requisito 2) só pode fazer sentido enviado para o **e-mail do Admin autenticado que está realizando a ação** (extraído do próprio token de acesso, igual a qualquer outra ação feita "como Admin" no sistema) — nunca um endereço fixo nem o e-mail de outro Admin.
- **Professor inativo já não consegue logar — nenhuma mudança necessária (Requisito 5)**: `AutenticacaoService.LoginAsync` já chama `ValidarCredenciais(professor?.Ativo, ...)`, que lança `CredenciaisInvalidasException` quando `Ativo` não é `true`; o fluxo de `Refresh` também já busca o professor via `ObterPorIdAtivoAsync` (só professor ativo), então nem uma sessão já aberta sobrevive a um refresh de token após a desativação. O único caso não coberto (comportamento padrão de qualquer token JWT, não específico desta feature): um *access token* já emitido continua válido até expirar sozinho (hoje 15 minutos, `Jwt:AccessTokenExpirationMinutes`) mesmo que o professor seja desativado no meio desse intervalo — mesmo comportamento que qualquer outra desativação já tem hoje (Aluno, Turma), não é uma lacuna nova.
- **Nenhum mecanismo de código/OTP genérico existe para reaproveitar diretamente (Requisito 2)** — o único precedente é `SenhaResetToken` (specs anteriores: código de 15 minutos, hash armazenado, uso único, usado pelo fluxo "esqueci minha senha"), mas ele é **exclusivo da própria professora redefinir a própria senha** (o e-mail de destino é sempre o da conta dona do token). Esta feature precisa de um token **da mesma forma** (hash, expiração, uso único) mas com dono e propósito diferentes — autoriza uma ação de um **Admin** sobre um **Professor que não é ele**. A spec adota o mesmo padrão técnico (não o mesmo registro).
- **Exclusão lógica + reativação já têm precedente direto a copiar**: `AlunoService.ExcluirAsync`/`ReativarAsync` (e o mesmo em Turma/Matéria/VinculoCobranca) seguem exatamente `Ativo = false` / `Ativo = true`, expostos como `DELETE /{id}` e `PATCH /{id}/reativar`. `Professor` já tem o campo `Ativo`; só falta os métodos/endpoints equivalentes, hoje ausentes em `ProfessorController`/`IProfessorService`.
- **A "tela de gerenciamento/perfil do Professor" já existe, mas é usada só para cadastro/edição**: `frontend/app/(app)/admin/cadastrar-professora` (componente `CadastrarProfessoraForm`) já é a mesma tela para cadastrar a professora pela primeira vez **ou** editá-la depois (o próprio subtítulo da página já diz isso) — é nela que o botão "Excluir Professor" deve entrar; nenhuma tela nova precisa ser criada. O backend já expõe `GET/PUT /api/professor/admin` (visualizar/editar como Admin), mas ainda não tem `DELETE`/`reativar`.
- **Infraestrutura de e-mail (spec 043)** já existe e funciona (`IEmailSender`/`BrevoEmailSender`) — reaproveitável diretamente para enviar o código ao Admin, sem nenhuma mudança na infraestrutura de envio em si.

## Clarifications

_Nenhuma pendente — a única ambiguidade real do pedido original ("e-mail do Admin", presumindo um Admin único) foi resolvida pela investigação acima (o e-mail é sempre do Admin autenticado que realiza a ação); as demais decisões têm um default claro por precedente direto no código (ver Assumptions)._

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Admin exclui a professora com segurança extra (Priority: P1)

Como Admin, quero excluir o cadastro da professora só depois de confirmar um código enviado para o meu próprio e-mail, para ter certeza de que ninguém além de mim (com acesso à minha caixa de entrada) pode efetivar essa ação crítica.

**Why this priority**: é o núcleo do pedido — sem o 2FA, a exclusão seria só mais um botão perigoso; o 2FA é o que a torna uma ação deliberada e segura.

**Independent Test**: logado como Admin, na tela de gerenciamento da professora, clicar em "Excluir Professor", receber um código no próprio e-mail do Admin, digitá-lo corretamente, e confirmar que a professora passa a `Ativo = false` sem perder nenhum dado vinculado.

**Acceptance Scenarios**:

1. **Given** o Admin autenticado na tela de gerenciamento da professora, **When** clica em "Excluir Professor", **Then** o sistema envia um código de verificação para o e-mail da conta Admin autenticada (não da professora) e pede esse código antes de qualquer outra coisa.
2. **Given** o código recebido, **When** o Admin digita o código correto dentro do prazo de validade, **Then** a professora é desativada (`Ativo = false`), permanecendo visível no histórico (Turmas, Alunos, Aulas, Pagamentos) exatamente como estava.
3. **Given** o código recebido, **When** o Admin digita um código errado, **Then** a exclusão não é efetivada e uma mensagem clara de erro aparece, sem revelar o código certo.
4. **Given** um código já expirado (mais de 15 minutos) ou já usado uma vez, **When** o Admin tenta usá-lo de novo, **Then** o sistema recusa e explica que o código não é mais válido, oferecendo reenviar um novo.
5. **Given** a tela de confirmação do código aberta, **When** o Admin cancela em vez de confirmar, **Then** nada é alterado e o código pendente deixa de poder ser usado depois (mesmo que o Admin tente digitá-lo mais tarde).

---

### User Story 2 - Aviso explícito do impacto single-tenant antes de confirmar (Priority: P1)

Como Admin, quero ser avisado em linguagem clara, antes de pedir o código, que excluir a única professora cadastrada torna o sistema inacessível para uso normal até eu cadastrar uma nova, para não fazer isso sem entender a consequência imediata.

**Why this priority**: sem esse aviso, o 2FA sozinho protege contra ação acidental, mas não contra uma decisão mal-informada sobre o efeito real (hoje, sistema inteiro parado).

**Independent Test**: clicar em "Excluir Professor" e confirmar que, antes do código ser pedido (ou junto com o pedido), aparece um aviso específico e proeminente sobre a consequência single-tenant — distinto do aviso genérico de "ação não pode ser desfeita".

**Acceptance Scenarios**:

1. **Given** o Admin clica em "Excluir Professor", **When** a confirmação é exibida, **Then** um aviso específico e visualmente destacado informa que isso torna o sistema indisponível para a rotina normal (login da professora, alunos, aulas) até uma nova professora ser cadastrada — não é só um aviso genérico de "esta ação não pode ser desfeita".

---

### User Story 3 - Reativar uma professora excluída (Priority: P2)

Como Admin, quero poder reativar uma professora que eu excluí por engano (ou por qualquer outro motivo), para restaurar o acesso dela sem precisar recriar o cadastro do zero.

**Why this priority**: é o mesmo padrão de consistência já aplicado a Aluno/Turma/Matéria — importante por coerência do sistema, mas P2 porque o cenário de uso real é raro neste sistema single-tenant (o Admin normalmente cadastraria uma professora nova em vez de reativar a antiga).

**Independent Test**: com uma professora desativada, acionar a reativação e confirmar que ela volta a conseguir logar normalmente, com todos os dados intactos.

**Acceptance Scenarios**:

1. **Given** uma professora desativada, **When** o Admin aciona "Reativar", **Then** ela volta a `Ativo = true` e consegue logar normalmente de novo, sem precisar de 2FA para essa ação de reversão (reativar não tem o mesmo risco que excluir).

---

### Edge Cases

- **E-mail do Admin falha ao enviar** (Trello/Brevo fora do ar, ou o mesmo bloqueio de rede documentado na spec 043): o Admin não recebe o código e não consegue prosseguir — a ação MUST falhar com uma mensagem clara (nunca travar silenciosamente), e o Admin pode tentar reenviar.
- **Admin pede um novo código antes do anterior expirar**: o código anterior MUST deixar de ser válido (um único código pendente por vez, mesmo padrão de uso único do `SenhaResetToken`).
- **Dois Admins tentam excluir a mesma professora ao mesmo tempo**: o código pertence a quem o pediu; o outro Admin recebe seu próprio código, no próprio e-mail, e quem confirmar primeiro efetiva a exclusão — o segundo encontra a professora já inativa.
- **Professora já está inativa e um Admin tenta excluir de novo**: não há uma segunda exclusão a fazer; o sistema informa que ela já está inativa.
- **Sistema sem nenhuma professora cadastrada** (já excluída, nenhuma nova ainda): a tela de gerenciamento reflete esse estado (mesmo comportamento do `404` já existente em `GET /api/professor/admin`) — fora de escopo mudar esse comportamento.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O sistema MUST exibir o botão "Excluir Professor" somente para usuários autenticados com papel Admin, na tela de gerenciamento/edição da professora já existente hoje.
- **FR-002**: Ao acionar a exclusão, o sistema MUST gerar um código de verificação e enviá-lo, por e-mail, exclusivamente para o endereço da conta Admin que está autenticada e realizando a ação — nunca para o e-mail da professora, nem para qualquer outro Admin.
- **FR-003**: O código MUST expirar automaticamente após um intervalo entre 10 e 15 minutos, e MUST ser aceito no máximo uma única vez — usado (com sucesso) ou expirado, deixa de ser válido para sempre.
- **FR-004**: Pedir um novo código antes do anterior expirar MUST invalidar imediatamente o código anterior (nunca dois códigos pendentes e válidos ao mesmo tempo para a mesma ação).
- **FR-005**: O sistema MUST exigir que o Admin informe o código corretamente antes de efetivar a exclusão; um código errado, expirado ou já usado MUST ser recusado com uma mensagem clara, sem revelar qual seria o código correto.
- **FR-006**: Antes (ou junto) de pedir o código, o sistema MUST exibir um aviso específico, visualmente destacado, informando que excluir a única professora cadastrada torna o sistema inacessível para a rotina normal até uma nova professora ser cadastrada pelo Admin — distinto de um aviso genérico de "ação irreversível".
- **FR-007**: Ao confirmar com o código correto e válido, o sistema MUST desativar a professora logicamente (equivalente a `Ativo = false`), MUST NOT apagar fisicamente o registro nem qualquer dado vinculado (Turmas, Alunos, Aulas, histórico financeiro, Pagamentos) — mesmo padrão de exclusão lógica já usado para Aluno/Turma/Matéria.
- **FR-008**: Uma professora desativada por esta ação MUST continuar impossibilitada de logar no sistema (reafirma o comportamento de login já existente — não introduz lógica nova, só preserva a já existente).
- **FR-009**: O sistema MUST permitir que o Admin reative uma professora previamente desativada por esta ação, restaurando o acesso normal dela, sem exigir um novo 2FA para essa reversão.
- **FR-010**: Cancelar a confirmação do código (ou simplesmente não completá-la) MUST NOT alterar o estado da professora, e o código pendente daquela tentativa MUST deixar de poder ser usado depois.
- **FR-011**: Uma falha ao enviar o e-mail do código (provedor fora do ar, rede bloqueada) MUST ser comunicada claramente ao Admin (nunca travar silenciosamente nem fingir sucesso), com a opção de tentar reenviar.
- **FR-012**: Esta feature MUST reaproveitar a infraestrutura de envio de e-mail já existente (`IEmailSender`/Brevo, spec 043) — MUST NOT introduzir um novo provedor ou mecanismo de envio.

### Key Entities

- **Professor (existente)**: ganha os métodos/endpoints de exclusão lógica e reativação (`Ativo`), espelhando exatamente o padrão já usado em Aluno/Turma/Matéria. Nenhum campo novo na entidade em si.
- **Código de verificação de exclusão (novo)**: associado ao Admin que o solicitou e à professora-alvo da exclusão; guarda o código (com hash, nunca em texto plano — mesmo padrão de `SenhaResetToken`), quando foi criado, quando expira, e se já foi usado. Nunca reaproveita o registro de redefinição de senha da própria professora (dono e propósito diferentes).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% das exclusões de professora exigem um código de verificação correto, recebido no e-mail do Admin que a solicitou, antes de qualquer efetivação.
- **SC-002**: 0 exclusões de professora resultam em perda de dado vinculado (Turmas, Alunos, Aulas, Pagamentos continuam acessíveis e intactos após a exclusão).
- **SC-003**: 100% das tentativas de login de uma professora desativada são recusadas, imediatamente após a exclusão.
- **SC-004**: Um Admin olhando a tela de confirmação identifica, sem precisar perguntar, que a exclusão torna o sistema indisponível para uso normal até uma nova professora ser cadastrada.
- **SC-005**: 0 códigos de verificação continuam válidos depois de expirados, usados uma vez, ou substituídos por um código mais novo.
- **SC-006**: 100% das professoras reativadas voltam a conseguir logar normalmente, sem nenhuma perda de dado histórico.

## Assumptions

- O código de verificação é enviado para o e-mail da conta Admin **autenticada no momento da ação** (extraído do próprio token de acesso) — não existe "o e-mail do Admin" fixo, porque o sistema permite múltiplas contas Admin.
- O prazo de validade do código é 15 minutos (mesmo valor já usado em `SenhaResetToken`, por consistência) — não um valor novo a justificar.
- O código de verificação desta feature é um registro novo, dedicado (dono = Admin solicitante, alvo = professora), e não reaproveita a tabela/fluxo de redefinição de senha da própria professora (propósitos e donos diferentes).
- Reativar uma professora **não** exige 2FA — é uma ação de reversão para o estado normal, de risco bem menor que a exclusão (mesma assimetria já aceita implicitamente no padrão de Aluno/Turma/Matéria, onde reativar também não tem nenhuma barreira extra).
- O botão "Excluir Professor" entra na tela já existente `admin/cadastrar-professora` (componente `CadastrarProfessoraForm`), sem necessidade de criar uma tela nova.
- Um *access token* (JWT) de uma professora já emitido antes da exclusão continua tecnicamente válido até expirar sozinho (hoje, até 15 minutos) — mesmo comportamento padrão de qualquer desativação já existente no sistema (Aluno, Turma); esta feature não muda essa janela.
- Fora de escopo: qualquer exclusão física (hard delete) de professora ou de dado vinculado; qualquer mudança no provedor ou mecanismo de envio de e-mail; qualquer mudança em quantos Admins podem existir ou em como eles são cadastrados; qualquer 2FA para outras ações do Admin além desta exclusão.
