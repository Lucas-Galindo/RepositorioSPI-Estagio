# Implementation Plan: Padronizar Botões da Zona de Risco

**Branch**: `046-padronizar-botoes-admin` | **Date**: 2026-10-06 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/046-padronizar-botoes-admin/spec.md`

## Summary

Corrige os dois botões "Cancelar" da Zona de Risco (`frontend/components/admin/CadastrarProfessoraForm.tsx`), que hoje usam `className="btn"` sem nenhuma variante (renderizando com a aparência padrão do navegador), trocando para `className="btn btn-ghost"` — a classe secundária já usada em todo o sistema (`ConfirmModal.tsx`, botão "Fechar" da Agenda). Os três botões destrutivos da mesma tela já usam a classe correta (`btn-danger`) e **não são tocados**, conforme já confirmado pela investigação prévia registrada no `spec.md`. Nenhuma classe CSS nova, nenhuma mudança de comportamento.

## Technical Context

**Language/Version**: TypeScript / Next.js App Router (frontend) — nenhuma mudança de backend.

**Primary Dependencies**: Nenhuma nova — reaproveita a classe `btn-ghost` já existente em `frontend/styles/dashboard.css`.

**Storage**: N/A.

**Testing**: Nenhuma suíte automatizada de frontend (mesma decisão já aceita nas specs 020-045) — validação via `quickstart.md`.

**Target Platform**: Web (Next.js App Router), mesma stack já em produção/dev local.

**Project Type**: Web application — esta feature é só frontend, e só CSS/classe (nenhuma lógica).

**Performance Goals**: Sem meta nova.

**Constraints**:
- FR-002/FR-005: MUST NOT criar classe/cor nova, MUST NOT alterar a definição de `.btn`/`.btn-ghost`/`.btn-danger`/`.btn-primary` em `dashboard.css` — só trocar qual classe é aplicada aos 2 botões "Cancelar".
- FR-003: MUST NOT tocar nos 3 botões `btn-danger` da mesma tela.
- FR-004: MUST NOT alterar nenhum `onClick`/handler — só o atributo `className`.

**Scale/Scope**: 1 arquivo modificado (`frontend/components/admin/CadastrarProfessoraForm.tsx`), 2 ocorrências de `className="btn"` → `className="btn btn-ghost"`. Nenhum arquivo novo, nenhuma mudança de backend/banco/CSS.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Princípio | Aplica-se? | Avaliação |
|---|---|---|
| I. Exclusão Lógica, Nunca Física | Não | Nenhuma exclusão de registro envolvida — é só uma troca de classe CSS. |
| II. Validação de Negócio Única e Centralizada no Backend | Não | Nenhuma regra de negócio nova ou alterada. |
| III. Nenhuma Capacidade Configurável Sem Implementação Real (NON-NEGOTIABLE) | Não | Nenhuma capacidade nova exposta — a correção só torna visualmente correta uma capacidade (cancelar) que já funciona. |
| IV. Autenticação e Segredos Seguros por Padrão | Não | Nenhuma mudança de autenticação/segredo. |
| V. Documentação Retroativa como Registro Histórico Vinculante | Não | Feature nova, não diverge de nenhuma spec retroativa. |

**Resultado**: Sem violação. Nenhum princípio se aplica diretamente — feature puramente cosmética, de baixíssimo risco.

## Project Structure

### Documentation (this feature)

```text
specs/046-padronizar-botoes-admin/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output (N/A — sem entidades)
├── quickstart.md        # Phase 1 output
├── checklists/requirements.md
└── tasks.md             # Phase 2 output (/speckit-tasks - NOT created by /speckit-plan)
```

Sem diretório `contracts/` — nenhum contrato de API novo ou alterado (mesma decisão das specs 021/045, features frontend-only sem mudança de interface externa).

### Source Code (repository root)

```text
frontend/
└── components/admin/CadastrarProfessoraForm.tsx   # MODIFICADO: 2 ocorrências de className="btn" (linhas ~380, ~409) → className="btn btn-ghost"
```

**Structure Decision**: Nenhum arquivo novo. A mudança inteira é uma troca de valor de atributo em duas linhas de um arquivo já existente.

## Post-Design Constitution Check

*Re-avaliação após Phase 1 (research.md, data-model.md, quickstart.md).*

Nenhuma mudança de avaliação — o design (Phase 1) confirma que a mudança é só a troca de classe em 2 pontos, sem nenhum novo elemento que acione algum dos 5 princípios.

**Resultado**: Sem violação bloqueante.

## Complexity Tracking

Não aplicável — nenhuma violação identificada em nenhum dos dois Constitution Checks.
