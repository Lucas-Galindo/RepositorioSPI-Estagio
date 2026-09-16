# Feature Specification: Autenticar Usuário

**Feature Branch**: `007-autenticar-usuario`

**Created**: 2026-09-11

**Status**: Implemented (documentação retroativa)

**Input**: Registro retroativo do comportamento real da Estória 7 (Autenticar Usuário) da ERS "PROFESSORAS INDEPENDENTES_v1.4", com base no código-fonte atual, não na ERS original.

## Nota histórica

A ERS original previa uma tela de login com **seletor de perfil** (Professora ou Aluno) e expiração de sessão **por inatividade**. Nenhum dos dois existe na implementação real: o login é único e unificado (o perfil é inferido automaticamente a partir do identificador informado), e a sessão expira por um tempo fixo (TTL), não por inatividade. Além disso, o Aluno consegue de fato se autenticar e receber um token válido, mas hoje não existe nenhuma área do sistema (endpoint de API) que reconheça esse perfil — ele loga, mas não tem para onde ir. Este documento descreve o comportamento real.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Login unificado por email ou RA (Priority: P1)

Como usuário do sistema (professora, aluno ou administrador), eu informo um único identificador (meu email, ou meu RA se for aluno) e minha senha, sem precisar escolher previamente meu tipo de perfil, para acessar o sistema.

**Why this priority**: É o ponto de entrada de todo o sistema.

**Independent Test**: Chamar `POST /api/auth/login` com um email de professora e senha correta, e separadamente com um RA de aluno e senha correta, confirmando que ambos autenticam sem selecionar perfil.

**Acceptance Scenarios**:

1. **Given** um identificador contendo "@", **When** o usuário informa a senha correta, **Then** o sistema tenta localizar o cadastro nessa ordem: Professor, depois Aluno (por email), depois Admin — usando o primeiro que corresponder.
2. **Given** um identificador sem "@", **When** o usuário informa a senha, **Then** o sistema o trata como RA e busca exclusivamente no cadastro de Aluno.
3. **Given** credenciais corretas e conta ativa, **When** o login é validado, **Then** o sistema emite um token de acesso (access token) de curta duração e um token de renovação (refresh token) de duração mais longa.
4. **Given** credenciais incorretas ou conta inativa, **When** o usuário tenta logar, **Then** o sistema rejeita sem indicar qual campo especificamente está incorreto.

---

### User Story 2 - Sessão de longa duração com "Manter conectada" (Priority: P1)

Como usuário, ao marcar "Manter conectada" no login, minha sessão é restaurada automaticamente da próxima vez que eu abrir o sistema, sem precisar logar de novo — enquanto meu token de renovação continuar válido.

**Why this priority**: Evita relogins constantes no uso diário da professora.

**Independent Test**: Fazer login marcando "Manter conectada", recarregar a página e confirmar que a sessão é restaurada automaticamente via renovação de token.

**Acceptance Scenarios**:

1. **Given** "Manter conectada" marcado no login, **When** o usuário recarrega a página em uma sessão futura, **Then** o sistema usa o token de renovação salvo para obter um novo token de acesso automaticamente, sem exigir nova digitação de credenciais.
2. **Given** "Manter conectada" **não** marcado, **When** o usuário recarrega a página, **Then** a sessão não é restaurada — é necessário logar novamente.
3. **Given** um token de renovação usado uma vez, **When** o sistema o renova, **Then** o token antigo é invalidado e substituído por um novo (rotação), impedindo reuso do token anterior.

---

### User Story 3 - Encerrar sessão (logout) (Priority: P2)

Como usuário autenticado, eu posso encerrar minha sessão explicitamente, invalidando meu token de renovação atual.

**Why this priority**: Permite controle de segurança sobre o próprio dispositivo/sessão.

**Independent Test**: Chamar `POST /api/auth/logout` com um refresh token válido e confirmar que uma tentativa posterior de renovação com esse mesmo token falha.

**Acceptance Scenarios**:

1. **Given** uma sessão ativa, **When** o usuário faz logout, **Then** o refresh token daquela sessão é revogado e removido do armazenamento local do navegador.
2. **Given** um refresh token já revogado, **When** o sistema tenta usá-lo para renovar a sessão, **Then** a renovação falha, exigindo novo login.

---

### Edge Cases

- O que acontece se um Aluno fizer login com sucesso e tentar acessar qualquer funcionalidade do sistema? Hoje, nenhum endpoint de API autoriza o perfil Aluno — ele recebe um token válido, mas não há nenhuma tela/rota da aplicação preparada para esse perfil.
- Existe expiração de sessão por inatividade (o usuário fica parado e é deslogado)? Não — a única expiração existente é por tempo fixo desde a emissão do token (TTL do access token e do refresh token), independentemente de o usuário estar ativo ou não.
- O que acontece se o usuário esquecer a senha? Ver o fluxo de recuperação, exclusivo da Professora, descrito em [Gerenciar Professor](../002-gerenciar-professor/spec.md).
- O token de acesso é renovado automaticamente enquanto o usuário usa o sistema ativamente (antes de expirar)? Não foi identificado esse mecanismo — a renovação automática ocorre apenas ao carregar/recarregar a página, não em segundo plano durante o uso contínuo.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O sistema MUST oferecer um único formulário de login (identificador + senha), sem exigir seleção prévia de perfil (Professora/Aluno/Admin).
- **FR-002**: O sistema MUST inferir o perfil do usuário automaticamente: identificadores contendo "@" são tratados como email (buscando em Professor, depois Aluno, depois Admin); identificadores sem "@" são tratados como RA (buscando exclusivamente em Aluno).
- **FR-003**: O sistema MUST emitir, em um login bem-sucedido, um token de acesso de curta duração e um token de renovação de duração mais longa.
- **FR-004**: O sistema MUST oferecer a opção "Manter conectada", que, quando marcada, permite restaurar a sessão automaticamente em acessos futuros usando o token de renovação salvo localmente.
- **FR-005**: O sistema MUST rotacionar o token de renovação a cada uso (invalidando o anterior e emitindo um novo), impedindo reuso de um token de renovação já utilizado.
- **FR-006**: O sistema MUST permitir logout explícito, revogando o token de renovação da sessão atual.
- **FR-007**: O sistema MUST armazenar senhas apenas como hash (nunca em texto plano) para todos os perfis (Professor, Aluno, Admin).

### Key Entities *(include if feature involves data)*

- **RefreshToken**: token de renovação de sessão, armazenado apenas como hash, vinculado a um usuário e perfil, com data de expiração e possibilidade de revogação/substituição (rotação).
- **PerfilUsuario**: enumeração dos três perfis existentes no sistema — Professor, Aluno, Admin.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% dos logins bem-sucedidos ocorrem sem que o usuário precise informar previamente seu tipo de perfil.
- **SC-002**: 100% das tentativas de reutilizar um token de renovação já rotacionado ou revogado são rejeitadas.
- **SC-003**: Um usuário que marcou "Manter conectada" tem sua sessão restaurada automaticamente ao reabrir o sistema, sem precisar digitar credenciais novamente, enquanto o token de renovação continuar válido.

## Assumptions

- A ausência de área funcional para o perfil Aluno é aceita como estado atual do sistema (escopo ainda não implementado), não como um defeito desta funcionalidade de autenticação em si — a autenticação em si funciona corretamente para os três perfis.
- A ausência de expiração por inatividade é aceita como decisão de design atual (TTL fixo é considerado suficiente nesta fase), não uma lacuna a ser corrigida por este registro retroativo.
