# Feature Specification: Corrigir Filtro de Receita/Despesa Pendente no Relatório Financeiro

**Feature Branch**: `035-fix-saldo-previsto-filtro`

**Created**: 2026-09-20

**Status**: Draft

**Input**: User description: Item selecionado do relatório de investigação de faxina do módulo Financeiro — "Bug: Saldo Previsto ignora filtros": em `GET /api/relatorios/financeiro` (aba "Visão Financeiro" do Relatório Financeiro), os cards "Receita Pendente", "Despesa Pendente" e "Saldo Previsto" ignoram os filtros de aluno/turma/matéria aplicados pela professora, mostrando sempre o total global do negócio, enquanto "Total Recebido" e "Saldo Realizado" respeitam esses mesmos filtros corretamente.

## Nota de investigação prévia

Confirmado por leitura do código atual:

- O cálculo vive em `RelatorioService.ObterFinanceiroAsync` ([RelatorioService.cs:111-158](../../src/SPI.Application/Relatorios/Services/RelatorioService.cs#L111-L158)), endpoint `GET /api/relatorios/financeiro`, consumido pela aba "Visão Financeiro" de `frontend/app/(app)/relatorios/financeiro/page.tsx`.
- `TotalRecebido`/`SaldoRealizado` (linhas 114-115, 129) são calculados a partir de `ListarPagosNoPeriodoAsync(inicio, fim, formaPagamentoId, alunoId, turmaId, materiaId, ...)` — **respeitam corretamente** os filtros `alunoId`/`turmaId`/`materiaId` recebidos pelo método.
- `ReceitaPendente`/`DespesaPendente` (linhas 124-127) vêm de `ObterReceitasPendentesSegregadasAsync(cancellationToken)`/`ObterDespesasPendentesSegregadasAsync(cancellationToken)`, **chamados sem nenhum argumento de filtro** — sempre retornam o total global do negócio, independentemente de `alunoId`/`turmaId`/`materiaId` terem sido informados. `SaldoPrevisto` (linha 138) é derivado de `ReceitaPendente`/`DespesaPendente`, herdando o mesmo problema.
- `ObterReceitasPendentesSegregadasAsync` ([IRelatorioRepository.cs:58-59](../../src/SPI.Domain/Repositories/IRelatorioRepository.cs#L58-L59)) já aceita filtros opcionais `turmaNome`/`materiaNome`/`alunoBusca` (por texto), usados hoje pela tela "Financeiro — Visão Geral" — mas `RelatorioService.ObterFinanceiroAsync` recebe `alunoId`/`turmaId`/`materiaId` (por id numérico), não por nome, e não repassa nenhum desses filtros na chamada.
- `ObterDespesasPendentesSegregadasAsync` ([IRelatorioRepository.cs:61](../../src/SPI.Domain/Repositories/IRelatorioRepository.cs#L61)) **não tem nenhum parâmetro de filtro** — Contas a Pagar (despesas) não tem nenhum vínculo com Aluno/Turma/Matéria no modelo de dados, então esse lado do cálculo é legitimamente global por natureza, não um bug.
- Confirmado que a mesma tela já comunica esse exato princípio em outro contexto: a aba "Indicadores" (mesma página, método diferente — `ObterIndicadoresFinanceirosAsync`) já exibe o aviso "Filtros de turma/matéria/aluno aqui se aplicam só ao lado da receita — despesas (Contas a Pagar) não têm ligação com aluno/turma/matéria, então continuam representando o total do negócio" ([relatorios/financeiro/page.tsx:380-383](../../frontend/app/(app)/relatorios/financeiro/page.tsx#L380-L383)). A aba "Visão Financeiro" (`ObterFinanceiroAsync`) não tem nenhum aviso equivalente, e hoje nem sequer aplica o filtro ao lado da receita.
- Confirmado que os três filtros (`alunoId`, `turmaId`, `materiaId`) já chegam ao backend nessa chamada: o mesmo filtro compartilhado acima das duas abas da tela (`frontend/app/(app)/relatorios/financeiro/page.tsx:81-83`) é enviado tanto para `obterRelatorioFinanceiro` (aba Visão Financeiro) quanto para `obterIndicadoresFinanceiros` (aba Indicadores).

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Receita Pendente e Saldo Previsto refletem o filtro aplicado (Priority: P1)

Como professora consultando a aba "Visão Financeiro" do Relatório Financeiro, ao filtrar por um aluno, turma ou matéria específicos, eu quero que "Receita Pendente" e "Saldo Previsto" mostrem valores calculados dentro desse mesmo filtro — assim como "Total Recebido" e "Saldo Realizado" já fazem — para que eu não seja induzida a pensar que aquele aluno/turma/matéria tem uma previsão de recebimento que na verdade é do negócio inteiro.

**Why this priority**: É o próprio defeito relatado — hoje a tela mistura, sem aviso, valores filtrados (Recebido, Saldo Realizado) com valores globais (Receita Pendente, Despesa Pendente, Saldo Previsto) na mesma consulta, o que pode levar a uma decisão financeira equivocada sobre um aluno/turma/matéria específico.

**Independent Test**: Filtrar o Relatório Financeiro (aba Visão Financeiro) por um aluno com pagamentos pendentes, e confirmar que "Receita Pendente" corresponde à soma dos valores pendentes apenas desse aluno (conferível manualmente), não ao total pendente de todos os alunos.

**Acceptance Scenarios**:

1. **Given** um filtro de `alunoId` aplicado, **When** `GET /api/relatorios/financeiro?alunoId=...` é consultado, **Then** `ReceitaPendente` reflete apenas os valores pendentes (a vencer + atrasados) desse aluno, não o total do negócio.
2. **Given** um filtro de `turmaId` e/ou `materiaId` aplicado (sem `alunoId`), **When** o relatório é consultado, **Then** `ReceitaPendente` reflete apenas os valores pendentes dos alunos/pagamentos associados a essa turma/matéria.
3. **Given** qualquer combinação dos filtros acima, **When** o relatório é consultado, **Then** `DespesaPendente` continua representando o total do negócio (Contas a Pagar não tem vínculo com aluno/turma/matéria) — comportamento inalterado, mesmo com filtro aplicado.
4. **Given** os valores corrigidos de `ReceitaPendente` e `DespesaPendente` (este último sempre global), **When** `SaldoPrevisto` é calculado, **Then** o resultado usa `SaldoRealizado` (já filtrado) mais a diferença entre a `ReceitaPendente` filtrada e a `DespesaPendente` global — nunca a receita pendente global.
5. **Given** nenhum filtro de aluno/turma/matéria aplicado, **When** o relatório é consultado, **Then** o comportamento é idêntico ao atual (`ReceitaPendente`/`DespesaPendente`/`SaldoPrevisto` continuam representando o total do negócio) — sem nenhuma mudança para o caso sem filtro.

---

### Edge Cases

- Um aluno sem nenhum pagamento pendente, filtrado isoladamente: `ReceitaPendente` deve ser 0, não o total global nem um erro.
- Filtro por turma/matéria que não tem nenhum aluno com pendência no momento: mesmo caso acima, `ReceitaPendente` = 0 para esse filtro.
- `DespesaPendente` nunca deve variar com `alunoId`/`turmaId`/`materiaId` — é o comportamento correto e já esperado (documentado no aviso da aba Indicadores), não um caso a "corrigir" para também filtrar.
- Esta correção precisa de mudança de contrato de API? Não — `RelatorioFinanceiroResponse` mantém os mesmos campos (`ReceitaPendente`, `DespesaPendente`, `SaldoPrevisto`), apenas o valor calculado muda para os casos com filtro de aluno/turma/matéria aplicado.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O sistema MUST calcular `ReceitaPendente` (em `GET /api/relatorios/financeiro`) respeitando os filtros `alunoId`, `turmaId` e `materiaId` quando informados, do mesmo modo que `TotalRecebido` já respeita esses filtros hoje.
- **FR-002**: O sistema MUST manter `DespesaPendente` sempre como o total global do negócio, independentemente de `alunoId`/`turmaId`/`materiaId` serem informados — Contas a Pagar não tem vínculo com aluno/turma/matéria no modelo de dados.
- **FR-003**: O sistema MUST calcular `SaldoPrevisto` a partir do `SaldoRealizado` (já filtrado) somado à diferença entre a `ReceitaPendente` corrigida (filtrada) e a `DespesaPendente` (sempre global).
- **FR-004**: Quando nenhum filtro de aluno/turma/matéria for informado, o sistema MUST manter o comportamento atual (`ReceitaPendente`/`DespesaPendente`/`SaldoPrevisto` representando o total do negócio) — sem alteração de valor para consultas sem esses filtros.
- **FR-005**: Esta correção MUST NOT alterar nenhum outro campo da resposta de `GET /api/relatorios/financeiro` (`TotalRecebido`, `TotalPago`, `SaldoRealizado`, `PorFormaPagamento`, `PorAluno`, `PorTurma`) nem introduzir nenhum novo parâmetro de filtro.

### Key Entities

- Não introduz entidades novas — usa dados já existentes de `Pagamento` (receita) e `ContaPagar` (despesa), com os filtros `alunoId`/`turmaId`/`materiaId` já recebidos por este endpoint.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Ao filtrar o Relatório Financeiro por aluno, turma ou matéria, o valor de "Receita Pendente" exibido corresponde exatamente à soma manual dos valores pendentes (a vencer + atrasados) restritos a esse filtro — nunca ao total do negócio.
- **SC-002**: "Despesa Pendente" permanece idêntica com ou sem filtro de aluno/turma/matéria aplicado, em 100% das consultas.
- **SC-003**: Sem nenhum filtro de aluno/turma/matéria, os três valores (`ReceitaPendente`, `DespesaPendente`, `SaldoPrevisto`) permanecem idênticos ao comportamento anterior à correção.

## Assumptions

- "Receita Pendente filtrada por turma/matéria" segue a mesma lógica já usada para `TotalRecebido` neste mesmo método: pagamentos associados (diretamente ou via aula) à turma/matéria informada — não uma nova regra de associação.
- Nenhuma migração de banco de dados é necessária — a correção reaproveita filtros (`alunoId`/`turmaId`/`materiaId`) já recebidos pelo endpoint e dados já persistidos.
- A forma exata de repassar os filtros para `ObterReceitasPendentesSegregadasAsync` (reaproveitar os parâmetros de texto já existentes via alguma tradução id→nome, ou estender o repositório para aceitar os ids diretamente, como feito na spec 032 para `ObterValorPendenteAsync`) é uma decisão de implementação, não de comportamento observável — fica para `/speckit-plan`.
- Esta correção não afeta a aba "Indicadores" da mesma tela (`ObterIndicadoresFinanceirosAsync`) nem a tela "Financeiro — Visão Geral" (`FinanceiroService.ObterVisaoGeralAsync`), que não são filtráveis por aluno/turma/matéria da mesma forma e já não apresentam esse problema.
