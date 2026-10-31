# Implementation Plan: Semestralidade e Anuidade como Formas de Cobrança do Aluno

**Branch**: `062-semestralidade-anuidade` | **Date**: 2026-10-29 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/062-semestralidade-anuidade/spec.md`

## Summary

Duas modalidades novas no enum (`Semestral = 4`, `Anual = 5`) que **se comportam como a Mensalidade na hora da aula** (cobrem tudo, sem cobrança por presença) mas **têm vigência** (data de início e fim) e **parcelas próprias**.

A regra fica toda no backend:
- **Cadastro**: `VinculoCobrancaService.CadastrarAsync` cria o vínculo e todas as parcelas (contas a receber "Pendente") **num único salvamento**, porque os repositórios compartilham o mesmo contexto do banco. As parcelas são `Pagamento` já existentes, ligadas ao vínculo, em categoria própria; **não há tabela nova de parcelas**.
- **Presença**: `RegraCobrancaPresenca.Decidir` passa a receber a data da aula. Mensalidade vale sempre; Semestral e Anual valem só dentro do período. O tipo `Mensalidade` da decisão vira `PlanoPeriodico`.
- **Exclusão**: cancela, na mesma transação, as parcelas pendentes com vencimento depois de hoje.
- **Renovação**: um tipo novo de notificação (`RenovacaoPlano`), sincronizado no mesmo `NotificacaoService` das specs 059 e 060.

No frontend, o modal de vínculo ganha início, número de parcelas e um editor de parcelas (a sugestão de parcelas iguais é conveniência de tela; a validação autoritativa é do backend).

## Technical Context

**Language/Version**: C# / .NET 10; TypeScript / React 19 / Next.js 16

**Primary Dependencies**: nenhuma nova (EF Core, FluentValidation, xUnit com fakes manuais)

**Storage**: MySQL; migração `database/24_semestralidade_anuidade.sql`

**Testing**: xUnit para a regra (cadastro com parcelas, combinação, vigência, exclusão, notificação, e-mail); `tsc`, `eslint`, build e leitura do diff no frontend; quickstart contra a API local

**Project Type**: web application

**Constraints**: nenhuma parcela duplicada (índice único já existente); tudo-ou-nada no cadastro; sem cobrança por presença dentro do período; só tokens de tema no frontend (spec 057); e-mail sempre redirecionado em desenvolvimento (spec 060)

**Scale/Scope**: dezenas de alunos; até 24 parcelas por vínculo

## Constitution Check

- **I. Exclusão lógica**: o vínculo é inativado, nunca apagado; as parcelas canceladas ficam como "Cancelado", nunca removidas.
- **II. Regra de negócio no backend**: vigência, exclusividade, geração e cancelamento de parcelas, e o aviso de renovação ficam no backend. A sugestão de parcelas iguais no formulário é só conveniência de tela, e o backend revalida tudo (valor, mês único, limite de 24).
- **III. Nada configurável sem efeito**: `Notificacoes:RenovacaoDiasAntes` é lido e usado de fato; o valor total do período e as parcelas têm efeito real (geram as contas).
- **IV. Segredos / perfil**: endpoints existentes, sob o perfil Professor; nenhum segredo novo.
- **V. Documentação retroativa vinculante**: notas nas specs 037 (modalidades), 039 (a Mensalidade deixa de ser a única forma periódica) e 056 (exclusividade e prioridade de cobrança passam a incluir Semestral e Anual).

**Resultado**: PASS.

## Project Structure

```text
database/24_semestralidade_anuidade.sql                              # NOVO
src/SPI.Domain/Enums/ModalidadeCobranca.cs                           # + Semestral=4, Anual=5
src/SPI.Domain/Enums/ModalidadeCobrancaExtensions.cs                 # NOVO: EhPlanoPeriodico, MesesDoPeriodo
src/SPI.Domain/Enums/TipoNotificacao.cs                              # + RenovacaoPlano
src/SPI.Domain/Entities/VinculoCobranca.cs                           # + DataInicio, DataFim
src/SPI.Domain/Repositories/{IVinculoCobranca,IPagamento}Repository.cs   # consultas novas
src/SPI.Infrastructure/Persistence/Configurations/VinculoCobrancaConfiguration.cs
src/SPI.Infrastructure/Repositories/{VinculoCobranca,Pagamento}Repository.cs
src/SPI.Application/VinculosCobranca/Dtos/{VinculoCobrancaRequest,VinculoCobrancaResponse,ParcelaRequest,ParcelaResponse}.cs
src/SPI.Application/VinculosCobranca/Validators/VinculoCobrancaRequestValidator.cs
src/SPI.Application/VinculosCobranca/Services/VinculoCobrancaService.cs     # cadastro com parcelas, exclusão, regras
src/SPI.Application/Aulas/Services/{RegraCobrancaPresenca,AulaService}.cs   # vigência + PlanoPeriodico
src/SPI.Application/Notificacoes/...                                 # RenovacaoPlano (service, options, DTO)
src/SPI.Infrastructure/BackgroundServices/MensalidadeLembreteDispatcherService.cs  # texto geral
src/SPI.Api/Controllers/VinculosCobrancaController.cs                # respostas 409 / confirmação
src/SPI.Api/appsettings.json                                         # Notificacoes:RenovacaoDiasAntes
tests/SPI.Application.Tests/...                                      # novos e ajustados
frontend/lib/api/{vinculosCobranca,notificacoes}.ts
frontend/lib/parcelas.ts                                             # NOVO: sugestão de parcelas iguais (só conveniência)
frontend/components/alunos/{VinculoCobrancaFormModal,VinculosCobrancaSection}.tsx
frontend/components/layout/NotificacoesSino.tsx
frontend/styles/dashboard.css                                        # só tokens de tema
specs/{037,039,056}/spec.md                                          # notas de substituição (Princípio V)
```

**Structure Decision**: sem tabela de parcelas e sem rotina nova. As parcelas são `Pagamento` ligados ao vínculo, com a trava de duplicidade que o banco já tem. A vigência é calculada na hora da presença, em vez de uma rotina que desative vínculos.

## Complexity Tracking

Sem violações.
