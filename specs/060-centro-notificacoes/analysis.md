# Análise de consistência: Centro de notificações e lembretes (spec 060)

**Data do registro**: 2026-10-29 (**retroativo**)
**Natureza**: spec implementada (commit `b179133`) antes de a convenção "um `analysis.md` por spec" existir. A análise original terminou com **0 achados**. Este arquivo refaz, **lendo `spec.md` e `tasks.md`**, o mapa de cobertura. Nenhum código foi alterado ao gerá-lo.

## Resultado

| Métrica | Valor |
|---|---|
| Requisitos funcionais | 15 (FR-001 a FR-015) |
| Critérios de sucesso | 4 (SC-001 a SC-004) |
| Tarefas | 20 (T001 a T020), 19 concluídas, 1 manual aberta (T020) |
| Requisitos com pelo menos uma tarefa | 15 de 15 |
| "MUST NOT" com verificação por teste e por leitura do diff/`grep` | 3 de 3 (FR-003, FR-007, FR-015) |
| Achados críticos | 0 |
| Violações da constituição | 0 |

## Cobertura: requisito → tarefas (lida em `tasks.md`)

| Requisito | Tarefas |
|---|---|
| FR-001 pendência de pagamento | T006, T007 |
| FR-002 uma notificação por aluno com pendência | T001 (`uq_notificacao_tipo_professor_aluno`), T006, T007 |
| FR-003 sem duplicata; reativa se a pendência muda (MUST NOT) | T001, T007, T015 |
| FR-004 resolvida quando não há mais pendência | T006, T007 |
| FR-005 popup com a lista de pendências e o atalho | T013 |
| FR-006 e-mail semanal conforme a regra (dia D, hora H) | T010, T011 |
| FR-007 no máximo um e-mail com sucesso por aluno por dia de envio (MUST NOT) | T001 (histórico `envio_lembrete_pagamento`), T010, T011, T015 |
| FR-008 falha registrada e tentada de novo no mesmo dia | T010, T011 |
| FR-009 cada tentativa registrada | T005, T010, T012 |
| FR-010 regra editável e com efeito real | T012, T014, T018 |
| FR-011 aviso "aula futura" | T004, T006, T008 |
| FR-012 aviso resolvido se a aula sai da janela | T006, T008 |
| FR-013 sino soma os tipos; filtro Todas/tipo | T013 |
| FR-014 só a professora vê notificações | T013, T018 |
| FR-015 em desenvolvimento, todo e-mail vai para o endereço de teste (MUST NOT enviar a outro) | T002, T003, T016 |
| SC-001 a SC-004 | T007, T008, T011, T018 |

## Verificação dos "MUST NOT" (regra do `CLAUDE.md`)

| Requisito | Por teste | Por leitura do diff / `grep` / `git status` |
|---|---|---|
| FR-003 sem notificação duplicada | T007 | T015 (`NotificacaoService`: ponto único de criação) |
| FR-007 sem e-mail duplicado e sem e-mail à professora | T011 | T015 (`LembretePagamentoService`) |
| FR-015 sem e-mail real em desenvolvimento e **sem endereço de teste gravado no repositório** | T003 | T016 (`grep` nos `appsettings*.json`: nenhum endereço gravado; o valor fica nos User Secrets) |
| Sem arquivo novo indevido | n/a | T019 (`git status --porcelain -- src tests frontend database`) |

## Constituição

- **III. Nada configurável sem efeito**: atendido (a regra do lembrete é editável e lida pela rotina; `RedirecionarTodosPara` é lido pelo envio).
- **IV. Segredos e perfil**: atendido (o endereço de teste mora nos User Secrets; sem ele, o envio em desenvolvimento é recusado).
- **V. Documentação retroativa**: atendido (T017 anotou a spec 059).
- **I. Exclusão lógica**: atendido (notificações resolvidas, não apagadas).

## Riscos residuais

- **L3 (revisão de código)**: em falha parcial de gravação do histórico, `LembretePagamentoService.TentarEnviarAsync` pode reenviar o e-mail de cobrança no ciclo seguinte (e-mail duplicado, contra a intenção de FR-007). Não corrigido; está na lista de correções da revisão.
- **SMTP real**: as credenciais do Gmail (conta Google dedicada com 2FA e Senha de app) ainda não foram criadas. Sem elas, nenhum e-mail sai de verdade. Não definir `RedirecionarTodosPara` em produção.
- O card "Melhorar título dos lembretes de aula" (inclui o "Falha Falha") continua no Backlog.
- A migração 23 ainda não foi aplicada em produção (backup antes).

## Pendências manuais

- **T020**: conferir o sino e a aba Lembretes num navegador real; opcionalmente testar com credenciais SMTP reais, mantendo o redirecionamento para o endereço de teste.
