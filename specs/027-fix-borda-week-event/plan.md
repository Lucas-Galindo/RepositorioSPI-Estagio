# Implementation Plan: Corrigir Borda Curva Sobrepondo Texto na Agenda Semanal da Home

**Branch**: `027-fix-borda-week-event` | **Date**: 2026-09-17 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/027-fix-borda-week-event/spec.md`

**Note**: This template is filled in by the `/speckit-plan` command; its definition describes the execution workflow.

## Summary

Corrigir três artefatos visuais em `.week-event` (card de evento da agenda semanal da Home,
`frontend/styles/dashboard.css`), todos resolvidos copiando fielmente o comportamento já
correto do componente irmão `.cal-event` (tela cheia de Agenda): (1) `border-radius: 11px`
combinado com `padding` maior fazia a borda colorida da esquerda curvar de forma exagerada,
sobrepondo o texto — corrigido trocando `border-radius`/`padding` pelos valores exatos de
`.cal-event` (7px / `4px 6px`); (2) `.week-event` é um `<Link>` real (âncora), diferente de
`.cal-event` (um `<div onClick>`), então herdava o estilo padrão de link do navegador (azul,
sublinhado) e usava um separador `·` entre horário e matéria — corrigido com
`color: inherit`/`text-decoration: none` no CSS e removendo o separador no JSX
(`frontend/app/(app)/dashboard/page.tsx`); (3) `.week-event` nunca teve `display` definido,
herdando `display: inline` de `<a>` enquanto contém filhos de bloco (`.t`/`.s`) — cenário
"block-in-inline" que fazia a borda/fundo renderizar separados do texto — corrigido com
`display: block`, mesmo princípio já usado por `.lesson-item` (que usa `flex` por precisar de
alinhamento horizontal, diferente do empilhamento vertical de `.week-event`). Nenhuma outra
tela ou componente tocado.

## Technical Context

**Language/Version**: CSS puro + um trecho de JSX/TypeScript (nenhuma linguagem/framework novo)
— `frontend/styles/dashboard.css` e `frontend/app/(app)/dashboard/page.tsx`, já usados pela
aplicação Next.js

**Primary Dependencies**: Nenhuma — ajuste de propriedades CSS padrão (`border-radius`,
`padding`, `color`, `text-decoration`) e remoção de um caractere literal em uma string JSX

**Storage**: N/A

**Testing**: Sem framework de teste automatizado configurado no projeto — validação visual
manual via quickstart.md (mesma limitação já registrada em specs anteriores)

**Target Platform**: Navegador web (tela Home já existente)

**Project Type**: Web application (frontend Next.js já existente) — esta correção toca um
arquivo de estilo e um trecho de JSX no mesmo componente

**Performance Goals**: N/A — mudança de renderização CSS/texto sem custo de performance

**Constraints**: A correção MUST NOT alterar a aparência de `.cal-event` (tela cheia de Agenda)
nem de nenhum outro seletor além de `.week-event` (FR-003); MUST preservar as cores de status já
existentes (`.week-event.st-realizada`, `.week-event.st-cancelada`) e o arredondamento dos
cantos do card (FR-004, FR-005); MUST NOT alterar o `href`/comportamento de navegação do
`<Link>` (FR-007); a borda decorativa e o texto MUST sempre renderizar como uma única caixa
visual (FR-009)

**Scale/Scope**: 2 arquivos alterados (`frontend/styles/dashboard.css`,
`frontend/app/(app)/dashboard/page.tsx`) — ajuste de valor em 2 propriedades CSS existentes,
adição de 3 propriedades CSS novas (`display`, `color`, `text-decoration`) à mesma regra
`.week-event`, e remoção de um separador literal em 1 linha de JSX — nenhum arquivo novo,
nenhum componente React novo

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Avaliação contra os 5 princípios ratificados em `.specify/memory/constitution.md` (v1.0.0):

| Princípio | Aplicável? | Avaliação |
|---|---|---|
| I. Exclusão Lógica, Nunca Física | Não (observado) | Nenhum registro de domínio é excluído ou alterado — é uma correção visual sem relação com dados persistidos |
| II. Validação de Negócio Única e Centralizada no Backend | Não (observado) | Nenhuma regra de negócio é criada, alterada ou duplicada — mudança restrita a uma propriedade de estilo CSS |
| III. Nenhuma Capacidade Configurável Sem Implementação Real | Não (observado) | Nenhuma opção configurável nova é exposta; a correção não adiciona nem remove nenhuma capacidade funcional, só ajusta como algo já existente é desenhado |
| IV. Autenticação e Segredos Seguros por Padrão | Não (observado) | Nenhuma mudança de autenticação, sessão ou segredo envolvida |
| V. Documentação Retroativa como Registro Histórico Vinculante | Não (observado) | Nenhuma spec retroativa (001-019) descreve o estilo visual deste card como uma decisão vinculante a preservar; não há conflito documental a resolver |

**Resultado**: PASS — nenhum princípio é violado. Mudança de risco mínimo, puramente visual,
sem impacto em regra de negócio, dado persistido ou segurança.

**Re-check pós-design (Phase 1)**: PASS, sem mudanças. O research.md confirmou que a correção
não depende de nenhuma outra regra CSS nem de `.cal-event`, reforçando que o escopo permanece
restrito a um único seletor.

## Project Structure

### Documentation (this feature)

```text
specs/027-fix-borda-week-event/
├── plan.md              # This file (/speckit-plan command output)
├── research.md          # Phase 0 output (/speckit-plan command)
└── quickstart.md        # Phase 1 output (/speckit-plan command)
```

Sem `data-model.md` nem `contracts/`: esta correção não envolve entidades de dados nem
contratos de API novos/alterados (ver spec.md "Key Entities": não aplicável) — é uma mudança de
apresentação isolada.

### Source Code (repository root)

```text
frontend/
├── styles/
│   └── dashboard.css        # regra .week-event: border-radius 11px->7px, padding 8px 9px->4px 6px
│                             #   (copiados de .cal-event); + display: block; (evita block-in-inline);
│                             #   + color: inherit; + text-decoration: none;
└── app/(app)/dashboard/
    └── page.tsx              # remove o separador "·" entre horário e matéria no card .week-event
```

**Structure Decision**: Aplicação web já existente (frontend Next.js). A correção fica contida
em `frontend/styles/dashboard.css` (regra `.week-event`) e em
`frontend/app/(app)/dashboard/page.tsx` (a única página que renderiza `.week-event`) — nenhum
componente React novo, arquivo novo ou outra regra CSS é tocado.

## Complexity Tracking

> Sem violações de constituição a justificar (Constitution Check acima = PASS). Não há decisão
> de design com mais de uma opção razoável não descartada — a alternativa de reduzir o
> `border-radius` geral (como um raio menor) foi avaliada e rejeitada no research.md por não
> eliminar o artefato de forma garantida, só reduzi-lo.
