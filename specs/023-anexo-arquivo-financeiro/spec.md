# Feature Specification: Anexo de Arquivo em Contas a Pagar e a Receber

**Feature Branch**: `023-anexo-arquivo-financeiro`

**Created**: 2026-09-16

**Status**: Draft

**Input**: User description: "Adicionar anexo de arquivo (nota fiscal/cupom fiscal) aos registros de Contas a Pagar e Contas a Receber, dentro da aba Financeiro. Requisitos: 1. Cada registro pode ter UM arquivo anexado (nota fiscal OU cupom). Anexar um novo arquivo substitui o anterior — o BLOB antigo deve ser sobrescrito/removido, não acumular. 2. Formatos aceitos: imagens (JPG, PNG) e PDF. Tamanho máximo: 10MB por arquivo. Rejeitar com mensagem clara qualquer outro formato ou tamanho maior. 3. O arquivo deve ser armazenado como BLOB (binário) diretamente no banco de dados MySQL, na própria tabela do registro (ou em uma tabela relacionada 1:1), junto com metadados: nome original do arquivo, tipo MIME, tamanho, data do upload. Não usar armazenamento em disco/sistema de arquivos nem serviço externo de storage — a simplicidade operacional (um único backup cobrindo tudo) é prioridade para este sistema de pequeno porte. 4. Interface: botão 'Anexar arquivo' no registro; se já houver um anexo, mostrar isso visualmente (ícone/nome do arquivo) com opção de visualizar/baixar e substituir. 5. Apenas usuários autenticados com acesso normal àquele registro podem ver/baixar o anexo — sem regra de permissão extra além da já existente pro próprio registro financeiro. 6. Isso é SOMENTE armazenamento/digitalização — sem OCR, sem leitura automática de dados da imagem. O usuário preenche os campos do registro manualmente como já faz hoje; o anexo é só para consulta/comprovação visual futura."

## Clarifications

### Session 2026-09-16

- Q: O anexo pode ser enviado durante a criação de um NOVO registro (antes de ele ser salvo), ou só depois que o registro já existe (na tela de detalhe/edição de um registro já salvo)? → A: Só após o registro existir (o botão "Anexar arquivo" não faz parte do formulário de novo registro), mas com fluxo suave: ao salvar um novo registro com sucesso, a interface automaticamente oferece/abre a opção de anexar arquivo em seguida — como uma etapa 2 natural do mesmo fluxo (tecnicamente duas chamadas separadas: criar registro → depois anexar). Se o usuário fechar sem anexar nesse momento, o registro permanece salvo normalmente e o anexo pode ser adicionado depois, a qualquer momento, sem diferença de comportamento.
- Q: Substituir um anexo existente deve pedir confirmação antes de enviar o novo arquivo, já que o anexo anterior se torna permanentemente indisponível (FR-006)? → A: Sim — o sistema exibe um aviso avisando que o anexo atual será substituído e não poderá ser recuperado, antes de a substituição se efetivar.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Anexar o comprovante a um registro financeiro (Priority: P1)

Como professora registrando uma Conta a Pagar (despesa) ou uma Conta a Receber (cobrança de aluno), eu quero anexar a nota fiscal ou o cupom fiscal correspondente àquele registro, para ter o comprovante visual guardado junto com o lançamento, sem precisar procurá-lo depois em outro lugar (papel, e-mail, pasta do computador).

**Why this priority**: É a capacidade central pedida — sem ela, nenhuma das demais (visualizar, substituir) tem o que fazer. Sozinha já entrega o valor principal: parar de perder ou desorganizar comprovantes fiscais.

**Independent Test**: Abrir um registro existente de Contas a Pagar (ou de Contas a Receber) que ainda não tem anexo, usar o botão "Anexar arquivo" para enviar uma imagem ou PDF válido, e verificar que o registro passa a mostrar esse arquivo como anexado.

**Acceptance Scenarios**:

1. **Given** um registro de Contas a Pagar (ou Contas a Receber) sem nenhum anexo, **When** a professora usa o botão "Anexar arquivo" e escolhe um arquivo JPG, PNG ou PDF de até 10MB, **Then** o sistema salva o arquivo vinculado a esse registro e passa a exibi-lo como anexado (nome do arquivo visível).
2. **Given** a professora está anexando um arquivo, **When** o arquivo escolhido está em um formato diferente de JPG, PNG ou PDF, **Then** o sistema rejeita o envio e exibe uma mensagem clara informando quais formatos são aceitos, sem salvar nada.
3. **Given** a professora está anexando um arquivo, **When** o arquivo escolhido é maior que 10MB, **Then** o sistema rejeita o envio e exibe uma mensagem clara informando o limite de tamanho, sem salvar nada.
4. **Given** um registro sem anexo, **When** a professora tenta salvar o registro sem anexar nenhum arquivo, **Then** o sistema permite normalmente — anexar um comprovante é opcional, nunca bloqueia o cadastro do registro financeiro em si.
5. **Given** a professora está cadastrando um NOVO registro de Contas a Pagar (ou Contas a Receber), **When** o registro é salvo com sucesso, **Then** o sistema oferece imediatamente, na mesma sequência de tela, a opção de anexar um arquivo a esse registro recém-criado — sem exigir que a professora navegue de volta para localizá-lo.
6. **Given** a professora acabou de salvar um novo registro e o sistema ofereceu a opção de anexar um arquivo em seguida, **When** ela decide não anexar nada naquele momento (fecha ou ignora a oferta), **Then** o registro permanece salvo normalmente, e o comprovante pode ser anexado depois, a qualquer momento, pela tela de detalhe/edição do registro — sem nenhuma diferença de comportamento em relação a anexar imediatamente.

---

### User Story 2 - Visualizar e baixar o comprovante anexado (Priority: P1)

Como professora consultando um registro financeiro que já tem um comprovante anexado, eu quero visualizar ou baixar esse arquivo diretamente da tela do registro, para conferir a nota fiscal/cupom sempre que precisar (por exemplo, ao prestar contas ou tirar uma dúvida sobre um valor).

**Why this priority**: Anexar um comprovante só tem valor prático se ele puder ser recuperado depois com facilidade — sem esta capacidade, User Story 1 vira apenas um armazenamento sem utilidade real.

**Independent Test**: Abrir um registro que já tem um anexo e verificar que é possível visualizar/baixar o arquivo exatamente como foi enviado (mesmo conteúdo, nome reconhecível).

**Acceptance Scenarios**:

1. **Given** um registro com um anexo já salvo, **When** a professora abre esse registro, **Then** o sistema mostra visualmente que há um anexo (ícone e nome original do arquivo), com uma opção para visualizar ou baixar.
2. **Given** um registro com um anexo, **When** a professora escolhe visualizar/baixar o anexo, **Then** o sistema entrega o arquivo com o mesmo conteúdo que foi originalmente enviado, sem perda ou corrupção.
3. **Given** um usuário autenticado que já tem acesso normal aos registros financeiros do sistema (mesmo nível de acesso de hoje, sem regra nova), **When** esse usuário abre um registro com anexo, **Then** ele consegue visualizar/baixar o anexo da mesma forma — não existe uma permissão adicional específica para anexos.

---

### User Story 3 - Substituir o comprovante anexado (Priority: P2)

Como professora que já anexou um comprovante a um registro, eu quero poder substituí-lo por um novo arquivo (por exemplo, ao perceber que anexou o comprovante errado, ou ao receber uma versão mais legível), para manter sempre apenas o comprovante correto e atual vinculado ao registro.

**Why this priority**: É um refinamento sobre User Story 1 — importante para corrigir enganos e manter os dados corretos, mas o sistema já entrega valor real mesmo que a professora precise, por ora, excluir e recriar o registro para trocar um anexo errado (o que a User Story 1 sozinha não impede, mas não é o fluxo ideal).

**Independent Test**: Abrir um registro que já tem um anexo, usar a opção de substituir, enviar um novo arquivo válido, e verificar que o anexo antigo deixa de existir e só o novo arquivo fica disponível para visualização/download.

**Acceptance Scenarios**:

1. **Given** um registro com um anexo já salvo, **When** a professora usa a opção de substituir, **Then** o sistema exibe um aviso de confirmação informando que o anexo atual será substituído e não poderá ser recuperado, antes de permitir escolher o novo arquivo (ou antes de efetivar o envio).
2. **Given** o aviso de confirmação de substituição, **When** a professora confirma e envia um novo arquivo válido (formato e tamanho aceitos), **Then** o sistema passa a mostrar o novo arquivo como o anexo do registro, e o arquivo anterior deixa de estar disponível (não é mais possível visualizá-lo ou baixá-lo).
3. **Given** o aviso de confirmação de substituição, **When** a professora cancela a confirmação, **Then** nada é alterado — o anexo atual permanece exatamente como estava, sem nenhum arquivo novo sendo solicitado.
4. **Given** a professora confirmou a substituição, **When** o novo arquivo enviado é inválido (formato não aceito ou maior que 10MB), **Then** o sistema rejeita o envio com uma mensagem clara e o anexo anterior permanece inalterado (a substituição só se efetiva quando o novo arquivo é válido).

---

### Edge Cases

- O que acontece se a professora tentar anexar um arquivo com extensão de imagem/PDF mas cujo conteúdo real não corresponde a esse tipo (arquivo renomeado maliciosamente)? O sistema deve validar o conteúdo real do arquivo, não apenas a extensão do nome, e rejeitar como formato inválido caso não corresponda.
- O que acontece se dois usuários tentarem anexar/substituir o arquivo do mesmo registro ao mesmo tempo? O último envio bem-sucedido prevalece como o anexo atual do registro — não é exigido nenhum mecanismo de bloqueio ou aviso de conflito nesta versão.
- O que acontece com o anexo se o registro financeiro ao qual ele pertence for cancelado (mudar de status, sem ser fisicamente excluído, conforme já é o comportamento hoje)? O anexo permanece vinculado e disponível para consulta, já que o registro em si continua existindo no sistema.
- O que acontece se o arquivo enviado estiver vazio (0 bytes) ou corrompido? O sistema deve rejeitar como envio inválido, com mensagem clara, sem salvar nada.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O sistema MUST permitir anexar um único arquivo a qualquer registro de Contas a Pagar e a qualquer registro de Contas a Receber, por meio de um botão "Anexar arquivo" visível no próprio registro.
- **FR-002**: O sistema MUST aceitar apenas arquivos dos formatos JPG, PNG ou PDF como anexo, validando o conteúdo real do arquivo (não apenas o nome/extensão informados).
- **FR-003**: O sistema MUST rejeitar qualquer arquivo maior que 10MB, sem salvá-lo.
- **FR-004**: Quando um arquivo enviado for rejeitado (formato não aceito, tamanho acima do limite, ou conteúdo inválido/corrompido), o sistema MUST exibir uma mensagem clara explicando o motivo da rejeição, sem alterar o anexo atual do registro (se houver um).
- **FR-005**: O sistema MUST persistir o conteúdo binário do arquivo anexado no banco de dados (não em disco/sistema de arquivos do servidor nem em serviço externo de armazenamento), junto dos seus metadados: nome original do arquivo, tipo do arquivo, tamanho e data do upload.
- **FR-006**: Ao anexar um novo arquivo a um registro que já possui um anexo, o sistema MUST substituir completamente o anexo anterior pelo novo — o conteúdo e os metadados do arquivo antigo MUST deixar de existir no sistema após a substituição ser concluída com sucesso, sem acumular versões.
- **FR-007**: Se o novo arquivo enviado para substituição for inválido (formato ou tamanho não aceitos), o sistema MUST manter o anexo anterior inalterado — a substituição só se efetiva quando o novo arquivo passa em todas as validações.
- **FR-008**: O sistema MUST exibir, em qualquer registro que já tenha um anexo, uma indicação visual clara desse anexo (incluindo o nome original do arquivo), com uma ação para visualizá-lo ou baixá-lo.
- **FR-009**: O sistema MUST entregar, ao visualizar/baixar um anexo, exatamente o mesmo conteúdo de arquivo que foi originalmente enviado, sem alteração.
- **FR-010**: O sistema MUST restringir o acesso para visualizar/baixar um anexo aos mesmos usuários autenticados que já têm acesso ao registro financeiro correspondente hoje — sem introduzir nenhuma regra de permissão adicional específica para anexos.
- **FR-011**: Anexar um comprovante MUST ser opcional — o sistema MUST continuar permitindo criar, editar e salvar registros de Contas a Pagar e Contas a Receber sem nenhum anexo, exatamente como já funciona hoje.
- **FR-012**: O sistema MUST NOT realizar nenhum tipo de leitura ou extração automática de dados a partir do conteúdo do arquivo anexado (sem OCR) — o anexo serve apenas para consulta/comprovação visual, e os campos do registro continuam sendo preenchidos manualmente pelo usuário.
- **FR-013**: O botão "Anexar arquivo" MUST estar disponível apenas para um registro que já existe (já foi salvo) — o formulário de criação de um novo registro MUST NOT incluir o campo de anexo como parte do mesmo envio que cria o registro.
- **FR-014**: Imediatamente após um novo registro de Contas a Pagar ou Contas a Receber ser salvo com sucesso, o sistema MUST oferecer, na mesma sequência de tela, a opção de anexar um arquivo a esse registro recém-criado, sem exigir navegação adicional para localizá-lo.
- **FR-015**: Ignorar ou fechar essa oferta MUST NOT ter nenhum efeito sobre o registro já salvo — ele permanece disponível normalmente, e o anexo pode ser adicionado depois pela tela de detalhe/edição, a qualquer momento, com o mesmo comportamento.
- **FR-016**: Antes de efetivar a substituição de um anexo existente, o sistema MUST exibir um aviso de confirmação informando que o anexo atual será substituído e não poderá ser recuperado — a substituição só MUST prosseguir se a professora confirmar; cancelar a confirmação MUST NOT alterar o anexo existente.

### Key Entities *(include if feature involves data)*

- **Anexo de Comprovante**: representa o arquivo (nota fiscal ou cupom fiscal) vinculado a exatamente um registro de Contas a Pagar ou de Contas a Receber, numa relação um-para-um (cada registro tem no máximo um anexo; cada anexo pertence a exatamente um registro). Atributos: conteúdo binário do arquivo, nome original do arquivo, tipo do arquivo (imagem ou PDF), tamanho em bytes, e data/hora do upload. Substituir o anexo de um registro remove por completo o anexo anterior daquele registro.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Uma professora consegue anexar um comprovante válido a um registro financeiro em menos de 30 segundos, sem precisar de instruções adicionais.
- **SC-002**: 100% das tentativas de anexar um arquivo em formato ou tamanho não aceitos são rejeitadas com uma mensagem compreensível, e nenhum desses arquivos rejeitados é salvo no sistema.
- **SC-003**: Ao substituir um anexo existente, o arquivo anterior deixa de estar disponível para visualização/download em 100% dos casos — nenhum arquivo antigo permanece acessível após uma substituição bem-sucedida.
- **SC-004**: 100% dos arquivos baixados/visualizados correspondem exatamente (mesmo conteúdo) ao arquivo que foi originalmente enviado como anexo.
- **SC-005**: Registros financeiros continuam podendo ser criados e salvos sem anexo em 100% dos casos, sem nenhuma mudança no fluxo de cadastro já existente.
- **SC-006**: 100% das tentativas de substituir um anexo existente exibem o aviso de confirmação antes de qualquer envio de arquivo, e uma substituição cancelada na confirmação nunca altera o anexo existente.

## Assumptions

- O botão "Anexar arquivo" e a exibição do anexo existente ficam na tela de detalhe (e/ou edição) de um registro já salvo de Contas a Pagar / Contas a Receber, não na tela de listagem nem no formulário de criação de um novo registro — consistente com o pedido do usuário ("botão... no registro") e com a decisão em Clarifications de que anexar só é possível após o registro existir (a oferta automática logo após salvar um novo registro, conforme FR-014, direciona a professora para essa mesma tela/fluxo, sem exigir busca manual).
- "Acesso normal àquele registro" (FR-010) equivale, hoje, ao mesmo controle de acesso já aplicado às telas de Contas a Pagar e Contas a Receber como um todo — não há hoje nenhuma regra de permissão por registro individual (todo usuário autenticado com o perfil que já acessa essas telas vê todos os registros), e este recurso não muda isso.
- Um registro cujo status muda (por exemplo, para "Cancelado") continua existindo no sistema — como já é o comportamento atual de Contas a Pagar/Receber — então o anexo vinculado a ele também continua existindo e acessível; esta funcionalidade não introduz nenhuma forma de exclusão física de registro ou de anexo por mudança de status.
- Armazenar o conteúdo binário do arquivo no banco de dados (em vez de em disco ou serviço externo) é uma decisão explícita do usuário, motivada por simplicidade operacional (um único backup cobrindo dados e anexos) — não uma limitação técnica desta especificação.
- Não é exigido nesta versão: histórico de anexos anteriores, anexar mais de um arquivo por registro, pré-visualização de imagem em miniatura na listagem, ou qualquer processamento do conteúdo do arquivo além de guardá-lo e devolvê-lo integralmente.
