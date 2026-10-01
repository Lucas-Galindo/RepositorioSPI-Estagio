# Research: Esgotamento de Pacote de Aulas

Decisões técnicas da Phase 0. Não havia nenhum "NEEDS CLARIFICATION" pendente no Technical Context (a única pergunta de produto foi resolvida no spec, sessão 2026-09-23).

## R1. Onde fica a checagem de bloqueio

- **Decision**: dentro de `AulaService.RegistrarSessaoAsync`, no laço que grava a presença de cada aluno, **antes** de `vinculo.Presente = presente` / `Frequencia += 1`. Para cada aluno marcado como presente, chama `_vinculoCobrancaRepository.ObterAtivoPorAlunoEContextoAsync(alunoId, aula.TurmaId)` (método que a 038 já criou); se o vínculo for `Pacote` com `SaldoAulas == 0`, o aluno é tratado como bloqueado.
- **Rationale**: `RegistrarSessaoAsync` é o **único** ponto que grava presença (confirmado por leitura do controller e do frontend: só `POST /api/aulas/{id}/registrar-sessao` faz isso) — dono único da regra (Princípio II). Reaproveita a mesma resolução de contexto (turma × individual) da 038, garantindo que "vínculo do contexto da aula" significa exatamente a mesma coisa nos dois lugares. Fazer a checagem antes da gravação evita ter de "desfazer" frequência.
- **Alternatives considered**: bloquear dentro de `GerarContasAReceberAsync` — descartado: roda depois de a presença e a frequência já terem sido gravadas, teria que reverter. Validator FluentValidation — descartado: precisa de acesso ao repositório e ao contexto da aula, não é validação de formato do request.

## R2. Estado gravado do aluno bloqueado

- **Decision (revisada em 2026-09-23 por decisão do usuário)**: o aluno bloqueado fica com `AulaAluno.Presente = false`, `Aluno.Frequencia` inalterada, nenhum débito de saldo e nenhuma conta a receber (o laço de `GerarContasAReceberAsync` já filtra `Presente == true`, então nada muda ali) — **e o motivo é persistido** numa coluna nova, nullable, `aula_aluno.motivo_nao_registro` (propriedade `AulaAluno.MotivoNaoRegistro`, `string?`). Valor único nesta fatia: `"PacoteEsgotado"` (constante `AulaAluno.MotivoPacoteEsgotado`). `null` = comportamento normal ou falta comum; **só** é preenchido no cenário de bloqueio por pacote esgotado.
- **Rationale**: no histórico da aula, "faltou" e "foi barrado porque o pacote acabou" são situações diferentes para a professora (uma é do aluno, a outra é pendência de renovação/cobrança) e precisam continuar distinguíveis depois do momento do registro, não só no aviso da resposta (R3). Coluna nullable e sem default não altera nenhuma linha existente. `Presente` continua `false` (não `null`) para não quebrar a premissa dos relatórios/validador de que toda presença de aula `Realizada` é `true`/`false`.
- **Integridade**: a coluna só é escrita por `RegistrarSessaoAsync` no momento do bloqueio; como só aulas `Agendada` aceitam registro de sessão, o valor nunca precisa ser limpo. O script de banco adiciona um `CHECK` restringindo os valores permitidos (hoje só `'PacoteEsgotado'`), para que um valor arbitrário nunca entre — ampliar a lista de motivos no futuro exige um script novo, de propósito.
- **Alternatives considered**: `Presente = null` (nunca registrado) — descartado (quebra a premissa acima). Manter o trade-off "indistinguível de falta" e só avisar na resposta — **rejeitado pelo usuário** (versão anterior desta decisão). Enum numérico em vez de string — descartado: string legível no banco e na API, alinhada a `Status` de `Pagamento`/`Aula`, e o `CHECK` cobre a integridade.
- **Impacto em relatórios existentes**: nenhum nesta fatia — `Presente == false` continua contando como não-presente (não altera cálculo de frequência). O dado novo fica disponível para relatórios futuros distinguirem falta de bloqueio.

## R3. Como o aviso chega à professora

- **Decision**: novo campo `List<string> Avisos` (padrão vazio) em `AulaResponse`. `RegistrarSessaoAsync` preenche uma mensagem por aluno bloqueado (ex.: `"Pacote esgotado: <Nome> não teve a presença registrada. Renove o pacote (edite o Vínculo de Cobrança) para liberar novos registros."`). Demais chamadas que devolvem `AulaResponse` mantêm `Avisos` vazio (compatível — campo aditivo).
- **Rationale**: a resposta é 200 OK com a aula `Realizada` (clarificação: o registro da aula nunca é recusado por causa de um aluno esgotado), então o aviso não pode ser um erro HTTP; um campo aditivo em `AulaResponse` é a forma de menor impacto. O frontend (`handleRegistrarSessao`) já recebe o `Aula` e passa a exibir `avisos` além do toast de sucesso.
- **Alternatives considered**: 409 Conflict com a lista — descartado (contradiz a opção A escolhida). Envelope novo `{ aula, avisos }` — descartado: quebraria o contrato existente sem ganho.

## R4. Como o painel da Home recebe os dados

- **Decision**: estender `DashboardResponse` com `PacotesEmAtencao: List<PacoteEmAtencaoResponse>` (`GET /api/dashboard`, já chamado pela Home). `DashboardService` ganha a dependência `IVinculoCobrancaRepository` (já registrada no DI) e um método estático puro `MontarPacotesEmAtencao(IEnumerable<VinculoCobranca>)` que calcula o `Estado` e a ordenação.
- **Rationale**: a Home já busca `obterDashboard` ao carregar, então o painel fica "dinâmico" (sempre o saldo atual, sem cache) sem endpoint, rota, autorização ou chamada nova. O estado e a ordem saem prontos do backend — o frontend não compara saldos (Princípio II).
- **Alternatives considered**: endpoint dedicado `GET /api/vinculos-cobranca/pacotes-em-atencao` — descartado: uma chamada extra na Home e um controller/autorização a mais sem benefício. Calcular o estado no frontend — descartado (Princípio II).

## R5. Consulta e critérios do painel

- **Decision**: **(refinado na etapa de tasks)** sem método de repositório novo: o painel reaproveita `IVinculoCobrancaRepository.ListarAtivosPorModalidadeAsync(Pacote)` (já existente, specs/039), que passa a incluir também `Include(Aluno)`; a regra de elegibilidade `Ativo && Modalidade == Pacote && SaldoAulas != null && SaldoAulas <= LimiteAlertaPacote && Aluno.Ativo` vive **inteira** no método estático puro `DashboardService.MontarPacotesEmAtencao`. `LimiteAlertaPacote = 2` é a constante de domínio.
- **Rationale**: uma única dona da regra, testável sem banco — os filtros negativos (saldo ≥ 3, nulo, inativo, outra modalidade, aluno inativo) ganham teste automatizado (regra do projeto para requisitos "MUST NOT"). Custo aceito: filtra em memória sobre poucos vínculos (single-tenant). `Aluno.Ativo` evita listar aluno já desativado (exclusão lógica — Princípio I). Constante em Domain = dono único do limite; sem campo configurável (Princípio III).
- **Ordenação (no método estático)**: `Esgotado` (saldo 0) primeiro, depois saldo crescente, depois nome do aluno (FR-003).
- **Contexto exibido**: `Turma.Nome` quando há turma, senão `"Atendimento individual"` (mesma convenção de 038/039).

## R6. Migração de banco (revisada: agora existe uma)

- **Decision**: um único script novo, `database/16_aula_aluno_motivo_nao_registro.sql`: `ALTER TABLE aula_aluno ADD COLUMN motivo_nao_registro VARCHAR(30) NULL` + `CONSTRAINT ck_aulaaluno_motivo_nao_registro CHECK (motivo_nao_registro IS NULL OR motivo_nao_registro IN ('PacoteEsgotado'))`, com comentário de coluna explicando o significado de `NULL`. Segue o padrão numerado de `database/` (constituição: mudança de schema vira script versionado, nunca alteração manual). Aplicado via `mysql` CLI durante a implementação.
- **Rationale**: consequência direta de R2 (persistir o motivo). Coluna nullable sem default: seguro e instantâneo em MySQL, nenhuma linha existente muda (todas ficam `NULL` = comportamento normal/falta comum). Nenhum índice novo (a coluna não é usada em filtros nesta fatia). O painel e o bloqueio continuam sem nenhum dado persistido novo — só o registro histórico do motivo.
- **Alternatives considered**: tabela separada de "eventos de bloqueio" — descartado: sobredimensionado para um único atributo 1:1 com `aula_aluno`.

## R7. Estratégia de testes

- **Decision**: xUnit + fakes manuais, seguindo o padrão da sessão.
  - `AulaServiceRegistrarSessaoPacoteEsgotadoTests`: bloqueio individual com os demais processados (FR-005/006) **e `MotivoNaoRegistro == "PacoteEsgotado"` só no aluno bloqueado**, ausência comum aceita **com `MotivoNaoRegistro == null`** (falta comum distinguível de bloqueio), saldo nulo não bloqueia, vínculo de outro contexto não bloqueia, vínculo inativo/`Avulsa`/`Mensalidade` não bloqueiam, todos bloqueados ainda gera `Realizada`, renovação libera (US3), débito com saldo > 0 inalterado (FR-009 — não-regressão explícita).
  - `DashboardPacotesEmAtencaoTests` (método estático puro): saldo 0/1/2 entram, 3+ e nulo não; estado correto; ordenação; contexto "Atendimento individual".
- **Rationale**: FRs "MUST NOT" (FR-007, FR-009, FR-010) exigem cobertura explícita, não só dos caminhos positivos.

## R8. Superação de comportamento da spec 038 (Princípio V)

- **Decision**: a 038 aceitava presença com `SaldoAulas == 0` (só não decrementava abaixo de zero). Esta feature muda isso deliberadamente. Impacto concreto: em `AulaServiceGerarContasAReceberPacoteTests`, o teste `Pacote_com_saldo_zero_nao_gera_conta_e_nao_fica_negativo` e a terceira presença de `Sequencia_de_tres_presencas_decresce_ate_zero_sem_gerar_conta` passam a exercitar uma presença **bloqueada**; os testes são ajustados para o novo contrato (a garantia "nunca negativo / sem conta" continua, agora por bloqueio) e a spec 038 recebe uma nota de superação apontando para a 041.
- **Rationale**: Princípio V exige atualizar ou referenciar a spec anterior em vez de deixá-la silenciosamente desatualizada.

## R9. Frontend

- **Decision**: componente novo `PacotesEmAtencaoPanel` recebe a lista pronta e renderiza dois estilos (Atenção / Esgotado) usando os tokens/estilos existentes da Home; cada linha linka para `/alunos/{alunoId}` (onde a edição do vínculo, specs/037, já existe) para facilitar a renovação. Mensagem vazia clara quando a lista é vazia (FR-004). A tela `aulas/[id]` exibe `avisos` do retorno de `registrarSessao` (o toast atual só tem uma mensagem de sucesso) **e** mostra, na lista de presenças da aula (também ao reabrir a aula depois, é o "histórico"), um selo "Pacote esgotado" para o aluno cujo `motivoNaoRegistro` vier preenchido — o frontend só exibe o rótulo do motivo recebido, sem inferir nada.
- **Rationale**: nenhum cálculo no frontend; só apresentação. Sem rota nova.
