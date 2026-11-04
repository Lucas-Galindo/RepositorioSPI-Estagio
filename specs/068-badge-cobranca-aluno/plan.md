# Implementation Plan: Selo de cobrança do aluno nas telas da professora

**Branch**: `068-badge-cobranca-aluno` | **Date**: 2026-11-03 | **Spec**: [spec.md](spec.md)

## Summary

A professora passa a ver, sem abrir a ficha, a cobrança de cada aluno: na **lista de alunos** (resumo: categoria principal + "+N", dica com todos os vínculos), na **página da turma** (o vínculo que vale naquela turma) e na **chamada** (o vínculo que vale para aquela aula). O backend calcula tudo em lote num novo serviço de leitura (`ResumoCobrancaService`), reaproveitando `RegraCobrancaPresenca.Decidir` (a mesma regra que cobra a presença) e `ProgressoPacote.Calcular` (spec 058), e devolve um objeto `cobranca` junto das listagens existentes, **sem consulta por aluno**. O frontend ganha **um** componente (`SeloCobranca`) que só exibe, reaproveitando as pílulas `status-pill` já verificadas. Somente leitura: sem migração e sem mudar nenhuma regra de cobrança. O resumo da lista é **sob demanda** (`incluirCobranca=true`, pedido só pela tela de Alunos), porque a lista de alunos é usada por 12 telas só para selecionar um aluno. Abordagem e alternativas: [research.md](research.md).

## Technical Context

**Language/Version**: C# 14 / .NET 10 (backend); TypeScript 5 / Next.js 16.3.4 / React 19.2.8 (frontend)

**Primary Dependencies**: nenhuma nova; reaproveita `RegraCobrancaPresenca`, `ProgressoPacote`, `IAulaRepository.ContarPresencasPorVinculosAsync`, `status-pill` e `currency` do frontend

**Storage**: nenhuma mudança (somente leitura; sem migração)

**Testing**: xUnit com fakes manuais (`dotnet test tests/SPI.Application.Tests`, sem `--no-build`, porque o `.slnx` não compila os testes); validação final pela API autenticada, pelo Chrome (claro e escuro) e pelo contraste, como nas specs 063, 064 e 066

**Target Platform**: servidor web único + navegador desktop

**Project Type**: aplicação web (`src/` + `frontend/`)

**Performance Goals**: no máximo **2 consultas** por tela (vínculos ativos em lote + presenças dos pacotes em lote), independente do número de alunos; mesmo número de requisições HTTP de antes

**Constraints**: sem N+1; sem regra no frontend; sem migração; só tokens do tema; um único componente novo de frontend; nenhuma regra de cobrança alterada

**Scale/Scope**: ~8 arquivos de backend (3 novos), ~6 de frontend (1 novo), 4 pacotes de testes; 0 migrações

## Constitution Check

| Princípio | Avaliação |
|---|---|
| I. Exclusão lógica | ✅ Somente leitura; só vínculos ativos contam; nada é apagado |
| II. Validação única no backend | ✅ Principal, "o que vale", pacote esgotado e aulas restantes são decididos no servidor (`Decidir`, `ProgressoPacote`); o frontend só exibe (FR-007) |
| III. Sem capacidade configurável sem implementação | ✅ Nenhuma opção nova; Semestral/Anual entram sozinhas quando a spec 062 existir |
| IV. Segredos | ✅ Sem impacto |
| V. Documentação como registro vinculante | ✅ Comportamento de cobrança não muda (nenhuma spec anterior fica desatualizada); nota curta nas specs 058 (pacote na tela) e 056 (regra reutilizada) apontando para esta; relatório ao final |

**Re-avaliação pós-design**: sem violações. Complexity Tracking vazio.

## Project Structure

### Documentation (this feature)

```text
specs/068-badge-cobranca-aluno/
├── spec.md
├── plan.md            # este arquivo
├── research.md        # decisões R1–R9
├── data-model.md      # DTOs calculados; sem banco
├── quickstart.md
├── contracts/selo-cobranca-api.md
├── checklists/requirements.md
└── tasks.md           # /speckit-tasks
```

### Source Code (repository root)

```text
src/
├── SPI.Domain/Repositories/IVinculoCobrancaRepository.cs        # + ListarAtivosDosAlunosAsync
├── SPI.Infrastructure/Repositories/VinculoCobrancaRepository.cs # implementação (uma consulta IN)
├── SPI.Application/
│   ├── Common/Dtos/AlunoResumoResponse.cs                       # + Cobranca
│   ├── Alunos/Dtos/AlunoResponse.cs                             # + Cobranca (resumo da lista)
│   ├── Aulas/Dtos/AlunoPresencaResponse.cs                      # + Cobranca
│   ├── VinculosCobranca/
│   │   ├── Dtos/SeloCobrancaResponse.cs                         # NOVO (+ DetalheCobrancaResponse)
│   │   └── Services/
│   │       ├── IResumoCobrancaService.cs                        # NOVO
│   │       └── ResumoCobrancaService.cs                         # NOVO (lote; reaproveita Decidir/Progresso)
│   ├── Alunos/Services/IAlunoService.cs + AlunoService.cs       # ListarAsync(..., incluirCobranca = false) preenche o resumo só sob demanda
│   ├── Turmas/Services/TurmaService.cs                          # MapearComPacotesAsync preenche o selo do grupo
│   ├── Aulas/Services/AulaService.cs                            # retornos de uma aula preenchem o selo do grupo
│   └── DependencyInjection/ApplicationServiceCollectionExtensions.cs  # AddScoped
└── SPI.Api/Controllers/AlunosController.cs                      # Listar: parâmetro opcional ?incluirCobranca=true (único controller tocado)

frontend/
├── components/alunos/SeloCobranca.tsx                           # NOVO (único componente novo)
├── lib/api/{alunos,turmas,aulas}.ts                             # tipo `cobranca`
├── app/(app)/alunos/page.tsx                                    # coluna "Cobrança" (pede incluirCobranca=true)
├── app/(app)/turmas/[id]/page.tsx                               # selo por aluno (modo grupo)
└── app/(app)/aulas/[id]/page.tsx                                # selo por aluno e por "Outros alunos"

tests/SPI.Application.Tests/
├── VinculosCobranca/ResumoCobrancaServiceTests.cs               # NOVO
├── Alunos/AlunoServiceCobrancaListaTests.cs                     # NOVO
├── Turmas/TurmaServiceCobrancaTests.cs                          # NOVO
├── Aulas/AulaServiceCobrancaSeloTests.cs                        # NOVO
└── (fakes de IVinculoCobrancaRepository: + método novo, sem mudar Asserts)

docs/relatorios/<data>-badge-cobranca-aluno.md
specs/058-progresso-pacote/spec.md, specs/056-cobranca-por-aluno/spec.md   # notas
```

**Structure Decision**: aplicação web existente, em camadas. O serviço novo mora em `VinculosCobranca/Services` (junto de `ProgressoPacote`); os controllers não mudam. **Nada em `database/`.**

## Ordem sugerida (insumo do /speckit-tasks)

1. Fundação: DTOs, método novo do repositório (+ fakes), `ResumoCobrancaService` com testes (lista, grupo, consistência com `Decidir`, guarda de N+1) e registro no DI.
2. US1 (lista): `AlunoService.ListarAsync` + coluna no frontend.
3. US2 (turma): `TurmaService.MapearComPacotesAsync` + selo na página da turma.
4. US3 (chamada): `AulaService` (retornos de uma aula) + selo na chamada.
5. Acabamento: contraste, build, validação pela API e Chrome, notas nas specs 056/058, relatório, Trello.

O componente `SeloCobranca` nasce na US1 e é reaproveitado nas US2 e US3.

## Verificações "MUST NOT" que o /speckit-tasks deve cobrir (regra do CLAUDE.md)

| Requisito | Verificação exigida |
|---|---|
| FR-006 sem consulta por aluno | teste que conta chamadas ao fake do repositório (uma por lista) **e** leitura do diff de `ResumoCobrancaService`/serviços: nenhum `await` dentro de `foreach`/`Select` por aluno; no frontend, `grep` de `listarVinculos`/`vinculos-cobranca` nas três telas sem resultado |
| FR-007 frontend não decide | **leitura do diff** de `SeloCobranca.tsx` e das três páginas: nenhuma ordenação, `.filter`/`.find` de vínculo, comparação de `categoria` para escolher, nem conta de aulas restantes; `grep -n "\.sort(\|\.filter(\|\.find(\|aulasRestantes -\|saldo" ` nesses arquivos sem lógica de cobrança |
| FR-008 não alterar dado nem regra | `git status --porcelain -- database` vazio; `git diff` de `RegraCobrancaPresenca.cs` e `ProgressoPacote.cs` vazio; contagens das tabelas iguais antes/depois; nenhum `SaveChanges`/`Adicionar`/`Atualizar` no serviço novo |
| FR-009 cores só do tema | `git diff -- frontend/styles frontend/lib/paleta.ts` vazio; `grep` por `#` (cores literais) e `rgb(` no componente novo sem resultado; `npm run verificar-contraste` |
| FR-013 resumo só sob demanda | teste: sem `incluirCobranca` o serviço **não** chama o resumo e `Cobranca` fica `null`; `grep -rn "incluirCobranca" frontend` mostra **somente** `lib/api/alunos.ts` e `alunos/page.tsx` (as outras 11 telas não pedem) |
| FR-010 um único componente novo | `git status --porcelain -- frontend` mostra **somente** `SeloCobranca.tsx` como arquivo novo (nenhum outro `??`) |
| FR-012 selo sem ação | leitura do diff: o componente não tem `onClick` nem links de edição |
| Sem arquivo novo indevido | `git status --porcelain -- src tests frontend database` com a lista esperada; `git diff -- src/SPI.Api/Controllers` mostra **somente** o parâmetro novo de `AlunosController.Listar` |

## Complexity Tracking

Sem violações da constituição a justificar.
