# Implementation Plan: Link da aula temporário

**Branch**: `067-link-da-aula` | **Date**: 2026-11-03 | **Spec**: [spec.md](spec.md)

## Summary

A professora cola o link da reunião nos detalhes da aula; o servidor valida (http/https, sem espaços, ≤ 500) e o guarda **só em memória** (singleton `LinkAulaTemporario`), com validade até o fim da aula. A resposta da aula passa a trazer `linkAula` e `aceitaLinkAula`; os detalhes (tela e popup) mostram o link; o e-mail do lembrete de aula inclui o link da **próxima aula da turma** quando ele existe. **Nada vai para o banco** (sem coluna, tabela ou migração). Abordagem e alternativas: [research.md](research.md).

## Technical Context

**Language/Version**: C# 14 / .NET 10 (backend); TypeScript 5 / Next.js 16.3.4 / React 19.2.8 (frontend)

**Primary Dependencies**: nenhuma nova; FluentValidation (já usado), `TimeProvider` da plataforma (spec 064), `ErrosDeNegocioFilter` (spec 065) para 404/409

**Storage**: **nenhum** (memória do processo da API; o banco não muda)

**Testing**: xUnit com fakes manuais (`tests/SPI.Application.Tests`) e `TimeProvider` falso; validação final pela API autenticada, pelos testes do corpo do e-mail e no Chrome, como nas specs 063 e 065

**Target Platform**: servidor web único (Windows em desenvolvimento) + navegador desktop

**Project Type**: aplicação web (`src/` + `frontend/`)

**Performance Goals**: sem meta nova; a leitura do link é um acesso a dicionário por aula (listas de dezenas de aulas)

**Constraints**: FR-002 (nada em banco/arquivo), link nunca em log, esquema só http/https, sem `try/catch` novo em controller, nenhum arquivo em `database/`, regra de envio dos lembretes intacta

**Scale/Scope**: ~8 arquivos de backend (3 novos), 3 de frontend, 5 pacotes de testes; 0 migrações

## Constitution Check

| Princípio | Avaliação |
|---|---|
| I. Exclusão lógica | ✅ Nada é gravado nem apagado em banco; aula excluída apenas perde o link da memória |
| II. Validação única no backend | ✅ Validação do endereço, elegibilidade e expiração só no servidor (`RegraLinkAula`, validador); a tela só exibe `linkAula`/`aceitaLinkAula` e repassa o texto |
| III. Sem capacidade configurável sem implementação | ✅ Nenhuma opção nova; validade fixa (fim da aula), sem parâmetro de configuração |
| IV. Segredos | ✅ O link pode ter senha de reunião: não é gravado, não é logado; esquema restrito; e-mail e página escapam o conteúdo |
| V. Documentação como registro vinculante | ✅ Nota na spec do lembrete de aula (001) sobre o conteúdo opcional do e-mail (FR-012); relatório ao final |

**Re-avaliação pós-design**: sem violações. Complexity Tracking vazio.

## Project Structure

### Documentation (this feature)

```text
specs/067-link-da-aula/
├── spec.md
├── plan.md            # este arquivo
├── research.md        # decisões R1–R11
├── data-model.md      # sem banco; entidade em memória
├── quickstart.md
├── contracts/aulas-link-api.md
├── checklists/requirements.md
└── tasks.md
```

### Source Code (repository root)

```text
src/
├── SPI.Application/
│   ├── Common/RelogioExtensions.cs                    # NOVO só se a spec 064 ainda não o criou
│   ├── Aulas/
│   │   ├── Dtos/
│   │   │   ├── AulaResponse.cs                        # + LinkAula, AceitaLinkAula
│   │   │   └── DefinirLinkAulaRequest.cs              # NOVO { Link }
│   │   ├── Services/
│   │   │   ├── ILinkAulaTemporario.cs                 # NOVO (contrato)
│   │   │   ├── LinkAulaTemporario.cs                  # NOVO (singleton, em memória)
│   │   │   ├── RegraLinkAula.cs                       # NOVO (regra pura)
│   │   │   ├── IAulaService.cs / AulaService.cs       # DefinirLink/RemoverLink; Mapear; cancelar/excluir/editar
│   │   └── Validators/DefinirLinkAulaRequestValidator.cs   # NOVO
│   └── DependencyInjection/ApplicationServiceCollectionExtensions.cs   # singleton + TimeProvider
├── SPI.Infrastructure/BackgroundServices/LembreteDispatcherService.cs  # busca link e inclui no e-mail
└── SPI.Api/Controllers/AulasController.cs             # PUT/DELETE /{id}/link (sem try/catch)

frontend/
├── lib/api/aulas.ts                                   # campos novos + definirLinkAula/removerLinkAula
├── app/(app)/aulas/[id]/page.tsx                      # bloco "Link da aula"
└── components/aulas/AulaDetalhesPopup.tsx             # mostra o link (somente leitura)

tests/SPI.Application.Tests/
├── Aulas/{LinkAulaTemporarioTests,RegraLinkAulaTests,AulaServiceLinkTests}.cs   # NOVOS
├── Aulas/DefinirLinkAulaRequestValidatorTests.cs      # NOVO
└── Lembretes/LembreteDispatcherLinkTests.cs           # NOVO

specs/001-lembretes-canal-email/spec.md                # nota (FR-012): conteudo opcional do e-mail
docs/relatorios/<data>-link-da-aula.md
```

**Structure Decision**: aplicação web existente, em camadas. O armazenamento fica na Application (que já tem o framework ASP.NET) para não criar pacote nem projeto; o dispatcher (Infrastructure) depende só da interface. **Nada em `database/`.**

## Ordem sugerida (insumo do /speckit-tasks)

1. Fundação: ajudante de relógio (se faltar), contrato/implementação do armazenamento, `RegraLinkAula`, validador e testes deles; registro no DI.
2. US1: `AulaService` (definir/remover, `Mapear`), controller, frontend (detalhes e popup).
3. US3: ciclo de vida (cancelar/excluir/editar) — fica junto da US1 no `AulaService`.
4. US2: e-mail do lembrete.
5. Acabamento: nota na spec 001, relatório, Trello.

**Dependência da spec 064**: ambas editam `AulaService`, `AulaResponse` e `aulas/[id]/page.tsx` (pontos distintos). Recomenda-se implementar a 064 primeiro; se a 067 vier antes, a fundação cria `RelogioExtensions.cs`/registro de `TimeProvider` e a 064 apenas os reaproveita.

## Verificações "MUST NOT" que o /speckit-tasks deve cobrir (regra do CLAUDE.md)

| Requisito | Verificação exigida |
|---|---|
| FR-002 não gravar em banco/arquivo | `git status --porcelain -- database` vazio; `git diff -- src/SPI.Domain src/SPI.Infrastructure/Persistence` sem mudança de entidade; `grep -rn "LinkAula" src/SPI.Domain src/SPI.Infrastructure/Persistence database` sem resultado; `DESCRIBE aula` antes/depois igual no banco local |
| FR-003 não aceitar outros esquemas | testes do validador **e** leitura do diff do validador (nenhuma lista que inclua `javascript`, `data`, `ftp`) |
| FR-005 não mostrar/enviar link expirado | testes do armazenamento (exato no fim) e do dispatcher |
| FR-008 não alterar regra de envio | testes atuais do dispatcher sem alteração de `Assert` + leitura do diff de `LembreteDispatcherService` (só entra o trecho do link; filtros, orçamento, tentativas e status intactos) |
| FR-009 não logar o link | `grep -rn` do nome do parâmetro/propriedade do link em chamadas `_logger`/`logger?.Log` e em mensagens de exceção: sem resultado |
| FR-010 aula sem link como hoje | teste de não regressão do e-mail (texto exato) e leitura do diff de `Mapear` |
| FR-011 frontend não decide | leitura do diff de `aulas/[id]/page.tsx` e `AulaDetalhesPopup.tsx`: `grep -n "new Date\|Date.now\|startsWith\|new URL\|http" ` sem validação/decisão; elegibilidade vem só de `aceitaLinkAula` |
| Sem arquivo novo indevido | `git status --porcelain -- src tests frontend database` com a lista esperada (nenhum componente de frontend novo) |

## Complexity Tracking

Sem violações da constituição a justificar.
