# Implementation Plan: Esgotamento de Pacote de Aulas (Alerta e Bloqueio)

**Branch**: `041-pacote-esgotado` | **Date**: 2026-09-23 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/041-pacote-esgotado/spec.md`

## Summary

Duas mudanças pequenas e independentes sobre o `VinculoCobranca` já existente (specs/037/038), com **uma única migração de banco** (coluna nova `aula_aluno.motivo_nao_registro`, script `16_*.sql`):

1. **Bloqueio (US2)**: em `AulaService.RegistrarSessaoAsync`, antes de gravar a presença de cada aluno, consultar o vínculo ativo do contexto da aula; se for `Pacote` com `SaldoAulas == 0` e o aluno veio marcado como presente, a presença **não** é registrada (`Presente = false`, sem frequência, sem débito), o **motivo é persistido** (`MotivoNaoRegistro = "PacoteEsgotado"`, distinguindo o bloqueio de uma falta comum no histórico da aula) e um aviso imediato é devolvido na resposta (novo campo `Avisos` em `AulaResponse`; `AlunoPresencaResponse` ganha `MotivoNaoRegistro`). A aula segue para `Realizada` normalmente para os demais alunos (clarificação de 2026-09-23, opção A).
2. **Painel (US1)**: novo campo `PacotesEmAtencao` em `DashboardResponse` (mesmo endpoint `GET /api/dashboard` que a Home já consome), alimentado pelo método de repositório já existente `ListarAtivosPorModalidadeAsync(Pacote)` (que passa a incluir o `Aluno`) e por um método estático puro que aplica a regra de elegibilidade (saldo informado ≤ 2, aluno ativo). O backend devolve o estado (`Atencao`/`Esgotado`) já calculado e a lista já ordenada (esgotados primeiro); o frontend só renderiza (Princípio II).
3. **Renovação (US3)**: nenhum código novo — a edição do vínculo (specs/037) já altera `SaldoAulas`; como o bloqueio e o painel leem sempre o saldo atual, a liberação é automática. Validada pelo quickstart.

## Technical Context

**Language/Version**: C# / .NET 10 (backend); TypeScript / Next.js App Router (frontend) — stack já vigente.

**Primary Dependencies**: EF Core + Pomelo MySQL (já em uso). Nenhuma dependência nova.

**Storage**: MySQL — **uma coluna nova**: `aula_aluno.motivo_nao_registro VARCHAR(30) NULL` (+ `CHECK` restringindo a `'PacoteEsgotado'`), via `database/16_aula_aluno_motivo_nao_registro.sql` (research.md R2/R6). `VinculoCobranca.SaldoAulas`, `Modalidade`, `Ativo` já existem; o "estado" Atenção/Esgotado é derivado, nunca armazenado — só o motivo histórico do bloqueio é persistido.

**Testing**: xUnit com fakes manuais em `tests/SPI.Application.Tests/` (padrão da sessão, sem biblioteca de mock). Lógica nova extraída em métodos estáticos puros, testáveis sem fakes (mesmo espírito de `MensalidadeDispatcherService.DeveDispararNesteMomento`).

**Target Platform**: API ASP.NET Core + frontend Next.js — sem infraestrutura nova.

**Project Type**: Web application (backend + frontend).

**Performance Goals**: Sem meta nova. O painel carrega os vínculos `Pacote` ativos (consulta simples) e filtra em memória, sobre uma tabela pequena (single-tenant, uma professora); a checagem de bloqueio faz no máximo uma consulta a mais por aluno presente por registro de sessão (já é uma consulta por aluno em `GerarContasAReceberAsync`).

**Constraints**:
- FR-006/SC-003: o bloqueio de um aluno nunca afeta os demais da mesma aula.
- FR-009: o débito de saldo da 038 permanece intacto (presença com saldo > 0 continua consumindo 1).
- Princípio II: limite de alerta (2) e estado (`Atencao`/`Esgotado`) vivem só no backend; o frontend não repete a regra.
- Princípio III: limite de alerta é constante fixa, não configurável — nenhuma opção nova sem implementação.

**Scale/Scope**: 1 constante de domínio, 1 `Include(Aluno)` no método de repositório existente `ListarAtivosPorModalidadeAsync`, 1 método estático puro no `DashboardService` + 1 dependência injetada, 1 campo novo em `DashboardResponse` (+ DTO de item), 1 campo novo em `AulaResponse` (`Avisos`), 1 propriedade nova em `AulaAluno` (`MotivoNaoRegistro`, + mapeamento EF e campo em `AlunoPresencaResponse`), ajuste em `RegistrarSessaoAsync`, 1 componente de frontend novo + ajuste na Home e na tela da aula, **1 script de banco (`16_*.sql`)**.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Princípio | Aplica-se? | Avaliação |
|---|---|---|
| I. Exclusão Lógica, Nunca Física | Sim (indireto) | Nada é excluído, e a coluna nova só acrescenta histórico (não remove nem sobrescreve dado). Vínculo inativo (`Ativo == false`) é excluído do painel e do bloqueio — respeita a exclusão lógica já existente. |
| II. Validação de Negócio Única e Centralizada no Backend | **Sim — central** | A regra de bloqueio vive uma única vez em `RegistrarSessaoAsync` (único ponto de entrada de registro de presença). O limite (2) e o estado (`Atencao`/`Esgotado`) são calculados no backend e entregues prontos; o frontend só exibe. |
| III. Nenhuma Capacidade Configurável Sem Implementação Real (NON-NEGOTIABLE) | Sim | Sem nova opção configurável. `SaldoAulas` deixa de ser um número "informativo com débito parcial" e passa a ter efeito real completo (alerta + bloqueio) — reforça o princípio, sem violá-lo. Saldo nulo continua sem efeito por definição ("nunca informado"). |
| IV. Autenticação e Segredos Seguros por Padrão | Sim (indireto) | Painel é entregue pelo `GET /api/dashboard`, já restrito a `Professor` (`[Authorize(Roles = Professor)]`). Nenhum segredo novo. |
| V. Documentação Retroativa como Registro Histórico Vinculante | Sim | Esta feature **supera** um comportamento documentado: spec 038 (FR-010 / cenário "saldo zero") aceitava presença com saldo 0 sem bloqueio. A superação é registrada aqui e refletida na spec 038 (nota de superação) e nos 2 testes da 038 que assumiam o comportamento antigo (ver research.md R8). |

**Resultado**: Sem violação. Sem exceção nova a rastrear (diferente de EX-001).

## Project Structure

### Documentation (this feature)

```text
specs/041-pacote-esgotado/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output
│   ├── registrar-sessao.md
│   └── dashboard-pacotes-atencao.md
├── checklists/requirements.md
└── tasks.md             # Phase 2 output (/speckit-tasks - NOT created by /speckit-plan)
```

### Source Code (repository root)

```text
src/
├── SPI.Domain/
│   ├── Entities/VinculoCobranca.cs                  # + const LimiteAlertaPacote = 2
│   └── Entities/AulaAluno.cs                        # + string? MotivoNaoRegistro; const MotivoPacoteEsgotado
├── SPI.Application/
│   ├── Aulas/
│   │   ├── Dtos/AulaResponse.cs                     # + List<string> Avisos
│   │   ├── Dtos/AlunoPresencaResponse.cs            # + string? MotivoNaoRegistro
│   │   └── Services/AulaService.cs                  # RegistrarSessaoAsync: checagem de pacote esgotado
│   └── Dashboard/
│       ├── Dtos/DashboardResponse.cs                # + PacotesEmAtencao
│       ├── Dtos/PacoteEmAtencaoResponse.cs          # NOVO: item do painel
│       └── Services/DashboardService.cs             # + IVinculoCobrancaRepository; método estático MontarPacotesEmAtencao
└── SPI.Infrastructure/
    ├── Persistence/Configurations/AulaAlunoConfiguration.cs  # mapeia motivo_nao_registro
    └── Repositories/VinculoCobrancaRepository.cs    # Include(Aluno) em ListarAtivosPorModalidadeAsync

database/
└── 16_aula_aluno_motivo_nao_registro.sql            # NOVO: coluna + CHECK

frontend/
├── lib/api/dashboard.ts                             # + PacoteEmAtencao / Dashboard.pacotesEmAtencao
├── lib/api/aulas.ts                                 # + Aula.avisos; AlunoPresenca.motivoNaoRegistro
├── components/dashboard/PacotesEmAtencaoPanel.tsx   # NOVO
├── app/(app)/dashboard/page.tsx                     # renderiza o painel
└── app/(app)/aulas/[id]/page.tsx                    # exibe Avisos + selo "Pacote esgotado" por aluno

tests/SPI.Application.Tests/
├── Aulas/AulaServiceRegistrarSessaoPacoteEsgotadoTests.cs   # NOVO
├── Aulas/AulaServiceGerarContasAReceberPacoteTests.cs       # 2 testes ajustados (superação da 038)
└── Dashboard/DashboardPacotesEmAtencaoTests.cs              # NOVO (método estático puro)
```

**Structure Decision**: Estrutura já existente (camadas Domain/Application/Infrastructure/Api + `frontend/`). Nenhum controller novo: o painel reutiliza `DashboardController` (só o DTO cresce) e o aviso/motivo viajam no `AulaResponse` já devolvido por `RegistrarSessaoAsync`. Um script novo em `database/` (`16_aula_aluno_motivo_nao_registro.sql`), seguindo o padrão numerado.

## Post-Design Constitution Check

*Re-avaliação após Phase 1 (research.md, data-model.md, contracts/, quickstart.md).*

- Princípio II: research.md R1/R4 e os contratos confirmam que regra, limite e estado têm dono único no backend; o contrato do painel entrega `estado` e ordem prontos, e o frontend não compara saldos.
- Princípio III: nenhum campo/opção nova exposto sem implementação; `Avisos` e `PacotesEmAtencao` são preenchidos por código real e cobertos por testes.
- Princípio I: a coluna nova só acrescenta histórico; nenhuma linha existente é alterada (todas `NULL`).
- Princípio V: superação do comportamento da 038 registrada (R8) — a spec 038 recebe uma nota de superação na implementação.
- Nenhuma violação nova.

**Resultado**: Sem violação bloqueante.

## Complexity Tracking

Não aplicável — nenhuma violação identificada em nenhum dos dois Constitution Checks.
