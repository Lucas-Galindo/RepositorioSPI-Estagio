# Feature Specification: Código de Exclusão de Professora via E-mail da Própria Professora

**Feature Branch**: `054-codigo-exclusao-email-professora`

**Created**: 2026-10-17

**Status**: Draft

**Input**: User description: "O código de confirmação de exclusão da professora (specs/044) deve passar a ser enviado para o e-mail já cadastrado da própria professora, em vez do e-mail do Admin autenticado — o Admin continua logando normalmente e clicando em 'Excluir Professor', só muda para onde o código é enviado. O Admin segue digitando o código pra confirmar."

## Nota de investigação prévia

Confirmado por leitura do código e da spec 044 atual (grounding antes de especificar):

- **Esta feature reverte uma decisão deliberada da spec 044, não corrige um bug**: `specs/044-excluir-professor-2fa/spec.md` FR-002 diz explicitamente "MUST... enviá-lo, por e-mail, exclusivamente para o endereço da conta Admin que está autenticada e realizando a ação — nunca para o e-mail da professora". A User Story 1 daquela spec explica o motivo original: confirmar que é o próprio Admin autenticado (não uma sessão roubada) quem pede a exclusão. O usuário decidiu conscientemente, nesta conversa, trocar esse modelo — a professora (dona da conta sendo excluída) passa a ser quem recebe e, na prática, autoriza a própria exclusão ao compartilhar o código com o Admin. Decisão de produto do usuário, registrada aqui como a nova fonte de verdade (specs/044 permanece como documentação histórica do comportamento anterior, não é reescrita).
- **`ProfessorService.SolicitarExclusaoAsync`** (`src/SPI.Application/Professores/Services/ProfessorService.cs:146-186`) já carrega a entidade `professor` inteira (via `_professorRepository.ObterUnicaAsync`) antes de montar o e-mail — `professor.Email` já está disponível no mesmo método, trocar `admin.Email` por `professor.Email` no `EnviarAsync` final é a mudança mínima de destino. A busca de `admin` (`_adminRepository.ObterPorIdAtivoAsync`) continua necessária: `admin.Nome`/`AdminId` seguem sendo gravados no token (`ExclusaoProfessorToken.AdminId`) para rastreabilidade de quem pediu a exclusão — isso não muda.
- **`Professor.Email`** (`src/SPI.Domain/Entities/Professor.cs`) é `string` não-anulável — mesmo padrão de `Admin.Email`, já é o identificador de login da professora (login unificado). Na prática, uma professora cadastrada sempre tem um e-mail válido (exigido no cadastro) — o caso "sem e-mail" é um extremo defensivo, não um cenário esperado no dia a dia.
- **O corpo do e-mail precisa mudar de destinatário no texto, não só no envio**: hoje (`ProfessorService.cs:179-184`) a saudação é `"Olá, {admin.Nome}."` e o aviso final é `"...considere revisar o acesso à sua conta de Admin"` — ambos presumem que quem lê é o Admin. Como quem lê passa a ser a professora, o texto precisa ser reescrito para falar com ela (avisando que a exclusão da própria conta dela foi solicitada por um Admin, e o que o código serve para confirmar).
- **`ConfirmarExclusaoAsync`** (fluxo de confirmação em si, validação do código, desativação lógica `Ativo = false`) não muda nada — continua sendo o Admin quem digita o código na tela; só a etapa de "pra onde o código vai" muda.

## Clarifications

_Nenhuma pendente — a decisão central (destino do e-mail passa a ser a professora, Admin continua operando a tela normalmente) já foi tomada e confirmada explicitamente pelo usuário nesta conversa, com a ressalva (também já decidida) de reescrever o texto do e-mail para a nova audiência._

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Código de exclusão vai para o e-mail da professora, não do Admin (Priority: P1)

Como Admin, quero que, ao clicar em "Excluir Professor", o código de confirmação seja enviado para o e-mail já cadastrado da própria professora (não para o meu e-mail de Admin), para que a exclusão dependa de um código que chega numa caixa de entrada que eu sei que é real e ativamente monitorada — diferente de contas de Admin que podem ter e-mail de cadastro/teste.

**Why this priority**: é o núcleo do pedido — sem essa troca de destino, o fluxo de 2FA continua indisponível sempre que o Admin autenticado não tiver um e-mail real cadastrado.

**Independent Test**: logado como Admin, clicar em "Excluir Professor"; confirmar que o código chega no e-mail cadastrado da professora (não no do Admin), e que digitar esse código na tela do Admin efetiva a exclusão normalmente.

**Acceptance Scenarios**:

1. **Given** o Admin autenticado na tela de gerenciamento da professora, **When** clica em "Excluir Professor", **Then** o sistema envia o código de verificação para o e-mail cadastrado da professora (não para o e-mail do Admin).
2. **Given** o código recebido no e-mail da professora, **When** o Admin digita esse código corretamente na tela (independente de ter sido ele mesmo a ver o e-mail, ou a professora ter repassado o código), **Then** a professora é desativada (`Ativo = false`), exatamente como a spec 044 já define (sem mudança nessa parte).
3. **Given** o e-mail enviado, **When** a professora (ou quem abrir o e-mail) lê o conteúdo, **Then** o texto se dirige a ela (não ao Admin), deixando claro que um Admin solicitou a exclusão da conta dela e qual é o código.
4. **Given** todas as demais regras já existentes da spec 044 (expiração em 15 minutos, uso único, invalidação ao pedir um novo código, aviso de consequência single-tenant, exclusão lógica preservando histórico), **When** esta feature é aplicada, **Then** nenhuma delas muda de comportamento — só o destinatário do e-mail e o texto do corpo mudam.

---

### Edge Cases

- **Professora sem e-mail cadastrado** (extremo defensivo, não deveria acontecer já que o cadastro exige e-mail): o sistema MUST recusar a solicitação do código com uma mensagem clara ao Admin, em vez de tentar enviar para um endereço vazio ou travar silenciosamente — mesmo espírito de FR-011 da spec 044 (falha de envio sempre comunicada).
- **A professora não vê o e-mail a tempo** (código expira em 15 minutos, mesmo prazo de specs/044): mesmo comportamento já existente — o Admin pode pedir um novo código, invalidando o anterior.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O sistema MUST enviar o código de verificação de exclusão de professora, por e-mail, exclusivamente para o endereço cadastrado da **professora-alvo** da exclusão — substitui o destino anterior (e-mail do Admin autenticado, specs/044 FR-002).
- **FR-002**: O texto do e-mail MUST se dirigir à professora (saudação e contexto), explicando que um Admin solicitou a exclusão da conta dela e informando o código — não mais ao Admin.
- **FR-003**: O sistema MUST continuar registrando qual Admin solicitou a exclusão (associação já existente em `ExclusaoProfessorToken.AdminId`) — esta feature MUST NOT remover essa rastreabilidade, mesmo o e-mail não indo mais para esse Admin.
- **FR-004**: Quando a professora-alvo não tiver nenhum e-mail cadastrado, o sistema MUST recusar a solicitação do código com uma mensagem clara ao Admin, em vez de tentar um envio inválido.
- **FR-005**: Esta feature MUST NOT alterar nenhuma outra regra já especificada em specs/044 — expiração do código (15 minutos), uso único, invalidação ao pedir código novo, aviso de consequência single-tenant, confirmação pelo Admin via código digitado, exclusão lógica (`Ativo = false`) preservando todo histórico vinculado, e reativação sem exigir novo 2FA.

### Key Entities

- **Código de verificação de exclusão (`ExclusaoProfessorToken`, existente, specs/044)**: nenhum campo novo — continua associado ao Admin solicitante (`AdminId`) e à professora-alvo (`ProfessorId`); só o destino do e-mail enviado a partir dele muda, não a estrutura do registro.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% dos códigos de verificação de exclusão de professora chegam ao e-mail cadastrado da professora-alvo, não ao e-mail do Admin.
- **SC-002**: 0 mudanças de comportamento nas demais regras já validadas pela spec 044 (expiração, uso único, exclusão lógica, reativação) — confirmável pela suíte de testes já existente dessa spec continuando 100% verde, exceto os testes que afirmavam explicitamente o destino antigo (ajustados nesta feature).
- **SC-003**: Uma professora sem e-mail cadastrado nunca recebe uma tentativa de envio silenciosamente falha — o Admin sempre vê uma mensagem de erro clara nesse caso.

## Assumptions

- Esta é uma mudança de regra de negócio consciente, não uma correção de bug — specs/044 continua sendo a documentação histórica correta do comportamento anterior; esta spec é quem passa a valer a partir de agora (mesmo princípio de "spec mais recente prevalece" já usado no projeto para evoluções, ex.: specs/041 sobre specs/038).
- O Admin continua sendo quem efetivamente digita o código na tela de confirmação — esta feature não muda quem opera a tela, só de onde o Admin precisa obter o código (agora depende da professora repassar, por exemplo).
- Fora de escopo: qualquer mudança na tela/fluxo de confirmação do Admin, no formato do código, no prazo de validade, ou na lógica de exclusão/reativação em si (tudo isso continua exatamente como specs/044 já define).
