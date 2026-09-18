# Implementation Plan: Remover Índice de Cobertura de Custos Fixos do Backend

**Branch**: `030-remove-indice-cobertura-custos` | **Date**: 2026-09-18 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/030-remove-indice-cobertura-custos/spec.md`

**Note**: This template is filled in by the `/speckit-plan` command; its definition describes the execution workflow.

## Summary

Remover o campo `IndiceCoberturaCustosFixos` (e o cálculo `valorFaturado / valorPago` que o preenche) do DTO compartilhado `IndicadoresFinanceirosResponse`, que é retornado por dois endpoints (`GET /api/dashboard` via `DashboardService.ObterIndicadoresFinanceirosAsync` e `GET /api/relatorios/indicadores-financeiros` via `RelatorioService.ObterIndicadoresFinanceirosAsync`). A spec 024 já havia removido a exibição desse indicador da tela de Indicadores; esta mudança completa a remoção eliminando o campo do contrato de API nos dois pontos e do espelhamento de tipo TypeScript correspondente no frontend, sem alterar nenhum outro campo/indicador.

## Technical Context

**Language/Version**: C# / .NET (backend) e TypeScript (mirror de tipo no frontend), mesmas versões já usadas no projeto.

**Primary Dependencies**: Nenhuma nova dependência — apenas remoção de uma propriedade de um `record` já existente e de duas atribuições/cálculos já existentes.

**Storage**: N/A — não há coluna de banco nem entidade persistida envolvida (o campo é 100% calculado em memória a partir de outros valores já buscados).

**Testing**: xUnit (`tests/SPI.Application.Tests`). Não há teste existente que referencie `IndiceCoberturaCustosFixos`/`indiceCobertura` (confirmado por busca) — nenhum teste precisa ser removido; a validação é por compilação (o campo deixa de existir no tipo) mais uma checagem manual de payload via quickstart.

**Target Platform**: ASP.NET Core Web API (`SPI.Application`/`SPI.Api`) + espelho de tipo no frontend Next.js/TypeScript (`frontend/lib/api/dashboard.ts`), sem nenhum componente de UI a alterar (o campo já não é renderizado em lugar nenhum).

**Project Type**: Web application (estrutura já existente do repositório).

**Performance Goals**: Sem meta nova — a mudança remove uma divisão/arredondamento por requisição em dois métodos; efeito é neutro a levemente positivo, não é o objetivo da mudança.

**Constraints**: A remoção MUST NOT alterar nenhum outro campo/valor da resposta de indicadores financeiros (FR-004/SC-002 da spec).

**Scale/Scope**: Estritamente limitado a: 1 propriedade no DTO `IndicadoresFinanceirosResponse` ([IndicadoresFinanceirosResponse.cs](../../src/SPI.Application/Dashboard/Dtos/IndicadoresFinanceirosResponse.cs)), 2 blocos de cálculo/atribuição (`DashboardService.cs` linhas ~76 e ~89; `RelatorioService.cs` linhas ~207 e ~221), e 1 campo no tipo espelhado do frontend (`frontend/lib/api/dashboard.ts`, interface `IndicadoresFinanceiros`).

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Princípio | Aplica-se? | Avaliação |
|---|---|---|
| I. Exclusão Lógica, Nunca Física | Não | Não há exclusão de registro de domínio — é remoção de um campo calculado de um DTO de resposta, não de dado persistido. |
| II. Validação de Negócio Única e Centralizada no Backend | Não diretamente | Não há regra de validação de negócio envolvida; é remoção de um cálculo derivado sem lógica de validação associada. |
| III. Nenhuma Capacidade Configurável Sem Implementação Real | Sim (sentido inverso) | Este princípio existe para impedir que a UI ofereça algo que o backend não processa; aqui o caso é o oposto (backend calcula algo que a UI não exibe desde a spec 024) — a remoção alinha o sistema ao espírito do princípio, eliminando a divergência residual entre "o que a API calcula" e "o que é realmente usado". Conforme. |
| IV. Autenticação e Segredos Seguros por Padrão | Não | Sem relação com autenticação/segredos. |
| V. Documentação Retroativa como Registro Histórico Vinculante | Sim | Esta mudança altera intencionalmente um comportamento documentado explicitamente em `specs/015-relatorio-financeiro/spec.md` (User Story 3, Acceptance Scenario 1 e FR-005 citam "Cobertura de custos (recebido ÷ pago)" como indicador calculado pela API) e referenciado nas Assumptions da própria `specs/024-remover-card-cobertura-custos/spec.md` ("não há requisito... de remover o cálculo subjacente no backend... nesta [024] especificação" — implicando que uma spec futura como esta poderia fazê-lo). `specs/011-dashboard/spec.md` não cita "Cobertura de custos" nominalmente (só "indicadores financeiros avançados" de forma genérica, remetendo a 015) — não precisa de alteração direta. `specs/015-relatorio-financeiro/spec.md` MUST ser atualizada para não descrever mais um campo que deixará de existir na API — ação registrada como tarefa de documentação em tasks.md (Polish).

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
└── SPI.Application/
    ├── Dashboard/
    │   ├── Dtos/
    │   │   └── IndicadoresFinanceirosResponse.cs   # remove a propriedade IndiceCoberturaCustosFixos
    │   └── Services/
    │       └── DashboardService.cs                  # remove o calculo `indiceCobertura` e a atribuicao
    └── Relatorios/
        └── Services/
            └── RelatorioService.cs                  # remove o mesmo calculo/atribuicao (segunda ocorrencia)

frontend/
└── lib/
    └── api/
        └── dashboard.ts   # remove `indiceCoberturaCustosFixos` da interface IndicadoresFinanceiros
                            # (reaproveitada por relatorios.ts via IndicadoresFinanceirosFiltrados -- sem edicao propria la)

specs/
├── 015-relatorio-financeiro/spec.md   # atualizar (Principio V): remover mencao a "Cobertura de custos"
└── 024-remover-card-cobertura-custos/spec.md   # sem edicao necessaria (ja registra a decisao como pendente)
```

**Structure Decision**: Web application já existente (Option 2 do template). Mudança isolada à camada `SPI.Application` do backend (dois services + um DTO compartilhado) e a um único arquivo de espelhamento de tipo no frontend — sem tocar em `SPI.Domain`, `SPI.Infrastructure` (nenhuma query/coluna de banco envolvida) nem `SPI.Api` (nenhuma rota/controller muda de assinatura, só o corpo da resposta fica menor).

## Post-Design Constitution Check

*Re-avaliação após Phase 1 (research.md, data-model.md, quickstart.md).*

Nenhuma mudança em relação ao Constitution Check inicial. Confirmações após o design:
- Princípio V: `research.md` (R3) confirma, por leitura direta, que apenas
  `specs/015-relatorio-financeiro/spec.md` cita o indicador por nome e precisa de atualização;
  `specs/011-dashboard/spec.md` e `specs/024-remover-card-cobertura-custos/spec.md` foram
  verificadas e não precisam de edição. Isso substitui a suposição inicial (mais ampla) do
  Constitution Check pré-design.
- `data-model.md` confirma que a mudança é puramente de DTO/tipo, sem entidade nem coluna de
  banco — reforça que os Princípios I e IV seguem não aplicáveis.

**Resultado**: Nenhuma violação. Nenhuma linha em Complexity Tracking necessária.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

Não aplicável — nenhuma violação identificada em nenhum dos dois Constitution Checks.
