# Implementation Plan: Tabela de Lançamentos em Indicadores Financeiros

**Branch**: `022-tabela-lancamentos-indicadores` | **Date**: 2026-09-16 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/022-tabela-lancamentos-indicadores/spec.md`

**Note**: This template is filled in by the `/speckit-plan` command; its definition describes the execution workflow.

## Summary

Substituir o card "Gargalo de caixa" (que hoje só mostra o dia de maior entrada e o dia de
maior saída do período, agregados por `ObterEntradasPorDiaDoMesAsync`/`ObterSaidasPorDiaDoMesAsync`)
por uma tabela com um lançamento individual por linha, na aba Indicadores de
`frontend/app/(app)/relatorios/financeiro/page.tsx`. Requer expor a lista individual de
`Pagamento` (entrada) e `ContaPagar` (saída) que hoje só é somada/agrupada — não existe hoje
nenhum endpoint que devolva os lançamentos individuais do período de Indicadores, então a
mudança tem uma perna de backend (novo(s) método(s) de repositório + serviço + item na resposta
de `ObterIndicadoresFinanceirosAsync`) e uma perna de frontend (tabela nova + dois botões de
filtro de tipo "Entradas"/"Saídas" reaproveitando o padrão visual de botão `btn btn-sm`
introduzido em [021-fix-hitbox-cliques](../021-fix-hitbox-cliques/spec.md), sem o wrapper
`financeiro-tabs` — ver research.md decisão 3). Mesma semântica de
dados já usada por "Gargalo de caixa" e "Fluxo de caixa" na mesma tela: todo lançamento com
status diferente de "Cancelado" (pendente, atrasado ou pago), por data de vencimento, com a
restrição de turma/matéria/aluno aplicada somente ao lado da receita (decisão registrada em
Clarifications do spec).

## Technical Context

**Language/Version**: C# / .NET (backend, `SPI.Application` + `SPI.Infrastructure` + `SPI.Api`) e TypeScript / React 19 (Next.js 16, App Router, frontend) — mesmo stack do restante do projeto, nenhuma linguagem nova

**Primary Dependencies**: Backend: EF Core (consulta already-used `_dbContext.Pagamentos` / `_dbContext.ContasPagar`), FluentValidation (não se aplica aqui — endpoint é `[FromQuery]` GET, sem novo request body). Frontend: Next.js/React já existentes, nenhuma dependência nova

**Storage**: MySQL já existente — nenhuma migração de schema; a feature apenas lê `Pagamentos` e `ContasPagar`, tabelas já usadas por `RelatorioRepository`

**Testing**: Backend: xUnit (`tests/SPI.Application.Tests`, mesmo padrão dos testes existentes em `Lembretes/`) para o novo método de serviço/repositório que lista os lançamentos individuais. Frontend: sem framework de teste automatizado configurado (sem Jest/Vitest/Playwright em `frontend/package.json`, mesma limitação já registrada nas specs 020/021) — verificação manual no navegador (fluxo de filtro de período + Entradas/Saídas + estado vazio)

**Target Platform**: Navegador web (mesmo público da tela de Relatórios já existente), API ASP.NET Core já hospedada pelo backend do projeto

**Project Type**: Web application (frontend Next.js + backend ASP.NET Core já existentes) — esta feature toca os dois lados

**Performance Goals**: N/A explícito no spec além de SC-003 (troca de filtro Entradas/Saídas refletida em até 1s, sem reload de página) — resolvido por filtragem client-side da lista já carregada, sem nova chamada de API ao alternar o filtro de tipo

**Constraints**: A restrição de turma/matéria/aluno MUST valer somente para o lado das entradas (FR-008), nunca para saídas — mesma regra já aplicada aos demais indicadores desta tela (`AplicarFiltroReceita`, sem equivalente para `ContasPagar`). Os botões de filtro de tipo MUST reaproveitar o padrão visual de botão `btn btn-sm btn-primary`/`btn-ghost` já corrigido em 021 (FR-004) — sem exigir o wrapper `financeiro-tabs`, que é específico para navegação por `<a>` (ver research.md, decisão 3), não introduzindo um novo padrão de botão paralelo

**Scale/Scope**: 1 novo método de repositório para entradas individuais + 1 novo método para saídas individuais (`RelatorioRepository`/`IRelatorioRepository`), 1 novo campo na resposta de `ObterIndicadoresFinanceirosAsync` (`IndicadoresFinanceirosFiltradosResponse`, novo DTO de item de lançamento), 1 endpoint já existente reaproveitado (`GET /api/relatorios/indicadores-financeiros` — apenas a resposta cresce, sem novo endpoint), e 1 seção do frontend reescrita (`frontend/app/(app)/relatorios/financeiro/page.tsx`, painel "Gargalo de caixa" → tabela de lançamentos) + tipo espelhado em `frontend/lib/api/relatorios.ts`/`dashboard.ts`

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Avaliação contra os 5 princípios ratificados em `.specify/memory/constitution.md` (v1.0.0):

| Princípio | Aplicável? | Avaliação |
|---|---|---|
| I. Exclusão Lógica, Nunca Física | Não | Funcionalidade é somente leitura (relatório); nenhum registro é criado, editado ou excluído |
| II. Validação de Negócio Única e Centralizada no Backend | Sim | A regra "turma/matéria/aluno filtra só entradas, nunca saídas" já existe centralizada no backend (`AplicarFiltroReceita` em `RelatorioRepository`) e será reaproveitada pelo novo método de listagem individual — o frontend não reimplementa nem duplica essa regra, apenas exibe o resultado |
| III. Nenhuma Capacidade Configurável Sem Implementação Real | Sim | Os botões "Entradas"/"Saídas" filtram dados que o backend de fato devolve (lançamentos reais do período) — nenhuma opção é oferecida sem a capacidade correspondente implementada |
| IV. Autenticação e Segredos Seguros por Padrão | Não | Nenhuma mudança em autenticação, senha ou segredo; endpoint reaproveitado já exige o mesmo token de sessão que os demais endpoints de Relatórios |
| V. Documentação Retroativa como Registro Histórico Vinculante | Não | Nenhuma spec retroativa (001-019) descreve o card "Gargalo de caixa" — comportamento introduzido depois, sem conflito a resolver |

**Resultado**: PASS — nenhum princípio é violado por esta feature.

## Project Structure

### Documentation (this feature)

```text
specs/022-tabela-lancamentos-indicadores/
├── plan.md              # This file (/speckit-plan command output)
├── research.md          # Phase 0 output (/speckit-plan command)
├── data-model.md        # Phase 1 output (/speckit-plan command)
├── quickstart.md        # Phase 1 output (/speckit-plan command)
├── contracts/           # Phase 1 output (/speckit-plan command)
└── tasks.md             # Phase 2 output (/speckit-tasks command - NOT created by /speckit-plan)
```

### Source Code (repository root)

```text
# Option 2: Web application (frontend Next.js + backend ASP.NET Core já existentes)
# Esta feature toca os dois lados.

src/
├── SPI.Domain/
│   └── Repositories/
│       └── IRelatorioRepository.cs             # + 2 assinaturas: listar entradas e saídas
│                                                 #   individuais do período (com filtro receita)
├── SPI.Infrastructure/
│   └── Repositories/
│       └── RelatorioRepository.cs               # + implementação dos 2 métodos, reaproveitando
│                                                 #   AplicarFiltroReceita já existente
└── SPI.Application/
    └── Relatorios/
        ├── Dtos/
        │   ├── IndicadoresFinanceirosFiltradosResponse.cs   # + campo novo: lista de lançamentos
        │   └── LancamentoIndicadorItem.cs                    # novo DTO: descrição, data, tipo
        └── Services/
            └── RelatorioService.cs              # ObterIndicadoresFinanceirosAsync monta a nova
                                                    # lista (entradas + saídas) a partir dos 2
                                                    # métodos de repositório novos

frontend/
├── lib/api/
│   ├── relatorios.ts                            # tipo IndicadoresFinanceirosFiltrados espelha
│                                                 #   o novo campo da resposta
│   └── dashboard.ts                             # novo tipo espelhando LancamentoIndicadorItem
└── app/(app)/relatorios/financeiro/
    └── page.tsx                                 # painel "Gargalo de caixa" → tabela de
                                                    # lançamentos + botões "Entradas"/"Saídas"
                                                    # (classes btn btn-sm já existentes, sem o
                                                    # wrapper financeiro-tabs — research.md #3)

tests/
└── SPI.Application.Tests/
    └── Relatorios/                              # novo: testes do método de serviço que monta
                                                    # a lista de lançamentos (filtro de tipo,
                                                    # filtro de turma/matéria/aluno só em entradas,
                                                    # fallback de descrição vazia)
```

**Structure Decision**: Aplicação web já existente (frontend Next.js + backend ASP.NET Core). A
mudança é aditiva nas três camadas do backend (Domain → Infrastructure → Application), seguindo
exatamente o padrão já usado pelos métodos vizinhos de `RelatorioRepository`/`RelatorioService`
(mesma classe, mesmo endpoint HTTP reaproveitado — `GET /api/relatorios/indicadores-financeiros`
apenas passa a devolver mais um campo). No frontend, a mudança fica contida em
`relatorios/financeiro/page.tsx` mais os dois arquivos de tipos espelhados em `lib/api/`; o
padrão visual de botão (`btn btn-sm btn-primary`/`btn-ghost`) é reaproveitado sem alteração, não
duplicado — sem reaproveitar o wrapper `financeiro-tabs`, que é específico para os `<Link>` de
navegação do menu (ver research.md decisão 3).

## Complexity Tracking

> Sem violações de constituição a justificar (Constitution Check acima = PASS). A única
> decisão de design com mais de uma opção razoável — onde "cortar" a filtragem por tipo
> (Entradas/Saídas) entre backend e frontend — está registrada e resolvida em `research.md`
> (Phase 0): a API devolve a lista completa (entradas + saídas) já unificada por lançamento, e o
> frontend filtra por tipo no cliente, para cumprir SC-003 (troca de filtro sem nova chamada de
> rede) sem introduzir parâmetro de tipo na API.
