# Data Model: Cards do Painel de Pacotes em Atenção

Nenhuma entidade nova, nenhum campo novo. Esta feature só muda **como** os campos já existentes são apresentados.

## Entidade existente reutilizada: `PacoteEmAtencao` (frontend) / `PacoteEmAtencaoResponse` (backend)

| Campo | Tipo | Já existia? | Onde aparece no novo card |
|---|---|---|---|
| `alunoId` | `number` | Sim (spec 041) | Destino do `href`/`onClick` do card (`/alunos/{alunoId}`) — inalterado. |
| `alunoNome` | `string` | Sim | Título do card (`<h3>`). |
| `contexto` | `string` (turma ou `"Atendimento individual"`) | Sim | Subtítulo do card (`<p>`). |
| `saldoAulas` | `number` | Sim | Destaque numérico grande (`.big-value`) + legenda (`.big-label`, "aula restante"/"aulas restantes"). |
| `estado` | `"Esgotado" \| "Atencao"` | Sim | Badge no rodapé do card (`badge-cancel`/`badge-pending`, com `Icon name="warn"`) — mesmo elemento de hoje. |

Nome do aluno (`alunoNome`) e contexto (`contexto`) aparecem como duas linhas pequenas acima do saldo (`card-eyebrow` + `big-label`), espelhando a hierarquia do card "Alunos atendidos" (revisão de 2026-10-07, ver `research.md` R2/R3).

## Fluxo de apresentação

| Evento | Efeito |
|---|---|
| Backend retorna `pacotesEmAtencao` (já ordenado: Esgotado antes de Atenção, spec 041) | O componente faz `.map()` direto, na ordem recebida — nenhum `.sort()`/agrupamento no frontend (Princípio II). |
| Lista vazia | `EmptyState` já existente, sem alteração. |
| Clique em um card | Navega para `/alunos/{alunoId}` — mesmo destino de antes, só o elemento visual clicável mudou de uma linha (`week-event`) para um card (`card card-small`) de largura fixa. |
| Pacotes não cabem todos na largura do painel | O painel (`overflowX: "auto"`) ganha rolagem horizontal própria — os cards não encolhem (`flexShrink: 0`) nem quebram linha; a página em si não rola horizontalmente por causa disso. |

## Nenhuma validação nova

Não há regra de negócio, cálculo, ou autorização nova introduzida — a autorização de quem vê a Home e o cálculo de `estado`/ordenação já existem (spec 041) e não mudam.
