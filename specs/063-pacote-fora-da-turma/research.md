# Research: Turmas de Pacote e Pacote Coletivo por Turma (spec 063)

Revisão de 2026-10-30 (terceira rodada de decisões). Nada ficou como `NEEDS CLARIFICATION`.

## R1 — Participação continua pela matrícula

**Decisão**: o aluno de pacote de uma turma **é matriculado** nela (`alunos_turma`). `AulaRepository.ObterAlunosAtivosDaTurmaAsync` e tudo que lê a matrícula (lembrete por e-mail, filtro de alunos por turma, ficha do aluno, relatórios) **ficam como estão**.

**Por quê**: o usuário esclareceu que o aluno "poderá se matricular nessa turma e contratar o pacote normalmente". A primeira versão previa tirar o aluno da matrícula e alterar quatro leitores dela; deixou de ser necessário.

**Alternativa descartada**: participação só pelo vínculo de pacote. Exigia mudar participantes, lembretes, filtros e relatórios, sem ganho.

## R2 — Como o aluno entra na turma de pacote (e volta)

**Decisão**:
- O cadastro do pacote de turma **matricula** o aluno na mesma gravação (`TurmaRepository.VincularAlunoAsync` e o repositório de vínculos compartilham o mesmo contexto do banco), sem duplicar matrícula existente.
- A matrícula direta (`POST /api/turmas/{id}/alunos`) em turma de pacote só é aceita para quem **já tem pacote ativo daquela turma** (a volta de quem saiu), e só enquanto a turma está em andamento. Para qualquer outro aluno é recusada, com mensagem que distingue "entrada é pelo pacote" de "turma fechada".

**Por quê**: a turma de pacote só recebe quem contratou o pacote; mensalidade e avulso entram como "outros alunos" (reposição). A exceção da volta é decisão do usuário.

## R3 — Tipo da turma

**Decisão**: enum `TipoTurma { Mensalidade = 1, Pacote = 2 }`, persistido como texto (mesmo padrão de `modalidade`). Turmas existentes ficam `Mensalidade`, exceto as convertidas pela migração (R9). O tipo é **imutável** depois de criada (`409` na troca), porque "a turma não muda". Pelo mesmo motivo a **`data_inicio` não muda depois de preenchida** (mudá-la reabriria uma turma fechada); a `data_fim` pode ser ajustada (prorrogar ou antecipar), com `fim >= início`.

## R4 — Estados da turma de pacote (datas)

`hoje` = `DateOnly.FromDateTime(DateTime.Now)`, como nas specs 055 e 059.

| Estado | Definição |
|---|---|
| **Iniciada** | `data_inicio` preenchida e anterior a hoje. Sem data nunca é "iniciada" (turma migrada) |
| **Em andamento** | turma ativa **e** (`data_fim` vazia **ou** de hoje em diante) |
| **Encerrada** | turma inativa **ou** `data_fim` anterior a hoje |

- Cadastrar pacote de turma exige turma ativa, de pacote e **não iniciada** (FR-003).
- Reativar pacote e rematricular quem saiu exigem turma **em andamento** (FR-004, FR-021); não exigem "não iniciada".
- A API devolve `fechada` (iniciada) calculada no backend, para a tela não duplicar a regra (Princípio II).

## R5 — Escopo do pacote: o grupo da aula

**Decisão**: em `RegraCobrancaPresenca.Decidir`, um pacote só é **aplicável** se `p.TurmaId == turmaIdDaAula` (para aula individual, os dois são nulos). O resto do algoritmo é o atual e usa só os aplicáveis (consumo, aviso de esgotado, bloqueio). A ordem de prioridade não muda.

| Pacote | Aula da mesma turma | Aula de outra turma | Aula individual |
|---|---|---|---|
| De turma | consome | não aplicável (paga avulso) | não aplicável (paga à parte) |
| De aulas individuais (`TurmaId` nulo) | não aplicável (paga avulso) | não aplicável (paga avulso) | consome |

- Quem decide é a **turma da aula**, não a matrícula: o aluno que saiu da turma A e participa de uma aula dela como "outro aluno" ainda consome o pacote da A.
- Fora do grupo o pacote (mesmo esgotado) **não interfere**: nem barra, nem avisa. A decisão cai na Avulsa do contexto ou em "valor da aula" (`SemVinculo`).
- Há no máximo um pacote ativo por grupo (R6), então **não existe regra de escolha entre pacotes**. Dados antigos com mais de um pacote do mesmo grupo continuam desempatados pelo menor Id (como já faz a spec 056).

## R6 — Regras do vínculo de pacote

Em `VinculoCobrancaService` (todas no backend):

| Regra | Onde |
|---|---|
| `turmaId` preenchido: turma existe, é da professora, ativa, de pacote e **não iniciada** (cadastro) | `ValidarTurmaDePacoteAsync` (novo, ao lado do `ValidarTurmaAsync` da Avulsa) |
| Cadastro matricula o aluno (sem duplicar) | mesmo salvamento |
| `turmaId` nulo = pacote de aulas individuais | — |
| Combinação: no máximo **um pacote ativo por turma** e **um de aulas individuais** por aluno; N pacotes de turmas diferentes convivem | `GarantirRegrasDeCombinacaoAsync` (substitui o "o aluno já tem um pacote ativo") |
| Mensalidade continua exclusiva e sem turma; Avulsa continua por contexto | sem mudança |
| `turmaId` imutável na edição de Pacote (a Avulsa continua podendo mudar de turma como hoje) | `AtualizarAsync` |
| Reativar: turma **em andamento**, combinação, e matricula o aluno se faltar | `ReativarAsync` |

O validador de request recusa `turmaId` hoje para Mensalidade **e** Pacote; passa a recusar só para Mensalidade.

**Sem índice único no banco** para "um pacote por grupo": a spec 056 já decidiu que combinações proibidas herdadas não são resolvidas sozinhas, e um índice falharia se existirem duplicados antigos. Os duplicados entram nas consultas de "casos para decidir" da migração.

## R7 — Fim da turma: pacotes inativos, saldo preservado (FR-019)

**Decisão**: um serviço de aplicação, `EncerramentoTurmaPacoteService`, com duas operações:
1. `InativarPacotesDaTurmaAsync(turmaId)`: chamado por `TurmaService.ExcluirAsync` (a professora inativa a turma), no mesmo salvamento.
2. `EncerrarPacotesDeTurmasEncerradasAsync(hoje)`: inativa todo pacote ativo cuja turma está **inativa** ou com `data_fim` anterior a hoje.

Nas duas só `vinculo_cobranca.ativo` muda: `SaldoAulas`, `Valor`, `TotalAulasPacote`, pagamentos e presenças ficam como estão (FR-019).

**Rotina**: `TurmaEncerramentoDispatcherService` (`BackgroundService`, mesmo padrão do `NotificacaoDispatcherService`: `PeriodicTimer`, escopo por execução, falha registrada sem derrubar o serviço). Roda **ao iniciar** a aplicação (recupera dias perdidos) e depois a cada `Turmas:IntervaloEncerramentoMinutos` (padrão 60): o atraso máximo para inativar os pacotes depois da virada do dia é de uma hora (spec, decisão menor 6). A operação é **idempotente**, então rodar mais de uma vez por dia não causa efeito nenhum; por isso o intervalo curto garante "no máximo um dia" de atraso sem agendador de horário. O valor da configuração é lido de fato (Princípio III).

**Reativar a turma não reativa os pacotes** (`TurmaService.ReativarAsync` não muda). Pacote inativado pela professora por outro motivo nunca é reativado por nada automático.

**Alternativa descartada**: calcular "pacote vigente" na hora (sem gravar `ativo`). O usuário pediu que o pacote "torne inativo", e vários pontos do sistema (painel de pacotes, lista da ficha, regra de presença) já filtram por `ativo`.

## R8 — Aluno excluído que volta (FR-020)

**Decisão**: **nenhuma mudança de código**. `AlunoService.ExcluirAsync` e `ReativarAsync` só alteram `aluno.ativo`; não tocam em vínculo nem em matrícula. O requisito vira **teste** (pacote e matrícula intactos depois de excluir e reativar) e **verificação por leitura do diff** (esperado: `AlunoService` sem diff). A rotina de fim de turma (R7) inativa o pacote também de aluno inativo.

## R9 — Migração (banco)

Ordem e salvaguardas:

1. **Backup** do banco local antes de aplicar.
2. Estrutura: colunas de `turma`, troca do `CHECK` do vínculo (`turma_id IS NULL OR modalidade IN ('Avulsa','Pacote')`).
3. Hoje todo pacote existente é "sem turma". Para cada pacote **ativo**:
   - aluno matriculado em **exatamente uma** turma `T` **ativa** onde **todos** os alunos ativos matriculados têm pacote ativo: `T.tipo = 'Pacote'` e `vinculo.turma_id = T`. Turma **inativa** não converte, porque a rotina de fim de turma (R7) inativaria o pacote logo em seguida;
   - aluno **sem nenhuma matrícula**: nada muda (passa a ser o pacote de aulas individuais pelo significado de `turma_id` nulo);
   - **demais** (várias turmas, turma mista): nada muda, e entram na lista.
4. As consultas de "casos para decidir" ficam no arquivo SQL e no quickstart: (a) pacotes ativos de aluno em várias turmas; (b) turmas mistas; (c) alunos com mais de um pacote ativo do mesmo grupo; (d) pacote ativo de aluno matriculado em turma inativa.
5. Nenhum `UPDATE` de valor, saldo, total ou `ativo`; nenhuma matrícula removida; nenhuma aula tocada.

**Efeito aceito**: o pacote sem turma passa a valer só em aula individual. Os casos (a) e (b) continuam assim até a professora decidir, e as presenças deles em turma serão cobradas como avulsas. A lista os mostra.

**Produção**: não aplicar. O usuário aplica depois, com backup e conferência.

## R10 — Seção "Outros alunos"

**Decisão**: o frontend divide `aula.alunos` pelo campo `reposicao` que a API já devolve (spec 056). Nenhum campo novo. Substitui a etiqueta "Reposição" na lista, mantendo o botão de remover dos de reposição em aula agendada. Na chamada, as duas seções usam o mesmo estado de presença (`presencas[alunoId]`).

**Por quê**: FR-012 proíbe o frontend de decidir quem é de fora; agrupar por um campo recebido não é decidir.

## R11 — Conflito de horário do aluno

**Decisão**: nenhuma regra nova. `ExisteConflitoAsync` já bloqueia qualquer sobreposição entre aulas ativas da professora; todo aluno só participa de aulas dela. Entra um teste que prova a propriedade e uma leitura do diff de que não surgiu um segundo cálculo (FR-016).

## R12 — Cobrança automática na presença (FR-023)

**Decisão**: nenhuma mudança de fluxo. `AulaService.AplicarPresencasAsync` decide com `RegraCobrancaPresenca` e `GerarContasAReceberAsync` cria a conta a receber nos casos Avulsa e Sem vínculo, para todo `AulaAluno` com presença, inclusive reposição (mesma lista). Entram testes para "outro aluno" nos casos novos: pacote de turma em outra turma, pacote de aulas individuais em aula de turma e pacote de turma em aula individual geram conta; pacote consumido, mensalidade e plano não geram.

## R13 — Telas

- Turma: campo "Tipo" (só na criação; depois somente leitura) e datas para Pacote (turma migrada pode preencher as datas na edição). Na turma de pacote o botão de vincular aluno só aparece para quem tem pacote ativo dela; senão, texto explicando a entrada pelo pacote. `TurmaResponse.fechada` vem do backend (R4).
- Vínculo: para Pacote aparece "Turma (opcional)": turmas de pacote ativas e não fechadas (`!fechada`); vazio = pacote de aulas individuais. A listagem mostra a turma ou "Atendimento individual". O erro de "já tem pacote deste grupo" vem da API.
- Dashboard (Pacotes de aulas): o contexto do pacote deixa de ser "Todas as aulas do aluno" e mostra a turma ou "Atendimento individual".

## R14 — Fora desta spec (cards próprios)

- **Recorrência de aulas**: confirmado que não existe (o formulário e `AulaService.CadastrarAsync` criam uma aula por vez).
- **Saída com acerto proporcional**: o pacote não gera contas a receber nem parcelas hoje; sem saber o que o aluno pagou não há acerto. Card "Saída do aluno de turma de pacote".
