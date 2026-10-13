# Research: Migrar Envio de E-mail para Gmail SMTP

Decisões técnicas da Phase 0. A decisão de produto (trocar provedor para os 3 usos, espaçar lembretes, assumir o risco do Gmail) já estava fechada antes da spec; esta fase resolve os pontos de implementação que faltavam, incluindo um ajuste ao próprio `spec.md` (ver R2).

## R1. Renomeação: `BrevoOptions`→`SmtpOptions`, `BrevoEmailSender`→`SmtpEmailSender`, seção `"Brevo"`→`"Smtp"`

- **Decision**: renomear os 2 arquivos/classes e a seção de configuração; `SmtpOptions.SmtpHost` muda o valor padrão de `"smtp-relay.brevo.com"` para `"smtp.gmail.com"` (porta continua 587, `SecureSocketOptions.StartTls` já é o modo certo para Gmail também — nenhuma mudança de protocolo).
- **Rationale**: FR-007. A implementação do `EnviarAsync` (MailKit, conectar/autenticar/enviar) não muda uma linha — é puramente host/porta/credencial, por isso a classe nunca devia ter o nome do provedor "embutido" olhando em retrospecto, mas como já tinha, a troca de provedor é o momento certo de corrigir.
- **Impacto em User Secrets (passo manual do usuário, fora desta feature)**: as chaves hoje são `Brevo:SmtpUsuario`, `Brevo:SmtpChave`, `Brevo:RemetenteEmail` — precisam ser reconfiguradas como `Smtp:SmtpUsuario`, `Smtp:SmtpChave`, `Smtp:RemetenteEmail`, com os valores da conta Gmail dedicada (não os valores atuais da Brevo). **Achado importante para o usuário providenciar os valores**: para Gmail SMTP, `SmtpUsuario` (conta autenticada) e `RemetenteEmail` (endereço "De:") geralmente precisam ser a mesma conta Gmail (ou um alias dela) — diferente da Brevo, que permite autenticar como uma conta e enviar "De:" um endereço verificado diferente (hoje `RemetenteEmail` é `srlucasmtg@gmail.com`, que pode ou não ser a conta dedicada nova; o usuário precisa confirmar que os dois valores vão apontar pra mesma conta Gmail nova).
- **Alternatives considered**: manter os nomes "Brevo" e só trocar os valores — descartado (FR-007 explícito; nome desatualizado é uma lacuna de clareza desnecessária, já identificada na investigação prévia do spec).

## R2. Orçamento por ciclo com retomada parcial: 2 campos novos em `Lembrete`, não um limite "por lembrete"

- **Decision**: `Lembrete` ganha `DestinatariosTentados` (`string?`, lista de e-mails já tentados nesse lembrete, separados por `;`) e `EnviosComSucesso` (`int`, padrão 0, contagem cumulativa de sucessos entre ciclos). A cada ciclo, `LembreteDispatcherService` processa os lembretes vencidos em ordem de `HoraProgramada` ascendente; para cada um, calcula `destinatarios = ObterEmailsDestinatarios(lembrete) - DestinatariosTentados`, envia até o orçamento restante do ciclo (`Lembretes:MaxEmailsPorCiclo`, padrão 10), acumula os tentados em `DestinatariosTentados` e os sucessos em `EnviosComSucesso`. Só quando `DestinatariosTentados` cobre 100% dos destinatários originais o `Status` vira `"Enviado"` (se `EnviosComSucesso > 0`) ou `"Falha"` (se 0) — até lá, `Status` continua `"Pendente"`.
- **Rationale**: o pedido original do usuário ("mando 10 agora, mando 10 daqui a 5 minutos") é sobre **um único lembrete** com muitos destinatários (ex.: uma turma de 20 alunos) sendo dividido em lotes — não sobre lembretes diferentes sendo processados em ciclos diferentes (que já aconteceria naturalmente, sem precisar de nada novo). Satisfazer isso de verdade exige saber, entre um ciclo e o próximo, quais daqueles 20 destinatários já foram tentados — daí os 2 campos novos. **Isso contradiz a primeira versão do `spec.md`** (que dizia "nenhuma mudança de schema") — corrigido no próprio spec antes desta fase, para não deixar uma afirmação errada documentada.
- **Alternatives considered**:
  - Tratar cada lembrete como atômico (processar tudo ou nada por ciclo, pulando o lembrete inteiro se não couber no orçamento restante) — descartado: não divide os 20 destinatários de um único lembrete em lotes, só atrasa o lembrete inteiro para o próximo ciclo (ainda manda os 20 de uma vez, só mais tarde) — não é o que o usuário pediu.
  - Uma tabela nova `lembrete_envio` (uma linha por destinatário/lembrete, com status individual) em vez de 2 colunas na própria `Lembrete` — mais normalizado, mas desproporcional: não há necessidade de consultar/filtrar destinatários individualmente fora deste fluxo, e a tabela nova exigiria joins extras só para um controle de progresso que uma lista simples já resolve.
  - Resetar/ignorar `DestinatariosTentados` quando a lista de destinatários do lembrete mudar entre ciclos (ex.: um aluno é removido da turma no meio do processo) — não tratado explicitamente: `ObterEmailsDestinatarios` é recalculado a cada ciclo a partir do estado atual da turma, então um aluno removido simplesmente não aparece mais na lista alvo (efeito já é o correto, sem necessidade de lógica extra) — comportamento aceito, documentado como nota de implementação, não como requisito novo.

## R3. Ordenação de `ListarPendentesVencidosAsync`: por `HoraProgramada` ascendente

- **Decision**: adicionar `.OrderBy(l => l.HoraProgramada)` à query existente em `LembreteRepository.cs`.
- **Rationale**: FR-004 ("priorizando quem está esperando mais tempo") — hoje a query não tem nenhuma ordenação explícita (ordem efetivamente arbitrária, dependente do plano de execução do banco); ordenar por `HoraProgramada` garante que o lembrete vencido há mais tempo consome o orçamento do ciclo primeiro, de forma determinística.
- **Alternatives considered**: ordenar por `Id` — descartado, `HoraProgramada` é o campo que representa "há quanto tempo está esperando", não a ordem de criação do registro.

## R4. Sem `Task.Delay` bloqueante — o espaçamento é só o intervalo entre ciclos

- **Decision**: nenhuma mudança no `PeriodicTimer`/`ExecuteAsync` do `LembreteDispatcherService` — o orçamento por ciclo (R2) já produz o espaçamento pedido (10 agora, resto no próximo tick do timer, hoje a cada 60s por padrão — ajustável via `Lembretes:IntervaloVerificacaoSegundos`, já configurável, sem mudança nesta feature).
- **Rationale**: FR-005 explícito. Um `Task.Delay` de minutos dentro do processamento travaria o serviço inteiro (nenhum outro lembrete seria processado durante a espera) — o `PeriodicTimer` já existente é exatamente o mecanismo certo para isso, sem nenhuma mudança necessária nele.
- **Alternatives considered**: nenhuma — já confirmado que a arquitetura existente já serve.

## R5. Testes: fake de `IEmailSender`/`ILembreteRepository` para o orçamento por ciclo

- **Decision**: testes novos (xUnit, fakes manuais) cobrindo: um lembrete com destinatários acima do limite é dividido entre 2+ chamadas de `ProcessarLembretesVencidosAsync` (simulando 2 ciclos), nunca excedendo o limite numa única chamada; a ordem de prioridade por `HoraProgramada`; o `Status` só vira `"Enviado"`/`"Falha"` quando todos os destinatários originais já foram tentados; nenhuma mudança no cálculo de `ObterEmailsDestinatarios` nem na regra de elegibilidade (FR-006).
- **Rationale**: é lógica de negócio nova (orçamento, retomada parcial) — mesma decisão de testar já aplicada a outras specs deste projeto com lógica de serviço de background (ex.: `MensalidadeDispatcherService`, spec 039).
- **Alternatives considered**: nenhuma — segue o padrão já estabelecido do projeto.
