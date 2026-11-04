# Tasks: Selo de cobrança do aluno nas telas da professora

**Input**: `specs/068-badge-cobranca-aluno/` (spec.md, plan.md, research.md, data-model.md, contracts/, quickstart.md)
**Prerequisites**: nenhum. **Sem migração e sem mudança em `database/`** (somente leitura). A spec 067 (link da aula) está em standby e não é pré-requisito.

**Formato**: `- [ ] TNNN [P] [USn] descrição com caminho`. `[P]` = pode rodar em paralelo. Regras do `CLAUDE.md`: cada "MUST NOT" tem tarefa de verificação (`git status --porcelain` para "sem arquivo novo"; **leitura do diff** para "sem lógica no frontend").

**Testes**: rodar sempre `dotnet test tests/SPI.Application.Tests` **sem `--no-build`** (o `.slnx` não compila o projeto de testes).

**Segurança**: só o banco **local**; backup antes de criar dados de teste; dados sintéticos "TESTE-068" apagados no fim; dados do usuário (turma 15, vínculos 20/21/22/34, 1 aluno) intactos; a senha de teste só por variável de ambiente (`SPI_TESTE_SENHA`); nada em produção; para o `next build`, parar a API (memória).

## Phase 1: Linha de base

- [X] T001 Rodar `database/backup/backup-database.ps1`; anotar a linha de base: `dotnet build SPI.slnx` (0 erros, contagem de avisos), `dotnet test tests/SPI.Application.Tests` (todos verdes, anotar o total), `cd frontend && npx tsc --noEmit` e `npx eslint` limpos, `git status --porcelain`, e as contagens do banco local (`aula`, `aula_aluno`, `pagamento`, `alunos`, `turma`, `vinculo_cobranca`) para provar depois que nada mudou.

## Phase 2: Fundação (bloqueia as histórias)

- [X] T002 [P] Criar `src/SPI.Application/VinculosCobranca/Dtos/SeloCobrancaResponse.cs` (`Categoria`, `Valor`, `TurmaNome?`, `Progresso?` do tipo `ProgressoPacoteResponse` existente, `AvisoPacoteEsgotado`, `OutrosVinculos`, `Detalhes`) e `DetalheCobrancaResponse` (`Categoria`, `TurmaNome?`, `Valor`, `Progresso?`) conforme data-model.md. Acrescentar `public SeloCobrancaResponse? Cobranca { get; set; }` (com `summary` curto) em `AlunoResponse`, `src/SPI.Application/Common/Dtos/AlunoResumoResponse.cs` e `src/SPI.Application/Aulas/Dtos/AlunoPresencaResponse.cs`.
- [X] T003 [P] `src/SPI.Domain/Repositories/IVinculoCobrancaRepository.cs`: acrescentar `Task<List<VinculoCobranca>> ListarAtivosDosAlunosAsync(IReadOnlyCollection<int> alunoIds, CancellationToken cancellationToken = default)`; implementar em `src/SPI.Infrastructure/Repositories/VinculoCobrancaRepository.cs` com **uma** consulta (`Include(v => v.Turma)`, `Where(v => v.Ativo && alunoIds.Contains(v.AlunoId))`, `OrderBy(v => v.Id)`; lista vazia → retorna vazio sem consultar). Acrescentar o método (lançando `NotImplementedException`) nos fakes existentes `tests/SPI.Application.Tests/Aulas/AulaServiceFakes.cs`, `…/Mensalidade/MensalidadeFakes.cs` e `…/VinculosCobranca/VinculoCobrancaFakes.cs`, sem alterar nenhum `Assert`.
- [X] T004 Criar `tests/SPI.Application.Tests/VinculosCobranca/ResumoCobrancaServiceTests.cs` **antes** do serviço (fakes manuais próprios de `IVinculoCobrancaRepository` e `IAulaRepository`, este com `ContarPresencasPorVinculosAsync` funcional e um **contador de chamadas**): (a) lista: principal Mensalidade → Pacote → Avulsa, mais recente (maior Id) em empate, `OutrosVinculos` = ativos − 1, `Detalhes` com todos os ativos, `TurmaNome` preenchido, vínculo **inativo ignorado**; (b) aluno sem vínculo → `SemVinculo` com `Valor` = `ValorAula`; (c) pacote: `Progresso` com restantes, estado Esgotado, "Total não informado"; (d) grupo (turma/aula): **consistência com `RegraCobrancaPresenca.Decidir`** numa matriz de vínculos (mensalidade; pacote da turma com saldo; pacote **de outra turma** → não vale; avulsa do contexto; pacote esgotado + avulsa → `Avulsa` com `AvisoPacoteEsgotado`; pacote esgotado sem avulsa → `Pacote` com estado `Esgotado`; sem vínculo) para `turmaId` da turma e para `null` (individual); `TurmaNome`, `OutrosVinculos` e `Detalhes` **vazios** no modo grupo; (e) **guarda de N+1**: uma lista de 30 alunos faz **exatamente 1** chamada a `ListarAtivosDosAlunosAsync` e no máximo 1 a `ContarPresencasPorVinculosAsync`; lista vazia não consulta.
- [X] T005 Criar `src/SPI.Application/VinculosCobranca/Services/IResumoCobrancaService.cs` (`ResumirParaListaAsync(IReadOnlyCollection<Aluno>)` e `ResumirParaGrupoAsync(IReadOnlyCollection<Aluno>, int? turmaId)`, ambos devolvendo `Dictionary<int, SeloCobrancaResponse>`) e `ResumoCobrancaService.cs`: **somente leitura**; uma consulta de vínculos ativos em lote (T003), uma de presenças em lote só se houver pacote; lista = principal pela ordem fixa numa constante única + `OutrosVinculos` + `Detalhes`; grupo = `RegraCobrancaPresenca.Decidir(ativosDoAluno, turmaId)` mapeado conforme a tabela de research.md R4 (`Bloqueada` → categoria `Pacote` com estado Esgotado; `SemVinculo` → `ValorAula`); categoria = nome da modalidade; progresso por `ProgressoPacote.Calcular`. Nenhum `Salvar`/`Adicionar`/`Atualizar`. Comentar a regra (porquê) no estilo do projeto. Registrar `services.AddScoped<IResumoCobrancaService, ResumoCobrancaService>()` em `ApplicationServiceCollectionExtensions.cs`.
- [X] T006 Injetar o serviço como **último parâmetro opcional** (`IResumoCobrancaService? cobranca = null`) em `AlunoService`, `TurmaService` e `AulaService` (campo `_cobranca`), sem mexer ainda na lógica. Conferir que as construções de teste existentes compilam sem alteração e que o DI resolve (build da API).

**Checkpoint**: `dotnet test` verde (os testes novos do T004 passam com o serviço do T005); sem uso nas telas ainda.

## Phase 3: User Story 1 — Cobrança de cada aluno na lista de alunos (P1)

**Meta**: coluna "Cobrança" na lista de alunos com o resumo (principal + "+N" + dica), calculado **só quando a tela pede** (`incluirCobranca=true`).
**Teste independente**: lista com aluno de mensalidade, de pacote, com pacote + avulsa e sem vínculo.

- [X] T007 [US1] Criar `tests/SPI.Application.Tests/Alunos/AlunoServiceCobrancaListaTests.cs`: `ListarAsync(..., incluirCobranca: true)` devolve `Cobranca` preenchida para cada aluno (resumo); **sem** `incluirCobranca` (padrão) o serviço de resumo **não é chamado** e `Cobranca` fica `null` (FR-013); `ObterPorIdAsync`/cadastrar/atualizar **não** preenchem (`null`); sem o serviço injetado (`null`) a lista funciona e `Cobranca` fica `null`; uma única chamada em lote para a lista inteira.
- [X] T008 [US1] `src/SPI.Application/Alunos/Services/AlunoService.cs`: em `IAlunoService`/`AlunoService.ListarAsync` acrescentar o parâmetro opcional `bool incluirCobranca = false`; depois de mapear, `if (incluirCobranca && _cobranca is not null)` resumir a lista **toda** de uma vez (`ResumirParaListaAsync(alunos)`) e atribuir `Cobranca` por aluno; os demais retornos ficam como estão. `src/SPI.Api/Controllers/AlunosController.cs`: `Listar` ganha `[FromQuery] bool incluirCobranca = false` repassado ao serviço (único controller tocado; sem `try/catch`).
- [X] T009 [P] [US1] Frontend: `frontend/lib/api/alunos.ts`, `turmas.ts` e `aulas.ts` (tipos `SeloCobranca`/`DetalheCobranca` e o campo `cobranca` em `Aluno`, `AlunoResumo` e `AlunoPresenca`, espelhando o contrato); criar `frontend/components/alunos/SeloCobranca.tsx` (**único componente novo**): pílula `status-pill` (Mensalidade e Pacote em dia → `status-ativo`; Pacote em atenção → `status-pendente`; Esgotado → `status-cancelado`; Avulsa e Sem vínculo → `status-inativo`) com a categoria e "· restam N" no pacote; linha pequena com o valor (`currency`) e, só no modo `lista`, a turma; "+N" quando `outrosVinculos > 0`; `title` com todos os `detalhes` no modo `lista`; aviso "pacote esgotado" na avulsa; traço quando `cobranca` for nulo; propriedade `modo: "lista" | "grupo"`; **sem `onClick`, sem ordenação, sem decisão**. `listarAlunos` em `lib/api/alunos.ts` aceita o filtro opcional `incluirCobranca`; `frontend/app/(app)/alunos/page.tsx` o envia como `true` e ganha a coluna "Cobrança" (`<th>` e `<td>`) com `<SeloCobranca selo={a.cobranca} modo="lista" />`. As outras 11 telas que chamam `listarAlunos` **não** mudam.
- [X] T010 [US1] **Verificação dos "MUST NOT" FR-006, FR-007, FR-009, FR-010 e FR-012 (lista e componente)**: (a) ler o `git diff` de `SeloCobranca.tsx` e `alunos/page.tsx`: nada de `.sort(`, `.find(`, `.filter(` de vínculo, comparação de categoria para escolher, conta de aulas restantes nem `onClick`; (b) `grep -rn "listarVinculos\|vinculos-cobranca" "frontend/app/(app)/alunos/page.tsx" frontend/components/alunos/SeloCobranca.tsx` **sem resultado**; `grep -rn "incluirCobranca" frontend --include=*.ts --include=*.tsx` mostra **somente** `lib/api/alunos.ts` e `alunos/page.tsx` (FR-013); (c) `grep -nE "#[0-9a-fA-F]{3,6}|rgb\(" frontend/components/alunos/SeloCobranca.tsx` **sem resultado** e `git diff -- frontend/styles frontend/lib/paleta.ts` vazio; (d) `git status --porcelain -- frontend` mostra **só** `SeloCobranca.tsx` como arquivo novo (`??`).

**Checkpoint**: US1 funcional; `dotnet test`, `tsc` e `eslint` verdes.

## Phase 4: User Story 2 — Cobrança que vale na turma (P1)

**Meta**: selo por aluno na página da turma, com o vínculo que vale **naquela turma**.

- [X] T011 [US2] Criar `tests/SPI.Application.Tests/Turmas/TurmaServiceCobrancaTests.cs`: `ObterPorIdAsync` preenche `Cobranca` (modo grupo) para cada aluno matriculado; pacote da turma → `Pacote` com restantes; **pacote de outra turma** não vale (cai na avulsa ou `SemVinculo`); mensalidade vale; `TurmaNome`/`OutrosVinculos`/`Detalhes` vazios; `AlunosComPacoteForaDaTurma` **sem** `Cobranca`; `ListarAsync` (lista de turmas) não preenche; sem o serviço, tudo `null`.
- [X] T012 [US2] `src/SPI.Application/Turmas/Services/TurmaService.cs`: em `MapearComPacotesAsync`, `if (_cobranca is not null)` resumir os alunos matriculados com `ResumirParaGrupoAsync(alunosDaTurma, turma.Id)` (uma chamada) e atribuir `Cobranca` a cada `AlunoResumoResponse` de `Alunos`; **não** tocar `AlunosComPacoteForaDaTurma` nem `Mapear` (lista de turmas).
- [X] T013 [P] [US2] `frontend/app/(app)/turmas/[id]/page.tsx`: na linha de cada aluno de `turma.alunos`, `<SeloCobranca selo={a.cobranca} modo="grupo" />` abaixo do RA, sem tocar a regra de matrícula nem as demais seções.
- [X] T014 [US2] **Verificação dos "MUST NOT" FR-006, FR-007 e FR-012 (turma)**: ler o `git diff` de `turmas/[id]/page.tsx` e confirmar que só entrou o selo (nenhuma lógica, nenhuma chamada nova de API; `grep -n "vinculos-cobranca\|listarVinculos"` sem resultado); ler o `git diff` de `TurmaService.cs` confirmando uma única chamada em lote e nenhuma escrita.

**Checkpoint**: US2 funcional.

## Phase 5: User Story 3 — Cobrança na chamada (P1)

**Meta**: selo por aluno (e por "Outros alunos") nos detalhes da aula, com o vínculo que vale **para a aula**.

- [X] T015 [US3] Criar `tests/SPI.Application.Tests/Aulas/AulaServiceCobrancaSeloTests.cs`: `ObterPorIdAsync` preenche `Cobranca` por aluno (turma da aula; aula individual → grupo sem turma); pacote esgotado sem avulsa → `Pacote` com estado `Esgotado`; esgotado com avulsa → `Avulsa` com `AvisoPacoteEsgotado`; **aluno de reposição** com pacote de outra turma não recebe o pacote; os retornos de adicionar/remover participante, registrar sessão, cancelar e atualizar também preenchem; `ListarAsync` (lista de aulas) **não** preenche; sem o serviço, `null`.
- [X] T016 [US3] `src/SPI.Application/Aulas/Services/AulaService.cs`: método auxiliar assíncrono que, `if (_cobranca is not null)`, resume **todos** os alunos da aula com `ResumirParaGrupoAsync(alunos, aula.TurmaId)` (uma chamada) e preenche `Cobranca` em `AlunoPresencaResponse`; chamá-lo nos retornos de **uma aula só** (obter, atualizar, adicionar/remover participante, registrar sessão, cancelar). **Não** mexer em `Mapear` nem na lista (`ListarAsync`). Sem nova escrita.
- [X] T017 [P] [US3] `frontend/app/(app)/aulas/[id]/page.tsx`: em `linhaDoAluno`, `<SeloCobranca selo={al.cobranca} modo="grupo" />` abaixo do RA, para alunos da turma e "Outros alunos"; sem tocar na chamada, nos botões nem na regra de bloqueio (spec 064).
- [X] T018 [US3] **Verificação dos "MUST NOT" FR-006, FR-007 e FR-012 (chamada)**: ler o `git diff` de `aulas/[id]/page.tsx` e de `AulaService.cs`: só o selo e o auxiliar, nenhuma lógica de cobrança, nenhum `.find`/`.sort`; `grep -n "vinculos-cobranca\|listarVinculos"` sem resultado; o fluxo de registrar a chamada segue igual (testes das specs 056/063/064 sem alteração de `Assert`).

**Checkpoint**: US3 funcional; as três telas mostram o selo.

## Phase 6: Acabamento e validação final

- [X] T019 [P] Notas de Princípio V: em `specs/058-progresso-pacote/spec.md` e `specs/056-cobranca-por-aluno/spec.md` acrescentar uma nota curta apontando para `specs/068-badge-cobranca-aluno/spec.md` (o progresso do pacote e a regra de cobrança agora também aparecem como selo na lista, na turma e na chamada, sem mudar a regra). Conferir por `git diff` que só entraram as notas.
- [X] T020 **Verificação dos "MUST NOT" FR-008, FR-010 e "sem arquivo novo indevido"**: `git status --porcelain -- database` vazio; `git diff --stat -- src/SPI.Application/Aulas/Services/RegraCobrancaPresenca.cs src/SPI.Application/VinculosCobranca/Services/ProgressoPacote.cs` vazio; `grep -nE "Salvar|Adicionar|Atualizar|Remover" src/SPI.Application/VinculosCobranca/Services/ResumoCobrancaService.cs` sem escrita; `git status --porcelain -- src tests frontend` com arquivos novos **somente**: `SeloCobrancaResponse.cs`, `IResumoCobrancaService.cs`, `ResumoCobrancaService.cs`, `ResumoCobrancaServiceTests.cs`, `AlunoServiceCobrancaListaTests.cs`, `TurmaServiceCobrancaTests.cs`, `AulaServiceCobrancaSeloTests.cs` e `frontend/components/alunos/SeloCobranca.tsx`; `git diff -- src/SPI.Api/Controllers` mostra **somente** o parâmetro `incluirCobranca` de `AlunosController.Listar` (nenhum outro controller muda e nenhum `try/catch` entra).
- [X] T021 Rodar `dotnet build SPI.slnx` (0 erros, sem avisos novos), `dotnet test tests/SPI.Application.Tests` (tudo verde; nenhum `Assert` existente alterado, conferido por `git diff -- tests`), `cd frontend && npx tsc --noEmit`, `npx eslint` nos arquivos tocados, `npm run verificar-contraste` (0 pares abaixo de 4.5:1) e `npm run build` (com a API **parada**, `experimental.cpus: 2` temporário e `NODE_OPTIONS=--max-old-space-size=1536` se faltar memória, restaurando `next.config.ts`).
- [X] T022 Validar o `quickstart.md`: criar o cenário "TESTE-068" (alunos A a F, turmas T1 e T2, vínculos conforme a tabela) e conferir pela API autenticada os itens 1 a 7 (lista, turma, chamada, reposição, no máximo 2 consultas por resposta, selo = cobrança gerada ao registrar uma chamada, contagens do banco iguais) e no Chrome os itens 1 a 6 da seção 3 (claro e escuro; o número de requisições de cada tela igual ao de antes). Olhar as capturas.
- [X] T023 Fechamento: escrever `docs/relatorios/<data>-badge-cobranca-aluno.md` (pedido, o que foi feito, validação, o que ficou de fora e as decisões menores adotadas); hook `revisao-codigo` do Trello (com a API **parada**, para o hook rodar os testes) e comentário no card; apagar os dados "TESTE-068" (com backup) e conferir que os dados do usuário seguem intactos; parar os servidores iniciados por mim; atualizar a memória do projeto; **não** commitar nem fazer push sem pedido.

## Dependências e ordem

- Fase 1 → Fase 2 → histórias → Fase 6.
- Fase 2: T002 e T003 em paralelo; T004 (testes) antes de T005 (serviço); T006 depois de T005.
- **US1** (T007–T010) depende da Fase 2; T009 (frontend) pode andar em paralelo com T007–T008 depois que o contrato (T002) existir. O componente criado no T009 é **reaproveitado** nas US2 e US3.
- **US2** (T011–T014) e **US3** (T015–T018) dependem só da Fase 2 e do componente (T009); são independentes entre si (arquivos diferentes), mas T013 e T017 precisam do `SeloCobranca.tsx`.
- T019 é independente; T020–T023 são o fechamento, nessa ordem.

## Paralelismo

Fase 2: T002 e T003. US1: T009 com T007–T008. US2 e US3 inteiras podem andar em paralelo depois de T009.

## Estratégia de implementação

1. **MVP**: Fase 1 + Fase 2 + US1 (a lista de alunos com o selo): já entrega o pedido central e cria o componente.
2. Depois US2 (turma) e US3 (chamada), que só ligam o mesmo componente aos outros dois contextos.
3. Acabamento e validação final só com as três histórias verdes.

## Cobertura dos requisitos

| FR | Tarefas | FR | Tarefas |
|---|---|---|---|
| FR-001 | T004, T005, T007, T008, T009 | FR-007 | T010, T014, T018 |
| FR-002 | T004, T005, T011, T012, T015, T016 | FR-008 | T005, T020 |
| FR-003 | T004, T005, T009 | FR-009 | T009, T010, T021 |
| FR-004 | T004, T005, T009 | FR-010 | T010, T020 |
| FR-005 | T004, T005, T009 | FR-011 | T005, T009 |
| FR-006 | T003, T004, T010, T014, T018 | FR-012 | T009, T010, T014, T018 |
| FR-013 | T007, T008, T009, T010, T022 | | |

## Cobertura dos critérios de sucesso

| SC | Tarefas |
|---|---|
| SC-001 (todos os alunos com selo, conferido no banco local) | T007, T011, T015, T022 |
| SC-002 (selo = regra da cobrança da presença) | T004, T022 |
| SC-003 (0 requisições adicionais por aluno; telas de seleção sem o resumo) | T004, T007, T010, T022 |
| SC-004 (contraste ≥ 4.5:1, claro e escuro) | T009, T021, T022 |
| SC-005 (0 dados e 0 regras alterados) | T001, T020, T022 |
| SC-006 (identifica a cobrança de relance nas três telas) | T022 |
