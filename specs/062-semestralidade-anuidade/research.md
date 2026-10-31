# Research: Semestralidade e Anuidade (spec 062)

## R1 — Modalidades e "plano periódico"

**Decision**: `ModalidadeCobranca` ganha `Semestral = 4` e `Anual = 5`. Uma classe de extensão em `Domain/Enums` oferece:
- `EhPlanoPeriodico()`: verdadeiro para Mensalidade, Semestral e Anual (cobrem todas as aulas, sem cobrança por presença);
- `ComPeriodo()`: verdadeiro só para Semestral e Anual (têm início, fim e parcelas);
- `MesesDoPeriodo()`: 6 para Semestral, 12 para Anual.

**Rationale**: os pontos do código que hoje dizem "é Mensalidade" passam a perguntar "é plano periódico", sem repetir listas de modalidades.

## R2 — Banco

**Decision**: `database/24_semestralidade_anuidade.sql` (aplicada no banco local, com backup antes):
1. `vinculo_cobranca.data_inicio DATE NULL` e `data_fim DATE NULL`.
2. Troca do `chk_vinculocobranca_modalidade` para aceitar `'Semestral'` e `'Anual'` (`DROP CHECK` e novo `ADD CONSTRAINT CHECK`).
3. `chk_vinculocobranca_periodo`: Semestral/Anual **exigem** `data_inicio` e `data_fim` (fim depois do início); as demais modalidades exigem os dois **nulos**.
4. Duas categorias de receita novas: "Semestralidade" e "Anuidade".

**Rationale**: o valor é gravado como texto (`VARCHAR(20)`), então os nomes novos cabem sem mudar o tipo. O CHECK protege o banco mesmo se alguém chamar a API errado. A categoria própria deixa a professora distinguir a receita nos relatórios, como foi decidido para a Mensalidade (spec 039).

**Não muda**: `uq_pagamento_vinculo_competencia (vinculo_cobranca_id, competencia)` já impede duas cobranças do mesmo vínculo na mesma competência, e `chk_vinculocobranca_turma_so_avulsa` já exige turma nula para as novas modalidades.

## R3 — Período

**Decision**: `DataFim = DataInicio + MesesDoPeriodo() meses − 1 dia`. Exemplo: início 01/11/2026 e Semestral → fim 30/04/2027. Calculado no serviço e gravado.

**Rationale**: guardar o fim evita recalcular em cada consulta e permite consultas de vigência no banco.

## R4 — Cadastro atômico das parcelas

**Decision**: `VinculoCobrancaService.CadastrarAsync`, para Semestral/Anual:
1. Valida (validador): início, valor total > 0, 1 a 24 parcelas, cada uma com valor > 0, **um vencimento por mês**.
2. Valida a combinação (R6).
3. Cria o `VinculoCobranca` e, para cada parcela, um `Pagamento`: `Status = "Pendente"`, `AlunoId`, `ValorFinal`, `DataVencimento`, `Competencia = primeiro dia do mês do vencimento`, `CategoriaReceitaId` da categoria do plano, `Descricao = "{Plano} - parcela X/N"` e navegação `VinculoCobranca = vinculo`.
4. **Um único `SalvarAlteracoesAsync`**. O EF grava o vínculo e depois as parcelas na mesma transação, e a chave estrangeira é preenchida sozinha pela navegação.

**Rationale**: atende "tudo ou nada" (SC-005) sem transação manual, porque os repositórios compartilham o mesmo contexto. É o contrário do que a revisão de código apontou na gravação do registro de aula (relatório de 29/10, item L2).

**Atenção**: `VinculoCobrancaRepository.SalvarAlteracoesAsync` traduz qualquer chave duplicada para "Já existe um vínculo Avulsa ativo para este contexto." Com as parcelas, uma duplicidade de competência produziria essa mensagem errada. Como a regra "um vencimento por mês" já é validada antes, isso só aconteceria numa corrida; mesmo assim a mensagem passa a ser genérica ("Já existe um vínculo ativo ou parcela duplicada para este contexto.").

## R5 — Sugestão de parcelas (frontend)

**Decision**: `frontend/lib/parcelas.ts`, função pura: dado o número de parcelas N, o valor total e o 1º vencimento, devolve N parcelas mensais. O valor de cada uma é `total / N` com duas casas, e o **resto dos centavos vai para a última parcela**. A data de cada uma é o mês seguinte do 1º vencimento, **sem ultrapassar o fim do mês** (31/01 → 28/02).

**Rationale**: é conveniência de preenchimento, sem efeito sobre a regra: o backend revalida tudo e nunca confia na sugestão. A professora ajusta valor e data de cada parcela como quiser (inclusive taxa de maquininha embutida). Se a soma diferir do total, a tela mostra um aviso, sem bloquear (FR-005).

## R6 — Regras de combinação (todas no `GarantirRegrasDeCombinacaoAsync`)

**Decision**, comparando com os outros vínculos ativos do aluno:

| Pedido | Recusa quando |
|---|---|
| Mensalidade | houver **qualquer** outro vínculo ativo (regra atual, já inclui os planos) |
| Semestral/Anual | houver Mensalidade, Pacote ou Avulsa ativos; ou outro Semestral/Anual **com período que se sobreponha** (início ≤ fim do outro e fim ≥ início do outro) |
| Pacote, Avulsa | houver Mensalidade **ou** Semestral/Anual ativo |

Mensagens novas: "O aluno tem um plano (mensalidade, semestralidade ou anuidade) ativo, que já cobre todas as aulas." e "Já existe um plano com período sobreposto para este aluno."

**Rationale**: reaproveita o padrão atual e permite a renovação antecipada (novo plano começando depois do fim do vigente), como decidido.

## R7 — Vigência na decisão de presença

**Decision**: `RegraCobrancaPresenca.Decidir(ativos, turmaId, dataDaAula)`. O passo 1 vira:
- uma **Mensalidade** ativa vale sempre (como hoje);
- um **Semestral/Anual** ativo vale se `DataInicio ≤ dataDaAula ≤ DataFim`.

O resultado passa a se chamar `TipoCobrancaPresenca.PlanoPeriodico` (antes `Mensalidade`). Se o plano está fora do período, ele é ignorado e a decisão segue para pacote, avulsa ou valor da aula. `AulaService.AplicarPresencasAsync` passa `aula.DataInicio`; o efeito (sem cobrança, vínculo gravado na presença) é o mesmo da Mensalidade.

**Rationale**: a decisão continua num lugar só. Nenhuma rotina precisa "desativar" o plano ao fim do período.

## R8 — Edição, exclusão e reativação

**Decision**:
- **Editar**: `AtualizarAsync` recusa (409) quando o vínculo existente ou o pedido é Semestral/Anual: "Semestralidade e anuidade não podem ser editadas: exclua e cadastre outro vínculo." A tela não mostra "Editar" para eles.
- **Excluir** (Semestral/Anual): na mesma transação do `Ativo = false`, as parcelas do vínculo com `Status = "Pendente"` e `DataVencimento > hoje` recebem `Status = "Cancelado"` (a rotina de status do banco só mexe em `data_pagamento` quando o novo status é "Pago", então alterar a entidade é equivalente). Nada é removido.
- **Reativar**: recusado (409) com orientação para criar um novo vínculo.

## R9 — Notificação de renovação

**Decision**: tipo `RenovacaoPlano`, uma por (professora, aluno), com a mesma trava `uq_notificacao_tipo_professor_aluno`. No `NotificacaoService.SincronizarAsync`, um terceiro laço por aluno:
- **Conjunto que precisa de aviso**: para cada **aluno ativo** com Semestral/Anual ativos, pega o plano de maior `DataFim`; se `DataFim ≤ hoje + RenovacaoDiasAntes`, o aluno precisa de aviso.
- **Criar, reativar ou resolver** pelas mesmas regras das demais. Um plano de renovação cadastrado com início depois do fim passa a ser o de maior `DataFim` e tira o aluno do conjunto; excluir o vínculo também.
- `Notificacoes:RenovacaoDiasAntes` (padrão 15) em `NotificacoesOptions`.

**Resposta**: o DTO ganha `renovacao: { alunoId, alunoNome, modalidade, dataFim }`. A mensagem é "O plano Semestral de {aluno} termina em dd/MM/aaaa." ou "...terminou em dd/MM/aaaa.". No sino, o tipo fica no novo filtro "Planos", e o atalho leva à ficha do aluno.

## R10 — E-mail de vencimento (5 dias antes)

**Decision**: `MensalidadeLembreteDispatcherService` já lê todo pagamento com vínculo (`VinculoCobrancaId != null`), então as parcelas entram sozinhas. Só o texto muda: assunto "SPI - Lembrete: pagamento vence em breve" e corpo "o pagamento {descrição} de {aluno}, no valor de R$ ..., vence em ...". Nenhum teste existente depende do texto antigo.

**Rationale**: pedido do usuário de manter o lembrete de 5 dias (spec 060) vale para qualquer cobrança que o sistema gerou a partir de um vínculo.

## R11 — Resposta da API e tela

**Decision**: `VinculoCobrancaResponse` ganha `DataInicio`, `DataFim` e `Parcelas` (para Semestral/Anual): `pagamentoId`, `descricao`, `vencimento`, `valor` e `status`. O `status` já vem como o sistema o mostra em Contas a Receber ("Atrasado" quando pendente e vencida), pela mesma regra do `PagamentoService`. As parcelas de todos os planos da listagem vêm numa **única consulta** (`ListarPorVinculosAsync`), sem uma consulta por vínculo.

**Rationale**: a ficha mostra o período e a situação de cada parcela sem o frontend decidir nada.
