# Feature Specification: Corrigir Agrupamento por Turma no Relatório Financeiro

**Feature Branch**: `029-fix-relatorio-financeiro-turma`

**Created**: 2026-09-17

**Status**: Draft

**Input**: User description: "Corrigir RelatorioService.ObterFinanceiroAsync (agrupamento "por turma"): hoje, quando busca a turma associada a um Pagamento para agrupar o relatório financeiro, o código usa FirstOrDefault() sobre a lista genérica de turmas do Aluno, o que atribui incorretamente TODO o valor financeiro do aluno à primeira turma encontrada, ignorando as demais turmas que ele participa. Correção: usar o vínculo real já existente — cada Pagamento está associado a Aulas específicas via PagamentoAula, e cada Aula já sabe sua Turma (Aula.TurmaId). Agrupar cada Pagamento pela(s) turma(s) das aulas que ele efetivamente cobre, não pela lista genérica de turmas do aluno. Caso um Pagamento cubra aulas de turmas diferentes (se isso for possível hoje — verificar), definir e documentar a regra de tratamento antes de implementar."

## Nota de investigação prévia

Foi verificado se um único Pagamento pode cobrir aulas de turmas diferentes: **sim, é possível hoje**. O registro manual de pagamento aceita uma lista livre de `AulaIds` sem nenhuma validação de que essas aulas pertençam à mesma turma (nem ao mesmo aluno). Já a geração automática de cobrança (ao registrar presença) sempre vincula um Pagamento a exatamente uma Aula, então nunca produz esse cenário sozinha — o caso de múltiplas turmas só ocorre em pagamentos lançados manualmente cobrindo mais de uma aula. A regra de tratamento para esse cenário está definida na User Story 2 abaixo.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Ver o valor financeiro atribuído à turma correta (Priority: P1)

Como professora consultando Relatórios → Relatório Financeiro → Visão Geral, eu quero que o agrupamento "Por Turma" mostre o valor recebido de cada pagamento na turma cuja aula ele efetivamente cobriu, para que eu possa confiar nos totais por turma ao avaliar a rentabilidade de cada uma, em vez de ver o valor inteiro de um aluno matriculado em várias turmas concentrado incorretamente em apenas uma delas.

**Why this priority**: É o defeito relatado — hoje o relatório mente sobre a origem do valor sempre que um aluno participa de mais de uma turma, o que compromete qualquer decisão tomada com base nesse agrupamento. Corrigir isso é o valor central desta mudança.

**Independent Test**: Cadastrar um aluno vinculado a duas turmas distintas, registrar pagamentos vinculados a aulas de cada uma dessas turmas, e confirmar em `GET` do Relatório Financeiro que o agrupamento "Por Turma" mostra o valor correto em cada turma (não tudo concentrado em uma só).

**Acceptance Scenarios**:

1. **Given** um aluno matriculado nas turmas A e B, com um pagamento vinculado a uma aula da turma A e outro pagamento vinculado a uma aula da turma B, **When** a professora consulta o agrupamento "Por Turma" do Relatório Financeiro, **Then** o valor do primeiro pagamento aparece somado em "Turma A" e o valor do segundo aparece somado em "Turma B" — nenhum dos dois aparece incorretamente na outra turma.
2. **Given** um aluno matriculado em apenas uma turma, com pagamentos vinculados a aulas dessa turma, **When** a professora consulta o agrupamento "Por Turma", **Then** o comportamento permanece o mesmo de hoje (valor corretamente somado nessa única turma) — esta correção não MUST alterar o resultado do caso já correto.
3. **Given** um pagamento vinculado a uma aula individual (sem turma), **When** a professora consulta o agrupamento "Por Turma", **Then** o valor continua aparecendo no grupo "Atendimento particular", como já acontece hoje.
4. **Given** um pagamento registrado manualmente sem nenhuma aula vinculada (`AulaIds` vazio), **When** a professora consulta o agrupamento "Por Turma", **Then** o valor aparece no grupo "Atendimento particular", pelo mesmo motivo do cenário 3 (não há aula para identificar a turma).

---

### User Story 2 - Ver com clareza quando um pagamento cobre mais de uma turma (Priority: P2)

Como professora, quando um pagamento lançado manualmente cobre aulas de turmas diferentes (por exemplo, um pagamento único que quita aulas de reposição de duas turmas ao mesmo tempo), eu quero que esse valor seja identificado de forma explícita e sem duplicação no relatório, para que o total geral do relatório continue batendo e eu não seja levada a pensar que o dinheiro foi recebido duas vezes.

**Why this priority**: É um caso possível, porém raro (só ocorre em lançamento manual multi-aula) — não é o defeito principal relatado, mas precisa de uma regra definida para que a correção da User Story 1 não introduza um novo tipo de erro (duplicar ou perder valores na soma).

**Independent Test**: Registrar manualmente um único pagamento vinculado a uma aula da turma A e a uma aula da turma B, e confirmar que o valor total do pagamento aparece uma única vez no agrupamento "Por Turma" (sob o rótulo "Múltiplas turmas"), e que a soma de todos os grupos do relatório continua igual ao total recebido no período.

**Acceptance Scenarios**:

1. **Given** um único pagamento vinculado a aulas de duas turmas diferentes, **When** a professora consulta o agrupamento "Por Turma", **Then** o valor total desse pagamento aparece uma única vez, sob o rótulo "Múltiplas turmas" — não é somado em nenhuma das turmas individuais nem duplicado entre elas.
2. **Given** qualquer combinação de pagamentos (turma única, múltiplas turmas, sem turma), **When** a professora soma os valores de todos os grupos exibidos em "Por Turma", **Then** o resultado é exatamente igual ao "Total Recebido" já exibido na Visão Geral do mesmo relatório, para o mesmo período e filtros.

---

### Edge Cases

- Um pagamento vinculado a duas aulas da **mesma** turma: conta apenas uma vez, no grupo dessa turma (não duplica por ter mais de uma aula vinculada).
- Um pagamento vinculado a aulas de uma turma e também a uma aula individual (sem turma) ao mesmo tempo: é tratado como "Múltiplas turmas" (mesma regra da User Story 2), já que combina mais de uma origem de turma (incluindo a ausência de turma como uma origem distinta) — ver FR-004.
- O filtro por turma já existente no relatório (`turmaId`) continua funcionando como hoje (filtra pagamentos com pelo menos uma aula vinculada à turma informada); esta correção não altera esse filtro, apenas o agrupamento "Por Turma" exibido no resultado.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O sistema MUST agrupar, no agrupamento "Por Turma" do Relatório Financeiro, o valor de cada pagamento pela(s) turma(s) das aulas efetivamente cobertas por ele (via seu vínculo com aulas), e MUST NOT usar a lista geral de turmas em que o aluno está matriculado para essa decisão.
- **FR-002**: O sistema MUST atribuir o valor de um pagamento ao grupo "Atendimento particular" quando nenhuma das aulas cobertas por ele tiver turma associada, ou quando o pagamento não tiver nenhuma aula vinculada — preservando o comportamento já existente para esses casos.
- **FR-003**: O sistema MUST considerar cada turma distinta apenas uma vez por pagamento, mesmo que o pagamento cubra mais de uma aula da mesma turma (sem duplicar o valor do pagamento dentro do mesmo grupo).
- **FR-004**: O sistema MUST atribuir o valor de um pagamento ao grupo "Múltiplas turmas" quando as aulas cobertas por ele pertencerem a mais de uma turma distinta (incluindo o caso de combinar aulas com turma e aulas sem turma), em vez de atribuí-lo a apenas uma delas ou duplicá-lo entre todas.
- **FR-005**: O sistema MUST garantir que a soma dos valores de todos os grupos exibidos em "Por Turma" seja sempre igual ao total recebido no mesmo período/filtros já exibido na Visão Geral do Relatório Financeiro — nenhum valor pode ser perdido ou contado mais de uma vez pelo agrupamento.
- **FR-006**: Esta correção MUST se limitar ao agrupamento "Por Turma" do Relatório Financeiro — os agrupamentos "Por Forma de Pagamento" e "Por Aluno" da mesma tela, e os demais indicadores/relatórios do sistema, MUST permanecer com o comportamento atual, sem alteração.

### Key Entities

- **Pagamento**: já existente; o valor (`ValorFinal`) é o que está sendo mal atribuído hoje. Não há mudança de estrutura — apenas na lógica de como sua turma é determinada para fins de agrupamento no relatório.
- **PagamentoAula**: vínculo já existente entre Pagamento e Aula, usado hoje só para listar quais aulas um pagamento cobre. Passa a ser a fonte usada para descobrir a(s) turma(s) do pagamento no agrupamento "Por Turma".
- **Aula**: já existente; sua turma (`TurmaId`, opcional) é a informação real a ser usada em vez da lista de turmas do aluno.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% dos pagamentos vinculados a aulas de uma única turma aparecem, no agrupamento "Por Turma", somados na turma correta (a da aula que efetivamente cobrem), mesmo quando o aluno participa de outras turmas.
- **SC-002**: 100% dos pagamentos vinculados a aulas de mais de uma turma aparecem uma única vez, sob o rótulo "Múltiplas turmas", sem duplicação de valor.
- **SC-003**: Em qualquer consulta ao Relatório Financeiro, a soma dos valores de todos os grupos exibidos em "Por Turma" é idêntica ao "Total Recebido" exibido na mesma consulta, com variação zero.
- **SC-004**: Os demais agrupamentos e indicadores da mesma tela (Por Forma de Pagamento, Por Aluno, Recebido/Pendente/Atrasado) permanecem com exatamente os mesmos valores antes e depois da correção, para o mesmo conjunto de dados.

## Assumptions

- O rótulo "Múltiplas turmas" é um novo grupo distinto de "Atendimento particular" — este último continua reservado para pagamentos sem nenhuma turma identificável (sem aula vinculada, ou aulas vinculadas sem turma).
- A geração automática de cobrança (Estória 8, ao registrar presença) nunca produz um pagamento vinculado a mais de uma aula, logo nunca cai no caso "Múltiplas turmas" — esse caso é exclusivo de pagamentos lançados manualmente com mais de uma aula vinculada a turmas diferentes.
- O comportamento do filtro por turma (`turmaId`) na tela de Indicadores Financeiros Filtrados (que já usa a mesma convenção de "aluno tem aula vinculada à turma X" para filtrar) está fora do escopo desta correção — só o agrupamento de exibição "Por Turma" da Visão Geral está sendo corrigido.
- Não há necessidade de alterar nenhum contrato de API além do conteúdo do agrupamento "Por Turma" em si (a chave/rótulo do grupo muda de comportamento, mas o formato da resposta continua o mesmo: uma lista de `{ Chave, Total }`).
