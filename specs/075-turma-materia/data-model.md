# Data Model: Turma ↔ Matéria (spec 075)

## Tabela `turma` (única alterada)

| Campo | Tipo | Regra |
|---|---|---|
| `materia_id` (novo) | `INT NULL` | chave para `materias(id)` (`fk_turma_materia`); vazio = turma sem matéria; `AFTER nome` |

Índice automático da chave estrangeira. Nada mais muda em `turma`; `aula`, `vinculo_cobranca`, `pagamento`, `alunos_turma` e `materias` **não** são tocadas.

## Preenchimento inicial (migração)

Para cada turma com `materia_id IS NULL`: se as aulas **ativas** dela (`aula.turma_id = turma.id AND aula.ativo = TRUE`) usam exatamente **uma** `materia_id`, a turma recebe essa matéria; caso contrário, fica `NULL`.

## Entidade

`Turma.MateriaId : int?` e `Turma.Materia : Materia?`. Regras (no serviço): ao criar ou **trocar** a matéria, ela deve existir e estar ativa; manter a mesma matéria (mesmo inativada depois) é permitido; `null` no PUT remove.
