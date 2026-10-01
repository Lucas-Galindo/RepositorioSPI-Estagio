# Contrato: `sync-card.ps1` — delta sobre [specs/040-trello-sync-hooks/contracts/sync-card-cli.md](../../040-trello-sync-hooks/contracts/sync-card-cli.md)

Esta feature **não substitui** o contrato da spec 040 — só acrescenta. Tudo que já valia (código de saída sempre `0`, log em `sync.log`, autenticação via `.env`) continua igual.

```powershell
.specify/hooks/trello/sync-card.ps1 -Fase <backlog|design|a-fazer|em-andamento|revisao-codigo|analyze|clarify|retroativo> [-Detalhe <texto>] [-DryRun]
```

## Parâmetros novos

| Parâmetro | Obrigatório | Valores | Efeito |
|---|---|---|---|
| `-Fase analyze` | — | — | Reescreve só o `STATUS:` do card da feature atual (lido de `.specify/feature.json`, igual às demais fases). **Nunca** move de lista. |
| `-Fase clarify` | — | — | Idem `analyze`. |
| `-Fase retroativo` | — | — | Varre **todos** os cards do quadro; para cada um sem o marcador `Feature: <id>` e sem `STATUS:` na 1ª linha, insere `STATUS: card criado manualmente, sem spec formal ainda`. Não lê `.specify/feature.json` (não é sobre a feature atual). Sem hook associado — roda manualmente, uma vez. |
| `-Detalhe <texto>` | Não | string, uma linha | Texto que vira o `<detalhe>` da linha `STATUS:` (ver [status-line-convention.md](./status-line-convention.md)). Quando omitido, o script deriva um texto padrão por fase (research.md R4). Ignorado na fase `retroativo` (detalhe é sempre o texto fixo do FR-009). |

## Fases já existentes (`backlog`, `design`, `a-fazer`, `em-andamento`, `revisao-codigo`)

Comportamento de mover/criar card **inalterado** (specs/040). Adicionalmente, toda vez que o card é encontrado (ou criado), sua descrição passa a ser reescrita com `STATUS:` na frente, seguindo [status-line-convention.md](./status-line-convention.md). `-Detalhe` funciona nessas fases também, se o agente quiser sobrepor o texto derivado.

## Saída (aditivo)

- `sync.log`: cada entrada de fase `analyze`/`clarify`/`retroativo` segue o mesmo formato de timestamp/fase/resultado/detalhe já usado.
- stdout: uma linha por execução, no mesmo padrão (`[trello-sync] analyze: STATUS atualizado (037-vinculo-cobranca)`, `[trello-sync] retroativo: 12 cards atualizados, 0 ja tinham STATUS`).
