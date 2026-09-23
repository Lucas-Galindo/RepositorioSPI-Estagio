# Research: Job de Cobrança Automática de Mensalidade

Todas as decisões abaixo foram tomadas lendo o código atual; nenhum `NEEDS CLARIFICATION` restou após `/speckit-clarify`.

## R1. Mecanismo de idempotência (FR-004)

- **Decision**: Adicionar `vinculo_cobranca_id INT NULL` (FK para `vinculo_cobranca(id)`) à tabela `pagamento`, com `UNIQUE INDEX uq_pagamento_vinculo_competencia (vinculo_cobranca_id, competencia)`. A checagem de idempotência é `IPagamentoRepository.ExisteMensalidadeGeradaAsync(vinculoCobrancaId, competencia, ct)` (`_dbContext.Pagamentos.AnyAsync(p => p.VinculoCobrancaId == vinculoCobrancaId && p.Competencia == competencia)`), verificada **antes** de criar o `Pagamento`; o índice único é a rede de segurança contra corrida (duas execuções do job em paralelo, ou reinício no meio do processamento), no mesmo espírito do índice único de `vinculo_cobranca` (specs/037, `uq_vinculocobranca_chave_ativa`).
- **Rationale**: `Pagamento` hoje não tem nenhuma referência a `VinculoCobranca` — as contas geradas por presença (specs/038) se relacionam com a `Aula`, não com o vínculo. Sem uma FK explícita, a única alternativa seria "adivinhar" que Pagamento corresponde a qual vínculo por heurística (ex.: `AlunoId` + `Competencia` + texto da `Descricao`) — frágil e ambíguo justamente no caso que a spec pede suporte explícito (Acceptance Scenario 3 da US1: um aluno com **dois** vínculos Mensalidade ativos, em turmas diferentes, no mesmo mês — sem a FK, não haveria como saber que Pagamento pertence a qual vínculo).
- **Alternatives considered**: (a) checar só por `AlunoId + Competencia + CategoriaReceitaId` — descartado, quebra o cenário de dois vínculos Mensalidade simultâneos do mesmo aluno; (b) checar por texto da `Descricao` (nome da turma embutido) — descartado, string matching é frágil e não sustenta um índice único de verdade no banco; (c) só verificação na aplicação, sem índice único — descartado, mesma razão de specs/037 (SC-002 exige "0 duplicadas", e o índice fecha a janela de corrida que a aplicação sozinha não fecha).

## R2. Condição de disparo (FR-001) e sua extração testável

- **Decision**: `MensalidadeDispatcherService` expõe um método `internal static bool DeveDispararNesteMomento(DateTime agora) => agora.Day == DateTime.DaysInMonth(agora.Year, agora.Month) && agora.Hour >= 23;`, chamado a cada tick do `PeriodicTimer` com `DateTime.Now` (horário local, mesmo padrão de `LembreteDispatcherService.ProcessarLembretesVencidosAsync` que já usa `DateTime.Now`). Só quando `true`, o método que consulta o banco (`ProcessarCobrancasMensaisAsync`) é chamado.
- **Rationale**: `DateTime.DaysInMonth` já resolve corretamente meses de 28/29/30/31 dias sem cálculo manual. Extrair a condição para um método estático puro permite testá-la exaustivamente (todo dia do mês × hora do dia) sem precisar mockar `DateTime.Now` nem instanciar o `BackgroundService` inteiro — o mesmo problema de testabilidade que `LembreteDispatcherService` nunca precisou resolver (ele reage a "já venceu", não a "é exatamente este instante").
- **Alternatives considered**: introduzir uma abstração `IClock`/`TimeProvider` (disponível desde .NET 8) para injetar o relógio — descartado por ora: nenhum outro serviço do projeto usa essa abstração (incluindo o próprio `LembreteDispatcherService`, que usa `DateTime.Now` direto); introduzi-la aqui criaria um padrão divergente sem necessidade, já que o método estático puro resolve a testabilidade sem exigir DI adicional.

## R3. Intervalo de verificação padrão (FR-008)

- **Decision**: `Mensalidade:IntervaloVerificacaoSegundos`, default **3600** (1 hora) quando ausente do `appsettings.json` — mesmo padrão de leitura de `Lembretes:IntervaloVerificacaoSegundos` (`configuration.GetValue<int?>(...) ?? default`).
- **Rationale**: 3600 segundos divide exatamente as 24 horas do dia (24 ticks/dia). Isso garante uma propriedade determinística: **independentemente do instante em que o serviço foi iniciado**, have um tick por hora-do-dia todo santo dia (ex.: se o serviço subiu às 08:15, os ticks caem em 08:15, 09:15, ..., 23:15, 00:15, ...) — logo, exatamente um tick cai dentro da janela 23:00–23:59 a cada dia, sem depender de alinhamento com o relógio de parede. Um intervalo de "1 dia" (86400s), a interpretação ingênua de "roda uma vez por dia" do pedido original, **não** tem essa propriedade: se o serviço iniciar às 03:00, todos os ticks futuros caem às 03:00, e a janela 23h nunca é alcançada — o job nunca dispararia. Um intervalo menor (ex.: 60s, igual a Lembretes) também funcionaria, mas é desnecessariamente frequente para uma verificação cujo custo fora da janela é uma comparação de `DateTime` em memória — 3600s é o maior valor que ainda garante a propriedade acima com uma margem confortável (a janela dura 60 minutos).
- **Alternatives considered**: 86400s (1 dia) — descartado pelo risco real de nunca disparar, explicado acima; 60s (igual a Lembretes) — funciona, mas overhead de scheduling desnecessário para este caso; um valor não-divisor de 24h (ex.: 5000s) — descartado, quebra a garantia matemática de "um tick por hora-do-dia todo dia".

## R4. Data de vencimento (FR-007)

- **Decision**: `DataVencimento = new DateOnly(competencia.Year, competencia.Month, 1).AddMonths(1)` — sempre o 1º dia do mês seguinte ao mês de competência.
- **Rationale**: Regra simples, previsível e sem necessidade de campo configurável adicional (a spec já resolveu isso como requisito, FR-007) — corresponde ao costume comum de mensalidade vencer no início do mês seguinte ao período cursado. Reaproveita `DateOnly`, já usado em todo o domínio financeiro (`Pagamento.DataVencimento`, `Competencia`).
- **Alternatives considered**: vencimento no mesmo dia do disparo (ex.: último dia do mês corrente) — descartado, daria à professora e ao aluno zero dias de margem entre a geração e o vencimento; dia configurável por vínculo — fora de escopo (spec já resolveu com uma regra fixa).

## R5. Descrição da cobrança e contexto (FR-006)

- **Decision**: `Descricao = $"Mensalidade - {contexto} - {competencia:MM/yyyy}"`, onde `contexto` é `vinculo.Turma.Nome` quando `TurmaId` não é nulo, ou `"Atendimento individual"` quando é nulo — mesmo termo canônico já usado em specs/037/038 (`VinculoCobrancaFormModal.tsx`, `VinculosCobrancaSection.tsx`) para "sem turma", evitando introduzir um segundo termo ("particular") para o mesmo conceito.
- **Rationale**: Segue o padrão de `AulaService.GerarContasAReceberAsync`, que já monta `Descricao` combinando um identificador de contexto com uma data (`$"{Materia.Nome} - {DataInicio:dd/MM/yyyy}"`); aqui adaptado para o contexto mensal (mês/ano de competência, não uma data de aula específica). Usar o termo "Atendimento individual" (em vez de "particular", que a spec original citava como alternativa) mantém consistência terminológica com o resto do sistema.
- **Alternatives considered**: usar "Atendimento particular" (mesmo texto de `CategoriaReceita.Nome = "Aula particular"`) — descartado por introduzir um segundo termo para o mesmo conceito de "sem turma", já unificado como "Atendimento individual" desde specs/037.

## R6. Categoria de receita "Mensalidade" (Clarifications, FR-006/FR-012)

- **Decision**: Novo script `database/15_job_cobranca_mensalidade.sql` insere `('Mensalidade', TRUE)` em `categoria_receita`, mesmo padrão de `10_financeiro_contas.sql`. `ICategoriaReceitaRepository.ObterPorNomeAsync("Mensalidade", ct)` (método já existente, usado por `AulaService`) resolve o `Id` no momento da geração.
- **Rationale**: Decisão já resolvida em `/speckit-clarify` (2026-09-22, Option A). Reaproveita a interface de repositório existente sem nenhuma mudança de assinatura.
- **Alternatives considered**: já avaliadas na clarificação (reaproveitar "Aula em turma"/"Aula particular" — rejeitado pelo usuário).

## R7. Onde a lógica de teste vive (Nota do plan.md Technical Context)

- **Decision**: `MensalidadeDispatcherService.DeveDispararNesteMomento(DateTime)` é `internal static`, e `tests/SPI.Application.Tests` ganha `InternalsVisibleTo` de `SPI.Infrastructure` — **a verificar em `/speckit-tasks`** se esse atributo já existe no projeto (não confirmado nesta pesquisa) ou se o método deve ser `public static` em vez de `internal static` para simplificar (sem exigir `InternalsVisibleTo`). Dado que não há precedente de `InternalsVisibleTo` no projeto (grep não encontrou nenhuma ocorrência), a rota mais simples e menos surpreendente é `public static` — decisão final registrada aqui como `public static` para evitar introduzir um mecanismo novo.
- **Rationale**: Testabilidade sem introduzir infraestrutura de teste nova (mock de `IHostedService`, biblioteca de teste de tempo) nem revelar acidentalmente APIs internas via `InternalsVisibleTo` que o projeto nunca usou.
- **Alternatives considered**: testar via `ExecuteAsync` completo com um `FakeTimeProvider` — descartado, exigiria introduzir a abstração `TimeProvider` rejeitada em R2.

## R8. Escopo confirmado fora desta feature

- Qualquer geração de cobrança para Modalidade Pacote ou Avulsa (FR-009 — já coberto por specs/038, por presença).
- Qualquer tratamento de Pacote esgotado (FR-010 — já fora de escopo desde specs/038).
- Proporcionalização (pró-rata) de mensalidade para vínculos cadastrados no meio do mês (Assumptions do spec.md).
- Qualquer mecanismo de recuperação retroativa ("catch-up") se o disparo inteiro for perdido (Assumptions do spec.md).
- Qualquer indicador visual novo de "mensalidade já cobrada este mês" — só o ajuste textual do aviso existente (FR-013) está em escopo, nada além disso na UI.
