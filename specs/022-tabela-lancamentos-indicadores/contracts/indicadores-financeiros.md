# Contract: `GET /api/relatorios/indicadores-financeiros`

Endpoint já existente (`RelatoriosController.IndicadoresFinanceiros`) — esta feature apenas
**estende a resposta**, sem alterar a assinatura da requisição nem os campos já existentes.

## Request

Sem mudança.

```
GET /api/relatorios/indicadores-financeiros?inicio={date}&fim={date}&turmaId={int}&materiaId={int}&alunoId={int}
Authorization: Bearer {accessToken}
```

| Query param | Tipo | Obrigatório | Efeito sobre a lista de lançamentos (novo campo) |
|---|---|---|---|
| `inicio` | `date` (`yyyy-MM-dd`) | Não (padrão: início do mês corrente) | Filtra `DataVencimento >= inicio` para entradas e saídas |
| `fim` | `date` | Não (padrão: fim do mês corrente) | Filtra `DataVencimento <= fim` para entradas e saídas |
| `turmaId` | `int` | Não | Filtra apenas as **entradas** cujo aluno pertence à turma; saídas nunca são afetadas |
| `materiaId` | `int` | Não | Filtra apenas as **entradas** vinculadas a aulas da matéria; saídas nunca são afetadas |
| `alunoId` | `int` | Não | Filtra apenas as **entradas** do aluno; saídas nunca são afetadas |

## Response — `200 OK`

Corpo estendido (campos já existentes preservados; apenas `lancamentos` é novo):

```json
{
  "indicadores": {
    "taxaInadimplenciaPercentual": 0,
    "prazoMedioAtrasoDias": null,
    "margemSegurancaPercentual": 0,
    "fluxoCaixaOperacional": 0,
    "indiceCoberturaCustosFixos": null,
    "gargaloCaixa": { "diaMaiorEntrada": null, "valorMaiorEntrada": 0, "diaMaiorSaida": null, "valorMaiorSaida": 0 }
  },
  "fluxoCaixaMensal": [
    { "ano": 2026, "mes": 9, "entradas": 0, "saidas": 0, "saldo": 0 }
  ],
  "lancamentos": [
    {
      "id": 42,
      "tipo": "Entrada",
      "descricao": "Mensalidade — João Silva",
      "dataVencimento": "2026-09-10",
      "valor": 350.00,
      "status": "Pago"
    },
    {
      "id": 17,
      "tipo": "Saida",
      "descricao": "Aluguel da sala",
      "dataVencimento": "2026-09-05",
      "valor": 1200.00,
      "status": "Pendente"
    }
  ]
}
```

### Campo novo: `lancamentos[]`

| Campo | Tipo | Notas |
|---|---|---|
| `id` | `int` | Id do `Pagamento` (quando `tipo = "Entrada"`) ou da `ContaPagar` (quando `tipo = "Saida"`) — não é uma chave global única entre os dois tipos |
| `tipo` | `"Entrada" \| "Saida"` | Usado pelo frontend para o filtro de tipo (não requer nova chamada de API ao alternar) |
| `descricao` | `string`, nunca vazio | Descrição cadastrada, ou fallback (nome do aluno / favorecido / categoria da despesa) já resolvido pelo backend — ver `data-model.md` |
| `dataVencimento` | `string` (`yyyy-MM-dd`) | Data de vencimento do lançamento — mesma semântica de "Gargalo de caixa"/"Fluxo de caixa" |
| `valor` | `number` | Não exibido nesta versão do frontend; incluído para não quebrar o contrato se uma coluna de valor for adicionada depois |
| `status` | `string` | `"Pendente"`, `"Atrasado"` (calculado) ou `"Pago"`; nunca `"Cancelado"` (esses são excluídos da lista) |

`lancamentos` já vem ordenado por `dataVencimento` decrescente (FR-011) — o frontend não precisa
reordenar.

## Response — `500 Internal Server Error`

Sem mudança (mesmo formato `Problem` já usado pelos demais endpoints de `RelatoriosController`).

## Compatibilidade

Mudança **aditiva e não destrutiva**: nenhum campo existente de
`IndicadoresFinanceirosFiltradosResponse` é removido ou renomeado (incluindo `gargaloCaixa`, que
permanece na resposta mesmo não sendo mais renderizado pela tela de Indicadores — ver
`data-model.md`). Qualquer outro consumidor deste endpoint continua funcionando sem alteração.
