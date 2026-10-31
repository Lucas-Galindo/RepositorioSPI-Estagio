# Análise de consistência: Notificações de aula pendente (spec 059)

**Data do registro**: 2026-10-29 (**retroativo**)
**Natureza**: spec implementada (commit `031a7e7`) antes de a convenção "um `analysis.md` por spec" existir. Este arquivo registra o achado da análise original e refaz, **lendo `spec.md` e `tasks.md`**, o mapa de cobertura. Nenhum código foi alterado ao gerá-lo.

## Resultado

| Métrica | Valor |
|---|---|
| Requisitos funcionais | 13 (FR-001 a FR-013) |
| Critérios de sucesso | 4 (SC-001 a SC-004) |
| Tarefas | 17 (T001 a T017), 16 concluídas, 1 manual aberta (T017) |
| Requisitos com pelo menos uma tarefa | 13 de 13 |
| "MUST NOT" com verificação por teste e por leitura do diff | 3 de 3 (FR-003, FR-004, FR-012) |
| Achados críticos | 0 |
| Violações da constituição | 0 |

## Achado da análise original

| ID | Categoria | Gravidade | Resumo | Correção aplicada na época |
|---|---|---|---|---|
| A1 | Lacuna | Média | Não estava dito que o sino só aparece para a Professora (FR-006) | A condição do cabeçalho entrou em T010 |

## Cobertura: requisito → tarefas (lida em `tasks.md`)

| Requisito | Tarefas |
|---|---|
| FR-001 definição de aula pendente | T004, T007 |
| FR-002 exatamente uma notificação por aula pendente | T001 (índice único), T004, T007 |
| FR-003 nunca duplicar (MUST NOT) | T001, T007, T012 |
| FR-004 aulas anteriores à vigência não notificam (MUST NOT) | T006 (`AulasPendentesDesde`), T007 |
| FR-005 rotina periódica e verificação ao carregar | T005, T006, T008 |
| FR-006 só as notificações das próprias aulas | T008, T010 |
| FR-007 contador do sino (sem número quando é zero) | T010 |
| FR-008 lista das mais recentes para as mais antigas | T008, T010 |
| FR-009 abrir marca como lida, sem resolver | T008, T009 |
| FR-010 popup com os dados e o atalho | T010 |
| FR-011 aviso resolvido quando a aula deixa de ser pendente | T004, T007 |
| FR-012 resolvida não volta (MUST NOT) | T007, T012 |
| FR-013 tipo da notificação extensível | T001, T002, T003 |
| SC-001 a SC-004 | T007, T011, T014 |

## Verificação dos "MUST NOT" (regra do `CLAUDE.md`)

| Requisito | Por teste | Por leitura do diff / `git status` |
|---|---|---|
| FR-003/FR-012 sem duplicata e sem reaparecer | T007 (execuções repetidas mantêm 1) | T012 (um único ponto de criação, `AdicionarAsync`, sempre depois de checar `(tipo, aula)`; nenhum `Remove`) |
| FR-004 sem notificação antes da vigência | T007 | T006 |
| Princípio II: frontend não calcula "pendente" | n/a | T013 (o frontend só exibe o que a API devolve e conta `lida == false`) |
| Sem arquivo novo indevido | n/a | T015 (`git status --porcelain -- src tests frontend database`) |

## Constituição

- **II. Regra no backend**: atendido (T013).
- **III. Nada configurável sem efeito**: atendido (`Notificacoes:AulasPendentesDesde` e `IntervaloVerificacaoSegundos` são lidos pela rotina).
- **I. Exclusão lógica**: atendido (notificação resolvida é marcada, não apagada).

## Riscos residuais

- A spec 060 **reescreveu** parte desta (`SincronizarAsync` com criar/reativar/resolver e novos tipos). Esta análise vale para o comportamento de aula pendente, que a 060 preservou (T009 da 060 manteve os testes).
- Achado L1 da revisão de código: aula **excluída** ainda aceita registro e cancelamento. Se acontecer com uma aula já notificada, o aviso é resolvido no próximo ciclo, mas o dado fica inconsistente. Não corrigido.

## Pendências manuais

- **T017**: conferir o sino, o painel e o popup num navegador real.
