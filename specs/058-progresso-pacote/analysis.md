# Análise de consistência: Progresso do pacote de aulas (spec 058)

**Data do registro**: 2026-10-29 (**retroativo**)
**Natureza**: spec implementada (commit `97413c9`) antes de a convenção "um `analysis.md` por spec" existir. A análise original terminou com **0 achados**. Este arquivo refaz, **lendo `spec.md` e `tasks.md`**, o mapa de cobertura. Nenhum código foi alterado ao gerá-lo.

## Resultado

| Métrica | Valor |
|---|---|
| Requisitos funcionais | 11 (FR-001 a FR-011) |
| Critérios de sucesso | 4 (SC-001 a SC-004) |
| Tarefas | 17 (T001 a T017), 16 concluídas, 1 manual aberta (T017) |
| Requisitos com pelo menos uma tarefa | 11 de 11 |
| "MUST NOT" com verificação por teste e por leitura do diff/`git status` | 3 de 3 |
| Achados críticos | 0 |
| Violações da constituição | 0 |

## Cobertura: requisito → tarefas (lida em `tasks.md`)

| Requisito | Tarefas |
|---|---|
| FR-001 aulas dadas = presenças com o vínculo (falta e cancelada não contam) | T002, T003, T007 |
| FR-002 restantes = total − dadas, mínimo 0 | T001, T003 |
| FR-003 estado do pacote | T001, T003 |
| FR-004 ficha e Home usam o mesmo cálculo, num único lugar | T001, T004, T009, T013 |
| FR-005 ficha mostra total, dadas, restantes, estado | T004, T008 |
| FR-006 lista das aulas dadas de um pacote | T004, T005, T006, T008 |
| FR-007 campo "Total de aulas" e saldo inicial | T004, T006, T008 |
| FR-008 edição recusa total menor que as dadas | T004, T006 |
| FR-009 seção "Pacotes de aulas" na Home | T009, T010, T011 |
| FR-010 filtro por aluno e período | T009, T010, T011 |
| FR-011 não criar campo nem tabela (MUST NOT) | T012, T016 |
| SC-001 dadas = aulas listadas | T006, T015 |
| SC-002 ficha e Home iguais | T006, T010, T015 |
| SC-003 falta e cancelada nunca alteram | T007 |
| SC-004 percentuais corretos | T003, T015 |

## Verificação dos "MUST NOT" (regra do `CLAUDE.md`)

| Requisito | Por teste | Por leitura do diff / `git status` |
|---|---|---|
| FR-001 falta e aula cancelada não contam | T007 (presença, falta e cancelada do mesmo aluno; só a presença fica ligada ao pacote) | T012 |
| FR-011 sem campo/tabela novo | n/a | T012 (`git status --porcelain -- database` sem arquivo novo e leitura do diff de `Domain/Entities` e `Persistence/Configurations`: nenhuma propriedade nem mapeamento novo) |
| Princípio II: frontend não recalcula | n/a | T013 (a única conta no frontend é a porcentagem da barra, `dadas/total`) |
| Sem arquivo novo indevido | n/a | T016 (`git status --porcelain -- src tests frontend`) |

## Constituição

- **II. Regra no backend**: atendido (`ProgressoPacote.Calcular` é a única fonte; T013 confere o frontend).
- **V. Documentação retroativa**: atendido (T014 anotou as specs 041 e 052: o painel virou progresso; o bloqueio continua pelo saldo).

## Riscos residuais

- **Dois conceitos de "restantes"**: o painel mostra `total − dadas`, mas o **bloqueio** de presença continua usando o **saldo**. Se alguém ajustar o saldo à mão, os dois divergem. Está documentado na spec 041 (T014), mas não há alerta na tela.
- Em 2026-10-29 a Home deixou de exibir "Concluídos" e "Aulas dadas no período". O backend ainda devolve `concluidos` e `aulasDadasNoPeriodo` (card pendente "Estudar implementação de dashboard de aulas").

## Pendências manuais

- **T017**: conferir a ficha do aluno e a Home num navegador real.
