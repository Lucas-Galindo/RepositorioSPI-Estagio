# Implementation Plan: Lançamentos Clicáveis na Tabela de Indicadores

**Branch**: `045-lancamentos-clicaveis` | **Date**: 2026-10-03 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/045-lancamentos-clicaveis/spec.md`

## Summary

Torna cada linha da tabela de lançamentos (aba Indicadores do Relatório Financeiro, spec 022) clicável, navegando para a tela de detalhe já existente do registro de origem — Contas a Receber para entradas, Contas a Pagar para saídas. A investigação prévia (registrada no spec) já confirmou que **nenhuma mudança de backend é necessária**: a API já expõe `Id` e `Tipo` com os valores corretos. A implementação é cirúrgica, só no frontend: adicionar `className="row-link"` e um `onClick` com `router.push(...)` em cada `<tr>` da tabela, exatamente como já é feito na lista de Contas a Pagar (`frontend/app/(app)/financeiro/contas-a-pagar/page.tsx`, linha ~196) — mesmo padrão, zero mecanismo novo.

## Technical Context

**Language/Version**: TypeScript / Next.js App Router 16.3.4 (frontend) — nenhuma mudança de backend (C# / .NET) nesta feature.

**Primary Dependencies**: Nenhuma nova — reaproveita `useRouter` de `next/navigation` (já importado e usado em `contas-a-pagar/page.tsx`) e a classe CSS `row-link` já existente.

**Storage**: N/A — nenhuma mudança de banco de dados ou de API.

**Testing**: Nenhuma suíte automatizada de frontend (mesma decisão já aceita nas specs 020-044) — validação via `quickstart.md`.

**Target Platform**: Web (Next.js App Router), mesma stack já em produção/dev local.

**Project Type**: Web application — esta feature é só frontend.

**Performance Goals**: Sem meta nova.

**Constraints**:
- FR-004/FR-006: MUST NOT criar tela nova nem exigir mudança de backend — confirmado viável pela investigação prévia do spec (API já traz `Id`/`Tipo` reais).
- FR-007/FR-008: MUST NOT alterar o cálculo, os filtros Entradas/Saídas, nem os demais indicadores da aba (spec 022) — a mudança é restrita ao `onClick`/classe CSS da linha.
- FR-009 (preservar filtros ao voltar): decisão tomada em R1 (research.md) — não introduzir sincronização de filtros com a URL (mudança de arquitetura desproporcional a um requisito `SHOULD` de prioridade P3); reaproveitar o comportamento de navegação "voltar" do navegador exatamente como ele já se comporta hoje em qualquer outra lista→detalhe→voltar do sistema (ex.: Contas a Pagar), verificando empiricamente durante a implementação se esse comportamento já preserva o estado local da página de relatório.

**Scale/Scope**: 1 arquivo modificado (`frontend/app/(app)/relatorios/financeiro/page.tsx`); nenhum arquivo novo; nenhuma mudança em `src/` (backend) ou `database/`.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Princípio | Aplica-se? | Avaliação |
|---|---|---|
| I. Exclusão Lógica, Nunca Física | Não | Esta feature não exclui nem desativa nenhum registro — é só navegação de UI. |
| II. Validação de Negócio Única e Centralizada no Backend | Não | Não introduz nenhuma regra de negócio nova (nem no frontend, nem no backend) — só roteamento entre telas já existentes. |
| III. Nenhuma Capacidade Configurável Sem Implementação Real (NON-NEGOTIABLE) | Sim, de forma positiva | A capacidade exposta (clicar e navegar) já tem implementação real completa por trás — API já traz `Id`/`Tipo` corretos, telas de detalhe já existem e funcionam. Nenhum risco de expor uma opção sem backend real. |
| IV. Autenticação e Segredos Seguros por Padrão | Não | Nenhuma mudança de autenticação, sessão ou segredo. |
| V. Documentação Retroativa como Registro Histórico Vinculante | Não | Feature nova; não diverge de nenhuma spec retroativa — só estende a spec 022 (ainda vigente, comportamento preservado por FR-007/FR-008). |

**Resultado**: Sem violação.

## Project Structure

### Documentation (this feature)

```text
specs/045-lancamentos-clicaveis/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md         # Phase 1 output
├── quickstart.md        # Phase 1 output
├── checklists/requirements.md
└── tasks.md             # Phase 2 output (/speckit-tasks - NOT created by /speckit-plan)
```

Sem diretório `contracts/` — não há nenhum contrato de API novo ou alterado (FR-006); mesma decisão já tomada em specs frontend-only anteriores (ex.: [021-fix-hitbox-cliques](../021-fix-hitbox-cliques/plan.md)).

### Source Code (repository root)

```text
frontend/
└── app/(app)/relatorios/financeiro/page.tsx   # MODIFICADO: linhas da tabela de lançamentos (linhas ~437-442) ganham className="row-link" + onClick com router.push para o detalhe correspondente
```

**Structure Decision**: Nenhum arquivo novo, nenhum diretório novo. A mudança inteira vive dentro de um único arquivo já existente, no mesmo componente de tabela introduzido pela spec 022 — consistente com o pedido original ("reaproveitar telas existentes, sem criar tela nova").

## Post-Design Constitution Check

*Re-avaliação após Phase 1 (research.md, data-model.md, quickstart.md).*

- Princípio III: `data-model.md` confirma que nenhum campo novo é necessário em `LancamentoIndicador` — `id`/`tipo` já existem e já são usados hoje (como `key` da linha). Nenhuma opção exposta sem implementação real.
- Nenhum dos outros princípios é tocado pelo design (mesma conclusão da Constitution Check inicial).

**Resultado**: Sem violação bloqueante.

## Complexity Tracking

Não aplicável — nenhuma violação identificada em nenhum dos dois Constitution Checks.
