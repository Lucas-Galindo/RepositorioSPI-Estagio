# Implementation Plan: Recorrência na criação de aulas

**Branch**: `077-recorrencia-aulas` | **Date**: 2026-11-07 | **Spec**: [spec.md](spec.md)

## Summary

Opção "Repetir" no cadastro de nova aula (tela e pop-up da Agenda): a professora define data inicial, dias da semana com horários próprios e o término (nº de aulas ou data final), vê a prévia das datas (com conflitos) e cria tudo de uma vez, de forma atômica e com bloqueio total em caso de conflito. O servidor gera as datas (função pura), valida (hoje em diante, máx. 60, conflitos) e grava; sem migração.

## Technical Context

- **Backend**: .NET 10; `AulasController`, `AulaService`/`IAulaService`, novo `GeradorSerieAulas` (puro), DTOs e validador novos; testes xUnit (`dotnet test` sem `--no-build`; `--artifacts-path` se a API do usuário travar as DLLs).
- **Frontend**: Next.js 16 / React 19; `AulaForm.tsx`, novo `RecorrenciaAulas.tsx`, `lib/api/aulas.ts`, `DateInput`/`TimeInput`.
- **Validação**: API real + Chrome com login real; dados de teste removidos depois.

## Constitution Check

- **Princípio II**: datas, limite, passado e conflitos calculados e validados no servidor; o frontend só envia e exibe. ✔
- **Princípio V**: contrato em `contracts/serie-aulas.md`, relatório e nota na spec 064 (regras de aula) e no card. ✔
- Sem banco, segredo ou opção configurável. ✔

## Project Structure

```text
specs/077-recorrencia-aulas/   spec.md, plan.md, research.md, data-model.md, contracts/serie-aulas.md, quickstart.md, tasks.md, analysis.md
src/SPI.Application/Aulas/Dtos/ (RecorrenciaAulaRequest, HorarioSerieRequest, PreviaSerieResponse, SerieCriadaResponse)
src/SPI.Application/Aulas/Services/ (GeradorSerieAulas.cs, IAulaService.cs, AulaService.cs)
src/SPI.Application/Aulas/Validators/RecorrenciaAulaRequestValidator.cs
src/SPI.Api/Controllers/AulasController.cs        (2 endpoints novos)
tests/SPI.Application.Tests/Aulas/ (GeradorSerieAulasTests, AulaServiceSerieTests, RecorrenciaAulaRequestValidatorTests)
frontend/lib/api/aulas.ts, components/aulas/RecorrenciaAulas.tsx (novo), components/aulas/AulaForm.tsx
```

## Design

Ver [research.md](research.md) R1–R6 e [contracts/serie-aulas.md](contracts/serie-aulas.md).

## Complexity Tracking

Um componente novo no frontend e um serviço puro novo no backend; nada a justificar além disso.
