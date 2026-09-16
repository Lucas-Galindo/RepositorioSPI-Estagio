<!--
Sync Impact Report
==================
Version change: N/A (template, unratified) → 1.0.0
Rationale for MAJOR: Primeira ratificação real da constituição — o arquivo anterior era o
  scaffold do template do Spec Kit, com todos os placeholders ([PRINCIPLE_1_NAME] etc.) ainda
  não preenchidos. Não há "versão anterior" de conteúdo a comparar; 1.0.0 é o ponto de partida.
Modified principles: N/A (nenhum princípio pré-existente — todos os 5 são novos)
Added sections:
  - Core Principles: I. Exclusão Lógica, Nunca Física; II. Validação de Negócio Única e
    Centralizada no Backend; III. Nenhuma Capacidade Configurável Sem Implementação Real;
    IV. Autenticação e Segredos Seguros por Padrão; V. Documentação Retroativa como Registro
    Histórico Vinculante
  - Stack Tecnológico e Convenções (SECTION_2)
  - Fluxo de Trabalho com Spec Kit (SECTION_3)
  - Governance
Removed sections: nenhuma (todas as seções do template foram preenchidas, nenhuma removida)
Templates requiring updates:
  - .specify/templates/plan-template.md: ⚠ pending manual review — o "Constitution Check" do
    plan.md agora deve avaliar os 5 princípios reais listados abaixo, não mais "nenhuma
    constituição definida" (era o estado assumido em specs/001 a specs/020, geradas antes desta
    ratificação)
  - .specify/templates/spec-template.md: ✅ sem mudança necessária (não referencia princípios
    específicos por nome)
  - .specify/templates/tasks-template.md: ✅ sem mudança necessária
Follow-up TODOs:
  - TODO(RATIFICATION_DATE): a data de adoção original deste conjunto de princípios não é
    rastreável (eles descrevem convenções já vigentes no código antes desta constituição
    existir formalmente) — usada a data desta ratificação (2026-09-11) tanto como Ratified
    quanto como Last Amended, já que é o primeiro registro formal.
-->

# SPI (Sistema para Professoras Independentes) Constitution

## Core Principles

### I. Exclusão Lógica, Nunca Física

Todo registro de domínio que possa ter histórico dependente (Aluno, Matéria, Turma, Aula, e
qualquer entidade futura equivalente) MUST ser desativado por um indicador de estado (`Ativo`
ou campo equivalente) ao ser "excluído" pelo usuário, e MUST NOT ser removido fisicamente do
banco de dados por essa ação. Toda funcionalidade de exclusão MUST preservar os vínculos e o
histórico já existentes (aulas, pagamentos, frequência) associados ao registro desativado.

**Rationale**: É o padrão já implementado de forma consistente em todos os módulos de cadastro
do sistema (ver [Gerenciar Alunos](../../specs/003-gerenciar-alunos/spec.md), [Gerenciar
Matérias](../../specs/004-gerenciar-materias/spec.md), [Gerenciar Turma](../../specs/006-gerenciar-turma/spec.md)).
Divergir dele quebraria relatórios financeiros e pedagógicos que dependem de dados históricos
mesmo após um aluno, turma ou matéria deixar de estar em uso ativo.

### II. Validação de Negócio Única e Centralizada no Backend

Toda regra de negócio com potencial de ser verificada a partir de mais de um ponto de entrada
da interface (por exemplo, um mesmo formulário reaproveitado em duas telas, ou uma ação
disponível tanto por um formulário completo quanto por um atalho) MUST ser implementada uma
única vez no backend e reaproveitada por todos os pontos de entrada correspondentes. O
frontend MUST NOT duplicar ou reimplementar lógica de validação de negócio — ele apenas exibe
os erros retornados pela API.

**Rationale**: É o padrão comprovado na validação de conflito de horário de Aula, implementada
uma única vez no backend e reaproveitada tanto pelo formulário completo de cadastro quanto pelo
agendamento rápido da Agenda, sem nenhuma divergência entre os dois caminhos (ver [Gerenciar
Aula](../../specs/005-gerenciar-aula/spec.md) e [Agendamento](../../specs/010-agendamento/spec.md)).
Duplicar validação em múltiplos lugares é a forma mais comum de bugs de regra de negócio
divergente entre telas.

### III. Nenhuma Capacidade Configurável Sem Implementação Real (NON-NEGOTIABLE)

O sistema MUST NOT oferecer, como opção configurável pelo usuário, qualquer valor, canal ou
capacidade que o backend não processe de fato. Antes de expor uma nova opção em um formulário,
enum, dropdown ou campo de configuração, MUST existir a implementação funcional correspondente
que efetivamente processa essa opção; se a capacidade for removida ou nunca tiver sido
implementada, a opção correspondente MUST ser removida da validação e da interface no mesmo
momento.

**Rationale**: Este princípio nasce diretamente de um bug real e documentado do sistema: os
canais "WhatsApp" e "SMS" eram aceitos como opção válida ao cadastrar um Lembrete, mas o
`LembreteDispatcherService` nunca os despachava (`continue` explícito no código), fazendo
lembretes cadastrados com esses canais ficarem "Pendentes" para sempre, silenciosamente, sem
erro visível para a professora (ver [Lembretes — Somente por
Email](../../specs/001-lembretes-canal-email/spec.md)). A correção foi restringir a validação
a "Email", único canal realmente despachado. Este princípio existe para que esse tipo de
divergência silenciosa entre "o que a UI permite" e "o que o backend realmente faz" não se
repita em nenhum módulo futuro.

### IV. Autenticação e Segredos Seguros por Padrão

Toda senha MUST ser armazenada apenas como hash (nunca em texto plano), usando o mecanismo de
hashing já padronizado no projeto. Toda sessão MUST usar um token de acesso de curta duração
combinado com um token de renovação de duração maior, armazenado apenas como hash e sujeito a
rotação a cada uso — nunca um token de sessão de vida longa e não renovável. Nenhuma chave,
segredo ou credencial MUST ser versionada em `appsettings.json` ou equivalente; segredos MUST
viver fora do controle de versão (variáveis de ambiente, user secrets, ou mecanismo
equivalente).

**Rationale**: É o padrão já implementado e validado em [Autenticar
Usuário](../../specs/007-autenticar-usuario/spec.md) (BCrypt para senha, JWT de curta duração
mais refresh token opaco rotativo com hash SHA-256 persistido) e reforçado por
`src/SPI.Api/appsettings.Development.json` e `**/appsettings.*.local.json` estarem
explicitamente no `.gitignore` do projeto. Regressões de segurança de autenticação são caras de
corrigir depois de expostas; este princípio impede que uma decisão de conveniência (senha em
texto plano, token sem expiração, segredo commitado) seja introduzida silenciosamente.

### V. Documentação Retroativa como Registro Histórico Vinculante

Quando o comportamento real implementado do sistema divergir de qualquer documento de
requisitos anterior (a ERS original ou qualquer especificação desatualizada), a spec retroativa
correspondente em `specs/001` a `specs/019` — e qualquer spec futura marcada como
"documentação retroativa" — MUST ser tratada como a descrição vigente do comportamento atual,
não o documento original. Alterações futuras que mudem intencionalmente esse comportamento
MUST atualizar a spec correspondente (ou criar uma nova, referenciando a anterior) em vez de
deixá-la desatualizada silenciosamente.

**Rationale**: As specs `001`-`019` foram produzidas lendo o código-fonte real (entidades,
validators, controllers, componentes de frontend) e citam explicitamente cada ponto onde
divergem da ERS original de "PROFESSORAS INDEPENDENTES v1.4" — por exemplo, a inexistência da
classe `PessoaInfo` prevista na ERS (ver [Modelo Conceitual
PessoaInfo](../../specs/019-modelo-pessoainfo/spec.md)), o sistema ser single-tenant com
cadastro pelo Admin (ver [Gerenciar Professor](../../specs/002-gerenciar-professor/spec.md)), e
a maioria dos relatórios da ERS estarem implementados no backend mas "órfãos" sem consumidor no
frontend (ver [Relatório de Agenda](../../specs/012-relatorio-agenda/spec.md) e specs
correlatas). Sem este princípio, um documento desatualizado poderia voltar a ser tratado como
fonte de verdade, reintroduzindo decisões já corrigidas na prática.

## Stack Tecnológico e Convenções

- **Backend**: C# / ASP.NET Core Web API (.NET), organizado em `SPI.Domain` (entidades),
  `SPI.Application` (DTOs, validators FluentValidation, services), `SPI.Infrastructure`
  (repositórios, EF Core, serviços de background) e `SPI.Api` (controllers). Toda nova regra de
  validação de request MUST usar FluentValidation, seguindo o padrão já estabelecido
  (`*RequestValidator.cs` ao lado do DTO correspondente).
- **Frontend**: Next.js (App Router) com React e TypeScript, em `frontend/`. Componentes e
  chamadas de API residem em `frontend/components/` e `frontend/lib/api/`, espelhando 1:1 os
  DTOs do backend (padrão de comentário `/** Espelha ... */` já em uso).
- **Banco de dados**: MySQL, com schema versionado em `database/*.sql` e uso pontual de stored
  procedures para operações que exigem geração controlada de identificadores (ex.: RA de
  aluno) ou atualização atômica de status. Novas stored procedures MUST ser documentadas com
  comentário explicando por que a lógica não está inteiramente na camada de aplicação.
  Migrações de dados retroativas (correção de dados existentes por mudança de regra de negócio)
  MUST viver como script SQL numerado em `database/`, nunca como alteração manual direta em
  produção.
- **Autenticação e perfis**: três perfis (`Professor`, `Aluno`, `Admin`) via enum
  `PerfilUsuario`, com login unificado (identificador único, perfil inferido automaticamente).
  O sistema é single-tenant nesta fase (uma única professora por instalação) — qualquer mudança
  para multi-tenant MUST ser tratada como uma mudança de escopo maior, não incremental.
- **Idioma do domínio**: nomes de entidades, campos, mensagens de erro e rotas de UI usam
  Português (Brasil), consistente em todo o código já existente; novo código MUST seguir essa
  convenção, e não misturar nomenclatura em inglês para conceitos de domínio já nomeados em
  português (ex.: `Aluno`, `Turma`, `Lembrete`, não `Student`, `Class`, `Reminder`).

## Fluxo de Trabalho com Spec Kit

- Este projeto usa o Spec Kit (`/speckit-specify`, `/speckit-plan`, `/speckit-tasks`,
  `/speckit-implement`) para toda feature nova ou registro retroativo de comportamento
  existente. O diretório `specs/` é a fonte de verdade viva sobre o que o sistema faz — mais
  atual e confiável que a ERS original sempre que os dois divergirem (ver Princípio V).
- Especificações retroativas (como `specs/001` a `specs/019`) documentam o comportamento já
  implementado e validado em produção; elas não substituem a necessidade de planejamento
  (`/speckit-plan`) e tarefas (`/speckit-tasks`) para mudanças *futuras* de comportamento —
  apenas registram o estado passado/atual como referência.
- Toda mudança de comportamento (não apenas visual/layout) MUST passar pelo ciclo completo
  `/speckit-specify` → `/speckit-clarify` (se houver ambiguidade) → `/speckit-plan` →
  `/speckit-tasks` → `/speckit-implement`, na ordem, sem pular etapas, exceto quando o próprio
  usuário pedir explicitamente um ajuste pontual de baixo risco (ex.: correção visual isolada).
- Checklists de qualidade de especificação (`checklists/requirements.md`) gerados pelo
  `/speckit-specify` e pelo `/speckit-clarify` MUST estar com todos os itens aprovados antes de
  avançar para `/speckit-plan`, salvo justificativa explícita registrada nas notas do checklist.

## Governance

Esta constituição prevalece sobre a ERS original ("PROFESSORAS INDEPENDENTES_v1.4") e sobre
qualquer prática de desenvolvimento informal sempre que houver conflito. Ela não prevalece
sobre as specs retroativas de comportamento real (`specs/001`-`019`) quanto a *fatos* sobre o
sistema — essas specs são a fonte factual; esta constituição estabelece os *princípios* que
essas mesmas specs revelaram e que devem orientar decisões futuras.

**Emendas**: qualquer alteração a este documento MUST ser proposta explicitamente (nova
execução de `/speckit-constitution` ou edição direta revisada por outra pessoa), MUST atualizar
o número de versão conforme a política de versionamento semântico abaixo, e MUST atualizar o
campo "Last Amended" com a data da mudança. Alterações que removam ou reescrevam um princípio
existente de forma incompatível com o comportamento atualmente documentado nas specs
retroativas MUST justificar explicitamente a divergência (ou atualizar a spec correspondente na
mesma mudança).

**Versionamento semântico**:
- MAJOR: remoção ou redefinição incompatível de um princípio existente, ou mudança que altere
  o significado de "compliance" para trabalho já em andamento.
- MINOR: adição de um novo princípio ou seção, ou expansão material de orientação existente.
- PATCH: esclarecimentos de redação, correções de erro de digitação, refinamentos não
  semânticos.

**Revisão de conformidade**: toda revisão de código (`/code-review` ou revisão manual) que
toque em exclusão de registros, validação de regra de negócio duplicada entre frontend/backend,
introdução de nova opção configurável, autenticação/segredos, ou divergência entre uma spec
retroativa e o código, MUST verificar conformidade com os 5 princípios acima antes de aprovar a
mudança. Complexidade adicional que viole um princípio MUST ser justificada explicitamente no
PR ou na spec correspondente — nunca introduzida silenciosamente.

**Version**: 1.0.0 | **Ratified**: 2026-09-11 | **Last Amended**: 2026-09-11
