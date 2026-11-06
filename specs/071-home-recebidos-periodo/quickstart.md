# Quickstart: validar a spec 071

Contrato em [contracts/dashboard.md](contracts/dashboard.md); decisões em [research.md](research.md).

## Pré-requisitos

- API local em `http://localhost:5099` (SMTP inválido) e Next em desenvolvimento com `NEXT_PUBLIC_API_URL=http://localhost:5099` definida **dentro do comando** (`cmd /c "set ...&& npx next dev --webpack"`); login de teste só por `SPI_TESTE_SENHA`.
- Sem migração e sem dados novos. Memória baixa: `NODE_OPTIONS=--max-old-space-size=1536` e saída dos roteiros longos em arquivo.

## 1. Automatizado

`dotnet test tests/SPI.Application.Tests` (sem `--no-build`): testes novos de `ResolverPeriodo` e do repasse do `alunoId`; `cd frontend`: `npx tsc --noEmit`, `npx eslint`.

## 2. API real (banco local)

| # | Chamada | Esperado |
|---|---|---|
| 1 | `GET /api/dashboard` (sem parâmetros) antes e depois da mudança | mesmos `alunosAtendidosNoPeriodo` e `valorFaturadoNoPeriodo` (mês atual) |
| 2 | `?periodoInicio=2026-01-01&periodoFim=2026-12-31` sem aluno | mesmos valores de antes (ex.: 1 aluno, R$ 870,00) |
| 3 | o mesmo período com `alunoId` de um aluno com pagamentos | `valorFaturadoNoPeriodo` = soma dos pagamentos pagos dele no período (conferir em `/api/pagamentos`); `alunosAtendidosNoPeriodo` 0 ou 1 |
| 4 | `alunoId` de um aluno sem nada no período | 0 e 0,00 |
| 5 | só `periodoFim=2026-09-15` | período considerado 01/09 a 15/09 (aulas agendadas e demais números coerentes) |
| 6 | só `periodoInicio=2026-09-10` | fim = 09/10 (um mês depois menos um dia; sem mudança) |

## 3. Chrome (login real, claro e escuro, capturas)

| # | Passo | Esperado |
|---|---|---|
| 1 | Home sem filtro | card "Recebidos no período"; legendas citam período; valores iguais aos de antes |
| 2 | Período de 2026 inteiro | valores acompanham; textos continuam falando de período |
| 3 | Escolher um aluno | os dois cards mudam para o aluno; legenda "do aluno selecionado" |
| 4 | Voltar a "Todos os alunos" | valores voltam ao total |
| 5 | Só o "até" preenchido | cards e demais números consideram o mês do "até" |
| 6 | Dica (ícone de informação) | texto fala do período |
| 7 | `git status --porcelain -- frontend` | só `dashboard/page.tsx` modificado, nenhum arquivo novo |
