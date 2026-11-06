# Implementation Plan: Alunos — coluna "Tipo" de cobrança na tabela

**Branch**: `072-alunos-coluna-tipo` | **Date**: 2026-11-05 | **Spec**: [spec.md](spec.md)

## Summary

A lista de Alunos ganha a coluna **"Tipo"** depois de "Cobrança". O `SeloCobranca` passa a ter os modos `"tipo"` (pílula, "+N" e dica) e `"detalhe"` (valor, turma e aviso de pacote esgotado), substituindo o modo `"lista"`. Só frontend, dois arquivos existentes, sem arquivo novo, backend, banco ou dependência.

## Technical Context

- **Frontend**: Next.js 16 / React 19 / TypeScript; `components/alunos/SeloCobranca.tsx` e `app/(app)/alunos/page.tsx`.
- **Dados**: o resumo `cobranca` de cada aluno já vem de `GET /api/alunos?incluirCobranca=true` (spec 068); nenhuma mudança na API.
- **Validação**: `tsc`, `eslint`, build; Chrome com login real e dados simulados por interceptação da resposta.

## Constitution Check

- **Princípio II**: o frontend só exibe o resumo calculado pelo servidor. ✔
- **Princípio V**: relatório em `docs/relatorios/`; nota na spec 068 (que criou o selo da lista) apontando para esta spec. ✔
- Sem opção configurável, segredo ou banco. ✔

Sem violações.

## Project Structure

```text
specs/072-alunos-coluna-tipo/            spec.md, plan.md, research.md, quickstart.md, tasks.md
frontend/components/alunos/SeloCobranca.tsx   (modo "lista" vira "tipo" + "detalhe"; "grupo" intacto)
frontend/app/(app)/alunos/page.tsx            (coluna "Tipo")
specs/068-*/spec.md                           (nota)
docs/relatorios/<data>-alunos-coluna-tipo.md
```

## Design

Ver [research.md](research.md) (R1 a R3): dois modos no mesmo componente, a tabela com a coluna nova, "—" quando não há selo.

## Complexity Tracking

Nada a justificar.
