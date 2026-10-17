# Implementation Plan: Código de Exclusão de Professora via E-mail da Própria Professora

**Branch**: `054-codigo-exclusao-email-professora` | **Date**: 2026-10-17 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/054-codigo-exclusao-email-professora/spec.md`

## Summary

Trocar o destinatário do e-mail de confirmação de exclusão de professora (`ProfessorService.SolicitarExclusaoAsync`): de `admin.Email` para `professor.Email`, reescrevendo o corpo do e-mail para se dirigir à professora. Nenhuma mudança de schema, endpoint, ou nas regras de expiração/uso único/confirmação já existentes (specs/044) — só o destino e o texto do e-mail, mais um bloqueio defensivo quando a professora não tiver e-mail cadastrado.

## Technical Context

**Language/Version**: C# / .NET, mesma versão já usada no projeto

**Primary Dependencies**: Nenhuma nova — reaproveita `IEmailSender` (specs/043/049) já injetado em `ProfessorService`

**Storage**: Nenhuma mudança de schema — `ExclusaoProfessorToken` continua exatamente como está (specs/044)

**Testing**: xUnit, fakes manuais já existentes (`ProfessorServiceFakes.cs`) — mesmo padrão do restante do projeto

**Target Platform**: Backend ASP.NET Core existente

**Project Type**: Web application — esta feature é só backend (1 service + textos de e-mail + testes), nenhuma mudança de frontend

**Performance Goals**: N/A

**Constraints**: Zero mudança nas regras já especificadas em specs/044 (expiração 15min, uso único, invalidação por novo pedido, exclusão lógica, reativação sem 2FA) — FR-005 explícito

**Scale/Scope**: 1 arquivo de produção alterado (`ProfessorService.cs`), 1 arquivo de contrato atualizado, testes existentes ajustados + 1 teste novo (professora sem e-mail)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Princípio | Avaliação |
|---|---|
| I. Exclusão Lógica, Nunca Física | N/A direto — esta feature não muda a exclusão lógica em si (`Ativo = false`), só o destino do e-mail de confirmação. **Conforme** (inalterado). |
| II. Validação de Negócio Única e Centralizada no Backend | N/A — nenhuma validação nova de UI; o bloqueio de "professora sem e-mail" (FR-004) vive no mesmo `ProfessorService` que já centraliza essa regra, único ponto de entrada (endpoint Admin). **Conforme.** |
| III. Nenhuma Capacidade Configurável Sem Implementação Real (NON-NEGOTIABLE) | N/A — nenhuma opção nova exposta ao usuário; é uma troca de destinatário de um fluxo já 100% funcional. **Conforme.** |
| IV. Autenticação e Segredos Seguros por Padrão | N/A — nenhuma mudança de autenticação/segredo. |
| V. Documentação Retroativa como Registro Histórico Vinculante | **Aplica-se diretamente**: esta feature reverte conscientemente FR-002 de specs/044. Por este princípio, a spec mais recente que descreve o comportamento atual prevalece — specs/044 permanece como registro histórico do comportamento anterior (não é reescrita), e esta spec (054) passa a ser a fonte de verdade vigente. Já tratado explicitamente em spec.md (Nota de investigação prévia + Assumptions). **Conforme**, com a mudança registrada corretamente. |

**Resultado**: Sem violação. Nenhuma entrada necessária em Complexity Tracking.

## Project Structure

### Documentation (this feature)

```text
specs/054-codigo-exclusao-email-professora/
├── plan.md              # This file (/speckit-plan command output)
├── research.md          # Phase 0 output (/speckit-plan command)
├── data-model.md        # Phase 1 output (/speckit-plan command) — N/A, sem entidade nova
├── quickstart.md        # Phase 1 output (/speckit-plan command)
├── contracts/           # Phase 1 output (/speckit-plan command) — delta de specs/044
└── tasks.md             # Phase 2 output (/speckit-tasks command - NOT created by /speckit-plan)
```

### Source Code (repository root)

```text
src/SPI.Application/Professores/Services/
└── ProfessorService.cs          # SolicitarExclusaoAsync: destino e corpo do e-mail, + FR-004

tests/SPI.Application.Tests/Professores/
└── ProfessorServiceExclusaoTests.cs   # testes ajustados ao novo destino + 1 teste novo (FR-004)
```

**Structure Decision**: Backend puro, 1 arquivo de produção (`ProfessorService.cs`) + 1 arquivo de teste já existente ajustado — nenhum arquivo novo de produção, nenhuma mudança de schema/endpoint/frontend.

## Complexity Tracking

Não aplicável — Constitution Check acima não identificou nenhuma violação.
