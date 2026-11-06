# Contrato: `GET /api/aulas` (spec 074)

Só **parâmetros novos opcionais** na query; resposta (`AulaResponse[]`) e ordenação inalteradas.

| Parâmetro | Tipo | Significado |
|---|---|---|
| `status`, `turmaId`, `alunoId`, `dataInicio`, `dataFim` | (existentes) | sem mudança |
| `materiaId` | int | só aulas dessa matéria |
| `horaInicioDe` | `HH:mm` | horário de início **maior ou igual** |
| `horaInicioAte` | `HH:mm` | horário de início **menor ou igual** |

- Todos os filtros se combinam (E); ausentes = sem filtro; `De` > `Até` = lista vazia (sem erro).
- Valor de horário inválido: 400 padrão.
- Sem os parâmetros novos, a resposta é idêntica à de antes (FR-004).
