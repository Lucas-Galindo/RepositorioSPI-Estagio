# Implementation Plan: Turmas — filtro de matéria

**Branch**: `076-turmas-filtro-materia` | **Date**: 2026-11-05 | **Spec**: [spec.md](spec.md)

## Summary

A tela de Turmas ganha o seletor de matéria ("todas", "Sem matéria" e as matérias), combinando com status e nome. O servidor filtra (`materiaId` e `semMateria` opcionais em `GET /api/turmas`). Sem migração, sem dependência.

## Technical Context

Backend .NET 10 (`TurmasController`, `TurmaService`, `ITurmaRepository`/`TurmaRepository`); frontend Next.js 16 (`turmas/page.tsx`, `lib/api/turmas.ts`, `listarMaterias`); testes xUnit (`dotnet test` sem `--no-build`; `--artifacts-path` se a API do usuário travar as DLLs).

## Constitution Check

- **Princípio II**: o servidor filtra; o frontend só envia e exibe. ✔
- **Princípio V**: contrato em `contracts/turmas.md`, relatório e nota na spec 073/075. ✔
- Sem banco, segredo ou opção configurável. ✔

## Project Structure

```text
specs/076-turmas-filtro-materia/   spec.md, plan.md, research.md, contracts/turmas.md, quickstart.md, tasks.md
src/SPI.Domain/Repositories/ITurmaRepository.cs, src/SPI.Infrastructure/Repositories/TurmaRepository.cs
src/SPI.Application/Turmas/Services/ITurmaService.cs, TurmaService.cs
src/SPI.Api/Controllers/TurmasController.cs
tests/SPI.Application.Tests/Turmas/TurmaServiceListarFiltrosTests.cs + 5 fakes de ITurmaRepository
frontend/app/(app)/turmas/page.tsx, frontend/lib/api/turmas.ts
```

## Design

Ver [research.md](research.md) R1–R5 e [contracts/turmas.md](contracts/turmas.md).

## Complexity Tracking

Nada a justificar.
