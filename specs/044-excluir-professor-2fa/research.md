# Research: Excluir Professor com Confirmação por 2FA via E-mail

Decisões técnicas da Phase 0. Não havia "NEEDS CLARIFICATION" pendente no Technical Context — a única ambiguidade de produto (qual e-mail recebe o código) já foi resolvida no spec pela investigação prévia.

## R1. Formato do código: reaproveitar `ITokenService.GerarTokenSeguro()`, não um OTP numérico novo

- **Decision**: o "código de verificação" é gerado exatamente como o token de `SenhaResetToken` — `ITokenService.GerarTokenSeguro()` (64 bytes aleatórios, Base64, ~88 caracteres), hash SHA-256 armazenado via `CalcularHash()`. O Admin copia/cola esse valor no campo de confirmação, igual ao fluxo de recuperação de senha já existente (`EsqueciSenhaForm.tsx`).
- **Rationale**: o pedido original chama isso de "código", mas o único precedente real no sistema (`SenhaResetToken`) já estabeleceu esse formato como "o código" que o usuário recebe por e-mail e cola na tela — não um OTP de 6 dígitos (que exigiria um gerador novo, sem nenhum precedente no código). Reaproveitar o gerador existente é mais simples, mais seguro (mais entropia que 6 dígitos) e consistente com a experiência que a professora já tem hoje ao recuperar a própria senha.
- **Alternatives considered**: gerar um OTP numérico curto (ex.: 6 dígitos) — mais parecido com 2FA de apps bancários, mas exigiria um gerador novo e uma política de tentativas (rate limit de tentativas de adivinhação, já que 6 dígitos têm bem menos entropia que um token de 64 bytes) — complexidade extra sem necessidade, dado que o próprio sistema já resolveu esse problema de outra forma e a experiência (copiar/colar de um e-mail) é a mesma de qualquer jeito.

## R2. Entidade nova (`ExclusaoProfessorToken`), não reaproveitar `SenhaResetToken`

- **Decision**: nova tabela `exclusao_professor_token` com `admin_id` (dono do código, FK para `admin`) e `professor_id` (alvo da exclusão, FK para `professor`) — campos de hash/validade/uso único idênticos a `senha_reset_token`.
- **Rationale**: `SenhaResetToken` modela "a professora redefine a própria senha" — dono e alvo são sempre a mesma pessoa, e a tabela nem tem coluna para um "solicitante" diferente do dono. Aqui dono (Admin) e alvo (Professor) são entidades diferentes; forçar isso em `SenhaResetToken` exigiria adicionar uma coluna sem sentido para o caso de uso original (recuperação de senha nunca tem um "Admin solicitante"). Uma tabela nova e pequena, espelhando a estrutura já validada, é mais simples e não arrisca misturar dois propósitos num único registro.
- **Alternatives considered**: generalizar `SenhaResetToken` para um "token genérico de verificação" com um campo de propósito — descartado, over-engineering para dois usos concretos; nenhuma necessidade hoje de um terceiro uso que justificasse a generalização.

## R3. Destinatário do e-mail: sempre o Admin autenticado, nunca um parâmetro do cliente

- **Decision**: o endpoint de solicitar o código não recebe nenhum e-mail de destino no corpo da requisição — o backend resolve o Admin a partir do próprio token de acesso (`User.ObterUsuarioId()`, já usado em todo o resto do sistema) e busca o e-mail via `IAdminRepository.ObterPorIdAtivoAsync(adminId)`.
- **Rationale**: FR-002 exige que o código vá sempre para quem está de fato autenticado fazendo a ação — se o cliente pudesse informar o e-mail, um Admin (ou alguém com a sessão dele comprometida) poderia redirecionar o código para outro endereço. Resolver pelo claim do token, nunca por parâmetro, fecha essa brecha por desenho.
- **Alternatives considered**: nenhuma — essa é a única forma consistente com "o Admin confirma com algo que só ele recebe".

## R4. Escopo de invalidação do código anterior: por Admin, não por professora

- **Decision**: ao solicitar um novo código, todo código ainda não usado e pertencente ao **mesmo `admin_id`** é marcado `usado = true` antes de criar o novo (independente de já ter expirado ou não). O escopo é por Admin, não por professora-alvo.
- **Rationale**: a spec (edge case "dois Admins tentam excluir a mesma professora ao mesmo tempo") exige que cada Admin tenha seu próprio código independente — um Admin pedindo um novo código não pode invalidar o código que outro Admin já está prestes a usar. Escopar por `admin_id` entrega exatamente isso; escopar por `professor_id` (ou globalmente) quebraria esse cenário.
- **Alternatives considered**: um único código pendente por professora (ignorando quem pediu) — mais simples, mas contradiz o comportamento explícito pedido no edge case da spec.

## R5. Endpoints: convenção singular `admin`, sem `{id}` (mesmo padrão de `GET/PUT /api/professor/admin`)

- **Decision**: três endpoints novos em `ProfessorController` (`[Route("api/professor")]`, já existente), todos `[Authorize(Roles = Admin)]`:
  - `POST /api/professor/admin/excluir/solicitar-codigo` — gera e envia o código.
  - `POST /api/professor/admin/excluir/confirmar` — `{ Codigo }`, valida e efetiva a exclusão.
  - `PATCH /api/professor/admin/reativar` — sem corpo, sem 2FA.
- **Rationale**: o sistema é single-tenant — `GET`/`PUT /api/professor/admin` já tratam "a professora" sem precisar de um `{id}` na rota (sempre a única que existe, via `ObterUnicaAsync`). Seguir essa mesma convenção evita introduzir um padrão `/{id}` novo só para esta feature, quando o resto do controller já resolve isso sem id.
- **Alternatives considered**: `DELETE /api/professor/{id}` com `{id}` explícito (mesmo padrão de `AlunosController`) — descartado por inconsistência com o restante do próprio `ProfessorController`, que já é todo single-tenant sem `{id}`.

## R6. Sem rate limiting dedicado (diferente de `esqueci-senha`)

- **Decision**: os 3 endpoints novos não ganham `[EnableRateLimiting]` (diferente de `POST /api/auth/esqueci-senha`, que tem).
- **Rationale**: `esqueci-senha` é anônimo (qualquer um pode chamar, por isso precisa de rate limit contra abuso/enumeração). Os endpoints desta feature exigem `Admin` autenticado — o risco de abuso é de outra natureza (um Admin já autenticado espamando o próprio e-mail), não justificando a mesma política.
- **Alternatives considered**: aplicar o mesmo rate limit por precaução — descartado, sem motivo concreto diferente do já coberto pela autenticação exigida.

## R7. Estratégia de testes

- **Decision**: xUnit + fakes manuais (padrão do projeto), cobrindo: solicitar código (gera e envia para o e-mail do Admin certo, invalida código anterior do mesmo Admin), confirmar (código certo ativa a exclusão; código errado/expirado/usado é recusado, sem alterar `Ativo`), reativar (sem exigir código), e os requisitos negativos (e-mail nunca vai para a professora; dois Admins têm códigos independentes). Frontend validado manualmente (`quickstart.md`), mesma decisão já aceita nas specs 040-043.
- **Rationale**: FRs "MUST NOT" (FR-002 nunca para o e-mail da professora, FR-007 nunca exclusão física, FR-010 cancelar nunca altera estado) exigem cobertura explícita, não só dos caminhos positivos — mesma prática já estabelecida nesta sessão.
