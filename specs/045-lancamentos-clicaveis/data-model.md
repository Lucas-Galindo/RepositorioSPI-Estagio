# Data Model: Lançamentos Clicáveis na Tabela de Indicadores

Nenhuma entidade nova, nenhum campo novo. Esta feature só passa a **usar** dois campos que já existem na resposta da API.

## Entidade existente reutilizada: `LancamentoIndicador` (frontend) / `LancamentoIndicadorItem` (backend)

| Campo | Tipo | Já existia? | Uso nesta feature |
|---|---|---|---|
| `id` | `number` | Sim (spec 022) — já usado hoje como parte da `key` da linha (`` `${tipo}-${id}` ``) | Determina o `id` da rota de destino (`/financeiro/contas-a-receber/{id}` ou `/financeiro/contas-a-pagar/{id}`). |
| `tipo` | `"Entrada" \| "Saida"` | Sim (spec 022) | Determina qual rota de detalhe usar: `"Entrada"` → Contas a Receber; `"Saida"` → Contas a Pagar. |
| `descricao`, `dataVencimento`, `valor`, `status` | — | Sim (spec 022) | Inalterados — continuam exibidos exatamente como hoje (FR-007). |

## Fluxo de navegação

| Evento | Efeito |
|---|---|
| Professora clica em qualquer ponto de uma linha da tabela | `router.push` para a rota de detalhe correspondente ao `tipo`/`id` daquela linha — mesma mecânica de `contas-a-pagar/page.tsx`. |
| Professora passa o mouse sobre uma linha (sem clicar) | Classe `row-link` já aplica cursor de ponteiro + destaque de hover — nenhum CSS novo. |
| Professora volta (gesto/botão do navegador) | Comportamento de navegação já existente do Next.js App Router — ver `research.md` R1. Filtros da aba Indicadores podem ou não estar preservados, dependendo do comportamento observado na implementação; ambos os resultados são aceitos pela spec (FR-009). |

## Nenhuma validação nova

Não há regra de negócio, validação de formato, ou autorização nova introduzida por esta feature — a autorização de quem pode ver o relatório e as telas de detalhe já existe e não muda.
