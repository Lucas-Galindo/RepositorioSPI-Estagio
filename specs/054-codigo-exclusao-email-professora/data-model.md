# Data Model: Código de Exclusão de Professora via E-mail da Própria Professora

Não aplicável. Nenhuma entidade nova, nenhum campo novo, nenhuma mudança de schema. `ExclusaoProfessorToken` (specs/044) continua exatamente como está — `AdminId` (quem solicitou) e `ProfessorId` (quem é afetada) já existem e continuam gravados; só o destino do e-mail enviado a partir desse token muda (ver [research.md](./research.md) R1).
