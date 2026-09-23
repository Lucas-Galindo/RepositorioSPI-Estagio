---
name: "trello-sync-card"
description: "Sincroniza o card da feature atual no quadro Trello 'Estagio SPI' para a fase indicada (backlog, design, a-fazer, em-andamento, revisao-codigo). Nunca falha visivelmente -- e' um extra sobre o fluxo real do Spec Kit."
argument-hint: "Fase: backlog | design | a-fazer | em-andamento | revisao-codigo"
compatibility: "Requires spec-kit project structure with .specify/ directory; PowerShell 5.1+"
metadata:
  source: "specs/040-trello-sync-hooks"
user-invocable: true
disable-model-invocation: false
---

## User Input

```text
$ARGUMENTS
```

O texto acima **é** a fase (`prompt` da entrada de hook correspondente em `.specify/extensions.yml` -- ver [contracts/extensions-yml.md](../../../specs/040-trello-sync-hooks/contracts/extensions-yml.md)): um de `backlog`, `design`, `a-fazer`, `em-andamento`, `revisao-codigo`.

## O que esta skill faz

Esta skill é uma orquestração fina, sem lógica de negócio própria (a lógica real vive em `.specify/hooks/trello/sync-card.ps1`, documentada em [contracts/sync-card-cli.md](../../../specs/040-trello-sync-hooks/contracts/sync-card-cli.md)):

1. Resolve o caminho absoluto de `.specify/hooks/trello/sync-card.ps1` a partir da raiz do repositório atual.
2. Invoca esse script passando `-Fase <fase recebida em $ARGUMENTS>` (e `-DryRun` se o argumento pedir explicitamente um dry-run).
3. Captura a única linha de stdout que o script imprime (ex.: `[trello-sync] backlog: card criado (037-vinculo-cobranca)`) e a repassa ao usuário como uma nota breve.

## Regra central: nunca é um erro do comando que a invocou

O contrato de `sync-card.ps1` garante que o script **sempre** termina com código de saída `0`, mesmo quando a sincronização com o Trello falhou de verdade (Trello fora do ar, credencial inválida, card ou lista não encontrados) -- qualquer detalhe de falha fica só em `.specify/hooks/trello/sync.log` (FR-011 de [spec.md](../../../specs/040-trello-sync-hooks/spec.md)).

Por isso, esta skill:
- **NUNCA** trata a saída desta invocação como uma falha do hook `before_specify`/`after_specify`/`after_plan`/`after_tasks`/`before_implement`/`after_implement` que a chamou.
- **NUNCA** interrompe ou atrasa de forma perceptível o comando `/speckit-*` que disparou o hook.
- Se, por algum motivo excepcional, a própria invocação do script falhar antes de rodar (ex.: arquivo `sync-card.ps1` ausente ou PowerShell indisponível), esta skill apenas registra uma nota breve ao usuário e segue -- nunca propaga isso como erro do comando do Spec Kit.

## Execução

Rodar (a partir da raiz do repositório):

```powershell
.specify/hooks/trello/sync-card.ps1 -Fase <fase>
```

Repassar `-DryRun` somente se o argumento recebido pedir explicitamente um dry-run (uso normal dos hooks nunca pede).
