# Quickstart: validar o esgotamento de Pacote

Guia de validação ponta a ponta. Contratos em [contracts/](./contracts/); modelo em [data-model.md](./data-model.md).

## Pré-requisitos

- Script `database/16_aula_aluno_motivo_nao_registro.sql` aplicado no MySQL local.
- Backend rodando contra o MySQL local; login como Professor.
- Alunos de teste com Vínculos de Cobrança `Pacote` ativos e saldos **5, 2, 1 e 0** (criados pela tela do aluno, specs/037), de preferência um deles como "Atendimento individual" e outro numa turma.
- Uma aula `Agendada` com dois alunos: um de saldo 0 e outro de saldo positivo.

## 1. Painel na Home (US1, FR-001 a FR-004, SC-001, SC-005)

Abrir a Home (`/dashboard`).

| Esperado |
|---|
| Aparecem só os vínculos com saldo 2, 1 e 0 (o de saldo 5 **não** aparece) |
| Cada linha mostra nome do aluno, contexto (turma ou "Atendimento individual") e saldo |
| O de saldo 0 aparece primeiro, com o estilo "Esgotado/Bloqueado" distinto de "Atenção" |
| Com nenhum vínculo em atenção/esgotado, o painel mostra a mensagem de que ninguém precisa de atenção |

## 2. Bloqueio ao registrar a sessão (US2, FR-005 a FR-007, SC-002, SC-003)

Na aula com dois alunos (um esgotado, um com saldo), marcar **ambos presentes** e registrar a sessão.

| Esperado |
|---|
| A aula fica `Realizada` (não há erro) |
| O aluno com saldo positivo: presente, frequência +1, saldo −1 |
| O aluno esgotado: **sem** presença/frequência/débito; um aviso "Pacote esgotado ... renove" aparece na tela e o selo "Pacote esgotado" fica ao lado dele na lista de presenças |
| Reabrir a aula depois: o selo "Pacote esgotado" continua lá (motivo persistido — `aula_aluno.motivo_nao_registro = 'PacoteEsgotado'`) |
| Repetir com o aluno esgotado marcado **ausente**: aceito normalmente, sem aviso e **sem** selo (`motivo_nao_registro` fica `NULL` — falta comum) |

## 3. Renovação libera o aluno (US3, FR-008, SC-004)

Editar o vínculo do aluno esgotado (tela do aluno, specs/037) definindo saldo 10. Numa nova aula, marcar esse aluno presente.

| Esperado |
|---|
| Presença aceita, saldo 10 → 9, **sem** aviso |
| O aluno sai do painel da Home |

## 4. Não-regressão (FR-009, FR-010)

- Aluno com saldo positivo continua consumindo exatamente 1 por presença (specs/038 intacto).
- Aluno com saldo `null`, `Avulsa` ou `Mensalidade` nunca é bloqueado nem listado.
- Nenhuma conta a receber é gerada ao esgotar; nenhum aviso é enviado ao aluno.

## 5. Suíte automatizada

```powershell
dotnet test "tests/SPI.Application.Tests"
```

Esperado: todos os testes passando (incluindo os novos de bloqueio e de painel).
