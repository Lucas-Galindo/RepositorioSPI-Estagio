# Feature Specification: Unificar Cálculo de "Valor Pendente a Receber"

**Feature Branch**: `032-unify-pendente-receber`

**Created**: 2026-09-18

**Status**: Draft

**Input**: User description: "Unificar os dois cálculos de 'valor pendente a receber' que hoje existem separadamente: (1) ValorPendenteRecebimento (Dashboard, GET /api/dashboard): soma de Pagamento.ValorFinal onde Status == 'Pendente', sem nenhum filtro (sempre o total geral); (2) TotalPendenteConsolidado (Relatório de Pagamentos, GET /api/relatorios/pagamentos): mesma fórmula, mas aceitando filtros opcionais de alunoId, status e período por Data de Vencimento — hoje não consumido por nenhuma tela do frontend. Substituir por uma única implementação compartilhada e filtrável (alunoId, status, período), usada pelos dois endpoints: o Dashboard continua chamando sem filtros (resultado idêntico ao comportamento atual), e o Relatório de Pagamentos passa a poder usar os filtros que já aceita hoje sem consumo real. Não é necessário adicionar exibição em nenhuma tela nova nesta feature — só eliminar a duplicação de lógica, mantendo o comportamento externo do Dashboard idêntico ao de hoje."

## Nota de investigação prévia

Confirmado por leitura do código atual (contexto de uma investigação anterior nesta mesma conversa):

- `ValorPendenteRecebimento` é calculado por `RelatorioRepository.ObterValorPendenteAsync()` ([RelatorioRepository.cs:27-37](../../src/SPI.Infrastructure/Repositories/RelatorioRepository.cs#L27-L37)) — uma soma SQL direta (`SUM(ValorFinal) WHERE Status == "Pendente"`), sem nenhum parâmetro de filtro. Consumido por `DashboardService` (`GET /api/dashboard`).
- `TotalPendenteConsolidado` é calculado por `RelatorioService.ObterPagamentosAsync()` ([RelatorioService.cs:73-96](../../src/SPI.Application/Relatorios/Services/RelatorioService.cs#L73-L96)) — busca a lista completa de pagamentos via `PagamentoService.ListarAsync(alunoId, status, vencimentoInicio, vencimentoFim)` (que já aplica os três filtros), mapeia cada um calculando o status efetivo ("Atrasado" quando `Pendente` e vencido), e soma em memória os itens cujo status efetivo é `"Pendente"` ou `"Atrasado"`. Consumido por `GET /api/relatorios/pagamentos` — endpoint confirmado **sem nenhum consumidor no frontend** (nem o tipo TypeScript do campo existe em `frontend/`).
- As duas fórmulas somam exatamente o mesmo conjunto de linhas quando chamadas sem filtro algum (todo `Pagamento` com `Status` persistido `"Pendente"`, incluindo os que hoje aparecem como "Atrasado" por estarem vencidos) — não há divergência de definição, só de forma de implementação (uma consulta SQL agregada vs. uma listagem completa de entidades mapeadas e filtradas em memória).
- O filtro `status` do Relatório de Pagamentos hoje tem uma interação não intuitiva com o total pendente: se o chamador passar `status` diferente de `"Pendente"`/`"Atrasado"` (ex.: `"Pago"`), a lista já vem pré-filtrada por esse status antes da soma final, o que zera `TotalPendenteConsolidado` nesse caso. Como o endpoint nunca é chamado por nenhuma tela hoje, esse comportamento nunca se manifestou na prática — esta feature preserva esse comportamento tal como está (não é uma correção de bug, é uma unificação de implementação).

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Dashboard continua correto após a unificação (Priority: P1)

Como mantenedora do sistema, eu quero que o valor pendente a receber exibido/calculado pelo Dashboard continue exatamente igual após a unificação da lógica, para que a consolidação de código não introduza nenhuma regressão perceptível na única tela que hoje depende desse número.

**Why this priority**: É a única consequência externa real desta mudança — o Dashboard é o único consumidor ativo hoje; qualquer divergência aqui seria uma regressão visível na prática (mesmo que a tela não renderize esse campo especificamente ainda, o contrato da API não pode mudar).

**Independent Test**: Chamar `GET /api/dashboard` antes e depois da mudança, para o mesmo conjunto de dados, e confirmar que `valorPendenteRecebimento` retorna exatamente o mesmo valor.

**Acceptance Scenarios**:

1. **Given** um conjunto de pagamentos com status variados (Pendente, Pago, Cancelado, e Pendentes vencidos), **When** `GET /api/dashboard` é consultado sem nenhum filtro adicional, **Then** `valorPendenteRecebimento` retorna a soma de todos os pagamentos com status persistido `"Pendente"`, incluindo os vencidos — mesmo valor que a implementação atual retorna.
2. **Given** a unificação aplicada, **When** qualquer outro campo da resposta de `GET /api/dashboard` é consultado, **Then** nenhum deles muda de valor ou comportamento.

---

### User Story 2 - Relatório de Pagamentos ganha uma implementação filtrável compartilhada (Priority: P2)

Como mantenedora do sistema, eu quero que o total pendente consolidado do Relatório de Pagamentos continue sendo calculado com os mesmos filtros que já aceita hoje (aluno, status, período por vencimento), mas usando a mesma implementação central do Dashboard, para que exista só uma fórmula de "valor pendente" no sistema, e as duas nunca possam divergir silenciosamente no futuro.

**Why this priority**: Completa a unificação — sem esta parte, a duplicação continuaria existindo do lado do Relatório de Pagamentos, mesmo que o Dashboard já estivesse usando a implementação central.

**Independent Test**: Chamar `GET /api/relatorios/pagamentos` com e sem cada um dos filtros (`alunoId`, `status`, `inicio`/`fim`), antes e depois da mudança, e confirmar que `totalPendenteConsolidado` retorna exatamente os mesmos valores em cada combinação testada.

**Acceptance Scenarios**:

1. **Given** os mesmos dados de teste da User Story 1, **When** `GET /api/relatorios/pagamentos` é consultado sem filtros, **Then** `totalPendenteConsolidado` retorna o mesmo valor que `valorPendenteRecebimento` do Dashboard (mesma fórmula, mesmo resultado, sem filtro).
2. **Given** um filtro de `alunoId` específico, **When** consultado, **Then** `totalPendenteConsolidado` retorna a soma apenas dos pagamentos pendentes daquele aluno — mesmo valor que a implementação atual (baseada em listagem completa) já retorna hoje.
3. **Given** um filtro de período (`inicio`/`fim`) por Data de Vencimento, **When** consultado, **Then** `totalPendenteConsolidado` retorna a soma apenas dos pagamentos pendentes com vencimento no período — mesmo valor que hoje.
4. **Given** um filtro de `status` incompatível com "pendente" (ex.: `"Pago"`), **When** consultado, **Then** `totalPendenteConsolidado` continua retornando `0` — mesmo comportamento (não intuitivo, mas preservado) que a implementação atual já tem.

---

### Edge Cases

- E se, no futuro, um terceiro endpoint precisar do mesmo cálculo? Deve reutilizar a mesma implementação compartilhada, não criar uma terceira independente — é exatamente o problema que esta feature elimina.
- A interação estranha entre o filtro `status` e o total pendente (zerar quando `status` não é "Pendente"/"Atrasado") é um bug a corrigir aqui? Não — está fora do escopo desta feature, que existe para unificar a implementação preservando o comportamento externo atual, não para corrigir comportamentos pré-existentes (mesmo que discutíveis).
- Esta mudança adiciona algum filtro novo ao Dashboard? Não — o Dashboard continua chamando a implementação compartilhada sem nenhum filtro, produzindo o mesmo resultado de sempre.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O sistema MUST calcular "valor pendente a receber" através de uma única implementação compartilhada, reutilizada tanto por `GET /api/dashboard` quanto por `GET /api/relatorios/pagamentos`, substituindo as duas implementações independentes existentes hoje.
- **FR-002**: A implementação compartilhada MUST aceitar, como parâmetros opcionais, os mesmos filtros que `GET /api/relatorios/pagamentos` já aceita hoje: aluno, status (incluindo o status calculado "Atrasado") e período por Data de Vencimento.
- **FR-003**: Quando chamada sem nenhum filtro (caso do Dashboard), a implementação compartilhada MUST retornar exatamente o mesmo valor que `ValorPendenteRecebimento` retorna hoje, para o mesmo conjunto de dados.
- **FR-004**: Quando chamada com qualquer combinação dos filtros já aceitos hoje por `GET /api/relatorios/pagamentos`, a implementação compartilhada MUST retornar exatamente o mesmo valor que `TotalPendenteConsolidado` retorna hoje para essa mesma combinação de filtros — incluindo o comportamento de retornar `0` quando um `status` incompatível com "pendente" é informado (ver Edge Cases).
- **FR-005**: Esta mudança MUST NOT alterar nenhum outro campo ou valor retornado por `GET /api/dashboard` ou `GET /api/relatorios/pagamentos`.
- **FR-006**: Esta mudança MUST NOT adicionar nenhuma exibição nova em nenhuma tela do frontend — é uma consolidação de lógica interna, sem mudança de contrato de API nem de UI.

### Key Entities

- Não introduz nem remove entidades de domínio — unifica a lógica de agregação sobre `Pagamento` (ver [Registrar Pagamento](../009-registrar-pagamento/spec.md)) já usada pelos dois cálculos existentes.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Para o mesmo conjunto de dados e sem filtros, `GET /api/dashboard` retorna exatamente o mesmo `valorPendenteRecebimento` antes e depois da mudança, em 100% dos casos testados.
- **SC-002**: Para qualquer combinação de filtros (`alunoId`, `status`, período de vencimento) já aceita por `GET /api/relatorios/pagamentos`, o `totalPendenteConsolidado` retornado é idêntico antes e depois da mudança, em 100% dos casos testados.
- **SC-003**: Após a mudança, existe exatamente uma implementação do cálculo de "valor pendente a receber" no código-fonte — nenhuma segunda fórmula equivalente permanece duplicada.
- **SC-004**: Nenhum outro campo de `GET /api/dashboard` ou `GET /api/relatorios/pagamentos` muda de valor ou comportamento.

## Assumptions

- "Sem filtro" é o único modo de chamada usado hoje em produção (pelo Dashboard); os demais filtros existem no contrato de `GET /api/relatorios/pagamentos` mas não são exercidos por nenhum consumidor real — esta feature garante que eles continuem funcionando exatamente como hoje, mesmo sem uso ativo, para não quebrar um contrato de API existente.
- A implementação compartilhada deve evitar regressão de desempenho: o cálculo do Dashboard hoje é uma soma agregada direta (sem carregar entidades completas); a unificação MUST preservar essa característica para o caso sem filtro (não é aceitável que o Dashboard passe a carregar a lista completa de pagamentos em memória só para somar um total, quando uma soma agregada com filtros opcionais é tecnicamente possível).
- Nenhuma migração de banco de dados é necessária — a unificação é inteiramente de lógica de aplicação sobre dados já existentes.
- O comportamento não intuitivo do filtro `status` (zerar o total quando incompatível com "pendente") é preservado deliberadamente nesta feature; uma eventual correção desse comportamento é tratada como uma decisão de produto separada, fora deste escopo.
