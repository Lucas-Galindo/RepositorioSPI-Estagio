# Feature Specification: Lembretes Somente por Email

**Feature Branch**: `001-lembretes-canal-email`

**Created**: 2026-09-11

**Status**: Implemented (documentação retroativa)

**Input**: User description: "Este é um registro retroativo de uma mudança que já foi implementada: removemos WhatsApp e SMS como canais válidos de Lembrete, deixando apenas Email. Motivo: o LembreteDispatcherService nunca despachava esses canais (continue explícito no código), fazendo lembretes cadastrados com eles ficarem Pendentes para sempre, silenciosamente, sem erro visível. A validação foi removida no backend (LembreteRequestValidator) e no frontend (LembreteForm, tipo CanalLembrete), rejeitando outros canais com mensagem clara. Lembretes existentes com WhatsApp/SMS foram migrados para Email via script SQL (database/11_lembrete_canal_email_somente.sql). Gere o spec.md refletindo esse comportamento já implementado, como documentação histórica da decisão."

## Nota histórica

Este documento registra, de forma retroativa, uma decisão e mudança já implementadas no sistema. Ele existe como referência histórica sobre por que o canal de lembrete foi restringido a Email, não como um roteiro de implementação futura.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Cadastrar lembrete com canal válido (Priority: P1)

Como usuário responsável por configurar lembretes de aula, ao criar ou editar um lembrete eu só posso escolher "Email" como canal de envio, para garantir que o lembrete realmente será disparado.

**Why this priority**: É o fluxo principal afetado pela mudança — sem essa restrição, o usuário podia configurar um lembrete que nunca seria enviado, sem qualquer aviso.

**Independent Test**: Pode ser testado criando um lembrete pela tela de Lembretes e confirmando que apenas "Email" aparece/é aceito como opção de canal.

**Acceptance Scenarios**:

1. **Given** a tela de cadastro de lembrete, **When** o usuário seleciona o canal de envio, **Then** somente a opção "Email" está disponível.
2. **Given** uma requisição de criação/edição de lembrete enviada diretamente à API com `Canal = "Email"`, **When** os demais campos são válidos, **Then** o lembrete é salvo com sucesso.

---

### User Story 2 - Rejeitar canais descontinuados (Priority: P1)

Como usuário ou integração que tenta cadastrar um lembrete com canal WhatsApp ou SMS, eu recebo uma mensagem de erro clara explicando que esses canais foram descontinuados, em vez de o lembrete ser aceito e nunca disparado.

**Why this priority**: É a proteção central que evita a recorrência do bug original (lembretes presos em "Pendente" para sempre, sem erro visível).

**Independent Test**: Pode ser testado enviando uma requisição à API com `Canal = "WhatsApp"` ou `Canal = "SMS"` e verificando que a requisição é rejeitada com mensagem explicativa.

**Acceptance Scenarios**:

1. **Given** uma requisição de criação de lembrete com `Canal = "WhatsApp"`, **When** a requisição é validada, **Then** o sistema rejeita a requisição com a mensagem "Canal deve ser 'Email'. WhatsApp e SMS foram descontinuados."
2. **Given** uma requisição de criação de lembrete com `Canal = "SMS"`, **When** a requisição é validada, **Then** o sistema rejeita a requisição com a mesma mensagem de erro.
3. **Given** o formulário de lembrete na interface, **When** o usuário tenta submeter um canal diferente de Email (por exemplo, via manipulação do formulário), **Then** a interface impede o envio e exibe mensagem equivalente.

---

### User Story 3 - Migração de lembretes existentes (Priority: P2)

Como administrador do sistema, os lembretes que já estavam cadastrados com canal WhatsApp ou SMS antes da mudança foram automaticamente migrados para Email, para que continuem sendo processados em vez de ficarem permanentemente presos.

**Why this priority**: Trata os dados históricos afetados pelo bug, mas é um evento único (script de migração), não um fluxo recorrente de uso.

**Independent Test**: Pode ser verificado consultando a tabela de lembretes após a migração e confirmando que nenhum registro possui canal WhatsApp ou SMS.

**Acceptance Scenarios**:

1. **Given** lembretes pré-existentes com `canal IN ('WhatsApp', 'SMS')`, **When** o script de migração é executado, **Then** todos esses registros passam a ter `canal = 'Email'`.
2. **Given** a base de dados após a migração, **When** consultados os canais distintos de lembretes, **Then** apenas "Email" existe como valor de canal.

---

### Edge Cases

- O que acontece se uma integração externa (fora da tela padrão) enviar um valor de canal em caixa diferente, como "email" em vez de "Email"? O sistema trata o valor como inválido (comparação sensível a maiúsculas/minúsculas) e rejeita a requisição.
- O que acontece se o campo `Canal` vier vazio ou nulo? É tratado como canal inválido e rejeitado com a mesma mensagem de erro.
- Como o sistema evita que o bug original se repita para um canal futuro (por exemplo, se alguém reintroduzir WhatsApp na lista de canais válidos sem implementar o despacho)? Fora do escopo desta mudança — é uma lacuna de processo que deve ser tratada quando/se um novo canal for reintroduzido.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O sistema MUST aceitar apenas "Email" como valor válido para o canal de um lembrete, tanto na criação quanto na edição.
- **FR-002**: O sistema MUST rejeitar requisições de criação/edição de lembrete com canal "WhatsApp", "SMS" ou qualquer outro valor diferente de "Email", retornando uma mensagem de erro explicando que WhatsApp e SMS foram descontinuados.
- **FR-003**: A interface de cadastro/edição de lembrete MUST oferecer apenas "Email" como opção selecionável de canal, sem apresentar WhatsApp ou SMS como alternativas.
- **FR-004**: O sistema MUST ter migrado, de forma única e retroativa, todos os lembretes existentes com canal "WhatsApp" ou "SMS" para canal "Email".
- **FR-005**: O serviço de despacho de lembretes (`LembreteDispatcherService`) MUST continuar processando normalmente lembretes com canal "Email", sem alterações de comportamento para esse canal.

### Key Entities *(include if feature involves data)*

- **Lembrete**: Representa um aviso agendado sobre uma aula/turma, com atributos como turma associada, canal de envio, antecedência (em horas) e destinatários (Alunos, Responsáveis ou ambos). O canal agora é restrito a "Email".

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% das tentativas de criar ou editar um lembrete com canal diferente de "Email" são rejeitadas com mensagem de erro explicativa, tanto pela API quanto pela interface.
- **SC-002**: Zero lembretes na base de dados possuem canal "WhatsApp" ou "SMS" após a migração.
- **SC-003**: Nenhum lembrete válido (canal "Email") permanece indefinidamente com status "Pendente" por causa de um canal não suportado pelo serviço de despacho.

## Assumptions

- Nenhum outro canal (por exemplo, push notification) estava planejado para ser adicionado no mesmo momento desta mudança; o escopo foi estritamente remover WhatsApp/SMS e manter Email.
- A migração de dados (script SQL) é executada uma única vez em cada ambiente e não precisa ser reexecutável de forma idempotente além de checar `canal IN ('WhatsApp', 'SMS')`.
- Usuários e integrações que dependiam dos canais WhatsApp/SMS não tinham qualquer envio funcional através deles antes desta mudança (confirmado pelo `continue` explícito no `LembreteDispatcherService`), portanto a remoção não interrompe nenhum fluxo de envio que estivesse realmente funcionando.
