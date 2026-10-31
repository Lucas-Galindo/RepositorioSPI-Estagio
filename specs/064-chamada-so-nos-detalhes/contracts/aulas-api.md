# Contrato: API de aulas (spec 064)

Só o que muda em relação ao contrato atual. Autenticação e perfil (`Professor`) não mudam.

## `POST /api/aulas` — agendar

**Request** (`AulaRequest`): perde o campo `presencas`.

```json
{ "descricao": null, "materiaId": 1, "turmaId": 3, "alunoId": null,
  "dataInicio": "2026-10-30", "horaInicio": "10:00:00", "horaFim": "11:00:00" }
```

- Um cliente antigo que ainda envie `presencas` não recebe erro: o campo é **ignorado**.
- Toda aula é criada com `status = "Agendada"`, **em qualquer data** (hoje, passado ou futuro). Nenhuma presença, frequência de aluno ou conta a receber é criada (FR-002).
- Erros inalterados: 400 (validação), 404 (matéria/turma/aluno), 409 (conflito de horário da professora, em qualquer data).
- Removido: o 409 "Informe a presenca de cada aluno para criar uma aula retroativa."

## `GET /api/aulas`, `GET /api/aulas/{id}`, `POST /api/aulas`, `PUT /api/aulas/{id}`, `POST …/registrar-sessao`, `POST …/cancelar`, `POST/DELETE …/participantes` — `AulaResponse`

Todas as respostas que devolvem uma aula ganham dois campos, calculados pelo servidor com o relógio de **America/Sao_Paulo**:

| Campo | Tipo | Significado |
|---|---|---|
| `chamadaLiberada` | boolean | `true` só se a aula está ativa, `"Agendada"` e o horário atual ≥ `dataInicio` + `horaInicio` |
| `segundosParaChamada` | integer ou `null` | segundos até o início, quando a aula está ativa, `"Agendada"` e ainda não começou; `null` nos demais casos |

Exemplos:

| Situação (agora = 2026-10-31 14:00) | `chamadaLiberada` | `segundosParaChamada` |
|---|---|---|
| Agendada, 31/10 às 15:00 | `false` | `3600` |
| Agendada, 31/10 às 14:00 (minuto exato) | `true` | `null` |
| Agendada, 31/10 às 13:30 (em andamento) | `true` | `null` |
| Agendada, 30/10 | `true` | `null` |
| Agendada, 01/11 | `false` | valor positivo |
| Realizada / Cancelada / excluída | `false` | `null` |

## `POST /api/aulas/{id}/registrar-sessao` — registrar a chamada

Request inalterado (`{ "presencas": { "<alunoId>": true|false } }`, todos os alunos da aula).

| Resposta | Quando |
|---|---|
| 200 | chamada registrada: aula vira `"Realizada"`, `Avisos` como hoje, `FrequenciaRegistradaEm` gravada |
| 404 `Aula nao encontrada.` | aula inexistente **ou excluída** (novo para a excluída) |
| 409 `Somente aulas com status 'Agendada' podem ter a sessao registrada.` | aula já realizada ou cancelada (inalterado) |
| 409 `A chamada ficara disponivel a partir das HH:mm de dd/MM/yyyy.` | **novo**: o horário de início ainda não chegou; nada é gravado |
| 409 `Presenca do aluno … nao foi informada.` | inalterado |

A ordem das verificações é: existe/ativa → status → início → presenças. O 409 sai pelo `ErrosDeNegocioFilter` (spec 065), pois a ação já declara `ProducesResponseType(409)`.

## Não mudam

`PUT /api/aulas/{id}` (editar, inclusive a descrição de aula realizada), cancelar, excluir e reposição seguem as mesmas regras. Chamada bloqueada **não** bloqueia nenhuma delas (FR-009).
