# Implementation Plan: Chamada Só nos Detalhes da Aula e Aviso de Aula Sem Frequência

**Branch**: `064-chamada-so-nos-detalhes` | **Date**: 2026-10-31 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/064-chamada-so-nos-detalhes/spec.md`

## Summary

Hoje a aula é tratada como "realizada" só pela **data** (spec 055) e o servidor aceita a chamada a qualquer momento. A mudança, em quatro frentes pequenas:

1. **Agendar não faz chamada**: `AulaRequest` perde `presencas`; `AulaService.CadastrarAsync` sempre cria "Agendada" (sem frequência nem cobrança); o formulário perde a seção "Presença" e o botão é sempre "Agendar aula".
2. **Chamada só nos detalhes, só depois do início**: uma regra pura e única (`RegraChamada`) decide, no servidor e no fuso `America/Sao_Paulo` (via `TimeProvider`), se a chamada está liberada (`agora >= data + hora de início`). `RegistrarSessaoAsync` recusa com 409 antes disso; a resposta da aula informa `chamadaLiberada` e `segundosParaChamada`, e a tela só exibe (e refaz a busca na hora certa).
3. **Aviso de correção sem corte**: o conjunto de `AulaPendente` perde o filtro de 19/10/2026 (e a configuração `AulasPendentesDesde`); texto passa a falar em frequência.
4. **Auditoria**: coluna `aula.frequencia_registrada_em` (migração 26), gravada no único ponto que realiza a aula, e uma consulta SQL **somente de leitura** das realizadas antes do início.

Abordagem técnica e alternativas descartadas: [research.md](research.md).

## Technical Context

**Language/Version**: C# 14 / .NET 10 (backend); TypeScript 5 / Next.js 16 / React 19 (frontend)

**Primary Dependencies**: ASP.NET Core Web API, EF Core (Pomelo MySQL), FluentValidation; `TimeProvider` da plataforma (sem pacote novo); `ErrosDeNegocioFilter` da spec 065 para o 409

**Storage**: MySQL (`spi_db`); uma coluna nova `aula.frequencia_registrada_em DATETIME NULL` (migração `database/26_…`)

**Testing**: xUnit com fakes manuais (`tests/SPI.Application.Tests`); `TimeProvider` falso escrito à mão; validação final pela API autenticada e por testes visuais no Chrome (como nas specs 063 e 065)

**Target Platform**: Servidor web (Windows em desenvolvimento; Linux possível) + navegador desktop

**Project Type**: Aplicação web (API `src/` + frontend `frontend/`)

**Performance Goals**: sem meta nova; `RegraChamada` é uma comparação de datas calculada por aula na resposta (listas de aulas de um mês, dezenas de itens)

**Constraints**: fuso fixo `America/Sao_Paulo` decidido no servidor (FR-007); nenhum dado existente alterado ou apagado (FR-015/FR-016); sem `DateTime.Now` novo nos pontos da regra; sem `try/catch` novo em controller (spec 065)

**Scale/Scope**: sistema de uma professora; ~10 arquivos de backend, ~6 de frontend, 1 migração, 1 consulta, 3 pacotes de testes

## Constitution Check

*GATE: avaliado antes da pesquisa e reavaliado depois do desenho (`.specify/memory/constitution.md`).*

| Princípio | Avaliação |
|---|---|
| I. Exclusão lógica | ✅ Nada é apagado; a auditoria é só leitura; a aula excluída continua apenas inativa (e passa a ser recusada na chamada) |
| II. Validação única no backend | ✅ A regra "chamada liberada" existe **uma vez** (`RegraChamada`), usada por `RegistrarSessaoAsync` (recusa) e por `Mapear` (informa). O frontend só exibe `chamadaLiberada` e **não compara horas** |
| III. Sem capacidade configurável sem implementação | ✅ Reforçado: `Notificacoes:AulasPendentesDesde` é **removida** em vez de ficar sem efeito; `Presencas` sai do contrato de criação |
| IV. Segredos | ✅ Sem impacto; o login de teste só vai por variável de ambiente |
| V. Documentação como registro vinculante | ✅ Notas nas specs 055 (FR-001 a FR-007) e 059 (FR-004) apontando para a 064 (FR-017); relatório ao final |

**Re-avaliação pós-design**: sem violações. Complexity Tracking vazio.

## Project Structure

### Documentation (this feature)

```text
specs/064-chamada-so-nos-detalhes/
├── spec.md
├── plan.md                         # este arquivo
├── research.md                     # decisões R1–R12
├── data-model.md                   # coluna nova, campos calculados, estados
├── quickstart.md                   # validação (testes, API, telas, auditoria)
├── contracts/
│   ├── aulas-api.md                # criar sem presença; chamadaLiberada; 409 da chamada
│   └── notificacoes-api.md         # conjunto sem corte e texto do aviso
├── checklists/requirements.md
└── tasks.md                        # /speckit-tasks (não criado aqui)
```

### Source Code (repository root)

```text
database/
├── 26_aula_frequencia_registrada_em.sql      # NOVO (migração)
└── consultas/aulas_realizadas_antes_do_inicio.sql   # NOVO (somente leitura)

src/
├── SPI.Domain/
│   ├── Entities/Aula.cs                      # + FrequenciaRegistradaEm
│   └── Repositories/IAulaRepository.cs       # ListarPendentesDeStatusAsync sem 'desde'
├── SPI.Application/
│   ├── Common/RelogioExtensions.cs           # NOVO: AgoraEmSaoPaulo(this TimeProvider)
│   ├── Aulas/
│   │   ├── Dtos/AulaRequest.cs               # - Presencas
│   │   ├── Dtos/AulaResponse.cs              # + ChamadaLiberada, SegundosParaChamada
│   │   └── Services/
│   │       ├── RegraChamada.cs               # NOVO: regra pura
│   │       └── AulaService.cs                # Cadastrar sem retroativo; RegistrarSessao confere início e Ativo; grava FrequenciaRegistradaEm; Mapear com agora
│   ├── Notificacoes/Services/
│   │   ├── NotificacoesOptions.cs            # - AulasPendentesDesde
│   │   └── NotificacaoService.cs             # sem 'desde'; novo texto
│   └── DependencyInjection/ApplicationServiceCollectionExtensions.cs   # TimeProvider.System
├── SPI.Infrastructure/
│   ├── Persistence/Configurations/AulaConfiguration.cs   # + coluna
│   ├── Repositories/AulaRepository.cs        # ListarPendentesDeStatusAsync sem filtro de corte
│   └── BackgroundServices/NotificacaoDispatcherService.cs # agora em SP
└── SPI.Api/
    ├── appsettings.json                      # - Notificacoes:AulasPendentesDesde
    └── Controllers/NotificacoesController.cs # agora em SP (TimeProvider injetado)

frontend/
├── components/aulas/
│   ├── AulaForm.tsx                          # sem retroativa/presença
│   └── PresencaChecklist.tsx                 # REMOVIDO (sem uso)
├── components/layout/NotificacoesSino.tsx    # título "Aula sem frequência"
├── lib/api/aulas.ts                          # - presencas; + chamadaLiberada, segundosParaChamada
└── app/(app)/aulas/[id]/page.tsx             # bloqueio, mensagem e nova busca na hora

tests/SPI.Application.Tests/
├── Aulas/RelogioFalso.cs                     # NOVO: TimeProvider de teste
├── Aulas/RegraChamadaTests.cs                # NOVO
├── Aulas/AulaServiceChamadaTests.cs          # NOVO
├── Aulas/AulaServiceCadastrarRetroativoTests.cs   # reescrito (nasce Agendada)
├── Aulas/*  (registrar sessão)               # arrange com relógio fixo; Asserts intactos
└── Notificacoes/*                            # sem 'desde'; casos sem corte e em andamento

specs/055-*/spec.md, specs/059-*/spec.md      # notas (FR-017)
docs/relatorios/<data>-chamada-so-nos-detalhes.md
```

**Structure Decision**: aplicação web já existente (API `src/` em camadas Domain/Application/Infrastructure/Api + `frontend/`). Os únicos arquivos novos de código são `RelogioExtensions.cs`, `RegraChamada.cs`, as duas peças de SQL e os testes; nenhum projeto novo.

## Ordem sugerida de implementação (insumo do /speckit-tasks)

1. Migração 26 + entidade + mapeamento (base).
2. `RelogioExtensions` + registro do `TimeProvider` + `RegraChamada` com testes (US2 base).
3. US1: criação sem presença (backend, depois formulário).
4. US2: `RegistrarSessaoAsync` (início, `Ativo`, `FrequenciaRegistradaEm`), `AulaResponse`, tela de detalhes.
5. US3: notificações sem corte, texto e título; remover a opção de configuração.
6. US4: consulta de auditoria e conferência no banco local.
7. Notas nas specs 055/059, relatório, Trello.

## Verificações "MUST NOT" que o /speckit-tasks deve cobrir (regra do CLAUDE.md)

| Requisito | Verificação exigida |
|---|---|
| FR-001 formulário **sem** Presença/“A data já chegou…” | leitura do diff de `AulaForm.tsx` + `grep` pelos textos removidos; `git status --porcelain -- frontend/components` (só `PresencaChecklist.tsx` removido, nenhum arquivo novo) |
| FR-002 criação **sem** frequência/cobrança/presenças | testes de serviço + leitura do diff de `CadastrarAsync` |
| FR-004 comparação **não** só pela data | `RegraChamadaTests` (minuto exato, hoje antes/depois) + leitura do diff de `RegraChamada` |
| FR-007 tela **não** decide por relógio do navegador | leitura do diff de `aulas/[id]/page.tsx` e `grep` por `new Date`/`Date.now`/`dataIsoLocal` nos arquivos tocados |
| FR-009 aula já realizada **não** aceita nova chamada | teste existente de status + novo caso após a chamada |
| FR-011 em andamento **não** gera aviso | teste de notificação |
| FR-015 coluna **não** preenchida retroativamente | leitura da migração (sem `UPDATE`) e `SELECT` no banco local depois |
| FR-016 auditoria **não** altera/apaga | leitura da consulta e contagens iguais antes/depois |
| Sem arquivo novo indevido | `git status --porcelain -- src tests frontend database` |

## Complexity Tracking

Sem violações da constituição a justificar.
