# Feature Specification: Job de Cobrança Automática de Mensalidade

**Feature Branch**: `039-job-cobranca-mensalidade`

**Created**: 2026-09-22

**Status**: Draft

**Input**: User description: "Criar um job em background que gera automaticamente as cobranças de Mensalidade, disparando no fim de cada mês, às 23h (horário local), conforme descrito originalmente: para cada aluno com um VinculoCobranca ativo de Modalidade == Mensalidade, gerar uma Conta a Receber (Pagamento) com valor igual ao Valor do vínculo, Status == 'Pendente', independente de quantas aulas o aluno teve presença naquele mês. Requisitos: 1) mesmo padrão técnico do LembreteDispatcherService; 2) rodar periodicamente mas só executar no último dia do mês, 23h ou depois, sem duplicar disparo no mesmo mês; 3) idempotência — não duplicar cobrança já gerada para o vínculo naquela competência; 4) cada Pagamento registra Competência, Descrição indicando mensalidade, DataVencimento (sugerir e justificar), CategoriaReceitaId apropriada; 5) vínculos excluídos antes do disparo não geram cobrança; 6) configurável via appsettings, padrão de Lembretes:IntervaloVerificacaoSegundos. Fora de escopo: geração para Pacote/Avulsa (specs/038) e tratamento de pacote esgotado."

## Nota de investigação prévia

Confirmado por leitura do código atual (grounding antes de especificar):

- **`LembreteDispatcherService`** (`src/SPI.Infrastructure/BackgroundServices/LembreteDispatcherService.cs`): já implementa o padrão técnico pedido — `BackgroundService` com `PeriodicTimer`, intervalo lido de `IConfiguration` (`Lembretes:IntervaloVerificacaoSegundos`, default 60s), `IServiceScopeFactory.CreateScope()` por execução, `try/catch` por tick que loga e nunca derruba o serviço. Este job de Mensalidade segue exatamente essa estrutura.
- **`VinculoCobranca`** (specs/037) e a ramificação por modalidade em `AulaService.GerarContasAReceberAsync` (specs/038, já implementada e testada): confirmam que Modalidade Mensalidade hoje **não tem nenhum efeito automático** — é exatamente essa lacuna que fecha a exceção EX-001 (`specs/037-vinculo-cobranca/plan.md`), que já está registrada como "parcialmente resolvida... pendente do job de Mensalidade".
- **`Pagamento`**: hoje é gerado só por presença de aula (`AulaService`) ou manualmente (`PagamentoService`), sempre vinculado a uma `Aula` específica via `PagamentoAula`. Uma cobrança de mensalidade não corresponde a uma aula específica — ela cobre o mês inteiro, "independente de quantas aulas o aluno teve presença". Por isso, `PagamentoAula` **não se aplica** a este tipo de conta (ver Assumptions).
- **`CategoriaReceita`**: os únicos valores hoje seedados no banco (`database/10_financeiro_contas.sql`) são "Aula particular", "Aula em turma", "Reposicao" e "Outro" — não existe uma categoria "Mensalidade". Resolvido em Clarifications: esta feature cria essa categoria nova, para que a professora consiga distinguir, nos relatórios financeiros (specs/033-036 trataram extensivamente da precisão desses relatórios), receita de mensalidade fixa de receita por aula avulsa.
- **`Status` de `Pagamento`**: confirmado que "Atrasado" é um status **calculado** (não persistido) quando `Status == "Pendente"` e `DataVencimento` já passou — o job só precisa persistir `"Pendente"`, nunca `"Atrasado"`.

## Clarifications

### Session 2026-09-22

- Q: A cobrança de mensalidade gerada automaticamente deve usar uma categoria de receita própria ("Mensalidade"), ou reaproveitar as categorias já existentes ("Aula em turma"/"Aula particular")? → A: Categoria nova "Mensalidade", distinta das já existentes — exige um script de banco pequeno (`database/NN_*.sql`) adicionando a categoria a `categoria_receita`.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Gerar automaticamente a cobrança mensal no fim do mês (Priority: P1)

Como professora, quero que, no fim de cada mês, o sistema gere automaticamente a conta a receber da mensalidade de cada aluno que eu configurei com essa modalidade — pelo valor que cadastrei no vínculo — sem que eu precise lançar essa cobrança manualmente todo mês, independentemente de quantas aulas o aluno teve naquele período.

**Why this priority**: É o núco do pedido — sem a geração automática em si, as demais regras (idempotência, exclusão, configuração) não têm o que proteger.

**Independent Test**: Com um aluno tendo um Vínculo de Cobrança Mensalidade ativo, avançar o relógio (ou simular) até o último dia do mês às 23h ou depois, e confirmar que uma conta a receber pendente foi criada para esse aluno, com o valor do vínculo e a competência do mês que terminou.

**Acceptance Scenarios**:

1. **Given** um aluno com um Vínculo de Cobrança Mensalidade ativo (Valor R$ 300, contexto de uma turma), **When** chega o último dia do mês corrente às 23h (horário local) ou depois, **Then** uma conta a receber pendente é gerada para esse aluno, com valor R$ 300 e competência do mês que está terminando.
2. **Given** um aluno com um Vínculo de Cobrança Mensalidade ativo, sem nenhuma aula registrada como realizada naquele mês, **When** o job dispara, **Then** a cobrança é gerada mesmo assim, pelo valor cheio do vínculo — a mensalidade não depende de presença.
3. **Given** um aluno com dois Vínculos de Cobrança Mensalidade ativos, cada um em uma turma diferente, **When** o job dispara, **Then** duas contas a receber são geradas para esse aluno naquele mês, uma para cada vínculo, cada uma com o valor do respectivo vínculo.
4. **Given** o job verificando o sistema em um dia que não é o último dia do mês, ou antes das 23h do último dia, **When** a verificação ocorre, **Then** nenhuma cobrança de mensalidade é gerada nessa verificação.

---

### User Story 2 - Nunca duplicar a cobrança do mesmo mês (Priority: P1)

Como professora, quero ter certeza de que, mesmo que o sistema verifique a condição de disparo várias vezes no mesmo dia (ou seja reiniciado e verifique de novo), a mensalidade de um aluno não seja cobrada duas vezes no mesmo mês.

**Why this priority**: Sem idempotência garantida, uma reinicialização do sistema durante a janela de disparo (último dia, a partir das 23h) poderia gerar cobranças duplicadas — um erro financeiro visível e constrangedor para a professora explicar ao aluno.

**Independent Test**: Disparar a verificação do job duas vezes seguidas dentro da mesma janela de disparo (último dia do mês, 23h ou depois) e confirmar que só uma conta a receber foi gerada por vínculo.

**Acceptance Scenarios**:

1. **Given** a cobrança de mensalidade de um vínculo já foi gerada para o mês corrente, **When** o job verifica novamente (mesmo ainda dentro da janela de disparo, ou em uma verificação seguinte no mesmo dia), **Then** nenhuma nova conta a receber é criada para esse vínculo naquele mês.
2. **Given** o sistema é reiniciado durante a janela de disparo do último dia do mês, **When** o job volta a rodar e verifica novamente, **Then** os vínculos que já tiveram a mensalidade do mês gerada não geram uma segunda cobrança.

---

### User Story 3 - Vínculo excluído antes do disparo não é cobrado (Priority: P2)

Como professora, quero que, se eu excluir (desativar) o Vínculo de Cobrança Mensalidade de um aluno antes do fim do mês, ele não seja cobrado automaticamente por aquele mês.

**Why this priority**: Evita cobrar um aluno que a professora já decidiu não cobrar mais daquela forma — importante para confiança no sistema, mas menos crítico que a geração e a idempotência em si (P1).

**Independent Test**: Excluir o Vínculo de Cobrança Mensalidade de um aluno antes do fim do mês e confirmar que, quando o job dispara, nenhuma conta a receber é gerada para esse vínculo.

**Acceptance Scenarios**:

1. **Given** um aluno com um Vínculo de Cobrança Mensalidade que foi excluído (desativado) antes do último dia do mês às 23h, **When** o job dispara, **Then** nenhuma conta a receber é gerada para esse vínculo.
2. **Given** um Vínculo de Cobrança Mensalidade excluído e depois reativado antes do disparo do job, **When** o job dispara, **Then** a cobrança é gerada normalmente (o que importa é o estado Ativo no momento do disparo, não o histórico do mês).

---

### Edge Cases

- O que acontece se um aluno tiver Vínculo de Cobrança Mensalidade cadastrado no meio do mês (não desde o início)? A cobrança do mês é gerada pelo valor cheio do vínculo, sem proporcionalização — o sistema não calcula pró-rata nesta fatia.
- O que acontece se o servidor ficar fora do ar durante toda a janela de disparo (último dia do mês, a partir das 23h) e só voltar depois de o mês já ter virado? A cobrança daquele mês não é gerada retroativamente nesta fatia — não há mecanismo de recuperação ("catch-up") automático; a professora precisaria lançar manualmente, se notar a falta.
- O que acontece se dois Vínculos de Cobrança Mensalidade do mesmo aluno (contextos diferentes) existirem no mesmo mês? Cada vínculo gera sua própria cobrança, de forma independente (Acceptance Scenario 3 da User Story 1) — a regra é "por vínculo", não "por aluno".
- O que acontece com um Vínculo de Cobrança Pacote ou Avulsa quando o job roda? Nada — o job só considera vínculos com Modalidade Mensalidade; os demais continuam exatamente como especificado em specs/038 (efeito por presença, não por job mensal).
- O que acontece se meses diferentes forem "recuperados" manualmente por engano (ex.: a professora gera uma cobrança manual idêntica pela tela)? Fora do escopo desta feature evitar essa duplicação manual — a idempotência aqui garante apenas que o próprio job não gera duas vezes a cobrança automática do mesmo vínculo no mesmo mês.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O sistema MUST verificar periodicamente, em segundo plano, se o momento atual corresponde ao último dia do mês corrente às 23h (horário local) ou depois.
- **FR-002**: Quando a condição de disparo (FR-001) for satisfeita, o sistema MUST, para cada Vínculo de Cobrança ativo com Modalidade Mensalidade, gerar uma conta a receber pendente com valor igual ao Valor cadastrado nesse vínculo, com competência do mês que está terminando — independentemente de quantas aulas o aluno teve presença nesse mês.
- **FR-003**: O sistema MUST gerar uma conta a receber por Vínculo de Cobrança Mensalidade ativo, não uma por aluno — um aluno com múltiplos vínculos Mensalidade ativos (em contextos diferentes) recebe uma cobrança para cada um.
- **FR-004**: O sistema MUST NOT gerar uma nova conta a receber de mensalidade para um Vínculo de Cobrança cuja cobrança da competência (mês) corrente já tenha sido gerada anteriormente — mesmo que a condição de disparo (FR-001) seja verificada múltiplas vezes dentro da mesma janela, ou o processo seja reiniciado.
- **FR-005**: O sistema MUST NOT gerar cobrança de mensalidade para um Vínculo de Cobrança que estiver excluído (inativo) no momento do disparo, mesmo que ele tenha estado ativo em algum momento anterior do mês.
- **FR-006**: Cada conta a receber gerada por este job MUST registrar: a competência (mês de referência que está terminando), uma descrição que identifique claramente tratar-se de mensalidade e o contexto do vínculo (nome da turma, ou "Atendimento individual"/"particular" quando não houver turma), uma data de vencimento, e a categoria de receita "Mensalidade" (nova, distinta de "Aula em turma"/"Aula particular").
- **FR-007**: A data de vencimento de uma cobrança de mensalidade gerada automaticamente MUST ser o primeiro dia do mês seguinte ao mês de competência (ex.: mensalidade de setembro, gerada no fim de setembro, vence em 1º de outubro).
- **FR-008**: O intervalo de verificação periódica (FR-001) MUST ser configurável, seguindo o mesmo padrão de configuração já usado para o intervalo de verificação de Lembretes.
- **FR-009**: Esta feature MUST NOT gerar cobrança automática alguma para Vínculos de Cobrança de Modalidade Pacote ou Avulsa — o efeito desses dois continua sendo exclusivamente o já implementado em specs/038 (por presença de aula), sem nenhuma sobreposição com este job.
- **FR-010**: Esta feature MUST NOT implementar nenhum tratamento especial para quando um Vínculo de Cobrança Pacote se esgota (Saldo de Aulas chega a zero) — esse comportamento permanece exatamente como especificado em specs/038, fora do escopo deste job.
- **FR-011**: Uma falha ao processar a cobrança de um Vínculo de Cobrança específico (ex.: um dado inconsistente) MUST NOT impedir que os demais vínculos sejam processados na mesma execução do job, nem derrubar o serviço em segundo plano.
- **FR-012**: O sistema MUST disponibilizar uma categoria de receita "Mensalidade", distinta das já existentes, antes que este job possa gerar qualquer cobrança — nenhuma cobrança automática de mensalidade MUST ser gerada usando uma categoria diferente desta.
- **FR-013**: A partir desta feature, o aviso hoje exibido na seção "Vínculos de Cobrança" (specs/037) de que o vínculo "ainda não afeta a cobrança automática" MUST deixar de se aplicar a vínculos de Modalidade Mensalidade, já que eles passam a ter efeito automático real — o aviso continua válido apenas enquanto existir alguma modalidade sem efeito automático (nenhuma, depois desta feature, considerando specs/038 já ter coberto Avulsa/Pacote).

### Key Entities

- **Vínculo de Cobrança (VinculoCobranca)**: já existente (specs/037). Nesta feature, passa a ser consultado também por este job (além da consulta já feita por presença de aula, specs/038) — todo vínculo Ativo com Modalidade Mensalidade é candidato a gerar uma cobrança por competência.
- **Conta a Receber (Pagamento)**: já existente. Passa a poder ser gerada também por este job, de forma independente de qualquer Aula específica — ao contrário das contas geradas por presença (specs/038), uma cobrança de mensalidade não fica associada a uma aula em particular. O sistema precisa conseguir identificar, para um dado Vínculo de Cobrança e uma dada competência, se a cobrança correspondente já existe (para a idempotência de FR-004).
- **Categoria de Receita ("Mensalidade")**: nova categoria, distinta das já existentes ("Aula em turma", "Aula particular", "Reposição", "Outro"), usada exclusivamente pelas cobranças geradas por este job — permite à professora diferenciar, nos relatórios financeiros, receita de mensalidade fixa de receita de aula avulsa.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% dos Vínculos de Cobrança Mensalidade ativos no momento do disparo recebem exatamente uma cobrança gerada automaticamente para o mês corrente, sem exceção e sem depender de presença em aula.
- **SC-002**: 0 cobranças de mensalidade duplicadas são observadas para o mesmo Vínculo de Cobrança na mesma competência, mesmo após múltiplas verificações do job dentro da mesma janela de disparo ou reinicializações do sistema.
- **SC-003**: 100% dos Vínculos de Cobrança Mensalidade excluídos antes do disparo permanecem sem nenhuma cobrança gerada para aquele mês.
- **SC-004**: Uma professora consegue, em um relatório financeiro, distinguir a receita vinda de mensalidades automáticas da receita vinda de aulas avulsas, sem análise manual linha a linha — pela categoria "Mensalidade".
- **SC-005**: Nenhuma falha de processamento de um Vínculo de Cobrança individual interrompe a geração de cobrança dos demais vínculos na mesma execução do job.

## Assumptions

- Uma cobrança de mensalidade gerada por este job MUST NOT ficar vinculada a nenhuma Aula específica (ao contrário das contas geradas por presença em specs/038) — ela representa o mês inteiro, não uma sessão isolada.
- Não há proporcionalização (pró-rata): um Vínculo de Cobrança Mensalidade cadastrado no meio do mês, mas ativo no momento do disparo, gera a cobrança pelo valor cheio do vínculo.
- Não há mecanismo de recuperação retroativa ("catch-up"): se o disparo for perdido inteiramente (ex.: sistema fora do ar durante toda a janela do último dia), a cobrança daquele mês não é gerada automaticamente depois — fica a critério da professora lançar manualmente, se notar.
- A data de vencimento das cobranças geradas é sempre o primeiro dia do mês seguinte ao mês de competência (FR-007) — uma regra simples e previsível, sem campo configurável adicional nesta fatia.
- O intervalo de verificação periódica é configurável, mas seu valor padrão é escolhido para garantir que a janela de disparo (último dia do mês, a partir das 23h) seja alcançada de forma confiável antes da virada do mês — não necessariamente um valor literal de "24 horas", cuja escolha ingênua poderia, dependendo do horário de início do serviço, nunca coincidir com a janela de disparo. A justificativa técnica do valor padrão fica para `/speckit-plan` (`research.md`).
- Esta feature é majoritariamente de backend (job em segundo plano); a única mudança de tela é a remoção/ajuste do aviso "ainda não afeta a cobrança automática" para vínculos Mensalidade (FR-013, ver Constitution/EX-001 em plan.md) — nenhum indicador visual novo (ex.: "mensalidade já cobrada este mês") é adicionado nesta fatia.
- Fora de escopo, reafirmando o pedido original: geração de cobrança para Modalidade Pacote ou Avulsa (specs/038 já cobre essas duas por presença), e qualquer tratamento de Pacote esgotado.
- A criação da categoria de receita "Mensalidade" (FR-012) é um script de dados pequeno (padrão já usado no projeto para categorias fixas, `database/10_financeiro_contas.sql`), não uma mudança de schema — só uma nova linha em `categoria_receita`.
