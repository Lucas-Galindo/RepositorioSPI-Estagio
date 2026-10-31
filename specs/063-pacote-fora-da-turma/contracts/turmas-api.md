# Contrato: API de Turmas (delta da spec 063)

Base: `/api/turmas` (perfil Professor). Só as mudanças.

## `TurmaRequest` (POST e PUT)

| Campo | Tipo | Regra |
|---|---|---|
| `nome` | string | como hoje |
| `tipo` | `"Mensalidade"` \| `"Pacote"` | opcional na entrada; ausente = `Mensalidade` (compatível com clientes antigos) |
| `dataInicio` | `YYYY-MM-DD` \| null | obrigatória em turma **nova** de pacote; proibida em mensalidade |
| `dataFim` | `YYYY-MM-DD` \| null | obrigatória em turma **nova** de pacote; `>= dataInicio`; proibida em mensalidade |

Respostas de erro (como o resto da API): `400` validação; `409` regra de negócio.

### `PUT /api/turmas/{id}`

- `tipo` **não pode mudar** depois de criada: `409`.
- `dataInicio` **não pode mudar depois de preenchida**: `409` (mudá-la reabriria uma turma fechada). Em turma migrada sem data, preencher é aceito. `dataFim` pode ser ajustada (`>= dataInicio`).
- Em turma migrada sem datas, a edição aceita preencher `dataInicio` e `dataFim`. Prorrogar `dataFim` é permitido e **não** reativa pacotes.

### `DELETE /api/turmas/{id}` (inativar)

Além de `ativo = false`, os pacotes ativos da turma ficam **inativos no mesmo salvamento**, com saldo, valor, total de aulas e pagamentos intactos. Matrículas e aulas não mudam.

### `PATCH /api/turmas/{id}/reativar`

Só reativa a turma; **não** reativa pacotes.

## `TurmaResponse`

Acrescenta `tipo`, `dataInicio`, `dataFim` e `fechada` (booleano calculado no servidor: turma de pacote cuja `dataInicio` é anterior a hoje). `alunos` continua sendo os matriculados. `alunosComPacoteForaDaTurma` (só turma de pacote): alunos ativos que saíram da turma mas ainda têm o pacote dela ativo, ou seja, os únicos que podem voltar pela matrícula direta; vazio nas turmas de mensalidade.

## `POST /api/turmas/{id}/alunos` (matricular)

Em turma de **pacote**, aceita só o aluno que **já tem pacote ativo dessa turma** e só com a turma **em andamento** (a volta de quem saiu). Recusa com `409`:

| Caso | Mensagem (resumo) |
|---|---|
| Aluno sem pacote ativo da turma | "Turma de pacote: o aluno entra pelo cadastro do pacote dele, não por matrícula direta." |
| Turma encerrada | "A turma de pacote está encerrada." |

Turma de mensalidade: como hoje.

`DELETE /api/turmas/{id}/alunos/{alunoId}` (sair da turma) **não muda** e continua permitido em qualquer turma, inclusive de pacote já iniciada. Não altera vínculo, saldo nem pagamentos.
