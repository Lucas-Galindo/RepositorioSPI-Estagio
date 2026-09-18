# Phase 0 Research: Corrigir Agrupamento por Turma no Relatório Financeiro

## R1 — Fonte de dados correta para a turma de um Pagamento

**Decision**: Trocar a fonte usada em `RelatorioService.ObterFinanceiroAsync` (`PorTurma`) de `p.Aluno.AlunosTurma.Select(at => at.Turma.Nome).FirstOrDefault()` para as turmas das aulas efetivamente cobertas pelo pagamento: `p.PagamentosAula.Select(pa => pa.Aula.TurmaId).Distinct()`.

**Rationale**: A spec (FR-001) exige que o agrupamento reflita a turma real da aula coberta, não a lista genérica de turmas do aluno. O vínculo `Pagamento → PagamentoAula → Aula.TurmaId` já existe no domínio (usado hoje só para listar `AulaIds` em `PagamentoResponse`) e é a única fonte que amarra o valor financeiro à aula/turma que ele efetivamente quitou.

**Alternatives considered**:
- Manter `Aluno.AlunosTurma` mas usar alguma heurística de "turma mais recente": rejeitado — continuaria adivinhando em vez de usar o vínculo real, e não resolve o caso de múltiplas turmas.
- Denormalizar `TurmaId` diretamente em `Pagamento`: rejeitado — mudaria schema/entidade sem necessidade; o vínculo via `PagamentoAula`/`Aula` já é suficiente e é a fonte de verdade usada em outros pontos do sistema (ex.: filtro `turmaId` em `AplicarFiltroReceita`, que já usa `p.Aluno.AlunosTurma.Any(at => at.TurmaId == turmaId.Value)` — nota: esse filtro tem a mesma limitação estrutural, mas está fora do escopo desta correção conforme a spec, Assumptions).

## R2 — Carregamento dos dados necessários (EF Core Include)

**Decision**: `RelatorioRepository.ListarPagosNoPeriodoAsync` precisa incluir `PagamentosAula` → `Aula` → `Turma` no carregamento, além dos `Include`s já existentes (`Aluno.AlunosTurma.Turma`, `FormaPagamento`).

**Rationale**: Inspeção do código atual confirma que o projeto **não usa lazy-loading proxies** (nenhuma chamada a `UseLazyLoadingProxies` em `SpiDbContext`/DI), então qualquer navegação não explicitamente incluída via `.Include()`/`.ThenInclude()` vem vazia. Hoje `ListarPagosNoPeriodoAsync` já usa `p.PagamentosAula.Any(...)` no filtro por `materiaId` (`AplicarFiltroReceita`), o que funciona porque EF Core traduz esse `Any` para SQL diretamente na query (não depende de a coleção estar materializada em memória) — mas o agrupamento `PorTurma`, que hoje roda em memória sobre a lista já materializada (`pagos.GroupBy(...)`), precisa que `p.PagamentosAula` e `pa.Aula.TurmaId`/`Aula.Turma.Nome` estejam de fato carregados no objeto.

**Alternatives considered**:
- Buscar as turmas em uma consulta separada após materializar os pagamentos (round-trip extra ao banco): rejeitado — mais simples e consistente com o padrão já usado no método (um único carregamento via `Include`, agrupamento em memória) adicionar o `Include` que falta.
- Mover o agrupamento inteiro para o banco (LINQ traduzido para SQL) em vez de em memória: rejeitado como fora de escopo — o método já materializa `pagos` em memória para reaproveitar a mesma lista em `PorFormaPagamento`, `PorAluno` e `PorTurma`; mudar essa estratégia é uma refatoração maior não pedida pela spec (FR-006 limita o escopo a `PorTurma`).

## R3 — Regra para pagamento cobrindo aulas de mais de uma turma

**Decision**: Conforme spec FR-004/FR-002, calcular por pagamento o conjunto de `TurmaId` distintos entre as aulas vinculadas (ignorando aulas sem turma para fins de contagem de "quantas turmas distintas", mas tratando "tem aula sem turma junto com aula com turma" como parte do caso de múltiplas origens — ver Edge Cases da spec):
- 0 turmas distintas (nenhuma aula vinculada, ou todas as aulas vinculadas são individuais/sem turma) → grupo `"Atendimento particular"`.
- Exatamente 1 turma distinta, e nenhuma aula vinculada sem turma → grupo com o nome dessa turma.
- Mais de 1 "origem" distinta (mais de uma turma, ou uma turma + pelo menos uma aula sem turma) → grupo `"Múltiplas turmas"`.

**Rationale**: Só a geração manual de pagamento (`PagamentoService.RegistrarAsync`) permite associar uma lista livre de `AulaIds` sem validar que pertençam à mesma turma — confirmado por leitura de `RegistrarPagamentoRequestValidator` (não valida `AulaIds` além de existência de cada aula) e de `AulaService.GerarContasAReceberAsync` (geração automática sempre cria um `Pagamento` por aula, nunca multi-aula). A regra evita tanto perda de valor (se fosse ignorado) quanto duplicação (se fosse somado em cada turma), preservando SC-003 (soma dos grupos = Total Recebido).

**Alternatives considered**:
- Dividir proporcionalmente o valor do pagamento entre as turmas cobertas: rejeitado — não há base de proporção definida (o pagamento tem um único `ValorFinal`, não um valor por aula), e a spec optou explicitamente por um grupo dedicado em vez de uma divisão arbitrária.
- Atribuir à turma da "primeira aula" (ordenada por data ou id): rejeitado — é exatamente o tipo de escolha arbitrária que está causando o bug original (a atribuição incorreta de hoje já usa "primeiro encontrado"); repetir o padrão com outra fonte não resolveria a causa raiz.

## R4 — Impacto em testes existentes

**Decision**: Nenhum teste automatizado existente cobre hoje `ObterFinanceiroAsync`/`PorTurma` (confirmado: só existe `RelatorioServiceLancamentosTests.cs`, que cobre `ObterIndicadoresFinanceirosAsync`/`Lancamentos`, método diferente). Novos testes serão adicionados seguindo o mesmo padrão (fake manual de `IRelatorioRepository`, sem biblioteca de mock, já que o projeto de testes não tem Moq/NSubstitute instalado).

**Rationale**: Evita introduzir uma dependência de teste nova só para esta correção; mantém consistência com o padrão já estabelecido no projeto.
