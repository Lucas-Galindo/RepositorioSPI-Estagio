# Implementation Plan: Unificar Cálculo de "Valor Pendente a Receber"

**Branch**: `032-unify-pendente-receber` | **Date**: 2026-09-18 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/032-unify-pendente-receber/spec.md`

**Note**: This template is filled in by the `/speckit-plan` command; its definition describes the execution workflow.

## Summary

Estender `IRelatorioRepository.ObterValorPendenteAsync` com parâmetros opcionais (`alunoId`, `status`, `vencimentoInicio`, `vencimentoFim`), replicando exatamente a semântica hoje implementada em `RelatorioService.ObterPagamentosAsync` (incluindo o caso `status == "Atrasado"` mapeado para `Pendente` vencido, e o caso de qualquer outro status concreto zerando o total). `DashboardService` passa a chamar essa mesma implementação sem argumentos (comportamento idêntico ao atual). `RelatorioService.ObterPagamentosAsync` passa a calcular `TotalPendenteConsolidado` chamando a implementação compartilhada com os filtros já recebidos do controller, em vez de somar em memória sobre a lista de `Itens` já mapeada. Nenhuma mudança de contrato de API, nenhuma entidade nova, nenhuma migração.

## Technical Context

**Language/Version**: C# / .NET (backend), mesma versão já usada no projeto.

**Primary Dependencies**: Nenhuma nova dependência — extensão de assinatura de um método já existente em `IRelatorioRepository`/`RelatorioRepository`, consumido por dois services já existentes.

**Storage**: MySQL via EF Core — a consulta une os filtros já usados individualmente em `PagamentoRepository.ListarAsync` (alunoId, status, vencimento) com a lógica de status efetivo "Atrasado" já usada em `PagamentoService.Mapear` ([PagamentoService.cs:200](../../src/SPI.Application/Pagamentos/Services/PagamentoService.cs#L200), cutoff `DateOnly.FromDateTime(DateTime.UtcNow)`), tudo em uma única consulta agregada (`SUM`) — sem carregar entidades completas.

**Testing**: xUnit (`tests/SPI.Application.Tests`). Não há teste existente cobrindo `ObterValorPendenteAsync` (é uma consulta de `IRelatorioRepository`, testada indiretamente hoje só via integração real com banco — não há fake/mock desse repositório específico nos testes atuais de `DashboardService`). A verificação principal é por comportamento equivalente antes/depois (`dotnet test` completo) e validação manual via quickstart com dados reais.

**Target Platform**: ASP.NET Core Web API (`SPI.Domain`/`SPI.Infrastructure`/`SPI.Application`), sem impacto em `SPI.Api` (assinaturas de controller e contrato de resposta inalterados) nem em frontend.

**Project Type**: Web application (estrutura já existente do repositório).

**Performance Goals**: O caso sem filtro (Dashboard) MUST continuar sendo uma soma agregada direta no banco (`SUM` via LINQ traduzido para SQL), sem carregar a lista completa de `Pagamento` em memória — mesma característica de hoje (ver spec.md, Assumptions). A extensão com filtros opcionais não introduz nenhuma consulta adicional por chamada (continua sendo uma única query).

**Constraints**: A implementação compartilhada MUST reproduzir exatamente, para cada combinação de filtros já aceita hoje por `GET /api/relatorios/pagamentos`, o valor que `TotalPendenteConsolidado` retorna atualmente — incluindo o comportamento de zerar quando `status` é incompatível com "pendente" (ver spec.md, Edge Cases). Não é uma correção de bug, é preservação de comportamento (FR-004).

**Scale/Scope**: 1 método estendido em `IRelatorioRepository`/`RelatorioRepository.ObterValorPendenteAsync` ([RelatorioRepository.cs:27-37](../../src/SPI.Infrastructure/Repositories/RelatorioRepository.cs#L27-L37), [IRelatorioRepository.cs:19](../../src/SPI.Domain/Repositories/IRelatorioRepository.cs#L19)), 1 chamador inalterado em assinatura (`DashboardService.cs:30`), 1 chamador que passa a usar os filtros já recebidos (`RelatorioService.ObterPagamentosAsync`, [RelatorioService.cs:73-96](../../src/SPI.Application/Relatorios/Services/RelatorioService.cs#L73-L96) — substitui o cálculo em memória de `totalPendente` pela chamada ao repositório).

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Princípio | Aplica-se? | Avaliação |
|---|---|---|
| I. Exclusão Lógica, Nunca Física | Não | Nenhuma exclusão de registro envolvida. |
| II. Validação de Negócio Única e Centralizada no Backend | Sim | É exatamente o que esta mudança implementa — hoje existem duas implementações independentes da mesma regra de negócio ("o que conta como pendente"); a unificação é a aplicação direta deste princípio a um cálculo de leitura (análogo ao caso já citado no princípio, de validação de conflito de horário unificada). Conforme. |
| III. Nenhuma Capacidade Configurável Sem Implementação Real | Não diretamente | Os filtros de `GET /api/relatorios/pagamentos` já são processados de fato hoje (não é uma opção fantasma) — apenas não têm consumidor de UI. Esta mudança não altera essa situação (fora de escopo, FR-006). |
| IV. Autenticação e Segredos Seguros por Padrão | Não | Sem relação com autenticação/segredos. |
| V. Documentação Retroativa como Registro Histórico Vinculante | Não | Nenhuma spec retroativa (`specs/001`-`019` ou specs técnicas posteriores) documenta a implementação interna de `ObterValorPendenteAsync` como comportamento observável — é um detalhe de implementação, não um comportamento de negócio documentado. `specs/011-dashboard/spec.md` e `specs/014-relatorio-pendencias-financeiras/spec.md` descrevem o *resultado* (valor pendente a receber existe e é calculado), não *como* — esse resultado não muda (FR-003/FR-004), então nenhuma spec retroativa precisa de atualização.

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
│       └── IRelatorioRepository.cs        # estende a assinatura de ObterValorPendenteAsync
├── SPI.Infrastructure/
│   └── Repositories/
│       └── RelatorioRepository.cs         # implementa os filtros opcionais na mesma consulta agregada
└── SPI.Application/
    ├── Dashboard/
    │   └── Services/
    │       └── DashboardService.cs        # chamada inalterada (sem argumentos) -- resultado identico
    └── Relatorios/
        └── Services/
            └── RelatorioService.cs        # ObterPagamentosAsync: troca o calculo em memoria de
                                            # totalPendente pela chamada ao repositorio compartilhado
```

**Structure Decision**: Mudança isolada às três camadas do backend já existentes (`SPI.Domain`, `SPI.Infrastructure`, `SPI.Application`) — sem tocar em `SPI.Api` (nenhuma rota/controller/contrato de resposta muda) nem em `frontend/` (nenhum tipo TypeScript referencia a implementação interna). Não há projeto novo nem estrutura nova.

## Post-Design Constitution Check

*Re-avaliação após Phase 1 (research.md, data-model.md, quickstart.md).*

Nenhuma mudança em relação ao Constitution Check inicial. Confirmações após o design:
- Princípio II: `research.md` (R1-R3) confirma, com a tabela completa de comportamento por
  combinação de filtro, que a unificação é viável sem nenhuma perda de precisão — reforça que
  esta é uma aplicação direta do princípio (regra única, dois consumidores).
- `data-model.md` confirma que nenhum contrato de API muda de shape — apenas a assinatura interna
  de `IRelatorioRepository.ObterValorPendenteAsync` ganha parâmetros opcionais compatíveis com a
  chamada atual.

**Resultado**: Nenhuma violação. Nenhuma linha em Complexity Tracking necessária.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

Não aplicável — nenhuma violação identificada em nenhum dos dois Constitution Checks.
| [e.g., Repository pattern] | [specific problem] | [why direct DB access insufficient] |
