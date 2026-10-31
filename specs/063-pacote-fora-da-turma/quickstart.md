# Quickstart: validar Turmas de Pacote (spec 063)

Contratos em [contracts/](./contracts/); regras em [research.md](./research.md).

## Pré-requisitos

- **Backup** do banco local (`database/backup/backup-database.ps1`) e `database/25_turmas_de_pacote.sql` aplicado só no banco local.
- API local com o servidor de e-mail apontado para um endereço inexistente (nenhum e-mail pode sair) e token de Professor.
- `Turmas:IntervaloEncerramentoMinutos` pequeno (por exemplo 1) só para o teste da rotina.
- Dados sintéticos "TESTE-063", apagados ao final. Nunca acessar produção.

## Cenários (API)

### Turma e pacote

| # | Passo | Esperado |
|---|---|---|
| 1 | Criar turma `Pacote` sem datas | 400 |
| 2 | Criar duas turmas `Pacote` (Matemática Básica, Matemática Ensino Médio) com início amanhã e fim daqui a 3 meses | 201, `tipo = Pacote`, `fechada = false` |
| 3 | Cadastrar pacote da turma 1 e da turma 2 para o aluno A | 201 nos dois; A fica matriculado nas duas |
| 4 | Cadastrar pacote de aulas individuais para A | 201 |
| 5 | Cadastrar um segundo pacote da turma 1 para A | 409 |
| 6 | Cadastrar um segundo pacote de aulas individuais para A | 409 |
| 7 | Matricular diretamente o aluno B (sem pacote) na turma 1 | 409 |
| 8 | Pacote de turma com `turmaId` de turma de mensalidade | 409 |
| 9 | Trocar o `tipo` de uma turma, ou a turma de um pacote (`PUT`) | 409 |
| 10 | Mensalidade para A (que tem pacotes) | 409 (mensalidade é exclusiva) |
| 11 | Cadastrar pacote para a turma 1 depois que ela começou (início no passado) | 409 |
| 12 | Criar aula da turma 1 | participantes = alunos com pacote nela, como matriculados |

### Cobrança por grupo

| # | Passo | Esperado |
|---|---|---|
| 13 | Presença de A em aula da turma 1 | pacote da turma 1 −1; sem conta a receber |
| 14 | Presença de A em aula da turma 2 | pacote da turma 2 −1 (cada turma consome o seu) |
| 15 | Presença de A em aula individual | pacote de aulas individuais −1 |
| 16 | Desativar o pacote individual de A; presença em aula individual | conta a receber da aula individual; pacote da turma 1 intacto |
| 17 | A como "outro aluno" numa aula da turma 3 (sem pacote dela) | saldos intactos; conta a receber avulsa; A aparece em "Outros alunos" |
| 18 | Pacote de aulas individuais, A como "outro aluno" em aula de turma | saldo intacto; conta a receber avulsa |
| 19 | Falta de A | saldos intactos; sem conta |
| 20 | Aluno de mensalidade como "outro aluno" na turma de pacote | nenhuma cobrança |
| 21 | Aluno avulso como "outro aluno" na turma de pacote | conta a receber avulsa / valor da aula |
| 22 | Pacote da turma 1 esgotado, presença na aula da turma 1, sem avulsa | presença barrada com aviso |
| 23 | Pacote da turma 1 esgotado, presença de A em aula **individual** | cobra a aula individual; **não** barra, sem aviso de pacote |
| 24 | Aluno que saiu da turma 1 (desvinculado), participando de uma aula dela como "outro aluno", pacote ativo | consome o pacote da turma 1 |

### Saída, volta, fim da turma e aluno excluído

| # | Passo | Esperado |
|---|---|---|
| 25 | Desvincular A da turma 1 já iniciada | 204; pacote, saldo e pagamentos intactos |
| 26 | Matricular A de novo na turma 1 (pacote ativo, turma em andamento) | 200 (turma devolvida com A) |
| 27 | Matricular de novo alguém que saiu e **não** tem pacote ativo dela | 409 |
| 28 | Excluir o aluno A (lógico) e reativar, com a turma 1 em andamento | pacote, saldo e matrícula como antes; presença consome normalmente |
| 29 | Inativar a turma 2 (`DELETE`) | pacotes ativos dela ficam inativos; saldo, valor e total iguais; matrículas e aulas intactas |
| 30 | Reativar a turma 2 | turma ativa; pacotes continuam inativos |
| 31 | Reativar o pacote da turma 2 (turma em andamento) | 200; A rematriculado se necessário |
| 32 | Ajustar `data_fim` da turma 1 para ontem; aguardar a rotina | pacotes da turma 1 inativos, saldo igual (mesmo com o aluno inativo) |
| 33 | Reativar um pacote da turma 1 sem prorrogar a data de fim | 409 |
| 34 | Prorrogar `dataFim` da turma 1 para o futuro e reativar o pacote | 200 (os pacotes não voltaram sozinhos antes) |
| 35 | Turma `Pacote` migrada sem datas: rotina roda | nada é encerrado |
| 36 | `PUT` mudando a `dataInicio` já preenchida da turma 1; ajustar só a `dataFim` | 409 na `dataInicio`; 200 na `dataFim` |

### Outros

| # | Passo | Esperado |
|---|---|---|
| 37 | Criar aula sobreposta a outra da professora (com A como participante) | 409 (regra existente) |
| 38 | Adicionar e remover reposição de aluno de pacote e de mensalidade | continuam funcionando |
| 39 | Apagar os dados de teste e conferir os vínculos reais | totais iguais aos de antes |

## Migração (banco local, depois do backup)

1. Antes: guardar contagens de `vinculo_cobranca` (total, ativos, soma de `valor` e `saldo_aulas`), `alunos_turma`, `aula`, `aula_aluno` e `pagamento`.
2. Rodar o script.
3. Depois, conferir que **todas** as contagens acima são idênticas, e que só mudaram `turma.tipo` e `vinculo_cobranca.turma_id` dos casos convertidos.
4. Rodar as consultas de "casos para decidir" do final do script: (a) aluno com pacote ativo em várias turmas; (b) turmas mistas; (c) aluno com mais de um pacote ativo do mesmo grupo; (d) pacote ativo de aluno matriculado em turma inativa (não é convertido).
5. Conferir que pacote de aluno sem matrícula continua sem turma (pacote de aulas individuais).

## Frontend (manual, navegador)

- Turma nova: escolher Pacote mostra início e fim; Mensalidade não mostra. Tipo somente leitura na edição.
- Página da turma de pacote: sem botão de vincular aluno para quem não tem pacote dela, com o texto explicando a entrada pelo pacote.
- Ficha do aluno → Pacote: campo "Turma (opcional)" lista só turmas de pacote ativas e não fechadas; vazio = aulas individuais. A ficha mostra a turma de cada pacote.
- Página da aula: lista principal e seção **"Outros alunos"** (oculta quando vazia); presença e falta funcionam nas duas.
- Claro/escuro e 360px, sem rolagem horizontal.
