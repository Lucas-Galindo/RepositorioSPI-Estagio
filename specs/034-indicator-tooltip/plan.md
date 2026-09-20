# Implementation Plan: Tooltip Explicativo para Cards de Indicador

**Branch**: `034-indicator-tooltip` | **Date**: 2026-09-19 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/034-indicator-tooltip/spec.md`

**Note**: This template is filled in by the `/speckit-plan` command; its definition describes the execution workflow.

## Summary

Criar um componente `InfoTooltip` genérico e reutilizável em `frontend/components/shared/`, acionado por um ícone `info` novo (adicionado ao registro de ícones já existente), que recebe o texto explicativo como prop e exibe um balão via hover, toque ou foco de teclado (CSS-only, `:hover`/`:focus-within`, sem nova dependência). Aplicar esse componente a todos os 10 cards/tabela de indicador atualmente renderizados nas 4 telas identificadas na investigação prévia da spec (Dashboard, Financeiro — Visão Geral, Relatório Financeiro — Visão Financeiro/Indicadores, Relatório de Turmas), usando os textos já definidos na spec.

## Technical Context

**Language/Version**: TypeScript / React 19 (Next.js 16, App Router), mesma versão já usada no projeto.

**Primary Dependencies**: Nenhuma nova dependência — `frontend/package.json` não tem biblioteca de UI/tooltip hoje (confirmado na investigação prévia da spec) e o componente é implementado com CSS puro (`:hover`/`:focus-within`) e um `<button>` nativo, consistente com os demais componentes de `frontend/components/shared/`.

**Storage**: N/A — componente de apresentação puro, sem dado persistido ou consultado; os textos de tooltip são literais definidos na spec (tabela "Textos de tooltip por indicador").

**Testing**: Não há framework de teste automatizado no frontend hoje (`frontend/package.json` não tem Jest/Vitest/Testing Library — confirmado). Validação é manual: rodar `npm run dev` e verificar hover/toque/foco de teclado em cada uma das 4 telas listadas, seguindo o padrão já estabelecido nesta sessão para mudanças de frontend (iniciar o servidor e testar no navegador antes de reportar concluído).

**Target Platform**: Next.js App Router (`frontend/app/(app)/...`), sem nenhuma mudança de backend (`SPI.Api`/`SPI.Application`) — feature 100% frontend.

**Project Type**: Web application (estrutura já existente do repositório) — mudança isolada ao frontend.

**Performance Goals**: Sem meta nova — CSS-only, sem nenhuma chamada de rede adicional, sem re-render condicionado a estado externo.

**Constraints**: O ícone de informação MUST ser alcançável por Tab e expor o texto a leitores de tela via `aria-label`/`aria-describedby` (FR-002a, decisão da clarificação); o componente MUST NOT introduzir nenhuma dependência nova (Assumptions da spec); o texto de cada tooltip é definido por prop, nunca fixo dentro do componente (FR-001).

**Scale/Scope**: 1 componente novo (`InfoTooltip.tsx`), 1 ícone novo adicionado ao registro existente (`Icon.tsx`), regras de CSS novas em `frontend/styles/dashboard.css`, e edições pontuais em 4 arquivos de página para inserir o componente nos 10 indicadores em escopo (ver data-model.md para a lista completa).

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Princípio | Aplica-se? | Avaliação |
|---|---|---|
| I. Exclusão Lógica, Nunca Física | Não | Nenhuma exclusão de registro envolvida — feature de apresentação pura. |
| II. Validação de Negócio Única e Centralizada no Backend | Não | Não introduz nenhuma regra de validação de negócio, nem no frontend nem no backend — apenas texto explicativo estático. |
| III. Nenhuma Capacidade Configurável Sem Implementação Real | Não | Não introduz nenhuma opção configurável pelo usuário. |
| IV. Autenticação e Segredos Seguros por Padrão | Não | Sem relação com autenticação/segredos. |
| V. Documentação Retroativa como Registro Histórico Vinculante | Não diretamente | Não altera nenhum comportamento/cálculo documentado nas specs retroativas (013, 016, 022 etc.) — a investigação prévia da spec já cita essas specs apenas para confirmar quais indicadores têm card ativo, sem propor nenhuma mudança a elas. |

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
frontend/
├── components/
│   └── shared/
│       ├── Icon.tsx              # adiciona entrada "info" ao registro PATHS existente
│       └── InfoTooltip.tsx       # componente novo: recebe `text` (e `label`? opcional) como prop
├── styles/
│   └── dashboard.css             # novas regras .info-tooltip / .info-tooltip-trigger / .info-tooltip-bubble
└── app/
    └── (app)/
        ├── dashboard/page.tsx                  # "Recebido este mês" (.card-eyebrow)
        ├── financeiro/page.tsx                 # "Saldo realizado (mês)", "Saldo previsto" (.kpi .label)
        └── relatorios/
            ├── financeiro/page.tsx             # "Recebido", "Pago", "Saldo realizado", "Receita pendente",
            │                                    # "Despesa pendente", "Saldo previsto", "Inadimplência",
            │                                    # "Prazo médio de atraso", tabela "Fluxo de caixa (últimos 6 meses)"
            └── turmas/page.tsx                 # "Ocupação média" (.kpi .label)
```

**Structure Decision**: Mudança isolada ao frontend (`frontend/`) — nenhum arquivo de `src/` (backend .NET) é tocado. Um componente novo reutilizável (`InfoTooltip.tsx`) mais uma entrada de ícone nova, aplicados via edições pontuais nos 4 arquivos de página que já renderizam os 10 indicadores em escopo (ver data-model.md).

## Post-Design Constitution Check

*Re-avaliação após Phase 1 (research.md, data-model.md, quickstart.md).*

Nenhuma mudança em relação ao Constitution Check inicial. Confirmações após o design:
- `research.md` (R1-R2) confirma que nenhuma dependência nova é introduzida — CSS puro mais um
  ícone adicionado ao registro já existente (Princípio III não se aplica: não há capacidade
  configurável nova, apenas apresentação estática).
- `data-model.md` confirma que os 12 pontos de inserção mapeados cobrem exatamente os
  indicadores com card ativo hoje, e que os 3 indicadores sem card (Fluxo de Caixa Operacional,
  Gargalo de Caixa, Taxa de Ocupação real) permanecem fora de escopo — nenhuma spec retroativa
  precisa ser atualizada, pois nenhum cálculo documentado nelas muda.

**Resultado**: Nenhuma violação. Nenhuma linha em Complexity Tracking necessária.

## Complexity Tracking

Não aplicável — nenhuma violação identificada em nenhum dos dois Constitution Checks.
