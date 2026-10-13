# Implementation Plan: Persistir Filtros da Visão de Indicadores

**Branch**: `048-persistir-filtros-indicadores` | **Date**: 2026-10-08 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/048-persistir-filtros-indicadores/spec.md`

## Summary

Persiste os 9 pedaços de estado local de `frontend/app/(app)/relatorios/financeiro/page.tsx` (`aba`, `inicio`, `fim`, `formaPagamentoId`, `alunoId`, `turmaId`, `materiaId`, `semestre`, `tipoLancamento`) em `sessionStorage`, lidos só após o mount (mesmo padrão de segurança de hidratação já usado em `ThemeContext.tsx` para `localStorage`) e gravados a cada mudança. Como os `useEffect` que buscam os dados da página já são reativos a esses mesmos `useState`, restaurar os valores já dispara o recarregamento correto automaticamente — nenhuma chamada de API nova, nenhum recálculo no frontend. O botão "Limpar filtros" (`filter-clear`) já existe e só precisa ser estendido para também considerar/limpar o filtro Entradas/Saídas (`tipoLancamento`), que hoje fica de fora da conta de `temFiltro`/`limparFiltros`.

## Technical Context

**Language/Version**: TypeScript / Next.js App Router (frontend) — nenhuma mudança de backend.

**Primary Dependencies**: Nenhuma nova — `sessionStorage` é API nativa do navegador, já com precedente de uso seguro de storage do navegador em `ThemeContext.tsx` (lá com `localStorage`, aqui com `sessionStorage` pela semântica de vida mais curta exigida por FR-004).

**Storage**: `sessionStorage` do navegador (chave `"spi:filtros-relatorio-financeiro"`, seguindo a convenção `"spi:<descrição>"` já usada por `ThemeContext.tsx`). Nenhuma mudança de banco de dados ou de API.

**Testing**: Nenhuma suíte automatizada de frontend (mesma decisão já aceita nas specs 020-047) — validação via `quickstart.md`.

**Target Platform**: Web (Next.js App Router), mesma stack já em produção/dev local.

**Project Type**: Web application — esta feature é só frontend.

**Performance Goals**: Sem meta nova.

**Constraints**:
- FR-004/FR-011: `sessionStorage` (não `localStorage`, não URL) — vida da aba/janela, nunca entre sessões, nunca compartilhado com outra página (chave exclusiva desta página).
- FR-008/FR-009: a restauração MUST só popular os `useState` já existentes — MUST NOT introduzir nenhuma chamada de API nova nem recalcular nada que os `useEffect` de busca já existentes não façam sozinhos ao reagir à mudança desses `useState`.
- FR-010: o botão "Limpar filtros" MUST continuar sendo o mesmo elemento (`className="filter-clear"`) já renderizado hoje — só a lógica de `temFiltro`/`limparFiltros` é estendida para incluir `tipoLancamento`.
- Princípio IV (segurança por padrão) não exige nada aqui — filtros de relatório não são dado sensível (mesma categoria de preferência de tema, não de credencial), então `sessionStorage` em claro (sem necessidade de hash/criptografia) é apropriado, igual à decisão já tomada para o tema.

**Scale/Scope**: 1 arquivo modificado — `frontend/app/(app)/relatorios/financeiro/page.tsx` (2 `useEffect` novos: leitura após mount e gravação a cada mudança; `temFiltro`/`limparFiltros` estendidos). Nenhum arquivo novo, nenhuma mudança de backend/banco, nenhuma mudança nas telas de detalhe (`contas-a-pagar/[id]`, `contas-a-receber/[id]`).

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Princípio | Aplica-se? | Avaliação |
|---|---|---|
| I. Exclusão Lógica, Nunca Física | Não | Nenhuma exclusão de registro envolvida. |
| II. Validação de Negócio Única e Centralizada no Backend | Sim, preservado | Nenhum cálculo novo no frontend — a restauração só repopula `useState` já existentes; os mesmos `useEffect` de busca (que já chamam a API) continuam sendo a única fonte dos números exibidos. |
| III. Nenhuma Capacidade Configurável Sem Implementação Real (NON-NEGOTIABLE) | Não | Nenhuma capacidade nova exposta ao usuário além da persistência em si (que é implementada de ponta a ponta) e do botão "Limpar filtros" (que já existe e já funciona). |
| IV. Autenticação e Segredos Seguros por Padrão | Não diretamente | Filtros de relatório não são credencial/segredo — `sessionStorage` em claro é apropriado (mesma categoria de dado não sensível já tratada assim para preferência de tema). |
| V. Documentação Retroativa como Registro Histórico Vinculante | Não | Feature nova; estende a spec 045 (contrato do detalhe inalterado) e corrige um fallback que a própria spec 045 havia deixado em aberto. |

**Resultado**: Sem violação.

## Project Structure

### Documentation (this feature)

```text
specs/048-persistir-filtros-indicadores/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── checklists/requirements.md
└── tasks.md             # Phase 2 output (/speckit-tasks - NOT created by /speckit-plan)
```

Sem diretório `contracts/` — nenhum contrato de API novo ou alterado (mesma decisão de features frontend-only anteriores: 021, 045, 046, 047).

### Source Code (repository root)

```text
frontend/
└── app/(app)/relatorios/financeiro/page.tsx   # MODIFICADO: + leitura/gravação de sessionStorage (2 useEffect novos), temFiltro/limparFiltros estendidos para incluir tipoLancamento
```

**Structure Decision**: Nenhum arquivo novo. Toda a mudança vive dentro de um único arquivo já existente, reaproveitando os `useEffect` de busca que já existem.

## Post-Design Constitution Check

*Re-avaliação após Phase 1 (research.md, data-model.md, quickstart.md).*

- Princípio II: `data-model.md` confirma que a restauração só define valores de `useState` já existentes — os `useEffect` de busca (únicos pontos que chamam a API) permanecem exatamente como estão, sem nenhuma lógica nova de cálculo.
- Nenhum dos outros princípios é tocado pelo design (mesma conclusão da Constitution Check inicial).

**Resultado**: Sem violação bloqueante.

## Complexity Tracking

Não aplicável — nenhuma violação identificada em nenhum dos dois Constitution Checks.
