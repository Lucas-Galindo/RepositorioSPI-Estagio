# Análise de consistência: Calendário nos campos de data (spec 069)

**Data**: 2026-11-04
**Artefatos analisados**: `spec.md`, `plan.md`, `research.md`, `contracts/date-input.md`, `quickstart.md`, `tasks.md`, a constituição, as regras do `CLAUDE.md` e o código atual (`DateInput.tsx`, `TurmaForm.tsx`, `VinculoCobrancaFormModal.tsx`, `dashboard.css`).
**Natureza**: análise sem alteração de código. Os achados A1 a A3 foram aplicados nos documentos.

## Resultado

| Métrica | Valor |
|---|---|
| Requisitos funcionais / critérios de sucesso | 13 (FR-001 a FR-013) / 5 (SC-001 a SC-005) |
| Histórias | 3 (US1 e US2 P1, US3 P2) |
| Tarefas | 14 (T001 a T014) |
| Cobertura de requisitos por tarefas | 100% |
| Achados | 3 médios (todos aplicados), 2 baixos |
| Críticos | 0 |
| Violações da constituição | 0 |

## Achados

| ID | Categoria | Severidade | Onde | Achado | Resolução |
|---|---|---|---|---|---|
| A1 | Lacuna | Média | spec, contrato, T007 | O campo **"Fim da turma"** (`TurmaForm`) usa `min` no campo nativo, que impede escolher e enviar dia anterior ao início; o `DateInput` não tinha `min`. Migrar sem tratar perderia essa trava | Propriedade opcional `min` (vai ao auxiliar, o calendário não deixa escolher dia anterior); novo FR-013 e caso de borda; o servidor continua validando (a trava no envio do navegador deixa de existir e isso foi declarado) |
| A2 | Inconsistência | Média | plano, contrato, T003 | `.field` é uma coluna flex que estica os filhos diretos; com o campo dentro do invólucro ele deixaria de esticar nos formulários | Regras `.field .date-wrap` (bloco) e `.field .date-wrap .date-input { width: 100% }` |
| A3 | Inconsistência | Média | plano, T003 | `.field input` (preenchimento, sombra, fundo) atinge o auxiliar invisível nos formulários | O auxiliar usa o seletor `.date-wrap .date-nativo` (mais específico), com `opacity: 0` e altura zero |
| A4 | Observação | Baixa | spec | Os números da spec conferem com o código: 23 usos de `DateInput` em 14 arquivos, 5 campos `type="date"` (2 em `TurmaForm`, 3 em `VinculoCobrancaFormModal`) e 4 campos `type="month"` | Nenhuma |
| A5 | Observação | Baixa | spec | `required` no campo mascarado só exige texto não vazio (data parcial vira valor vazio); já era assim em todos os usos atuais do `DateInput` | Mantido; o servidor valida |

## Verificação dos "MUST NOT" (regra do `CLAUDE.md`)

| Requisito | Tarefa de verificação |
|---|---|
| FR-003 (sem conversão de data) | T011(a): leitura do diff de `DateInput.tsx` e `grep` de `new Date(`, `Date.parse`, `toISOString` |
| FR-010 (backend intocado) | T011(b): `git status` e `git diff --stat` de `src`, `tests` e `database` vazios |
| FR-011 (sem componente novo, sem cor literal) | T011(c) e T011(d): `git status` sem `??` em `frontend` e `grep` de cores literais nas linhas adicionadas |
| FR-012 (nenhum campo nativo fora do componente) | T011(e): `grep` mostra só o auxiliar dentro de `DateInput.tsx` |

## Constituição

Sem violações: nada de regra de negócio no frontend (o servidor continua validando as datas), sem opção configurável nova, sem segredos, documentação (notas de Princípio V nas specs 062 e 063 e relatório).

## Riscos conferidos no código

- O auxiliar com `showPicker()` foi provado no Chrome (abre sem erro; `color-scheme` dark deixa o ícone nativo claro).
- O modal de parcelas usa o campo em linha com `flex: 1`: coberto pela propriedade `style` do invólucro (T008).
- O calendário aberto não sai em captura de tela; o tema dele é conferido pelo `color-scheme` calculado (declarado em R7).

## Próximos passos

1. `/speckit-implement` seguindo `tasks.md`, com relatório em `docs/relatorios/` e card movido para Revisão de Código/Conceito pelo hook.
