# Contrato: `sync-card.ps1` (interface de linha de comando)

```powershell
.specify/hooks/trello/sync-card.ps1 -Fase <backlog|design|a-fazer|em-andamento|revisao-codigo> [-DryRun]
```

## Parâmetros

| Parâmetro | Obrigatório | Valores | Efeito |
|---|---|---|---|
| `-Fase` | Sim | `backlog`, `design`, `a-fazer`, `em-andamento`, `revisao-codigo` | Determina a ação (criar, ou mover para a lista correspondente) — ver [../data-model.md](../data-model.md), "Efeito por fase". |
| `-DryRun` | Não | switch | Executa toda a lógica de busca/decisão (qual card, qual lista de destino, se a checagem de completude passaria) e imprime o que faria, **sem** chamar nenhum endpoint de escrita do Trello (`POST`/`PUT`). Usado pelo quickstart e para depuração manual (research.md R8). |

## Entradas implícitas (não são parâmetros de linha de comando)

- `.specify/feature.json` — de onde vem `FeatureId` (a feature atual).
- Variáveis de ambiente `TRELLO_API_KEY`, `TRELLO_TOKEN`, `TRELLO_BOARD_ID` (ou `.specify/hooks/trello/.env` equivalente, research.md R9).
- `specs/<feature>/spec.md` (só na fase `backlog`, para o resumo da descrição do card).
- `specs/<feature>/tasks.md` (só na fase `revisao-codigo`, para a checagem de completude).

## Saída

- **Código de saída**: sempre `0`, em qualquer cenário (sucesso, falha de rede, credencial inválida, card/quadro/lista não encontrados) — contrato central de FR-011 (research.md R6). O script nunca é o motivo de uma falha percebida pelo comando do Spec Kit que o invocou.
- **stdout**: uma linha por execução resumindo o resultado (ex.: `[trello-sync] backlog: card criado (037-vinculo-cobranca)`, `[trello-sync] revisao-codigo: NAO movido -- 2 tarefas pendentes, testes ok`) — a skill `trello-sync-card` pode repassar essa linha ao usuário como uma nota breve, mas nunca a trata como sinal de erro do comando do Spec Kit.
- **Log** (`.specify/hooks/trello/sync.log`, gitignored): toda execução grava uma entrada com timestamp, fase, resultado e (quando houve falha) o erro completo — é o único lugar com detalhe suficiente para diagnosticar um problema depois.

## Sem contrato de API HTTP próprio

Esta feature não expõe nenhum endpoint do SPI (`src/SPI.Api` inalterado) — o único "contrato" externo é o consumo da API REST pública do Trello (autenticação, endpoints de card/lista/board/comentário), documentado por referência em [trello-api.md](./trello-api.md).
