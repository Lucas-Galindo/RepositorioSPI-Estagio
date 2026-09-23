# Contrato: consumo da API REST do Trello

Referência dos endpoints que `sync-card.ps1` consome. Autenticação em toda chamada via query string `?key={TRELLO_API_KEY}&token={TRELLO_TOKEN}` (padrão da API pública do Trello — nunca em header customizado, nunca logado).

| Uso | Método + Endpoint | Campos relevantes |
|---|---|---|
| Listar as listas do quadro (research.md R5) | `GET /1/boards/{TRELLO_BOARD_ID}/lists` | `id`, `name` — casado por nome exato contra as 5 listas automáticas |
| Listar os cards do quadro, para localizar o card da feature (research.md R4) | `GET /1/boards/{TRELLO_BOARD_ID}/cards?fields=name,desc,idList` | `id`, `name`, `desc`, `idList` — filtrado client-side pela última linha `Feature: <id>` na `desc` |
| Criar o card (fase `backlog`) | `POST /1/cards` | `idList` (id da lista "Backlog"), `name` (título da feature), `desc` (resumo + linha `Feature: <id>`) |
| Mover o card (fases `design`, `a-fazer`, `em-andamento`, `revisao-codigo`) | `PUT /1/cards/{id}` | `idList` (id da lista de destino) |
| Comentar no card (só na fase `revisao-codigo`, quando move) | `POST /1/cards/{id}/actions/comments` | `text` (resumo do que foi implementado) |

## Falhas tratadas (todas resultam em log + `exit 0`, nunca em exceção propagada — FR-011)

- Timeout/indisponibilidade de rede.
- `401`/`403` (API Key ou Token inválidos/expirados).
- `404` em qualquer um dos 4 endpoints (quadro, lista, card não encontrados).
- Qualquer outro código de erro HTTP ou corpo de resposta inesperado.

## Fora do escopo (reafirmando FR-009/FR-010)

- Nenhuma chamada a endpoints de leitura usada para trazer dado de volta ao Spec Kit (o `GET /1/boards/.../cards` existe só para localizar o card a mover/comentar dentro da própria execução, nunca para alimentar uma decisão do Spec Kit).
- Nenhuma chamada que mova um card para as listas "Fase de teste"/"Concluído" — os ids dessas duas listas nunca são resolvidos pelo script.
