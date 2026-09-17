# Implementation Plan: Reativar Registros Desativados (Turma, Aluno, Matéria)

**Branch**: `026-reativar-registros-desativados` | **Date**: 2026-09-17 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/026-reativar-registros-desativados/spec.md`

**Note**: This template is filled in by the `/speckit-plan` command; its definition describes the execution workflow.

## Summary

Adicionar o caminho de volta que hoje não existe para Turma, Aluno e Matéria: um endpoint
`PATCH /{id}/reativar` por entidade (espelhando o `DELETE /{id}` já existente, mesma
autorização, sem checagem nova de negócio) e um botão "Reativar" na tela de detalhe de cada
entidade, visível só quando o registro está Inativo (e, como contraponto natural, o botão
"Excluir" passa a só aparecer quando o registro está Ativo). A resposta do PATCH devolve o
recurso atualizado para a tela atualizar seu estado local imediatamente, sem navegar para outra
página nem exigir recarregar. Nenhum dado histórico ou vínculo é tocado — só o campo `Ativo`.

## Technical Context

**Language/Version**: C# / ASP.NET Core (.NET) no backend; TypeScript / React 19 (Next.js 16,
App Router) no frontend — mesmo stack já usado pelas 3 entidades, nenhuma tecnologia nova

**Primary Dependencies**: Nenhuma dependência nova — reaproveita `ObterPorIdAsync`/
`SalvarAlteracoesAsync` já existentes nos 3 repositórios, e adiciona um helper `apiPatch` ao
cliente HTTP do frontend já existente (`frontend/lib/api/client.ts`), mesmo padrão dos outros 5
verbos já implementados ali

**Storage**: MySQL já existente — nenhuma migração de schema (o campo `Ativo` já existe nas 3
tabelas; a mudança é só de valor, via código de aplicação)

**Testing**: Sem framework de teste automatizado configurado no projeto (mesma limitação já
registrada em specs anteriores) — validação manual via quickstart.md

**Target Platform**: Navegador web (telas de detalhe já existentes de Turma, Aluno e Matéria) +
API já existente

**Project Type**: Web application (backend ASP.NET Core + frontend Next.js já existentes) —
esta feature toca ambos, mas sem criar nenhum projeto/módulo novo

**Performance Goals**: N/A — operação pontual sobre um único registro, mesmo perfil de custo do
`DELETE` já existente

**Constraints**: `ReativarAsync` MUST alterar somente o campo `Ativo` de cada entidade (FR-002);
MUST NOT introduzir nenhuma validação de negócio nova (FR-008, ver research.md Decisão 7); MUST
usar a mesma autorização (`Professor`) já aplicada à desativação (FR-006); MUST ser idempotente
quando chamado num registro já Ativo (FR-007)

**Scale/Scope**: 3 novos endpoints (1 por entidade) + 3 novos métodos de serviço + 3 novas
funções no cliente API do frontend + 1 novo helper `apiPatch` + 3 telas de detalhe alteradas
(botão condicional) — 16 arquivos de backend/frontend ao todo (3 controllers + 3 interfaces + 3
services + 1 `client.ts` + 3 `lib/api/*.ts` + 3 telas de detalhe), nenhum arquivo novo de
infraestrutura (banco, autenticação, etc.)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Avaliação contra os 5 princípios ratificados em `.specify/memory/constitution.md` (v1.0.0):

| Princípio | Aplicável? | Avaliação |
|---|---|---|
| I. Exclusão Lógica, Nunca Física | Sim (observado) | Esta feature é uma extensão direta do princípio: a exclusão já era lógica (via `Ativo`), e reativar apenas devolve o registro ao estado anterior pelo mesmo mecanismo — nenhuma exclusão física é introduzida ou removida |
| II. Validação de Negócio Única e Centralizada no Backend | Sim (observado) | O botão "Reativar" no frontend só chama o endpoint — nenhuma regra de negócio é decidida no cliente. O backend é a única fonte de verdade sobre se a reativação é permitida (hoje: sempre, salvo 404/autorização) |
| III. Nenhuma Capacidade Configurável Sem Implementação Real | Sim (observado) | O botão "Reativar" só passa a existir no frontend depois que o endpoint correspondente existir e funcionar de fato no backend — nenhuma opção de UI é exposta antes da capacidade real (evita repetir o bug documentado de WhatsApp/SMS em Lembretes) |
| IV. Autenticação e Segredos Seguros por Padrão | Não (observado) | Reaproveita a autorização já existente (`Professor`), nenhuma mudança em autenticação/segredos |
| V. Documentação Retroativa como Registro Histórico Vinculante | Sim (observado) | As specs retroativas de Turma (006), Aluno (003) e Matéria (004) documentam hoje só o fluxo de desativação de mão única — esta feature não as contradiz, apenas adiciona uma capacidade nova sobre o mesmo campo `Ativo` já documentado. Nenhuma dessas specs precisa ser reescrita, já que elas continuam descrevendo corretamente o comportamento de criação/desativação |

**Resultado**: PASS — nenhum princípio é violado; os Princípios I e III são diretamente
reforçados pela feature.

**Re-check pós-design (Phase 1)**: PASS, sem mudanças. O contrato dos 3 endpoints (ver
contracts/reativar-endpoints.md) confirma que nenhuma validação de negócio nova é introduzida
(Princípio II) e que a autorização usada é idêntica à já existente (Princípio IV não é tocado).

## Project Structure

### Documentation (this feature)

```text
specs/026-reativar-registros-desativados/
├── plan.md              # This file (/speckit-plan command output)
├── research.md          # Phase 0 output (/speckit-plan command)
├── contracts/
│   └── reativar-endpoints.md   # Phase 1 output — contrato dos 3 novos endpoints
├── quickstart.md        # Phase 1 output (/speckit-plan command)
└── tasks.md             # Phase 2 output (/speckit-tasks command - NOT created by /speckit-plan)
```

Sem `data-model.md`: nenhuma entidade ou campo novo é criado — `Ativo` já existe em Turma,
Aluno e Matéria; esta feature só adiciona um caminho de escrita a mais para um campo já
modelado (ver spec.md "Key Entities").

### Source Code (repository root)

```text
# Aplicação web já existente (backend ASP.NET Core + frontend Next.js + MySQL).

src/
├── SPI.Api/Controllers/
│   ├── TurmasController.cs      # + PATCH {id}/reativar (espelha Excluir)
│   ├── AlunosController.cs      # + PATCH {id}/reativar
│   └── MateriasController.cs    # + PATCH {id}/reativar
└── SPI.Application/
    ├── Turmas/Services/
    │   ├── ITurmaService.cs     # + ReativarAsync(int id)
    │   └── TurmaService.cs      # + ReativarAsync (espelha ExcluirAsync)
    ├── Alunos/Services/
    │   ├── IAlunoService.cs     # + ReativarAsync(int id)
    │   └── AlunoService.cs      # + ReativarAsync
    └── Materias/Services/
        ├── IMateriaService.cs   # + ReativarAsync(int id)
        └── MateriaService.cs    # + ReativarAsync

frontend/
├── lib/api/
│   ├── client.ts       # + apiPatch (helper generico, espelha apiPut)
│   ├── turmas.ts        # + reativarTurma(id, accessToken)
│   ├── alunos.ts        # + reativarAluno(id, accessToken)
│   └── materias.ts      # + reativarMateria(id, accessToken)
└── app/(app)/
    ├── turmas/[id]/page.tsx      # botao "Reativar" condicional (so quando !ativo)
    ├── alunos/[id]/page.tsx      # botao "Reativar" condicional
    └── materias/[id]/page.tsx    # botao "Reativar" condicional
```

**Structure Decision**: Aplicação web já existente (backend ASP.NET Core + frontend Next.js +
MySQL). A mudança fica inteiramente dentro dos módulos já existentes de Turma, Aluno e Matéria,
em ambas as camadas — nenhum controller, service, repositório, componente ou rota novos além do
necessário para espelhar o padrão de desativação já estabelecido, invertido.

## Complexity Tracking

> Sem violações de constituição a justificar (Constitution Check acima = PASS). A única decisão
> com mais de uma alternativa razoável (reaproveitar lógica via abstração genérica vs. métodos
> independentes espelhando o padrão já usado) está documentada e justificada no research.md
> Decisão 1 — optar pelos métodos independentes é a escolha que **menos** diverge da convenção
> já estabelecida no projeto, não uma violação dela.
