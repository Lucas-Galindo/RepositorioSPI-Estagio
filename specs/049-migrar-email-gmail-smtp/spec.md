# Feature Specification: Migrar Envio de E-mail para Gmail SMTP

**Feature Branch**: `049-migrar-email-gmail-smtp`

**Created**: 2026-10-12

**Status**: Draft

**Input**: User description: "Substituir a Brevo pelo Gmail SMTP como provedor de envio de e-mail, para os 3 usos atuais (recuperação de senha, código de 2FA de exclusão de professor, e lembretes de aula). Os lembretes continuam usando e-mail (não foram tirados de escopo), mas precisam de uma cláusula de risco sobre o limite de envio do Gmail e de um mecanismo que evite mandar muitos e-mails de uma vez (ex.: 10 agora, 10 daqui a alguns minutos, reaproveitando o ciclo periódico que o serviço de lembretes já tem), para reduzir o risco de a conta Google ser marcada como suspeita. Decisão consciente de assumir o risco do limite de envio do Gmail por enquanto (pode ser revisto depois). Requer uma conta Google dedicada (não a pessoal da professora) com Verificação em Duas Etapas e Senha de app — fora do escopo desta feature em si, é uma tarefa manual de configuração do usuário."

## Nota de investigação prévia

Confirmado por leitura do código atual (grounding antes de especificar):

- **A Brevo já não é uma "API" no sentido de SDK/REST — é só o destino SMTP**: `src/SPI.Infrastructure/Email/BrevoEmailSender.cs` já usa MailKit (biblioteca .NET de SMTP puro, já em uso, nenhuma dependência nova): conecta, autentica e envia via `SmtpClient` do MailKit, usando host/porta/credenciais vindos de `BrevoOptions` (`appsettings.json`: `SmtpHost: "smtp-relay.brevo.com"`, `SmtpPorta: 587`, `RemetenteNome`; `SmtpUsuario`/`SmtpChave`/`RemetenteEmail` em User Secrets). Trocar de provedor é, estruturalmente, só trocar esses valores de configuração — nenhuma biblioteca nova, nenhuma reescrita do protocolo de envio.
- **`IEmailSender` tem hoje 3 chamadores, todos usando a mesma implementação injetada**: `RecuperacaoSenhaService` (recuperação de senha, spec 001/007), `ProfessorService` (código de 2FA para excluir professor, spec 044), e `LembreteDispatcherService` (lembrete de aula por e-mail a alunos/responsáveis, spec 001/"Lembretes — Somente por Email"). Como os 3 compartilham a mesma classe/configuração, trocar o provedor de uma vez (em vez de só para 2 dos 3) é a opção mais simples — bifurcar exigiria duas implementações/configurações separadas de `IEmailSender`, sem nenhum motivo técnico real para isso.
- **O ponto de risco real está em `LembreteDispatcherService.cs`**: hoje, `ProcessarLembretesVencidosAsync` manda todos os e-mails de todos os lembretes vencidos de uma só vez, num `foreach` sem nenhuma pausa — exatamente o padrão de envio "em lote" que as heurísticas anti-automação de provedores como o Gmail observam. O serviço já roda em ciclos periódicos (`PeriodicTimer`, intervalo hoje configurável via `Lembretes:IntervaloVerificacaoSegundos`, padrão 60 segundos) — é esse mecanismo que será reaproveitado para espaçar os envios, sem precisar de nenhum `Task.Delay` bloqueante dentro do próprio envio (que travaria o serviço inteiro, inclusive para outros lembretes, pelo tempo da pausa).
- **Nomenclatura hoje referencia "Brevo" explicitamente** (`BrevoOptions`, `BrevoEmailSender`, seção `"Brevo"` em `appsettings.json`) — manter esses nomes depois de trocar para o Gmail seria confuso (uma classe chamada `BrevoEmailSender` que na prática fala com o Gmail). Esta feature inclui renomear essas referências para algo neutro de provedor (ex.: `SmtpEmailSender`/`SmtpOptions`/seção `"Smtp"`), já que é a mesma mudança de qualquer forma (trocar quem configura o quê) e evita deixar o código documentando um provedor que não é mais o usado.
- **Limites e exigências do Gmail SMTP já levantados na conversa que originou este pedido**: ~500 destinatários/dia (conta pessoal), penalização por rajada de envio mesmo abaixo do limite diário, exige Verificação em Duas Etapas + Senha de app (login/senha normal não funciona), e recomenda-se conta dedicada (não pessoal) por risco de a conta toda ser suspensa se marcada como suspeita. Essas informações vêm de conhecimento geral já documentado publicamente pelo Google, não de uma investigação no código deste projeto — ficam registradas como Assumption/risco aceito, não como achado de código.

## Clarifications

_Nenhuma pendente — a decisão técnica central (trocar o provedor para os 3 usos de uma vez, espaçar os envios de lembretes reaproveitando o ciclo periódico já existente, assumir o risco do limite do Gmail por ora) já foi tomada na conversa que precedeu esta spec. Não há ambiguidade de produto a esclarecer com `/speckit-clarify`._

## User Scenarios & Testing *(mandatory)*

### User Story 1 - E-mails de recuperação de senha e de 2FA continuam chegando, agora via Gmail (Priority: P1)

Como professora ou Admin, quero continuar recebendo o código de recuperação de senha e o código de 2FA de exclusão de professor por e-mail, exatamente como hoje, mesmo com a troca de provedor por trás.

**Why this priority**: é a continuidade mínima que não pode quebrar — recuperação de senha e 2FA são os dois fluxos de autenticação que dependem de e-mail chegar de verdade.

**Independent Test**: solicitar recuperação de senha (ou o código de 2FA de exclusão de professor) e confirmar que o e-mail chega normalmente, com o mesmo conteúdo de antes, só vindo agora pela conta Gmail configurada.

**Acceptance Scenarios**:

1. **Given** a professora esqueceu a senha, **When** ela solicita a recuperação, **Then** recebe o código por e-mail, enviado através do Gmail SMTP configurado — mesmo texto/formato de antes.
2. **Given** um Admin solicitando a exclusão de uma professora (spec 044), **When** ele confirma o pedido, **Then** recebe o código de 2FA por e-mail, enviado através do Gmail SMTP configurado.

---

### User Story 2 - Lembretes de aula continuam sendo enviados, de forma espaçada (Priority: P1)

Como professora, quero que os lembretes de aula continuem chegando por e-mail aos alunos/responsáveis, mas enviados em pequenos grupos espaçados no tempo (não todos de uma vez), para reduzir o risco de a conta de e-mail ser penalizada por um envio que pareça em massa.

**Why this priority**: é a mesma prioridade da US1 — sem o espaçamento, a troca de provedor herdaria o padrão de envio em lote que já é o risco conhecido do Gmail SMTP; o lembrete é a única das 3 situações que já manda e-mail para várias pessoas de uma vez.

**Independent Test**: criar um lembrete vencido com destinatários suficientes para exceder o limite de envio por ciclo (ex.: mais que o configurado), deixar o serviço rodar por alguns ciclos, e confirmar que os e-mails saem em grupos ao longo de múltiplos ciclos, não todos de uma vez.

**Acceptance Scenarios**:

1. **Given** um lembrete vencido com um número de destinatários maior que o limite configurado por ciclo, **When** o serviço periódico processa esse lembrete, **Then** só até o limite configurado de e-mails é enviado naquele ciclo; os destinatários restantes são enviados em ciclo(s) seguintes.
2. **Given** um lembrete vencido com destinatários dentro do limite por ciclo (incluindo o caso de vários lembretes pequenos cuja soma excede o limite), **When** o serviço processa, **Then** o total de e-mails enviados num único ciclo nunca excede o limite configurado, mesmo somando todos os lembretes vencidos daquele ciclo.
3. **Given** destinatários que não foram enviados num ciclo por causa do limite, **When** o próximo ciclo roda, **Then** esses destinatários pendentes são tentados primeiro, antes de qualquer lembrete mais novo.

---

### Edge Cases

- **Falha ao enviar um e-mail dentro de um lote** (ex.: destinatário com endereço invalido): o lembrete correspondente é marcado como já é hoje (`"Falha"`/parcialmente `"Enviado"`) — esta feature não muda esse tratamento de erro existente, só adiciona o limite de quantidade por ciclo.
- **Volume de lembretes pendentes muito maior que o limite por ciclo, por vários ciclos seguidos** (ex.: acúmulo depois de o serviço ficar parado por um tempo): os envios continuam se espalhando pelos ciclos seguintes até esvaziar a fila — não é exigido um mecanismo de prioridade além de "o que já estava esperando vai primeiro" (cenário 3 da US2).
- **Conta Gmail bloqueada/suspensa de qualquer forma, mesmo com o espaçamento**: fora do controle desta feature — o espaçamento reduz o risco, não o elimina (documentado como risco aceito, não como garantia).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O sistema MUST enviar e-mails de recuperação de senha, de código de 2FA (exclusão de professor), e de lembrete de aula através de uma conta Gmail configurada via SMTP, em vez da Brevo.
- **FR-002**: O sistema MUST continuar usando a mesma interface/abstração de envio de e-mail já existente (`IEmailSender`) para os 3 usos — nenhuma lógica de negócio de recuperação de senha, 2FA, ou lembretes MUST ser duplicada ou alterada por esta feature.
- **FR-003**: O sistema MUST limitar a quantidade de e-mails de lembrete enviados por ciclo do serviço periódico já existente a um valor configurável (não hardcoded), com um padrão razoável (ex.: 10) se não configurado.
- **FR-004**: Quando o total de destinatários pendentes num ciclo exceder o limite configurado, o sistema MUST enviar só até o limite naquele ciclo, e MUST enviar os destinatários restantes em ciclo(s) seguintes, priorizando quem está esperando mais tempo.
- **FR-005**: O sistema MUST NOT introduzir nenhuma pausa bloqueante (ex.: `Task.Delay` de minutos) dentro de um único ciclo de processamento — o espaçamento entre lotes MUST vir do próprio intervalo entre ciclos do serviço já existente.
- **FR-006**: O sistema MUST NOT alterar o conteúdo, formato, ou destinatário de nenhum dos 3 tipos de e-mail (recuperação de senha, 2FA, lembrete) — só o provedor por trás e o ritmo de envio dos lembretes mudam.
- **FR-007**: Nomes de classe/configuração que hoje referenciam explicitamente "Brevo" MUST ser renomeados para algo neutro de provedor, para não deixar o código documentando um provedor que não é mais o usado.
- **FR-008**: A spec MUST documentar, como risco aceito (não como defeito a corrigir), as limitações conhecidas do Gmail SMTP (limite diário de envio, penalização por rajada, exigência de Verificação em Duas Etapas + Senha de app numa conta dedicada) — ver Assumptions.

### Key Entities

- **Lembrete (existente, spec 001, com 2 campos novos)**: para cumprir FR-004 de verdade (retomar exatamente de onde parou, destinatário por destinatário, entre ciclos), `Lembrete` precisa registrar quais destinatários daquele lembrete específico já foram tentados antes de o envio completo ser concluído — hoje ele só sabe "Pendente"/"Enviado"/"Falha" como um bloco único, sem noção de "enviado pra metade dos destinatários". Os 2 campos novos guardam esse progresso parcial entre ciclos; nenhum campo existente muda de significado.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% dos e-mails de recuperação de senha e de código de 2FA continuam sendo entregues com sucesso, agora via Gmail SMTP.
- **SC-002**: Em um lembrete com destinatários acima do limite configurado por ciclo, os e-mails são distribuídos em pelo menos 2 ciclos do serviço, nunca excedendo o limite num único ciclo.
- **SC-003**: 0 mudanças no conteúdo/formato dos 3 tipos de e-mail, e 0 mudanças na lógica de negócio de recuperação de senha, 2FA, ou regra de quais lembretes são elegíveis (confirmável por inspeção do código após a implementação).
- **SC-004**: 0 referências a "Brevo" remanescentes no código de produção (classes, opções, seções de configuração) após a renomeação.

## Assumptions

- **Risco aceito conscientemente**: Gmail SMTP (conta pessoal) tem limite de referência de ~500 destinatários/dia e penaliza rajadas de envio mesmo abaixo desse limite — este projeto assume esse risco por ora, dado o volume atual (sistema single-tenant, poucos alunos), com a opção de revisar/trocar de provedor depois se o volume crescer ou a conta for penalizada.
- O SMTP do Gmail exige Verificação em Duas Etapas ativada na conta e uma Senha de app gerada especificamente para isso (login/senha normais não funcionam) — configuração manual fora do escopo desta feature (o usuário já está ciente e vai providenciar).
- Recomenda-se uma conta Google dedicada para isso (não a conta pessoal de ninguém envolvido no sistema), para que um eventual bloqueio por atividade suspeita não afete nada além do envio de e-mail do sistema — decisão de operação do usuário, fora do escopo desta feature em si.
- O limite de e-mails por ciclo (FR-003) é uma configuração de aplicação (ex.: `appsettings.json`), não uma entidade de banco — mesmo padrão já usado para `Lembretes:IntervaloVerificacaoSegundos`.
- Fora de escopo: qualquer outro provedor de e-mail (SendGrid/SES/Mailgun) além do Gmail; hospedar um servidor de e-mail próprio; mudar o conteúdo/texto de qualquer e-mail; mudar a regra de quais lembretes são elegíveis para envio (spec 001) ou o cálculo de destinatários; qualquer mecanismo de retomada automática caso a conta Gmail seja de fato bloqueada (tratado manualmente se acontecer).
