# Research: Código de Exclusão de Professora via E-mail da Própria Professora

Decisões técnicas da Phase 0. A decisão de produto (destino do código passa a ser a professora, Admin continua operando a tela) já veio fechada da conversa que precedeu a spec; esta fase resolve os detalhes de implementação.

## R1. Trocar `admin.Email` por `professor.Email` no `EnviarAsync` final — sem remover a busca de `admin`

- **Decision**: em `ProfessorService.SolicitarExclusaoAsync` (`ProfessorService.cs:179-186`), a chamada final a `_emailSender.EnviarAsync` passa a usar `professor.Email` em vez de `admin.Email`. A busca `_adminRepository.ObterPorIdAtivoAsync(adminId)` **continua existindo** — `admin.Nome` é usado no novo texto do e-mail (para a professora saber quem solicitou) e `AdminId` continua sendo gravado em `ExclusaoProfessorToken` (FR-003, rastreabilidade inalterada).
- **Rationale**: `professor` já está carregado no mesmo método (linha 148, `_professorRepository.ObterUnicaAsync`) — é a mudança mínima de destino, sem nenhuma consulta nova.
- **Alternatives considered**: remover a busca de `admin` já que o e-mail não vai mais para ele — descartado, `admin.Nome` ainda aparece no corpo do e-mail (para a professora saber quem pediu a exclusão) e `AdminId` continua necessário para o token (FR-003).

## R2. Corpo do e-mail reescrito para a nova audiência (a professora)

- **Decision**: novo corpo:
  ```html
  <p>Olá, {ProfessorNome}.</p>
  <p>O Admin {AdminNome} solicitou a exclusão do seu cadastro no SPI. Se você concorda, informe o código abaixo a ele para confirmar. Ele expira em 15 minutos e só pode ser usado uma vez.</p>
  <p style="font-size:20px;font-weight:bold;letter-spacing:1px;">{Codigo}</p>
  <p>Se você não esperava este e-mail, não compartilhe o código e entre em contato para entender o que está havendo.</p>
  ```
- **Rationale**: FR-002 — a professora precisa entender que (a) é ela quem está recebendo, (b) quem pediu a exclusão foi um Admin identificado pelo nome, (c) o código é para ela repassar ao Admin (não para ela mesma usar em lugar nenhum, já que quem confirma na tela continua sendo o Admin — `ConfirmarExclusaoAsync` inalterado), e (d) o que fazer se não reconhecer a solicitação.
- **Alternatives considered**: manter o aviso final genérico "ignore este e-mail" (igual ao texto antigo, que falava em "revisar o acesso à sua conta de Admin") — descartado, não faz sentido para a professora (ela não tem "conta de Admin" para revisar); o texto novo orienta uma ação que faz sentido pra ela (não repassar o código, buscar contato).

## R3. FR-004 (professora sem e-mail): validação no início do método, mesmo padrão de `NaoEncontradoException`/`ConflitoException` já usado

- **Decision**: logo após carregar `professor` (antes de invalidar tokens pendentes ou criar um novo), se `string.IsNullOrWhiteSpace(professor.Email)`, lançar `ConflitoException("A professora nao tem e-mail cadastrado; nao e possivel enviar o codigo de confirmacao.")` — nenhum token é criado, nenhum e-mail é tentado.
- **Rationale**: FR-004 — evitar criar um token "órfão" (sem como ser confirmado de verdade, já que o código nunca chegaria a ninguém) ou uma tentativa de envio para endereço vazio que o MailKit rejeitaria de forma confusa. Verificar antes de qualquer efeito colateral (igual ao padrão já usado pelas demais validações do método — `NaoEncontradoException` para professora/admin inexistente, antes de qualquer `AdicionarAsync`).
- **Alternatives considered**: deixar `EnviarAsync` falhar naturalmente com um destinatário vazio e deixar isso propagar como já acontece em FR-011 (falha de envio é propagada) — descartado: criaria um token pendente inútil no banco (nunca confirmável), e a mensagem de erro do MailKit para destinatário vazio seria técnica demais para o Admin entender a causa real.
- **Nota de probabilidade**: `Professor.Email` é `string` não-anulável e já é o identificador de login da professora — no fluxo normal do sistema, nunca deveria estar vazio. Este é um caso defensivo (dado legado/corrompido), não um cenário esperado.

## R4. Testes existentes: ajustar nomes/asserções, não reescrever a suíte

- **Decision**: em `tests/SPI.Application.Tests/Professores/ProfessorServiceExclusaoTests.cs`:
  - `SolicitarExclusao_EnviaCodigoApenasParaOEmailDoAdminAutenticado` → renomear para `SolicitarExclusao_EnviaCodigoParaOEmailDaProfessora` e inverter as asserções (destinatário = `professora.Email`, `DoesNotContain` o e-mail do admin).
  - `SolicitarExclusao_DoisAdminsDiferentes_CadaUmRecebeSeuProprioCodigoNoProprioEmail` → o cenário muda de sentido (os dois agora enviam para o mesmo e-mail da professora, não mais para e-mails diferentes); renomear para `SolicitarExclusao_DoisAdminsDiferentes_AmbosEnviamParaOEmailDaProfessora` e ajustar a asserção para `Assert.All(emailSender.EnviosRealizados, e => Assert.Equal(professora.Email, e.Destinatario))`.
  - As demais (expiração, uso único, invalidação por novo pedido, confirmação, falha de envio) continuam válidas sem mudança de asserção — só passam a rodar contra o novo destino implicitamente.
  - 1 teste novo: `SolicitarExclusao_ProfessoraSemEmail_RecusaSemCriarToken` (FR-004).
- **Rationale**: specs/044 já tem cobertura completa das regras que não mudam (FR-003 a FR-011 daquela spec) — reescrever do zero duplicaria esforço sem necessidade; só os testes que afirmavam o destino antigo precisam mudar.
- **Alternatives considered**: nenhuma — ajuste direto é o caminho natural dado que a suíte já existe e é bem estruturada.
