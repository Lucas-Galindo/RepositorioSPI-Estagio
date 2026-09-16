# Tasks: Tabela de Lançamentos em Indicadores Financeiros

**Input**: Design documents from `/specs/022-tabela-lancamentos-indicadores/`

**Prerequisites**: [plan.md](./plan.md) (required), [spec.md](./spec.md) (required), [research.md](./research.md), [data-model.md](./data-model.md), [contracts/indicadores-financeiros.md](./contracts/indicadores-financeiros.md), [quickstart.md](./quickstart.md)

**Tests**: Solicitados explicitamente para a lógica de backend (fallback de descrição, exclusão de "Cancelado", ordenação — ver T007), conforme decisão registrada em [plan.md](./plan.md) Technical Context ("Testing"). O projeto não tem biblioteca de mock instalada (`tests/SPI.Application.Tests` só usa `xunit` + `FluentValidation`, ver `SPI.Application.Tests.csproj`) — T007 usa um fake manual de `IRelatorioRepository` em vez de introduzir Moq/NSubstitute como dependência nova. Frontend continua sem framework de teste automatizado (sem Jest/Vitest/Playwright em `frontend/package.json`) — validação manual via [quickstart.md](./quickstart.md).

**Organization**: Tasks agrupadas por user story. US1 (P1) e US3 (P1) têm a mesma prioridade máxima, mas US3 depende do trabalho de backend de US1 (a lista unificada de lançamentos já precisa existir para verificar que os filtros de período/turma/matéria/aluno a afetam corretamente). US2 (P2, filtro Entradas/Saídas) é puramente frontend e client-side, podendo ser feita em paralelo com o polimento de US1 depois que a tabela básica existir.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Pode rodar em paralelo (arquivos diferentes, sem dependência)
- **[Story]**: US1 (tabela substitui o card), US2 (filtro Entradas/Saídas), US3 (integração com filtros já existentes)
- Caminhos de arquivo exatos em cada descrição

## Path Conventions

Projeto web existente (frontend Next.js + backend ASP.NET Core). Esta feature toca:
- Backend: `src/SPI.Domain/Repositories/IRelatorioRepository.cs`, `src/SPI.Infrastructure/Repositories/RelatorioRepository.cs`, `src/SPI.Application/Relatorios/Dtos/` (novo DTO + resposta estendida), `src/SPI.Application/Relatorios/Services/RelatorioService.cs`
- Frontend: `frontend/lib/api/dashboard.ts`, `frontend/lib/api/relatorios.ts`, `frontend/app/(app)/relatorios/financeiro/page.tsx`
- Testes: `tests/SPI.Application.Tests/Relatorios/` (novo diretório)

---

## Phase 1: Setup

Não aplicável — projeto já inicializado, nenhuma dependência nova (ver [plan.md](./plan.md) Technical Context: "nenhuma dependência nova"). Prosseguir direto para a Fase 2.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Criar o DTO compartilhado e estender o contrato de resposta que tanto US1 (renderização da tabela) quanto qualquer verificação de US2/US3 vão consumir. Sem isso, nenhuma user story tem o que exibir ou filtrar.

**⚠️ CRITICAL**: Esta fase bloqueia toda a Fase 3 (US1). US2 e US3 dependem, por sua vez, de US1 estar completa (ver Dependencies).

- [X] T001 [P] Criar `src/SPI.Application/Relatorios/Dtos/LancamentoIndicadorItem.cs` com o DTO descrito em [data-model.md](./data-model.md): `Id` (`int`), `Tipo` (`string`, valores `"Entrada"` ou `"Saida"`), `Descricao` (`string`, nunca vazio — preenchido com fallback antes de chegar aqui), `DataVencimento` (`DateOnly`), `Valor` (`decimal`), `Status` (`string`).
- [X] T002 [P] Em `src/SPI.Application/Relatorios/Dtos/IndicadoresFinanceirosFiltradosResponse.cs`, adicionar a propriedade `public List<LancamentoIndicadorItem> Lancamentos { get; set; } = new();` — sem remover nenhum campo existente (mudança aditiva, ver [contracts/indicadores-financeiros.md](./contracts/indicadores-financeiros.md) "Compatibilidade").
- [X] T003 Em `src/SPI.Domain/Repositories/IRelatorioRepository.cs`, adicionar as duas assinaturas novas (ao lado de `ObterEntradasPorDiaDoMesAsync`/`ObterSaidasPorDiaDoMesAsync`):
  - `Task<List<(int Id, string? Descricao, string AlunoNome, DateOnly DataVencimento, decimal Valor, string Status)>> ListarEntradasNoPeriodoAsync(DateOnly inicio, DateOnly fim, CancellationToken cancellationToken = default, int? turmaId = null, int? materiaId = null, int? alunoId = null);`
  - `Task<List<(int Id, string? Descricao, string? Favorecido, string CategoriaNome, DateOnly DataVencimento, decimal Valor, string Status)>> ListarSaidasNoPeriodoAsync(DateOnly inicio, DateOnly fim, CancellationToken cancellationToken = default);`

**Checkpoint**: DTO e contrato de repositório definidos — a Fase 3 (US1) já pode implementar contra essas assinaturas.

---

## Phase 3: User Story 1 - Ver todos os lançamentos do período em uma tabela (Priority: P1) 🎯 MVP

**Goal**: No lugar do card "Gargalo de caixa" na aba Indicadores (`frontend/app/(app)/relatorios/financeiro/page.tsx`), exibir uma tabela com uma linha por lançamento individual (entrada ou saída) do período filtrado, com colunas de descrição e data de vencimento, incluindo todo lançamento com status diferente de "Cancelado" (pendente, atrasado ou pago — decisão de [Clarifications](./spec.md#clarifications) do spec) e com fallback de descrição (nome do aluno / favorecido / categoria) quando o campo estiver vazio.

**Independent Test**: Acessar `/relatorios/financeiro` → aba Indicadores com lançamentos de entrada e saída no período do mês corrente (incluindo ao menos um sem `Descricao` cadastrada) e verificar que: (a) o card "Gargalo de caixa" não aparece mais; (b) a tabela lista cada lançamento individualmente, com descrição nunca vazia e data de vencimento; (c) um período sem lançamentos mostra estado vazio, sem erro.

### Implementation for User Story 1

- [X] T004 [P] [US1] Implementar `ListarEntradasNoPeriodoAsync` em `src/SPI.Infrastructure/Repositories/RelatorioRepository.cs`: reaproveitar `AplicarFiltroReceita` (mesmo helper usado por `ObterEntradasPorDiaDoMesAsync`, linhas 345-361) sobre `_dbContext.Pagamentos.Include(p => p.Aluno).Where(p => p.Status != "Cancelado" && p.DataVencimento >= inicio && p.DataVencimento <= fim)`, projetando `Id`, `Descricao`, `Aluno.Nome` (como `AlunoNome`), `DataVencimento`, `ValorFinal` (como `Valor`) e `Status`.
- [X] T005 [P] [US1] Implementar `ListarSaidasNoPeriodoAsync` em `src/SPI.Infrastructure/Repositories/RelatorioRepository.cs`: sobre `_dbContext.ContasPagar.Include(c => c.CategoriaDespesa).Where(c => c.Status != "Cancelado" && c.DataVencimento >= inicio && c.DataVencimento <= fim)` (sem `AplicarFiltroReceita` — saídas nunca são filtradas por turma/matéria/aluno, FR-008/data-model.md), projetando `Id`, `Descricao`, `Favorecido`, `CategoriaDespesa.Nome` (como `CategoriaNome`), `DataVencimento`, `Valor`, `Status`.
- [X] T006 [US1] Em `src/SPI.Application/Relatorios/Services/RelatorioService.cs`, dentro de `ObterIndicadoresFinanceirosAsync`, chamar os dois métodos novos (mesmos `periodoInicio`/`periodoFim`/`turmaId`/`materiaId`/`alunoId` já usados por `ObterEntradasPorDiaDoMesAsync`/`ObterSaidasPorDiaDoMesAsync` nas linhas 177-179) e compor a lista final de `LancamentoIndicadorItem`:
  - Entrada: `Tipo = "Entrada"`, `Descricao = string.IsNullOrWhiteSpace(x.Descricao) ? x.AlunoNome : x.Descricao` (FR-012).
  - Saída: `Tipo = "Saida"`, `Descricao = string.IsNullOrWhiteSpace(x.Descricao) ? (string.IsNullOrWhiteSpace(x.Favorecido) ? x.CategoriaNome : x.Favorecido) : x.Descricao` (FR-012, ordem de precedência explícita: descrição cadastrada → favorecido → categoria da despesa).
  - Unir as duas listas e ordenar por `DataVencimento` descendente (FR-011) antes de atribuir a `indicadores.Lancamentos` (nome do campo em `IndicadoresFinanceirosFiltradosResponse`, não em `IndicadoresFinanceirosResponse` — ver T002).

### Tests for User Story 1 ⚠️

> **NOTE**: T007 depende de T004-T006 (usa os DTOs/assinaturas reais de `RelatorioService`/`IRelatorioRepository`, ainda que com um fake de repositório — não é um teste de contrato pré-implementação). Escrever e rodar T007 imediatamente após T006 para capturar qualquer regressão na lógica de fallback/exclusão/ordenação antes de seguir para o frontend (T008+).

- [X] T007 [US1] Criar `tests/SPI.Application.Tests/Relatorios/RelatorioServiceLancamentosTests.cs`: um fake manual de `IRelatorioRepository` (implementando todos os métodos da interface — sem Moq/NSubstitute, ver nota de **Tests** no topo) devolvendo dados controlados para `ListarEntradasNoPeriodoAsync`/`ListarSaidasNoPeriodoAsync` (e valores neutros/vazios para os demais métodos da interface, não usados por este teste), instanciar `RelatorioService` com o fake, chamar `ObterIndicadoresFinanceirosAsync` e verificar em `indicadores.Lancamentos`:
  - Precedência do fallback de descrição para saída: uma saída com `Descricao` vazia e `Favorecido` preenchido usa o `Favorecido`; uma saída com `Descricao` e `Favorecido` ambos vazios usa `CategoriaNome` (FR-012, precedência descrição → favorecido → categoria).
  - Exclusão de "Cancelado": um `Pagamento`/`ContaPagar` com `Status == "Cancelado"` incluído nos dados de entrada do fake **não** aparece em `Lancamentos` — como `ListarEntradasNoPeriodoAsync`/`ListarSaidasNoPeriodoAsync` já filtram por `Status != "Cancelado"` na query, este teste verifica o comportamento do fake espelhando a mesma regra (documentando a expectativa no nível do serviço, já que o filtro real é do repositório/EF Core e não é exercitado por este teste em memória).
  - Ordenação: dados do fake fora de ordem por `DataVencimento` resultam em `Lancamentos` ordenado por `DataVencimento` descendente (FR-011).
- [X] T008 [P] [US1] Em `frontend/lib/api/dashboard.ts`, adicionar `export interface LancamentoIndicador { id: number; tipo: "Entrada" | "Saida"; descricao: string; dataVencimento: string; valor: number; status: string; }` (espelha `LancamentoIndicadorItem`, mesma convenção de comentário `/** Espelha ... */` já usada no arquivo).
- [X] T009 [US1] Em `frontend/lib/api/relatorios.ts`, adicionar `lancamentos: LancamentoIndicador[];` à interface `IndicadoresFinanceirosFiltrados` (importar `LancamentoIndicador` de `./dashboard`).
- [X] T010 [US1] Em `frontend/app/(app)/relatorios/financeiro/page.tsx`, remover o bloco do painel "Gargalo de caixa" (linhas ~394-422, `<div className="mini-panel">...<Icon name="cal" .../> Gargalo de caixa...</div>`) e substituir, na mesma posição dentro de `grid-2b` (antes do painel "Fluxo de caixa"), por um novo `mini-panel` com título (ex.: "Lançamentos do período") e uma tabela (`table-wrap`/`table`, mesmo padrão visual das demais tabelas da tela) com colunas "Descrição" e "Data", uma linha por item de `indicadoresDados.lancamentos` (sem filtro de tipo aplicado nesta task — isso é US2), formatando `dataVencimento` com a mesma função de data já usada na tela (ex.: `fmtData`/equivalente já importado) e ordenação já vinda do backend (T006), sem reordenar no frontend.
- [X] T011 [US1] No mesmo bloco de `frontend/app/(app)/relatorios/financeiro/page.tsx`, exibir um `EmptyState` (mesmo componente já usado nos outros estados vazios da tela) quando `indicadoresDados.lancamentos.length === 0`, com mensagem equivalente a "Nenhum lançamento no período" (FR-010).

**Checkpoint**: User Story 1 completa e testável de forma independente — a tabela substitui o card, mostra entradas e saídas do período (incluindo pendentes/atrasadas) com descrição nunca vazia, e trata o estado vazio.

---

## Phase 4: User Story 2 - Filtrar a tabela por Entradas ou Saídas (Priority: P2)

**Goal**: Dois botões no topo da tabela de lançamentos, "Entradas" e "Saídas", com o mesmo padrão visual de botão do menu Financeiro (`btn btn-sm btn-primary`/`btn-ghost`, corrigidos em [021-fix-hitbox-cliques](../021-fix-hitbox-cliques/spec.md)), que restringem — inteiramente no cliente, sem nova chamada de API (ver [research.md](./research.md) decisão 1) — quais linhas aparecem, com um clique repetido no botão já ativo voltando ao estado "mostrar tudo" (spec.md US2, Acceptance Scenario 5).

**Independent Test**: Com a tabela de US1 já exibindo entradas e saídas, clicar em "Entradas" e verificar que só entradas aparecem (com o botão "Entradas" com aparência de ativo); clicar em "Saídas" e verificar a troca; clicar de novo em "Saídas" (já ativo) e verificar que a tabela volta a mostrar entradas e saídas juntas; confirmar que nenhuma chamada de rede nova ocorre em nenhuma dessas trocas (painel Network do navegador).

### Implementation for User Story 2

- [X] T012 [US2] Em `frontend/app/(app)/relatorios/financeiro/page.tsx`, adicionar um estado local `const [tipoLancamento, setTipoLancamento] = useState<"todos" | "Entrada" | "Saida">("todos")` próximo aos demais estados de filtro da tela (`aba`, `inicio`, `fim`, etc., linhas ~35-52) — estado de UI puro, não reenviado à API.
- [X] T013 [US2] No painel de lançamentos (dentro do `mini-panel` criado em T010), adicionar dois `<button type="button">` acima da tabela, rotulados "Entradas" e "Saídas", com `className` `btn btn-sm ${tipoLancamento === "Entrada" ? "btn-primary" : "btn-ghost"}` (e equivalente para "Saida"), cada um chamando `setTipoLancamento(tipo => tipo === "Entrada" ? "todos" : "Entrada")` (e equivalente para "Saida") — o clique alterna entre aplicar o tipo e voltar a "todos" quando o próprio botão já está ativo (FR-004/FR-006, spec.md US2 Acceptance Scenario 5) — dentro de um contêiner `<div style={{ display: "flex", gap: 8, marginBottom: 12 }}>` (não usar a classe `.financeiro-tabs` como wrapper: sua regra `> a { flex: 1 1 0 }`, em `frontend/styles/dashboard.css:251-258`, é específica para `<a>` de navegação de página inteira — ver [research.md](./research.md) decisão 3; aqui bastam as classes `btn btn-sm` diretamente nos botões para o mesmo padrão visual de cor/tamanho).
- [X] T014 [US2] Filtrar as linhas renderizadas em T010 por `tipoLancamento === "todos" ? indicadoresDados.lancamentos : indicadoresDados.lancamentos.filter(l => l.tipo === tipoLancamento)` antes do `.map(...)` da tabela.
- [X] T015 [US2] Ajustar o `EmptyState` de T011 para usar uma mensagem específica quando `tipoLancamento !== "todos"` e a lista filtrada estiver vazia (ex.: "Nenhuma entrada no período" / "Nenhuma saída no período"), mantendo a mensagem genérica quando `tipoLancamento === "todos"` (FR-010, US2 Acceptance Scenario 4).

**Checkpoint**: User Stories 1 e 2 funcionam juntas — a tabela filtra por tipo instantaneamente (incluindo o toggle de volta a "todos"), sem nova chamada de API.

---

## Phase 5: User Story 3 - Filtros de período já existentes continuam valendo para a tabela (Priority: P1)

**Goal**: Confirmar e, onde necessário, ajustar para que os filtros de período/turma/matéria/aluno já existentes na página continuem afetando a nova tabela — e que o filtro de tipo (US2) não seja resetado quando esses filtros mudarem.

**Independent Test**: Com a tabela filtrada por "Entradas" (US2), mudar o filtro de período existente na página e verificar que a tabela recarrega só com o novo período, mantendo o filtro "Entradas" ativo; aplicar um filtro de turma/aluno e verificar que só a lista de entradas é restringida (saídas continuam completas).

### Implementation for User Story 3

- [X] T016 [US3] Confirmar em `frontend/app/(app)/relatorios/financeiro/page.tsx` que o `useEffect` que chama `obterIndicadoresFinanceiros` (linha ~95, disparado por mudanças em `inicio`/`fim`/`turmaId`/`materiaId`/`alunoId`) não reseta `tipoLancamento` (estado de T012) — o estado de filtro de tipo deve ser independente do estado de dados (`indicadoresDados`), então uma nova busca de dados não deve tocar em `tipoLancamento`; se algum código resetar esse estado, remover.
- [X] T017 [US3] Confirmar que o hint já existente na tela (`"Filtros de turma/matéria/aluno aqui se aplicam só ao lado da receita — despesas (Contas a Pagar) não têm ligação com aluno/turma/matéria, então continuam representando o total do negócio."`, linhas ~389-392) permanece visível e correto para a nova tabela — nenhuma mudança de texto necessária, já que a implementação de T004/T005 segue exatamente essa regra (FR-008); apenas verificar que o hint não ficou posicionado de forma confusa após a remoção do card "Gargalo de caixa".
- [ ] T018 [US3] Validar manualmente (sem teste automatizado) o Cenário 3 completo do [quickstart.md](./quickstart.md): trocar período com filtro de tipo ativo, aplicar filtro de turma/aluno e comparar a soma dos valores de entradas na tabela com o total equivalente já mostrado em "Fluxo de caixa (últimos 6 meses)" para o mesmo mês. **Não executado nesta sessão**: requer navegador com sessão autenticada e banco de desenvolvimento com dados semeados, indisponíveis neste ambiente. A lógica correspondente (T006) é exercitada por T007 (mesma base de dados/filtros para entradas e fluxo de caixa), mas a verificação visual fim-a-fim continua pendente de validação manual pelo usuário.

**Checkpoint**: As três user stories funcionam em conjunto — tabela, filtro de tipo e filtros de período/turma/matéria/aluno já existentes, sem regressão nos demais indicadores da tela.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Verificação final cruzando as três user stories, conforme [quickstart.md](./quickstart.md).

- [X] T019 [P] Rodar `dotnet test tests/SPI.Application.Tests --filter "FullyQualifiedName~Relatorios"` e confirmar que T007 passa.
- [X] T020 [P] Rodar `npx tsc --noEmit` e o lint do frontend nos arquivos alterados (`frontend/lib/api/dashboard.ts`, `frontend/lib/api/relatorios.ts`, `frontend/app/(app)/relatorios/financeiro/page.tsx`) — sem erros.
- [X] T021 [P] Rodar `dotnet build` na solução (ou ao menos `src/SPI.Application`, `src/SPI.Infrastructure`, `src/SPI.Domain`, `src/SPI.Api`) para confirmar que os DTOs/assinaturas novos compilam sem erro.
- [ ] T022 Executar a validação completa do [quickstart.md](./quickstart.md) (Cenários 1, 2 e 3) em `/relatorios/financeiro` → aba Indicadores, incluindo a verificação de fallback de descrição vazia, o estado vazio por tipo, e o toggle de volta a "todos" (US2 Acceptance Scenario 5). Como parte da verificação de SC-002 ("sem nenhum lançamento ausente ou duplicado"), cruzar manualmente a contagem de linhas da tabela (com o filtro em "todos") contra a contagem de registros com `Status != "Cancelado"` no mesmo período em `Pagamentos` + `ContasPagar` (por exemplo, via consulta direta ao banco de desenvolvimento ou pela contagem já visível em outras telas do módulo Financeiro para o mesmo filtro) — os números devem coincidir exatamente. **Não executado nesta sessão**: mesma limitação de ambiente de T018 (sem navegador com sessão autenticada nem banco de desenvolvimento com dados semeados disponíveis aqui). `npx tsc --noEmit`, `eslint` (T020), a suíte de testes de backend (T007/T019) e `dotnet build` (T021) foram executados como verificação estática/automatizada substituta, mas não confirmam o comportamento visual real no navegador. Pendente de validação manual pelo usuário.
- [X] T023 Confirmar, por `git diff --stat`, que nenhum arquivo fora do escopo listado em "Path Conventions" foi alterado, e que `GargaloCaixaResponse`/`IndicadoresFinanceirosResponse.GargaloCaixa` permanecem intactos no DTO compartilhado com o Dashboard (ver [data-model.md](./data-model.md) nota sobre `GargaloCaixa`) — sem regressão no Dashboard.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: N/A.
- **Foundational (Phase 2)**: Sem dependências — pode começar imediatamente. Bloqueia toda a Phase 3.
- **User Story 1 (Phase 3)**: Depende de Phase 2 completa (T001-T003).
- **User Story 2 (Phase 4)**: Depende de Phase 3 completa (a tabela e os dados de `indicadoresDados.lancamentos` precisam existir antes de filtrar por tipo).
- **User Story 3 (Phase 5)**: Depende de Phase 3 completa (mesma razão) e conceitualmente também de Phase 4 (T016 verifica que o filtro de tipo de US2 não é resetado) — na prática, executar Phase 5 depois de Phase 4.
- **Polish (Phase 6)**: Depende de Phases 3, 4 e 5 completas.

### Dentro de cada User Story

- T004 e T005 (US1) são paralelizáveis entre si (métodos de repositório independentes, arquivos/regiões diferentes do mesmo arquivo). T006 depende de T004 e T005. T007 (teste) depende de T004-T006 existirem para exercitar `RelatorioService` real — embora escrito com o fake de repositório, o teste importa e chama o `RelatorioService`/DTOs concretos criados em T001-T006. T008 é paralelizável com T004-T007 (arquivo de frontend, sem dependência de backend). T009 depende de T008. T010 depende de T006 (precisa do campo `lancamentos` na resposta) e T009 (precisa do tipo `LancamentoIndicador`). T011 depende de T010.
- T012 (US2) pode começar em paralelo com T010/T011 (estado novo, não interfere na tabela ainda). T013 depende de T012. T014 depende de T013 e de T010 (tabela já existir). T015 depende de T014 e T011.
- T016-T018 (US3) dependem de T010/T011 (US1) e T012-T015 (US2) estarem completas.

### Parallel Opportunities

- T001 e T002 (Foundational) podem rodar em paralelo (arquivos diferentes).
- T004 e T005 (US1, backend) podem rodar em paralelo.
- T008 (US1, frontend types) pode rodar em paralelo com T004-T007 (US1, backend) — arquivos completamente diferentes.
- T019, T020 e T021 (Polish) podem rodar em paralelo.

---

## Parallel Example: User Story 1

```bash
# T004 e T005 (métodos de repositório) em paralelo:
Task: "T004 Implementar ListarEntradasNoPeriodoAsync em src/SPI.Infrastructure/Repositories/RelatorioRepository.cs"
Task: "T005 Implementar ListarSaidasNoPeriodoAsync em src/SPI.Infrastructure/Repositories/RelatorioRepository.cs"

# Em paralelo com os dois acima, o tipo de frontend já pode ser criado:
Task: "T008 Adicionar interface LancamentoIndicador em frontend/lib/api/dashboard.ts"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Completar Phase 2: Foundational (T001-T003)
2. Completar Phase 3: User Story 1 (T004-T011, incluindo o teste T007) → tabela substitui o card, sem filtro de tipo ainda → **entregável isoladamente como MVP**, já cumprindo o requisito mais visível do pedido original
3. Parar e validar Cenário 1 do quickstart.md

### Incremental Delivery

1. Foundational (T001-T003) → contrato de dados pronto
2. User Story 1 (T004-T011) → tabela com todos os lançamentos, com teste de backend → MVP entregável
3. User Story 2 (T012-T015) → filtro Entradas/Saídas com toggle de volta a "todos" → entregável isoladamente por cima do MVP
4. User Story 3 (T016-T018) → confirmação/ajuste de integração com filtros já existentes
5. Polish (T019-T023) → verificação final cruzada, incluindo checagem de contagem para SC-002

## Notes

- Teste automatizado (T007) é a única automação desta feature — introduzida porque a lógica de fallback/exclusão/ordenação em `RelatorioService` é a parte de maior risco de regressão silenciosa; o restante do frontend segue sem testes automatizados (mesma limitação pré-existente do projeto).
- Nenhuma task desta lista deve alterar `GargaloCaixaResponse`/`IndicadoresFinanceirosResponse.GargaloCaixa` (consumido pelo Dashboard) — apenas parar de renderizar o card correspondente na tela de Indicadores (T010).
- FR-008 (saídas nunca filtradas por turma/matéria/aluno) é garantido por T005 explicitamente **não** chamar `AplicarFiltroReceita` — não adicionar esse filtro a `ListarSaidasNoPeriodoAsync` em nenhuma revisão futura sem atualizar o spec primeiro.
- O toggle "clicar de novo no filtro ativo volta a 'todos'" (T013) é um requisito explícito desde a resolução de `/speckit-analyze` (spec.md US2 Acceptance Scenario 5) — não é mais uma adição não documentada.
