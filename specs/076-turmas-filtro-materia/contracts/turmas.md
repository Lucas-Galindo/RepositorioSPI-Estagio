# Contrato: `GET /api/turmas` (spec 076)

Só **parâmetros novos opcionais** na query; resposta (`TurmaResponse[]`) e ordenação (por nome) inalteradas.

| Parâmetro | Tipo | Significado |
|---|---|---|
| `nome`, `ativo` | (existentes) | sem mudança |
| `materiaId` | int | só turmas dessa matéria |
| `semMateria` | bool | `true` = só turmas sem matéria |

- Todos se combinam (E); ausentes = sem filtro.
- `materiaId` e `semMateria=true` juntos: **400** com mensagem clara.
- Matéria inexistente em `materiaId`: lista vazia (200), sem erro.
- Sem os parâmetros novos, a resposta é idêntica à de antes (FR-004).
