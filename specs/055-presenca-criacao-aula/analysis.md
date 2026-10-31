# Análise de consistência: Presença na criação da aula (spec 055)

**Data do registro**: 2026-10-29 (**retroativo**)
**Natureza**: esta spec foi implementada (commit `d44b5a9`) antes de a convenção "um `analysis.md` por spec" existir. Este arquivo documenta a análise feita na época (resumida no card do Trello) e refaz, **lendo `spec.md` e `tasks.md`**, o mapa de cobertura. Nenhum código foi alterado ao gerá-lo.

## Resultado

| Métrica | Valor |
|---|---|
| Requisitos funcionais | 10 (FR-001 a FR-010) |
| Critérios de sucesso | 4 (SC-001 a SC-004) |
| Tarefas | 20 (T001 a T020), 20 concluídas |
| Requisitos com pelo menos uma tarefa | 10 de 10 |
| "MUST NOT" com verificação por leitura do diff ou `git status` | 1 de 1 (FR-010) |
| Achados críticos | 0 |
| Violações da constituição | 0 |

## Achados da análise original

| ID | Categoria | Gravidade | Resumo | Correção aplicada na época |
|---|---|---|---|---|
| C1 | Lacuna | Média | Relógio do navegador e do servidor podem divergir perto da meia-noite; a aula "retroativa" é decidida nos dois lados | Caso de borda acrescentado à spec; a regra final é a do servidor |
| E1 | Cobertura | Média | FR-006 (pacote esgotado bloqueia) só tinha teste no caminho de "Registrar Sessão", não no caminho retroativo | Teste (f) explícito em T009 |
| C2 | Ordem | Média | A ordem entre criar a aula e recalcular frequência/lembrete não estava explícita | T004 passou a fixar a ordem (`RecalcularAsync` só quando **não** retroativa) |

## Achado durante a implementação (fora do plano)

- **T020**: a validação real mostrou que o trigger legado `trg_aula_before_update_frequencia` somava +1 de frequência a todos os alunos ativos da turma ao marcar a aula como realizada, duplicando a contagem. Removido pela migração `database/20_remove_trigger_frequencia.sql`. Registrado aqui porque a análise original não podia prever (o trigger não estava descrito em nenhuma spec).

## Cobertura: requisito → tarefas (lida em `tasks.md`)

| Requisito | Tarefas |
|---|---|
| FR-001 lista de presença no formulário | T005, T006, T007, T016 |
| FR-002 sem lista para data futura | T007, T016 (§3, §4) |
| FR-003 marcado = presente | T006, T007 |
| FR-004 aula criada já "Realizada" | T001, T003, T004, T009 |
| FR-005 reaproveitar a lógica de cobrança | T002, T004, T014 |
| FR-006 pacote esgotado continua bloqueando | T002, T009 (caso f), T016 |
| FR-007 presença de todos obrigatória | T003, T009 (caso a) |
| FR-008 editar descrição de aula realizada | T011, T012, T013, T017 |
| FR-009 demais campos continuam bloqueados | T011, T012, T013 |
| FR-010 não alterar a regra de cobrança (MUST NOT) | T010, T014, T018 |
| SC-001 a SC-004 | T016, T017, T018 (quickstart §1 a §7) |

## Verificação dos "MUST NOT" (regra do `CLAUDE.md`)

| Requisito | Por teste | Por leitura do diff / `git status` |
|---|---|---|
| FR-010 não duplicar a regra de cobrança e saldo | T010 (suíte existente de `RegistrarSessao` segue verde), T018 | T014 (diff de `AulaService.cs`: `CadastrarAsync` só chama o método extraído) |
| Sem arquivo novo indevido | n/a | T015 (`git status --porcelain -- src frontend`: único arquivo novo é `PresencaChecklist.tsx`) |
| Não regredir a página da aula (FR-009/SC-003) | n/a | T008 (`git diff` de `aulas/[id]/page.tsx` vazio) |

## Constituição

- **II. Regra no backend**: atendido; a cobrança fica em `AulaService` (método extraído, T002). O frontend só envia `presencas`.
- **I. Exclusão lógica**, **IV. Segredos**: sem impacto.

## Riscos residuais

- **Frequências antigas**: aulas realizadas antes de T020 podem ter frequência dobrada. A decisão sobre corrigir os dados antigos ficou com o usuário e **continua em aberto**.
- Os relatórios da revisão de código (`docs/relatorios/2026-10-29-revisao-codigo-cards-em-revisao.md`, achado L2) registram que a criação retroativa não é atômica (vários salvamentos). Não corrigido.

## Pendências manuais

Nenhuma tarefa aberta (as 20 estão concluídas).
