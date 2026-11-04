# Análise de consistência: Selo de cobrança do aluno (spec 068)

**Data**: 2026-11-03
**Artefatos analisados**: `spec.md`, `plan.md`, `research.md`, `data-model.md`, `contracts/selo-cobranca-api.md`, `quickstart.md`, `tasks.md`, a constituição, as regras do `CLAUDE.md` e o código atual (para conferir as premissas).
**Natureza**: análise sem alteração de código. Os achados A1 a A4 foram aplicados nos documentos (inclusive nas histórias e nos requisitos); A5 só vai para o relatório.

## Resultado

| Métrica | Valor |
|---|---|
| Requisitos funcionais / critérios de sucesso | 13 (FR-001 a FR-013) / 6 (SC-001 a SC-006) |
| Histórias | 3 (US1, US2, US3, todas P1) |
| Tarefas | 23 (T001 a T023) |
| Requisitos com pelo menos uma tarefa | 13 de 13 (100%); SC 6 de 6 |
| Achados críticos / altos | 0 / 0 |
| Achados médios / baixos | 2 / 3 (4 aplicados, 1 só registro) |
| Violações da constituição | 0 |

## Achados

| ID | Gravidade | Achado | Tratamento |
|---|---|---|---|
| A1 | **Média** | `GET /api/alunos` é chamado por 12 telas, quase todas só para escolher um aluno; calcular o resumo sempre custaria 2 consultas e mais dados em todas | Resumo **sob demanda**: parâmetro `incluirCobranca` (padrão falso); só a tela de Alunos pede. Novo **FR-013**, cenário 6 da US1, decisão menor 10, SC-003 estendido, research R10 e contrato |
| A2 | **Média** | O plano dizia "nenhum controller muda", o que o A1 invalida | Plano, T008 e T020 ajustados: `AlunosController.Listar` ganha o parâmetro opcional; é o único controller tocado (diff conferido por tarefa); `grep` de `incluirCobranca` só em `lib/api/alunos.ts` e `alunos/page.tsx` |
| A3 | Baixa | Com vínculos duplicados herdados, a lista escolhe o mais recente e `Decidir` (turma/chamada) escolhe o mais antigo | Decisão menor 11 na spec e nota no research R3; só diverge em dados já marcados como conflito |
| A4 | Baixa | Estimativa de "~15 construções de teste" imprecisa | Corrigida no research R6: 1 de `AlunoService`, 3 de `TurmaService` e cerca de 11 de `AulaService` |
| A5 | Baixa | Aula já realizada mostra o vínculo vigente, não o aplicado na época | Já documentado (research R9); registrar no relatório |

## Verificação dos "MUST NOT" (regra do `CLAUDE.md`)

| Requisito | Verificação |
|---|---|
| FR-006 sem N+1 | T004 (teste conta chamadas: 1 por lista), T010/T014/T018 (leitura do diff e `grep` de `listarVinculos`/`vinculos-cobranca`) |
| FR-007 frontend não decide | T010, T014, T018 (leitura do diff; `grep` de `.sort(`/`.find(`/`.filter(`) |
| FR-008 não alterar dado/regra | T020 (`git status` de `database`, diff vazio em `RegraCobrancaPresenca`/`ProgressoPacote`, `grep` de escrita no serviço novo), T022 (contagens iguais) |
| FR-009 só cores do tema | T010 (`grep` de cores literais, diff de `styles`/`paleta`), T021 (contraste) |
| FR-010 um componente novo | T010, T020 (`git status` do frontend) |
| FR-012 selo sem ação | T010, T014, T018 (sem `onClick`) |
| FR-013 resumo só sob demanda | T007 (teste: sem `incluirCobranca` o serviço não é chamado), T010 (`grep` de `incluirCobranca`), T022 (item 1b) |

## Constituição

- **I** sem impacto. **II** reforçado: a regra mora no servidor e reusa `Decidir`/`ProgressoPacote`. **III** sem opção nova. **IV** sem impacto. **V** notas nas specs 056 e 058 (T019).

## Riscos conferidos no código

- O repositório de turma já inclui o aluno (valor da aula), a aula traz os alunos completos e `ContarPresencasPorVinculosAsync` já existe em lote.
- O parâmetro opcional no construtor dos três serviços não quebra o DI nem os testes; a validação pela API (T022) confirma que o selo vem preenchido em produção.
- O `.slnx` não compila os testes: usar `dotnet test tests/SPI.Application.Tests` sem `--no-build` (registrado nas tarefas).

## Próximos passos

1. `/speckit-implement`, em ordem: fase 1, fase 2 (fundação), US1 (cria o componente), US2 e US3 e acabamento. Ainda **não** iniciado, por pedido do usuário.
