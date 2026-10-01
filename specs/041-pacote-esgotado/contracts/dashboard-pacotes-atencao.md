# Contrato: `GET /api/dashboard` (alteração)

Endpoint, autorização (`Professor`) e parâmetros **inalterados**. A resposta ganha um campo aditivo.

## Campo novo em `DashboardResponse`

| Campo | Tipo | Descrição |
|---|---|---|
| `pacotesEmAtencao` | `PacoteEmAtencaoItem[]` | Vínculos `Pacote` ativos, de aluno ativo, com saldo informado ≤ 2. `[]` quando ninguém precisa de atenção. |

### `PacoteEmAtencaoItem`

| Campo | Tipo | Descrição |
|---|---|---|
| `vinculoId` | int | Id do vínculo |
| `alunoId` | int | Para o link de renovação (`/alunos/{alunoId}`) |
| `alunoNome` | string | Nome do aluno |
| `contexto` | string | Nome da turma, ou `"Atendimento individual"` |
| `saldoAulas` | int | 0, 1 ou 2 |
| `estado` | `"Esgotado"` \| `"Atencao"` | `Esgotado` = saldo 0; `Atencao` = saldo 1 ou 2 |

**Ordem garantida pelo backend**: `Esgotado` primeiro; depois `saldoAulas` crescente; depois `alunoNome`. O frontend renderiza na ordem recebida, sem reordenar nem recalcular `estado`.

Exemplo:

```json
{
  "pacotesEmAtencao": [
    { "vinculoId": 3, "alunoId": 9, "alunoNome": "Bruno", "contexto": "Atendimento individual", "saldoAulas": 0, "estado": "Esgotado" },
    { "vinculoId": 5, "alunoId": 7, "alunoNome": "Ana", "contexto": "Turma A", "saldoAulas": 1, "estado": "Atencao" },
    { "vinculoId": 6, "alunoId": 8, "alunoNome": "Carla", "contexto": "Turma B", "saldoAulas": 2, "estado": "Atencao" }
  ]
}
```

## Não-regressão

Todos os campos existentes de `DashboardResponse` permanecem com o mesmo nome, tipo e significado.

## Sem endpoint de renovação

A renovação continua sendo a edição já existente do vínculo (`PUT` de `VinculosCobrancaController`, specs/037) — esta feature não adiciona rota nem lógica de renovação (FR-010).
