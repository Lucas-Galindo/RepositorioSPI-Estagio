# Quickstart: validar a spec 078

## Pré-requisitos

- Migração 28 aplicada **só no banco local**, com backup antes (`database/backup`). Produção em standby.
- API nova na 5099 (`dotnet run --artifacts-path <scratchpad>` se houver API do usuário travando as DLLs) e Next com `cmd /c "set NEXT_PUBLIC_API_URL=http://localhost:5099&& set NODE_OPTIONS=--max-old-space-size=1536&& npx next dev --webpack"`. **Se o usuário estiver com API/Next rodando (7002/3000), não encerrar nem buildar por cima.**
- Login só por `SPI_TESTE_SENHA` em processo. Dados "TESTE-078" (turma de pacote, 3–4 alunos, pacotes, aulas e presenças) criados pela API e **removidos no fim**; contagens de tabelas iguais às do início (exceto `refresh_token`); `ra_controle` restaurado se criar aluno.

## 1. Automatizado

`dotnet test tests/SPI.Application.Tests` (sem `--no-build`); `cd frontend`: `npx tsc --noEmit`, `npx eslint`, `npm run build` (se o ambiente do usuário estiver parado).

## 2. API real

Cenário base: turma de pacote em andamento (início ontem, fim daqui a 30 dias), aluno com pacote de **R$ 100 / 10 aulas** e **7 presenças** registradas.

| # | Chamada | Esperado |
|---|---|---|
| 1 | prévia, `valorPago = 30` | 200: consumidas 7, por aula 10,00, devido 70,00, diferença 40,00, `GerarConta`, `precisaVencimento = true`; nada gravado |
| 2 | prévia, `valorPago = 100` | devido 70,00, `ExcedenteNaoDevolvido`, excedente 30,00 |
| 3 | prévia, `valorPago = 70` | `Fechado` |
| 4 | pacote R$ 100 / 3 aulas, 2 presenças | devido 66,67 |
| 5 | pacote com 0 presenças / com tudo consumido | devido 0,00 / devido = valor do pacote |
| 6 | gravar com pago 30 e vencimento de hoje em diante | 200; aluno fora da turma, pacote inativo com `saidaRegistradaEm`, **1** conta `Pendente` de 40,00 na categoria "Acerto de pacote", vencimento certo |
| 7 | gravar o mesmo de novo | 409; **nenhuma** conta a mais |
| 8 | gravar com pago 100 (outro aluno) | 200; saída feita; **nenhuma** conta; nenhum lançamento negativo |
| 9 | gravar com pago 30 sem vencimento / com vencimento de ontem | 409; nada muda |
| 10 | gravar depois de registrar mais uma presença (consumo mudou) | 409 "confira o novo resumo"; nada muda |
| 11 | `valorPago = -1` / `10.005` | 400 |
| 12 | turma inexistente / aluno não matriculado / turma de mensalidade / turma já encerrada / pacote inativo | 404 / 409 |
| 13 | reativar o pacote encerrado pela saída; matricular o aluno de novo | 409 nos dois |
| 14 | `DELETE` desvincular em outro aluno | igual ao de antes (pacote, saldo e pagamentos intactos; pode voltar) |
| 15 | `GET /api/turmas/{id}` | `podeRegistrarSaida = true` só para quem tem pacote ativo na turma em andamento |
| 16 | conta do acerto em Contas a Receber; marcar como paga | comporta-se como as demais (relatórios) |
| 18 | prévia (sai de 2 aulas), criar mais uma aula futura da turma e gravar com os ecos antigos | 409 "mudou"; nada muda; o novo resumo mostra 3 aulas |
| 17 | aluno em 2 aulas agendadas futuras, 1 aula já iniciada ainda agendada, 1 realizada e 1 reposição da turma; prévia e gravação | prévia: `aulasAgendadasQueSairao = 2`; gravação: sai **só** das 2 futuras (conferir em `aula_aluno`); a iniciada, a realizada (presença intacta) e a reposição ficam |

## 3. Chrome (login real, claro e escuro, capturas)

| # | Passo | Esperado |
|---|---|---|
| 1 | Turma de pacote em andamento: lista de alunos | "Registrar saída" aparece ao lado de "Remover" **só** nos alunos elegíveis; na turma de mensalidade e na turma encerrada, não aparece |
| 2 | Abrir "Registrar saída", informar o pago | resumo com consumidas, valor por aula, devido, pago, diferença e a consequência; vencimento só quando gera conta |
| 3 | Mudar o pago rápido várias vezes | o resumo mostrado é sempre o do último valor (sem resposta atrasada) |
| 4 | Fechar sem confirmar | nada muda |
| 5 | Confirmar (pago < devido) | toast; aluno some da lista; conta Pendente em Contas a Receber |
| 6 | Confirmar (pago ≥ devido) | toast; nenhuma conta nova |
| 7 | Ficha do aluno | pacote inativo com "Saída registrada em …" e **sem** botão "Reativar" |
| 8 | "Remover" (desvincular) de outro aluno | igual ao de hoje |
| 9 | Consumo mudou com o modal aberto | mensagem do servidor e resumo recarregado |
| 10 | `git status --porcelain -- database` | só a migração 28 |
| 11 | Resumo com aulas futuras | mostra "sai de N aulas agendadas que ainda não começaram"; depois da saída o aluno não consta nelas (detalhes da aula) |
