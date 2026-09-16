# Data Model: Tabela de Lançamentos em Indicadores Financeiros

Nenhuma tabela, coluna ou migração nova de banco de dados — a feature apenas lê registros já
existentes de `Pagamentos` (entidade `Pagamento`) e `ContasPagar` (entidade `ContaPagar`) e os
expõe individualmente em um novo item de resposta da API. As duas tabelas/entidades de origem já
existem e não são alteradas.

## Entidade lógica: Lançamento (entrada ou saída) — DTO `LancamentoIndicadorItem`

Representa uma linha da nova tabela na aba Indicadores. É uma projeção somente-leitura, não uma
entidade persistida — construída no serviço a partir de `Pagamento` (quando `Tipo = "Entrada"`)
ou `ContaPagar` (quando `Tipo = "Saida"`).

| Campo | Tipo | Origem | Regra |
|---|---|---|---|
| `Id` | `int` | `Pagamento.Id` ou `ContaPagar.Id` | Identificador do registro de origem; único dentro do seu `Tipo`, não entre os dois tipos (um `Pagamento` id=5 e uma `ContaPagar` id=5 podem coexistir na lista) |
| `Tipo` | `string` (`"Entrada"` \| `"Saida"`) | Fixo por método de origem | Usado pelo frontend para o filtro visual "Entradas"/"Saídas" (FR-005) |
| `Descricao` | `string` | `Pagamento.Descricao` / `ContaPagar.Descricao`, com fallback | Se vazio/nulo: nome do `Aluno` vinculado (entrada) ou `ContaPagar.Favorecido` — e, se este também vazio, `CategoriaDespesa.Nome` (saída). MUST NOT ficar em branco (FR-012) |
| `DataVencimento` | `DateOnly` | `Pagamento.DataVencimento` / `ContaPagar.DataVencimento` | Data exibida na coluna "Data" (FR-002) — mesma data já usada por "Gargalo de caixa"/"Fluxo de caixa" nesta tela, não a data de pagamento efetivo (decisão em Clarifications) |
| `Valor` | `decimal` | `Pagamento.ValorFinal` / `ContaPagar.Valor` | Não exigido como coluna pela spec (que só pede descrição + data), mas incluído no contrato para permitir uso futuro sem quebrar o contrato; frontend nesta versão pode ignorá-lo |
| `Status` | `string` | `Pagamento.Status` / `ContaPagar.Status` | Incluído para permitir estilização futura (ex.: destacar atrasados); não exigido como coluna visível pela spec |

### Regras de seleção (quais registros entram na lista)

- **Entradas**: `Pagamento` com `Status != "Cancelado"` e `DataVencimento` dentro do
  período filtrado; restrito por turma/matéria/aluno quando esses filtros da página estiverem
  ativos (reaproveita `AplicarFiltroReceita`, mesma regra dos demais indicadores da tela — FR-008).
- **Saídas**: `ContaPagar` com `Status != "Cancelado"` e `DataVencimento` dentro do período
  filtrado; **nunca** restrito por turma/matéria/aluno (despesas não têm ligação com
  aluno/turma/matéria no domínio, mesma nota já exibida na tela hoje).
- Ordenação final da lista combinada: por `DataVencimento` decrescente (FR-011).
- Lançamentos com a mesma data: sem critério de desempate adicional (spec Edge Cases) — ordem
  relativa entre eles não é garantida.

### Estados vazios

- Nenhum lançamento (entrada nem saída) no período/filtros → lista vazia; frontend exibe estado
  vazio genérico (FR-010, US1 cenário 3).
- Nenhuma entrada (com filtro "Entradas" ativo) → lista filtrada fica vazia; frontend exibe
  estado vazio específico (US2 cenário 4). Mesma lógica simétrica para "Saídas".

## Resposta da API (estendida)

`IndicadoresFinanceirosFiltradosResponse` (já existente) recebe um novo campo:

```text
IndicadoresFinanceirosFiltradosResponse
├── Indicadores: IndicadoresFinanceirosResponse   # já existente — GargaloCaixa é REMOVIDO daqui
│                                                   # (FR-001: card correspondente deixa de existir)
├── FluxoCaixaMensal: List<FluxoCaixaMensalItem>  # já existente, inalterado
└── Lancamentos: List<LancamentoIndicadorItem>    # NOVO — lista unificada de entradas e saídas
                                                    # do período, já ordenada por data desc
```

Nota sobre `GargaloCaixa`: a spec (FR-001) exige a remoção do card visual; como
`GargaloCaixaResponse`/`IndicadoresFinanceirosResponse.GargaloCaixa` também é consumido pelo
Dashboard (`DashboardService.ObterIndicadoresFinanceirosAsync`, mesmo DTO
`IndicadoresFinanceirosResponse`), o campo `GargaloCaixa` **não** é removido do DTO compartilhado
— apenas o card correspondente na tela de Indicadores do Relatório Financeiro deixa de ser
renderizado no frontend. O Dashboard continua recebendo e usando `GargaloCaixa` normalmente, sem
regressão.
