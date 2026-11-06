# Implementation Plan: Home — "Recebidos no período" e cards que acompanham os filtros

**Branch**: `071-home-recebidos-periodo` | **Date**: 2026-11-05 | **Spec**: [spec.md](spec.md)

## Summary

Os cards "Alunos atendidos" e "Recebido este mês" da Home passam a respeitar também o filtro de aluno, o card de recebidos vira **"Recebidos no período"** e os textos falam de "período". Quando só o "até" é informado, o período começa no 1º dia do mês do "até". Backend pequeno (um parâmetro opcional, um método estático de período, repasse do `alunoId`) e troca de textos na Home. Sem banco, sem campo novo na API, sem dependência.

## Technical Context

- **Backend**: .NET 10, `SPI.Application` (`DashboardService`), `SPI.Domain` (`IRelatorioRepository`), `SPI.Infrastructure` (`RelatorioRepository`), `SPI.Api` (`DashboardController`, só comentários XML).
- **Frontend**: Next.js 16 / React 19, só `app/(app)/dashboard/page.tsx` (textos).
- **Testes**: xUnit em `tests/SPI.Application.Tests` (`dotnet test` **sem** `--no-build`: o `dotnet build SPI.slnx` não compila os testes).
- **Validação**: API local (5099) + Chrome com login real; sem migração, sem dados novos (as contas e aulas do banco local bastam).

## Constitution Check

- **Princípio II (regra no servidor)**: filtro de aluno e período calculados no servidor; o frontend só envia parâmetros e exibe. ✔
- **Princípio V (documentação)**: nota na spec 058 e atualização dos comentários do controller; relatório em `docs/relatorios/`. ✔
- Sem opção configurável nova, sem segredo, sem banco. ✔

Sem violações.

## Project Structure

```text
specs/071-home-recebidos-periodo/   spec.md, plan.md, research.md, contracts/dashboard.md, quickstart.md, tasks.md
src/SPI.Domain/Repositories/IRelatorioRepository.cs          (+ int? alunoId em ContarAlunosAtendidosNoPeriodoAsync)
src/SPI.Infrastructure/Repositories/RelatorioRepository.cs   (filtro por aluno na contagem)
src/SPI.Application/Dashboard/Services/DashboardService.cs   (ResolverPeriodo; alunoId nos dois cálculos)
src/SPI.Api/Controllers/DashboardController.cs               (comentários XML)
tests/SPI.Application.Tests/Dashboard/                       (testes novos) + 3 fakes de Relatorios*Tests
frontend/app/(app)/dashboard/page.tsx                        (textos dos 2 cards)
```

## Design

1. `DashboardService.ResolverPeriodo(DateOnly? inicio, DateOnly? fim, DateOnly hoje)` estático e puro (R4).
2. `ObterAsync` usa `ResolverPeriodo`, passa `alunoId` para `ContarAlunosAtendidosNoPeriodoAsync` e para `ObterValorFaturadoNoPeriodoAsync` (R2, R3).
3. Textos da Home (R6); a Home já reenvia a consulta quando o aluno muda.

## Complexity Tracking

Nada a justificar.
