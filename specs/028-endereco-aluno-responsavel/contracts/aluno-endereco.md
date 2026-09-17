# Contrato: Campos de Endereço nos Endpoints de Aluno

Nenhum endpoint novo — os 15 campos de endereço (ver data-model.md) são adicionados aos
endpoints de Aluno já existentes.

## `POST /api/alunos` (Cadastrar) e `PUT /api/alunos/{id}` (Atualizar)

**Request** — 15 campos novos no corpo (todos opcionais no payload exceto as regras abaixo):

```json
{
  "...campos já existentes...": "...",
  "cep": "01001000",
  "rua": "Praça da Sé",
  "numero": "100",
  "complemento": "Apto 12",
  "bairro": "Sé",
  "cidade": "São Paulo",
  "estado": "SP",
  "ehMenorDeIdade": true,
  "responsavelMesmoEndereco": true,
  "responsavelCep": null,
  "responsavelRua": null,
  "responsavelNumero": null,
  "responsavelComplemento": null,
  "responsavelBairro": null,
  "responsavelCidade": null,
  "responsavelEstado": null
}
```

**Regras de validação** (400 Bad Request se violadas) — **assimétricas entre `POST` e `PUT`**,
ver data-model.md para o detalhe completo:

- **`POST /api/alunos`** (Aluno novo): `cep`, `rua`, `numero` obrigatórios sempre. `cep` deve
  ter exatamente 8 dígitos numéricos (sem hífen). Quando `ehMenorDeIdade == true` e
  `responsavelMesmoEndereco == false`: `responsavelCep`, `responsavelRua`, `responsavelNumero`
  também obrigatórios, com a mesma regra de 8 dígitos para `responsavelCep`.
- **`PUT /api/alunos/{id}`** (Aluno existente): `cep`/`rua`/`numero` só se tornam obrigatórios
  **juntos** se **pelo menos um** vier preenchido no corpo da requisição — enviar os três como
  `null`/ausentes é válido e preserva o endereço atual (ou a ausência dele, para Alunos
  cadastrados antes desta funcionalidade). O mesmo vale para
  `responsavelCep`/`responsavelRua`/`responsavelNumero` quando `ehMenorDeIdade == true` e
  `responsavelMesmoEndereco == false`.
- Em ambos: quando `responsavelMesmoEndereco == true`, os campos `responsavel*` do payload são
  ignorados (o backend calcula o espelho a partir do endereço do Aluno).

**Sucesso — 200/201**: `AlunoResponse` (ver abaixo).

## `GET /api/alunos/{id}` e `GET /api/alunos` (Obter/Listar)

**Response** (`AlunoResponse`) — mesmos 15 campos:

```json
{
  "...campos já existentes...": "...",
  "cep": "01001000",
  "rua": "Praça da Sé",
  "numero": "100",
  "complemento": "Apto 12",
  "bairro": "Sé",
  "cidade": "São Paulo",
  "estado": "SP",
  "responsavelMesmoEndereco": true,
  "responsavelCep": "01001000",
  "responsavelRua": "Praça da Sé",
  "responsavelNumero": "100",
  "responsavelComplemento": "Apto 12",
  "responsavelBairro": "Sé",
  "responsavelCidade": "São Paulo",
  "responsavelEstado": "SP"
}
```

Quando `responsavelMesmoEndereco == true`, os 7 campos `responsavel*` sempre vêm iguais aos
campos correspondentes do Aluno (espelhados no momento da leitura — ver data-model.md).

`ehMenorDeIdade` **não aparece no response** — continua sendo só um campo de request (ver
research.md Decisão 1). O frontend infere o estado a partir da presença de
`telefoneResponsavel`/`emailResponsavel`/campos `responsavel*`.

## Integração externa: ViaCEP (client-side, sem endpoint próprio)

- **Chamada**: `GET https://viacep.com.br/ws/{cep}/json/` (8 dígitos, sem hífen), disparada pelo
  frontend diretamente do navegador quando o campo CEP atinge 8 dígitos — não passa pelo backend
  do SPI.
- **Resposta de sucesso** (campos relevantes): `logradouro`, `bairro`, `localidade` (→ Cidade),
  `uf` (→ Estado). Usados para preencher Rua/Bairro/Cidade/Estado automaticamente.
- **Resposta de CEP inexistente**: `{"erro": true}` — tratada como falha silenciosa (FR-012):
  nenhum campo é preenchido, nenhum erro bloqueante é exibido.
- **Falha de rede/timeout**: mesma tratativa — falha silenciosa, campos continuam editáveis
  manualmente.
- Aplicada de forma independente ao campo CEP do Aluno e ao campo CEP do responsável (quando
  exibido).
