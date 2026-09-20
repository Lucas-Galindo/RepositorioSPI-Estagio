# Contrato: API de Vínculos de Cobrança

Base: `api/alunos/{alunoId}/vinculos-cobranca` — `[Authorize(Roles = Professor)]` (Bearer JWT), como `TurmasController`. JSON em camelCase; enums como string (`JsonStringEnumConverter` global).

## Tipos

**VinculoCobrancaRequest** (POST e PUT)

```json
{
  "turmaId": 12,            // int | null — null = "Atendimento individual"
  "modalidade": "Mensalidade", // "Avulsa" | "Mensalidade" | "Pacote"
  "valor": 350.00,          // decimal > 0
  "aulasIncluidas": 8,      // int | null — só com Mensalidade (>= 1)
  "saldoAulas": null        // int | null — só com Pacote (>= 0)
}
```

**VinculoCobrancaResponse**

```json
{
  "id": 5,
  "alunoId": 3,
  "turmaId": 12,
  "turmaNome": "Turma A",   // null quando turmaId é null (a UI mostra "Atendimento individual")
  "modalidade": "Mensalidade",
  "valor": 350.00,
  "aulasIncluidas": 8,
  "saldoAulas": null,
  "ativo": true
}
```

## Endpoints

| Método | Rota | Descrição | Sucesso | Erros |
|---|---|---|---|---|
| GET | `/` | Lista vínculos do aluno; query `ativo` (bool, opcional). Ordenação: ativos primeiro, depois turma (individual por último), por nome. | 200 `VinculoCobrancaResponse[]` | 404 aluno inexistente |
| GET | `/{id}` | Obtém um vínculo do aluno | 200 | 404 |
| POST | `/` | Cadastra vínculo (nasce `ativo = true`) | 201 (`CreatedAtAction` → GET `/{id}`) | 400 validação; 404 aluno/turma inexistente; 409 turma inativa ou aluno fora da turma; 409 já existe vínculo ativo para a combinação |
| PUT | `/{id}` | Atualiza todos os campos (Turma, Modalidade, Valor, AulasIncluidas, SaldoAulas) | 200 | 400; 404; 409 (turma alterada inválida, conflito de combinação, ou vínculo excluído — "Reative o vínculo antes de editá-lo.") |
| DELETE | `/{id}` | Exclusão lógica (`ativo = false`); idempotente | 200 | 404 |
| PATCH | `/{id}/reativar` | Reativa (`ativo = true`) | 200 `VinculoCobrancaResponse` | 404; 409 já existe outro ativo para a mesma combinação (FR-014) |

## Formato de erro

Igual ao restante da API: 400 → lista de strings (mensagens do FluentValidation); 404/409 → string com a mensagem; 500 → `ProblemDetails`. O `extrairErro` do frontend (`lib/api/client.ts`) já trata os três formatos.

## Mensagens de erro (pt-BR, sem acentuação no backend, como no restante do projeto)

- `Ja existe um vinculo de cobranca ativo para este aluno nesta turma.`
- `Ja existe um vinculo de cobranca ativo para este aluno em atendimento individual.`
- `A turma informada esta inativa ou o aluno nao participa dela.`
- `Aulas incluidas so se aplica a modalidade Mensalidade.`
- `Saldo de aulas so se aplica a modalidade Pacote.`
- `O Valor deve ser maior que zero.`
- `Reative o vinculo antes de edita-lo.`

## Não faz parte do contrato

Nenhum endpoint desta feature é chamado por `AulaService`, `PagamentoService` ou qualquer fluxo de geração de cobrança (FR-012). `TurmaResumoResponse` (dentro de `GET /api/alunos/{id}`) ganha o campo aditivo `ativo` (bool).
