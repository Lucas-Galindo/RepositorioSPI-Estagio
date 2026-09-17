# Feature Specification: Padronizar Formato Brasileiro de Data e Hora

**Feature Branch**: `025-padronizar-formato-data-hora`

**Created**: 2026-09-17

**Status**: Draft

**Input**: User description: "Padronizar todo o sistema para formato brasileiro: datas como dd/mm/aaaa (não mm/dd/aaaa) e horários em formato 24h (ex: 16:00, não 4:00 PM), em todas as telas, tabelas, relatórios e formulários do frontend. Verificar se isso é configuração de locale (ex: Intl.DateTimeFormat com locale pt-BR) que pode ser centralizada em um único lugar, em vez de corrigido tela por tela."

## Clarifications

### Session 2026-09-17

- Q: Os campos de formulário que hoje usam o seletor nativo de data/hora do navegador devem ser substituídos por um componente próprio para forçar dd/mm/aaaa e 24h, ou podem continuar usando o seletor nativo do navegador mesmo que seu formato interno dependa do idioma/SO da usuária? → A: Substituir por um componente próprio, garantindo dd/mm/aaaa e 24h no código, independentemente do idioma/SO do navegador da usuária.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Ler qualquer data no formato brasileiro (Priority: P1)

Como usuária do sistema (professora ou administradora) navegando por qualquer tela, tabela, relatório ou formulário, eu quero ver todas as datas no formato dd/mm/aaaa, para que eu não precise interpretar se "03/05" significa 3 de maio ou 5 de março — evitando erros de agendamento, cobrança ou registro causados por ambiguidade de formato.

**Why this priority**: É o pedido central da mudança. Uma data em formato ambíguo (ou claramente americano) pode levar a decisões erradas (marcar aula no dia errado, cobrar no dia errado) — o maior risco prático do sistema hoje.

**Independent Test**: Percorrer cada tela do sistema que exibe datas (Agenda, Turmas, Alunos, Financeiro, Relatórios, Dashboard, formulários de cadastro/edição) e confirmar visualmente que toda data exibida segue o padrão dd/mm/aaaa, sem nenhuma ocorrência de mm/dd/aaaa.

**Acceptance Scenarios**:

1. **Given** a usuária está em qualquer tela que exibe uma data (cartão, tabela, relatório ou campo de formulário já preenchido), **When** a tela carrega, **Then** a data aparece no formato dd/mm/aaaa (ex: "17/09/2026"), nunca mm/dd/aaaa.
2. **Given** a usuária está preenchendo um formulário com um campo de data, **When** ela abre o seletor de data ou digita o valor, **Then** o campo exibe e aceita o valor no formato dd/mm/aaaa, independentemente do idioma/sistema operacional do navegador dela.
3. **Given** um relatório (Financeiro, Agenda, Histórico do Aluno, Pendências, etc.) é exportado ou exibido em tela, **When** a usuária revisa as datas do relatório, **Then** todas seguem dd/mm/aaaa de forma consistente entre si.

---

### User Story 2 - Ler qualquer horário em formato 24h (Priority: P1)

Como usuária do sistema, eu quero ver todos os horários no formato 24h (ex: "16:00"), sem "AM/PM", para que eu identifique rapidamente o horário de uma aula ou evento sem converter mentalmente o período do dia.

**Why this priority**: Mesmo risco prático de ambiguidade da User Story 1, mas para horário — uma aula marcada "4:00" sem indicação clara de manhã/tarde já gerou confusão histórica no uso do sistema (agenda com aulas ao longo de todo o dia).

**Independent Test**: Percorrer as telas que exibem horário (Agenda, cards de aula, relatórios com horário, formulários de agendamento) e confirmar que todo horário aparece em 24h (00:00–23:59), sem "AM"/"PM".

**Acceptance Scenarios**:

1. **Given** a usuária está na Agenda ou em qualquer tela que exiba o horário de uma aula/evento, **When** a tela carrega, **Then** o horário aparece no formato 24h (ex: "16:00"), nunca com sufixo AM/PM.
2. **Given** um horário à tarde/noite (ex: 16h), **When** exibido em qualquer parte do sistema, **Then** aparece como "16:00", não como "4:00 PM" ou "4:00".
3. **Given** a usuária está preenchendo um formulário com um campo de horário, **When** ela abre o seletor de horário ou digita o valor, **Then** o campo exibe e aceita o valor em formato 24h, independentemente do idioma/sistema operacional do navegador dela.

---

### User Story 3 - Consistência garantida em telas novas ou alteradas no futuro (Priority: P2)

Como responsável por manter o sistema ao longo do tempo, eu quero que exista uma forma única e reutilizável de formatar data e hora, para que qualquer tela nova ou alterada no futuro siga automaticamente o padrão brasileiro, sem depender de cada desenvolvedor lembrar de formatar manualmente.

**Why this priority**: Sem uma forma centralizada, cada correção tela por tela é temporária — a inconsistência pode reaparecer a cada nova tela. Esta história consolida o ganho das duas primeiras ao longo do tempo, mas não é o problema imediato do usuário final.

**Independent Test**: Revisar o conjunto de telas que exibem data/hora e confirmar que todas usam a mesma origem de formatação (não fórmulas de formatação distintas e divergentes espalhadas pelo sistema); adicionar uma data/hora a uma tela nova e confirmar que o resultado já nasce no formato correto sem ajuste manual de formato.

**Acceptance Scenarios**:

1. **Given** uma tela do sistema precisa exibir uma nova data ou horário, **When** o desenvolvedor usa a forma padrão de formatação do sistema, **Then** o resultado já está em dd/mm/aaaa ou 24h, sem necessidade de lógica de formatação própria daquela tela.
2. **Given** o sistema já possui um mecanismo central de formatação, **When** uma auditoria é feita nas telas existentes, **Then** nenhuma tela reimplementa sua própria lógica de formatação de data/hora divergente do padrão central.

---

### Edge Cases

- Campos de formulário que hoje usam o seletor nativo do navegador (calendário/relógio do sistema operacional) passam a usar um componente de data/hora próprio do sistema, que exibe e aceita sempre dd/mm/aaaa e 24h, independentemente do idioma/SO configurado no navegador da usuária.
- Datas nulas/ausentes (ex: campo "Prazo médio de atraso" sem valor): devem continuar exibindo o placeholder já usado (ex: "—"), sem forçar uma data/hora inválida.
- Datas e horários combinados em um único campo (ex: "17/09/2026 16:00") devem seguir a mesma ordem e formato: data primeiro em dd/mm/aaaa, depois hora em 24h.
- Textos gerados a partir de datas (ex: "3 dias atrás", nome do mês por extenso) não são horário nem data numérica e continuam com sua própria regra de exibição, desde que não usem abreviações ou nomes em inglês.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O sistema MUST exibir toda data visível ao usuário (telas, tabelas, relatórios, formulários) no formato dd/mm/aaaa.
- **FR-002**: O sistema MUST exibir todo horário visível ao usuário no formato 24h (00:00 a 23:59), sem indicador AM/PM.
- **FR-003**: Quando data e hora aparecem juntas, o sistema MUST exibi-las na ordem "data hora" (dd/mm/aaaa HH:mm), de forma consistente em todas as telas.
- **FR-004**: O sistema MUST oferecer um único ponto de formatação de data e hora reutilizável por todas as telas, de modo que uma tela nova, ao usá-lo, produza o formato correto sem lógica própria de formatação.
- **FR-005**: O sistema MUST manter, para datas ausentes/nulas, o comportamento atual de placeholder (ex: "—"), sem alterá-lo por conta desta padronização.
- **FR-006**: A padronização MUST cobrir Agenda, Turmas, Alunos, Financeiro (contas a pagar/receber, pagamentos), todos os Relatórios e o Dashboard — todo o frontend que hoje exibe data ou hora.
- **FR-007**: A padronização MUST NOT alterar o valor, cálculo ou fuso horário de nenhuma data/hora armazenada — trata-se exclusivamente de como o valor é exibido, não do dado em si.
- **FR-008**: O sistema MUST substituir os campos de formulário que hoje usam o seletor nativo de data/hora do navegador por um componente próprio, que exiba e aceite sempre dd/mm/aaaa e 24h, independentemente do idioma/sistema operacional configurado no navegador da usuária.

### Key Entities

Não aplicável — esta funcionalidade não introduz nem altera entidades de dados; trata-se de uma regra de apresentação aplicada a datas e horários já existentes em todo o sistema.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% das datas exibidas em qualquer tela, tabela, relatório ou formulário do sistema seguem o formato dd/mm/aaaa, verificado por uma varredura completa de todas as telas do frontend.
- **SC-002**: 100% dos horários exibidos em qualquer tela do sistema seguem o formato 24h, sem nenhuma ocorrência de "AM"/"PM" verificada na mesma varredura.
- **SC-003**: Uma nova tela adicionada ao sistema após esta mudança, ao reutilizar o mecanismo central de formatação, exibe data/hora no formato correto sem exigir nenhum ajuste manual de formatação.
- **SC-004**: Nenhum dado (valor de data/hora armazenado, cálculo, ordenação) muda de comportamento — apenas a forma de exibição — confirmado comparando os valores antes e depois da mudança nas mesmas telas.
- **SC-005**: 100% dos campos de formulário de data/hora exibem e aceitam dd/mm/aaaa e 24h de forma idêntica em pelo menos dois navegadores configurados com idiomas diferentes (ex: um em inglês, um em português), confirmando que o formato não depende do idioma/SO da usuária.

## Assumptions

- O sistema já possui, hoje, um utilitário central de formatação de data/hora amplamente (mas não universalmente) adotado; a expectativa é auditar e completar sua adoção, não criar um mecanismo do zero.
- Parte do sistema já exibe datas/horários corretamente em formato brasileiro; esta funcionalidade cobre tanto os pontos que já estão certos (mantendo-os) quanto os que ainda não usam o padrão central (corrigindo-os).
- O componente próprio de data/hora (substituto do seletor nativo do navegador) é responsável apenas pela exibição/entrada do valor no formato correto; a validação de negócio sobre o valor (ex: data obrigatória, intervalo permitido) continua a mesma já existente hoje, não sendo alterada por esta funcionalidade.
- "Todo o frontend" refere-se às telas voltadas à usuária final (professora/administradora); não inclui logs técnicos, mensagens de erro de sistema ou dados de depuração não destinados ao usuário final.
- Backend e banco de dados não são alterados: datas continuam armazenadas no formato/fuso já usado hoje; a mudança é somente na camada de apresentação do frontend.
