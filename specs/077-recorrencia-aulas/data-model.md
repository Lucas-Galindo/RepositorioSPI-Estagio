# Data Model: Recorrência na criação de aulas (spec 077)

**Nenhuma mudança no banco** (FR-011): sem tabela, coluna ou migração. Cada ocorrência da série é gravada como uma `aula` comum (`status = 'Agendada'`, `ativo = TRUE`) com suas linhas em `aula_aluno`, exatamente como no cadastro de uma aula. Não existe "série" persistida: não há `serie_id`, e editar ou cancelar continuam aula por aula.

Tipos novos só na aplicação (DTOs): `RecorrenciaAulaRequest`, `HorarioSerieRequest`, `PreviaSerieResponse`, `OcorrenciaSerieResponse`, `SerieCriadaResponse`.
