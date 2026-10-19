# Contrato: Vínculos de Cobrança (mudanças)

Rotas existentes em `api/alunos/{alunoId}/vinculos-cobranca` — nenhuma rota nova, só regras e um campo novo na resposta.

## `POST` / `PUT {id}` — cadastrar / editar

- **Request** (`VinculoCobrancaRequest`, sem mudança de forma): `turmaId` passa a ser aceito **só** quando `modalidade = "Avulsa"`.
- **400 Bad Request** (validador): `turmaId` informado com modalidade Mensalidade ou Pacote — "Mensalidade e Pacote valem para todas as aulas do aluno e não têm turma."
- **409 Conflict** (serviço), conforme o vínculo resultante e os demais **ativos** do aluno (excluindo o próprio, na edição):
  - Mensalidade com qualquer outro vínculo ativo — "O aluno já tem outros vínculos ativos. Desative-os antes de cadastrar a mensalidade, que cobre todas as aulas."
  - Pacote ou Avulsa com mensalidade ativa — "O aluno tem mensalidade ativa, que já cobre todas as aulas."
  - Pacote com outro pacote ativo — "O aluno já tem um pacote ativo."
  - Avulsa com outra avulsa ativa no mesmo contexto — mensagens atuais ("...nesta turma." / "...em atendimento individual.").
- Demais regras existentes inalteradas (turma ativa com o aluno matriculado para Avulsa com turma; bloqueio de redução de total/teto da specs/052; editar vínculo inativo exige reativar).

## `PATCH {id}/reativar`

- Mesmas regras de combinação acima (409), avaliadas contra os vínculos ativos do aluno.

## `GET` — listar

- **Resposta**: cada `VinculoCobrancaResponse` ganha `conflito: string | null`. Preenchido só em vínculos ativos de um aluno em combinação proibida herdada da regra antiga:
  - aluno com mensalidade ativa e mais de um vínculo ativo → todos os ativos: "Mensalidade não pode coexistir com outros vínculos ativos."
  - senão, mais de um pacote ativo → cada pacote: "Mais de um pacote ativo."
- `GET {id}`, `POST`, `PUT`, `PATCH reativar` sempre devolvem `conflito: null`.
