# Implementation Plan: Ajustes de Layout — Turmas e Pagamentos

**Branch**: `020-ajustes-layout-turmas-pagamentos` | **Date**: 2026-09-11 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/020-ajustes-layout-turmas-pagamentos/spec.md`

**Note**: This template is filled in by the `/speckit-plan` command; its definition describes the execution workflow.

## Summary

Três correções de CSS/layout, sem alteração de dados, API ou lógica de negócio: (1) centralizar o estado vazio da lista de Turmas, hoje deslocado para a esquerda por ser o único item dentro de uma grade CSS de 3 colunas; (2) aumentar o espaçamento entre o `<select>` de aluno e o botão "Vincular" na tela de detalhe de Turma, hoje com `gap: 8px`, causando cliques acidentais; (3) mover a seção "Métodos de pagamento" (título + tabela de formas cadastradas) da aba Pagamentos para acima da tabela de registros de pagamento, hoje posicionada abaixo dela. A abordagem técnica é reaproveitar os componentes e classes CSS já existentes no projeto, sem introduzir novo componente, dependência ou padrão visual.

## Technical Context

**Language/Version**: TypeScript / React 19 (Next.js 16, App Router) para o frontend afetado; nenhuma mudança em C#/.NET (backend não é tocado por esta feature)

**Primary Dependencies**: Next.js, React — nenhuma dependência nova necessária; ajustes usam CSS já presente em `frontend/styles/dashboard.css` e classes utilitárias já existentes (`report-grid`, `empty-state`, `field-row`, `panel`, `table-wrap`)

**Storage**: N/A — nenhuma mudança de dado, schema ou chamada de API

**Testing**: Verificação visual manual (o projeto não tem framework de teste de UI configurado — sem Jest/Vitest/Playwright no `frontend/package.json`); validação via `npm run dev` e inspeção visual nas 3 telas afetadas, conforme os Acceptance Scenarios do spec

**Target Platform**: Navegador web (aplicação já responsiva; mudanças devem se manter corretas também em largura mobile, conforme Edge Cases do spec)

**Project Type**: Web application (frontend Next.js + backend ASP.NET Core já existentes — esta feature toca somente `frontend/`)

**Performance Goals**: N/A — mudança puramente visual, sem impacto de performance perceptível

**Constraints**: Não alterar comportamento, dados, filtros ou validações das telas afetadas (FR-004); não introduzir novo componente ou dependência visual; manter compatibilidade com o restante do design system já em uso (`dashboard.css`)

**Scale/Scope**: 3 arquivos de frontend a alterar: `frontend/app/(app)/turmas/page.tsx` (ou `frontend/styles/dashboard.css`, classe `.report-grid`/`.empty-state`), `frontend/app/(app)/turmas/[id]/page.tsx`, `frontend/app/(app)/pagamentos/page.tsx`

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

`.specify/memory/constitution.md` ainda está com os placeholders do template ([PRINCIPLE_1_NAME] etc.), sem nenhum princípio real ratificado neste projeto — não há gates de constituição aplicáveis para verificar. Gate considerado **PASS** por ausência de constituição definida.

## Project Structure

### Documentation (this feature)

```text
specs/020-ajustes-layout-turmas-pagamentos/
├── plan.md              # This file (/speckit-plan command output)
├── research.md          # Phase 0 output (/speckit-plan command)
├── data-model.md        # Phase 1 output (/speckit-plan command)
├── quickstart.md        # Phase 1 output (/speckit-plan command)
└── tasks.md             # Phase 2 output (/speckit-tasks command - NOT created by /speckit-plan)
```

Sem diretório `contracts/`: esta feature não expõe nem consome nenhuma interface externa nova (nenhuma rota de API, contrato de dados ou schema é criado/alterado) — é puramente CSS/JSX de reordenação e espaçamento em páginas já existentes.

### Source Code (repository root)

```text
# Option 2: Web application (frontend Next.js + backend ASP.NET Core já existentes)
# Esta feature toca SOMENTE o frontend — nenhum arquivo de backend/ é alterado.

frontend/
├── app/(app)/
│   ├── turmas/
│   │   ├── page.tsx          # US1: estado vazio "Nenhuma turma encontrada" (linha ~51)
│   │   └── [id]/
│   │       └── page.tsx      # US2: espaçamento select+botão "Vincular" (linha ~160)
│   └── pagamentos/
│       └── page.tsx          # US3: mover bloco "Métodos de pagamento" (linhas ~194-219) para antes da tabela (linha ~145)
└── styles/
    └── dashboard.css         # Possível ajuste em .report-grid / .empty-state (US1) e/ou .field-row (US2), se o fix for melhor feito via CSS global do que inline
```

**Structure Decision**: Aplicação web já existente (Next.js App Router no frontend, ASP.NET Core no backend). Esta feature é restrita a 3 arquivos de página (`.tsx`) dentro de `frontend/app/(app)/`, com possível ajuste complementar em `frontend/styles/dashboard.css` caso a correção do estado vazio (US1) seja mais bem resolvida no CSS compartilhado (`.report-grid`) do que localmente. Nenhuma nova pasta, rota ou camada é criada.

## Complexity Tracking

> Sem violações de constituição a justificar — não há constituição real ratificada neste projeto (ver Constitution Check acima), e a feature em si é de baixa complexidade (3 ajustes visuais localizados, sem novo componente/dependência/padrão).
