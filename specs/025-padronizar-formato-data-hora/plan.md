# Implementation Plan: Padronizar Formato Brasileiro de Data e Hora

**Branch**: `025-padronizar-formato-data-hora` | **Date**: 2026-09-17 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/025-padronizar-formato-data-hora/spec.md`

**Note**: This template is filled in by the `/speckit-plan` command; its definition describes the execution workflow.

## Summary

Garantir que todo o frontend exiba datas em dd/mm/aaaa e horários em 24h, com uma única fonte de
formatação (`frontend/lib/format.ts`, já existente e já amplamente adotada), e substituir os 21
campos de formulário que hoje usam `<input type="date">`/`<input type="time">` nativo por dois
novos componentes próprios (`DateInput`, `TimeInput` em `frontend/components/shared/`) que
sempre exibem e aceitam dd/mm/aaaa e 24h, independentemente do idioma/SO do navegador da
usuária (decisão da clarificação da spec). O contrato de valor desses componentes permanece
idêntico ao do input nativo (`yyyy-MM-dd` / `HH:mm`), então nenhuma chamada de API nem validação
de negócio no backend é afetada. Também é corrigido o único ponto de exibição hoje divergente do
padrão central (`dashboard/page.tsx:27`).

## Technical Context

**Language/Version**: TypeScript / React 19 (Next.js 16, App Router) — mesmo stack do restante
do frontend, nenhuma linguagem nova

**Primary Dependencies**: Nenhuma dependência nova (ver research.md Decisão 1) — os novos
componentes `DateInput`/`TimeInput` são construídos com `<input type="text">` mascarado, sem
biblioteca de date-picker de terceiros

**Storage**: N/A — mudança de apresentação/entrada no frontend; nenhum schema de banco tocado

**Testing**: Frontend sem framework de teste automatizado configurado (mesma limitação já
registrada em specs anteriores, ex. specs/023, specs/024) — verificação manual via
quickstart.md, incluindo o teste específico de SC-005 (dois navegadores com idiomas diferentes)

**Target Platform**: Navegador web (todas as telas existentes do sistema)

**Project Type**: Web application (frontend Next.js já existente) — esta feature toca somente o
frontend

**Performance Goals**: N/A — não há meta de performance de sistema; mudança é de apresentação

**Constraints**: O valor emitido/recebido por `DateInput`/`TimeInput` MUST permanecer no mesmo
formato já usado hoje (`yyyy-MM-dd` / `HH:mm`) para não alterar payloads de API nem duplicar
validação de negócio (Princípio II da constituição); parsing interno MUST evitar
`new Date(stringDataPura)` para não reintroduzir o bug de deslocamento de fuso já corrigido em
`lib/format.ts` (ver research.md Decisão 4)

**Scale/Scope**: 2 componentes novos (`DateInput`, `TimeInput`) + 1 arquivo de estilo; 20
arquivos alterados para trocar `<input type="date"/"time">` pelos novos componentes (21
ocorrências); 1 arquivo corrigido para usar `fmtData` em vez de `toLocaleDateString` direto
(`dashboard/page.tsx`); `lib/format.ts` não perde nenhuma função existente, pode ganhar
utilitários de parsing/máscara reaproveitados pelos novos componentes

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Avaliação contra os 5 princípios ratificados em `.specify/memory/constitution.md` (v1.0.0):

| Princípio | Aplicável? | Avaliação |
|---|---|---|
| I. Exclusão Lógica, Nunca Física | Não (observado) | Nenhum registro de domínio é excluído ou desativado por esta mudança — é padronização de apresentação/entrada de data e hora |
| II. Validação de Negócio Única e Centralizada no Backend | Sim (observado) | `DateInput`/`TimeInput` fazem apenas máscara/parsing de formato (dd/mm/aaaa ↔ yyyy-MM-dd, HH:mm ↔ HH:mm) — nenhuma regra de negócio (obrigatoriedade, intervalo permitido, conflito de horário) é adicionada ou duplicada nos componentes; essas validações continuam exclusivamente no backend, como hoje (ver research.md Decisão 2) |
| III. Nenhuma Capacidade Configurável Sem Implementação Real | Não (observado) | Nenhuma opção configurável nova é exposta — os componentes substituem a entrada de um valor já existente, sem adicionar campo/opção nova ao domínio |
| IV. Autenticação e Segredos Seguros por Padrão | Não (observado) | Nenhuma mudança de autenticação, sessão ou segredo envolvida |
| V. Documentação Retroativa como Registro Histórico Vinculante | Não (observado) | Nenhuma spec retroativa (001-019) descreve o formato de exibição de data/hora como uma decisão vinculante a preservar; não há conflito documental a resolver |

**Resultado**: PASS — nenhum princípio é violado. O único princípio diretamente aplicável (II)
é reforçado, não enfraquecido: os novos componentes são deliberadamente "burros" quanto a
regra de negócio (só formato), mantendo toda validação real no backend.

**Re-check pós-design (Phase 1)**: PASS, sem mudanças. O research.md (Decisão 2) confirma que o
contrato de valor dos novos componentes é idêntico ao do input nativo, então nenhum formulário
precisa duplicar ou mover validação de negócio ao adotá-los — o design reforça o gate original.

## Project Structure

### Documentation (this feature)

```text
specs/025-padronizar-formato-data-hora/
├── plan.md              # This file (/speckit-plan command output)
├── research.md          # Phase 0 output (/speckit-plan command)
├── quickstart.md        # Phase 1 output (/speckit-plan command)
└── tasks.md             # Phase 2 output (/speckit-tasks command - NOT created by /speckit-plan)
```

Sem `data-model.md` nem `contracts/`: esta feature não introduz entidades de dados novas (ver
spec.md "Key Entities": não aplicável) nem contratos de API novos/alterados — o valor trocado
entre frontend e backend não muda de forma (ver Decisão 2 do research.md).

### Source Code (repository root)

```text
# Aplicação web já existente (frontend Next.js + backend ASP.NET Core + MySQL).
# Esta feature toca SOMENTE o frontend.

frontend/
├── components/
│   ├── shared/
│   │   ├── DateInput.tsx       # NOVO — substitui <input type="date"> nativo
│   │   └── TimeInput.tsx       # NOVO — substitui <input type="time"> nativo
│   └── aulas/
│       └── AulaForm.tsx        # troca 1 date + 2 time inputs pelos novos componentes
├── app/(app)/
│   ├── dashboard/page.tsx                              # corrige toLocaleDateString → fmtData
│   ├── financeiro/page.tsx                             # troca 2 date inputs (filtro)
│   ├── financeiro/contas-a-pagar/page.tsx              # troca 2 date inputs
│   ├── financeiro/contas-a-pagar/novo/page.tsx         # troca 1 date input
│   ├── financeiro/contas-a-pagar/[id]/editar/page.tsx  # troca 1 date input
│   ├── financeiro/contas-a-receber/page.tsx            # troca 2 date inputs
│   ├── financeiro/contas-a-receber/novo/page.tsx       # troca 1 date input
│   ├── financeiro/contas-a-receber/[id]/editar/page.tsx # troca 1 date input
│   ├── lembretes/page.tsx                              # troca 2 date inputs (filtro)
│   ├── relatorios/financeiro/page.tsx                  # troca 2 date inputs (filtro)
│   ├── relatorios/alunos/page.tsx                      # troca 2 date inputs (filtro)
│   ├── relatorios/pagamentos/page.tsx                  # troca 2 date inputs (filtro)
│   └── relatorios/turmas/page.tsx                      # troca 2 date inputs (filtro)
├── lib/
│   └── format.ts        # sem função removida; pode ganhar helpers de parsing/máscara
│                          #   compartilhados pelos novos componentes
└── styles/
    └── dashboard.css     # ou arquivo de estilo global equivalente — estilo dos novos inputs
```

**Structure Decision**: Aplicação web já existente (frontend Next.js + backend ASP.NET Core +
MySQL). A mudança fica inteiramente contida no frontend: dois componentes novos em
`components/shared/` (convenção já usada para componentes transversais), reaproveitados pelos 20
arquivos que hoje têm input nativo de data/hora, mais uma correção pontual de consistência em
`dashboard/page.tsx`. Nenhum componente de domínio novo, nenhuma rota nova, nenhum arquivo de
backend ou banco de dados tocado.

## Complexity Tracking

> Sem violações de constituição a justificar (Constitution Check acima = PASS). A única decisão
> de design com mais de uma opção razoável (biblioteca de terceiros vs. componente próprio para
> os inputs de data/hora) está documentada e justificada no research.md Decisão 1 — não
> representa uma violação de princípio, apenas uma escolha de implementação.
