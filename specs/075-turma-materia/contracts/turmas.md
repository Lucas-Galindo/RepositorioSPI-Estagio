# Contrato: turmas (spec 075)

Mudança **aditiva** em `POST /api/turmas`, `PUT /api/turmas/{id}`, `GET /api/turmas` e `GET /api/turmas/{id}`.

## Requisição (`TurmaRequest`)

| Campo | Tipo | Observação |
|---|---|---|
| `materiaId` (novo) | int, opcional | no POST, ausente = sem matéria; no **PUT**, ausente/nulo **remove** o vínculo (o formulário sempre envia o valor atual) |

## Resposta (`TurmaResponse`)

| Campo | Tipo | Observação |
|---|---|---|
| `materiaId` (novo) | int \| null | matéria da turma |
| `materiaNome` (novo) | string \| null | nome da matéria (mesmo se inativa) |

## Erros

- Matéria inexistente: **404** com mensagem clara.
- Matéria inativa ao criar o vínculo ou trocá-lo: **409** com mensagem clara. Manter a mesma matéria já vinculada não revalida.

## Garantias

- Campos existentes inalterados; turma sem matéria devolve `materiaId = null` e `materiaNome = null`.
- Nenhuma regra de aulas, cobrança ou matrícula muda (FR-007, FR-008).
