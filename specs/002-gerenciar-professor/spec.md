# Feature Specification: Gerenciar Professor

**Feature Branch**: `002-gerenciar-professor`

**Created**: 2026-09-11

**Status**: Implemented (documentação retroativa)

**Input**: Registro retroativo do comportamento real da Estória 1 (Gerenciar Professor) da ERS "PROFESSORAS INDEPENDENTES_v1.4", com base no código-fonte atual, não na ERS original.

## Nota histórica

A ERS original (Estória 1) descrevia um fluxo de autocadastro da professora na instalação do sistema, com uma classe conceitual `PessoaInfo` compartilhada entre Professor e Aluno. O sistema implementado hoje diverge significativamente desse desenho: é **single-tenant** (só existe uma professora), o cadastro é feito por um ator **Admin** separado, e `PessoaInfo` nunca existiu como classe no código (ver spec [PessoaInfo](../019-modelo-pessoainfo/spec.md) para o registro dessa divergência específica). Este documento descreve o comportamento real, citando a ERS apenas como origem histórica.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Cadastro inicial da professora, feito pelo Admin (Priority: P1)

Como Admin do sistema, eu cadastro os dados da única professora que usa a plataforma (Nome, CPF, Email, Senha, Telefone), já que o sistema não permite autocadastro nem múltiplas professoras nesta fase.

**Why this priority**: Sem esse cadastro inicial, nenhum outro módulo do sistema pode ser usado (tudo depende de um Professor existente).

**Independent Test**: Pode ser testado chamando `POST /api/professor/cadastro-inicial` autenticado como Admin, uma única vez, e confirmando que uma segunda tentativa é rejeitada.

**Acceptance Scenarios**:

1. **Given** nenhuma professora cadastrada, **When** o Admin envia Nome, CPF válido, Email, Senha forte e Telefone, **Then** o cadastro é criado com sucesso.
2. **Given** uma professora já cadastrada, **When** o Admin tenta cadastrar uma segunda, **Then** o sistema rejeita com erro de conflito (o sistema é single-tenant).
3. **Given** um CPF ou Email já usados por outra professora, **When** o Admin tenta cadastrar, **Then** o sistema rejeita por duplicidade.

---

### User Story 2 - Professora visualiza e edita seu próprio perfil (Priority: P1)

Como professora autenticada, eu acesso "Meu Perfil" para visualizar meus dados (Nome, CPF, Email, Telefone) e editar Nome, Email e/ou Telefone.

**Why this priority**: É o uso recorrente do módulo no dia a dia da professora.

**Independent Test**: Autenticado como Professor, chamar `GET /api/professor/me` e depois `PUT /api/professor/me` alterando Nome/Email/Telefone.

**Acceptance Scenarios**:

1. **Given** a professora autenticada, **When** acessa "Meu Perfil", **Then** vê Nome, CPF, Email e Telefone atuais.
2. **Given** a tela de edição, **When** altera Nome e/ou Email e/ou Telefone e salva, **Then** os dados são atualizados (CPF não é editável por esse fluxo).
3. **Given** um novo Email já usado por outra conta, **When** a professora tenta salvar, **Then** o sistema rejeita por duplicidade.

---

### User Story 3 - Professora altera sua própria senha (Priority: P2)

Como professora autenticada, eu altero minha senha informando a senha atual e a nova senha, para manter minha conta segura.

**Why this priority**: É um fluxo de segurança importante, mas usado com menor frequência que a edição de perfil.

**Independent Test**: Chamar `PUT /api/professor/me/senha` com senha atual correta e uma nova senha válida.

**Acceptance Scenarios**:

1. **Given** a senha atual informada corretamente, **When** a professora informa uma nova senha com pelo menos 8 caracteres alfanuméricos, **Then** a senha é atualizada e todas as sessões (refresh tokens) da professora em outros dispositivos são revogadas, exigindo novo login.
2. **Given** uma senha atual incorreta, **When** a professora tenta trocar a senha, **Then** o sistema rejeita e mantém a senha anterior.
3. **Given** uma nova senha com caracteres especiais (ex.: `!`, `@`, espaço), **When** a professora tenta salvar, **Then** o sistema rejeita, pois a senha deve ser estritamente alfanumérica.

---

### User Story 4 - Professora esqueceu a senha (Priority: P2)

Como professora, se eu esquecer minha senha, posso solicitar uma redefinição por email, sem depender do Admin.

**Why this priority**: Evita que a professora fique bloqueada fora do sistema sem uma via de recuperação própria.

**Independent Test**: Chamar `POST /api/auth/esqueci-senha` com o email da professora e depois `POST /api/auth/redefinir-senha` com o token recebido.

**Acceptance Scenarios**:

1. **Given** um email de professora válido, **When** ela solicita recuperação de senha, **Then** um token de redefinição é gerado (o fluxo é exclusivo da Professora — Aluno e Admin não têm essa opção).
2. **Given** múltiplas tentativas em curto espaço de tempo, **When** a professora solicita recuperação repetidamente, **Then** o sistema aplica limitação de taxa (rate limiting) no endpoint.

---

### Edge Cases

- O que acontece se o Admin tentar cadastrar uma segunda professora? É rejeitado — o sistema é single-tenant por design atual, não apenas por regra de UI.
- O que acontece se a professora tentar editar o próprio CPF? Não é possível pelo endpoint `PUT /api/professor/me` — CPF não está entre os campos editáveis.
- O que acontece se a nova senha tiver menos de 8 caracteres ou caracteres não alfanuméricos? É rejeitada pela regra `SenhaForte`.
- O que acontece com as sessões ativas da professora em outros dispositivos após uma troca de senha? Todos os refresh tokens dela são revogados, forçando novo login em todos os dispositivos.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O sistema MUST permitir o cadastro de exatamente uma professora (Nome, CPF, Email, Senha, Telefone opcional), executado por um usuário com perfil Admin, e MUST rejeitar uma segunda tentativa de cadastro inicial.
- **FR-002**: O sistema MUST validar que o CPF informado no cadastro é único e possui dígito verificador válido.
- **FR-003**: O sistema MUST validar que o Email é único (checado tanto no cadastro quanto em qualquer atualização posterior, ignorando o próprio registro) e possui formato válido.
- **FR-004**: O sistema MUST exigir senha com no mínimo 8 caracteres e composta exclusivamente por caracteres alfanuméricos, tanto no cadastro quanto na troca de senha.
- **FR-005**: O sistema MUST permitir que a professora autenticada visualize seus próprios dados (Nome, CPF, Email, Telefone) e edite Nome, Email e Telefone (CPF não é editável por esse fluxo).
- **FR-006**: O sistema MUST permitir que a professora troque sua própria senha mediante confirmação da senha atual, e MUST revogar todos os refresh tokens ativos dela ao concluir a troca.
- **FR-007**: O sistema MUST oferecer um fluxo de recuperação de senha por email exclusivo para o perfil Professora, com limitação de taxa de solicitações.
- **FR-008**: O sistema MUST também permitir que um Admin visualize e edite os dados da professora por endpoints próprios (`GET/PUT /api/professor/admin`), independentemente do autoatendimento da professora.

### Key Entities *(include if feature involves data)*

- **Professor**: Representa a única professora do sistema, com Id, Nome, Cpf, Email, Senha (hash), Telefone (opcional) e Ativo. Não herda nem compõe nenhuma classe compartilhada de "pessoa" — seus campos são próprios e independentes dos de Aluno.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% das tentativas de cadastrar uma segunda professora são rejeitadas.
- **SC-002**: 100% das tentativas de cadastro/atualização com CPF ou Email duplicados são rejeitadas com mensagem de erro.
- **SC-003**: A professora consegue visualizar e atualizar seus dados de contato em uma única tela ("Meu Perfil"), sem depender do Admin para alterações do dia a dia.
- **SC-004**: Após uma troca de senha bem-sucedida, nenhuma sessão anterior da professora em outro dispositivo permanece válida.

## Assumptions

- O sistema é intencionalmente single-tenant nesta fase (conforme o escopo da ERS original, seção 1.2): suportar múltiplas professoras é um requisito adiado para versão futura, não uma falha.
- O papel "Admin" existe como ator técnico/operacional separado da professora, responsável pelo cadastro inicial e por eventuais correções administrativas do perfil dela.
- Não há registro de data de nascimento nem outros dados pessoais além dos listados nos requisitos — o escopo de dados da professora permanece deliberadamente enxuto.
