# Feature Specification: Auditoria de Aulas Dadas por Vínculo de Cobrança

**Feature Branch**: `052-auditoria-vinculo-cobranca`

**Created**: 2026-10-12

**Status**: Draft

**Input**: User description: "Reformular a regra de negócio de VinculoCobranca para dar suporte a auditoria histórica completa de 'quantas aulas já foram dadas' por vínculo, tanto para Modalidade Pacote quanto Mensalidade, como pré-requisito para uma feature futura de visualização (dashboard/relatórios). 1) VinculoCobranca.TotalAulasPacote (novo campo, só Pacote): hoje só existe SaldoAulas, decrementado a cada presença — não existe nenhum campo que guarde o tamanho original do pacote. Adicionar TotalAulasPacote, gravado na criação, nunca decrementado; aulas dadas = TotalAulasPacote - SaldoAulas. 2) AulaAluno.VinculoCobrancaId (novo campo, FK): hoje o vínculo ativo é resolvido só no momento da presença, sem ficar registrado qual vínculo específico foi afetado — impede auditoria confiável se o aluno trocar de pacote ao longo do tempo. Preencher para todas as modalidades com vínculo correspondente (Avulsa, Mensalidade, Pacote), no mesmo ponto em que o vínculo já é resolvido hoje (AulaService.GerarContasAReceberAsync). 3) Mensalidade.AulasIncluidas já é opcional — quando definido, é um teto mensal; aulas dadas no mês = contagem de AulaAluno ligados ao vínculo, filtrando pela competência da aula; reseta sozinho por mês, sem job novo. 4) Edição bloqueada quando o novo total/teto ficaria menor que o que já foi consumido (ambas as modalidades, mesma regra, mensagem de erro explicando o motivo). Fora de escopo: o job de cobrança automática de Mensalidade (specs/039, já implementado, não mexer), qualquer tela nova de dashboard/relatório (fica para a próxima spec), o lembrete de e-mail de vencimento (já é card no Trello) e qualquer mudança na geração de cobrança por presença além de popular o novo campo de auditoria (specs/038 continua valendo)."

## Nota de investigação prévia

Confirmado por leitura do código atual (grounding feito ao longo da conversa que precedeu esta spec):

- **`VinculoCobranca`** (`src/SPI.Domain/Entities/VinculoCobranca.cs`, specs/037): hoje tem `AulasIncluidas` (só Mensalidade) e `SaldoAulas` (só Pacote, decrementado a cada presença por `AulaService.GerarContasAReceberAsync`, specs/038). **Não existe nenhum campo que guarde o tamanho original de um Pacote** — `VinculoCobrancaRequestValidator.cs` restringe `AulasIncluidas` à Mensalidade e `SaldoAulas` ao Pacote (nunca os dois ao mesmo tempo), então o número digitado na criação de um Pacote vira diretamente `SaldoAulas` e começa a cair a partir da primeira presença, sem deixar rastro do valor inicial.
- **`AulaService.GerarContasAReceberAsync`** (`AulaService.cs`, linhas ~219-274): a cada presença confirmada, resolve o `VinculoCobranca` ativo do aluno para o contexto da aula (`ObterAtivoPorAlunoEContextoAsync`) e decide o efeito por `Modalidade` — hoje esse vínculo resolvido **não fica gravado em lugar nenhum** além do efeito que produz (gerar `Pagamento` ou decrementar `SaldoAulas`). Se o mesmo aluno tiver, ao longo do tempo, mais de um vínculo Pacote para a mesma turma (um esgotado substituído por um novo), não há como reconstruir depois quais presenças específicas pertenceram a qual vínculo.
- **`AulaAluno`** (`src/SPI.Domain/Entities/AulaAluno.cs`): hoje tem `AulaId`, `AlunoId`, `Presente`, `MotivoNaoRegistro` (specs/041, só valor hoje é `"PacoteEsgotado"`). Não tem nenhuma referência a `VinculoCobranca`.
- **`specs/039-job-cobranca-mensalidade`** (já implementada, testada e com EX-001 formalmente fechada em `specs/037-vinculo-cobranca/plan.md`): o job `MensalidadeDispatcherService` já gera automaticamente, no fim de cada mês, a cobrança cheia (`Pagamento`) de cada vínculo Mensalidade ativo, pelo `Valor` do vínculo — **independentemente de quantas aulas foram dadas**. Essa fatia não altera esse job; `AulasIncluidas` continua sendo só um teto informativo de frequência, nunca usado para decidir o valor cobrado.
- **`Pagamento.VinculoCobrancaId`** (specs/039): já existe o precedente de um campo de auditoria FK de `Pagamento` para `VinculoCobranca`, usado para idempotência do job mensal — esta spec aplica o mesmo princípio a `AulaAluno`, mas para rastrear presenças em vez de cobranças.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Saber quantas aulas de um Pacote já foram dadas, sempre (Priority: P1)

Como professora, quero que o sistema guarde o tamanho original de cada Pacote que cadastro, pra que eu sempre consiga saber quantas aulas já dei e quantas faltam — mesmo depois de o pacote já estar parcialmente consumido —, sem precisar lembrar de cabeça quantas aulas ele tinha no início.

**Why this priority**: É a lacuna de dado mais básica — sem o total persistido, nenhuma conta de "dado/falta" é possível, para nenhuma das telas futuras que vão consumir essa informação.

**Independent Test**: Cadastrar um Vínculo de Cobrança Pacote com 10 aulas, registrar presença em 3 aulas, e confirmar que o sistema ainda sabe que o total original era 10 (não só que restam 7).

**Acceptance Scenarios**:

1. **Given** um Vínculo de Cobrança Pacote recém-cadastrado com 10 aulas, **When** a professora consulta esse vínculo, **Then** o sistema mostra tanto o total original (10) quanto o saldo atual (10, ainda não consumido).
2. **Given** um Vínculo de Cobrança Pacote com total original de 10 aulas, **When** 3 presenças já foram registradas para ele, **Then** o total original continua sendo 10 (nunca decrementado) e o saldo é 7 — "aulas dadas" é calculável como 10 − 7 = 3.

---

### User Story 2 - Rastrear exatamente quais presenças pertencem a qual vínculo (Priority: P1)

Como professora, quero que cada presença confirmada fique permanentemente associada ao Vínculo de Cobrança que ela afetou no momento em que aconteceu, pra que eu consiga reconstruir o histórico de aulas de um aluno com precisão, mesmo que esse aluno troque de pacote (ou de mensalidade) ao longo do tempo na mesma turma.

**Why this priority**: Sem esse rastro, a auditoria histórica é ambígua sempre que há mais de um vínculo ao longo do tempo para o mesmo aluno+turma — o mesmo problema vale tanto para Pacote (quando um se esgota e um novo é cadastrado) quanto para Mensalidade (quando o teto mensal precisa ser contado por competência).

**Independent Test**: Cadastrar um Vínculo de Cobrança (qualquer modalidade com efeito), registrar uma presença confirmada para esse contexto, e confirmar que a presença gravada referencia exatamente esse vínculo.

**Acceptance Scenarios**:

1. **Given** um aluno com um Vínculo de Cobrança ativo (Avulsa, Mensalidade ou Pacote) para o contexto de uma aula, **When** a presença dele nessa aula é confirmada, **Then** o registro de presença fica associado ao Id desse vínculo específico.
2. **Given** um aluno sem nenhum Vínculo de Cobrança correspondente ao contexto da aula, **When** a presença dele é confirmada, **Then** o registro de presença não fica associado a nenhum vínculo (continua exatamente como hoje para esse caso).
3. **Given** um aluno que teve um Vínculo de Cobrança Pacote esgotado e substituído por um novo Vínculo de Cobrança Pacote na mesma turma, **When** se consulta o histórico de presenças desse aluno, **Then** as presenças anteriores à troca aparecem associadas ao vínculo antigo, e as posteriores ao vínculo novo — nunca misturadas.
4. **Given** uma presença não confirmada (falta), **When** a sessão da aula é registrada, **Then** nenhuma associação a vínculo é gravada para essa presença (mesma regra de "só presença confirmada dispara efeito" já usada em specs/038).

---

### User Story 3 - Mensalidade com teto mensal opcional e contagem automática por mês (Priority: P2)

Como professora, quero poder, opcionalmente, definir quantas aulas uma Mensalidade cobre por mês (ex.: 100 aulas), e ver quantas já foram dadas nesse mês especificamente — contagem essa que reinicia sozinha a cada novo mês, sem eu precisar fazer nada manualmente.

**Why this priority**: Fecha a paridade entre Pacote e Mensalidade para a visualização futura, mas depende das User Stories 1 e 2 já existirem (o cálculo mensal usa a mesma associação de presença a vínculo).

**Independent Test**: Cadastrar uma Mensalidade com teto de 10 aulas/mês, registrar 4 presenças em um mês, e confirmar que o sistema mostra "4 de 10" nesse mês; no mês seguinte, confirmar que a contagem volta a zero automaticamente.

**Acceptance Scenarios**:

1. **Given** um Vínculo de Cobrança Mensalidade com teto de 10 aulas definido, **When** 4 presenças são confirmadas nesse vínculo dentro do mês corrente, **Then** o sistema consegue informar que 4 das 10 aulas do mês já foram dadas.
2. **Given** o mesmo vínculo do cenário anterior, **When** o mês vira, **Then** a contagem de aulas dadas no novo mês começa em zero, sem nenhuma ação manual da professora (a contagem é sempre filtrada pela competência da aula, nunca acumulada entre meses).
3. **Given** um Vínculo de Cobrança Mensalidade sem nenhum teto definido, **When** presenças são confirmadas nesse vínculo, **Then** o sistema ainda consegue informar quantas foram dadas no mês, mas sem nenhum teto para comparar (a professora escolheu não limitar).

---

### User Story 4 - Impedir reduzir o total/teto abaixo do que já foi consumido (Priority: P2)

Como professora, quero que, se eu tentar editar um Vínculo de Cobrança reduzindo o total de um Pacote (ou o teto mensal de uma Mensalidade) para um valor menor do que já foi dado, o sistema me impeça e explique o motivo — pra eu nunca ficar com um registro inconsistente (saldo negativo, ou mais aulas dadas do que o teto permite).

**Why this priority**: Protege a integridade do dado criado pelas User Stories 1 e 3; é uma regra de validação, não uma funcionalidade nova por si só — por isso depende delas.

**Independent Test**: Com um Pacote de 10 aulas já com 6 dadas, tentar editar o total para 5 e confirmar que a edição é rejeitada com uma mensagem clara; editar para 6 ou mais funciona normalmente.

**Acceptance Scenarios**:

1. **Given** um Vínculo de Cobrança Pacote com total original de 10 e 6 aulas já dadas (saldo 4), **When** a professora tenta editar o total para 5, **Then** a edição é rejeitada com uma mensagem explicando que o novo total não pode ser menor que as aulas já dadas.
2. **Given** o mesmo vínculo do cenário anterior, **When** a professora edita o total para 6 (igual às aulas já dadas) ou mais, **Then** a edição é aceita normalmente — o saldo do vínculo continua sendo o que a professora informar na mesma edição (campo independente, sem recálculo automático pelo sistema; ver Assumptions).
3. **Given** um Vínculo de Cobrança Mensalidade com teto de 10 aulas/mês e 7 aulas já dadas no mês corrente, **When** a professora tenta editar o teto para 5, **Then** a edição é rejeitada com uma mensagem equivalente, referente ao mês corrente.
4. **Given** um Vínculo de Cobrança Mensalidade sem teto definido, **When** a professora edita o vínculo sem definir nenhum teto, **Then** a edição é aceita normalmente (não há o que bloquear, já que não existe limite).

---

### Edge Cases

- O que acontece com um Vínculo de Cobrança Pacote que já existia antes desta feature (criado só com `SaldoAulas`, sem `TotalAulasPacote`)? Ver Assumptions — recebe um valor de migração, mas esse valor pode não refletir o total original real se o pacote já estava parcialmente consumido antes da migração; a professora pode corrigir manualmente editando o vínculo depois.
- O que acontece com presenças confirmadas **antes** desta feature entrar em produção? Permanecem sem nenhuma associação a vínculo (`AulaAluno.VinculoCobrancaId` nulo) — não há tentativa de reconstruir retroativamente qual vínculo elas afetaram, pelo mesmo motivo que motivou esta feature (a informação não existia). A auditoria completa vale a partir da entrada em produção desta fatia.
- O que acontece se o Vínculo de Cobrança correspondente ao contexto da aula estiver excluído (inativo) no momento da presença? Mesma regra já existente em specs/038 (FR-002): é tratado como inexistente, então nenhuma associação é gravada.
- O que acontece se um Pacote tiver `TotalAulasPacote` preenchido mas, por alguma inconsistência de dado legado, `SaldoAulas` nunca foi informado? Segue a mesma regra já estabelecida em specs/038 (saldo nunca informado = tratado como esgotado) — "aulas dadas" não é exibido como uma conta confiável nesse caso, fica para a tela de visualização decidir como sinalizar.
- O que acontece com uma Mensalidade cujo teto nunca foi definido e a professora decide definir um pela primeira vez, já havendo aulas dadas no mês corrente? Mesma regra de bloqueio da User Story 4 — se o teto novo for menor que o que já foi dado nesse mês, a edição é rejeitada.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O sistema MUST persistir, para todo Vínculo de Cobrança de Modalidade Pacote, o total original de aulas do pacote (`TotalAulasPacote`), gravado no momento da criação do vínculo.
- **FR-002**: O sistema MUST NOT decrementar `TotalAulasPacote` automaticamente por nenhuma presença — ao contrário do saldo, o total original é imutável exceto por edição explícita da professora (sujeita a FR-007).
- **FR-003**: O sistema MUST permitir calcular, para qualquer Vínculo de Cobrança Pacote com `TotalAulasPacote` informado, quantas aulas já foram dadas (`TotalAulasPacote` menos o saldo atual).
- **FR-004**: A cada presença confirmada em uma aula com status Realizada, o sistema MUST registrar, na presença, o Id do Vínculo de Cobrança ativo que corresponde ao contexto da aula (mesma regra de correspondência já usada em specs/038 — mesma Turma, ou nenhuma Turma para atendimento individual), para qualquer Modalidade que tiver um vínculo correspondente (Avulsa, Mensalidade ou Pacote).
- **FR-005**: Quando não existir Vínculo de Cobrança correspondente ao contexto da presença (nenhum cadastrado, excluído, ou de outro contexto), o sistema MUST NOT registrar nenhuma associação a vínculo para essa presença — mesmo comportamento de ausência de vínculo já usado em specs/038.
- **FR-006**: Uma presença não confirmada (falta) MUST NOT disparar nenhuma associação a vínculo, para nenhuma modalidade — mesma regra de "só presença confirmada" já estabelecida em specs/038.
- **FR-007**: Quando a professora editar um Vínculo de Cobrança Pacote reduzindo `TotalAulasPacote` para um valor menor que as aulas já dadas (total atual menos saldo atual), o sistema MUST rejeitar a edição com uma mensagem explicando que o novo total não pode ser menor que o que já foi consumido. Fora dessa condição de bloqueio, o sistema MUST NOT recalcular `SaldoAulas` automaticamente a partir do novo `TotalAulasPacote` — `SaldoAulas` continua sendo um campo independente, definido pela professora na mesma edição (ver Assumptions).
- **FR-008**: O sistema MUST permitir calcular, para qualquer Vínculo de Cobrança Mensalidade, quantas aulas foram dadas em uma competência (mês/ano) específica, contando as presenças confirmadas associadas a esse vínculo (FR-004) cuja aula pertença a essa competência.
- **FR-009**: `AulasIncluidas` de um Vínculo de Cobrança Mensalidade MUST continuar opcional — a professora pode definir um teto mensal de aulas ou deixar sem nenhum teto, sem que isso afete a cobrança automática já existente (specs/039).
- **FR-010**: Quando a professora editar um Vínculo de Cobrança Mensalidade definindo ou reduzindo `AulasIncluidas` para um valor menor que as aulas já dadas na competência corrente (FR-008), o sistema MUST rejeitar a edição com uma mensagem explicando o motivo, equivalente à de FR-007.
- **FR-011**: Esta feature MUST NOT alterar o comportamento do job de cobrança automática mensal de Mensalidade (`specs/039-job-cobranca-mensalidade`) — o valor cobrado automaticamente continua sendo sempre o `Valor` cheio do vínculo, independentemente de `AulasIncluidas` estar definido ou de quantas aulas foram dadas no mês.
- **FR-012**: Esta feature MUST NOT alterar o comportamento de geração de cobrança por presença já existente (`specs/038`) além de acrescentar o registro da associação a vínculo (FR-004) — nenhuma mudança em quando ou quanto é cobrado, nem no decremento de `SaldoAulas`.
- **FR-013**: Esta feature MUST NOT implementar nenhuma tela, painel ou endpoint de visualização (dashboard, relatório, ou qualquer tela do cadastro de aluno) — é exclusivamente a base de dados e regra de negócio que uma feature seguinte vai consumir.
- **FR-014**: Esta feature MUST NOT implementar nenhum lembrete, aviso ou notificação por e-mail — inclui o lembrete de vencimento de cobrança 5 dias antes, que permanece registrado como item de backlog separado, fora de escopo aqui.

### Key Entities

- **Vínculo de Cobrança (VinculoCobranca)**: já existente (specs/037, specs/038, specs/039). Ganha um novo atributo, `TotalAulasPacote` (só Modalidade Pacote) — o tamanho original do pacote, imutável por consumo automático, editável pela professora sujeito à regra de não ficar menor que o já consumido (FR-007).
- **Presença (AulaAluno)**: já existente (specs/041). Ganha uma nova referência ao Vínculo de Cobrança que ela afetou no momento em que foi confirmada — preenchida automaticamente, nunca editável manualmente, nula quando não havia vínculo correspondente.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Para 100% dos Vínculos de Cobrança Pacote criados a partir desta feature, é possível determinar, a qualquer momento, exatamente quantas aulas já foram dadas e quantas restam, sem ambiguidade.
- **SC-002**: A partir da entrada em produção desta feature, 100% das presenças confirmadas com Vínculo de Cobrança correspondente ficam permanentemente associadas ao vínculo exato que afetaram — permitindo reconstruir o histórico de um aluno mesmo que ele troque de vínculo ao longo do tempo na mesma turma.
- **SC-003**: Para 100% dos Vínculos de Cobrança Mensalidade com teto mensal definido, é possível determinar, a qualquer momento, quantas aulas já foram dadas na competência corrente, com a contagem reiniciando automaticamente a cada novo mês.
- **SC-004**: 100% das tentativas de reduzir o total de um Pacote ou o teto mensal de uma Mensalidade abaixo do que já foi consumido são rejeitadas, com uma mensagem explicando o motivo.
- **SC-005**: 0 mudanças observáveis no valor ou no momento das cobranças geradas pelo job mensal de Mensalidade (specs/039) e pela geração de cobrança por presença (specs/038) além do novo registro de auditoria.

## Assumptions

- **Migração de dados para Vínculos de Cobrança Pacote já existentes**: como não há como saber com certeza o total original de um pacote que já estava parcialmente consumido antes desta feature, `TotalAulasPacote` dos vínculos Pacote já existentes é preenchido, na migração, com o valor atual de `SaldoAulas` (assumindo nenhum consumo prévio) — um valor de partida razoável, mas que pode subestimar o total real se já havia consumo. A professora pode corrigir manualmente editando o vínculo (sujeito a FR-007 a partir da correção).
- Presenças confirmadas antes desta feature entrar em produção permanecem sem associação a vínculo (`AulaAluno.VinculoCobrancaId` nulo) — sem tentativa de reconstrução retroativa; a auditoria completa vale a partir daqui.
- O cálculo de "aulas dadas na competência" (Mensalidade, FR-008) usa a data da aula (`Aula.DataInicio`), não a data em que a presença foi registrada no sistema — mesma referência de competência já usada pelo job de specs/039.
- Esta feature é majoritariamente de backend (modelo de dados e regra de negócio); nenhuma tela existente precisa mudar além da validação de edição (FR-007/FR-010), que já tem um ponto único de validação (`VinculoCobrancaRequestValidator`/`VinculoCobrancaService`) por onde toda edição de vínculo já passa.
- Fora de escopo, reafirmando o pedido original: o job de cobrança automática de Mensalidade (specs/039, intocado), qualquer tela nova de visualização/dashboard (próxima spec), e o lembrete por e-mail de vencimento (item de backlog separado no Trello).
- **`SaldoAulas` não é recalculado automaticamente quando `TotalAulasPacote` muda (FR-007)**: `SaldoAulas` continua sendo um campo independente do request, exatamente como já funciona hoje (`VinculoCobrancaService.AtualizarAsync` grava o `SaldoAulas` que vier no request, sem derivá-lo de outro campo). Se a professora quiser que o saldo reflita o novo total, ela informa o novo `SaldoAulas` na mesma edição — o sistema só garante que o novo `TotalAulasPacote` não fique menor que o já consumido (FR-007), nunca ajusta `SaldoAulas` por conta própria. Decisão tomada em `/speckit-analyze` (achado F1) para evitar uma mutação implícita não pedida por nenhum FR.
