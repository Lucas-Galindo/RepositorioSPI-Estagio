# Implementation Plan: Registrar Presença na Criação da Aula

**Branch**: `055-presenca-criacao-aula` | **Date**: 2026-10-18 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/055-presenca-criacao-aula/spec.md`

**Note**: This template is filled in by the `/speckit-plan` command; its definition describes the execution workflow.

## Summary

Hoje, lançar uma aula que já aconteceu exige dois passos: criar a aula (fica "Agendada") e, depois, abrir uma tela separada ("Registrar Sessão") para marcar a presença de cada aluno — ação que dispara os efeitos financeiros existentes (cobrança Avulsa, decremento de saldo de Pacote) e muda o status para "Realizada". Esta feature funde os dois passos em um só: quando a Data de Início informada na criação da aula é hoje ou passada, o próprio formulário de criação já exibe a lista de presença e, ao salvar, a aula nasce direto como "Realizada" com os mesmos efeitos já existentes. Aulas futuras continuam exatamente como hoje. Também libera a edição do campo Descrição mesmo após a aula estar "Realizada" (hoje qualquer edição é bloqueada nesse status).

Abordagem técnica: extrair a lógica já existente em `AulaService.RegistrarSessaoAsync` (validação de presença por aluno, bloqueio por pacote esgotado, incremento de frequência, recálculo de lembretes, geração de cobrança/decremento de saldo) para um método privado reaproveitado também por `CadastrarAsync` — nenhuma duplicação de regra de negócio (Princípio II). Nenhuma mudança de schema de banco é necessária.

## Technical Context

**Language/Version**: C# / .NET 10 (backend, `SPI.Api`/`SPI.Application`/`SPI.Domain`/`SPI.Infrastructure`); TypeScript / Next.js (App Router) + React (frontend, `frontend/`)

**Primary Dependencies**: ASP.NET Core Web API, Entity Framework Core (Pomelo MySQL), FluentValidation (backend); Next.js, React, `fetch`-based API client já existente (frontend) — nenhuma dependência nova

**Storage**: MySQL (`spi_db`) — nenhuma mudança de schema necessária; a feature reaproveita as colunas já existentes (`aula.status`, `aula.descricao`, `aula_aluno.presente`, `aula_aluno.motivo_nao_registro`)

**Testing**: xUnit com fakes manuais (`tests/SPI.Application.Tests/`, sem biblioteca de mock) para o backend; validação manual via `quickstart.md` para o frontend (projeto não tem suíte de testes de frontend configurada, mesmo padrão de specs anteriores como specs/047/048/053)

**Target Platform**: Aplicação web (navegador desktop), backend hospedado como API REST — mesmo alvo de todo o restante do sistema

**Project Type**: Web application (frontend Next.js + backend ASP.NET Core API já existentes)

**Performance Goals**: Nenhuma meta nova além do já existente — a operação de criação retroativa reaproveita exatamente as mesmas consultas/gravações que "Registrar Sessão" já faz hoje, só reordenadas para acontecer dentro de `CadastrarAsync` em vez de numa chamada separada

**Constraints**: MUST reaproveitar a lógica já existente de geração de cobrança/decremento de saldo (FR-005) — nenhuma regra financeira nova, nenhuma duplicação entre `CadastrarAsync` e `RegistrarSessaoAsync`

**Scale/Scope**: 2 endpoints de backend afetados (`POST /api/aulas`, `PUT /api/aulas/{id}`), 1 tela de frontend afetada (formulário de criação/edição de aula, `AulaForm.tsx`) — escopo pequeno e contido, sem novas entidades

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Princípio I (Exclusão Lógica, Nunca Física)**: N/A — esta feature não introduz nem altera nenhuma funcionalidade de exclusão.
- **Princípio II (Validação de Negócio Única e Centralizada no Backend)**: ✅ Aplicável e respeitado por design — a validação de presença (todo aluno precisa de uma entrada), o bloqueio por pacote esgotado, e os efeitos financeiros são reaproveitados de um único método de serviço (`AplicarPresencasAsync`, extraído de `RegistrarSessaoAsync`), usado tanto pela criação retroativa quanto por "Registrar Sessão". O frontend apenas coleta os checkboxes e exibe os erros/avisos que o backend retornar — nenhuma regra de negócio é recalculada no cliente.
- **Princípio III (Nenhuma Capacidade Configurável Sem Implementação Real)**: ✅ Respeitado — a lista de presença só aparece no frontend quando a data já permite o fluxo retroativo, e o backend é quem decide autoritativamente se uma aula pode nascer "Realizada" (comparando a mesma data recebida); não há opção exposta na UI sem suporte correspondente no backend.
- **Princípio IV (Autenticação e Segredos Seguros por Padrão)**: N/A — nenhuma mudança em autenticação, sessão ou segredos.
- **Princípio V (Documentação Retroativa como Registro Histórico Vinculante)**: N/A — esta é uma spec de comportamento futuro, não um registro retroativo.

**Gate status**: PASS — nenhuma violação a justificar em Complexity Tracking.

**Re-check pós-Phase 1 (design)**: Confirmado — o design em `research.md` (R1-R6) reforça o Princípio II ao extrair `AplicarPresencasAsync` como único ponto de validação/efeito reaproveitado por `CadastrarAsync` e `RegistrarSessaoAsync`, e não introduz nenhuma opção de UI sem suporte real no backend (Princípio III). Nenhum novo risco de violação identificado após o design de `data-model.md`/`contracts/`.

## Project Structure

### Documentation (this feature)

```text
specs/055-presenca-criacao-aula/
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
│   └── Entities/Aula.cs, AulaAluno.cs                       # sem mudanca de estrutura
├── SPI.Application/
│   └── Aulas/
│       ├── Dtos/AulaRequest.cs                               # novo campo opcional Presencas
│       └── Services/AulaService.cs                           # CadastrarAsync, AtualizarAsync,
│                                                               # RegistrarSessaoAsync (refatorado),
│                                                               # novo metodo privado AplicarPresencasAsync
├── SPI.Api/
│   └── Controllers/AulasController.cs                        # sem mudanca de assinatura (so repassa
│                                                               # o campo novo do DTO existente)
└── SPI.Infrastructure/                                        # sem mudanca (repositorios ja existentes
                                                                 # cobrem o que e necessario)

frontend/
├── components/aulas/
│   ├── AulaForm.tsx                                           # lista de presenca condicional na criacao
│   └── PresencaChecklist.tsx                                  # novo componente (checkbox por aluno)
├── app/(app)/aulas/
│   ├── nova/page.tsx                                          # sem mudanca (so usa AulaForm)
│   └── [id]/editar/page.tsx                                   # sem mudanca (so usa AulaForm)
└── lib/api/aulas.ts                                           # tipo AulaRequest espelha o novo campo

database/                                                       # sem migracao nova (nenhuma coluna nova)

tests/SPI.Application.Tests/Aulas/                              # testes novos para CadastrarAsync
                                                                 # retroativo e para AtualizarAsync com
                                                                 # Descricao em aula Realizada
```

**Structure Decision**: Reaproveita a estrutura em camadas já estabelecida (`SPI.Domain` → `SPI.Application` → `SPI.Infrastructure` → `SPI.Api` no backend; `frontend/components` + `frontend/app` no frontend). Nenhuma camada nova, nenhum projeto novo — a mudança é inteiramente dentro do módulo de Aulas já existente.

## Complexity Tracking

> Nenhuma violação do Constitution Check a justificar — seção vazia por design.
