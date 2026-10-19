# Implementation Plan: Centro de Notificações e Aba Lembretes

**Branch**: `060-centro-notificacoes` | **Date**: 2026-10-19 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/060-centro-notificacoes/spec.md`

## Summary

Três frentes, com a regra de cada uma num lugar só:

1. **Notificações** (`NotificacaoService.SincronizarAsync`).
   - Os tipos de aula (pendente, da 059, e futura, nova) usam o mesmo laço: criar, reativar ou resolver por aula.
   - O tipo "pagamento pendente" faz o mesmo por aluno.
   - A tabela `notificacao` ganha `aluno_id` e o índice único `(tipo, professor_id, aluno_id)`.
2. **E-mail semanal** (`LembretePagamentoService.ProcessarAsync`).
   - A regra editável fica em `regra_lembrete_pagamento` (uma linha).
   - Cada envio é registrado em `envio_lembrete_pagamento`, com índice único `(aluno_id, data_envio)`: no máximo um registro por aluno por dia de envio, atualizado nas novas tentativas.
   - Uma rotina nova chama esse serviço periodicamente.
3. **Segurança de e-mail** (`SmtpEmailSender`). Em Development, todo e-mail vai para `Smtp:RedirecionarTodosPara`, guardado em User Secrets. Sem esse valor, o envio é recusado.

No frontend:
- o sino ganha rótulo por tipo, filtro e popup específico de pagamento;
- a aba Lembretes ganha a seção "Pagamento pendente", com a regra e o histórico.

## Technical Context

**Language/Version**: C# / .NET 10; TypeScript / React 19 / Next.js 16

**Primary Dependencies**: nenhuma nova (MailKit já usado)

**Storage**: MySQL — `database/23_centro_notificacoes.sql`

**Testing**: xUnit com fakes. Notificações (aula futura, pagamento), envio semanal (dia, hora, deduplicação, nova tentativa, sem destinatário, regra desligada) e redirecionamento de e-mail, como função pura. Quickstart contra a API local.

**Project Type**: web application

**Constraints**:
- **Sem duplicidade**: no máximo uma notificação por chave e um envio por aluno por dia de envio, garantidos por índice único no banco.
- **E-mail em desenvolvimento**: nunca chega a destinatário real.

**Scale/Scope**: dezenas de alunos; uma execução da rotina a cada 5 min

## Constitution Check

- **I. Exclusão lógica**: notificação resolvida, nunca apagada. Os registros de envio são histórico e nunca são apagados.
- **II. Regra no backend**: pendência, janela de aula futura, dia e hora de envio e deduplicação ficam só nos services. O frontend exibe e edita a regra.
- **III. Nada configurável sem efeito**:
  - a regra da aba Lembretes (ligada, dia, hora) é lida pela rotina a cada execução;
  - a antecedência do aviso de aula futura é lida da configuração;
  - o redirecionamento de e-mail é aplicado em todo envio.
- **IV. Segredos**: o endereço de teste fica em User Secrets (dev), nunca em `appsettings.json`. Endpoints sob o perfil Professor.
- **V. Documentação retroativa**: a spec 059 recebe nota dizendo que o sino virou centro com vários tipos (060).

**Resultado**: PASS.

## Project Structure

```text
database/23_centro_notificacoes.sql                                       # NOVO
src/SPI.Domain/Enums/TipoNotificacao.cs                                   # + AulaFutura, PagamentoPendente
src/SPI.Domain/Entities/{Notificacao(+AlunoId),RegraLembretePagamento,EnvioLembretePagamento}.cs
src/SPI.Domain/Repositories/{INotificacaoRepository,IAulaRepository,IPagamentoRepository,ILembretePagamentoRepository}.cs
src/SPI.Infrastructure/Persistence/Configurations/{Notificacao,RegraLembretePagamento,EnvioLembretePagamento}Configuration.cs
src/SPI.Infrastructure/Repositories/{Notificacao,Aula,Pagamento,LembretePagamento}Repository.cs
src/SPI.Infrastructure/BackgroundServices/{NotificacaoDispatcherService,LembretePagamentoDispatcherService}.cs
src/SPI.Infrastructure/Email/{SmtpOptions,SmtpEmailSender}.cs                # redirecionamento em dev
src/SPI.Application/Notificacoes/...                                       # tipos novos + DTO de pagamento
src/SPI.Application/Lembretes/{Dtos,Services}/LembretePagamento*.cs        # NOVO
src/SPI.Api/Controllers/LembretesController.cs                            # regra + envios
src/SPI.Api/appsettings.json                                               # Notificacoes:AulaFutura*, LembretePagamento:Intervalo
tests/SPI.Application.Tests/{Notificacoes,Lembretes,Email}/...
frontend/lib/api/{notificacoes,lembretes}.ts
frontend/components/layout/NotificacoesSino.tsx
frontend/components/lembretes/LembretePagamentoSection.tsx                # NOVO
frontend/app/(app)/lembretes/page.tsx
frontend/styles/dashboard.css
```

## Complexity Tracking

Sem violações.
