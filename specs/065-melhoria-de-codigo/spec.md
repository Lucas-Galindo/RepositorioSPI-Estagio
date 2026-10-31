# Feature Specification: Melhoria do Código sem Mudar o Comportamento (redundâncias, código morto e comentários)

**Feature Branch**: `065-melhoria-de-codigo`

**Created**: 2026-10-31

**Status**: Draft

**Input**: Pedido do usuário em 2026-10-31: investigar o código e melhorá-lo, **tirando redundâncias, simplificando a lógica, comentando as partes complexas e as regras de negócio e removendo código morto**. A investigação foi feita antes desta spec (somente leitura) e os números abaixo vêm dela. *(O usuário chamou esta spec de "063"; esse número já é a spec do pacote por turma e o 064 é a chamada nos detalhes, então ela é a **065**.)*

## Contexto (o que a investigação encontrou)

Tamanho do código de produção e de testes: 252 arquivos em `src`, 62 em `tests` e 106 em `frontend`. Os analisadores do .NET (usings, membros privados e parâmetros sem uso) **não acusam nada**, e uma busca por tipos, métodos, propriedades e constantes referenciados uma única vez só achou ganchos do framework (ações de controller e configurações do EF). Ou seja, o código morto do backend é **pequeno**; a maior fonte de peso é a **repetição**.

| Achado | Número medido | Onde |
|---|---|---|
| **Tratamento de erro repetido nas ações dos controllers** | 87 blocos `catch (Exception)`, 53 `catch NaoEncontrado → NotFound(e.Message)`, 24 `catch Conflito → Conflict(e.Message)`, em 17 controllers | `src/SPI.Api/Controllers` |
| Blocos de código copiados entre telas | contas a pagar × contas a receber (detalhes e edição: 19 e 14 trechos iguais); relatórios de alunos × turmas (10) | `frontend/app/(app)/financeiro` e `relatorios` |
| Cópia entre validadores e DTOs | cadastro × atualização de aluno | `Alunos/Dtos` e `Alunos/Validators` |
| Mesma decisão escrita duas vezes | "turma de pacote ou turma comum" no cadastro e na edição do vínculo; leitura de `DateTime.Now` espalhada em 22 pontos | `VinculoCobrancaService`, serviços em geral |
| **Código morto confirmado** | 18 classes CSS definidas e nunca usadas; `decodeJwtPayload` e `amostraMaisProxima` exportados sem uso fora do arquivo (`misturar` e `CONTRASTE_MINIMO` parecem sem uso, mas o script `verificar-contraste.mjs` os importa: ficam); comentário de teste que aponta para um método que não existe mais (`MontarPacotesEmAtencao`); aviso do compilador de pacote que pode ser removido (`PackageReference` sem necessidade) | `frontend/styles`, `frontend/lib`, testes, `SPI.Application.csproj` |
| **Regras complexas com pouco ou nenhum comentário** | 10 métodos com mais de 35 linhas e no máximo 2 linhas de comentário, entre eles `PagamentoService.RegistrarAsync` (52 linhas, 0), `AlunoService.CadastrarAsync` (51, 0), `RelatorioService.ObterIndicadoresFinanceirosAsync` (96, 3), `LembreteDispatcherService.ProcessarLembretesVencidosAsync` (77, 3), `FinanceiroService.ObterVisaoGeralAsync` (65, 2) | `Services`, `Repositories`, `BackgroundServices` |

Observações que **não** fazem parte do código morto e ficam registradas:
- O painel de pacotes da Home ainda devolve `Concluidos` e `AulasDadasNoPeriodo`, mesmo sem exibi-los: ficam para o card "Estudar implementação de dashboard de aulas".
- Existe um aviso de segurança de pacote do NuGet (`System.Security.Cryptography.Pkcs` 10.0.0); atualizar dependência muda o produto e vira card próprio.

## Decisões de negócio (respondidas pelo usuário)

| Tema | Decisão |
|---|---|
| Objetivo | Melhorar o código: **tirar redundâncias, simplificar a lógica, comentar as partes complexas e as regras de negócio, e remover código morto**. |
| O que não pode mudar | O sistema continua **se comportando exatamente igual** para a professora e para a API. |
| Banco de dados | Esta spec **não** altera o banco. |

## Decisões menores adotadas (a confirmar na aprovação desta spec)

1. **"Igual" quer dizer igual na prática**: mesmos códigos de status, mesmos corpos de resposta, mesmas mensagens, mesmas regras de cobrança e frequência, mesmas telas. Isso é conferido por testes, por um **instantâneo das respostas da API antes e depois** e por **capturas de tela antes e depois**.
2. **O tratamento de erro dos controllers passa a ser feito em um lugar só** (um filtro), com as **mesmas três respostas de hoje**: não encontrado → 404 com a mensagem; conflito → 409 com a mensagem; qualquer outro erro → 500 "Erro inesperado" com o detalhe. Os casos especiais (login, refresh, logout e o `BadRequest` do cadastro inicial) continuam no controller, porque têm resposta e registro próprios.
3. **Zona da spec 064**: o que a 064 vai remover ou reescrever **não é mexido aqui** — o ramo retroativo de `AulaService.CadastrarAsync`, o campo `AulaRequest.Presencas`, `PresencaChecklist`, a configuração `Notificacoes:AulasPendentesDesde` e a leitura do relógio (`DateTime.Now`). Refatorar isso agora seria trabalho jogado fora.
4. **Comentário explica o porquê** (regra de negócio, decisão, risco), em português sem acento como o resto do código, e **nunca** conta o que a linha já diz.
5. **Duplicação de telas** (contas a pagar × a receber, relatórios) é prioridade menor (P3): só se faz o que puder ser provado idêntico nas capturas de tela; o que não der entra em card próprio.
6. **A ordem recomendada é fazer esta spec antes de implementar a 064**, para a 064 nascer em cima de código mais limpo.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Remover código morto confirmado (Priority: P1)

O código que ninguém usa é removido, depois de confirmado que não há uso nenhum, nem dinâmico.

**Why this priority**: É a mudança de menor risco e a que mais clareza dá por esforço.

**Independent Test**: Para cada item da lista, mostrar que não há referência (busca por nome, inclusive em textos de classe montados dinamicamente) e rodar build, testes e `tsc`; nada muda para o usuário.

**Acceptance Scenarios**:

1. **Given** as 18 classes CSS sem uso, **When** cada uma é removida, **Then** nenhuma tela muda (capturas de antes e depois idênticas) e nenhuma delas aparece montada por texto dinâmico.
2. **Given** `decodeJwtPayload` e `amostraMaisProxima` exportados sem uso externo, **When** deixam de ser exportados (ou são removidos, se não houver uso nenhum), **Then** o frontend compila e o login e a paleta funcionam igual.
3. **Given** o comentário de teste que aponta para `MontarPacotesEmAtencao`, **When** ele é corrigido, **Then** aponta para o que existe hoje.
4. **Given** o `PackageReference` desnecessário, **When** ele é removido, **Then** o aviso do compilador some e o build e os testes continuam passando.

---

### User Story 2 - Centralizar o tratamento de erro dos controllers (Priority: P1)

As ações dos controllers deixam de repetir `try/catch` para "não encontrado", "conflito" e "erro inesperado". O tratamento passa a existir em um lugar, com as mesmas respostas.

**Why this priority**: É a maior redundância do código (mais de 160 blocos) e a que mais polui a leitura de cada ação.

**Independent Test**: Capturar, antes da mudança, as respostas (status e corpo) de um conjunto de chamadas, entre elas as de sucesso, 400, 404 e 409; repetir depois e comparar: devem ser idênticas.

**Acceptance Scenarios**:

1. **Given** uma ação cujo serviço lança "não encontrado", **When** a chamada é feita, **Then** a resposta é 404 com o mesmo texto de hoje.
2. **Given** uma ação cujo serviço lança "conflito", **When** a chamada é feita, **Then** a resposta é 409 com o mesmo texto de hoje.
3. **Given** um erro inesperado, **When** ele acontece, **Then** a resposta é 500 com o título "Erro inesperado" e o detalhe da mensagem, como hoje.
4. **Given** login, refresh e logout, **When** as credenciais ou o token são inválidos, **Then** continuam respondendo 401 com o mesmo registro de aviso de hoje.
5. **Given** o cadastro inicial da professora, **When** há conflito, **Then** continua respondendo 400 como hoje.

---

### User Story 3 - Simplificar lógica repetida nos serviços (Priority: P2)

Decisões escritas mais de uma vez passam a existir uma só vez, com um nome que explica a regra.

**Why this priority**: Reduz o risco de duas cópias da mesma regra divergirem.

**Independent Test**: Os testes existentes do serviço continuam passando sem alterar expectativas, e as chamadas de API conferidas antes e depois dão o mesmo resultado.

**Acceptance Scenarios**:

1. **Given** o cadastro e a edição do vínculo de cobrança com a mesma decisão sobre turma, **When** a decisão passa a ser um método só, **Then** as recusas e mensagens são as mesmas.
2. **Given** o cadastro e a atualização de aluno com validações e campos repetidos, **When** a parte comum passa a existir uma vez, **Then** as mensagens de validação são as mesmas.
3. **Given** qualquer simplificação, **When** ela é revisada, **Then** o diff mostra menos linhas e nenhuma regra nova.

---

### User Story 4 - Comentar as regras complexas e de negócio (Priority: P2)

Os métodos longos e as regras de negócio sem explicação ganham comentários que dizem o porquê.

**Why this priority**: Quem mexer depois (inclusive nas specs 062 e 064) precisa entender a regra sem reler a conversa.

**Independent Test**: Para cada método da lista, o diff do commit tem **somente linhas de comentário**, e o comentário responde "por que é assim?".

**Acceptance Scenarios**:

1. **Given** os 10 métodos longos com pouco comentário, **When** são comentados, **Then** cada um passa a explicar a regra de negócio, a ordem das etapas ou o risco que ela evita.
2. **Given** qualquer comentário novo, **When** ele é lido, **Then** não repete o que o código já diz e não cita conversa nem data de pedido.
3. **Given** a mudança de comentários, **When** o build roda, **Then** o resultado compilado é o mesmo.

---

### User Story 5 - Reduzir telas copiadas (Priority: P3)

Os trechos copiados entre telas parecidas viram componentes ou funções compartilhados, só quando as telas ficam visualmente idênticas.

**Why this priority**: Tem ganho, mas também o maior risco visual; por isso fica por último e é opcional.

**Independent Test**: Capturas de tela de antes e depois das telas tocadas são idênticas, e tsc, eslint e build passam.

**Acceptance Scenarios**:

1. **Given** as páginas de detalhes e de edição de contas a pagar e a receber, **When** a parte comum é compartilhada, **Then** as capturas de antes e depois são idênticas.
2. **Given** uma tela cuja extração não puder ser provada idêntica, **When** isso for constatado, **Then** ela fica como está e vira card próprio.

---

### Edge Cases

- **Classe CSS montada por texto** (por exemplo `status-${x}`): só se remove uma classe quando nenhum texto dinâmico a gera.
- **Exportação usada só por teste ou só no próprio arquivo**: deixa de ser exportada em vez de ser apagada, se a função ainda é usada ali.
- **Exceção tratada no controller com registro próprio** (login, refresh, logout): não vai para o filtro.
- **Ação que devolve 400 de validação antes do serviço**: continua no controller; o filtro só trata o que o serviço lança.
- **Diferença de ordem de resposta**: o filtro não pode mudar cabeçalhos nem o formato do corpo (texto puro no 404 e no 409, `ProblemDetails` no 500).
- **Código tocado pela spec 064**: fica de fora (decisão menor 3).
- **Teste que depende do texto de um comentário**: não existe; se existir, é só comentário.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O sistema MUST NOT mudar de comportamento observável: códigos de status, corpos de resposta e mensagens da API, regras de negócio (cobrança, frequência, vínculos, turmas, notificações) e telas MUST permanecer iguais. O esquema do banco MUST NOT mudar.
- **FR-002**: O código morto da lista de "Contexto" MUST ser removido, cada item só depois de confirmado que não há uso, inclusive dinâmico.
- **FR-003**: Uma classe CSS MUST ser removida somente se nenhum arquivo do frontend a usar, nem montando o nome por texto.
- **FR-004**: O tratamento de "não encontrado", "conflito" e "erro inesperado" dos controllers MUST ser feito em um único lugar, com as mesmas respostas de hoje (404 e 409 com a mensagem em texto; 500 com `ProblemDetails`, título "Erro inesperado" e detalhe da mensagem). Os controllers MUST NOT repetir esses blocos.
- **FR-005**: As exceções específicas com resposta ou registro próprio (login, refresh, logout e o `BadRequest` do cadastro inicial) MUST continuar tratadas no controller, com a mesma resposta e o mesmo registro.
- **FR-006**: Decisões repetidas nos serviços listados em "User Story 3" MUST passar a existir uma só vez, sem mudar mensagens nem regras.
- **FR-007**: Os métodos listados em "Contexto" (mais de 35 linhas e no máximo 2 de comentário) e as regras de negócio de cobrança, frequência, vínculos e notificações MUST receber comentário que explique o **porquê**.
- **FR-008**: Mudanças só de comentário MUST NOT alterar nenhuma linha de código (verificável pelo diff).
- **FR-009**: Comentários MUST NOT repetir o que o código diz, citar a conversa ou datas de pedido, nem usar acentos (padrão do código).
- **FR-010**: O que a spec 064 vai remover ou reescrever (ramo retroativo de `CadastrarAsync`, `AulaRequest.Presencas`, `PresencaChecklist`, `Notificacoes:AulasPendentesDesde`, leitura do relógio) MUST NOT ser alterado por esta spec.
- **FR-011**: Antes de qualquer refatoração, MUST existir um **instantâneo das respostas da API** (status e corpo) de um conjunto de chamadas representativo, e depois de cada etapa ele MUST ser repetido e comparado.
- **FR-012**: As telas tocadas MUST ter **capturas de antes e depois idênticas**.
- **FR-013**: A suíte de testes existente MUST continuar passando **sem alterar nenhuma expectativa**; testes novos só para o filtro de erros.
- **FR-014**: Nenhuma duplicação de tela MUST ser extraída se as capturas de antes e depois não forem idênticas (essas viram card próprio).
- **FR-015**: O relatório da spec MUST registrar os números de antes e depois (blocos `try/catch` nos controllers, linhas removidas, classes CSS removidas, métodos comentados).
- **FR-016**: Nenhuma regra de negócio muda, então nenhuma spec anterior recebe nota (Princípio V). O relatório MUST registrar onde o tratamento de erro da API passou a morar.

### Key Entities

Não há entidade nova nem alteração de dados. O único elemento novo é o **filtro de tratamento de erros** da API.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% dos testes existentes continuam passando sem alterar expectativas (hoje 464).
- **SC-002**: 100% das chamadas do instantâneo da API (status e corpo) são idênticas antes e depois.
- **SC-003**: Os blocos `catch` repetidos de "não encontrado", "conflito" e "erro inesperado" nos controllers caem de mais de 160 para 0 (os especiais ficam), e o total de linhas dos controllers cai em pelo menos 1.000.
- **SC-004**: 0 classes CSS da lista permanecem; 0 referências restantes a nomes removidos; build, `tsc` e `eslint` sem novos avisos.
- **SC-005**: 100% dos métodos da lista de "Contexto" ficam com comentário de regra, com diff contendo só comentários.
- **SC-006**: As capturas de antes e depois das telas tocadas são 100% idênticas.

## Assumptions

- Esta spec não depende da 062, da 063 nem da 064, mas a 064 deve ser implementada **depois** dela.
- O instantâneo e as capturas são feitos com o login de teste e dados sintéticos "TESTE-065", apagados no fim, no banco local.
- O trabalho é feito sem commit até o usuário pedir.

## Fora de escopo

- Qualquer mudança de comportamento, de regra de negócio ou de banco.
- Atualizar dependências (o aviso de segurança do NuGet vira card próprio).
- O que a spec 064 vai remover ou reescrever.
- Remover `Concluidos` e `AulasDadasNoPeriodo` da resposta da Home (card próprio).
- Layout responsivo para celular.
