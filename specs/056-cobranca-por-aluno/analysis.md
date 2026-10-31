# Análise de consistência: Cobrança por aluno e reposição de aulas (spec 056)

**Data do registro**: 2026-10-29 (**retroativo**)
**Natureza**: spec implementada (commit `020be7a`) antes de a convenção "um `analysis.md` por spec" existir. Este arquivo registra as correções que a análise original provocou e refaz, **lendo `spec.md` e `tasks.md`**, o mapa de cobertura. Nenhum código foi alterado ao gerá-lo.

## Resultado

| Métrica | Valor |
|---|---|
| Requisitos funcionais | 23 (FR-001 a FR-023) |
| Critérios de sucesso | 5 (SC-001 a SC-005) |
| Tarefas | 33 (T001 a T033), 33 concluídas |
| Requisitos com tarefa **explícita** | 22 de 23 |
| Requisitos sem tarefa própria | 1 (FR-006, ver achado R1 abaixo) |
| "MUST NOT" com verificação por teste **e** por leitura do diff/`git status` | 4 de 4 |
| Achados críticos | 0 |
| Violações da constituição | 0 |

## Achados da análise original

A análise da época encontrou lacunas de cobertura e de dados existentes; as correções foram aplicadas em `tasks.md` antes da implementação:

- A migração foi **renumerada para 21** (já existiam as de 20 e anteriores).
- Entrou a tarefa **T018** (ajustar os testes antigos que codificavam o modelo por contexto), pois eles quebrariam com a nova regra.
- Os "MUST NOT" de FR-020/FR-021 (não alterar vínculos herdados) ganharam a verificação por leitura do diff da migração (**T028**) e a regra de duplicação de prioridade ganhou **T030**.
- A nota nas specs 037, 038, 039, 041 e 052 (FR-023, Princípio V) virou a tarefa **T029**.

## Achado novo deste registro retroativo

| ID | Categoria | Gravidade | Resumo | Situação |
|---|---|---|---|---|
| R1 | Cobertura | Baixa | **FR-006** (teto de "aulas incluídas" da mensalidade conta só presença, em qualquer aula) não tem tarefa própria em `tasks.md`. O comportamento já existia (spec 039) e continua coberto por testes antigos (`AulaServiceGerarContasAReceberMensalidadeTests`), mas a spec 056 não o verifica de forma explícita para aula **individual** | Aceito como risco baixo; se o teto for tocado de novo, acrescentar um caso de aula individual |

## Cobertura: requisito → tarefas (lida em `tasks.md`)

| Requisito | Tarefas |
|---|---|
| FR-001 mensalidade só sem turma | T007, T008, T013 |
| FR-002 mensalidade recusa outro vínculo ativo | T008, T011 |
| FR-003 pacote/avulsa recusados com mensalidade ativa | T008, T011 |
| FR-004 mensalidade não gera cobrança por aula (MUST NOT) | T006, T010 |
| FR-005 cobrança mensal automática igual | T009, T012 |
| FR-006 teto de aulas incluídas | **sem tarefa própria** (achado R1) |
| FR-007 pacote só sem turma | T007, T008, T013 |
| FR-008 no máximo um pacote ativo | T008, T017 |
| FR-009 presença consome 1 aula em qualquer aula | T006, T016 |
| FR-010 falta não consome (MUST NOT) | T016 |
| FR-011 avulsa continua por contexto | T008, T015 |
| FR-012 ordem de prioridade | T005, T006, T015, T030 |
| FR-013 aviso de pacote esgotado cobrado como avulsa | T006, T015, T016 |
| FR-014 auditoria do vínculo aplicado | T006 |
| FR-015 adicionar aluno de reposição | T019, T021, T022, T024 |
| FR-016 remover aluno de reposição | T019, T021, T022, T024 |
| FR-017 recusar em aula realizada/cancelada/individual | T019, T022 |
| FR-018 editar aula preserva reposição | T020, T022 |
| FR-019 mesmas regras de presença e cobrança | T016, T022 |
| FR-020 migração de vínculos para o aluno | T002, T028 |
| FR-021 não alterar vínculos em conflito (MUST NOT) | T025, T026, T027, T028 |
| FR-022 prevalência enquanto o conflito existir | T005, T015 |
| FR-023 notas nas specs antigas | T029 |
| SC-001 a SC-005 | T010, T012, T016, T022, T028, T033 |

## Verificação dos "MUST NOT" (regra do `CLAUDE.md`)

| Requisito | Por teste | Por leitura do diff / `git status` |
|---|---|---|
| FR-004 sem cobrança por aula com mensalidade | T010 | T030 (ordem só em `RegraCobrancaPresenca`) |
| FR-010 falta não consome pacote | T016 | T030 |
| FR-020/FR-021 não alterar vínculos herdados | T026 (conflito é só marcado) | T028 (o único comando sobre dados na migração é `UPDATE ... SET turma_id = NULL` para Mensalidade/Pacote) |
| Sem arquivo novo indevido | n/a | T031 (`git status --porcelain -- src frontend database tests`) |

## Constituição

- **II. Regra no backend**: atendido; a prioridade de cobrança existe só em `RegraCobrancaPresenca`.
- **V. Documentação retroativa**: atendido (T029 anotou as specs 037, 038, 039, 041 e 052).
- **I. Exclusão lógica**: atendido (nenhum vínculo é apagado ou desativado pela migração).

## Riscos residuais

- **Migração 21 em produção**: ainda não aplicada fora do banco local. Exige backup antes e conferência dos vínculos antes e depois. O usuário aplica.
- Achado L1 da revisão de código: aula excluída ainda aceita registro de presença/cancelamento (inclui as rotas de participantes). Não corrigido.
- Achado L2: o registro de aula não é atômico (`AplicarPresencasAsync` e `GerarContasAReceberAsync` salvam em momentos diferentes). Não corrigido.

## Pendências manuais

Nenhuma tarefa aberta (as 33 estão concluídas). A tela da reposição foi conferida pelo usuário.
