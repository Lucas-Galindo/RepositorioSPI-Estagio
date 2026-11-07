# Quickstart: validar a spec 076

## Pré-requisitos

API nova na 5099 (`dotnet run --artifacts-path <scratchpad>` se houver API do usuário travando as DLLs) e Next com `cmd /c "set NEXT_PUBLIC_API_URL=http://localhost:5099&& set NODE_OPTIONS=--max-old-space-size=1536&& npx next dev --webpack"`. **Se o usuário estiver com API/Next rodando (7002/3000), não encerrar nem buildar por cima**: usar `page.route` (7002→5099). Login só por `SPI_TESTE_SENHA`. Sem migração; dados de teste "TESTE-076" removidos no fim (contagens iguais; `refresh_token` cresce 1 por login).

## 1. Automatizado

`dotnet test tests/SPI.Application.Tests` (sem `--no-build`); `cd frontend`: `npx tsc --noEmit`, `npx eslint`, `npm run build` (se o ambiente do usuário estiver parado).

## 2. API real

| # | Chamada | Esperado |
|---|---|---|
| 1 | `GET /api/turmas` sem parâmetros, antes e depois | JSON idêntico |
| 2 | `?materiaId=1` | só a(s) turma(s) com Matemática (conferir contra a lista completa) |
| 3 | `?semMateria=true` | só as turmas sem matéria |
| 4 | `?materiaId=1&ativo=false` e com `nome=` | interseção |
| 5 | `?materiaId=999` | 200 e lista vazia |
| 6 | `?materiaId=1&semMateria=true` | 400 |
| 7 | com 2 turmas "TESTE-076" em matérias diferentes | cada `materiaId` devolve só a sua |

## 3. Chrome (login real, claro, escuro e 360 px, capturas)

| # | Passo | Esperado |
|---|---|---|
| 1 | Turmas sem filtro | seletor "Matéria — todas"; 4 turmas na ordem de antes |
| 2 | Matéria "Matemática" | só as turmas dela; contagem acompanha; requisição com `materiaId` |
| 3 | "Sem matéria" | só as sem matéria; requisição com `semMateria=true` |
| 4 | Matéria + status + busca | os três juntos |
| 5 | Matéria inativada (temporário) | continua no seletor e filtrável |
| 6 | Sem resultado | "Nenhuma turma encontrada" |
| 7 | "Limpar filtros" | volta a todos |
| 8 | Clique no card | abre a turma |
| 9 | 360 px | sem rolagem horizontal da página |
| 10 | `git status --porcelain -- frontend` | só `turmas/page.tsx` e `lib/api/turmas.ts`; nada novo |
