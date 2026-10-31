# Análise de consistência: Semestralidade e Anuidade (spec 062)

**Data**: 2026-10-29
**Artefatos analisados**: `spec.md`, `plan.md`, `research.md`, `data-model.md`, `contracts/`, `quickstart.md`, `tasks.md`, constituição do projeto e regras do `CLAUDE.md`.
**Natureza**: análise somente leitura. As correções abaixo foram aplicadas depois, nos próprios documentos da spec. Nenhum código foi alterado.

## Resultado

| Métrica | Valor |
|---|---|
| Requisitos funcionais | 18 (FR-001 a FR-018) |
| Critérios de sucesso | 5 (SC-001 a SC-005) |
| Tarefas | 31 (T001 a T031), das quais 1 manual (T031) |
| Requisitos com pelo menos uma tarefa | 18 de 18 (100%) |
| "MUST NOT" com teste **e** verificação por leitura do diff, `grep` ou `git status` | 6 de 6 |
| Achados críticos | 0 |
| Violações da constituição | 0 |
| `[NEEDS CLARIFICATION]` restantes | 0 |
| Achados totais | 6 (todos corrigidos) |

## Achados

| ID | Categoria | Gravidade | Onde | Resumo | Correção aplicada |
|---|---|---|---|---|---|
| A1 | Cobertura | Média | FR-018 | Parcelas aparecerem em Contas a Receber e no aviso de pagamento pendente não tinha tarefa nem cenário | Cenário 16 no `quickstart.md` e T030 passou a cobrir 17 cenários |
| A2 | Cobertura | Média | Edge cases da spec | Plano com início no passado, parcela antes do início e plano com uma única parcela sem teste | Acrescentados à T011 |
| A3 | Ambiguidade | Média | FR-012 | Faltava definir o que acontece com a parcela que vence **hoje** na exclusão | Spec e T018: a que vence hoje **não** é cancelada, a de amanhã é |
| A4 | Lacuna | Média | Spec | Não estava dito que um plano vencido e ainda ativo continua bloqueando novos vínculos até ser excluído ou renovado | Novo caso de borda na spec |
| A5 | Clareza | Baixa | T016 | Um item de teste estava confuso ("plano vencido não impede Pacote ou Avulsa") | Reescrito como caso defensivo (dado antigo) |
| A6 | Terminologia | Baixa | Spec e plano | "Plano" tinha dois sentidos (Semestral/Anual na spec; "plano periódico" no código, incluindo a Mensalidade) | Glossário nas premissas da spec |

## Cobertura: requisito → tarefas

| Requisito | Tarefas |
|---|---|
| FR-001 modalidades Semestral e Anual, sem turma | T002, T003, T006, T007, T008 |
| FR-002 início, valor total, ao menos 1 parcela, fim calculado | T008, T009, T011, T012 |
| FR-003 1 a 24 parcelas, uma por mês | T008, T011, T012 |
| FR-004 sugerir parcelas e permitir ajuste | T013, T014 |
| FR-005 diferença de soma como aviso, sem bloquear | T011, T014 |
| FR-006 criar tudo de uma vez, tudo ou nada | T009, T011, T025 |
| FR-007 nunca duplicar parcela | T002 (trava do banco), T011, T025 |
| FR-008 exclusividade e renovação antecipada | T010, T011 |
| FR-009 vigência na presença | T015, T016 |
| FR-010 regra de cobrança só no backend | T015, T026 |
| FR-011 plano travado, sem edição | T014, T017, T018, T026 |
| FR-012 exclusão cancela só as pendentes futuras, sem apagar | T017, T018, T027 |
| FR-013 reativar recusado | T017, T018 |
| FR-014 um aviso de renovação por aluno | T020, T021 |
| FR-015 aviso resolvido na renovação ou exclusão | T020, T021 |
| FR-016 popup e filtro no sino | T022 |
| FR-017 lembrete de vencimento para as parcelas, sem e-mail real em desenvolvimento | T023, T030 |
| FR-018 parcelas em Contas a Receber, no aviso de pagamento pendente e nos relatórios | T030 (cenário 16) |

## Verificação dos "MUST NOT" (regra do `CLAUDE.md`)

| Requisito | Verificação por teste | Verificação por leitura do diff, `grep` ou `git status` |
|---|---|---|
| FR-005 não bloquear por diferença de soma | T011 | T014 (aviso na tela) |
| FR-006/FR-007 não gravar pela metade nem duplicar | T011 | T025 (único ponto que cria parcelas e um único salvamento) |
| FR-009 não cobrar por presença no período | T016 | T015 |
| FR-010 frontend não decide cobrança | n/a (sem testes de frontend) | T026 (leitura do diff do frontend) |
| FR-011 não editar | T018 | T026 (`grep` de edição no frontend) |
| FR-012 não apagar pagamento | T018 | T027 (`grep` de `Remove`/`ExecuteDelete`) |
| Sem arquivo novo indevido | n/a | T028 (`git status --porcelain`) |

## Constituição

- **I. Exclusão lógica**: atendido (vínculo inativado, parcelas "Cancelado", nada apagado).
- **II. Regra no backend**: atendido. A sugestão de parcelas iguais no formulário é conveniência, e o backend revalida tudo.
- **III. Nada configurável sem efeito**: atendido (`RenovacaoDiasAntes` é lido e usado).
- **IV. Segredos e perfil**: sem impacto.
- **V. Documentação retroativa**: atendido (T024 acrescenta notas nas specs 037, 039 e 056).

## Riscos para a implementação

- **Alto**: `RegraCobrancaPresenca` (renomear `Mensalidade` para `PlanoPeriodico` e receber a data da aula) mexe em testes existentes (T015). Fazer antes dos testes novos.
- **Médio**: a migração troca um `CHECK` que já existe (`DROP CHECK` e novo `ADD`). Aplicar só com backup e conferir os vínculos antes e depois (T001, T002).
- **Médio**: a mensagem de chave duplicada do repositório de vínculos hoje cita "Avulsa"; precisa ficar genérica (T005).
- **Baixo**: vigência e aviso de renovação dependem da data do servidor, como as demais rotinas.

## Próximos passos

1. Aprovação do usuário das decisões menores (listadas na spec e no card).
2. `implement` seguindo `tasks.md`, com relatório em `docs/relatorios/` e comentário no card ao final.
