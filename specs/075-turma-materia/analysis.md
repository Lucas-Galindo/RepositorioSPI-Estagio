# Análise de consistência: Turma ↔ Matéria (spec 075)

**Data**: 2026-11-05 · **Natureza**: leitura cruzada de `spec.md`, `plan.md`, `research.md`, `data-model.md`, `contracts/turmas.md`, `quickstart.md`, `tasks.md`, da constituição e das regras do `CLAUDE.md`. Sem alteração de código.

## Resultado

| Métrica | Valor |
|---|---|
| Requisitos funcionais / critérios de sucesso | 9 (FR-001 a FR-009) / 4 (SC-001 a SC-004) |
| Histórias | 3 (US1 e US2 P1, US3 P2) |
| Tarefas | 20 (T001 a T020) |
| Cobertura de requisitos por tarefas | 100% |
| Achados críticos / violações da constituição | 0 / 0 |

## Achados

| ID | Categoria | Severidade | Achado | Resolução |
|---|---|---|---|---|
| A1 | Risco | Média | A migração é a única etapa com risco de dados | T001 faz backup e registra contagens de **todas** as tabelas; T003 exige contagens iguais depois e segunda execução inócua; T016(a) confere que o `.sql` só altera `turma` |
| A2 | Lacuna | Baixa | `PUT` com `materiaId` ausente remove o vínculo (diferente do tipo, em que ausente = mantém) | Declarado em FR-002, no contrato e em T007; o formulário sempre envia o valor atual |
| A3 | Lacuna | Baixa | Matéria inativada depois do vínculo precisa continuar editável | FR-003, T007 (não revalida a mesma matéria), T008 (teste) e T011 (campo mostra a atual como "(inativa)") |
| A4 | Observação | — | Construtor do `TurmaService` muda (injeta `IMateriaRepository`) | T007 atualiza os testes que o constroem; `dotnet test` sem `--no-build` |
| A5 | Observação | — | Ambiente do usuário (API 7002/Next 3000) pode estar ligado | Quickstart e tarefas usam `--artifacts-path` e `page.route`; não encerrar nem buildar por cima |

## Verificação dos "MUST NOT" (regra do `CLAUDE.md`)

| Requisito | Tarefa de verificação |
|---|---|
| FR-005 (sem perda/alteração de dados) | T016(a): leitura do `.sql` + contagens antes/depois |
| FR-007 (regra de aulas intacta) | T016(b): `grep` e diff de `Aulas/` |
| FR-008 (turmas sem matéria iguais) | T016(c): comparação de `GET /api/turmas` e diff do repositório |
| FR-009 (sem dependência/outras tabelas/produção) | T016(d) |

## Constituição

Sem violações: validação da matéria no servidor (Princípio II); contrato, modelo de dados, nota na 063 e relatório (Princípio V); migração segura, com backup, só no banco local.

## Próximo passo

Aguardar a aprovação do usuário para `/speckit-implement` (envolve alterar o banco local).
