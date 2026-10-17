# Implementation Plan: Padronizar Visual do Alternador Visão Financeiro/Indicadores

**Branch**: `053-visao-indicadores-tabs` | **Date**: 2026-10-13 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/053-visao-indicadores-tabs/spec.md`

## Summary

Trocar o contêiner do alternador "Visão Financeiro"/"Visão de Indicadores" (`frontend/app/(app)/relatorios/financeiro/page.tsx`) de `.row-gap` para `.financeiro-tabs`, e estender a regra CSS `.financeiro-tabs > a` (`frontend/styles/dashboard.css`) para também cobrir `> button` — dando aos 2 botões o mesmo visual de barra de abas em largura total já usado pelas abas de Contas a Receber/Pagar, sem alterar o comportamento de clique de nenhum dos dois pares.

## Technical Context

**Language/Version**: TypeScript / React (Next.js App Router), mesma versão já usada no projeto (`frontend/package.json`)

**Primary Dependencies**: Nenhuma dependência nova — só CSS já existente (`frontend/styles/dashboard.css`) e o componente React já existente

**Storage**: N/A

**Testing**: Validação manual via `quickstart.md` (comparação visual) — mesma decisão já aceita para toda mudança de frontend anterior nesta sessão (specs/045-048, 050, 051); o projeto não tem suíte de teste de frontend configurada

**Target Platform**: Navegador (mesma tela já existente, Relatório Financeiro)

**Project Type**: Web application — esta feature é só frontend (CSS + 1 atributo `className`), nenhuma mudança de backend/banco

**Performance Goals**: N/A (mudança de CSS/layout, sem impacto de performance)

**Constraints**: Zero mudança no markup/visual/comportamento das abas de Contas a Receber/Pagar (FR-003); zero mudança no comportamento de clique do alternador (FR-002, estado local `aba` e persistência de filtros de specs/048 inalterados)

**Scale/Scope**: 2 arquivos tocados (`frontend/app/(app)/relatorios/financeiro/page.tsx`, `frontend/styles/dashboard.css`) — nenhum arquivo novo

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Princípio | Avaliação |
|---|---|
| I. Exclusão Lógica, Nunca Física | N/A — nenhuma exclusão de registro envolvida. |
| II. Validação de Negócio Única e Centralizada no Backend | N/A — nenhuma regra de negócio ou validação envolvida, é puramente visual. |
| III. Nenhuma Capacidade Configurável Sem Implementação Real (NON-NEGOTIABLE) | N/A — nenhuma opção/capacidade nova é exposta; o alternador já funciona hoje, só muda a aparência. **Conforme.** |
| IV. Autenticação e Segredos Seguros por Padrão | N/A. |
| V. Documentação Retroativa como Registro Histórico Vinculante | N/A — não é documentação retroativa. |

**Resultado**: Sem violação. Nenhuma entrada necessária em Complexity Tracking.

## Project Structure

### Documentation (this feature)

```text
specs/053-visao-indicadores-tabs/
├── plan.md              # This file (/speckit-plan command output)
├── research.md          # Phase 0 output (/speckit-plan command)
├── data-model.md        # Phase 1 output (/speckit-plan command) — N/A, sem entidade
├── quickstart.md        # Phase 1 output (/speckit-plan command)
├── contracts/           # Phase 1 output (/speckit-plan command) — N/A, sem contrato de API
└── tasks.md             # Phase 2 output (/speckit-tasks command - NOT created by /speckit-plan)
```

### Source Code (repository root)

```text
frontend/
├── app/(app)/relatorios/financeiro/
│   └── page.tsx          # Troca className="row-gap" → "financeiro-tabs" no wrapper do alternador
└── styles/
    └── dashboard.css      # Estende o seletor .financeiro-tabs > a para também cobrir > button
```

**Structure Decision**: Esta feature é 100% frontend, limitada a 2 arquivos já existentes (`relatorios/financeiro/page.tsx`, `dashboard.css`) — nenhum diretório novo, nenhum arquivo novo, nenhuma mudança em `src/` (backend) ou `database/`.

## Complexity Tracking

Não aplicável — Constitution Check acima não identificou nenhuma violação.
