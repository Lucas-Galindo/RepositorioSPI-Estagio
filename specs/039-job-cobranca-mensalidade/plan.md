# Implementation Plan: Job de Cobrança Automática de Mensalidade

**Branch**: `039-job-cobranca-mensalidade` | **Date**: 2026-09-22 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/039-job-cobranca-mensalidade/spec.md`

## Summary

Criar `MensalidadeDispatcherService`, um `BackgroundService` no mesmo padrão de `LembreteDispatcherService`: verifica periodicamente (intervalo configurável) se o momento atual é o último dia do mês às 23h ou depois; quando sim, para cada `VinculoCobranca` ativo de Modalidade Mensalidade, gera uma `Pagamento` (Status "Pendente", Valor do vínculo, Competência do mês, vencimento no 1º dia do mês seguinte, categoria "Mensalidade" nova), sem vínculo a nenhuma `Aula`. Idempotência garantida por uma FK nova `Pagamento.VinculoCobrancaId` + índice único `(vinculo_cobranca_id, competencia)`, que também fecha totalmente a exceção EX-001 (`specs/037-vinculo-cobranca/plan.md`) — depois desta feature, todas as três modalidades (Avulsa, Pacote via specs/038; Mensalidade aqui) têm efeito automático real.

## Technical Context

**Language/Version**: C# / .NET 10 (`net10.0`) — mesma versão já usada no projeto.

**Primary Dependencies**: ASP.NET Core `BackgroundService`/`IHostedService`, EF Core + Pomelo MySQL, `Microsoft.Extensions.Configuration`/`Logging` — todas já usadas por `LembreteDispatcherService`. Nenhuma dependência nova.

**Storage**: MySQL. Migração nova `database/15_job_cobranca_mensalidade.sql`: (1) seed da categoria de receita "Mensalidade" (mesmo padrão de `10_financeiro_contas.sql`); (2) `ALTER TABLE pagamento ADD COLUMN vinculo_cobranca_id INT NULL` + FK para `vinculo_cobranca(id)` + índice único `(vinculo_cobranca_id, competencia)` — sustenta a idempotência de FR-004 no próprio banco, não só na aplicação (mesmo espírito do índice único de `vinculo_cobranca` em specs/037).

**Testing**: xUnit (`tests/SPI.Application.Tests`), fakes manuais. Primeira suíte para o novo serviço em `tests/SPI.Infrastructure.Tests/` (ver Nota abaixo) para a lógica de disparo/geração; e testes em `tests/SPI.Application.Tests` para qualquer regra que viva na camada de repositório/aplicação.

**Nota sobre localização dos testes**: `LembreteDispatcherService` (o próprio padrão que este job segue) não tem nenhum teste automatizado hoje — não há precedente de teste de `BackgroundService` no projeto, nem um projeto `SPI.Infrastructure.Tests`. Para não deixar a lógica de disparo (a parte mais fácil de acertar errado) sem cobertura, a condição "é o momento de disparar?" é extraída para um método estático puro e testável (`MensalidadeDispatcherService.DeveDispararNesteMomento(DateTime)`), testável diretamente de `tests/SPI.Application.Tests` (referenciando `SPI.Infrastructure` como já fazem os testes de repositório) sem precisar instanciar o `BackgroundService` inteiro nem mockar `DateTime.Now`. Ver research.md R7.

**Target Platform**: ASP.NET Core Web API (`SPI.Infrastructure`, hosted service), sem impacto em `SPI.Api` (nenhum endpoint novo) nem em `frontend/` além do ajuste textual de FR-013.

**Project Type**: Web application (estrutura já existente do repositório).

**Performance Goals**: Sem meta nova — a verificação de horário é uma comparação de `DateTime` em memória (desprezível); só quando cai na janela de disparo é que o banco é consultado, no máximo uma vez por vínculo Mensalidade ativo (tipicamente poucas dezenas, dado o porte da aplicação).

**Constraints**: FR-009/FR-010 (nenhuma sobreposição com specs/038: Pacote/Avulsa continuam exclusivamente por presença); FR-011 (falha em um vínculo não derruba o serviço nem impede os demais — mesmo padrão de isolamento por item já usado em `LembreteDispatcherService`).

**Scale/Scope**: 1 `BackgroundService` novo, 1 migração de banco (categoria + coluna + índice), 2 métodos novos de repositório (`IVinculoCobrancaRepository.ListarAtivosPorModalidadeAsync`, `IPagamentoRepository.ExisteMensalidadeGeradaAsync`), 1 registro em DI, 1 configuração nova em `appsettings.json`, 1 ajuste textual no frontend (FR-013). Nenhuma mudança de contrato de API.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Princípio | Aplica-se? | Avaliação |
|---|---|---|
| I. Exclusão Lógica, Nunca Física | Não diretamente | Nenhuma exclusão de registro nesta feature. **Conforme.** |
| II. Validação de Negócio Única e Centralizada no Backend | Sim | A decisão de "quando disparar" e "quem cobrar" vive inteiramente em `MensalidadeDispatcherService` (um único ponto de entrada, sem duplicação em nenhum controller/frontend). A idempotência é reforçada em duas camadas (checagem na aplicação + índice único no banco), mesmo padrão já usado em specs/037 para a unicidade de `VinculoCobranca`. **Conforme.** |
| III. Nenhuma Capacidade Configurável Sem Implementação Real (NON-NEGOTIABLE) | Sim — **fecha a exceção EX-001 por completo** | `specs/037-vinculo-cobranca/plan.md` registra EX-001 como "🟡 PARCIALMENTE RESOLVIDA (specs/038) — Avulsa e Pacote com efeito real; Mensalidade pendente do job futuro". Esta é exatamente essa fatia. Como o compromisso original previa remover o aviso da UI "nesse momento", o plano inclui FR-013 (spec.md, ajustada nesta sessão) — sem isso, o aviso continuaria dizendo algo agora falso para vínculos Mensalidade. Ação de tarefa: atualizar o registro de EX-001 em specs/037 para 🔒 RESOLVIDA. |
| IV. Autenticação e Segredos Seguros por Padrão | Não | Nenhum endpoint novo, nenhuma mudança de autenticação/segredo. O job roda em segundo plano, sem superfície de API. |
| V. Documentação Retroativa como Registro Histórico Vinculante | Não | Nenhuma spec retroativa documenta o comportamento de Mensalidade como definitivamente "sem efeito" — o `spec.md` desta feature é quem passa a valer. |

**Resultado**: Sem violação nova. EX-001 é fechada por completo por esta feature (não apenas atualizada, como em specs/038).

## Project Structure

### Documentation (this feature)

```text
specs/039-job-cobranca-mensalidade/
├── plan.md              # This file (/speckit-plan command output)
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output (nota — sem contrato de API novo)
├── checklists/requirements.md
└── tasks.md             # Phase 2 output (/speckit-tasks command - NOT created by /speckit-plan)
```

### Source Code (repository root)

```text
database/
└── 15_job_cobranca_mensalidade.sql          # NOVO: categoria "Mensalidade" + pagamento.vinculo_cobranca_id + indice unico

src/
├── SPI.Domain/
│   ├── Entities/Pagamento.cs                # + VinculoCobrancaId (int?) + navegacao VinculoCobranca
│   └── Repositories/
│       ├── IVinculoCobrancaRepository.cs    # + ListarAtivosPorModalidadeAsync(ModalidadeCobranca, ct)
│       └── IPagamentoRepository.cs          # + ExisteMensalidadeGeradaAsync(vinculoCobrancaId, competencia, ct)
├── SPI.Infrastructure/
│   ├── BackgroundServices/
│   │   └── MensalidadeDispatcherService.cs  # NOVO -- mesmo padrao de LembreteDispatcherService.cs
│   ├── Persistence/Configurations/
│   │   └── PagamentoConfiguration.cs        # + mapeamento de VinculoCobrancaId/FK
│   ├── Repositories/
│   │   ├── VinculoCobrancaRepository.cs     # implementa ListarAtivosPorModalidadeAsync
│   │   └── PagamentoRepository.cs           # implementa ExisteMensalidadeGeradaAsync
│   └── DependencyInjection/
│       └── InfrastructureServiceCollectionExtensions.cs   # + AddHostedService<MensalidadeDispatcherService>()
└── SPI.Api/
    └── appsettings.json                     # + "Mensalidade": { "IntervaloVerificacaoSegundos": 3600 }

specs/037-vinculo-cobranca/
└── plan.md                                  # atualiza o registro de EX-001 para 🔒 RESOLVIDA

frontend/
└── components/alunos/VinculosCobrancaSection.tsx   # ajusta o texto do aviso (FR-013): nao mencionar
                                                       # mais Mensalidade como "sem efeito automatico"

tests/
└── SPI.Application.Tests/
    └── Mensalidade/
        ├── MensalidadeDispatcherServiceDeveDispararTests.cs   # o metodo estatico puro (research.md R7)
        └── ... (demais testes de geracao/idempotencia, definidos em tasks.md)
```

**Structure Decision**: Mudança isolada a `SPI.Domain` (1 propriedade nova em entidade existente, 2 assinaturas de repositório), `SPI.Infrastructure` (1 `BackgroundService` novo, 2 implementações de repositório, 1 registro de DI), `SPI.Api` (1 chave de configuração) e um ajuste textual mínimo em `frontend/` — sem nenhum endpoint novo ou alterado. Primeira suíte de teste para a lógica de um `BackgroundService` no projeto, resolvida extraindo a condição de disparo para um método estático puro (research.md R7), em vez de introduzir um framework de teste de `IHostedService`.

## Post-Design Constitution Check

*Re-avaliação após Phase 1 (research.md, data-model.md, quickstart.md).*

- Princípio II: confirmado pelo design — `data-model.md` mostra que a idempotência tem duas camadas (aplicação + índice único `uq_pagamento_vinculo_competencia`), mesmo padrão já validado em specs/037 para `VinculoCobranca`.
- Princípio III / EX-001: `research.md` (R1-R6) e `data-model.md` confirmam que Mensalidade passa a ter efeito real completo (geração de cobrança) e que FR-013 cobre a atualização do aviso — a exceção fecha por completo, sem ressalva.
- Nenhuma violação nova introduzida pelo design.

**Resultado**: Sem violação bloqueante. Nenhuma linha nova em Complexity Tracking.

## Complexity Tracking

Não aplicável — nenhuma violação nova identificada em nenhum dos dois Constitution Checks. EX-001 (já existente, registrada em specs/037) é fechada por esta feature, não recriada.
