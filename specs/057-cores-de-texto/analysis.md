# Análise de consistência: Cores de texto com contraste automático (spec 057)

**Data do registro**: 2026-10-29 (**retroativo**)
**Natureza**: spec implementada (commit `ba4e5d0`) antes de a convenção "um `analysis.md` por spec" existir. Este arquivo registra o achado da análise original e refaz, **lendo `spec.md` e `tasks.md`**, o mapa de cobertura. Nenhum código foi alterado ao gerá-lo.

## Resultado

| Métrica | Valor |
|---|---|
| Requisitos funcionais | 12 (FR-001 a FR-012) |
| Critérios de sucesso | 4 (SC-001 a SC-004) |
| Tarefas | 19 (T001 a T019), 18 concluídas, 1 manual aberta (T019) |
| Requisitos com pelo menos uma tarefa | 12 de 12 |
| "MUST NOT" com verificação por `grep` ou leitura do diff | 4 de 4 (FR-001, FR-006, FR-007, e arquivos novos) |
| Achados críticos | 0 |
| Violações da constituição | 0 |

## Achado da análise original

| ID | Categoria | Gravidade | Resumo | Correção aplicada na época |
|---|---|---|---|---|
| A1 | Lacuna | Média | Faltava dizer onde mora a migração das preferências antigas (cor livre e modo "Personalizado") | Ficou em `migrarPreferencias` dentro de `lib/paleta.ts` (T012) e é conferida pelo script de contraste (T013) |

## Cobertura: requisito → tarefas (lida em `tasks.md`)

| Requisito | Tarefas |
|---|---|
| FR-001 toda cor de texto vem de token (MUST NOT literal) | T001, T008, T009, T010, T014 |
| FR-002 cada superfície com cor de texto definida | T003, T008 |
| FR-003 contraste mínimo 4.5:1 em todas as combinações | T002, T003, T004, T018 |
| FR-004 paleta num único lugar | T003, T006, T007 |
| FR-005 destaque só entre amostras nomeadas | T003, T005, T011 |
| FR-006 sem seletor livre nem modo Personalizado (MUST NOT) | T011, T015 |
| FR-007 sem destaque fora da paleta (MUST NOT) | T005, T012, T015 |
| FR-008 migrar destaque fora da paleta para a amostra mais próxima | T012, T013 |
| FR-009 migrar "Personalizado" para "Padrão" | T012, T013 |
| FR-010 daltonismo preservado | T003, T004, T011 |
| FR-011 troca imediata sem recarregar | T005, T006, T019 (manual) |
| FR-012 nota na spec 051 | T016 |
| SC-001 a SC-004 | T004, T013, T014, T015, T018 |

## Verificação dos "MUST NOT" (regra do `CLAUDE.md`)

| Requisito | Verificação |
|---|---|
| FR-001/SC-002 sem cor literal de texto | T014: `grep -rnE "#[0-9a-fA-F]{3,8}\b"` em `frontend/` e só sobra `lib/paleta.ts` (a regra é de código, e a prova é o `grep`, não a aparência da tela) |
| FR-006/FR-007 sem cor livre | T015: `grep` de `type="color"`, `personalizado`, `customAccent`, `accentColor` sem ocorrência fora de `migrarPreferencias` |
| Contraste | T004 e T018: `npm run verificar-contraste` (2 × 4 × 6 combinações) |
| Sem arquivo novo indevido | T017: `git status --porcelain -- frontend specs` (novos: `lib/paleta.ts`, `scripts/verificar-contraste.mjs` e os documentos da spec) |

## Constituição

- **III. Nada configurável sem efeito**: atendido (o seletor livre e o modo "Personalizado" foram removidos, não escondidos).
- **V. Documentação retroativa**: atendido (T016 anotou a spec 051).
- **II. Regra no backend**: sem impacto (só frontend).

## Riscos residuais

- A garantia de contraste vale para os pares **declarados** em `contracts/tokens-de-tema.md`. Uma tela nova que use outro par precisa entrar na tabela do script.
- Achado M2 da revisão de código: `amostraMaisProxima` é exportada sem uso externo. Cosmético.

## Pendências manuais

- **T019**: percorrer o quickstart §2 num navegador real. O usuário informou que fechou este card (2026-10-29); a tarefa fica registrada como validada por ele, sem evidência escrita no `tasks.md`.
