# Implementation Plan: Modo de Cores Personalizado

**Branch**: `051-cores-personalizadas` | **Date**: 2026-10-13 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/051-cores-personalizadas/spec.md`

## Summary

Adiciona "Personalizado" como quinta opção em `ColorMode` (`frontend/contexts/ThemeContext.tsx`), ao lado de `normal`/`protanopia`/`deuteranopia`/`tritanopia`. Quando ativo, 4 novos campos de preferência (espelhando `--c-accent`/`--c-accent2`/`--c-accent3`/`--c-danger`) ficam livres para o usuário escolher, em vez de fixos como nos presets de daltonismo. `AppShell.tsx` passa a injetar as 4 cores (e suas variantes `-deep`/`-tint`, já calculadas pelos helpers existentes `darkenHex`/`hexToRgba`) como CSS custom properties quando `colorMode === "personalizado"`, mesmo mecanismo já usado hoje para a cor de destaque única. Na tela de Configurações, o bloco "Cor de destaque" passa a mostrar 4 seletores (em vez de 1) quando o modo Personalizado está ativo. Nenhum mecanismo novo de persistência — mesmo `localStorage`/chave já usada, só com mais campos no mesmo objeto.

## Technical Context

**Language/Version**: TypeScript / Next.js App Router (frontend) — nenhuma mudança de backend.

**Primary Dependencies**: Nenhuma nova — reaproveita `darkenHex`/`hexToRgba` (`frontend/lib/color.ts`), já existentes e usados exatamente para este propósito.

**Storage**: `localStorage` do navegador, mesma chave já usada (`spi:preferencias-tema`) — objeto `Preferencias` ganha 4 campos novos, nenhuma chave nova.

**Testing**: Nenhuma suíte automatizada de frontend (mesma decisão já aceita nas specs 020-050) — validação via `quickstart.md`.

**Target Platform**: Web (Next.js App Router), mesma stack já em produção/dev local.

**Project Type**: Web application — esta feature é só frontend (preferência de UI local).

**Performance Goals**: Sem meta nova.

**Constraints**:
- FR-001/FR-007: `ColorMode` ganha o valor `"personalizado"`; trocar de/para esse modo MUST NOT apagar os 4 campos de cor custom (eles continuam no objeto `Preferencias` independente de qual `colorMode` está ativo no momento — mesmo padrão já usado hoje para `accentColor`, que não é zerado quando o usuário troca para um modo de daltonismo e depois volta para "Padrão").
- FR-003: valor inicial dos 4 campos novos = os mesmos 4 valores do preset "Padrão" (`#4ecdc4`, `#bb9457`, `#ffe66d`, `#c0392b`), usados como padrão tanto no `COLOR_MODES[0].swatches` já existente em `configuracoes/page.tsx` quanto no `:root` de `tokens.css`.
- FR-004: `AppShell.tsx` MUST injetar as 4 variáveis (+ variantes `-tint`, e `-deep` só para `--c-accent`, que é a única com variante `-deep` hoje usada em `tokens.css`) quando `colorMode === "personalizado"`, usando os mesmos helpers já existentes — MUST NOT introduzir nenhum novo cálculo de cor.
- FR-005: botão "Restaurar padrão" MUST resetar só os 4 campos de cor, sem chamar `setColorMode` — continua em "Personalizado" depois.
- FR-008: nenhuma chamada de API em nenhum ponto desta feature.
- FR-009: nenhuma validação de contraste/legibilidade é adicionada — os 4 `<input type="color">` aceitam qualquer valor, sem restrição.

**Scale/Scope**: 3 arquivos modificados — `ThemeContext.tsx` (tipo `ColorMode`, 4 campos novos em `Preferencias`, novos setters), `AppShell.tsx` (injeção condicional das 4 variáveis), `configuracoes/page.tsx` (5ª opção no grid de modos + 4 seletores em vez de 1). Nenhum arquivo novo, nenhuma mudança de backend/banco.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Princípio | Aplica-se? | Avaliação |
|---|---|---|
| I. Exclusão Lógica, Nunca Física | Não | Nenhuma exclusão de registro envolvida. |
| II. Validação de Negócio Única e Centralizada no Backend | Não | Nenhuma regra de negócio — preferência de UI local, sem backend envolvido. |
| III. Nenhuma Capacidade Configurável Sem Implementação Real (NON-NEGOTIABLE) | Sim | A capacidade (escolher 4 cores) é implementada de ponta a ponta nesta mesma fatia — nenhuma opção exposta sem efeito real na interface. |
| IV. Autenticação e Segredos Seguros por Padrão | Não | Preferência de cor não é segredo — mesma categoria já decidida para `accentColor`/`colorMode` (uso de `localStorage`, não de um mecanismo de segredo). |
| V. Documentação Retroativa como Registro Histórico Vinculante | Não | Feature nova; não diverge de nenhuma spec retroativa — estende um mecanismo de UI já existente. |

**Resultado**: Sem violação.

## Project Structure

### Documentation (this feature)

```text
specs/051-cores-personalizadas/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── checklists/requirements.md
└── tasks.md             # Phase 2 output (/speckit-tasks - NOT created by /speckit-plan)
```

Sem diretório `contracts/` — nenhuma API externa envolvida (preferência de UI 100% local).

### Source Code (repository root)

```text
frontend/
├── contexts/ThemeContext.tsx                       # MODIFICADO: ColorMode ganha "personalizado"; Preferencias ganha 4 campos; novos setters/reset
├── components/layout/AppShell.tsx                   # MODIFICADO: injeta as 4 variáveis CSS quando colorMode === "personalizado"
└── app/(app)/configuracoes/page.tsx                  # MODIFICADO: 5ª opção no grid "Modo de cores"; bloco de cor mostra 4 seletores quando Personalizado está ativo
```

**Structure Decision**: Nenhum arquivo novo. A mudança é uma extensão direta de 3 arquivos já existentes, reaproveitando o mecanismo de preferências/CSS custom properties já em produção.

## Post-Design Constitution Check

*Re-avaliação após Phase 1 (research.md, data-model.md, quickstart.md).*

- Princípio III: `data-model.md` confirma que os 4 campos novos têm efeito real e completo (aplicados via `AppShell.tsx`) — nenhuma opção "fantasma".
- Nenhum dos outros princípios é tocado pelo design.

**Resultado**: Sem violação bloqueante.

## Complexity Tracking

Não aplicável — nenhuma violação identificada em nenhum dos dois Constitution Checks.
