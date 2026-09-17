# Implementation Plan: Remover Card "Cobertura de Custos" da Aba Indicadores

**Branch**: `024-remover-card-cobertura-custos` | **Date**: 2026-09-16 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/024-remover-card-cobertura-custos/spec.md`

**Note**: This template is filled in by the `/speckit-plan` command; its definition describes the execution workflow.

## Summary

Remover o card "Cobertura de custos" da fileira de indicadores da aba Indicadores (Relatórios →
Relatório Financeiro) e realinhar os 3 cards restantes (Inadimplência, Prazo médio de atraso,
Margem de segurança) para ocupar a fileira de forma equilibrada. É uma mudança puramente de
apresentação, restrita a `frontend/app/(app)/relatorios/financeiro/page.tsx`: remove o bloco JSX
do card e troca a classe CSS da fileira de 4 colunas (`kpi-row`, `grid-template-columns:
repeat(4, 1fr)`) para a classe de 3 colunas já existente no projeto (`kpi-row kpi-row-3`,
`frontend/styles/dashboard.css`). O campo `indiceCoberturaCustosFixos` no DTO/backend
(`IndicadoresFinanceirosResponse`) permanece intacto e sem alteração: ele também alimenta o
Dashboard (`DashboardService.ObterIndicadoresAsync`), que não faz parte do pedido do usuário —
remover o campo do backend quebraria essa outra tela sem necessidade.

## Technical Context

**Language/Version**: TypeScript / React 19 (Next.js 16, App Router) — mesmo stack do restante
do frontend, nenhuma linguagem nova

**Primary Dependencies**: Nenhuma dependência nova — usa apenas JSX e a classe CSS `kpi-row-3`
já existente em `frontend/styles/dashboard.css` (usada por outras fileiras de indicadores com 3
cards no sistema)

**Storage**: N/A — mudança puramente de apresentação, nenhum schema de banco tocado

**Testing**: Frontend sem framework de teste automatizado configurado (mesma limitação já
registrada em specs anteriores, ex. specs/023) — verificação manual via quickstart.md. Não há
lógica de negócio nova a testar (nenhum cálculo é alterado, apenas removida a exibição de um
cálculo já existente)

**Target Platform**: Navegador web (mesma tela de Relatório Financeiro já existente)

**Project Type**: Web application (frontend Next.js já existente) — esta feature toca somente o
frontend

**Performance Goals**: N/A — não há meta de performance de sistema; a única expectativa
observável é visual (SC-002 da spec: cards preenchendo a fileira sem espaço vazio)

**Constraints**: A remoção do card MUST NOT alterar o cálculo, valor ou comportamento dos
demais indicadores (FR-002) nem tocar o backend/DTO compartilhado com o Dashboard (ver Summary)

**Scale/Scope**: 1 arquivo de frontend alterado (`frontend/app/(app)/relatorios/financeiro/page.tsx`)
— remoção de ~15 linhas de JSX (o card) e troca de 1 className (`kpi-row` → `kpi-row kpi-row-3`)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Avaliação contra os 5 princípios ratificados em `.specify/memory/constitution.md` (v1.0.0):

| Princípio | Aplicável? | Avaliação |
|---|---|---|
| I. Exclusão Lógica, Nunca Física | Não (observado) | Não há exclusão de registro de domínio nesta mudança — é a remoção de um elemento visual (card) de uma tela de relatório, não de um dado persistido. Nada a excluir logicamente |
| II. Validação de Negócio Única e Centralizada no Backend | Não (observado) | Nenhuma regra de validação de negócio é criada, alterada ou duplicada — a mudança não introduz nem toca lógica de validação |
| III. Nenhuma Capacidade Configurável Sem Implementação Real | Não (observado) | Nenhuma opção configurável nova é exposta; ao contrário, uma exibição é removida, não adicionada. Não há risco de "opção fantasma" |
| IV. Autenticação e Segredos Seguros por Padrão | Não (observado) | Nenhuma mudança de autenticação, sessão ou segredo envolvida |
| V. Documentação Retroativa como Registro Histórico Vinculante | Não (observado) | Nenhuma spec retroativa (001-019) descreve especificamente este card; não há conflito documental a resolver |

**Resultado**: PASS — nenhum princípio é violado por esta feature. Mudança de baixíssimo risco,
puramente de apresentação, sem impacto em regra de negócio, dado persistido ou segurança.

**Re-check pós-design (Phase 1)**: PASS, sem mudanças. O research.md (Decisão 3) confirmou que
o campo de backend compartilhado com o Dashboard não é tocado, reforçando a avaliação acima —
nenhum novo risco foi descoberto durante o design.

## Project Structure

### Documentation (this feature)

```text
specs/024-remover-card-cobertura-custos/
├── plan.md              # This file (/speckit-plan command output)
├── research.md          # Phase 0 output (/speckit-plan command)
├── quickstart.md        # Phase 1 output (/speckit-plan command)
└── tasks.md             # Phase 2 output (/speckit-tasks command - NOT created by /speckit-plan)
```

Sem `data-model.md` nem `contracts/`: esta feature não envolve entidades de dados novas/alteradas
nem contratos de API novos/alterados (ver spec.md "Key Entities": não aplicável).

### Source Code (repository root)

```text
# Aplicação web já existente (frontend Next.js + backend ASP.NET Core + MySQL).
# Esta feature toca SOMENTE o frontend, um único arquivo de página e nenhum arquivo novo.

frontend/
├── app/(app)/relatorios/financeiro/
│   └── page.tsx           # remove o bloco JSX do card "Cobertura de custos" (linhas
│                           #   ~377-391) e troca a className da fileira de indicadores
│                           #   de "kpi-row" para "kpi-row kpi-row-3"
└── styles/
    └── dashboard.css       # nenhuma mudança necessária -- a classe kpi-row-3
                             #   (grid-template-columns: repeat(3, 1fr)) ja existe
```

**Structure Decision**: Aplicação web já existente (frontend Next.js + backend ASP.NET Core +
MySQL). A mudança é puramente de apresentação e fica inteiramente contida em
`frontend/app/(app)/relatorios/financeiro/page.tsx`: remoção de um bloco JSX e troca de uma
className por outra já definida em `frontend/styles/dashboard.css`. Nenhum componente novo,
nenhuma rota nova, nenhum arquivo de backend tocado.

## Complexity Tracking

> Sem violações de constituição a justificar (Constitution Check acima = PASS). Não há decisão
> de design com mais de uma opção razoável nesta mudança: a classe `kpi-row-3` já existe no
> projeto especificamente para fileiras de 3 cards, então não há alternativa a avaliar.
