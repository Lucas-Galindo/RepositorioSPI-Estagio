# Análise de consistência: Melhoria do Código sem Mudar o Comportamento (spec 065)

**Data**: 2026-10-31
**Artefatos analisados**: `spec.md`, `plan.md`, `research.md`, `data-model.md`, `contracts/erros-da-api.md`, `quickstart.md`, `tasks.md`, constituição, regras do `CLAUDE.md` e o código atual (para conferir as premissas).
**Natureza**: análise sem alteração de código. As correções abaixo foram aplicadas nos próprios documentos.

## Resultado

| Métrica | Valor |
|---|---|
| Requisitos funcionais | 16 (FR-001 a FR-016) |
| Critérios de sucesso | 6 (SC-001 a SC-006) |
| Histórias | 5 (US1 a US5; a US5 é opcional) |
| Tarefas | 25 (T001 a T025) |
| Requisitos com pelo menos uma tarefa | 16 de 16 |
| "MUST NOT" com tarefa de verificação | 7 de 7 |
| Achados críticos | 0 |
| Violações da constituição | 0 |
| Achados totais | 7 (6 corrigidos, 1 risco aceito) |

## Achados

| ID | Categoria | Gravidade | Onde | Resumo | Correção aplicada |
|---|---|---|---|---|---|
| A1 | Premissa errada | **Alta** | spec, research, T005 | A investigação marcou `misturar` e `CONTRASTE_MINIMO` como exports sem uso, mas a busca só olhava `.ts` e `.tsx`. O script `verificar-contraste.mjs` os **importa**. Removê-los quebraria `npm run verificar-contraste` | Os dois ficam. Sobram `decodeJwtPayload` e `amostraMaisProxima`. A T005 já mandava conferir o `.mjs` antes; spec, pesquisa, quickstart e tarefa foram corrigidas |
| A2 | Ambiguidade | Média | FR-016 | Pedia "nota nas specs afetadas" sem dizer quais; nenhuma regra muda, então não havia o que anotar e nem tarefa | FR-016 reescrito: nenhuma spec recebe nota; o relatório (T025) registra onde o tratamento de erro passou a morar |
| A3 | Cobertura | Média | US2, T009, T010 | O instantâneo da API só **amostra** as 87 ações; uma ação fora da amostra poderia mudar de 500 para 404/409 sem ninguém ver | T010 passa a gerar a tabela ação × (declara 404/409, capturava 404/409) antes e depois e comparar; a investigação já mostrou 84 de 87 consistentes e os 3 desvios explicados |
| A4 | Risco | Média | filtro | O filtro depende de `ProducesResponseType` declarado: uma ação nova que declare 409 sem lançar conflito não muda nada, mas uma que lance conflito sem declarar vira 500 | Documentado em contracts/erros-da-api.md; os testes da T007 cobrem os dois lados (declarado e não declarado) |
| A5 | Numeração | Baixa | pedido | O usuário chamou a spec de "063"; esse número é do pacote por turma e o 064 é a chamada nos detalhes | Spec numerada 065; avisado ao usuário |
| A6 | Dependência | Média | 064 | A 064 mexe em `AulaService`, `AulaForm`, `PresencaChecklist` e na configuração do corte de vigência | FR-010 e a T020 protegem essa zona; a 064 deve ser implementada **depois** desta |
| A7 | Risco aceito | Baixa | US5 | Extrair telas copiadas pode alterar a aparência sem querer | Só se faz com capturas idênticas (FR-014); é P3 e opcional |

## Cobertura: requisito → tarefas

| Requisito | Tarefas |
|---|---|
| FR-001 não mudar comportamento | T002, T010, T013, T021 |
| FR-002 remover código morto | T004, T005, T006 |
| FR-003 CSS só se nenhum uso | T004, T022 |
| FR-004 tratamento de erro em um lugar | T007, T008, T009, T010 |
| FR-005 exceções específicas ficam | T009, T010 |
| FR-006 lógica repetida em um lugar | T011, T012, T013 |
| FR-007 comentários de regra | T014, T015, T016 |
| FR-008 só comentário não altera código | T017 |
| FR-009 comentário não repete o código | T022 |
| FR-010 zona da 064 intacta | T020 |
| FR-011 instantâneo antes e depois | T002, T010, T013, T021 |
| FR-012 capturas idênticas | T003, T018, T021 |
| FR-013 testes sem mudar expectativas | T021, T024 |
| FR-014 telas só se idênticas | T018, T019 |
| FR-015 relatório com antes e depois | T001, T025 |
| FR-016 sem notas; relatório registra | T025 |

## Verificação dos "MUST NOT" (regra do `CLAUDE.md`)

| Requisito | Por teste / instantâneo | Por leitura do diff, `grep` ou `git status` |
|---|---|---|
| FR-001 não mudar comportamento | testes (464) e instantâneo da API | T021 (`git status -- database`, diff de `tests` sem mudar `Assert`) |
| FR-003 não remover CSS em uso | capturas de tela | T004 (`grep` com texto dinâmico), T022 |
| FR-008 só comentário | T017 (compara o código sem comentários, antes e depois) | T017 |
| FR-009 comentário que repete o código | n/a | T022 (revisão) |
| FR-010 zona da 064 | n/a | T020 (`git diff` dos arquivos da zona) |
| FR-014 telas só se idênticas | capturas (T018, T019) | T018 |
| Sem arquivo novo indevido | n/a | T023 (`git status --porcelain`) |

## Constituição

- **I**: sem impacto. **II**: reforçado (o mapeamento de erros e as decisões repetidas passam a existir uma vez). **III**: sem impacto. **IV**: o filtro devolve o mesmo corpo de hoje, sem expor mais. **V**: nenhuma regra muda; sem notas.

## Riscos para a implementação

- **Alto**: a transformação mecânica dos 17 controllers. Mitigação: script que só aceita os `catch` padronizados, diff lido arquivo a arquivo, build, testes, instantâneo e a tabela de A3.
- **Médio**: o projeto de testes passa a referenciar `SPI.Api` (executável web). Se o build reclamar, o teste do filtro vai para um projeto próprio.
- **Médio**: o filtro lê metadados do endpoint; se a ordem de execução dos filtros ou a leitura do metadado não funcionar como esperado, o instantâneo (404/409/500) acusa na hora.
- **Baixo**: a remoção do `PackageReference` pode quebrar; nesse caso volta.

## Próximos passos

1. `implement` seguindo `tasks.md`, na ordem: rede de segurança, US1, US2, US3, US4 e, se sobrar fôlego, US5.
2. Depois, `/speckit-plan` da spec 064.
