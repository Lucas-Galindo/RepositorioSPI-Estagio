---
name: "trello-sync-card"
description: "Sincroniza o card da feature atual no quadro Trello 'Estagio SPI' para a fase indicada (backlog, design, a-fazer, em-andamento, revisao-codigo, analyze, clarify, retroativo). Nunca falha visivelmente -- e' um extra sobre o fluxo real do Spec Kit."
argument-hint: "Fase[: detalhe] -- ex.: 'backlog', ou 'analyze: 2 achados aguardando decisão'"
compatibility: "Requires spec-kit project structure with .specify/ directory; PowerShell 5.1+"
metadata:
  source: "specs/040-trello-sync-hooks, specs/042-trello-status-marker"
user-invocable: true
disable-model-invocation: false
---

## User Input

```text
$ARGUMENTS
```

O texto acima segue o formato `<fase>[: <detalhe>]` — a fase (`prompt` da entrada de hook correspondente em `.specify/extensions.yml`; ver [contracts/extensions-yml.md](../../../specs/040-trello-sync-hooks/contracts/extensions-yml.md) e [contracts/extensions-yml-delta.md](../../../specs/042-trello-status-marker/contracts/extensions-yml-delta.md)) é sempre um de `backlog`, `design`, `a-fazer`, `em-andamento`, `revisao-codigo`, `analyze`, `clarify`, `retroativo`; o `: <detalhe>` opcional depois dela é um texto curto para a linha `STATUS:` do card (ver "Detalhe opcional" abaixo).

## O que esta skill faz

Esta skill é uma orquestração fina, sem lógica de negócio própria (a lógica real vive em `.specify/hooks/trello/sync-card.ps1`, documentada em [contracts/sync-card-cli.md](../../../specs/040-trello-sync-hooks/contracts/sync-card-cli.md) e [contracts/sync-card-cli-delta.md](../../../specs/042-trello-status-marker/contracts/sync-card-cli-delta.md)):

1. Resolve o caminho absoluto de `.specify/hooks/trello/sync-card.ps1` a partir da raiz do repositório atual.
2. Separa `$ARGUMENTS` em `<fase>` e, se houver `: <detalhe>`, no texto que vem depois dos dois-pontos (tudo antes do primeiro `:` é a fase; tudo depois é o detalhe, sem os dois-pontos).
3. Invoca o script passando `-Fase <fase>` (e `-Detalhe <detalhe>` quando presente, e `-DryRun` se o argumento pedir explicitamente um dry-run).
4. Captura a única linha de stdout que o script imprime (ex.: `[trello-sync] backlog: card criado (037-vinculo-cobranca)`) e a repassa ao usuário como uma nota breve.

## Detalhe opcional (`: <detalhe>`) -- quando incluir

As fases `analyze` e `clarify` disparam logo depois de `/speckit-analyze`/`/speckit-clarify` terminarem, num momento em que só quem acabou de rodar esses comandos (você, o agente) sabe de algo relevante que aconteceu naquela execução específica — quantos achados ficaram aguardando decisão, quantas perguntas foram respondidas. Isso nunca está disponível para o script (que só lê arquivos em disco), então:

- Se você tiver esse detalhe da conversa, inclua-o: `analyze: 2 achados aguardando decisão`, `clarify: 2 perguntas respondidas`.
- Se não tiver (ou for qualquer outra fase), invoque só com a fase: `analyze`, `design`, `revisao-codigo`. O script deriva um texto padrão a partir do estado dos arquivos (`specs/040-trello-sync-hooks/research.md` R4).

O `: <detalhe>` funciona em qualquer fase, não só `analyze`/`clarify` — mas normalmente só faz sentido nessas duas, já que as demais já têm um detalhe derivado suficiente (progresso de `tasks.md`, resultado do bloqueio de `revisao-codigo`).

## Regra central: nunca é um erro do comando que a invocou

O contrato de `sync-card.ps1` garante que o script **sempre** termina com código de saída `0`, mesmo quando a sincronização com o Trello falhou de verdade (Trello fora do ar, credencial inválida, card ou lista não encontrados) -- qualquer detalhe de falha fica só em `.specify/hooks/trello/sync.log` (FR-011 de [spec.md](../../../specs/040-trello-sync-hooks/spec.md)).

Por isso, esta skill:
- **NUNCA** trata a saída desta invocação como uma falha do hook `before_specify`/`after_specify`/`after_plan`/`after_tasks`/`before_implement`/`after_implement` que a chamou.
- **NUNCA** interrompe ou atrasa de forma perceptível o comando `/speckit-*` que disparou o hook.
- Se, por algum motivo excepcional, a própria invocação do script falhar antes de rodar (ex.: arquivo `sync-card.ps1` ausente ou PowerShell indisponível), esta skill apenas registra uma nota breve ao usuário e segue -- nunca propaga isso como erro do comando do Spec Kit.

## Execução

Rodar (a partir da raiz do repositório):

```powershell
.specify/hooks/trello/sync-card.ps1 -Fase <fase> [-Detalhe <detalhe>]
```

Repassar `-DryRun` somente se o argumento recebido pedir explicitamente um dry-run (uso normal dos hooks nunca pede). A fase `retroativo` não é disparada por nenhum hook — só roda quando alguém invoca esta skill manualmente com `retroativo` para a atualização em lote de FR-009 (specs/042).
