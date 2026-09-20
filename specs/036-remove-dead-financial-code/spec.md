# Feature Specification: Remover Código Morto do Módulo Financeiro (Dashboard e ObterValorAPagarAsync)

**Feature Branch**: `036-remove-dead-financial-code`

**Created**: 2026-09-20

**Status**: Draft

**Input**: User description: "Remover código morto identificado na faxina do módulo Financeiro: 1. DashboardService.ObterIndicadoresFinanceirosAsync e ObterFluxoCaixaMensalAsync: não são consumidos por nenhuma tela do frontend (confirmar isso de novo antes de remover, já que indicadores individuais já foram removidos nas specs 030/031/032 — só remover se, de fato, mais nenhum indicador desses dois métodos estiver em uso após essas remoções). 2. RelatorioRepository/IRelatorioRepository.ObterValorAPagarAsync: método nunca chamado em lugar nenhum do código. Para cada um: confirmar ausência total de uso (busca em todo o repositório), remover o método e qualquer código que só existia para alimentá-lo, e rodar a suíte de testes completa para confirmar que nada quebrou."

## Nota de investigação prévia

Confirmado por leitura do código atual (re-verificação feita nesta spec, como pedido, após as
remoções das specs 030/031/032):

- **Item 1 — `DashboardService`**: `frontend/app/(app)/dashboard/page.tsx` chama `obterDashboard()` e só lê dois campos do retorno: `dashboard?.alunosAtendidosNoPeriodo` e `dashboard?.valorFaturadoNoPeriodo` ([dashboard/page.tsx:53-54](../../frontend/app/(app)/dashboard/page.tsx#L53-L54)) — confirmado via busca em todo `frontend/**/*.ts*` que nenhum outro arquivo lê `dashboard.indicadores` ou `dashboard.fluxoCaixaMensal`, nem existe nenhum outro consumidor de `obterDashboard()`. Os campos `Indicadores` (populado por `ObterIndicadoresFinanceirosAsync`, [DashboardService.cs:62-94](../../src/SPI.Application/Dashboard/Services/DashboardService.cs#L62-L94)) e `FluxoCaixaMensal` (populado por `ObterFluxoCaixaMensalAsync`, [DashboardService.cs:96-119](../../src/SPI.Application/Dashboard/Services/DashboardService.cs#L96-L119)) são, portanto, código morto de ponta a ponta — confirmado que os indicadores individuais já removidos (specs 030/031/032) não deixaram nenhum indicador remanescente desses dois métodos em uso.
  - **Importante (não confundir com item morto)**: os tipos DTO reaproveitados por esses métodos (`IndicadoresFinanceirosResponse`, `FluxoCaixaMensalItem`, `GargaloCaixaResponse`, todos em `SPI.Application.Dashboard.Dtos`) **continuam em uso** — são os mesmos tipos reaproveitados por `RelatorioService.ObterIndicadoresFinanceirosAsync` ([RelatorioService.cs:196-291](../../src/SPI.Application/Relatorios/Services/RelatorioService.cs#L196-L291)) via `IndicadoresFinanceirosFiltradosResponse` ([IndicadoresFinanceirosFiltradosResponse.cs:11-12](../../src/SPI.Application/Relatorios/Dtos/IndicadoresFinanceirosFiltradosResponse.cs#L11-L12)), que alimenta a aba "Indicadores" do Relatório Financeiro — essa aba está ativa e não faz parte desta remoção. As classes DTO em si **não devem ser removidas**; apenas as duas propriedades de `DashboardResponse` que as referenciam (`Indicadores`, `FluxoCaixaMensal`) e os dois métodos privados de `DashboardService` que as populam.
  - Os repositórios chamados dentro desses dois métodos (`ObterInadimplenciaNoPeriodoAsync`, `ObterPagamentosComAtrasoNoPeriodoAsync`, `ObterValorPagoNoPeriodoAsync`, `ObterEntradasPorDiaDoMesAsync`, `ObterSaidasPorDiaDoMesAsync`) **continuam em uso** por `RelatorioService.ObterIndicadoresFinanceirosAsync` (confirmado por leitura direta) — nenhum deles deve ser removido do repositório.
- **Item 2 — `ObterValorAPagarAsync`**: confirmado por busca em todo `src/` e `tests/` que `IRelatorioRepository.ObterValorAPagarAsync` ([IRelatorioRepository.cs:47](../../src/SPI.Domain/Repositories/IRelatorioRepository.cs#L47)) e sua implementação em `RelatorioRepository.cs:96-107` não são chamados por nenhum serviço — as únicas outras ocorrências do nome são as implementações obrigatórias (stubs) do método em três fakes de teste manuais (`RelatorioServiceFinanceiroPorTurmaTests.cs`, `RelatorioServiceLancamentosTests.cs`, `RelatorioServiceFinanceiroPendenteTests.cs`), que existem apenas porque as classes fake implementam `IRelatorioRepository` por completo — nenhum teste de fato exercita este método.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Remover cálculo de indicadores/fluxo de caixa não usados no Dashboard (Priority: P1)

Como desenvolvedora mantendo o sistema, eu quero que `GET /api/dashboard` pare de calcular indicadores financeiros e fluxo de caixa mensal que nenhuma tela exibe, para que o carregamento da Home não gaste consultas ao banco com um resultado que é sempre descartado, e para que o código não sugira, incorretamente, que esses valores são usados em algum lugar.

**Why this priority**: É o item de maior impacto identificado na investigação — mais de 10 idas ao banco por carregamento do Dashboard para popular um resultado 100% descartado.

**Independent Test**: Chamar `GET /api/dashboard` antes e depois da mudança e confirmar que os campos `alunosAtendidosNoPeriodo`/`valorFaturadoNoPeriodo` (e os demais campos que a tela usa) continuam idênticos, e que os campos `indicadores`/`fluxoCaixaMensal` não aparecem mais na resposta. Confirmar visualmente que a tela Home continua funcionando normalmente.

**Acceptance Scenarios**:

1. **Given** o código atual de `DashboardService`, **When** os métodos `ObterIndicadoresFinanceirosAsync` e `ObterFluxoCaixaMensalAsync` e as propriedades `Indicadores`/`FluxoCaixaMensal` de `DashboardResponse` são removidos, **Then** `GET /api/dashboard` continua retornando corretamente todos os demais campos (incluindo `alunosAtendidosNoPeriodo` e `valorFaturadoNoPeriodo`, que a Home usa).
2. **Given** a tela Home (`/dashboard`) após a remoção, **When** a professora a acessa, **Then** os cards "Alunos atendidos" e "Recebido este mês" continuam exibindo os valores corretos, sem nenhum erro no console ou na tela.
3. **Given** a aba "Indicadores" do Relatório Financeiro após a remoção, **When** a professora a acessa, **Then** "Inadimplência", "Prazo médio de atraso" e "Fluxo de caixa (últimos 6 meses)" continuam funcionando normalmente — essa tela usa uma implementação separada (`RelatorioService`), não afetada por esta remoção.

---

### User Story 2 - Remover método de repositório nunca chamado (Priority: P2)

Como desenvolvedora mantendo o sistema, eu quero que `ObterValorAPagarAsync` seja removido da interface e da implementação do repositório, para que o código não sugira uma capacidade que nenhum serviço usa.

**Why this priority**: Menor impacto que o item 1 (não gera nenhuma consulta desnecessária em produção, já que nunca é chamado), mas ainda assim código morto que aumenta a superfície de manutenção.

**Independent Test**: Confirmar, após a remoção, que o projeto compila e que a suíte de testes completa passa sem nenhuma referência remanescente a `ObterValorAPagarAsync`.

**Acceptance Scenarios**:

1. **Given** o código atual, **When** `ObterValorAPagarAsync` é removido de `IRelatorioRepository` e de `RelatorioRepository`, **Then** o projeto compila sem erros.
2. **Given** os três fakes de teste que hoje implementam um stub de `ObterValorAPagarAsync` só para satisfazer a interface, **When** o método é removido da interface, **Then** esses stubs também são removidos dos fakes (não são mais exigidos pela interface e não servem a nenhum propósito).

---

### Edge Cases

- Existe algum teste que hoje exercita `DashboardService.ObterIndicadoresFinanceirosAsync`/`ObterFluxoCaixaMensalAsync` diretamente (não apenas via `ObterAsync`)? Não encontrado — nenhum teste em `tests/` referencia esses dois métodos (são privados, só acessíveis via `ObterAsync`, que não tem teste dedicado hoje).
- A remoção dos campos `Indicadores`/`FluxoCaixaMensal` de `DashboardResponse` é uma mudança de contrato de API. Isso quebra algum consumidor? Não — confirmado que nenhuma tela lê esses campos hoje; é uma mudança de contrato segura por ausência total de uso.
- Os DTOs `IndicadoresFinanceirosResponse`/`FluxoCaixaMensalItem`/`GargaloCaixaResponse` devem ser removidos junto? Não — continuam em uso por `RelatorioService`/`IndicadoresFinanceirosFiltradosResponse` (aba Indicadores do Relatório Financeiro, tela ativa).
- Algum repositório chamado exclusivamente pelos dois métodos removidos fica órfão? Não — todos os métodos de `IRelatorioRepository` chamados por eles (`ObterInadimplenciaNoPeriodoAsync`, `ObterPagamentosComAtrasoNoPeriodoAsync`, `ObterValorPagoNoPeriodoAsync`, `ObterEntradasPorDiaDoMesAsync`, `ObterSaidasPorDiaDoMesAsync`) continuam em uso por `RelatorioService.ObterIndicadoresFinanceirosAsync`.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O sistema MUST remover os métodos privados `DashboardService.ObterIndicadoresFinanceirosAsync` e `DashboardService.ObterFluxoCaixaMensalAsync`, e as chamadas a eles dentro de `ObterAsync`.
- **FR-002**: O sistema MUST remover as propriedades `Indicadores` e `FluxoCaixaMensal` de `DashboardResponse`, e os campos correspondentes (`indicadores`, `fluxoCaixaMensal`) da interface `Dashboard` no frontend (`frontend/lib/api/dashboard.ts`).
- **FR-003**: O sistema MUST NOT remover os tipos `IndicadoresFinanceirosResponse`, `FluxoCaixaMensalItem`, `GargaloCaixaResponse` (backend) nem `IndicadoresFinanceiros`, `FluxoCaixaMensalItem`, `GargaloCaixa` (frontend), nem nenhum método de `IRelatorioRepository` além de `ObterValorAPagarAsync` — todos continuam em uso por `RelatorioService`/aba "Indicadores" do Relatório Financeiro.
- **FR-004**: O sistema MUST NOT alterar nenhum outro campo de `GET /api/dashboard` (`totalAulasAgendadasNoPeriodo`, `totalAlunosAtivos`, `alunosAtendidosNoPeriodo`, `totalTurmasAtivas`, `valorPendenteRecebimento`, `valorFaturadoNoPeriodo`, `proximasAulasHoje`, `lembretesPendentes`) nem seu comportamento — remoção estritamente restrita aos dois campos mortos.
- **FR-005**: O sistema MUST remover `ObterValorAPagarAsync` de `IRelatorioRepository` e de sua implementação em `RelatorioRepository`, junto com os stubs correspondentes nos fakes de teste que hoje o implementam apenas para satisfazer a interface.
- **FR-006**: Após ambas as remoções, a suíte de testes completa do backend MUST passar sem falhas, confirmando ausência de regressão.

### Key Entities

- Não introduz entidades novas — remove exclusivamente código de apresentação/agregação (métodos de serviço, propriedades de DTO, um método de repositório) já confirmado sem nenhum consumidor.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: `GET /api/dashboard` não retorna mais os campos `indicadores`/`fluxoCaixaMensal`, e todos os demais campos permanecem com o mesmo valor de antes da remoção, para a mesma consulta.
- **SC-002**: A tela Home carrega normalmente após a remoção, sem nenhum campo faltando dos que ela de fato usa.
- **SC-003**: A aba "Indicadores" do Relatório Financeiro continua funcionando de forma idêntica após a remoção (não é afetada, por usar uma implementação separada).
- **SC-004**: 100% dos testes da suíte backend passam após ambas as remoções.
- **SC-005**: Nenhuma referência a `ObterIndicadoresFinanceirosAsync`/`ObterFluxoCaixaMensalAsync` (em `DashboardService`) ou a `ObterValorAPagarAsync` (em qualquer arquivo) permanece no código-fonte após a mudança.

## Assumptions

- O carregamento do Dashboard passa a fazer menos consultas ao banco (as ~10 consultas usadas só para popular `Indicadores`/`FluxoCaixaMensal` deixam de acontecer) — uma melhoria de desempenho incidental, não o objetivo principal desta feature.
- Nenhuma migração de banco de dados é necessária — remoção de código de aplicação/apresentação apenas.
- Esta feature não introduz nenhuma spec retroativa nova nem atualiza specs retroativas existentes, pois nenhuma delas documenta os campos `Indicadores`/`FluxoCaixaMensal` de `DashboardResponse` como comportamento aceito (diferente do caso de "Margem de Segurança"/"Cobertura de Custos", que eram indicadores nomeados e documentados; aqui é a ausência total de consumo de dois métodos internos).
