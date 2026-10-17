# Implementation Plan: Auditoria de Aulas Dadas por Vínculo de Cobrança

**Branch**: `052-auditoria-vinculo-cobranca` | **Date**: 2026-10-12 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/052-auditoria-vinculo-cobranca/spec.md`

## Summary

Reformular `VinculoCobranca`/`AulaAluno` para que "quantas aulas já foram dadas" seja sempre calculável e auditável, para Pacote e Mensalidade: (1) `VinculoCobranca` ganha `TotalAulasPacote` (tamanho original do Pacote, imutável por consumo); (2) `AulaAluno` ganha `VinculoCobrancaId`, preenchido automaticamente no mesmo ponto em que `AulaService.GerarContasAReceberAsync` já resolve o vínculo ativo de cada presença, para as 3 modalidades; (3) a contagem mensal de Mensalidade é derivada por consulta (sem job novo, sem campo de saldo); (4) edição de vínculo passa a bloquear reduzir o total/teto abaixo do já consumido. Puramente backend — nenhum endpoint novo, apenas 2 campos aditivos nos contratos já existentes de `VinculoCobranca`.

## Technical Context

**Language/Version**: C# / .NET (mesma versão já usada no projeto, ver `src/SPI.Api/SPI.Api.csproj`)

**Primary Dependencies**: ASP.NET Core Web API, Entity Framework Core (MySQL via Pomelo, já em uso), FluentValidation

**Storage**: MySQL — 2 colunas aditivas (`vinculo_cobranca.total_aulas_pacote`, `aula_aluno.vinculo_cobranca_id`), schema versionado em `database/*.sql`

**Testing**: xUnit, mesmo padrão de fakes manuais já usado em `tests/SPI.Application.Tests` (ex.: `VinculoCobrancaFakes.cs`, `AulaServiceFakes.cs`)

**Target Platform**: Servidor ASP.NET Core existente (sem mudança de plataforma)

**Project Type**: Web application (backend .NET em camadas + frontend Next.js já existentes) — esta feature é só backend

**Performance Goals**: N/A (operação administrativa de baixo volume — cadastro/edição de vínculo e registro de presença, já no caminho crítico existente, sem novo volume significativo)

**Constraints**: Nenhuma mudança de comportamento observável na cobrança já gerada (specs/038, specs/039) — só o novo campo de auditoria é adicionado ao fluxo existente, sem alterar quando/quanto é cobrado (FR-011, FR-012)

**Scale/Scope**: 2 entidades alteradas (`VinculoCobranca`, `AulaAluno`), 1 serviço alterado (`AulaService.GerarContasAReceberAsync`), 1 serviço de validação alterado (`VinculoCobrancaService.AtualizarAsync`), 1 método de repositório novo (contagem por competência) — sem nenhuma tela nova

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Princípio | Avaliação |
|---|---|
| I. Exclusão Lógica, Nunca Física | N/A direto — esta feature não adiciona nenhuma exclusão nova; `VinculoCobranca`/`AulaAluno` continuam usando os mecanismos de exclusão lógica já existentes (`Ativo`), intocados. **Conforme.** |
| II. Validação de Negócio Única e Centralizada no Backend | A regra de bloqueio (FR-007/FR-010) é nova e stateful (depende do estado atual do vínculo e, para Mensalidade, de uma contagem) — vive inteiramente em `VinculoCobrancaService.AtualizarAsync`, o único ponto por onde toda edição de vínculo já passa hoje (nenhum outro caminho de edição existe). **Conforme.** |
| III. Nenhuma Capacidade Configurável Sem Implementação Real (NON-NEGOTIABLE) | `TotalAulasPacote` e o uso de `AulasIncluidas` como teto mensal só são expostos via contrato (request/response) acompanhados da implementação que os consome nesta mesma fatia (cálculo de "aulas dadas", bloqueio de edição) — nunca um campo aceito sem efeito. **Conforme.** |
| IV. Autenticação e Segredos Seguros por Padrão | Nenhuma mudança de autenticação/segredo; endpoints já protegidos por `[Authorize(Roles = Professor)]` permanecem como estão. **N/A.** |
| V. Documentação Retroativa como Registro Histórico Vinculante | Não é documentação retroativa — é uma extensão de regra de negócio nova, especificada do zero nesta própria spec. **N/A.** |

**Resultado**: Sem violação. Nenhuma entrada necessária em Complexity Tracking.

## Project Structure

### Documentation (this feature)

```text
specs/052-auditoria-vinculo-cobranca/
├── plan.md              # This file (/speckit-plan command output)
├── research.md          # Phase 0 output (/speckit-plan command)
├── data-model.md        # Phase 1 output (/speckit-plan command)
├── quickstart.md        # Phase 1 output (/speckit-plan command)
├── contracts/           # Phase 1 output (/speckit-plan command)
└── tasks.md             # Phase 2 output (/speckit-tasks command - NOT created by /speckit-plan)
```

### Source Code (repository root)

```text
database/
└── 18_auditoria_vinculo_cobranca.sql       # NOVO — 2 colunas aditivas + backfill

src/SPI.Domain/
├── Entities/
│   ├── VinculoCobranca.cs                  # + TotalAulasPacote
│   └── AulaAluno.cs                        # + VinculoCobrancaId, + VinculoCobranca (navegação)
└── Repositories/
    └── IAulaRepository.cs                  # + ContarPresencasPorVinculoECompetenciaAsync

src/SPI.Application/
├── Aulas/Services/
│   └── AulaService.cs                      # GerarContasAReceberAsync: popula AulaAluno.VinculoCobrancaId
└── VinculosCobranca/
    ├── Dtos/
    │   ├── VinculoCobrancaRequest.cs        # + TotalAulasPacote
    │   └── VinculoCobrancaResponse.cs       # + TotalAulasPacote, + AulasDadas (Pacote) / AulasDadasNoMes (Mensalidade)
    ├── Validators/
    │   └── VinculoCobrancaRequestValidator.cs  # TotalAulasPacote só Pacote, >= 1
    └── Services/
        └── VinculoCobrancaService.cs        # AtualizarAsync: bloqueio FR-007/FR-010

src/SPI.Infrastructure/
├── Persistence/Configurations/
│   ├── VinculoCobrancaConfiguration.cs      # + TotalAulasPacote
│   └── AulaAlunoConfiguration.cs            # + VinculoCobrancaId, + FK
└── Repositories/
    └── AulaRepository.cs                    # + ContarPresencasPorVinculoECompetenciaAsync

tests/SPI.Application.Tests/
├── VinculosCobranca/
│   ├── VinculoCobrancaFakes.cs              # + suporte a TotalAulasPacote
│   └── VinculoCobrancaServiceAuditoriaTests.cs  # NOVO — bloqueio de edição
└── Aulas/
    └── AulaServiceAuditoriaTests.cs         # NOVO — AulaAluno.VinculoCobrancaId populado por modalidade
```

**Structure Decision**: Web application em camadas já estabelecida pelo projeto (`SPI.Domain` → `SPI.Application` → `SPI.Infrastructure` → `SPI.Api`). Esta feature é 100% backend — nenhum arquivo em `frontend/` é tocado (FR-013); os 2 campos novos entram nos contratos já existentes de `VinculoCobranca`, consumidos por uma feature futura de visualização.

## Complexity Tracking

Não aplicável — Constitution Check acima não identificou nenhuma violação.
