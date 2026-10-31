# Análise de consistência: Detalhes da aula em pop-up na Home (spec 061)

**Data do registro**: 2026-10-29 (**retroativo**)
**Natureza**: spec implementada (commit `1ee1325`) antes de a convenção "um `analysis.md` por spec" existir. A análise original terminou com **0 achados**. Este arquivo refaz, **lendo `spec.md` e `tasks.md`**, o mapa de cobertura. Nenhum código foi alterado ao gerá-lo.

## Resultado

| Métrica | Valor |
|---|---|
| Requisitos funcionais | 9 (FR-001 a FR-009) |
| Critérios de sucesso | 4 (SC-001 a SC-004) |
| Tarefas | 9 (T001 a T009), 8 concluídas, 1 manual aberta (T009) |
| Requisitos com pelo menos uma tarefa | 9 de 9 |
| "MUST NOT" com verificação por leitura do diff, `grep` ou `git status` | 3 de 3 (FR-006, FR-007 e a proibição de cor literal do FR-008) |
| Achados críticos | 0 |
| Violações da constituição | 0 |

## Cobertura: requisito → tarefas (lida em `tasks.md`)

| Requisito | Tarefas |
|---|---|
| FR-001 clicar na aula abre o pop-up, sem navegar | T001, T003 |
| FR-002 pop-up mostra matéria, data, horário, turma/individual, descrição | T001 |
| FR-003 alunos; Presente/Faltou só em aula realizada | T001 |
| FR-004 botões "Abrir aula" e "Fechar" | T001 |
| FR-005 fechar por botão, clique fora e Esc; foco devolvido | T001, T009 (manual) |
| FR-006 sem editar, cancelar, excluir nem registrar presença (MUST NOT) | T004 |
| FR-007 sem nova consulta ao servidor (MUST NOT) | T005 |
| FR-008 claro/escuro, telas estreitas, sem cor literal | T002, T006, T009 (manual) |
| FR-009 componente reutilizável | T001 (props `aula` e `onFechar`), T003 |
| SC-001 a SC-004 | T004, T005, T008, T009 |

## Verificação dos "MUST NOT" (regra do `CLAUDE.md`)

| Requisito | Verificação |
|---|---|
| FR-006 sem ações de edição | T004: leitura do diff de `AulaDetalhesPopup.tsx` (só "Fechar" e "Abrir aula"; nenhum botão de editar, cancelar ou excluir) |
| FR-007 sem consulta nova | T005: leitura do diff do componente e de `dashboard/page.tsx` (o componente importa **só tipos** de `lib/api/aulas`) |
| FR-008 sem cor literal | T006: `grep -nE "#[0-9a-fA-F]{3,8}\b"` em `styles/dashboard.css` e no componente; `npm run verificar-contraste` |
| Sem arquivo novo indevido | T007: `git status --porcelain -- frontend src database tests` (novo: só `components/aulas/AulaDetalhesPopup.tsx`) |

## Constituição

- **II. Regra no backend**: atendido; o pop-up só exibe dados já carregados e não calcula nada.
- **I., III., IV., V.**: sem impacto.

## Riscos residuais

- O pop-up depende de a agenda trazer os alunos e as presenças da semana. Se a API da Home deixar de enviá-los, o pop-up mostrará a aula sem alunos, sem erro visível.
- A Home já não exibe "Concluídos" e "Aulas dadas no período" (commit `b27c5f9`); isso não muda esta spec.

## Pendências manuais

- **T009**: percorrer o quickstart no navegador (aula realizada, agendada, reposição; Esc, clique fora e retorno do foco; modo escuro; 360px).
