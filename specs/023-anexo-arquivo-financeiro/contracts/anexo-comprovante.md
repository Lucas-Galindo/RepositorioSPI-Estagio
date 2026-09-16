# Contract: Anexo de Comprovante (Contas a Pagar e Contas a Receber)

Quatro endpoints novos — dois por entidade, mesmo formato para ambas. Nenhum endpoint existente
muda de assinatura, exceto `GET .../{id}` (resposta ganha um campo novo, opcional — ver seção
final).

## `POST /api/contas-pagar/{id}/anexo` e `POST /api/pagamentos/{id}/anexo`

Anexa um arquivo ao registro `{id}`. Usado tanto para o primeiro anexo quanto para substituir um
anexo existente (mesmo endpoint — a confirmação antes de substituir, FR-016, é responsabilidade
do frontend, não da API).

### Request

```
POST /api/contas-pagar/{id}/anexo
Authorization: Bearer {accessToken}
Content-Type: multipart/form-data; boundary=...

------boundary
Content-Disposition: form-data; name="arquivo"; filename="nota-fiscal.pdf"
Content-Type: application/pdf

<bytes do arquivo>
------boundary--
```

| Campo do form | Obrigatório | Regra |
|---|---|---|
| `arquivo` | Sim | Um único arquivo. Rejeitado se: (a) ausente; (b) maior que 10MB; (c) conteúdo real não corresponde a JPG, PNG ou PDF (verificado por assinatura binária, não pela extensão do nome nem pelo `Content-Type` declarado) |

### Response — `200 OK`

```json
{
  "nomeOriginal": "nota-fiscal.pdf",
  "tipoMime": "application/pdf",
  "tamanhoBytes": 482113,
  "dataUpload": "2026-09-16T14:32:00"
}
```

Corpo: `AnexoResponse` — os mesmos metadados descritos em [data-model.md](../data-model.md), sem
o conteúdo binário.

### Response — `400 Bad Request`

Quando o arquivo é rejeitado (FR-004). Corpo: lista de mensagens (mesmo formato já usado pelos
demais endpoints de validação do projeto — `FluentValidation`), por exemplo:

```json
["O arquivo excede o tamanho máximo de 10MB.", "Apenas arquivos JPG, PNG ou PDF são aceitos."]
```

O anexo anterior do registro (se houver) permanece inalterado quando a resposta é `400` (FR-007).

### Response — `404 Not Found`

Quando `{id}` não corresponde a nenhum registro de Contas a Pagar / Contas a Receber existente.

## `GET /api/contas-pagar/{id}/anexo` e `GET /api/pagamentos/{id}/anexo`

Devolve o conteúdo binário do anexo atual do registro `{id}`, para visualização/download
(FR-008, FR-009).

### Request

```
GET /api/contas-pagar/{id}/anexo
Authorization: Bearer {accessToken}
```

### Response — `200 OK`

```
Content-Type: application/pdf
Content-Disposition: inline; filename="nota-fiscal.pdf"

<bytes do arquivo, exatamente como enviados originalmente>
```

`Content-Type` reflete `arquivo_tipo_mime`; `Content-Disposition: inline` permite que o navegador
exiba o arquivo diretamente (imagem ou PDF) numa aba nova quando possível, mantendo a opção de
download disponível pelos controles nativos do navegador — cobre "visualizar" e "baixar" com uma
única resposta (FR-008).

### Response — `404 Not Found`

Quando `{id}` não existe, **ou** quando o registro existe mas não tem nenhum anexo salvo (as duas
situações retornam o mesmo status; o frontend já sabe se há anexo pelos metadados devolvidos em
`GET /api/contas-pagar/{id}` — ver abaixo — então não precisa distinguir os dois casos ao chamar
este endpoint).

## Extensão de `GET /api/contas-pagar/{id}` e `GET /api/pagamentos/{id}` (já existentes)

Mudança aditiva: `ContaPagarResponse` e `PagamentoResponse` ganham um campo novo, opcional:

```json
{
  "id": 42,
  "descricao": "Aluguel da sala",
  "...": "...(demais campos já existentes, inalterados)",
  "anexo": {
    "nomeOriginal": "nota-fiscal.pdf",
    "tipoMime": "application/pdf",
    "tamanhoBytes": 482113,
    "dataUpload": "2026-09-16T14:32:00"
  }
}
```

`anexo` é `null` quando o registro não tem nenhum arquivo anexado. O conteúdo binário **nunca**
aparece neste corpo JSON — apenas os metadados, exatamente o mesmo formato de `AnexoResponse`
devolvido pelo `POST`. `GET /api/contas-pagar` e `GET /api/pagamentos` (as listagens) **não**
ganham este campo — a listagem continua exatamente como está hoje, sem custo adicional de
consulta para todo registro listado (decisão implícita: nenhuma pré-visualização em lista, já
registrada em spec.md Assumptions).

## Compatibilidade

Mudança aditiva em toda a superfície: nenhum endpoint, campo ou comportamento existente é
removido ou alterado, exceto a adição do campo opcional `anexo` (sempre `null` até o primeiro
upload) nas duas respostas de detalhe já existentes.
