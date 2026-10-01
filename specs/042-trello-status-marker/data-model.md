# Data Model: Marcador de STATUS nos Cards do Trello

Nenhuma entidade de domínio do SPI envolvida (FR-013) — mesmo modelo puramente externo (Trello) da spec 040. Este documento estende [specs/040-trello-sync-hooks/data-model.md](../040-trello-sync-hooks/data-model.md) com a nova convenção de descrição.

## Estrutura da descrição de um card (nova convenção)

```text
STATUS: <detalhe>

<corpo>
```

| Parte | Origem/Regra |
|---|---|
| `STATUS: <detalhe>` | Sempre a primeira linha. Reescrita em toda fase de escrita (existente ou nova). Nunca duplicada (FR-006). |
| (linha em branco) | Presente só quando `<corpo>` não é vazio. |
| `<corpo>` | Tudo que a descrição já tinha antes desta feature: para card de feature, o resumo do spec + a última linha `Feature: <id>` (specs/040, inalterado — FR-005); para card manual, o texto original da professora, palavra por palavra. |

Quando a descrição de entrada já **não** tem `STATUS:` na primeira linha, `<corpo>` = a descrição inteira (nada é perdido); quando já tem, `<corpo>` = tudo exceto essa primeira linha (e a linha em branco seguinte, se houver).

## `<detalhe>`: duas origens possíveis (FR-008)

| Origem | Quando | Exemplo |
|---|---|---|
| Repassado pelo agente (`-Detalhe`) | O comando que disparou o hook tinha algo só disponível na conversa | `"2 achados aguardando decisão"` |
| Derivado dos arquivos (padrão por fase, research.md R4) | Nenhum `-Detalhe` informado | `"/speckit-tasks concluído, 31 tarefas geradas"` |

## Efeito por fase (estende specs/040-trello-sync-hooks/data-model.md "Efeito por fase")

| Fase | Card não existe | Card existe |
|---|---|---|
| `backlog` | Cria com `STATUS: /speckit-specify concluído` (ou `-Detalhe`) + resumo + marcador (FR-001) | Idempotente (specs/040) — `STATUS:` **não** é reescrito num card já existente nesta fase (a criação só acontece uma vez) |
| `design`, `a-fazer`, `em-andamento` | Log "card não encontrado", nada feito (specs/040) | Move de lista (specs/040) **e** reescreve `STATUS:` com o detalhe da fase |
| `revisao-codigo` | idem | Se mover: reescreve `STATUS:` + comenta (specs/040); se não mover: reescreve `STATUS:` com o motivo do bloqueio, sem mover |
| `analyze`, `clarify` (novas) | Log "card não encontrado", nada feito | Reescreve só o `STATUS:` — **nunca** move de lista (FR-004) |
| `retroativo` (nova, sem hook) | N/A (varre todos os cards do quadro) | Para cada card **sem** o marcador `Feature: <id>` e **sem** `STATUS:` na 1ª linha: insere `STATUS: card criado manualmente, sem spec formal ainda` (FR-009). Cards que já têm `STATUS:` (de qualquer origem) não são tocados (idempotente). |

## Estado em memória de uma execução (novos campos, além dos já listados em specs/040)

| Campo | Descrição |
|---|---|
| `Detalhe` (opcional) | Texto repassado via `-Detalhe`; quando presente, sempre vence o derivado dos arquivos. |
| `CorpoAtual` | Resultado de `Extrair-CorpoSemStatus` sobre a descrição atual do card encontrado. |
| `DetalheDerivado` (só quando `Detalhe` está vazio) | Resultado de `Obter-StatusDetalhePadrao`, function of `Fase` + estado de `tasks.md` (contagem total/concluídas, IDs de tarefas manuais pendentes). |

## Validações (dono único: `sync-card.ps1`, mesmo padrão da spec 040)

| Regra | Onde |
|---|---|
| `STATUS:` nunca duplicado, mesmo em reescritas repetidas (FR-006) | `Extrair-CorpoSemStatus` + `Montar-DescricaoComStatus` |
| `analyze`/`clarify` nunca movem o card de lista (FR-004) | Branch dedicado, sem `Move-TrelloCard` |
| `retroativo` nunca toca card de feature (com marcador) nem card que já tem `STATUS:` (FR-009) | Filtro duplo antes de escrever |
| Falha ao ler/escrever o STATUS nunca propaga (FR-010) | Mesmo wrapper `try/catch` + `exit 0` global (specs/040) |
| Nenhuma decisão de fluxo do Spec Kit a partir do que é lido do Trello (FR-011) | A leitura serve só para montar `CorpoAtual`; nunca é usada para decidir mover, pular ou repetir um comando `/speckit-*` |
