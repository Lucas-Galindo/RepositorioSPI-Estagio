# Implementation Plan: Aulas — filtros de data, horário, matéria e status

**Branch**: `074-aulas-filtros` | **Date**: 2026-11-05 | **Spec**: [spec.md](spec.md)

## Summary

A tela de Aulas ganha os filtros Data De/Até (calendário do sistema), Horário De/Até, Matéria e Limpar filtros (Status e Turma continuam). O servidor passa a filtrar por matéria e por horário de início (parâmetros opcionais novos em `GET /api/aulas`). Sem migração, sem dependência.

## Technical Context

- **Backend**: .NET 10; `AulasController`, `AulaService`/`IAulaService`, `IAulaRepository`/`AulaRepository`; testes xUnit (`dotnet test` **sem** `--no-build`).
- **Frontend**: Next.js 16 / React 19; `aulas/page.tsx`, `lib/api/aulas.ts`; `DateInput`, `TimeInput`, `listarMaterias`.
- **Validação**: API local + Chrome com login real; comparação antes/depois sem filtro.

## Constitution Check

- **Princípio II**: filtros no servidor; frontend só envia e exibe. ✔
- **Princípio V**: relatório em `docs/relatorios/`; contrato em `contracts/aulas.md`. ✔
- Sem banco, segredo ou opção configurável. ✔

## Project Structure

```text
specs/074-aulas-filtros/   spec.md, plan.md, research.md, contracts/aulas.md, quickstart.md, tasks.md
src/SPI.Domain/Repositories/IAulaRepository.cs         (+3 parâmetros opcionais)
src/SPI.Infrastructure/Repositories/AulaRepository.cs  (Where por matéria e horário)
src/SPI.Application/Aulas/Services/IAulaService.cs, AulaService.cs
src/SPI.Api/Controllers/AulasController.cs             (3 [FromQuery])
tests/SPI.Application.Tests/Aulas/ + 5 fakes de IAulaRepository
frontend/app/(app)/aulas/page.tsx, frontend/lib/api/aulas.ts
```

## Design

Ver [research.md](research.md) R1–R5 e [contracts/aulas.md](contracts/aulas.md).

## Complexity Tracking

Nada a justificar.
