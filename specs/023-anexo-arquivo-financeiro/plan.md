# Implementation Plan: Anexo de Arquivo em Contas a Pagar e a Receber

**Branch**: `023-anexo-arquivo-financeiro` | **Date**: 2026-09-16 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/023-anexo-arquivo-financeiro/spec.md`

**Note**: This template is filled in by the `/speckit-plan` command; its definition describes the execution workflow.

## Summary

Adicionar, a cada registro de `ContaPagar` (Contas a Pagar) e `Pagamento` (Contas a Receber), a
capacidade de anexar exatamente um arquivo (nota fiscal/cupom fiscal — JPG, PNG ou PDF, até
10MB), com o conteúdo binário armazenado como BLOB diretamente na própria linha da tabela (não em
disco nem em serviço externo — decisão explícita do usuário, ver spec.md Assumptions), e os
metadados (nome original, tipo, tamanho, data do upload) nas mesmas colunas novas. Anexar um novo
arquivo sobrescreve a coluna existente — como o conteúdo mora na mesma linha do registro, não há
"lixo" a limpar (diferente de storage em disco): a substituição é uma única operação `UPDATE`
atômica. A mudança tem três pernas: (1) banco de dados — 5 colunas novas em cada uma das duas
tabelas, via novo script SQL numerado, seguindo a convenção já usada pelo projeto (não há EF Core
Migrations neste projeto); (2) backend — dois endpoints por entidade (upload/substituição e
download) reaproveitando uma validação de arquivo centralizada e compartilhada entre
`ContaPagarService` e `PagamentoService`; (3) frontend — um componente de anexo reaproveitado nas
telas de detalhe de Contas a Pagar e de Contas a Receber, mais a oferta automática de anexar logo
após salvar um novo registro (FR-013 a FR-015) e o diálogo de confirmação antes de substituir
(FR-016).

## Technical Context

**Language/Version**: C# / .NET (backend, `SPI.Application` + `SPI.Infrastructure` + `SPI.Api`) e TypeScript / React 19 (Next.js 16, App Router, frontend) — mesmo stack do restante do projeto, nenhuma linguagem nova

**Primary Dependencies**: Backend: ASP.NET Core `IFormFile`/multipart (já embutido no framework, nenhum pacote NuGet novo), EF Core (mapeamento de coluna binária nas configurações já existentes de `ContaPagar`/`Pagamento`), FluentValidation (validação de tamanho/formato do arquivo, mesmo padrão `*RequestValidator.cs` já usado no projeto). Frontend: Next.js/React já existentes, nenhuma dependência nova — upload via `FormData`/`fetch` nativo do navegador, sem biblioteca de upload

**Storage**: MySQL já existente — 5 colunas novas em `conta_pagar` e em `pagamento` (conteúdo binário `MEDIUMBLOB` + 4 colunas de metadados), adicionadas por um novo script SQL numerado em `database/` (`12_...sql`, seguindo a convenção de `01` a `11` já existente — ver research.md). Nenhuma EF Core Migration é usada neste projeto; o script SQL é a fonte de verdade do schema, e as `IEntityTypeConfiguration<T>` de `ContaPagar`/`Pagamento` são atualizadas manualmente em conjunto, como já é o padrão do projeto

**Testing**: Backend: xUnit (`tests/SPI.Application.Tests`) para a validação centralizada de arquivo (formato aceito/rejeitado por conteúdo real, limite de 10MB, mensagens de erro) — é a peça de lógica de negócio não trivial desta feature, na mesma linha da decisão tomada em specs/022 de testar a lógica de composição do serviço, não a UI. Frontend: sem framework de teste automatizado configurado (mesma limitação já registrada em specs anteriores) — verificação manual via quickstart.md

**Target Platform**: Navegador web (mesmas telas de Contas a Pagar/Receber já existentes), API ASP.NET Core já hospedada pelo backend do projeto

**Project Type**: Web application (frontend Next.js + backend ASP.NET Core já existentes) — esta feature toca os dois lados, mais o schema do banco

**Performance Goals**: N/A explícito no spec além de SC-001 (anexar em menos de 30s) — não é uma meta de performance de sistema, apenas de fluxo de UI simples; nenhum requisito de throughput/concorrência de upload

**Constraints**: Arquivo MUST ser rejeitado acima de 10MB (FR-003) e fora de JPG/PNG/PDF por conteúdo real, não extensão (FR-002) — validação MUST ficar centralizada em um único ponto reaproveitado por Contas a Pagar e Contas a Receber (Princípio II da constituição: validação de negócio única, não duplicada entre os dois controllers/services). O limite padrão do Kestrel para tamanho de requisição (30MB, `src/SPI.Api/Program.cs` não define um limite customizado hoje) já comporta um upload de 10MB com folga — nenhuma mudança de configuração de infraestrutura é necessária. Substituir um anexo MUST exigir confirmação explícita no frontend antes do envio (FR-016) — meramente uma barreira de UI, não uma restrição de API (o endpoint de upload sempre sobrescreve; a confirmação é responsabilidade exclusiva do cliente)

**Scale/Scope**: 1 script SQL novo (10 colunas: 5 por tabela × 2 tabelas), 2 configurações EF Core atualizadas (`ContaPagarConfiguration`, `PagamentoConfiguration`), 1 validador de arquivo compartilhado + 2 DTOs de metadados de anexo (um por entidade, ou um único reaproveitado — ver data-model.md), 4 endpoints novos no total (upload + download × 2 controllers), 2 telas de detalhe atualizadas (`contas-a-pagar/[id]/page.tsx`, `contas-a-receber/[id]/page.tsx` — nome exato do segundo a confirmar em research.md) mais as 2 telas de criação (`novo/page.tsx` × 2, apenas para adicionar a oferta pós-salvamento) + 1 componente de anexo novo reaproveitado nas duas telas + 2 novas funções de cliente de API (upload multipart, download binário) em `frontend/lib/api/client.ts`

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Avaliação contra os 5 princípios ratificados em `.specify/memory/constitution.md` (v1.0.0):

| Princípio | Aplicável? | Avaliação |
|---|---|---|
| I. Exclusão Lógica, Nunca Física | Não (observado) | O anexo não é uma entidade de domínio com histórico dependente (como Aluno/Turma/Matéria/Aula) — é uma coluna a mais na própria linha de `ContaPagar`/`Pagamento`, no mesmo nível de "Descricao" ou "Valor". Sobrescrever essa coluna ao substituir o anexo (FR-006) é o mesmo tipo de operação que já acontece hoje ao editar qualquer outro campo desses registros — não é uma "exclusão" de registro de domínio, e a spec já captura essa decisão explicitamente (Clarifications: confirmação antes de substituir, exatamente por ser irreversível) |
| II. Validação de Negócio Única e Centralizada no Backend | Sim | Formato aceito (conteúdo real do arquivo) e limite de 10MB MUST ser validados uma única vez, num único validador/serviço reaproveitado pelos dois pontos de entrada (Contas a Pagar e Contas a Receber) — nenhuma duplicação de regra entre os dois controllers |
| III. Nenhuma Capacidade Configurável Sem Implementação Real | Sim | O botão "Anexar arquivo" e a exibição de anexo só aparecem quando a capacidade real (upload/download persistidos em banco) está implementada — nenhuma opção fantasma |
| IV. Autenticação e Segredos Seguros por Padrão | Sim | Os endpoints novos de upload/download MUST exigir o mesmo `[Authorize(Roles = Professor)]` já usado pelos demais endpoints de `ContasPagarController`/`PagamentosController` (FR-010) — nenhum segredo novo envolvido; nenhuma mudança em autenticação |
| V. Documentação Retroativa como Registro Histórico Vinculante | Não | Nenhuma spec retroativa (001-019) descreve anexos — funcionalidade nova, sem conflito a resolver |

**Resultado**: PASS — nenhum princípio é violado por esta feature.

## Project Structure

### Documentation (this feature)

```text
specs/023-anexo-arquivo-financeiro/
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
# Esta feature toca banco de dados + backend + frontend.

database/
└── 12_anexo_comprovante_financeiro.sql   # novo: 5 colunas em conta_pagar e em pagamento
                                            # (conteudo binario MEDIUMBLOB + 4 metadados),
                                            # seguindo a convencao numerada 01-11 existente

src/
├── SPI.Domain/
│   └── Entities/
│       ├── ContaPagar.cs                 # + propriedades de anexo (conteudo, nome, tipo,
│       │                                   #   tamanho, data upload)
│       └── Pagamento.cs                  # + mesmas propriedades de anexo
├── SPI.Infrastructure/
│   └── Persistence/
│       └── Configurations/
│           ├── ContaPagarConfiguration.cs # + HasColumnName para as colunas novas
│           └── PagamentoConfiguration.cs  # + HasColumnName para as colunas novas
└── SPI.Application/
    ├── Anexos/                            # novo: validacao de arquivo compartilhada entre
    │   └── Services/                      #   ContaPagar e Pagamento (Principio II)
    │       └── IAnexoValidator.cs / AnexoValidator.cs
    ├── ContasPagar/
    │   ├── Dtos/AnexoResponse.cs          # novo: metadados do anexo (sem o binario)
    │   └── Services/ContaPagarService.cs  # + AnexarArquivoAsync/ObterArquivoAsync
    └── Pagamentos/
        ├── Dtos/AnexoResponse.cs          # (ou tipo compartilhado — ver data-model.md)
        └── Services/PagamentoService.cs   # + AnexarArquivoAsync/ObterArquivoAsync

src/SPI.Api/
├── Controllers/
│   ├── ContasPagarController.cs          # + POST/GET .../{id}/anexo
│   └── PagamentosController.cs           # + POST/GET .../{id}/anexo
└── Program.cs                             # + registro de IAnexoValidator/AnexoValidator no DI

frontend/
├── lib/api/
│   ├── client.ts                         # + helper de upload multipart e de download binario
│   ├── contasPagar.ts                    # + anexarArquivoContaPagar/obterAnexoContaPagar
│   └── pagamentos.ts                     # + anexarArquivoPagamento/obterAnexoPagamento
├── components/financeiro/
│   └── AnexoComprovante.tsx              # novo: componente reaproveitado nas duas telas de
│                                           #   detalhe (botao anexar/substituir, indicacao
│                                           #   visual, confirmacao antes de substituir)
└── app/(app)/financeiro/
    ├── contas-a-pagar/
    │   ├── [id]/page.tsx                 # usa <AnexoComprovante />
    │   └── novo/page.tsx                 # + oferta pos-salvamento (FR-013 a FR-015)
    └── contas-a-receber/
        ├── [id]/page.tsx                 # usa <AnexoComprovante />
        └── novo/page.tsx                 # + oferta pos-salvamento

tests/
└── SPI.Application.Tests/
    └── Anexos/                            # novo: testes do validador compartilhado
```

**Structure Decision**: Aplicação web já existente (frontend Next.js + backend ASP.NET Core +
MySQL). A mudança é aditiva em todas as camadas, seguindo exatamente os padrões já
estabelecidos pelo projeto: script SQL numerado (não EF Migrations), `IEntityTypeConfiguration<T>`
por entidade, DTOs de resposta separados do domínio, controllers finos delegando a services, e
componentes de frontend compartilhados quando a mesma UI se repete em duas telas (mesmo padrão já
usado para os botões de aba do Financeiro). A validação de arquivo (formato/tamanho) é
centralizada em um serviço novo e pequeno (`SPI.Application/Anexos/`) para não duplicar a regra
entre `ContaPagarService` e `PagamentoService` — nenhuma das duas entidades "possui" essa regra
de validação, então um serviço de apoio compartilhado é mais correto que colocar a lógica dentro
de uma delas e a outra reusá-la por acoplamento indireto.

## Complexity Tracking

> Sem violações de constituição a justificar (Constitution Check acima = PASS). A decisão de
> design com mais de uma opção razoável — como estruturar o anexo no banco de dados (colunas
> diretas vs. tabela separada) e como transportar o upload/download pela API — está registrada e
> resolvida em `research.md` (Phase 0).
