# Contrato: Participantes de reposição e efeito de cobrança nas aulas

## `POST /api/aulas/{id}/participantes` (novo)

- **Autorização**: `Professor` (herdada do controller).
- **Request**: `{ "alunoId": number }`
- **200 OK**: `AulaResponse` atualizado; o aluno aparece em `alunos[]` com `reposicao: true`.
- **404**: aula ou aluno não encontrado.
- **409**: aula não está "Agendada" ou está inativa; aula individual (sem turma); aluno inativo; aluno já participa da aula.

## `DELETE /api/aulas/{id}/participantes/{alunoId}` (novo)

- **200 OK**: `AulaResponse` atualizado, sem o aluno.
- **404**: aula não encontrada.
- **409**: aula não está "Agendada"; aluno não participa da aula; aluno é matriculado na turma (`reposicao: false`) — só participantes de reposição podem ser removidos individualmente.

## `PUT /api/aulas/{id}` (comportamento alterado)

- Participantes de reposição são preservados ao editar a aula. A lista de matriculados é recalculada a partir da turma (nova ou a mesma); um aluno de reposição que agora é matriculado deixa de ser marcado como reposição. Se a aula passa a ser individual, os participantes de reposição são removidos.

## `GET /api/aulas`, `GET /api/aulas/{id}`

- `alunos[]` (`AlunoPresencaResponse`) ganha `reposicao: boolean`.

## `POST /api/aulas/{id}/registrar-sessao` (efeito alterado, contrato igual)

- A cobrança de cada presença confirmada segue a ordem de `data-model.md` ("Decisão de cobrança"): mensalidade do aluno → pacote do aluno com saldo → avulsa do contexto da aula → bloqueio (pacote esgotado sem avulsa) → valor da aula.
- `avisos[]` passa a incluir também "Pacote esgotado: {aluno} foi cobrado como aula avulsa. Renove o pacote para voltar a usar o saldo." quando a presença caiu na avulsa porque o pacote acabou (além do aviso de bloqueio já existente).
