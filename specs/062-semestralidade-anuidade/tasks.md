---

description: "Task list for Semestralidade e Anuidade como Formas de Cobrança do Aluno"
---

# Tasks: Semestralidade e Anuidade como Formas de Cobrança do Aluno

**Input**: Design documents from `specs/062-semestralidade-anuidade/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: incluídos (xUnit com fakes). A feature mexe em dinheiro (contas a receber geradas, cancelamento) e tem vários "MUST NOT" (FR-005, FR-007, FR-009, FR-010, FR-011, FR-012). Pela regra do `CLAUDE.md`, cada um tem tarefa de verificação explícita, por teste e por leitura do diff, e a ausência de arquivo novo indevido é conferida com `git status --porcelain`.

**Organization**: a base (Phase 2) serve todas as histórias. US1 e US2 são P1 e independentes entre si depois da base; US3 e US4 são P2.

## Format: `[ID] [P?] [Story] Description`

---

## Phase 1: Setup

- [ ] T001 Backup do banco local (`database/backup/backup-database.ps1`) antes de qualquer alteração de dados (regra de segurança do usuário).
- [ ] T002 Criar `database/24_semestralidade_anuidade.sql` conforme data-model.md e research.md R2: `data_inicio` e `data_fim` (DATE NULL); trocar `chk_vinculocobranca_modalidade` para aceitar `'Semestral'` e `'Anual'`; novo `chk_vinculocobranca_periodo` (Semestral/Anual exigem as duas datas e `data_fim > data_inicio`; as demais exigem as duas nulas); categorias "Semestralidade" e "Anuidade". Aplicar no MySQL local e conferir com `SHOW CREATE TABLE vinculo_cobranca` e `SELECT * FROM categoria_receita`. Conferir que os vínculos existentes continuam iguais (contagem e soma de `valor`).

---

## Phase 2: Foundational (Blocking Prerequisites)

- [ ] T003 [P] `src/SPI.Domain/Enums/ModalidadeCobranca.cs`: `Semestral = 4` e `Anual = 5`. Criar `ModalidadeCobrancaExtensions.cs` com `EhPlanoPeriodico()` (Mensalidade, Semestral, Anual), `ComPeriodo()` (Semestral, Anual) e `MesesDoPeriodo()` (6 e 12); comentar o porquê (research.md R1).
- [ ] T004 [P] `src/SPI.Domain/Entities/VinculoCobranca.cs`: `DateOnly? DataInicio` e `DateOnly? DataFim`, com comentário. Mapear em `VinculoCobrancaConfiguration.cs` (`data_inicio`, `data_fim`).
- [ ] T005 Repositórios:
  - `IPagamentoRepository` e `PagamentoRepository`: `ListarPorVinculosAsync(IReadOnlyCollection<int>)` (parcelas de vários vínculos numa consulta) e `ListarPendentesFuturosDoVinculoAsync(vinculoId, hoje)` (rastreadas, para cancelar);
  - `IVinculoCobrancaRepository` e `VinculoCobrancaRepository`: `ListarPlanosAtivosAsync()` (Semestral/Anual ativos, com `Aluno`);
  - trocar a mensagem de chave duplicada de `VinculoCobrancaRepository.SalvarAlteracoesAsync` para uma genérica ("Já existe um vínculo ativo ou parcela duplicada para este contexto.");
  - atualizar todos os fakes de teste que implementam as interfaces.
- [ ] T006 [P] DTOs em `src/SPI.Application/VinculosCobranca/Dtos/`: `ParcelaRequest` (`Vencimento`, `Valor`) e `ParcelaResponse` (`PagamentoId`, `Descricao`, `Vencimento`, `Valor`, `Status`); `VinculoCobrancaRequest` ganha `DataInicio` e `Parcelas`; `VinculoCobrancaResponse` ganha `DataInicio`, `DataFim` e `Parcelas`.
- [ ] T007 [P] Testes de `ModalidadeCobrancaExtensions` em `tests/SPI.Application.Tests/VinculosCobranca/ModalidadeCobrancaExtensionsTests.cs` (as três funções, para as cinco modalidades).

**Checkpoint**: banco, domínio e contratos prontos; nada muda ainda para o usuário.

---

## Phase 3: User Story 1 - Cadastrar plano Semestral/Anual à vista ou parcelado (Priority: P1) 🎯 MVP

- [ ] T008 [US1] `VinculoCobrancaRequestValidator.cs`:
  - Semestral/Anual exigem `DataInicio` e de 1 a 24 `Parcelas`;
  - cada parcela com `Valor > 0` (até o limite de `decimal(10,2)`) e vencimentos em **meses diferentes**;
  - as outras modalidades **não** aceitam `DataInicio` nem `Parcelas`;
  - `TurmaId` deve ser nulo também para Semestral/Anual (estender a regra de Mensalidade/Pacote).
- [ ] T009 [US1] `VinculoCobrancaService.cs`: injetar `IPagamentoRepository` e `ICategoriaReceitaRepository`; em `CadastrarAsync` para Semestral/Anual, calcular `DataFim`, criar o vínculo e uma `Pagamento` por parcela (research.md R4: status Pendente, competência = 1º dia do mês do vencimento, categoria do plano, descrição "{Plano} - parcela X/N", navegação para o vínculo) e dar **um único** `SalvarAlteracoesAsync`. Recusar com 409 se a categoria do plano não existir (migração não aplicada). Preencher `Parcelas`, `DataInicio` e `DataFim` nas respostas, com as parcelas dos vínculos da listagem lidas em lote (T005) e o status "Atrasado" calculado como em Contas a Receber.
- [ ] T010 [US1] `GarantirRegrasDeCombinacaoAsync` conforme research.md R6: Semestral/Anual recusados com Mensalidade, Pacote ou Avulsa ativos e com período sobreposto a outro plano; Pacote e Avulsa recusados com plano Semestral/Anual ativo. Mensagens da research.md R6.
- [ ] T011 [P] [US1] Testes em `tests/SPI.Application.Tests/VinculosCobranca/VinculoCobrancaServicePlanosTests.cs`:
  - cadastro à vista gera 1 parcela; Semestral com 6 e Anual com 12 geram 6 e 12, com vencimento, valor, competência, categoria e descrição corretos;
  - `DataFim` do Semestral e do Anual (incl. 31/08 + 6 meses e ano bissexto);
  - soma das parcelas diferente do total é aceita (FR-005);
  - **FR-006 ("tudo ou nada")**: com o fake lançando erro no salvamento, não sobra vínculo nem parcela;
  - **FR-007 ("MUST NOT" duplicar)**: dois cadastros iguais e uma duplicidade de competência não geram parcelas duplicadas;
  - recusas (400/409) de cada caso da tabela da research.md R6, incluindo renovação antecipada aceita e período sobreposto recusado;
  - casos de borda da spec: plano com início no passado (parcelas vencidas nascem "Pendente"), parcela com vencimento antes do início e plano com uma única parcela.
- [ ] T012 [P] [US1] Testes do validador em `VinculoCobrancaRequestValidatorTests.cs`: sem parcela, 25 parcelas, valor ≤ 0, dois vencimentos no mesmo mês, sem início, turma informada, e `DataInicio`/`Parcelas` em modalidade que não é Semestral/Anual.
- [ ] T013 [P] [US1] `frontend/lib/parcelas.ts`: função pura `sugerirParcelas(total, quantidade, primeiroVencimento)` (valor igual com o **resto dos centavos na última parcela**; vencimentos mensais sem ultrapassar o fim do mês, ex.: 31/01 → 28/02), com comentário de que é só conveniência e o backend revalida (research.md R5).
- [ ] T014 [US1] Frontend:
  - `lib/api/vinculosCobranca.ts`: modalidades `Semestral` e `Anual`, `dataInicio`, `dataFim`, `parcelas`;
  - `VinculoCobrancaFormModal.tsx`: ao escolher Semestral/Anual, mostrar início, número de parcelas, valor total, 1º vencimento e um editor das parcelas sugeridas (data e valor de cada uma editáveis), com o aviso da diferença entre a soma e o total (sem bloquear);
  - `VinculosCobrancaSection.tsx`: período, parcelas e status; sem botão "Editar" para esses vínculos; o contexto é "Todas as aulas do aluno";
  - só tokens de tema.

---

## Phase 4: User Story 2 - Presença com plano vigente não cobra por aula (Priority: P1)

- [ ] T015 [US2] `RegraCobrancaPresenca.cs`: receber `DateOnly dataDaAula`; Mensalidade vale sempre, Semestral/Anual só dentro de `DataInicio..DataFim` (research.md R7); renomear `TipoCobrancaPresenca.Mensalidade` para `PlanoPeriodico`. Ajustar `AulaService` (passar `aula.DataInicio`, trocar o `case`) e os testes que citam o nome antigo.
- [ ] T016 [P] [US2] Testes em `RegraCobrancaPresencaTests.cs` e `AulaServiceCobrancaPorAlunoTests.cs`:
  - plano vigente em aula de turma e individual não gera cobrança e grava o vínculo na presença;
  - **FR-009 ("MUST NOT" cobrar por presença no período)**: nenhum pagamento criado nem pacote consumido;
  - aula antes do início e depois do fim ignoram o plano e cobram o valor da aula;
  - o limite do primeiro e do último dia entra no período;
  - defensivo: um plano fora do período é ignorado na decisão, e os outros vínculos ativos do aluno (se existirem por dado antigo) passam a valer;
  - falta continua sem efeito na cobrança.

---

## Phase 5: User Story 3 - Excluir cancelando as parcelas pendentes futuras (Priority: P2)

- [ ] T017 [US3] `VinculoCobrancaService.cs`:
  - `ExcluirAsync` para Semestral/Anual: `Ativo = false` e `Status = "Cancelado"` nas parcelas `Pendente` com vencimento depois de hoje, **num único salvamento**; nunca remover pagamento;
  - `ReativarAsync` e `AtualizarAsync` recusam (409) Semestral/Anual, com as mensagens do contrato;
  - confirmar no `VinculosCobrancaController` o mapeamento dos 409.
- [ ] T018 [P] [US3] Testes em `VinculoCobrancaServicePlanosTests.cs`:
  - exclusão com parcelas pagas, vencidas pendentes e futuras cancela **só** as futuras pendentes;
  - **FR-012 ("MUST NOT" apagar)**: o total de pagamentos antes e depois é o mesmo;
  - a parcela que vence **hoje** não é cancelada, e a que vence amanhã é;
  - exclusão de plano com todas as parcelas pagas apenas o encerra, sem cancelar nada;
  - exclusão idempotente;
  - **FR-011 ("MUST NOT" editar)**: `AtualizarAsync` recusa o vínculo Semestral/Anual e a troca de outra modalidade para Semestral/Anual;
  - reativação recusada.
- [ ] T019 [US3] Frontend: confirmação ao excluir um plano ("As parcelas futuras ainda pendentes serão canceladas; as pagas e as vencidas não mudam."); sem opção de reativar plano excluído.

---

## Phase 6: User Story 4 - Aviso de renovação no sino (Priority: P2)

- [ ] T020 [US4] Backend:
  - `TipoNotificacao.RenovacaoPlano`;
  - `NotificacoesOptions.RenovacaoDiasAntes` (padrão 15) e a chave em `appsettings.json`;
  - em `NotificacaoService.SincronizarAsync`, terceiro laço por aluno (research.md R9: plano de maior `DataFim` por aluno ativo; `DataFim ≤ hoje + dias`), reaproveitando `CriarOuReativarAsync` e o padrão do laço de pagamento;
  - DTO `renovacao` e a mensagem ("termina em" e "terminou em").
- [ ] T021 [P] [US4] Testes em `tests/SPI.Application.Tests/Notificacoes/NotificacaoServiceRenovacaoTests.cs`:
  - aviso aparece a 15 dias do fim e depois do fim; não aparece antes;
  - **FR-014 ("MUST NOT" duplicar)**: sincronizar 3 vezes mantém 1 aviso;
  - renovação cadastrada (início depois do fim) e exclusão do vínculo resolvem;
  - aluno inativo não gera aviso;
  - ajustar os fakes de `NotificacaoFakes.cs`.
- [ ] T022 [US4] Frontend `NotificacoesSino.tsx` e `lib/api/notificacoes.ts`: tipo `RenovacaoPlano` (rótulo "Plano", novo filtro "Planos"), popup com aluno, tipo do plano e data de fim, e atalho para a ficha do aluno (`/alunos/{id}`).

---

## Phase 7: Polish & Cross-Cutting Concerns

- [ ] T023 `MensalidadeLembreteDispatcherService.cs`: assunto "SPI - Lembrete: pagamento vence em breve" e corpo com a descrição do pagamento em vez de "mensalidade" (research.md R10); conferir que os testes existentes passam e acrescentar um teste de uma parcela de plano no lembrete.
- [ ] T024 Notas de substituição (Princípio V) no topo de `specs/037-vinculo-cobranca/spec.md`, `specs/039-job-cobranca-mensalidade/spec.md` e `specs/056-cobranca-por-aluno/spec.md`, dizendo o que mudou em cada uma.
- [ ] T025 FR-007/FR-006 ("MUST NOT" parcela duplicada / gravação parcial): ler o diff de `VinculoCobrancaService.cs` e confirmar que existe um único ponto que cria `Pagamento` a partir de um vínculo, que ele depende de um único `SalvarAlteracoesAsync`, e que a competência vem do mês do vencimento (sem laço de repetição fora das parcelas pedidas).
- [ ] T026 FR-010/FR-011 ("MUST NOT" lógica de cobrança no frontend / edição): ler o diff do frontend e confirmar que ele não decide cobrança por presença nem valida combinação ou vigência (só `sugerirParcelas`, que é conveniência); `grep` confirma que não há botão ou chamada de edição para Semestral/Anual (`PUT`) no frontend.
- [ ] T027 FR-012 ("MUST NOT" apagar): `grep -n "Remove(\|RemoveRange\|ExecuteDelete" src/SPI.Application/VinculosCobranca src/SPI.Infrastructure/Repositories/PagamentoRepository.cs src/SPI.Infrastructure/Repositories/VinculoCobrancaRepository.cs` sem resultado novo.
- [ ] T028 `git status --porcelain -- src tests frontend database specs`: os arquivos novos esperados são só `database/24_...sql`, `ModalidadeCobrancaExtensions.cs`, `ParcelaRequest.cs`, `ParcelaResponse.cs`, `frontend/lib/parcelas.ts`, os arquivos de teste novos e os documentos desta spec.
- [ ] T029 `dotnet build` e `dotnet test` (suíte completa); `npx tsc --noEmit`, `npx eslint` nos arquivos tocados, `npm run build` e `npm run verificar-contraste` em `frontend/`.
- [ ] T030 Validar o [quickstart.md](./quickstart.md), cenários 1 a 17, com a API local (servidor de e-mail inválido, dados "TESTE-062" apagados ao final, vínculos reais conferidos). O cenário 17 cobre o FR-018: uma parcela vencida e pendente aparece em Contas a Receber e no aviso de pagamento pendente (spec 060).
- [ ] T031 (parte manual) Percorrer a seção "Frontend" do quickstart no navegador: cadastro com parcelas editáveis, listagem, exclusão com confirmação, aviso de renovação, claro/escuro e 360px.

---

## Dependencies & Execution Order

- Phase 1 → Phase 2 → US1 e US2 (independentes) → US3 (depende da US1) e US4 (depende da US1) → Polish.
- T009 e T010 dependem de T003 a T006; T011 depende de T009 e T010.
- T015 (renomear o tipo da decisão) mexe em testes existentes: fazer antes dos novos testes da US2.
- O frontend (T013, T014, T019, T022) depende só dos contratos.

## Implementation Strategy

MVP = Phases 1 a 3 (cadastrar o plano e gerar as parcelas) mais a US2 (a aula não cobrar dentro do período), que juntas já entregam o valor de negócio. US3 fecha o ciclo de vida do plano e US4 avisa a renovação. Entregar por história, com commit ao fim do card.
