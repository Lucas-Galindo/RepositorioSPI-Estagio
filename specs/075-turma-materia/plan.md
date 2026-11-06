# Implementation Plan: Turma ↔ Matéria

**Branch**: `075-turma-materia` | **Date**: 2026-11-05 | **Spec**: [spec.md](spec.md)

## Summary

A turma passa a ter uma matéria opcional: coluna `turma.materia_id` (migração `27_turma_materia.sql` preenchendo as turmas existentes a partir das aulas quando há uma só matéria), campo na API e no formulário de turma, nome da matéria exibido, e pré-seleção (trocável) da matéria na nova aula. A regra de aulas não muda.

## Technical Context

- **Banco**: MySQL, script numerado em `database/` aplicado via `mysql` CLI **só no banco local**, com backup antes (`database/backup`); credencial de User Secrets (nunca impressa).
- **Backend**: .NET 10 (`Turma`, `TurmaConfiguration`, `TurmaService`, `TurmaRepository`, DTOs); testes xUnit.
- **Frontend**: Next.js 16 / React 19 (`TurmaForm`, `turmas/page`, `turmas/[id]/page`, `AulaForm`, `lib/api/turmas.ts`).
- **Validação**: contagens do banco antes/depois, API real, Chrome com login real.

## Constitution Check

- **Princípio II**: validação da matéria no servidor; o frontend só exibe e pré-seleciona (conveniência, sem regra). ✔
- **Princípio V**: contrato em `contracts/turmas.md`, `data-model.md`, relatório e nota na spec 063 (que definiu o tipo da turma). ✔
- Migração segura (só `turma`, sem `DELETE`), backup antes, produção em standby. ✔

## Project Structure

```text
specs/075-turma-materia/   spec.md, plan.md, research.md, data-model.md, contracts/turmas.md, quickstart.md, tasks.md, analysis.md
database/27_turma_materia.sql
src/SPI.Domain/Entities/Turma.cs
src/SPI.Infrastructure/Persistence/Configurations/TurmaConfiguration.cs, Repositories/TurmaRepository.cs
src/SPI.Application/Turmas/ (Dtos, Services/TurmaService.cs)
src/SPI.Api/Controllers/TurmasController.cs   (só comentários, se necessário)
tests/SPI.Application.Tests/Turmas/TurmaServiceMateriaTests.cs (+ construtores nos testes de turma)
frontend/lib/api/turmas.ts, components/turmas/TurmaForm.tsx, app/(app)/turmas/page.tsx, app/(app)/turmas/[id]/page.tsx, components/aulas/AulaForm.tsx
```

## Design

Ver [research.md](research.md) R1–R6, [data-model.md](data-model.md) e [contracts/turmas.md](contracts/turmas.md).

## Complexity Tracking

Nada a justificar.
