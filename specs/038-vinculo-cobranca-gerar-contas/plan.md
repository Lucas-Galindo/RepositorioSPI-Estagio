# Implementation Plan: Cobrança Automática por Modalidade do Vínculo

**Branch**: `038-vinculo-cobranca-gerar-contas` | **Date**: 2026-09-22 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/038-vinculo-cobranca-gerar-contas/spec.md`

## Summary

Ramificar `AulaService.GerarContasAReceberAsync` por modalidade do `VinculoCobranca` correspondente ao contexto da presença (mesma Turma da aula, ou nulo para aula individual): sem vínculo ativo correspondente, comportamento idêntico ao atual (avulsa com `Aluno.ValorAula`); com vínculo Avulsa, mesma geração mas usando `VinculoCobranca.Valor`; com vínculo Mensalidade, nenhuma cobrança; com vínculo Pacote, nenhuma cobrança e decremento de `SaldoAulas` em 1 (nunca abaixo de zero, e sem decrementar quando nunca foi informado). Isso fecha a exceção EX-001 (`specs/037-vinculo-cobranca/plan.md`) para as modalidades Avulsa e Pacote — Mensalidade permanece com efeito zero até um job futuro fora de escopo.

## Technical Context

**Language/Version**: C# / .NET 10 (`net10.0`) — mesma versão já usada no projeto.

**Primary Dependencies**: ASP.NET Core, EF Core + Pomelo MySQL — nenhuma dependência nova. A mudança é inteiramente dentro de `SPI.Domain`/`SPI.Application`/`SPI.Infrastructure` já existentes (nenhum endpoint novo, nenhum controller alterado).

**Storage**: MySQL. Sem migração de schema — `vinculo_cobranca.saldo_aulas` já existe (specs/037); esta feature só passa a escrevê-lo via `UPDATE` (o mesmo caminho de `SalvarAlteracoesAsync` já usado pela edição manual). Sem nova tabela.

**Testing**: xUnit (`tests/SPI.Application.Tests`), fakes manuais (sem biblioteca de mock instalada, mesmo padrão de `tests/SPI.Application.Tests/VinculosCobranca/` e `Relatorios/`). Não existe hoje nenhuma suíte para `AulaService` — esta feature cria a primeira (`tests/SPI.Application.Tests/Aulas/`), o que exige fakes novos de `IAulaRepository`, `IPagamentoRepository`, `ICategoriaReceitaRepository` além de reaproveitar `IVinculoCobrancaRepository`.

**Target Platform**: ASP.NET Core Web API (`SPI.Application`), sem impacto em `SPI.Api` nem em `frontend/` (mudança 100% de regra de negócio no backend; ver Assumptions do spec.md sobre a UI permanecer inalterada).

**Project Type**: Web application (estrutura já existente do repositório).

**Performance Goals**: Sem meta nova — no máximo uma consulta adicional (`ObterAtivoPorAlunoEContextoAsync`) por aluno presente, dentro do mesmo laço que já existe.

**Constraints**: FR-011 (`VinculoCobranca` CRUD e `Aluno.ValorAula` inalterados) e FR-009/FR-010 (nenhum job de mensalidade, nenhum aviso/bloqueio de pacote esgotado) — nada além da ramificação em `GerarContasAReceberAsync` pode ser tocado.

**Scale/Scope**: 1 método novo em `IVinculoCobrancaRepository`/`VinculoCobrancaRepository`, reescrita de `AulaService.GerarContasAReceberAsync` (privado, sem mudar assinatura pública), 1 pasta de testes nova (`tests/SPI.Application.Tests/Aulas/`). Nenhuma mudança de contrato de API, nenhuma entidade nova, nenhuma migração de banco.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Princípio | Aplica-se? | Avaliação |
|---|---|---|
| I. Exclusão Lógica, Nunca Física | Não diretamente | Nenhuma exclusão de registro nesta feature. O decremento de `SaldoAulas` é atualização de um campo numérico de um registro já ativo, não uma exclusão. **Conforme.** |
| II. Validação de Negócio Única e Centralizada no Backend | Sim | A ramificação por modalidade é implementada uma única vez, dentro de `AulaService.GerarContasAReceberAsync` — único ponto de entrada que gera cobrança automática (chamado só por `RegistrarSessaoAsync`). Nenhuma duplicação no frontend (que nem é tocado). **Conforme.** |
| III. Nenhuma Capacidade Configurável Sem Implementação Real (NON-NEGOTIABLE) | Sim — **fecha parcialmente a exceção EX-001** | Esta é a "próxima fatia" prevista em `specs/037-vinculo-cobranca/plan.md` (Complexity Tracking, EX-001). Após esta feature, Modalidade Avulsa e Pacote passam a ter efeito real (valor usado / saldo decrementado). Modalidade Mensalidade **continua** sem efeito automático (job futuro, fora de escopo) — portanto EX-001 **não fecha por completo**; ver Post-Design Constitution Check e nota em EX-001 abaixo. |
| IV. Autenticação e Segredos Seguros por Padrão | Não | Nenhum endpoint novo, nenhuma mudança de autenticação/segredo. |
| V. Documentação Retroativa como Registro Histórico Vinculante | Não | Nenhuma spec retroativa (`001`-`019`) documenta `GerarContasAReceberAsync` como definitivamente "sempre avulsa" — o comportamento atual está descrito no código e no `spec.md` desta própria feature, que é quem passa a valer. |

**Resultado**: Sem violação nova. EX-001 (já registrada e aceita pelo usuário em 2026-09-20) é parcialmente resolvida por esta feature — ver nota dedicada abaixo, não uma linha de Complexity Tracking nova.

### Nota sobre EX-001 (não é uma nova exceção, é a atualização da existente)

- `specs/037-vinculo-cobranca/plan.md` registra EX-001 como **ABERTA**, com o compromisso: "a próxima fatia... MUST ser priorizada... e implementar o efeito real. A exceção só é considerada encerrada quando essa fatia estiver entregue".
- Esta feature entrega o efeito real para **Avulsa** e **Pacote**, mas não para **Mensalidade** (explicitamente fora de escopo, FR-009 do spec.md). Logo, ao final desta feature, EX-001 deve ser **atualizada, não fechada**: o aviso da UI ("Este vínculo é apenas um cadastro...") continua necessário enquanto Mensalidade não tiver efeito real — a spec.md já documenta isso em Assumptions.
- Ação de tarefa (`/speckit-tasks`): atualizar o registro de EX-001 em `specs/037-vinculo-cobranca/plan.md` (texto e status) para refletir a resolução parcial e apontar o job de Mensalidade como o que resta para fechá-la — não basta implementar aqui e esquecer o registro.

## Project Structure

### Documentation (this feature)

```text
specs/038-vinculo-cobranca-gerar-contas/
├── plan.md              # This file (/speckit-plan command output)
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output (vazio/nota — sem contrato de API novo)
├── checklists/requirements.md
└── tasks.md             # Phase 2 output (/speckit-tasks command - NOT created by /speckit-plan)
```

### Source Code (repository root)

```text
src/
├── SPI.Domain/
│   └── Repositories/
│       └── IVinculoCobrancaRepository.cs        # + ObterAtivoPorAlunoEContextoAsync(alunoId, turmaId?, ct)
├── SPI.Infrastructure/
│   └── Repositories/
│       └── VinculoCobrancaRepository.cs         # implementa o novo metodo (mesmo padrao de ExisteAtivoAsync)
└── SPI.Application/
    └── Aulas/
        └── Services/
            └── AulaService.cs                   # + IVinculoCobrancaRepository no construtor;
                                                    # GerarContasAReceberAsync ramifica por modalidade;
                                                    # RegistrarSessaoAsync/demais metodos publicos inalterados

specs/037-vinculo-cobranca/
└── plan.md                                       # atualiza o registro de EX-001 (nao fecha, atualiza)

tests/
└── SPI.Application.Tests/
    └── Aulas/
        ├── AulaServiceFakes.cs                                # NOVO -- fakes manuais de IAulaRepository,
        │                                                        # IPagamentoRepository, ICategoriaReceitaRepository,
        │                                                        # IVinculoCobrancaRepository, ITurmaRepository,
        │                                                        # IMateriaRepository, IAlunoRepository, ILembreteService
        │                                                        # (padrao de VinculosCobranca/VinculoCobrancaFakes.cs)
        ├── AulaServiceGerarContasAReceberSemVinculoTests.cs    # NOVO -- US1 (FR-001/002/008)
        ├── AulaServiceGerarContasAReceberAvulsaTests.cs        # NOVO -- US2 (FR-003, FR-011)
        ├── AulaServiceGerarContasAReceberPacoteTests.cs        # NOVO -- US3 (FR-005/006/008/010)
        ├── AulaServiceGerarContasAReceberMensalidadeTests.cs   # NOVO -- US4 (FR-004/008/009)
        └── AulaServiceGerarContasAReceberMultiplosAlunosTests.cs   # NOVO -- FR-007 (Polish)
```

**Structure Decision**: Mudança isolada a `SPI.Domain`/`SPI.Infrastructure` (um método de repositório novo) e `SPI.Application/Aulas/Services/AulaService.cs` (um método privado reescrito, uma dependência nova injetada) — sem tocar em `SPI.Api` (nenhum endpoint muda de assinatura) nem em `frontend/` (nenhuma tela nova ou alterada). Primeira suíte de testes para `AulaService`, criada do zero nesta feature.

## Post-Design Constitution Check

*Re-avaliação após Phase 1 (research.md, data-model.md, quickstart.md).*

- Princípio II: confirmado pelo design — `ObterAtivoPorAlunoEContextoAsync` centraliza a busca do vínculo no repositório (não duplicada em nenhum outro serviço); a decisão de ramificação vive só em `GerarContasAReceberAsync`.
- Princípio III / EX-001: `data-model.md` (seção "Efeito por modalidade") confirma que Mensalidade continua com zero efeito automático nesta fatia — a nota acima permanece válida sem mudança após o design.
- Nenhuma violação nova introduzida pelo design (sem mudança de contrato de API, sem novo endpoint, sem novo dado de UI).

**Resultado**: Sem violação bloqueante. Nenhuma linha nova em Complexity Tracking.

## Complexity Tracking

Não aplicável — nenhuma violação nova identificada em nenhum dos dois Constitution Checks. A única exceção referenciada (EX-001) já existe em `specs/037-vinculo-cobranca/plan.md` e é atualizada, não recriada aqui.
