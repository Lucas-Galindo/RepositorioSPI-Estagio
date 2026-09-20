# Implementation Plan: Remover Código Morto do Módulo Financeiro (Dashboard e ObterValorAPagarAsync)

**Branch**: `036-remove-dead-financial-code` | **Date**: 2026-09-20 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/036-remove-dead-financial-code/spec.md`

**Note**: This template is filled in by the `/speckit-plan` command; its definition describes the execution workflow.

## Summary

Remover dois trechos de código morto confirmados no módulo financeiro: (1) `DashboardService.ObterIndicadoresFinanceirosAsync`/`ObterFluxoCaixaMensalAsync` e as propriedades `DashboardResponse.Indicadores`/`.FluxoCaixaMensal` que eles populam — sem nenhum consumidor no frontend, confirmado por busca completa; (2) `IRelatorioRepository.ObterValorAPagarAsync` e sua implementação — nunca chamado por nenhum serviço. Ambas as remoções preservam explicitamente os tipos DTO e métodos de repositório reaproveitados por `RelatorioService` (aba Indicadores do Relatório Financeiro), que continuam ativos.

## Technical Context

**Language/Version**: C# / .NET (backend); TypeScript (frontend, apenas remoção de 2 campos de uma interface), mesma versão já usada no projeto.

**Primary Dependencies**: Nenhuma nova dependência — remoção pura de código já existente.

**Storage**: N/A — nenhuma consulta nova, nenhuma migração; a remoção reduz o número de consultas feitas por `GET /api/dashboard` (efeito colateral positivo, não objetivo desta feature).

**Testing**: xUnit (`tests/SPI.Application.Tests`). Nenhum teste existente cobre `DashboardService.ObterAsync`/`ObterIndicadoresFinanceirosAsync`/`ObterFluxoCaixaMensalAsync` diretamente (a confirmar em research.md) — a validação principal é a suíte completa continuar passando (FR-006) e a remoção dos 3 stubs de `ObterValorAPagarAsync` nos fakes de teste já existentes.

**Target Platform**: ASP.NET Core Web API (`SPI.Application`) + Next.js/TypeScript (`frontend/lib/api/dashboard.ts`), sem impacto em `SPI.Api` (assinatura do controller `GET /api/dashboard` inalterada, só o shape do DTO de resposta perde 2 campos nunca lidos).

**Project Type**: Web application (estrutura já existente do repositório).

**Performance Goals**: Sem meta nova — a remoção elimina consultas hoje feitas e descartadas (efeito colateral, não meta formal desta feature).

**Constraints**: A remoção MUST NOT tocar em `IndicadoresFinanceirosResponse`, `FluxoCaixaMensalItem`, `GargaloCaixaResponse` (tipos DTO, ainda usados por `RelatorioService`) nem em nenhum método de `IRelatorioRepository` além de `ObterValorAPagarAsync` — todos os demais métodos chamados pelos dois métodos removidos de `DashboardService` continuam em uso por `RelatorioService.ObterIndicadoresFinanceirosAsync` (FR-003).

**Scale/Scope**: 4 arquivos alterados (`DashboardService.cs`, `DashboardResponse.cs`, `IRelatorioRepository.cs`, `RelatorioRepository.cs`), 1 arquivo frontend (`dashboard.ts`), 3 arquivos de teste (remoção de stub). Nenhuma entidade nova, nenhuma migração, nenhuma mudança de contrato de API além da remoção de 2 campos nunca consumidos.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Princípio | Aplica-se? | Avaliação |
|---|---|---|
| I. Exclusão Lógica, Nunca Física | Não | Não se trata de exclusão de registro de domínio (Aluno/Turma/Matéria/Aula) — é remoção de código morto (métodos/propriedades de DTO sem consumidor), fora do escopo deste princípio. |
| II. Validação de Negócio Única e Centralizada no Backend | Não | Não introduz nem remove nenhuma regra de validação de negócio duplicada. |
| III. Nenhuma Capacidade Configurável Sem Implementação Real | Não diretamente | Na verdade o oposto do problema que este princípio previne: aqui a implementação existe mas não é exposta/usada por nenhuma tela — removê-la é consistente com o espírito do princípio (não deixar capacidades órfãs no código). |
| IV. Autenticação e Segredos Seguros por Padrão | Não | Sem relação com autenticação/segredos. |
| V. Documentação Retroativa como Registro Histórico Vinculante | Não | Nenhuma spec retroativa documenta `DashboardResponse.Indicadores`/`.FluxoCaixaMensal` ou `ObterValorAPagarAsync` como comportamento aceito e ativo — não há spec retroativa a atualizar. |

**Resultado**: Nenhuma violação. Nenhuma linha em Complexity Tracking necessária.

## Project Structure

### Documentation (this feature)

```text
specs/[###-feature]/
├── plan.md              # This file (/speckit-plan command output)
├── research.md          # Phase 0 output (/speckit-plan command)
├── data-model.md        # Phase 1 output (/speckit-plan command)
├── quickstart.md        # Phase 1 output (/speckit-plan command)
├── contracts/           # Phase 1 output (/speckit-plan command)
└── tasks.md             # Phase 2 output (/speckit-tasks command - NOT created by /speckit-plan)
```

### Source Code (repository root)

```text
src/
├── SPI.Domain/
│   └── Repositories/
│       └── IRelatorioRepository.cs        # remove ObterValorAPagarAsync (linha ~47)
├── SPI.Infrastructure/
│   └── Repositories/
│       └── RelatorioRepository.cs         # remove implementacao (linhas ~96-107)
└── SPI.Application/
    └── Dashboard/
        ├── Services/
        │   └── DashboardService.cs        # remove ObterIndicadoresFinanceirosAsync,
        │                                    # ObterFluxoCaixaMensalAsync e as chamadas
        │                                    # em ObterAsync (linhas 35-36, 62-119)
        └── Dtos/
            └── DashboardResponse.cs       # remove propriedades Indicadores, FluxoCaixaMensal

frontend/
└── lib/
    └── api/
        └── dashboard.ts                  # remove campos indicadores/fluxoCaixaMensal da
                                            # interface Dashboard (mantem os tipos
                                            # IndicadoresFinanceiros/FluxoCaixaMensalItem/
                                            # GargaloCaixa, ainda usados por relatorios.ts)

tests/
└── SPI.Application.Tests/
    └── Relatorios/
        ├── RelatorioServiceFinanceiroPorTurmaTests.cs    # remove stub ObterValorAPagarAsync
        ├── RelatorioServiceLancamentosTests.cs           # remove stub ObterValorAPagarAsync
        └── RelatorioServiceFinanceiroPendenteTests.cs    # remove stub ObterValorAPagarAsync
```

**Structure Decision**: Mudança isolada a `SPI.Domain`/`SPI.Infrastructure`/`SPI.Application`
(remoção de um método de repositório e de dois métodos de serviço + propriedades de DTO) e a
`frontend/lib/api/dashboard.ts` (remoção de 2 campos de uma interface TypeScript) — sem tocar em
`SPI.Api` (assinatura do controller inalterada) nem em nenhuma página do frontend (nenhuma
consome os campos removidos).

## Post-Design Constitution Check

*Re-avaliação após Phase 1 (research.md, data-model.md, quickstart.md).*

Nenhuma mudança em relação ao Constitution Check inicial. Confirmações após o design:
- `research.md` (R2) e `data-model.md` (tabela "Explicitamente preservado") confirmam, com
  citação de linha exata, que nenhum tipo DTO ou método de repositório compartilhado com
  `RelatorioService` é tocado — a remoção é estritamente limitada ao código já confirmado morto.
- `research.md` (R3) confirma que os 3 stubs de teste a remover não escondem nenhuma asserção
  real (nenhum teste exercita `ObterValorAPagarAsync`).

**Resultado**: Nenhuma violação. Nenhuma linha em Complexity Tracking necessária.

## Complexity Tracking

Não aplicável — nenhuma violação identificada em nenhum dos dois Constitution Checks.
