# Análise de consistência: Turmas de Pacote e Pacote Coletivo por Turma (spec 063)

**Data**: 2026-10-30 (análise final, depois das três rodadas de decisões, do `/speckit-plan` e do `/speckit-tasks` revisados)
**Artefatos analisados**: `spec.md`, `plan.md`, `research.md`, `data-model.md`, `contracts/`, `quickstart.md`, `tasks.md`, constituição do projeto, regras do `CLAUDE.md` e o código atual (para conferir as premissas).
**Natureza**: análise sem alteração de código. As correções abaixo foram aplicadas depois, **só nos documentos da spec**, e estão listadas em cada achado.

## Resultado

| Métrica | Valor |
|---|---|
| Requisitos funcionais | 23 (FR-001 a FR-023) |
| Critérios de sucesso | 7 (SC-001 a SC-007) |
| Histórias | 5 (US1 a US5) |
| Tarefas | 34 (T001 a T034), das quais 1 manual (T034) |
| Requisitos com pelo menos uma tarefa | 23 de 23 (100%) |
| Critérios de sucesso com tarefa de verificação | 7 de 7 |
| "MUST NOT" com teste (quando há lógica) **e** verificação por leitura do diff, `grep` ou `git status` | 12 de 12 |
| Achados críticos | 0 |
| Violações da constituição | 0 |
| `[NEEDS CLARIFICATION]` restantes | 0 |
| Achados totais | 9 (todos corrigidos) |

## Achados

| ID | Categoria | Gravidade | Onde | Resumo | Correção aplicada |
|---|---|---|---|---|---|
| A1 | Inconsistência | **Alta** | FR-014, migração, rotina de fim de turma | A migração podia apontar um pacote para uma turma **inativa**. A rotina de fim de turma inativa os pacotes de turmas inativas, então o pacote ativo seria inativado logo depois da migração, sem a professora saber | A migração só converte turma **ativa**; o caso de turma inativa entra na lista "casos para decidir" (consulta (d)). Alterados: spec (decisão menor 9, FR-014, FR-015, US4), T002, T017, `research.md` R9, `data-model.md`, quickstart |
| A2 | Lacuna | Média | FR-005, edição da turma | A edição de uma turma de pacote já iniciada podia mudar a `dataInicio` e reabrir a turma "fechada", contornando FR-004 | `dataInicio` passa a ser imutável depois de preenchida; `dataFim` pode ser ajustada (`fim >= início`). Alterados: FR-005, decisão menor 3, US2 (cenário 5), T009, T011, `research.md` R3, contrato de turmas, quickstart (cenário 36) |
| A3 | Inconsistência | Média | FR-019, decisão menor, SC-006 × plano | A spec dizia "rotina diária" e o plano roda ao iniciar e a cada 60 minutos | Spec reescrita para "ao iniciar a aplicação e periodicamente", com atraso máximo de uma hora depois da virada do dia (decisão menor 6, FR-019, SC-006) |
| A4 | Inconsistência | Média | US2 (cenário 3) e decisão menor 1 × FR-004 | A história dizia que matricular alguém na turma iniciada é sempre recusado, mas FR-004 permite a volta de quem tem pacote ativo | Cenários e decisão menor reescritos: recusa para quem **não** tem pacote ativo dela |
| A5 | Terminologia | Baixa | Plano, pesquisa, T008, T015 | A tela do vínculo já usa "Atendimento individual"; os documentos novos usavam "Aulas individuais" para o mesmo rótulo | Rótulo de tela padronizado como "Atendimento individual"; "pacote de aulas individuais" continua sendo o nome do conceito |
| A6 | Estrutura | Baixa | Spec | FRs fora de ordem (FR-022/023 depois de FR-002; FR-018 depois de FR-021) e decisões menores numeradas fora de ordem | FRs ordenados de 001 a 023 e decisões menores renumeradas de 1 a 11 |
| A7 | Clareza | Baixa | Quickstart, cenário 26 | Status esperado "204/200" | "200 (turma devolvida com A)" |
| A8 | Risco aceito | Média | Plano (Complexity Tracking) | "Um pacote por turma e um individual por aluno" não tem índice único no banco; só regra de serviço e teste | Mantido, com justificativa: dados antigos podem ter duplicados, e a spec 056 adotou o mesmo critério. Os duplicados entram na consulta (c) da migração |
| A9 | Dependência | Média | Spec 062 | A 062 também altera `RegraCobrancaPresenca` (nome do tipo `PlanoPeriodico`) e usa a migração 24 | Migração da 063 é a **25**; quem implementar depois ajusta o outro (registrado nas premissas) |

## Cobertura: requisito → tarefas

| Requisito | Tarefas |
|---|---|
| FR-001 tipo e datas da turma | T002, T003, T009, T011, T029 |
| FR-002 todo pacote tem um grupo; nada vale em qualquer aula | T005, T006, T010, T015 |
| FR-003 cadastro do pacote de turma e matrícula junto | T010, T012 |
| FR-004 turma fechada; volta de quem tem pacote | T009, T011, T012, T029 |
| FR-005 tipo, turma do pacote e data de início imutáveis | T009, T010, T011, T029 |
| FR-006 saída não altera vínculo, saldo ou pagamentos | T009, T011, T028 |
| FR-007 ordem de prioridade da cobrança | T005, T006 |
| FR-008 pacote só vale no seu grupo | T005, T006, T007 |
| FR-009 falta não consome | T006 |
| FR-010 cobrança só em `RegraCobrancaPresenca` | T005, T025 |
| FR-011 presença e falta para todos | T006, T013 |
| FR-012 seção "Outros alunos" | T016, T026 |
| FR-013 reposição avulsa continua | T006 (caso k) |
| FR-014 migração dos pacotes | T002, T017 |
| FR-015 migração sem alterar vínculos, matrículas e aulas | T002, T017, T028 |
| FR-016 sem segunda regra de conflito | T013, T027 |
| FR-017 telas da turma e da ficha | T014, T015 |
| FR-018 notas nas specs antigas | T024 |
| FR-019 fim da turma inativa os pacotes, saldo preservado | T018, T019, T020, T022, T028 |
| FR-020 excluir o aluno não mexe no pacote | T023, T028 |
| FR-021 reativar só com a turma em andamento | T021, T023 |
| FR-022 0 ou N pacotes de turma; um individual | T006 (caso e), T010, T012, T029 |
| FR-023 cobrança automática na presença | T006 (caso j) |
| SC-001 | T006 |
| SC-002 | T010, T012 |
| SC-003 | T002, T017, T028 |
| SC-004 | T016, T034 |
| SC-005 | T009, T013 |
| SC-006 | T022 |
| SC-007 | T023 |

Tarefas sem requisito direto (esperado): T001 (backup), T004 (consultas de apoio das T009 e T018), T008 (rótulo do painel), T030 a T034 (verificação final).

## Verificação dos "MUST NOT" (regra do `CLAUDE.md`)

| Requisito | Por teste | Por leitura do diff, `grep` ou `git status` |
|---|---|---|
| FR-001 mensalidade sem datas | T011 | T029 (validador e `CHECK`) |
| FR-004 turma de pacote sem matrícula direta e sem entrada depois do início | T011, T012 | T029 |
| FR-005 tipo, turma do pacote e data de início não mudam | T011, T012 | T029 |
| FR-006 saída não altera vínculo, saldo ou pagamentos | T011 | T028 (`DesvincularAlunoAsync` sem diff) |
| FR-008 pacote não decrementa fora do grupo | T006 (b, c, d) | T025 |
| FR-009 falta não consome | T006 (i) | T025 |
| FR-010 cobrança fora de `RegraCobrancaPresenca` | n/a | T025 (`grep "Modalidade =="` e diff do backend e do frontend) |
| FR-012 frontend não decide quem é "outro aluno" | n/a | T026 (a divisão usa só `al.reposicao`) |
| FR-015 migração sem tocar vínculos, matrículas e aulas | T017 (contagens antes e depois) | T028 (diff do SQL, sem `DELETE`) |
| FR-016 sem segunda regra de conflito | T013 | T027 (`AulaRepository` sem diff relevante) |
| FR-019 encerramento só muda `ativo` | T022 | T028 (`grep` de `SaldoAulas`, `Valor` e `TotalAulasPacote` no serviço, sem atribuição) |
| FR-020 excluir o aluno não mexe no pacote | T023 | T028 (`AlunoService` sem diff) |
| FR-022 dois pacotes ativos do mesmo grupo | T012 | T029 |
| Sem arquivo novo indevido | n/a | T030 (`git status --porcelain -- src tests frontend database`) |

## Constituição

- **I. Exclusão lógica**: atendido sem exceção. A migração, o encerramento e a saída só mudam `ativo` ou `turma_id`; nada é apagado.
- **II. Regra no backend**: atendido. Escopo do pacote, fechamento (`fechada` calculada no servidor), combinação, encerramento e reativação ficam no backend; o frontend só agrupa pelo `reposicao` e exibe `fechada` (T025 confere).
- **III. Nada configurável sem efeito**: atendido. `Turmas:IntervaloEncerramentoMinutos` é lido pela rotina (T019); tipo e datas têm efeito real.
- **IV. Segredos e perfil**: sem impacto; a rotina não envia e-mail.
- **V. Documentação retroativa**: atendido (T024).

## Riscos para a implementação

- **Alto**: a regra de cobrança de presença muda (T005) e vários testes da spec 056 assumem o pacote valendo em qualquer aula (T007). Rodar a suíte inteira a cada passo e registrar no relatório cada teste reescrito.
- **Alto**: a migração (T002/T017) altera `turma.tipo` e `vinculo_cobranca.turma_id`. Só com backup, conferindo as contagens antes e depois; nunca em produção.
- **Médio**: a rotina em segundo plano (T019) é código novo de execução contínua; seguir o padrão do `NotificacaoDispatcherService` e testar a operação idempotente (T022).
- **Médio**: pacotes inativados pelo fim da turma somem do painel "Pacotes de aulas" da Home, mesmo com saldo. É consequência de o painel listar só pacotes ativos; avaliar no card da Home se a professora quer ver "encerrado com saldo".
- **Médio**: até a professora decidir os casos listados pela migração, as presenças desses alunos em turma serão cobradas como avulsas (efeito aceito e escrito na spec).
- **Baixo**: se a 062 for implementada antes, o nome do tipo de decisão muda de `Mensalidade` para `PlanoPeriodico`.

## Decisões menores que dependem da sua confirmação

1. O cadastro do pacote de turma **matricula** o aluno; a matrícula direta só vale para quem já tem pacote ativo da turma (a volta).
2. Turma de pacote exige turma ativa, da professora e **não iniciada** no cadastro; turma migrada sem data nunca é "iniciada".
3. Tipo da turma, turma do pacote e **data de início** (depois de preenchida) não mudam; a data de fim pode ser ajustada.
4. Pacotes de turma: 0 ou N por aluno, um por turma; de aulas individuais, no máximo um.
5. Sair da turma é permitido e **não altera** o pacote enquanto a turma está em andamento.
6. A verificação de fim de turma roda ao iniciar e a cada 60 minutos; atraso máximo de uma hora depois da virada do dia.
7. A migração converte só o aluno de pacote matriculado em **uma** turma **ativa** onde todos têm pacote; o resto é listado (inclusive turma inativa).

## Próximos passos

1. Sua confirmação das decisões menores acima.
2. `implement` seguindo `tasks.md`, com relatório em `docs/relatorios/` e comentário no card ao final.
3. Depois: especificar os cards de recorrência de aulas e de saída do aluno de pacote.
