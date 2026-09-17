# Contrato: Endpoints de Reativação

Três novos endpoints, um por entidade, todos seguindo a mesma forma (mesmo padrão de
`DELETE /{id}` já existente, invertido).

## `PATCH /api/turmas/{id}/reativar`

- **Autorização**: `Professor` (herdada do `[Authorize]` de classe em `TurmasController`, igual
  ao `DELETE /api/turmas/{id}`).
- **Request**: sem corpo.
- **Sucesso — 200 OK**: `TurmaResponse` (mesma forma já devolvida por `GET /api/turmas/{id}`),
  com `Ativo: true`.
- **Erros**:
  - `404 Not Found` — id não existe (mesmo `NaoEncontradoException` já usado por `Excluir`).
  - `401/403` — sem token ou sem role `Professor` (mesmo comportamento já existente).
- **Efeito**: `Ativo` passa para `true`. Nenhum outro campo de `Turma` muda. Nenhum registro
  relacionado (`AlunosTurma`, aulas) é alterado.
- **Idempotência**: chamar num registro já `Ativo` é seguro — retorna 200 com o estado atual
  (`Ativo: true`), sem erro (FR-007).

## `PATCH /api/alunos/{id}/reativar`

- **Autorização**: `Professor` (herdada, igual ao `DELETE /api/alunos/{id}`).
- **Request**: sem corpo.
- **Sucesso — 200 OK**: `AlunoResponse`, com `Ativo: true`.
- **Erros**: mesmos de Turma (404, 401/403).
- **Efeito**: `Ativo` passa para `true`. Histórico de aulas/pagamentos do aluno permanece
  intacto.
- **Idempotência**: igual a Turma.

## `PATCH /api/materias/{id}/reativar`

- **Autorização**: `Professor` (herdada, igual ao `DELETE /api/materias/{id}`).
- **Request**: sem corpo.
- **Sucesso — 200 OK**: `MateriaResponse`, com `Ativo: true`.
- **Erros**: mesmos de Turma (404, 401/403).
- **Efeito**: `Ativo` passa para `true`. Vínculos existentes (turmas, aulas) permanecem
  intactos.
- **Idempotência**: igual a Turma.

## Fora de escopo (confirmado em research.md)

- Nenhuma checagem de unicidade nova (nome de Matéria, CPF de Aluno) — as já existentes rodam
  contra todos os registros, ativos ou não, então nunca há colisão possível ao reativar.
- Nenhum cascade para entidades relacionadas — reativar uma Turma não reativa uma Matéria
  inativa vinculada a ela, e vice-versa.
