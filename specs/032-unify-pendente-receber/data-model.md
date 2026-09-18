# Data Model: Unificar Cálculo de "Valor Pendente a Receber"

Nenhuma entidade de domínio nova, nenhuma coluna de banco, nenhuma migração. Esta mudança
consolida uma regra de agregação já existente sobre a entidade `Pagamento` já existente
(ver [Registrar Pagamento](../009-registrar-pagamento/spec.md)) em um único método, sem alterar
nenhum contrato de API.

## Método unificado

### `IRelatorioRepository.ObterValorPendenteAsync`
([IRelatorioRepository.cs:19](../../src/SPI.Domain/Repositories/IRelatorioRepository.cs#L19))

**Antes**: `Task<decimal> ObterValorPendenteAsync(CancellationToken cancellationToken = default);`
— sem parâmetros, sempre soma todos os `Pagamento` com `Status == "Pendente"`.

**Depois**: mesma assinatura, com quatro parâmetros novos opcionais (todos com default `null`,
preservando 100% de compatibilidade com a chamada atual do Dashboard):

| Parâmetro | Tipo | Efeito |
|---|---|---|
| `alunoId` | `int?` | Restringe a soma aos pagamentos daquele aluno. |
| `status` | `string?` | Ver tabela de comportamento em [research.md — R1](./research.md#r1--mapear-exatamente-o-comportamento-hoje-replicado-por-combinação-de-filtro) — `null`/`"Pendente"` incluem tudo; `"Atrasado"` restringe à fatia vencida; qualquer outro status zera o resultado. |
| `vencimentoInicio` | `DateOnly?` | Restringe a `DataVencimento >= vencimentoInicio`. |
| `vencimentoFim` | `DateOnly?` | Restringe a `DataVencimento <= vencimentoFim`. |

## Chamadores

| Chamador | Antes | Depois |
|---|---|---|
| `DashboardService` (`GET /api/dashboard`) | `ObterValorPendenteAsync(cancellationToken)` | Mesma chamada, sem argumentos — resultado idêntico (FR-003). |
| `RelatorioService.ObterPagamentosAsync` (`GET /api/relatorios/pagamentos`) | Soma em memória sobre `Itens` já mapeados (`itens.Where(Pendente/Atrasado).Sum()`) | `ObterValorPendenteAsync(cancellationToken, alunoId, status, inicio, fim)` — resultado idêntico para cada combinação de filtros já aceita hoje (FR-004). |

## Contrato de resposta (API) — sem alteração de shape

Nenhum campo muda de nome, tipo ou aparece/desaparece em `DashboardResponse.ValorPendenteRecebimento`
nem em `RelatorioPagamentosResponse.TotalPendenteConsolidado`. A mudança é inteiramente interna à
camada de aplicação/infraestrutura.
