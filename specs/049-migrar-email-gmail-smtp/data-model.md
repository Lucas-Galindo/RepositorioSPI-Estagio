# Data Model: Migrar Envio de E-mail para Gmail SMTP

## Entidade existente modificada: `Lembrete` (spec 001), + 2 campos novos

| Campo | Tipo | Regra | Já existia? |
|---|---|---|---|
| `Id`, `TurmaId`, `HoraProgramada`, `Destinatarios`, `Canal`, `AntecedenciaHora`, `Ativo` | — | Inalterados | Sim |
| `Status` | `string` | Continua `"Pendente"`/`"Enviado"`/`"Falha"` — só muda **quando** vira `"Enviado"`/`"Falha"` (agora só depois que `DestinatariosTentados` cobre 100% dos destinatários originais, não no primeiro ciclo em que o lembrete é processado) | Sim, significado ajustado |
| `DestinatariosTentados` | `string?` (nullable, e-mails separados por `;`) | Lista de e-mails já tentados (sucesso ou falha) para este lembrete, acumulada entre ciclos; `NULL`/vazio = nenhum tentado ainda | **Novo** |
| `EnviosComSucesso` | `int`, padrão `0` | Contagem cumulativa de envios bem-sucedidos para este lembrete, entre ciclos — usada para decidir `"Enviado"` (`> 0`) vs `"Falha"` (`== 0`) quando todos os destinatários já foram tentados | **Novo** |

**Migração**: `database/18_lembrete_destinatarios_tentados.sql` — `ALTER TABLE lembrete ADD COLUMN destinatarios_tentados TEXT NULL, ADD COLUMN envios_com_sucesso INT NOT NULL DEFAULT 0;`

## Entidade renomeada (sem mudança de schema): `BrevoOptions` → `SmtpOptions`

| Campo | Tipo | Valor padrão novo |
|---|---|---|
| `SmtpHost` | `string` | `"smtp.gmail.com"` (era `"smtp-relay.brevo.com"`) |
| `SmtpPorta` | `int` | `587` (inalterado) |
| `SmtpUsuario`, `SmtpChave`, `RemetenteEmail` | `string` | Vazios por padrão — valores reais em User Secrets, chave de seção passa de `Brevo:*` para `Smtp:*` (passo manual do usuário) |
| `RemetenteNome` | `string` | Inalterado |

## Fluxo de processamento por ciclo (`LembreteDispatcherService`)

| Passo | Efeito |
|---|---|
| Tick do `PeriodicTimer` | Busca lembretes `Pendente` e vencidos, ordenados por `HoraProgramada` ascendente (R3) |
| Para cada lembrete, na ordem | Calcula `destinatarios = ObterEmailsDestinatarios(lembrete) - DestinatariosTentados` (já tentados são ignorados) |
| Envio | Envia até `min(destinatarios.Count, orçamentoRestante)` e-mails; acumula tentados em `DestinatariosTentados`, sucessos em `EnviosComSucesso`; decrementa o orçamento do ciclo |
| Orçamento do ciclo chega a 0 | Para de processar lembretes (os restantes ficam para o próximo tick) |
| `DestinatariosTentados` cobre 100% dos destinatários originais | `Status` vira `"Enviado"` (se `EnviosComSucesso > 0`) ou `"Falha"` (se `0`) — lembrete não é mais buscado nos ciclos seguintes |
| `DestinatariosTentados` ainda não cobre todos | `Status` continua `"Pendente"` — retomado no próximo tick, a partir de onde parou |

## Nenhuma mudança de API externa

Nenhum endpoint HTTP novo ou alterado — `LembreteDispatcherService` é um `BackgroundService` interno, sem superfície de API.
