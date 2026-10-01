# Contrato: `.specify/extensions.yml` — delta sobre [specs/040-trello-sync-hooks/contracts/extensions-yml.md](../../040-trello-sync-hooks/contracts/extensions-yml.md)

As 5 entradas existentes (`after_specify`, `after_plan`, `after_tasks`, `before_implement`, `after_implement`) **não mudam**. Duas entradas novas:

```yaml
hooks:
  # ...5 entradas existentes, inalteradas...

  after_analyze:
    - extension: "trello"
      command: "trello.sync-card"
      description: "Atualiza o STATUS do card com o resultado do /speckit-analyze (nao move de lista)."
      prompt: "analyze"
      optional: false

  after_clarify:
    - extension: "trello"
      command: "trello.sync-card"
      description: "Atualiza o STATUS do card com o resultado do /speckit-clarify (nao move de lista)."
      prompt: "clarify"
      optional: false
```

- Mesmas regras de sempre: `optional: false` (mandatório), nenhuma usa `condition` (research.md R2 desta feature confirma que o mecanismo já suporta esses dois eventos; nada aqui precisa de lógica condicional).
- `prompt` continua sendo só o nome da fase (`"analyze"`/`"clarify"`) — é o piso mínimo. Quando o agente que dispara o hook tiver um detalhe da conversa para repassar, ele invoca a skill com `"analyze: <detalhe>"` em vez do `prompt` literal (research.md R3) — isso é uma decisão de invocação, não algo declarado no YAML.
- `-Fase retroativo` **não** tem entrada em `extensions.yml` — não é acionada por nenhum comando `/speckit-*`, é rodada manualmente uma única vez (FR-009).
