# Análise de consistência: Link da aula temporário (spec 067)

**Data**: 2026-11-03
**Artefatos analisados**: `spec.md`, `plan.md`, `research.md`, `data-model.md`, `contracts/aulas-link-api.md`, `quickstart.md`, `tasks.md`, a constituição, as regras do `CLAUDE.md` e o código atual (para conferir as premissas).
**Natureza**: análise sem alteração de código. Os ajustes A1, A2, A3 e A6 foram aplicados no `tasks.md`; os demais não pedem ação.

## Resultado

| Métrica | Valor |
|---|---|
| Requisitos funcionais / critérios de sucesso | 12 (FR-001 a FR-012) / 6 (SC-001 a SC-006) |
| Histórias | 3 (US1, US2, US3, todas P1) |
| Tarefas | 23 (T001 a T023) |
| Requisitos com pelo menos uma tarefa | 12 de 12 (100%); SC 6 de 6 |
| "MUST NOT" com tarefa de verificação | todos |
| Achados críticos / altos | 0 / 0 |
| Achados médios / baixos | 2 / 4 (todos tratados) |
| Violações da constituição | 0 |

## Achados

| ID | Categoria | Gravidade | Onde | Resumo | Tratamento |
|---|---|---|---|---|---|
| A1 | Risco | Média | T017 | O fake `FakeLembreteRepositoryParaOrcamento` lança `NotImplementedException` em `ObterProximaAulaDaTurmaAsync`; se o dispatcher chamasse essa busca com `links` nulo, os testes atuais quebrariam | T017 exige a busca **somente quando `links != null`** |
| A2 | Dependência | Média | T022, spec 064 | Sem a 064, aula com data de hoje exige presença e nasce "Realizada" (gera conta de teste); ela ainda aceita link, então a validação funciona, mas suja dados de teste | T022 avisa e recomenda a 064 antes; dados "TESTE-067" apagados no fim |
| A3 | Subespecificação | Baixa | T009 | `Mapear` é `private static`; precisa virar método de instância para acessar o armazenamento e o relógio | T009 explicita a mudança |
| A4 | Ambiguidade | Baixa | spec (decisão 2) × plano | "Não aparece em listas" refere-se à **exibição**; o campo vem em toda resposta porque o popup usa a aula da lista | Esclarecido em T009 e no plano (research R5) |
| A5 | Consistência | Baixa | spec FR-012 | A nota do e-mail vai na spec `001-lembretes-canal-email` (a pasta `049` é de outro assunto) | Já corrigido na spec e no plano |
| A6 | Cobertura | Baixa | tasks | Os SC não eram citados por id nas tarefas | Tabela "Cobertura dos critérios de sucesso" acrescentada |

## Verificação dos "MUST NOT" (regra do `CLAUDE.md`)

| Requisito | Verificação |
|---|---|
| FR-002 sem banco/arquivo | T015: `git status`/`git diff` de `database` e `Persistence` vazios, `grep` de `LinkAula` sem resultado, `DESCRIBE aula` igual |
| FR-003 só `http`/`https` | T006 (testes) e T012 (leitura do diff do validador) |
| FR-005 sem link expirado | T006, T013, T015 |
| FR-008 regra de envio intacta | T016 e T018 (leitura do diff + testes atuais sem alterar `Assert`) |
| FR-009 sem log do link | T003 e T012 (`grep` em chamadas de log e mensagens de exceção); T022 confere `logs/` |
| FR-010 aula sem link como hoje | T008, T012, T016 (texto exato do e-mail) |
| FR-011 frontend não decide | T012: leitura do diff e `grep` por `new Date`/`new URL`/`startsWith`/`http` |
| Sem arquivo novo indevido | T020: `git status --porcelain` com a lista esperada |

## Constituição

- **I** sem impacto (nada em banco). **II** reforçado: validação, elegibilidade e expiração só no servidor; a tela exibe `linkAula`/`aceitaLinkAula`. **III** sem opção nova. **IV** o link (pode ter senha) não é gravado nem logado, esquema restrito, conteúdo protegido no e-mail e na página. **V** nota na spec 001 (T019).

## Riscos conferidos no código

- `ProcessarLembretesVencidosAsync` já recebe `ILembreteRepository`, que expõe `ObterProximaAulaDaTurmaAsync`; o parâmetro novo é opcional, então as chamadas existentes continuam compilando.
- `AulasController` recebe os validadores no construtor; o novo validador entra do mesmo modo. As ações novas declaram 400/404/409, o que o `ErrosDeNegocioFilter` (spec 065) exige para devolver 404/409.
- O `LinkAulaTemporario` é singleton e o `AulaService` é scoped: a injeção é válida; o dispatcher resolve o singleton do provedor.
- Reiniciar a API perde os links (risco aceito pelo usuário; a tela avisa).

## Próximos passos

1. `/speckit-implement` na ordem: fase 1, fase 2 (fundação), US1, US3, US2 e acabamento. Recomenda-se implementar a spec 064 antes.
2. Sincronizar o Trello (os hooks `backlog`, `design`, `a-fazer` e `analyze` falharam por timeout de rede em 2026-11-03).
