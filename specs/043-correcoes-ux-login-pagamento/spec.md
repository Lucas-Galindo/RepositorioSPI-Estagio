# Feature Specification: Três Correções de UX (Login, Recuperação de Senha, Status de Pagamento)

**Feature Branch**: `043-correcoes-ux-login-pagamento`

**Created**: 2026-09-29

**Status**: Draft

**Input**: User description: "Três correções pequenas e independentes de UX: (1) Enter nos campos de login deve submeter, igual o botão 'Entrar' — hoje só o clique funciona. (2) Investigar se o fluxo de 'esqueci a senha' está de fato enviando o e-mail de recuperação — pode já funcionar, ou pode ter um bug real. (3) Botões de status de pagamento (Pendente/Pago/Atrasado/Cancelado) na tela de detalhe de Contas a Pagar/Receber parecem um filtro de exibição, mas são uma ação que muda o dado de verdade — ajustar o visual (modal de confirmação, ou estilo mais assertivo)."

## Nota de investigação prévia

Confirmado por leitura do código atual (grounding antes de especificar) — os 3 itens tiveram achados concretos, não só suposições:

- **Item 1 (Enter no login)**: `frontend/components/auth/LoginForm.tsx` já tem `<form onSubmit={handleSubmit}>`, os dois campos (e-mail/RA e senha) dentro do `<form>`, e um `<button type="submit">`. Não há nenhum `onKeyDown`/listener de `keydown` em nenhum lugar do frontend interceptando a tecla Enter. Pela leitura do código, o comportamento padrão do HTML (Enter num campo de texto dentro de um `<form>` com botão `submit` já dispara o `onSubmit`) **deveria já funcionar**. Não descarto um problema específico de ambiente/navegador que só apareça rodando de verdade — por isso esta feature MUST incluir uma verificação real no navegador antes de decidir se algum código muda.
- **Item 2 (e-mail de recuperação)**: o fluxo de backend está implementado de ponta a ponta (`RecuperacaoSenhaService.EsqueciSenhaAsync` gera um token, salva com expiração de 15 minutos, e chama `IEmailSender.EnviarAsync`, implementado por `BrevoEmailSender` via SMTP/MailKit). **Achado concreto**: `BrevoOptions.SmtpUsuario`, `SmtpChave` e `RemetenteEmail` (declarados em `src/SPI.Infrastructure/Email/BrevoOptions.cs`) não têm valor em nenhum lugar deste ambiente — nem em `appsettings.json` (só `SmtpHost`/`SmtpPorta`/`RemetenteNome` estão lá), nem em `appsettings.Development.json`, nem em User Secrets (`dotnet user-secrets list` não lista nenhuma chave `Brevo:*`). Isso significa que, hoje, todo envio de e-mail de recuperação **falha silenciosamente**: `RecuperacaoSenhaService` captura qualquer exceção do envio e só registra no log (`_logger.LogError`), por desenho — a resposta ao cliente é sempre a mesma, para não virar canal de enumeração de e-mail cadastrado. Ou seja: não é "talvez tenha um bug" — hoje, comprovadamente, nenhum e-mail de recuperação chega a lugar nenhum neste ambiente, por falta de credencial configurada, e ninguém percebe isso a não ser olhando o log.
- **Item 3 (botões de status)**: confirmado nas duas telas (`frontend/app/(app)/financeiro/contas-a-pagar/[id]/page.tsx` e `contas-a-receber/[id]/page.tsx`, mesmo padrão nas duas): os 4 botões (`Pendente`/`Pago`/`Atrasado`/`Cancelado`) são estilizados como `btn-primary` (o status atual) e `btn-ghost` (os demais) lado a lado — visualmente idêntico a um grupo de abas/filtro de seleção. `onClick` chama `handleStatus(s)` que chama `atualizarStatusContaPagar` **imediatamente**, sem nenhum modal ou passo de confirmação intermediário. Existe só um texto de aviso abaixo dos botões ("Ao marcar como Pago, a data de pagamento é preenchida automaticamente. Cancelar não exclui o registro..."), fácil de não notar.

## Clarifications

_Nenhuma — as 3 correções têm escopo claro após a investigação acima; nenhuma pergunta atinge o limiar de impacto que justificaria `/speckit-clarify` (ver Assumptions para as decisões de default tomadas)._

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Recuperação de senha realmente chega no e-mail (Priority: P1)

Como professora ou aluno que esqueceu a senha, quero que o e-mail de recuperação realmente chegue na minha caixa de entrada quando eu peço, para eu conseguir voltar a acessar o sistema sem depender de outra pessoa.

**Why this priority**: É a prioridade mais alta das três — hoje, comprovadamente, o e-mail nunca chega neste ambiente (credencial SMTP ausente). Um fluxo de recuperação de acesso que não funciona é um risco de trancar alguém para fora do sistema permanentemente.

**Independent Test**: Com credenciais reais de um provedor SMTP configuradas (Brevo, via User Secrets), pedir a recuperação de senha para uma conta cadastrada de teste e confirmar que o e-mail chega numa caixa de entrada real, com o código correto, dentro de poucos minutos.

**Acceptance Scenarios**:

1. **Given** uma professora com e-mail cadastrado e ativo, **When** ela pede a recuperação de senha, **Then** um e-mail com o código de redefinição chega na caixa de entrada dela em poucos minutos, com o texto e o código corretos.
2. **Given** um e-mail que não corresponde a nenhuma conta cadastrada (ou uma conta inativa), **When** a recuperação é solicitada, **Then** a resposta observável pelo cliente é idêntica à do cenário de sucesso (nenhuma pista de que o e-mail existe ou não) — comportamento já implementado, que esta feature MUST preservar.
3. **Given** as credenciais do provedor de e-mail ausentes ou inválidas (como hoje, neste ambiente), **When** alguém pede a recuperação, **Then** a falha continua registrada no log do backend, de forma identificável para quem opera o sistema.

---

### User Story 2 - Botões de status de pagamento deixam claro que é uma ação, não um filtro (Priority: P1)

Como professora gerenciando Contas a Pagar/Receber, quero que fique claro, antes de eu confirmar, que clicar num botão de status muda o dado de verdade (não é só uma forma de visualizar), para eu não alterar um pagamento sem querer.

**Why this priority**: É um risco real de integridade de dado financeiro — um clique "de passagem" hoje já altera o status de verdade, sem nenhuma confirmação.

**Independent Test**: Abrir o detalhe de uma Conta a Pagar/Receber, clicar num status diferente do atual, e confirmar que aparece um passo de confirmação explícito antes da mudança ser aplicada; cancelar a confirmação e confirmar que nada muda.

**Acceptance Scenarios**:

1. **Given** uma conta com status "Pendente", **When** a professora clica em "Pago", **Then** o sistema pede confirmação explícita antes de aplicar a mudança, deixando claro que a data de pagamento será preenchida automaticamente.
2. **Given** o passo de confirmação aberto, **When** a professora cancela, **Then** o status permanece o mesmo de antes, sem nenhuma chamada ao backend.
3. **Given** o passo de confirmação aberto, **When** a professora confirma, **Then** o status muda exatamente como hoje (mesmo endpoint, mesmo efeito), e a tela reflete o novo status.
4. **Given** os 4 botões de status, **When** a professora olha para eles antes de clicar em qualquer um, **Then** o estilo visual comunica que são ações (mudam o dado), não uma seleção de filtro de exibição.
5. **Given** o indicador do status atual da conta, **When** a professora olha ou toca nele, **Then** nada acontece — não gera confirmação nem ação, pois já é o valor atual (comportamento já existente, preservado, independente de ser um botão desabilitado ou um indicador não-clicável).

---

### User Story 3 - Enter nos campos de login submete o formulário (Priority: P2)

Como usuária entrando no sistema, quero que apertar Enter no campo de e-mail/RA ou de senha entre no sistema, igual clicar em "Entrar", para não precisar tirar a mão do teclado.

**Why this priority**: É a menor das três em risco — a investigação prévia sugere que o comportamento já pode estar correto (Prioridade P2, mais baixa, pois pode não exigir nenhuma mudança de código, só confirmação).

**Independent Test**: Abrir a tela de login, preencher e-mail/RA e senha, e apertar Enter (sem clicar em "Entrar") — confirmar que o login é submetido exatamente como um clique no botão submeteria.

**Acceptance Scenarios**:

1. **Given** e-mail/RA e senha preenchidos, **When** a usuária aperta Enter em qualquer um dos dois campos, **Then** o formulário é submetido exatamente como clicar em "Entrar" (mesma validação, mesmo resultado de sucesso/erro).
2. **Given** um envio já em andamento (após Enter ou clique), **When** a usuária aperta Enter de novo antes da resposta chegar, **Then** nenhum segundo envio é disparado (mesma proteção contra duplo-envio que already existe para o clique no botão, hoje só parcialmente garantida via `disabled` do botão).
3. **Given** campos vazios, **When** a usuária aperta Enter, **Then** a mesma mensagem de validação de campos obrigatórios aparece, igual ao clique no botão.

---

### Edge Cases

- Se a verificação em navegador (Item 1) confirmar que Enter já funciona exatamente como esperado, esta fatia MUST documentar essa confirmação e não introduzir nenhuma mudança de código nesse ponto — só o reforço contra duplo-envio (Cenário 2 da US3) é aplicado de qualquer forma, por ser uma lacuna real e independente do Enter.
- Se, ao configurar credenciais reais de teste e enviar um e-mail de verdade (Item 2), aparecer um erro diferente da mera ausência de credencial (ex.: formatação do e-mail, host/porta errados), esse erro concreto MUST ser corrigido como parte desta feature.
- Confirmar/cancelar a mudança de status (Item 3) não deve afetar nenhum outro dado da conta (anexo de comprovante, observações) — só o campo `status` (e a data de pagamento, quando aplicável, comportamento já existente).
- Um usuário sem JavaScript ou com o formulário de login carregado de forma incomum (ex.: autofill do navegador) não é um cenário coberto por esta fatia — o foco é o comportamento padrão de teclado em uso normal.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O sistema MUST enviar de fato o e-mail de recuperação de senha para uma conta cadastrada e ativa que o solicitar, de forma verificável (e-mail realmente recebido numa caixa de entrada real), não apenas logicamente processado no backend.
- **FR-002**: Credenciais do provedor de envio de e-mail (usuário/senha SMTP, e-mail remetente) MUST ser configuradas fora do controle de versão (variável de ambiente ou User Secrets, nunca em `appsettings.json`), consistente com o padrão já adotado no projeto para segredos.
- **FR-003**: A resposta observável pelo cliente ao pedir recuperação de senha MUST continuar idêntica entre um e-mail cadastrado/ativo e um e-mail não-cadastrado/inativo — esta feature MUST NOT introduzir nenhuma diferença perceptível que permita descobrir se um e-mail existe no sistema (não-regressão do comportamento de segurança já implementado).
- **FR-004**: Uma falha real no envio (credencial inválida, provedor fora do ar) MUST continuar sendo registrada de forma identificável no log do backend, sem mudar a resposta ao cliente (FR-003).
- **FR-005**: Ao clicar em qualquer um dos botões de status (Pendente/Pago/Atrasado/Cancelado) numa Conta a Pagar ou a Receber, diferente do status atual, o sistema MUST exigir uma confirmação explícita do usuário antes de aplicar a mudança de status de verdade.
- **FR-006**: A confirmação (FR-005) MUST deixar claro, em texto, qual mudança está prestes a ser aplicada e qual efeito colateral ela tem quando houver um (ex.: marcar como Pago preenche a data de pagamento automaticamente; Cancelado encerra o registro sem excluí-lo) — mesmo texto informativo já existente hoje, só que apresentado no momento da decisão, não como uma nota separada e fácil de ignorar.
- **FR-007**: Cancelar a confirmação (FR-005) MUST NOT alterar o status da conta nem gerar nenhuma chamada ao backend.
- **FR-008**: O estilo visual dos 4 botões de status MUST comunicar que são ações que alteram dado, não uma seleção de filtro de exibição — MUST distinguir visualmente do padrão de filtro/aba já usado em outras telas do sistema.
- **FR-009**: O status atual da conta MUST ser exibido de forma não-interativa (sem gerar confirmação nem ação ao ser tocado) — preserva o resultado já existente hoje (clicar no status atual não faz nada), independente do mecanismo visual escolhido na implementação (ex.: um botão desabilitado ou um indicador não-clicável).
- **FR-010**: Pressionar Enter em qualquer um dos campos do formulário de login (e-mail/RA, senha) MUST submeter o formulário com o mesmo comportamento de clicar no botão "Entrar" (mesma validação de campos obrigatórios, mesmo tratamento de sucesso/erro).
- **FR-011**: O sistema MUST NOT permitir um segundo envio do formulário de login enquanto um envio anterior ainda está em andamento, independentemente de o gatilho ser um clique ou a tecla Enter.
- **FR-012**: Esta feature MUST NOT alterar nenhum outro campo de uma Conta a Pagar/Receber (anexo, observações) além do `status` (e da data de pagamento, quando já é o comportamento existente ao marcar como Pago).

### Key Entities

Nenhuma entidade nova — esta feature ajusta comportamento de UX/configuração sobre entidades e endpoints já existentes (`Professor`/`Aluno` para login e recuperação de senha; `Pagamento`/Conta a Pagar-Receber para o status).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% das solicitações de recuperação de senha para uma conta cadastrada e ativa resultam num e-mail realmente recebido, em até poucos minutos, uma vez que as credenciais do provedor estejam corretamente configuradas.
- **SC-002**: 0 diferenças observáveis pelo cliente entre pedir recuperação para um e-mail existente e um inexistente, em qualquer teste realizado.
- **SC-003**: 0 mudanças de status de Conta a Pagar/Receber ocorrem sem uma confirmação explícita, em 100% das tentativas.
- **SC-004**: Uma pessoa nova olhando a tela de status pela primeira vez identifica, sem precisar perguntar, que aqueles botões executam uma ação (não filtram a exibição).
- **SC-005**: 100% das tentativas de submeter o login apertando Enter (em qualquer um dos dois campos) têm o mesmo resultado que clicar em "Entrar".
- **SC-006**: 0 envios duplicados de login ocorrem ao apertar Enter repetidamente durante um envio em andamento.

## Assumptions

- **Item 1 (login)**: a leitura do código não encontrou nenhum bloqueio óbvio ao Enter; esta feature trata isso como "a confirmar rodando de verdade" antes de qualquer mudança de código — se a investigação em navegador confirmar que já funciona, o item se torna só uma verificação documentada (mais o reforço contra duplo-envio, que é uma lacuna real e separada).
- **Item 2 (e-mail)**: confirmar o envio de verdade exige credenciais reais (Brevo ou similar) — como não existem configuradas em nenhum lugar deste ambiente, será necessário pedir as credenciais reais durante a implementação para rodar um envio de teste e confirmar o recebimento; diferente das credenciais pontuais do Trello (specs/040-042), estas são a configuração contínua do produto e ficam persistidas em User Secrets, não descartadas ao final. **Limitação de ambiente confirmada na implementação**: a rede/máquina usada para implementar e testar esta feature bloqueia especificamente tráfego SMTP destinado à Brevo (testado nas portas 587, 2525 e 465 — mesmo sintoma nas três; SMTP para outro provedor, Gmail, funciona normalmente pela mesma rede, e HTTPS para o próprio host da Brevo também funciona), o padrão típico de um antivírus/firewall local com heurística de reputação contra relays de e-mail em massa. O código (`BrevoEmailSender`/`RecuperacaoSenhaService`) está correto; SC-001 (e-mail realmente recebido) fica confirmado apenas parcialmente nesta implementação (resposta e log corretos — FR-003/FR-004) e precisa ser revalidado num ambiente sem esse bloqueio (ex.: onde o backend for hospedado de verdade) antes de considerar o item 2 fechado de ponta a ponta.
- **Item 3 (status de pagamento)**: a confirmação (FR-005) é exigida para os 4 valores de status, não só para Pago/Cancelado — mais simples e consistente do que ter regras diferentes por valor, ao custo de exigir confirmação também nas transições sem efeito colateral (ex.: voltar para Pendente). O mecanismo exato (modal vs. estilo visual mais assertivo, ambas as opções sugeridas pelo usuário) é uma decisão de design deixada para `/speckit-plan`; esta spec exige o resultado (confirmação + comunicação visual clara), não a técnica.
- Nenhuma das 3 correções introduz endpoint novo no backend — os endpoints de login, recuperação de senha e atualização de status já existem e continuam com o mesmo contrato.
- Fora de escopo: qualquer redesenho maior das telas de Financeiro ou de Login além do que os 3 itens pedem; qualquer novo canal de recuperação de senha (SMS, pergunta de segurança); qualquer mudança na regra de negócio de quais transições de status são permitidas (isso já existe e não muda).
