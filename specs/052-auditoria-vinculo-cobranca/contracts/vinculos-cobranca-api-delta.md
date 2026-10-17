# Contrato (delta): API de Vínculos de Cobrança

Estende o contrato já existente em [specs/037-vinculo-cobranca/contracts/vinculos-cobranca-api.md](../../037-vinculo-cobranca/contracts/vinculos-cobranca-api.md). Mesma base, autenticação e formato de erro — não repetidos aqui. Nenhum endpoint novo; nenhum método/rota removido.

## Tipos (campos aditivos)

**VinculoCobrancaRequest** (POST e PUT) — ganha 1 campo:

```json
{
  "turmaId": 12,
  "modalidade": "Pacote",
  "valor": 350.00,
  "aulasIncluidas": null,
  "saldoAulas": 10,
  "totalAulasPacote": 10   // NOVO — int | null, só com Pacote (>= 1)
}
```

**VinculoCobrancaResponse** — ganha 3 campos (2 somente-leitura, calculados):

```json
{
  "id": 5,
  "alunoId": 3,
  "turmaId": 12,
  "turmaNome": "Turma A",
  "modalidade": "Pacote",
  "valor": 350.00,
  "aulasIncluidas": null,
  "saldoAulas": 7,
  "totalAulasPacote": 10,      // NOVO — espelha o que foi cadastrado/editado
  "aulasDadas": 3,             // NOVO, somente-leitura — so Pacote: totalAulasPacote - saldoAulas; null se totalAulasPacote for null
  "aulasDadasNoMes": null,     // NOVO, somente-leitura — so Mensalidade: contagem da competencia corrente; null para Avulsa/Pacote
  "ativo": true
}
```

## Endpoints

Sem mudança de rota/método — `POST /`, `PUT /{id}` passam a aceitar `totalAulasPacote` no corpo; todas as respostas (`GET /`, `GET /{id}`, `POST /`, `PUT /{id}`, `PATCH /{id}/reativar`) passam a incluir os 3 campos novos.

## Novos erros de validação (400, FluentValidation)

- `Total de aulas do pacote so se aplica a modalidade Pacote.`
- `Total de aulas do pacote deve ser no minimo 1.`

## Novos erros de negócio (409, `PUT /{id}`)

- `O novo total de aulas do pacote nao pode ser menor que as aulas ja dadas ({dado}).`
- `O novo teto de aulas da mensalidade nao pode ser menor que as aulas ja dadas neste mes ({dado}).`

## Não faz parte do contrato

Nenhuma tela nova consome esses campos nesta feature (FR-013) — ficam disponíveis no contrato para a feature de visualização futura. `aulasDadas`/`aulasDadasNoMes` nunca são aceitos em request (somente calculados na resposta).
