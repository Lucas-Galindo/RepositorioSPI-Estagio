# Implementation Plan: Corrigir Agrupamento por Turma no Relatório Financeiro

**Branch**: `029-fix-relatorio-financeiro-turma` | **Date**: 2026-09-17 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/029-fix-relatorio-financeiro-turma/spec.md`

**Note**: This template is filled in by the `/speckit-plan` command; its definition describes the execution workflow.

## Summary

O agrupamento "Por Turma" do Relatório Financeiro (`RelatorioService.ObterFinanceiroAsync`) atualmente atribui o valor de cada `Pagamento` à primeira turma da lista geral de turmas do aluno (`Aluno.AlunosTurma.FirstOrDefault()`), ignorando qual turma a aula efetivamente coberta pelo pagamento pertence. A correção troca essa fonte pelo vínculo real já existente — `Pagamento` → `PagamentoAula` → `Aula.TurmaId` — e define uma regra explícita para o caso (já possível hoje, apenas em lançamento manual) de um pagamento cobrir aulas de mais de uma turma: nesse caso o valor entra em um grupo novo, "Múltiplas turmas", em vez de ser atribuído a uma única turma ou duplicado entre elas. É uma correção pontual de lógica de agregação dentro de um método já existente, sem mudança de schema, sem novo endpoint e sem alteração de contrato de resposta.

## Technical Context

**Language/Version**: C# / .NET (mesma versão já usada no projeto, ver `SPI.Application.csproj`)

**Primary Dependencies**: Nenhuma nova dependência. Usa apenas Entity Framework Core (já em uso via `IRelatorioRepository`/`SpiDbContext`) e LINQ em memória (mesmo padrão já usado no restante de `RelatorioService`).

**Storage**: MySQL (nenhuma alteração de schema — a correção usa colunas/relacionamentos já existentes: `pagamento_aula.aula_id`, `aula.turma_id`).

**Testing**: xUnit (`tests/SPI.Application.Tests`), seguindo o padrão já estabelecido em `RelatorioServiceLancamentosTests.cs` (fake manual de `IRelatorioRepository`, sem biblioteca de mocking).

**Target Platform**: ASP.NET Core Web API (backend existente), sem impacto direto no frontend Next.js além de os valores exibidos mudarem (o formato da resposta `RelatorioFinanceiroResponse.PorTurma` — lista de `{ Chave, Total }` — permanece o mesmo).

**Project Type**: Web application (backend `SPI.Application`/`SPI.Infrastructure`/`SPI.Domain`/`SPI.Api` + frontend `frontend/`, estrutura já existente do repositório).

**Performance Goals**: Sem meta nova — o método já carrega os `Pagamento`s do período em memória (via `ListarPagosNoPeriodoAsync`) antes de agrupar; a correção troca a chave de agrupamento usada nesse `GroupBy` em memória, sem adicionar novas consultas ao banco por pagamento (ver Phase 0 para confirmar que os `Include`s existentes já trazem os dados necessários).

**Constraints**: Nenhuma restrição nova. A correção MUST manter a soma dos grupos "Por Turma" igual ao "Total Recebido" já exibido (SC-003 da spec) e MUST NOT alterar os demais agrupamentos/indicadores da mesma tela (FR-006).

**Scale/Scope**: Escopo estritamente limitado ao método `RelatorioService.ObterFinanceiroAsync` (agrupamento `PorTurma`) e, se necessário na Fase 0, ao `Include` usado por `RelatorioRepository.ListarPagosNoPeriodoAsync` para garantir que `Pagamento.PagamentosAula → Aula.Turma` esteja carregado.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Princípio | Aplica-se? | Avaliação |
|---|---|---|
| I. Exclusão Lógica, Nunca Física | Não | Não há exclusão de registro envolvida nesta correção. |
| II. Validação de Negócio Única e Centralizada no Backend | Sim | A correção é puramente backend (lógica de agregação em `RelatorioService`); o frontend apenas exibe o resultado já agrupado pela API — nenhuma duplicação de lógica no cliente. Conforme. |
| III. Nenhuma Capacidade Configurável Sem Implementação Real | Não diretamente | Não introduz nenhuma opção configurável nova (nenhum enum/dropdown novo); o rótulo "Múltiplas turmas" é um valor calculado de exibição, não uma opção que o usuário escolhe. Conforme. |
| IV. Autenticação e Segredos Seguros por Padrão | Não | Sem relação com autenticação/segredos. |
| V. Documentação Retroativa como Registro Histórico Vinculante | Sim | Esta é uma mudança *intencional* de comportamento (não uma nova documentação retroativa) sobre uma spec já existente (015-relatorio-financeiro). Conforme o princípio, `specs/015-relatorio-financeiro/spec.md` MUST ser atualizada para refletir a nova regra de agrupamento por turma, referenciando esta spec (029), em vez de ficar desatualizada. Ação registrada como tarefa de documentação na Fase 1 / tasks.

**Resultado**: Nenhuma violação. Nenhuma linha em Complexity Tracking necessária.

## Project Structure

### Documentation (this feature)

```text
specs/[###-feature]/
├── plan.md              # This file (/speckit-plan command output)
├── research.md          # Phase 0 output (/speckit-plan command)
├── data-model.md        # Phase 1 output (/speckit-plan command)
├── quickstart.md        # Phase 1 output (/speckit-plan command)
├── contracts/           # Phase 1 output (/speckit-plan command)
└── tasks.md             # Phase 2 output (/speckit-tasks command - NOT created by /speckit-plan)
```

### Source Code (repository root)

```text
src/
├── SPI.Domain/
│   ├── Entities/              # Pagamento, PagamentoAula, Aula, Turma (sem alteração de campos)
│   └── Repositories/
│       └── IRelatorioRepository.cs
├── SPI.Application/
│   └── Relatorios/
│       └── Services/
│           └── RelatorioService.cs   # ObterFinanceiroAsync — método a corrigir (PorTurma)
├── SPI.Infrastructure/
│   └── Repositories/
│       └── RelatorioRepository.cs    # ListarPagosNoPeriodoAsync — confirmar/ajustar Include de Aula/Turma
└── SPI.Api/
    └── Controllers/
        └── RelatoriosController.cs   # sem alteração de rota/contrato

tests/
└── SPI.Application.Tests/
    └── Relatorios/
        └── RelatorioServiceLancamentosTests.cs  # padrão de teste a seguir (fake manual de IRelatorioRepository)
        # novo arquivo de teste para ObterFinanceiroAsync/PorTurma nesta mesma pasta

frontend/
└── (sem alteração funcional esperada — consome RelatorioFinanceiroResponse.PorTurma,
     cujo formato {Chave, Total} não muda; o novo rótulo "Múltiplas turmas" aparece
     como mais um item da lista existente, sem exigir mudança de componente)
```

**Structure Decision**: Web application já existente (Option 2 do template, backend .NET em `src/` + frontend Next.js em `frontend/`). A correção é isolada à camada Application/Infrastructure do backend (`RelatorioService` + `RelatorioRepository`), sem necessidade de tocar em `SPI.Domain` (nenhuma entidade nova ou campo novo) nem em `SPI.Api`/`frontend` (contrato de resposta inalterado).

## Post-Design Constitution Check

*Re-avaliação após Phase 1 (research.md, data-model.md, quickstart.md).*

Nenhuma mudança de avaliação em relação ao Constitution Check inicial. Confirmações após o
design:
- Princípio II (validação única no backend): confirmado — `data-model.md` mostra que a regra
  de agregação vive inteiramente em `RelatorioService`/`RelatorioRepository`; o frontend não
  precisa de nenhuma lógica nova (apenas exibe mais um item possível em `PorTurma`).
- Princípio V (documentação retroativa vinculante): a tarefa de atualizar
  `specs/015-relatorio-financeiro/spec.md` para referenciar esta correção (029) permanece
  necessária e será incluída em `tasks.md`.

**Resultado**: Nenhuma violação. Nenhuma linha em Complexity Tracking necessária.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

Não aplicável — nenhuma violação identificada em nenhum dos dois Constitution Checks.
