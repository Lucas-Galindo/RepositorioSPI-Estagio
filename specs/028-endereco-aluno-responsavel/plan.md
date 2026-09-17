# Implementation Plan: Endereço do Aluno e do Responsável

**Branch**: `028-endereco-aluno-responsavel` | **Date**: 2026-09-17 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/028-endereco-aluno-responsavel/spec.md`

**Note**: This template is filled in by the `/speckit-plan` command; its definition describes the execution workflow.

## Summary

Adicionar endereço (CEP, Rua, Número, Complemento, Bairro, Cidade, Estado) ao Aluno, mais um
endereço opcional do responsável (exibido só quando `EhMenorDeIdade`), com um checkbox "Mesmo
endereço do aluno" (default marcado) que espelha o endereço do Aluno em tempo de leitura em vez
de duplicá-lo fisicamente. Descoberta importante durante a pesquisa: `EhMenorDeIdade` não é hoje
um campo persistido (só existe como flag de request) — o formulário de edição, portanto,
precisa inferir esse estado a partir da presença de dados de responsável já salvos (telefone,
email ou endereço), em vez de depender de uma coluna nova, para que a seção de endereço do
responsável reapareça corretamente ao reabrir a edição de um Aluno menor de idade já cadastrado.
O preenchimento automático por CEP usa a API pública ViaCEP chamada diretamente do frontend
(primeira integração externa client-side do projeto), sem bloquear o cadastro em caso de falha.

## Technical Context

**Language/Version**: C# / ASP.NET Core (.NET) no backend; TypeScript / React 19 (Next.js 16,
App Router) no frontend; MySQL — mesmo stack já usado pelo módulo de Aluno, nenhuma tecnologia
nova

**Primary Dependencies**: Nenhuma dependência nova — reutiliza FluentValidation (backend) e o
cliente HTTP nativo (`fetch`) do navegador para a chamada direta à API pública ViaCEP (frontend)

**Storage**: MySQL já existente — 1 migração nova (`database/13_endereco_aluno_responsavel.sql`)
adicionando 15 colunas nullable à tabela `alunos` já existente; nenhuma tabela nova

**Testing**: Sem framework de teste automatizado configurado no projeto — validação manual via
quickstart.md (mesma limitação já registrada em specs anteriores)

**Target Platform**: Navegador web (formulário e detalhe de Aluno já existentes) + API já
existente do SPI + API pública ViaCEP (terceiros)

**Project Type**: Web application (backend ASP.NET Core + frontend Next.js + MySQL já
existentes) — esta feature estende o módulo de Aluno já existente em ambas as camadas

**Performance Goals**: N/A — operação pontual por cadastro/edição de Aluno; a chamada a ViaCEP é
uma busca simples disparada só quando o CEP atinge 8 dígitos (sem polling nem chamadas em loop)

**Constraints**: A obrigatoriedade de CEP/Rua/Número (FR-002) MUST ser aplicada só na camada de
validação (FluentValidation), nunca via `NOT NULL` no banco, para não quebrar Alunos já
cadastrados sem endereço (ver research.md Decisão 3); a busca automática de CEP MUST NUNCA
bloquear o cadastro em caso de falha (FR-012); o endereço do responsável espelhado MUST refletir
mudanças no endereço do Aluno em tempo de leitura, não como cópia física persistida (FR raiz:
Assumptions da spec, research.md Decisão 2)

**Scale/Scope**: 15 campos novos numa entidade já existente (Aluno) — nenhuma entidade nova.
Backend: entidade + 3 DTOs + 2 validators + service + migração SQL (8 arquivos). Frontend: 3
interfaces TypeScript num arquivo já existente + formulário de Aluno + detalhe de Aluno + 1
helper novo (`lib/viacep.ts`) — nenhuma tela nova, nenhum componente novo de rota

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Avaliação contra os 5 princípios ratificados em `.specify/memory/constitution.md` (v1.0.0):

| Princípio | Aplicável? | Avaliação |
|---|---|---|
| I. Exclusão Lógica, Nunca Física | Não (observado) | Nenhuma mudança em exclusão/desativação de registros — só adição de campos |
| II. Validação de Negócio Única e Centralizada no Backend | Sim (observado) | A obrigatoriedade de CEP/Rua/Número e a regra condicional do responsável vivem só nos validators do backend (`CadastrarAlunoRequestValidator`/`AtualizarAlunoRequestValidator`); o frontend só exibe os erros retornados, não reimplementa a regra. A lógica de "mesmo endereço do aluno" (espelhamento) também é decidida e computada só no backend, não em código duplicado no cliente |
| III. Nenhuma Capacidade Configurável Sem Implementação Real | Sim (observado) | Os campos de endereço só aparecem no frontend depois que o backend realmente os aceita/persiste/retorna — nenhuma opção de UI exposta antes da capacidade real |
| IV. Autenticação e Segredos Seguros por Padrão | Não (observado) | A chamada a ViaCEP não usa nenhuma credencial (API pública sem chave); nenhuma mudança em autenticação/segredos do sistema |
| V. Documentação Retroativa como Registro Histórico Vinculante | Sim (observado) | `specs/003-gerenciar-alunos/spec.md` (FR-004) documenta a regra de responsável para menores de idade — esta feature estende essa regra (endereço, além de telefone/email) sem contradizer o que já está documentado; `specs/003` continua válida e não precisa ser reescrita |

**Resultado**: PASS — nenhum princípio é violado; Princípios II e III são diretamente
reforçados pela abordagem escolhida (validação só no backend, campos só liberados quando reais).

**Re-check pós-design (Phase 1)**: PASS, sem mudanças. O data-model.md confirma que toda regra
de obrigatoriedade e a lógica de espelhamento do endereço do responsável vivem inteiramente nos
validators/service do backend (research.md Decisões 2 e 3), reforçando o Princípio II.

## Project Structure

### Documentation (this feature)

```text
specs/028-endereco-aluno-responsavel/
├── plan.md              # This file (/speckit-plan command output)
├── research.md          # Phase 0 output (/speckit-plan command)
├── data-model.md         # Phase 1 output — 15 campos novos, migração SQL, regras de leitura
├── contracts/
│   └── aluno-endereco.md   # Phase 1 output — contrato dos campos nos endpoints de Aluno + ViaCEP
└── quickstart.md        # Phase 1 output (/speckit-plan command)
```

Sem `tasks.md` ainda — gerado pelo `/speckit-tasks` (Phase 2, não parte deste comando).

### Source Code (repository root)

```text
src/
├── SPI.Domain/Entities/
│   └── Aluno.cs                          # + 15 campos de endereço
└── SPI.Application/Alunos/
    ├── Dtos/
    │   ├── CadastrarAlunoRequest.cs      # + 15 campos
    │   ├── AtualizarAlunoRequest.cs      # + 15 campos
    │   └── AlunoResponse.cs              # + 15 campos (com espelhamento do responsável)
    ├── Validators/
    │   ├── CadastrarAlunoRequestValidator.cs   # + regras de CEP/Rua/Numero (Aluno e responsável)
    │   └── AtualizarAlunoRequestValidator.cs   # idem
    └── Services/
        └── AlunoService.cs               # CadastrarAsync: seta endereço apos o insert via
                                            #   procedure (sem alterar a procedure); AtualizarAsync
                                            #   e Mapear: incluem os 15 campos + logica de espelho

database/
└── 13_endereco_aluno_responsavel.sql     # ALTER TABLE alunos (15 colunas novas, todas nullable)

frontend/
├── lib/
│   ├── api/alunos.ts                     # Aluno/CadastrarAlunoRequest/AtualizarAlunoRequest: + 15 campos
│   └── viacep.ts                          # NOVO — helper de busca de CEP (ViaCEP), client-side
├── components/alunos/
│   └── AlunoForm.tsx                      # + secao de endereco do aluno; + secao condicional de
│                                            #   endereco do responsavel (checkbox "mesmo endereco");
│                                            #   inicializacao de ehMenorDeIdade inferida (ver research.md)
└── app/(app)/alunos/[id]/
    └── page.tsx                           # + exibicao do endereco do aluno e, se aplicavel, do responsavel
```

**Structure Decision**: Aplicação web já existente (backend ASP.NET Core + frontend Next.js +
MySQL). A mudança fica inteiramente dentro do módulo de Aluno já existente, em ambas as camadas,
mais 1 migração de banco e 1 helper novo de frontend para a integração com ViaCEP — nenhuma
entidade, tela ou endpoint novo.

## Complexity Tracking

> Sem violações de constituição a justificar (Constitution Check acima = PASS). A única decisão
> com mais de uma alternativa razoável (como persistir/inferir "é menor de idade" sem coluna
> nova) está documentada e justificada no research.md Decisão 1 — optar por inferir a partir de
> dados já existentes é a escolha que **menos** amplia o escopo, não uma violação de princípio.
