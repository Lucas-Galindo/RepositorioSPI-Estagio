# Data Model: Corrigir Agrupamento por Turma no Relatório Financeiro

Nenhuma entidade nova, nenhum campo novo, nenhuma migração de banco. Esta correção reorganiza a
lógica de agregação em memória usando relacionamentos e entidades já existentes. As entidades
abaixo estão listadas apenas pelo papel que já desempenham hoje e que passam a ser efetivamente
usados na determinação da turma de um pagamento.

## Entidades envolvidas (sem alteração de estrutura)

### Pagamento
Já existente ([Pagamento.cs](../../src/SPI.Domain/Entities/Pagamento.cs)). Campo relevante:
`ValorFinal` (o valor agregado por grupo). Relacionamento relevante: `PagamentosAula`
(coleção de `PagamentoAula`) — passa a ser a fonte usada para determinar a(s) turma(s) do
pagamento no agrupamento `PorTurma`, em vez de `Aluno.AlunosTurma`.

### PagamentoAula
Já existente ([PagamentoAula.cs](../../src/SPI.Domain/Entities/PagamentoAula.cs)). Tabela
associativa N:N entre `Pagamento` e `Aula` (`pagamento_id`, `aula_id`). É o vínculo real que
liga um pagamento às aulas que ele efetivamente cobre — a base da correção.

### Aula
Já existente ([Aula.cs](../../src/SPI.Domain/Entities/Aula.cs)). Campo relevante: `TurmaId`
(nullable — `null` para aula individual/sem turma). É a fonte real e única da turma associada a
um pagamento, substituindo a lista genérica de turmas do aluno.

### Turma
Já existente ([Turma.cs](../../src/SPI.Domain/Entities/Turma.cs)). Campo relevante: `Nome`
(usado como `Chave` no item do agrupamento, igual ao comportamento atual para o caso de turma
única).

## Regra de agregação (comportamento novo, não estrutura de dados)

Para cada `Pagamento` no conjunto já filtrado por `ObterFinanceiroAsync` (status "Pago" no
período/filtros), calcular o conjunto de origens de turma a partir de
`Pagamento.PagamentosAula.Select(pa => pa.Aula.TurmaId)`:

| Condição sobre o conjunto de `TurmaId` das aulas vinculadas | Grupo (`Chave`) resultante |
|---|---|
| Vazio (nenhuma aula vinculada), ou todas as aulas vinculadas têm `TurmaId == null` | `"Atendimento particular"` (comportamento já existente, preservado) |
| Exatamente um `TurmaId` não nulo distinto, e nenhuma aula sem turma no mesmo pagamento | Nome da turma (`Turma.Nome`) |
| Mais de um `TurmaId` distinto, ou combinação de turma(s) com aula(s) sem turma | `"Múltiplas turmas"` (grupo novo) |

`Total` de cada grupo continua sendo a soma de `ValorFinal` dos pagamentos que caem nele — sem
duplicação (um pagamento contribui para exatamente um grupo) e sem perda (todo pagamento
"Pago" no conjunto filtrado cai em exatamente um dos três casos acima), o que garante SC-003 da
spec (soma dos grupos == Total Recebido).

## Contrato de resposta (API) — sem alteração de forma

`RelatorioFinanceiroResponse.PorTurma` continua sendo `List<RelatorioFinanceiroItem>` com
`{ Chave: string, Total: decimal }` — o mesmo formato já usado por `PorFormaPagamento` e
`PorAluno`. A única mudança observável pelo consumidor (frontend) é o **conteúdo**: os valores
passam a ser corretos por turma, e pode aparecer um novo item com `Chave == "Múltiplas turmas"`
quando aplicável. Nenhuma migração de contrato de API é necessária; ver [plan.md](./plan.md) —
não há pasta `contracts/` nesta feature por não haver mudança de shape/endpoint.
