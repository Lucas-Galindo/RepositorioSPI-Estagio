# Análise de consistência: Recorrência na criação de aulas (spec 077)

**Data**: 2026-11-07 · **Natureza**: leitura cruzada de spec, plano, pesquisa, contrato, quickstart, tasks, constituição, regras do `CLAUDE.md` e do código atual (`AulaService`, `AulaForm`, `AulaRepository`). Sem alteração de código.

## Resultado

| Métrica | Valor |
|---|---|
| Requisitos funcionais / critérios de sucesso | 11 (FR-001 a FR-011) / 5 (SC-001 a SC-005) |
| Histórias | 2 (US1 e US2, ambas P1) |
| Tarefas | 19 (T001 a T019) |
| Cobertura de requisitos por tarefas | 100% |
| Achados críticos / violações da constituição | 0 / 0 |

## Achados

| ID | Categoria | Severidade | Achado | Resolução |
|---|---|---|---|---|
| A1 | Risco | Média | A atomicidade exige que a série vá num **único** salvamento; o cadastro único faz dois (aula e depois alunos) | T006 monta as aulas já com os `AulaAlunos` e salva uma vez; T008 testa "conflito não grava nada" |
| A2 | Lacuna | Baixa | "De hoje em diante" compara só a data: uma ocorrência de hoje em horário já passado é permitida | Declarado em Assumptions; consistente com o cadastro único (nasce "Agendada") |
| A3 | Lacuna | Baixa | Os alunos da turma valem **no momento do envio** para todas as datas, não por data | Declarado em Assumptions e FR-007 |
| A4 | Observação | — | Ação nova de controller sem `[ProducesResponseType]` de 404/409 vira 500 (lição das specs 065 e 075) | T007 exige declarar 400, 404 e 409 |
| A5 | Observação | — | O ambiente do usuário (API 7002, Next 3000) pode estar ligado | Quickstart e tarefas usam `--artifacts-path` e `page.route`; não encerrar nem buildar por cima |
| A6 | Observação | — | Pop-up da Agenda recebe `onSalvo` sem argumento usado | T012 passa a primeira aula da série; o pop-up só fecha e recarrega o mês |

## Verificação dos "MUST NOT" (regra do `CLAUDE.md`)

| Requisito | Tarefa de verificação |
|---|---|
| FR-001 (sem "Repetir" na edição) | T015(b) + Chrome (T017 passo 7) |
| FR-009 (aula única inalterada) | T015(a): diff só com acréscimos + JSON do `POST/PUT` igual ao T001; T008 |
| FR-010 (sem cálculo no frontend) | T015(c): leitura do diff e `grep` |
| FR-011 (sem banco/dependência) | T015(d) |

## Constituição

Sem violações: regras no servidor (Princípio II), contrato e nota na 064 (Princípio V), sem banco nem opção configurável.

## Próximo passo

`/speckit-implement` (a aprovação para implementar já foi dada pelo usuário ao pedir o fluxo completo).
