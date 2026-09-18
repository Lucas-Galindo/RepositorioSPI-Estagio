# Implementation Plan: Remover Indicador "Margem de Segurança %"

**Branch**: `031-remove-margem-seguranca` | **Date**: 2026-09-18 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/031-remove-margem-seguranca/spec.md`

**Note**: This template is filled in by the `/speckit-plan` command; its definition describes the execution workflow.

## Summary

Remover o indicador "Margem de Segurança %" em duas frentes: (1) a exibição visual — o único card real está na aba "Visão de Indicadores" de Relatórios → Relatório Financeiro (a spec original mencionava um card no Dashboard que a investigação confirmou não existir); e (2) o contrato de API — remover o campo `MargemSegurancaPercentual` do DTO compartilhado `IndicadoresFinanceirosResponse` (retornado por `GET /api/dashboard` e `GET /api/relatorios/indicadores-financeiros`) e o cálculo `FluxoCaixaOperacional ÷ ValorFaturado × 100` que o preenche nos dois services. Segue o mesmo padrão de remoção de contrato já usado na spec 030 (Cobertura de Custos), mas soma a remoção de UI que a spec 024 fez para aquele outro indicador — aqui as duas partes acontecem numa única mudança, já que a exibição nunca tinha sido removida antes.

## Technical Context

**Language/Version**: C# / .NET (backend) e TypeScript/React (frontend), mesmas versões já usadas no projeto.

**Primary Dependencies**: Nenhuma nova dependência — remoção de uma propriedade de um `record` já existente, de um cálculo já existente em dois services, e de um bloco JSX já existente numa página React.

**Storage**: N/A — sem coluna de banco nem entidade persistida (campo 100% calculado em memória).

**Testing**: xUnit (`tests/SPI.Application.Tests`). Nenhum teste existente referencia `MargemSegurancaPercentual`/`margemSeguranca` (a confirmar em research.md) — validação principal por compilação (backend) e checagem manual de tela + payload (frontend/quickstart).

**Target Platform**: ASP.NET Core Web API (`SPI.Application`/`SPI.Api`) + Next.js/React (`frontend/app/(app)/relatorios/financeiro/page.tsx`, `frontend/lib/api/dashboard.ts`).

**Project Type**: Web application (estrutura já existente do repositório).

**Performance Goals**: Sem meta nova — mudança remove uma divisão/arredondamento por requisição e um bloco JSX; efeito neutro.

**Constraints**: A remoção MUST NOT alterar nenhum outro campo/card de indicadores financeiros (FR-006/SC-004 da spec); o reflow visual dos 2 cards restantes MUST seguir o mesmo padrão já usado quando "Cobertura de custos" foi removida (spec 024) — layout `kpi-row` com largura igual entre os cards restantes, sem vão vazio (já existe precedente de classe CSS para fileira de 2 cards, `kpi-row-2`, em outras telas do projeto).

**Scale/Scope**: Backend: 1 propriedade no DTO ([IndicadoresFinanceirosResponse.cs:25](../../src/SPI.Application/Dashboard/Dtos/IndicadoresFinanceirosResponse.cs#L25)), 2 blocos de cálculo/atribuição (`DashboardService.cs` linhas ~75 e ~86; `RelatorioService.cs` linhas ~206 e ~218), 1 campo no tipo espelhado do frontend (`frontend/lib/api/dashboard.ts`). Frontend visual: 1 bloco JSX (`frontend/app/(app)/relatorios/financeiro/page.tsx`, linhas ~369-377) e o reflow da fileira `kpi-row-3` → 2 cards.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Princípio | Aplica-se? | Avaliação |
|---|---|---|
| I. Exclusão Lógica, Nunca Física | Não | Não há exclusão de registro de domínio — remoção de um campo calculado de DTO e de um elemento visual, não de dado persistido. |
| II. Validação de Negócio Única e Centralizada no Backend | Não diretamente | Não há regra de validação de negócio envolvida; é remoção de um cálculo derivado e sua exibição, sem lógica de validação associada. |
| III. Nenhuma Capacidade Configurável Sem Implementação Real | Sim (sentido inverso) | Mesmo raciocínio já aplicado na spec 030: o backend calculava um valor cujo nome induzia a uma leitura de mercado que o cálculo não sustenta — removê-lo (cálculo e exibição) alinha o sistema ao espírito do princípio, eliminando um indicador que poderia levar a uma decisão de negócio equivocada. Conforme. |
| IV. Autenticação e Segredos Seguros por Padrão | Não | Sem relação com autenticação/segredos. |
| V. Documentação Retroativa como Registro Histórico Vinculante | Sim | Esta mudança altera intencionalmente um comportamento documentado em `specs/015-relatorio-financeiro/spec.md` (User Story 3, Acceptance Scenario 1, FR-005 citam "Margem de segurança %"). `specs/015-relatorio-financeiro/spec.md` MUST ser atualizada, seguindo o mesmo padrão de nota "Atualização (data, ver spec)" já usado duas vezes nesse arquivo (specs 029 e 030). `specs/024-remover-card-cobertura-custos/spec.md` cita "Margem de segurança" apenas como contexto histórico de uma mudança já concluída (os "3 cards restantes" daquela remoção) — continua factualmente correta sobre o que aconteceu naquela mudança e não precisa de edição (mesmo raciocínio da spec 030 para specs históricas neutras).

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
    │   │   └── IndicadoresFinanceirosResponse.cs   # remove a propriedade MargemSegurancaPercentual
    │   └── Services/
    │       └── DashboardService.cs                  # remove o calculo `margemSeguranca` e a atribuicao
    └── Relatorios/
        └── Services/
            └── RelatorioService.cs                  # remove o mesmo calculo/atribuicao (segunda ocorrencia)

frontend/
├── lib/
│   └── api/
│       └── dashboard.ts   # remove `margemSegurancaPercentual` da interface IndicadoresFinanceiros
└── app/
    └── (app)/
        └── relatorios/
            └── financeiro/
                └── page.tsx   # remove o bloco JSX do card "Margem de segurança" e reflui
                                # a fileira kpi-row-3 -> 2 cards (Inadimplencia, Prazo medio de atraso)

specs/
├── 015-relatorio-financeiro/spec.md            # atualizar (Principio V): remover mencao a "Margem de segurança"
└── 024-remover-card-cobertura-custos/spec.md   # sem edicao necessaria (registro historico correto)
```

**Structure Decision**: Web application já existente (Option 2 do template). Mudança isolada à camada `SPI.Application` do backend (dois services + um DTO compartilhado) e a dois arquivos do frontend (tipo espelhado + página que renderiza o card) — sem tocar em `SPI.Domain`, `SPI.Infrastructure` (nenhuma query/coluna de banco envolvida) nem `SPI.Api` (nenhuma rota/controller muda de assinatura).

## Post-Design Constitution Check

*Re-avaliação após Phase 1 (research.md, data-model.md, quickstart.md).*

Nenhuma mudança em relação ao Constitution Check inicial. Confirmações após o design:
- Princípio V: `research.md` (R5) confirma, por leitura direta, que apenas
  `specs/015-relatorio-financeiro/spec.md` cita o indicador por nome e precisa de atualização;
  `specs/024-remover-card-cobertura-custos/spec.md` foi verificada e não precisa de edição
  (registro histórico correto de uma mudança já concluída). `specs/022-.../contracts/indicadores-financeiros.md`
  permanece como atualização opcional de qualidade de documentação, não uma obrigação do Princípio V.
- `data-model.md`/R3 confirmam que o reflow visual reaproveita uma classe CSS já existente
  (`kpi-row-2`) — nenhuma decisão de design nova, nenhum risco de inconsistência visual.

**Resultado**: Nenhuma violação. Nenhuma linha em Complexity Tracking necessária.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

Não aplicável — nenhuma violação identificada em nenhum dos dois Constitution Checks.
