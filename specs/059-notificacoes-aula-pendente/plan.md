# Implementation Plan: Notificações de Status de Aula Pendente

**Branch**: `059-notificacoes-aula-pendente` | **Date**: 2026-10-19 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/059-notificacoes-aula-pendente/spec.md`

## Summary

Uma tabela nova, `notificacao`, com tipo, professora e aula, guarda **um registro por aula**. O índice único `(tipo, aula_id)` garante isso no banco.

`NotificacaoService.SincronizarAulasPendentesAsync(agora, professorId?)` concentra a regra numa função só (Princípio II):
- cria a notificação de cada aula pendente que ainda não tem;
- reativa a de uma aula que voltou a ficar pendente;
- resolve as que deixaram de estar pendentes.

Quem chama a sincronização:
- um `BackgroundService` a cada 5 minutos, para todas as professoras;
- o `GET /api/notificacoes`, antes de listar, só para a professora logada.

O mesmo serviço lista as notificações ativas da professora e marca uma como lida.

No frontend, o sino decorativo do cabeçalho vira o componente `NotificacoesSino`:
- contador de não lidas;
- lista;
- popup com o botão "Ir para a aula".

Ele consulta a API ao trocar de tela e a cada 60 segundos.

## Technical Context

**Language/Version**: C# / .NET 10 (backend), TypeScript / React 19 / Next.js 16 (frontend)

**Primary Dependencies**: EF Core (Pomelo MySQL), FluentValidation, xUnit — nenhuma nova

**Storage**: MySQL — tabela nova `notificacao` (`database/22_notificacao.sql`)

**Testing**: xUnit com fakes manuais (padrão do projeto); quickstart contra a API local

**Target Platform**: API ASP.NET Core + frontend Next.js

**Project Type**: web application

**Performance Goals**:
- A sincronização por professora faz poucas consultas indexadas: aulas pendentes da professora e notificações dessas aulas ou ativas.
- A rotina global repete isso para todas as professoras.

**Constraints**: uma notificação por aula, mesmo com execuções simultâneas (índice único + tratamento de chave duplicada); horário local (`DateTime.Now`, como as demais rotinas)

**Scale/Scope**: dezenas de aulas por professora por semana

## Constitution Check

- **I. Exclusão lógica**: notificação não é cadastro do usuário; ela é *resolvida* (`resolvida_em`), nunca apagada. Aula excluída logicamente resolve a notificação.
- **II. Regra única no backend**: o que é "pendente", a criação, a resolução e a mensagem ficam só no `NotificacaoService`/`AulaRepository`. O frontend só exibe e marca como lida.
- **III. Nada configurável sem efeito real**: o sino deixa de ser decorativo. As configurações novas (`Notificacoes:AulasPendentesDesde` e `Notificacoes:IntervaloVerificacaoSegundos`) são lidas e usadas de fato.
- **IV. Segredos**: endpoints sob `[Authorize(Roles = "Professor")]`; uma professora não lê nem marca a notificação de outra (404).
- **V. Documentação retroativa**: nenhuma spec anterior descreve o sino; não há nota a acrescentar.

**Resultado**: PASS.

## Project Structure

```text
database/22_notificacao.sql                                   # NOVO
src/SPI.Domain/Entities/Notificacao.cs                         # NOVO
src/SPI.Domain/Enums/TipoNotificacao.cs                        # NOVO
src/SPI.Domain/Repositories/INotificacaoRepository.cs          # NOVO
src/SPI.Domain/Repositories/IAulaRepository.cs                 # + ListarPendentesDeStatusAsync
src/SPI.Infrastructure/Persistence/Configurations/NotificacaoConfiguration.cs  # NOVO
src/SPI.Infrastructure/Persistence/SpiDbContext.cs             # DbSet
src/SPI.Infrastructure/Repositories/NotificacaoRepository.cs   # NOVO
src/SPI.Infrastructure/Repositories/AulaRepository.cs          # + ListarPendentesDeStatusAsync
src/SPI.Infrastructure/BackgroundServices/NotificacaoDispatcherService.cs     # NOVO
src/SPI.Infrastructure/DependencyInjection/InfrastructureServiceCollectionExtensions.cs  # registros + opções
src/SPI.Application/Notificacoes/{Dtos,Services}/...           # NOVO (service, options, response)
src/SPI.Application/DependencyInjection/ApplicationServiceCollectionExtensions.cs
src/SPI.Api/Controllers/NotificacoesController.cs              # NOVO
src/SPI.Api/appsettings.json                                   # seção Notificacoes
tests/SPI.Application.Tests/Notificacoes/...                   # NOVO
frontend/lib/api/notificacoes.ts                               # NOVO
frontend/components/layout/NotificacoesSino.tsx                # NOVO
frontend/components/layout/Header.tsx                          # usa o sino real
frontend/styles/dashboard.css                                  # estilos do painel/popup (só tokens)
```

**Structure Decision**: o padrão das features de backend (Domain → Infrastructure → Application → Api) e um componente de layout no frontend.

## Complexity Tracking

Sem violações.
