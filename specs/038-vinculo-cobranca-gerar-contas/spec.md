# Feature Specification: Cobrança Automática por Modalidade do Vínculo

**Feature Branch**: `038-vinculo-cobranca-gerar-contas`

**Created**: 2026-09-22

**Status**: Draft

**Input**: User description: "Modificar AulaService.GerarContasAReceberAsync para verificar a modalidade de cobrança do aluno (via VinculoCobranca) antes de decidir o que fazer a cada presença registrada, em vez de sempre gerar uma cobrança avulsa. Regras: 1) Buscar o VinculoCobranca do aluno que corresponda ao contexto da aula (mesmo TurmaId da aula, ou TurmaId nulo se a aula for individual/particular). 2) Sem vínculo correspondente: comportamento atual inalterado — cobrança avulsa com Aluno.ValorAula. 3) Modalidade Avulsa: mesmo comportamento de hoje, mas usando o Valor do vínculo em vez de Aluno.ValorAula. 4) Modalidade Mensalidade: NÃO gerar cobrança nessa presença (job futuro, fora de escopo). 5) Modalidade Pacote: NÃO gerar cobrança; decrementar SaldoAulas em 1, nunca abaixo de zero. Fora de escopo: job de mensalidade e lógica de aviso/renovação de pacote esgotado."

## Nota de investigação prévia

Confirmado por leitura do código atual (grounding antes de especificar):

- **`AulaService.GerarContasAReceberAsync`** (`AulaService.cs`, chamado por `RegistrarSessaoAsync` logo após a aula virar `Realizada`): hoje itera `aula.AulaAlunos.Where(v => v.Presente == true)` e, para cada presença, cria incondicionalmente um `Pagamento` com `ValorFinal = vinculo.Aluno.ValorAula`, `Status = "Pendente"`, categoria "Aula em turma" ou "Aula particular" conforme `aula.TurmaId`, e vincula o pagamento à aula. Esta é exatamente a fatia que passa a ramificar por modalidade.
- **`VinculoCobranca`** (specs/037, já implementada e commitada): tem `AlunoId`, `TurmaId` (nulo = atendimento individual), `Modalidade` (`Avulsa`/`Mensalidade`/`Pacote`), `Valor`, `SaldoAulas` (opcional, só Pacote), `Ativo`. A unicidade "no máximo um vínculo ATIVO por combinação Aluno+Turma (ou Aluno sem turma)" já é garantida (validação no serviço + índice único no banco), então nunca há ambiguidade sobre qual vínculo usar para um contexto.
- **`IVinculoCobrancaRepository`**: hoje só expõe `ObterPorIdAsync`, `ListarPorAlunoAsync`, `ExisteAtivoAsync` e `AdicionarAsync`/`SalvarAlteracoesAsync` — não há ainda um método de busca "o vínculo ativo deste aluno neste contexto de turma", que esta feature precisará (fica para `/speckit-plan`).
- **Exceção EX-001** (`specs/037-vinculo-cobranca/plan.md`, Complexity Tracking): registrada como exceção conhecida e temporária ao Princípio III da constituição — `VinculoCobranca` foi cadastrado sem nenhum efeito automático, com o compromisso de que "a próxima fatia... deve ser priorizada o quanto antes e implementar o efeito real". Esta é essa próxima fatia. Importante: ela fecha a exceção apenas para as modalidades Avulsa e Pacote — Mensalidade continua sem efeito automático até o job futuro (fora de escopo aqui), então o aviso da interface ("Este vínculo é apenas um cadastro...") não pode ser removido nesta fatia; ver Assumptions.
- **`Aluno.ValorAula`**: continua existindo e sendo a fonte de valor para todo aluno sem vínculo correspondente ao contexto da aula — nada nesta feature altera esse campo ou seu significado para quem não tem `VinculoCobranca`.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Aluno sem vínculo de cobrança continua cobrado exatamente como hoje (Priority: P1)

Como professora, quero que todo aluno que eu ainda não configurei com um Vínculo de Cobrança continue sendo cobrado automaticamente pelo valor geral dele (`Valor da aula`) a cada presença, sem nenhuma mudança de comportamento — essa é a base de segurança sobre a qual as novas modalidades são construídas.

**Why this priority**: É a garantia de não-regressão. Toda a base de alunos já cadastrada hoje não tem `VinculoCobranca` (feature nova) — se esse caminho quebrar, todo o financeiro existente quebra junto.

**Independent Test**: Registrar a sessão de uma aula de um aluno sem nenhum `VinculoCobranca` ativo para o contexto dela (turma ou individual) e confirmar que a conta a receber gerada é idêntica à gerada antes desta feature (mesmo valor, vindo de `Aluno.ValorAula`).

**Acceptance Scenarios**:

1. **Given** um aluno sem nenhum Vínculo de Cobrança cadastrado, **When** uma presença dele é registrada em uma aula (de turma ou individual), **Then** uma conta a receber pendente é gerada com o valor de `Valor da aula` do aluno, exatamente como antes desta feature.
2. **Given** um aluno com Vínculos de Cobrança cadastrados apenas para outras turmas (não a da aula em questão), **When** uma presença dele é registrada nessa aula sem vínculo correspondente, **Then** a conta a receber usa `Valor da aula` do aluno (o contexto não bateu, então é como se não houvesse vínculo).

---

### User Story 2 - Cobrança avulsa usa o valor específico do vínculo (Priority: P2)

Como professora, quero que, quando eu tiver cadastrado um Vínculo de Cobrança com modalidade Avulsa para um aluno em um contexto (turma ou atendimento individual), a cobrança gerada a cada presença use o Valor definido nesse vínculo — que pode ser diferente do valor geral do aluno — em vez do valor geral.

**Why this priority**: É a modalidade mais simples de ativar e a que mais imediatamente aproveita o cadastro feito em specs/037, permitindo cobrar valores diferentes por turma para o mesmo aluno.

**Independent Test**: Cadastrar um Vínculo de Cobrança Avulsa com um Valor diferente de `Aluno.ValorAula` para o contexto de uma aula, registrar uma presença nela e confirmar que a conta a receber gerada usa o Valor do vínculo.

**Acceptance Scenarios**:

1. **Given** um aluno com um Vínculo de Cobrança Avulsa ativo para a turma de uma aula, com Valor diferente do `Valor da aula` geral do aluno, **When** a presença dele nessa aula é registrada, **Then** a conta a receber gerada usa o Valor do vínculo, não o valor geral do aluno.
2. **Given** um aluno com um Vínculo de Cobrança Avulsa ativo para atendimento individual, **When** a presença dele em uma aula individual (sem turma) é registrada, **Then** a conta a receber usa o Valor desse vínculo.

---

### User Story 3 - Pacote consome saldo em vez de gerar cobrança (Priority: P2)

Como professora, quero que, quando um aluno tiver um Vínculo de Cobrança Pacote para o contexto de uma aula, cada presença dele consuma uma aula do saldo do pacote já pago, em vez de gerar uma nova cobrança avulsa — e que o saldo nunca fique negativo.

**Why this priority**: Evita cobrar duas vezes um aluno que já pagou um pacote fechado de aulas, e mede o consumo real do pacote a cada presença.

**Independent Test**: Cadastrar um Vínculo de Cobrança Pacote com Saldo de Aulas igual a 2, registrar três presenças em sequência para esse contexto e confirmar: nenhuma conta a receber é gerada em nenhuma delas, o saldo cai para 1 após a primeira, para 0 após a segunda, e permanece em 0 após a terceira (sem ficar negativo).

**Acceptance Scenarios**:

1. **Given** um aluno com um Vínculo de Cobrança Pacote ativo com Saldo de Aulas igual a 5, **When** uma presença dele nesse contexto é registrada, **Then** nenhuma conta a receber é gerada e o Saldo de Aulas do vínculo passa para 4.
2. **Given** um aluno com um Vínculo de Cobrança Pacote ativo com Saldo de Aulas igual a 0, **When** uma nova presença dele nesse contexto é registrada, **Then** nenhuma conta a receber é gerada e o Saldo de Aulas permanece em 0 (nunca fica negativo).
3. **Given** um aluno com um Vínculo de Cobrança Pacote ativo sem nenhum Saldo de Aulas informado (campo vazio desde o cadastro), **When** uma presença dele nesse contexto é registrada, **Then** nenhuma conta a receber é gerada e o Saldo de Aulas permanece vazio (tratado como já esgotado).

---

### User Story 4 - Mensalidade não gera cobrança avulsa por presença (Priority: P3)

Como professora, quero que, quando um aluno tiver um Vínculo de Cobrança Mensalidade para o contexto de uma aula, nenhuma cobrança avulsa seja gerada a cada presença individual — a cobrança mensal desse aluno será tratada por uma automação futura, fora desta fatia.

**Why this priority**: Evita que um aluno de mensalidade seja cobrado por aula avulsa enquanto o job de cobrança mensal ainda não existe; é a modalidade com o efeito mais simples (nenhuma ação) e a de menor prioridade de negócio imediata, já que a cobrança mensal em si ainda não acontece automaticamente nesta fatia.

**Independent Test**: Cadastrar um Vínculo de Cobrança Mensalidade para o contexto de uma aula, registrar uma presença nela e confirmar que nenhuma conta a receber é gerada para essa presença.

**Acceptance Scenarios**:

1. **Given** um aluno com um Vínculo de Cobrança Mensalidade ativo para o contexto de uma aula, **When** a presença dele nessa aula é registrada, **Then** nenhuma conta a receber é gerada para essa presença, e nenhum outro dado do vínculo (Valor, Aulas Incluídas) é alterado automaticamente.

---

### Edge Cases

- O que acontece se, na mesma aula de turma, alunos diferentes tiverem modalidades diferentes (um sem vínculo, um Avulsa, um Pacote, um Mensalidade)? Cada presença é decidida individualmente, pelo vínculo (ou ausência dele) daquele aluno especificamente — uma mesma aula pode gerar cobrança avulsa para um aluno, decrementar o pacote de outro, e não gerar nada para um terceiro.
- O que acontece se um aluno faltar (presença = não)? Nada muda em relação a hoje — nenhuma cobrança é gerada e nenhum saldo de pacote é decrementado, independentemente da modalidade (só presenças confirmadas disparam qualquer efeito).
- O que acontece se o Vínculo de Cobrança correspondente ao contexto estiver excluído (inativo)? É tratado como se não existisse — cai no comportamento da User Story 1 (cobrança avulsa pelo valor geral do aluno).
- O que acontece se o aluno tiver um Vínculo de Cobrança ativo, mas para um contexto diferente do da aula (ex.: vínculo cadastrado para a Turma A, mas a aula registrada é individual, sem turma)? O contexto não bate, então também cai no comportamento da User Story 1 para essa aula específica.
- O que acontece com o pacote quando o Saldo de Aulas nunca foi informado no cadastro (campo vazio, não zero)? É tratado como já esgotado — mesmo efeito de Saldo de Aulas igual a 0 (nenhuma cobrança, nenhum decremento).
- Esta feature implementa o job de cobrança mensal automática, ou algum aviso/bloqueio/renovação quando um pacote se esgota? Não — ambos ficam fora de escopo, como já declarado pelo usuário; o único efeito de Mensalidade e de Pacote esgotado, nesta fatia, é não gerar cobrança.
- Esta feature altera o cadastro do Vínculo de Cobrança em si (criar, editar, excluir, listar — specs/037) ou o campo `Aluno.ValorAula`? Não — o cadastro continua exatamente como está; `Aluno.ValorAula` continua existindo e sendo usado normalmente para todo aluno sem vínculo correspondente ao contexto.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: A cada presença confirmada (`Presente = sim`) em uma aula com status `Realizada`, o sistema MUST buscar o Vínculo de Cobrança ativo do aluno cujo contexto corresponda exatamente ao da aula (mesma Turma da aula, ou nenhuma Turma quando a aula for individual).
- **FR-002**: Quando não existir Vínculo de Cobrança correspondente ao contexto (nenhum cadastrado, ou o único cadastrado estiver excluído, ou for de outro contexto), o sistema MUST gerar a conta a receber exatamente como faz hoje, usando `Valor da aula` do aluno — nenhuma mudança de comportamento para esse caso.
- **FR-003**: Quando o Vínculo de Cobrança correspondente tiver Modalidade Avulsa, o sistema MUST gerar a conta a receber da mesma forma que hoje (mesma descrição, categoria, data de vencimento e competência), exceto que o valor usado MUST ser o Valor do vínculo, não `Valor da aula` do aluno.
- **FR-004**: Quando o Vínculo de Cobrança correspondente tiver Modalidade Mensalidade, o sistema MUST NOT gerar nenhuma conta a receber para essa presença.
- **FR-005**: Quando o Vínculo de Cobrança correspondente tiver Modalidade Pacote e Saldo de Aulas maior que zero, o sistema MUST NOT gerar nenhuma conta a receber para essa presença, e MUST decrementar o Saldo de Aulas desse vínculo em exatamente 1.
- **FR-006**: Quando o Vínculo de Cobrança correspondente tiver Modalidade Pacote e Saldo de Aulas igual a zero (ou nunca informado), o sistema MUST NOT gerar nenhuma conta a receber para essa presença e MUST NOT decrementar o Saldo de Aulas abaixo de zero (nem alterar o campo, se ele nunca foi informado).
- **FR-007**: Em uma mesma aula com múltiplos alunos presentes, o sistema MUST decidir o comportamento (FR-002 a FR-006) de forma independente para cada aluno, conforme o vínculo (ou ausência dele) específico daquele aluno para o contexto da aula.
- **FR-008**: Um aluno com presença = não (falta) MUST NOT disparar nenhum dos efeitos acima (nem cobrança, nem decremento de saldo), para nenhuma modalidade — mesmo comportamento de hoje.
- **FR-009**: Esta feature MUST NOT implementar nenhuma geração automática de cobrança mensal (job de Mensalidade) — o único efeito de um vínculo Mensalidade nesta fatia é a ausência de cobrança avulsa (FR-004).
- **FR-010**: Esta feature MUST NOT implementar nenhum aviso, bloqueio ou renovação automática quando o Saldo de Aulas de um Pacote se esgota — o único efeito, nesta fatia, é parar de decrementar e não gerar cobrança (FR-006).
- **FR-011**: Esta feature MUST NOT alterar o cadastro do Vínculo de Cobrança em si (criação, edição, listagem, exclusão lógica, reativação — specs/037) nem o significado ou a fonte de `Aluno.ValorAula` para alunos sem vínculo correspondente.

### Key Entities

- **Vínculo de Cobrança (VinculoCobranca)**: já existente (specs/037). Nesta feature, passa a ser efetivamente consultado e, no caso da modalidade Pacote, seu Saldo de Aulas passa a ser alterado automaticamente pelo sistema (antes só era editável manualmente).
- **Aula / Presença**: já existentes. O registro de uma sessão de aula (com a presença de cada aluno) passa a ser o gatilho que consulta o Vínculo de Cobrança de cada aluno presente para decidir o efeito financeiro daquela presença.
- **Conta a Receber (Pagamento)**: já existente. Passa a não ser mais gerada automaticamente para toda presença — só quando não há vínculo correspondente, ou quando ele é Avulsa.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% das contas a receber geradas para alunos sem Vínculo de Cobrança correspondente ao contexto da aula continuam idênticas ao comportamento anterior a esta feature (mesmo valor, mesma categoria, mesma data de vencimento).
- **SC-002**: 100% das contas a receber geradas para presenças de alunos com Vínculo de Cobrança Avulsa usam o Valor cadastrado no vínculo, nunca o valor geral do aluno.
- **SC-003**: 0 contas a receber são geradas automaticamente para presenças de alunos com Vínculo de Cobrança Mensalidade ou Pacote.
- **SC-004**: O Saldo de Aulas de um Vínculo de Cobrança Pacote nunca é observado negativo em nenhum momento, decrescendo em exatamente 1 por presença confirmada até chegar a zero.
- **SC-005**: Uma professora que já cadastrou os Vínculos de Cobrança de seus alunos (specs/037) passa a ver o efeito financeiro correto (cobrança avulsa com valor específico, ou consumo de pacote, ou nenhuma cobrança para mensalidade) já na primeira presença registrada depois desta feature, sem nenhuma ação manual adicional além do cadastro já feito.

## Assumptions

- Só Vínculos de Cobrança ATIVOS (não excluídos logicamente) são considerados na busca por contexto (FR-001) — um vínculo excluído é tratado como inexistente, caindo no comportamento de FR-002. Segue o mesmo princípio de "só ativos contam" já estabelecido em specs/037 (FR-005/006/007 de lá).
- Como a unicidade de "no máximo um Vínculo de Cobrança ativo por combinação Aluno+Turma (ou Aluno sem turma)" já é garantida pelo cadastro (specs/037), nunca há ambiguidade sobre qual vínculo usar para um contexto — no máximo um corresponde.
- Um Vínculo de Cobrança Pacote com Saldo de Aulas nunca informado (campo vazio) é tratado, para efeito desta feature, exatamente como Saldo de Aulas igual a zero: nenhuma cobrança, nenhum decremento, nenhum erro.
- O valor usado para gerar uma conta a receber (seja `Aluno.ValorAula`, seja o Valor do vínculo Avulsa) continua sendo copiado para a conta no momento da geração, como hoje — mudanças posteriores no aluno ou no vínculo não alteram contas já geradas.
- Esta feature é puramente de backend (regra de negócio em `AulaService`); não inclui nenhuma mudança na tela "Vínculos de Cobrança" nem no aviso hoje exibido nela — como a modalidade Mensalidade continua sem efeito automático real até o job futuro, esse aviso não pode ser removido só com esta fatia (fica para quando o job de Mensalidade existir).
- Fora de escopo, reafirmando o pedido original: o job de geração automática de cobrança mensal, e qualquer lógica de aviso, bloqueio ou renovação quando um Pacote se esgota.
