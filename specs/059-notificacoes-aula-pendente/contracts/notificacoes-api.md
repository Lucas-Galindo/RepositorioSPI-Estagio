# Contrato: API de notificações (spec 059)

Todos os endpoints exigem `[Authorize(Roles = "Professor")]`. A professora é a do token (claim `sub`).

## `GET /api/notificacoes`

Primeiro sincroniza as notificações de aula pendente da professora (research.md R3). Depois devolve as **ativas** (não resolvidas), em ordem decrescente de `criadaEm`.

- **200 OK**:

```json
[
  {
    "id": 12,
    "tipo": "AulaPendente",
    "mensagem": "Você ainda não atualizou o status desta aula.",
    "criadaEm": "2026-10-19T11:05:00",
    "lida": false,
    "aula": {
      "id": 40,
      "data": "2026-10-19",
      "horaInicio": "10:00:00",
      "horaFim": "11:00:00",
      "materiaNome": "Matemática",
      "contexto": "Turma A"
    }
  }
]
```

## `PATCH /api/notificacoes/{id}/lida`

Marca a notificação como lida. É idempotente: se ela já estava lida, a data da leitura não muda.

- **204 No Content**.
- **404**: a notificação não existe ou é de outra professora.
