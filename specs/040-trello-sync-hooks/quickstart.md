# Quickstart: validar a sincronização Spec Kit → Trello

Guia de validação ponta a ponta. Contratos em [contracts/](./contracts/); modelo em [data-model.md](./data-model.md).

## Pré-requisitos

- Um quadro Trello chamado "Estágio SPI" com as 7 listas exatas: Backlog, Design, A Fazer, Em andamento, Revisão de código, Fase de teste, Concluído (Assumptions do spec.md — esta feature não cria o quadro).
- `TRELLO_API_KEY`, `TRELLO_TOKEN`, `TRELLO_BOARD_ID` configurados (variável de ambiente real, ou `.specify/hooks/trello/.env` a partir de `.env.example`) — ver `.specify/hooks/trello/README.md`.
- Uma feature de teste (pode ser fictícia, especificada só para este teste, para não sujar o quadro real) para percorrer o ciclo completo.

## 1. Validação sem tocar o Trello de verdade (`-DryRun`)

```powershell
.specify/hooks/trello/sync-card.ps1 -Fase backlog -DryRun
```

Esperado: uma linha de saída indicando o que o script faria (criar/mover), sem nenhuma chamada de escrita real — confirma que a lógica de leitura de `.specify/feature.json`/`spec.md`/credenciais está correta antes de tocar o quadro real.

## 2. Ciclo completo, contra um quadro de teste real

| # | Passo | Resultado esperado |
|---|---|---|
| 1 | `/speckit-specify` de uma feature de teste | Um novo card aparece em "Backlog", com o título da feature e a descrição terminando em `Feature: <id-da-feature>` (FR-001/FR-002) |
| 2 | Rodar `/speckit-specify` de novo para a mesma feature (ex.: editar o spec) | Nenhum card novo aparece — continua havendo só 1 card para essa feature (FR-003, SC-002) |
| 3 | `/speckit-plan` | O card se move de "Backlog" para "Design" (FR-004) |
| 4 | `/speckit-tasks` | O card se move para "A Fazer" (FR-005) |
| 5 | `/speckit-implement` (início) | O card se move para "Em andamento" (FR-006) |
| 6 | `/speckit-implement` (fim), com todas as tarefas não-manuais concluídas e a suíte de testes passando | O card se move para "Revisão de código" **e** recebe um novo comentário resumindo o trabalho (FR-007/FR-008) |
| 7 | Repetir o passo 6 num cenário com alguma tarefa não-manual ainda incompleta (ou testes falhando) | O card **permanece** em "Em andamento" — não se move (FR-007a) |

## 3. Falha do Trello não trava o Spec Kit (FR-011, SC-003)

```powershell
$env:TRELLO_TOKEN = "invalido-de-proposito"
.specify/hooks/trello/sync-card.ps1 -Fase backlog
echo "codigo de saida: $LASTEXITCODE"
```

Esperado: `codigo de saida: 0` mesmo com o token inválido; uma entrada de erro aparece em `.specify/hooks/trello/sync.log`. Depois, rodar `/speckit-specify` normalmente (com a credencial inválida ainda ativa) e confirmar que o comando gera o `spec.md` normalmente, sem nenhuma mensagem de erro do Spec Kit em si.

## 4. Segurança (SC-005)

```powershell
git -C "C:\PROJETO - SPI" status --short -- .specify/hooks/trello/.env .specify/hooks/trello/sync.log
```

Esperado: vazio ou `??` (não rastreado) — nunca `A`/`M` (nunca preparado para commit). Conferir também que `.specify/hooks/trello/sync-card.ps1` não contém nenhum valor literal de API Key/Token.

## 5. Não-regressão

```powershell
git -C "C:\PROJETO - SPI" diff --stat -- src frontend database
```

Esperado: vazio — esta feature não toca em nenhum código do produto SPI (FR-013).
