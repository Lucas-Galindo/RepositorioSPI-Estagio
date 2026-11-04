# Implementation Plan: Calendário nos campos de data

**Branch**: `069-calendario-campos-de-data` | **Date**: 2026-11-04 | **Spec**: [spec.md](spec.md)

## Summary

O componente compartilhado `DateInput` (campo de texto `dd/mm/aaaa`, 23 usos em 14 arquivos) ganha um **botão de calendário dentro do campo** que abre o **calendário do navegador** por meio de um `<input type="date">` auxiliar invisível; o dia escolhido volta como `yyyy-MM-dd`, o mesmo contrato de hoje. O botão usa só tokens de cor (contraste já verificado), e o auxiliar e os campos de competência (`type="month"`) recebem `color-scheme` claro/escuro para o ícone e o calendário nativo seguirem o tema. Os 5 campos `type="date"` nativos dos formulários de turma de pacote e de plano passam a usar o `DateInput`. **Somente frontend**: sem backend, sem banco, sem componente novo. Abordagem e alternativas: [research.md](research.md); contrato do componente: [contracts/date-input.md](contracts/date-input.md).

## Technical Context

**Language/Version**: TypeScript 5 / Next.js 16.3.4 / React 19.2.8 (somente frontend)

**Primary Dependencies**: nenhuma nova; reaproveita o `Icon` (`cal`), os tokens de tema e o `verificar-contraste`

**Storage**: nenhuma mudança

**Testing**: `tsc`, `eslint`, `npm run verificar-contraste`, `npm run build` e Chrome automatizado (Playwright) em claro e escuro, como nas specs 063 a 068; não há testes de unidade de interface no projeto

**Target Platform**: navegador desktop e celular (360 px); Chrome, Edge, Firefox e Safari atuais

**Project Type**: aplicação web existente (`frontend/`)

**Performance Goals**: sem consulta nova; o auxiliar é um campo estático por `DateInput`

**Constraints**: valor `yyyy-MM-dd` inalterado; sem `new Date`; sem cor literal; sem componente novo; sem rolagem horizontal em 360 px; backend intocado

**Scale/Scope**: 4 arquivos de frontend alterados (`DateInput.tsx`, `dashboard.css`, `TurmaForm.tsx`, `VinculoCobrancaFormModal.tsx`), 0 novos; 4 campos de competência só pela regra de CSS

## Constitution Check

| Princípio | Avaliação |
|---|---|
| I. Exclusão lógica | ✅ Sem dados envolvidos |
| II. Validação única no backend | ✅ O componente continua só aceitando/emitindo texto; a validação real das datas segue no servidor |
| III. Sem capacidade configurável sem implementação | ✅ Nenhuma opção nova |
| IV. Segredos | ✅ Sem impacto |
| V. Documentação como registro vinculante | ✅ Notas curtas nas specs 062 e 063 (os formulários que trocaram o campo nativo) apontando para esta; relatório ao final |

**Re-avaliação pós-design**: sem violações. Complexity Tracking vazio.

## Project Structure

### Documentation (this feature)

```text
specs/069-calendario-campos-de-data/
├── spec.md
├── plan.md            # este arquivo
├── research.md        # decisões R1–R8
├── contracts/date-input.md
├── quickstart.md
├── checklists/requirements.md
├── tasks.md           # /speckit-tasks
└── analysis.md        # /speckit-analyze
```

### Source Code (repository root)

```text
frontend/
├── components/shared/DateInput.tsx            # + botão, + auxiliar nativo, + ariaLabel, style e min
├── styles/dashboard.css                       # + .date-wrap, .date-cal-btn, .date-nativo; color-scheme (auxiliar e type="month")
├── components/turmas/TurmaForm.tsx            # 2 campos nativos -> DateInput (Fim da turma usa a nova propriedade min)
└── components/alunos/VinculoCobrancaFormModal.tsx   # 3 campos nativos -> DateInput (inclui as parcelas)
```

**Structure Decision**: nenhuma pasta nem arquivo novo de código; os 4 campos de competência só se beneficiam da regra de CSS (nenhuma edição nas 4 páginas). **Nada em `src/`, `tests/` ou `database/`.**

## Ordem sugerida (insumo do /speckit-tasks)

1. Linha de base (backup do estado, `tsc`/`eslint`/contraste/`git status`).
2. `DateInput` + CSS (US1 e US2) e verificação em Chrome numa tela de filtro.
3. Migrar os campos nativos (US3) e a regra dos campos de mês.
4. Notas de Princípio V, relatório, verificação final em todas as telas (claro, escuro, 360 px), build e Trello.

## Verificações "MUST NOT" que o /speckit-tasks deve cobrir (regra do CLAUDE.md)

| Requisito | Verificação exigida |
|---|---|
| FR-003 sem conversão de data | **leitura do diff** de `DateInput.tsx`: nenhuma ocorrência de `new Date(`/`Date.parse`/`toISOString`; `grep` no arquivo sem resultado |
| FR-010 backend intocado | `git status --porcelain -- src tests database` vazio e `git diff --stat -- src tests database` vazio |
| FR-011 sem componente novo e sem cor literal | `git status --porcelain -- frontend` sem arquivo novo (`??`); `grep -nE "#[0-9a-fA-F]{3,6}|rgb\(" ` nas linhas adicionadas ao CSS e ao componente sem resultado |
| FR-012 nenhum campo nativo fora do componente | `grep -rn 'type="date"' frontend/app frontend/components` mostra **somente** o auxiliar dentro de `DateInput.tsx` |
| FR-010/SC-004 mesmo valor, mesma requisição | no Chrome, preencher um filtro digitando e depois pelo calendário e comparar a requisição enviada |

## Complexity Tracking

Sem violações da constituição a justificar.
