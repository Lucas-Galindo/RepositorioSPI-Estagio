# Quickstart: validar cobrança por aluno e reposição

Decisões em [research.md](./research.md); contratos em [contracts/](./contracts/).

## Pré-requisitos

- Backup do banco local (`database/backup/backup-database.ps1`) e migração `database/21_cobranca_por_aluno.sql` aplicada.
- API (`dotnet run` em `src/SPI.Api`) e frontend (`npm run dev` em `frontend/`) rodando; login de Professor.
- Duas turmas (A e B) e alunos de teste sintéticos (nenhum e-mail real).

## 1. Mensalidade cobre tudo (US1)

| # | Passo | Esperado |
|---|---|---|
| 1 | Ficha de um aluno sem vínculos → Adicionar vínculo → Mensalidade | O campo Turma não aparece; vínculo salvo e exibido como "Todas as aulas do aluno" |
| 2 | Tentar adicionar um Pacote ou uma Avulsa para o mesmo aluno | Recusado com mensagem de que a mensalidade já cobre todas as aulas |
| 3 | Registrar presença do aluno numa aula da turma dele e numa aula individual | Nenhuma conta a receber gerada para essas presenças |

## 2. Pacote do aluno (US2)

| # | Passo | Esperado |
|---|---|---|
| 1 | Aluno com Pacote de saldo 3 (sem turma) | Exibido como "Todas as aulas do aluno · Saldo: 3" |
| 2 | Presença numa aula da Turma A | Saldo 2, nenhuma conta a receber |
| 3 | Falta numa aula da Turma A | Saldo continua 2 |
| 4 | Presença numa aula individual | Saldo 1 |
| 5 | Tentar cadastrar um segundo Pacote | Recusado ("já tem um pacote ativo") |
| 6 | Com saldo 0 e uma Avulsa individual ativa, presença numa aula individual | Conta a receber pelo valor da Avulsa + aviso "pacote esgotado ... cobrado como avulsa" |
| 7 | Com saldo 0 e sem Avulsa no contexto, presença | Presença bloqueada com aviso de renovar o pacote (como hoje) |

## 3. Reposição (US3)

| # | Passo | Esperado |
|---|---|---|
| 1 | Abrir uma aula agendada da Turma B; adicionar um aluno matriculado só na Turma A | Ele aparece na lista com a marca "Reposição" |
| 2 | Remover esse aluno | Ele sai da lista |
| 3 | Adicioná-lo de novo, editar o horário da aula e salvar | Ele continua na aula |
| 4 | Registrar a sessão com ele presente | Cobrança segue o vínculo dele (ex.: consome o pacote dele) |
| 5 | Tentar remover um aluno matriculado na Turma B | Recusado |
| 6 | Tentar adicionar/remover participante numa aula realizada ou individual | Recusado / opção não aparece |

## 4. Dados existentes (US4)

| # | Passo | Esperado |
|---|---|---|
| 1 | Antes da migração, ter um Pacote ativo ligado a uma turma | Depois da migração, o mesmo pacote aparece sem turma, mesmo saldo/total |
| 2 | Aluno com Mensalidade e Pacote ativos (inserido antes da migração) | Ficha do aluno mostra aviso de conflito listando ambos; nada foi desativado |
| 3 | Desativar o excedente | Aviso some |
