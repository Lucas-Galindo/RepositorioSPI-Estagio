# Implementation Plan: Corrigir Filtro de Receita/Despesa Pendente no Relatório Financeiro

**Branch**: `035-fix-saldo-previsto-filtro` | **Date**: 2026-09-20 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/035-fix-saldo-previsto-filtro/spec.md`

**Note**: This template is filled in by the `/speckit-plan` command; its definition describes the execution workflow.

## Summary

Corrigir `RelatorioService.ObterFinanceiroAsync` (`GET /api/relatorios/financeiro`) para que `ReceitaPendente` (e, por consequência, `SaldoPrevisto`) respeite os filtros `alunoId`/`turmaId`/`materiaId` já recebidos pelo endpoint, do mesmo modo que `TotalRecebido` já respeita. A correção estende `IRelatorioRepository.ObterReceitasPendentesSegregadasAsync` com os mesmos parâmetros `turmaId`/`materiaId`/`alunoId` já usados por `ObterValorFaturadoNoPeriodoAsync`, reaproveitando o helper privado `AplicarFiltroReceita` já existente em `RelatorioRepository` — a mesma lógica de filtro que `ListarPagosNoPeriodoAsync` já usa para `TotalRecebido`, garantindo semântica idêntica. `DespesaPendente` permanece global (sem parâmetro novo), pois Contas a Pagar não tem vínculo com aluno/turma/matéria.

## Technical Context

**Language/Version**: C# / .NET (backend), mesma versão já usada no projeto.

**Primary Dependencies**: Nenhuma nova dependência — extensão de uma interface e um método já existentes (`IRelatorioRepository.ObterReceitasPendentesSegregadasAsync`), reaproveitando o helper privado `AplicarFiltroReceita` já implementado em `RelatorioRepository.cs` (usado hoje por `ObterValorFaturadoNoPeriodoAsync`).

**Storage**: MySQL via EF Core — nenhuma consulta nova, apenas os mesmos filtros (`Where`) já usados por `AplicarFiltroReceita`/`ListarPagosNoPeriodoAsync` aplicados a uma query já existente.

**Testing**: xUnit (`tests/SPI.Application.Tests`). Não há teste existente cobrindo `ObterFinanceiroAsync` (a confirmar em research.md) nem `ObterReceitasPendentesSegregadasAsync` diretamente — oportunidade de adicionar cobertura nova para o comportamento corrigido, seguindo o padrão de fakes manuais já estabelecido no projeto.

**Target Platform**: ASP.NET Core Web API (`SPI.Application`/`SPI.Infrastructure`), sem impacto em `SPI.Api` (assinatura do controller e contrato de resposta inalterados) nem em frontend (nenhuma mudança de contrato, os mesmos filtros já são enviados pela tela hoje).

**Project Type**: Web application (estrutura já existente do repositório).

**Performance Goals**: Sem meta nova — a correção não adiciona nenhuma consulta ao banco, apenas restringe (via `Where`) uma consulta que já existe.

**Constraints**: A filtragem de `ReceitaPendente` por `turmaId`/`materiaId`/`alunoId` MUST usar exatamente a mesma semântica de identidade já usada por `AplicarFiltroReceita`/`ListarPagosNoPeriodoAsync` para `TotalRecebido` (aluno filtrado por `AlunoId`; turma filtrada por `Aluno.AlunosTurma`; matéria filtrada por `PagamentosAula.Aula.MateriaId`) — não uma nova regra de associação. `ObterDespesasPendentesSegregadasAsync` MUST NOT receber nenhum parâmetro de filtro novo (FR-002).

**Scale/Scope**: 1 método de repositório estendido (`ObterReceitasPendentesSegregadasAsync`, interface + implementação), 1 método de serviço alterado (`RelatorioService.ObterFinanceiroAsync`, repassando os 3 filtros já recebidos). Nenhuma mudança de contrato de API, nenhuma entidade nova, nenhuma migração.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Princípio | Aplica-se? | Avaliação |
|---|---|---|
| I. Exclusão Lógica, Nunca Física | Não | Nenhuma exclusão de registro envolvida. |
| II. Validação de Negócio Única e Centralizada no Backend | Sim | A correção reaproveita `AplicarFiltroReceita`, o mesmo helper já usado por `ListarPagosNoPeriodoAsync`/`ObterValorFaturadoNoPeriodoAsync` — evita reimplementar a regra de "o que conta como receita de um aluno/turma/matéria" pela terceira vez, alinhado ao princípio de validação/regra única centralizada. |
| III. Nenhuma Capacidade Configurável Sem Implementação Real | Não | Não introduz nenhuma opção configurável nova — os filtros `alunoId`/`turmaId`/`materiaId` já existem e já são aceitos pelo endpoint hoje; a correção apenas os faz surtir efeito também em `ReceitaPendente`. |
| IV. Autenticação e Segredos Seguros por Padrão | Não | Sem relação com autenticação/segredos. |
| V. Documentação Retroativa como Registro Histórico Vinculante | Não | Nenhuma spec retroativa documenta o comportamento atual (não filtrado) de `ReceitaPendente` como aceito — é um bug não documentado, não um comportamento a ser formalmente revisto em uma spec retroativa existente. |

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
│       └── IRelatorioRepository.cs        # ObterReceitasPendentesSegregadasAsync: adiciona
│                                            # turmaId/materiaId/alunoId (int?, opcionais)
├── SPI.Infrastructure/
│   └── Repositories/
│       └── RelatorioRepository.cs         # implementa os novos parametros reaproveitando
│                                            # AplicarFiltroReceita (ja existente, linha ~310)
└── SPI.Application/
    └── Relatorios/
        └── Services/
            └── RelatorioService.cs        # ObterFinanceiroAsync: repassa alunoId/turmaId/materiaId
                                             # ja recebidos para ObterReceitasPendentesSegregadasAsync

tests/
└── SPI.Application.Tests/
    └── Relatorios/
        └── RelatorioServiceFinanceiroPendenteTests.cs   # novo arquivo -- cobre o comportamento corrigido
```

**Structure Decision**: Mudança isolada às camadas `SPI.Domain` (assinatura de interface),
`SPI.Infrastructure` (implementação do filtro, reaproveitando helper já existente) e
`SPI.Application` (repasse dos filtros já recebidos) — sem tocar em `SPI.Api` (contrato do
controller inalterado) nem `frontend` (mesmos filtros já enviados pela tela hoje).

## Post-Design Constitution Check

*Re-avaliação após Phase 1 (research.md, data-model.md, quickstart.md).*

Nenhuma mudança em relação ao Constitution Check inicial. Confirmações após o design:
- Princípio II: `research.md` (R1) confirma que o filtro reaproveitado (`AplicarFiltroReceita`)
  é byte-a-byte o mesmo já usado por `ListarPagosNoPeriodoAsync`, eliminando qualquer risco de
  uma terceira implementação divergente da regra "o que conta como receita de um aluno/turma/
  matéria".
- `data-model.md` confirma que nenhum contrato de API muda — apenas o valor calculado de
  `ReceitaPendente`/`SaldoPrevisto` para os casos com filtro.

**Resultado**: Nenhuma violação. Nenhuma linha em Complexity Tracking necessária.

## Complexity Tracking

Não aplicável — nenhuma violação identificada em nenhum dos dois Constitution Checks.
