# Implementation Plan: Turmas de Pacote e Pacote Coletivo por Turma

**Branch**: `063-pacote-fora-da-turma` | **Date**: 2026-10-30 (terceira revisão) | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/063-pacote-fora-da-turma/spec.md`

## Summary

A turma ganha um **tipo** (Mensalidade ou Pacote). O aluno de pacote **continua matriculado** na turma de pacote; o que muda é que **todo pacote passa a pertencer a um grupo** (uma turma de pacote, ou aulas individuais), que o pacote **só vale em aula do seu grupo**, e que a turma de pacote é **fechada** depois que começa e **encerra os pacotes dela** quando acaba. A regra de cobrança de presença continua num lugar só (`RegraCobrancaPresenca`) e fica mais simples: um pacote só é aplicável quando `pacote.TurmaId == aula.TurmaId`.

Seis peças, todas no backend (Princípio II):

1. **Banco** (`database/25_turmas_de_pacote.sql`): `turma.tipo`, `turma.data_inicio`, `turma.data_fim`; o `CHECK` do vínculo passa a aceitar turma também para Pacote; migração dos pacotes hoje cadastrados. **Nenhuma matrícula é removida e nada é apagado.**
2. **Vínculo**: `VinculoCobrancaService` valida a turma de pacote (tipo, ativa, da professora, não iniciada), matricula o aluno na mesma gravação, aplica a nova regra de combinação (0 ou N pacotes de turma, um por turma; no máximo um de aulas individuais), mantém a turma do pacote imutável e restringe a reativação à turma em andamento.
3. **Turma**: `TurmaService` guarda tipo e datas, mantém o tipo imutável, recusa matrícula direta na turma de pacote (exceto a volta de quem tem pacote ativo dela) e, ao inativar a turma, inativa os pacotes dela.
4. **Fim da turma**: `EncerramentoTurmaPacoteService` + `TurmaEncerramentoDispatcherService` (rotina que roda ao iniciar e a cada `Turmas:IntervaloEncerramentoMinutos`) inativam os pacotes ativos de turmas encerradas, preservando o saldo.
5. **Cobrança**: `RegraCobrancaPresenca.Decidir` considera só os pacotes **aplicáveis** (do grupo da aula).
6. **Chamada**: seção "Outros alunos", agrupando só pelo indicador `reposicao` que a API já devolve.

Como o aluno continua matriculado, **a lista de participantes das aulas, o lembrete por e-mail, os filtros e os relatórios não mudam**. Excluir ou reativar o aluno **não muda** (só `aluno.ativo`), e o requisito de FR-020 vira teste e leitura de diff.

## Technical Context

**Language/Version**: C# / .NET 10; TypeScript / React 19 / Next.js 16

**Primary Dependencies**: nenhuma nova (EF Core, FluentValidation, xUnit com fakes manuais, `BackgroundService`)

**Storage**: MySQL; migração `database/25_turmas_de_pacote.sql` (a 24 é da spec 062)

**Testing**: xUnit para a regra (cobrança por grupo, vínculo, matrícula, tipo da turma, encerramento, aluno excluído que volta); migração conferida por contagens antes e depois; `tsc`, `eslint`, build e leitura do diff no frontend; quickstart contra a API local

**Project Type**: web application

**Constraints**: nenhuma regra de cobrança fora de `RegraCobrancaPresenca`; nenhuma segunda regra de conflito de horário; migração sem apagar nem alterar vínculo, matrícula ou aula; o encerramento só muda `vinculo_cobranca.ativo`; só tokens de tema (spec 057); e-mail em desenvolvimento só para o endereço de teste (spec 060, sem e-mail novo nesta spec)

**Scale/Scope**: dezenas de alunos e turmas

## Constitution Check

- **I. Exclusão lógica**: nada é apagado em nenhum fluxo (migração, encerramento, saída). Pacote inativado continua no banco com saldo. Excluir o aluno continua só desativando-o.
- **II. Regra de negócio no backend**: tipo da turma, fechamento (`fechada` calculada no servidor), escopo do pacote por grupo, combinação, encerramento e reativação ficam no backend. O frontend só agrupa pelo `reposicao` recebido e exibe `fechada`.
- **III. Nada configurável sem efeito**: tipo e datas da turma têm efeito real (restringem entrada, encerram pacotes); `Turmas:IntervaloEncerramentoMinutos` é lido de fato pela rotina.
- **IV. Segredos / perfil**: endpoints existentes, perfil Professor; nenhum segredo novo; a rotina não envia e-mail.
- **V. Documentação retroativa**: tarefa de notas nas specs 056, 041 e 052.

Sem violações. Pós-design: sem mudança.

## Project Structure

### Documentação (esta feature)

```text
specs/063-pacote-fora-da-turma/
├── spec.md
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── tasks.md
├── analysis.md
├── checklists/requirements.md
└── contracts/
    ├── turmas-api.md
    └── vinculos-e-aulas-api.md
```

### Código-fonte

```text
database/
└── 25_turmas_de_pacote.sql                           # NOVO

src/SPI.Domain/
├── Enums/TipoTurma.cs                                # NOVO (Mensalidade=1, Pacote=2)
├── Entities/Turma.cs                                 # + Tipo, DataInicio, DataFim
└── Repositories/IVinculoCobrancaRepository.cs        # + pacotes ativos de turmas encerradas / da turma

src/SPI.Infrastructure/
├── Persistence/Configurations/TurmaConfiguration.cs  # mapeia tipo e datas
├── Repositories/VinculoCobrancaRepository.cs         # as consultas acima
├── BackgroundServices/TurmaEncerramentoDispatcherService.cs   # NOVO (rotina)
└── DependencyInjection / Program (registro + opções Turmas)   # lê Turmas:IntervaloEncerramentoMinutos

src/SPI.Application/
├── Turmas/ (Dtos, Validators)                        # tipo/datas/fechada
├── Turmas/Services/TurmaService.cs                   # tipo imutável; matrícula direta; inativar encerra pacotes
├── Turmas/Services/EncerramentoTurmaPacoteService.cs # NOVO (+ interface)
├── VinculosCobranca/ (Validators, Services)          # turma de pacote; combinação; reativar; matricula junto
├── Aulas/Services/RegraCobrancaPresenca.cs           # pacote aplicável = mesmo grupo da aula
└── Dashboard/Services/DashboardService.cs            # contexto do pacote (turma ou "Atendimento individual")

src/SPI.Api/appsettings.json                          # Turmas:IntervaloEncerramentoMinutos = 60

tests/SPI.Application.Tests/
├── Aulas/RegraCobrancaPresencaTests.cs               # ajustado
├── Aulas/AulaServicePacoteGrupoTests.cs              # NOVO
├── Aulas/AulaServiceConflitoAlunoTests.cs            # NOVO
├── Alunos/AlunoServicePacoteIntactoTests.cs          # NOVO (excluir/reativar não mexe)
├── Turmas/TurmaServiceTipoTests.cs                   # NOVO
├── Turmas/EncerramentoTurmaPacoteServiceTests.cs     # NOVO
└── VinculosCobranca/VinculoCobrancaServiceTurmaPacoteTests.cs  # NOVO

frontend/
├── lib/api/ (turmas.ts, vinculosCobranca.ts)
├── app/(app)/turmas/ (TurmaForm.tsx, [id]/page.tsx)
├── app/(app)/aulas/[id]/page.tsx                     # seção "Outros alunos"
└── components/alunos/ (VinculoCobrancaFormModal.tsx, VinculosCobrancaSection.tsx)
```

**Structure Decision**: aplicação web existente; arquivos novos só de enum, migração, serviço de encerramento, rotina e testes. Nenhum componente novo de frontend. `AulaRepository`, `AulaService` (participantes), lembretes, relatórios e `AlunoService` **não mudam**.

## Complexity Tracking

| Ponto | Por quê | Alternativa mais simples descartada |
|---|---|---|
| Pacote apontando de novo para turma | O usuário definiu o pacote coletivo como fechado com uma turma | Manter o pacote sem turma: não haveria "turma de pacote" nem fim junto |
| Cadastro do pacote matricula o aluno | A turma de pacote só recebe aluno pelo pacote; evita matricular mensalidade ou avulso como membro | Permitir matrícula direta: abriria a turma de pacote para quem não tem pacote |
| Rotina em segundo plano (encerramento) | O usuário quer o pacote inativo quando a data de fim passa, sem depender de alguém abrir o sistema | Calcular "vigente" na hora: vários pontos já filtram por `ativo` e o usuário pediu que o pacote "torne inativo" |
| Sem índice único para "um pacote por grupo" | Dados antigos podem ter duplicados e o índice falharia; a spec 056 já adotou o mesmo critério | Índice gerado como o da Avulsa: exigiria resolver duplicados na migração |
