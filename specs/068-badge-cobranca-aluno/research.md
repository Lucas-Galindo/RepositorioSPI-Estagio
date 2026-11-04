# Research: Selo de cobrança do aluno (spec 068)

Decisões baseadas na leitura do código (2026-11-03) e nas respostas do usuário (3 telas; resumo "principal +N" na lista; selo com categoria, aulas restantes, valor e turma).

## R1 — Um serviço novo de leitura: `ResumoCobrancaService`

**Decisão**: `IResumoCobrancaService` / `ResumoCobrancaService` em `SPI.Application/VinculosCobranca/Services/`, **somente leitura**, com dois métodos em lote:

- `ResumirParaListaAsync(IReadOnlyCollection<Aluno> alunos)` → `Dictionary<int, SeloCobrancaResponse>`: o **resumo** de todos os vínculos ativos de cada aluno (principal + "+N" + detalhes). Usado pela lista de alunos.
- `ResumirParaGrupoAsync(IReadOnlyCollection<Aluno> alunos, int? turmaId)` → `Dictionary<int, SeloCobrancaResponse>`: o vínculo que **vale naquele grupo**, decidido por `RegraCobrancaPresenca.Decidir(ativos, turmaId)` (a **mesma** função que decide a cobrança da presença). Usado pela página da turma (`turmaId` da turma) e pela chamada (`turmaId` da aula; nulo = aula individual).

**Por quê**: o Princípio II manda a regra morar no backend, uma vez. Reusar `Decidir` garante que o selo nunca diverge do que será cobrado (SC-002). O resumo da lista é a única regra nova (qual é o principal) e fica no mesmo serviço.

**Alternativas descartadas**: (a) calcular no frontend a partir de `GET /vinculos-cobranca` por aluno — viola o Princípio II e cria N+1; (b) duplicar a regra em cada serviço (Aluno, Turma, Aula) — três cópias; (c) campo persistido de "categoria" no aluno — dado derivado e desatualizável (e exigiria migração).

## R2 — Sem N+1 (FR-006)

Cada chamada do serviço faz **no máximo duas consultas**, para a lista inteira:

1. `IVinculoCobrancaRepository.ListarAtivosDosAlunosAsync(IReadOnlyCollection<int> alunoIds)` (**novo**; `WHERE ativo AND aluno_id IN (...)`, com `Include(Turma)` para o nome da turma; ordenado por `Id`).
2. `IAulaRepository.ContarPresencasPorVinculosAsync(idsDosPacotes, null, null)` (**já existe**, usado pela ficha do aluno) — só se houver algum pacote entre os vínculos — para o progresso ("restam N") via `ProgressoPacote.Calcular` (spec 058).

Um teste de contagem de chamadas ao fake do repositório garante "uma consulta por tela, não por aluno".

## R3 — Regra do vínculo principal (lista)

Ordem fixa e simples, decidida no servidor: **Mensalidade → Pacote → Avulsa**; dentro da mesma categoria, o **de maior Id** (o mais recente). `OutrosVinculos` = total de ativos − 1. A dica traz **todos** os ativos (categoria, turma, valor, restantes). Semestral/Anual (spec 062) entram na ordem quando existirem (a ordem fica numa constante única; a categoria vem do nome da modalidade, então nenhuma tela muda).

Observação (dados herdados): com vínculos duplicados do mesmo tipo (conflito antigo, spec 056) a lista escolhe o **mais recente**, enquanto `Decidir` (turma e chamada) escolhe o **mais antigo**; só pode divergir nesses dados já sinalizados como conflito.

Observação: na lista a mensalidade já aparece sozinha (é exclusiva); só pacote e avulsa convivem, então o "+N" tem pouco a esconder na prática.

## R4 — O selo por grupo (turma e chamada)

`Decidir(ativos, turmaId)` devolve `DecisaoCobranca(Tipo, Vinculo, PacoteEsgotado)`. Mapeamento para o selo:

| Decisão | Categoria do selo | Extras |
|---|---|---|
| `Mensalidade` | `Mensalidade` | valor do vínculo |
| `Pacote` | `Pacote` | valor, progresso (`restam N`, estado) |
| `Avulsa` (sem pacote esgotado) | `Avulsa` | valor |
| `Avulsa` com `PacoteEsgotado` | `Avulsa` | valor + `AvisoPacoteEsgotado = true` |
| `Bloqueada` | `Pacote` | progresso com estado **Esgotado** (a presença será barrada) |
| `SemVinculo` | `SemVinculo` | valor = `Aluno.ValorAula` |

`Decidir` é estático e já cobre o aluno de reposição (olha a turma da aula, não a matrícula).

## R5 — Onde cada resposta ganha o selo

| Resposta | Campo novo | Quando é preenchido |
|---|---|---|
| `AlunoResponse` | `Cobranca` (resumo da lista) | só em `AlunoService.ListarAsync` **quando `incluirCobranca` é verdadeiro** (R10); nos demais retornos (obter, cadastrar, atualizar…) e nas listas sem o parâmetro fica `null` (a ficha tem a seção própria) |
| `AlunoResumoResponse` (alunos da turma, em `TurmaResponse`) | `Cobranca` (do grupo = a turma) | em `TurmaService.MapearComPacotesAsync` (alunos matriculados); `AlunosComPacoteForaDaTurma` **não** recebe (não está na turma) |
| `AlunoPresencaResponse` (participantes da aula) | `Cobranca` (do grupo = turma da aula) | nos retornos de **uma aula só** (obter, atualizar, participantes, registrar sessão, cancelar); **não** na lista de aulas |

`AulaService.Mapear` é síncrono e usado também pela lista de aulas; por isso o selo é preenchido por um passo assíncrono separado, só nos retornos de uma aula, sem pesar a lista.

## R6 — Injeção sem quebrar dezenas de testes

`AlunoService`, `TurmaService` e `AulaService` recebem `IResumoCobrancaService? cobranca = null` como **último parâmetro opcional**: sem ele (nos testes antigos) nenhum selo é preenchido e nada muda; em produção o DI sempre injeta. Evita alterar as construções de teste existentes (conferido: 1 de `AlunoService`, 3 de `TurmaService` e cerca de 11 de `AulaService`). O risco (esquecer o registro) é coberto por um teste do serviço e pela validação pela API (o selo precisa vir preenchido).

## R7 — Frontend: um componente, nenhum CSS novo

`frontend/components/alunos/SeloCobranca.tsx` (**único arquivo novo** de frontend), usado pela lista, pela turma e pela chamada.

- **Visual**: reaproveita as classes `status-pill` já existentes (cores e contraste já conferidos): Mensalidade e Pacote em dia → `status-ativo`; Pacote em atenção → `status-pendente`; Pacote esgotado → `status-cancelado`; Avulsa e Sem vínculo → `status-inativo`. Nenhuma classe nem cor nova; só tokens (FR-009).
- **Conteúdo**: pílula com a categoria (e "· restam N" no pacote), e uma linha pequena abaixo (`who`) com valor e, no modo `lista`, a turma; "+N" ao lado da pílula quando `outrosVinculos > 0`; `title` com a dica (todos os vínculos) no modo `lista`; aviso "pacote esgotado" na avulsa.
- **Formatação** usa `currency` de `lib/format`; o componente só formata (sem decidir).
- Propriedade `modo: "lista" | "grupo"` controla só o que exibir (turma/dica); não calcula nada.
- Se `cobranca` vier nulo (falha ou serviço ausente), mostra um traço, sem quebrar a tela.

## R8 — Testes

- `ResumoCobrancaServiceTests` (fakes manuais): ordem do principal e "+N"; sem vínculo; pacote com restantes e esgotado; mensalidade; **consistência com `Decidir`** numa matriz de vínculos (SC-002); pacote de outra turma não vale; aluno de reposição; aluno inativo; **uma consulta por lista** (guarda de N+1); vínculo inativo ignorado.
- Serviços: `AlunoServiceListarCobrancaTests`, `TurmaServiceCobrancaTests`, `AulaServiceCobrancaSeloTests` (o selo vem preenchido; ausente quando o serviço não é injetado).
- Os fakes de `IVinculoCobrancaRepository` (3 arquivos) ganham o método novo; nenhum `Assert` existente muda.

## R10 — Resumo da lista sob demanda (`incluirCobranca`)

**Problema**: `GET /api/alunos` é chamado por 12 telas (aula, lembretes, pagamentos, relatórios, dashboard, contas a receber etc.), quase todas só para preencher listas de seleção. Calcular o resumo sempre custaria 2 consultas e mais dados em todas elas, sem uso.

**Decisão**: parâmetro opcional `incluirCobranca` (padrão **falso**) em `GET /api/alunos`, repassado a `IAlunoService.ListarAsync(..., bool incluirCobranca = false)`. Só a tela de Alunos envia `incluirCobranca=true`. Sem o parâmetro, a resposta é idêntica à de hoje. Os testes existentes de `ListarAsync` continuam compilando (parâmetro opcional).

**Alternativas descartadas**: (a) sempre calcular — custo e payload em 11 telas sem necessidade; (b) endpoint novo só para a lista com cobrança — duplica filtros e rota; (c) o frontend pedir por aluno — N+1, proibido.

A turma e a chamada **não** precisam do parâmetro: o selo faz parte do que essas telas já pedem.

## R9 — Riscos

| Risco | Mitigação |
|---|---|
| Selo diverge da cobrança real | usa `RegraCobrancaPresenca.Decidir` e um teste de consistência |
| N+1 escondido | duas consultas por lista; teste conta as chamadas |
| Aula realizada: o vínculo vigente pode diferir do aplicado na época | documentado; o histórico exato continua na ficha e nos relatórios (spec 052) |
| Conflito com a spec 067 (mesmos arquivos de aula) | 067 em standby; edições aditivas |
| Selo largo na lista | `title` para o detalhe e "+N" curto; linha de apoio pequena |
