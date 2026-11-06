# Implementation Plan: Turmas — filtro de status (ativas/inativas)

**Branch**: `073-turmas-filtro-status` | **Date**: 2026-11-05 | **Spec**: [spec.md](spec.md)

## Summary

A tela de Turmas ganha o seletor "Status — todos / Ativo / Inativo" e o botão "Limpar filtros", no padrão da lista de Alunos, usando o parâmetro `ativo` que a API já aceita. Só `turmas/page.tsx`; sem backend, banco, CSS novo ou arquivo novo.

## Technical Context

Next.js 16 / React 19 / TypeScript; `listarTurmas` já aceita `ativo`. Validação: `tsc`, `eslint`, build e Chrome com login real.

## Constitution Check

- **Princípio II**: o servidor filtra; o frontend só envia o parâmetro. ✔
- **Princípio V**: relatório em `docs/relatorios/`. ✔
- Sem opção configurável, segredo ou banco. ✔

## Project Structure

```text
specs/073-turmas-filtro-status/        spec.md, plan.md, research.md, quickstart.md, tasks.md
frontend/app/(app)/turmas/page.tsx     (seletor, estado, Limpar filtros)
docs/relatorios/<data>-turmas-filtro-status.md
```

## Design

Ver [research.md](research.md) R2: estado `status`, parâmetro `ativo`, `filter-select` e `filter-clear`.

## Complexity Tracking

Nada a justificar.
