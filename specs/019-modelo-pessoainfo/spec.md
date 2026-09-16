# Feature Specification: Modelo Conceitual PessoaInfo (registro de divergência)

**Feature Branch**: `019-modelo-pessoainfo`

**Created**: 2026-09-11

**Status**: Not implemented — a classe descrita na ERS nunca existiu no código (documentação retroativa)

**Input**: Registro retroativo da divergência entre o Modelo Conceitual de Classes da ERS "PROFESSORAS INDEPENDENTES_v1.4" (seção 2.2 e diagramas de contexto das Estórias 1 e 2) e a estrutura de dados real implementada.

## Nota histórica

A ERS original desenha `PessoaInfo` como uma classe conceitual compartilhada — `Professor` e `Alunos` apontam para ela (via associação/herança nos diagramas), carregando Nome, Telefone, Email, Senha e CPF em um único lugar comum. Essa classe **nunca foi implementada no código**: não existe `PessoaInfo.cs`, `Pessoa.cs`, nem qualquer interface ou classe base equivalente em `src/SPI.Domain/Entities`. Este documento não descreve uma funcionalidade de usuário — é o registro formal dessa divergência estrutural, para que futuras leituras da ERS não presumam uma abstração que não existe no sistema real.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Entender por que Professor e Aluno têm campos duplicados e independentes (Priority: P1)

Como pessoa lendo a documentação ou o código do sistema, eu preciso saber que `Professor` e `Aluno` são entidades totalmente independentes, cada uma com seus próprios campos de Nome/Email/Telefone/Senha, para não presumir que uma alteração em uma classe compartilhada afetaria ambas.

**Why this priority**: Evita retrabalho ou bugs causados pela suposição incorreta de uma abstração compartilhada que não existe.

**Independent Test**: Inspecionar `src/SPI.Domain/Entities/Professor.cs` e `src/SPI.Domain/Entities/Aluno.cs` e confirmar que nenhum dos dois referencia uma classe base ou composição comum para esses campos.

**Acceptance Scenarios**:

1. **Given** o código-fonte do domínio, **When** se busca por `PessoaInfo`, `Pessoa` ou qualquer classe base equivalente, **Then** nenhuma é encontrada — `Professor` e `Aluno` declaram Nome, Email, Telefone e Senha (ou seus equivalentes) diretamente, de forma independente.
2. **Given** uma alteração de regra de validação de Email ou Senha em `Professor`, **When** aplicada, **Then** ela **não** se propaga automaticamente para `Aluno` — cada entidade tem seu próprio validator, com regras próprias (por exemplo, CPF é obrigatório em Professor mas opcional em Aluno).

---

### Edge Cases

- Existe algum ponto do sistema onde Professor e Aluno compartilham lógica de autenticação, mesmo sem uma classe `PessoaInfo`? Sim — o serviço de autenticação (`AutenticacaoService`) trata os três perfis de forma polimórfica em tempo de execução (buscando em cada tabela separadamente), mas isso é lógica de serviço, não uma estrutura de dados compartilhada (ver [Autenticar Usuário](../007-autenticar-usuario/spec.md)).
- Isso significa que os campos são inconsistentes entre Professor e Aluno? Sim, intencionalmente: CPF é obrigatório para Professor mas opcional para Aluno; Aluno tem campos que Professor não tem (Ra, TelefoneResponsavel, EmailResponsavel, ValorAula, Frequencia); Professor não tem equivalentes a esses.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O sistema MUST manter `Professor` e `Aluno` como entidades de domínio totalmente independentes, sem herança nem composição de uma classe/estrutura compartilhada para seus dados pessoais (Nome, Email, Telefone, Senha).
- **FR-002**: Toda regra de validação de dados pessoais (formato de email, força de senha, unicidade de CPF) MUST ser definida separadamente para cada entidade, permitindo que as regras divirjam entre Professor e Aluno conforme a necessidade de negócio de cada perfil.

### Key Entities *(include if feature involves data)*

- **Professor**: Id, Nome, Cpf (obrigatório), Email, Senha, Telefone (opcional), Ativo.
- **Aluno**: Id, Ra, Nome, Cpf (opcional), TelefoneAluno, TelefoneResponsavel, Email (opcional), Senha (opcional), EmailResponsavel, ValorAula, Frequencia, Ativo.
- Nenhuma classe/entidade `PessoaInfo` existe para unificar os dois modelos acima.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Qualquer nova leitura da ERS original por um membro da equipe, ao chegar ao Modelo Conceitual de Classes (seção 2.2) ou aos diagramas de contexto das Estórias 1 e 2, encontra este documento referenciado como a correção factual sobre a inexistência de `PessoaInfo`, evitando decisões de design baseadas na abstração desenhada na ERS original.

## Assumptions

- Este documento é deliberadamente estrutural, não funcional — não descreve uma tela ou fluxo de usuário, mas registra uma decisão de modelagem de dados relevante para qualquer trabalho futuro que toque Professor ou Aluno.
- Não há indício no histórico do código de que `PessoaInfo` tenha existido em algum momento e sido removida — a divergência parece ter surgido já na fase inicial de implementação, divergindo do desenho conceitual da ERS desde o início.
