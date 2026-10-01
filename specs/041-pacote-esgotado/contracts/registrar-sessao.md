# Contrato: `POST /api/aulas/{id}/registrar-sessao` (alteração)

Endpoint e request **inalterados** (`RegistrarSessaoRequest.Presencas`: `AlunoId → presente`). Só a resposta ganha um campo aditivo e o comportamento ganha uma regra.

## Regra nova (FR-005/006/007)

Para cada aluno marcado como `true` (presente) na aula:

1. Resolve o vínculo ativo do contexto da aula (`TurmaId` da aula, ou nenhum para individual).
2. Se o vínculo existe, é `Pacote` e `SaldoAulas == 0` → **bloqueado**: `Presente` é gravado `false` **e `MotivoNaoRegistro` é gravado `"PacoteEsgotado"`**, sem frequência, sem débito, sem conta a receber; uma mensagem entra em `avisos`.
3. Caso contrário → comportamento atual (presença, frequência, e débito/cobrança conforme a modalidade — specs/038).

Nunca bloqueia: aluno marcado ausente; vínculo com saldo `null`; vínculo `Avulsa`/`Mensalidade`; vínculo inativo; aluno sem vínculo no contexto; vínculo esgotado só em **outro** contexto.

O registro da aula **nunca** é recusado por causa de um aluno esgotado: a aula fica `Realizada` para todos, inclusive quando todos os presentes estão esgotados.

## Resposta (`200 OK`, `AulaResponse`)

Campos novos:

| Campo | Tipo | Descrição |
|---|---|---|
| `avisos` | `string[]` | Aviso **imediato** (não persistido): uma mensagem por aluno bloqueado; `[]` quando ninguém foi bloqueado (e em toda outra rota que devolve `AulaResponse`) |
| `alunos[].motivoNaoRegistro` | `string \| null` | Motivo **persistido** de a presença não ter sido gravada. Hoje só `"PacoteEsgotado"`; `null` = comportamento normal (presente) ou falta comum — **nunca** indica bloqueio. Aparece também em `GET /api/aulas/{id}` e na listagem, ou seja, no histórico da aula, depois do momento do registro. |

**Como a professora distingue no histórico**: `presente == false` + `motivoNaoRegistro == null` → faltou; `presente == false` + `motivoNaoRegistro == "PacoteEsgotado"` → foi barrado porque o pacote acabou.

Exemplo:

```json
{
  "id": 42,
  "status": "Realizada",
  "alunos": [
    { "alunoId": 7, "nome": "Ana", "presente": true, "motivoNaoRegistro": null },
    { "alunoId": 9, "nome": "Bruno", "presente": false, "motivoNaoRegistro": "PacoteEsgotado" }
  ],
  "avisos": [
    "Pacote esgotado: Bruno não teve a presença registrada. Renove o pacote (edite o Vínculo de Cobrança) para liberar novos registros."
  ]
}
```

## Erros

Inalterados (`404` aula não encontrada, `409` aula que não está `Agendada` ou presença não informada para algum aluno). **Não** existe erro novo — pacote esgotado nunca produz `409`.
