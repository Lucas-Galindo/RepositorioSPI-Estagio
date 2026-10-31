# Data Model: Turmas de Pacote (spec 063)

Migração: `database/25_turmas_de_pacote.sql` (pré-requisito: 01 a 23; a 24 é da spec 062 e não é pré-requisito).

## `turma` (alterada)

| Coluna | Tipo | Regra |
|---|---|---|
| `tipo` | VARCHAR(20) NOT NULL DEFAULT 'Mensalidade' | `CHECK (tipo IN ('Mensalidade','Pacote'))` |
| `data_inicio` | DATE NULL | só para `Pacote` |
| `data_fim` | DATE NULL | só para `Pacote`; vazia nas turmas migradas até a professora preencher |

`CHECK chk_turma_datas_so_pacote`: `tipo = 'Pacote' OR (data_inicio IS NULL AND data_fim IS NULL)`.
`CHECK chk_turma_periodo`: `data_inicio IS NULL OR data_fim IS NULL OR data_fim >= data_inicio`.

Entidade: `Turma.Tipo` (`TipoTurma`), `Turma.DataInicio`, `Turma.DataFim` (`DateOnly?`). "Obrigatórias em turma nova de pacote" é regra do validador, não do banco (a turma migrada pode ficar sem data).

### Estados (calculados, não gravados)

| Estado | Definição (hoje = data do servidor) |
|---|---|
| Iniciada | `data_inicio` não nula e `< hoje` |
| Em andamento | `ativo` e (`data_fim` nula ou `>= hoje`) |
| Encerrada | `ativo = 0` ou `data_fim < hoje` |

## `vinculo_cobranca` (constraint trocada, dados intactos)

| Antes (spec 056) | Depois |
|---|---|
| `chk_vinculocobranca_turma_so_avulsa`: `turma_id IS NULL OR modalidade = 'Avulsa'` | `chk_vinculocobranca_turma_avulsa_ou_pacote`: `turma_id IS NULL OR modalidade IN ('Avulsa','Pacote')` |

- Mensalidade continua sem turma.
- Pacote com `turma_id` = **pacote de turma** (a turma deve ser do tipo Pacote: regra do serviço); `turma_id` nulo = **pacote de aulas individuais**.
- `uq_vinculocobranca_chave_avulsa_ativa` (só Avulsa) **não muda**.
- "No máximo um pacote ativo por turma e um de aulas individuais por aluno" é regra do serviço, sem índice (pesquisa R6).

### Transições do pacote

```text
cadastrar ──► ATIVO ──excluir (professora)──────────────► INATIVO
                │──turma inativada pela professora────────► INATIVO   (saldo preservado)
                │──data_fim da turma passou (rotina)──────► INATIVO   (saldo preservado)
INATIVO ──reativar (professora, só com a turma em andamento)──► ATIVO
```

Só a coluna `ativo` muda nessas transições.

## `alunos_turma`, `aula`, `aula_aluno`, `pagamento`, `notificacao`, `aluno`

Sem mudança de estrutura **e sem mudança de dados**. A matrícula continua sendo a fonte dos participantes; o aluno de fora tem `aula_aluno.reposicao = 1`. Excluir ou reativar o aluno continua só mudando `aluno.ativo`.

## Transformação de dados da migração

```text
Para cada pacote P ativo (hoje todos têm turma_id nulo), do aluno A:
  se A está em exatamente 1 turma T, com T ativa,
     e todo aluno ativo matriculado em T tem pacote ativo:
       turma.tipo = 'Pacote'          (T)
       vinculo_cobranca.turma_id = T  (P)
  senão se A não está matriculado em nenhuma turma:
       nada muda (P é o pacote de aulas individuais de A)
  senão:
       nada muda; A/T entram nas consultas de "casos para decidir"
```

Invariantes depois da migração (conferidos no quickstart):
- total, valor, saldo e `ativo` de todos os vínculos iguais aos de antes;
- contagem de `alunos_turma`, `aula`, `aula_aluno` e `pagamento` **idênticas** às de antes;
- todo pacote com `turma_id` aponta para turma do tipo Pacote e o aluno está matriculado nela.

## Configuração

`Turmas:IntervaloEncerramentoMinutos` (inteiro, padrão 60): intervalo da rotina que inativa pacotes de turmas encerradas. Lido de fato por `TurmaEncerramentoDispatcherService`. A rotina também roda uma vez ao iniciar a aplicação.

## Estruturas calculadas (DTOs)

- `TurmaResponse`: + `tipo`, `dataInicio`, `dataFim`, `fechada` (calculada no backend, pesquisa R4) e `alunosComPacoteForaDaTurma` (quem saiu e ainda tem o pacote ativo da turma; só a lista de "voltar").
- `TurmaRequest`: + `tipo`, `dataInicio`, `dataFim`.
- `VinculoCobrancaResponse`: sem campo novo (`turmaId` e `turmaNome` já existem).
- `AlunoPresencaResponse`: sem campo novo (`reposicao` já existe).
