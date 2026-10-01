# Implementation Plan: Marcador de STATUS nos Cards do Trello

**Branch**: `042-trello-status-marker` | **Date**: 2026-09-29 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/042-trello-status-marker/spec.md`

## Summary

Estende `.specify/hooks/trello/sync-card.ps1` (spec 040) para que toda descrição de card escrita pela integração comece com uma linha `STATUS: <detalhe>`, sem alterar o restante da descrição nem o marcador `Feature: <id>` já existente na última linha. Três frentes:

1. **Reescrever a linha nas 5 fases já existentes** (`backlog`, `design`, `a-fazer`, `em-andamento`, `revisao-codigo`): toda vez que o script cria ou move o card, ele extrai o corpo atual da descrição (tudo exceto uma eventual linha `STATUS:` inicial), monta um novo detalhe (derivado de `tasks.md`/estado dos arquivos) e reconstrói a descrição com o `STATUS:` novo na frente. Isso também resolve, de graça, a US3 (card antigo sem `STATUS:` ganha um na próxima vez que qualquer hook o tocar), porque a reconstrução da descrição roda sempre, independente de já haver `STATUS:` ou não.
2. **Duas fases novas**, `analyze` e `clarify`, registradas em `.specify/extensions.yml` como `after_analyze`/`after_clarify` (o mecanismo de hook do Spec Kit já suporta esses dois eventos — confirmado por leitura das próprias skills `speckit-analyze`/`speckit-clarify`, que já têm o bloco genérico de disparo de hook; nenhum arquivo de skill do Spec Kit precisa ser editado). Essas duas fases só atualizam o `STATUS:`, nunca movem o card de lista.
3. **Atualização retroativa em lote** (`-Fase retroativo`, sem hook associado — rodada manualmente uma única vez): para todo card do quadro sem o marcador `Feature: <id>`, insere `STATUS: card criado manualmente, sem spec formal ainda` quando ainda não tiver `STATUS:` na primeira linha.

## Technical Context

**Language/Version**: PowerShell 5.1 — mesma stack de `.specify/hooks/trello/sync-card.ps1` (spec 040). Nenhuma dependência nova.

**Primary Dependencies**: Nenhuma (mesmo `Invoke-RestMethod` nativo).

**Storage**: Nenhuma no SPI — mesmo modelo da spec 040 (estado vive só no Trello, redescoberto a cada chamada).

**Testing**: Sem suíte automatizada para o script (mesma decisão de research.md R8 da spec 040) — validação via `-DryRun` e `quickstart.md` contra um quadro Trello de teste real.

**Target Platform**: Ambiente de desenvolvimento local — mesmo escopo da spec 040 (ferramenta de processo, fora do produto SPI).

**Project Type**: Ferramenta de processo de desenvolvimento (FR-013) — estende os mesmos arquivos de `.specify/hooks/trello/` e `.claude/skills/trello-sync-card/` criados na spec 040; nenhum arquivo novo de skill do Spec Kit.

**Performance Goals**: Sem meta nova — cada hook continua fazendo no máximo 2-3 chamadas REST simples.

**Constraints**:
- FR-005/FR-006: reescrever o `STATUS:` MUST NOT tocar em mais nada da descrição nem duplicar a linha — a lógica de extrair-e-reconstruir precisa ser exata (idempotente byte a byte quando o detalhe não muda).
- FR-010: mesma garantia de nunca travar o Spec Kit (try/catch global + `exit 0`) já herdada do núcleo da spec 040 — as duas fases novas entram no mesmo wrapper, sem caminho de saída próprio.
- FR-011: nenhuma leitura do Trello influencia decisão de fluxo do Spec Kit — a leitura serve só para descobrir o corpo atual da descrição antes de reescrevê-la.
- **Restrição técnica nova, descoberta ao planejar**: o bug já documentado em research.md R-anterior (specs/040) — `Invoke-RestMethod -Method Put -Body <hashtable>` não é aplicado pela API do Trello — também afeta a atualização de `desc`. A correção usada para `idList` (colocar o parâmetro na própria query string do PUT) exige, para `desc`, URL-encoding explícito (`[System.Uri]::EscapeDataString`), porque a descrição pode ter espaços, acentos, `&`, quebras de linha — diferente do `idList`, que é só um hex simples.

**Scale/Scope**: Nenhum arquivo novo de infraestrutura — só edições em `sync-card.ps1` (2 fases novas + 1 fase de manutenção + reescrita da lógica de descrição nas 5 fases existentes), `.claude/skills/trello-sync-card/SKILL.md` (documentar o novo formato de argumento) e `.specify/extensions.yml` (+2 entradas). Nenhum script de banco, nenhuma mudança em `src/SPI.*`/`frontend/`.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Princípio | Aplica-se? | Avaliação |
|---|---|---|
| I. Exclusão Lógica, Nunca Física | Não | Nenhum registro de domínio do SPI é tocado. |
| II. Validação de Negócio Única e Centralizada no Backend | Sim (adaptado) | A regra de "como montar o detalhe do STATUS" e "como extrair/reconstruir a descrição" tem um único dono: `sync-card.ps1`. Nenhuma lógica duplicada na skill `trello-sync-card` (que continua só orquestrando) nem em nenhum outro lugar. |
| III. Nenhuma Capacidade Configurável Sem Implementação Real (NON-NEGOTIABLE) | Sim | Mesma postura da spec 040: nenhuma entrada nova de `extensions.yml` usa `condition`; as duas fases novas (`analyze`/`clarify`) são `optional: false` e de fato implementadas no script antes de serem anunciadas. |
| IV. Autenticação e Segredos Seguros por Padrão | Sim | Reaproveita a mesma autenticação/`.env` da spec 040 — nenhuma credencial nova. |
| V. Documentação Retroativa como Registro Histórico Vinculante | Não | Feature nova; não há comportamento documentado divergente a superar. |

**Resultado**: Sem violação.

## Project Structure

### Documentation (this feature)

```text
specs/042-trello-status-marker/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output (delta sobre os contratos da spec 040)
│   ├── status-line-convention.md
│   ├── sync-card-cli-delta.md
│   └── extensions-yml-delta.md
├── checklists/requirements.md
└── tasks.md             # Phase 2 output (/speckit-tasks - NOT created by /speckit-plan)
```

### Source Code (repository root) — todos arquivos JÁ existentes (spec 040), só editados

```text
.specify/
├── extensions.yml                          # + after_analyze, + after_clarify
└── hooks/trello/
    └── sync-card.ps1                       # + fases analyze/clarify/retroativo
                                             # + parametro -Detalhe
                                             # + helpers de STATUS (extrair/montar)
                                             # + Update-TrelloCardDescription (PUT com
                                             #   desc via query string URL-encoded)
.claude/
└── skills/trello-sync-card/
    └── SKILL.md                            # documenta o formato "<fase>[: <detalhe>]"
```

**Structure Decision**: Nenhum diretório novo — esta feature é uma extensão pontual dos mesmos artefatos que a spec 040 criou, mantendo a regra de nunca viver em `src/SPI.*`/`frontend/` (FR-013).

## Post-Design Constitution Check

*Re-avaliação após Phase 1 (research.md, data-model.md, contracts/, quickstart.md).*

- Princípio II: research.md R1/R2 confirmam que a montagem do STATUS e a extração/reconstrução da descrição vivem só em `sync-card.ps1`.
- Princípio III: `contracts/extensions-yml-delta.md` confirma que as 2 entradas novas não usam `condition` e apontam para lógica de verdade já implementada.
- Nenhuma violação nova introduzida pelo design.

**Resultado**: Sem violação bloqueante.

## Complexity Tracking

Não aplicável — nenhuma violação identificada em nenhum dos dois Constitution Checks.
