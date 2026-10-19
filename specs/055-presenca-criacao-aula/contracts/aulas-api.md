# Contrato: Endpoints de Aula afetados

Dois endpoints já existentes mudam de comportamento condicionalmente — nenhum endpoint novo é criado.

## `POST /api/aulas` (`AulasController.Cadastrar`)

- **Autorização**: `Professor` (herdada, sem mudança).
- **Request**: `AulaRequest`, com um campo novo opcional:
  - `presencas?: Record<number, boolean>` — chave = `AlunoId`, valor = presente. Obrigatório (não pode ser omitido/nulo) quando `dataInicio` é hoje ou passada; ignorado/irrelevante quando `dataInicio` é futura.
- **Sucesso — 201 Created**: `AulaResponse` (mesma forma de hoje), com:
  - `status: "Realizada"` quando `dataInicio` é hoje ou passada (antes: sempre `"Agendada"`).
  - `status: "Agendada"` quando `dataInicio` é futura (sem mudança).
  - `alunos[].presente` já preenchido (não mais `null`) quando a aula nasce `"Realizada"`.
  - `avisos: string[]` preenchido com o mesmo aviso de "pacote esgotado" que `RegistrarSessaoAsync` já produz hoje, quando aplicável (antes, `Cadastrar` nunca preenchia `avisos`).
- **Erros novos**:
  - `409 Conflict` — `dataInicio` é hoje ou passada e `presencas` está ausente/nulo: `"Informe a presenca de cada aluno para criar uma aula retroativa."`
  - `409 Conflict` — `presencas` não cobre todos os alunos da turma/aluno individual: `"Presenca do aluno {id} nao foi informada."` (mesma mensagem já usada por `RegistrarSessaoAsync` hoje).
- **Erros existentes**: inalterados (404 matéria/turma/aluno não encontrado; 409 conflito de horário).
- **Efeito**: quando a aula nasce `"Realizada"`, os mesmos efeitos financeiros que `POST /api/aulas/{id}/registrar-sessao` já produz hoje são aplicados no mesmo momento — geração de cobrança (vínculo Avulsa ou sem vínculo), decremento de saldo (vínculo Pacote), nenhum efeito avulso (vínculo Mensalidade), incremento de `Aluno.Frequencia` por presença confirmada, recálculo da hora programada dos lembretes da turma (specs/005 "Estoria 5").
- **Idempotência**: N/A — é uma criação, como hoje.

## `PUT /api/aulas/{id}` (`AulasController.Atualizar`)

- **Autorização**: `Professor` (herdada, sem mudança).
- **Request**: `AulaRequest` (mesma forma de hoje; o campo novo `presencas` é ignorado por este endpoint — presença de uma aula já existente não é editável por aqui, nem antes nem depois desta feature).
- **Sucesso — 200 OK**: `AulaResponse`.
  - Quando `aula.status != "Realizada"`: comportamento idêntico a hoje (todos os campos podem ser editados, sujeitos às validações já existentes).
  - Quando `aula.status == "Realizada"` **e** o request não tenta mudar `materiaId`, `turmaId`, `alunoId`, `dataInicio`, `horaInicio` ou `horaFim` (comparados ao valor atual da aula): a `Descricao` é atualizada e a resposta reflete a mudança — **novo** comportamento (antes, qualquer edição de uma aula Realizada era rejeitada).
- **Erros**:
  - `409 Conflict` — `aula.status == "Realizada"` **e** o request tenta mudar qualquer campo além da Descrição: mesma mensagem de hoje ("Uma aula ja realizada nao pode ser alterada.") ou uma variação equivalente informando que só a descrição pode mudar.
  - Erros existentes (404, 409 de conflito de horário) continuam se aplicando normalmente quando `aula.status != "Realizada"`.
- **Efeito**: nenhuma mudança de alunos vinculados, nenhum recálculo de lembrete, nenhuma revalidação de conflito de horário quando a edição é só da Descrição de uma aula Realizada (nenhum desses aspectos pode ter mudado, já que os campos que os disparam estão bloqueados).
- **Idempotência**: enviar a mesma Descrição duas vezes é seguro (sem efeito colateral adicional).

## Fora de escopo (confirmado em research.md)

- Nenhum endpoint novo para "editar só a descrição" — reaproveita o `PUT` já existente (R5).
- Nenhuma mudança em `POST /api/aulas/{id}/registrar-sessao` nem em `DELETE`/`cancelar` — continuam exatamente como hoje (incluindo o recálculo de lembrete já corrigido anteriormente para `ExcluirAsync`/`RegistrarSessaoAsync`/`CancelarAsync`).
- Nenhuma mudança na regra de quais vínculos de cobrança geram cobrança, decrementam saldo, ou não têm efeito (FR-010) — só reaproveita a regra já existente.
