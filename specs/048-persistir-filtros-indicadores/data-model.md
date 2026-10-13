# Data Model: Persistir Filtros da Visão de Indicadores

Nenhuma entidade de domínio nova. O único "modelo" desta feature é o formato do objeto persistido em `sessionStorage` — puro estado de UI, já existente como `useState`, agora também serializado.

## Objeto persistido: `FiltrosRelatorioFinanceiro` (shape interno, não exportado)

| Campo | Tipo | Já existia como `useState`? | Valor padrão |
|---|---|---|---|
| `aba` | `"financeiro" \| "indicadores"` | Sim | `"financeiro"` |
| `inicio` | `string` (data `YYYY-MM-DD` ou vazio) | Sim | `""` |
| `fim` | `string` | Sim | `""` |
| `formaPagamentoId` | `string` (id ou vazio) | Sim | `""` |
| `alunoId` | `string` | Sim | `""` |
| `turmaId` | `string` | Sim | `""` |
| `materiaId` | `string` | Sim | `""` |
| `semestre` | `string` (`"1"`/`"2"`/vazio) | Sim | `""` |
| `tipoLancamento` | `"todos" \| "Entrada" \| "Saida"` | Sim | `"todos"` |

**Chave de armazenamento**: `"spi:filtros-relatorio-financeiro"` (`sessionStorage`), valor serializado em JSON.

## Fluxo de leitura/gravação

| Evento | Efeito |
|---|---|
| Página monta (primeira vez na sessão, nada salvo ainda) | Nenhum valor restaurado — todos os `useState` permanecem no padrão, como hoje. |
| Página monta (já existe algo salvo nesta aba/sessão) | Os 9 campos são lidos de `sessionStorage` e aplicados aos `useState` correspondentes — os `useEffect` de busca já existentes reagem sozinhos e recarregam os dados com esses valores. |
| Qualquer um dos 9 campos muda (usuário altera um filtro, ou troca de aba) | O objeto completo (os 9 campos, valor atual) é regravado em `sessionStorage`. |
| Usuário clica em "Limpar filtros" | Os 6 filtros de dado (`inicio`...`materiaId`) e `tipoLancamento` voltam ao padrão (R4); a gravação (evento acima) salva esse estado "limpo" — `aba` não é alterada por essa ação. |
| Fechar a aba/janela, ou abrir uma aba nova | `sessionStorage` não é compartilhado — a próxima vez que a página montar nessa nova aba/sessão, começa do padrão. |

## Nenhuma validação nova

Não há regra de negócio, cálculo, ou chamada de API nova — os valores restaurados são só mais um jeito de os `useState` existentes começarem preenchidos, em vez de vazios.
