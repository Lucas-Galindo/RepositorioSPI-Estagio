# Contrato: link da aula (spec 067)

Autenticação e perfil (`Professor`) como no resto de `AulasController`. Os 409/404 saem pelo `ErrosDeNegocioFilter` (spec 065), por isso cada ação declara `ProducesResponseType`.

## `PUT /api/aulas/{id}/link` — informar ou trocar o link

**Request**

```json
{ "link": "https://meet.google.com/abc-defg-hij" }
```

**Validação (400, lista de mensagens)**: link obrigatório; endereço absoluto `http://` ou `https://`; sem espaços; no máximo 500 caracteres.

| Resposta | Quando |
|---|---|
| 200 `AulaResponse` | link guardado em memória; `linkAula` = o link, `aceitaLinkAula = true` |
| 400 | validação acima |
| 404 `Aula nao encontrada.` | aula inexistente ou excluída |
| 409 `Esta aula nao aceita link: ja terminou ou foi cancelada.` | aula cancelada ou com o fim já passado |

## `DELETE /api/aulas/{id}/link` — remover o link

| Resposta | Quando |
|---|---|
| 200 `AulaResponse` | link removido (também se não havia link: idempotente); `linkAula = null` |
| 404 `Aula nao encontrada.` | aula inexistente ou excluída |

## `AulaResponse` (todas as respostas que devolvem aula)

Campos novos, calculados pelo servidor:

| Campo | Tipo | Significado |
|---|---|---|
| `linkAula` | string ou `null` | link vigente da aula; `null` se não há, expirou, a aula foi cancelada/excluída ou a API reiniciou |
| `aceitaLinkAula` | boolean | `true` se a aula está ativa, não cancelada e ainda não terminou |

Vale para `GET /api/aulas`, `GET /api/aulas/{id}`, `POST`, `PUT`, registrar sessão, cancelar e participantes.

## E-mail do lembrete de aula

Sem mudança de API. Quando a próxima aula da turma tem link vigente no momento do envio, o corpo inclui:

```html
<p>Link da aula: <a href="https://meet.google.com/abc-defg-hij">https://meet.google.com/abc-defg-hij</a></p>
```

(texto e `href` com HTML protegido). Sem link, o corpo é exatamente o atual.

## O que não muda

Banco de dados, `POST/PUT /api/aulas`, regra de envio dos lembretes e demais respostas.
