# Quickstart: validar registro de presença na criação da aula

Guia de validação ponta a ponta. Decisões técnicas em [research.md](./research.md). Contrato dos endpoints em [contracts/aulas-api.md](./contracts/aulas-api.md).

## Pré-requisitos

- Backend (`dotnet run` em `src/SPI.Api/`) e frontend (`npm run dev` em `frontend/`) rodando, login como Professor feito.
- Uma turma com pelo menos 2 alunos ativos, um deles (opcional) com vínculo de cobrança Pacote de saldo 0 (para o cenário de bloqueio).

## 1. Criar aula retroativa de turma com presença (US1, FR-001 a FR-004, SC-001/SC-002)

| # | Passo | Esperado |
|---|---|---|
| 1 | Abrir Aulas → Nova aula, selecionar Matéria e Turma | Formulário normal, sem lista de presença ainda (data não preenchida) |
| 2 | Preencher a Data de Início com **ontem** | A lista de alunos da turma aparece, cada um com checkbox marcado por padrão |
| 3 | Desmarcar o checkbox de 1 aluno, preencher horário, salvar | Aula criada com sucesso; ao abrir o detalhe, Status = "Realizada", o aluno desmarcado aparece como "Faltou", os demais como "Presente" |
| 4 | Conferir Financeiro → Contas a Receber (ou o saldo do vínculo Pacote do aluno presente) | Mesmo efeito que "Registrar Sessão" já produz hoje para o mesmo cenário de presença (cobrança gerada para Avulsa/sem vínculo, saldo decrementado para Pacote) |

## 2. Aluno com pacote esgotado numa aula retroativa (Edge Case, FR-006)

| # | Passo | Esperado |
|---|---|---|
| 1 | Repetir o passo 1 do cenário acima com um aluno cujo vínculo Pacote tem saldo 0, marcando-o como presente | Aula criada com sucesso; o aviso de "pacote esgotado" aparece na tela após salvar, igual ao que "Registrar Sessão" já mostra hoje |
| 2 | Abrir o detalhe da aula | O aluno aparece como não-presente com o motivo "Pacote esgotado"; os demais alunos continuam "Realizada" normalmente |

## 3. Aula futura continua sem lista de presença (US1, FR-002, SC-003 — não-regressão)

| # | Passo | Esperado |
|---|---|---|
| 1 | Nova aula, preencher Data de Início com uma data futura | Nenhuma lista de presença aparece — formulário idêntico ao comportamento atual |
| 2 | Salvar | Aula criada com Status "Agendada", como hoje |

## 4. Trocar a data de futura para hoje/passada (e vice-versa) antes de salvar (US1, Acceptance Scenario 4)

| # | Passo | Esperado |
|---|---|---|
| 1 | Nova aula de turma, preencher Data de Início futura, depois trocar para hoje | A lista de presença aparece dinamicamente, sem precisar recarregar a tela |
| 2 | Trocar a data de volta para uma data futura | A lista de presença desaparece novamente |

## 5. Aula individual retroativa (US1, Acceptance Scenario 5)

| # | Passo | Esperado |
|---|---|---|
| 1 | Nova aula sem Turma (atendimento individual), selecionar um Aluno, Data de Início = hoje | A lista de presença mostra só esse aluno, com checkbox |
| 2 | Salvar | Aula criada "Realizada" com a presença desse aluno, mesmo efeito financeiro de "Registrar Sessão" |

## 6. Editar a Descrição de uma aula já Realizada (US2, FR-007/FR-008, SC-004)

| # | Passo | Esperado |
|---|---|---|
| 1 | Abrir uma aula com Status "Realizada" (de qualquer cenário acima, ou uma pré-existente), ir em Editar | Formulário de edição abre |
| 2 | Alterar só o campo Descrição, salvar | Alteração salva com sucesso |
| 3 | Tentar, numa nova tentativa, alterar Data/Hora/Turma/Matéria/Aluno dessa mesma aula Realizada e salvar | Sistema rejeita a alteração (mensagem de erro, aula não realizada não pode ser alterada exceto a descrição) — mesmo bloqueio de hoje para esses campos |

## 7. Não-regressão: "Registrar Sessão" continua funcionando para aulas agendadas no futuro que já chegaram

| # | Passo | Esperado |
|---|---|---|
| 1 | Pegar uma aula já existente, criada como "Agendada" (antes desta feature, ou criada com data futura e cuja data já passou naturalmente) | A tela de detalhe continua oferecendo "Registrar como realizada" com a lista de presença, exatamente como hoje |
| 2 | Registrar a sessão normalmente | Mesmo comportamento de sempre — nenhuma regressão |
