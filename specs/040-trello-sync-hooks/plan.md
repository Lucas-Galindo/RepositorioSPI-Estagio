# Implementation Plan: Sincronização Automática Spec Kit → Trello

**Branch**: `040-trello-sync-hooks` | **Date**: 2026-09-23 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/040-trello-sync-hooks/spec.md`

## Summary

Criar `.specify/extensions.yml` registrando 5 hooks obrigatórios (`after_specify`, `after_plan`, `after_tasks`, `before_implement`, `after_implement`), todos apontando para um único comando novo — `trello.sync-card` (`/trello-sync-card`) — com um argumento de fase diferente por hook (`prompt` da entrada do hook). Esse comando é uma skill fina (`.claude/skills/trello-sync-card/`) que só orquestra: lê `.specify/feature.json` para saber a feature atual e invoca um script PowerShell (`.specify/hooks/trello/sync-card.ps1`) que faz o trabalho de verdade contra a API REST do Trello (buscar/criar/mover card, comentar). O script nunca propaga exceção — captura tudo, registra em log, e sempre sai com sucesso, para nunca travar o fluxo real do Spec Kit (FR-011).

## Technical Context

**Language/Version**: PowerShell 5.1 (Windows PowerShell, o mesmo shell já usado por 100% dos scripts de automação do Spec Kit neste projeto — `.specify/scripts/powershell/*.ps1`, `"script": "ps"` em `init-options.json`). `Invoke-RestMethod`, nativo do PowerShell 5.1, cobre toda a necessidade de chamada REST/JSON contra a API do Trello sem nenhuma dependência externa.

**Primary Dependencies**: Nenhuma — `Invoke-RestMethod`/`ConvertTo-Json`/`ConvertFrom-Json` já vêm no PowerShell 5.1. Nenhum pacote NuGet, npm ou módulo PowerShell adicional.

**Storage**: Nenhuma no SPI — o "estado" da sincronização vive inteiramente no próprio Trello (qual lista o card está) e é redescoberto a cada chamada via busca por marcador na descrição do card (FR-002/FR-003), nunca persistido em arquivo ou banco do SPI.

**Testing**: Nenhuma suíte automatizada de unidade para o script (decisão explícita, ver research.md R8 — diverge do padrão de TDD pesado usado no restante desta sessão, e por isso está destacada, não escondida). Validação via `-DryRun` (roda toda a lógica de busca/decisão sem OU chamar a API do Trello) e via `quickstart.md`, contra um quadro de teste real no Trello.

**Target Platform**: Ambiente de desenvolvimento local (a máquina de quem roda o Claude Code/Spec Kit neste repositório) — não é implantado em nenhum servidor do SPI, não faz parte do runtime do produto.

**Project Type**: Ferramenta de processo de desenvolvimento (fora do domínio de negócio do SPI, conforme FR-013) — vive em `.specify/hooks/trello/` (script) e `.claude/skills/trello-sync-card/` (skill que o invoca), nunca em `src/SPI.*` nem `frontend/`.

**Performance Goals**: Sem meta nova — cada hook dispara no máximo algumas chamadas REST simples (buscar listas, buscar cards, criar/mover/comentar), no fim de comandos que já levam segundos a minutos; nenhuma meta de latência dedicada.

**Constraints**:
- FR-011: qualquer falha de sincronização MUST NOT interromper nem atrasar de forma perceptível o comando do Spec Kit em execução — o script sempre termina com sucesso do ponto de vista de quem o invocou, mesmo quando a sincronização real falhou.
- FR-012: credenciais (API Key + Token) só via variável de ambiente (ou um arquivo local `.env` fora do controle de versão, análogo ao `frontend/.env.local` já usado no projeto) — nunca hardcoded, nunca commitado.
- FR-009/FR-010: nenhuma automação toca "Fase de teste"/"Concluído"; nenhuma leitura do Trello volta para o Spec Kit.
- **Restrição de design derivada do Princípio III** (ver Constitution Check): o campo `condition` de uma entrada de hook em `extensions.yml`, pelo próprio mecanismo documentado em todas as skills `speckit-*` deste projeto, é **sempre ignorado** por Claude quando não-vazio ("skip the hook and leave condition evaluation to the HookExecutor implementation" — não existe HookExecutor real instalado neste projeto, confirmado por busca em `.specify/scripts/`). Registrar qualquer lógica condicional real nesse campo criaria exatamente o tipo de "capacidade configurável que o backend não processa" que o Princípio III proíbe. Por isso, **nenhuma entrada de hook desta feature usa `condition`** — toda decisão condicional (ex.: FR-007a, mover só se os testes passarem) vive dentro do próprio script.

**Scale/Scope**: 1 arquivo `.specify/extensions.yml` novo (5 entradas de hook), 1 skill nova (`trello-sync-card`), 1 script PowerShell novo com a lógica de negócio, 1 README de setup (como obter API Key/Token/Board ID), 2 padrões novos em `.gitignore`. Nenhuma mudança em `src/SPI.*`, `frontend/`, nem `database/`.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Princípio | Aplica-se? | Avaliação |
|---|---|---|
| I. Exclusão Lógica, Nunca Física | Não | Nenhum registro de domínio do SPI é criado, editado ou excluído. Cards do Trello não são uma entidade de domínio do SPI. |
| II. Validação de Negócio Única e Centralizada no Backend | Não diretamente | Não há regra de negócio do SPI aqui. A única "regra" (quando mover, quando não) tem um único dono: o script `sync-card.ps1`, nunca duplicada em outro lugar. |
| III. Nenhuma Capacidade Configurável Sem Implementação Real (NON-NEGOTIABLE) | Sim — **ponto de atenção ativo, não uma exceção** | O mecanismo de `condition` em `extensions.yml` é, na prática, um "canal morto" neste projeto (Claude sempre o ignora quando preenchido). Esta feature **evita ativamente** cair na armadilha do princípio: nenhuma entrada de hook usa `condition`; toda lógica condicional real (FR-007a) fica dentro do script, que de fato a executa. Diferente da exceção EX-001 (specs/037-039), aqui não há nenhuma capacidade anunciada e não implementada — é uma decisão de design que **previne** a violação, registrada aqui para deixar explícito o raciocínio. |
| IV. Autenticação e Segredos Seguros por Padrão | Sim | API Key + Token só via variável de ambiente ou `.env` local gitignored (mesmo padrão de `frontend/.env.local`) — nunca hardcoded, nunca commitado (FR-012). `.gitignore` ganha os padrões correspondentes. |
| V. Documentação Retroativa como Registro Histórico Vinculante | Não | Feature nova, sem comportamento retroativo em jogo. |

**Resultado**: Sem violação. Um ponto de atenção do Princípio III foi identificado e endereçado pelo próprio design (não usar `condition`), não uma exceção a ser aceita.

## Project Structure

### Documentation (this feature)

```text
specs/040-trello-sync-hooks/
├── plan.md              # This file (/speckit-plan command output)
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output
├── checklists/requirements.md
└── tasks.md             # Phase 2 output (/speckit-tasks command - NOT created by /speckit-plan)
```

### Source Code (repository root)

```text
.specify/
├── extensions.yml                          # NOVO -- registra os 5 hooks (after_specify,
│                                              # after_plan, after_tasks, before_implement,
│                                              # after_implement), todos optional:false,
│                                              # nenhum usa "condition" (ver Constraints acima)
└── hooks/
    └── trello/
        ├── sync-card.ps1                    # NOVO -- toda a logica de negocio: autenticar,
        │                                      # resolver quadro/listas, buscar/criar/mover
        │                                      # card, comentar, sempre sair com sucesso
        ├── README.md                        # NOVO -- setup: variaveis de ambiente, como obter
        │                                      # API Key/Token/Board ID, formato do .env local
        └── .env.example                     # NOVO -- template (sem valores reais) do .env local

.claude/
└── skills/
    └── trello-sync-card/
        └── SKILL.md                          # NOVO -- skill fina: le .specify/feature.json,
                                                 # chama sync-card.ps1 com a fase recebida,
                                                 # nunca falha visivelmente para quem a invocou

.gitignore                                    # + .specify/hooks/trello/.env
                                               # + .specify/hooks/trello/*.log
```

**Structure Decision**: Tudo fora de `src/SPI.*`/`frontend/`/`database/` (FR-013) — uma pasta de ferramenta de processo (`.specify/hooks/trello/`) mais uma skill fina de orquestração (`.claude/skills/trello-sync-card/`), replicando a separação já usada pelas skills `speckit-*` (orquestração em `.claude/skills/`, lógica reaproveitável nos scripts de `.specify/scripts/`).

## Post-Design Constitution Check

*Re-avaliação após Phase 1 (research.md, data-model.md, contracts/, quickstart.md).*

- Princípio III: `research.md` (R2, R7) e `contracts/extensions-yml.md` confirmam que nenhuma entrada de hook usa `condition` — a checagem de completude de FR-007/FR-007a é implementada inteiramente dentro de `sync-card.ps1` (R7), nunca declarada como uma configuração que o "backend" (o próprio mecanismo de hooks) não processa.
- Princípio IV: `contracts/trello-api.md` e o README de setup confirmam que nenhuma credencial aparece em nenhum artefato versionado; `quickstart.md` reforça a checagem manual disso.
- Nenhuma violação nova introduzida pelo design.

**Resultado**: Sem violação bloqueante.

## Complexity Tracking

Não aplicável — nenhuma violação identificada em nenhum dos dois Constitution Checks.
