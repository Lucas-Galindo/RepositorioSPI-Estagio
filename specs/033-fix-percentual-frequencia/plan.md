# Implementation Plan: Corrigir Cálculo de "% Frequência do Aluno"

**Branch**: `033-fix-percentual-frequencia` | **Date**: 2026-09-18 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/033-fix-percentual-frequencia/spec.md`

**Note**: This template is filled in by the `/speckit-plan` command; its definition describes the execution workflow.

## Summary

Corrigir `RelatorioService.ObterHistoricoAlunoAsync` (`GET /api/relatorios/historico-aluno/{alunoId}`) para calcular `PercentualFrequencia` a partir de presenças reais do aluno dentro do filtro aplicado, em vez do contador vitalício `Aluno.Frequencia`, sempre que qualquer filtro (`inicio`, `fim`, `status` ou `turmaId`) restringir o conjunto de aulas consideradas. Sem filtro nenhum, mantém o comportamento atual (numerador = `Aluno.Frequencia`). A correção reaproveita dados já carregados por `_aulaRepository.ListarAsync` (que já inclui `AulaAlunos` com `Presente`), sem nenhuma consulta nova ao banco.

## Technical Context

**Language/Version**: C# / .NET (backend), mesma versão já usada no projeto.

**Primary Dependencies**: Nenhuma nova dependência — ajuste de lógica em um método já existente (`RelatorioService.ObterHistoricoAlunoAsync`), reaproveitando dados já carregados pela consulta já existente (`IAulaRepository.ListarAsync`).

**Storage**: MySQL via EF Core — nenhuma consulta nova. `IAulaRepository.ListarAsync` já inclui `.Include(a => a.AulaAlunos)` (confirmado em [AulaRepository.cs:25-38](../../src/SPI.Infrastructure/Repositories/AulaRepository.cs#L25-L38)), então `AulaAluno.Presente` já está disponível em memória depois da chamada existente — só é preciso parar de descartar esse dado no mapeamento.

**Testing**: xUnit (`tests/SPI.Application.Tests`). Não há teste existente cobrindo `ObterHistoricoAlunoAsync` (a confirmar em research.md) — esta feature é uma boa oportunidade de adicionar cobertura nova, já que o bug é puramente de lógica de aplicação (fácil de testar com um fake de `IAulaRepository`/`IAlunoRepository`, sem banco real).

**Target Platform**: ASP.NET Core Web API (`SPI.Application`), sem impacto em `SPI.Api` (assinatura do controller e contrato de resposta inalterados — mesmo campo `PercentualFrequencia`, mesmo tipo) nem em frontend (endpoint sem consumidor hoje, confirmado na investigação prévia da spec).

**Project Type**: Web application (estrutura já existente do repositório).

**Performance Goals**: Sem meta nova — a correção não adiciona nenhuma consulta ao banco; apenas reaproveita, em memória, dados que a consulta já existente já carrega.

**Constraints**: O numerador correto (presenças reais) MUST ser contado apenas entre as aulas com `Status == "Realizada"` que já compõem o denominador (mesmo filtro) — não é uma contagem independente. O critério de "algum filtro restringe o conjunto" (que decide se usa presença real ou `Aluno.Frequencia`) MUST considerar `inicio`, `fim`, `status` e `turmaId` (decisão da clarificação — qualquer um desses já dispara o recálculo).

**Scale/Scope**: 1 método alterado (`RelatorioService.ObterHistoricoAlunoAsync`, [RelatorioService.cs:49-71](../../src/SPI.Application/Relatorios/Services/RelatorioService.cs#L49-L71)), possível extração de um pequeno helper privado de mapeamento reaproveitado por `ObterAgendaAsync` (para não duplicar a lógica de `RelatorioAgendaItem` em dois lugares). Nenhuma mudança de contrato de API, nenhuma entidade nova, nenhuma migração.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Princípio | Aplica-se? | Avaliação |
|---|---|---|
| I. Exclusão Lógica, Nunca Física | Não | Nenhuma exclusão de registro envolvida. |
| II. Validação de Negócio Única e Centralizada no Backend | Não diretamente | Não há regra de validação duplicada entre frontend/backend — o endpoint não tem consumidor de UI hoje (confirmado na spec). |
| III. Nenhuma Capacidade Configurável Sem Implementação Real | Não | Não introduz nenhuma opção configurável nova. |
| IV. Autenticação e Segredos Seguros por Padrão | Não | Sem relação com autenticação/segredos. |
| V. Documentação Retroativa como Registro Histórico Vinculante | Sim | `specs/013-relatorio-historico-aluno/spec.md` documenta explicitamente o comportamento antigo (contador vitalício com denominador filtrado) como "aceito... não corrigido neste registro retroativo" (Assumptions) — esta é exatamente a correção que aquela spec antecipava. `specs/013` MUST ser atualizada (Acceptance Scenario 2, FR-002, Edge Cases, Assumptions) para refletir o novo comportamento, referenciando esta spec (033) — ação registrada como tarefa de documentação em tasks.md (Polish).

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
└── SPI.Application/
    └── Relatorios/
        └── Services/
            └── RelatorioService.cs   # ObterHistoricoAlunoAsync: corrige o calculo do numerador;
                                       # possivel extracao de helper de mapeamento compartilhado
                                       # com ObterAgendaAsync (RelatorioAgendaItem)

tests/
└── SPI.Application.Tests/
    └── Relatorios/
        └── RelatorioServiceHistoricoAlunoTests.cs   # novo arquivo -- primeira cobertura deste metodo

specs/
└── 013-relatorio-historico-aluno/spec.md   # atualizar (Principio V): refletir o novo calculo
```

**Structure Decision**: Mudança isolada à camada `SPI.Application` do backend (um método de um service já existente) — sem tocar em `SPI.Domain`, `SPI.Infrastructure` (nenhuma query nova, nenhuma coluna de banco) nem `SPI.Api`/`frontend` (contrato de resposta inalterado, endpoint sem consumidor de UI).

## Post-Design Constitution Check

*Re-avaliação após Phase 1 (research.md, data-model.md, quickstart.md).*

Nenhuma mudança em relação ao Constitution Check inicial. Confirmações após o design:
- Princípio V: `research.md` (R3) confirma exatamente a regra de gatilho já definida na
  clarificação da spec (qualquer um de `inicio`/`fim`/`status`/`turmaId`), o que será refletido
  na atualização de `specs/013-relatorio-historico-aluno/spec.md`.
- `data-model.md` confirma que a correção usa apenas campos já existentes (`AulaAluno.Presente`,
  já carregado hoje e descartado), sem nenhuma consulta nova — reforça a ausência de impacto de
  performance e a ausência de mudança de contrato de API.

**Resultado**: Nenhuma violação. Nenhuma linha em Complexity Tracking necessária.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

Não aplicável — nenhuma violação identificada em nenhum dos dois Constitution Checks.
