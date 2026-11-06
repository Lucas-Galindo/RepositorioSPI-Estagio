# Quickstart: validar a spec 074

## Pré-requisitos

API local em `http://localhost:5099` e Next com `cmd /c "set NEXT_PUBLIC_API_URL=http://localhost:5099&& set NODE_OPTIONS=--max-old-space-size=1536&& npx next dev --webpack"` (conferir antes se há processos do usuário nas portas e não encerrar os que não subi); login só por `SPI_TESTE_SENHA`; `ignoreHTTPSErrors` se o front apontar para a 7002. Sem migração e sem dados novos (11 aulas no banco local).

## 1. Automatizado

`dotnet test tests/SPI.Application.Tests` (sem `--no-build`); `cd frontend`: `npx tsc --noEmit`, `npx eslint`, `npm run build`.

## 2. API real

| # | Chamada | Esperado |
|---|---|---|
| 1 | `GET /api/aulas` sem parâmetros, antes e depois | JSON idêntico (FR-004) |
| 2 | `?materiaId=<Matemática>` | só aulas dessa matéria; confere com a lista completa filtrada à mão |
| 3 | `?horaInicioDe=15:00` / `?horaInicioAte=10:00` / os dois | só aulas no intervalo (limites inclusos) |
| 4 | `?dataInicio=...&dataFim=...&materiaId=...&horaInicioDe=...&status=...` | interseção dos filtros |
| 5 | `?horaInicioDe=18:00&horaInicioAte=09:00` | lista vazia, 200 |
| 6 | `?horaInicioDe=abc` | 400 |

## 3. Chrome (login real, claro, escuro e 360 px, capturas)

| # | Passo | Esperado |
|---|---|---|
| 1 | Aulas sem filtro | 11 aulas; campos vazios; sem "Limpar filtros" |
| 2 | Matéria | só dela; contagem acompanha |
| 3 | Data De/Até pelo calendário do sistema | só aulas no intervalo |
| 4 | Horário De/Até | só aulas com início no intervalo (limites inclusos) |
| 5 | Status e Turma combinados | interseção |
| 6 | Sem resultado e "De" > "Até" | "Nenhuma aula encontrada" |
| 7 | Limpar filtros | tudo vazio; 11 aulas |
| 8 | Clique na linha | abre a aula |
| 9 | 360 px | filtros quebram em linhas, sem rolagem horizontal da página |
| 10 | `git status --porcelain -- frontend` | só `aulas/page.tsx` e `lib/api/aulas.ts`, nada novo |
