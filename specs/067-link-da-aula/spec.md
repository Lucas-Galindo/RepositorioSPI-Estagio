# Feature Specification: Link da aula temporário (informado nos detalhes, enviado no lembrete)

**Feature Branch**: `067-link-da-aula`

**Created**: 2026-11-03

**Status**: Draft

**Input**: Card do Trello "Link da aula na expansão da aula (opcional)" (prioridade 2 do backlog). Pedido do usuário em 2026-11-03: o link opcional **não deve ficar no banco de dados**; deve ser informado/postado na aula (nos detalhes dela) e **encaminhado no e-mail de lembrete de aula** já configurado. "O link não fica no banco para sempre, é como se fosse uma memória cache que logo vai embora." **Esta spec substitui o desenho do card**, que previa uma coluna nova e migração no banco.

## Contexto (estado atual, lido do código)

- A aula (`Aula`, tabela `aula`) **não tem** campo de link. Nenhum lugar do sistema guarda link de reunião.
- Os detalhes da aula são a tela `frontend/app/(app)/aulas/[id]/page.tsx` e o popup `frontend/components/aulas/AulaDetalhesPopup.tsx` (Home/Agenda, spec 061). As respostas de aula vêm de `AulaResponse` (`AulaService.Mapear`).
- O lembrete de aula é **por turma** (`Lembrete`, `LembreteDispatcherService`): a rotina envia, no horário programado (próxima aula da turma menos a antecedência), um e-mail ao aluno e ao responsável. O e-mail hoje diz só o nome da turma e a hora do lembrete. A "próxima aula" da turma é a que o sistema já usa para calcular esse horário (`ObterProximaAulaDaTurmaAsync`).
- Aula **individual** (sem turma) não tem lembrete por e-mail; só aparece nos detalhes.
- Em Development todo e-mail é desviado para o endereço de teste (`Smtp:RedirecionarTodosPara`).

## Clarificações (respondidas pelo usuário em 2026-11-03)

- Q: De onde vem o link? → A: **A professora informa** (cola um link pronto, ex.: Meet ou Zoom) num campo nos detalhes da aula. O sistema **não** cria salas nem integra com provedores.
- Q: Onde o link fica guardado, já que não vai para o banco? → A: **Em memória do servidor**, com validade; nada é gravado em banco nem em arquivo. Risco aceito: se a API reiniciar, os links ainda não usados se perdem e a professora informa de novo.
- Q: Quando o link "vai embora"? → A: **Ao fim da aula** (data + hora de fim).

## Decisões menores adotadas (a confirmar na aprovação)

1. **Quem aceita link**: aula ativa, não cancelada e que **ainda não terminou**. Aula já terminada, cancelada ou excluída recusa (409) e não mostra o campo.
2. **Onde aparece**: nos **detalhes da aula** (tela e popup da Home/Agenda) e no **e-mail do lembrete**. Não aparece em listas.
3. **Aula individual** aceita e mostra o link nos detalhes; como não tem lembrete por e-mail, não há envio.
4. **Editar e remover**: a professora pode trocar ou apagar o link a qualquer momento antes do fim da aula.
5. **Validação**: obrigatório quando informado, sem espaços, endereço absoluto `http://` ou `https://`, até **500 caracteres**.
6. **Mudança de horário da aula**: ao editar data/hora, a validade acompanha o novo fim; se a aula for cancelada ou excluída, o link é descartado.
7. **E-mail**: o link entra no corpo do lembrete **só quando existe** no momento do envio, tomado da **próxima aula da turma** (a mesma que define o horário do lembrete). Sem link, o e-mail fica exatamente como hoje. **Não** há reenvio se o link for informado depois do e-mail ter saído.
8. **Segurança**: o link pode conter senha de reunião; **não** é registrado em log, e o e-mail o escreve com os caracteres especiais protegidos. Só esquemas `http`/`https` são aceitos (nada de `javascript:`).
9. A tela avisa, junto do campo, que o link fica guardado só até o fim da aula e que **se perde se o sistema reiniciar**.
10. **Uma instalação só**: o armazenamento temporário assume um único servidor da API (o que o sistema é hoje).
11. **Ordem de entrega**: depende do relógio de São Paulo da spec 064, que **já foi implementada** (2026-11-03); a 067 só acrescenta a conversão de fuso na mesma classe.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Informar o link nos detalhes da aula (Priority: P1)

A professora abre os detalhes de uma aula futura (ou em andamento), cola o link da reunião e salva. O link aparece clicável nos detalhes e pode ser trocado ou removido.

**Why this priority**: É a base: sem poder informar e ver o link, o e-mail não tem o que enviar.

**Independent Test**: Abrir os detalhes de uma aula futura, informar `https://meet.google.com/abc-defg-hij`, ver o link clicável (abre em nova aba); trocar e remover; tentar `ftp://x` e texto sem endereço e ver o erro.

**Acceptance Scenarios**:

1. **Given** uma aula futura sem link, **When** a professora informa um link válido e salva, **Then** os detalhes mostram o link clicável e o aviso de que ele é temporário.
2. **Given** uma aula com link, **When** ela troca o link, **Then** vale o novo; **When** remove, **Then** os detalhes voltam a mostrar o campo vazio.
3. **Given** um texto que não é endereço `http(s)` (ex.: `abc`, `ftp://x`, `javascript:alert(1)`, com espaços, ou acima de 500 caracteres), **When** ela salva, **Then** é recusado com mensagem e nada é guardado.
4. **Given** uma aula que já terminou, cancelada ou excluída, **When** os detalhes abrem, **Then** o campo de link não aparece e a API recusa (409).
5. **Given** o popup de detalhes da aula (Home/Agenda), **When** a aula tem link, **Then** o link aparece clicável também ali.

---

### User Story 2 - O link vai no lembrete de aula por e-mail (Priority: P1)

Quando o lembrete de uma turma é enviado, o e-mail do aluno e do responsável inclui o link da próxima aula, se a professora tiver informado.

**Why this priority**: É o objetivo do pedido: o aluno recebe o link no lembrete.

**Independent Test**: Informar o link na próxima aula de uma turma com lembrete vencido, rodar o envio (com o servidor de e-mail desviado) e ver o link no corpo; repetir sem link e ver o e-mail igual ao de hoje.

**Acceptance Scenarios**:

1. **Given** uma turma com lembrete a enviar e a próxima aula com link, **When** o lembrete é enviado, **Then** o e-mail contém o link clicável da aula.
2. **Given** a próxima aula sem link, **When** o lembrete é enviado, **Then** o e-mail é idêntico ao de hoje (sem menção a link).
3. **Given** o link de uma aula já expirado ou perdido (reinício), **When** o lembrete é enviado, **Then** o e-mail sai sem link, sem erro.
4. **Given** um link com caracteres especiais (`&`, `<`, aspas), **When** o e-mail é montado, **Then** o conteúdo é protegido e o link continua correto e clicável.
5. **Given** qualquer lembrete, **When** é enviado com ou sem link, **Then** quando e para quem enviar (regra de envio) não muda.

---

### User Story 3 - O link some sozinho (Priority: P1)

O link não fica guardado para sempre: some ao fim da aula, quando a aula é cancelada ou excluída, e se o sistema reiniciar.

**Why this priority**: É a restrição central do pedido ("memória que logo vai embora") e protege senhas de reunião.

**Independent Test**: Informar o link numa aula que termina em poucos minutos, esperar passar o fim e ver que o link sumiu dos detalhes e da API; cancelar outra aula e ver o link sumir; confirmar no banco que nada foi gravado.

**Acceptance Scenarios**:

1. **Given** uma aula com link, **When** passa a data e hora de fim, **Then** o link deixa de aparecer nos detalhes e na API e não vai mais em e-mail.
2. **Given** uma aula com link, **When** ela é cancelada ou excluída, **Then** o link é descartado.
3. **Given** uma aula com link, **When** a data ou o horário são editados, **Then** o link continua e passa a valer até o novo fim (se o novo fim já passou, é descartado).
4. **Given** o sistema reiniciado, **When** a professora abre os detalhes, **Then** o link não está lá e ela pode informar de novo.
5. **Given** qualquer momento, **When** se examina o banco, **Then** não existe coluna, tabela ou registro com o link.

---

### Edge Cases

- **Reinício da API antes do lembrete**: o link se perde e o e-mail sai sem link (aceito; a tela avisa).
- **Lembrete enviado antes de a professora informar o link**: não há reenvio; só o e-mail seguinte (de outra aula) terá link.
- **Duas aulas da mesma turma no mesmo dia**: o e-mail leva o link da próxima aula (a que define o horário do lembrete).
- **Aula em andamento**: aceita informar/trocar o link até o fim (a professora pode precisar colar o link já com a aula começada).
- **Aula "Realizada" ainda em andamento**: continua aceitando e mostrando o link até o fim.
- **Link com senha na URL**: nunca aparece em log; aparece só nos detalhes e no e-mail.
- **Aula sem link**: tudo funciona exatamente como hoje (detalhes, listas, e-mail).
- **Dois cliques/duas abas**: vale o último link salvo.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: A professora MUST poder informar, trocar e remover um link de aula nos detalhes da aula; o link é opcional.
- **FR-002**: O link MUST NOT ser gravado no banco de dados nem em arquivo: nenhuma coluna, tabela, migração ou registro persistente. Ele MUST ficar apenas em memória do servidor.
- **FR-003**: O servidor MUST validar o link: obrigatório quando informado, sem espaços, endereço absoluto `http://` ou `https://`, até 500 caracteres. Qualquer outro esquema (ex.: `javascript:`, `ftp:`, `data:`) MUST NOT ser aceito.
- **FR-004**: O servidor MUST aceitar link somente para aula ativa, não cancelada e que ainda não terminou; nos demais casos MUST recusar com 409 e a tela MUST NOT mostrar o campo. A decisão de "aceita link" MUST vir do servidor (a tela só exibe).
- **FR-005**: O link MUST ser descartado ao fim da aula (data + hora de fim, no fuso America/Sao_Paulo), quando a aula é cancelada ou excluída; ao editar data/hora, a validade MUST acompanhar o novo fim. Um link expirado MUST NOT ser mostrado nem enviado.
- **FR-006**: Os detalhes da aula (tela e popup) MUST mostrar o link, quando existir, como endereço clicável que abre em nova aba com proteção (`noopener noreferrer`), e MUST informar que o link é temporário e se perde se o sistema reiniciar.
- **FR-007**: O e-mail do lembrete de aula MUST incluir o link da próxima aula da turma quando ele existir no momento do envio, com os caracteres especiais protegidos (HTML). Sem link, o e-mail MUST permanecer idêntico ao atual.
- **FR-008**: A inclusão do link MUST NOT alterar a regra de envio dos lembretes (quando, para quem, orçamento por ciclo, tentativas e status); um link ausente, expirado ou perdido MUST NOT causar erro nem impedir o envio.
- **FR-009**: O link MUST NOT ser registrado em log nem aparecer em mensagens de erro.
- **FR-010**: Aula sem link MUST continuar funcionando exatamente como hoje (detalhes, listas, e-mail).
- **FR-011**: O frontend MUST NOT validar o endereço, decidir se a aula aceita link nem calcular a expiração; apenas exibe o que o servidor informa e envia o que a professora digitou.
- **FR-012**: A spec do lembrete de aula (`specs/001-lembretes-canal-email`) MUST receber uma nota apontando para esta spec sobre o novo conteúdo opcional do e-mail (Princípio V da constituição).

### Key Entities

- **Link da aula (temporário)**: endereço informado pela professora, associado ao identificador de uma aula, com validade até o fim dessa aula. **Não é persistido**; vive só na memória do servidor. Não é um campo de `Aula`.
- **Aula**: sem mudança de estrutura; a resposta da aula passa a trazer o link (quando houver) e se a aula aceita link.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 0 registros, colunas ou arquivos com o link: o banco e o disco ficam idênticos antes e depois de informar links.
- **SC-002**: 100% dos e-mails de lembrete de aulas com link vigente contêm o link; 100% dos de aulas sem link são idênticos aos de hoje.
- **SC-003**: 0 links mostrados ou enviados depois do fim da aula, depois de cancelar/excluir, ou depois de um reinício.
- **SC-004**: 100% dos endereços fora de `http`/`https`, com espaços ou acima de 500 caracteres são recusados pelo servidor, sem depender da tela.
- **SC-005**: Informar o link leva no máximo 3 passos nos detalhes da aula (abrir, colar, salvar).
- **SC-006**: 0 mudanças nas regras de envio dos lembretes (mesmos testes de não regressão do `LembreteDispatcherService` passando sem alteração de expectativa).

## Assumptions

- A API roda em um único servidor (sem balanceamento), por isso a memória local basta.
- O link é de reunião online (Meet, Zoom etc.); o sistema não verifica se a sala existe.
- Os destinatários do e-mail continuam sendo os atuais (alunos e/ou responsáveis da regra do lembrete).
- A professora é a única usuária que informa o link; não há login do aluno.

## Fora de escopo

- Criar a sala/link automaticamente (integração com Google Meet, Zoom ou similar).
- Guardar o link em banco, arquivo ou serviço externo (explicitamente descartado).
- Reenviar o lembrete quando o link é informado depois do envio.
- Mostrar o link em listas, no sino de notificações ou em relatórios.
- Link em lembretes de pagamento, notificações ou outros e-mails.
