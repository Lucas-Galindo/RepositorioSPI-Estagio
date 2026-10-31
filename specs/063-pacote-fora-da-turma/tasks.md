# Tasks: Turmas de Pacote e Pacote Coletivo por Turma

**Input**: `specs/063-pacote-fora-da-turma/` (spec.md, plan.md, research.md, data-model.md, contracts/, quickstart.md)
**Prerequisites**: banco local com 01 a 23 aplicados. Esta spec não depende da 062 nem dos cards de recorrência e de saída do aluno.

**Formato**: `- [ ] TNNN [P] [USn] descrição com caminho`. `[P]` = pode rodar em paralelo (arquivos diferentes, sem dependência pendente). Regras do `CLAUDE.md`: cada "MUST NOT" da spec tem uma tarefa de verificação (leitura do diff, `grep` ou `git status`), além de teste quando houver lógica.

**Histórias**: US1 cobrança por grupo (P1) · US2 turma de pacote fechada e pacote por turma (P1) · US3 "Outros alunos" na chamada (P1) · US4 migração (P2) · US5 fim da turma e volta do aluno (P2).

## Phase 1: Setup e fundação (bloqueia as histórias)

- [X] T001 Rodar `database/backup/backup-database.ps1` e guardar as contagens "antes" de `vinculo_cobranca` (total, ativos, soma de `valor` e de `saldo_aulas`), `alunos_turma`, `aula`, `aula_aluno` e `pagamento` (base da verificação da T017). Regra de segurança: nunca alterar dados sem copiar antes, nunca tocar em produção.
- [X] T002 Criar `database/25_turmas_de_pacote.sql` conforme data-model.md, nesta ordem: (1) `turma.tipo VARCHAR(20) NOT NULL DEFAULT 'Mensalidade'` com `CHECK (tipo IN ('Mensalidade','Pacote'))`, `data_inicio` e `data_fim` DATE NULL, `chk_turma_datas_so_pacote` = `tipo = 'Pacote' OR (data_inicio IS NULL AND data_fim IS NULL)` e `chk_turma_periodo` = `data_inicio IS NULL OR data_fim IS NULL OR data_fim >= data_inicio`; (2) trocar `chk_vinculocobranca_turma_so_avulsa` por `chk_vinculocobranca_turma_avulsa_ou_pacote` = `turma_id IS NULL OR modalidade IN ('Avulsa','Pacote')`; (3) migração dos dados (research.md R9): só o pacote **ativo** de aluno matriculado em exatamente uma turma **ativa** (`turma.ativo = 1`) onde todos os alunos ativos têm pacote ativo recebe `vinculo_cobranca.turma_id` e a turma recebe `tipo = 'Pacote'` (datas ficam nulas); turma inativa **não** é convertida, porque a rotina de fim de turma inativaria o pacote logo em seguida; pacote de aluno sem matrícula **não muda**; **nenhum `DELETE`**, nenhum `UPDATE` de valor, saldo, total ou `ativo`; (4) ao final, como SELECTs só de leitura (executados na aplicação do script), as consultas de "casos para decidir": (a) pacote ativo de aluno em várias turmas, (b) turma mista, (c) aluno com mais de um pacote ativo do mesmo grupo, (d) pacote ativo de aluno matriculado em turma **inativa**. Cabeçalho no padrão do `21_cobranca_por_aluno.sql`, com a seção "Segurança". Aplicar só na T017.
- [X] T003 [P] `src/SPI.Domain/Enums/TipoTurma.cs` (`Mensalidade = 1`, `Pacote = 2`, com comentário no padrão de `ModalidadeCobranca`); `src/SPI.Domain/Entities/Turma.cs` com `Tipo`, `DateOnly? DataInicio`, `DateOnly? DataFim`; mapear em `TurmaConfiguration.cs` (`tipo` como texto, `data_inicio`, `data_fim`).
- [X] T004 [P] Repositório de vínculos: em `IVinculoCobrancaRepository`/`VinculoCobrancaRepository`, `ListarPacotesAtivosDaTurmaAsync(turmaId)` e `ListarPacotesAtivosDeTurmasEncerradasAsync(DateOnly hoje)` (pacotes ativos cuja turma está inativa **ou** com `data_fim` anterior a `hoje`; traz `Turma`).

**Checkpoint**: entidade mapeada e consultas prontas; `dotnet build` verde.

## Phase 2: User Story 1 — Cobrar a presença pelo grupo do pacote (P1)

**Meta**: o pacote só vale em aula do seu grupo (a turma da aula é a do pacote; ou aula individual e pacote de aulas individuais); fora do grupo, cobrança avulsa gerada em Contas a Receber.
**Teste independente**: cenários 13 a 24 do quickstart.

- [X] T005 [US1] `src/SPI.Application/Aulas/Services/RegraCobrancaPresenca.cs`: considerar **aplicáveis** só os pacotes com `p.TurmaId == turmaIdDaAula` (null == null para aula individual) nos passos de consumo, aviso de esgotado e bloqueio (research.md R5). A ordem (mensalidade → pacote → avulsa → bloqueio → sem vínculo) **não muda**; sem regra de escolha entre pacotes (desempate legado pelo menor Id). Pacote fora do grupo, esgotado ou não, **não** barra nem avisa. Atualizar o comentário do cabeçalho.
- [X] T006 [P] [US1] Ajustar `tests/SPI.Application.Tests/Aulas/RegraCobrancaPresencaTests.cs` e criar `tests/SPI.Application.Tests/Aulas/AulaServicePacoteGrupoTests.cs`: (a) pacote de turma × aula da mesma turma consome 1 e não gera conta; (b) × aula de **outra** turma não consome e gera conta avulsa/valor da aula (FR-008, "MUST NOT"); (c) × aula individual não consome e gera conta; (d) pacote de aulas individuais × aula individual consome 1; × aula de turma não consome e gera conta; (e) aluno com pacote de duas turmas consome o da turma de cada aula (FR-022); (f) aluno que saiu da turma A e participa de aula dela como "outro aluno" consome o pacote da A; (g) pacote aplicável esgotado e sem avulsa barra; esgotado com Avulsa cai nela; pacote esgotado **fora do grupo** não barra nem avisa; (h) mensalidade nunca gera conta; (i) falta não consome nem gera conta (FR-009, "MUST NOT"); (j) **toda** presença que manda cobrar gera conta a receber, inclusive "outro aluno" (reposição) e criação retroativa (FR-023); (k) adicionar e remover reposição continua funcionando para aluno de pacote e de mensalidade (FR-013).
- [X] T007 [US1] Ajustar os testes existentes da spec 056/041 que assumem o pacote valendo em qualquer aula (`AulaServiceCobrancaPorAlunoTests.cs`, `AulaServiceRegistrarSessaoPacoteEsgotadoTests.cs` e os de `RegraCobrancaPresencaTests.cs` não cobertos pela T006): trocar o cenário "pacote sem turma em aula de turma" pelo novo comportamento (cobra avulso). Rodar `dotnet test` da suíte inteira e listar no relatório cada teste alterado e por quê.
- [X] T008 [US1] `DashboardService`: **sem mudança de backend** -- desde a spec 058 o painel de pacotes da Home não devolve "contexto" do pacote (conferido por `grep`); o rótulo "Todas as aulas do aluno" existia só em `VinculoCobrancaFormModal.tsx` e `VinculosCobrancaSection.tsx`, que passam a mostrar a turma do pacote ou "Atendimento individual" na T015.

## Phase 3: User Story 2 — Turma de pacote fechada, com o pacote por turma (P1)

**Meta**: turma com tipo e datas; o cadastro do pacote de turma matricula o aluno; o aluno pode ter 0 ou N pacotes de turma (um por turma) e no máximo um de aulas individuais; depois do início ninguém novo entra.
**Teste independente**: cenários 1 a 12 e 25 a 27 do quickstart.

- [X] T009 [US2] Turmas: `TurmaRequest` (+ `tipo`, `dataInicio`, `dataFim`), `TurmaResponse` (+ os três e `fechada`, calculada no servidor: turma de pacote com `dataInicio` anterior a hoje), `TurmaRequestValidator` (turma **nova** de pacote exige as duas datas e `fim >= início`; mensalidade não aceita datas) e `TurmaService`: cadastro; edição que aceita preencher datas de turma migrada; **recusa (409) a troca de `tipo` e a mudança da `dataInicio` depois de preenchida** (FR-005; mudar o início reabriria a turma fechada), aceitando ajustar a `dataFim` com `fim >= início`; em `VincularAlunoAsync`, em turma de pacote aceitar **só** quem já tem pacote ativo dela (consulta da T004) com a turma em andamento, recusando os demais com as mensagens de contracts/turmas-api.md. `DesvincularAlunoAsync` **não muda** (FR-006). Mensagens no padrão do projeto.
- [X] T010 [US2] `VinculoCobrancaRequestValidator.cs`: `TurmaId` passa a ser proibido só para Mensalidade. `VinculoCobrancaService`: (a) `ValidarTurmaDePacoteAsync` (existe, é da professora, ativa, de pacote e **não iniciada** no cadastro; `dataInicio` vazia não bloqueia); (b) o cadastro matricula o aluno na mesma gravação, sem duplicar; (c) em `GarantirRegrasDeCombinacaoAsync`, trocar "o aluno já tem um pacote ativo" por: no máximo um pacote ativo **por turma** e um de **aulas individuais** por aluno (0 ou N pacotes de turma; FR-022); mensalidade continua exclusiva; (d) `turmaId` imutável na edição de Pacote. Textos de erro conforme contracts/vinculos-e-aulas-api.md. O `ReativarAsync` fica na T022.
- [X] T011 [P] [US2] Testes em `tests/SPI.Application.Tests/Turmas/TurmaServiceTipoTests.cs`: turma nova de pacote sem datas recusada; mensalidade com datas recusada; troca de `tipo` recusada; mudança da `dataInicio` já preenchida recusada, preencher a data de turma migrada aceito e prorrogar ou antecipar a `dataFim` aceito (FR-005); `fechada` correta (antes, no dia e depois do início; sem data); matrícula direta recusada para quem não tem pacote dela e aceita para quem tem, só com a turma em andamento; turma de mensalidade matricula como hoje; `DesvincularAluno` funciona em turma de pacote e não altera vínculo, saldo nem pagamento (FR-006, "MUST NOT").
- [X] T012 [P] [US2] Testes em `tests/SPI.Application.Tests/VinculosCobranca/VinculoCobrancaServiceTurmaPacoteTests.cs`: pacote de turma aceito e aluno matriculado na mesma gravação, sem duplicar matrícula; recusado para turma de mensalidade, inativa, de outra professora e **já iniciada** (FR-003/FR-004, "MUST NOT"); turma migrada sem data aceita; aluno com pacote das turmas 1 e 2 e o individual, todos ativos (FR-022); segundo pacote da **mesma** turma recusado; segundo pacote de aulas individuais recusado; `turmaId` imutável; mensalidade com `turmaId` recusada e mensalidade recusada para aluno com pacotes; aluno com pacote aceita Avulsa individual.
- [X] T013 [P] [US2] Teste em `tests/SPI.Application.Tests/Aulas/AulaServiceConflitoAlunoTests.cs`: aula nova sobreposta a outra da professora é recusada mesmo com um aluno de pacote como participante (FR-016, regra existente); aula nova de turma de pacote inclui os alunos matriculados, como em qualquer turma.
- [X] T014 [US2] Frontend: `frontend/lib/api/turmas.ts` (`tipo`, `dataInicio`, `dataFim`, `fechada`), `frontend/app/(app)/turmas/TurmaForm.tsx` (campo Tipo na criação, somente leitura depois; datas para Pacote) e `frontend/app/(app)/turmas/[id]/page.tsx` (tipo, datas; botão de vincular só para quem tem pacote dela, senão texto explicando a entrada pelo pacote; usa `fechada` da API, sem calcular). Só tokens de tema (spec 057).
- [X] T015 [US2] Frontend: `frontend/lib/api/vinculosCobranca.ts` (Pacote aceita `turmaId`), `frontend/components/alunos/VinculoCobrancaFormModal.tsx` (para Pacote, campo "Turma (opcional)" só com turmas `tipo = Pacote`, ativas e `!fechada`; vazio = aulas individuais; erros de "já tem pacote deste grupo" vêm da API) e `VinculosCobrancaSection.tsx` (mostra a turma do pacote ou "Atendimento individual"; a ficha mostra um pacote por linha) — FR-017.

**Checkpoint**: US1 e US2 funcionando pela API e pelas telas.

## Phase 4: User Story 3 — "Outros alunos" na chamada (P1)

**Meta**: separar visualmente quem é de fora, com presença e falta para todos.
**Teste independente**: cenário "Frontend" da página da aula no quickstart.

- [X] T016 [US3] `frontend/app/(app)/aulas/[id]/page.tsx`: dividir `aula.alunos` pelo campo `reposicao` já devolvido pela API em duas listas: a principal e a seção **"Outros alunos"**, abaixo, oculta quando vazia. Os dois grupos usam o mesmo estado `presencas`; a etiqueta "Reposição" é substituída pelo título da seção e o botão de remover dos de reposição em aula agendada continua. Sem nenhuma conta nem filtro novo no frontend (FR-012).

## Phase 5: User Story 4 — Migração dos pacotes existentes (P2)

- [X] T017 [US4] Aplicar `database/25_turmas_de_pacote.sql` no banco **local** (depois da T001) e conferir pelo quickstart "Migração": contagens de vínculo, `alunos_turma`, `aula`, `aula_aluno` e `pagamento` **idênticas** às de antes; só `turma.tipo` e `vinculo_cobranca.turma_id` dos convertidos mudaram; todo pacote com `turma_id` aponta para turma do tipo Pacote e o aluno está matriculado nela; pacote de aluno sem matrícula continua sem turma; rodar as consultas (a), (b), (c) e (d) de "casos para decidir"; conferir que nenhum pacote foi apontado para turma inativa. Registrar os números no relatório.

## Phase 6: User Story 5 — Fim da turma e volta do aluno (P2)

**Meta**: pacotes de turma encerrada ficam inativos com o saldo preservado; reativação só com a turma em andamento; aluno excluído que volta mantém o direito.
**Teste independente**: cenários 25 a 35 do quickstart.

- [X] T018 [US5] `src/SPI.Application/Turmas/Services/EncerramentoTurmaPacoteService.cs` (+ interface `IEncerramentoTurmaPacoteService`, registrada em DI): `InativarPacotesDaTurmaAsync(turmaId)` e `EncerrarPacotesDeTurmasEncerradasAsync(DateOnly hoje)` usando a T004. Nas duas **só `Ativo` muda**: `SaldoAulas`, `Valor`, `TotalAulasPacote`, pagamentos e presenças ficam como estão (FR-019, "MUST NOT"); idempotente. Classe de opções `TurmasOptions` (`IntervaloEncerramentoMinutos`, padrão 60) ao lado de `NotificacoesOptions`, e a seção `Turmas` em `src/SPI.Api/appsettings.json` com `"IntervaloEncerramentoMinutos": 60`.
- [X] T019 [US5] `src/SPI.Infrastructure/BackgroundServices/TurmaEncerramentoDispatcherService.cs` (mesmo padrão do `NotificacaoDispatcherService`: `PeriodicTimer`, escopo por execução, falha registrada sem derrubar o serviço): executa **uma vez ao iniciar** e depois a cada `Turmas:IntervaloEncerramentoMinutos`, chamando `EncerrarPacotesDeTurmasEncerradasAsync(DateOnly.FromDateTime(DateTime.Now))`. Registrar em `InfrastructureServiceCollectionExtensions.cs` (opções, serviço e `AddHostedService`). Nenhuma chamada de e-mail.
- [X] T020 [US5] `TurmaService.ExcluirAsync`: além de `Ativo = false`, chamar `InativarPacotesDaTurmaAsync` no mesmo salvamento. `TurmaService.ReativarAsync` **não muda** (reativar a turma não reativa pacotes; verificado na T028). Matrículas e aulas não mudam.
- [X] T021 [US5] `VinculoCobrancaService.ReativarAsync` para pacote de turma: exigir a turma **em andamento** (ativa e `data_fim` vazia ou de hoje em diante), **sem** exigir "não iniciada"; repetir a regra de combinação (FR-022); matricular o aluno se faltar (FR-021). Pacote de aulas individuais: como hoje.
- [X] T022 [P] [US5] Testes em `tests/SPI.Application.Tests/Turmas/EncerramentoTurmaPacoteServiceTests.cs`: `data_fim` anterior a hoje inativa os pacotes ativos da turma, **inclusive** de aluno inativo, com saldo, valor e total iguais; `data_fim` de hoje não inativa; turma sem data não inativa; turma inativada pela professora inativa os pacotes; reativar a turma não reativa; pacote já inativo continua; rodar duas vezes não muda nada; pacote de aulas individuais nunca é tocado; pagamentos e matrículas intactos (FR-019, "MUST NOT").
- [X] T023 [US5] Estender `VinculoCobrancaServiceTurmaPacoteTests.cs` (depois da T012) com a reativação: recusada com a turma encerrada, aceita depois de prorrogar a data de fim, aceita com a turma já iniciada e em andamento, rematricula o aluno, recusada se já houver outro pacote ativo da mesma turma (FR-021). Criar `tests/SPI.Application.Tests/Alunos/AlunoServicePacoteIntactoTests.cs`: excluir e reativar o aluno não altera pacote, saldo nem matrícula, e a presença volta a consumir o saldo (FR-020, "MUST NOT").

## Phase 7: Polish e verificações transversais

- [X] T024 Nota "Regra substituída em parte pela spec 063" no topo de `specs/056-cobranca-por-aluno/spec.md`, `specs/041-pacote-esgotado/spec.md` e `specs/052-auditoria-vinculo-cobranca/spec.md` (pacote com grupo, só vale no grupo; vários pacotes de turma; turma com tipo) — FR-018, Princípio V.
- [X] T025 FR-010 / Princípio II ("MUST NOT" regra de cobrança fora de `RegraCobrancaPresenca`): ler o diff de `AulaService.cs`, `TurmaService.cs`, `EncerramentoTurmaPacoteService.cs` e `VinculoCobrancaService.cs` e confirmar que nenhum decide cobrança por modalidade ou por turma (`grep -n "Modalidade ==" src/SPI.Application/Aulas`). Ler o diff de `frontend/` e confirmar que nenhum arquivo decide cobrança nem calcula "fechada"/"em andamento".
- [X] T026 FR-012 ("MUST NOT" frontend decidir quem é "outro aluno"): ler o diff de `aulas/[id]/page.tsx` e confirmar que a divisão usa só `al.reposicao` recebido, sem comparar turma, matrícula ou vínculo.
- [X] T027 FR-016 ("MUST NOT" segunda regra de conflito): ler o diff de `AulaService.cs` e `AulaRepository.cs` (esperado: sem diff relevante) e confirmar que `ExisteConflitoAsync` continua sendo o único cálculo de sobreposição e que o teste da T013 passa.
- [X] T028 FR-006/FR-015/FR-019/FR-020 ("MUST NOT" apagar ou alterar vínculos, saldos, matrículas e aulas): (a) ler o diff de `database/25_turmas_de_pacote.sql`: nenhum `DELETE`, o único `UPDATE` em `vinculo_cobranca` é `SET turma_id` dos convertidos, nada toca `alunos_turma`, `aula`, `aula_aluno` nem `pagamento`; (b) ler o diff de `EncerramentoTurmaPacoteService.cs` e `grep -n "SaldoAulas\|Valor\|TotalAulasPacote" ` nele: nenhuma atribuição; (c) ler o diff de `AlunoService.cs` e `TurmaService.DesvincularAlunoAsync`/`ReativarAsync` (esperado: sem diff); (d) comparar com as contagens da T001/T017.
- [X] T029 FR-001/FR-004/FR-005/FR-022 ("MUST NOT" datas em turma de mensalidade; entrada em turma de pacote por matrícula direta ou depois do início; troca de tipo ou de turma do pacote; dois pacotes ativos do mesmo grupo): conferir que os testes das T011, T012 e T023 cobrem cada recusa e que os `CHECK` do SQL cobrem as datas, lendo o diff do validador, do `TurmaService` e do `VinculoCobrancaService`.
- [X] T030 `git status --porcelain -- src tests frontend database`: arquivos novos esperados só `database/25_turmas_de_pacote.sql`, `TipoTurma.cs`, `EncerramentoTurmaPacoteService.cs` (e sua interface), `TurmasOptions.cs`, `TurmaEncerramentoDispatcherService.cs` e os seis arquivos de teste novos (`AulaServicePacoteGrupoTests`, `AulaServiceConflitoAlunoTests`, `TurmaServiceTipoTests`, `VinculoCobrancaServiceTurmaPacoteTests`, `EncerramentoTurmaPacoteServiceTests`, `AlunoServicePacoteIntactoTests`); nenhum arquivo novo de frontend. Qualquer outro é achado.
- [X] T031 Rodar `dotnet build` e `dotnet test` (suíte completa), `npx tsc --noEmit`, `npx eslint` nos arquivos tocados, `npm run verificar-contraste` e `npm run build` no frontend.
- [X] T032 **(feito em 2026-10-31 com o login de teste que o usuário passou: Fase 1 = 44 PASS, Fase 2 = 5 PASS, 0 FAIL)** Validar o quickstart.md, cenários 1 a 39, com a API local (SMTP inválido; `Turmas:IntervaloEncerramentoMinutos` pequeno só para o cenário 32; dados "TESTE-063" apagados ao final; valor do intervalo restaurado).
- [X] T033 Relatório em `docs/relatorios/<data>-pacote-fora-da-turma.md` e comentário no card do Trello com o resumo, os testes alterados (T007), as decisões menores a confirmar e os casos listados pela migração.
- [X] T034 (feito em 2026-10-31 de forma automatizada, Chrome headless + capturas: 16 PASS, 0 FAIL; a conferência humana continua bem-vinda) Percorrer o "Frontend" do quickstart.md num navegador real (turma de pacote, vínculo com mais de um pacote, página da aula com "Outros alunos"; claro/escuro; 360px).

## Dependências e ordem

- Phase 1 bloqueia tudo: T001 antes da T017; T003 antes das tarefas de código; T004 antes da T009 e da T018.
- **US1** (T005 a T008) só depende da fundação. T007 depois da T005.
- **US2** (T009 a T015): T009 e T010 antes dos testes T011 a T013; T014 e T015 dependem dos contratos (não do código pronto).
- **US3** (T016) depende só do contrato atual da API (`reposicao` já existe); pode rodar em paralelo com US1 e US2.
- **US4** (T017) depende da T001 e da T002.
- **US5** (T018 a T023): T018 antes da T019 e da T020; T021 depois da T010; T022 depois da T018; T023 depois da T012 e da T021.
- **Polish** (T024 a T034) depois de tudo, com a T034 por último.

## Execução em paralelo (exemplos)

- Fundação: T003 e T004.
- US1: T006 enquanto se escreve a T008.
- US2: T011, T012 e T013 (arquivos de teste diferentes) depois de T009 e T010.
- US2 e US3 e US1 em paralelo entre si (código diferente), exceto nos arquivos compartilhados: `VinculoCobrancaService.cs` (T010, T021) e `TurmaService.cs` (T009, T020) devem ser feitos em sequência.

## Estratégia

MVP = Phase 1 + US1 + US2 (a regra de cobrança por grupo e a turma fechada). Depois US3 (tela da chamada), US5 (encerramento e reativação) e, por último, US4 (conversão dos dados de teste). Entregar por fases, com `dotnet test` verde a cada uma e commit só quando o usuário pedir.

## Achados da validação visual (2026-10-31) — corrigidos

- [X] T035 `VinculoCobrancaService.MarcarConflitosHerdados`: o aviso "Mais de um pacote ativo" da ficha do aluno (herdado da spec 056) passou a valer só para mais de um pacote ativo do **mesmo grupo** (a mesma turma, ou vários de aulas individuais); pacotes de turmas diferentes e o individual **não** são conflito (FR-022). Texto do aviso em `VinculosCobrancaSection.tsx` ajustado. Testes em `VinculoCobrancaServiceConflitoTests.cs`.
- [X] T036 `TurmaResponse.AlunosComPacoteForaDaTurma` (calculado no servidor: pacote ativo dela, aluno ativo, sem matrícula) e a página da turma de pacote passou a oferecer para "voltar" só esse grupo, em vez de todos os alunos ativos (FR-004, FR-017). Testes em `TurmaServiceTipoTests.cs`.
