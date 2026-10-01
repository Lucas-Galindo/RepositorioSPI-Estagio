# Quickstart: validar o marcador de STATUS

Guia de validação ponta a ponta. Contratos em [contracts/](./contracts/); modelo em [data-model.md](./data-model.md). Pré-requisitos e credenciais: mesmos da spec 040 (`.specify/hooks/trello/.env`, quadro "Estágio SPI" real).

## 1. Reescrita de STATUS nas fases já existentes (US1)

Para uma feature de teste com card já criado (backlog):

| # | Passo | Esperado |
|---|---|---|
| 1 | `sync-card.ps1 -Fase design` | Card move para "Design" **e** a 1ª linha da descrição passa a ser `STATUS: /speckit-plan concluído` (ou o detalhe repassado) |
| 2 | `sync-card.ps1 -Fase a-fazer` | Move para "A Fazer"; `STATUS:` reflete `/speckit-tasks concluído, N tarefas geradas` |
| 3 | `sync-card.ps1 -Fase em-andamento` | Move para "Em andamento"; `STATUS:` reflete `/speckit-implement rodando, X/N tarefas concluídas` |
| 4 | Repetir o passo 3 | `STATUS:` é **substituído** (nunca duplicado — conferir só 1 linha começando com `STATUS:` na descrição) |
| 5 | Conferir o resumo do spec e o marcador `Feature: <id>` (última linha) | Permanecem exatamente como antes, só a 1ª linha mudou |

## 2. `analyze`/`clarify` atualizam o STATUS sem mover o card (US2)

| # | Passo | Esperado |
|---|---|---|
| 1 | Anotar a lista atual do card (ex.: "Em andamento") | — |
| 2 | `sync-card.ps1 -Fase clarify -Detalhe "2 perguntas respondidas"` | `STATUS: 2 perguntas respondidas`; card **continua** na mesma lista |
| 3 | `sync-card.ps1 -Fase analyze -Detalhe "2 achados aguardando decisão"` | `STATUS: 2 achados aguardando decisão`; card continua na mesma lista |
| 4 | `sync-card.ps1 -Fase analyze` (sem `-Detalhe`) | `STATUS: /speckit-analyze concluído` (fallback derivado) |

## 3. Card sem STATUS ganha um ao ser tocado (US3)

| # | Passo | Esperado |
|---|---|---|
| 1 | Editar manualmente, via Trello, a descrição de um card de feature de teste removendo a linha `STATUS:` (deixar só o resumo + `Feature: <id>`) | — |
| 2 | Rodar qualquer fase dessa feature (ex.: `design`) | A descrição volta a ter `STATUS:` na frente, com o resumo e o marcador intactos |

## 4. Atualização retroativa dos cards manuais (US4)

| # | Passo | Esperado |
|---|---|---|
| 1 | `sync-card.ps1 -Fase retroativo` | Todo card **sem** marcador `Feature:` e **sem** `STATUS:` ganha `STATUS: card criado manualmente, sem spec formal ainda`; cards de feature (com marcador) não são tocados |
| 2 | Rodar de novo | Nenhum card muda (idempotente) |

## 5. Falha nunca trava o Spec Kit (US5)

```powershell
$env:TRELLO_TOKEN = "invalido-de-proposito"
.specify/hooks/trello/sync-card.ps1 -Fase clarify
echo "codigo de saida: $LASTEXITCODE"
```

Esperado: `codigo de saida: 0`; erro registrado em `sync.log`; rodar `/speckit-clarify` de verdade em seguida continua funcionando normalmente.

## 6. Não-regressão (FR-012)

- Repetir os cenários de `quickstart.md` da spec 040 (criação, movimentação, comentário em Revisão de código) — devem continuar funcionando, agora só com o `STATUS:` a mais na descrição.
- `git diff --stat -- src frontend database` continua vazio.
