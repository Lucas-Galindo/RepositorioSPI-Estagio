# Contrato: série de aulas (spec 077)

Dois endpoints novos; `POST /api/aulas` e `PUT /api/aulas/{id}` **não mudam**.

## Requisição (a mesma nos dois)

```json
{
  "descricao": "opcional",
  "materiaId": 1,
  "turmaId": 10,
  "alunoId": null,
  "dataInicial": "2026-12-01",
  "horarios": [
    { "diaSemana": 2, "horaInicio": "14:00:00", "horaFim": "15:00:00" },
    { "diaSemana": 4, "horaInicio": "10:00:00", "horaFim": "11:00:00" }
  ],
  "quantidadeAulas": 10,
  "dataFinal": null
}
```

- `diaSemana`: 0 = domingo … 6 = sábado.
- **Exatamente um** entre `quantidadeAulas` (1 a 60) e `dataFinal`.
- Turma **ou** aluno (como na aula única): aula de turma não aceita `alunoId`; individual exige `alunoId`.

## `POST /api/aulas/serie/previa` → 200

```json
{
  "total": 10, "limite": 60, "conflitos": 1, "podeCriar": false,
  "aulas": [ { "data": "2026-12-01", "diaSemana": 2, "horaInicio": "14:00:00", "horaFim": "15:00:00", "conflito": false } ]
}
```

Não grava nada. `podeCriar` = nenhuma ocorrência em conflito e todas as regras ok.

## `POST /api/aulas/serie` → 201

```json
{ "quantidade": 10, "aulas": [ /* AulaResponse de cada aula criada, na ordem das datas */ ] }
```

## Erros

| Status | Quando |
|---|---|
| 400 | formato inválido: sem horários, dia fora de 0–6, fim ≤ início, horários sobrepostos no mesmo dia, quantidade fora de 1–60, quantidade e data final juntas (ou nenhuma), data final antes da inicial, turma e aluno juntos ou nenhum |
| 404 | matéria, turma ou aluno inexistentes |
| 409 | `dataInicial` anterior a hoje; série com mais de 60 aulas (mensagem cita o limite de 60; o total exato não é calculado, a geração para ao passar do limite); **conflito de horário em qualquer data** (lista até 10 datas e "e mais N"); nenhuma aula é criada |

## Garantias

- Criação **atômica**; datas calculadas no servidor; cada aula criada é uma aula comum, "Agendada", sem vínculo com as demais.
- A prévia e a criação usam o mesmo cálculo e a mesma checagem de conflito.
