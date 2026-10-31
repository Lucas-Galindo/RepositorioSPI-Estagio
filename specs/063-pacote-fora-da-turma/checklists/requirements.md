# Specification Quality Checklist: Turmas de Pacote e Alunos de Pacote Fora do Cadastro Fixo

**Purpose**: Validate specification completeness and quality before proceeding to planning
**Created**: 2026-10-29
**Feature**: [spec.md](../spec.md)

## Content Quality

- [x] No implementation details (languages, frameworks, APIs)
- [x] Focused on user value and business needs
- [x] Written for non-technical stakeholders
- [x] All mandatory sections completed

## Requirement Completeness

- [x] No [NEEDS CLARIFICATION] markers remain
- [x] Requirements are testable and unambiguous
- [x] Success criteria are measurable
- [x] Success criteria are technology-agnostic (no implementation details)
- [x] All acceptance scenarios are defined
- [x] Edge cases are identified
- [x] Scope is clearly bounded
- [x] Dependencies and assumptions identified

## Feature Readiness

- [x] All functional requirements have clear acceptance criteria
- [x] User scenarios cover primary flows
- [x] Feature meets measurable outcomes defined in Success Criteria
- [x] No implementation details leak into specification

## Notes

- As dúvidas de negócio do card foram respondidas pelo usuário em 2026-10-29 e 2026-10-30 (migrar direto, reposição continua, escopo do pacote, pacote coletivo com matrícula, turma fechada) e estão na tabela de decisões da spec.
- **Revisão de 2026-10-30**: o usuário esclareceu que o aluno de pacote *é matriculado* na turma de pacote. A spec foi refeita nesse modelo (a primeira versão tirava o aluno da matrícula).
- A spec **reverte parte da 056** (pacote do aluno sem turma, pacote consumido em aula individual). A tabela "O que muda em relação à spec 056" deixa isso explícito.
- **Segunda rodada de 2026-10-30** (respostas do usuário): vários pacotes por aluno (no máximo um por turma de pacote); fim da turma inativa os pacotes com o saldo preservado (por data de fim, via rotina diária, ou por inativação da turma); excluir o aluno não mexe no pacote; reativação de pacote só com a turma em andamento; quem saiu pode voltar se o pacote da turma está ativo; consome primeiro o pacote da turma da aula. Entraram FR-019 a FR-021, SC-006, SC-007 e a User Story 5. (O plano, as tarefas e a análise foram refeitos depois; ver a atualização final abaixo.)
- **Terceira rodada de 2026-10-30**: acabou o "pacote avulso" que valia em qualquer aula. Todo pacote pertence a um grupo (**aulas individuais** ou **uma turma de pacote**), só é consumido em aula do seu grupo e há no máximo um pacote ativo por grupo (ajustado depois pelo usuário: pacotes de turma são 0 ou N por aluno, um por turma; só o pacote de aulas individuais é limitado a um). Fora do grupo o aluno paga como avulso. Isso **removeu** a regra de escolha entre vários pacotes e o consumo "fora do escopo". Migração: pacote de aluno sem matrícula vira o pacote de aulas individuais. Entrou FR-022. O plano, as tarefas e a análise continuam desatualizados. **Atualização final (2026-10-30)**: plan, research, data-model, contracts, quickstart e tasks (34 tarefas) refeitos; `/speckit-analyze` concluído (9 achados corrigidos, `analysis.md` atualizado).
- Recorrência de aulas e acerto de saída do aluno viraram **cards próprios** no Backlog (seção "Fora de escopo"); esta spec não depende deles.
- Os nomes de tabela e coluna aparecem apenas no contexto ("lido do código"), não nos requisitos.
