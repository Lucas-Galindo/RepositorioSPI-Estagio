# Research: Endereço do Aluno e do Responsável

## Contexto levantado no código atual

- `Aluno` (entidade, DTOs, tabela `alunos`) não tem nenhum campo de endereço hoje. Tem
  `TelefoneResponsavel`/`EmailResponsavel` (nullable), exigidos apenas via FluentValidation
  quando `EhMenorDeIdade` é `true` no request — o mesmo padrão que o endereço do responsável
  deve seguir.
- **`EhMenorDeIdade` não é um campo persistido.** Existe só como flag transiente em
  `CadastrarAlunoRequest`/`AtualizarAlunoRequest`, usada apenas para decidir a regra de validação
  no momento do submit — não há coluna no banco, não há campo na entidade nem no `AlunoResponse`.
  Confirmado também que `AlunoForm.tsx` hoje inicializa `ehMenorDeIdade` sempre como
  `useState(false)`, mesmo ao editar um Aluno que já tem `telefoneResponsavel`/`emailResponsavel`
  preenchidos — ou seja, reabrir o formulário de edição de um Aluno menor de idade já cadastrado
  hoje não remarca o indicador automaticamente (falha pré-existente, não introduzida por esta
  feature, mas que esta feature precisa evitar herdar — ver Decisão 1).
- `CadastrarAsync` insere o Aluno via `CadastrarViaProcedureAsync` (procedure MySQL, parâmetros
  posicionais) — usada para gerar o RA atomicamente. `AtualizarAsync` faz atribuição direta de
  propriedades com fallback `??` (não apaga campos não enviados) e `SalvarAlteracoesAsync` via
  EF Core — padrão simples de update, sem procedure.
- Convenção de migração: arquivos SQL numerados sequencialmente em `database/`, sem framework de
  migração — o mais recente é `12_anexo_comprovante_financeiro.sql`; o próximo é `13_...`.
- Não existe hoje nenhuma chamada a API externa de terceiros feita diretamente do frontend
  (tudo passa pelo backend via `lib/api/client.ts`). A integração com ViaCEP seria a primeira do
  tipo neste projeto.

## Decisão 1: Inferir "é menor de idade" a partir dos dados já persistidos, sem nova coluna

**Decision**: Ao abrir o formulário de edição de um Aluno, `ehMenorDeIdade` (e, por
consequência, a visibilidade da seção de endereço do responsável) é inicializado como `true` se
o Aluno já tiver `telefoneResponsavel`, `emailResponsavel` OU qualquer campo de endereço do
responsável preenchido — sem adicionar uma coluna `ehMenorDeIdade` nova ao banco.

**Rationale**: A spec (FR-004, Edge Case da US2) exige que a seção de endereço do responsável
apareça "quando o Aluno é marcado como menor de idade" — isso só funciona de forma útil se o
sistema lembrar esse estado entre uma edição e outra. Como não há coluna persistida para isso
hoje, e adicionar uma seria uma mudança de escopo maior (nova migração, nova regra de
sincronização com os campos de responsável já existentes) não pedida explicitamente, a saída
mais barata e consistente com o padrão já usado (inferir dado a partir de campos já existentes)
é usar a presença de dados de responsável como sinal. Isso também corrige, como efeito colateral
necessário para esta feature funcionar corretamente, a falha pré-existente do `AlunoForm.tsx`
(checkbox sempre iniciando `false` na edição).

**Alternatives considered**:
- **Adicionar coluna `eh_menor_de_idade BOOLEAN` à tabela `alunos`**: rejeitada — escopo maior
  que o pedido (nova migração, nova regra de negócio persistida), quando um sinal já existente
  (presença de dados de responsável) resolve o problema real sem mudança de schema adicional.

## Decisão 2: Endereço do responsável "espelhado", não duplicado fisicamente

**Decision**: Quando `responsavel_mesmo_endereco` (novo campo persistido, boolean, default
`true`) está marcado, o endereço do responsável retornado pela API é sempre uma cópia em tempo
de leitura do endereço do próprio Aluno — as colunas de endereço do responsável no banco
permanecem `NULL`/não usadas enquanto essa flag for `true`. Só quando desmarcado (`false`) as 7
colunas de endereço do responsável no banco passam a ser lidas/gravadas com valores próprios.

**Rationale**: Já documentado em spec.md (Assumptions) — evita que o endereço do responsável
"congele" numa cópia desatualizada se o endereço do Aluno mudar depois enquanto a flag continuar
marcada. É o comportamento que a spec pede explicitamente ("se o endereço do Aluno mudar depois,
o endereço do responsável espelhado muda junto").

## Decisão 3: Colunas de endereço nullable no banco; obrigatoriedade só na validação de negócio

**Decision**: Todas as 14 novas colunas (7 do Aluno + 7 do responsável) são `NULL`-áveis no
banco. A obrigatoriedade de CEP/Rua/Número (FR-002) é imposta só pelo FluentValidation nos
DTOs de cadastro/edição — nunca por `NOT NULL` no schema.

**Rationale**: Alunos já cadastrados antes desta feature não têm endereço (spec.md Assumptions)
— uma coluna `NOT NULL` quebraria a leitura desses registros existentes. É o mesmo padrão já
usado por `telefone_responsavel`/`email_responsavel` (nullable no banco, obrigatórios só
condicionalmente via `When(x => x.EhMenorDeIdade, ...)` no validator).

## Decisão 4: Não alterar a stored procedure de cadastro

**Decision**: `CadastrarViaProcedureAsync` continua com a assinatura atual (gera RA + insere os
campos já existentes). O `AlunoService.CadastrarAsync` passa a, logo após obter o `id` do novo
Aluno, buscar a entidade recém-criada e setar os campos de endereço diretamente via EF
(`SalvarAlteracoesAsync`), antes de mapear a resposta — mesmo padrão já usado por `AtualizarAsync`.

**Rationale**: A procedure existe especificamente para geração atômica do RA (ver Stack
Tecnológico da constituição: "stored procedures para operações que exigem geração controlada de
identificadores"). Endereço não tem essa exigência — é inserção/atualização direta de colunas
simples. Estender a assinatura da procedure (14 parâmetros novos) seria uma mudança
desproporcional; inserir os campos de endereço num segundo passo, dentro da mesma transação
lógica do `CadastrarAsync`, atinge o mesmo resultado com uma mudança muito menor e mais fácil de
revisar.

## Decisão 5: ViaCEP chamado diretamente do frontend, sem proxy no backend

**Decision**: Um novo helper `frontend/lib/viacep.ts` chama
`https://viacep.com.br/ws/{cep}/json/` diretamente do navegador (primeira integração externa
client-side do projeto), disparado quando o campo CEP atinge 8 dígitos, com falha tratada
silenciosamente (sem bloquear o formulário, sem toast de erro — os campos continuam editáveis).

**Rationale**: FR-012 exige que a busca nunca bloqueie o cadastro; ViaCEP é um serviço público
sem necessidade de credenciais, e não há dado sensível do sistema envolvido (só o CEP digitado
pela usuária) — não há motivo de segurança/privacidade para proxiar pelo backend. Chamar
diretamente evita tráfego e latência extra passando pela API própria sem necessidade.

**Alternatives considered**:
- **Proxiar via um endpoint novo no backend**: rejeitada — adicionaria um endpoint só para
  repassar uma chamada pública sem lógica de negócio nenhuma no meio; nenhum ganho de segurança
  real, já que o CEP não é dado sensível.

## Escopo de banco de dados

Nova migração `database/13_endereco_aluno_responsavel.sql`: `ALTER TABLE alunos ADD COLUMN` para
as 7 colunas de endereço do Aluno (`cep`, `rua`, `numero`, `complemento`, `bairro`, `cidade`,
`estado`, todas nullable), mais `responsavel_mesmo_endereco BOOLEAN NOT NULL DEFAULT TRUE`, mais
as 7 colunas de endereço do responsável com prefixo `responsavel_` (também nullable) — ver
data-model.md para o SQL exato.
