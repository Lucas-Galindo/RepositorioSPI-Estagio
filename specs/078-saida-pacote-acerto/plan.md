# Implementation Plan: Saída do aluno de turma de pacote com acerto proporcional ao consumo

**Branch**: `078-saida-pacote-acerto` | **Date**: 2026-11-08 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/078-saida-pacote-acerto/spec.md`

## Summary

Um botão "Registrar saída" na turma de pacote mostra o **acerto proporcional ao consumo** (devido = valor × consumidas ÷ total, arredondado a centavos) com o que a professora informa que o aluno já pagou e, ao confirmar, **numa transação só**: desvincula o aluno, encerra o pacote (marcando `saida_registrada_em`) retira o aluno das aulas agendadas da turma que ainda não começaram e, se ele pagou menos do que consumiu, cria **uma conta a receber Pendente** da diferença na categoria "Acerto de pacote" (mecanismo das contas ligadas ao vínculo da spec 062). Pagou a mais: nada é devolvido. Abordagem: serviço novo `SaidaPacoteService` + cálculo puro `AcertoSaidaPacote`, dois endpoints sob a turma (prévia e gravação), uma migração pequena (28) e um modal novo na tela da turma. Detalhes em [research.md](research.md).

## Technical Context

**Language/Version**: C# / .NET 10 (backend); TypeScript, Next.js 16 / React 19 (frontend)

**Primary Dependencies**: ASP.NET Core Web API, EF Core + Pomelo MySQL, FluentValidation, xUnit; frontend sem pacote novo

**Storage**: MySQL (`spi_db`). Migração 28: coluna `vinculo_cobranca.saida_registrada_em`, `CHECK` e a categoria de receita "Acerto de pacote" (só acréscimos). Ver [data-model.md](data-model.md)

**Testing**: xUnit com fakes (grade do cálculo, serviço, validador, reativação, `PodeRegistrarSaida`); validação real na API e no Chrome ([quickstart.md](quickstart.md))

**Target Platform**: aplicação web, sistema de uma professora (single-tenant)

**Project Type**: web application (backend em camadas + frontend)

**Performance Goals**: prévia e gravação em menos de 1 s (poucas consultas por chave e uma contagem de presenças)

**Constraints**: cálculo só no servidor (Princípio II); a gravação é atômica; "Desvincular" não muda; nenhuma saída de dinheiro; relógio do sistema (`TimeProvider`/`AgoraLocal`)

**Scale/Scope**: um aluno por saída; sem processamento em lote

## Constitution Check

| Princípio | Avaliação |
|---|---|
| I. Exclusão lógica | ✅ o pacote é **inativado** (`ativo = false` + `saida_registrada_em`), nunca apagado; nenhuma conta é apagada. A matrícula (`alunos_turma`) é removida como o desvincular de hoje já faz (tabela de junção, comportamento existente da spec 063) |
| II. Validação de negócio única no backend | ✅ prévia e gravação usam `AcertoSaidaPacote`; a tela só envia o pago/vencimento e ecoa os números que o servidor devolveu (FR-014) |
| III. Sem capacidade configurável sem implementação | ✅ nada de opção nova configurável; a categoria "Acerto de pacote" é inserida junto com o código que a usa |
| IV. Autenticação e segredos | ✅ rotas autenticadas como as demais da turma; nenhum segredo novo |
| V. Documentação como registro vinculante | ✅ nota na spec 063 (FR-015) e no card; relatório ao fim |

**Resultado**: sem violações; nada a justificar em "Complexity Tracking". Reavaliado depois do desenho: igual.

## Project Structure

### Documentation (this feature)

```text
specs/078-saida-pacote-acerto/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/saida-pacote.md
├── checklists/requirements.md
└── tasks.md            # /speckit-tasks
```

### Source Code (repository root)

```text
database/
└── 28_saida_pacote_acerto.sql                                  # NOVO (coluna, CHECK, categoria)

src/SPI.Domain/Entities/VinculoCobranca.cs                       # + SaidaRegistradaEm
src/SPI.Infrastructure/Persistence/Configurations/VinculoCobrancaConfiguration.cs  # + coluna

src/SPI.Application/VinculosCobranca/
├── Dtos/SaidaPacoteRequest.cs, AcertoSaidaPacoteResponse.cs, SaidaPacoteRegistradaResponse.cs   # NOVOS
├── Dtos/VinculoCobrancaResponse.cs                              # + SaidaRegistradaEm
├── Validators/SaidaPacoteRequestValidator.cs                    # NOVO
└── Services/
    ├── AcertoSaidaPacote.cs                                     # NOVO: cálculo puro
    ├── ISaidaPacoteService.cs, SaidaPacoteService.cs            # NOVOS
    └── VinculoCobrancaService.cs                                # ReativarAsync recusa pacote encerrado pela saída; mapeia SaidaRegistradaEm
src/SPI.Application/Common/Dtos/AlunoResumoResponse.cs           # + PodeRegistrarSaida
src/SPI.Application/Turmas/Services/TurmaService.cs              # só MapearComPacotesAsync preenche PodeRegistrarSaida (desvincular intacto)
src/SPI.Domain/Repositories/IAulaRepository.cs + src/SPI.Infrastructure/Repositories/AulaRepository.cs  # + ListarParticipacoesAgendadasQueNaoComecaramAsync e RemoverParticipacoes
src/SPI.Application/DependencyInjection/ApplicationServiceCollectionExtensions.cs  # registra o serviço
src/SPI.Api/Controllers/TurmasController.cs                      # + 2 POSTs (DELETE intacto)

tests/SPI.Application.Tests/VinculosCobranca/
├── AcertoSaidaPacoteTests.cs, SaidaPacoteServiceTests.cs, SaidaPacoteRequestValidatorTests.cs     # NOVOS
└── (testes existentes de reativar/turmas estendidos)

frontend/
├── lib/api/turmas.ts                                            # + tipos e funções da saída; podeRegistrarSaida
├── lib/api/vinculosCobranca.ts                                  # + saidaRegistradaEm
├── components/turmas/SaidaPacoteModal.tsx                       # NOVO
├── app/(app)/turmas/[id]/page.tsx                               # botão "Registrar saída" ao lado de "Remover"
└── components/alunos/VinculosCobrancaSection.tsx                # esconde "Reativar" e mostra "Saída registrada em"

docs: nota em specs/063-pacote-fora-da-turma/spec.md; relatório em docs/relatorios/
```

**Structure Decision**: segue as camadas e os padrões já usados (specs 062/077): cálculo estático puro + serviço de aplicação + controller fino, migração numerada, DTOs que o frontend espelha 1:1 com `/** Espelha ... */`.

## Complexity Tracking

Sem violações da constituição; nada a justificar.
