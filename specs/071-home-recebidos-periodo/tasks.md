# Tasks: Home — "Recebidos no período" e cards que acompanham os filtros

**Input**: `specs/071-home-recebidos-periodo/` ([spec.md](spec.md), [plan.md](plan.md), [research.md](research.md), [contracts/dashboard.md](contracts/dashboard.md), [quickstart.md](quickstart.md))

**Formato**: `- [ ] T### [P?] [US?] descrição com caminho`. `[P]` = pode rodar em paralelo (arquivos diferentes, sem dependência).

**Ambiente**: `dotnet test tests/SPI.Application.Tests` **sem** `--no-build` (o `dotnet build SPI.slnx` não compila os testes); `NODE_OPTIONS=--max-old-space-size=1536` no `tsc`/`eslint`; API em `http://localhost:5099` e Next com `NEXT_PUBLIC_API_URL` definida dentro do comando; senha só por `SPI_TESTE_SENHA`; sem commit/push até o usuário pedir.

## Phase 1: Linha de base

- [X] T001 Anotar a linha de base **antes de mexer no código**: `git status --porcelain` (só `.gitignore` e os `.csproj` do usuário); `dotnet test tests/SPI.Application.Tests` (688 aprovados); com a API local no ar, gravar em arquivo do scratchpad as respostas de `GET /api/dashboard` **sem parâmetros** e com `periodoInicio=2026-01-01&periodoFim=2026-12-31` (campos `alunosAtendidosNoPeriodo`, `valorFaturadoNoPeriodo` e `pacotes`), para o antes/depois do T020. Linha de base: dashboard sem parâmetros (0 atendidos, R$ 350,00) e 2026 (1 aluno, R$ 870,00); 688 testes.

## Phase 2: User Story 3 — Período coerente com só o "até" (P2) · base do backend

**Goal**: o período do dashboard sai de uma função pura e testável. **Independent Test**: `dotnet test` dos casos de `ResolverPeriodo`.

- [X] T002 [US3] `src/SPI.Application/Dashboard/Services/DashboardService.cs`: criar `public static (DateOnly Inicio, DateOnly Fim) ResolverPeriodo(DateOnly? periodoInicio, DateOnly? periodoFim, DateOnly hoje)`: início = `periodoInicio` ou, se só há "até", o **1º dia do mês do "até"**, ou, sem nenhum, o 1º dia do mês de `hoje`; fim = `periodoFim` ou um mês depois do início menos um dia (comportamento atual, mantido). `ObterAsync` passa a usá-la (no lugar das 3 linhas atuais de `inicio`/`fim`).
- [X] T003 [P] [US3] `tests/SPI.Application.Tests/Dashboard/DashboardPeriodoTests.cs` (novo): casos de `ResolverPeriodo`: nenhum filtro (mês de `hoje`), só "a partir de" (fim = último dia daquele mês), só "até" (início = dia 1 do mês do "até"; ex.: 15/09 → 01/09 a 15/09), os dois informados (inalterados), virada de ano e fevereiro bissexto (29/02/2024), "até" no dia 1. **Dois testes falharam de início por erro da spec** (só o "a partir de" termina um mês depois menos um dia, não no último dia do mês); spec e testes corrigidos, código mantido.

**Checkpoint**: `dotnet test` verde para T003.

## Phase 3: User Story 2 — O filtro de aluno filtra os dois cards (P1)

**Goal**: com aluno escolhido, "Recebidos" e "Alunos atendidos" consideram só ele. **Independent Test**: API com `alunoId` + testes do repasse.

- [X] T004 [US2] `src/SPI.Domain/Repositories/IRelatorioRepository.cs` e `src/SPI.Infrastructure/Repositories/RelatorioRepository.cs`: `ContarAlunosAtendidosNoPeriodoAsync(DateOnly inicio, DateOnly fim, CancellationToken cancellationToken = default, int? alunoId = null)`; com `alunoId`, acrescentar `aa.AlunoId == alunoId.Value` à consulta (o `Distinct().CountAsync` resulta em 0 ou 1); sem `alunoId`, a consulta fica idêntica à atual.
- [X] T005 [P] [US2] Atualizar a assinatura de `ContarAlunosAtendidosNoPeriodoAsync` nos 3 fakes: `tests/SPI.Application.Tests/Relatorios/RelatorioServiceFinanceiroPendenteTests.cs`, `RelatorioServiceFinanceiroPorTurmaTests.cs` e `RelatorioServiceLancamentosTests.cs` (só o parâmetro novo; comportamento do fake igual).
- [X] T006 [US2] `DashboardService.ObterAsync`: passar `alunoId` para `ContarAlunosAtendidosNoPeriodoAsync(inicio, fim, cancellationToken, alunoId)` e para `ObterValorFaturadoNoPeriodoAsync(inicio, fim, cancellationToken, alunoId: alunoId)` (parâmetro nomeado; o método já existe).
- [X] T007 [P] [US2] `src/SPI.Api/Controllers/DashboardController.cs`: atualizar os comentários XML de `periodoInicio` (só o "até" → 1º dia do mês do "até") e `alunoId` (filtra os cards de atendidos e recebidos e a seção de pacotes). Nenhum parâmetro novo.
- [X] T008 [US2] `tests/SPI.Application.Tests/Dashboard/DashboardFiltroAlunoTests.cs` (novo): com `System.Reflection.DispatchProxy` implementar `IRelatorioRepository`, `IVinculoCobrancaRepository` e `IAulaRepository` (devolvem vazio/zero e registram os argumentos); verificar que `ObterAsync(..., alunoId: 7)` repassa `7` a `ContarAlunosAtendidosNoPeriodoAsync` e `ObterValorFaturadoNoPeriodoAsync`, e que com `alunoId` nulo repassa `null` (FR-006); e que o período repassado segue `ResolverPeriodo`.
- [X] T009 [US2] `dotnet test tests/SPI.Application.Tests` (sem `--no-build`) até passar tudo (688 + os novos). 698 testes aprovados (688 + 10 novos).

**Checkpoint**: backend completo e coberto.

## Phase 4: User Story 1 — Cards com o nome certo (P1)

**Goal**: rótulos falam de período. **Independent Test**: Home no Chrome.

- [X] T010 [US1] `frontend/app/(app)/dashboard/page.tsx` (único arquivo do frontend): card "Alunos atendidos": legenda "Com aula realizada no período"; card "Recebido este mês" → **"Recebidos no período"**, dica "Total de contas a receber que já foram pagas dentro do período escolhido (sem período, o mês atual).", legenda "Contas a receber pagas no período". Quando há aluno escolhido (`alunoId` já é estado da tela), acrescentar " · do aluno selecionado" às duas legendas. Renomear as variáveis locais (`recebidoMes` → `recebidoNoPeriodo`) sem mudar de onde vêm. **Sem** somar, contar ou filtrar nada no frontend.
- [X] T011 [US1] `cd frontend`: `npx tsc --noEmit` e `npx eslint` (projeto inteiro, 0 problemas).

## Phase 5: Acabamento e validação

- [X] T012 [P] Nota na `specs/058-*/spec.md` (a spec da Home com período e aluno): "specs/071: o período (só o "até" começa no 1º dia do mês do "até") e o filtro de aluno passaram a valer também para os cards Alunos atendidos e Recebidos no período; ver `specs/071-home-recebidos-periodo/`." (Princípio V).
- [X] T013 **Verificação dos "MUST NOT" FR-004, FR-006, FR-007, FR-008, FR-009**: (a) FR-004: ler o `git diff` de `dashboard/page.tsx` e confirmar que só há textos/nomes de variável, sem `.reduce`, `.filter`, `.sort`, soma ou contagem novos e sem outra fonte para os dois números além de `dashboard?.alunosAtendidosNoPeriodo`/`valorFaturadoNoPeriodo`; (b) FR-007: `git diff --stat -- src/SPI.Application/Financeiro src/SPI.Application/Relatorios` sem alterações e o método `ObterValorFaturadoNoPeriodoAsync` sem mudança de assinatura (`git diff` de `RelatorioRepository.cs` só na contagem de atendidos); (c) FR-008: `git diff -- src/SPI.Application/Dashboard/Dtos` vazio (nenhum campo novo/renomeado) e `frontend/lib/api/dashboard.ts` intacto; (d) FR-009: `git status --porcelain -- frontend database` mostrando **só** `dashboard/page.tsx` modificado, nenhum arquivo não rastreado, nenhuma migração, e `git diff -- frontend/package.json src/**/*.csproj` sem mudança feita por esta spec (os `.csproj` do usuário já estavam modificados antes).
- [X] T014 API real no banco local (quickstart §2): (a) FR-006: repetir as chamadas do T001 e comparar com a linha de base: **mesmos** valores sem `alunoId`; (b) `alunoId` de um aluno com pagamentos: `valorFaturadoNoPeriodo` = soma dos pagamentos pagos dele no período (conferir com `GET /api/pagamentos`) e atendidos 0 ou 1; (c) aluno sem nada: 0 e 0,00; (d) só `periodoFim=2026-09-15`: período 01/09 a 15/09; (e) só `periodoInicio`: fim = um mês depois menos um dia (sem mudança). **API real: JSON idêntico à linha de base nos dois casos; aluno 1 em 2026 = 1 e R$ 870,00 (= soma dos pagamentos pagos); aluno 999 = 0 e 0; só o "até" 15/09 = período 01/09 a 15/09.** O banco local só tem 1 aluno, então o aluno 999 é o que prova o filtro.
- [X] T015 Chrome com login real (quickstart §3), claro e escuro, com capturas: card "Recebidos no período" e legendas; valores com o período de 2026; escolher aluno e voltar a "Todos"; só o "até"; dica do ícone; 360 px sem rolagem horizontal; 0 erros de console. **12 PASS e 1 FAIL do roteiro (comparação de caixa: o CSS exibe o rótulo em maiúsculas); texto conferido.**
- [X] T016 Final: `dotnet test` (sem `--no-build`), `npx tsc --noEmit`, `npx eslint`, e `npm run build` (API parada; `.next/dev` removido antes).
- [X] T017 Fechamento: `docs/relatorios/<data>-home-recebidos-periodo.md` (pedido, o que foi feito, validação, o que ficou de fora); hook `revisao-codigo` do Trello (com a API parada); memória do projeto (arquivo novo + linha no `MEMORY.md`); **sem commit/push**.

## Dependências e ordem

- T001 antes de qualquer alteração. T002 antes de T003 e T006. T004 antes de T005 e T006. T006 antes de T008. T010 depende só da leitura do spec (pode andar em paralelo com a Phase 3, mas as legendas "do aluno selecionado" só fazem sentido com T006 pronto para validar).
- T013 a T015 só depois de todo o código pronto.

## Paralelismo

- T003, T005, T007, T012 em arquivos independentes. Backend (T002–T009) e frontend (T010–T011) podem andar juntos.

## Estratégia

MVP = Phase 2 + Phase 3 (backend e testes) + T010. Em seguida validar em API e Chrome (T013–T015) e fechar.

## Cobertura dos requisitos

| Requisito | Tarefas |
|---|---|
| FR-001, FR-002 (textos) | T010, T015 |
| FR-003 (filtro de aluno) | T004, T005, T006, T008, T014, T015 |
| FR-004 (**MUST NOT** recalcular no frontend) | T010, **T013(a)** |
| FR-005 (só o "até") | T002, T003, T014, T015 |
| FR-006 (**MUST NOT** mudar sem filtro) | T001, T008, **T014(a)** |
| FR-007 (**MUST NOT** mudar Financeiro/Relatórios) | **T013(b)**, T016 |
| FR-008 (**MUST NOT** mudar o contrato) | **T013(c)** |
| FR-009 (**MUST NOT** banco/arquivo novo/dependência) | **T013(d)** |
| SC-001 a SC-005 | T014, T015, T013 |
