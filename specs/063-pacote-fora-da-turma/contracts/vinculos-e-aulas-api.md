# Contrato: Vínculos de cobrança e aulas (delta da spec 063)

## Vínculos — `/api/alunos/{alunoId}/vinculos-cobranca`

### `POST` e `PUT` com `modalidade = "Pacote"`

| Campo | Regra |
|---|---|
| `turmaId` | **opcional**. Nulo = pacote de aulas individuais. Preenchido = pacote de turma. Mensalidade continua proibindo `turmaId` (`400`) |

`409` (e `404` para turma inexistente) nos casos:

| Caso | Mensagem (resumo) |
|---|---|
| Turma inativa ou de outra professora | "Turma não encontrada ou inativa." |
| Turma não é de pacote | "Só turma do tipo Pacote recebe pacote." |
| Turma de pacote já iniciada (`dataInicio` anterior a hoje), no **cadastro** | "A turma de pacote já começou e está fechada: ninguém novo entra." |
| O aluno já tem pacote ativo **desta turma** | "O aluno já tem um pacote ativo desta turma." |
| O aluno já tem pacote ativo de aulas individuais (e o novo também é) | "O aluno já tem um pacote de aulas individuais ativo." |
| Aluno com mensalidade ativa | como hoje (mensalidade cobre tudo) |
| `PUT` mudando `turmaId` de um pacote | "A turma de um pacote não pode ser alterada. Inative-o e cadastre outro." |

Pacotes de **turmas diferentes** e o de aulas individuais podem estar ativos ao mesmo tempo.

Efeitos (no mesmo salvamento):
- `POST` / reativar pacote de turma: o aluno é **matriculado** na turma, se ainda não estiver.
- `DELETE` (inativar): a matrícula **não** é alterada.

### `PATCH .../{id}/reativar`

Para pacote de turma: exige a turma **em andamento** (ativa e com `dataFim` vazia ou de hoje em diante); **não** exige que a turma não tenha começado. Recusa com `409` ("A turma já terminou; prorrogue a data de fim para reativar o pacote."). Repete a regra de combinação e matricula o aluno de novo se necessário. Pacote de aulas individuais: como hoje.

`VinculoCobrancaResponse` não ganha campos (`turmaId`, `turmaNome` já existem).

## Alunos — `/api/alunos`

`DELETE` (excluir logicamente) e `PATCH .../reativar` **não mudam**: não alteram pacote, saldo nem matrícula (FR-020).

## Aulas — `/api/aulas`

- Cadastro e edição de aula de turma: **sem mudança** (participantes = matriculados ativos + reposições preservadas).
- `POST /api/aulas/{id}/participantes` (reposição): sem mudança.
- `GET /api/aulas/{id}`: `alunos[].reposicao` já existe e é o que o frontend usa para a seção "Outros alunos".

## Cobrança na presença (registrar sessão e criação retroativa)

Sem mudança de contrato; muda o efeito. "Mesma turma" = a turma da aula é a do pacote.

| Aluno | Aula da mesma turma | Aula de outra turma | Aula individual |
|---|---|---|---|
| Mensalidade (e planos da 062) | sem cobrança | sem cobrança | sem cobrança |
| Pacote de turma, com saldo | consome 1 | cobra avulsa / valor da aula | cobra individualmente |
| Pacote de aulas individuais, com saldo | cobra avulsa / valor da aula | cobra avulsa / valor da aula | consome 1 |
| Pacote aplicável (do grupo da aula) esgotado, sem avulsa | barrado (aviso) | — | — |
| Pacote fora do grupo, esgotado ou não | não interfere | não interfere | não interfere |
| Avulso / sem vínculo | cobra avulsa / valor da aula | cobra avulsa / valor da aula | cobra avulsa / valor da aula |

Toda cobrança é gerada **automaticamente em Contas a Receber** quando há presença confirmada (FR-023), inclusive para "outros alunos". Falta nunca consome nem cobra.
