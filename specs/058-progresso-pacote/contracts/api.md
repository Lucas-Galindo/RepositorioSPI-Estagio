# Contrato: progresso de pacote (spec 058)

## `GET /api/alunos/{alunoId}/vinculos-cobranca` e `GET .../{id}` (alterado)

Cada item de Pacote traz `progresso` (data-model.md) no lugar do antigo `aulasDadas`. Nos demais vínculos, `progresso` vem nulo.

## `POST /api/alunos/{alunoId}/vinculos-cobranca` (efeito alterado)

Um Pacote com `totalAulasPacote` informado e `saldoAulas` nulo é salvo com `saldoAulas = totalAulasPacote`.

## `PUT /api/alunos/{alunoId}/vinculos-cobranca/{id}` (validação alterada)

Responde **409** quando `totalAulasPacote` < aulas dadas (presenças registradas), com a mensagem "O novo total de aulas do pacote nao pode ser menor que as aulas ja dadas (N)."

## `GET /api/alunos/{alunoId}/vinculos-cobranca/{id}/aulas` (novo)

- **200**: devolve `AulaDoPacoteResponse[]`, as aulas que contaram como dadas, mais recentes primeiro.
- **404**: aluno ou vínculo inexistente, ou vínculo de outro aluno.

## `GET /api/dashboard?periodoInicio=&periodoFim=&alunoId=` (alterado)

- `alunoId` é novo e opcional; filtra só a seção de pacotes.
- `pacotesEmAtencao` é substituído por `pacotes` (`PacotesResumoResponse`).
