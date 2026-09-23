# Contrato: `.specify/extensions.yml`

Formato exato das 5 entradas que esta feature registra. Todas `optional: false` (mandatórias — é o único jeito documentado de disparo automático, sem intervenção manual; research.md R2). Nenhuma usa `condition` (research.md R3 / Constitution Check, Princípio III).

```yaml
hooks:
  after_specify:
    - extension: "trello"
      command: "trello.sync-card"
      description: "Cria o card da feature no Backlog do quadro Estágio SPI."
      prompt: "backlog"
      optional: false

  after_plan:
    - extension: "trello"
      command: "trello.sync-card"
      description: "Move o card da feature para Design."
      prompt: "design"
      optional: false

  after_tasks:
    - extension: "trello"
      command: "trello.sync-card"
      description: "Move o card da feature para A Fazer."
      prompt: "a-fazer"
      optional: false

  before_implement:
    - extension: "trello"
      command: "trello.sync-card"
      description: "Move o card da feature para Em andamento."
      prompt: "em-andamento"
      optional: false

  after_implement:
    - extension: "trello"
      command: "trello.sync-card"
      description: "Move o card da feature para Revisão de código, se tudo (não-manual) estiver concluído e os testes passarem; comenta um resumo."
      prompt: "revisao-codigo"
      optional: false
```

- `command: "trello.sync-card"` (com pontos) → convertido para `/trello-sync-card` no momento da invocação, seguindo a mesma regra de conversão já usada por todas as skills `speckit-*` (`speckit.git.commit` → `/speckit-git-commit`).
- `prompt` carrega a fase; é isso que diferencia as 5 entradas — o comando invocado é sempre o mesmo (research.md R2).
- Nenhuma entrada usa `condition`: qualquer necessidade de lógica condicional (a mais notável: FR-007a) vive dentro de `sync-card.ps1`, nunca declarada aqui.
