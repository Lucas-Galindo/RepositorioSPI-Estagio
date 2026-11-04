# Tasks: Link da aula temporário (informado nos detalhes, enviado no lembrete)

**Input**: `specs/067-link-da-aula/` (spec.md, plan.md, research.md, data-model.md, contracts/, quickstart.md)
**Prerequisites**: spec 065 implementada (filtro de erros). **Sem migração e sem mudança em `database/`**. Recomenda-se implementar a spec 064 antes (mesmos arquivos, pontos distintos); se a 067 vier antes, a fundação cria o que faltar do relógio.

**Formato**: `- [ ] TNNN [P] [USn] descrição com caminho`. `[P]` = pode rodar em paralelo. Regras do `CLAUDE.md`: cada "MUST NOT" tem tarefa de verificação (`git status --porcelain` para "sem arquivo novo"; **leitura do diff** para "sem lógica no frontend").

**Segurança**: só o banco **local**; backup antes de qualquer escrita de teste; dados sintéticos "TESTE-067" apagados no fim; dados do usuário (turma 15, vínculos 20/21/22/34, 1 aluno) intactos; nenhum e-mail sai (`Smtp__SmtpHost=smtp.invalido.localhost`); a senha de teste só por variável de ambiente (`SPI_TESTE_SENHA`); **o link de teste nunca é impresso em log nem em relatório com senha de reunião real** (usar endereços fictícios como `https://meet.exemplo.com/teste-067`); nada em produção.

## Phase 1: Linha de base

- [X] T001 Rodar `database/backup/backup-database.ps1`; anotar a linha de base: `dotnet build` (0 erros e a contagem de avisos), `dotnet test tests/SPI.Application.Tests` **sem `--no-build`** (o `.slnx` não compila os testes; todos verdes, anotar o total: 511 na última rodada), `cd frontend && npx tsc --noEmit` e `npx eslint` limpos, `git status --porcelain`, `DESCRIBE aula` e `SELECT COUNT(*) FROM aula` no banco local (para provar depois que o banco não mudou). Capturas "antes" das telas (scripts do scratchpad da spec 065; se não existirem, recriar).

## Phase 2: Fundação (bloqueia as histórias)

- [X] T002 [P] `src/SPI.Application/Common/RelogioExtensions.cs` **já existe** (spec 064, com `AgoraEmSaoPaulo(this TimeProvider)` e o fuso `America/Sao_Paulo` num `Lazy` privado) e o `TimeProvider.System` **já está registrado** no DI. Acrescentar **na mesma classe** um método público para converter a hora de parede de São Paulo em instante absoluto (ex.: `ParaInstanteUtc(DateTime horaDeParede)` → `DateTimeOffset`), reaproveitando o fuso privado (nenhum segundo lugar resolve o fuso). Teste do método em `RegraChamadaTests` ou em arquivo novo de testes do relógio (ex.: fim da aula 16:00 em SP = 19:00Z).
- [X] T003 [P] Criar `src/SPI.Application/Aulas/Services/ILinkAulaTemporario.cs` (`string? Obter(int aulaId)`, `void Definir(int aulaId, string link, DateTime fimDaAula)`, `void Remover(int aulaId)`) e `LinkAulaTemporario.cs` (`ConcurrentDictionary<int, (string Link, DateTimeOffset ExpiraEm)>`, recebe `TimeProvider`; `Obter` devolve `null` e remove se `agora >= ExpiraEm`; limpeza das vencidas a cada `Definir`/`Obter`; **nenhum log, nenhuma exceção contendo o link**). Registrar como **singleton** no `ApplicationServiceCollectionExtensions.cs`.
- [X] T004 [P] Criar `src/SPI.Application/Aulas/Services/RegraLinkAula.cs` (estático e puro): `FimDaAula(Aula)` (`DataInicio` + `HoraFim`) e `AceitaLink(Aula, DateTime agora)` = `Ativo` **e** `Status != "Cancelada"` **e** `agora < FimDaAula`. Comentar a regra (o porquê) no estilo do projeto.
- [X] T005 [P] Criar `src/SPI.Application/Aulas/Dtos/DefinirLinkAulaRequest.cs` (`string Link`) e `src/SPI.Application/Aulas/Validators/DefinirLinkAulaRequestValidator.cs` (FluentValidation, mensagens sem acento): obrigatório; sem espaços em branco; `Uri.TryCreate(..., UriKind.Absolute)` com esquema **somente** `http` ou `https`; máximo **500** caracteres.
- [X] T006 [P] Testes da fundação (`tests/SPI.Application.Tests/Aulas/`): `RelogioFalso.cs` **já existe** (spec 064; reaproveitar, não recriar), `LinkAulaTemporarioTests.cs` (define/obtém; troca; remove; **expira exatamente no fim** e um segundo antes ainda vale; limpeza; fim já passado não guarda), `RegraLinkAulaTests.cs` (ativa/cancelada/excluída/terminada; instante exato do fim), `DefinirLinkAulaRequestValidatorTests.cs` (válidos `http`/`https`; inválidos `ftp://x`, `javascript:alert(1)`, `data:text/html,…`, `abc`, vazio, com espaço, 501 caracteres, 500 caracteres válido).
- [X] T007 `src/SPI.Application/Aulas/Services/AulaService.cs`: receber `ILinkAulaTemporario? links = null` como **último parâmetro opcional** (o `TimeProvider` já foi injetado pela 064), no mesmo padrão adotado para o selo de cobrança da spec 068. Os testes atuais constroem o `AulaService` em cerca de **11 arquivos** (`AulaServiceCadastroFakes`, `…Auditoria…`, `…CobrancaPorAluno…`, `…GerarContasAReceber{Avulsa,Mensalidade,MultiplosAlunos,Pacote,SemVinculo}…`, `…RecalculoLembrete…`, `…RegistrarSessaoPacote{Esgotado,Renovacao}…`): com o parâmetro opcional **nenhum deles muda**. Só os testes novos (`AulaServiceLinkTests`) passam um `LinkAulaTemporario` real (com `RelogioFalso`). Se a spec 068 já tiver entrado, usar **argumentos nomeados** nos testes novos para não depender da ordem dos opcionais.

**Checkpoint**: `dotnet build` e toda a suíte anterior verdes, mais os testes novos da fundação.

## Phase 3: User Story 1 — Informar o link nos detalhes da aula (P1)

**Meta**: informar, trocar e remover o link nos detalhes; ele aparece clicável na tela e no popup.
**Teste independente**: detalhes de aula futura → informar `https://meet.exemplo.com/teste-067` → clicável; trocar; remover; inválido recusado.

- [X] T008 [US1] Criar `tests/SPI.Application.Tests/Aulas/AulaServiceLinkTests.cs`: definir/trocar/remover; `Mapear` devolve `LinkAula` e `AceitaLinkAula` (aula futura aceita; sem link → `null`); 409 em aula cancelada e em aula já terminada; `NaoEncontradoException` em aula excluída e inexistente; remover é idempotente; o link não aparece em nenhuma mensagem de exceção.
- [X] T009 [US1] `src/SPI.Application/Aulas/Dtos/AulaResponse.cs`: acrescentar `string? LinkAula` e `bool AceitaLinkAula` (com `summary` curto). `IAulaService.cs`/`AulaService.cs`: `DefinirLinkAsync(int id, string link)` (404 se não existe/excluída; `ConflitoException("Esta aula nao aceita link: ja terminou ou foi cancelada.")` se `!RegraLinkAula.AceitaLink`; `_links.Definir(id, link, RegraLinkAula.FimDaAula(aula))`; devolve a aula mapeada) e `RemoverLinkAsync(int id)` (404 se não existe/excluída; `_links.Remover`); `MapearEm(aula, agora)` (hoje `private static`, introduzido pela spec 064; `Mapear(aula)` já é de instância e a lista de aulas usa o mesmo `agora`) passa a ser **método de instância** para acessar `_links`, mantendo **uma** leitura do relógio por resposta/lista, e preenche `LinkAula = AceitaLink ? _links.Obter(id) : null` em **toda** resposta de aula, inclusive as listas, porque o popup da Home/Agenda usa a aula já carregada da lista (o link só é **exibido** nos detalhes e no popup, nunca em listas) e `AceitaLinkAula` (uma leitura de relógio por chamada/lista; **todos** os pontos que chamam `Mapear` continuam funcionando).
- [X] T010 [US1] `src/SPI.Api/Controllers/AulasController.cs`: `PUT {id}/link` (valida `DefinirLinkAulaRequest` como as outras ações; 400 com a lista de mensagens) e `DELETE {id}/link`, ambos com `[ProducesResponseType]` de 200, 400 (só o PUT), 404, 409 (só o PUT) e 500, **sem `try/catch`** (o `ErrosDeNegocioFilter` devolve 404/409 pelas declarações).
- [X] T011 [P] [US1] Frontend: `frontend/lib/api/aulas.ts` (campos `linkAula: string | null` e `aceitaLinkAula: boolean` em `Aula`; `definirLinkAula(id, link, token)` e `removerLinkAula(id, token)`); `frontend/app/(app)/aulas/[id]/page.tsx` (bloco "Link da aula": com `linkAula` mostra `<a target="_blank" rel="noopener noreferrer">` e botões "Trocar" e "Remover"; sem link e `aceitaLinkAula` mostra o campo e "Salvar link"; sem link e `!aceitaLinkAula` não mostra nada; aviso fixo "Este link fica guardado só até o fim da aula e se perde se o sistema reiniciar."; erros do servidor no toast como no resto da tela); `frontend/components/aulas/AulaDetalhesPopup.tsx` (mostra o link clicável quando existe; **somente leitura**). Usar só classes existentes; nenhum componente novo.
- [X] T012 [US1] **Verificação dos "MUST NOT" FR-003, FR-009, FR-010 e FR-011**: (a) ler o `git diff` do validador e confirmar que o esquema aceito é **só** `http`/`https` (nenhuma lista com `javascript`, `data`, `ftp`); (b) `grep -rn "LinkAula\|Link\b" src --include=*.cs` nas chamadas de log (`_logger`, `logger?.Log`, `LogWarning/LogInformation/LogError`) e em mensagens de exceção: **sem resultado**; (c) ler o `git diff` de `aulas/[id]/page.tsx` e `AulaDetalhesPopup.tsx`: só repassa o texto digitado e exibe `linkAula`/`aceitaLinkAula`; `grep -n "new Date\|Date.now\|new URL\|startsWith\|https\?://" ` nesses arquivos não mostra validação do endereço nem cálculo de expiração/elegibilidade; (d) o teste de aula sem link e a leitura do diff de `MapearEm` confirmam que nada muda para aulas sem link; (e) **logs**: o link viaja no **corpo** do `PUT` (nunca na URL) e o `UseSerilogRequestLogging` do `Program.cs` registra só método, caminho e status (conferir que nenhum enricher/configuração em `appsettings*.json` inclui o corpo); o `ErrosDeNegocioFilter` agora **registra a exceção completa dos 500** (spec 065, revisão de 2026-11-04), então nenhuma exceção pode ter o link na mensagem (já coberto pelo `grep` do item (b) e pelo teste da T008).

**Checkpoint**: US1 funcional; `dotnet test`, `tsc` e `eslint` verdes.

## Phase 4: User Story 3 — O link some sozinho (P1)

**Meta**: o link não fica para sempre: some no fim da aula, ao cancelar/excluir, e no reinício.
**Teste independente**: aula que termina em poucos minutos → link some; cancelar → some; banco sem rastro.

- [X] T013 [US3] Acrescentar em `AulaServiceLinkTests.cs`: `CancelarAsync` e `ExcluirAsync` removem o link; `AtualizarAsync` que muda data/hora de uma aula com link renova a validade para o novo fim e, se o novo fim já passou, remove; `AtualizarAsync` sem mudar o horário mantém o link; registrar a chamada (`Realizada`) **mantém** o link até o fim; depois do fim `Mapear` devolve `LinkAula = null` e `AceitaLinkAula = false` (relógio fixo).
- [X] T014 [US3] `AulaService.cs`: `CancelarAsync` e `ExcluirAsync` chamam `_links.Remover(id)`; `AtualizarAsync` (aula agendada com link existente) refaz `Definir` com o novo fim ou `Remover` se o novo fim já passou; `RegistrarSessaoAsync` **não** mexe no link. Comentários de regra (porquê) nos pontos tocados.
- [X] T015 [US3] **Verificação dos "MUST NOT" FR-002 e FR-005**: `git status --porcelain -- database` vazio e `git diff --stat -- src/SPI.Domain src/SPI.Infrastructure/Persistence database` vazio; `grep -rn "LinkAula\|link_aula" src/SPI.Domain src/SPI.Infrastructure/Persistence database` **sem resultado**; no banco local `DESCRIBE aula` e `SELECT COUNT(*) FROM aula` **iguais** aos da T001 depois de informar links pela API; ler o `git diff` de `LinkAulaTemporario.cs` confirmando que `Obter` nunca devolve entrada vencida.

**Checkpoint**: US3 coberta; nada persistido.

## Phase 5: User Story 2 — O link vai no lembrete de aula por e-mail (P1)

**Meta**: o e-mail do lembrete inclui o link da próxima aula da turma, se existir.
**Teste independente**: lembrete vencido com a próxima aula com link → corpo com o link; sem link → corpo igual ao atual.

- [X] T016 [US2] Criar `tests/SPI.Application.Tests/Lembretes/LembreteDispatcherLinkTests.cs` (reaproveitando os fakes de `LembreteDispatcherOrcamentoTests`): com link vigente da próxima aula → o corpo enviado contém o link clicável; **sem link → corpo idêntico ao atual (comparação de texto exato)**; link expirado/ausente → sai sem link e sem erro; link com `&`, `<` e aspas → protegido no texto e no `href`; status, tentativas, destinatários e orçamento por ciclo **iguais** aos do teste sem link (FR-008); aula da turma não encontrada → sai sem link. Os testes atuais do dispatcher continuam sem alteração de `Assert`.
- [X] T017 [US2] `src/SPI.Infrastructure/BackgroundServices/LembreteDispatcherService.cs`: `ProcessarLembretesVencidosAsync` ganha o parâmetro **opcional** `ILinkAulaTemporario? links = null`; para cada lembrete que será enviado, **somente quando `links != null`** (o fake `FakeLembreteRepositoryParaOrcamento` dos testes atuais lança `NotImplementedException` em `ObterProximaAulaDaTurmaAsync`, então com `links` nulo essa chamada **não pode acontecer**), `var aula = await lembreteRepository.ObterProximaAulaDaTurmaAsync(lembrete.TurmaId)` e `links.Obter(aula.Id)`; aula nula → sem link; `MontarCorpoEmail(lembrete, link)` acrescenta o parágrafo "Link da aula" **só** quando há link, com `WebUtility.HtmlEncode` (texto e `href`); sem link a string é a atual. `ProcessarCicloAsync` resolve `ILinkAulaTemporario` (singleton) e o repassa. **Não** mexer em seleção, ordem, orçamento, tentativas nem status.
- [X] T018 [US2] **Verificação dos "MUST NOT" FR-007 e FR-008**: ler o `git diff` de `LembreteDispatcherService.cs` e confirmar que **só** entram a busca do link e o parágrafo opcional (filtros, orçamento, tentativas, `Status` e destinatários intactos; nenhuma linha de lógica de envio removida); `git diff -- tests/SPI.Application.Tests/Lembretes` não altera nenhum `Assert` existente.

**Checkpoint**: US2 coberta por testes do corpo.

## Phase 6: Acabamento e validação final

- [X] T019 [P] Nota de Princípio V (FR-012): em `specs/001-lembretes-canal-email/spec.md` acrescentar uma nota curta apontando para `specs/067-link-da-aula/spec.md` sobre o conteúdo **opcional** do e-mail (o link da próxima aula, quando existir). Não reescrever o histórico; conferir com `git diff` que só entrou a nota.
- [X] T020 **Verificação "sem arquivo novo indevido"**: `git status --porcelain -- src tests frontend database` — novos esperados: `ILinkAulaTemporario.cs`, `LinkAulaTemporario.cs`, `RegraLinkAula.cs`, `DefinirLinkAulaRequest.cs`, `DefinirLinkAulaRequestValidator.cs`, os testes novos (`LinkAulaTemporarioTests`, `RegraLinkAulaTests`, `DefinirLinkAulaRequestValidatorTests`, `AulaServiceLinkTests`, `LembreteDispatcherLinkTests`), (`RelogioExtensions.cs` e `RelogioFalso.cs` **não** são novos: já existem desde a spec 064 e só o primeiro é alterado); **nenhum** arquivo em `database/` e **nenhum** componente novo em `frontend/`. Conferir por `git diff -- src/SPI.Api/Controllers` que nenhum `try/catch` entrou em controller.
- [X] T021 Rodar `dotnet build` (0 erros, sem avisos novos), `dotnet test` (tudo verde; nenhum `Assert` existente alterado, conferido por `git diff -- tests`), `cd frontend && npx tsc --noEmit`, `npx eslint` nos arquivos tocados, `npm run verificar-contraste` e `npm run build` (com `experimental.cpus: 2` temporário e `NODE_OPTIONS=--max-old-space-size=1536` se faltar memória, restaurando `next.config.ts`).
- [X] T022 Validar o `quickstart.md`: API autenticada (itens 1–12, com dados "TESTE-067" e endereços fictícios; inclui expiração real no fim de uma aula de poucos minutos — **atenção**: enquanto a spec 064 não estiver implementada, uma aula com data de hoje exige presença e nasce "Realizada" (gera conta a receber de teste, a apagar no fim); ela ainda aceita link até o fim, então o teste vale, ou então implementar a 064 antes — reinício da API e conferência de que o log não contém o link), corpo do e-mail pelos testes (seção 3) e Chrome (seção 4: detalhes, popup, erro, aula terminada sem campo, claro e escuro). Repetir as capturas "antes/depois": só os detalhes e o popup da aula podem mudar. **Feito em 2026-11-04** (login com a conta da professora): API (links, 400/404/409, DELETE idempotente, cancelar, expirar no fim, reinício apaga, log sem o link, banco sem coluna) e Chrome claro/escuro; dados TESTE-067 apagados (backup `spi_db_20261104_093619.sql`). Ajuste: cor do link e do aviso passaram a usar tokens (ilegível no escuro).
- [X] T023 Fechamento: escrever `docs/relatorios/<data>-link-da-aula.md` (pedido, o que foi feito, validação, o que ficou de fora e o risco aceito de perder links no reinício); hook `revisao-codigo` do Trello e comentário no card; apagar os dados "TESTE-067" (com backup) e conferir que os dados do usuário seguem intactos; parar os servidores iniciados por mim; atualizar a memória do projeto; **não** commitar nem fazer push sem pedido.

## Dependências e ordem

- Fase 1 → Fase 2 → histórias → Fase 6. Na Fase 2: T002–T006 em paralelo; T007 depois de T003.
- **US1** (T008–T012) depende da Fase 2; dentro dela T009 → T010; T011 (frontend) em paralelo com T008–T010 depois que o contrato (T009) estiver definido.
- **US3** (T013–T015) usa o `AulaService` da US1 (T009 → T014).
- **US2** (T016–T018) depende só de T003 (armazenamento) e é independente das US1/US3.
- T019 é independente; T020–T023 são o fechamento, nessa ordem.

## Paralelismo

Fase 2: T002, T003, T004, T005, T006. US1: T011 com T008–T010. US2 inteira em paralelo com US1/US3 (outro arquivo).

## Estratégia de implementação

1. **MVP**: Fase 1 + Fase 2 + US1 (informar e ver o link) e a parte de expiração da US3 que já nasce na fundação.
2. Depois US2 (e-mail), que é o objetivo final do pedido, e o restante da US3.
3. Acabamento e validação final só com as três histórias verdes.

## Cobertura dos requisitos

| FR | Tarefas | FR | Tarefas |
|---|---|---|---|
| FR-001 | T008, T009, T010, T011 | FR-007 | T016, T017, T018 |
| FR-002 | T003, T015, T020 | FR-008 | T016, T017, T018 |
| FR-003 | T005, T006, T012 | FR-009 | T003, T012, T022 |
| FR-004 | T004, T006, T009 | FR-010 | T008, T012, T016 |
| FR-005 | T003, T006, T013, T014, T015 | FR-011 | T011, T012 |
| FR-006 | T011 | FR-012 | T019 |

## Cobertura dos critérios de sucesso

| SC | Tarefas |
|---|---|
| SC-001 (0 registros/colunas/arquivos com o link) | T001, T015, T020, T022 |
| SC-002 (e-mail com link vigente; sem link idêntico ao atual) | T016, T018 |
| SC-003 (0 links depois do fim, cancelar/excluir ou reinício) | T006, T013, T014, T022 |
| SC-004 (endereços inválidos recusados no servidor) | T005, T006, T012 |
| SC-005 (no máximo 3 passos para informar) | T011, T022 |
| SC-006 (0 mudanças nas regras de envio) | T016, T018, T021 |
