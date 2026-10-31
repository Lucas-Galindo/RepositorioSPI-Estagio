# Tasks: Chamada Só nos Detalhes da Aula e Aviso de Aula Sem Frequência

**Input**: `specs/064-chamada-so-nos-detalhes/` (spec.md, plan.md, research.md, data-model.md, contracts/, quickstart.md)
**Prerequisites**: spec 065 já implementada (o 409 da chamada sai pelo `ErrosDeNegocioFilter`; **não** se acrescenta `try/catch` em controller). A migração `26` depende de `01`–`23` e `25`; a `24` (spec 062) não é pré-requisito.

**Formato**: `- [ ] TNNN [P] [USn] descrição com caminho`. `[P]` = pode rodar em paralelo (arquivos diferentes, sem dependência de tarefa incompleta). Regras do `CLAUDE.md`: cada "MUST NOT" tem tarefa de verificação (`git status --porcelain` para "sem arquivo novo indevido"; **leitura do diff** para "MUST NOT" sobre lógica de código).

**Segurança dos dados**: só o banco **local**; backup antes da migração e de qualquer escrita de teste; dados sintéticos "TESTE-064" apagados no fim; os dados do usuário (turma 15, vínculos 20/21/22/34, 1 aluno) **não** podem ser tocados; nenhum e-mail sai (`Smtp__SmtpHost=smtp.invalido.localhost`); a senha de teste só vai por variável de ambiente (`SPI_TESTE_SENHA`), nunca em arquivo, relatório ou comentário do Trello; nada em produção.

## Phase 1: Linha de base (antes de qualquer mudança)

- [ ] T001 Rodar `database/backup/backup-database.ps1`; guardar a linha de base: `dotnet build` (0 erros e a contagem de avisos), `dotnet test` (469 aprovados), `cd frontend && npx tsc --noEmit` e `npx eslint` limpos, `git status --porcelain` (anotar o que já está sem commit das specs 055–065, para distinguir depois o que é desta spec).
- [ ] T002 [P] Reaproveitar (scratchpad, fora do repositório) o instantâneo da API e as capturas de tela da spec 065 como rede de segurança das telas e rotas que **não** devem mudar: rodar o "antes" com dados "TESTE-064" (se os scripts não existirem mais, recriá-los: 89 chamadas normalizadas e 34 telas por hash). Esperado depois: só diferem o formulário de agendar, os detalhes da aula e o sino.

**Checkpoint**: linha de base, instantâneo e capturas "antes" guardados.

## Phase 2: Fundação (bloqueia todas as histórias)

- [ ] T003 Criar `database/26_aula_frequencia_registrada_em.sql` conforme data-model.md: `ALTER TABLE aula ADD COLUMN frequencia_registrada_em DATETIME NULL COMMENT '…' AFTER ativo;`. Cabeçalho no padrão da `25_*.sql` (objetivo, segurança "não apaga nada e não preenche retroativamente", pré-requisito, "fazer backup antes"). **Sem** `UPDATE`, `DELETE`, valor padrão ou constraint (FR-015).
- [ ] T004 Aplicar a `26` no banco **local** pelo `mysql` CLI (credencial dos User Secrets, nunca em arquivo), depois do backup da T001; conferir `DESCRIBE aula` e `SELECT COUNT(*) FROM aula WHERE frequencia_registrada_em IS NOT NULL` = **0** (coluna não preenchida retroativamente).
- [ ] T005 [P] `src/SPI.Domain/Entities/Aula.cs`: acrescentar `public DateTime? FrequenciaRegistradaEm { get; set; }`; `src/SPI.Infrastructure/Persistence/Configurations/AulaConfiguration.cs`: mapear `frequencia_registrada_em`.
- [ ] T006 [P] Criar `src/SPI.Application/Common/RelogioExtensions.cs` com `AgoraEmSaoPaulo(this TimeProvider)` (research.md R1): converte `GetUtcNow()` para `America/Sao_Paulo`, tenta o id IANA e depois `E. South America Standard Time`, e **lança erro explícito** se nenhum resolver; devolve `DateTime` de parede (`Kind` não especificado). Registrar `services.AddSingleton(TimeProvider.System)` em `src/SPI.Application/DependencyInjection/ApplicationServiceCollectionExtensions.cs` (conferir que não há outro registro).
- [ ] T007 [P] Criar `src/SPI.Application/Aulas/Services/RegraChamada.cs` (estático e puro, como `RegraCobrancaPresenca`): `InicioDaAula(Aula)`, `Liberada(Aula, DateTime agora)` (aula **ativa**, `Status == "Agendada"` e `agora >= DataInicio + HoraInicio`, comparação **conjunta** e com `>=`), `SegundosParaLiberar(Aula, DateTime agora)` (`null` se liberada ou não se aplica) e `MensagemBloqueio(Aula)` = `"A chamada ficara disponivel a partir das HH:mm de dd/MM/yyyy."`. Comentar a regra (porquê) no estilo do projeto.
- [ ] T008 [P] Criar `tests/SPI.Application.Tests/Aulas/RelogioFalso.cs` (`TimeProvider` que sobrescreve `GetUtcNow`, com o instante em hora de São Paulo informado no construtor) e `tests/SPI.Application.Tests/Aulas/RegraChamadaTests.cs`: hoje antes do início → bloqueada; hoje já iniciada → liberada; data futura → bloqueada; data passada → liberada; em andamento → liberada; **minuto exato do início** → liberada e um segundo antes → bloqueada; início movido para o futuro → bloqueada de novo; Realizada/Cancelada/excluída → nunca liberada; `SegundosParaLiberar`; texto da mensagem; teste do helper `AgoraEmSaoPaulo` (UTC 15:00 → 12:00 em SP, inclusive virada de dia: 02:30Z → 23:30 do dia anterior).
- [ ] T009 `src/SPI.Application/Aulas/Services/AulaService.cs`: receber `TimeProvider` no construtor (campo `_relogio`). Atualizar **todas** as construções em teste para não quebrar a compilação: `AulaServiceCadastroFakes.cs:155`, `AulaServiceCobrancaPorAlunoTests.cs:57`, `AulaServiceGerarContasAReceberAvulsaTests.cs:53`, `…MultiplosAlunosTests.cs:53` e `…SemVinculoTests.cs:70`, passando um `RelogioFalso` **fixo em 2100-01-01 12:00** por padrão (depois de qualquer data usada nos testes existentes, para os testes que registram chamada com data futura continuarem válidos sem mudar nenhum `Assert`; testes que verificam o bloqueio passam o próprio relógio). **Deixar comentário** na `RelogioFalso` e nos fakes explicando que esse padrão faz toda aula antiga "já ter começado" de propósito, e que o bloqueio só é testado em `RegraChamadaTests` e `AulaServiceChamadaTests` (achado A4 da análise).

**Checkpoint**: coluna criada no banco local, entidade mapeada, relógio e `RegraChamada` testados, `dotnet build` e os 469 testes ainda verdes.

## Phase 3: User Story 1 — Agendar aula sem lançar presença (P1)

**Meta**: o formulário só agenda; toda aula nasce "Agendada", em qualquer data, sem frequência nem cobrança.
**Teste independente**: agendar uma aula para hoje às 23:00 e outra para ontem; o formulário não mostra "Presença" nos dois casos, o botão é "Agendar aula" e as duas ficam "Agendada", sem conta a receber.

- [ ] T010 [US1] Reescrever `tests/SPI.Application.Tests/Aulas/AulaServiceCadastrarRetroativoTests.cs` com `git mv` para `AulaServiceCadastrarSemPresencaTests.cs` (renomear classe): cadastrar com data de hoje, de ontem e futura → `Status == "Agendada"`, nenhum `AulaAluno.Presente` definido, nenhuma `Frequencia` de aluno alterada, nenhum `Pagamento` criado, `FrequenciaRegistradaEm == null`, e o recálculo de lembretes da turma chamado **sempre** (inclusive data passada). Incluir também o caso do edge case da spec: aula criada com **data passada num horário que já tem outra aula da professora** → `ConflitoException` ("Ja existe uma aula cadastrada nesse horario."), ou seja, a aula antiga continua sujeita ao conflito de horário (achado A2 da análise). As expectativas mudam porque a regra mudou (FR-002); registrar isso num comentário do arquivo apontando para a spec 064.
- [ ] T011 [US1] `src/SPI.Application/Aulas/Dtos/AulaRequest.cs`: remover `Presencas` (e o comentário); `AulaService.CadastrarAsync`: remover `retroativa`, a exceção "Informe a presenca…" e a chamada a `AplicarPresencasAsync`; recalcular os lembretes da turma sempre; devolver `Avisos` vazio. Atualizar o comentário de `AplicarPresencasAsync` (agora só `RegistrarSessaoAsync` o chama). Conferir que nenhum outro código/teste referencia `AulaRequest.Presencas` (`grep`).
- [ ] T012 [P] [US1] `frontend/lib/api/aulas.ts`: remover `presencas` de `AulaRequest`. `frontend/components/aulas/AulaForm.tsx`: remover `retroativa`, o estado `presencas`, `alunosDaAula`, a seção "Presença" com o texto "A data já chegou…", o import de `PresencaChecklist` e de `dataIsoLocal`, e os textos "Registrar aula realizada"/"Aula registrada como realizada"; botão sempre "Agendar aula" (ou "Salvar alterações" ao editar); toast simples. Remover `frontend/components/aulas/PresencaChecklist.tsx` (`git rm`; só o `AulaForm` o usava — confirmar por `grep`).
- [ ] T013 [US1] **Verificação dos "MUST NOT" FR-001 e FR-002**: (a) ler o `git diff` de `AulaForm.tsx` e `AulaService.CadastrarAsync` e confirmar que não sobrou lógica de presença/retroativa nem nova comparação de data; (b) `grep -rn "A data já chegou\|Registrar aula realizada\|retroativ" frontend/components frontend/app frontend/lib src` sem resultado de código; (c) `git status --porcelain -- frontend/components` mostra **só** `PresencaChecklist.tsx` removido e `AulaForm.tsx` alterado, nenhum arquivo novo.

**Checkpoint**: US1 funcional; `dotnet test` verde; `npx tsc --noEmit` limpo.

## Phase 4: User Story 2 — Chamada só nos detalhes, depois do início (P1)

**Meta**: o servidor só aceita a chamada a partir do início da aula; os detalhes mostram bloqueio ou chamada conforme o servidor.
**Teste independente**: detalhes de uma aula de hoje às 23:00 → bloqueada com a mensagem; de ontem → registra; registrar a de 23:00 direto pela API → 409.

- [ ] T014 [US2] Criar `tests/SPI.Application.Tests/Aulas/AulaServiceChamadaTests.cs` (relógio fixo): chamada antes do início → `ConflitoException` com a mensagem de `RegraChamada.MensagemBloqueio`, **status continua Agendada, nenhuma presença/frequência/conta/`FrequenciaRegistradaEm` gravada**; no minuto exato do início → registra; aula antiga → registra e vira Realizada; segunda chamada na mesma aula → 409 (status); aula **excluída** (`Ativo = false`) → `NaoEncontradoException`; `FrequenciaRegistradaEm` == `AgoraEmSaoPaulo()` do relógio fixo; editar, cancelar e excluir aula **com chamada bloqueada** continuam funcionando (FR-009); `Mapear` devolve `ChamadaLiberada`/`SegundosParaChamada` corretos para os 5 casos de borda; **repetir o bloqueio (409 antes do início) e a liberação para uma aula individual** (sem turma, um único aluno), conforme o edge case "Aula individual: a mesma regra de horário vale" (achado A1 da análise).
- [ ] T015 [US2] `src/SPI.Application/Aulas/Dtos/AulaResponse.cs`: acrescentar `bool ChamadaLiberada` e `int? SegundosParaChamada` (com `summary` curto). `AulaService.Mapear` passa a receber o `agora` (`_relogio.AgoraEmSaoPaulo()`, uma leitura por chamada/lista) e preenche os dois campos **somente** via `RegraChamada`; atualizar todos os pontos que chamam `Mapear` (listar, obter, cadastrar, atualizar, participantes, registrar, cancelar, descrição de aula realizada).
- [ ] T016 [US2] `AulaService.RegistrarSessaoAsync`: na ordem existe/**ativa** (`!Ativo` → `NaoEncontradoException("Aula nao encontrada.")`) → `Status == "Agendada"` (existente) → `!RegraChamada.Liberada` → `ConflitoException(RegraChamada.MensagemBloqueio(aula))` → presenças. `AplicarPresencasAsync`: gravar `aula.FrequenciaRegistradaEm = _relogio.AgoraEmSaoPaulo()` junto com `Status = "Realizada"` (único ponto). **Não** adicionar `try/catch` em `AulasController` (o 409 sai pelo filtro; a ação já declara `ProducesResponseType(409)` — conferir).
- [ ] T017 [P] [US2] `frontend/lib/api/aulas.ts`: acrescentar `chamadaLiberada: boolean` e `segundosParaChamada: number | null` em `Aula`. `frontend/app/(app)/aulas/[id]/page.tsx`: com `status === "Agendada"`, exibir as caixas de presença e o botão "Registrar como realizada" **somente se** `aula.chamadaLiberada`; senão exibir "A chamada ficará disponível a partir das HH:MM de DD/MM/AAAA." montada com `fmtHora(aula.horaInicio)`/`fmtData(aula.dataInicio)`; se `segundosParaChamada` vier preenchido, agendar **um** `setTimeout` (duração + 1 s, com limpeza no `useEffect`) que busca a aula de novo. Manter Editar, Cancelar, Excluir e "Adicionar (reposição)" como estão. O tratamento do 409 já mostra a mensagem do servidor no toast.
- [ ] T018 [US2] **Verificação dos "MUST NOT" FR-004, FR-007 e FR-009**: ler o `git diff` de `RegraChamada.cs` (comparação conjunta data+hora, `>=`, sem `Date` isolada), de `AulaService.cs` e de `aulas/[id]/page.tsx`; `grep -n "new Date\|Date.now\|dataIsoLocal\|getHours" "frontend/app/(app)/aulas/[id]/page.tsx" frontend/lib/api/aulas.ts` **sem resultado**; confirmar que a tela não compara horários e que "Registrar como realizada" depende só de `chamadaLiberada`; o teste da T014 cobre "aula realizada não aceita nova chamada". **FR-003 (chamada somente nos detalhes)**: `grep -rn "registrarSessao" frontend --include=*.ts --include=*.tsx` deve mostrar **um único ponto de uso** (`aulas/[id]/page.tsx`, fora a definição em `lib/api/aulas.ts`); qualquer outro é achado (achado A3 da análise). A mensagem do 409 vem do servidor (sem acento, padrão do backend) e a tela monta o próprio texto com acento: a diferença é intencional (achado A7).

**Checkpoint**: US2 funcional ponta a ponta; `dotnet test` e `tsc` verdes.

## Phase 5: User Story 3 — Aviso de aula antiga sem frequência (P1)

**Meta**: toda aula que já terminou e está sem frequência gera um aviso, sem corte de data, com o botão para os detalhes.
**Teste independente**: com as 2 aulas antigas do banco local (uma anterior a 19/10/2026), as duas aparecem como aviso; registrar a chamada faz o aviso sumir.

- [ ] T019 [US3] Atualizar os fakes para a nova assinatura (sem `desde`): `AulaServiceCadastroFakes.cs:74`, `AulaServiceFakes.cs:43`, `Notificacoes/NotificacaoFakes.cs:16` (remover também `Desde` e `AulasPendentesDesde` na construção do serviço), `Relatorios/RelatorioServiceHistoricoAlunoTests.cs:175` e `VinculosCobranca/VinculoCobrancaFakes.cs:206`. Em `NotificacaoServiceTests.cs:80` o caso "aula anterior ao corte não gera aviso" passa a ser **"aula anterior a 19/10/2026 gera aviso"** (a regra mudou: FR-010; comentar o motivo). Criar `Notificacoes/NotificacaoServiceAulaPendenteSemCorteTests.cs`: aula terminada antiga gera 1 aviso; várias sincronizações não duplicam (FR-010); aula **em andamento** não gera (FR-011); cancelada/excluída não gera e resolve o aviso existente; registrar a frequência, cancelar ou mover o término para o futuro resolve o aviso (FR-013); o texto da mensagem é o novo.
- [ ] T020 [US3] Remover o corte: `src/SPI.Domain/Repositories/IAulaRepository.cs` e `src/SPI.Infrastructure/Repositories/AulaRepository.cs` (`ListarPendentesDeStatusAsync` sem o parâmetro `desde` e sem o filtro `HoraFim >= horaDesde`; manter o comentário sobre comparar data e hora em duas partes); `src/SPI.Application/Notificacoes/Services/NotificacaoService.cs` (chamada sem `desde`; mensagem de `AulaPendente` = "Esta aula já terminou e está sem frequência registrada. Registre a chamada para corrigir."; atualizar o comentário do cabeçalho); `NotificacoesOptions.cs` (remover `AulasPendentesDesde`); `src/SPI.Api/appsettings.json` (remover a chave `Notificacoes:AulasPendentesDesde`).
- [ ] T021 [US3] Usar o relógio de São Paulo no `agora` das notificações: `src/SPI.Api/Controllers/NotificacoesController.cs` (injetar `TimeProvider`; `ListarAtivasAsync` e `MarcarComoLidaAsync` recebem `_relogio.AgoraEmSaoPaulo()` no lugar de `DateTime.Now`) e `src/SPI.Infrastructure/BackgroundServices/NotificacaoDispatcherService.cs` (resolver `TimeProvider` e passar `AgoraEmSaoPaulo()` ao `SincronizarAsync`). **Sem** `try/catch` novo no controller. Os demais `DateTime.Now` do projeto **não** são tocados (research.md R1).
- [ ] T022 [P] [US3] `frontend/components/layout/NotificacoesSino.tsx`: título de `AulaPendente` de "Aula sem status" para "Aula sem frequência". O botão "Ir para a aula" não muda.
- [ ] T023 [US3] **Verificação dos "MUST NOT" FR-010, FR-011 e FR-014**: `grep -rn "AulasPendentesDesde" src tests frontend --include=*.cs --include=*.json --include=*.ts --include=*.tsx` **sem resultado** (specs e relatórios antigos mantêm a menção histórica); ler o `git diff` de `AulaRepository.cs`/`NotificacaoService.cs` confirmando que não entrou nenhum outro filtro de data nem criação de duplicata; os testes da T019 cobrem a unicidade e o "em andamento".

**Checkpoint**: US3 funcional; avisos conferidos no banco local (as 2 aulas antigas passam a aparecer).

## Phase 6: User Story 4 — Auditar e avisar sobre dados inconsistentes (P2)

**Meta**: a data/hora do registro fica guardada e há uma consulta somente de leitura das aulas realizadas antes do início.
**Teste independente**: registrar uma chamada e ver `frequencia_registrada_em` preenchida; rodar a consulta e ver que ela só lista.

- [ ] T024 [US4] Criar `database/consultas/aulas_realizadas_antes_do_inicio.sql`: comentário inicial "SOMENTE LEITURA — não altera nem apaga nada"; `SELECT` das aulas ativas "Realizada" em que `TIMESTAMP(data_inicio, hora_inicio) > NOW()` **ou** `frequencia_registrada_em < TIMESTAMP(data_inicio, hora_inicio)`, com id, data, horários, matéria, turma e `frequencia_registrada_em`; observar no comentário que `NOW()` é o do servidor do banco. Nenhuma instrução de escrita.
- [ ] T025 [US4] Rodar a consulta no banco **local** (somente leitura): esperado 0 linhas hoje (medido na pesquisa); registrar as contagens de `aula`, `aula_aluno` e `pagamento` **antes e depois** (devem ser iguais, SC-006).
- [ ] T026 [US4] **Verificação dos "MUST NOT" FR-015 e FR-016**: `grep -inE "update|delete|insert|alter|drop|truncate" database/consultas/aulas_realizadas_antes_do_inicio.sql` sem instrução de escrita (só o comentário); ler a migração 26 confirmando que **não há `UPDATE`** (coluna não preenchida retroativamente); `SELECT COUNT(*)` de `frequencia_registrada_em IS NOT NULL` só cresce por chamadas feitas depois da migração.

**Checkpoint**: auditoria pronta e conferida.

## Phase 7: Acabamento e validação final

- [ ] T027 [P] Notas de Princípio V (FR-017): em `specs/055-presenca-criacao-aula/spec.md` (FR-001 a FR-007 e a US1 do lançamento retroativo na criação) e em `specs/059-notificacoes-aula-pendente/spec.md` (FR-004, corte de vigência) acrescentar uma nota curta apontando para `specs/064-chamada-so-nos-detalhes/spec.md` como a regra vigente; deixar explícito que a US2 da 055 (editar a descrição de aula realizada) **continua valendo**. Não reescrever o histórico. Conferir com `git diff` que só entraram as notas.
- [ ] T028 **Verificação "sem arquivo novo indevido"**: `git status --porcelain -- src tests frontend database` — arquivos novos **desta spec** esperados: `RelogioExtensions.cs`, `RegraChamada.cs`, `RelogioFalso.cs`, `RegraChamadaTests.cs`, `AulaServiceChamadaTests.cs`, `NotificacaoServiceAulaPendenteSemCorteTests.cs`, `database/26_aula_frequencia_registrada_em.sql` e `database/consultas/aulas_realizadas_antes_do_inicio.sql` (e o `AulaServiceCadastrarSemPresencaTests.cs`, que é renomeação); `PresencaChecklist.tsx` removido. Qualquer outro é achado. Conferir também, por `git diff -- src/SPI.Api/Controllers`, que **nenhum `try/catch`** entrou em controller (só o `NotificacoesController` ganhou o `TimeProvider`).
- [ ] T029 Rodar `dotnet build` (0 erros, sem avisos novos), `dotnet test` (todos verdes; nenhum `Assert` de teste existente alterado além do caso revogado da T019 e da reescrita da T010, conferido por `git diff -- tests`), `cd frontend && npx tsc --noEmit`, `npx eslint` nos arquivos tocados, `npm run verificar-contraste` e `npm run build` (com `experimental.cpus: 2` temporário e `NODE_OPTIONS=--max-old-space-size=1536` se faltar memória, restaurando `next.config.ts` depois).
- [ ] T030 Validar o `quickstart.md` com hora real de São Paulo: API autenticada (itens 1–11 da seção 2, com dados "TESTE-064": aula futura bloqueada → 409; aula iniciada → 200 com `FrequenciaRegistradaEm`; aula excluída → 404; avisos sem duplicar e sem corte) e Chrome (seção 3: formulário sem presença, bloqueio com a mensagem, liberação sozinha na hora, aviso → "Ir para a aula" em ≤ 2 cliques (SC-004 conta a partir do clique no item da notificação no sino: item + "Ir para a aula"; achado A5), relógio do computador alterado não muda a tela). Repetir o instantâneo da API e as capturas da T002: só devem mudar as telas/rotas previstas.
- [ ] T031 Fechamento: medir e escrever `docs/relatorios/<data>-chamada-so-nos-detalhes.md` (o que foi pedido, o que foi feito, resultados dos testes, avisos novos, o que **não** foi feito, cards sugeridos: trocar os demais `DateTime.Now` pelo relógio de SP; migração 26 e consulta em produção pelo usuário); rodar o hook `revisao-codigo` do Trello e comentar no card; apagar os dados "TESTE-064" (com backup) e conferir que os dados do usuário seguem intactos; parar os servidores locais (só os iniciados por mim); atualizar a memória do projeto; **não** commitar nem fazer push sem pedido.

## Dependências e ordem

- Fase 1 → Fase 2 → histórias → Fase 7. Dentro da Fase 2: T003 → T004; T005–T008 em paralelo; T009 depende de T006 (e do build das anteriores).
- **US1** (T010–T013) e **US2** (T014–T018) dependem da Fase 2; US2 depende de T009 (relógio no serviço) e usa `AulaResponse`/`Mapear` (T015) que a US1 não altera, então podem andar em paralelo se os arquivos de `AulaService.cs` forem editados em sequência (T011 → T015 → T016).
- **US3** (T019–T023) depende só de T006 (relógio) e T009 (fakes); independe das US1/US2.
- **US4** (T024–T026) depende da T004 (coluna) e, para ver a coluna preenchida, da T016.
- T027 é independente; T028–T031 são o fechamento, nessa ordem.

## Paralelismo

- Fase 2: T005, T006, T007 e T008 (arquivos diferentes).
- US1: T012 (frontend) em paralelo com T010–T011 (backend/testes).
- US2: T017 (frontend) em paralelo com T014–T016 depois de T015 definir o contrato.
- US3: T022 (frontend) em paralelo com T019–T021.

## Estratégia de implementação

1. **MVP**: Fase 1 + Fase 2 + US2 (a regra do servidor é o coração: sem ela o resto não protege os dados). Entregar com US1 junto, porque o formulário ainda cria aulas "Realizada".
2. Depois US3 (o aviso de correção que revela as aulas antigas) e US4 (auditoria).
3. Acabamento e validação final (Fase 7) só quando as quatro histórias estiverem verdes.

## Cobertura dos requisitos

| FR | Tarefas | FR | Tarefas |
|---|---|---|---|
| FR-001 | T012, T013 | FR-010 | T019, T020, T023 |
| FR-002 | T010, T011, T013 | FR-011 | T019, T023 |
| FR-003 | T014, T017 | FR-012 | T019, T020, T022, T030 |
| FR-004 | T007, T008, T018 | FR-013 | T019 |
| FR-005 | T007, T017 | FR-014 | T020, T023 |
| FR-006 | T014, T016 | FR-015 | T003, T004, T016, T026 |
| FR-007 | T006, T015, T017, T018 | FR-016 | T024, T025, T026 |
| FR-008 | T014, T030 | FR-017 | T027 |
| FR-009 | T014, T018 | | |
