# Phase 0 Research: Unificar Cálculo de "Valor Pendente a Receber"

## R1 — Mapear exatamente o comportamento hoje replicado por combinação de filtro

**Decision**: Antes de desenhar a assinatura unificada, foi mapeado o comportamento exato de
`TotalPendenteConsolidado` hoje (via `RelatorioService.ObterPagamentosAsync` →
`PagamentoService.ListarAsync` → `PagamentoRepository.ListarAsync` + `Mapear`), para cada valor
possível do parâmetro `status`:

| `status` recebido | O que a implementação atual retorna |
|---|---|
| `null` (sem filtro) | Soma de **todos** os `Pagamento` com `Status` persistido `"Pendente"` (inclui os efetivamente "Atrasado") — idêntico ao `ValorPendenteRecebimento` do Dashboard. |
| `"Pendente"` | **Mesmo resultado que `null`** — surpreendente à primeira vista, mas correto: o repositório filtra por `Status == "Pendente"` (todos, vencidos ou não), e o filtro final `itens.Where(Pendente ou Atrasado)` não exclui nenhum deles, já que todo `Pagamento` persistido como `"Pendente"` tem status efetivo `"Pendente"` ou `"Atrasado"`. |
| `"Atrasado"` | Só a soma da fatia **vencida** — `PagamentoService.ListarAsync` já filtra a lista mapeada para `Status == "Atrasado"` antes de retornar, então só essa fatia chega para o segundo filtro. |
| Qualquer outro status real (`"Pago"`, `"Cancelado"`) | **Zero** — o repositório já filtra por esse status na consulta (`Status == "Pago"`, por exemplo), então nenhum item mapeado pode ter status efetivo `Pendente`/`Atrasado`, e a soma final é vazia. |

`alunoId`, `vencimentoInicio` e `vencimentoFim` são aplicados como filtros adicionais independentes do valor de `status`, em todos os casos acima.

**Rationale**: FR-004 exige paridade exata; sem esse mapeamento explícito, a tabela de casos (em
especial `"Pendente"` sendo equivalente a `null`, e `"Atrasado"` produzindo um subconjunto) é
fácil de implementar incorretamente na consulta unificada.

**Alternatives considered**: Nenhuma — é um levantamento de fato, não uma decisão de design.

## R2 — Onde estender a implementação compartilhada

**Decision**: Estender `IRelatorioRepository.ObterValorPendenteAsync` ([IRelatorioRepository.cs:19](../../src/SPI.Domain/Repositories/IRelatorioRepository.cs#L19)) com quatro parâmetros opcionais (`alunoId`, `status`, `vencimentoInicio`, `vencimentoFim`, todos com default `null`), preservando a assinatura atual como um subconjunto válido (chamada sem argumentos = comportamento de hoje). `RelatorioRepository.ObterValorPendenteAsync` ([RelatorioRepository.cs:27-37](../../src/SPI.Infrastructure/Repositories/RelatorioRepository.cs#L27-L37)) implementa a tabela de R1 numa única consulta EF Core traduzida para SQL (sem materializar entidades completas), usando o mesmo corte `DateOnly.FromDateTime(DateTime.UtcNow)` já usado em `PagamentoService.Mapear` ([PagamentoService.cs:200](../../src/SPI.Application/Pagamentos/Services/PagamentoService.cs#L200)) para decidir "vencido".

**Rationale**: `IRelatorioRepository` já concentra outras consultas agregadas e filtráveis por `alunoId`/período seguindo exatamente esse padrão de parâmetros opcionais (ex.: `ObterInadimplenciaNoPeriodoAsync`, `ObterEntradasPorDiaDoMesAsync`) — estender o método existente é consistente com a convenção já usada no restante do repositório, em vez de criar um método novo paralelo.

**Alternatives considered**:
- Criar um método novo (`ObterValorPendenteFiltradoAsync`) mantendo o antigo intacto: rejeitado — recriaria a duplicação que esta feature existe para eliminar (o Dashboard continuaria numa implementação "antiga" separada da nova).
- Mover a lógica para `PagamentoRepository`/`PagamentoService` (já que ela reusa a definição de "Atrasado" de lá): rejeitado — `IRelatorioRepository` é o lugar já estabelecido para agregações financeiras multi-consumidor (ver comentário no topo do arquivo); manter a coerência estrutural existente.

## R3 — Trocar o cálculo em memória do Relatório de Pagamentos pela chamada ao repositório

**Decision**: Em `RelatorioService.ObterPagamentosAsync` ([RelatorioService.cs:73-96](../../src/SPI.Application/Relatorios/Services/RelatorioService.cs#L73-L96)), a linha `var totalPendente = itens.Where(i => i.Status is "Pendente" or "Atrasado").Sum(i => i.Valor);` é substituída por uma chamada a `_relatorioRepository.ObterValorPendenteAsync(cancellationToken, alunoId, status, inicio, fim)` (já injetado no service — `_relatorioRepository`, ver construtor). A lista `Itens` da resposta continua vindo de `_pagamentoService.ListarAsync(...)` exatamente como hoje — só o total consolidado muda de fonte.

**Rationale**: Elimina a segunda implementação da regra sem alterar nenhum outro campo da resposta (`Itens` continua sendo a lista individual de pagamentos, usada para exibir cada linha, que é um propósito diferente do total agregado).

**Alternatives considered**: Fazer o Dashboard passar a usar a mesma listagem completa (`PagamentoService.ListarAsync` + filtro em memória) que o Relatório usa hoje — rejeitado explicitamente pela spec (Assumptions): regrediria a performance do único consumidor ativo (Dashboard), que hoje é uma soma agregada direta sem carregar entidades completas.

## R4 — Nenhum teste automatizado cobre este cálculo hoje

**Decision**: Confirmado que não existe teste de unidade para `ObterValorPendenteAsync`
diretamente (é uma consulta de repositório sobre `SpiDbContext`, testada hoje só indiretamente —
`DashboardServiceLancamentosTests`-like arquivos não existem para este método específico). A
verificação desta feature é por equivalência de comportamento (mesma saída antes/depois) validada
manualmente com dados reais via `quickstart.md`, e pela suíte de testes completa para garantir
ausência de regressão nos demais fluxos que dependem de `DashboardService`/`RelatorioService`.

**Rationale**: Consistente com o padrão já observado nas features anteriores desta mesma sessão
(024/030/031) — quando não há teste pré-existente, a validação é por compilação + verificação
manual guiada por cenários explícitos.
