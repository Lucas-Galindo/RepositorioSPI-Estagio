# Implementation Plan: Migrar Envio de E-mail para Gmail SMTP

**Branch**: `049-migrar-email-gmail-smtp` | **Date**: 2026-10-12 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/049-migrar-email-gmail-smtp/spec.md`

## Summary

Renomeia `BrevoOptions`/`BrevoEmailSender`/seção `"Brevo"` para `SmtpOptions`/`SmtpEmailSender`/seção `"Smtp"` (mesma implementação MailKit, só troca de host/credencial — nenhuma biblioteca nova), e ajusta `LembreteDispatcherService` para respeitar um limite configurável de e-mails por ciclo (`Lembretes:MaxEmailsPorCiclo`, padrão 10), retomando exatamente de onde parou entre ciclos. Para isso, `Lembrete` ganha 2 campos novos (`DestinatariosTentados`, `EnviosComSucesso`) que guardam o progresso parcial de um lembrete cujos destinatários não couberam todos no orçamento de um único ciclo — sem isso, não seria possível cumprir FR-004 de verdade (retomar destinatário por destinatário, não só lembrete por lembrete).

## Technical Context

**Language/Version**: C# / .NET 10 (backend) — nenhuma mudança de frontend.

**Primary Dependencies**: Nenhuma nova — MailKit já é a biblioteca usada (`BrevoEmailSender`/futuro `SmtpEmailSender`), só muda o host/credencial de destino.

**Storage**: MySQL — 2 colunas novas em `lembrete` (`destinatarios_tentados`, `envios_com_sucesso`), nenhuma tabela nova.

**Testing**: xUnit com fakes manuais (padrão do projeto) para a lógica de orçamento por ciclo do `LembreteDispatcherService`. Renomeação de `BrevoOptions`→`SmtpOptions` não tem teste próprio (é troca de nome, comportamento idêntico).

**Target Platform**: Backend ASP.NET Core, mesma stack já em produção/dev local.

**Project Type**: Web application — esta feature é backend-only (nenhum componente de frontend muda).

**Performance Goals**: Sem meta nova — o limite por ciclo é sobre *ritmo* de envio, não sobre throughput do sistema.

**Constraints**:
- FR-003/FR-004: o limite por ciclo MUST ser configurável (`Lembretes:MaxEmailsPorCiclo`, padrão 10 se ausente) e MUST retomar destinatário por destinatário entre ciclos, priorizando por `HoraProgramada` ascendente (quem está esperando mais tempo primeiro) — exige os 2 campos novos em `Lembrete` (ver Key Entities do spec).
- FR-005: MUST NOT haver `Task.Delay` bloqueante — o espaçamento vem só do intervalo entre ciclos do `PeriodicTimer` já existente.
- FR-006: MUST NOT alterar o conteúdo/formato dos 3 tipos de e-mail, nem a regra de elegibilidade de lembretes (spec 001) — só a cadência de envio dos lembretes e o provedor por trás dos 3.
- FR-007: MUST renomear toda referência a "Brevo" (`BrevoOptions`→`SmtpOptions`, `BrevoEmailSender`→`SmtpEmailSender`, seção `"Brevo"`→`"Smtp"` em `appsettings.json`) — mesma implementação, só nomes.
- Princípio IV (segredos seguros por padrão): `SmtpUsuario`/`SmtpChave`/`RemetenteEmail` continuam em User Secrets (nunca em `appsettings.json`), só a chave de seção muda de `Brevo:*` para `Smtp:*` — requer que o usuário reconfigure os 3 valores em User Secrets manualmente com as credenciais da conta Gmail dedicada (fora do escopo desta feature, já assumido no spec).

**Scale/Scope**: 5 arquivos backend renomeados/modificados (`BrevoOptions.cs`→`SmtpOptions.cs`, `BrevoEmailSender.cs`→`SmtpEmailSender.cs`, `InfrastructureServiceCollectionExtensions.cs`, `appsettings.json`, `IEmailSender.cs` só comentário) + `LembreteDispatcherService.cs` (lógica de orçamento por ciclo) + `Lembrete.cs` (2 campos novos) + `LembreteRepository.cs` (ordenação por `HoraProgramada`) + 1 script de migração (`database/18_*.sql`, 2 colunas novas) + testes novos para a lógica de orçamento.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Princípio | Aplica-se? | Avaliação |
|---|---|---|
| I. Exclusão Lógica, Nunca Física | Não | Nenhuma exclusão de registro envolvida. |
| II. Validação de Negócio Única e Centralizada no Backend | Sim, preservado | A regra de elegibilidade de lembretes (spec 001) e o cálculo de destinatários (`ObterEmailsDestinatarios`) não mudam — só a cadência de envio é nova, e vive inteiramente no backend (nenhuma lógica duplicada no frontend, que nem participa desta feature). |
| III. Nenhuma Capacidade Configurável Sem Implementação Real (NON-NEGOTIABLE) | Sim | `Lembretes:MaxEmailsPorCiclo` é uma configuração com implementação real completa (o limite é de fato respeitado) — não é uma opção exposta sem efeito. |
| IV. Autenticação e Segredos Seguros por Padrão | Sim | `SmtpUsuario`/`SmtpChave`/`RemetenteEmail` continuam fora do controle de versão (User Secrets) — só a chave de seção muda de nome; nenhuma credencial nova é versionada. |
| V. Documentação Retroativa como Registro Histórico Vinculante | Não | Feature nova; estende as specs 001 (lembretes) e 043 (achado do bloqueio de rede da Brevo) sem contradizer nenhuma delas — a mudança de provedor é uma evolução consciente, não uma correção de divergência de documentação. |

**Resultado**: Sem violação.

## Project Structure

### Documentation (this feature)

```text
specs/049-migrar-email-gmail-smtp/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── checklists/requirements.md
└── tasks.md             # Phase 2 output (/speckit-tasks - NOT created by /speckit-plan)
```

Sem diretório `contracts/` — nenhum contrato de API externo novo ou alterado (a mudança é inteiramente interna ao backend: configuração, nome de classe, e um serviço de background).

### Source Code (repository root)

```text
src/
├── SPI.Application/
│   └── Common/IEmailSender.cs                    # MODIFICADO: comentário (não cita mais "Brevo" como exemplo)
├── SPI.Domain/
│   └── Entities/Lembrete.cs                      # MODIFICADO: + DestinatariosTentados, EnviosComSucesso
├── SPI.Infrastructure/
│   ├── Email/
│   │   ├── BrevoOptions.cs → SmtpOptions.cs       # RENOMEADO: classe, SectionName, SmtpHost padrão (smtp.gmail.com)
│   │   └── BrevoEmailSender.cs → SmtpEmailSender.cs  # RENOMEADO: classe (implementação idêntica)
│   ├── DependencyInjection/InfrastructureServiceCollectionExtensions.cs  # MODIFICADO: registra SmtpOptions/SmtpEmailSender
│   ├── BackgroundServices/LembreteDispatcherService.cs  # MODIFICADO: orçamento por ciclo, retomada parcial
│   └── Repositories/LembreteRepository.cs          # MODIFICADO: ListarPendentesVencidosAsync ordena por HoraProgramada
└── SPI.Api/appsettings.json                        # MODIFICADO: seção "Brevo" → "Smtp", SmtpHost → smtp.gmail.com

database/
└── 18_lembrete_destinatarios_tentados.sql           # NOVO: 2 colunas em lembrete

tests/SPI.Application.Tests/
└── Lembretes/ (ou pasta equivalente já existente)
    └── LembreteDispatcherOrcamentoTests.cs (ou equivalente)  # NOVO: testa o orçamento por ciclo e a retomada parcial
```

**Structure Decision**: Nenhum diretório novo. A renomeação é 1:1 (mesmo arquivo, nome novo); a lógica de orçamento vive dentro do serviço de background já existente; os 2 campos novos vivem na entidade já existente.

## Post-Design Constitution Check

*Re-avaliação após Phase 1 (research.md, data-model.md, quickstart.md).*

- Princípio II: `data-model.md` confirma que `ObterEmailsDestinatarios` (cálculo de quem recebe) não muda — só o ponto em que o resultado é filtrado pelo que já foi tentado e pelo orçamento restante.
- Princípio IV: `research.md` documenta exatamente quais chaves de User Secrets o usuário precisa reconfigurar (passo manual, fora do código desta feature).
- Nenhum dos outros princípios é tocado pelo design.

**Resultado**: Sem violação bloqueante.

## Complexity Tracking

Não aplicável — nenhuma violação identificada em nenhum dos dois Constitution Checks. Os 2 campos novos em `Lembrete` são a única adição de estado desta feature, e são diretamente exigidos por FR-004 (retomada destinatário por destinatário) — não é complexidade além do necessário.
