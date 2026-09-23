# Implementation Plan: Vínculo de Cobrança do Aluno

**Branch**: `037-vinculo-cobranca` | **Date**: 2026-09-20 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/037-vinculo-cobranca/spec.md`

## Summary

Criar a entidade `VinculoCobranca` (aluno + turma opcional + modalidade Avulsa/Mensalidade/Pacote + valor + campos específicos da modalidade + `Ativo`) como cadastro puro, com CRUD completo (criar, editar, listar por aluno, excluir/reativar logicamente) e uma seção "Vínculos de Cobrança" na tela de detalhe do Aluno, com adição/edição em modal dentro da própria seção. A unicidade "no máximo um vínculo ativo por Aluno+Turma e por Aluno sem turma" é garantida em duas camadas: verificação no serviço (mensagem clara, 409) e índice único sobre coluna gerada no MySQL (rede de segurança contra concorrência). A feature não toca em `AulaService.GerarContasAReceberAsync` nem em `Aluno.ValorAula` (FR-012), e isso é protegido por teste de não-regressão.

## Technical Context

**Language/Version**: C# / .NET 10 (backend, `net10.0`); TypeScript / Next.js App Router (frontend) — mesmas versões já usadas no projeto.

**Primary Dependencies**: ASP.NET Core Web API, EF Core + Pomelo MySQL 9.0, FluentValidation 12 — todas já presentes. Nenhuma dependência nova.

**Storage**: MySQL. Nova tabela `vinculo_cobranca` via script numerado `database/14_vinculo_cobranca.sql` (o repositório não usa migrations do EF; schema é versionado em `database/*.sql`). Modalidade guardada como `VARCHAR(20)` com `CHECK`, seguindo o padrão de `aula.status`; no C#, enum `ModalidadeCobranca` com `HasConversion<string>()`.

**Testing**: xUnit + `FluentValidation.TestHelper` em `tests/SPI.Application.Tests`, com fakes manuais de repositório (não há biblioteca de mock instalada, padrão de `RelatorioService*Tests`). Frontend não tem suíte de testes; validação por `quickstart.md` e `npm run lint`/`tsc`.

**Target Platform**: ASP.NET Core Web API + Next.js (web).

**Project Type**: Web application (backend em camadas Domain/Application/Infrastructure/Api + `frontend/`).

**Performance Goals**: Sem meta nova — listagem por aluno (poucos registros: no máximo 1 por turma do aluno + 1 individual, ativos).

**Constraints**:
- FR-012: `AulaService`, `Aluno.ValorAula` e a geração de contas a receber MUST NOT ser alterados nem passar a depender de `VinculoCobranca`.
- FR-013: a Turma do vínculo só pode ser uma turma ativa da qual o aluno participa; `VinculoCobranca` MUST NOT criar/alterar `AlunoTurma`.
- Exclusão sempre lógica (`Ativo`), verbos `ExcluirAsync`/`ReativarAsync` (Constituição I).
- Regras de negócio implementadas uma única vez no backend; o frontend só exibe erros (Constituição II).
- Migração aplicada via CLI `mysql` durante a implementação (connection string nos User Secrets .NET, não em `appsettings.json`).

**Scale/Scope**: 1 entidade, 1 enum, 1 tabela, 1 serviço, 1 repositório, 1 controller, 1 validator, 1 seção de frontend com modal. Um pequeno campo aditivo (`Ativo`) em `TurmaResumoResponse` para o formulário oferecer apenas turmas ativas.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Princípio | Aplica-se? | Avaliação |
|---|---|---|
| I. Exclusão Lógica, Nunca Física | Sim | `VinculoCobranca` é registro de domínio com histórico futuro (cobrança). "Excluir" define `Ativo = false`; nenhum `DELETE` físico é exposto. Reativação é a contrapartida. **Conforme.** |
| II. Validação de Negócio Única e Centralizada no Backend | Sim | Todas as regras (modalidade × campos aplicáveis, valor > 0, unicidade, turma elegível, conflito na reativação) ficam em `VinculoCobrancaRequestValidator` + `VinculoCobrancaService`. O modal do frontend só limpa campos não aplicáveis ao trocar de modalidade (conveniência de UI, sem decidir regra) e exibe erros da API. O dropdown de turmas filtra por `ativo` apenas para apresentação; a rejeição real é do backend. **Conforme.** |
| III. Nenhuma Capacidade Configurável Sem Implementação Real (NON-NEGOTIABLE) | Sim — **exceção conhecida e temporária, aceita pelo usuário** | Modalidades Avulsa/Mensalidade/Pacote e `AulasIncluidas`/`SaldoAulas` são opções configuráveis pelo usuário **sem** efeito automático nesta fatia, o que pelo texto literal do princípio é uma violação. O usuário confirmou (2026-09-20) aceitá-la, com o aviso na UI planejado e o compromisso de fechá-la na próxima fatia (ver Complexity Tracking, EX-001). Não é uma tensão genérica nem um esquecimento: é uma exceção rastreada, com condição de encerramento. |
| IV. Autenticação e Segredos Seguros por Padrão | Sim (mínimo) | Endpoints sob `[Authorize(Roles = Professor)]`, como `TurmasController`. A connection string para aplicar a migração vem dos User Secrets. Nenhum segredo versionado. **Conforme.** |
| V. Documentação Retroativa como Registro Histórico Vinculante | Não | Nenhuma spec retroativa descreve `VinculoCobranca`; nenhum comportamento existente documentado é alterado (FR-012 garante). |

**Resultado**: Sem violação bloqueante. Uma tensão justificada com o Princípio III registrada em Complexity Tracking.

## Project Structure

### Documentation (this feature)

```text
specs/037-vinculo-cobranca/
├── plan.md              # This file (/speckit-plan command output)
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/
│   └── vinculos-cobranca-api.md   # Phase 1 output
├── checklists/requirements.md
└── tasks.md             # Phase 2 output (/speckit-tasks command - NOT created by /speckit-plan)
```

### Source Code (repository root)

```text
database/
└── 14_vinculo_cobranca.sql                          # NOVO: tabela + FKs + CHECKs + índice único de ativos

src/
├── SPI.Domain/
│   ├── Enums/ModalidadeCobranca.cs                  # NOVO
│   ├── Entities/VinculoCobranca.cs                  # NOVO
│   ├── Entities/Aluno.cs                            # + ICollection<VinculoCobranca> (navegação; sem mudar ValorAula)
│   ├── Entities/Turma.cs                            # + ICollection<VinculoCobranca> (navegação)
│   └── Repositories/IVinculoCobrancaRepository.cs   # NOVO
├── SPI.Application/
│   ├── VinculosCobranca/
│   │   ├── Dtos/VinculoCobrancaRequest.cs           # NOVO
│   │   ├── Dtos/VinculoCobrancaResponse.cs          # NOVO
│   │   ├── Validators/VinculoCobrancaRequestValidator.cs   # NOVO
│   │   └── Services/{IVinculoCobrancaService,VinculoCobrancaService}.cs   # NOVO
│   ├── Common/Dtos/TurmaResumoResponse.cs           # + Ativo (aditivo)
│   ├── Alunos/Services/AlunoService.cs              # só popula TurmaResumoResponse.Ativo
│   └── DependencyInjection/ApplicationServiceCollectionExtensions.cs   # registra o serviço
├── SPI.Infrastructure/
│   ├── Persistence/Configurations/VinculoCobrancaConfiguration.cs      # NOVO
│   ├── Persistence/SpiDbContext.cs                  # + DbSet<VinculoCobranca>
│   ├── Repositories/VinculoCobrancaRepository.cs    # NOVO (traduz violação do índice único em ConflitoException)
│   └── DependencyInjection/InfrastructureServiceCollectionExtensions.cs   # registra o repositório
└── SPI.Api/
    └── Controllers/VinculosCobrancaController.cs    # NOVO — api/alunos/{alunoId}/vinculos-cobranca

frontend/
├── lib/api/vinculosCobranca.ts                      # NOVO (espelha os DTOs)
├── lib/api/alunos.ts                                # + ativo em TurmaResumo
├── components/alunos/VinculosCobrancaSection.tsx    # NOVO (lista + "Mostrar excluídos" + ações)
├── components/alunos/VinculoCobrancaFormModal.tsx   # NOVO (modal com formulário, padrão do popup de aula da Agenda)
├── app/(app)/alunos/[id]/page.tsx                   # renderiza a seção
└── styles/dashboard.css                             # classe de modal para o formulário (se necessária)

tests/
└── SPI.Application.Tests/
    └── VinculosCobranca/
        ├── VinculoCobrancaRequestValidatorTests.cs
        ├── VinculoCobrancaServiceTests.cs           # fakes manuais de repositório
        └── VinculoCobrancaNaoAfetaCobrancaAutomaticaTests.cs   # FR-012 (não-regressão)
```

**Structure Decision**: Segue exatamente o esqueleto por módulo já usado em `Turmas` (Domain entity + repository interface → Application Dtos/Validators/Services → Infrastructure Configuration/Repository → Api Controller), sem novo projeto nem camada. Aditivos em `Aluno`, `Turma` e `TurmaResumoResponse` são apenas navegação/campo novo; nenhuma propriedade ou método existente é alterado.

## Post-Design Constitution Check

*Re-avaliação após Phase 1 (research.md, data-model.md, contracts/, quickstart.md).*

- Princípios I, II e IV: confirmados pelo design — sem `DELETE` físico no contrato (`DELETE` HTTP apenas dispara `Ativo = false`), toda regra listada em `data-model.md` tem dono único no backend, endpoints sob role `Professor`.
- Princípio III: a exceção EX-001 permanece, aceita pelo usuário como conhecida e temporária (ver Complexity Tracking); o design não amplia o efeito do vínculo além do cadastro (FR-012 protegido por teste) e inclui o aviso na UI como parte do escopo desta fatia.
- Nenhuma nova violação introduzida pelo design.

**Resultado**: Sem violação bloqueante além da exceção EX-001, aceita explicitamente pelo usuário.

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| **EX-001 — Exceção conhecida e temporária ao Princípio III**: expor Modalidade/AulasIncluidas/SaldoAulas configuráveis sem efeito automático | O usuário pediu explicitamente, como fatia própria, o cadastro antes da cobrança automática ("próxima fatia"); o dado precisa existir e ser preenchido antes de a automação poder usá-lo | Adiar todo o cadastro até a automação juntar — descartado: impede validar o modelo de dados com dados reais e viola o recorte incremental pedido. |
| Unicidade em duas camadas (serviço + índice único em coluna gerada) | Regra genuinamente nova (não existe unicidade parcial/nula em nenhuma outra tabela) e sujeita a corrida entre duas requisições simultâneas | Só verificar no serviço — descartado: deixa janela de corrida que geraria dois vínculos ativos conflitantes, violando SC-002 ("100% rejeitadas"). |

### EX-001 — Registro da exceção ao Princípio III

- **Natureza**: exceção **CONHECIDA E TEMPORÁRIA**, decidida conscientemente e **aceita explicitamente pelo usuário em 2026-09-20**. Não é um esquecimento nem uma tensão genérica.
- **O que viola**: o Princípio III (NON-NEGOTIABLE) proíbe oferecer, como opção configurável, capacidade que o backend não processa de fato. Nesta fatia, Modalidade, Valor, Aulas Incluídas e Saldo de Aulas são cadastrados e persistidos, mas **nenhum processamento os consome** (FR-012: `AulaService.GerarContasAReceberAsync` e `Aluno.ValorAula` seguem intocados).
- **Mitigação obrigatória nesta fatia** (parte do escopo, não opcional): a seção "Vínculos de Cobrança" MUST exibir um aviso visível de que o vínculo é apenas um cadastro e ainda **não afeta a cobrança automática**, para que a professora nunca presuma que um vínculo cadastrado altera o valor das contas a receber (o risco do bug de referência, o Lembrete que ficava "Pendente" para sempre). Deve virar tarefa em `/speckit-tasks`.
- **Compromisso de encerramento**: a **próxima fatia** — ligação de `VinculoCobranca` a `AulaService.GerarContasAReceberAsync` (efeito real sobre o valor da cobrança e, conforme o desenho dela, sobre `SaldoAulas`/`AulasIncluidas`) — MUST ser priorizada o quanto antes e implementar o efeito real. A exceção só é considerada **encerrada** quando essa fatia estiver entregue; nesse momento o aviso da UI deve ser removido e este registro marcado como resolvido. Enquanto isso não ocorrer, nenhuma nova capacidade configurável sem efeito real deve ser adicionada por cima desta exceção (ela não abre precedente).
- **Prazo**: implícito ("o quanto antes"), sem data fixa; a próxima fatia deve citar EX-001 na sua própria spec/plan como pré-condição de fechamento.
- **Atualização (specs/038, 2026-09-22)**: a fatia prevista acima foi entregue — `AulaService.GerarContasAReceberAsync` agora consulta o `VinculoCobranca` correspondente ao contexto de cada presença. Modalidade **Avulsa** usa o `Valor` do vínculo (em vez de `Aluno.ValorAula`) e Modalidade **Pacote** decrementa `SaldoAulas` em vez de gerar cobrança — as duas passam a ter efeito real. Modalidade **Mensalidade** continua sem nenhum efeito automático (a cobrança mensal em si fica para um job futuro, explicitamente fora do escopo de specs/038) — por isso a exceção **não fecha por completo** e o aviso da UI ("Este vínculo é apenas um cadastro...") **permanece**, agora descrevendo com precisão apenas o caso Mensalidade. A exceção só fecha de vez (🔒 RESOLVIDA) quando esse job de Mensalidade existir e o aviso puder ser removido.
- **Atualização (specs/039, 2026-09-22)**: o job de cobrança mensal (`MensalidadeDispatcherService`) foi entregue — todo `VinculoCobranca` ativo de Modalidade Mensalidade passa a gerar automaticamente sua conta a receber no fim do mês, pelo `Valor` do vínculo. Com isso, as três modalidades (Avulsa, Pacote — specs/038; Mensalidade — specs/039) têm efeito automático real, sem exceção. O aviso "Este vínculo é apenas um cadastro..." foi removido de `frontend/components/alunos/VinculosCobrancaSection.tsx` (specs/039).
- **Status**: 🔒 RESOLVIDA (specs/039) — as três modalidades têm efeito automático real; aviso da UI removido; exceção encerrada conforme o compromisso original.
