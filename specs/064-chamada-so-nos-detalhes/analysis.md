# Análise de consistência: Chamada só nos detalhes da aula (spec 064)

**Data**: 2026-10-31
**Artefatos analisados**: `spec.md`, `plan.md`, `research.md`, `data-model.md`, `contracts/`, `quickstart.md`, `tasks.md`, a constituição, as regras do `CLAUDE.md` e o código atual (para conferir as premissas).
**Natureza**: análise sem alteração de código. Os ajustes A1 a A5 e A7 foram aplicados no `tasks.md`; A6, A8 e A9 não pedem ação.

## Resultado

| Métrica | Valor |
|---|---|
| Requisitos funcionais / critérios de sucesso | 17 (FR-001 a FR-017) / 6 (SC-001 a SC-006) |
| Histórias | 4 (US1 a US4) |
| Tarefas | 31 (T001 a T031) |
| Requisitos com pelo menos uma tarefa | 17 de 17 (100%) |
| "MUST NOT" com tarefa de verificação | todos (ver tabela abaixo) |
| Achados críticos / altos | 0 / 0 |
| Achados médios / baixos | 4 / 5 (todos tratados) |
| Violações da constituição | 0 |

## Achados

| ID | Categoria | Gravidade | Onde | Resumo | Tratamento |
|---|---|---|---|---|---|
| A1 | Cobertura | Média | edge "Aula individual", T014 | Nenhum teste cobria a regra de horário numa aula individual | T014 passou a repetir bloqueio e liberação com aula sem turma |
| A2 | Cobertura | Média | edge "data passada… conflito", T010 | Nada provava que a aula antiga continua sujeita ao conflito de horário | T010 ganhou o caso de conflito em data passada |
| A3 | Cobertura | Média | FR-003, T018 | "Chamada somente nos detalhes" só era provada por ausência de outro uso | T018 ganhou `grep` de `registrarSessao` (um único ponto de uso) |
| A4 | Risco | Média | T009 | O relógio padrão de 2100 nos fakes faz toda aula parecer "já iniciada" | T009 exige comentário explicando e dizendo onde o bloqueio é testado |
| A5 | Ambiguidade | Baixa | SC-004 | "Notificação até a tela" = 2 cliques; contando do sino fechado são 3 | T030 registra que a contagem começa no item da notificação |
| A6 | Ambiguidade | Baixa | FR-012 | "ou o tipo da aula": só existem turma e atendimento individual | Sem ação; o `Contexto` atual cobre; registrar no relatório |
| A7 | Consistência de texto | Baixa | FR-005, T016/T017 | Mensagem do servidor sem acento; a da tela, com acento | Intencional (padrão das mensagens do backend); nota na T018 |
| A8 | Dependência | Baixa | T002 | Scripts de linha de base vivem no scratchpad da spec 065 | Já há o fallback "recriar" |
| A9 | Terminologia | Baixa | "chamada" × "registrar sessão" | O código usa o nome do endpoint existente | Mantido |

## Verificação dos "MUST NOT" (regra do `CLAUDE.md`)

| Requisito | Verificação |
|---|---|
| FR-001 sem Presença/“A data já chegou…” | T013: leitura do diff, `grep` e `git status --porcelain -- frontend/components` |
| FR-002 criação sem frequência/cobrança/presenças | T010 e T013: testes e leitura do diff de `CadastrarAsync` |
| FR-004 comparação não só pela data | T008 (minuto exato) e T018 (leitura do diff de `RegraChamada`) |
| FR-007 tela não decide pelo relógio do navegador | T018: leitura do diff e `grep` por `new Date`/`Date.now`/`dataIsoLocal` |
| FR-009 realizada não aceita nova chamada | T014 e T018 |
| FR-010 sem aviso duplicado / FR-011 em andamento sem aviso | T019 e T023 |
| FR-015 coluna não preenchida retroativamente / FR-016 auditoria não altera | T004, T025 e T026: leitura da migração e da consulta, contagens antes e depois |
| Sem arquivo novo indevido | T028: `git status --porcelain` com a lista esperada |

## Constituição

- **I**: nada é apagado; a auditoria é só leitura. **II**: reforçado, pois a regra "chamada liberada" existe uma vez (`RegraChamada`) e a tela só exibe `chamadaLiberada`. **III**: reforçado, pois `Notificacoes:AulasPendentesDesde` é removida em vez de ficar sem efeito. **IV**: sem impacto. **V**: notas nas specs 055 e 059 (T027).

## Riscos conferidos no código

- Os testes que registram chamada constroem as aulas com `Ativo = true` (o padrão da entidade é `false`), então o novo 404 para aula excluída não quebra teste antigo.
- A ação `RegistrarSessao` já declara `ProducesResponseType(409)`, que o `ErrosDeNegocioFilter` (spec 065) exige para devolver 409.
- Cinco implementações de `IAulaRepository` em teste precisam perder o parâmetro `desde` (T019) e cinco construções de `AulaService` ganham o relógio (T009); ambas listadas por arquivo e linha.
- O servidor de desenvolvimento resolve `America/Sao_Paulo` (testado); um servidor Linux sem `tzdata` cairia no fallback do id do Windows e, falhando, em erro explícito (T006/T008).

## Próximos passos

1. `/speckit-implement`, em ordem: fase 1, fase 2 (fundação), US2 e US1 juntas (o formulário ainda cria aulas "Realizada" até a US1), US3, US4 e acabamento.
2. Aplicar a migração 26 em produção só depois, pelo usuário, com backup.
