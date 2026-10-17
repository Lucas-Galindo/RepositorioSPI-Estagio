# Relatório: Lembrete por e-mail de vencimento da Mensalidade (5 dias antes)

**Card Trello**: "Lembrete por e-mail de vencimento da Mensalidade (5 dias antes)" (coluna Backlog → Em andamento → Revisão)
**Spec**: nenhuma — card criado manualmente, sem spec formal (implementado direto a partir da descrição do card)
**Status final**: ✅ Concluído

## Cartão duplicado encontrado

Existiam **dois cards idênticos** com esse mesmo nome no Backlog (`6acd960e93021c82ab70a529` e `6acd960e7f7bf0d8426a51a3`), com a mesma descrição (byte a byte) e `dateLastActivity` 0,4s um do outro — claramente uma duplicação acidental de uma criação anterior nesta mesma sessão, não dois pedidos distintos. Segui a instrução do processo autônomo: trabalhei só o primeiro (`6acd960e93021c82ab70a529`) e **arquivei o segundo** (`6acd960e7f7bf0d8426a51a3`) no Trello, com um comentário explicando o motivo.

## O que foi entendido do pedido

O card descreve uma dependência (migração Brevo→Gmail SMTP, specs/049) que **já estava concluída** antes deste card ser iniciado (confirmado pelos commits já existentes nesta branch). Com a dependência resolvida, o pedido é: quando uma cobrança de mensalidade automática é gerada (pelo `MensalidadeDispatcherService`, specs/039 — `Pagamento.Status=Pendente`, `VinculoCobrancaId` preenchido), o sistema deve agendar um lembrete por e-mail enviado **5 dias antes** da `DataVencimento`, avisando aluno e/ou responsável. O próprio card sugeria reaproveitar o padrão técnico já existente no projeto (`LembreteDispatcherService`: `BackgroundService` + `PeriodicTimer` + varredura de repositório + `IEmailSender`).

Importante (já confirmado pela descrição do card, sem necessidade de decisão nova): nenhuma integração de gateway de pagamento é criada — o sistema continua apenas registrando pagamentos manualmente, como hoje.

## O que foi feito

- **`database/19_lembrete_vencimento_mensalidade.sql`** (nova migração): coluna `pagamento.lembrete_vencimento_enviado` (`TINYINT(1) NOT NULL DEFAULT 0`) — aplicada no MySQL local dev durante esta sessão (confirmado via `DESCRIBE pagamento`).
- **`Pagamento.cs`**: nova propriedade `LembreteVencimentoEnviado` (bool) + mapeamento em `PagamentoConfiguration.cs`.
- **`IPagamentoRepository`/`PagamentoRepository`**: novo método `ListarMensalidadesParaLembreteVencimentoAsync(dataVencimentoAlvo)` — filtra `VinculoCobrancaId != null` (só cobranças automáticas de mensalidade, nunca pagamentos manuais/por presença — specs/039 já documenta essa convenção), `Status == "Pendente"`, `!LembreteVencimentoEnviado`, `DataVencimento == dataVencimentoAlvo`.
- **`MensalidadeLembreteDispatcherService.cs`** (novo `BackgroundService`): a cada tick (`MensalidadeLembrete:IntervaloVerificacaoSegundos`, default 3600s), calcula `hoje + AntecedenciaDias` (`MensalidadeLembrete:AntecedenciaDias`, default 5) e dispara e-mail para `Aluno.Email` e/ou `Aluno.EmailResponsavel` (os que existirem) de cada pagamento encontrado. Mesmo padrão técnico de `LembreteDispatcherService`/`MensalidadeDispatcherService`: lógica de processamento estática e testável sem `IServiceScopeFactory`, try/catch por tick sem derrubar o serviço, try/catch por pagamento (uma falha não impede os demais).
- Registrado em `InfrastructureServiceCollectionExtensions.cs` (`AddHostedService`) e `appsettings.json` (seção `MensalidadeLembrete`).
- **9 testes novos** (`tests/SPI.Application.Tests/MensalidadeLembrete/`): dentro/fora da janela de antecedência, status diferente de Pendente, pagamento manual sem vínculo nunca é considerado, fire-once (não reenvia no mesmo dia nem em dias seguintes), sem e-mail cadastrado não lança excessão, falha de envio para um pagamento não impede os demais.
- 2 fakes de `IPagamentoRepository` existentes (`AulaServiceFakes.cs`, `MensalidadeFakes.cs`) ganharam o stub do novo método da interface (`NotImplementedException`), para manter a compilação.

## Decisões tomadas e por quê

- **Fire-once em vez de retry indefinido**: uma vez tentado o envio (sucesso, falha, ou sem e-mail cadastrado), `LembreteVencimentoEnviado` vira `true` e o pagamento nunca mais é reconsiderado. Decisão técnica pequena: evita reenviar o mesmo lembrete todo dia indefinidamente até o pagamento ser dado como pago, o que seria mais invasivo que o pedido original ("avisando... do vencimento próximo", um aviso único). Se o negócio quiser reenvio periódico até o pagamento, isso é uma mudança de regra de negócio que não estava no card e portanto não foi assumida.
- **Filtro por `VinculoCobrancaId != null`** (em vez de também checar `CategoriaReceita.Nome == "Mensalidade"`): o próprio comentário já existente em `Pagamento.VinculoCobrancaId` (specs/039) documenta que esse campo só é preenchido pelo job de mensalidade — é o filtro mais direto e já estabelecido no código para "é uma cobrança automática de mensalidade".
- **Card sem spec formal**: optei por implementar direto (sem rodar o ciclo completo `/speckit-specify` → `/speckit-plan` → `/speckit-tasks`), seguindo o fluxo por card do processo autônomo ("entenda o que se pede e implemente exatamente o que a descrição pede"), já que a descrição do card era suficientemente específica e não exigia nenhuma decisão de escopo significativa.
- **Teste ao vivo contra o Gmail SMTP real, mas com destinatário seguro**: para testar de verdade (não só com fakes), criei um aluno **temporário e sintético** (RA `TESTE-LEMBRETE-001`, nome "Teste Lembrete Vencimento (APAGAR)") com `email = srlucasmtg@gmail.com` — nunca usei e-mail de aluno/responsável real, conforme a regra de segurança. O registro (aluno + vínculo + pagamento) foi apagado do banco ao final do teste.

## Como foi testado e o resultado

- `dotnet build`: sem erros.
- `dotnet test`: **250/250** (242 baseline + 8 novos desta suíte — o 9º teste listado acima é uma combinação dentro do mesmo arquivo, contabilizado nos 8).
- **Teste real de ponta a ponta**: subi a API localmente (`dotnet run`, ambiente Development) com `MensalidadeLembrete:IntervaloVerificacaoSegundos=5` via variável de ambiente, inserido o aluno de teste (e-mail `srlucasmtg@gmail.com`) com um vínculo de Mensalidade e um `Pagamento` Pendente vencendo em `hoje+5`. No primeiro tick do dispatcher, o log mostrou `Processados 1 lembretes de vencimento de mensalidade` sem nenhum erro/warning — ou seja, o envio via Gmail SMTP real foi bem-sucedido. Nos ticks seguintes (a cada 5s, por ~20s), `Processados 0`, confirmando o fire-once funcionando. Confirmei no banco que `lembrete_vencimento_enviado` foi para `1`. Processo finalizado e dados de teste apagados ao final.
- Não foi possível ler a caixa de entrada do Gmail para confirmar visualmente o e-mail recebido (sem ferramenta de acesso a e-mail nesta sessão) — a validação ficou pela ausência de erro/warning no log do envio SMTP real + o estado do banco, que é a evidência disponível de que o envio foi aceito pelo servidor SMTP do Gmail.

## Pendências, riscos e o que não foi feito

- Não vi a caixa de entrada do Gmail diretamente — se quiser confirmação visual, basta reexecutar um teste semelhante e checar `srlucasmtg@gmail.com`.
- O texto do e-mail é simples (nome do aluno, valor, data de vencimento) — não foi pedido um template mais elaborado no card, então mantive mínimo, no mesmo espírito do corpo de e-mail do `LembreteDispatcherService`.
- Card duplicado (`6acd960e7f7bf0d8426a51a3`) foi arquivado no Trello, não excluído permanentemente (reversível, caso haja engano).

## Commit

`feat(lembrete-mensalidade): lembrete de vencimento de mensalidade por e-mail (5 dias antes)`
