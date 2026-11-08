# Quickstart: validar a spec 077

## Pré-requisitos

API nova na 5099 (`dotnet run --artifacts-path <scratchpad>` se houver API do usuário travando as DLLs) e Next com `cmd /c "set NEXT_PUBLIC_API_URL=http://localhost:5099&& set NODE_OPTIONS=--max-old-space-size=1536&& npx next dev --webpack"`; **se o usuário estiver com API/Next rodando (7002/3000), não encerrar nem buildar por cima**. Login só por `SPI_TESTE_SENHA`. Sem migração. Dados de teste "TESTE-077" (turma, aluno, aulas) removidos no fim; contagens iguais (exceto `refresh_token`) e `ra_controle` restaurado se criar aluno.

## 1. Automatizado

`dotnet test tests/SPI.Application.Tests` (sem `--no-build`); `cd frontend`: `npx tsc --noEmit`, `npx eslint`, `npm run build` (se o ambiente do usuário estiver parado).

## 2. API real

| # | Chamada | Esperado |
|---|---|---|
| 1 | `serie/previa` terça 14:00 + quinta 10:00, 10 aulas, começando numa segunda futura | 10 datas na ordem, primeira numa terça, `podeCriar = true` |
| 2 | o mesmo por `dataFinal` | todas as ocorrências até a data (inclusive) |
| 3 | `POST serie` | 201, 10 aulas "Agendadas", cada uma com os alunos ativos da turma |
| 4 | `POST serie` de novo (mesmas datas) | 409 listando as datas; nenhuma aula criada |
| 5 | série com 61 aulas / com `dataFinal` muito distante | 400 / 409 citando o limite de 60 |
| 6 | `dataInicial` ontem | 409 |
| 7 | horários sobrepostos no mesmo dia; quantidade e data final juntas | 400 |
| 8 | matéria/turma inexistentes | 404 |
| 9 | uma data em conflito no meio da série | 409; contagem de aulas igual à de antes |
| 10 | `POST /api/aulas` e `PUT` de uma aula | iguais aos de antes |

## 3. Chrome (login real, claro, escuro e 360 px, capturas)

| # | Passo | Esperado |
|---|---|---|
| 1 | Nova aula: marcar "Repetir" | aparecem data inicial, dias/horários, término e "Ver prévia" |
| 2 | Adicionar/remover dia; término por quantidade e por data final | funciona |
| 3 | Ver prévia | lista de datas com dia e horário e total |
| 4 | Série com conflito | datas em conflito marcadas; não deixa agendar; se enviar, mostra as datas |
| 5 | Agendar série sem conflito | toast "N aulas agendadas"; as N aulas em Aulas |
| 6 | Pop-up da Agenda | mesma opção; fecha e o mês mostra as aulas |
| 7 | Editar uma aula existente | sem a opção "Repetir" |
| 8 | Aula única sem repetir | continua igual |
| 9 | Largura estreita (o shell do app tem largura mínima de desktop; o layout para celular está fora de escopo no projeto) | o bloco "Repetir" não estoura a largura do contêiner: linhas quebram e a data não é espremida |
| 10 | `git status --porcelain -- database` | nenhuma migração |
